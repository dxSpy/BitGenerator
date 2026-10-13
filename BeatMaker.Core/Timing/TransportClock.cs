using BeatMaker.Core.Audio;
using BeatMaker.Core.Constants;

namespace BeatMaker.Core.Timing;

/// <summary>Tracks musical position from rendered audio frames without cumulative rounding drift.</summary>
public sealed class TransportClock
{
    private long _samplePosition;
    private long _currentTick;
    private long _fractionalTickRemainder;

    /// <summary>Creates a transport clock with the specified audio format and tempo.</summary>
    /// <param name="sampleRate">Audio sample rate in frames per second.</param>
    /// <param name="tempoBpm">Tempo in beats per minute.</param>
    public TransportClock(int sampleRate, int tempoBpm)
    {
        ValidateSampleRate(sampleRate);
        ValidateTempo(tempoBpm);
        SampleRate = sampleRate;
        TempoBpm = tempoBpm;
    }

    /// <summary>Gets the audio sample rate in frames per second.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the current tempo in beats per minute.</summary>
    public int TempoBpm { get; private set; }

    /// <summary>Gets the number of audio frames advanced since the last reset.</summary>
    public long SamplePosition => _samplePosition;

    /// <summary>Gets the current musical position in PPQ ticks.</summary>
    public long CurrentTick => _currentTick;

    /// <summary>Gets whether the clock is advancing.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Starts or resumes the clock.</summary>
    public void Start() => IsRunning = true;

    /// <summary>Pauses the clock while retaining its position.</summary>
    public void Pause() => IsRunning = false;

    /// <summary>Stops and resets the clock to the beginning.</summary>
    public void Reset()
    {
        IsRunning = false;
        _samplePosition = 0;
        _currentTick = 0;
        _fractionalTickRemainder = 0;
    }

    /// <summary>Changes the tempo without resetting the musical position.</summary>
    /// <param name="tempoBpm">Tempo from 40 through 300 beats per minute.</param>
    public void SetTempo(int tempoBpm)
    {
        ValidateTempo(tempoBpm);
        TempoBpm = tempoBpm;
        _fractionalTickRemainder = 0;
    }

    /// <summary>Advances the clock by the supplied number of audio frames when running.</summary>
    /// <param name="frameCount">Number of audio frames to advance.</param>
    public void AdvanceFrames(int frameCount)
    {
        if (frameCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        }

        if (!IsRunning || frameCount == 0)
        {
            return;
        }

        var denominator = (long)SampleRate * 60;
        var numerator = (long)frameCount * TempoBpm * MusicalConstants.PulsesPerQuarterNote + _fractionalTickRemainder;
        _currentTick = checked(_currentTick + numerator / denominator);
        _fractionalTickRemainder = numerator % denominator;
        _samplePosition = checked(_samplePosition + frameCount);
    }

    private static void ValidateSampleRate(int sampleRate)
    {
        if (sampleRate is < 8_000 or > 192_000)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }
    }

    private static void ValidateTempo(int tempoBpm)
    {
        if (tempoBpm is < MusicalConstants.MinimumBpm or > MusicalConstants.MaximumBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoBpm));
        }
    }
}
