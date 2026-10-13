namespace BeatMaker.Core.Audio;

/// <summary>Represents decoded interleaved floating-point PCM audio.</summary>
public sealed class AudioSample
{
    private readonly float[] _samples;

    /// <summary>Creates an audio sample with validated PCM data.</summary>
    /// <param name="name">User-visible sample name.</param>
    /// <param name="sampleRate">Sample rate in frames per second.</param>
    /// <param name="channelCount">Number of interleaved audio channels.</param>
    /// <param name="samples">Non-empty interleaved floating-point PCM samples.</param>
    public AudioSample(string name, int sampleRate, int channelCount, ReadOnlySpan<float> samples)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sample name cannot be empty.", nameof(name));
        }

        if (sampleRate is < 8_000 or > 192_000)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (channelCount is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(channelCount));
        }

        if (samples.IsEmpty || samples.Length % channelCount != 0)
        {
            throw new ArgumentException("PCM data must contain complete, non-empty frames.", nameof(samples));
        }

        for (var index = 0; index < samples.Length; index++)
        {
            if (!float.IsFinite(samples[index]))
            {
                throw new ArgumentException("PCM data must contain only finite values.", nameof(samples));
            }
        }

        Name = name.Trim();
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        _samples = samples.ToArray();
    }

    /// <summary>Gets the user-visible sample name.</summary>
    public string Name { get; }

    /// <summary>Gets the sample rate in frames per second.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the number of interleaved audio channels.</summary>
    public int ChannelCount { get; }

    /// <summary>Gets the number of audio frames in the sample.</summary>
    public int FrameCount => _samples.Length / ChannelCount;

    /// <summary>Gets the sample duration in seconds.</summary>
    public double DurationSeconds => (double)FrameCount / SampleRate;

    /// <summary>Gets interleaved PCM samples as read-only memory.</summary>
    public ReadOnlyMemory<float> Samples => _samples;
}
