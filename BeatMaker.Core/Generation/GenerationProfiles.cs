using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Shared genre ranges and pitch-class helpers for beat generators.</summary>
internal static class GenerationProfiles
{
    private static readonly int[] MajorIntervals = [0, 2, 4, 5, 7, 9, 11];
    private static readonly int[] NaturalMinorIntervals = [0, 2, 3, 5, 7, 8, 10];
    private static readonly int[] HarmonicMinorIntervals = [0, 2, 3, 5, 7, 8, 11];
    private static readonly int[] DorianIntervals = [0, 2, 3, 5, 7, 9, 10];
    private static readonly int[] PhrygianIntervals = [0, 1, 3, 5, 7, 8, 10];
    private static readonly int[] MixolydianIntervals = [0, 2, 4, 5, 7, 9, 10];

    public static (int MinimumBpm, int MaximumBpm, float SwingPercent) GetStyleProfile(BeatSubstyle style) => style switch
    {
        BeatSubstyle.OldSchoolRussianRap => (85, 95, 14f),
        BeatSubstyle.RussianTrap => (130, 150, 0f),
        BeatSubstyle.CloudRapPlugg => (140, 160, 3f),
        BeatSubstyle.DarkTrap => (120, 140, 0f),
        BeatSubstyle.HyperpopRage => (150, 180, 0f),
        BeatSubstyle.UndergroundAbstract => (80, 100, 10f),
        BeatSubstyle.MelodicTrap => (140, 150, 0f),
        _ => throw new ArgumentOutOfRangeException(nameof(style))
    };

    public static ReadOnlySpan<int> GetScaleIntervals(ScaleMode mode) => mode switch
    {
        ScaleMode.Major => MajorIntervals,
        ScaleMode.NaturalMinor => NaturalMinorIntervals,
        ScaleMode.HarmonicMinor => HarmonicMinorIntervals,
        ScaleMode.Dorian => DorianIntervals,
        ScaleMode.Phrygian => PhrygianIntervals,
        ScaleMode.Mixolydian => MixolydianIntervals,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static int GetPitchClass(KeyRoot keyRoot) => keyRoot switch
    {
        KeyRoot.C => 0,
        KeyRoot.CSharp => 1,
        KeyRoot.D => 2,
        KeyRoot.DSharp => 3,
        KeyRoot.E => 4,
        KeyRoot.F => 5,
        KeyRoot.FSharp => 6,
        KeyRoot.G => 7,
        KeyRoot.GSharp => 8,
        KeyRoot.A => 9,
        KeyRoot.ASharp => 10,
        KeyRoot.B => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(keyRoot))
    };

    public static int GetPitchClassForDegree(int keyPitchClass, ScaleMode mode, int degree, int octave = 0)
    {
        var intervals = GetScaleIntervals(mode);
        var normalizedDegree = ((degree % intervals.Length) + intervals.Length) % intervals.Length;
        var degreeOctave = (int)Math.Floor(degree / (double)intervals.Length);
        return keyPitchClass + intervals[normalizedDegree] + (degreeOctave + octave) * 12;
    }

    public static ScaleMode GetDefaultScaleMode(BeatSubstyle style, BeatMood mood) => (style, mood) switch
    {
        (BeatSubstyle.OldSchoolRussianRap, BeatMood.Bright) => ScaleMode.Major,
        (BeatSubstyle.CloudRapPlugg, BeatMood.Bright) => ScaleMode.Dorian,
        (BeatSubstyle.MelodicTrap, BeatMood.Bright) => ScaleMode.NaturalMinor,
        (_, BeatMood.Dark or BeatMood.Aggressive) => ScaleMode.Phrygian,
        (BeatSubstyle.UndergroundAbstract, _) => ScaleMode.Dorian,
        _ => ScaleMode.NaturalMinor
    };

    public static int GetBpm(BeatGenerationOptions options, Random random)
    {
        var profile = GetStyleProfile(options.Substyle);
        if (options.TempoBpm is { } requested)
        {
            return requested;
        }

        return random.Next(profile.MinimumBpm, profile.MaximumBpm + 1);
    }

    public static int GetShortBarCount(BeatSubstyle style) => style switch
    {
        BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract => 36,
        _ => 40
    };

    public static int GetExtendedVerseLength(BeatSubstyle style) => style switch
    {
        BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract => 24,
        _ => 20
    };
}
