using BeatMaker.App.ViewModels;
using BeatMaker.App.Persistence;
using BeatMaker.Audio.Samples;
using BeatMaker.Audio.Midi;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Models;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.App;

/// <summary>Validates main-window view model state and audio-engine integration.</summary>
public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_CreatesSixteenStepsAndInitialPattern()
    {
        var engine = new FakeAudioEngine();
        var viewModel = CreateViewModel(engine);

        viewModel.Steps.Should().HaveCount(16);
        engine.Pattern.Should().NotBeNull();
        engine.Pattern!.StepCount.Should().Be(16);
        engine.StepSamples.Should().HaveCount(16);
    }

    [Fact]
    public async Task SaveProjectAsync_PersistsCurrentPatternTempoAndSamplePath()
    {
        var engine = new FakeAudioEngine();
        var sample = new AudioSample("Kick", 48_000, 1, [0.5f]);
        var persistence = new FakeProjectPersistence();
        var midiNote = new Note(60, 120, 240, 0.75f);
        var midiPattern = new Pattern(Guid.NewGuid(), "Imported", notes: [midiNote]);
        var viewModel = new MainWindowViewModel(engine, new FakeSampleLoader(sample), persistence, new FakeMidiPatternService(new MidiPatternImportResult(midiPattern, null)));
        viewModel.Steps[2].IsEnabled = true;
        viewModel.TempoBpm = 135;
        await viewModel.ImportMidiAsync("notes.mid");
        await viewModel.LoadSampleAsync("kick.wav");

        await viewModel.SaveProjectAsync("project.beatmaker");

        persistence.SavedProject.Should().NotBeNull();
        persistence.SavedProject!.Bpm.Should().Be(135);
        persistence.SavedProject.Patterns[0].Steps[2].IsEnabled.Should().BeTrue();
        persistence.SavedProject.Patterns[0].Notes.Should().ContainSingle().Which.Should().Be(midiNote);
        persistence.SavedProject.Assets.Should().ContainSingle();
        viewModel.ProjectFilePath.Should().Be(Path.GetFullPath("project.beatmaker"));
    }

    [Fact]
    public async Task OpenProjectAsync_RestoresPatternTempoAndSample()
    {
        var sample = new AudioSample("Kick", 48_000, 1, [0.5f]);
        var track = new Track(Guid.NewGuid(), "Drums", TrackType.Drum);
        var steps = Enumerable.Range(0, 16).Select(index => new Step(index, index == 4)).ToArray();
        var note = new Note(64, 240, 480, 0.8f);
        var pattern = new Pattern(Guid.NewGuid(), "Restored", steps: steps, notes: [note]);
        var project = new Project(
            Guid.NewGuid(),
            "Restored",
            bpm: 145,
            tracks: [track],
            patterns: [pattern],
            clips: [new Clip(Guid.NewGuid(), track.Id, pattern.Id, 0, pattern.LengthTicks, isLooped: true)],
            assets: [new ProjectAsset(Guid.NewGuid(), "Kick", Path.GetFullPath("kick.wav"))]);
        var persistence = new FakeProjectPersistence(project);
        var viewModel = new MainWindowViewModel(new FakeAudioEngine(), new FakeSampleLoader(sample), persistence, new FakeMidiPatternService());

        await viewModel.OpenProjectAsync("restored.beatmaker");

        viewModel.TempoBpm.Should().Be(145);
        viewModel.Steps[4].IsEnabled.Should().BeTrue();
        viewModel.Steps[3].IsEnabled.Should().BeFalse();
        viewModel.SampleName.Should().Be("Kick");
        viewModel.Notes.Should().ContainSingle().Which.Should().Be(note);
    }

    [Fact]
    public void TogglingStep_PublishesUpdatedPattern()
    {
        var engine = new FakeAudioEngine();
        var viewModel = CreateViewModel(engine);

        viewModel.Steps[3].ToggleCommand.Execute(null);

        engine.Pattern!.Steps[3].IsEnabled.Should().BeTrue();
        engine.Pattern.Steps[2].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SettingTempo_UpdatesAudioEngine()
    {
        var engine = new FakeAudioEngine();
        var viewModel = CreateViewModel(engine);

        viewModel.TempoBpm = 140;

        engine.TempoBpm.Should().Be(140);
    }

    [Fact]
    public async Task LoadSampleAsync_AssignsLoadedSampleToAllSteps()
    {
        var engine = new FakeAudioEngine();
        var sample = new AudioSample("Kick", 48_000, 1, [0.5f]);
        var viewModel = new MainWindowViewModel(engine, new FakeSampleLoader(sample), new FakeProjectPersistence(), new FakeMidiPatternService());

        await viewModel.LoadSampleAsync("kick.wav");

        viewModel.SampleName.Should().Be("Kick");
        engine.StepSamples.Should().HaveCount(16).And.OnlyContain(assigned => ReferenceEquals(assigned, sample));
    }

    [Fact]
    public void SelectingStep_UpdatesToggleCommandAvailability()
    {
        var viewModel = CreateViewModel(new FakeAudioEngine());

        viewModel.ToggleStepCommand.CanExecute(null).Should().BeFalse();
        viewModel.SelectedStep = viewModel.Steps[0];
        viewModel.ToggleStepCommand.CanExecute(null).Should().BeTrue();
    }

    private static MainWindowViewModel CreateViewModel(FakeAudioEngine engine) =>
        new(engine, new FakeSampleLoader(), new FakeProjectPersistence(), new FakeMidiPatternService());

    private sealed class FakeMidiPatternService : IMidiPatternService
    {
        private readonly MidiPatternImportResult? _importResult;

        public FakeMidiPatternService(MidiPatternImportResult? importResult = null)
        {
            _importResult = importResult;
        }

        public Task<MidiPatternImportResult> ImportAsync(string filePath, string patternName, CancellationToken cancellationToken = default) =>
            _importResult is null
                ? Task.FromException<MidiPatternImportResult>(new FileNotFoundException(filePath))
                : Task.FromResult(_importResult);

        public Task ExportAsync(Pattern pattern, string filePath, int tempoBpm, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeProjectPersistence : IProjectPersistenceService
    {
        private readonly Project? _projectToLoad;

        public FakeProjectPersistence(Project? projectToLoad = null)
        {
            _projectToLoad = projectToLoad;
        }

        public Project? SavedProject { get; private set; }

        public Task SaveAsync(Project project, string filePath, CancellationToken cancellationToken = default)
        {
            SavedProject = project;
            return Task.CompletedTask;
        }

        public Task<Project> LoadAsync(string filePath, CancellationToken cancellationToken = default) =>
            _projectToLoad is null
                ? Task.FromException<Project>(new FileNotFoundException(filePath))
                : Task.FromResult(_projectToLoad);
    }

    private sealed class FakeAudioEngine : IAudioEngine
    {
        public TransportState State { get; private set; } = TransportState.Stopped;
        public int SampleRate => 48_000;
        public int TempoBpm { get; private set; } = 120;
        public long SamplePosition => 0;
        public long CurrentTick => 0;
        public Pattern? Pattern { get; private set; }
        public IReadOnlyList<AudioSample?> StepSamples { get; private set; } = [];

        public void SetTempo(int bpm) => TempoBpm = bpm;
        public bool TryPlaySample(AudioSample sample, float volume = 1f, float pan = 0f) => true;
        public void SetPattern(Pattern? pattern, IReadOnlyList<AudioSample?>? stepSamples = null)
        {
            Pattern = pattern;
            StepSamples = stepSamples ?? [];
        }
        public void SetProjectArrangement(Project? project, IReadOnlyDictionary<Guid, AudioSample>? trackSamples = null) { }
        public void Play() => State = TransportState.Playing;
        public void Pause() => State = TransportState.Paused;
        public void Stop() => State = TransportState.Stopped;
        public void Dispose() { }
    }

    private sealed class FakeSampleLoader(AudioSample? sample = null) : IAudioSampleLoader
    {
        public Task<AudioSample> LoadAsync(string filePath, CancellationToken cancellationToken = default) =>
            sample is null
                ? Task.FromException<AudioSample>(new FileNotFoundException(filePath))
                : Task.FromResult(sample);
    }
}
