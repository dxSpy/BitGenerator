using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Generates harmonically coherent chord progressions.</summary>
public interface IChordProgressionGenerator
{
    /// <summary>Generates chords in one key and mode using the supplied deterministic random source.</summary>
    /// <param name="keyRoot">Root pitch class of the key.</param>
    /// <param name="scaleMode">Scale mode.</param>
    /// <param name="barCount">Length of the progression in bars.</param>
    /// <param name="random">Random source owned by the generation request.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <returns>Harmony events local to the progression start.</returns>
    IReadOnlyList<Chord> Generate(
        KeyRoot keyRoot,
        ScaleMode scaleMode,
        int barCount,
        Random random,
        CancellationToken cancellationToken = default);
}
