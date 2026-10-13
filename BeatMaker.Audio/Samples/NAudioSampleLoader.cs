using BeatMaker.Core.Audio;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace BeatMaker.Audio.Samples;

/// <summary>Loads supported audio files through NAudio and converts them to float PCM.</summary>
public sealed class NAudioSampleLoader : IAudioSampleLoader
{
    private const int ReadBufferFrames = 16_384;
    private const long MaximumDecodedBytes = 512L * 1024 * 1024;

    /// <inheritdoc />
    public async Task<AudioSample> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(filePath);
        var name = Path.GetFileNameWithoutExtension(fullPath);
        await using var file = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new WaveFileReader(file);
        var sampleProvider = reader.ToSampleProvider();

        var sampleRate = sampleProvider.WaveFormat.SampleRate;
        var channelCount = sampleProvider.WaveFormat.Channels;
        if (sampleRate is < 8_000 or > 192_000 || channelCount is < 1 or > 8)
        {
            throw new InvalidDataException("The audio file has an unsupported sample rate or channel count.");
        }

        var samples = new List<float>();
        var buffer = new float[ReadBufferFrames * channelCount];
        var maximumSamples = MaximumDecodedBytes / sizeof(float);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = sampleProvider.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (samples.Count + read > maximumSamples)
            {
                throw new InvalidDataException("Decoded audio exceeds the 512 MiB sample size limit.");
            }

            for (var index = 0; index < read; index++)
            {
                if (!float.IsFinite(buffer[index]))
                {
                    throw new InvalidDataException("Decoded audio contains a non-finite PCM value.");
                }
            }

            for (var index = 0; index < read; index++)
            {
                samples.Add(buffer[index]);
            }

            await Task.Yield();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (samples.Count == 0 || samples.Count % channelCount != 0)
        {
            throw new InvalidDataException("The audio file contains no complete PCM frames.");
        }

        return new AudioSample(name, sampleRate, channelCount, CollectionsMarshal.AsSpan(samples));
    }
}
