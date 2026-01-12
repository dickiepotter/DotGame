using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Renders energy bars above particles to display their energy levels.
/// </summary>
public class EnergyBarRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<int, (Rectangle bg, Rectangle fg)> _energyBars;

    public EnergyBarRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _energyBars = new Dictionary<int, (Rectangle, Rectangle)>();
    }

    /// <summary>
    /// Creates energy bar visual elements for a particle.
    /// </summary>
    public void CreateEnergyBar(Particle particle)
    {
        const double barWidth = RenderingConstants.ENERGY_BAR_WIDTH;
        const double barHeight = RenderingConstants.ENERGY_BAR_HEIGHT;

        // Background (red)
        var bgBar = new Rectangle
        {
            Width = barWidth,
            Height = barHeight,
            Fill = Brushes.DarkRed,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };

        // Foreground (green, will change based on energy level)
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

    /// <summary>
    /// Updates the energy bar for a particle.
    /// </summary>
    public void UpdateEnergyBar(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // Create energy bar if it doesn't exist
        if (!_energyBars.TryGetValue(particle.Id, out var bars))
        {
            CreateEnergyBar(particle);
            if (!_energyBars.TryGetValue(particle.Id, out bars))
                return;
        }

        const double barWidth = RenderingConstants.ENERGY_BAR_WIDTH;
        const double barHeight = RenderingConstants.ENERGY_BAR_HEIGHT;
        const double barOffset = RenderingConstants.ENERGY_BAR_OFFSET;

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

        // Color code based on energy thresholds
        if (energyPercent > RenderingConstants.ENERGY_HIGH_THRESHOLD)
            bars.fg.Fill = Brushes.LimeGreen;
        else if (energyPercent > RenderingConstants.ENERGY_MEDIUM_THRESHOLD)
            bars.fg.Fill = Brushes.Yellow;
        else
            bars.fg.Fill = Brushes.OrangeRed;
    }

    /// <summary>
    /// Removes energy bar for a specific particle.
    /// </summary>
    public void RemoveEnergyBar(int particleId)
    {
        if (_energyBars.TryGetValue(particleId, out var bars))
        {
            _canvas.Children.Remove(bars.bg);
            _canvas.Children.Remove(bars.fg);
            _energyBars.Remove(particleId);
        }
    }

    /// <summary>
    /// Clears all energy bars from the canvas.
    /// </summary>
    public void Clear()
    {
        foreach (var bars in _energyBars.Values)
        {
            _canvas.Children.Remove(bars.bg);
            _canvas.Children.Remove(bars.fg);
        }
        _energyBars.Clear();
    }
}
