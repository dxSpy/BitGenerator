using System.Threading;
using BeatMaker.Audio.Samples;
using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;
using BeatMaker.Core.Models;

namespace BeatMaker.Audio.Scheduling;

/// <summary>Schedules sample-backed pattern clips on the project timeline.</summary>
public sealed class ArrangementScheduler
{
    private ArrangementSnapshot? _snapshot;
    private ArrangementSnapshot? _activeSnapshot;
    private long _lastTick = -1;

    /// <summary>Publishes a new project arrangement and track sample assignments.</summary>
    /// <param name="project">Project arrangement, or null to disable scheduling.</param>
    /// <param name="trackSamples">Samples keyed by track identifier.</param>
    public void SetProject(Project? project, IReadOnlyDictionary<Guid, AudioSample>? trackSamples)
    {
        if (project is null)
        {
            if (trackSamples is { Count: > 0 })
            {
                throw new ArgumentException("Track sample assignments require a project.", nameof(trackSamples));
            }

            Volatile.Write(ref _snapshot, null);
            return;
        }

        var trackCount = project.Tracks.Count;
        var tracks = new Track[trackCount];
        var samples = new AudioSample?[trackCount];
        for (var index = 0; index < trackCount; index++)
        {
            var track = project.Tracks[index];
            tracks[index] = track;
            if (trackSamples is not null && trackSamples.TryGetValue(track.Id, out var sample))
            {
                samples[index] = sample ?? throw new ArgumentException("Track sample assignments cannot contain null values.", nameof(trackSamples));
            }
        }

        var patterns = new Pattern[project.Patterns.Count];
        for (var index = 0; index < patterns.Length; index++)
        {
            patterns[index] = project.Patterns[index];
        }

        var clips = new Clip[project.Clips.Count];
        for (var index = 0; index < clips.Length; index++)
        {
            clips[index] = project.Clips[index];
        }

        Volatile.Write(ref _snapshot, new ArrangementSnapshot(project.Bpm, tracks, samples, patterns, clips));
    }

    /// <summary>Resets timeline traversal state so playback starts at the current position.</summary>
    public void Reset()
    {
        _activeSnapshot = null;
        _lastTick = -1;
    }

    /// <summary>Schedules clip pattern events crossed between the previous and current ticks.</summary>
    /// <param name="currentTick">Absolute project transport tick.</param>
    /// <param name="trigger">Callback that attempts to play a sample.</param>
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

        if (snapshot is null || snapshot.Clips.Length == 0 || currentTick == _lastTick)
        {
            _lastTick = currentTick;
            return 0;
        }

        var firstTick = Math.Max(0, _lastTick);
        var triggerCount = 0;
        var hasSolo = false;
        for (var trackIndex = 0; trackIndex < snapshot.Tracks.Length; trackIndex++)
        {
            hasSolo |= snapshot.Tracks[trackIndex].IsSolo;
        }

        for (var clipIndex = 0; clipIndex < snapshot.Clips.Length; clipIndex++)
        {
            var clip = snapshot.Clips[clipIndex];
            var clipEndTick = checked(clip.StartTick + clip.LengthTicks);
            if (clip.StartTick > currentTick || clipEndTick <= firstTick)
            {
                continue;
            }

            var trackIndex = FindTrackIndex(snapshot.Tracks, clip.TrackId);
            var patternIndex = FindPatternIndex(snapshot.Patterns, clip.PatternId);
            if (trackIndex < 0 || patternIndex < 0)
            {
                continue;
            }

            var track = snapshot.Tracks[trackIndex];
            var sample = snapshot.TrackSamples[trackIndex];
            if (track.IsMuted || (hasSolo && !track.IsSolo) || sample is null)
            {
                continue;
            }

            var pattern = snapshot.Patterns[patternIndex];
            var activeStartTick = Math.Max(firstTick, clip.StartTick);
            var activeEndTick = Math.Min(currentTick, clipEndTick - 1);
            var firstRelativeStep = Math.Max(0, (activeStartTick - clip.StartTick) / MusicalConstants.TicksPerSixteenth);
            var lastRelativeStep = (activeEndTick - clip.StartTick) / MusicalConstants.TicksPerSixteenth;
            for (var relativeStep = firstRelativeStep; relativeStep <= lastRelativeStep; relativeStep++)
            {
                var patternStep = clip.IsLooped
                    ? (int)(relativeStep % pattern.StepCount)
                    : (int)relativeStep;
                if (patternStep >= pattern.StepCount)
                {
                    break;
                }

                var step = pattern.Steps[patternStep];
                if (!step.IsEnabled || !StepPatternScheduler.ShouldTriggerProbability(step.Probability, relativeStep, patternStep))
                {
                    continue;
                }

                var absoluteStepTick = checked(clip.StartTick + (relativeStep * MusicalConstants.TicksPerSixteenth));
                if (absoluteStepTick < firstTick || absoluteStepTick > currentTick || absoluteStepTick >= clipEndTick)
                {
                    continue;
                }

                var triggerVolume = track.Volume * step.Velocity;
                trigger(sample, triggerVolume, track.Pan);
                triggerCount++;
            }
        }

        _lastTick = currentTick;
        return triggerCount;
    }

    private static int FindTrackIndex(Track[] tracks, Guid trackId)
    {
        for (var index = 0; index < tracks.Length; index++)
        {
            if (tracks[index].Id == trackId)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindPatternIndex(Pattern[] patterns, Guid patternId)
    {
        for (var index = 0; index < patterns.Length; index++)
        {
            if (patterns[index].Id == patternId)
            {
                return index;
            }
        }

        return -1;
    }

    private sealed record ArrangementSnapshot(
        int Bpm,
        Track[] Tracks,
        AudioSample?[] TrackSamples,
        Pattern[] Patterns,
        Clip[] Clips);
}
