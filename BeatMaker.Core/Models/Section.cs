namespace BeatMaker.Core.Models;

/// <summary>Contains an ordered musical section and its immutable event collections.</summary>
public sealed record Section
{
    /// <summary>Creates a validated section of a beat.</summary>
    /// <param name="type">Structural role of the section.</param>
    /// <param name="startBar">Zero-based first bar in the beat.</param>
    /// <param name="barCount">Positive number of bars.</param>
    /// <param name="chords">Optional harmony events local to the section.</param>
    /// <param name="melody">Optional melody notes local to the section.</param>
    /// <param name="drumHits">Optional drum triggers local to the section.</param>
    /// <param name="bassNotes">Optional bass notes local to the section.</param>
    public Section(
        SectionType type,
        int startBar,
        int barCount,
        IReadOnlyList<Chord>? chords = null,
        IReadOnlyList<Note>? melody = null,
        IReadOnlyList<DrumHit>? drumHits = null,
        IReadOnlyList<BassNote>? bassNotes = null)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (startBar < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startBar));
        }

        if (barCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(barCount));
        }

        var lengthTicks = checked((long)barCount * TicksPerBar);
        var chordArray = chords?.ToArray() ?? [];
        var melodyArray = melody?.ToArray() ?? [];
        var drumArray = drumHits?.ToArray() ?? [];
        var bassArray = bassNotes?.ToArray() ?? [];

        foreach (var chord in chordArray)
        {
            EnsureFits(chord.StartTick, chord.DurationTicks, lengthTicks, nameof(chords));
        }

        foreach (var note in melodyArray)
        {
            EnsureFits(note.StartTick, note.DurationTicks, lengthTicks, nameof(melody));
        }

        foreach (var hit in drumArray)
        {
            EnsureFits(hit.StartTick, 0, lengthTicks, nameof(drumHits));
        }

        foreach (var note in bassArray)
        {
            EnsureFits(note.StartTick, note.DurationTicks, lengthTicks, nameof(bassNotes));
        }

        Type = type;
        StartBar = startBar;
        BarCount = barCount;
        LengthTicks = lengthTicks;
        Chords = Array.AsReadOnly(chordArray);
        Melody = Array.AsReadOnly(melodyArray);
        DrumHits = Array.AsReadOnly(drumArray);
        BassNotes = Array.AsReadOnly(bassArray);
    }

    /// <summary>Gets the number of PPQ ticks in one 4/4 bar.</summary>
    public const int TicksPerBar = 3840;

    /// <summary>Gets the structural role of this section.</summary>
    public SectionType Type { get; }

    /// <summary>Gets the zero-based first bar of this section in its beat.</summary>
    public int StartBar { get; }

    /// <summary>Gets the number of bars in this section.</summary>
    public int BarCount { get; }

    /// <summary>Gets the section length in PPQ ticks.</summary>
    public long LengthTicks { get; }

    /// <summary>Gets harmony events local to the section.</summary>
    public IReadOnlyList<Chord> Chords { get; }

    /// <summary>Gets melody notes local to the section.</summary>
    public IReadOnlyList<Note> Melody { get; }

    /// <summary>Gets drum triggers local to the section.</summary>
    public IReadOnlyList<DrumHit> DrumHits { get; }

    /// <summary>Gets bass notes local to the section.</summary>
    public IReadOnlyList<BassNote> BassNotes { get; }

    private static void EnsureFits(long startTick, long durationTicks, long lengthTicks, string parameterName)
    {
        if (startTick > lengthTicks || durationTicks > lengthTicks - startTick)
        {
            throw new ArgumentException("Musical events must fit within the section bounds.", parameterName);
        }
    }
}
