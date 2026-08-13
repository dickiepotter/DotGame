using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DotGame.Models;

namespace DotGame.Rendering;

/// <summary>
/// Renders the simulation as emitted light rather than as drawn shapes.
///
/// Every particle is treated as a small star: a white-hot core, a coloured body, and a wide
/// halo that fades to nothing. Because the <see cref="LightField"/> accumulates additively
/// and tone-maps at the end, a dense cluster genuinely burns brighter than a lone particle
/// and its overlap blooms toward white - no compositing trick, just summed energy.
///
/// Ability state is expressed as light too: hunting flares warm, fleeing snaps cold, phasing
/// goes ghostly and diffuse, a speed burst smears into a comet.
/// </summary>
public sealed class LuminousRenderer
{
    // Chroma applied to a particle's colour before it is emitted, to survive tone compression
    private const float CHROMA_BOOST = 1.55f;

    private readonly Canvas _canvas;
    private readonly LightField _field = new();
    private readonly Image _surface;
    private double _time;

    // --- exposed look controls -------------------------------------------------------
    /// <summary>Overall brightness applied before the tone curve.</summary>
    public float Exposure { get; set; } = 1.15f;

    /// <summary>Display gamma.</summary>
    public float Gamma { get; set; } = 2.2f;

    /// <summary>
    /// Fraction of the previous frame's light retained. Above zero, movement leaves a
    /// decaying wake of light. Exposure is compensated automatically so the steady-state
    /// brightness of a stationary particle does not change.
    /// </summary>
    public float TrailPersistence { get; set; } = 0.0f;

    /// <summary>Multiplier on every glow radius - the "bloom" control.</summary>
    public float GlowScale { get; set; } = 1.0f;

    public bool ShowVisionCones { get; set; }
    public Particle? HoveredParticle { get; set; }

    public LuminousRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _surface = new Image
        {
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        // Bilinear filtering when the field renders below canvas resolution; on a glow this
        // is indistinguishable from rendering at full size and softens it further.
        RenderOptions.SetBitmapScalingMode(_surface, BitmapScalingMode.Linear);
        Panel.SetZIndex(_surface, -100);
    }

    /// <summary>Attaches the light surface to the canvas and hides shape-based visuals.</summary>
    public void Attach()
    {
        if (!_canvas.Children.Contains(_surface))
            _canvas.Children.Add(_surface);
        _surface.Visibility = Visibility.Visible;
    }

    /// <summary>Detaches the light surface, returning the canvas to shape rendering.</summary>
    public void Detach()
    {
        if (_canvas.Children.Contains(_surface))
            _canvas.Children.Remove(_surface);
    }

    public void Render(List<Particle> particles, double deltaTime,
        IReadOnlyList<ParticleExplosion> explosions, IReadOnlyList<ParticleBirth> births)
    {
        double w = _canvas.ActualWidth, h = _canvas.ActualHeight;
        if (w < 1 || h < 1) return;

        if (_field.Resize(w, h))
            _surface.Source = _field.Bitmap;

        _surface.Width = w;
        _surface.Height = h;
        Canvas.SetLeft(_surface, 0);
        Canvas.SetTop(_surface, 0);

        _time += deltaTime;

        // Persistence leaves an imprint of earlier frames; a full clear does not.
        float persistence = Math.Clamp(TrailPersistence, 0f, 0.95f);
        if (persistence > 0.001f) _field.Decay(persistence);
        else _field.Clear();

        if (ShowVisionCones && HoveredParticle != null)
            EmitVisionField(HoveredParticle);

        foreach (var particle in particles)
            EmitParticle(particle);

        foreach (var explosion in explosions)
            EmitExplosion(explosion);

        foreach (var birth in births)
            EmitBirth(birth);

        // A persistent field converges to 1/(1-persistence) times the per-frame contribution,
        // so exposure must be scaled by exactly (1-persistence) to hold brightness steady.
        // Anything softer than that multiplies up: at persistence 0.8 the field is 5x, so a
        // 0.32 factor instead of 0.2 blows the whole frame out to white.
        float exposure = Exposure * (1f - persistence);
        _field.Resolve(exposure, Gamma);
    }

    private void EmitParticle(Particle particle)
    {
        double r = Math.Max(0.6, particle.Radius) * GlowScale;
        Vector2 pos = particle.Position;
        Color baseColor = Saturate(Opaque(particle.Color), CHROMA_BOOST);

        var abilities = particle.HasAbilities ? particle.Abilities : null;
        double energy = particle.HasAbilities ? Math.Clamp(particle.EnergyPercentage, 0, 1) : 1.0;

        // A starving particle is an ember; a full one is a star.
        float vitality = 0.30f + 0.70f * (float)energy;

        bool phasing = abilities?.IsPhasing == true;
        bool birthing = abilities?.IsBirthing == true;

        // Birth fades the newborn in rather than popping it into existence
        float birthFade = 1f;
        if (birthing)
        {
            double p = Math.Clamp(particle.Abilities!.BirthState.TimeRemaining, 0, 10);
            birthFade = 0.35f + 0.65f * (float)Math.Max(0, 1.0 - p / Math.Max(0.001, 1.0));
        }

        float scale = vitality * birthFade;

        // --- motion smear: fast movers stretch into the direction of travel -------------
        float speed = particle.Velocity.Length();
        if (speed > 12f && !phasing)
        {
            float smear = Math.Min(speed * 0.045f, (float)(r * 6));
            var tail = pos - Vector2.Normalize(particle.Velocity) * smear;
            _field.AddStreak(pos.X, pos.Y, tail.X, tail.Y, r * 1.1, baseColor, 0.32f * scale);
        }

        if (phasing)
        {
            // Ghostly: the body all but vanishes and spreads into a cold, diffuse cloud
            _field.AddGlow(pos.X, pos.Y, r * 4.5, Tint(baseColor, Color.FromRgb(150, 190, 255), 0.6f),
                0.13f * scale);
            _field.AddGlow(pos.X, pos.Y, r * 1.6, baseColor, 0.22f * scale);
            _field.AddGlow(pos.X, pos.Y, r * 0.5, Tint(baseColor, Colors.White, 0.4f),
                0.35f * scale, Falloff.Tight);
            return;
        }

        // --- the star itself -------------------------------------------------------------
        // Three nested falloffs. Their sum is what removes any sense of a boundary: the
        // halo is already near zero where the body begins, and the body near zero where the
        // core begins, so brightness slides continuously from bright centre to black space.
        //
        // The core stays small and only lightly whitened. Tinting it hard toward white, or
        // letting the body sprawl, drives every channel to saturation and the whole scene
        // washes to cream - the particle's identity colour has to survive the summation.
        _field.AddGlow(pos.X, pos.Y, r * 2.4, baseColor, 0.15f * scale);
        _field.AddGlow(pos.X, pos.Y, r * 1.05, baseColor, 0.62f * scale);
        _field.AddGlow(pos.X, pos.Y, r * 0.42, Tint(baseColor, Colors.White, 0.18f),
            1.95f * scale, Falloff.Tight);

        if (abilities == null) return;

        // --- state auras -----------------------------------------------------------------
        // Auras are wide, dim glows rather than rings. A ring has two visible boundaries and
        // reads as an outline, which is precisely what this mode exists to avoid.
        float pulse = 0.65f + 0.35f * (float)Math.Sin(_time * 7.0 + particle.Id * 0.7);

        switch (abilities.CurrentState)
        {
            case AbilityState.Hunting:
                // Predatory flare - warm, pulsing, reaching outward
                _field.AddGlow(pos.X, pos.Y, r * 3.4, Color.FromRgb(255, 110, 35), 0.26f * pulse);
                break;

            case AbilityState.Fleeing:
                // Cold panic - a tight, bright, anxious shimmer
                _field.AddGlow(pos.X, pos.Y, r * 2.8, Color.FromRgb(140, 230, 255), 0.32f * pulse);
                break;

            case AbilityState.Eating:
                _field.AddGlow(pos.X, pos.Y, r * 1.9, Color.FromRgb(255, 215, 130), 0.55f);
                break;

            case AbilityState.Reproducing:
            case AbilityState.Splitting:
                _field.AddGlow(pos.X, pos.Y, r * 2.6, Color.FromRgb(170, 255, 195), 0.34f);
                break;
        }

        // Speed burst reads as a comet: a long luminous wake plus a hot leading edge
        if (abilities.IsSpeedBoosted && speed > 1f)
        {
            var dir = Vector2.Normalize(particle.Velocity);
            var tail = pos - dir * Math.Min(speed * 0.16f, (float)(r * 14));
            _field.AddStreak(pos.X, pos.Y, tail.X, tail.Y, r * 1.5,
                Tint(baseColor, Color.FromRgb(255, 245, 200), 0.55f), 0.70f);
            _field.AddGlow(pos.X + dir.X * r, pos.Y + dir.Y * r, r * 1.2, Colors.White, 1.2f, Falloff.Tight);
        }

        // Growth and shrinkage colour the particle's own light rather than drawing a shell.
        // The threshold is well above the classic mode's 0.1: energy-mass conversion nudges
        // the radius almost every frame, so a lower bar would flag nearly every particle at
        // once and the cue would carry no information.
        double radiusDelta = particle.Radius - particle.PreviousRadius;
        if (Math.Abs(radiusDelta) > 0.35)
        {
            bool growing = radiusDelta > 0;
            _field.AddGlow(pos.X, pos.Y, r * 2.1,
                growing ? Color.FromRgb(120, 255, 230) : Color.FromRgb(255, 140, 60),
                0.30f * pulse);
        }
    }

    private void EmitExplosion(ParticleExplosion explosion)
    {
        float fade = (float)(1.0 - explosion.Progress);
        if (fade <= 0f) return;
        float f2 = fade * fade;

        // Central flash that expands and dies
        _field.AddGlow(explosion.Position.X, explosion.Position.Y,
            explosion.Radius * (1.5 + 5.0 * explosion.Progress) * GlowScale,
            Color.FromRgb(255, 200, 120), 1.6f * f2);

        // Each fragment is its own small hot light, streaked along its flight
        foreach (var fragment in explosion.Fragments)
        {
            var tail = fragment.Position - fragment.Velocity * 0.05f;
            _field.AddStreak(fragment.Position.X, fragment.Position.Y, tail.X, tail.Y,
                Math.Max(1.0, fragment.Size * 2.2) * GlowScale,
                Tint(fragment.Color, Color.FromRgb(255, 230, 170), 0.5f), 1.5f * f2);
        }
    }

    private void EmitBirth(ParticleBirth birth)
    {
        float progress = (float)birth.EasedProgress;
        float fade = 1f - progress;
        if (fade <= 0f) return;

        // Motes of light converging on the new particle
        foreach (var fragment in birth.Fragments)
        {
            var p = fragment.GetCurrentPosition(birth.EasedProgress);
            _field.AddGlow(p.X, p.Y, Math.Max(1.0, fragment.Size * 2.5) * GlowScale,
                Tint(fragment.Color, Colors.White, 0.4f), 1.1f * fade, Falloff.Tight);
        }

        // Expanding shell announcing the arrival. This is the one place a ring earns its
        // keep - a shockwave is genuinely an annulus - so it is kept thick and soft enough
        // to read as a breath of light rather than a drawn circle.
        var pos = birth.GetCurrentPosition();
        double shell = birth.TargetRadius * (0.5 + 2.5 * progress) * GlowScale;
        _field.AddRing(pos.X, pos.Y, shell, Math.Max(2.0, shell * 0.55),
            Tint(birth.Color, Colors.White, 0.5f), 0.55f * fade * fade);
    }

    private void EmitVisionField(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // A barely-there wash showing how far this particle can perceive, with a soft rim
        // so the boundary is legible without becoming a drawn circle.
        double range = particle.Abilities!.VisionRange;
        _field.AddGlow(particle.Position.X, particle.Position.Y, range, Color.FromRgb(90, 160, 255), 0.05f);
        _field.AddRing(particle.Position.X, particle.Position.Y, range, Math.Max(3.0, range * 0.10),
            Color.FromRgb(140, 200, 255), 0.16f);
    }

    /// <summary>
    /// Pushes a colour away from its own luminance, increasing chroma.
    ///
    /// Needed because summing three glows and then compressing through the tone curve pulls
    /// every channel toward its ceiling, which drains saturation: a muted green particle
    /// resolves to pale grey-green. Boosting chroma on the way in leaves the particle still
    /// recognisably its own colour on the way out.
    /// </summary>
    private static Color Saturate(Color c, float amount)
    {
        float lum = 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;
        return Color.FromRgb(
            (byte)Math.Clamp(lum + (c.R - lum) * amount, 0f, 255f),
            (byte)Math.Clamp(lum + (c.G - lum) * amount, 0f, 255f),
            (byte)Math.Clamp(lum + (c.B - lum) * amount, 0f, 255f));
    }

    /// <summary>Blends a colour toward another by <paramref name="amount"/> (0-1).</summary>
    private static Color Tint(Color from, Color to, float amount)
    {
        float a = Math.Clamp(amount, 0f, 1f);
        return Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * a),
            (byte)(from.G + (to.G - from.G) * a),
            (byte)(from.B + (to.B - from.B) * a));
    }

    /// <summary>
    /// Discards a colour's alpha. Transparency is meaningless for an emitter - a phasing
    /// particle is dimmer and more diffuse, not see-through, and that is handled explicitly.
    /// </summary>
    private static Color Opaque(Color c) => Color.FromRgb(c.R, c.G, c.B);
}
