using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;

namespace DotGame.Rendering;

public class ParticleRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, Ellipse> _visualElements;

    public ParticleRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _visualElements = new Dictionary<int, Ellipse>();
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

    public void Render(List<Particle> particles)
    {
        // Update position of each particle's visual representation
        foreach (var particle in particles)
        {
            if (_visualElements.TryGetValue(particle.Id, out var ellipse))
            {
                // Set position (Canvas.Left and Canvas.Top are for top-left corner)
                // Subtract radius to center the ellipse on the particle position
                Canvas.SetLeft(ellipse, particle.Position.X - particle.Radius);
                Canvas.SetTop(ellipse, particle.Position.Y - particle.Radius);
            }
        }
    }
}
