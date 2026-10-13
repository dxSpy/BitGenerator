namespace BeatMaker.Core.Constants;

/// <summary>
/// Defines shared musical and MIDI limits used by the domain model.
/// </summary>
public static class MusicalConstants
{
    /// <summary>Gets the standard number of pulses in a quarter note.</summary>
    public const int PulsesPerQuarterNote = 960;

    /// <summary>Gets the number of sixteenth-note steps in a quarter note.</summary>
    public const int StepsPerQuarterNote = 4;

    /// <summary>Gets the number of ticks in a sixteenth note.</summary>
    public const int TicksPerSixteenth = PulsesPerQuarterNote / StepsPerQuarterNote;

    /// <summary>Gets the minimum supported project tempo.</summary>
    public const int MinimumBpm = 40;

    /// <summary>Gets the maximum supported project tempo.</summary>
    public const int MaximumBpm = 300;

    /// <summary>Gets the smallest supported pattern length in steps.</summary>
    public const int MinimumPatternStepCount = 16;

    /// <summary>Gets the largest supported pattern length in steps.</summary>
    public const int MaximumPatternStepCount = 64;

    /// <summary>Gets the maximum supported ratchets on one step.</summary>
    public const int MaximumRatchetCount = 8;

    /// <summary>Gets the maximum supported absolute pitch offset in semitones.</summary>
    public const int MaximumPitchOffsetSemitones = 48;

    /// <summary>Gets the smallest valid MIDI note number.</summary>
    public const int MinimumMidiNote = 0;

    /// <summary>Gets the largest valid MIDI note number.</summary>
    public const int MaximumMidiNote = 127;

    /// <summary>Determines whether a step count is a supported pattern length.</summary>
    /// <param name="stepCount">The number of sixteenth-note steps.</param>
    /// <returns><see langword="true"/> when the count is 16, 32, or 64.</returns>
    public static bool IsSupportedPatternStepCount(int stepCount) =>
        stepCount is MinimumPatternStepCount or 32 or MaximumPatternStepCount;
}
