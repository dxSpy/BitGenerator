using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Immutable, validated configuration for deterministic beat generation.</summary>
public sealed record BeatGenerationOptions
{
    /// <summary>Creates a generation request.</summary>
    /// <param name="substyle">Broad production substyle.</param>
    /// <param name="mood">Desired emotional character.</param>
    /// <param name="length">Target arrangement length.</param>
    /// <param name="tempoBpm">Optional fixed tempo; null selects a style-specific tempo.</param>
    /// <param name="keyRoot">Optional fixed key root; null selects a key automatically.</param>
    /// <param name="scaleMode">Optional fixed scale mode; null selects a mode automatically.</param>
    /// <param name="seed">Optional deterministic random seed.</param>
    /// <param name="referenceTrack">Optional user-provided reference description; not a request to reproduce a recording.</param>
    public BeatGenerationOptions(
        BeatSubstyle substyle,
        BeatMood mood,
        BeatLengthPreset length,
        int? tempoBpm = null,
        KeyRoot? keyRoot = null,
        ScaleMode? scaleMode = null,
        int? seed = null,
        string? referenceTrack = null)
    {
        if (!Enum.IsDefined(substyle))
        {
            throw new ArgumentOutOfRangeException(nameof(substyle));
        }

        if (!Enum.IsDefined(mood))
        {
            throw new ArgumentOutOfRangeException(nameof(mood));
        }

        if (!Enum.IsDefined(length))
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (tempoBpm is { } bpm && bpm is < Beat.MinimumTempoBpm or > Beat.MaximumTempoBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoBpm));
        }

        if (keyRoot is { } root && !Enum.IsDefined(root))
        {
            throw new ArgumentOutOfRangeException(nameof(keyRoot));
        }

        if (scaleMode is { } mode && !Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleMode));
        }

        if (referenceTrack is { Length: > MaximumReferenceTrackLength })
        {
            throw new ArgumentOutOfRangeException(nameof(referenceTrack));
        }

        Substyle = substyle;
        Mood = mood;
        Length = length;
        TempoBpm = tempoBpm;
        KeyRoot = keyRoot;
        ScaleMode = scaleMode;
        Seed = seed;
        ReferenceTrack = string.IsNullOrWhiteSpace(referenceTrack) ? null : referenceTrack.Trim();
    }

    /// <summary>Gets the maximum accepted length of an optional reference description.</summary>
    public const int MaximumReferenceTrackLength = 256;

    /// <summary>Gets the selected production substyle.</summary>
    public BeatSubstyle Substyle { get; }

    /// <summary>Gets the target emotional character.</summary>
    public BeatMood Mood { get; }

    /// <summary>Gets the target arrangement length preset.</summary>
    public BeatLengthPreset Length { get; }

    /// <summary>Gets a fixed tempo, or null for automatic style-aware selection.</summary>
    public int? TempoBpm { get; }

    /// <summary>Gets a fixed key root, or null for automatic selection.</summary>
    public KeyRoot? KeyRoot { get; }

    /// <summary>Gets a fixed scale mode, or null for automatic selection.</summary>
    public ScaleMode? ScaleMode { get; }

    /// <summary>Gets a deterministic seed, or null when the caller chooses a random seed.</summary>
    public int? Seed { get; }

    /// <summary>Gets an optional reference description supplied by the user.</summary>
    public string? ReferenceTrack { get; }
}
