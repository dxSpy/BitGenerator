namespace BeatMaker.Core.Models;

/// <summary>Represents an instrument or MIDI output lane in a project.</summary>
public sealed record Track
{
    /// <summary>Creates a track with validated mixer settings.</summary>
    /// <param name="id">Unique track identifier.</param>
    /// <param name="name">User-visible track name.</param>
    /// <param name="type">Instrument or output type.</param>
    /// <param name="volume">Normalized track volume from 0 to 1.</param>
    /// <param name="pan">Stereo pan from -1 (left) to 1 (right).</param>
    /// <param name="isMuted">Whether the track is muted.</param>
    /// <param name="isSolo">Whether the track is soloed.</param>
    /// <param name="sendA">Normalized send level to FX bus A.</param>
    /// <param name="sendB">Normalized send level to FX bus B.</param>
    public Track(
        Guid id,
        string name,
        TrackType type,
        float volume = 1f,
        float pan = 0f,
        bool isMuted = false,
        bool isSolo = false,
        float sendA = 0f,
        float sendB = 0f)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Track ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Track name cannot be empty.", nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ValidateUnitInterval(volume, nameof(volume));
        ValidatePan(pan, nameof(pan));
        ValidateUnitInterval(sendA, nameof(sendA));
        ValidateUnitInterval(sendB, nameof(sendB));

        Id = id;
        Name = name.Trim();
        Type = type;
        Volume = volume;
        Pan = pan;
        IsMuted = isMuted;
        IsSolo = isSolo;
        SendA = sendA;
        SendB = sendB;
    }

    /// <summary>Gets the unique track identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the track name.</summary>
    public string Name { get; }

    /// <summary>Gets the track instrument or output type.</summary>
    public TrackType Type { get; }

    /// <summary>Gets the normalized volume from 0 to 1.</summary>
    public float Volume { get; }

    /// <summary>Gets the stereo pan from -1 to 1.</summary>
    public float Pan { get; }

    /// <summary>Gets whether the track is muted.</summary>
    public bool IsMuted { get; }

    /// <summary>Gets whether the track is soloed.</summary>
    public bool IsSolo { get; }

    /// <summary>Gets the normalized level sent to FX bus A.</summary>
    public float SendA { get; }

    /// <summary>Gets the normalized level sent to FX bus B.</summary>
    public float SendB { get; }

    private static void ValidateUnitInterval(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidatePan(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < -1f or > 1f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
