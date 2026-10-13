namespace BeatMaker.Core.Models;

/// <summary>Places a pattern on a project track at a specific timeline position.</summary>
public sealed record Clip
{
    /// <summary>Creates a clip referencing an existing track and pattern.</summary>
    /// <param name="id">Unique clip identifier.</param>
    /// <param name="trackId">Identifier of the destination track.</param>
    /// <param name="patternId">Identifier of the pattern to play.</param>
    /// <param name="startTick">Timeline start position in PPQ ticks.</param>
    /// <param name="lengthTicks">Clip length in PPQ ticks.</param>
    /// <param name="isLooped">Whether the pattern repeats to fill the clip.</param>
    public Clip(Guid id, Guid trackId, Guid patternId, long startTick, long lengthTicks, bool isLooped = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Clip ID cannot be empty.", nameof(id));
        }

        if (trackId == Guid.Empty)
        {
            throw new ArgumentException("Track ID cannot be empty.", nameof(trackId));
        }

        if (patternId == Guid.Empty)
        {
            throw new ArgumentException("Pattern ID cannot be empty.", nameof(patternId));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (lengthTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthTicks));
        }

        if (startTick > long.MaxValue - lengthTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthTicks), "Clip end position exceeds the supported timeline range.");
        }

        Id = id;
        TrackId = trackId;
        PatternId = patternId;
        StartTick = startTick;
        LengthTicks = lengthTicks;
        IsLooped = isLooped;
    }

    /// <summary>Gets the unique clip identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the destination track identifier.</summary>
    public Guid TrackId { get; }

    /// <summary>Gets the referenced pattern identifier.</summary>
    public Guid PatternId { get; }

    /// <summary>Gets the clip start position in PPQ ticks.</summary>
    public long StartTick { get; }

    /// <summary>Gets the clip length in PPQ ticks.</summary>
    public long LengthTicks { get; }

    /// <summary>Gets whether the referenced pattern repeats across the clip.</summary>
    public bool IsLooped { get; }
}
