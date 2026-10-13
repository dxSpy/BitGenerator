namespace BeatMaker.Core.Models;

/// <summary>Represents a complete generated beat with style, mood, tempo, and structure.</summary>
public sealed record Beat
{
    /// <summary>Creates a validated beat with ordered, non-overlapping sections.</summary>
    /// <param name="id">Unique beat identifier.</param>
    /// <param name="name">Display name.</param>
    /// <param name="substyle">Broad production substyle.</param>
    /// <param name="mood">Intended emotional character.</param>
    /// <param name="tempoBpm">Tempo in beats per minute.</param>
    /// <param name="sections">Ordered song sections; may be empty for a draft.</param>
    /// <param name="keyRoot">Optional selected key root; null means automatic key selection.</param>
    /// <param name="scaleMode">Optional selected scale mode; null means automatic mode selection.</param>
    public Beat(
        Guid id,
        string name,
        BeatSubstyle substyle,
        BeatMood mood,
        int tempoBpm,
        IReadOnlyList<Section> sections,
        KeyRoot? keyRoot = null,
        ScaleMode? scaleMode = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Beat ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Beat name cannot be empty.", nameof(name));
        }

        if (!Enum.IsDefined(substyle))
        {
            throw new ArgumentOutOfRangeException(nameof(substyle));
        }

        if (!Enum.IsDefined(mood))
        {
            throw new ArgumentOutOfRangeException(nameof(mood));
        }

        if (keyRoot is { } root && !Enum.IsDefined(root))
        {
            throw new ArgumentOutOfRangeException(nameof(keyRoot));
        }

        if (scaleMode is { } mode && !Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleMode));
        }

        if (tempoBpm is < MinimumTempoBpm or > MaximumTempoBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoBpm));
        }

        ArgumentNullException.ThrowIfNull(sections);
        var sectionArray = sections.ToArray();
        var expectedStartBar = 0;
        foreach (var section in sectionArray)
        {
            if (section is null)
            {
                throw new ArgumentException("Beat sections cannot contain null values.", nameof(sections));
            }

            if (section.StartBar != expectedStartBar)
            {
                throw new ArgumentException("Beat sections must be ordered and contiguous from bar zero.", nameof(sections));
            }

            expectedStartBar = checked(expectedStartBar + section.BarCount);
        }

        Id = id;
        Name = name.Trim();
        Substyle = substyle;
        Mood = mood;
        TempoBpm = tempoBpm;
        KeyRoot = keyRoot;
        ScaleMode = scaleMode;
        Sections = Array.AsReadOnly(sectionArray);
        TotalBars = expectedStartBar;
    }

    /// <summary>Gets the smallest supported beat tempo.</summary>
    public const int MinimumTempoBpm = 40;

    /// <summary>Gets the largest supported beat tempo.</summary>
    public const int MaximumTempoBpm = 200;

    /// <summary>Gets the unique beat identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; }

    /// <summary>Gets the production substyle.</summary>
    public BeatSubstyle Substyle { get; }

    /// <summary>Gets the intended mood.</summary>
    public BeatMood Mood { get; }

    /// <summary>Gets tempo in beats per minute.</summary>
    public int TempoBpm { get; }

    /// <summary>Gets the selected tonal root, or <see langword="null"/> for automatic selection.</summary>
    public KeyRoot? KeyRoot { get; }

    /// <summary>Gets the selected scale mode, or <see langword="null"/> for automatic selection.</summary>
    public ScaleMode? ScaleMode { get; }

    /// <summary>Gets ordered beat sections.</summary>
    public IReadOnlyList<Section> Sections { get; }

    /// <summary>Gets total number of bars across all sections.</summary>
    public int TotalBars { get; }
}
