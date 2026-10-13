namespace BeatMaker.Core.Audio;

/// <summary>Describes the current state of the audio transport.</summary>
public enum TransportState
{
    /// <summary>The transport is stopped and its position is reset.</summary>
    Stopped,

    /// <summary>The transport clock and audio output are running.</summary>
    Playing,

    /// <summary>The transport is paused and retains its current position.</summary>
    Paused
}
