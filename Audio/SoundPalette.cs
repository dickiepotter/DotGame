using System;
using System.Collections.Generic;
using RP.Sound;
using RP.Sound.Games;

namespace DotGame.Audio;

/// <summary>Which of the simulation's events a sound belongs to.</summary>
public enum Cue
{
    Eat,
    Death,
    Birth,
    Split,
    Phase,
    SpeedBurst
}

/// <summary>
/// The simulation's sounds, rendered once at start-up and then played back.
///
/// RP.Sound describes sounds and renders them offline; the simulation needs one *now*, at a pitch
/// that depends on the mass of a particle nobody knew about a frame ago. Baking bridges the two:
/// every cue is rendered up front, and playback varies pitch by reading the buffer faster or
/// slower, the way a sampler always has.
///
/// The catch is that reading far from the recorded rate audibly shortens the sound and stretches
/// its character, and this simulation asks for a wide range — pitch follows mass over a span of
/// nearly eleven to one. So each pitched cue is baked at several base pitches spaced evenly in
/// octaves, and playback picks the nearest and nudges from there, which keeps every read within
/// about 27% of the rate its buffer was written at. That costs under two megabytes and well under
/// a second, both of which are free at this scale.
/// </summary>
public sealed class SoundPalette
{
    /// <summary>
    /// How many base pitches each pitched cue is baked at. Five over an eleven-to-one span puts the
    /// bands a little over half an octave apart, so the worst-case stretch is small enough not to
    /// hear. More would be diminishing returns; three would start to sound like a sampler.
    /// </summary>
    private const int Bands = 5;

    // The span the mass-to-pitch mapping actually produces, as multiples of a cue's reference
    // pitch. Mass is clamped to 0.3-90 and the pitch is reference / mass^0.42, so the extremes are
    // 0.3^-0.42 and 90^-0.42. Kept here as the one place the two facts have to agree.
    private const double LowestFactor = 0.151;
    private const double HighestFactor = 1.659;

    /// <summary>The pitch each cue is centred on before mass moves it.</summary>
    private static readonly Dictionary<Cue, double> ReferencePitch = new()
    {
        [Cue.Eat] = 900,
        [Cue.Death] = 320,
        [Cue.Birth] = 520,
        [Cue.Split] = 620,
    };

    /// <summary>The frequency the ambient bed is baked at; the drone is resampled either side of it.</summary>
    public const double DroneReferenceHertz = 55;

    private readonly Dictionary<Cue, Variant[]> banks;

    /// <summary>The looping ambient bed. Played at a rate that sets its pitch.</summary>
    public AudioBuffer Drone { get; }

    private SoundPalette(Dictionary<Cue, Variant[]> banks, AudioBuffer drone)
    {
        this.banks = banks;
        Drone = drone;
    }

    private readonly record struct Variant(AudioBuffer Buffer, double Pitch);

    /// <summary>
    /// Renders the whole palette. Takes a noticeable fraction of a second, so callers run it off
    /// the UI thread — see <see cref="SimulationAudio"/>, which bakes in the background and simply
    /// drops any event that arrives before the palette is ready.
    /// </summary>
    public static SoundPalette Bake(int sampleRate)
    {
        var context = new AudioRenderContext(sampleRate);
        var banks = new Dictionary<Cue, Variant[]>
        {
            [Cue.Eat] = Pitched(context, Cue.Eat, SciFi.Zap),
            [Cue.Death] = Pitched(context, Cue.Death, SciFi.Implode),
            [Cue.Birth] = Pitched(context, Cue.Birth, SciFi.Chime),
            [Cue.Split] = Pitched(context, Cue.Split, SciFi.Fission),

            // Phasing and thrusting are properties of the effect, not of whatever it happens to,
            // so these are one buffer each and always play at the rate they were written.
            [Cue.Phase] = [new Variant(Render(context, SciFi.Shimmer()), 0)],
            [Cue.SpeedBurst] = [new Variant(Render(context, SciFi.Thrust()), 0)],
        };

        return new SoundPalette(banks, Render(context, SciFi.Drone(DroneReferenceHertz, 2.0)));
    }

    private static Variant[] Pitched(AudioRenderContext context, Cue cue, Func<Frequency, ISound> preset)
    {
        double reference = ReferencePitch[cue];
        var variants = new Variant[Bands];

        // Band centres sit at the midpoints of equal slices of the span measured in octaves, so
        // every requested pitch is at most half a band away from one of them.
        double span = HighestFactor / LowestFactor;
        for (int i = 0; i < Bands; i++)
        {
            double factor = LowestFactor * System.Math.Pow(span, (i + 0.5) / Bands);
            double pitch = reference * factor;
            variants[i] = new Variant(Render(context, preset(pitch)), pitch);
        }

        return variants;
    }

    private static AudioBuffer Render(AudioRenderContext context, ISound sound) =>
        sound.Render(context, sound.Duration).SoftClipped();

    /// <summary>
    /// The buffer to play for a cue, and the rate to play it at so it lands on
    /// <paramref name="pitch"/>. Unpitched cues ignore the pitch and come back at rate 1.
    /// </summary>
    public (AudioBuffer Buffer, double Rate) Pick(Cue cue, double pitch)
    {
        Variant[] variants = this.banks[cue];
        if (variants.Length == 1 && variants[0].Pitch <= 0) return (variants[0].Buffer, 1);

        // Nearest in octaves rather than in hertz, because that is how the bands were laid out and
        // how the distance is actually heard.
        Variant best = variants[0];
        double bestDistance = double.MaxValue;
        foreach (Variant variant in variants)
        {
            double distance = System.Math.Abs(System.Math.Log2(pitch / variant.Pitch));
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = variant;
        }

        return (best.Buffer, pitch / best.Pitch);
    }
}
