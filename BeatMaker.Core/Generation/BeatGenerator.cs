using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Composes complete structured beats from deterministic domain generators.</summary>
public sealed class BeatGenerator : IBeatGenerator
{
    private readonly IChordProgressionGenerator _chordGenerator;
    private readonly IMelodyGenerator _melodyGenerator;
    private readonly IDrumPatternGenerator _drumGenerator;
    private readonly IBassLineGenerator _bassGenerator;

    /// <summary>Creates the beat orchestrator with its domain generators.</summary>
    public BeatGenerator(
        IChordProgressionGenerator chordGenerator,
        IMelodyGenerator melodyGenerator,
        IDrumPatternGenerator drumGenerator,
        IBassLineGenerator bassGenerator)
    {
        _chordGenerator = chordGenerator ?? throw new ArgumentNullException(nameof(chordGenerator));
        _melodyGenerator = melodyGenerator ?? throw new ArgumentNullException(nameof(melodyGenerator));
        _drumGenerator = drumGenerator ?? throw new ArgumentNullException(nameof(drumGenerator));
        _bassGenerator = bassGenerator ?? throw new ArgumentNullException(nameof(bassGenerator));
    }

    /// <inheritdoc />
    public Beat Generate(BeatGenerationOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        var rootSeed = options.Seed ?? Random.Shared.Next();
        var selectionRandom = new Random(DeriveSeed(rootSeed, 0));
        var tempo = GenerationProfiles.GetBpm(options, selectionRandom);
        var keyRoot = options.KeyRoot ?? (KeyRoot)selectionRandom.Next(12);
        var scaleMode = options.ScaleMode ?? GenerationProfiles.GetDefaultScaleMode(options.Substyle, options.Mood);
        var structure = GetStructure(options.Length, options.Substyle);
        var sections = new List<Section>(structure.Count);
        var startBar = 0;
        var seedSequence = 0;

        for (var sectionIndex = 0; sectionIndex < structure.Count; sectionIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (type, barCount) = structure[sectionIndex];
            var harmonyRandom = new Random(DeriveSeed(rootSeed, seedSequence++));
            var melodyRandom = new Random(DeriveSeed(rootSeed, seedSequence++));
            var drumRandom = new Random(DeriveSeed(rootSeed, seedSequence++));
            var bassRandom = new Random(DeriveSeed(rootSeed, seedSequence++));
            var chords = _chordGenerator.Generate(keyRoot, scaleMode, barCount, harmonyRandom, cancellationToken);
            var melody = _melodyGenerator.Generate(keyRoot, scaleMode, chords, barCount, options.Substyle, melodyRandom, cancellationToken);
            var swing = GenerationProfiles.GetStyleProfile(options.Substyle).SwingPercent;
            var drums = _drumGenerator.Generate(options.Substyle, type, barCount, swing, drumRandom, cancellationToken);
            var bass = _bassGenerator.Generate(keyRoot, scaleMode, chords, barCount, options.Substyle, bassRandom, cancellationToken);

            if (type is SectionType.Intro or SectionType.Outro)
            {
                drums = drums
                    .Where(hit => hit.Instrument is not (DrumInstrument.Kick or DrumInstrument.Snare or DrumInstrument.Clap)
                        || hit.Velocity < 0.7f)
                    .Select(hit => hit.Instrument is DrumInstrument.Kick or DrumInstrument.Snare or DrumInstrument.Clap
                        ? new DrumHit(hit.Instrument, hit.StartTick, hit.Velocity * 0.65f, hit.Pan)
                        : hit)
                    .ToArray();
            }

            if (type == SectionType.Bridge && options.Substyle is BeatSubstyle.DarkTrap or BeatSubstyle.UndergroundAbstract)
            {
                drums = [];
            }

            sections.Add(new Section(type, startBar, barCount, chords, melody, drums, bass));
            startBar = checked(startBar + barCount);
        }

        var beatId = new Guid(
            DeriveSeed(rootSeed, 100),
            unchecked((short)DeriveSeed(rootSeed, 101)),
            unchecked((short)DeriveSeed(rootSeed, 102)),
            [1, 2, 3, 4, 5, 6, 7, 8]);
        return new Beat(beatId, BuildBeatName(options), options.Substyle, options.Mood, tempo, sections, keyRoot, scaleMode);
    }

    private static IReadOnlyList<(SectionType Type, int Bars)> GetStructure(BeatLengthPreset length, BeatSubstyle style)
    {
        var verseBars = length switch
        {
            BeatLengthPreset.Short => 8,
            BeatLengthPreset.Full => 16,
            BeatLengthPreset.Extended => GenerationProfiles.GetExtendedVerseLength(style),
            _ => throw new ArgumentOutOfRangeException(nameof(length))
        };
        var introBars = length == BeatLengthPreset.Short ? 4 : 8;
        var outroBars = length == BeatLengthPreset.Short ? 4 : 8;
        var bridgeBars = length == BeatLengthPreset.Short ? 4 : 8;
        return
        [
            (SectionType.Intro, introBars),
            (SectionType.Verse, verseBars),
            (SectionType.Hook, 8),
            (SectionType.Verse, verseBars),
            (SectionType.Hook, 8),
            (SectionType.Bridge, bridgeBars),
            (SectionType.Hook, 8),
            (SectionType.Outro, outroBars)
        ];
    }

    private static int DeriveSeed(int seed, int stream) => unchecked(seed * 486187739 + stream * 16777619);

    private static string BuildBeatName(BeatGenerationOptions options) =>
        $"{options.Substyle} {options.Mood} Beat";
}
