using System.Collections.Generic;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Renders motion trails behind particles.
/// </summary>
public class TrailRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, Queue<Vector2>> _particleTrails;
    private readonly List<Polyline> _trailPolylines;

    public int TrailLength { get; set; } = 15;

    public TrailRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _particleTrails = new Dictionary<int, Queue<Vector2>>();
        _trailPolylines = new List<Polyline>();
    }

    /// <summary>
    /// Updates the trail for a particle with its current position.
    /// </summary>
    public void UpdateTrail(Particle particle)
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

    /// <summary>
    /// Renders all particle trails as polylines.
    /// </summary>
    public void RenderTrails()
    {
        // Clear existing polylines
        ClearPolylines();

        // Create new polylines for each trail
        foreach (var kvp in _particleTrails)
        {
            var trail = kvp.Value;
            if (trail.Count < 2) continue;

            var polyline = new Polyline
            {
                Stroke = Brushes.Gray,
                StrokeThickness = RenderingConstants.TRAIL_LINE_THICKNESS,
                Opacity = RenderingConstants.TRAIL_OPACITY
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

    /// <summary>
    /// Removes trail data for a specific particle.
    /// </summary>
    public void RemoveTrail(int particleId)
    {
        _particleTrails.Remove(particleId);
    }

    /// <summary>
    /// Clears all trails and visual elements.
    /// </summary>
    public void Clear()
    {
        ClearPolylines();
        _particleTrails.Clear();
    }

    /// <summary>
    /// Clears polyline visual elements from canvas.
    /// </summary>
    private void ClearPolylines()
    {
        foreach (var polyline in _trailPolylines)
        {
            _canvas.Children.Remove(polyline);
        }
        _trailPolylines.Clear();
    }
}
