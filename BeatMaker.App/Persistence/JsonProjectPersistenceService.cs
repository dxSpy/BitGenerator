using System.Text.Json;
using System.Text.Json.Serialization;
using BeatMaker.Core.Models;

namespace BeatMaker.App.Persistence;

/// <summary>Stores projects as versioned JSON documents with atomic replacement.</summary>
public sealed class JsonProjectPersistenceService : IProjectPersistenceService
{
    /// <summary>Maximum accepted project document size.</summary>
    public const long MaximumProjectFileBytes = 16L * 1024 * 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 32,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public async Task SaveAsync(Project project, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(stream, ProjectDocument.FromDomain(project), SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Project> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Project file was not found.", fullPath);
        }

        if (fileInfo.Length > MaximumProjectFileBytes)
        {
            throw new InvalidDataException("Project file exceeds the supported 16 MiB size limit.");
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        var document = await JsonSerializer.DeserializeAsync<ProjectDocument>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (document is null)
        {
            throw new InvalidDataException("Project document is empty.");
        }

        try
        {
            return document.ToDomain();
        }
        catch (Exception exception) when (exception is ArgumentException or NullReferenceException or OverflowException)
        {
            throw new InvalidDataException("Project document contains invalid domain data.", exception);
        }
    }
}
