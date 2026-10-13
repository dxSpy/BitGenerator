using BeatMaker.Core.Audio;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates decoded PCM sample invariants.</summary>
public sealed class AudioSampleTests
{
    [Fact]
    public void Constructor_CopiesPcmAndCalculatesDuration()
    {
        var input = new[] { 0.25f, -0.25f, 0.5f, -0.5f };
        var sample = new AudioSample("  Kick  ", 48_000, 2, input);
        input[0] = 1f;

        sample.Name.Should().Be("Kick");
        sample.FrameCount.Should().Be(2);
        sample.DurationSeconds.Should().BeApproximately(2d / 48_000, 0.0000001);
        sample.Samples.Span[0].Should().Be(0.25f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Constructor_RejectsEmptyOrIncompleteFrames(int sampleCount)
    {
        var samples = new float[sampleCount];

        Action create = () => new AudioSample("Sample", 48_000, 2, samples);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_RejectsNonFinitePcm()
    {
        Action create = () => new AudioSample("Sample", 48_000, 1, [float.NaN]);

        create.Should().Throw<ArgumentException>();
    }
}
