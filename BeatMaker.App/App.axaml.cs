using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BeatMaker.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BeatMaker.App;

/// <summary>
/// Represents the Avalonia application and resolves its desktop window from dependency injection.
/// </summary>
public partial class App : Application
{
    internal static IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Loads the application resources from XAML.
    /// </summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Creates the main desktop window after the Avalonia framework is initialized.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
