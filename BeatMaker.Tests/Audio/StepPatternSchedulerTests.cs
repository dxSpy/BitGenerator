using BeatMaker.Audio.Scheduling;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates step pattern scheduling against PPQ ticks.</summary>
public sealed class StepPatternSchedulerTests
{
    [Fact]
    public void Process_TriggersEnabledStepsAndLoopsPattern()
    {
        var scheduler = new StepPatternScheduler();
        var sample = new AudioSample("Hit", 48_000, 1, [1f]);
        var pattern = CreatePattern(new Step(0, true), new Step(1, true));
        scheduler.SetPattern(pattern, CreateAssignments(sample));
        var triggers = 0;

        scheduler.Process(0, (_, _, _) => triggers++).Should().Be(1);
        scheduler.Process(17 * MusicalConstants.TicksPerSixteenth, (_, _, _) => triggers++).Should().Be(3);

        triggers.Should().Be(4);
    }

    [Fact]
    public void Process_SkipsDisabledAndUnassignedSteps()
    {
        var scheduler = new StepPatternScheduler();
        var sample = new AudioSample("Hit", 48_000, 1, [1f]);
        var pattern = CreatePattern(new Step(0, true), new Step(1, false));
        scheduler.SetPattern(pattern, CreateAssignments(sample));
        var triggers = 0;

        scheduler.Process(0, (_, _, _) => triggers++);
        scheduler.Process(2 * MusicalConstants.TicksPerSixteenth, (_, _, _) => triggers++);

        triggers.Should().Be(1);
    }

    [Fact]
    public void Process_ProbabilityZeroNeverTriggersAndOneAlwaysTriggers()
    {
        var scheduler = new StepPatternScheduler();
        var sample = new AudioSample("Hit", 48_000, 1, [1f]);
        var pattern = CreatePattern(
            new Step(0, true, probability: 0f),
            new Step(1, true, probability: 1f));
        scheduler.SetPattern(pattern, CreateAssignments(sample));
        var triggeredSteps = new List<int>();

        scheduler.Process(0, (_, _, _) => triggeredSteps.Add(0));
        scheduler.Process(2 * MusicalConstants.TicksPerSixteenth, (_, _, _) => triggeredSteps.Add(1));

        triggeredSteps.Should().Equal(1);
    }

    [Fact]
    public void Process_TriggersRatchetsAtSubStepTicksWithAdjustedVelocity()
    {
        var scheduler = new StepPatternScheduler();
        var sample = new AudioSample("Hit", 48_000, 1, [1f]);
        var pattern = CreatePattern(new Step(0, true, velocity: 1f, ratchetCount: 4));
        scheduler.SetPattern(pattern, CreateAssignments(sample));
        var volumes = new List<float>();

        scheduler.Process(0, (_, volume, _) => volumes.Add(volume));
        scheduler.Process(MusicalConstants.TicksPerSixteenth / 2, (_, volume, _) => volumes.Add(volume));
        scheduler.Process(MusicalConstants.TicksPerSixteenth, (_, volume, _) => volumes.Add(volume));

        volumes.Should().HaveCount(4).And.OnlyContain(volume => Math.Abs(volume - 0.5f) < 0.0001f);
    }

    [Fact]
    public void SetPattern_RejectsAssignmentCountMismatch()
    {
        var scheduler = new StepPatternScheduler();
        var pattern = CreatePattern(new Step(0, true), new Step(1, true));

        Action setPattern = () => scheduler.SetPattern(pattern, [new AudioSample("Hit", 48_000, 1, [1f])]);

        setPattern.Should().Throw<ArgumentException>();
    }

    private static Pattern CreatePattern(params Step[] steps)
    {
        var completeSteps = Enumerable.Range(0, 16)
            .Select(index => index < steps.Length ? steps[index] : new Step(index, false))
            .ToArray();
        return new Pattern(Guid.NewGuid(), "Test", steps: completeSteps);
    }

    private static AudioSample?[] CreateAssignments(AudioSample sample)
    {
        var assignments = new AudioSample?[16];
        Array.Fill(assignments, sample);
        return assignments;
    }
}
