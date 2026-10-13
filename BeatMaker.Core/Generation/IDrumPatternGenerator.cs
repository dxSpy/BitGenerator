using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates section-aware drum patterns and fills.</summary>
public interface IDrumPatternGenerator
{
    /// <summary>Generates drum triggers appropriate for the section type and substyle.</summary>
    /// <param name="substyle">Production substyle.</param>
    /// <param name="sectionType">Structural role of the section.</param>
    /// <param name="barCount">Section length in bars.</param>
    /// <param name="swingPercent">Swing amount from zero through twenty-five percent.</param>
    /// <param name="random">Random source owned by the generation request.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <returns>Drum triggers local to the section.</returns>
    IReadOnlyList<DrumHit> Generate(
        BeatSubstyle substyle,
        SectionType sectionType,
        int barCount,
        float swingPercent,
        Random random,
        CancellationToken cancellationToken = default);
}
