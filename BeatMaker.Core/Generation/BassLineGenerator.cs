using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates deterministic chord-root bass lines with optional 808 glides.</summary>
public sealed class BassLineGenerator : IBassLineGenerator
{
    private const int BassOctave = 2;
    private const float BaseVelocity = 0.78f;

    /// <inheritdoc />
    public IReadOnlyList<BassNote> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        IReadOnlyList<Chord> chords,
        int barCount,
        BeatSubstyle substyle,
        Random random,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chords);
        ArgumentNullException.ThrowIfNull(random);
        if (!Enum.IsDefined(keyRoot)) throw new ArgumentOutOfRangeException(nameof(keyRoot));
        if (!Enum.IsDefined(scaleMode)) throw new ArgumentOutOfRangeException(nameof(scaleMode));
        if (!Enum.IsDefined(substyle)) throw new ArgumentOutOfRangeException(nameof(substyle));
        if (barCount <= 0) throw new ArgumentOutOfRangeException(nameof(barCount));

        var sectionLength = checked((long)barCount * Section.TicksPerBar);
        var notes = new List<BassNote>(barCount * 3);
        var barTicks = Section.TicksPerBar;
        var noteDuration = substyle is BeatSubstyle.OldSchoolRussianRap or BeatSubstyle.UndergroundAbstract
            ? barTicks * 3L / 4
            : barTicks / 2L;
        var glideEnabled = substyle is BeatSubstyle.RussianTrap or BeatSubstyle.DarkTrap or BeatSubstyle.HyperpopRage or BeatSubstyle.MelodicTrap;

        for (var bar = 0; bar < barCount; bar++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var barStart = (long)bar * barTicks;
            var chord = FindChord(chords, barStart);
            var pitch = chord is null
                ? GenerationProfiles.GetPitchClass(keyRoot) + BassOctave * 12
                : (chord.RootPitch % 12) + BassOctave * 12;
            var duration = Math.Min(noteDuration, sectionLength - barStart);
            int? glideTarget = null;
            long glideDuration = 0;

            if (glideEnabled && bar < barCount - 1 && random.NextDouble() < 0.24)
            {
                var nextChord = FindChord(chords, barStart + barTicks);
                if (nextChord is not null)
                {
                    var nextPitch = (nextChord.RootPitch % 12) + BassOctave * 12;
                    if (nextPitch != pitch && Math.Abs(nextPitch - pitch) <= 12)
                    {
                        glideTarget = nextPitch;
                        glideDuration = Math.Min(barTicks / 4L, duration);
                    }
                }
            }

            var velocity = BaseVelocity + (float)(random.NextDouble() - 0.5) * 0.12f;
            notes.Add(new BassNote(pitch, barStart, duration, velocity, glideTarget, glideDuration));

            if (substyle is BeatSubstyle.RussianTrap or BeatSubstyle.HyperpopRage && bar % 2 == 1 && random.NextDouble() < 0.45)
            {
                var offbeatStart = barStart + barTicks * 5L / 8;
                if (offbeatStart < sectionLength)
                {
                    var offbeatDuration = Math.Min(barTicks / 8L, sectionLength - offbeatStart);
                    notes.Add(new BassNote(pitch, offbeatStart, offbeatDuration, velocity * 0.72f));
                }
            }
        }

        return Array.AsReadOnly(notes.OrderBy(note => note.StartTick).ToArray());
    }

    private static Chord? FindChord(IReadOnlyList<Chord> chords, long tick)
    {
        for (var index = chords.Count - 1; index >= 0; index--)
        {
            if (chords[index].StartTick <= tick)
            {
                return chords[index];
            }
        }

        return null;
    }
}
