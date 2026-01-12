using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DotGame.Utilities;

namespace DotGame.Rendering;

/// <summary>
/// Renders a grid overlay on the canvas for visual reference.
/// </summary>
public class GridRenderer
{
    private readonly Canvas _canvas;
    private readonly List<Line> _gridLines;

    public GridRenderer(Canvas canvas)
    {
        _canvas = canvas;
        _gridLines = new List<Line>();
    }

    /// <summary>
    /// Creates and displays the grid overlay.
    /// </summary>
    public void CreateGrid()
    {
        ClearGrid();

        const double gridSpacing = RenderingConstants.GRID_SPACING;
        var canvasWidth = _canvas.ActualWidth;
        var canvasHeight = _canvas.ActualHeight;

        if (canvasWidth == 0 || canvasHeight == 0) return;

        // Create vertical lines
        for (double x = 0; x <= canvasWidth; x += gridSpacing)
        {
            var line = CreateGridLine(x, 0, x, canvasHeight);
            _canvas.Children.Add(line);
            _gridLines.Add(line);
        }

        // Create horizontal lines
        for (double y = 0; y <= canvasHeight; y += gridSpacing)
        {
            var line = CreateGridLine(0, y, canvasWidth, y);
            _canvas.Children.Add(line);
            _gridLines.Add(line);
        }
    }

    /// <summary>
    /// Removes all grid lines from the canvas.
    /// </summary>
    public void ClearGrid()
    {
        foreach (var line in _gridLines)
        {
            _canvas.Children.Remove(line);
        }
        _gridLines.Clear();
    }

    /// <summary>
    /// Creates a single grid line with standard styling.
    /// </summary>
    private Line CreateGridLine(double x1, double y1, double x2, double y2)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brushes.LightGray,
            StrokeThickness = 0.5,
            Opacity = 0.5
        };
        Canvas.SetZIndex(line, -1000); // Behind everything
        return line;
    }
}
