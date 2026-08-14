using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DotGame.Models;

namespace DotGame.Rendering;

/// <summary>
/// Hosts a <see cref="LightScene"/> on a WPF canvas: owns the bitmap surface, sizes the light
/// field to the canvas, and resolves each composed frame to pixels.
///
/// All decisions about what light a particle emits live in <see cref="LightScene"/>, which
/// has no dependency on a window and can therefore be verified directly.
/// </summary>
public sealed class LuminousRenderer
{
    private readonly Canvas _canvas;
    private readonly LightField _field = new();
    private readonly LightScene _scene = new();
    private readonly Image _surface;

    /// <summary>Overall brightness applied before the tone curve.</summary>
    public float Exposure
    {
        get => _scene.Exposure;
        set => _scene.Exposure = value;
    }

    /// <summary>Display gamma. Shared with the scene so identity colours resolve exactly.</summary>
    public float Gamma
    {
        get => _scene.Gamma;
        set => _scene.Gamma = value;
    }

    /// <summary>
    /// Fraction of the previous frame's light retained. Above zero, movement leaves a
    /// decaying wake. Exposure is compensated automatically so the steady-state brightness of
    /// a stationary particle does not change.
    /// </summary>
    public float TrailPersistence { get; set; } = 0.0f;

    /// <summary>
    /// On-screen pixels per world unit. The world is a fixed size scaled to fit the window,
    /// so this tells the field how much resolution to render at to stay crisp when zoomed.
    /// </summary>
    public double ViewScale { get; set; } = 1.0;

    /// <summary>Multiplier on every glow radius - the "bloom" control.</summary>
    public float GlowScale
    {
        get => _scene.GlowScale;
        set => _scene.GlowScale = value;
    }

    // Visual tab overlays, all honoured in this mode as well as in Classic
    public bool ShowGrid { get => _scene.ShowGrid; set => _scene.ShowGrid = value; }
    public bool ShowEnergyBars { get => _scene.ShowEnergyBars; set => _scene.ShowEnergyBars = value; }
    public bool ShowTrails { get => _scene.ShowTrails; set => _scene.ShowTrails = value; }
    public bool ShowVisionCones { get => _scene.ShowVisionCones; set => _scene.ShowVisionCones = value; }
    public Particle? HoveredParticle { get => _scene.HoveredParticle; set => _scene.HoveredParticle = value; }

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

    /// <summary>Attaches the light surface to the canvas.</summary>
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
        IReadOnlyList<ParticleExplosion> explosions, IReadOnlyList<ParticleBirth> births,
        IReadOnlyDictionary<int, Queue<Vector2>>? trails)
    {
        double w = _canvas.ActualWidth, h = _canvas.ActualHeight;
        if (w < 1 || h < 1) return;

        if (_field.Resize(w, h, ViewScale))
            _surface.Source = _field.Bitmap;

        _surface.Width = w;
        _surface.Height = h;
        Canvas.SetLeft(_surface, 0);
        Canvas.SetTop(_surface, 0);

        _scene.Advance(deltaTime);

        // Persistence leaves an imprint of earlier frames; a full clear does not.
        float persistence = Math.Clamp(TrailPersistence, 0f, 0.95f);
        if (persistence > 0.001f) _field.Decay(persistence);
        else _field.Clear();

        _scene.Compose(_field, particles, explosions, births, trails, w, h);

        // A persistent field converges to 1/(1-persistence) times the per-frame contribution,
        // so exposure must be scaled by exactly (1-persistence) to hold brightness steady.
        float exposure = Exposure * (1f - persistence);
        _field.Resolve(exposure, Gamma);
    }
}
