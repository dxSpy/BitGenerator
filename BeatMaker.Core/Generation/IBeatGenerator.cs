using BeatMaker.Core.Models;

namespace BeatMaker.Core.Generation;

/// <summary>Composes a complete structured beat from validated generation options.</summary>
public interface IBeatGenerator
{
    /// <summary>Generates a complete beat; a fixed options seed must produce identical output.</summary>
    /// <param name="options">Validated generation parameters.</param>
    /// <param name="cancellationToken">Token used to stop generation outside the real-time audio thread.</param>
    /// <returns>A complete beat.</returns>
    Beat Generate(BeatGenerationOptions options, CancellationToken cancellationToken = default);
}
