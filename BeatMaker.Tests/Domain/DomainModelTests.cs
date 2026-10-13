using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Domain;

/// <summary>Validates domain-model invariants used by the beatmaking project.</summary>
public sealed class DomainModelTests
{
    /// <summary>Verifies that a new pattern contains an empty default 16-step bar.</summary>
    [Fact]
    public void Pattern_DefaultsToAnEmptySixteenthStepBar()
    {
        var pattern = new Pattern(Guid.NewGuid(), "Main");

        pattern.StepCount.Should().Be(MusicalConstants.MinimumPatternStepCount);
        pattern.LengthTicks.Should().Be(MusicalConstants.PulsesPerQuarterNote * 4L);
        pattern.Steps.Should().HaveCount(MusicalConstants.MinimumPatternStepCount)
            .And.OnlyContain(step => !step.IsEnabled);
    }

    /// <summary>Verifies that unsupported pattern lengths are rejected.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(24)]
    [InlineData(128)]
    public void Pattern_RejectsUnsupportedStepCount(int stepCount)
    {
        Action createPattern = () => new Pattern(Guid.NewGuid(), "Invalid", stepCount);

        createPattern.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies that step collections must be complete and in order.</summary>
    [Fact]
    public void Pattern_RejectsStepsWithMissingPositions()
    {
        IReadOnlyList<Step> steps =
        [
            new Step(0, isEnabled: true),
            new Step(2, isEnabled: false)
        ];

        Action createPattern = () => new Pattern(Guid.NewGuid(), "Invalid", 16, steps);

        createPattern.Should().Throw<ArgumentException>();
    }

    /// <summary>Verifies that invalid probability and ratchet values are rejected.</summary>
    [Fact]
    public void Step_RejectsInvalidPerformanceParameters()
    {
        Action invalidProbability = () => new Step(0, true, probability: float.NaN);
        Action invalidRatchet = () => new Step(0, true, ratchetCount: MusicalConstants.MaximumRatchetCount + 1);
        Action overflowingPitchOffset = () => new Step(0, true, pitchOffsetSemitones: int.MinValue);

        invalidProbability.Should().Throw<ArgumentOutOfRangeException>();
        invalidRatchet.Should().Throw<ArgumentOutOfRangeException>();
        overflowingPitchOffset.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies that the project accepts tempo limits and owns defensive collection copies.</summary>
    [Theory]
    [InlineData(MusicalConstants.MinimumBpm)]
    [InlineData(MusicalConstants.MaximumBpm)]
    public void Project_AcceptsTempoBoundary(int bpm)
    {
        var tracks = new List<Track> { new(Guid.NewGuid(), "Drums", TrackType.Drum) };
        var project = new Project(Guid.NewGuid(), "Demo", bpm, tracks: tracks);
        tracks.Clear();

        project.Bpm.Should().Be(bpm);
        project.Tracks.Should().ContainSingle();
    }

    /// <summary>Verifies that clips cannot refer to tracks or patterns outside the project.</summary>
    [Fact]
    public void Project_RejectsClipWithUnknownPattern()
    {
        var track = new Track(Guid.NewGuid(), "Drums", TrackType.Drum);
        var clip = new Clip(Guid.NewGuid(), track.Id, Guid.NewGuid(), startTick: 0, lengthTicks: 1_920);

        Action createProject = () => new Project(
            Guid.NewGuid(),
            "Invalid arrangement",
            tracks: [track],
            clips: [clip]);

        createProject.Should().Throw<ArgumentException>().WithParameterName("clips");
    }

    /// <summary>Verifies that notes cannot extend past their containing pattern.</summary>
    [Fact]
    public void Pattern_RejectsNoteBeyondPatternLength()
    {
        var note = new Note(pitch: 60, startTick: 3_800, durationTicks: 100);

        Action createPattern = () => new Pattern(Guid.NewGuid(), "Invalid", notes: [note]);

        createPattern.Should().Throw<ArgumentException>().WithParameterName("notes");
    }
}
