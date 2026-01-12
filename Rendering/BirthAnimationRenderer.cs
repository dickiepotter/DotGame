using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;

namespace DotGame.Rendering;

/// <summary>
/// Renders birth animations for newly created particles.
/// </summary>
public class BirthAnimationRenderer
{
    private readonly Canvas _canvas;
    private readonly List<ParticleBirth> _activeBirths;
    private readonly Dictionary<ParticleBirth, List<Ellipse>> _birthElements;
    private readonly Dictionary<int, ParticleBirth> _particleBirthMap;

    public BirthAnimationRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _activeBirths = new List<ParticleBirth>();
        _birthElements = new Dictionary<ParticleBirth, List<Ellipse>>();
        _particleBirthMap = new Dictionary<int, ParticleBirth>();
    }

    /// <summary>
    /// Detects particles that need birth animations and creates them.
    /// </summary>
    public void DetectAndCreateBirthAnimations(List<Particle> particles)
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

    /// <summary>
    /// Adds a birth animation for a particle.
    /// </summary>
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

    /// <summary>
    /// Updates all active birth animations.
    /// </summary>
    public void UpdateBirthAnimations(double deltaTime)
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

    /// <summary>
    /// Gets the birth animation for a specific particle if it exists.
    /// </summary>
    public ParticleBirth? GetBirthAnimation(int particleId)
    {
        return _particleBirthMap.TryGetValue(particleId, out var birth) ? birth : null;
    }

    /// <summary>
    /// Clears all birth animations.
    /// </summary>
    public void Clear()
    {
        foreach (var elements in _birthElements.Values)
        {
            foreach (var ellipse in elements)
            {
                _canvas.Children.Remove(ellipse);
            }
        }
        _activeBirths.Clear();
        _birthElements.Clear();
        _particleBirthMap.Clear();
    }
}
