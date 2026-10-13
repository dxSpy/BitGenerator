using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents one editable step in a sequencer pattern.</summary>
public readonly record struct Step
{
    /// <summary>Initializes a step with timing, probability, and performance parameters.</summary>
    /// <param name="index">Zero-based step position within a pattern.</param>
    /// <param name="isEnabled">Whether the step triggers an event.</param>
    /// <param name="velocity">Normalized velocity in the inclusive range 0–1.</param>
    /// <param name="probability">Trigger probability in the inclusive range 0–1.</param>
    /// <param name="ratchetCount">Number of retriggers for this step.</param>
    /// <param name="nudgeTicks">Timing offset in PPQ ticks.</param>
    /// <param name="pitchOffsetSemitones">Pitch offset relative to the instrument's root note.</param>
    public Step(
        int index,
        bool isEnabled,
        float velocity = 1f,
        float probability = 1f,
        int ratchetCount = 1,
        int nudgeTicks = 0,
        int pitchOffsetSemitones = 0)
    {
        if (index < 0 || index >= MusicalConstants.MaximumPatternStepCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (!float.IsFinite(velocity) || velocity is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(velocity));
        }

        if (!float.IsFinite(probability) || probability is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(probability));
        }

        if (ratchetCount is < 1 or > MusicalConstants.MaximumRatchetCount)
        {
            throw new ArgumentOutOfRangeException(nameof(ratchetCount));
        }

        if (Math.Abs((long)nudgeTicks) > MusicalConstants.PulsesPerQuarterNote)
        {
            throw new ArgumentOutOfRangeException(nameof(nudgeTicks));
        }

        if (pitchOffsetSemitones < -MusicalConstants.MaximumPitchOffsetSemitones ||
            pitchOffsetSemitones > MusicalConstants.MaximumPitchOffsetSemitones)
        {
            throw new ArgumentOutOfRangeException(nameof(pitchOffsetSemitones));
        }

        Index = index;
        IsEnabled = isEnabled;
        Velocity = velocity;
        Probability = probability;
        RatchetCount = ratchetCount;
        NudgeTicks = nudgeTicks;
        PitchOffsetSemitones = pitchOffsetSemitones;
    }

    /// <summary>Gets the zero-based position in the pattern.</summary>
    public int Index { get; }

    /// <summary>Gets whether this step is active.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets the normalized velocity.</summary>
    public float Velocity { get; }

    /// <summary>Gets the probability that this step triggers.</summary>
    public float Probability { get; }

    /// <summary>Gets the number of ratchets to emit for this step.</summary>
    public int RatchetCount { get; }

    /// <summary>Gets the timing offset in PPQ ticks.</summary>
    public int NudgeTicks { get; }

    /// <summary>Gets the pitch offset in semitones.</summary>
    public int PitchOffsetSemitones { get; }
}
