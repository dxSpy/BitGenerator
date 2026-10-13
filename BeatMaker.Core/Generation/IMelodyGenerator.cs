using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates melody events that follow a chord progression and key.</summary>
public interface IMelodyGenerator
{
    /// <summary>Generates melody notes following the supplied harmony.</summary>
    /// <param name="keyRoot">Root pitch class of the key.</param>
    /// <param name="scaleMode">Scale mode.</param>
    /// <param name="chords">Chord progression local to the section.</param>
    /// <param name="barCount">Section length in bars.</param>
    /// <param name="substyle">Production substyle guiding melodic density and register.</param>
    /// <param name="random">Random source owned by the generation request.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <returns>Melody notes local to the section.</returns>
    IReadOnlyList<Note> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        IReadOnlyList<Chord> chords,
        int barCount,
        BeatSubstyle substyle,
        Random random,
        CancellationToken cancellationToken = default);
}
