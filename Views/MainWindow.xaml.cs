using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;
using DotGame.Models;
using DotGame.Simulation;
using DotGame.Utilities;
using DotGame.UI;
using RP.Game.Mechanics;
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
        // Shape the world to the display before anything is created with it
        ApplyDisplayAspectToWorld();

        // Initialize simulation after window is loaded and canvas is laid out
        InitializeSimulation();
    }

    /// <summary>
    /// Sets the default world to the display's aspect ratio, so full screen fills it exactly
    /// instead of showing bars down the sides.
    ///
    /// The configured *area* is preserved rather than a dimension, because particle density
    /// is what the ecosystem is balanced around - stretching 800x600 out to 1067x600 would
    /// quietly make the world a third emptier and change how often anything meets anything.
    ///
    /// Only the starting value is derived this way; the Sim Width and Sim Height fields remain
    /// the source of truth and can be set to anything.
    /// </summary>
    private void ApplyDisplayAspectToWorld()
    {
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        if (screenWidth <= 0 || screenHeight <= 0) return;

        double area = _config.SimulationWidth * _config.SimulationHeight;
        if (area <= 0) return;

        double aspect = screenWidth / screenHeight;
        double height = Math.Round(Math.Sqrt(area / aspect));
        double width = Math.Round(height * aspect);
        if (width < 100 || height < 100) return;

        _config.SimulationWidth = width;
        _config.SimulationHeight = height;
        SimWidthTextBox.Text = width.ToString("F0");
        SimHeightTextBox.Text = height.ToString("F0");
    }

    /// <summary>
    /// Releases the audio device and its feed thread on close. The thread is a background
    /// thread so the process would exit regardless, but closing the waveOut handle explicitly
    /// avoids leaving the device claimed during a slow shutdown.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _uiUpdateTimer?.Stop();
        _hintTimer?.Stop();
        _simulationManager?.Shutdown();
        base.OnClosed(e);
    }

    // ---------------------------------------------------------------- full screen

    private bool _isFullScreen;
    private WindowStyle _restoreStyle;
    private WindowState _restoreState;
    private ResizeMode _restoreResizeMode;
    private Thickness _restoreBorderMargin;
    private Thickness _restoreBorderThickness;
    private DispatcherTimer? _hintTimer;

    /// <summary>
    /// Enters or leaves full screen: the sidebar is removed, the window loses its chrome, and
    /// the simulation fills the display.
    ///
    /// The world itself is unchanged - the Viewbox simply scales it up to the larger area, so
    /// the same simulation is shown bigger rather than a bigger simulation being shown.
    /// </summary>
    private void SetFullScreen(bool on)
    {
        if (on == _isFullScreen) return;
        _isFullScreen = on;

        if (on)
        {
            _restoreStyle = WindowStyle;
            _restoreState = WindowState;
            _restoreResizeMode = ResizeMode;
            _restoreBorderMargin = SimulationBorder.Margin;
            _restoreBorderThickness = SimulationBorder.BorderThickness;

            SidebarPanel.Visibility = Visibility.Collapsed;
            SidebarColumn.Width = new GridLength(0);

            // Drop the frame so the simulation runs edge to edge
            SimulationBorder.Margin = new Thickness(0);
            SimulationBorder.BorderThickness = new Thickness(0);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;

            // Cycle through Normal first. Removing the chrome while already maximised leaves
            // the window sized for the frame it no longer has, so it would not cover the
            // taskbar or the full screen height.
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;

            ShowFullScreenHint();
        }
        else
        {
            SidebarPanel.Visibility = Visibility.Visible;
            SidebarColumn.Width = new GridLength(300);
            SimulationBorder.Margin = _restoreBorderMargin;
            SimulationBorder.BorderThickness = _restoreBorderThickness;

            WindowStyle = _restoreStyle;
            ResizeMode = _restoreResizeMode;
            WindowState = _restoreState;

            HideFullScreenHint();
        }
    }

    /// <summary>
    /// Briefly says how to get back out. Without it, a borderless window with no visible
    /// controls is easy to mistake for a hang.
    /// </summary>
    private void ShowFullScreenHint()
    {
        FullScreenHint.Visibility = Visibility.Visible;

        _hintTimer ??= new DispatcherTimer();
        _hintTimer.Stop();
        _hintTimer.Interval = TimeSpan.FromSeconds(4);
        _hintTimer.Tick -= HintTimer_Tick;
        _hintTimer.Tick += HintTimer_Tick;
        _hintTimer.Start();
    }

    private void HintTimer_Tick(object? sender, EventArgs e)
    {
        _hintTimer?.Stop();
        FullScreenHint.Visibility = Visibility.Collapsed;
    }

    private void HideFullScreenHint()
    {
        _hintTimer?.Stop();
        FullScreenHint.Visibility = Visibility.Collapsed;
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e) => SetFullScreen(!_isFullScreen);

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Preview, so the shortcut works even while a sidebar text box has focus
        if (e.Key == Key.F11)
        {
            SetFullScreen(!_isFullScreen);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullScreen)
        {
            SetFullScreen(false);
            e.Handled = true;
        }
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

        // The world's dimensions come from the configuration and nowhere else - the Sim Width
        // and Sim Height fields. The window only decides how large that world is drawn.
        //
        // This is what makes a seed portable: previously the world was sized from whatever
        // window it happened to open in, so the same seed produced a different simulation on
        // a different display. Now it does not.

        // Release the outgoing simulation's audio device. Each SimulationManager owns a
        // waveOut handle and a feed thread; without this, every Reset leaks both.
        _simulationManager?.Shutdown();

        // Create simulation manager
        _simulationManager = new SimulationManager(SimulationCanvas, _config);

        // Choose the render mode before Initialize builds any visuals. Doing it afterwards
        // would create a full set of Classic ellipses only to tear them down again.
        ApplyRenderModeToRenderer(_simulationManager.Renderer);

        // Give the canvas the world's dimensions; the Viewbox handles fitting it on screen
        ApplyWorldSize();

        _simulationManager.Initialize();

        // Initialize UI managers
        _tooltipManager = new ParticleTooltipManager(ParticleTooltip, TooltipText);
        _inputHandler = new SimulationInputHandler(SimulationCanvas, SimulationSurface, _simulationManager, _tooltipManager);

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
        // The world keeps its dimensions; only the zoom applied to it changes.
        UpdateViewScale();
    }

    /// <summary>
    /// Sets the canvas to the world's dimensions. The Viewbox scales that to the space
    /// available, preserving aspect ratio, so circles stay circular at any window size.
    /// </summary>
    private void ApplyWorldSize()
    {
        if (_config.SimulationWidth <= 0 || _config.SimulationHeight <= 0) return;

        SimulationCanvas.Width = _config.SimulationWidth;
        SimulationCanvas.Height = _config.SimulationHeight;
        UpdateViewScale();
    }

    /// <summary>
    /// Tells the renderer how many screen pixels a world unit currently occupies, so the
    /// light field can render at matching resolution instead of being stretched and softened.
    /// </summary>
    private void UpdateViewScale()
    {
        if (_simulationManager == null) return;
        if (SimulationCanvas.Width <= 0 || SimulationViewbox.ActualWidth <= 0) return;

        double scale = Math.Min(
            SimulationViewbox.ActualWidth / SimulationCanvas.Width,
            SimulationViewbox.ActualHeight / SimulationCanvas.Height);

        if (scale > 0 && !double.IsInfinity(scale))
            _simulationManager.Renderer.ViewScale = scale;
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
                // JsonStore writes to a temporary file and then replaces the target, so a crash or
                // a full disk part-way through leaves the previous settings intact rather than a
                // truncated file where they used to be.
                JsonStore.Save(saveFileDialog.FileName, _config);

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
                // TryLoad reports a missing or corrupt file as false rather than throwing, so a
                // hand-edited settings file is a message to the user, not an unhandled exception.
                JsonStore.TryLoad(openFileDialog.FileName, out SimulationConfig? loadedConfig);

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

        // The world rarely matches the window's aspect exactly, so there are usually bars to
        // either side of it. They belong to the Border, which the renderer cannot reach.
        SimulationBorder.Background = luminous
            ? System.Windows.Media.Brushes.Black
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x2A));

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
