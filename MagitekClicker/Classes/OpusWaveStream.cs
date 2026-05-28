using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using System;
using System.IO;

namespace MagitekClicker.Classes;

/// <summary>
/// Custom WaveStream wrapper for Concentus to handle Opus-encoded .ogg files
/// </summary>
internal class OpusWaveStream : WaveStream
{
    private readonly FileStream _fileStream;
    private readonly OpusDecoder _decoder;
    private readonly OpusOggReadStream _opusStream;
    private readonly WaveFormat _waveFormat;
    private long _position;
    private short[] _decodedBuffer;
    private int _bufferPosition;
    private int _bufferLength;

    public OpusWaveStream(string fileName)
    {
        _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        _decoder = new OpusDecoder(48000, 2); // Opus uses 48kHz, stereo
        _opusStream = new OpusOggReadStream(_decoder, _fileStream);

        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        _position = 0;
        _decodedBuffer = new short[48000 * 2]; // 1 second buffer
        _bufferPosition = 0;
        _bufferLength = 0;
    }

    public override WaveFormat WaveFormat => _waveFormat;

    public override long Length => long.MaxValue;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException("Seeking is not supported for Opus streams");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int samplesNeeded = count / 4; // 4 bytes per float
        float[] floatBuffer = new float[samplesNeeded];
        int totalSamplesRead = 0;

        try
        {
            while (totalSamplesRead < samplesNeeded)
            {
                // Fill buffer if needed
                if (_bufferPosition >= _bufferLength)
                {
                    short[] packet = _opusStream.DecodeNextPacket();
                    if (packet == null || packet.Length == 0)
                        break;

                    _decodedBuffer = packet;
                    _bufferLength = packet.Length;
                    _bufferPosition = 0;
                }

                // Copy from buffer
                int samplesToCopy = Math.Min(_bufferLength - _bufferPosition, samplesNeeded - totalSamplesRead);
                for (int i = 0; i < samplesToCopy; i++)
                {
                    floatBuffer[totalSamplesRead + i] = _decodedBuffer[_bufferPosition + i] / 32768f;
                }

                _bufferPosition += samplesToCopy;
                totalSamplesRead += samplesToCopy;
            }
        }
        catch
        {
            // End of stream
        }

        if (totalSamplesRead > 0)
        {
            Buffer.BlockCopy(floatBuffer, 0, buffer, offset, totalSamplesRead * 4);
            _position += totalSamplesRead * 4;
            return totalSamplesRead * 4;
        }

        return 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileStream?.Dispose();
        }
        base.Dispose(disposing);
    }
}
