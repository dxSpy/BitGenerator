using System.Text.Json;
using BeatMaker.App.Persistence;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Persistence;

/// <summary>Validates versioned project JSON persistence.</summary>
public sealed class JsonProjectPersistenceServiceTests
{
    private readonly JsonProjectPersistenceService _service = new();

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsCompleteProject()
    {
        var project = CreateProject();
        var path = CreateTemporaryPath();
        try
        {
            await _service.SaveAsync(project, path);
            var loaded = await _service.LoadAsync(path);

            loaded.Should().BeEquivalentTo(project);
            File.Exists(path).Should().BeTrue();
            Directory.GetFiles(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.*.tmp").Should().BeEmpty();
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsUnsupportedDocumentVersion()
    {
        var path = CreateTemporaryPath();
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":99,\"id\":\"9b870e0d-bfd5-4419-b9ad-cfe5a278014b\",\"name\":\"Test\",\"tracks\":[],\"patterns\":[],\"clips\":[],\"assets\":[]}");

            var act = () => _service.LoadAsync(path);

            await act.Should().ThrowAsync<InvalidDataException>().WithMessage("Unsupported project document version: 99.");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsInvalidDomainData()
    {
        var path = CreateTemporaryPath();
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":1,\"id\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"Bad\",\"tracks\":[],\"patterns\":[],\"clips\":[],\"assets\":[]}");

            var act = () => _service.LoadAsync(path);

            await act.Should().ThrowAsync<InvalidDataException>().WithMessage("Project document contains invalid domain data.");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsMalformedJson()
    {
        var path = CreateTemporaryPath();
        try
        {
            await File.WriteAllTextAsync(path, "{invalid-json");

            var act = () => _service.LoadAsync(path);

            await act.Should().ThrowAsync<JsonException>();
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsOversizedFile()
    {
        var path = CreateTemporaryPath();
        try
        {
            await using (var stream = File.Create(path))
            {
                stream.SetLength(JsonProjectPersistenceService.MaximumProjectFileBytes + 1);
            }

            var act = () => _service.LoadAsync(path);

            await act.Should().ThrowAsync<InvalidDataException>();
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task SaveAsync_ObservesCancellationBeforeWriting()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var path = CreateTemporaryPath();

        var act = () => _service.SaveAsync(CreateProject(), path, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        File.Exists(path).Should().BeFalse();
    }

    private static Project CreateProject()
    {
        var track = new Track(Guid.NewGuid(), "Drums", TrackType.Drum, volume: 0.8f, pan: -0.1f, sendA: 0.2f);
        var steps = Enumerable.Range(0, 16)
            .Select(index => new Step(index, index is 0 or 4, velocity: index == 4 ? 0.75f : 1f, probability: 0.9f, ratchetCount: index == 4 ? 2 : 1))
            .ToArray();
        var note = new Note(60, startTick: 0, durationTicks: 120, velocity: 0.7f);
        var pattern = new Pattern(Guid.NewGuid(), "Beat", steps: steps, notes: [note]);
        var clip = new Clip(Guid.NewGuid(), track.Id, pattern.Id, startTick: 960, lengthTicks: pattern.LengthTicks, isLooped: true);
        var asset = new ProjectAsset(Guid.NewGuid(), "Kick", "samples/kick.wav");
        return new Project(Guid.NewGuid(), "Round trip", bpm: 137, swing: 0.25f, tracks: [track], patterns: [pattern], clips: [clip], assets: [asset]);
    }

    private static string CreateTemporaryPath() => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.beatmaker");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
