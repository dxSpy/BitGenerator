using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents a timed bass note with optional pitch glide.</summary>
public readonly record struct BassNote
{
    /// <summary>Creates a validated bass note event.</summary>
    /// <param name="pitch">Starting MIDI note number.</param>
    /// <param name="startTick">Zero-based start time in PPQ ticks.</param>
    /// <param name="durationTicks">Positive duration in PPQ ticks.</param>
    /// <param name="velocity">Normalized velocity from zero to one.</param>
    /// <param name="glideToPitch">Optional destination MIDI pitch for a glide.</param>
    /// <param name="glideDurationTicks">Glide duration; must not exceed note duration.</param>
    public BassNote(int pitch, long startTick, long durationTicks, float velocity = 1f, int? glideToPitch = null, long glideDurationTicks = 0)
    {
        if (pitch is < MusicalConstants.MinimumMidiNote or > MusicalConstants.MaximumMidiNote)
        {
            throw new ArgumentOutOfRangeException(nameof(pitch));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        if (!float.IsFinite(velocity) || velocity is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(velocity));
        }

        if (glideToPitch is < MusicalConstants.MinimumMidiNote or > MusicalConstants.MaximumMidiNote)
        {
            throw new ArgumentOutOfRangeException(nameof(glideToPitch));
        }

        if (glideDurationTicks < 0 || glideDurationTicks > durationTicks || (glideToPitch is null && glideDurationTicks != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(glideDurationTicks));
        }

        Pitch = pitch;
        StartTick = startTick;
        DurationTicks = durationTicks;
        Velocity = velocity;
        GlideToPitch = glideToPitch;
        GlideDurationTicks = glideDurationTicks;
    }

    /// <summary>Gets the starting MIDI pitch.</summary>
    public int Pitch { get; }

    /// <summary>Gets the start time in PPQ ticks.</summary>
    public long StartTick { get; }

    /// <summary>Gets the duration in PPQ ticks.</summary>
    public long DurationTicks { get; }

    /// <summary>Gets normalized velocity.</summary>
    public float Velocity { get; }

    /// <summary>Gets the destination pitch for an optional glide.</summary>
    public int? GlideToPitch { get; }

    /// <summary>Gets the glide duration in PPQ ticks.</summary>
    public long GlideDurationTicks { get; }
}
