using BeatMaker.Core.Constants;
using BeatMaker.Core.Timing;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Timing;

/// <summary>Validates transport-clock timing and state transitions.</summary>
public sealed class TransportClockTests
{
    [Fact]
    public void AdvanceFrames_ConvertsOneQuarterNoteToPpqTicks()
    {
        var clock = new TransportClock(sampleRate: 48_000, tempoBpm: 120);
        clock.Start();

        clock.AdvanceFrames(24_000);

        clock.CurrentTick.Should().Be(MusicalConstants.PulsesPerQuarterNote);
        clock.SamplePosition.Should().Be(24_000);
    }

    [Fact]
    public void AdvanceFrames_PreservesFractionalTicksAcrossBuffers()
    {
        var clock = new TransportClock(sampleRate: 44_100, tempoBpm: 123);
        clock.Start();

        for (var i = 0; i < 100; i++)
        {
            clock.AdvanceFrames(441);
        }

        clock.CurrentTick.Should().Be(1_968);
        clock.SamplePosition.Should().Be(44_100);
    }

    [Fact]
    public void Pause_RetainsPositionAndStopsAdvancing()
    {
        var clock = new TransportClock(sampleRate: 48_000, tempoBpm: 120);
        clock.Start();
        clock.AdvanceFrames(12_000);
        clock.Pause();

        clock.AdvanceFrames(12_000);

        clock.CurrentTick.Should().Be(480);
        clock.SamplePosition.Should().Be(12_000);
    }

    [Fact]
    public void Reset_ClearsPositionAndStopsClock()
    {
        var clock = new TransportClock(sampleRate: 48_000, tempoBpm: 120);
        clock.Start();
        clock.AdvanceFrames(12_000);

        clock.Reset();
        clock.AdvanceFrames(12_000);

        clock.IsRunning.Should().BeFalse();
        clock.CurrentTick.Should().Be(0);
        clock.SamplePosition.Should().Be(0);
    }

    [Fact]
    public void SetTempo_ChangesSubsequentTickRateWithoutResettingPosition()
    {
        var clock = new TransportClock(sampleRate: 48_000, tempoBpm: 120);
        clock.Start();
        clock.AdvanceFrames(12_000);

        clock.SetTempo(60);
        clock.AdvanceFrames(24_000);

        clock.CurrentTick.Should().Be(960);
        clock.TempoBpm.Should().Be(60);
    }

    [Theory]
    [InlineData(39)]
    [InlineData(301)]
    public void SetTempo_RejectsUnsupportedTempo(int bpm)
    {
        var clock = new TransportClock(sampleRate: 48_000, tempoBpm: 120);

        Action setTempo = () => clock.SetTempo(bpm);

        setTempo.Should().Throw<ArgumentOutOfRangeException>();
    }
}
