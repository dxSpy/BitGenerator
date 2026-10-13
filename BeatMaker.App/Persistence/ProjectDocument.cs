using BeatMaker.Core.Models;

namespace BeatMaker.App.Persistence;

/// <summary>Versioned JSON representation of a complete BeatMaker project.</summary>
public sealed class ProjectDocument
{
    /// <summary>Current supported project document version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Gets or sets the document format version.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Gets or sets the project identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the project name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the project tempo.</summary>
    public int Bpm { get; set; } = 120;

    /// <summary>Gets or sets the project swing.</summary>
    public float Swing { get; set; }

    /// <summary>Gets or sets the project tracks.</summary>
    public List<TrackDocument> Tracks { get; set; } = [];

    /// <summary>Gets or sets the project patterns.</summary>
    public List<PatternDocument> Patterns { get; set; } = [];

    /// <summary>Gets or sets timeline clips.</summary>
    public List<ClipDocument> Clips { get; set; } = [];

    /// <summary>Gets or sets external audio assets referenced by the project.</summary>
    public List<ProjectAssetDocument> Assets { get; set; } = [];

    /// <summary>Reconstructs and validates a domain project from this document.</summary>
    public Project ToDomain()
    {
        if (Version != CurrentVersion)
        {
            throw new InvalidDataException($"Unsupported project document version: {Version}.");
        }

        ArgumentNullException.ThrowIfNull(Tracks);
        ArgumentNullException.ThrowIfNull(Patterns);
        ArgumentNullException.ThrowIfNull(Clips);
        ArgumentNullException.ThrowIfNull(Assets);

        var tracks = Tracks.Select(track => track.ToDomain()).ToArray();
        var patterns = Patterns.Select(pattern => pattern.ToDomain()).ToArray();
        var clips = Clips.Select(clip => clip.ToDomain()).ToArray();
        var assets = Assets.Select(asset => asset.ToDomain()).ToArray();
        return new Project(Id, Name, Bpm, Swing, tracks, patterns, clips, assets);
    }

    /// <summary>Creates a versioned document from a domain project.</summary>
    public static ProjectDocument FromDomain(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new ProjectDocument
        {
            Id = project.Id,
            Name = project.Name,
            Bpm = project.Bpm,
            Swing = project.Swing,
            Tracks = project.Tracks.Select(TrackDocument.FromDomain).ToList(),
            Patterns = project.Patterns.Select(PatternDocument.FromDomain).ToList(),
            Clips = project.Clips.Select(ClipDocument.FromDomain).ToList(),
            Assets = project.Assets.Select(ProjectAssetDocument.FromDomain).ToList()
        };
    }

/// <summary>Serializable reference to an external project asset.</summary>
public sealed class ProjectAssetDocument
{
    /// <summary>Gets or sets the asset ID.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the source file path.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Creates a domain asset reference.</summary>
    public ProjectAsset ToDomain() => new(Id, Name, FilePath);

    internal static ProjectAssetDocument FromDomain(ProjectAsset asset) => new()
    {
        Id = asset.Id,
        Name = asset.Name,
        FilePath = asset.FilePath
    };
}
}

/// <summary>Serializable track value object.</summary>
public sealed class TrackDocument
{
    /// <summary>Gets or sets the track ID.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the track name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the track type.</summary>
    public TrackType Type { get; set; }
    /// <summary>Gets or sets the normalized volume.</summary>
    public float Volume { get; set; } = 1f;
    /// <summary>Gets or sets the pan.</summary>
    public float Pan { get; set; }
    /// <summary>Gets or sets mute state.</summary>
    public bool IsMuted { get; set; }
    /// <summary>Gets or sets solo state.</summary>
    public bool IsSolo { get; set; }
    /// <summary>Gets or sets send A.</summary>
    public float SendA { get; set; }
    /// <summary>Gets or sets send B.</summary>
    public float SendB { get; set; }

    /// <summary>Creates a domain track, validating all settings.</summary>
    public Track ToDomain() => new(Id, Name, Type, Volume, Pan, IsMuted, IsSolo, SendA, SendB);

    internal static TrackDocument FromDomain(Track track) => new()
    {
        Id = track.Id,
        Name = track.Name,
        Type = track.Type,
        Volume = track.Volume,
        Pan = track.Pan,
        IsMuted = track.IsMuted,
        IsSolo = track.IsSolo,
        SendA = track.SendA,
        SendB = track.SendB
    };
}

/// <summary>Serializable pattern value object.</summary>
public sealed class PatternDocument
{
    /// <summary>Gets or sets the pattern ID.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the pattern name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the step count.</summary>
    public int StepCount { get; set; }
    /// <summary>Gets or sets the step list.</summary>
    public List<StepDocument> Steps { get; set; } = [];
    /// <summary>Gets or sets piano-roll notes.</summary>
    public List<NoteDocument> Notes { get; set; } = [];

    /// <summary>Creates a domain pattern, validating its steps and notes.</summary>
    public Pattern ToDomain()
    {
        ArgumentNullException.ThrowIfNull(Steps);
        ArgumentNullException.ThrowIfNull(Notes);
        return new Pattern(Id, Name, StepCount, Steps.Select(step => step.ToDomain()).ToArray(), Notes.Select(note => note.ToDomain()).ToArray());
    }

    internal static PatternDocument FromDomain(Pattern pattern) => new()
    {
        Id = pattern.Id,
        Name = pattern.Name,
        StepCount = pattern.StepCount,
        Steps = pattern.Steps.Select(StepDocument.FromDomain).ToList(),
        Notes = pattern.Notes.Select(NoteDocument.FromDomain).ToList()
    };
}

/// <summary>Serializable sequencer step value object.</summary>
public sealed class StepDocument
{
    /// <summary>Gets or sets the index.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets whether the step is active.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Gets or sets velocity.</summary>
    public float Velocity { get; set; } = 1f;
    /// <summary>Gets or sets trigger probability.</summary>
    public float Probability { get; set; } = 1f;
    /// <summary>Gets or sets ratchet count.</summary>
    public int RatchetCount { get; set; } = 1;
    /// <summary>Gets or sets timing nudge in ticks.</summary>
    public int NudgeTicks { get; set; }
    /// <summary>Gets or sets pitch offset in semitones.</summary>
    public int PitchOffsetSemitones { get; set; }

    /// <summary>Creates a validated domain step.</summary>
    public Step ToDomain() => new(Index, IsEnabled, Velocity, Probability, RatchetCount, NudgeTicks, PitchOffsetSemitones);

    internal static StepDocument FromDomain(Step step) => new()
    {
        Index = step.Index,
        IsEnabled = step.IsEnabled,
        Velocity = step.Velocity,
        Probability = step.Probability,
        RatchetCount = step.RatchetCount,
        NudgeTicks = step.NudgeTicks,
        PitchOffsetSemitones = step.PitchOffsetSemitones
    };
}

/// <summary>Serializable piano-roll note value object.</summary>
public sealed class NoteDocument
{
    /// <summary>Gets or sets MIDI pitch.</summary>
    public int Pitch { get; set; }
    /// <summary>Gets or sets start tick.</summary>
    public long StartTick { get; set; }
    /// <summary>Gets or sets duration ticks.</summary>
    public long DurationTicks { get; set; }
    /// <summary>Gets or sets normalized velocity.</summary>
    public float Velocity { get; set; } = 1f;

    /// <summary>Creates a validated domain note.</summary>
    public Note ToDomain() => new(Pitch, StartTick, DurationTicks, Velocity);

    internal static NoteDocument FromDomain(Note note) => new()
    {
        Pitch = note.Pitch,
        StartTick = note.StartTick,
        DurationTicks = note.DurationTicks,
        Velocity = note.Velocity
    };
}

/// <summary>Serializable arrangement clip value object.</summary>
public sealed class ClipDocument
{
    /// <summary>Gets or sets the clip ID.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the destination track ID.</summary>
    public Guid TrackId { get; set; }
    /// <summary>Gets or sets the pattern ID.</summary>
    public Guid PatternId { get; set; }
    /// <summary>Gets or sets timeline start tick.</summary>
    public long StartTick { get; set; }
    /// <summary>Gets or sets clip length ticks.</summary>
    public long LengthTicks { get; set; }
    /// <summary>Gets or sets whether this clip loops its pattern.</summary>
    public bool IsLooped { get; set; }

    /// <summary>Creates a validated domain clip.</summary>
    public Clip ToDomain() => new(Id, TrackId, PatternId, StartTick, LengthTicks, IsLooped);

    internal static ClipDocument FromDomain(Clip clip) => new()
    {
        Id = clip.Id,
        TrackId = clip.TrackId,
        PatternId = clip.PatternId,
        StartTick = clip.StartTick,
        LengthTicks = clip.LengthTicks,
        IsLooped = clip.IsLooped
    };
}
