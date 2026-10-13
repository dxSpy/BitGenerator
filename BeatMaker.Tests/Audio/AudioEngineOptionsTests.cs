using BeatMaker.Audio.Configuration;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates audio output configuration constraints.</summary>
public sealed class AudioEngineOptionsTests
{
    [Fact]
    public void Constructor_AcceptsAsioWithDriverName()
    {
        var options = new AudioEngineOptions(
            backend: AudioOutputBackend.Asio,
            asioDriverName: "  ASIO Driver  ");

        options.AsioDriverName.Should().Be("ASIO Driver");
    }

    [Fact]
    public void Constructor_RejectsAsioWithoutDriverName()
    {
        Action create = () => new AudioEngineOptions(backend: AudioOutputBackend.Asio);

        create.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(7_999)]
    [InlineData(192_001)]
    public void Constructor_RejectsUnsupportedSampleRate(int sampleRate)
    {
        Action create = () => new AudioEngineOptions(sampleRate: sampleRate);

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(21)]
    public void Constructor_RejectsLatencyOutsideSupportedRange(int latencyMilliseconds)
    {
        Action create = () => new AudioEngineOptions(bufferLatencyMilliseconds: latencyMilliseconds);

        create.Should().Throw<ArgumentOutOfRangeException>();
    }
}
