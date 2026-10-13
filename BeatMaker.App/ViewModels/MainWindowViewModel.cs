using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BeatMaker.Audio.Samples;
using BeatMaker.App.Persistence;
using BeatMaker.Audio.Midi;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;

namespace BeatMaker.App.ViewModels;

/// <summary>Coordinates the main step sequencer, sample selection, and audio transport.</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IAudioEngine _audioEngine;
    private readonly IAudioSampleLoader _sampleLoader;
    private readonly IProjectPersistenceService _projectPersistence;
    private readonly IMidiPatternService _midiPatternService;
    private readonly SequencerStepViewModel[] _stepItems;
    private AudioSample? _sample;
    private int _tempoBpm = 120;
    private string? _statusMessage = "Загрузите WAV-сэмпл и включите шаги.";
    private SequencerStepViewModel? _selectedStep;
    private string? _sampleFilePath;
    private string? _projectFilePath;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _trackId = Guid.NewGuid();
    private readonly Guid _patternId = Guid.NewGuid();
    private readonly Guid _sampleId = Guid.NewGuid();
    private IReadOnlyList<Note> _notes = [];

    /// <summary>Creates the main window view model.</summary>
    /// <param name="audioEngine">Audio transport and playback engine.</param>
    /// <param name="sampleLoader">Audio file importer.</param>
    /// <param name="projectPersistence">Project JSON persistence service.</param>
    /// <param name="midiPatternService">MIDI pattern import/export service.</param>
    public MainWindowViewModel(
        IAudioEngine audioEngine,
        IAudioSampleLoader sampleLoader,
        IProjectPersistenceService projectPersistence,
        IMidiPatternService midiPatternService)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        ArgumentNullException.ThrowIfNull(sampleLoader);
        ArgumentNullException.ThrowIfNull(projectPersistence);
        ArgumentNullException.ThrowIfNull(midiPatternService);
        _audioEngine = audioEngine;
        _sampleLoader = sampleLoader;
        _projectPersistence = projectPersistence;
        _midiPatternService = midiPatternService;
        _stepItems = Enumerable.Range(0, MusicalConstants.MinimumPatternStepCount)
            .Select(index => new SequencerStepViewModel(index))
            .ToArray();
        Steps = new ObservableCollection<SequencerStepViewModel>(_stepItems);
        foreach (var step in _stepItems)
        {
            step.PropertyChanged += OnStepPropertyChanged;
        }

        PlayCommand = new RelayCommand(Play);
        PauseCommand = new RelayCommand(Pause);
        StopCommand = new RelayCommand(Stop);
        LoadSampleCommand = new AsyncRelayCommand(LoadSampleCommandAsync);
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectCommandAsync);
        OpenProjectCommand = new AsyncRelayCommand(OpenProjectCommandAsync);
        ImportMidiCommand = new AsyncRelayCommand(ImportMidiCommandAsync);
        ExportMidiCommand = new AsyncRelayCommand(ExportMidiCommandAsync);
        ToggleStepCommand = new RelayCommand(ToggleSelectedStep, () => SelectedStep is not null);
        _audioEngine.SetTempo(_tempoBpm);
        UpdatePattern();
    }

    /// <summary>Pauses audio playback.</summary>
    public void Pause()
    {
        _audioEngine.Pause();
        OnPropertyChanged(nameof(TransportState));
    }

    /// <summary>Stops audio playback and resets transport.</summary>
    public void Stop()
    {
        _audioEngine.Stop();
        OnPropertyChanged(nameof(TransportState));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the sequencer steps.</summary>
    public ObservableCollection<SequencerStepViewModel> Steps { get; }

    /// <summary>Gets the play command.</summary>
    public RelayCommand PlayCommand { get; }

    /// <summary>Gets the pause command.</summary>
    public RelayCommand PauseCommand { get; }

    /// <summary>Gets the stop command.</summary>
    public RelayCommand StopCommand { get; }

    /// <summary>Gets the command that imports an audio sample.</summary>
    public AsyncRelayCommand LoadSampleCommand { get; }

    /// <summary>Gets the command that saves the current project.</summary>
    public AsyncRelayCommand SaveProjectCommand { get; }

    /// <summary>Gets the command that opens a project.</summary>
    public AsyncRelayCommand OpenProjectCommand { get; }

    /// <summary>Gets the command that imports a MIDI file.</summary>
    public AsyncRelayCommand ImportMidiCommand { get; }

    /// <summary>Gets the command that exports the active pattern to MIDI.</summary>
    public AsyncRelayCommand ExportMidiCommand { get; }

    /// <summary>Gets the keyboard-accessible command for toggling the selected step.</summary>
    public RelayCommand ToggleStepCommand { get; }

    /// <summary>Gets or sets the selected step.</summary>
    public SequencerStepViewModel? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (ReferenceEquals(_selectedStep, value))
            {
                return;
            }

            _selectedStep = value;
            OnPropertyChanged();
            ToggleStepCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the current audio transport state.</summary>
    public TransportState TransportState => _audioEngine.State;

    /// <summary>Gets the display name of the loaded sample.</summary>
    public string SampleName => _sample?.Name ?? "Сэмпл не загружен";

    /// <summary>Gets the current project file path, if the project has been saved.</summary>
    public string? ProjectFilePath => _projectFilePath;

    /// <summary>Gets the current pattern notes imported from or exported to MIDI.</summary>
    public IReadOnlyList<Note> Notes => _notes;

    /// <summary>Imports notes from a MIDI file into the active pattern.</summary>
    /// <param name="filePath">Source MIDI file path.</param>
    public async Task ImportMidiAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            var result = await _midiPatternService.ImportAsync(filePath, "Main");
            if (result.Pattern.StepCount != _stepItems.Length)
            {
                throw new InvalidDataException("The current step grid supports 16-step patterns only.");
            }

            _notes = result.Pattern.Notes;
            if (result.TempoBpm is { } bpm)
            {
                _tempoBpm = bpm;
                _audioEngine.SetTempo(bpm);
                OnPropertyChanged(nameof(TempoBpm));
            }

            UpdatePattern();
            OnPropertyChanged(nameof(Notes));
            StatusMessage = $"Импортировано MIDI-нот: {_notes.Count}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or System.Text.Json.JsonException)
        {
            StatusMessage = $"Не удалось импортировать MIDI: {exception.Message}";
        }
    }

    /// <summary>Exports the active pattern's notes to a MIDI file.</summary>
    /// <param name="filePath">Destination MIDI file path.</param>
    public async Task ExportMidiAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            await _midiPatternService.ExportAsync(CreatePattern(), filePath, _tempoBpm);
            StatusMessage = "MIDI-файл экспортирован.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            StatusMessage = $"Не удалось экспортировать MIDI: {exception.Message}";
        }
    }

    /// <summary>Loads a sample from a local file and assigns it to every sequencer step.</summary>
    /// <param name="filePath">Path to the audio file.</param>
    public async Task LoadSampleAsync(string filePath)
    {
        try
        {
            var loadedSample = await _sampleLoader.LoadAsync(filePath);
            _sample = loadedSample;
            _sampleFilePath = Path.GetFullPath(filePath);
            UpdatePattern();
            OnPropertyChanged(nameof(SampleName));
            StatusMessage = $"Загружен сэмпл: {loadedSample.Name}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or System.Text.Json.JsonException)
        {
            StatusMessage = $"Не удалось загрузить сэмпл: {exception.Message}";
        }
    }

    /// <summary>Saves the current 16-step pattern and its source sample path.</summary>
    /// <param name="filePath">Destination project file path.</param>
    public async Task SaveProjectAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            var track = new Track(_trackId, "Drums", TrackType.Drum);
            var pattern = CreatePattern();
            var clips = new[] { new Clip(Guid.NewGuid(), track.Id, pattern.Id, 0, pattern.LengthTicks, isLooped: true) };
            var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
            var assetPath = _sampleFilePath is null
                ? null
                : Path.GetRelativePath(projectDirectory, _sampleFilePath);
            var project = new Project(
                _projectId,
                "BeatMaker Project",
                _tempoBpm,
                tracks: [track],
                patterns: [pattern],
                clips: clips,
                assets: assetPath is null || _sample is null ? null : [new ProjectAsset(_sampleId, _sample.Name, assetPath)]);
            await _projectPersistence.SaveAsync(project, filePath);
            _projectFilePath = Path.GetFullPath(filePath);
            OnPropertyChanged(nameof(ProjectFilePath));
            StatusMessage = "Проект сохранён.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        {
            StatusMessage = $"Не удалось сохранить проект: {exception.Message}";
        }
    }

    /// <summary>Loads a project and restores its first step pattern and linked sample.</summary>
    /// <param name="filePath">Project file path.</param>
    public async Task OpenProjectAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            var project = await _projectPersistence.LoadAsync(filePath);
            if (project.Patterns.Count == 0 || project.Tracks.Count == 0)
            {
                throw new InvalidDataException("Project must contain a pattern and a track.");
            }

            AudioSample? loadedSample = null;
            string? loadedSamplePath = null;
            var audioAsset = project.Assets.FirstOrDefault();
            if (audioAsset is not null)
            {
                loadedSamplePath = Path.IsPathRooted(audioAsset.FilePath)
                    ? audioAsset.FilePath
                    : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filePath))!, audioAsset.FilePath));
                loadedSample = await _sampleLoader.LoadAsync(loadedSamplePath);
            }

            var pattern = project.Patterns[0];
            if (pattern.StepCount != _stepItems.Length)
            {
                throw new InvalidDataException("The current sequencer UI supports 16-step patterns only.");
            }

            for (var index = 0; index < _stepItems.Length; index++)
            {
                _stepItems[index].IsEnabled = pattern.Steps[index].IsEnabled;
            }

            _sample = loadedSample;
            _sampleFilePath = loadedSamplePath;
            _projectFilePath = Path.GetFullPath(filePath);
            _tempoBpm = project.Bpm;
            _notes = pattern.Notes;
            _audioEngine.SetTempo(_tempoBpm);
            UpdatePattern();
            OnPropertyChanged(nameof(TempoBpm));
            OnPropertyChanged(nameof(SampleName));
            OnPropertyChanged(nameof(ProjectFilePath));
            OnPropertyChanged(nameof(Notes));
            StatusMessage = "Проект загружен.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or System.Text.Json.JsonException)
        {
            StatusMessage = $"Не удалось открыть проект: {exception.Message}";
        }
    }

    /// <summary>Gets or sets the current tempo.</summary>
    public int TempoBpm
    {
        get => _tempoBpm;
        set
        {
            if (value is < MusicalConstants.MinimumBpm or > MusicalConstants.MaximumBpm)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_tempoBpm == value)
            {
                return;
            }

            _tempoBpm = value;
            _audioEngine.SetTempo(value);
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the user-visible status message.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Starts or resumes audio playback.</summary>
    public void Play()
    {
        _audioEngine.Play();
        OnPropertyChanged(nameof(TransportState));
    }

    private void ToggleSelectedStep()
    {
        if (SelectedStep is not null)
        {
            SelectedStep.IsEnabled = !SelectedStep.IsEnabled;
        }
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(SequencerStepViewModel.IsEnabled))
        {
            UpdatePattern();
        }
    }

    private void UpdatePattern()
    {
        var pattern = CreatePattern();
        var assignments = new AudioSample?[_stepItems.Length];
        if (_sample is not null)
        {
            Array.Fill(assignments, _sample);
        }

        _audioEngine.SetPattern(pattern, assignments);
    }

    private Pattern CreatePattern()
    {
        var steps = _stepItems.Select(step => new Step(step.Index, step.IsEnabled)).ToArray();
        return new Pattern(_patternId, "Main", steps: steps, notes: _notes);
    }
    private Task LoadSampleCommandAsync() => _sampleFilePath is null
        ? Task.CompletedTask
        : LoadSampleAsync(_sampleFilePath);

    private Task SaveProjectCommandAsync() => _projectFilePath is null
        ? Task.CompletedTask
        : SaveProjectAsync(_projectFilePath);

    private Task OpenProjectCommandAsync() => _projectFilePath is null
        ? Task.CompletedTask
        : OpenProjectAsync(_projectFilePath);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
