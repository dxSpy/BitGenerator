using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Models;

/// <summary>Contains the step and note events that form a reusable musical pattern.</summary>
public sealed record Pattern
{
    /// <summary>Creates a pattern with a supported step count and immutable event collections.</summary>
    /// <param name="id">Unique pattern identifier.</param>
    /// <param name="name">User-visible pattern name.</param>
    /// <param name="stepCount">Pattern length in sixteenth-note steps: 16, 32, or 64.</param>
    /// <param name="steps">Optional complete ordered step collection; omitted steps are initialized disabled.</param>
    /// <param name="notes">Optional piano-roll note collection.</param>
    public Pattern(
        Guid id,
        string name,
        int stepCount = MusicalConstants.MinimumPatternStepCount,
        IReadOnlyList<Step>? steps = null,
        IReadOnlyList<Note>? notes = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Pattern ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pattern name cannot be empty.", nameof(name));
        }

        if (!MusicalConstants.IsSupportedPatternStepCount(stepCount))
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount));
        }

        var stepArray = steps?.ToArray() ?? Enumerable.Range(0, stepCount)
            .Select(index => new Step(index, isEnabled: false))
            .ToArray();
        if (stepArray.Length != stepCount)
        {
            throw new ArgumentException("A pattern must contain exactly one step per position.", nameof(steps));
        }

        for (var index = 0; index < stepArray.Length; index++)
        {
            if (stepArray[index].Index != index)
            {
                throw new ArgumentException("Steps must be ordered and indexed from zero without gaps.", nameof(steps));
            }
        }

        var noteArray = notes?.ToArray() ?? [];
        var lengthTicks = (long)stepCount * MusicalConstants.TicksPerSixteenth;
        foreach (var note in noteArray)
        {
            if (note.StartTick > lengthTicks || note.DurationTicks > lengthTicks - note.StartTick)
            {
                throw new ArgumentException("Notes must fit within the pattern length.", nameof(notes));
            }
        }

        Id = id;
        Name = name.Trim();
        StepCount = stepCount;
        LengthTicks = lengthTicks;
        Steps = Array.AsReadOnly(stepArray);
        Notes = Array.AsReadOnly(noteArray);
    }

    /// <summary>Gets the unique pattern identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the pattern name.</summary>
    public string Name { get; }

    /// <summary>Gets the number of sixteenth-note steps.</summary>
    public int StepCount { get; }

    /// <summary>Gets the pattern duration in PPQ ticks for 4/4 time.</summary>
    public long LengthTicks { get; }

    /// <summary>Gets the ordered step events.</summary>
    public IReadOnlyList<Step> Steps { get; }

    /// <summary>Gets the piano-roll notes.</summary>
    public IReadOnlyList<Note> Notes { get; }
}
