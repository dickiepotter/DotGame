using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotGame.Models;
using RP.Sound;
using RP.Sound.Playback;

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
/// The sounds themselves are RP.Sound's <see cref="RP.Sound.Games.SciFi"/> palette, rendered once
/// by <see cref="SoundPalette"/> and played back through RP.Sound's real-time mixer. What lives
/// here is only what is specific to *this* simulation: which event makes which sound, how mass
/// becomes pitch, how position becomes pan, and how often a given kind of event is allowed to
/// speak. That division is the whole point — the synthesis is a library concern, the sound
/// *design* is a game concern.
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

    private readonly SampleVoiceMixer _mixer = new(WaveOutDevice.SampleRate, WaveOutDevice.FramesPerBuffer);
    private readonly WaveOutDevice _device;
    private readonly Dictionary<Cue, double> _lastPlayed = new();

    // Baking the palette costs a fraction of a second. Doing it on a background thread keeps the
    // window responsive at start-up; until it lands, every event is simply dropped, which is
    // inaudible because nothing has had time to happen yet.
    private volatile SoundPalette? _palette;

    private double _clock;
    private double _worldWidth = 800;
    private bool _enabled;
    private readonly bool _offline;

    public SimulationAudio() : this(false) { }

    private SimulationAudio(bool offline)
    {
        _offline = offline;
        _device = new WaveOutDevice((buffer, frames) =>
        {
            _mixer.Fill(buffer, frames);
            return true;
        });

        if (offline) _palette = SoundPalette.Bake(WaveOutDevice.SampleRate);
        else Task.Run(() => _palette = SoundPalette.Bake(WaveOutDevice.SampleRate));
    }

    /// <summary>
    /// Creates an instance that synthesises without claiming an output device. The caller
    /// pumps the mixer itself through <see cref="RenderTo"/>, which is how the sound design
    /// can be auditioned or tested without anything being audible. The palette is baked
    /// synchronously here, so the first event after construction is already audible.
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
    public SampleVoiceMixer Mixer => _mixer;

    /// <summary>True once an output device is running.</summary>
    public bool IsAvailable => _device.IsOpen;

    /// <summary>Why audio could not start, if it did not.</summary>
    public string? FailureReason => _device.FailureReason;

    /// <summary>Master volume, 0..1.</summary>
    public double Volume
    {
        get => _mixer.Volume.Linear;
        set => _mixer.Volume = new Level(Math.Clamp(value, 0, 1));
    }

    /// <summary>Whether the low population drone is mixed in.</summary>
    public bool AmbientEnabled
    {
        get => _mixer.BedEnabled;
        set => _mixer.BedEnabled = value;
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

        SoundPalette? palette = _palette;
        if (palette is null) return;

        int count = particles.Count;
        if (count == 0)
        {
            _mixer.SetBed(palette.Drone, 40 / SoundPalette.DroneReferenceHertz, Level.Silence);
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
        double freq = 34.0 + 44.0 / (1.0 + count / 22.0);
        double level = 0.020 + 0.045 * fullness;

        _mixer.SetBed(palette.Drone, freq / SoundPalette.DroneReferenceHertz, new Level(level));
    }

    /// <summary>
    /// A particle consumed another - a hard energy-weapon discharge, pitched by the size of the
    /// meal and placed where the predator is.
    /// </summary>
    public void Eat(Particle predator, Particle prey) =>
        Fire(Cue.Eat, EatIntervalSeconds, PitchForMass(prey.Mass, 900), PanFor(predator), 0.55);

    /// <summary>A particle starved - a reactor losing containment, collapsing away into the delay.</summary>
    public void Death(Particle particle) =>
        Fire(Cue.Death, DeathIntervalSeconds, PitchForMass(particle.Mass, 320), PanFor(particle), 0.8);

    /// <summary>A particle was born - a materialisation, fed hard into the delay so it phases in.</summary>
    public void Birth(Particle particle) =>
        Fire(Cue.Birth, BirthIntervalSeconds, PitchForMass(particle.Mass, 520), PanFor(particle), 0.85);

    /// <summary>A particle divided - a replicator cycle, the two halves beating against each other.</summary>
    public void Split(Particle particle) =>
        Fire(Cue.Split, SplitIntervalSeconds, PitchForMass(particle.Mass, 620), PanFor(particle), 0.5);

    /// <summary>A particle phased - a transporter effect, smeared almost entirely into the delay.</summary>
    public void Phase(Particle particle) =>
        Fire(Cue.Phase, PhaseIntervalSeconds, 0, PanFor(particle), 0.95);

    /// <summary>A speed burst - a thruster igniting, the filter opening as it goes.</summary>
    public void SpeedBurst(Particle particle) =>
        Fire(Cue.SpeedBurst, BurstIntervalSeconds, 0, PanFor(particle), 0.35);

    /// <summary>
    /// The one path every event takes: rate-limit, pick the buffer baked nearest the wanted pitch,
    /// and hand it to the mixer. A refusal anywhere along here is silent and harmless — a dropped
    /// sound in a dense moment is inaudible, whereas stealing a sounding voice would be a click.
    /// </summary>
    private void Fire(Cue cue, double interval, double pitch, double pan, double send)
    {
        if (!Enabled || !RateLimit(cue, interval)) return;

        SoundPalette? palette = _palette;
        if (palette is null) return;

        (AudioBuffer buffer, double rate) = palette.Pick(cue, pitch);
        _mixer.Play(buffer, rate, pan: pan, send: send);
    }

    /// <summary>
    /// Heavier particles sound lower. Mass is taken to a fractional power rather than used
    /// directly so the range stays musical across the full 0.5-70 mass span the simulation
    /// actually produces. The clamp here is what <see cref="SoundPalette"/> sizes its bands from.
    /// </summary>
    private static double PitchForMass(double mass, double reference)
    {
        double m = Math.Clamp(mass, 0.3, 90.0);
        return reference / Math.Pow(m, 0.42);
    }

    private double PanFor(Particle particle)
    {
        return Math.Clamp(particle.Position.X / _worldWidth * 2.0 - 1.0, -1.0, 1.0) * 0.8;
    }

    private bool RateLimit(Cue cue, double interval)
    {
        if (_lastPlayed.TryGetValue(cue, out double last) && _clock - last < interval)
            return false;
        _lastPlayed[cue] = _clock;
        return true;
    }

    public void Dispose() => _device.Dispose();
}
