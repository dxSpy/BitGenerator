using BeatMaker.Core.Audio;

namespace BeatMaker.Audio.Samples;

/// <summary>Mixes a fixed number of PCM sample voices into preallocated output buffers.</summary>
public sealed class AudioSampleMixer
{
    /// <summary>Maximum number of simultaneously active sample voices.</summary>
    public const int MaximumVoiceCount = 64;

    private readonly Voice[] _voices = new Voice[MaximumVoiceCount];
    private readonly int _outputSampleRate;
    private readonly int _outputChannelCount;

    /// <summary>Creates a mixer for the specified output format.</summary>
    /// <param name="outputSampleRate">Output sample rate in frames per second.</param>
    /// <param name="outputChannelCount">Output channel count; one or two channels are supported.</param>
    public AudioSampleMixer(int outputSampleRate, int outputChannelCount)
    {
        if (outputSampleRate is < 8_000 or > 192_000)
        {
            throw new ArgumentOutOfRangeException(nameof(outputSampleRate));
        }

        if (outputChannelCount is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(outputChannelCount));
        }

        _outputSampleRate = outputSampleRate;
        _outputChannelCount = outputChannelCount;
        for (var index = 0; index < _voices.Length; index++)
        {
            _voices[index] = new Voice();
        }
    }

    /// <summary>Gets the number of currently active voices.</summary>
    public int ActiveVoiceCount
    {
        get
        {
            var activeCount = 0;
            for (var index = 0; index < _voices.Length; index++)
            {
                if (_voices[index].IsActive)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }
    }

    /// <summary>Starts a sample in the first available voice.</summary>
    /// <param name="sample">Decoded PCM sample.</param>
    /// <param name="volume">Linear playback volume from 0 to 1.</param>
    /// <param name="pan">Stereo pan from -1 to 1.</param>
    /// <returns><see langword="true"/> if a voice was available.</returns>
    public bool TryStartVoice(AudioSample sample, float volume, float pan)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (!float.IsFinite(volume) || volume is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        if (!float.IsFinite(pan) || pan is < -1f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(pan));
        }

        for (var index = 0; index < _voices.Length; index++)
        {
            if (!_voices[index].IsActive)
            {
                _voices[index].Start(sample, volume, pan);
                return true;
            }
        }

        return false;
    }

    /// <summary>Renders and sums active voices into the supplied interleaved output span.</summary>
    /// <param name="output">Output span containing complete frames, cleared before mixing.</param>
    public void Render(Span<float> output)
    {
        if (output.Length % _outputChannelCount != 0)
        {
            throw new ArgumentException("Output must contain complete frames.", nameof(output));
        }

        output.Clear();
        var frameCount = output.Length / _outputChannelCount;
        for (var voiceIndex = 0; voiceIndex < _voices.Length; voiceIndex++)
        {
            var voice = _voices[voiceIndex];
            if (!voice.IsActive)
            {
                continue;
            }

            var source = voice.Samples.Span;
            var sourceChannels = voice.ChannelCount;
            var sourceFrameCount = source.Length / sourceChannels;
            var position = voice.Position;
            var frameStep = (double)voice.SampleRate / _outputSampleRate;
            var leftGain = voice.Volume * (voice.Pan > 0f ? 1f - voice.Pan : 1f);
            var rightGain = voice.Volume * (voice.Pan < 0f ? 1f + voice.Pan : 1f);

            for (var frame = 0; frame < frameCount && position < sourceFrameCount; frame++)
            {
                var sourceFrame = (int)position;
                var nextFrame = Math.Min(sourceFrame + 1, sourceFrameCount - 1);
                var fraction = (float)(position - sourceFrame);
                var sourceOffset = sourceFrame * sourceChannels;
                var nextOffset = nextFrame * sourceChannels;
                var left = Interpolate(source[sourceOffset], source[nextOffset], fraction);
                var right = sourceChannels == 1
                    ? left
                    : Interpolate(source[sourceOffset + 1], source[nextOffset + 1], fraction);
                var outputOffset = frame * _outputChannelCount;

                if (_outputChannelCount == 1)
                {
                    output[outputOffset] += ((left + right) * 0.5f) * voice.Volume;
                }
                else
                {
                    output[outputOffset] += left * leftGain;
                    output[outputOffset + 1] += right * rightGain;
                }

                position += frameStep;
            }

            if (position >= sourceFrameCount)
            {
                voice.Stop();
            }
            else
            {
                voice.Position = position;
            }
        }
    }

    private static float Interpolate(float first, float second, float fraction) => first + ((second - first) * fraction);

    private sealed class Voice
    {
        public bool IsActive { get; private set; }
        public ReadOnlyMemory<float> Samples { get; private set; }
        public int SampleRate { get; private set; }
        public int ChannelCount { get; private set; }
        public float Volume { get; private set; }
        public float Pan { get; private set; }
        public double Position { get; set; }

        public void Start(AudioSample sample, float volume, float pan)
        {
            Samples = sample.Samples;
            SampleRate = sample.SampleRate;
            ChannelCount = sample.ChannelCount;
            Volume = volume;
            Pan = pan;
            Position = 0;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
            Samples = ReadOnlyMemory<float>.Empty;
            Position = 0;
        }
    }
}
