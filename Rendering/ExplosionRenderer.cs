using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;

namespace DotGame.Rendering;

/// <summary>
/// Renders explosion animations when particles are destroyed.
/// </summary>
public class ExplosionRenderer
{
    private readonly Canvas _canvas;
    private readonly List<ParticleExplosion> _activeExplosions;
    private readonly Dictionary<ParticleExplosion, List<Ellipse>> _explosionElements;

    public ExplosionRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _activeExplosions = new List<ParticleExplosion>();
        _explosionElements = new Dictionary<ParticleExplosion, List<Ellipse>>();
    }

    /// <summary>
    /// Adds an explosion animation for a particle.
    /// </summary>
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

    /// <summary>
    /// Updates all active explosion animations.
    /// </summary>
    public void UpdateExplosions(double deltaTime)
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

    /// <summary>
    /// Clears all explosion animations.
    /// </summary>
    public void Clear()
    {
        foreach (var elements in _explosionElements.Values)
        {
            foreach (var ellipse in elements)
            {
                _canvas.Children.Remove(ellipse);
            }
        }
        _activeExplosions.Clear();
        _explosionElements.Clear();
    }
}
