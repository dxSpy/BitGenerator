using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates deterministic melodies constrained to a scale and chord tones.</summary>
public sealed class MelodyGenerator : IMelodyGenerator
{
    /// <inheritdoc />
    public IReadOnlyList<Note> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        IReadOnlyList<Chord> chords,
        int barCount,
        BeatSubstyle substyle,
        Random random,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chords);
        ArgumentNullException.ThrowIfNull(random);
        if (!Enum.IsDefined(keyRoot)) throw new ArgumentOutOfRangeException(nameof(keyRoot));
        if (!Enum.IsDefined(scaleMode)) throw new ArgumentOutOfRangeException(nameof(scaleMode));
        if (!Enum.IsDefined(substyle)) throw new ArgumentOutOfRangeException(nameof(substyle));
        if (barCount <= 0) throw new ArgumentOutOfRangeException(nameof(barCount));

        var keyPitchClass = GenerationProfiles.GetPitchClass(keyRoot);
        var notes = new List<Note>(barCount * 4);
        var notesPerBar = substyle switch
        {
            BeatSubstyle.DarkTrap or BeatSubstyle.UndergroundAbstract => 2,
            BeatSubstyle.OldSchoolRussianRap => 3,
            _ => 4
        };
        var barTicks = Section.TicksPerBar;
        var noteDuration = barTicks / notesPerBar;

        for (var bar = 0; bar < barCount; bar++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var barStartTick = (long)bar * barTicks;
            var chord = FindChord(chords, barStartTick);
            for (var slot = 0; slot < notesPerBar; slot++)
            {
                if (slot > 0 && random.NextDouble() < 0.22)
                {
                    continue;
                }

                var degree = SelectScaleDegree(random, bar, slot);
                var pitchClass = GenerationProfiles.GetPitchClassForDegree(keyPitchClass, scaleMode, degree);
                if (chord is not null && random.NextDouble() < 0.68)
                {
                    pitchClass = (chord.RootPitch % 12 + SelectChordTone(chord.Quality, random)) % 12;
                }

                var octave = substyle is BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract ? 5 : 5 + random.Next(2);
                var pitch = octave * 12 + pitchClass;
                var startTick = barStartTick + (long)slot * barTicks / notesPerBar;
                var durationTicks = Math.Max(1, noteDuration * (random.NextDouble() < 0.25 ? 2L : 1L) / 2);
                durationTicks = Math.Min(durationTicks, (long)(bar + 1) * barTicks - startTick);
                notes.Add(new Note(pitch, startTick, durationTicks, 0.58f + (float)random.NextDouble() * 0.32f));
            }
        }

        return Array.AsReadOnly(notes.ToArray());
    }

    private static Chord? FindChord(IReadOnlyList<Chord> chords, long tick)
    {
        for (var index = chords.Count - 1; index >= 0; index--)
        {
            if (chords[index].StartTick <= tick)
            {
                return chords[index];
            }
        }

        return null;
    }

    private static int SelectScaleDegree(Random random, int bar, int slot)
    {
        if (slot == 0)
        {
            return (bar % 4) switch
            {
                0 => 0,
                1 => 2,
                2 => 4,
                _ => 5
            };
        }

        return random.Next(7);
    }

    private static int SelectChordTone(ChordQuality quality, Random random)
    {
        var intervals = quality switch
        {
            ChordQuality.Major or ChordQuality.MajorSeventh => new[] { 0, 4, 7 },
            ChordQuality.Diminished => new[] { 0, 3, 6 },
            ChordQuality.Augmented => new[] { 0, 4, 8 },
            _ => new[] { 0, 3, 7 }
        };

        return intervals[random.Next(intervals.Length)];
    }
}
