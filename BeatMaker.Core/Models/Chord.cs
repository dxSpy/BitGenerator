using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents a chord voicing at a position in a beat.</summary>
public sealed record Chord
{
    /// <summary>Creates a validated chord with a MIDI root pitch and duration.</summary>
    /// <param name="rootPitch">Root MIDI note number.</param>
    /// <param name="quality">Chord quality.</param>
    /// <param name="startTick">Zero-based start time in PPQ ticks.</param>
    /// <param name="durationTicks">Positive duration in PPQ ticks.</param>
    /// <param name="inversion">Chord inversion index.</param>
    public Chord(int rootPitch, ChordQuality quality, long startTick, long durationTicks, int inversion = 0)
    {
        if (rootPitch is < MusicalConstants.MinimumMidiNote or > MusicalConstants.MaximumMidiNote)
        {
            throw new ArgumentOutOfRangeException(nameof(rootPitch));
        }

        if (!Enum.IsDefined(quality))
        {
            throw new ArgumentOutOfRangeException(nameof(quality));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        if (inversion is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(inversion));
        }

        RootPitch = rootPitch;
        Quality = quality;
        StartTick = startTick;
        DurationTicks = durationTicks;
        Inversion = inversion;
    }

    /// <summary>Gets the root MIDI note number.</summary>
    public int RootPitch { get; }

    /// <summary>Gets the chord quality.</summary>
    public ChordQuality Quality { get; }

    /// <summary>Gets the start time in PPQ ticks.</summary>
    public long StartTick { get; }

    /// <summary>Gets the duration in PPQ ticks.</summary>
    public long DurationTicks { get; }

    /// <summary>Gets the inversion index.</summary>
    public int Inversion { get; }
}
