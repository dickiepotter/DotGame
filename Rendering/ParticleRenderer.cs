using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;

namespace DotGame.Rendering;

public class ParticleRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, Ellipse> _visualElements;
    private readonly Dictionary<int, Queue<Vector2>> _particleTrails;
    private readonly List<Polyline> _trailPolylines;
    private readonly List<ParticleExplosion> _activeExplosions;
    private readonly Dictionary<ParticleExplosion, List<Ellipse>> _explosionElements;

    // Birth animation tracking
    private readonly List<ParticleBirth> _activeBirths;
    private readonly Dictionary<ParticleBirth, List<Ellipse>> _birthElements;
    private readonly Dictionary<int, ParticleBirth> _particleBirthMap;

    // Grid overlay elements
    private readonly List<Line> _gridLines;

    // Vision cone element
    private Ellipse? _visionConeEllipse;

    // Energy bar elements
    private readonly Dictionary<int, (Rectangle bg, Rectangle fg)> _energyBars;

    // Visual settings
    public bool ShowGrid { get; set; }
    public bool ShowVisionCones { get; set; }
    public bool ShowTrails { get; set; }
    public bool ShowEnergyBars { get; set; } = true;
    public int TrailLength { get; set; } = 15;

    // Hovered particle for vision cone
    public Particle? HoveredParticle { get; set; }

    public ParticleRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _visualElements = new Dictionary<int, Ellipse>();
        _particleTrails = new Dictionary<int, Queue<Vector2>>();
        _trailPolylines = new List<Polyline>();
        _activeExplosions = new List<ParticleExplosion>();
        _explosionElements = new Dictionary<ParticleExplosion, List<Ellipse>>();
        _activeBirths = new List<ParticleBirth>();
        _birthElements = new Dictionary<ParticleBirth, List<Ellipse>>();
        _particleBirthMap = new Dictionary<int, ParticleBirth>();
        _gridLines = new List<Line>();
        _energyBars = new Dictionary<int, (Rectangle, Rectangle)>();
    }

    public void Initialize(List<Particle> particles)
    {
        // Clear existing elements
        _canvas.Children.Clear();
        _visualElements.Clear();
        _particleTrails.Clear();
        _trailPolylines.Clear();
        _energyBars.Clear();
        _gridLines.Clear();
        _visionConeEllipse = null; // Reset vision cone so it can be recreated

        // Create grid if enabled
        if (ShowGrid)
        {
            CreateGrid();
        }

        // Create an Ellipse for each particle
        foreach (var particle in particles)
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

            // Initialize trail
            if (ShowTrails)
            {
                _particleTrails[particle.Id] = new Queue<Vector2>();
            }

            // Initialize energy bar
            if (ShowEnergyBars && particle.HasAbilities)
            {
                CreateEnergyBar(particle);
            }
        }
    }

    private void CreateGrid()
    {
        const double gridSpacing = 50;
        var canvasWidth = _canvas.ActualWidth;
        var canvasHeight = _canvas.ActualHeight;

        if (canvasWidth == 0 || canvasHeight == 0) return;

        // Vertical lines
        for (double x = 0; x <= canvasWidth; x += gridSpacing)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = canvasHeight,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.5,
                Opacity = 0.5
            };
            _canvas.Children.Add(line);
            _gridLines.Add(line);
            Canvas.SetZIndex(line, -1000); // Behind everything
        }

        // Horizontal lines
        for (double y = 0; y <= canvasHeight; y += gridSpacing)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = canvasWidth,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.5,
                Opacity = 0.5
            };
            _canvas.Children.Add(line);
            _gridLines.Add(line);
            Canvas.SetZIndex(line, -1000); // Behind everything
        }
    }

    private void CreateEnergyBar(Particle particle)
    {
        double barWidth = particle.Radius * 2;
        double barHeight = 4;

        // Background (red)
        var bgBar = new Rectangle
        {
            Width = barWidth,
            Height = barHeight,
            Fill = Brushes.DarkRed,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };

        // Foreground (green)
        var fgBar = new Rectangle
        {
            Width = barWidth,
            Height = barHeight,
            Fill = Brushes.LimeGreen,
            Stroke = null
        };

        _canvas.Children.Add(bgBar);
        _canvas.Children.Add(fgBar);
        Canvas.SetZIndex(bgBar, 1000); // On top
        Canvas.SetZIndex(fgBar, 1001); // On top of background

        _energyBars[particle.Id] = (bgBar, fgBar);
    }

    public void Render(List<Particle> particles, double deltaTime = 0)
    {
        // Update grid visibility
        if (ShowGrid && _gridLines.Count == 0)
        {
            CreateGrid();
        }
        else if (!ShowGrid && _gridLines.Count > 0)
        {
            foreach (var line in _gridLines)
            {
                _canvas.Children.Remove(line);
            }
            _gridLines.Clear();
        }

        // Detect new birthing particles and create birth animations
        if (deltaTime > 0)
        {
            DetectAndCreateBirthAnimations(particles);
        }

        // Update position and appearance of each particle's visual representation
        foreach (var particle in particles)
        {
            if (_visualElements.TryGetValue(particle.Id, out var ellipse))
            {
                Vector2 renderPosition = particle.Position;
                double renderRadius = particle.Radius;

                // If particle is birthing, use animated position and size
                if (particle.HasAbilities && particle.Abilities.IsBirthing &&
                    _particleBirthMap.TryGetValue(particle.Id, out var birth))
                {
                    renderPosition = birth.GetCurrentPosition();
                    renderRadius = birth.GetCurrentRadius();

                    // Add transparency during birth
                    double alpha = 0.3 + (0.7 * birth.EasedProgress);
                    byte alphaValue = (byte)(alpha * 255);
                    ellipse.Fill = new SolidColorBrush(Color.FromArgb(
                        alphaValue, particle.Color.R, particle.Color.G, particle.Color.B));
                }
                else
                {
                    // Normal rendering
                    ellipse.Fill = new SolidColorBrush(particle.Color);
                }

                // Set position (Canvas.Left and Canvas.Top are for top-left corner)
                // Subtract radius to center the ellipse on the particle position
                Canvas.SetLeft(ellipse, renderPosition.X - renderRadius);
                Canvas.SetTop(ellipse, renderPosition.Y - renderRadius);

                // Update size and color (in case they changed)
                ellipse.Width = renderRadius * 2;
                ellipse.Height = renderRadius * 2;

                // Update trail
                if (ShowTrails)
                {
                    UpdateTrail(particle);
                }

                // Update energy bar
                if (ShowEnergyBars && particle.HasAbilities)
                {
                    UpdateEnergyBar(particle);
                }
            }
        }

        // Remove visual elements for particles that no longer exist
        var particleIds = new HashSet<int>(particles.Select(p => p.Id));
        var toRemove = _visualElements.Keys.Where(id => !particleIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            if (_visualElements.TryGetValue(id, out var ellipse))
            {
                _canvas.Children.Remove(ellipse);
                _visualElements.Remove(id);
            }

            if (_energyBars.TryGetValue(id, out var bars))
            {
                _canvas.Children.Remove(bars.bg);
                _canvas.Children.Remove(bars.fg);
                _energyBars.Remove(id);
            }

            _particleTrails.Remove(id);
        }

        // Render trails
        if (ShowTrails)
        {
            RenderTrails();
        }
        else
        {
            ClearTrails();
        }

        // Render vision cone
        if (ShowVisionCones && HoveredParticle != null)
        {
            RenderVisionCone(HoveredParticle);
        }
        else
        {
            HideVisionCone();
        }

        // Update explosions
        if (deltaTime > 0)
        {
            UpdateExplosions(deltaTime);
            UpdateBirthAnimations(deltaTime);
        }
    }

    private void UpdateTrail(Particle particle)
    {
        if (!_particleTrails.TryGetValue(particle.Id, out var trail))
        {
            trail = new Queue<Vector2>();
            _particleTrails[particle.Id] = trail;
        }

        // Add current position
        trail.Enqueue(particle.Position);

        // Keep only the last N positions
        while (trail.Count > TrailLength)
        {
            trail.Dequeue();
        }
    }

    private void RenderTrails()
    {
        // Clear existing polylines
        foreach (var polyline in _trailPolylines)
        {
            _canvas.Children.Remove(polyline);
        }
        _trailPolylines.Clear();

        // Create new polylines for each trail
        foreach (var kvp in _particleTrails)
        {
            var trail = kvp.Value;
            if (trail.Count < 2) continue;

            var polyline = new Polyline
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                Opacity = 0.3
            };

            foreach (var pos in trail)
            {
                polyline.Points.Add(new System.Windows.Point(pos.X, pos.Y));
            }

            _canvas.Children.Add(polyline);
            Canvas.SetZIndex(polyline, -100); // Behind particles
            _trailPolylines.Add(polyline);
        }
    }

    private void ClearTrails()
    {
        foreach (var polyline in _trailPolylines)
        {
            _canvas.Children.Remove(polyline);
        }
        _trailPolylines.Clear();
        _particleTrails.Clear();
    }

    private void RenderVisionCone(Particle particle)
    {
        if (!particle.HasAbilities) return;

        if (_visionConeEllipse == null)
        {
            _visionConeEllipse = new Ellipse
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                Opacity = 0.5
            };
            _canvas.Children.Add(_visionConeEllipse);
            Canvas.SetZIndex(_visionConeEllipse, -50); // Behind particles but above trails
        }

        double range = particle.Abilities.VisionRange;
        _visionConeEllipse.Width = range * 2;
        _visionConeEllipse.Height = range * 2;
        Canvas.SetLeft(_visionConeEllipse, particle.Position.X - range);
        Canvas.SetTop(_visionConeEllipse, particle.Position.Y - range);
        _visionConeEllipse.Visibility = Visibility.Visible;
    }

    private void HideVisionCone()
    {
        if (_visionConeEllipse != null)
        {
            _visionConeEllipse.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateEnergyBar(Particle particle)
    {
        if (!_energyBars.TryGetValue(particle.Id, out var bars))
        {
            CreateEnergyBar(particle);
            if (!_energyBars.TryGetValue(particle.Id, out bars))
                return;
        }

        const double barWidth = 30;
        const double barHeight = 4;
        const double barOffset = 8;

        // Position above particle
        double x = particle.Position.X - barWidth / 2;
        double y = particle.Position.Y - particle.Radius - barOffset;

        Canvas.SetLeft(bars.bg, x);
        Canvas.SetTop(bars.bg, y);
        Canvas.SetLeft(bars.fg, x);
        Canvas.SetTop(bars.fg, y);

        // Update foreground width based on energy percentage
        double energyPercent = particle.Abilities.Energy / particle.Abilities.MaxEnergy;
        energyPercent = Math.Clamp(energyPercent, 0, 1);
        bars.fg.Width = barWidth * energyPercent;

        // Color code: green > 60%, yellow > 30%, red otherwise
        if (energyPercent > 0.6)
            bars.fg.Fill = Brushes.LimeGreen;
        else if (energyPercent > 0.3)
            bars.fg.Fill = Brushes.Yellow;
        else
            bars.fg.Fill = Brushes.OrangeRed;
    }

    public void AddExplosion(Particle particle)
    {
        var explosion = new ParticleExplosion(particle);
        _activeExplosions.Add(explosion);

        // Create visual elements for explosion fragments
        var fragmentElements = new List<Ellipse>();
        foreach (var fragment in explosion.Fragments)
        {
            var ellipse = new Ellipse
            {
                Width = fragment.Size * 2,
                Height = fragment.Size * 2,
                Fill = new SolidColorBrush(fragment.Color),
                Stroke = null
            };

            Canvas.SetLeft(ellipse, fragment.Position.X - fragment.Size);
            Canvas.SetTop(ellipse, fragment.Position.Y - fragment.Size);

            _canvas.Children.Add(ellipse);
            fragmentElements.Add(ellipse);
        }

        _explosionElements[explosion] = fragmentElements;
    }

    private void UpdateExplosions(double deltaTime)
    {
        var completedExplosions = new List<ParticleExplosion>();

        foreach (var explosion in _activeExplosions)
        {
            explosion.Update(deltaTime);

            if (explosion.IsComplete)
            {
                completedExplosions.Add(explosion);
            }
            else
            {
                // Update fragment visual positions and fade out
                if (_explosionElements.TryGetValue(explosion, out var elements))
                {
                    double alpha = 1.0 - explosion.Progress;
                    byte alphaValue = (byte)(alpha * 255);

                    for (int i = 0; i < explosion.Fragments.Count && i < elements.Count; i++)
                    {
                        var fragment = explosion.Fragments[i];
                        var ellipse = elements[i];

                        Canvas.SetLeft(ellipse, fragment.Position.X - fragment.Size);
                        Canvas.SetTop(ellipse, fragment.Position.Y - fragment.Size);

                        // Fade out
                        var color = fragment.Color;
                        ellipse.Fill = new SolidColorBrush(Color.FromArgb(
                            alphaValue, color.R, color.G, color.B));
                    }
                }
            }
        }

        // Remove completed explosions
        foreach (var explosion in completedExplosions)
        {
            if (_explosionElements.TryGetValue(explosion, out var elements))
            {
                foreach (var ellipse in elements)
                {
                    _canvas.Children.Remove(ellipse);
                }
                _explosionElements.Remove(explosion);
            }
            _activeExplosions.Remove(explosion);
        }
    }

    private void DetectAndCreateBirthAnimations(List<Particle> particles)
    {
        foreach (var particle in particles)
        {
            // Check if this particle is birthing and doesn't have an animation yet
            if (particle.HasAbilities && particle.Abilities.IsBirthing &&
                !_particleBirthMap.ContainsKey(particle.Id))
            {
                // Find parent position if available
                Vector2? parentPosition = null;
                if (particle.Abilities.ParentParticleId.HasValue)
                {
                    var parent = particles.FirstOrDefault(p => p.Id == particle.Abilities.ParentParticleId.Value);
                    if (parent != null)
                    {
                        parentPosition = parent.Position;
                    }
                }

                AddBirthAnimation(particle, parentPosition);
            }
        }
    }

    public void AddBirthAnimation(Particle particle, Vector2? parentPosition = null)
    {
        var birth = new ParticleBirth(particle, parentPosition);
        _activeBirths.Add(birth);
        _particleBirthMap[particle.Id] = birth;

        // Create visual elements for birth fragments
        var fragmentElements = new List<Ellipse>();
        foreach (var fragment in birth.Fragments)
        {
            var ellipse = new Ellipse
            {
                Width = fragment.Size * 2,
                Height = fragment.Size * 2,
                Fill = new SolidColorBrush(Color.FromArgb(128, fragment.Color.R, fragment.Color.G, fragment.Color.B)),
                Stroke = null
            };

            Canvas.SetLeft(ellipse, fragment.StartPosition.X - fragment.Size);
            Canvas.SetTop(ellipse, fragment.StartPosition.Y - fragment.Size);

            _canvas.Children.Add(ellipse);
            Canvas.SetZIndex(ellipse, 100); // Above normal particles
            fragmentElements.Add(ellipse);
        }

        _birthElements[birth] = fragmentElements;
    }

    private void UpdateBirthAnimations(double deltaTime)
    {
        var completedBirths = new List<ParticleBirth>();

        foreach (var birth in _activeBirths)
        {
            birth.Update(deltaTime);

            if (birth.IsComplete)
            {
                completedBirths.Add(birth);
            }
            else
            {
                // Update fragment visual positions and fade in
                if (_birthElements.TryGetValue(birth, out var elements))
                {
                    double alpha = 0.8 * (1.0 - birth.Progress);
                    byte alphaValue = (byte)(alpha * 255);

                    for (int i = 0; i < birth.Fragments.Count && i < elements.Count; i++)
                    {
                        var fragment = birth.Fragments[i];
                        var ellipse = elements[i];

                        Vector2 currentPos = fragment.GetCurrentPosition(birth.EasedProgress);
                        Canvas.SetLeft(ellipse, currentPos.X - fragment.Size);
                        Canvas.SetTop(ellipse, currentPos.Y - fragment.Size);

                        // Fade out fragments as they converge
                        var color = fragment.Color;
                        ellipse.Fill = new SolidColorBrush(Color.FromArgb(
                            alphaValue, color.R, color.G, color.B));
                    }
                }
            }
        }

        // Remove completed birth animations
        foreach (var birth in completedBirths)
        {
            if (_birthElements.TryGetValue(birth, out var elements))
            {
                foreach (var ellipse in elements)
                {
                    _canvas.Children.Remove(ellipse);
                }
                _birthElements.Remove(birth);
            }
            _activeBirths.Remove(birth);
            _particleBirthMap.Remove(birth.ParticleId);
        }
    }
}
