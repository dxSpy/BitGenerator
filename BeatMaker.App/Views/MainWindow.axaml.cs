using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BeatMaker.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BeatMaker.App.Views;

/// <summary>
/// Represents the initial BeatMaker desktop window.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
        : this(App.Services.GetRequiredService<MainWindowViewModel>())
    {
    }

    /// <summary>
    /// Initializes the main window and its XAML controls.
    /// </summary>
    private readonly MainWindowViewModel _viewModel;

    /// <summary>Initializes the main window and its view model.</summary>
    /// <param name="viewModel">Main window view model.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var importButton = this.FindControl<Button>("ImportSampleButton");
        var saveButton = this.FindControl<Button>("SaveProjectButton");
        var openButton = this.FindControl<Button>("OpenProjectButton");
        var importMidiButton = this.FindControl<Button>("ImportMidiButton");
        var exportMidiButton = this.FindControl<Button>("ExportMidiButton");
        if (importButton is not null)
        {
            importButton.Click += OnImportSampleClick;
        }

        if (saveButton is not null)
        {
            saveButton.Click += OnSaveProjectClick;
        }

        if (openButton is not null)
        {
            openButton.Click += OnOpenProjectClick;
        }

        if (importMidiButton is not null)
        {
            importMidiButton.Click += OnImportMidiClick;
        }

        if (exportMidiButton is not null)
        {
            exportMidiButton.Click += OnExportMidiClick;
        }
    }

    private async void OnImportSampleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите WAV-сэмпл",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("WAV audio")
                {
                    Patterns = ["*.wav"]
                }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        var localPath = files[0].TryGetLocalPath();
        if (localPath is null)
        {
            return;
        }

        await _viewModel.LoadSampleAsync(localPath);
    }

    private async void OnSaveProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить проект BeatMaker",
            SuggestedFileName = "BeatMakerProject.beatmaker",
            DefaultExtension = "beatmaker",
            FileTypeChoices =
            [
                new FilePickerFileType("BeatMaker project")
                {
                    Patterns = ["*.beatmaker"]
                }
            ]
        });

        var localPath = file?.TryGetLocalPath();
        if (localPath is not null)
        {
            await _viewModel.SaveProjectAsync(localPath);
        }

    private async void OnImportMidiClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Импорт MIDI-паттерна",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("MIDI files")
                {
                    Patterns = ["*.mid", "*.midi"]
                }
            ]
        });

        var localPath = files.FirstOrDefault()?.TryGetLocalPath();
        if (localPath is not null)
        {
            await _viewModel.ImportMidiAsync(localPath);
        }
    }

    private async void OnExportMidiClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспорт MIDI-паттерна",
            SuggestedFileName = "BeatMaker.mid",
            DefaultExtension = "mid",
            FileTypeChoices =
            [
                new FilePickerFileType("MIDI files")
                {
                    Patterns = ["*.mid"]
                }
            ]
        });

        var localPath = file?.TryGetLocalPath();
        if (localPath is not null)
        {
            await _viewModel.ExportMidiAsync(localPath);
        }
    }
    }

    private async void OnOpenProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть проект BeatMaker",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("BeatMaker project")
                {
                    Patterns = ["*.beatmaker", "*.json"]
                }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        var localPath = files[0].TryGetLocalPath();
        if (localPath is not null)
        {
            await _viewModel.OpenProjectAsync(localPath);
        }
    }
}
