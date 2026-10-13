namespace BeatMaker.Core.Models;

/// <summary>References an external media asset used by a project.</summary>
public sealed record ProjectAsset
{
    /// <summary>Creates an asset reference with a stable identifier, display name, and file path.</summary>
    /// <param name="id">Unique asset identifier.</param>
    /// <param name="name">User-visible asset name.</param>
    /// <param name="filePath">Path to the external media file.</param>
    public ProjectAsset(Guid id, string name, string filePath)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Asset ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Asset name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Asset file path cannot be empty.", nameof(filePath));
        }

        Id = id;
        Name = name.Trim();
        FilePath = filePath.Trim();
    }

    /// <summary>Gets the unique asset identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the user-visible asset name.</summary>
    public string Name { get; }

    /// <summary>Gets the external media path.</summary>
    public string FilePath { get; }
}
