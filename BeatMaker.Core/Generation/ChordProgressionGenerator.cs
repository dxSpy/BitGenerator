using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates deterministic diatonic chord progressions.</summary>
public sealed class ChordProgressionGenerator : IChordProgressionGenerator
{
    private static readonly int[][] MajorTriadIntervals = [[0, 4, 7], [0, 3, 7], [0, 3, 7], [0, 4, 7], [0, 4, 7], [0, 3, 7], [0, 3, 6]];
    private static readonly int[][] MinorTriadIntervals = [[0, 3, 7], [0, 3, 6], [0, 4, 7], [0, 3, 7], [0, 3, 7], [0, 4, 7], [0, 4, 7]];
    private static readonly int[][] SeventhIntervals = [[0, 4, 7, 11], [0, 3, 7, 10], [0, 3, 7, 10], [0, 4, 7, 11], [0, 3, 7, 10], [0, 3, 7, 10], [0, 3, 6, 10]];
    private static readonly int[] MinorProgression = [0, 5, 2, 6];
    private static readonly int[] MajorProgression = [0, 5, 3, 4];

    /// <inheritdoc />
    public IReadOnlyList<Chord> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        int barCount,
        Random random,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!Enum.IsDefined(keyRoot)) throw new ArgumentOutOfRangeException(nameof(keyRoot));
        if (!Enum.IsDefined(scaleMode)) throw new ArgumentOutOfRangeException(nameof(scaleMode));
        if (barCount <= 0) throw new ArgumentOutOfRangeException(nameof(barCount));

        var keyPitchClass = GenerationProfiles.GetPitchClass(keyRoot);
        var progression = scaleMode == ScaleMode.Major ? MajorProgression : MinorProgression;
        var chordDuration = checked((long)Math.Min(4, barCount) * Section.TicksPerBar);
        var chords = new List<Chord>((barCount + 3) / 4);

        for (var startBar = 0; startBar < barCount; startBar += 4)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var degree = progression[(startBar / 4) % progression.Length];
            if (startBar > 0 && random.NextDouble() < 0.18)
            {
                degree = (degree + (random.Next(2) == 0 ? 1 : 5)) % 7;
            }

            var rootPitch = GenerationProfiles.GetPitchClassForDegree(keyPitchClass, scaleMode, degree, octave: 3);
            var intervals = GetTriadIntervals(scaleMode, degree);
            var quality = GetChordQuality(intervals);
            if (random.NextDouble() < 0.22)
            {
                quality = scaleMode == ScaleMode.Major ? ChordQuality.MajorSeventh : ChordQuality.MinorSeventh;
            }

            chords.Add(new Chord(rootPitch, quality, (long)startBar * Section.TicksPerBar, Math.Min(chordDuration, (long)(barCount - startBar) * Section.TicksPerBar)));
        }

        return Array.AsReadOnly(chords.ToArray());
    }

    private static int[] GetTriadIntervals(ScaleMode mode, int degree)
    {
        if (mode == ScaleMode.Major)
        {
            return MajorTriadIntervals[degree];
        }

        if (mode is ScaleMode.NaturalMinor or ScaleMode.HarmonicMinor)
        {
            return MinorTriadIntervals[degree];
        }

        var scale = GenerationProfiles.GetScaleIntervals(mode);
        var intervals = new int[3];
        for (var tone = 0; tone < intervals.Length; tone++)
        {
            var thirdDegree = degree + tone * 2;
            intervals[tone] = GenerationProfiles.GetPitchClassForDegree(0, mode, thirdDegree) -
                GenerationProfiles.GetPitchClassForDegree(0, mode, degree);
        }

        return intervals;
    }

    private static ChordQuality GetChordQuality(IReadOnlyList<int> intervals)
    {
        if (intervals[1] == 3 && intervals[2] == 6) return ChordQuality.Diminished;
        if (intervals[1] == 4 && intervals[2] == 8) return ChordQuality.Augmented;
        return intervals[1] == 4 ? ChordQuality.Major : ChordQuality.Minor;
    }
}
