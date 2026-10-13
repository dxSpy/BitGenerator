using BeatMaker.App.Views;
using BeatMaker.App.ViewModels;
using BeatMaker.App.Persistence;
using BeatMaker.Audio.Configuration;
using BeatMaker.Audio.Engine;
using BeatMaker.Audio.Midi;
using BeatMaker.Audio.Samples;
using BeatMaker.Core.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BeatMaker.App.DependencyInjection;

internal static class ServiceRegistration
{
    internal static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(logging => logging.AddSerilog(logger, dispose: true));
        services.AddSingleton(new AudioEngineOptions());
        services.AddSingleton<IAudioEngine, NAudioAudioEngine>();
        services.AddSingleton<IAudioSampleLoader, NAudioSampleLoader>();
        services.AddSingleton<IMidiPatternService, DryWetMidiPatternService>();
        services.AddSingleton<IProjectPersistenceService, JsonProjectPersistenceService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
