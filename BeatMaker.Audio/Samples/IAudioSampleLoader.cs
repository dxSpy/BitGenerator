using BeatMaker.Core.Audio;

namespace BeatMaker.Audio.Samples;

/// <summary>Loads audio files and decodes them to interleaved floating-point PCM.</summary>
public interface IAudioSampleLoader
{
    /// <summary>Loads and decodes an audio file asynchronously.</summary>
    /// <param name="filePath">Path to the audio file.</param>
    /// <param name="cancellationToken">Token used to cancel loading.</param>
    /// <returns>The decoded sample.</returns>
    Task<AudioSample> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
