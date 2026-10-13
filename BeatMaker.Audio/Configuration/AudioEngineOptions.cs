namespace BeatMaker.Audio.Configuration;

/// <summary>Provides immutable configuration for the NAudio output engine.</summary>
public sealed record AudioEngineOptions
{
    /// <summary>Gets the default output sample rate.</summary>
    public const int DefaultSampleRate = 48_000;

    /// <summary>Gets the default output channel count.</summary>
    public const int DefaultChannelCount = 2;

    /// <summary>Gets the default requested output latency in milliseconds.</summary>
    public const int DefaultBufferLatencyMilliseconds = 10;

    /// <summary>Gets the minimum supported sample rate.</summary>
    public const int MinimumSampleRate = 8_000;

    /// <summary>Gets the maximum supported sample rate.</summary>
    public const int MaximumSampleRate = 192_000;

    /// <summary>Gets the maximum supported number of output channels.</summary>
    public const int MaximumChannelCount = 2;

    /// <summary>Gets the minimum requested output latency in milliseconds.</summary>
    public const int MinimumBufferLatencyMilliseconds = 5;

    /// <summary>Gets the maximum requested output latency in milliseconds.</summary>
    public const int MaximumBufferLatencyMilliseconds = 20;

    /// <summary>Creates validated audio engine options.</summary>
    /// <param name="backend">Output backend.</param>
    /// <param name="sampleRate">Audio sample rate in samples per second.</param>
    /// <param name="channelCount">Number of output channels.</param>
    /// <param name="bufferLatencyMilliseconds">Requested output latency from 5 to 20 milliseconds.</param>
    /// <param name="asioDriverName">Registered driver name when ASIO is selected.</param>
    public AudioEngineOptions(
        AudioOutputBackend backend = AudioOutputBackend.WaveOut,
        int sampleRate = DefaultSampleRate,
        int channelCount = DefaultChannelCount,
        int bufferLatencyMilliseconds = DefaultBufferLatencyMilliseconds,
        string? asioDriverName = null)
    {
        if (!Enum.IsDefined(backend))
        {
            throw new ArgumentOutOfRangeException(nameof(backend));
        }

        if (sampleRate is < MinimumSampleRate or > MaximumSampleRate)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (channelCount is < 1 or > MaximumChannelCount)
        {
            throw new ArgumentOutOfRangeException(nameof(channelCount));
        }

        if (bufferLatencyMilliseconds is < MinimumBufferLatencyMilliseconds or > MaximumBufferLatencyMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferLatencyMilliseconds));
        }

        if (backend == AudioOutputBackend.Asio && string.IsNullOrWhiteSpace(asioDriverName))
        {
            throw new ArgumentException("The ASIO backend requires a registered driver name.", nameof(asioDriverName));
        }

        Backend = backend;
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        BufferLatencyMilliseconds = bufferLatencyMilliseconds;
        AsioDriverName = asioDriverName?.Trim();
    }

    /// <summary>Gets the selected audio output backend.</summary>
    public AudioOutputBackend Backend { get; }

    /// <summary>Gets the sample rate in samples per second.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the number of output channels.</summary>
    public int ChannelCount { get; }

    /// <summary>Gets the requested output latency in milliseconds.</summary>
    public int BufferLatencyMilliseconds { get; }

    /// <summary>Gets the ASIO driver name, if the ASIO backend is selected.</summary>
    public string? AsioDriverName { get; }
}
