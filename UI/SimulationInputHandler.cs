using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DotGame.Models;
using DotGame.Simulation;
using DotGame.Rendering;

namespace DotGame.UI;

/// <summary>
/// Handles mouse and touch input for simulation interactions.
/// Extracts input handling logic from MainWindow to improve separation of concerns.
/// </summary>
public class SimulationInputHandler
{
    private readonly Canvas _canvas;
    private readonly SimulationManager _simulationManager;
    private readonly ParticleTooltipManager _tooltipManager;

    // Mouse interaction state
    private Particle? _draggedParticle;
    private Vector2 _lastMousePosition;
    private bool _isDragging;

    // Touch interaction state
    private Particle? _touchedParticle;
    private Vector2 _lastTouchPosition;
    private bool _isTouchDragging;
    private int? _activeTouchId;

    // Impulse factor for dragging particles
    private const float IMPULSE_FACTOR = 50.0f;

    public SimulationInputHandler(
        Canvas canvas,
        SimulationManager simulationManager,
        ParticleTooltipManager tooltipManager)
    {
        _canvas = canvas;
        _simulationManager = simulationManager;
        _tooltipManager = tooltipManager;
    }

    #region Mouse Handlers

    /// <summary>
    /// Handles left mouse button down - adds a new particle at click position.
    /// </summary>
    public void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        var position = GetMousePosition(e);
        _simulationManager.AddParticle(position);
    }

    /// <summary>
    /// Handles right mouse button down - starts dragging a particle.
    /// </summary>
    public void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        var position = GetMousePosition(e);
        _draggedParticle = _simulationManager.FindParticleAtPosition(position);

        if (_draggedParticle != null)
        {
            _isDragging = true;
            _lastMousePosition = position;
            _canvas.CaptureMouse();
        }
    }

    /// <summary>
    /// Handles mouse movement - drags particles or shows tooltips.
    /// </summary>
    public void OnMouseMove(MouseEventArgs e)
    {
        var currentPosition = GetMousePosition(e);
        var mousePos = e.GetPosition(_canvas);

        if (_isDragging && _draggedParticle != null)
        {
            // Handle particle dragging
            HandleParticleDrag(currentPosition);
            _tooltipManager.Hide();
        }
        else
        {
            // Handle hover tooltip and vision cone
            HandleMouseHover(currentPosition, mousePos);
        }
    }

    /// <summary>
    /// Handles right mouse button up - stops dragging.
    /// </summary>
    public void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _draggedParticle = null;
            _canvas.ReleaseMouseCapture();
        }
    }

    #endregion

    #region Touch Handlers

    /// <summary>
    /// Handles touch down - starts dragging a particle or adds a new one.
    /// </summary>
    public void OnTouchDown(TouchEventArgs e)
    {
        // Only handle one touch at a time
        if (_activeTouchId.HasValue) return;

        var touchPoint = e.GetTouchPoint(_canvas);
        var position = GetTouchPosition(touchPoint);

        var particle = _simulationManager.FindParticleAtPosition(position);

        if (particle != null)
        {
            // Start dragging the touched particle
            _touchedParticle = particle;
            _isTouchDragging = true;
            _lastTouchPosition = position;
            _activeTouchId = e.TouchDevice.Id;

            // Show tooltip at touch position
            var canvasBounds = new Rect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight);
            _tooltipManager.Show(particle, touchPoint.Position, canvasBounds);

            e.TouchDevice.Capture(_canvas);
            e.Handled = true;
        }
        else
        {
            // Add new particle at touch position
            _simulationManager.AddParticle(position);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles touch movement - drags the touched particle.
    /// </summary>
    public void OnTouchMove(TouchEventArgs e)
    {
        // Only handle the active touch
        if (!_activeTouchId.HasValue || e.TouchDevice.Id != _activeTouchId.Value)
            return;

        var touchPoint = e.GetTouchPoint(_canvas);
        var currentPosition = GetTouchPosition(touchPoint);

        if (_isTouchDragging && _touchedParticle != null)
        {
            // Calculate and apply impulse
            var delta = currentPosition - _lastTouchPosition;
            var impulse = delta * IMPULSE_FACTOR;
            _simulationManager.ApplyImpulseToParticle(_touchedParticle, impulse);

            _lastTouchPosition = currentPosition;

            // Update tooltip to follow touch
            var canvasBounds = new Rect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight);
            _tooltipManager.Show(_touchedParticle, touchPoint.Position, canvasBounds);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles touch up - stops dragging and hides tooltip.
    /// </summary>
    public void OnTouchUp(TouchEventArgs e)
    {
        // Only handle the active touch
        if (!_activeTouchId.HasValue || e.TouchDevice.Id != _activeTouchId.Value)
            return;

        if (_isTouchDragging)
        {
            _isTouchDragging = false;
            _touchedParticle = null;
            _activeTouchId = null;

            _tooltipManager.Hide();
            e.TouchDevice.Capture(null);
            e.Handled = true;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the mouse position relative to the canvas as a Vector2.
    /// </summary>
    private Vector2 GetMousePosition(MouseEventArgs e)
    {
        var pos = e.GetPosition(_canvas);
        return new Vector2((float)pos.X, (float)pos.Y);
    }

    /// <summary>
    /// Gets the touch position relative to the canvas as a Vector2.
    /// </summary>
    private Vector2 GetTouchPosition(TouchPoint touchPoint)
    {
        return new Vector2((float)touchPoint.Position.X, (float)touchPoint.Position.Y);
    }

    /// <summary>
    /// Handles dragging a particle with the mouse.
    /// </summary>
    private void HandleParticleDrag(Vector2 currentPosition)
    {
        if (_draggedParticle == null) return;

        var delta = currentPosition - _lastMousePosition;
        var impulse = delta * IMPULSE_FACTOR;

        _simulationManager.ApplyImpulseToParticle(_draggedParticle, impulse);
        _lastMousePosition = currentPosition;
    }

    /// <summary>
    /// Handles mouse hover - shows tooltip and updates vision cone display.
    /// </summary>
    private void HandleMouseHover(Vector2 position, Point mousePos)
    {
        var hoveredParticle = _simulationManager.FindParticleAtPosition(position);

        // Update hovered particle for vision cone rendering
        _simulationManager.Renderer.HoveredParticle = hoveredParticle;

        if (hoveredParticle != null)
        {
            var canvasBounds = new Rect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight);
            _tooltipManager.Show(hoveredParticle, mousePos, canvasBounds);
        }
        else
        {
            _tooltipManager.Hide();
        }
    }

    #endregion
}
