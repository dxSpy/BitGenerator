namespace BeatMaker.Audio.Configuration;

/// <summary>Identifies an NAudio output backend.</summary>
public enum AudioOutputBackend
{
    /// <summary>Uses the Windows multimedia audio output backend.</summary>
    WaveOut,

    /// <summary>Uses WASAPI shared mode and the default playback device.</summary>
    WasapiShared,

    /// <summary>Uses an installed ASIO driver by its registered name.</summary>
    Asio
}
