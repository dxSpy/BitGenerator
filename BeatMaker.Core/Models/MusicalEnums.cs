namespace BeatMaker.Core.Models;

/// <summary>Describes a structural section in a generated beat.</summary>
public enum SectionType
{
    /// <summary>Opening section with reduced instrumentation.</summary>
    Intro,
    /// <summary>Primary verse section.</summary>
    Verse,
    /// <summary>High-energy refrain section.</summary>
    Hook,
    /// <summary>Contrasting atmospheric section.</summary>
    Bridge,
    /// <summary>Closing section.</summary>
    Outro
}

/// <summary>Describes a broad rap-production substyle rather than an individual artist.</summary>
public enum BeatSubstyle
{
    /// <summary>Sample-oriented boom-bap and classic hip-hop production.</summary>
    OldSchoolRussianRap,
    /// <summary>Modern trap with syncopated drums and 808 bass.</summary>
    RussianTrap,
    /// <summary>Airy, soft-edged cloud rap and plugg production.</summary>
    CloudRapPlugg,
    /// <summary>Minimal, heavy and dark trap production.</summary>
    DarkTrap,
    /// <summary>High-energy electronic rap production.</summary>
    HyperpopRage,
    /// <summary>Sample-driven abstract and underground hip-hop production.</summary>
    UndergroundAbstract,
    /// <summary>Melodic trap with emotional harmony and lead parts.</summary>
    MelodicTrap
}

/// <summary>Describes the intended emotional character of a beat.</summary>
public enum BeatMood
{
    /// <summary>Dark or tense character.</summary>
    Dark,
    /// <summary>Bright or hopeful character.</summary>
    Bright,
    /// <summary>Forceful and energetic character.</summary>
    Aggressive,
    /// <summary>Reflective and emotionally subdued character.</summary>
    Melancholic,
    /// <summary>Spacious and weightless character.</summary>
    Airy
}

/// <summary>Describes a chord quality used by the harmony model.</summary>
public enum ChordQuality
{
    /// <summary>Major triad.</summary>
    Major,
    /// <summary>Minor triad.</summary>
    Minor,
    /// <summary>Diminished triad.</summary>
    Diminished,
    /// <summary>Augmented triad.</summary>
    Augmented,
    /// <summary>Dominant seventh chord.</summary>
    DominantSeventh,
    /// <summary>Major seventh chord.</summary>
    MajorSeventh,
    /// <summary>Minor seventh chord.</summary>
    MinorSeventh,
    /// <summary>Suspended second chord.</summary>
    SuspendedSecond,
    /// <summary>Suspended fourth chord.</summary>
    SuspendedFourth
}

/// <summary>Describes a drum or percussion sound role.</summary>
public enum DrumInstrument
{
    /// <summary>Kick drum.</summary>
    Kick,
    /// <summary>Snare drum.</summary>
    Snare,
    /// <summary>Clap.</summary>
    Clap,
    /// <summary>Closed hi-hat.</summary>
    ClosedHiHat,
    /// <summary>Open hi-hat.</summary>
    OpenHiHat,
    /// <summary>Crash cymbal.</summary>
    Crash,
    /// <summary>Rim or side-stick.</summary>
    Rim,
    /// <summary>Other percussion.</summary>
    Percussion
}

/// <summary>Represents the chromatic root note of a musical key.</summary>
public enum KeyRoot
{
    /// <summary>C.</summary>
    C,
    /// <summary>C sharp or D flat.</summary>
    CSharp,
    /// <summary>D.</summary>
    D,
    /// <summary>D sharp or E flat.</summary>
    DSharp,
    /// <summary>E.</summary>
    E,
    /// <summary>F.</summary>
    F,
    /// <summary>F sharp or G flat.</summary>
    FSharp,
    /// <summary>G.</summary>
    G,
    /// <summary>G sharp or A flat.</summary>
    GSharp,
    /// <summary>A.</summary>
    A,
    /// <summary>A sharp or B flat.</summary>
    ASharp,
    /// <summary>B.</summary>
    B
}

/// <summary>Represents a scale mode used for harmonic and melodic generation.</summary>
public enum ScaleMode
{
    /// <summary>Major scale.</summary>
    Major,
    /// <summary>Natural minor scale.</summary>
    NaturalMinor,
    /// <summary>Harmonic minor scale.</summary>
    HarmonicMinor,
    /// <summary>Dorian mode.</summary>
    Dorian,
    /// <summary>Phrygian mode.</summary>
    Phrygian,
    /// <summary>Mixolydian mode.</summary>
    Mixolydian
}

/// <summary>Represents a target arrangement length preset.</summary>
public enum BeatLengthPreset
{
    /// <summary>Short arrangement for sketches and previews.</summary>
    Short,
    /// <summary>Standard song-length arrangement.</summary>
    Full,
    /// <summary>Extended arrangement with longer sections.</summary>
    Extended
}
