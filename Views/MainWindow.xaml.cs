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
using DotGame.UI;
using System.Collections.Generic;

namespace DotGame.Views;

public partial class MainWindow : Window
{
    private SimulationManager? _simulationManager;
    private SimulationConfig _config;
    private DispatcherTimer? _uiUpdateTimer;
    private ConfigUIBinder? _configBinder;

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

        // Initialize ConfigUIBinder after InitializeComponent so controls are available
        InitializeConfigBinder();

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

    private void InitializeConfigBinder()
    {
        var controls = new UIControlCollection
        {
            // Basic Configuration
            ParticleCountSlider = ParticleCountSlider,
            ParticleCountTextBox = ParticleCountTextBox,
            SeedTextBox = SeedTextBox,
            SimWidthTextBox = SimWidthTextBox,
            SimHeightTextBox = SimHeightTextBox,
            MaxParticlesSlider = MaxParticlesSlider,
            MaxParticlesTextBox = MaxParticlesTextBox,

            // Physics Parameters
            GravitySlider = GravitySlider,
            GravityTextBox = GravityTextBox,
            DampingSlider = DampingSlider,
            DampingTextBox = DampingTextBox,
            RestitutionSlider = RestitutionSlider,
            RestitutionTextBox = RestitutionTextBox,

            // Particle Ranges
            MinMassSlider = MinMassSlider,
            MinMassTextBox = MinMassTextBox,
            MaxMassSlider = MaxMassSlider,
            MaxMassTextBox = MaxMassTextBox,
            MinRadiusSlider = MinRadiusSlider,
            MinRadiusTextBox = MinRadiusTextBox,
            MaxRadiusSlider = MaxRadiusSlider,
            MaxRadiusTextBox = MaxRadiusTextBox,
            MaxVelocitySlider = MaxVelocitySlider,
            MaxVelocityTextBox = MaxVelocityTextBox,

            // Physics Toggles
            UseGravityCheckBox = UseGravityCheckBox,
            UseCollisionsCheckBox = UseCollisionsCheckBox,
            UseBoundariesCheckBox = UseBoundariesCheckBox,
            UseDampingCheckBox = UseDampingCheckBox,
            UseSpatialPartitioningCheckBox = UseSpatialPartitioningCheckBox,

            // Ability Toggles
            UseAbilitiesCheckBox = UseAbilitiesCheckBox,
            UseEatingCheckBox = UseEatingCheckBox,
            UseSplittingCheckBox = UseSplittingCheckBox,
            UseReproductionCheckBox = UseReproductionCheckBox,
            UsePhasingCheckBox = UsePhasingCheckBox,
            UseChaseCheckBox = UseChaseCheckBox,
            UseFleeCheckBox = UseFleeCheckBox,

            // Energy Parameters
            BaseEnergySlider = BaseEnergySlider,
            BaseEnergyTextBox = BaseEnergyTextBox,
            PassiveDrainSlider = PassiveDrainSlider,
            PassiveDrainTextBox = PassiveDrainTextBox,
            EatingGainSlider = EatingGainSlider,
            EatingGainTextBox = EatingGainTextBox,
            SizeRatioSlider = SizeRatioSlider,
            SizeRatioTextBox = SizeRatioTextBox,
            VisionRangeSlider = VisionRangeSlider,
            VisionRangeTextBox = VisionRangeTextBox,
            HungerThresholdSlider = HungerThresholdSlider,
            HungerThresholdTextBox = HungerThresholdTextBox,

            // Chase/Flee Parameters
            ChaseForceSlider = ChaseForceSlider,
            ChaseForceTextBox = ChaseForceTextBox,
            FleeForceSlider = FleeForceSlider,
            FleeForceTextBox = FleeForceTextBox,
            ChaseEnergyCostSlider = ChaseEnergyCostSlider,
            ChaseEnergyCostTextBox = ChaseEnergyCostTextBox,
            FleeEnergyCostSlider = FleeEnergyCostSlider,
            FleeEnergyCostTextBox = FleeEnergyCostTextBox,

            // Splitting Parameters
            SplittingEnergyCostTextBox = SplittingEnergyCostTextBox,
            SplittingCooldownSlider = SplittingCooldownSlider,
            SplittingCooldownTextBox = SplittingCooldownTextBox,
            SplittingSeparationTextBox = SplittingSeparationTextBox,

            // Reproduction Parameters
            ReproductionEnergyCostTextBox = ReproductionEnergyCostTextBox,
            ReproductionCooldownSlider = ReproductionCooldownSlider,
            ReproductionCooldownTextBox = ReproductionCooldownTextBox,
            ReproductionMassTransferTextBox = ReproductionMassTransferTextBox,
            ReproductionEnergyTransferTextBox = ReproductionEnergyTransferTextBox,

            // Phasing Parameters
            PhasingEnergyCostTextBox = PhasingEnergyCostTextBox,
            PhasingCooldownSlider = PhasingCooldownSlider,
            PhasingCooldownTextBox = PhasingCooldownTextBox,
            PhasingDurationSlider = PhasingDurationSlider,
            PhasingDurationTextBox = PhasingDurationTextBox,

            // Other Ability Cooldowns
            EatingCooldownSlider = EatingCooldownSlider,
            EatingCooldownTextBox = EatingCooldownTextBox,
            ChaseCooldownSlider = ChaseCooldownSlider,
            ChaseCooldownTextBox = ChaseCooldownTextBox,
            FleeCooldownSlider = FleeCooldownSlider,
            FleeCooldownTextBox = FleeCooldownTextBox,
            SpeedBurstCooldownSlider = SpeedBurstCooldownSlider,
            SpeedBurstCooldownTextBox = SpeedBurstCooldownTextBox,

            // Ability Probabilities
            EatingProbSlider = EatingProbSlider,
            EatingProbTextBox = EatingProbTextBox,
            SplittingProbSlider = SplittingProbSlider,
            SplittingProbTextBox = SplittingProbTextBox,
            ReproductionProbSlider = ReproductionProbSlider,
            ReproductionProbTextBox = ReproductionProbTextBox,
            PhasingProbSlider = PhasingProbSlider,
            PhasingProbTextBox = PhasingProbTextBox,
            ChaseProbSlider = ChaseProbSlider,
            ChaseProbTextBox = ChaseProbTextBox,
            FleeProbSlider = FleeProbSlider,
            FleeProbTextBox = FleeProbTextBox,

            // Type Distribution
            PredatorProbSlider = PredatorProbSlider,
            PredatorProbTextBox = PredatorProbTextBox,
            HerbivoreProbSlider = HerbivoreProbSlider,
            HerbivoreProbTextBox = HerbivoreProbTextBox,
            SocialProbSlider = SocialProbSlider,
            SocialProbTextBox = SocialProbTextBox,
            SolitaryProbSlider = SolitaryProbSlider,
            SolitaryProbTextBox = SolitaryProbTextBox,
            NeutralProbSlider = NeutralProbSlider,
            NeutralProbTextBox = NeutralProbTextBox
        };

        _configBinder = new ConfigUIBinder(_config, controls);
    }

    private void UpdateConfigFromUI()
    {
        _configBinder?.UpdateConfigFromUI();
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
        _configBinder?.PopulateUIFromConfig();
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
