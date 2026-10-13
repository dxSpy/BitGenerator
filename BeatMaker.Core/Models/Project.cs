using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Represents a complete beatmaking project and its timeline content.</summary>
public sealed record Project
{
    /// <summary>Creates a project with validated tempo, swing, and clip references.</summary>
    /// <param name="id">Unique project identifier.</param>
    /// <param name="name">User-visible project name.</param>
    /// <param name="bpm">Project tempo in beats per minute.</param>
    /// <param name="swing">Normalized swing amount from 0 to 1.</param>
    /// <param name="tracks">Project tracks.</param>
    /// <param name="patterns">Reusable patterns.</param>
    /// <param name="clips">Pattern placements on the timeline.</param>
    /// <param name="assets">External media referenced by the project.</param>
    public Project(
        Guid id,
        string name,
        int bpm = 120,
        float swing = 0f,
        IReadOnlyList<Track>? tracks = null,
        IReadOnlyList<Pattern>? patterns = null,
        IReadOnlyList<Clip>? clips = null,
        IReadOnlyList<ProjectAsset>? assets = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        }

        if (bpm is < MusicalConstants.MinimumBpm or > MusicalConstants.MaximumBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(bpm));
        }

        if (!float.IsFinite(swing) || swing is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(swing));
        }

        var trackArray = tracks?.ToArray() ?? [];
        var patternArray = patterns?.ToArray() ?? [];
        var clipArray = clips?.ToArray() ?? [];
        var assetArray = assets?.ToArray() ?? [];
        EnsureUniqueIds(trackArray.Select(track => track.Id), nameof(tracks));
        EnsureUniqueIds(patternArray.Select(pattern => pattern.Id), nameof(patterns));
        EnsureUniqueIds(clipArray.Select(clip => clip.Id), nameof(clips));
        EnsureUniqueIds(assetArray.Select(asset => asset.Id), nameof(assets));

        var trackIds = trackArray.Select(track => track.Id).ToHashSet();
        var patternIds = patternArray.Select(pattern => pattern.Id).ToHashSet();
        foreach (var clip in clipArray)
        {
            if (!trackIds.Contains(clip.TrackId))
            {
                throw new ArgumentException($"Clip '{clip.Id}' refers to an unknown track.", nameof(clips));
            }

            if (!patternIds.Contains(clip.PatternId))
            {
                throw new ArgumentException($"Clip '{clip.Id}' refers to an unknown pattern.", nameof(clips));
            }
        }

        Id = id;
        Name = name.Trim();
        Bpm = bpm;
        Swing = swing;
        Tracks = Array.AsReadOnly(trackArray);
        Patterns = Array.AsReadOnly(patternArray);
        Clips = Array.AsReadOnly(clipArray);
        Assets = Array.AsReadOnly(assetArray);
    }

    /// <summary>Gets the unique project identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the project name.</summary>
    public string Name { get; }

    /// <summary>Gets the project tempo in beats per minute.</summary>
    public int Bpm { get; }

    /// <summary>Gets the normalized swing amount.</summary>
    public float Swing { get; }

    /// <summary>Gets the project tracks.</summary>
    public IReadOnlyList<Track> Tracks { get; }

    /// <summary>Gets the project's reusable patterns.</summary>
    public IReadOnlyList<Pattern> Patterns { get; }

    /// <summary>Gets the timeline clips.</summary>
    public IReadOnlyList<Clip> Clips { get; }

    /// <summary>Gets external audio and media assets used by the project.</summary>
    public IReadOnlyList<ProjectAsset> Assets { get; }

    private static void EnsureUniqueIds(IEnumerable<Guid> ids, string parameterName)
    {
        if (ids.Distinct().Count() != ids.Count())
        {
            throw new ArgumentException("Identifiers must be unique within a collection.", parameterName);
        }
    }
}
