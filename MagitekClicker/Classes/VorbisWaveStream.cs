using NAudio.Wave;
using NVorbis;
using System;

namespace MagitekClicker.Classes;

/// <summary>
/// Custom WaveStream wrapper for NVorbis to handle Vorbis-encoded .ogg files
/// </summary>
internal class VorbisWaveStream : WaveStream
{
    private readonly VorbisReader _reader;
    private readonly WaveFormat _waveFormat;

    public VorbisWaveStream(string fileName)
    {
        _reader = new VorbisReader(fileName);
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
    }

    public override WaveFormat WaveFormat => _waveFormat;

    public override long Length => _reader.TotalSamples * _waveFormat.BlockAlign;

    public override long Position
    {
        get => _reader.DecodedPosition * _waveFormat.BlockAlign;
        set => _reader.DecodedPosition = value / _waveFormat.BlockAlign;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var floatBuffer = new float[count / 4];
        var samplesRead = _reader.ReadSamples(floatBuffer, 0, floatBuffer.Length);
        Buffer.BlockCopy(floatBuffer, 0, buffer, offset, samplesRead * 4);
        return samplesRead * 4;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader?.Dispose();
        }
        base.Dispose(disposing);
    }
}
