using System.Buffers.Binary;
using System.Threading.Channels;
using BeatMaker.Audio.Configuration;
using BeatMaker.Audio.Scheduling;
using BeatMaker.Audio.Samples;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Timing;
using BeatMaker.Core.Models;
using NAudio.Wave;
using NAudio.Wave.Asio;

namespace BeatMaker.Audio.Engine;

/// <summary>Provides NAudio output lifecycle and advances the musical transport clock.</summary>
public sealed class NAudioAudioEngine : IAudioEngine
{
    private readonly TransportClock _clock;
    private readonly IWavePlayer _output;
    private readonly SampleWaveProvider _provider;
    private TransportState _state = TransportState.Stopped;
    private bool _disposed;

    /// <summary>Creates an audio engine using the selected NAudio backend.</summary>
    /// <param name="options">Validated output configuration.</param>
    public NAudioAudioEngine(AudioEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        SampleRate = options.SampleRate;
        _clock = new TransportClock(options.SampleRate, 120);
        _provider = new SampleWaveProvider(options.SampleRate, options.ChannelCount, _clock);
        _output = CreateOutput(options);
        _output.Init(_provider);
    }

    /// <inheritdoc />
    public TransportState State => _state;

    /// <inheritdoc />
    public int SampleRate { get; }

    /// <inheritdoc />
    public int TempoBpm => _clock.TempoBpm;

    /// <inheritdoc />
    public long SamplePosition => _clock.SamplePosition;

    /// <inheritdoc />
    public long CurrentTick => _clock.CurrentTick;

    /// <inheritdoc />
    public bool TryPlaySample(AudioSample sample, float volume = 1f, float pan = 0f)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sample);
        if (!float.IsFinite(volume) || volume is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        if (!float.IsFinite(pan) || pan is < -1f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(pan));
        }

        return _provider.TryEnqueue(new PlaybackCommand(sample, volume, pan));
    }

    /// <inheritdoc />
    public void SetPattern(Pattern? pattern, IReadOnlyList<AudioSample?>? stepSamples = null)
    {
        ThrowIfDisposed();
        _provider.SetArrangement(null, null);
        _provider.SetPattern(pattern, stepSamples);
    }

    /// <inheritdoc />
    public void SetProjectArrangement(Project? project, IReadOnlyDictionary<Guid, AudioSample>? trackSamples = null)
    {
        ThrowIfDisposed();
        _provider.SetPattern(null, null);
        _provider.SetArrangement(project, trackSamples);
        if (project is not null)
        {
            _clock.SetTempo(project.Bpm);
        }
    }

    /// <inheritdoc />
    public void SetTempo(int bpm)
    {
        ThrowIfDisposed();
        _clock.SetTempo(bpm);
    }

    /// <inheritdoc />
    public void Play()
    {
        ThrowIfDisposed();
        if (_state == TransportState.Playing)
        {
            return;
        }

        _clock.Start();
        try
        {
            _output.Play();
            _state = TransportState.Playing;
        }
        catch
        {
            _clock.Pause();
            throw;
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        ThrowIfDisposed();
        if (_state != TransportState.Playing)
        {
            return;
        }

        _clock.Pause();
        _output.Pause();
        _state = TransportState.Paused;
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfDisposed();
        _clock.Pause();
        _output.Stop();
        _clock.Reset();
        _state = TransportState.Stopped;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _clock.Pause();
        _output.Dispose();
        _disposed = true;
        _state = TransportState.Stopped;
    }

    private static IWavePlayer CreateOutput(AudioEngineOptions options) => options.Backend switch
    {
        AudioOutputBackend.WaveOut => new WaveOutEvent
        {
            DesiredLatency = options.BufferLatencyMilliseconds,
            NumberOfBuffers = 2
        },
        AudioOutputBackend.WasapiShared => new WasapiOut(
            NAudio.CoreAudioApi.AudioClientShareMode.Shared,
            useEventSync: true,
            latency: options.BufferLatencyMilliseconds),
        AudioOutputBackend.Asio => new AsioOut(options.AsioDriverName!),
        _ => throw new ArgumentOutOfRangeException(nameof(options), "Unsupported audio output backend.")
    };

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private readonly record struct PlaybackCommand(AudioSample Sample, float Volume, float Pan);

    private sealed class SampleWaveProvider : IWaveProvider
    {
        private const int MaximumFramesPerRender = 4_096;
        private const int CommandQueueCapacity = 256;

        private readonly TransportClock _clock;
        private readonly AudioSampleMixer _mixer;
        private readonly StepPatternScheduler _scheduler = new();
        private readonly ArrangementScheduler _arrangementScheduler = new();
        private readonly SampleTrigger _scheduledTrigger;
        private readonly Channel<PlaybackCommand> _commands;
        private readonly float[] _mixBuffer;

        public SampleWaveProvider(int sampleRate, int channelCount, TransportClock clock)
        {
            _clock = clock;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
            _mixer = new AudioSampleMixer(sampleRate, channelCount);
            _scheduledTrigger = TryStartScheduledSample;
            _mixBuffer = new float[MaximumFramesPerRender * channelCount];
            _commands = Channel.CreateBounded<PlaybackCommand>(new BoundedChannelOptions(CommandQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public WaveFormat WaveFormat { get; }

        public bool TryEnqueue(PlaybackCommand command) => _commands.Writer.TryWrite(command);

        public void SetPattern(Pattern? pattern, IReadOnlyList<AudioSample?>? stepSamples)
        {
            _scheduler.SetPattern(pattern, stepSamples);
        }

        public void SetArrangement(Project? project, IReadOnlyDictionary<Guid, AudioSample>? trackSamples)
        {
            _arrangementScheduler.SetProject(project, trackSamples);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (offset < 0 || count < 0 || offset > buffer.Length - count || count % WaveFormat.BlockAlign != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var framesRemaining = count / WaveFormat.BlockAlign;
            var byteOffset = offset;
            while (framesRemaining > 0)
            {
                var frameCount = Math.Min(framesRemaining, MaximumFramesPerRender);
                while (_commands.Reader.TryRead(out var command))
                {
                    _mixer.TryStartVoice(command.Sample, command.Volume, command.Pan);
                }

                _scheduler.Process(_clock.CurrentTick, _scheduledTrigger);
                _arrangementScheduler.Process(_clock.CurrentTick, _scheduledTrigger);

                var sampleCount = frameCount * WaveFormat.Channels;
                var samples = _mixBuffer.AsSpan(0, sampleCount);
                _mixer.Render(samples);
                for (var index = 0; index < sampleCount; index++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        buffer.AsSpan(byteOffset + (index * sizeof(float)), sizeof(float)),
                        BitConverter.SingleToInt32Bits(samples[index]));
                }

                _clock.AdvanceFrames(frameCount);
                framesRemaining -= frameCount;
                byteOffset += sampleCount * sizeof(float);
            }

            return count;
        }

        private void TryStartScheduledSample(AudioSample sample, float volume, float pan) =>
            _mixer.TryStartVoice(sample, volume, pan);
    }
}
