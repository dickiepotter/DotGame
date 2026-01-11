using System.Numerics;
using System.Windows;
using System.Windows.Input;
using DotGame.Models;
using DotGame.Simulation;

namespace DotGame.Views;

public partial class MainWindow : Window
{
    private SimulationManager? _simulationManager;
    private SimulationConfig _config;

    // Mouse interaction state
    private Particle? _draggedParticle;
    private Vector2 _lastMousePosition;
    private bool _isDragging;

    public MainWindow()
    {
        InitializeComponent();
        _config = new SimulationConfig();
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        // Update config from UI
        UpdateConfigFromUI();

        // Create simulation manager
        _simulationManager = new SimulationManager(SimulationCanvas, _config);
        _simulationManager.Initialize();

        UpdateInfo();
    }

    private void UpdateConfigFromUI()
    {
        // Parse configuration from UI controls
        if (int.TryParse(ParticleCountTextBox.Text, out int particleCount))
            _config.ParticleCount = Math.Max(1, Math.Min(500, particleCount));

        if (int.TryParse(SeedTextBox.Text, out int seed))
            _config.RandomSeed = seed;

        if (double.TryParse(GravityTextBox.Text, out double gravity))
            _config.GravitationalConstant = gravity;

        _config.UseGravity = UseGravityCheckBox.IsChecked ?? true;
        _config.UseCollisions = UseCollisionsCheckBox.IsChecked ?? true;
        _config.UseBoundaries = UseBoundariesCheckBox.IsChecked ?? true;
        _config.UseDamping = UseDampingCheckBox.IsChecked ?? true;
    }

    private void UpdateInfo()
    {
        if (_simulationManager != null)
        {
            string status = _simulationManager.IsRunning ? "Running" : "Stopped";
            int particleCount = _simulationManager.Particles?.Count ?? _config.ParticleCount;
            InfoTextBlock.Text = $"Status: {status}\n" +
                                $"Particles: {particleCount}\n" +
                                $"Seed: {_config.RandomSeed}";
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _simulationManager?.Start();
        UpdateInfo();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _simulationManager?.Stop();
        UpdateInfo();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        InitializeSimulation();
    }

    // Mouse event handlers
    private void SimulationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_simulationManager == null) return;

        // Get mouse position relative to canvas
        var mousePos = e.GetPosition(SimulationCanvas);
        var position = new Vector2((float)mousePos.X, (float)mousePos.Y);

        // Add a new particle at the clicked position
        _simulationManager.AddParticle(position);

        // Update particle count in info
        if (_simulationManager.Particles != null)
        {
            string status = _simulationManager.IsRunning ? "Running" : "Stopped";
            InfoTextBlock.Text = $"Status: {status}\n" +
                                $"Particles: {_simulationManager.Particles.Count}\n" +
                                $"Seed: {_config.RandomSeed}";
        }
    }

    private void SimulationCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_simulationManager == null) return;

        // Get mouse position relative to canvas
        var mousePos = e.GetPosition(SimulationCanvas);
        var position = new Vector2((float)mousePos.X, (float)mousePos.Y);

        // Find particle at this position
        _draggedParticle = _simulationManager.FindParticleAtPosition(position);

        if (_draggedParticle != null)
        {
            _isDragging = true;
            _lastMousePosition = position;
            SimulationCanvas.CaptureMouse();
        }
    }

    private void SimulationCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _draggedParticle == null || _simulationManager == null)
            return;

        // Get current mouse position
        var mousePos = e.GetPosition(SimulationCanvas);
        var currentPosition = new Vector2((float)mousePos.X, (float)mousePos.Y);

        // Calculate mouse movement delta
        var delta = currentPosition - _lastMousePosition;

        // Apply impulse based on mouse movement
        // Scale factor controls how much impulse is applied
        const float impulseFactor = 50.0f;
        var impulse = delta * impulseFactor;

        _simulationManager.ApplyImpulseToParticle(_draggedParticle, impulse);

        // Update last mouse position
        _lastMousePosition = currentPosition;
    }

    private void SimulationCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _draggedParticle = null;
            SimulationCanvas.ReleaseMouseCapture();
        }
    }
}
