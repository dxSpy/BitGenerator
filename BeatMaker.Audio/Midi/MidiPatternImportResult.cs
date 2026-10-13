using BeatMaker.Core.Models;

namespace BeatMaker.Audio.Midi;

/// <summary>Contains a MIDI-imported pattern and its optional tempo.</summary>
/// <param name="Pattern">Decoded notes and pattern length.</param>
/// <param name="TempoBpm">Imported tempo in beats per minute, if present.</param>
public sealed record MidiPatternImportResult(Pattern Pattern, int? TempoBpm);
