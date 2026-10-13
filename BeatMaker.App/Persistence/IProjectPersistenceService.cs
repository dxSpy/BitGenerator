using BeatMaker.Core.Models;

namespace BeatMaker.App.Persistence;

/// <summary>Persists validated BeatMaker project documents.</summary>
public interface IProjectPersistenceService
{
    /// <summary>Saves a project to a versioned JSON file asynchronously.</summary>
    /// <param name="project">Project to save.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(Project project, string filePath, CancellationToken cancellationToken = default);

    /// <summary>Loads and validates a project from a versioned JSON file asynchronously.</summary>
    /// <param name="filePath">Project file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validated project.</returns>
    Task<Project> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
