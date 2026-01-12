using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Renders vision cone overlay for particles with abilities.
/// </summary>
public class VisionConeRenderer
{
    private readonly Canvas _canvas;
    private Ellipse? _visionConeEllipse;

    public VisionConeRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    /// Renders a vision cone for the specified particle.
    /// </summary>
    public void RenderVisionCone(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // Create vision cone element if it doesn't exist
        if (_visionConeEllipse == null)
        {
            _visionConeEllipse = new Ellipse
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = RenderingConstants.VISION_CONE_LINE_THICKNESS,
                Fill = Brushes.Transparent,
                Opacity = RenderingConstants.VISION_CONE_OPACITY
            };
            _canvas.Children.Add(_visionConeEllipse);
            Canvas.SetZIndex(_visionConeEllipse, -50); // Behind particles but above trails
        }

        // Update vision cone size and position
        double range = particle.Abilities.VisionRange;
        _visionConeEllipse.Width = range * 2;
        _visionConeEllipse.Height = range * 2;
        Canvas.SetLeft(_visionConeEllipse, particle.Position.X - range);
        Canvas.SetTop(_visionConeEllipse, particle.Position.Y - range);
        _visionConeEllipse.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the vision cone.
    /// </summary>
    public void HideVisionCone()
    {
        if (_visionConeEllipse != null)
        {
            _visionConeEllipse.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Clears the vision cone element from the canvas.
    /// </summary>
    public void Clear()
    {
        if (_visionConeEllipse != null)
        {
            _canvas.Children.Remove(_visionConeEllipse);
            _visionConeEllipse = null;
        }
    }
}
