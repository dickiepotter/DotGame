using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;

namespace DotGame.Rendering;

public class ParticleRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, Ellipse> _visualElements;
    private readonly List<ParticleExplosion> _activeExplosions;
    private readonly Dictionary<ParticleExplosion, List<Ellipse>> _explosionElements;

    public ParticleRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _visualElements = new Dictionary<int, Ellipse>();
        _activeExplosions = new List<ParticleExplosion>();
        _explosionElements = new Dictionary<ParticleExplosion, List<Ellipse>>();
    }

    public void Initialize(List<Particle> particles)
    {
        // Clear existing elements
        _canvas.Children.Clear();
        _visualElements.Clear();

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
        }
    }

    public void Render(List<Particle> particles, double deltaTime = 0)
    {
        // Update position and appearance of each particle's visual representation
        foreach (var particle in particles)
        {
            if (_visualElements.TryGetValue(particle.Id, out var ellipse))
            {
                // Set position (Canvas.Left and Canvas.Top are for top-left corner)
                // Subtract radius to center the ellipse on the particle position
                Canvas.SetLeft(ellipse, particle.Position.X - particle.Radius);
                Canvas.SetTop(ellipse, particle.Position.Y - particle.Radius);

                // Update size and color (in case they changed)
                ellipse.Width = particle.Radius * 2;
                ellipse.Height = particle.Radius * 2;
                ellipse.Fill = new SolidColorBrush(particle.Color);
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
        }

        // Update explosions
        if (deltaTime > 0)
        {
            UpdateExplosions(deltaTime);
        }
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
}
