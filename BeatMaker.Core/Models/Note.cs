using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents a MIDI-pitched note positioned in PPQ ticks.</summary>
public readonly record struct Note
{
    /// <summary>Initializes a note with pitch, timing, and normalized velocity.</summary>
    /// <param name="pitch">MIDI note number from 0 through 127.</param>
    /// <param name="startTick">Zero-based start position in PPQ ticks.</param>
    /// <param name="durationTicks">Note duration in PPQ ticks; must be positive.</param>
    /// <param name="velocity">Normalized velocity in the inclusive range 0–1.</param>
    public Note(int pitch, long startTick, long durationTicks, float velocity = 1f)
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

        Pitch = pitch;
        StartTick = startTick;
        DurationTicks = durationTicks;
        Velocity = velocity;
    }

    /// <summary>Gets the MIDI note number.</summary>
    public int Pitch { get; }

    /// <summary>Gets the note start position in PPQ ticks.</summary>
    public long StartTick { get; }

    /// <summary>Gets the note duration in PPQ ticks.</summary>
    public long DurationTicks { get; }

    /// <summary>Gets the normalized note velocity.</summary>
    public float Velocity { get; }
}
