namespace BeatMaker.Core.Models;

/// <summary>Identifies the instrument or output type of a track.</summary>
public enum TrackType
{
    /// <summary>A one-shot percussion instrument.</summary>
    Drum,

    /// <summary>A pitched sample playback track.</summary>
    Sampler,

    /// <summary>A synthesizer instrument track.</summary>
    Synth,

    /// <summary>A monophonic or polyphonic bass instrument track.</summary>
    Bass,

    /// <summary>A track that sends note events to external MIDI hardware or software.</summary>
    MidiOut
}
