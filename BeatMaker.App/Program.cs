using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BeatMaker.App.DependencyInjection;

namespace BeatMaker.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var serviceProvider = ServiceRegistration.BuildServiceProvider();
        App.Services = serviceProvider;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
