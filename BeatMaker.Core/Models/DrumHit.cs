using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents one drum or percussion trigger.</summary>
public readonly record struct DrumHit
{
    /// <summary>Creates a validated drum trigger.</summary>
    /// <param name="instrument">Drum role to trigger.</param>
    /// <param name="startTick">Zero-based start time in PPQ ticks.</param>
    /// <param name="velocity">Normalized velocity from zero to one.</param>
    /// <param name="pan">Stereo pan from -1 (left) to 1 (right).</param>
    public DrumHit(DrumInstrument instrument, long startTick, float velocity = 1f, float pan = 0f)
    {
        if (!Enum.IsDefined(instrument))
        {
            throw new ArgumentOutOfRangeException(nameof(instrument));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (!float.IsFinite(velocity) || velocity is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(velocity));
        }

        if (!float.IsFinite(pan) || pan is < -1f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(pan));
        }

        Instrument = instrument;
        StartTick = startTick;
        Velocity = velocity;
        Pan = pan;
    }

    /// <summary>Gets the drum role.</summary>
    public DrumInstrument Instrument { get; }

    /// <summary>Gets the start time in PPQ ticks.</summary>
    public long StartTick { get; }

    /// <summary>Gets normalized velocity.</summary>
    public float Velocity { get; }

    /// <summary>Gets stereo pan from -1 to 1.</summary>
    public float Pan { get; }
}
