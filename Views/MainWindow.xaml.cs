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
        // Basic Configuration
        if (int.TryParse(ParticleCountTextBox.Text, out int particleCount))
            _config.ParticleCount = Math.Max(1, Math.Min(500, particleCount));

        if (int.TryParse(SeedTextBox.Text, out int seed))
            _config.RandomSeed = seed;

        if (double.TryParse(SimWidthTextBox.Text, out double simWidth))
            _config.SimulationWidth = simWidth;

        if (double.TryParse(SimHeightTextBox.Text, out double simHeight))
            _config.SimulationHeight = simHeight;

        if (int.TryParse(MaxParticlesTextBox.Text, out int maxParticles))
            _config.MaxParticles = maxParticles;

        // Physics Parameters
        if (double.TryParse(GravityTextBox.Text, out double gravity))
            _config.GravitationalConstant = gravity;

        if (double.TryParse(DampingTextBox.Text, out double damping))
            _config.DampingFactor = damping;

        if (double.TryParse(RestitutionTextBox.Text, out double restitution))
            _config.RestitutionCoefficient = restitution;

        // Particle Ranges
        if (double.TryParse(MinMassTextBox.Text, out double minMass))
            _config.MinMass = minMass;

        if (double.TryParse(MaxMassTextBox.Text, out double maxMass))
            _config.MaxMass = maxMass;

        if (double.TryParse(MinRadiusTextBox.Text, out double minRadius))
            _config.MinRadius = minRadius;

        if (double.TryParse(MaxRadiusTextBox.Text, out double maxRadius))
            _config.MaxRadius = maxRadius;

        if (double.TryParse(MaxVelocityTextBox.Text, out double maxVelocity))
            _config.MaxInitialVelocity = maxVelocity;

        // Physics Toggles
        _config.UseGravity = UseGravityCheckBox.IsChecked ?? true;
        _config.UseCollisions = UseCollisionsCheckBox.IsChecked ?? true;
        _config.UseBoundaries = UseBoundariesCheckBox.IsChecked ?? true;
        _config.UseDamping = UseDampingCheckBox.IsChecked ?? true;
        _config.UseSpatialPartitioning = UseSpatialPartitioningCheckBox.IsChecked ?? true;

        // Ability Toggles
        _config.UseAbilities = UseAbilitiesCheckBox.IsChecked ?? true;
        _config.UseEating = UseEatingCheckBox.IsChecked ?? true;
        _config.UseSplitting = UseSplittingCheckBox.IsChecked ?? true;
        _config.UseReproduction = UseReproductionCheckBox.IsChecked ?? true;
        _config.UsePhasing = UsePhasingCheckBox.IsChecked ?? true;
        _config.UseChase = UseChaseCheckBox.IsChecked ?? true;
        _config.UseFlee = UseFleeCheckBox.IsChecked ?? true;

        // Energy Parameters
        if (double.TryParse(BaseEnergyTextBox.Text, out double baseEnergy))
            _config.BaseEnergyCapacity = baseEnergy;

        if (double.TryParse(PassiveDrainTextBox.Text, out double passiveDrain))
            _config.PassiveEnergyDrain = passiveDrain;

        if (double.TryParse(EatingGainTextBox.Text, out double eatingGain))
            _config.EatingEnergyGain = eatingGain;

        if (double.TryParse(SizeRatioTextBox.Text, out double sizeRatio))
            _config.SizeRatioForEating = sizeRatio;

        if (double.TryParse(VisionRangeTextBox.Text, out double visionRange))
            _config.VisionRangeMultiplier = visionRange;

        if (double.TryParse(HungerThresholdTextBox.Text, out double hungerThreshold))
            _config.HungerThreshold = hungerThreshold;

        // Chase/Flee Parameters
        if (double.TryParse(ChaseForceTextBox.Text, out double chaseForce))
            _config.ChaseForce = chaseForce;

        if (double.TryParse(FleeForceTextBox.Text, out double fleeForce))
            _config.FleeForce = fleeForce;

        if (double.TryParse(ChaseEnergyCostTextBox.Text, out double chaseEnergyCost))
            _config.ChaseEnergyCost = chaseEnergyCost;

        if (double.TryParse(FleeEnergyCostTextBox.Text, out double fleeEnergyCost))
            _config.FleeEnergyCost = fleeEnergyCost;

        // Splitting Parameters
        if (double.TryParse(SplittingEnergyCostTextBox.Text, out double splittingEnergyCost))
            _config.SplittingEnergyCost = splittingEnergyCost;

        if (double.TryParse(SplittingCooldownTextBox.Text, out double splittingCooldown))
            _config.SplittingCooldown = splittingCooldown;

        if (double.TryParse(SplittingSeparationTextBox.Text, out double splittingSeparation))
            _config.SplittingSeparationForce = splittingSeparation;

        // Reproduction Parameters
        if (double.TryParse(ReproductionEnergyCostTextBox.Text, out double reproductionEnergyCost))
            _config.ReproductionEnergyCost = reproductionEnergyCost;

        if (double.TryParse(ReproductionCooldownTextBox.Text, out double reproductionCooldown))
            _config.ReproductionCooldown = reproductionCooldown;

        if (double.TryParse(ReproductionMassTransferTextBox.Text, out double reproductionMassTransfer))
            _config.ReproductionMassTransfer = reproductionMassTransfer;

        if (double.TryParse(ReproductionEnergyTransferTextBox.Text, out double reproductionEnergyTransfer))
            _config.ReproductionEnergyTransfer = reproductionEnergyTransfer;

        // Phasing Parameters
        if (double.TryParse(PhasingEnergyCostTextBox.Text, out double phasingEnergyCost))
            _config.PhasingEnergyCost = phasingEnergyCost;

        if (double.TryParse(PhasingCooldownTextBox.Text, out double phasingCooldown))
            _config.PhasingCooldown = phasingCooldown;

        if (double.TryParse(PhasingDurationTextBox.Text, out double phasingDuration))
            _config.PhasingDuration = phasingDuration;

        // Ability Probabilities
        if (double.TryParse(EatingProbTextBox.Text, out double eatingProb))
            _config.EatingProbability = eatingProb;

        if (double.TryParse(SplittingProbTextBox.Text, out double splittingProb))
            _config.SplittingProbability = splittingProb;

        if (double.TryParse(ReproductionProbTextBox.Text, out double reproductionProb))
            _config.ReproductionProbability = reproductionProb;

        if (double.TryParse(PhasingProbTextBox.Text, out double phasingProb))
            _config.PhasingProbability = phasingProb;

        if (double.TryParse(ChaseProbTextBox.Text, out double chaseProb))
            _config.ChaseProbability = chaseProb;

        if (double.TryParse(FleeProbTextBox.Text, out double fleeProb))
            _config.FleeProbability = fleeProb;

        // Type Distribution
        if (double.TryParse(PredatorProbTextBox.Text, out double predatorProb))
            _config.PredatorProbability = predatorProb;

        if (double.TryParse(HerbivoreProbTextBox.Text, out double herbivoreProb))
            _config.HerbivoreProbability = herbivoreProb;

        if (double.TryParse(SocialProbTextBox.Text, out double socialProb))
            _config.SocialProbability = socialProb;

        if (double.TryParse(SolitaryProbTextBox.Text, out double solitaryProb))
            _config.SolitaryProbability = solitaryProb;

        if (double.TryParse(NeutralProbTextBox.Text, out double neutralProb))
            _config.NeutralProbability = neutralProb;
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
