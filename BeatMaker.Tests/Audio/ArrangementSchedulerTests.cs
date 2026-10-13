using BeatMaker.Audio.Scheduling;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates sample scheduling from project timeline clips.</summary>
public sealed class ArrangementSchedulerTests
{
    [Fact]
    public void Process_TriggersPatternAtClipOffsetWithTrackMixSettings()
    {
        var fixture = CreateFixture(
            new TrackSettings(Volume: 0.5f, Pan: -0.25f),
            clipStartTick: MusicalConstants.TicksPerSixteenth,
            clipLengthTicks: 2 * MusicalConstants.TicksPerSixteenth,
            isLooped: false);
        var events = new List<(AudioSample Sample, float Volume, float Pan)>();

        fixture.Scheduler.Process(0, Record);
        fixture.Scheduler.Process(2 * MusicalConstants.TicksPerSixteenth, Record);

        events.Should().ContainSingle();
        events[0].Sample.Should().BeSameAs(fixture.Sample);
        events[0].Volume.Should().Be(0.25f);
        events[0].Pan.Should().Be(-0.25f);
        return;

        void Record(AudioSample sample, float volume, float pan) => events.Add((sample, volume, pan));
    }

    [Fact]
    public void Process_LoopedClipRepeatsPatternUntilClipEnd()
    {
        var fixture = CreateFixture(
            new TrackSettings(),
            clipStartTick: 0,
            clipLengthTicks: 18 * MusicalConstants.TicksPerSixteenth,
            isLooped: true);
        var triggers = 0;

        fixture.Scheduler.Process(0, (_, _, _) => { });
        for (var step = 1; step <= 20; step++)
        {
            fixture.Scheduler.Process(step * MusicalConstants.TicksPerSixteenth, (_, _, _) => triggers++);
        }

        triggers.Should().Be(3);
    }

    [Fact]
    public void Process_NonLoopedClipStopsAtClipLength()
    {
        var fixture = CreateFixture(
            new TrackSettings(),
            clipStartTick: 0,
            clipLengthTicks: 2 * MusicalConstants.TicksPerSixteenth,
            isLooped: false);
        var triggers = 0;

        fixture.Scheduler.Process(0, (_, _, _) => triggers++);
        fixture.Scheduler.Process(4 * MusicalConstants.TicksPerSixteenth, (_, _, _) => triggers++);

        triggers.Should().Be(2);
    }

    [Fact]
    public void Process_MutedTrackDoesNotTrigger()
    {
        var fixture = CreateFixture(new TrackSettings(IsMuted: true), 0, MusicalConstants.TicksPerSixteenth, false);
        var triggers = 0;

        fixture.Scheduler.Process(0, (_, _, _) => triggers++);
        fixture.Scheduler.Process(MusicalConstants.TicksPerSixteenth, (_, _, _) => triggers++);

        triggers.Should().Be(0);
    }

    [Fact]
    public void Process_SoloTrackSuppressesNonSoloTracks()
    {
        var trackA = new Track(Guid.NewGuid(), "A", TrackType.Drum);
        var trackB = new Track(Guid.NewGuid(), "B", TrackType.Drum, isSolo: true);
        var patternA = CreateActivePattern("A");
        var patternB = CreateActivePattern("B");
        var clipA = new Clip(Guid.NewGuid(), trackA.Id, patternA.Id, 0, MusicalConstants.TicksPerSixteenth);
        var clipB = new Clip(Guid.NewGuid(), trackB.Id, patternB.Id, 0, MusicalConstants.TicksPerSixteenth);
        var project = new Project(
            Guid.NewGuid(),
            "Solo",
            tracks: [trackA, trackB],
            patterns: [patternA, patternB],
            clips: [clipA, clipB]);
        var sampleA = new AudioSample("A", 48_000, 1, [1f]);
        var sampleB = new AudioSample("B", 48_000, 1, [1f]);
        var scheduler = new ArrangementScheduler();
        scheduler.SetProject(project, new Dictionary<Guid, AudioSample> { [trackA.Id] = sampleA, [trackB.Id] = sampleB });
        var samples = new List<AudioSample>();

        scheduler.Process(0, (_, _, _) => { });
        scheduler.Process(MusicalConstants.TicksPerSixteenth, (sample, _, _) => samples.Add(sample));

        samples.Should().ContainSingle().Which.Should().BeSameAs(sampleB);
    }

    private static Fixture CreateFixture(TrackSettings settings, long clipStartTick, long clipLengthTicks, bool isLooped)
    {
        var track = new Track(
            Guid.NewGuid(),
            "Drums",
            TrackType.Drum,
            volume: settings.Volume,
            pan: settings.Pan,
            isMuted: settings.IsMuted);
        var pattern = CreateActivePattern("Hit", velocity: 0.5f);
        var clip = new Clip(Guid.NewGuid(), track.Id, pattern.Id, clipStartTick, clipLengthTicks, isLooped);
        var project = new Project(Guid.NewGuid(), "Test", tracks: [track], patterns: [pattern], clips: [clip]);
        var sample = new AudioSample("Kick", 48_000, 1, [1f]);
        var scheduler = new ArrangementScheduler();
        scheduler.SetProject(project, new Dictionary<Guid, AudioSample> { [track.Id] = sample });
        return new Fixture(scheduler, sample);
    }

    private static Pattern CreateActivePattern(string name, float velocity = 1f)
    {
        var steps = Enumerable.Range(0, 16)
            .Select(index => new Step(index, index == 0, velocity: velocity))
            .ToArray();
        return new Pattern(Guid.NewGuid(), name, steps: steps);
    }

    private readonly record struct TrackSettings(float Volume = 1f, float Pan = 0f, bool IsMuted = false);
    private readonly record struct Fixture(ArrangementScheduler Scheduler, AudioSample Sample);
}
