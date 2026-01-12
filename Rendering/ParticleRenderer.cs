using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Coordinates all particle rendering using specialized renderer components.
/// Manages core particle visuals and delegates specialized rendering to focused classes.
/// </summary>
public class ParticleRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, Ellipse> _visualElements;

    // Specialized renderers
    private readonly GridRenderer _gridRenderer;
    private readonly EnergyBarRenderer _energyBarRenderer;
    private readonly TrailRenderer _trailRenderer;
    private readonly VisionConeRenderer _visionConeRenderer;
    private readonly ExplosionRenderer _explosionRenderer;
    private readonly BirthAnimationRenderer _birthAnimationRenderer;

    // Visual settings
    public bool ShowGrid { get; set; }
    public bool ShowVisionCones { get; set; }
    public bool ShowTrails { get; set; }
    public bool ShowEnergyBars { get; set; } = true;

    public int TrailLength
    {
        get => _trailRenderer.TrailLength;
        set => _trailRenderer.TrailLength = value;
    }

    // Hovered particle for vision cone
    public Particle? HoveredParticle { get; set; }

    public ParticleRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _visualElements = new Dictionary<int, Ellipse>();

        // Initialize specialized renderers
        _gridRenderer = new GridRenderer(canvas);
        _energyBarRenderer = new EnergyBarRenderer(canvas);
        _trailRenderer = new TrailRenderer(canvas);
        _visionConeRenderer = new VisionConeRenderer(canvas);
        _explosionRenderer = new ExplosionRenderer(canvas);
        _birthAnimationRenderer = new BirthAnimationRenderer(canvas);
    }

    /// <summary>
    /// Initializes the renderer with a list of particles.
    /// </summary>
    public void Initialize(List<Particle> particles)
    {
        // Clear existing elements
        _canvas.Children.Clear();
        _visualElements.Clear();
        _energyBarRenderer.Clear();
        _trailRenderer.Clear();
        _visionConeRenderer.Clear();
        _explosionRenderer.Clear();
        _birthAnimationRenderer.Clear();

        // Create grid if enabled
        if (ShowGrid)
        {
            _gridRenderer.CreateGrid();
        }

        // Create visual elements for each particle
        foreach (var particle in particles)
        {
            CreateParticleVisual(particle);
        }
    }

    /// <summary>
    /// Creates visual elements for a single particle.
    /// </summary>
    private void CreateParticleVisual(Particle particle)
    {
        var ellipse = new Ellipse
        {
            Width = particle.Radius * 2,
            Height = particle.Radius * 2,
            Fill = new SolidColorBrush(particle.Color),
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        _canvas.Children.Add(ellipse);
        _visualElements[particle.Id] = ellipse;

        // Initialize energy bar if needed
        if (ShowEnergyBars && particle.HasAbilities)
        {
            _energyBarRenderer.CreateEnergyBar(particle);
        }
    }

    /// <summary>
    /// Renders all particles and updates animations.
    /// </summary>
    public void Render(List<Particle> particles, double deltaTime = 0)
    {
        // Update grid visibility
        UpdateGridVisibility();

        // Detect new birthing particles and create birth animations
        if (deltaTime > 0)
        {
            _birthAnimationRenderer.DetectAndCreateBirthAnimations(particles);
        }

        // Get current particle IDs
        var currentParticleIds = new HashSet<int>(particles.Select(p => p.Id));

        // Remove visual elements for particles that no longer exist
        var elementsToRemove = _visualElements.Keys.Where(id => !currentParticleIds.Contains(id)).ToList();
        foreach (var id in elementsToRemove)
        {
            if (_visualElements.TryGetValue(id, out var ellipse))
            {
                _canvas.Children.Remove(ellipse);
                _visualElements.Remove(id);
            }
            _energyBarRenderer.RemoveEnergyBar(id);
            _trailRenderer.RemoveTrail(id);
        }

        // Render each particle
        foreach (var particle in particles)
        {
            RenderParticle(particle);
        }

        // Render trails if enabled
        if (ShowTrails)
        {
            _trailRenderer.RenderTrails();
        }

        // Render vision cone for hovered particle
        if (ShowVisionCones && HoveredParticle != null)
        {
            _visionConeRenderer.RenderVisionCone(HoveredParticle);
        }
        else
        {
            _visionConeRenderer.HideVisionCone();
        }

        // Update animations
        if (deltaTime > 0)
        {
            _explosionRenderer.UpdateExplosions(deltaTime);
            _birthAnimationRenderer.UpdateBirthAnimations(deltaTime);
        }
    }

    /// <summary>
    /// Renders a single particle with appropriate effects.
    /// </summary>
    private void RenderParticle(Particle particle)
    {
        if (!_visualElements.TryGetValue(particle.Id, out var ellipse))
        {
            CreateParticleVisual(particle);
            if (!_visualElements.TryGetValue(particle.Id, out ellipse))
                return;
        }

        Vector2 renderPosition = particle.Position;
        double renderRadius = particle.Radius;

        // Apply birth animation if active
        if (particle.HasAbilities && particle.Abilities.IsBirthing)
        {
            var birth = _birthAnimationRenderer.GetBirthAnimation(particle.Id);
            if (birth != null)
            {
                renderPosition = birth.GetCurrentPosition();
                renderRadius = birth.GetCurrentRadius();

                // Add transparency during birth
                double alpha = RenderingConstants.BIRTH_MIN_OPACITY +
                              ((RenderingConstants.BIRTH_MAX_OPACITY - RenderingConstants.BIRTH_MIN_OPACITY) * birth.EasedProgress);
                byte alphaValue = (byte)(alpha * 255);
                ellipse.Fill = new SolidColorBrush(Color.FromArgb(
                    alphaValue, particle.Color.R, particle.Color.G, particle.Color.B));
            }
        }
        else
        {
            // Normal rendering
            ellipse.Fill = new SolidColorBrush(particle.Color);
        }

        // Update phasing transparency
        if (particle.HasAbilities && particle.Abilities.IsPhasing)
        {
            var color = particle.Color;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(
                RenderingConstants.PHASING_OPACITY, color.R, color.G, color.B));
        }

        // Update size and position
        ellipse.Width = renderRadius * 2;
        ellipse.Height = renderRadius * 2;
        Canvas.SetLeft(ellipse, renderPosition.X - renderRadius);
        Canvas.SetTop(ellipse, renderPosition.Y - renderRadius);

        // Add visual effect for growth/shrinkage
        double radiusDiff = particle.Radius - particle.PreviousRadius;
        if (Math.Abs(radiusDiff) > 0.1)
        {
            if (radiusDiff > 0)
            {
                // Growing - green/cyan glow
                ellipse.Stroke = Brushes.Cyan;
                ellipse.StrokeThickness = 3;
            }
            else
            {
                // Shrinking - red/orange glow
                ellipse.Stroke = Brushes.OrangeRed;
                ellipse.StrokeThickness = 3;
            }
        }
        else
        {
            // Normal state - thin black outline
            ellipse.Stroke = Brushes.Black;
            ellipse.StrokeThickness = 1;
        }

        // Update trail
        if (ShowTrails)
        {
            _trailRenderer.UpdateTrail(particle);
        }

        // Update energy bar
        if (ShowEnergyBars && particle.HasAbilities)
        {
            _energyBarRenderer.UpdateEnergyBar(particle);
        }
    }

    /// <summary>
    /// Updates grid visibility based on settings.
    /// </summary>
    private void UpdateGridVisibility()
    {
        if (ShowGrid)
        {
            _gridRenderer.CreateGrid();
        }
        else
        {
            _gridRenderer.ClearGrid();
        }
    }

    /// <summary>
    /// Adds an explosion animation for a particle.
    /// </summary>
    public void AddExplosion(Particle particle)
    {
        _explosionRenderer.AddExplosion(particle);
    }

    /// <summary>
    /// Adds a birth animation for a particle.
    /// </summary>
    public void AddBirthAnimation(Particle particle, Vector2? parentPosition = null)
    {
        _birthAnimationRenderer.AddBirthAnimation(particle, parentPosition);
    }
}
