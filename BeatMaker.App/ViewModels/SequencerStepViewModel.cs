using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BeatMaker.App.ViewModels;

/// <summary>Exposes the editable state of one sequencer step.</summary>
public sealed class SequencerStepViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;

    /// <summary>Creates a step view model for the specified index.</summary>
    /// <param name="index">Zero-based step index.</param>
    /// <param name="isEnabled">Whether the step is initially enabled.</param>
    public SequencerStepViewModel(int index, bool isEnabled = false)
    {
        if (index is < 0 or >= 64)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        _isEnabled = isEnabled;
        ToggleCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the zero-based step index.</summary>
    public int Index { get; }

    /// <summary>Gets or sets whether this step triggers its sample.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that toggles this step.</summary>
    public RelayCommand ToggleCommand { get; }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
