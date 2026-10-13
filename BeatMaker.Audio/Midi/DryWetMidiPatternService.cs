using System.Buffers.Binary;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace BeatMaker.Audio.Midi;

/// <summary>Reads and writes Standard MIDI Files with normalized PPQ timing.</summary>
public sealed class DryWetMidiPatternService : IMidiPatternService
{
    private const long MaximumMidiFileBytes = 16L * 1024 * 1024;
    private const int MaximumTempoMetaEventBytes = 32;

    /// <inheritdoc />
    public async Task<MidiPatternImportResult> ImportAsync(
        string filePath,
        string patternName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(patternName);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("MIDI file was not found.", fullPath);
        }

        if (fileInfo.Length > MaximumMidiFileBytes)
        {
            throw new InvalidDataException("MIDI file exceeds the supported 16 MiB size limit.");
        }

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (bytes.Length < 14 || !bytes.AsSpan(0, 4).SequenceEqual("MThd"u8))
        {
            throw new InvalidDataException("File is not a valid Standard MIDI File.");
        }

        var division = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(12, 2));
        if ((division & 0x8000) != 0 || division == 0)
        {
            throw new InvalidDataException("SMPTE-timed MIDI files are not supported; PPQ timing is required.");
        }

        MidiFile midiFile;
        using (var stream = new MemoryStream(bytes, writable: false))
        {
            midiFile = MidiFile.Read(stream);
        }

        var sourceTicksPerQuarter = midiFile.TimeDivision;
        if (sourceTicksPerQuarter is not TicksPerQuarterNoteTimeDivision ppq || ppq.TicksPerQuarterNote == 0)
        {
            throw new InvalidDataException("MIDI file must use PPQ time division.");
        }

        var ticksPerQuarter = ppq.TicksPerQuarterNote;
        var noteStarts = new Dictionary<(FourBitNumber Channel, SevenBitNumber Note), Queue<PendingNote>>();
        var notes = new List<Note>();
        long maximumEndTick = 0;
        int? tempoBpm = null;
        var tempoEvents = midiFile.GetTrackChunks()
            .SelectMany(chunk => chunk.Events)
            .OfType<SetTempoEvent>()
            .OrderBy(tempoEvent => tempoEvent.DeltaTime)
            .Take(MaximumTempoMetaEventBytes)
            .ToArray();
        if (tempoEvents.Length > 0 && tempoEvents[0].MicrosecondsPerQuarterNote > 0)
        {
            tempoBpm = (int)Math.Round(60_000_000d / tempoEvents[0].MicrosecondsPerQuarterNote);
            if (tempoBpm is < MusicalConstants.MinimumBpm or > MusicalConstants.MaximumBpm)
            {
                tempoBpm = null;
            }
        }

        var timedEvents = midiFile.GetTrackChunks()
            .SelectMany(chunk => chunk.GetTimedEvents())
            .OrderBy(timedEvent => timedEvent.Time)
            .ThenBy(timedEvent => timedEvent.Event is NoteOffEvent || timedEvent.Event is NoteOnEvent noteOn && noteOn.Velocity == SevenBitNumber.Zero ? 0 : 1);

        foreach (var timedEvent in timedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedTick = ScaleTick(timedEvent.Time, ticksPerQuarter);
            if (timedEvent.Event is NoteOnEvent noteOnEvent && noteOnEvent.Velocity > SevenBitNumber.Zero)
            {
                var key = (noteOnEvent.Channel, noteOnEvent.NoteNumber);
                if (!noteStarts.TryGetValue(key, out var pendingNotes))
                {
                    pendingNotes = new Queue<PendingNote>();
                    noteStarts.Add(key, pendingNotes);
                }

                pendingNotes.Enqueue(new PendingNote(normalizedTick, noteOnEvent.Velocity, (int)noteOnEvent.NoteNumber));
            }
            else if (TryGetNoteOff(timedEvent.Event, out var channel, out var noteNumber))
            {
                var key = (channel, noteNumber);
                if (!noteStarts.TryGetValue(key, out var pendingNotes) || pendingNotes.Count == 0)
                {
                    continue;
                }

                var pending = pendingNotes.Dequeue();
                var duration = Math.Max(1, normalizedTick - pending.StartTick);
                var velocity = (float)pending.Velocity / SevenBitNumber.MaxValue;
                notes.Add(new Note(noteNumber, pending.StartTick, duration, velocity));
                maximumEndTick = Math.Max(maximumEndTick, pending.StartTick + duration);
            }
        }

        foreach (var pendingNotes in noteStarts.Values)
        {
            while (pendingNotes.Count > 0)
            {
                var pending = pendingNotes.Dequeue();
                var duration = MusicalConstants.TicksPerSixteenth;
                notes.Add(new Note(pending.NoteNumber, pending.StartTick, duration, (float)pending.Velocity / SevenBitNumber.MaxValue));
                maximumEndTick = Math.Max(maximumEndTick, pending.StartTick + duration);
            }
        }

        var stepCount = GetSupportedStepCount(maximumEndTick);
        if (stepCount == 0)
        {
            stepCount = MusicalConstants.MinimumPatternStepCount;
        }

        var pattern = new Pattern(Guid.NewGuid(), patternName, stepCount, notes: notes);
        return new MidiPatternImportResult(pattern, tempoBpm);
    }

    /// <inheritdoc />
    public async Task ExportAsync(
        Pattern pattern,
        string filePath,
        int tempoBpm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (tempoBpm is < MusicalConstants.MinimumBpm or > MusicalConstants.MaximumBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoBpm));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var tempoMicroseconds = (int)Math.Round(60_000_000d / tempoBpm);
            var tempoChunk = new TrackChunk(new SetTempoEvent(tempoMicroseconds));
            var noteChunk = new TrackChunk();
            var timedEvents = new List<TimedEvent>(pattern.Notes.Count * 2);
            foreach (var note in pattern.Notes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var velocity = (SevenBitNumber)Math.Clamp((int)Math.Round(note.Velocity * SevenBitNumber.MaxValue), 1, SevenBitNumber.MaxValue);
                timedEvents.Add(new TimedEvent(new NoteOnEvent((SevenBitNumber)note.Pitch, velocity), note.StartTick));
                timedEvents.Add(new TimedEvent(new NoteOffEvent((SevenBitNumber)note.Pitch, SevenBitNumber.Zero), checked(note.StartTick + note.DurationTicks)));
            }

            noteChunk.Events.AddRange(timedEvents.OrderBy(item => item.Time)
                .ThenBy(item => item.Event is NoteOffEvent ? 0 : 1)
                .Select(item => item.Event));
            var midiFile = new MidiFile(tempoChunk, noteChunk)
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision(MusicalConstants.PulsesPerQuarterNote)
            };
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                using var memory = new MemoryStream();
                midiFile.Write(memory);
                memory.Position = 0;
                await memory.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static long ScaleTick(long sourceTick, ushort sourceTicksPerQuarter)
    {
        var numerator = checked(sourceTick * MusicalConstants.PulsesPerQuarterNote);
        return checked((numerator + sourceTicksPerQuarter / 2L) / sourceTicksPerQuarter);
    }

    private static int GetSupportedStepCount(long endTick)
    {
        foreach (var supportedCount in new[]
                 {
                     MusicalConstants.MinimumPatternStepCount,
                     32,
                     MusicalConstants.MaximumPatternStepCount
                 })
        {
            if (endTick <= (long)supportedCount * MusicalConstants.TicksPerSixteenth)
            {
                return supportedCount;
            }
        }

        throw new InvalidDataException("MIDI notes exceed the maximum supported pattern length of 64 steps.");
    }

    private static bool TryGetNoteOff(MidiEvent midiEvent, out FourBitNumber channel, out SevenBitNumber noteNumber)
    {
        switch (midiEvent)
        {
            case NoteOffEvent noteOff:
                channel = noteOff.Channel;
                noteNumber = noteOff.NoteNumber;
                return true;
            case NoteOnEvent noteOn when noteOn.Velocity == SevenBitNumber.Zero:
                channel = noteOn.Channel;
                noteNumber = noteOn.NoteNumber;
                return true;
            default:
                channel = default;
                noteNumber = default;
                return false;
        }
    }

    private readonly record struct PendingNote(long StartTick, SevenBitNumber Velocity, int NoteNumber);
}
