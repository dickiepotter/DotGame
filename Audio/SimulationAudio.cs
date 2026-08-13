using System;
using System.Collections.Generic;
using DotGame.Models;

namespace DotGame.Audio;

/// <summary>
/// Turns simulation events into sound.
///
/// Two ideas run through the mapping. First, pitch follows mass: a heavy particle speaks low
/// and a light one high, on the same reasoning that makes a big bell deeper than a small one.
/// Second, stereo position follows screen position, so a death on the left is heard on the
/// left. Together they mean the audio carries real information about the simulation rather
/// than being decoration over it.
///
/// Everything is optional and entirely passive: if no audio device opens, or the feature is
/// switched off, every method here becomes a cheap no-op and the simulation is unaffected.
/// </summary>
public sealed class SimulationAudio : IDisposable
{
    // Minimum spacing between sounds of the same kind. Dozens of particles can eat in the
    // same frame; without a floor on the interval the result is a solid rasp rather than
    // distinguishable events.
    private const double EatIntervalSeconds = 0.045;
    private const double DeathIntervalSeconds = 0.060;
    private const double BirthIntervalSeconds = 0.055;
    private const double SplitIntervalSeconds = 0.080;
    private const double PhaseIntervalSeconds = 0.090;
    private const double BurstIntervalSeconds = 0.070;

    private readonly SynthMixer _mixer = new();
    private readonly WaveOutDevice _device;
    private readonly Dictionary<string, double> _lastPlayed = new();

    private double _clock;
    private double _worldWidth = 800;
    private bool _enabled;
    private readonly bool _offline;

    public SimulationAudio() : this(false) { }

    private SimulationAudio(bool offline)
    {
        _offline = offline;
        _device = new WaveOutDevice(_mixer.Fill);
    }

    /// <summary>
    /// Creates an instance that synthesises without claiming an output device. The caller
    /// pumps the mixer itself through <see cref="RenderTo"/>, which is how the sound design
    /// can be auditioned or tested without anything being audible.
    /// </summary>
    public static SimulationAudio CreateOffline() => new(true);

    /// <summary>
    /// Renders the current mix into an interleaved stereo buffer. Offline use only - when a
    /// device is open it is already pumping the mixer, and pulling from two places at once
    /// would tear the output.
    /// </summary>
    public void RenderTo(float[] interleavedStereo, int frames)
    {
        if (!_offline) throw new InvalidOperationException(
            "RenderTo is only valid on an instance created with CreateOffline().");
        _mixer.Fill(interleavedStereo, frames);
    }

    /// <summary>Direct access to mixer settings, for offline rendering.</summary>
    public SynthMixer Mixer => _mixer;

    /// <summary>True once an output device is running.</summary>
    public bool IsAvailable => _device.IsOpen;

    /// <summary>Why audio could not start, if it did not.</summary>
    public string? FailureReason => _device.FailureReason;

    /// <summary>Master volume, 0..1.</summary>
    public double Volume
    {
        get => _mixer.Volume;
        set => _mixer.Volume = (float)Math.Clamp(value, 0, 1);
    }

    /// <summary>Whether the low population drone is mixed in.</summary>
    public bool AmbientEnabled
    {
        get => _mixer.AmbientEnabled;
        set => _mixer.AmbientEnabled = value;
    }

    /// <summary>
    /// Turns sound on or off. Starting is lazy so a machine without an audio device never
    /// pays for one, and never fails at startup for a feature the user may not want.
    /// </summary>
    public bool Enabled
    {
        get => _enabled && (_offline || _device.IsOpen);
        set
        {
            if (value == _enabled) return;
            _enabled = value;

            if (value)
            {
                if (!_offline && !_device.IsOpen) _device.Start();
            }
            else
            {
                _mixer.StopAll();
            }
        }
    }

    /// <summary>Keeps stereo panning proportional to the actual canvas width.</summary>
    public void SetWorldWidth(double width)
    {
        if (width > 1) _worldWidth = width;
    }

    /// <summary>
    /// Advances the audio clock and updates the ambient bed from the state of the population.
    /// The drone drops in pitch as the population grows and swells with total energy, so the
    /// health of the ecosystem is audible without looking at it.
    /// </summary>
    public void Update(List<Particle> particles, double deltaTime)
    {
        _clock += deltaTime;
        if (!Enabled) return;

        int count = particles.Count;
        if (count == 0)
        {
            _mixer.SetAmbient(40f, 0f);
            return;
        }

        double totalEnergy = 0, maxEnergy = 0;
        foreach (var p in particles)
        {
            if (!p.HasAbilities) continue;
            totalEnergy += p.Abilities!.Energy;
            maxEnergy += p.Abilities.MaxEnergy;
        }

        float fullness = maxEnergy > 0 ? (float)(totalEnergy / maxEnergy) : 0.5f;

        // More particles -> lower, heavier drone
        float freq = 34f + 44f / (1f + count / 22f);
        float level = 0.020f + 0.045f * fullness;

        _mixer.SetAmbient(freq, level);
    }

    /// <summary>
    /// A particle consumed another - a hard energy-weapon discharge. A steep downward
    /// exponential sweep is the canonical "zap"; the FM sidebands give it a hard synthetic
    /// bite that a plain sine cannot produce.
    /// </summary>
    public void Eat(Particle predator, Particle prey)
    {
        if (!Enabled || !RateLimit("eat", EatIntervalSeconds)) return;

        float pan = PanFor(predator);
        float freq = PitchForMass(prey.Mass, 900f);

        var zap = VoiceSpec.Tone(freq, freq * 0.22f, 0.30f, 0.002f, 0.14f, pan);
        zap.ModRatio = 2.41f;   // deliberately not a whole number, so the partials are inharmonic
        zap.ModIndex = 5.5f;
        zap.DelaySend = 0.55f;
        _mixer.Play(zap);

        var spark = VoiceSpec.Noise(0.11f, 0.001f, 0.05f, pan, 0.85f, 0.25f);
        _mixer.Play(spark);
    }

    /// <summary>
    /// A particle starved - a reactor losing containment. Ring modulation over a long
    /// downward sweep gives the clangorous, failing-machine character; the long tail feeds
    /// the delay so it collapses away into the distance.
    /// </summary>
    public void Death(Particle particle)
    {
        if (!Enabled || !RateLimit("death", DeathIntervalSeconds)) return;

        float pan = PanFor(particle);
        float freq = PitchForMass(particle.Mass, 320f);

        var collapse = VoiceSpec.Tone(freq, freq * 0.18f, 0.34f, 0.004f, 0.55f, pan);
        collapse.ModRatio = 1.37f;
        collapse.ModIndex = 3.2f;
        collapse.RingFreq = freq * 0.51f;
        collapse.DelaySend = 0.8f;
        _mixer.Play(collapse);

        var sub = VoiceSpec.Tone(freq * 0.5f, freq * 0.16f, 0.26f, 0.006f, 0.60f, pan);
        sub.Timbre = Timbre.Triangle;
        _mixer.Play(sub);

        // Noise that darkens as it decays - the sound of something venting and dying down
        _mixer.Play(VoiceSpec.Noise(0.20f, 0.002f, 0.42f, pan, 0.55f, 0.04f));
    }

    /// <summary>
    /// A particle was born - a materialisation. Rising exponential sweep with vibrato and
    /// heavy FM, then a bloom of shimmer, all fed hard into the delay so it phases in rather
    /// than simply appearing.
    /// </summary>
    public void Birth(Particle particle)
    {
        if (!Enabled || !RateLimit("birth", BirthIntervalSeconds)) return;

        float pan = PanFor(particle);
        float freq = PitchForMass(particle.Mass, 520f);

        var materialise = VoiceSpec.Tone(freq * 0.5f, freq * 2.2f, 0.20f, 0.020f, 0.34f, pan);
        materialise.ModRatio = 3.02f;
        materialise.ModIndex = 2.6f;
        materialise.VibratoHz = 17f;
        materialise.VibratoDepth = 0.022f;
        materialise.DelaySend = 0.85f;
        _mixer.Play(materialise);

        var shimmer = VoiceSpec.Noise(0.07f, 0.030f, 0.26f, pan, 0.55f, 0.95f);
        shimmer.DelaySend = 0.7f;
        _mixer.Play(shimmer);
    }

    /// <summary>
    /// A particle divided - a replicator cycle. Two ring-modulated tones panned apart, the
    /// second slightly detuned so the pair beats against itself.
    /// </summary>
    public void Split(Particle particle)
    {
        if (!Enabled || !RateLimit("split", SplitIntervalSeconds)) return;

        float freq = PitchForMass(particle.Mass, 620f);
        float pan = PanFor(particle);

        var a = VoiceSpec.Tone(freq, freq * 1.32f, 0.18f, 0.004f, 0.20f, pan - 0.35f);
        a.RingFreq = freq * 0.74f;
        a.ModRatio = 2.0f;
        a.ModIndex = 1.8f;
        _mixer.Play(a);

        var b = VoiceSpec.Tone(freq * 1.005f, freq * 1.49f, 0.15f, 0.004f, 0.24f, pan + 0.35f);
        b.RingFreq = freq * 0.76f;
        b.ModRatio = 2.0f;
        b.ModIndex = 1.8f;
        _mixer.Play(b);
    }

    /// <summary>
    /// A particle phased - a transporter effect. Deep vibrato plus ring modulation over a
    /// long rising sweep, with almost everything routed to the delay so it smears.
    /// </summary>
    public void Phase(Particle particle)
    {
        if (!Enabled || !RateLimit("phase", PhaseIntervalSeconds)) return;

        float pan = PanFor(particle);

        var beam = VoiceSpec.Tone(420f, 2400f, 0.15f, 0.040f, 0.46f, pan);
        beam.RingFreq = 143f;
        beam.VibratoHz = 23f;
        beam.VibratoDepth = 0.05f;
        beam.ModRatio = 1.51f;
        beam.ModIndex = 2.2f;
        beam.DelaySend = 0.95f;
        _mixer.Play(beam);

        var air = VoiceSpec.Noise(0.08f, 0.050f, 0.40f, pan, 0.30f, 0.98f);
        air.DelaySend = 0.8f;
        _mixer.Play(air);
    }

    /// <summary>
    /// A speed burst - a thruster igniting. The filter opening from dull to bright over the
    /// noise is the whole effect; the rising tone underneath supplies the sense of thrust.
    /// </summary>
    public void SpeedBurst(Particle particle)
    {
        if (!Enabled || !RateLimit("burst", BurstIntervalSeconds)) return;

        float pan = PanFor(particle);

        _mixer.Play(VoiceSpec.Noise(0.24f, 0.015f, 0.26f, pan, 0.08f, 0.90f));

        var thrust = VoiceSpec.Tone(180f, 1150f, 0.13f, 0.012f, 0.22f, pan);
        thrust.ModRatio = 1.98f;
        thrust.ModIndex = 1.4f;
        _mixer.Play(thrust);
    }

    /// <summary>
    /// Heavier particles sound lower. Mass is taken to a fractional power rather than used
    /// directly so the range stays musical across the full 0.5-70 mass span the simulation
    /// actually produces.
    /// </summary>
    private static float PitchForMass(double mass, float reference)
    {
        double m = Math.Clamp(mass, 0.3, 90.0);
        return (float)(reference / Math.Pow(m, 0.42));
    }

    private float PanFor(Particle particle)
    {
        return (float)Math.Clamp(particle.Position.X / _worldWidth * 2.0 - 1.0, -1.0, 1.0) * 0.8f;
    }

    private bool RateLimit(string key, double interval)
    {
        if (_lastPlayed.TryGetValue(key, out double last) && _clock - last < interval)
            return false;
        _lastPlayed[key] = _clock;
        return true;
    }

    public void Dispose() => _device.Dispose();
}
