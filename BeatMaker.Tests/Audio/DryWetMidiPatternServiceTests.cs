using BeatMaker.Audio.Midi;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates MIDI pattern conversion and file validation.</summary>
public sealed class DryWetMidiPatternServiceTests
{
    private readonly DryWetMidiPatternService _service = new();

    [Fact]
    public async Task ExportAndImportAsync_RoundTripsNotesAndTempo()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mid");
        try
        {
            var note = new Note(64, 120, 360, 0.75f);
            var pattern = new Pattern(Guid.NewGuid(), "Round trip", notes: [note]);
            await _service.ExportAsync(pattern, path, 120);

            var result = await _service.ImportAsync(path, "Imported");

            result.Pattern.Notes.Should().ContainSingle();
            result.Pattern.Notes[0].Pitch.Should().Be(note.Pitch);
            result.Pattern.Notes[0].StartTick.Should().Be(note.StartTick);
            result.Pattern.Notes[0].DurationTicks.Should().Be(note.DurationTicks);
            result.Pattern.Notes[0].Velocity.Should().BeApproximately(note.Velocity, 0.01f);
            result.TempoBpm.Should().Be(120);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_RejectsNonMidiFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mid");
        try
        {
            await File.WriteAllTextAsync(path, "not a MIDI file");

            var act = () => _service.ImportAsync(path, "Invalid");

            await act.Should().ThrowAsync<InvalidDataException>();
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_RejectsPatternLongerThanSupportedLength()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mid");
        try
        {
            var pattern = new Pattern(
                Guid.NewGuid(),
                "Too long",
                MusicalConstants.MaximumPatternStepCount,
                notes: [new Note(60, MusicalConstants.MaximumPatternStepCount * MusicalConstants.TicksPerSixteenth, 1)]);
            await _service.ExportAsync(pattern, path, 120);

            var act = () => _service.ImportAsync(path, "Too long");

            await act.Should().ThrowAsync<InvalidDataException>();
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
