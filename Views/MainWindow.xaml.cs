using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using DotGame.Models;
using DotGame.Simulation;
using System.Collections.Generic;

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
        if (_simulationManager == null) return;

        // Get current mouse position
        var mousePos = e.GetPosition(SimulationCanvas);
        var currentPosition = new Vector2((float)mousePos.X, (float)mousePos.Y);

        // Handle dragging behavior
        if (_isDragging && _draggedParticle != null)
        {
            // Calculate mouse movement delta
            var delta = currentPosition - _lastMousePosition;

            // Apply impulse based on mouse movement
            // Scale factor controls how much impulse is applied
            const float impulseFactor = 50.0f;
            var impulse = delta * impulseFactor;

            _simulationManager.ApplyImpulseToParticle(_draggedParticle, impulse);

            // Update last mouse position
            _lastMousePosition = currentPosition;

            // Hide tooltip while dragging
            ParticleTooltip.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Show particle details on hover
            var hoveredParticle = _simulationManager.FindParticleAtPosition(currentPosition);

            if (hoveredParticle != null)
            {
                ShowParticleTooltip(hoveredParticle, mousePos);
            }
            else
            {
                ParticleTooltip.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void ShowParticleTooltip(Particle particle, System.Windows.Point mousePos)
    {
        // Format particle details
        var details = FormatParticleDetails(particle);
        TooltipText.Text = details;

        // Position tooltip near mouse cursor with offset
        const double offsetX = 15;
        const double offsetY = 15;
        double left = mousePos.X + offsetX;
        double top = mousePos.Y + offsetY;

        // Keep tooltip within canvas bounds
        if (left + ParticleTooltip.ActualWidth > SimulationCanvas.ActualWidth)
            left = mousePos.X - ParticleTooltip.ActualWidth - offsetX;

        if (top + ParticleTooltip.ActualHeight > SimulationCanvas.ActualHeight)
            top = mousePos.Y - ParticleTooltip.ActualHeight - offsetY;

        ParticleTooltip.Margin = new Thickness(left, top, 0, 0);
        ParticleTooltip.Visibility = Visibility.Visible;
    }

    private string FormatParticleDetails(Particle particle)
    {
        var details = $"╔══ Particle #{particle.Id} ══╗\n";
        details += $"Position: ({particle.Position.X:F1}, {particle.Position.Y:F1})\n";
        details += $"Velocity: ({particle.Velocity.X:F1}, {particle.Velocity.Y:F1})\n";
        details += $"Speed: {particle.Velocity.Length():F1}\n";
        details += $"Mass: {particle.Mass:F2}\n";
        details += $"Radius: {particle.Radius:F1}\n";

        if (particle.HasAbilities && particle.Abilities != null)
        {
            details += $"\n╠══ Abilities ══╣\n";
            details += $"Type: {particle.Abilities.Type}\n";
            details += $"State: {particle.Abilities.CurrentState}\n";
            details += $"Energy: {particle.Abilities.Energy:F1}/{particle.Abilities.MaxEnergy:F1} ({particle.EnergyPercentage:P0})\n";
            details += $"Generation: {particle.Abilities.Generation}\n";
            details += $"Vision Range: {particle.Abilities.VisionRange:F1}\n";

            // List abilities
            var abilityList = new List<string>();
            if (particle.Abilities.HasAbility(AbilitySet.Eating)) abilityList.Add("Eating");
            if (particle.Abilities.HasAbility(AbilitySet.Splitting)) abilityList.Add("Splitting");
            if (particle.Abilities.HasAbility(AbilitySet.Reproduction)) abilityList.Add("Reproduction");
            if (particle.Abilities.HasAbility(AbilitySet.Phasing)) abilityList.Add("Phasing");
            if (particle.Abilities.HasAbility(AbilitySet.Chase)) abilityList.Add("Chase");
            if (particle.Abilities.HasAbility(AbilitySet.Flee)) abilityList.Add("Flee");

            if (abilityList.Count > 0)
            {
                details += $"Has: {string.Join(", ", abilityList)}\n";
            }

            // Show special states
            if (particle.Abilities.IsPhasing)
                details += $"PHASING: {particle.Abilities.PhasingTimeRemaining:F1}s\n";
            if (particle.Abilities.IsSpeedBoosted)
                details += $"SPEED BOOST: {particle.Abilities.SpeedBoostTimeRemaining:F1}s\n";
            if (particle.Abilities.IsCamouflaged)
                details += $"CAMOUFLAGED: {particle.Abilities.CamouflageTimeRemaining:F1}s\n";
        }

        details += "╚" + new string('═', 20) + "╝";

        return details;
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

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Update config from current UI values
        UpdateConfigFromUI();

        // Show save file dialog
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = "DotGameSettings.json",
            Title = "Save Settings"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // Serialize config to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_config, options);

                // Write to file
                File.WriteAllText(saveFileDialog.FileName, json);

                MessageBox.Show($"Settings saved successfully to:\n{saveFileDialog.FileName}",
                    "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings:\n{ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Show open file dialog
        var openFileDialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            Title = "Load Settings"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Read from file
                string json = File.ReadAllText(openFileDialog.FileName);

                // Deserialize config from JSON
                var loadedConfig = JsonSerializer.Deserialize<SimulationConfig>(json);

                if (loadedConfig != null)
                {
                    _config = loadedConfig;
                    PopulateUIFromConfig();

                    MessageBox.Show($"Settings loaded successfully from:\n{openFileDialog.FileName}",
                        "Settings Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to load settings: Invalid file format.",
                        "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings:\n{ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PopulateUIFromConfig()
    {
        // Basic Configuration
        ParticleCountTextBox.Text = _config.ParticleCount.ToString();
        SeedTextBox.Text = _config.RandomSeed.ToString();
        SimWidthTextBox.Text = _config.SimulationWidth.ToString();
        SimHeightTextBox.Text = _config.SimulationHeight.ToString();
        MaxParticlesTextBox.Text = _config.MaxParticles.ToString();

        // Physics Parameters
        GravityTextBox.Text = _config.GravitationalConstant.ToString();
        DampingTextBox.Text = _config.DampingFactor.ToString();
        RestitutionTextBox.Text = _config.RestitutionCoefficient.ToString();

        // Particle Ranges
        MinMassTextBox.Text = _config.MinMass.ToString();
        MaxMassTextBox.Text = _config.MaxMass.ToString();
        MinRadiusTextBox.Text = _config.MinRadius.ToString();
        MaxRadiusTextBox.Text = _config.MaxRadius.ToString();
        MaxVelocityTextBox.Text = _config.MaxInitialVelocity.ToString();

        // Physics Toggles
        UseGravityCheckBox.IsChecked = _config.UseGravity;
        UseCollisionsCheckBox.IsChecked = _config.UseCollisions;
        UseBoundariesCheckBox.IsChecked = _config.UseBoundaries;
        UseDampingCheckBox.IsChecked = _config.UseDamping;
        UseSpatialPartitioningCheckBox.IsChecked = _config.UseSpatialPartitioning;

        // Ability Toggles
        UseAbilitiesCheckBox.IsChecked = _config.UseAbilities;
        UseEatingCheckBox.IsChecked = _config.UseEating;
        UseSplittingCheckBox.IsChecked = _config.UseSplitting;
        UseReproductionCheckBox.IsChecked = _config.UseReproduction;
        UsePhasingCheckBox.IsChecked = _config.UsePhasing;
        UseChaseCheckBox.IsChecked = _config.UseChase;
        UseFleeCheckBox.IsChecked = _config.UseFlee;

        // Energy Parameters
        BaseEnergyTextBox.Text = _config.BaseEnergyCapacity.ToString();
        PassiveDrainTextBox.Text = _config.PassiveEnergyDrain.ToString();
        EatingGainTextBox.Text = _config.EatingEnergyGain.ToString();
        SizeRatioTextBox.Text = _config.SizeRatioForEating.ToString();
        VisionRangeTextBox.Text = _config.VisionRangeMultiplier.ToString();
        HungerThresholdTextBox.Text = _config.HungerThreshold.ToString();

        // Chase/Flee Parameters
        ChaseForceTextBox.Text = _config.ChaseForce.ToString();
        FleeForceTextBox.Text = _config.FleeForce.ToString();
        ChaseEnergyCostTextBox.Text = _config.ChaseEnergyCost.ToString();
        FleeEnergyCostTextBox.Text = _config.FleeEnergyCost.ToString();

        // Splitting Parameters
        SplittingEnergyCostTextBox.Text = _config.SplittingEnergyCost.ToString();
        SplittingCooldownTextBox.Text = _config.SplittingCooldown.ToString();
        SplittingSeparationTextBox.Text = _config.SplittingSeparationForce.ToString();

        // Reproduction Parameters
        ReproductionEnergyCostTextBox.Text = _config.ReproductionEnergyCost.ToString();
        ReproductionCooldownTextBox.Text = _config.ReproductionCooldown.ToString();
        ReproductionMassTransferTextBox.Text = _config.ReproductionMassTransfer.ToString();
        ReproductionEnergyTransferTextBox.Text = _config.ReproductionEnergyTransfer.ToString();

        // Phasing Parameters
        PhasingEnergyCostTextBox.Text = _config.PhasingEnergyCost.ToString();
        PhasingCooldownTextBox.Text = _config.PhasingCooldown.ToString();
        PhasingDurationTextBox.Text = _config.PhasingDuration.ToString();

        // Ability Probabilities
        EatingProbTextBox.Text = _config.EatingProbability.ToString();
        SplittingProbTextBox.Text = _config.SplittingProbability.ToString();
        ReproductionProbTextBox.Text = _config.ReproductionProbability.ToString();
        PhasingProbTextBox.Text = _config.PhasingProbability.ToString();
        ChaseProbTextBox.Text = _config.ChaseProbability.ToString();
        FleeProbTextBox.Text = _config.FleeProbability.ToString();

        // Type Distribution
        PredatorProbTextBox.Text = _config.PredatorProbability.ToString();
        HerbivoreProbTextBox.Text = _config.HerbivoreProbability.ToString();
        SocialProbTextBox.Text = _config.SocialProbability.ToString();
        SolitaryProbTextBox.Text = _config.SolitaryProbability.ToString();
        NeutralProbTextBox.Text = _config.NeutralProbability.ToString();
    }
}
