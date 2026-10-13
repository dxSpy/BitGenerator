using System.Buffers.Binary;
using BeatMaker.Audio.Samples;
using FluentAssertions;
using Xunit;

namespace BeatMaker.Tests.Audio;

/// <summary>Validates WAV decoding without requiring an audio output device.</summary>
public sealed class NAudioSampleLoaderTests
{
    [Fact]
    public async Task LoadAsync_DecodesPcmWaveToFloatSamples()
    {
        var path = CreateWaveFile();
        try
        {
            var sample = await new NAudioSampleLoader().LoadAsync(path);

            sample.Name.Should().Be(Path.GetFileNameWithoutExtension(path));
            sample.SampleRate.Should().Be(8_000);
            sample.ChannelCount.Should().Be(1);
            sample.FrameCount.Should().Be(2);
            sample.Samples.ToArray().Should().Equal(0f, 0.5f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsMissingFile()
    {
        var loader = new NAudioSampleLoader();

        var act = () => loader.LoadAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav"));

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    private static string CreateWaveFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(40);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8_000);
        writer.Write(16_000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(4);
        Span<byte> pcm = stackalloc byte[4];
        BinaryPrimitives.WriteInt16LittleEndian(pcm, 0);
        BinaryPrimitives.WriteInt16LittleEndian(pcm[2..], 16_384);
        writer.Write(pcm);
        return path;
    }
}
