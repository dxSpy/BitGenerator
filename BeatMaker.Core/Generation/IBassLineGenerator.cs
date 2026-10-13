using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates bass notes that follow the harmonic roots and selected style.</summary>
public interface IBassLineGenerator
{
    /// <summary>Generates a bass line whose notes follow the supplied progression.</summary>
    /// <param name="keyRoot">Root pitch class of the key.</param>
    /// <param name="scaleMode">Scale mode.</param>
    /// <param name="chords">Chord progression local to the section.</param>
    /// <param name="barCount">Section length in bars.</param>
    /// <param name="substyle">Production substyle guiding bass behavior.</param>
    /// <param name="random">Random source owned by the generation request.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <returns>Bass notes local to the section.</returns>
    IReadOnlyList<BassNote> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        IReadOnlyList<Chord> chords,
        int barCount,
        BeatSubstyle substyle,
        Random random,
        CancellationToken cancellationToken = default);
}
