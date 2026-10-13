using BeatMaker.Audio.Samples;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;
using System.Threading;

namespace BeatMaker.Audio.Scheduling;

/// <summary>Schedules enabled step events from a repeating pattern against transport ticks.</summary>
public sealed class StepPatternScheduler
{
    private PatternSnapshot? _snapshot;
    private PatternSnapshot? _activeSnapshot;
    private long _lastTick = -1;

    /// <summary>Replaces the pattern and its sample assignments.</summary>
    /// <param name="pattern">Pattern to schedule, or null to disable scheduling.</param>
    /// <param name="stepSamples">Sample assignment by step index.</param>
    public void SetPattern(Pattern? pattern, IReadOnlyList<AudioSample?>? stepSamples)
    {
        if (pattern is null)
        {
            if (stepSamples is { Count: > 0 })
            {
                throw new ArgumentException("Sample assignments require a pattern.", nameof(stepSamples));
            }

            Volatile.Write(ref _snapshot, null);
            return;
        }

        if (stepSamples is not null && stepSamples.Count != pattern.StepCount)
        {
            throw new ArgumentException("There must be exactly one sample assignment per pattern step.", nameof(stepSamples));
        }

        var assignments = new AudioSample?[pattern.StepCount];
        if (stepSamples is not null)
        {
            for (var index = 0; index < assignments.Length; index++)
            {
                assignments[index] = stepSamples[index];
            }
        }

        Volatile.Write(ref _snapshot, new PatternSnapshot(pattern, assignments));
    }

    /// <summary>Resets tick traversal state so the next tick establishes a new baseline.</summary>
    public void Reset()
    {
        _activeSnapshot = null;
        _lastTick = -1;
    }

    /// <summary>Schedules events crossed between the previous and current transport ticks.</summary>
    /// <param name="currentTick">Current absolute transport tick.</param>
    /// <param name="trigger">Callback that attempts to start a sample voice.</param>
    /// <returns>Number of attempted sample triggers.</returns>
    public int Process(long currentTick, SampleTrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        if (currentTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentTick));
        }

        var snapshot = Volatile.Read(ref _snapshot);
        if (!ReferenceEquals(snapshot, _activeSnapshot))
        {
            _activeSnapshot = snapshot;
            _lastTick = -1;
        }

        if (currentTick < _lastTick)
        {
            _lastTick = currentTick;
            return 0;
        }

        if (snapshot is null || currentTick == _lastTick)
        {
            _lastTick = currentTick;
            return 0;
        }

        var pattern = snapshot.Pattern;
        var firstStep = Math.Max(0, _lastTick / MusicalConstants.TicksPerSixteenth);
        var lastStep = currentTick / MusicalConstants.TicksPerSixteenth;
        var triggerCount = 0;

        for (var absoluteStep = firstStep; absoluteStep <= lastStep; absoluteStep++)
        {
            var stepIndex = (int)(absoluteStep % pattern.StepCount);
            var step = pattern.Steps[stepIndex];
            var sample = snapshot.StepSamples[stepIndex];
            if (!step.IsEnabled || sample is null || !ShouldTriggerProbability(step.Probability, absoluteStep, stepIndex))
            {
                continue;
            }

            var ratchetCount = step.RatchetCount;
            var volumePerTrigger = step.Velocity / MathF.Sqrt(ratchetCount);
            for (var ratchet = 0; ratchet < ratchetCount; ratchet++)
            {
                var eventTick = checked((absoluteStep * MusicalConstants.TicksPerSixteenth) +
                    ((long)ratchet * MusicalConstants.TicksPerSixteenth / ratchetCount));
                if (eventTick <= _lastTick || eventTick > currentTick)
                {
                    continue;
                }

                trigger(sample, volumePerTrigger, 0f);
                triggerCount++;
            }
        }

        _lastTick = currentTick;
        return triggerCount;
    }

    internal static bool ShouldTriggerProbability(float probability, long absoluteStep, int stepIndex)
    {
        if (probability >= 1f)
        {
            return true;
        }

        if (probability <= 0f)
        {
            return false;
        }

        var value = unchecked((ulong)absoluteStep * 0x9E3779B97F4A7C15UL + (uint)stepIndex * 0xBF58476D1CE4E5B9UL);
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        value ^= value >> 31;
        var normalized = (value >> 11) * (1d / (1UL << 53));
        return normalized < probability;
    }

    private sealed record PatternSnapshot(Pattern Pattern, AudioSample?[] StepSamples);
}

/// <summary>Attempts to start a scheduled sample.</summary>
/// <param name="sample">Sample assigned to the step.</param>
/// <param name="volume">Step velocity adjusted for ratchet count.</param>
/// <param name="pan">Pan value for the step.</param>
public delegate void SampleTrigger(AudioSample sample, float volume, float pan);
