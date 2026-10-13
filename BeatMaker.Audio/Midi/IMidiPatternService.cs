using BeatMaker.Core.Models;

namespace BeatMaker.Audio.Midi;

/// <summary>Imports and exports musical patterns using Standard MIDI Files.</summary>
public interface IMidiPatternService
{
    /// <summary>Imports the first MIDI track as a PPQ-normalized pattern.</summary>
    /// <param name="filePath">MIDI file path.</param>
    /// <param name="patternName">Name assigned to the imported pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The imported pattern and optional tempo.</returns>
    Task<MidiPatternImportResult> ImportAsync(
        string filePath,
        string patternName,
        CancellationToken cancellationToken = default);

    /// <summary>Exports a pattern to a Standard MIDI File asynchronously.</summary>
    /// <param name="pattern">Pattern to export.</param>
    /// <param name="filePath">Destination MIDI file path.</param>
    /// <param name="tempoBpm">Tempo metadata in beats per minute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportAsync(
        Pattern pattern,
        string filePath,
        int tempoBpm,
        CancellationToken cancellationToken = default);
}
