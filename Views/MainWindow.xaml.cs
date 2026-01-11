using System.Windows;
using DotGame.Models;
using DotGame.Simulation;

namespace DotGame.Views;

public partial class MainWindow : Window
{
    private SimulationManager? _simulationManager;
    private SimulationConfig _config;

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
            InfoTextBlock.Text = $"Status: {status}\n" +
                                $"Particles: {_config.ParticleCount}\n" +
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
}
