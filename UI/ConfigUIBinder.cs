using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DotGame.Models;

namespace DotGame.UI;

/// <summary>
/// Handles bidirectional binding between UI controls and SimulationConfig.
/// Extracts configuration binding logic from MainWindow to improve maintainability.
/// </summary>
public class ConfigUIBinder
{
    private readonly SimulationConfig _config;
    private readonly UIControlCollection _controls;

    public ConfigUIBinder(SimulationConfig config, UIControlCollection controls)
    {
        _config = config;
        _controls = controls;
    }

    /// <summary>
    /// Updates the configuration from current UI control values.
    /// </summary>
    public void UpdateConfigFromUI()
    {
        UpdateBasicConfig();
        UpdatePhysicsConfig();
        UpdateParticleRanges();
        UpdatePhysicsToggles();
        UpdateAbilityToggles();
        UpdateEnergyParameters();
        UpdateChaseFleeParameters();
        UpdateSplittingParameters();
        UpdateReproductionParameters();
        UpdatePhasingParameters();
        UpdateAbilityCooldowns();
        UpdateAbilityProbabilities();
        UpdateTypeDistribution();

        // Validate and normalize after all updates
        _config.ValidateAndClamp();
        _config.NormalizeTypeProbabilities();
    }

    /// <summary>
    /// Updates UI controls from current configuration values.
    /// </summary>
    public void PopulateUIFromConfig()
    {
        PopulateBasicConfig();
        PopulatePhysicsConfig();
        PopulateParticleRanges();
        PopulatePhysicsToggles();
        PopulateAbilityToggles();
        PopulateEnergyParameters();
        PopulateChaseFleeParameters();
        PopulateSplittingParameters();
        PopulateReproductionParameters();
        PopulatePhasingParameters();
        PopulateAbilityCooldowns();
        PopulateAbilityProbabilities();
        PopulateTypeDistribution();
    }

    #region Update Config From UI

    private void UpdateBasicConfig()
    {
        if (int.TryParse(_controls.ParticleCountTextBox.Text, out int particleCount))
            _config.ParticleCount = particleCount;

        if (int.TryParse(_controls.SeedTextBox.Text, out int seed))
            _config.RandomSeed = seed;

        if (double.TryParse(_controls.SimWidthTextBox.Text, out double simWidth))
            _config.SimulationWidth = simWidth;

        if (double.TryParse(_controls.SimHeightTextBox.Text, out double simHeight))
            _config.SimulationHeight = simHeight;

        if (int.TryParse(_controls.MaxParticlesTextBox.Text, out int maxParticles))
            _config.MaxParticles = maxParticles;
    }

    private void UpdatePhysicsConfig()
    {
        if (double.TryParse(_controls.GravityTextBox.Text, out double gravity))
            _config.GravitationalConstant = gravity;

        if (double.TryParse(_controls.DampingTextBox.Text, out double damping))
            _config.DampingFactor = damping;

        if (double.TryParse(_controls.RestitutionTextBox.Text, out double restitution))
            _config.RestitutionCoefficient = restitution;
    }

    private void UpdateParticleRanges()
    {
        if (double.TryParse(_controls.MinMassTextBox.Text, out double minMass))
            _config.MinMass = minMass;

        if (double.TryParse(_controls.MaxMassTextBox.Text, out double maxMass))
            _config.MaxMass = maxMass;

        if (double.TryParse(_controls.MinRadiusTextBox.Text, out double minRadius))
            _config.MinRadius = minRadius;

        if (double.TryParse(_controls.MaxRadiusTextBox.Text, out double maxRadius))
            _config.MaxRadius = maxRadius;

        if (double.TryParse(_controls.MaxVelocityTextBox.Text, out double maxVelocity))
            _config.MaxInitialVelocity = maxVelocity;
    }

    private void UpdatePhysicsToggles()
    {
        _config.UseGravity = _controls.UseGravityCheckBox.IsChecked ?? true;
        _config.UseCollisions = _controls.UseCollisionsCheckBox.IsChecked ?? true;
        _config.UseBoundaries = _controls.UseBoundariesCheckBox.IsChecked ?? true;
        _config.UseDamping = _controls.UseDampingCheckBox.IsChecked ?? true;
        _config.UseSpatialPartitioning = _controls.UseSpatialPartitioningCheckBox.IsChecked ?? true;
    }

    private void UpdateAbilityToggles()
    {
        _config.UseAbilities = _controls.UseAbilitiesCheckBox.IsChecked ?? true;
        _config.UseEating = _controls.UseEatingCheckBox.IsChecked ?? true;
        _config.UseSplitting = _controls.UseSplittingCheckBox.IsChecked ?? true;
        _config.UseReproduction = _controls.UseReproductionCheckBox.IsChecked ?? true;
        _config.UsePhasing = _controls.UsePhasingCheckBox.IsChecked ?? true;
        _config.UseChase = _controls.UseChaseCheckBox.IsChecked ?? true;
        _config.UseFlee = _controls.UseFleeCheckBox.IsChecked ?? true;
    }

    private void UpdateEnergyParameters()
    {
        if (double.TryParse(_controls.BaseEnergyTextBox.Text, out double baseEnergy))
            _config.BaseEnergyCapacity = baseEnergy;

        if (double.TryParse(_controls.PassiveDrainTextBox.Text, out double passiveDrain))
            _config.PassiveEnergyDrain = passiveDrain;

        if (double.TryParse(_controls.EatingGainTextBox.Text, out double eatingGain))
            _config.EatingEnergyGain = eatingGain;

        if (double.TryParse(_controls.SizeRatioTextBox.Text, out double sizeRatio))
            _config.SizeRatioForEating = sizeRatio;

        if (double.TryParse(_controls.VisionRangeTextBox.Text, out double visionRange))
            _config.VisionRangeMultiplier = visionRange;

        if (double.TryParse(_controls.HungerThresholdTextBox.Text, out double hungerThreshold))
            _config.HungerThreshold = hungerThreshold;
    }

    private void UpdateChaseFleeParameters()
    {
        if (double.TryParse(_controls.ChaseForceTextBox.Text, out double chaseForce))
            _config.ChaseForce = chaseForce;

        if (double.TryParse(_controls.FleeForceTextBox.Text, out double fleeForce))
            _config.FleeForce = fleeForce;

        if (double.TryParse(_controls.ChaseEnergyCostTextBox.Text, out double chaseEnergyCost))
            _config.ChaseEnergyCost = chaseEnergyCost;

        if (double.TryParse(_controls.FleeEnergyCostTextBox.Text, out double fleeEnergyCost))
            _config.FleeEnergyCost = fleeEnergyCost;
    }

    private void UpdateSplittingParameters()
    {
        if (double.TryParse(_controls.SplittingEnergyCostTextBox.Text, out double splittingEnergyCost))
            _config.SplittingEnergyCost = splittingEnergyCost;

        if (double.TryParse(_controls.SplittingCooldownTextBox.Text, out double splittingCooldown))
            _config.SplittingCooldown = splittingCooldown;

        if (double.TryParse(_controls.SplittingSeparationTextBox.Text, out double splittingSeparation))
            _config.SplittingSeparationForce = splittingSeparation;
    }

    private void UpdateReproductionParameters()
    {
        if (double.TryParse(_controls.ReproductionEnergyCostTextBox.Text, out double reproductionEnergyCost))
            _config.ReproductionEnergyCost = reproductionEnergyCost;

        if (double.TryParse(_controls.ReproductionCooldownTextBox.Text, out double reproductionCooldown))
            _config.ReproductionCooldown = reproductionCooldown;

        if (double.TryParse(_controls.ReproductionMassTransferTextBox.Text, out double reproductionMassTransfer))
            _config.ReproductionMassTransfer = reproductionMassTransfer;

        if (double.TryParse(_controls.ReproductionEnergyTransferTextBox.Text, out double reproductionEnergyTransfer))
            _config.ReproductionEnergyTransfer = reproductionEnergyTransfer;
    }

    private void UpdatePhasingParameters()
    {
        if (double.TryParse(_controls.PhasingEnergyCostTextBox.Text, out double phasingEnergyCost))
            _config.PhasingEnergyCost = phasingEnergyCost;

        if (double.TryParse(_controls.PhasingCooldownTextBox.Text, out double phasingCooldown))
            _config.PhasingCooldown = phasingCooldown;

        if (double.TryParse(_controls.PhasingDurationTextBox.Text, out double phasingDuration))
            _config.PhasingDuration = phasingDuration;
    }

    private void UpdateAbilityCooldowns()
    {
        if (double.TryParse(_controls.EatingCooldownTextBox.Text, out double eatingCooldown))
            _config.EatingCooldown = eatingCooldown;

        if (double.TryParse(_controls.ChaseCooldownTextBox.Text, out double chaseCooldown))
            _config.ChaseCooldown = chaseCooldown;

        if (double.TryParse(_controls.FleeCooldownTextBox.Text, out double fleeCooldown))
            _config.FleeCooldown = fleeCooldown;

        if (double.TryParse(_controls.SpeedBurstCooldownTextBox.Text, out double speedBurstCooldown))
            _config.SpeedBurstCooldown = speedBurstCooldown;
    }

    private void UpdateAbilityProbabilities()
    {
        if (double.TryParse(_controls.EatingProbTextBox.Text, out double eatingProb))
            _config.EatingProbability = eatingProb;

        if (double.TryParse(_controls.SplittingProbTextBox.Text, out double splittingProb))
            _config.SplittingProbability = splittingProb;

        if (double.TryParse(_controls.ReproductionProbTextBox.Text, out double reproductionProb))
            _config.ReproductionProbability = reproductionProb;

        if (double.TryParse(_controls.PhasingProbTextBox.Text, out double phasingProb))
            _config.PhasingProbability = phasingProb;

        if (double.TryParse(_controls.ChaseProbTextBox.Text, out double chaseProb))
            _config.ChaseProbability = chaseProb;

        if (double.TryParse(_controls.FleeProbTextBox.Text, out double fleeProb))
            _config.FleeProbability = fleeProb;
    }

    private void UpdateTypeDistribution()
    {
        if (double.TryParse(_controls.PredatorProbTextBox.Text, out double predatorProb))
            _config.PredatorProbability = predatorProb;

        if (double.TryParse(_controls.HerbivoreProbTextBox.Text, out double herbivoreProb))
            _config.HerbivoreProbability = herbivoreProb;

        if (double.TryParse(_controls.SocialProbTextBox.Text, out double socialProb))
            _config.SocialProbability = socialProb;

        if (double.TryParse(_controls.SolitaryProbTextBox.Text, out double solitaryProb))
            _config.SolitaryProbability = solitaryProb;

        if (double.TryParse(_controls.NeutralProbTextBox.Text, out double neutralProb))
            _config.NeutralProbability = neutralProb;
    }

    #endregion

    #region Populate UI From Config

    private void PopulateBasicConfig()
    {
        _controls.ParticleCountSlider.Value = _config.ParticleCount;
        _controls.ParticleCountTextBox.Text = _config.ParticleCount.ToString();
        _controls.SeedTextBox.Text = _config.RandomSeed.ToString();
        _controls.SimWidthTextBox.Text = _config.SimulationWidth.ToString();
        _controls.SimHeightTextBox.Text = _config.SimulationHeight.ToString();
        _controls.MaxParticlesSlider.Value = _config.MaxParticles;
        _controls.MaxParticlesTextBox.Text = _config.MaxParticles.ToString();
    }

    private void PopulatePhysicsConfig()
    {
        _controls.GravitySlider.Value = _config.GravitationalConstant;
        _controls.GravityTextBox.Text = _config.GravitationalConstant.ToString();
        _controls.DampingSlider.Value = _config.DampingFactor;
        _controls.DampingTextBox.Text = _config.DampingFactor.ToString();
        _controls.RestitutionSlider.Value = _config.RestitutionCoefficient;
        _controls.RestitutionTextBox.Text = _config.RestitutionCoefficient.ToString();
    }

    private void PopulateParticleRanges()
    {
        _controls.MinMassSlider.Value = _config.MinMass;
        _controls.MinMassTextBox.Text = _config.MinMass.ToString();
        _controls.MaxMassSlider.Value = _config.MaxMass;
        _controls.MaxMassTextBox.Text = _config.MaxMass.ToString();
        _controls.MinRadiusSlider.Value = _config.MinRadius;
        _controls.MinRadiusTextBox.Text = _config.MinRadius.ToString();
        _controls.MaxRadiusSlider.Value = _config.MaxRadius;
        _controls.MaxRadiusTextBox.Text = _config.MaxRadius.ToString();
        _controls.MaxVelocitySlider.Value = _config.MaxInitialVelocity;
        _controls.MaxVelocityTextBox.Text = _config.MaxInitialVelocity.ToString();
    }

    private void PopulatePhysicsToggles()
    {
        _controls.UseGravityCheckBox.IsChecked = _config.UseGravity;
        _controls.UseCollisionsCheckBox.IsChecked = _config.UseCollisions;
        _controls.UseBoundariesCheckBox.IsChecked = _config.UseBoundaries;
        _controls.UseDampingCheckBox.IsChecked = _config.UseDamping;
        _controls.UseSpatialPartitioningCheckBox.IsChecked = _config.UseSpatialPartitioning;
    }

    private void PopulateAbilityToggles()
    {
        _controls.UseAbilitiesCheckBox.IsChecked = _config.UseAbilities;
        _controls.UseEatingCheckBox.IsChecked = _config.UseEating;
        _controls.UseSplittingCheckBox.IsChecked = _config.UseSplitting;
        _controls.UseReproductionCheckBox.IsChecked = _config.UseReproduction;
        _controls.UsePhasingCheckBox.IsChecked = _config.UsePhasing;
        _controls.UseChaseCheckBox.IsChecked = _config.UseChase;
        _controls.UseFleeCheckBox.IsChecked = _config.UseFlee;
    }

    private void PopulateEnergyParameters()
    {
        _controls.BaseEnergySlider.Value = _config.BaseEnergyCapacity;
        _controls.BaseEnergyTextBox.Text = _config.BaseEnergyCapacity.ToString();
        _controls.PassiveDrainSlider.Value = _config.PassiveEnergyDrain;
        _controls.PassiveDrainTextBox.Text = _config.PassiveEnergyDrain.ToString();
        _controls.EatingGainSlider.Value = _config.EatingEnergyGain;
        _controls.EatingGainTextBox.Text = _config.EatingEnergyGain.ToString();
        _controls.SizeRatioSlider.Value = _config.SizeRatioForEating;
        _controls.SizeRatioTextBox.Text = _config.SizeRatioForEating.ToString();
        _controls.VisionRangeSlider.Value = _config.VisionRangeMultiplier;
        _controls.VisionRangeTextBox.Text = _config.VisionRangeMultiplier.ToString();
        _controls.HungerThresholdSlider.Value = _config.HungerThreshold;
        _controls.HungerThresholdTextBox.Text = _config.HungerThreshold.ToString();
    }

    private void PopulateChaseFleeParameters()
    {
        _controls.ChaseForceSlider.Value = _config.ChaseForce;
        _controls.ChaseForceTextBox.Text = _config.ChaseForce.ToString();
        _controls.FleeForceSlider.Value = _config.FleeForce;
        _controls.FleeForceTextBox.Text = _config.FleeForce.ToString();
        _controls.ChaseEnergyCostSlider.Value = _config.ChaseEnergyCost;
        _controls.ChaseEnergyCostTextBox.Text = _config.ChaseEnergyCost.ToString();
        _controls.FleeEnergyCostSlider.Value = _config.FleeEnergyCost;
        _controls.FleeEnergyCostTextBox.Text = _config.FleeEnergyCost.ToString();
    }

    private void PopulateSplittingParameters()
    {
        _controls.SplittingEnergyCostTextBox.Text = _config.SplittingEnergyCost.ToString();
        _controls.SplittingCooldownSlider.Value = _config.SplittingCooldown;
        _controls.SplittingCooldownTextBox.Text = _config.SplittingCooldown.ToString();
        _controls.SplittingSeparationTextBox.Text = _config.SplittingSeparationForce.ToString();
    }

    private void PopulateReproductionParameters()
    {
        _controls.ReproductionEnergyCostTextBox.Text = _config.ReproductionEnergyCost.ToString();
        _controls.ReproductionCooldownSlider.Value = _config.ReproductionCooldown;
        _controls.ReproductionCooldownTextBox.Text = _config.ReproductionCooldown.ToString();
        _controls.ReproductionMassTransferTextBox.Text = _config.ReproductionMassTransfer.ToString();
        _controls.ReproductionEnergyTransferTextBox.Text = _config.ReproductionEnergyTransfer.ToString();
    }

    private void PopulatePhasingParameters()
    {
        _controls.PhasingEnergyCostTextBox.Text = _config.PhasingEnergyCost.ToString();
        _controls.PhasingCooldownSlider.Value = _config.PhasingCooldown;
        _controls.PhasingCooldownTextBox.Text = _config.PhasingCooldown.ToString();
        _controls.PhasingDurationSlider.Value = _config.PhasingDuration;
        _controls.PhasingDurationTextBox.Text = _config.PhasingDuration.ToString();
    }

    private void PopulateAbilityCooldowns()
    {
        _controls.EatingCooldownSlider.Value = _config.EatingCooldown;
        _controls.EatingCooldownTextBox.Text = _config.EatingCooldown.ToString();
        _controls.ChaseCooldownSlider.Value = _config.ChaseCooldown;
        _controls.ChaseCooldownTextBox.Text = _config.ChaseCooldown.ToString();
        _controls.FleeCooldownSlider.Value = _config.FleeCooldown;
        _controls.FleeCooldownTextBox.Text = _config.FleeCooldown.ToString();
        _controls.SpeedBurstCooldownSlider.Value = _config.SpeedBurstCooldown;
        _controls.SpeedBurstCooldownTextBox.Text = _config.SpeedBurstCooldown.ToString();
    }

    private void PopulateAbilityProbabilities()
    {
        _controls.EatingProbSlider.Value = _config.EatingProbability;
        _controls.EatingProbTextBox.Text = _config.EatingProbability.ToString();
        _controls.SplittingProbSlider.Value = _config.SplittingProbability;
        _controls.SplittingProbTextBox.Text = _config.SplittingProbability.ToString();
        _controls.ReproductionProbSlider.Value = _config.ReproductionProbability;
        _controls.ReproductionProbTextBox.Text = _config.ReproductionProbability.ToString();
        _controls.PhasingProbSlider.Value = _config.PhasingProbability;
        _controls.PhasingProbTextBox.Text = _config.PhasingProbability.ToString();
        _controls.ChaseProbSlider.Value = _config.ChaseProbability;
        _controls.ChaseProbTextBox.Text = _config.ChaseProbability.ToString();
        _controls.FleeProbSlider.Value = _config.FleeProbability;
        _controls.FleeProbTextBox.Text = _config.FleeProbability.ToString();
    }

    private void PopulateTypeDistribution()
    {
        _controls.PredatorProbSlider.Value = _config.PredatorProbability;
        _controls.PredatorProbTextBox.Text = _config.PredatorProbability.ToString();
        _controls.HerbivoreProbSlider.Value = _config.HerbivoreProbability;
        _controls.HerbivoreProbTextBox.Text = _config.HerbivoreProbability.ToString();
        _controls.SocialProbSlider.Value = _config.SocialProbability;
        _controls.SocialProbTextBox.Text = _config.SocialProbability.ToString();
        _controls.SolitaryProbSlider.Value = _config.SolitaryProbability;
        _controls.SolitaryProbTextBox.Text = _config.SolitaryProbability.ToString();
        _controls.NeutralProbSlider.Value = _config.NeutralProbability;
        _controls.NeutralProbTextBox.Text = _config.NeutralProbability.ToString();
    }

    #endregion
}

/// <summary>
/// Collection of all UI controls needed for configuration binding.
/// Passed to ConfigUIBinder to avoid tight coupling to MainWindow.
/// </summary>
public class UIControlCollection
{
    // Basic Configuration
    public required Slider ParticleCountSlider { get; init; }
    public required TextBox ParticleCountTextBox { get; init; }
    public required TextBox SeedTextBox { get; init; }
    public required TextBox SimWidthTextBox { get; init; }
    public required TextBox SimHeightTextBox { get; init; }
    public required Slider MaxParticlesSlider { get; init; }
    public required TextBox MaxParticlesTextBox { get; init; }

    // Physics Parameters
    public required Slider GravitySlider { get; init; }
    public required TextBox GravityTextBox { get; init; }
    public required Slider DampingSlider { get; init; }
    public required TextBox DampingTextBox { get; init; }
    public required Slider RestitutionSlider { get; init; }
    public required TextBox RestitutionTextBox { get; init; }

    // Particle Ranges
    public required Slider MinMassSlider { get; init; }
    public required TextBox MinMassTextBox { get; init; }
    public required Slider MaxMassSlider { get; init; }
    public required TextBox MaxMassTextBox { get; init; }
    public required Slider MinRadiusSlider { get; init; }
    public required TextBox MinRadiusTextBox { get; init; }
    public required Slider MaxRadiusSlider { get; init; }
    public required TextBox MaxRadiusTextBox { get; init; }
    public required Slider MaxVelocitySlider { get; init; }
    public required TextBox MaxVelocityTextBox { get; init; }

    // Physics Toggles
    public required CheckBox UseGravityCheckBox { get; init; }
    public required CheckBox UseCollisionsCheckBox { get; init; }
    public required CheckBox UseBoundariesCheckBox { get; init; }
    public required CheckBox UseDampingCheckBox { get; init; }
    public required CheckBox UseSpatialPartitioningCheckBox { get; init; }

    // Ability Toggles
    public required CheckBox UseAbilitiesCheckBox { get; init; }
    public required CheckBox UseEatingCheckBox { get; init; }
    public required CheckBox UseSplittingCheckBox { get; init; }
    public required CheckBox UseReproductionCheckBox { get; init; }
    public required CheckBox UsePhasingCheckBox { get; init; }
    public required CheckBox UseChaseCheckBox { get; init; }
    public required CheckBox UseFleeCheckBox { get; init; }

    // Energy Parameters
    public required Slider BaseEnergySlider { get; init; }
    public required TextBox BaseEnergyTextBox { get; init; }
    public required Slider PassiveDrainSlider { get; init; }
    public required TextBox PassiveDrainTextBox { get; init; }
    public required Slider EatingGainSlider { get; init; }
    public required TextBox EatingGainTextBox { get; init; }
    public required Slider SizeRatioSlider { get; init; }
    public required TextBox SizeRatioTextBox { get; init; }
    public required Slider VisionRangeSlider { get; init; }
    public required TextBox VisionRangeTextBox { get; init; }
    public required Slider HungerThresholdSlider { get; init; }
    public required TextBox HungerThresholdTextBox { get; init; }

    // Chase/Flee Parameters
    public required Slider ChaseForceSlider { get; init; }
    public required TextBox ChaseForceTextBox { get; init; }
    public required Slider FleeForceSlider { get; init; }
    public required TextBox FleeForceTextBox { get; init; }
    public required Slider ChaseEnergyCostSlider { get; init; }
    public required TextBox ChaseEnergyCostTextBox { get; init; }
    public required Slider FleeEnergyCostSlider { get; init; }
    public required TextBox FleeEnergyCostTextBox { get; init; }

    // Splitting Parameters
    public required TextBox SplittingEnergyCostTextBox { get; init; }
    public required Slider SplittingCooldownSlider { get; init; }
    public required TextBox SplittingCooldownTextBox { get; init; }
    public required TextBox SplittingSeparationTextBox { get; init; }

    // Reproduction Parameters
    public required TextBox ReproductionEnergyCostTextBox { get; init; }
    public required Slider ReproductionCooldownSlider { get; init; }
    public required TextBox ReproductionCooldownTextBox { get; init; }
    public required TextBox ReproductionMassTransferTextBox { get; init; }
    public required TextBox ReproductionEnergyTransferTextBox { get; init; }

    // Phasing Parameters
    public required TextBox PhasingEnergyCostTextBox { get; init; }
    public required Slider PhasingCooldownSlider { get; init; }
    public required TextBox PhasingCooldownTextBox { get; init; }
    public required Slider PhasingDurationSlider { get; init; }
    public required TextBox PhasingDurationTextBox { get; init; }

    // Other Ability Cooldowns
    public required Slider EatingCooldownSlider { get; init; }
    public required TextBox EatingCooldownTextBox { get; init; }
    public required Slider ChaseCooldownSlider { get; init; }
    public required TextBox ChaseCooldownTextBox { get; init; }
    public required Slider FleeCooldownSlider { get; init; }
    public required TextBox FleeCooldownTextBox { get; init; }
    public required Slider SpeedBurstCooldownSlider { get; init; }
    public required TextBox SpeedBurstCooldownTextBox { get; init; }

    // Ability Probabilities
    public required Slider EatingProbSlider { get; init; }
    public required TextBox EatingProbTextBox { get; init; }
    public required Slider SplittingProbSlider { get; init; }
    public required TextBox SplittingProbTextBox { get; init; }
    public required Slider ReproductionProbSlider { get; init; }
    public required TextBox ReproductionProbTextBox { get; init; }
    public required Slider PhasingProbSlider { get; init; }
    public required TextBox PhasingProbTextBox { get; init; }
    public required Slider ChaseProbSlider { get; init; }
    public required TextBox ChaseProbTextBox { get; init; }
    public required Slider FleeProbSlider { get; init; }
    public required TextBox FleeProbTextBox { get; init; }

    // Type Distribution
    public required Slider PredatorProbSlider { get; init; }
    public required TextBox PredatorProbTextBox { get; init; }
    public required Slider HerbivoreProbSlider { get; init; }
    public required TextBox HerbivoreProbTextBox { get; init; }
    public required Slider SocialProbSlider { get; init; }
    public required TextBox SocialProbTextBox { get; init; }
    public required Slider SolitaryProbSlider { get; init; }
    public required TextBox SolitaryProbTextBox { get; init; }
    public required Slider NeutralProbSlider { get; init; }
    public required TextBox NeutralProbTextBox { get; init; }
}
