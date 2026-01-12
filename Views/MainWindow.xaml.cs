using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;
using DotGame.Models;
using DotGame.Simulation;
using DotGame.Utilities;
using System.Collections.Generic;

namespace DotGame.Views;

public partial class MainWindow : Window
{
    private SimulationManager? _simulationManager;
    private SimulationConfig _config;
    private DispatcherTimer? _uiUpdateTimer;

    // Mouse interaction state
    private Particle? _draggedParticle;
    private Vector2 _lastMousePosition;
    private bool _isDragging;

    // Touch interaction state
    private Particle? _touchedParticle;
    private Vector2 _lastTouchPosition;
    private bool _isTouchDragging;
    private int? _activeTouchId;

    // Track if user has explicitly set a seed value
    private bool _userSetSeed = false;

    public MainWindow()
    {
        InitializeComponent();
        _config = new SimulationConfig();

        // Populate preset ComboBox
        foreach (var presetName in ConfigurationPresets.GetPresetNames())
        {
            PresetComboBox.Items.Add(presetName);
        }
        PresetComboBox.SelectedIndex = 0; // Select "Default"

        // Setup UI update timer
        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(RenderingConstants.UI_UPDATE_INTERVAL_MS)
        };
        _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        _uiUpdateTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize simulation after window is loaded and canvas is laid out
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        // Generate new random seed if user hasn't explicitly set one
        if (!_userSetSeed)
        {
            _config.RandomSeed = Environment.TickCount;
            SeedTextBox.Text = _config.RandomSeed.ToString();
        }
        else
        {
            // Use the user-provided seed
            if (int.TryParse(SeedTextBox.Text, out int userSeed))
            {
                _config.RandomSeed = userSeed;
            }
        }

        // Update config from UI
        UpdateConfigFromUI();

        // Use actual canvas size (should be available after Window_Loaded)
        if (SimulationCanvas.ActualWidth > 0 && SimulationCanvas.ActualHeight > 0)
        {
            _config.SimulationWidth = SimulationCanvas.ActualWidth;
            _config.SimulationHeight = SimulationCanvas.ActualHeight;

            // Update textboxes to reflect actual canvas size
            SimWidthTextBox.Text = SimulationCanvas.ActualWidth.ToString("F0");
            SimHeightTextBox.Text = SimulationCanvas.ActualHeight.ToString("F0");
        }

        // Create simulation manager
        _simulationManager = new SimulationManager(SimulationCanvas, _config);
        _simulationManager.Initialize();

        // Apply visual settings from UI to renderer
        ApplyVisualSettingsToRenderer();

        // Apply color scheme from UI
        ApplyColorSchemeToParticles();

        UpdateInfo();
    }

    private void UpdateConfigFromUI()
    {
        // Basic Configuration
        if (int.TryParse(ParticleCountTextBox.Text, out int particleCount))
            _config.ParticleCount = particleCount;

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

        // Other Ability Cooldowns
        if (double.TryParse(EatingCooldownTextBox.Text, out double eatingCooldown))
            _config.EatingCooldown = eatingCooldown;

        if (double.TryParse(ChaseCooldownTextBox.Text, out double chaseCooldown))
            _config.ChaseCooldown = chaseCooldown;

        if (double.TryParse(FleeCooldownTextBox.Text, out double fleeCooldown))
            _config.FleeCooldown = fleeCooldown;

        if (double.TryParse(SpeedBurstCooldownTextBox.Text, out double speedBurstCooldown))
            _config.SpeedBurstCooldown = speedBurstCooldown;

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

        // Validate and normalize configuration
        _config.ValidateAndClamp();
        _config.NormalizeTypeProbabilities();
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

    private void UiUpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_simulationManager == null) return;

        // Update FPS display
        var perfMonitor = _simulationManager.PerformanceMonitor;
        FpsTextBlock.Text = $"{perfMonitor.FPS:F1}";

        // Color code FPS (green = good, yellow = ok, red = bad)
        if (perfMonitor.FPS >= RenderingConstants.FPS_GOOD_THRESHOLD)
            FpsTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        else if (perfMonitor.FPS >= RenderingConstants.FPS_OK_THRESHOLD)
            FpsTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
        else
            FpsTextBlock.Foreground = System.Windows.Media.Brushes.Red;

        // Update particle count
        int particleCount = _simulationManager.Particles?.Count ?? 0;
        ParticleCountTextBlock.Text = $"{particleCount} / {_config.MaxParticles}";
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
        // Reset will generate a new random seed unless user has set one
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

            // Update hovered particle for vision cone display
            _simulationManager.Renderer.HoveredParticle = hoveredParticle;

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
        const double offsetX = RenderingConstants.TOOLTIP_OFFSET_X;
        const double offsetY = RenderingConstants.TOOLTIP_OFFSET_Y;
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
            if (particle.Abilities.HasAbility(AbilitySet.SpeedBurst)) abilityList.Add("SpeedBurst");
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

    private void SimulationBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Get the new size of the border (which contains the canvas)
        var newWidth = e.NewSize.Width;
        var newHeight = e.NewSize.Height;

        // Update canvas size to fill the border
        SimulationCanvas.Width = newWidth;
        SimulationCanvas.Height = newHeight;

        // Update simulation configuration with new dimensions
        _config.SimulationWidth = newWidth;
        _config.SimulationHeight = newHeight;

        // Update the UI textboxes to reflect new dimensions
        SimWidthTextBox.Text = newWidth.ToString("F0");
        SimHeightTextBox.Text = newHeight.ToString("F0");
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

                    // Validate loaded configuration
                    _config.ValidateAndClamp();
                    _config.NormalizeTypeProbabilities();

                    // Reset user seed flag when loading settings
                    _userSetSeed = false;

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
        ParticleCountSlider.Value = _config.ParticleCount;
        ParticleCountTextBox.Text = _config.ParticleCount.ToString();
        SeedTextBox.Text = _config.RandomSeed.ToString();
        SimWidthTextBox.Text = _config.SimulationWidth.ToString();
        SimHeightTextBox.Text = _config.SimulationHeight.ToString();
        MaxParticlesSlider.Value = _config.MaxParticles;
        MaxParticlesTextBox.Text = _config.MaxParticles.ToString();

        // Physics Parameters
        GravitySlider.Value = _config.GravitationalConstant;
        GravityTextBox.Text = _config.GravitationalConstant.ToString();
        DampingSlider.Value = _config.DampingFactor;
        DampingTextBox.Text = _config.DampingFactor.ToString();
        RestitutionSlider.Value = _config.RestitutionCoefficient;
        RestitutionTextBox.Text = _config.RestitutionCoefficient.ToString();

        // Particle Ranges
        MinMassSlider.Value = _config.MinMass;
        MinMassTextBox.Text = _config.MinMass.ToString();
        MaxMassSlider.Value = _config.MaxMass;
        MaxMassTextBox.Text = _config.MaxMass.ToString();
        MinRadiusSlider.Value = _config.MinRadius;
        MinRadiusTextBox.Text = _config.MinRadius.ToString();
        MaxRadiusSlider.Value = _config.MaxRadius;
        MaxRadiusTextBox.Text = _config.MaxRadius.ToString();
        MaxVelocitySlider.Value = _config.MaxInitialVelocity;
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
        BaseEnergySlider.Value = _config.BaseEnergyCapacity;
        BaseEnergyTextBox.Text = _config.BaseEnergyCapacity.ToString();
        PassiveDrainSlider.Value = _config.PassiveEnergyDrain;
        PassiveDrainTextBox.Text = _config.PassiveEnergyDrain.ToString();
        EatingGainSlider.Value = _config.EatingEnergyGain;
        EatingGainTextBox.Text = _config.EatingEnergyGain.ToString();
        SizeRatioSlider.Value = _config.SizeRatioForEating;
        SizeRatioTextBox.Text = _config.SizeRatioForEating.ToString();
        VisionRangeSlider.Value = _config.VisionRangeMultiplier;
        VisionRangeTextBox.Text = _config.VisionRangeMultiplier.ToString();
        HungerThresholdSlider.Value = _config.HungerThreshold;
        HungerThresholdTextBox.Text = _config.HungerThreshold.ToString();

        // Chase/Flee Parameters
        ChaseForceSlider.Value = _config.ChaseForce;
        ChaseForceTextBox.Text = _config.ChaseForce.ToString();
        FleeForceSlider.Value = _config.FleeForce;
        FleeForceTextBox.Text = _config.FleeForce.ToString();
        ChaseEnergyCostSlider.Value = _config.ChaseEnergyCost;
        ChaseEnergyCostTextBox.Text = _config.ChaseEnergyCost.ToString();
        FleeEnergyCostSlider.Value = _config.FleeEnergyCost;
        FleeEnergyCostTextBox.Text = _config.FleeEnergyCost.ToString();

        // Splitting Parameters
        SplittingEnergyCostTextBox.Text = _config.SplittingEnergyCost.ToString();
        SplittingCooldownSlider.Value = _config.SplittingCooldown;
        SplittingCooldownTextBox.Text = _config.SplittingCooldown.ToString();
        SplittingSeparationTextBox.Text = _config.SplittingSeparationForce.ToString();

        // Reproduction Parameters
        ReproductionEnergyCostTextBox.Text = _config.ReproductionEnergyCost.ToString();
        ReproductionCooldownSlider.Value = _config.ReproductionCooldown;
        ReproductionCooldownTextBox.Text = _config.ReproductionCooldown.ToString();
        ReproductionMassTransferTextBox.Text = _config.ReproductionMassTransfer.ToString();
        ReproductionEnergyTransferTextBox.Text = _config.ReproductionEnergyTransfer.ToString();

        // Phasing Parameters
        PhasingEnergyCostTextBox.Text = _config.PhasingEnergyCost.ToString();
        PhasingCooldownSlider.Value = _config.PhasingCooldown;
        PhasingCooldownTextBox.Text = _config.PhasingCooldown.ToString();
        PhasingDurationSlider.Value = _config.PhasingDuration;
        PhasingDurationTextBox.Text = _config.PhasingDuration.ToString();

        // Other Ability Cooldowns
        EatingCooldownSlider.Value = _config.EatingCooldown;
        EatingCooldownTextBox.Text = _config.EatingCooldown.ToString();
        ChaseCooldownSlider.Value = _config.ChaseCooldown;
        ChaseCooldownTextBox.Text = _config.ChaseCooldown.ToString();
        FleeCooldownSlider.Value = _config.FleeCooldown;
        FleeCooldownTextBox.Text = _config.FleeCooldown.ToString();
        SpeedBurstCooldownSlider.Value = _config.SpeedBurstCooldown;
        SpeedBurstCooldownTextBox.Text = _config.SpeedBurstCooldown.ToString();

        // Ability Probabilities
        EatingProbSlider.Value = _config.EatingProbability;
        EatingProbTextBox.Text = _config.EatingProbability.ToString();
        SplittingProbSlider.Value = _config.SplittingProbability;
        SplittingProbTextBox.Text = _config.SplittingProbability.ToString();
        ReproductionProbSlider.Value = _config.ReproductionProbability;
        ReproductionProbTextBox.Text = _config.ReproductionProbability.ToString();
        PhasingProbSlider.Value = _config.PhasingProbability;
        PhasingProbTextBox.Text = _config.PhasingProbability.ToString();
        ChaseProbSlider.Value = _config.ChaseProbability;
        ChaseProbTextBox.Text = _config.ChaseProbability.ToString();
        FleeProbSlider.Value = _config.FleeProbability;
        FleeProbTextBox.Text = _config.FleeProbability.ToString();

        // Type Distribution
        PredatorProbSlider.Value = _config.PredatorProbability;
        PredatorProbTextBox.Text = _config.PredatorProbability.ToString();
        HerbivoreProbSlider.Value = _config.HerbivoreProbability;
        HerbivoreProbTextBox.Text = _config.HerbivoreProbability.ToString();
        SocialProbSlider.Value = _config.SocialProbability;
        SocialProbTextBox.Text = _config.SocialProbability.ToString();
        SolitaryProbSlider.Value = _config.SolitaryProbability;
        SolitaryProbTextBox.Text = _config.SolitaryProbability.ToString();
        NeutralProbSlider.Value = _config.NeutralProbability;
        NeutralProbTextBox.Text = _config.NeutralProbability.ToString();
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetComboBox.SelectedItem == null) return;

        var selectedPreset = PresetComboBox.SelectedItem.ToString();
        if (selectedPreset == null) return;

        // Load the selected preset
        _config = ConfigurationPresets.GetPreset(selectedPreset);

        // Reset user seed flag when loading preset - allow random seeds again
        _userSetSeed = false;

        // Update UI to reflect loaded preset
        PopulateUIFromConfig();

        // Show message to user
        InfoTextBlock.Text = $"Loaded preset: {selectedPreset}\nClick Reset to apply.";
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not System.Windows.Controls.Slider slider) return;

        // Sync slider value with corresponding TextBox
        var sliderName = slider.Name;
        if (string.IsNullOrEmpty(sliderName)) return;

        // Get corresponding TextBox name by replacing "Slider" with "TextBox"
        var textBoxName = sliderName.Replace("Slider", "TextBox");

        // Find the TextBox
        var textBox = FindName(textBoxName) as System.Windows.Controls.TextBox;
        if (textBox != null)
        {
            // Format the value appropriately
            string format = slider.Name switch
            {
                // Integer values
                "ParticleCountSlider" => "F0",
                "MaxParticlesSlider" => "F0",
                "MinRadiusSlider" => "F0",
                "MaxRadiusSlider" => "F0",
                "MaxVelocitySlider" => "F0",
                "GravitySlider" => "F1",
                "ChaseForceSlider" => "F0",
                "FleeForceSlider" => "F0",
                "BaseEnergySlider" => "F0",
                "SplittingCooldownSlider" => "F0",
                "ReproductionCooldownSlider" => "F0",
                "PhasingCooldownSlider" => "F0",
                "SpeedBurstCooldownSlider" => "F0",
                // High precision decimals
                "DampingSlider" => "F3",
                "EatingCooldownSlider" => "F1",
                "ChaseCooldownSlider" => "F1",
                "FleeCooldownSlider" => "F1",
                // Standard decimals
                _ => "F2"
            };

            textBox.Text = slider.Value.ToString(format);
        }
    }

    private void VisualToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_simulationManager == null) return;

        // Update visual settings in the renderer
        var renderer = _simulationManager.Renderer;
        renderer.ShowGrid = ShowGridCheckBox.IsChecked ?? false;
        renderer.ShowVisionCones = ShowVisionConesCheckBox.IsChecked ?? false;
        renderer.ShowTrails = ShowTrailsCheckBox.IsChecked ?? false;
        renderer.ShowEnergyBars = ShowEnergyBarsCheckBox.IsChecked ?? true;
        renderer.TrailLength = (int)(TrailLengthSlider?.Value ?? 15);

        // Force a re-render
        renderer.Render(_simulationManager.Particles);
    }

    private void ColorScheme_Changed(object sender, RoutedEventArgs e)
    {
        if (_simulationManager == null) return;

        bool useTypeColors = ColorByTypeRadio.IsChecked ?? true;

        // Update particle colors based on selected scheme
        foreach (var particle in _simulationManager.Particles)
        {
            if (useTypeColors && particle.HasAbilities)
            {
                particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
            }
            else
            {
                particle.Color = Utilities.ColorGenerator.GetColorForMass(
                    particle.Mass, _config.MinMass, _config.MaxMass);
            }
        }

        // Force a re-render
        _simulationManager.Renderer.Render(_simulationManager.Particles);
    }

    private void ApplyVisualSettingsToRenderer()
    {
        if (_simulationManager == null) return;

        // Apply all visual settings from UI to renderer
        var renderer = _simulationManager.Renderer;
        renderer.ShowGrid = ShowGridCheckBox.IsChecked ?? false;
        renderer.ShowVisionCones = ShowVisionConesCheckBox.IsChecked ?? false;
        renderer.ShowTrails = ShowTrailsCheckBox.IsChecked ?? false;
        renderer.ShowEnergyBars = ShowEnergyBarsCheckBox.IsChecked ?? true;
        renderer.TrailLength = (int)(TrailLengthSlider?.Value ?? 15);

        // Force a re-render to apply settings
        renderer.Render(_simulationManager.Particles);
    }

    private void ApplyColorSchemeToParticles()
    {
        if (_simulationManager == null) return;

        bool useTypeColors = ColorByTypeRadio.IsChecked ?? true;

        // Update particle colors based on selected scheme
        foreach (var particle in _simulationManager.Particles)
        {
            if (useTypeColors && particle.HasAbilities)
            {
                particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
            }
            else
            {
                particle.Color = Utilities.ColorGenerator.GetColorForMass(
                    particle.Mass, _config.MinMass, _config.MaxMass);
            }
        }

        // Force a re-render
        _simulationManager.Renderer.Render(_simulationManager.Particles);
    }

    private void SeedTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // When user manually edits the seed textbox, mark it as user-set
        // so that future resets won't generate new random seeds
        if (!string.IsNullOrWhiteSpace(SeedTextBox.Text) && int.TryParse(SeedTextBox.Text, out _))
        {
            _userSetSeed = true;
        }
    }

    // Touch event handlers for touch screen support
    private void SimulationCanvas_TouchDown(object sender, TouchEventArgs e)
    {
        if (_simulationManager == null) return;

        // Only handle one touch at a time
        if (_activeTouchId.HasValue) return;

        // Get touch position relative to canvas
        var touchPoint = e.GetTouchPoint(SimulationCanvas);
        var position = new Vector2((float)touchPoint.Position.X, (float)touchPoint.Position.Y);

        // Find particle at this position
        var particle = _simulationManager.FindParticleAtPosition(position);

        if (particle != null)
        {
            // Touching a particle - start drag and show tooltip
            _touchedParticle = particle;
            _isTouchDragging = true;
            _lastTouchPosition = position;
            _activeTouchId = e.TouchDevice.Id;

            // Show particle details tooltip at touch position
            ShowParticleTooltip(particle, touchPoint.Position);

            // Capture this touch
            e.TouchDevice.Capture(SimulationCanvas);
            e.Handled = true;
        }
        else
        {
            // Touching empty space - create new particle
            _simulationManager.AddParticle(position);

            // Update particle count in info
            if (_simulationManager.Particles != null)
            {
                string status = _simulationManager.IsRunning ? "Running" : "Stopped";
                InfoTextBlock.Text = $"Status: {status}\n" +
                                    $"Particles: {_simulationManager.Particles.Count}\n" +
                                    $"Seed: {_config.RandomSeed}";
            }

            e.Handled = true;
        }
    }

    private void SimulationCanvas_TouchMove(object sender, TouchEventArgs e)
    {
        if (_simulationManager == null) return;

        // Only handle the active touch
        if (!_activeTouchId.HasValue || e.TouchDevice.Id != _activeTouchId.Value)
            return;

        // Get current touch position
        var touchPoint = e.GetTouchPoint(SimulationCanvas);
        var currentPosition = new Vector2((float)touchPoint.Position.X, (float)touchPoint.Position.Y);

        // Handle dragging behavior
        if (_isTouchDragging && _touchedParticle != null)
        {
            // Calculate touch movement delta
            var delta = currentPosition - _lastTouchPosition;

            // Apply impulse based on touch movement
            // Scale factor controls how much impulse is applied
            const float impulseFactor = 50.0f;
            var impulse = delta * impulseFactor;

            _simulationManager.ApplyImpulseToParticle(_touchedParticle, impulse);

            // Update last touch position
            _lastTouchPosition = currentPosition;

            // Update tooltip position to follow touch
            ShowParticleTooltip(_touchedParticle, touchPoint.Position);

            e.Handled = true;
        }
    }

    private void SimulationCanvas_TouchUp(object sender, TouchEventArgs e)
    {
        // Only handle the active touch
        if (!_activeTouchId.HasValue || e.TouchDevice.Id != _activeTouchId.Value)
            return;

        if (_isTouchDragging)
        {
            _isTouchDragging = false;
            _touchedParticle = null;
            _activeTouchId = null;

            // Hide tooltip when touch ends
            ParticleTooltip.Visibility = Visibility.Collapsed;

            // Release touch capture
            e.TouchDevice.Capture(null);
            e.Handled = true;
        }
    }
}
