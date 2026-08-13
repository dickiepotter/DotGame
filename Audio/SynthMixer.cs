using System;

namespace DotGame.Audio;

/// <summary>Shape of a voice's oscillator.</summary>
public enum Timbre
{
    /// <summary>Pure tone, and the carrier for FM.</summary>
    Sine,
    /// <summary>Softer than a square, richer than a sine.</summary>
    Triangle,
    /// <summary>Filtered noise - impacts, thrusters, shimmer.</summary>
    Noise
}

/// <summary>
/// The full description of one sound. Passed by reference so adding a parameter does not mean
/// another argument on an already long call.
/// </summary>
public struct VoiceSpec
{
    public Timbre Timbre;
    public float StartFreq;
    public float EndFreq;
    public float Amplitude;
    public float Attack;
    public float Decay;
    public float Pan;

    /// <summary>Noise filter cutoff, 0..1. Sweeping it open or shut is what makes a whoosh.</summary>
    public float LowPass;
    public float LowPassEnd;

    /// <summary>FM modulator frequency as a multiple of the carrier. 0 disables FM.</summary>
    public float ModRatio;

    /// <summary>FM depth. Non-integer ratios with real depth give inharmonic, metallic tone.</summary>
    public float ModIndex;

    /// <summary>Ring modulator frequency in Hz. 0 disables it.</summary>
    public float RingFreq;

    public float VibratoHz;
    public float VibratoDepth;

    /// <summary>
    /// Glide pitch geometrically rather than linearly. Pitch is perceived logarithmically, so
    /// an exponential sweep is the one that sounds like an even slide instead of lurching.
    /// </summary>
    public bool ExponentialSweep;

    /// <summary>How much of this voice is fed to the delay line, 0..1.</summary>
    public float DelaySend;

    public static VoiceSpec Tone(float startFreq, float endFreq, float amplitude, float attack,
        float decay, float pan) => new()
    {
        Timbre = Timbre.Sine,
        StartFreq = startFreq,
        EndFreq = endFreq,
        Amplitude = amplitude,
        Attack = attack,
        Decay = decay,
        Pan = pan,
        LowPass = 1f,
        LowPassEnd = 1f,
        ExponentialSweep = true,
        DelaySend = 0.5f
    };

    public static VoiceSpec Noise(float amplitude, float attack, float decay, float pan,
        float lowPass, float lowPassEnd) => new()
    {
        Timbre = Timbre.Noise,
        Amplitude = amplitude,
        Attack = attack,
        Decay = decay,
        Pan = pan,
        LowPass = lowPass,
        LowPassEnd = lowPassEnd,
        DelaySend = 0.35f
    };
}

/// <summary>
/// One sounding note. Pooled and reused - a garbage collection during buffer fill is audible
/// as a dropout, so nothing here allocates.
/// </summary>
public sealed class Voice
{
    public bool Active;
    public VoiceSpec Spec;

    private double _phase, _modPhase, _ringPhase;
    private float _elapsed;
    private float _lpState;

    public void Reset()
    {
        _phase = _modPhase = _ringPhase = 0;
        _elapsed = 0;
        _lpState = 0;
        Active = true;
    }

    /// <summary>Adds this voice into interleaved stereo dry and delay-send buffers.</summary>
    public void Render(float[] dry, float[] send, int frames, Random noise, float sampleRate)
    {
        float total = Spec.Attack + Spec.Decay;
        float leftGain = 0.5f * (1f - Spec.Pan);
        float rightGain = 0.5f * (1f + Spec.Pan);
        float sendLevel = Spec.DelaySend;

        for (int i = 0; i < frames; i++)
        {
            if (_elapsed >= total) { Active = false; return; }

            float env;
            if (_elapsed < Spec.Attack)
                env = Spec.Attack <= 0f ? 1f : _elapsed / Spec.Attack;
            else
            {
                float d = (_elapsed - Spec.Attack) / MathF.Max(0.0001f, Spec.Decay);
                env = (1f - d) * (1f - d);
            }

            float progress = _elapsed / MathF.Max(0.0001f, total);

            float freq;
            if (Spec.ExponentialSweep && Spec.StartFreq > 1f && Spec.EndFreq > 1f)
                freq = Spec.StartFreq * MathF.Pow(Spec.EndFreq / Spec.StartFreq, progress);
            else
                freq = Spec.StartFreq + (Spec.EndFreq - Spec.StartFreq) * progress;

            if (Spec.VibratoDepth > 0f)
                freq *= 1f + Spec.VibratoDepth * MathF.Sin(2f * MathF.PI * Spec.VibratoHz * _elapsed);

            float sample;
            switch (Spec.Timbre)
            {
                case Timbre.Sine:
                {
                    // Frequency modulation. A modulator at a non-integer ratio of the carrier
                    // produces partials that are not whole multiples of the fundamental, which
                    // is exactly what makes a tone read as metallic or synthetic rather than
                    // as a plain musical note.
                    float mod = 0f;
                    if (Spec.ModIndex > 0f)
                    {
                        mod = MathF.Sin((float)_modPhase) * Spec.ModIndex;
                        _modPhase += 2.0 * Math.PI * freq * Spec.ModRatio / sampleRate;
                        if (_modPhase > Math.PI * 2.0) _modPhase -= Math.PI * 2.0;
                    }
                    sample = MathF.Sin((float)_phase + mod);
                    break;
                }
                case Timbre.Triangle:
                {
                    float t = (float)(_phase / (Math.PI * 2.0)) % 1f;
                    sample = 4f * MathF.Abs(t - 0.5f) - 1f;
                    break;
                }
                default:
                {
                    float cutoff = Spec.LowPass + (Spec.LowPassEnd - Spec.LowPass) * progress;
                    float white = (float)(noise.NextDouble() * 2.0 - 1.0);
                    _lpState += (white - _lpState) * Math.Clamp(cutoff, 0.005f, 1f);
                    sample = _lpState;
                    break;
                }
            }

            // Ring modulation: multiplying by a second oscillator replaces the fundamental
            // with sum and difference frequencies. The result is clangorous and unmistakably
            // synthetic - the classic sound of a transporter or a hostile machine.
            if (Spec.RingFreq > 0f)
            {
                sample *= MathF.Sin((float)_ringPhase);
                _ringPhase += 2.0 * Math.PI * Spec.RingFreq / sampleRate;
                if (_ringPhase > Math.PI * 2.0) _ringPhase -= Math.PI * 2.0;
            }

            _phase += 2.0 * Math.PI * freq / sampleRate;
            if (_phase > Math.PI * 2.0) _phase -= Math.PI * 2.0;

            float value = sample * env * Spec.Amplitude;
            float l = value * leftGain, r = value * rightGain;

            dry[i * 2] += l;
            dry[i * 2 + 1] += r;
            if (sendLevel > 0f)
            {
                send[i * 2] += l * sendLevel;
                send[i * 2 + 1] += r * sendLevel;
            }

            _elapsed += 1f / sampleRate;
        }
    }
}

/// <summary>
/// Mixes a pool of voices, a continuous ambient bed, and a damped stereo delay.
///
/// The delay is what makes the palette read as science fiction rather than as beeps. Nothing
/// in the simulation is in a room, but a sound with no reflections at all is heard as tiny
/// and close; adding offset repeats that darken as they decay places every event in a large
/// cold space, and does more for the character of the whole thing than any single voice does.
/// </summary>
public sealed class SynthMixer
{
    private const int MaxVoices = 24;
    private const int DelayBufferFrames = WaveOutDevice.SampleRate; // one second, ample

    private readonly Voice[] _voices = new Voice[MaxVoices];
    private readonly Random _noise = new(12345);
    private readonly object _gate = new();

    private float[] _sendBuffer = Array.Empty<float>();
    private readonly float[] _delayL = new float[DelayBufferFrames];
    private readonly float[] _delayR = new float[DelayBufferFrames];
    private int _delayPos;
    private float _dampL, _dampR;

    // Offset left and right taps so repeats spread across the stereo field
    private readonly int _tapL = (int)(WaveOutDevice.SampleRate * 0.227);
    private readonly int _tapR = (int)(WaveOutDevice.SampleRate * 0.313);

    // Ambient bed
    private double _ambPhaseA, _ambPhaseB, _ambPhaseC, _ambLfo;
    private float _ambLevel, _ambTargetLevel;
    private float _ambFreq = 55f, _ambTargetFreq = 55f;

    /// <summary>Master gain, 0..1.</summary>
    public float Volume { get; set; } = 0.6f;

    /// <summary>Whether the low ambient drone is mixed in.</summary>
    public bool AmbientEnabled { get; set; } = true;

    /// <summary>How much delayed signal is returned into the mix, 0..1.</summary>
    public float DelayMix { get; set; } = 0.44f;

    /// <summary>
    /// How much of each repeat feeds the next, 0..0.9. Set high enough that a sound gets
    /// several audible reflections rather than one - a single repeat reads as a slapback
    /// artefact, whereas a decaying train of them reads as a large empty space.
    /// </summary>
    public float DelayFeedback { get; set; } = 0.52f;

    public SynthMixer()
    {
        for (int i = 0; i < _voices.Length; i++) _voices[i] = new Voice();
    }

    /// <summary>Sets the drone's target pitch and loudness; both are smoothed in the mixer.</summary>
    public void SetAmbient(float frequency, float level)
    {
        _ambTargetFreq = frequency;
        _ambTargetLevel = level;
    }

    /// <summary>
    /// Starts a voice if one is free. Returns false when the pool is saturated, which the
    /// caller can safely ignore - a dropped voice in a dense moment is inaudible.
    /// </summary>
    public bool Play(in VoiceSpec spec)
    {
        lock (_gate)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v.Active) continue;

                v.Spec = spec;
                v.Spec.Pan = Math.Clamp(spec.Pan, -1f, 1f);
                v.Reset();
                return true;
            }
        }
        return false;
    }

    public void StopAll()
    {
        lock (_gate)
        {
            foreach (var v in _voices) v.Active = false;
        }
        _ambLevel = 0f;
        Array.Clear(_delayL);
        Array.Clear(_delayR);
    }

    /// <summary>Fills an interleaved stereo buffer. Called from the audio thread.</summary>
    public bool Fill(float[] buffer, int frames)
    {
        int needed = frames * 2;
        if (_sendBuffer.Length < needed) _sendBuffer = new float[needed];

        Array.Clear(buffer, 0, needed);
        Array.Clear(_sendBuffer, 0, needed);

        lock (_gate)
        {
            foreach (var v in _voices)
                if (v.Active) v.Render(buffer, _sendBuffer, frames, _noise, WaveOutDevice.SampleRate);
        }

        RenderAmbient(buffer, frames);
        ApplyDelay(buffer, frames);

        // Soft saturation. With 24 voices plus delay returns, hard clipping would buzz; this
        // compresses peaks smoothly and leaves quiet material untouched.
        float master = Volume;
        for (int i = 0; i < needed; i++)
        {
            float v = buffer[i] * master;
            buffer[i] = v / (1f + MathF.Abs(v));
        }
        return true;
    }

    private void ApplyDelay(float[] buffer, int frames)
    {
        float mix = Math.Clamp(DelayMix, 0f, 1f);
        float feedback = Math.Clamp(DelayFeedback, 0f, 0.9f);
        if (mix <= 0.001f) return;

        for (int i = 0; i < frames; i++)
        {
            int readL = _delayPos - _tapL; if (readL < 0) readL += DelayBufferFrames;
            int readR = _delayPos - _tapR; if (readR < 0) readR += DelayBufferFrames;

            float echoL = _delayL[readL];
            float echoR = _delayR[readR];

            // Damp the feedback path so each repeat is darker than the last, the way a real
            // space absorbs high frequencies. Undamped repeats sound like a digital fault.
            _dampL += (echoL - _dampL) * 0.42f;
            _dampR += (echoR - _dampR) * 0.42f;

            // Cross the feedback between channels so repeats bounce side to side
            _delayL[_delayPos] = _sendBuffer[i * 2] + _dampR * feedback;
            _delayR[_delayPos] = _sendBuffer[i * 2 + 1] + _dampL * feedback;

            buffer[i * 2] += echoL * mix;
            buffer[i * 2 + 1] += echoR * mix;

            _delayPos++;
            if (_delayPos >= DelayBufferFrames) _delayPos = 0;
        }
    }

    private void RenderAmbient(float[] buffer, int frames)
    {
        float target = AmbientEnabled ? _ambTargetLevel : 0f;
        const float sampleRate = WaveOutDevice.SampleRate;

        for (int i = 0; i < frames; i++)
        {
            _ambLevel += (target - _ambLevel) * 0.0002f;
            _ambFreq += (_ambTargetFreq - _ambFreq) * 0.0002f;

            if (_ambLevel < 0.0002f) continue;

            // A slow tremolo across three partials - fundamental, a slightly sharp octave that
            // beats against it, and a fifth. The beating is what keeps a sustained drone
            // feeling alive and faintly uneasy rather than like a held organ note.
            _ambLfo += 0.09 / sampleRate;
            if (_ambLfo > 1.0) _ambLfo -= 1.0;
            float shimmer = 0.82f + 0.18f * MathF.Sin((float)(_ambLfo * Math.PI * 2.0));

            float a = MathF.Sin((float)_ambPhaseA);
            float b = MathF.Sin((float)_ambPhaseB) * 0.42f;
            float c = MathF.Sin((float)_ambPhaseC) * 0.22f;

            _ambPhaseA += 2.0 * Math.PI * _ambFreq / sampleRate;
            _ambPhaseB += 2.0 * Math.PI * (_ambFreq * 2.008f) / sampleRate;
            _ambPhaseC += 2.0 * Math.PI * (_ambFreq * 1.4983f) / sampleRate;
            if (_ambPhaseA > Math.PI * 2.0) _ambPhaseA -= Math.PI * 2.0;
            if (_ambPhaseB > Math.PI * 2.0) _ambPhaseB -= Math.PI * 2.0;
            if (_ambPhaseC > Math.PI * 2.0) _ambPhaseC -= Math.PI * 2.0;

            float value = (a + b + c) * _ambLevel * shimmer;
            buffer[i * 2] += value;
            buffer[i * 2 + 1] += value;
        }
    }
}
