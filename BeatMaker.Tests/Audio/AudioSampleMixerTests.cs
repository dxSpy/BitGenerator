using BeatMaker.Audio.Samples;
using BeatMaker.Core.Audio;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates sample voice mixing and resampling.</summary>
public sealed class AudioSampleMixerTests
{
    [Fact]
    public void Render_MixesVoicesWithVolumeAndPan()
    {
        var mixer = new AudioSampleMixer(48_000, 2);
        var sample = new AudioSample("Tone", 48_000, 1, [1f, 1f]);
        mixer.TryStartVoice(sample, volume: 0.5f, pan: -1f).Should().BeTrue();
        mixer.TryStartVoice(sample, volume: 0.25f, pan: 1f).Should().BeTrue();

        var output = new float[4];
        mixer.Render(output);

        output.Should().Equal(0.5f, 0.25f, 0.5f, 0.25f);
        mixer.ActiveVoiceCount.Should().Be(0);
    }

    [Fact]
    public void Render_ResamplesByLinearInterpolation()
    {
        var mixer = new AudioSampleMixer(48_000, 1);
        var sample = new AudioSample("Ramp", 24_000, 1, [0f, 1f, 0f]);
        mixer.TryStartVoice(sample, volume: 1f, pan: 0f).Should().BeTrue();

        var output = new float[6];
        mixer.Render(output);

        output.Should().Equal(0f, 0.5f, 1f, 0.5f, 0f, 0f);
        mixer.ActiveVoiceCount.Should().Be(0);
    }

    [Fact]
    public void TryStartVoice_ReturnsFalseWhenVoicePoolIsFull()
    {
        var mixer = new AudioSampleMixer(48_000, 1);
        var sample = new AudioSample("Long", 48_000, 1, new float[AudioSampleMixer.MaximumVoiceCount + 1]);

        for (var voice = 0; voice < AudioSampleMixer.MaximumVoiceCount; voice++)
        {
            mixer.TryStartVoice(sample, volume: 1f, pan: 0f).Should().BeTrue();
        }

        mixer.TryStartVoice(sample, volume: 1f, pan: 0f).Should().BeFalse();
        mixer.ActiveVoiceCount.Should().Be(AudioSampleMixer.MaximumVoiceCount);
    }

    [Fact]
    public void Render_ConvertsStereoSampleToMono()
    {
        var mixer = new AudioSampleMixer(48_000, 1);
        var sample = new AudioSample("Stereo", 48_000, 2, [1f, -1f]);
        mixer.TryStartVoice(sample, volume: 1f, pan: 0f).Should().BeTrue();

        var output = new float[1];
        mixer.Render(output);

        output[0].Should().Be(0f);
    }
}
