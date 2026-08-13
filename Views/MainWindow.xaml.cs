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
    private ParticleTooltipManager? _tooltipManager;
    private SimulationInputHandler? _inputHandler;

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

    /// <summary>
    /// Releases the audio device and its feed thread on close. The thread is a background
    /// thread so the process would exit regardless, but closing the waveOut handle explicitly
    /// avoids leaving the device claimed during a slow shutdown.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _uiUpdateTimer?.Stop();
        _simulationManager?.Shutdown();
        base.OnClosed(e);
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

        // Release the outgoing simulation's audio device. Each SimulationManager owns a
        // waveOut handle and a feed thread; without this, every Reset leaks both.
        _simulationManager?.Shutdown();

        // Create simulation manager
        _simulationManager = new SimulationManager(SimulationCanvas, _config);

        // Choose the render mode before Initialize builds any visuals. Doing it afterwards
        // would create a full set of Classic ellipses only to tear them down again.
        ApplyRenderModeToRenderer(_simulationManager.Renderer);

        _simulationManager.Initialize();

        // Initialize UI managers
        _tooltipManager = new ParticleTooltipManager(ParticleTooltip, TooltipText);
        _inputHandler = new SimulationInputHandler(SimulationCanvas, _simulationManager, _tooltipManager);

        // Apply visual settings from UI to renderer
        ApplyVisualSettingsToRenderer();

        // Apply color scheme from UI
        ApplyColorSchemeToParticles();

        // Carry audio settings across the rebuild
        ApplyAudioSettings();

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
            UseAmbientEnergyCheckBox = UseAmbientEnergyCheckBox,
            AmbientEnergySlider = AmbientEnergySlider,
            AmbientEnergyTextBox = AmbientEnergyTextBox,
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

    // Mouse event handlers - delegate to SimulationInputHandler
    private void SimulationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_inputHandler == null) return;
        _inputHandler.OnMouseLeftButtonDown(e);
        UpdateInfo(); // Update particle count display
    }

    private void SimulationCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _inputHandler?.OnMouseRightButtonDown(e);
    }

    private void SimulationCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        _inputHandler?.OnMouseMove(e);
    }

    private void SimulationCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _inputHandler?.OnMouseRightButtonUp(e);
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

        ApplyRenderModeToRenderer(renderer);

        // Force a re-render to apply settings
        renderer.Render(_simulationManager.Particles);
    }

    /// <summary>
    /// Pushes the render mode and its light settings into the renderer. Kept separate so it
    /// can also run when a fresh SimulationManager is built on Reset.
    /// </summary>
    private void ApplyRenderModeToRenderer(Rendering.ParticleRenderer renderer)
    {
        bool luminous = RenderLuminousRadio?.IsChecked ?? false;
        renderer.Mode = luminous ? Rendering.RenderMode.Luminous : Rendering.RenderMode.Classic;

        renderer.Luminous.Exposure = (float)(ExposureSlider?.Value ?? 1.15);
        renderer.Luminous.GlowScale = (float)(GlowScaleSlider?.Value ?? 1.0);
        renderer.Luminous.TrailPersistence = (float)(PersistenceSlider?.Value ?? 0.0);
    }

    /// <summary>
    /// Pushes audio settings into the simulation. Separate so it also runs after a Reset
    /// builds a new SimulationManager (and with it a new audio device).
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (_simulationManager == null) return;

        var audio = _simulationManager.Audio;
        audio.Volume = VolumeSlider?.Value ?? 0.6;
        audio.AmbientEnabled = AmbientDroneCheckBox?.IsChecked ?? true;
        audio.Enabled = EnableAudioCheckBox?.IsChecked ?? false;

        // Audio is optional: a machine with no output device reports why and carries on
        if (AudioStatusText != null)
        {
            AudioStatusText.Text = (audio.Enabled || !(EnableAudioCheckBox?.IsChecked ?? false))
                ? string.Empty
                : audio.FailureReason ?? "Audio could not be started.";
        }
    }

    private void AudioToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (VolumeSlider == null) return; // still parsing XAML
        ApplyAudioSettings();
    }

    private void AudioSetting_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeValueText == null) return; // still parsing XAML
        VolumeValueText.Text = VolumeSlider.Value.ToString("F2");
        ApplyAudioSettings();
    }

    private void RenderMode_Changed(object sender, RoutedEventArgs e)
    {
        // Fires during XAML parse, before the panel and simulation exist
        if (LuminousSettingsPanel == null) return;

        bool luminous = RenderLuminousRadio?.IsChecked ?? false;
        LuminousSettingsPanel.Visibility = luminous ? Visibility.Visible : Visibility.Collapsed;

        if (_simulationManager == null) return;

        var renderer = _simulationManager.Renderer;
        ApplyRenderModeToRenderer(renderer);

        // Classic mode rebuilds its shapes from scratch; Luminous just redraws
        if (!luminous)
            renderer.Initialize(_simulationManager.Particles);

        renderer.Render(_simulationManager.Particles);
    }

    private void LuminousSetting_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Each slider raises ValueChanged as XAML parsing reaches it, while controls declared
        // further down the file are still null. Every field this method touches must be
        // checked, not just the first one.
        if (ExposureValueText == null || GlowScaleValueText == null || PersistenceValueText == null)
            return;

        ExposureValueText.Text = ExposureSlider.Value.ToString("F2");
        GlowScaleValueText.Text = GlowScaleSlider.Value.ToString("F2");
        PersistenceValueText.Text = PersistenceSlider.Value.ToString("F2");

        if (_simulationManager == null) return;

        var renderer = _simulationManager.Renderer;
        renderer.Luminous.Exposure = (float)ExposureSlider.Value;
        renderer.Luminous.GlowScale = (float)GlowScaleSlider.Value;
        renderer.Luminous.TrailPersistence = (float)PersistenceSlider.Value;

        // Redraw immediately so the slider feels live even while paused
        if (!_simulationManager.IsRunning)
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

    // Touch event handlers - delegate to SimulationInputHandler
    private void SimulationCanvas_TouchDown(object sender, TouchEventArgs e)
    {
        if (_inputHandler == null) return;
        _inputHandler.OnTouchDown(e);
        UpdateInfo(); // Update particle count display if particle was added
    }

    private void SimulationCanvas_TouchMove(object sender, TouchEventArgs e)
    {
        _inputHandler?.OnTouchMove(e);
    }

    private void SimulationCanvas_TouchUp(object sender, TouchEventArgs e)
    {
        _inputHandler?.OnTouchUp(e);
    }
}
