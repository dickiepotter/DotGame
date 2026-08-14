using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DotGame.Audio;

/// <summary>
/// A minimal streaming audio output built directly on WinMM's waveOut API.
///
/// Done by P/Invoke rather than with an audio library because the project carries no NuGet
/// dependencies and ships no sound assets: everything here is generated at runtime, so all
/// that is actually required is somewhere to push a stream of samples.
///
/// A pool of buffers is kept in flight. A background thread refills each one as the device
/// finishes with it, so playback is continuous as long as the fill callback keeps returning.
/// </summary>
public sealed class WaveOutDevice : IDisposable
{
    public const int SampleRate = 44100;
    public const int Channels = 2;

    // ~23ms per buffer, four in flight. Short enough that an event and its sound stay
    // associated; long enough that a scheduling hiccup does not underrun. Public because the
    // mixer sizes its scratch buffers up front from it, so that filling one never allocates.
    public const int FramesPerBuffer = 1024;
    private const int BufferCount = 4;

    private const uint WAVE_MAPPER = 0xFFFFFFFF;
    private const uint CALLBACK_EVENT = 0x00050000;
    private const uint WHDR_DONE = 0x00000001;
    private const uint WHDR_PREPARED = 0x00000002;
    private const uint WAVE_FORMAT_PCM = 1;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct WaveFormatEx
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    [DllImport("winmm.dll")] private static extern int waveOutOpen(out IntPtr hWaveOut, uint uDeviceID,
        ref WaveFormatEx lpFormat, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);
    [DllImport("winmm.dll")] private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);
    [DllImport("winmm.dll")] private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);
    [DllImport("winmm.dll")] private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);
    [DllImport("winmm.dll")] private static extern int waveOutReset(IntPtr hWaveOut);
    [DllImport("winmm.dll")] private static extern int waveOutClose(IntPtr hWaveOut);

    private readonly Func<float[], int, bool> _fill;
    private readonly int _hdrSize = Marshal.SizeOf<WaveHdr>();

    private IntPtr _handle;
    private IntPtr[] _headers = Array.Empty<IntPtr>();
    private IntPtr[] _data = Array.Empty<IntPtr>();
    private byte[] _bytes = Array.Empty<byte>();
    private float[] _scratch = Array.Empty<float>();
    private AutoResetEvent? _bufferDone;
    private Thread? _thread;
    private volatile bool _running;

    /// <summary>True when a device was opened successfully.</summary>
    public bool IsOpen => _handle != IntPtr.Zero;

    /// <summary>
    /// Last failure reason, if opening did not succeed. Audio is optional, so a machine with
    /// no output device must degrade to silence rather than take the application down.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <param name="fill">
    /// Fills interleaved stereo floats in [-1, 1]. Returns false to request shutdown.
    /// </param>
    public WaveOutDevice(Func<float[], int, bool> fill)
    {
        _fill = fill;
    }

    public bool Start()
    {
        if (IsOpen) return true;

        try
        {
            var format = new WaveFormatEx
            {
                wFormatTag = (ushort)WAVE_FORMAT_PCM,
                nChannels = Channels,
                nSamplesPerSec = SampleRate,
                nAvgBytesPerSec = SampleRate * Channels * 2,
                nBlockAlign = Channels * 2,
                wBitsPerSample = 16,
                cbSize = 0
            };

            _bufferDone = new AutoResetEvent(false);
            int result = waveOutOpen(out _handle, WAVE_MAPPER, ref format,
                _bufferDone.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, CALLBACK_EVENT);

            if (result != 0 || _handle == IntPtr.Zero)
            {
                FailureReason = $"waveOutOpen failed ({result}) - no usable audio output device";
                _handle = IntPtr.Zero;
                return false;
            }

            int bufferBytes = FramesPerBuffer * Channels * 2;
            _bytes = new byte[bufferBytes];
            _scratch = new float[FramesPerBuffer * Channels];
            _headers = new IntPtr[BufferCount];
            _data = new IntPtr[BufferCount];

            for (int i = 0; i < BufferCount; i++)
            {
                _data[i] = Marshal.AllocHGlobal(bufferBytes);
                _headers[i] = Marshal.AllocHGlobal(_hdrSize);

                var hdr = new WaveHdr
                {
                    lpData = _data[i],
                    dwBufferLength = (uint)bufferBytes,
                    dwFlags = 0
                };
                Marshal.StructureToPtr(hdr, _headers[i], false);
                waveOutPrepareHeader(_handle, _headers[i], (uint)_hdrSize);

                // Mark as done so the feed thread picks it up on the first pass
                hdr = Marshal.PtrToStructure<WaveHdr>(_headers[i]);
                hdr.dwFlags |= WHDR_DONE;
                Marshal.StructureToPtr(hdr, _headers[i], false);
            }

            _running = true;
            _thread = new Thread(FeedLoop)
            {
                IsBackground = true,
                Name = "DotGame audio",
                Priority = ThreadPriority.AboveNormal
            };
            _thread.Start();
            return true;
        }
        catch (Exception ex)
        {
            FailureReason = $"audio unavailable: {ex.Message}";
            Stop();
            return false;
        }
    }

    private void FeedLoop()
    {
        while (_running)
        {
            bool queuedAny = false;

            for (int i = 0; i < _headers.Length && _running; i++)
            {
                var hdr = Marshal.PtrToStructure<WaveHdr>(_headers[i]);
                if ((hdr.dwFlags & WHDR_DONE) == 0) continue;

                if (!_fill(_scratch, FramesPerBuffer))
                {
                    _running = false;
                    break;
                }

                // Convert to 16-bit PCM, clamping rather than wrapping - a wrapped sample is
                // a loud click, which is far worse than a momentarily flattened peak.
                for (int s = 0; s < _scratch.Length; s++)
                {
                    float v = _scratch[s];
                    if (v > 1f) v = 1f; else if (v < -1f) v = -1f;
                    short pcm = (short)(v * 32767f);
                    _bytes[s * 2] = (byte)(pcm & 0xFF);
                    _bytes[s * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
                }
                Marshal.Copy(_bytes, 0, hdr.lpData, _bytes.Length);

                hdr.dwFlags &= ~WHDR_DONE;
                hdr.dwBufferLength = (uint)_bytes.Length;
                Marshal.StructureToPtr(hdr, _headers[i], false);

                waveOutWrite(_handle, _headers[i], (uint)_hdrSize);
                queuedAny = true;
            }

            // Nothing was free; wait for the device to retire a buffer
            if (!queuedAny) _bufferDone?.WaitOne(10);
        }
    }

    public void Stop()
    {
        _running = false;

        try { _thread?.Join(300); } catch { /* shutting down anyway */ }
        _thread = null;

        if (_handle != IntPtr.Zero)
        {
            waveOutReset(_handle);
            for (int i = 0; i < _headers.Length; i++)
            {
                if (_headers[i] != IntPtr.Zero)
                {
                    var hdr = Marshal.PtrToStructure<WaveHdr>(_headers[i]);
                    if ((hdr.dwFlags & WHDR_PREPARED) != 0)
                        waveOutUnprepareHeader(_handle, _headers[i], (uint)_hdrSize);
                }
            }
            waveOutClose(_handle);
            _handle = IntPtr.Zero;
        }

        for (int i = 0; i < _headers.Length; i++)
        {
            if (_headers[i] != IntPtr.Zero) Marshal.FreeHGlobal(_headers[i]);
            if (_data[i] != IntPtr.Zero) Marshal.FreeHGlobal(_data[i]);
        }
        _headers = Array.Empty<IntPtr>();
        _data = Array.Empty<IntPtr>();

        _bufferDone?.Dispose();
        _bufferDone = null;
    }

    public void Dispose() => Stop();
}
