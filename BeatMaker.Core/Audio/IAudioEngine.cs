using BeatMaker.Core.Models;

namespace BeatMaker.Core.Audio;

/// <summary>Controls audio output and exposes the musical transport position.</summary>
public interface IAudioEngine : IDisposable
{
    /// <summary>Gets the current transport state.</summary>
    TransportState State { get; }

    /// <summary>Gets the active sample rate in samples per second.</summary>
    int SampleRate { get; }

    /// <summary>Gets the current tempo in beats per minute.</summary>
    int TempoBpm { get; }

    /// <summary>Gets the number of audio frames rendered since the last stop.</summary>
    long SamplePosition { get; }

    /// <summary>Gets the current musical position in PPQ ticks.</summary>
    long CurrentTick { get; }

    /// <summary>Changes the transport tempo.</summary>
    /// <param name="bpm">Tempo from 40 through 300 beats per minute.</param>
    void SetTempo(int bpm);

    /// <summary>Queues an audio sample for playback.</summary>
    /// <param name="sample">Decoded PCM sample to play.</param>
    /// <param name="volume">Linear playback volume from 0 to 1.</param>
    /// <param name="pan">Stereo pan from -1 (left) to 1 (right).</param>
    /// <returns><see langword="true"/> if the playback command was accepted.</returns>
    bool TryPlaySample(AudioSample sample, float volume = 1f, float pan = 0f);

    /// <summary>Sets the active step pattern and sample assigned to each step.</summary>
    /// <param name="pattern">Pattern to schedule, or <see langword="null"/> to disable sequencing.</param>
    /// <param name="stepSamples">Sample assignments indexed by step position; null entries are silent.</param>
    void SetPattern(Pattern? pattern, IReadOnlyList<AudioSample?>? stepSamples = null);

    /// <summary>Sets the project arrangement and sample assignments by track identifier.</summary>
    /// <param name="project">Project arrangement, or null to disable arrangement playback.</param>
    /// <param name="trackSamples">Audio sample assignments keyed by project track ID.</param>
    void SetProjectArrangement(Project? project, IReadOnlyDictionary<Guid, AudioSample>? trackSamples = null);

    /// <summary>Starts or resumes audio output and transport.</summary>
    void Play();

    /// <summary>Pauses output while retaining the current transport position.</summary>
    void Pause();

    /// <summary>Stops output and resets the transport position to zero.</summary>
    void Stop();
}
