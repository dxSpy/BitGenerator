using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates section-aware drum patterns with swing and hat rolls.</summary>
public sealed class DrumPatternGenerator : IDrumPatternGenerator
{
    private const int TicksPerQuarter = Section.TicksPerBar / 4;
    private const int TicksPerEighth = TicksPerQuarter / 2;
    private const int TicksPerSixteenth = TicksPerQuarter / 4;

    /// <inheritdoc />
    public IReadOnlyList<DrumHit> Generate(
        BeatSubstyle substyle,
        SectionType sectionType,
        int barCount,
        float swingPercent,
        Random random,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!Enum.IsDefined(substyle)) throw new ArgumentOutOfRangeException(nameof(substyle));
        if (!Enum.IsDefined(sectionType)) throw new ArgumentOutOfRangeException(nameof(sectionType));
        if (barCount <= 0) throw new ArgumentOutOfRangeException(nameof(barCount));
        if (!float.IsFinite(swingPercent) || swingPercent is < 0f or > 25f)
        {
            throw new ArgumentOutOfRangeException(nameof(swingPercent));
        }

        var hits = new List<DrumHit>(barCount * 24);
        var styleProfile = GenerationProfiles.GetStyleProfile(substyle);
        var swing = swingPercent == 0 ? styleProfile.SwingPercent : swingPercent;
        for (var bar = 0; bar < barCount; bar++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var barStart = (long)bar * Section.TicksPerBar;
            var isLastBar = bar == barCount - 1;
            var sparse = sectionType is SectionType.Intro or SectionType.Bridge or SectionType.Outro;
            var hook = sectionType == SectionType.Hook;
            var fillBar = bar % 4 == 3 || isLastBar && sectionType is SectionType.Verse or SectionType.Hook;

            if (sectionType != SectionType.Intro || bar >= barCount - 2)
            {
                AddKickPattern(hits, barStart, substyle, sparse, hook, random);
            }

            if (sectionType is SectionType.Verse or SectionType.Hook || sectionType == SectionType.Intro && bar == barCount - 1)
            {
                AddBackbeat(hits, barStart, substyle, hook);
            }

            if (sectionType != SectionType.Outro || bar == 0)
            {
                AddHiHats(hits, barStart, substyle, sparse, swing, fillBar, random);
            }

            if (isLastBar && sectionType is SectionType.Hook or SectionType.Bridge or SectionType.Outro)
            {
                hits.Add(new DrumHit(DrumInstrument.Crash, barStart, hook ? 0.95f : 0.62f));
            }

            if (isLastBar && sectionType is SectionType.Verse or SectionType.Bridge)
            {
                hits.Add(new DrumHit(DrumInstrument.Snare, barStart + 3L * Section.TicksPerBar / 4, 0.82f));
            }
        }

        return Array.AsReadOnly(hits.OrderBy(hit => hit.StartTick).ToArray());
    }

    private static void AddKickPattern(List<DrumHit> hits, long barStart, BeatSubstyle style, bool sparse, bool hook, Random random)
    {
        var steps = style switch
        {
            BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract => new[] { 0, 8 },
            BeatSubstyle.DarkTrap => new[] { 0, 10 },
            BeatSubstyle.CloudRapPlugg => new[] { 0, 10, 14 },
            BeatSubstyle.HyperpopRage => new[] { 0, 4, 8, 12 },
            _ => new[] { 0, 6, 10, 14 }
        };

        foreach (var step in steps)
        {
            if (sparse && step != 0 || random.NextDouble() < (hook ? 0.03 : 0.12))
            {
                continue;
            }

            var tick = barStart + step * TicksPerSixteenth;
            var velocity = hook ? 0.94f : step == 0 ? 0.88f : 0.74f;
            hits.Add(new DrumHit(DrumInstrument.Kick, tick, velocity));
        }

        if (!sparse && style is BeatSubstyle.RussianTrap or BeatSubstyle.MelodicTrap && random.NextDouble() < 0.35)
        {
            hits.Add(new DrumHit(DrumInstrument.Kick, barStart + 13L * TicksPerSixteenth, 0.66f));
        }
    }

    private static void AddBackbeat(List<DrumHit> hits, long barStart, BeatSubstyle style, bool hook)
    {
        var halfTime = style is BeatSubstyle.RussianTrap or BeatSubstyle.CloudRapPlugg or BeatSubstyle.DarkTrap or BeatSubstyle.HyperpopRage or BeatSubstyle.MelodicTrap;
        var beats = halfTime ? new[] { 2 } : new[] { 1, 3 };
        foreach (var beat in beats)
        {
            var tick = barStart + beat * TicksPerQuarter;
            hits.Add(new DrumHit(DrumInstrument.Snare, tick, hook ? 0.96f : 0.82f));
            if (style is BeatSubstyle.CloudRapPlugg or BeatSubstyle.MelodicTrap or BeatSubstyle.RussianTrap)
            {
                hits.Add(new DrumHit(DrumInstrument.Clap, tick, hook ? 0.82f : 0.62f, 0.04f));
            }
        }
    }

    private static void AddHiHats(List<DrumHit> hits, long barStart, BeatSubstyle style, bool sparse, float swingPercent, bool fillBar, Random random)
    {
        var sixteenthDensity = style is BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract ? 2 : 4;
        if (style == BeatSubstyle.DarkTrap) sixteenthDensity = 1;
        if (sparse) sixteenthDensity = Math.Max(1, sixteenthDensity / 2);

        for (var step = 0; step < 16; step += 4 / sixteenthDensity)
        {
            if (random.NextDouble() < (sparse ? 0.38 : 0.12)) continue;
            var swingOffset = step % 2 == 1 ? (int)(TicksPerSixteenth * swingPercent / 100f) : 0;
            var tick = barStart + step * TicksPerSixteenth + swingOffset;
            var velocity = step % 4 == 0 ? 0.68f : 0.42f + (float)random.NextDouble() * 0.18f;
            hits.Add(new DrumHit(DrumInstrument.ClosedHiHat, tick, velocity, step % 2 == 0 ? -0.16f : 0.16f));
        }

        if (fillBar && style is BeatSubstyle.RussianTrap or BeatSubstyle.HyperpopRage or BeatSubstyle.MelodicTrap)
        {
            var rollStart = barStart + 12L * TicksPerSixteenth;
            var rollStep = TicksPerSixteenth / 3;
            for (var roll = 0; roll < 12; roll++)
            {
                hits.Add(new DrumHit(DrumInstrument.ClosedHiHat, rollStart + (long)roll * rollStep, 0.3f + roll * 0.025f, roll % 2 == 0 ? -0.2f : 0.2f));
            }
        }

        if (!sparse && random.NextDouble() < 0.22)
        {
            var openStep = random.Next(2, 4) * 4;
            hits.Add(new DrumHit(DrumInstrument.OpenHiHat, barStart + openStep * TicksPerSixteenth, 0.58f, 0.12f));
        }
    }
}
