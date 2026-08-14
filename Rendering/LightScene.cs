using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Media;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Composes a frame of the simulation into a <see cref="LightField"/> as emitted light.
///
/// Deliberately free of any WPF surface: it takes particles and writes energy, which keeps
/// the whole look testable without a window and lets the resolved colour of a particle be
/// checked against the documented palette.
/// </summary>
public sealed class LightScene
{
    /// <summary>Overall brightness applied before the tone curve.</summary>
    public float Exposure { get; set; } = 1.15f;

    /// <summary>Display gamma. Must match what the field resolves with, or identity
    /// colours will not come back out the way they went in.</summary>
    public float Gamma { get; set; } = 2.2f;

    /// <summary>Multiplier on every glow radius.</summary>
    public float GlowScale { get; set; } = 1.0f;

    // Overlays, mirroring the Visual tab toggles
    public bool ShowGrid { get; set; }
    public bool ShowEnergyBars { get; set; } = true;
    public bool ShowTrails { get; set; }
    public bool ShowVisionCones { get; set; }
    public Particle? HoveredParticle { get; set; }

    private double _time;

    /// <summary>Advances animation time. Pulses are driven from simulated time.</summary>
    public void Advance(double deltaTime) => _time += deltaTime;

    public void Compose(LightField field, List<Particle> particles,
        IReadOnlyList<ParticleExplosion> explosions, IReadOnlyList<ParticleBirth> births,
        IReadOnlyDictionary<int, Queue<Vector2>>? trails, double canvasWidth, double canvasHeight)
    {
        if (ShowGrid) EmitGrid(field, canvasWidth, canvasHeight);

        if (ShowVisionCones && HoveredParticle != null) EmitVisionField(field, HoveredParticle);

        if (ShowTrails && trails != null) EmitTrails(field, particles, trails);

        foreach (var particle in particles) EmitParticle(field, particle);

        if (ShowEnergyBars)
            foreach (var particle in particles) EmitEnergyBar(field, particle);

        foreach (var explosion in explosions) EmitExplosion(field, explosion);
        foreach (var birth in births) EmitBirth(field, birth);
    }

    // ---------------------------------------------------------------- particles

    private void EmitParticle(LightField field, Particle particle)
    {
        double r = Math.Max(0.6, particle.Radius) * GlowScale;
        Vector2 pos = particle.Position;

        // particle.Color is the documented identity colour: type hue, ability tints, and the
        // brightness already scaled by energy. It is reproduced rather than reinterpreted.
        Color identity = Opaque(particle.Color);

        var abilities = particle.HasAbilities ? particle.Abilities : null;
        double energy = particle.HasAbilities ? Math.Clamp(particle.EnergyPercentage, 0, 1) : 1.0;

        bool phasing = abilities?.IsPhasing == true;

        // Solve for the energy that resolves to exactly this colour. Emitting an arbitrary
        // intensity instead would let the tone curve pull every channel toward its ceiling,
        // which drains saturation and makes a red predator and a green herbivore both read
        // as pale cream.
        float er = LightField.LinearForDisplay(identity.R / 255f, Exposure, Gamma);
        float eg = LightField.LinearForDisplay(identity.G / 255f, Exposure, Gamma);
        float eb = LightField.LinearForDisplay(identity.B / 255f, Exposure, Gamma);

        if (phasing)
        {
            // Ghostly: dim and spread out, but still recognisably its own colour
            field.AddGlowLinear(pos.X, pos.Y, r * 4.0, er * 0.10f, eg * 0.10f, eb * 0.14f);
            field.AddGlowLinear(pos.X, pos.Y, r * 1.5, er * 0.30f, eg * 0.30f, eb * 0.36f);
            return;
        }

        // Motion smear for fast movers
        float speed = particle.Velocity.Length();
        if (speed > 12f)
        {
            float smear = Math.Min(speed * 0.045f, (float)(r * 6));
            var tail = pos - Vector2.Normalize(particle.Velocity) * smear;
            field.AddStreak(pos.X, pos.Y, tail.X, tail.Y, r * 1.1, identity, 0.30f);
        }

        // Halo, then body at full identity energy. The halo shares the hue, so the particle
        // reads as one colour fading into the dark rather than as a disc with a rim.
        field.AddGlowLinear(pos.X, pos.Y, r * 2.6, er * 0.16f, eg * 0.16f, eb * 0.16f);
        field.AddGlowLinear(pos.X, pos.Y, r * 1.05, er, eg, eb);

        // A small hot centre. Kept tight and only lightly whitened so it reads as a bright
        // core without bleaching the identity colour out of the particle.
        float coreBoost = 0.55f + 0.75f * (float)energy;
        var coreColor = Tint(identity, Colors.White, 0.25f);
        field.AddGlow(pos.X, pos.Y, r * 0.38, coreColor, coreBoost, Falloff.Tight);

        if (abilities == null) return;

        // State auras: wide dim glows, never rings, so nothing acquires an outline
        float pulse = 0.65f + 0.35f * (float)Math.Sin(_time * 7.0 + particle.Id * 0.7);

        switch (abilities.CurrentState)
        {
            case AbilityState.Hunting:
                field.AddGlow(pos.X, pos.Y, r * 3.4, Color.FromRgb(255, 110, 35), 0.22f * pulse);
                break;
            case AbilityState.Fleeing:
                field.AddGlow(pos.X, pos.Y, r * 2.8, Color.FromRgb(140, 230, 255), 0.28f * pulse);
                break;
            case AbilityState.Eating:
                field.AddGlow(pos.X, pos.Y, r * 1.9, Color.FromRgb(255, 215, 130), 0.45f);
                break;
            case AbilityState.Reproducing:
            case AbilityState.Splitting:
                field.AddGlow(pos.X, pos.Y, r * 2.6, Color.FromRgb(170, 255, 195), 0.30f);
                break;
        }

        if (abilities.IsSpeedBoosted && speed > 1f)
        {
            var dir = Vector2.Normalize(particle.Velocity);
            var tail = pos - dir * Math.Min(speed * 0.16f, (float)(r * 14));
            field.AddStreak(pos.X, pos.Y, tail.X, tail.Y, r * 1.5,
                Tint(identity, Color.FromRgb(255, 245, 200), 0.5f), 0.60f);
            field.AddGlow(pos.X + dir.X * r, pos.Y + dir.Y * r, r * 1.1, Colors.White, 1.0f, Falloff.Tight);
        }

        double radiusDelta = particle.Radius - particle.PreviousRadius;
        if (Math.Abs(radiusDelta) > 0.35)
        {
            bool growing = radiusDelta > 0;
            field.AddGlow(pos.X, pos.Y, r * 2.1,
                growing ? Color.FromRgb(120, 255, 230) : Color.FromRgb(255, 140, 60), 0.26f * pulse);
        }
    }

    // ---------------------------------------------------------------- overlays

    /// <summary>
    /// The reference grid, as faint rules of light. Same spacing as the Classic overlay so
    /// distances read identically between the two modes.
    /// </summary>
    private void EmitGrid(LightField field, double width, double height)
    {
        var color = Color.FromRgb(150, 190, 230);
        const float intensity = 0.055f;
        double spacing = RenderingConstants.GRID_SPACING;

        for (double x = spacing; x < width; x += spacing)
            field.AddSegment(x, 0, x, height, 1.1, color, intensity);

        for (double y = spacing; y < height; y += spacing)
            field.AddSegment(0, y, width, y, 1.1, color, intensity);
    }

    /// <summary>
    /// Energy bars above each particle, using the same dimensions and the same green/yellow/red
    /// thresholds as Classic mode so the reading transfers between modes.
    /// </summary>
    private void EmitEnergyBar(LightField field, Particle particle)
    {
        if (!particle.HasAbilities) return;

        double w = RenderingConstants.ENERGY_BAR_WIDTH;
        double h = RenderingConstants.ENERGY_BAR_HEIGHT;
        double y = particle.Position.Y - particle.Radius - RenderingConstants.ENERGY_BAR_OFFSET;
        double left = particle.Position.X - w / 2;

        double pct = Math.Clamp(particle.EnergyPercentage, 0, 1);

        // Empty portion, dim so the full width is legible without competing with the particle
        field.AddSegment(left, y, left + w, y, h, Color.FromRgb(120, 40, 40), 0.16f);

        if (pct <= 0.001) return;

        Color fill = pct > RenderingConstants.ENERGY_HIGH_THRESHOLD
            ? Color.FromRgb(60, 255, 90)
            : pct > RenderingConstants.ENERGY_MEDIUM_THRESHOLD
                ? Color.FromRgb(255, 235, 60)
                : Color.FromRgb(255, 110, 40);

        field.AddSegment(left, y, left + w * pct, y, h, fill, 0.75f);
    }

    /// <summary>
    /// Motion trails as fading light, drawn from the recorded history so the path matches
    /// what Classic mode draws. Older samples are dimmer and thinner.
    /// </summary>
    private void EmitTrails(LightField field, List<Particle> particles,
        IReadOnlyDictionary<int, Queue<Vector2>> trails)
    {
        var colours = new Dictionary<int, Color>(particles.Count);
        var radii = new Dictionary<int, double>(particles.Count);
        foreach (var p in particles)
        {
            colours[p.Id] = Opaque(p.Color);
            radii[p.Id] = p.Radius;
        }

        foreach (var kvp in trails)
        {
            if (kvp.Value.Count < 2) continue;
            if (!colours.TryGetValue(kvp.Key, out var colour)) continue;
            double radius = radii.TryGetValue(kvp.Key, out var rr) ? rr : 4.0;

            var points = kvp.Value.ToArray();   // oldest first
            for (int i = 1; i < points.Length; i++)
            {
                // Newer segments are brighter and wider
                float age = i / (float)(points.Length - 1);
                float intensity = 0.30f * age * age;
                double thickness = Math.Max(1.0, radius * 0.5 * age);

                field.AddSegment(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y,
                    thickness, colour, intensity);
            }
        }
    }

    private void EmitVisionField(LightField field, Particle particle)
    {
        if (!particle.HasAbilities) return;

        double range = particle.Abilities!.VisionRange;
        field.AddGlow(particle.Position.X, particle.Position.Y, range, Color.FromRgb(90, 160, 255), 0.05f);
        field.AddRing(particle.Position.X, particle.Position.Y, range, Math.Max(3.0, range * 0.10),
            Color.FromRgb(140, 200, 255), 0.16f);
    }

    // ---------------------------------------------------------------- effects

    private void EmitExplosion(LightField field, ParticleExplosion explosion)
    {
        float fade = (float)(1.0 - explosion.Progress);
        if (fade <= 0f) return;
        float f2 = fade * fade;

        field.AddGlow(explosion.Position.X, explosion.Position.Y,
            explosion.Radius * (1.5 + 5.0 * explosion.Progress) * GlowScale,
            Color.FromRgb(255, 200, 120), 1.4f * f2);

        foreach (var fragment in explosion.Fragments)
        {
            var tail = fragment.Position - fragment.Velocity * 0.05f;
            field.AddStreak(fragment.Position.X, fragment.Position.Y, tail.X, tail.Y,
                Math.Max(1.0, fragment.Size * 2.2) * GlowScale,
                Tint(fragment.Color, Color.FromRgb(255, 230, 170), 0.5f), 1.3f * f2);
        }
    }

    private void EmitBirth(LightField field, ParticleBirth birth)
    {
        float progress = (float)birth.EasedProgress;
        float fade = 1f - progress;
        if (fade <= 0f) return;

        foreach (var fragment in birth.Fragments)
        {
            var p = fragment.GetCurrentPosition(birth.EasedProgress);
            field.AddGlow(p.X, p.Y, Math.Max(1.0, fragment.Size * 2.5) * GlowScale,
                Tint(fragment.Color, Colors.White, 0.4f), 1.0f * fade, Falloff.Tight);
        }

        var pos = birth.GetCurrentPosition();
        double shell = birth.TargetRadius * (0.5 + 2.5 * progress) * GlowScale;
        field.AddRing(pos.X, pos.Y, shell, Math.Max(2.0, shell * 0.55),
            Tint(birth.Color, Colors.White, 0.5f), 0.5f * fade * fade);
    }

    // ---------------------------------------------------------------- helpers

    private static Color Tint(Color from, Color to, float amount)
    {
        float a = Math.Clamp(amount, 0f, 1f);
        return Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * a),
            (byte)(from.G + (to.G - from.G) * a),
            (byte)(from.B + (to.B - from.B) * a));
    }

    /// <summary>
    /// Discards alpha. Transparency is meaningless for an emitter - a phasing particle is
    /// dimmer and more diffuse, not see-through, and that is handled explicitly.
    /// </summary>
    private static Color Opaque(Color c) => Color.FromRgb(c.R, c.G, c.B);
}
