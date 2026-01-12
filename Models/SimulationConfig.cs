namespace DotGame.Models;

public class SimulationConfig
{
    // Simulation parameters
    public int ParticleCount { get; set; } = 50;
    public int RandomSeed { get; set; } = Environment.TickCount; // Random by default
    public double SimulationWidth { get; set; } = 800;
    public double SimulationHeight { get; set; } = 600;

    // Physics parameters
    public double GravitationalConstant { get; set; } = 100.0;
    public double DampingFactor { get; set; } = 0.995;
    public double RestitutionCoefficient { get; set; } = 0.8; // Bounciness (0-1)

    // Particle generation ranges
    public double MinMass { get; set; } = 1.0;
    public double MaxMass { get; set; } = 10.0;
    public double MinRadius { get; set; } = 5.0;
    public double MaxRadius { get; set; } = 20.0;
    public double MaxInitialVelocity { get; set; } = 50.0;

    // Feature toggles
    public bool UseGravity { get; set; } = true;
    public bool UseSpatialPartitioning { get; set; } = true;
    public bool UseCollisions { get; set; } = true;
    public bool UseBoundaries { get; set; } = true;
    public bool UseDamping { get; set; } = true;

    // Ability system toggles
    public bool UseAbilities { get; set; } = true;
    public bool UseEating { get; set; } = true;
    public bool UseSplitting { get; set; } = true;
    public bool UseReproduction { get; set; } = true;
    public bool UsePhasing { get; set; } = true;
    public bool UseChase { get; set; } = true;
    public bool UseFlee { get; set; } = true;

    // Ability parameters
    public double BaseEnergyCapacity { get; set; } = 100.0;
    public double PassiveEnergyDrain { get; set; } = 0.4; // Per second (reduced from 0.5)
    public double EatingEnergyGain { get; set; } = 0.8; // 80% of prey energy
    public double SizeRatioForEating { get; set; } = 1.25; // Must be 1.25x larger to eat
    public double VisionRangeMultiplier { get; set; } = 5.0; // vision = radius * multiplier
    public double HungerThreshold { get; set; } = 0.3; // Trigger hunting at 30% energy

    // Ability probabilities (for random assignment)
    public double EatingProbability { get; set; } = 0.7;
    public double SplittingProbability { get; set; } = 0.6;
    public double ReproductionProbability { get; set; } = 0.7;
    public double PhasingProbability { get; set; } = 0.2;
    public double ChaseProbability { get; set; } = 0.6;
    public double FleeProbability { get; set; } = 0.5;

    // Type distribution
    public double PredatorProbability { get; set; } = 0.3;
    public double HerbivoreProbability { get; set; } = 0.3;
    public double SocialProbability { get; set; } = 0.2;
    public double SolitaryProbability { get; set; } = 0.1;
    public double NeutralProbability { get; set; } = 0.1;

    // Chase/Flee parameters
    public double ChaseForce { get; set; } = 200.0;
    public double FleeForce { get; set; } = 250.0;
    public double ChaseEnergyCost { get; set; } = 0.1; // Per second
    public double FleeEnergyCost { get; set; } = 0.15; // Per second

    // Splitting parameters
    public double SplittingEnergyCost { get; set; } = 50.0; // 50% of current energy
    public double SplittingCooldown { get; set; } = 3.0; // Seconds (reduced from 5.0)
    public double SplittingSeparationForce { get; set; } = 100.0; // Push apart velocity

    // Reproduction parameters
    public double ReproductionEnergyCost { get; set; } = 40.0; // 40% of current energy
    public double ReproductionCooldown { get; set; } = 5.0; // Seconds (reduced from 8.0)
    public double ReproductionMassTransfer { get; set; } = 0.3; // 30% of parent mass
    public double ReproductionEnergyTransfer { get; set; } = 0.5; // 50% of parent energy (scaled)

    // Phasing parameters
    public double PhasingEnergyCost { get; set; } = 30.0; // Legacy fixed cost (deprecated)
    public double PhasingEnergyCostPercent { get; set; } = 0.4; // 40% of max energy
    public double PhasingCooldown { get; set; } = 10.0; // Seconds
    public double PhasingDuration { get; set; } = 2.0; // Seconds of phasing

    // Other ability cooldowns
    public double EatingCooldown { get; set; } = 0.5; // Seconds
    public double ChaseCooldown { get; set; } = 0.0; // Seconds (0 = no cooldown)
    public double FleeCooldown { get; set; } = 0.0; // Seconds (0 = no cooldown)
    public double SpeedBurstCooldown { get; set; } = 7.0; // Seconds

    // Splitting percentage-based costs
    public double SplittingEnergyCostPercent { get; set; } = 0.6; // 60% of max energy

    // Reproduction percentage-based costs
    public double ReproductionEnergyCostPercent { get; set; } = 0.5; // 50% of max energy

    // Max particles limit (prevent infinite splitting)
    public int MaxParticles { get; set; } = 1000;

    // Birth animation parameters
    public double BirthAnimationDuration { get; set; } = 1.0; // Seconds

    // Ambient energy parameters
    public bool UseAmbientEnergy { get; set; } = true;
    public double AmbientEnergyGainRate { get; set; } = 0.5; // Energy per second (ambient gain)

    // Energy-Mass Conversion Parameters
    public double EnergyToMassConversionRate { get; set; } = 0.1; // Energy units converted to mass per second
    public double MassToEnergyConversionRate { get; set; } = 0.05; // Mass units converted to energy per second
    public double EnergyToMassThresholdMin { get; set; } = 0.8; // Min threshold for energy->mass (80-95%)
    public double EnergyToMassThresholdMax { get; set; } = 0.95;
    public double MassToEnergyThresholdMin { get; set; } = 0.05; // Min threshold for mass->energy (5-15%)
    public double MassToEnergyThresholdMax { get; set; } = 0.15;
    public double EnergyAbundanceThresholdMin { get; set; } = 0.7; // Min threshold for speed boost (70-85%)
    public double EnergyAbundanceThresholdMax { get; set; } = 0.85;
    public double EnergyConservationThresholdMin { get; set; } = 0.2; // Min threshold for speed reduction (20-35%)
    public double EnergyConservationThresholdMax { get; set; } = 0.35;
    public double ThresholdInheritanceVariance { get; set; } = 0.1; // +/- 10% variance when inheriting
    public double MinParticleMass { get; set; } = 0.5; // Minimum mass before particle dies
    public double MovementSpeedMultiplierMax { get; set; } = 2.0; // Maximum speed multiplier
    public double MovementSpeedMultiplierMin { get; set; } = 0.5; // Minimum speed multiplier

    // Splitting Energy Parameters
    public double SplittingOffspringEnergyPercentage { get; set; } = 0.8; // Offspring gets 80% of max energy

    // Reproduction Mass/Energy Transfer Parameters
    public double ReproductionMassTransferMin { get; set; } = 0.2; // Min 20% of parent mass
    public double ReproductionMassTransferMax { get; set; } = 0.4; // Max 40% of parent mass
    public double ReproductionEnergyTransferMin { get; set; } = 0.3; // Min 30% of parent energy
    public double ReproductionEnergyTransferMaxPercent { get; set; } = 0.6; // Max 60% of parent energy
    public double ReproductionChildMaxSizeRatio { get; set; } = 0.8; // Child must be < 80% of parent size

    // Eating Mass/Energy Transfer Parameters
    public double EatingMassTransfer { get; set; } = 0.85; // Predator gains 85% of prey mass
    public double EatingEnergyTransfer { get; set; } = 0.9; // Predator gains 90% of prey energy (replaces EatingEnergyGain)

    // Validation and normalization methods
    public void NormalizeTypeProbabilities()
    {
        double sum = PredatorProbability + HerbivoreProbability +
                     SocialProbability + SolitaryProbability + NeutralProbability;
        if (sum > 0)
        {
            PredatorProbability /= sum;
            HerbivoreProbability /= sum;
            SocialProbability /= sum;
            SolitaryProbability /= sum;
            NeutralProbability /= sum;
        }
    }

    public void ValidateAndClamp()
    {
        // Basic configuration
        ParticleCount = Math.Clamp(ParticleCount, 1, MaxParticles);
        RandomSeed = Math.Max(0, RandomSeed);
        SimulationWidth = Math.Clamp(SimulationWidth, 100, 10000);
        SimulationHeight = Math.Clamp(SimulationHeight, 100, 10000);
        MaxParticles = Math.Clamp(MaxParticles, 1, 10000);

        // Physics parameters
        GravitationalConstant = Math.Clamp(GravitationalConstant, 0, 2000);
        DampingFactor = Math.Clamp(DampingFactor, 0, 1);
        RestitutionCoefficient = Math.Clamp(RestitutionCoefficient, 0, 1);

        // Particle ranges
        MinMass = Math.Clamp(MinMass, 0.1, 100);
        MaxMass = Math.Clamp(MaxMass, MinMass, 100);
        MinRadius = Math.Clamp(MinRadius, 1, 100);
        MaxRadius = Math.Clamp(MaxRadius, MinRadius, 100);
        MaxInitialVelocity = Math.Clamp(MaxInitialVelocity, 0, 500);

        // Energy parameters
        BaseEnergyCapacity = Math.Clamp(BaseEnergyCapacity, 10, 1000);
        PassiveEnergyDrain = Math.Clamp(PassiveEnergyDrain, 0, 10);
        EatingEnergyGain = Math.Clamp(EatingEnergyGain, 0, 1);
        SizeRatioForEating = Math.Clamp(SizeRatioForEating, 1.0, 5.0);
        VisionRangeMultiplier = Math.Clamp(VisionRangeMultiplier, 1.0, 20.0);
        HungerThreshold = Math.Clamp(HungerThreshold, 0, 1);

        // Ability probabilities (0-1 range, but don't need to sum to 1)
        EatingProbability = Math.Clamp(EatingProbability, 0, 1);
        SplittingProbability = Math.Clamp(SplittingProbability, 0, 1);
        ReproductionProbability = Math.Clamp(ReproductionProbability, 0, 1);
        PhasingProbability = Math.Clamp(PhasingProbability, 0, 1);
        ChaseProbability = Math.Clamp(ChaseProbability, 0, 1);
        FleeProbability = Math.Clamp(FleeProbability, 0, 1);

        // Chase/Flee parameters
        ChaseForce = Math.Clamp(ChaseForce, 0, 1000);
        FleeForce = Math.Clamp(FleeForce, 0, 1000);
        ChaseEnergyCost = Math.Clamp(ChaseEnergyCost, 0, 10);
        FleeEnergyCost = Math.Clamp(FleeEnergyCost, 0, 10);

        // Splitting parameters
        SplittingEnergyCost = Math.Clamp(SplittingEnergyCost, 0, 100);
        SplittingCooldown = Math.Clamp(SplittingCooldown, 0, 60);
        SplittingSeparationForce = Math.Clamp(SplittingSeparationForce, 0, 500);

        // Reproduction parameters
        ReproductionEnergyCost = Math.Clamp(ReproductionEnergyCost, 0, 100);
        ReproductionCooldown = Math.Clamp(ReproductionCooldown, 0, 60);
        ReproductionMassTransfer = Math.Clamp(ReproductionMassTransfer, 0, 1);
        ReproductionEnergyTransfer = Math.Clamp(ReproductionEnergyTransfer, 0, 1);

        // Phasing parameters
        PhasingEnergyCost = Math.Clamp(PhasingEnergyCost, 0, 100);
        PhasingCooldown = Math.Clamp(PhasingCooldown, 0, 60);
        PhasingDuration = Math.Clamp(PhasingDuration, 0, 10);

        // Energy-Mass Conversion parameters
        EnergyToMassConversionRate = Math.Clamp(EnergyToMassConversionRate, 0, 10);
        MassToEnergyConversionRate = Math.Clamp(MassToEnergyConversionRate, 0, 10);
        EnergyToMassThresholdMin = Math.Clamp(EnergyToMassThresholdMin, 0.5, 1.0);
        EnergyToMassThresholdMax = Math.Clamp(EnergyToMassThresholdMax, EnergyToMassThresholdMin, 1.0);
        MassToEnergyThresholdMin = Math.Clamp(MassToEnergyThresholdMin, 0, 0.3);
        MassToEnergyThresholdMax = Math.Clamp(MassToEnergyThresholdMax, MassToEnergyThresholdMin, 0.5);
        EnergyAbundanceThresholdMin = Math.Clamp(EnergyAbundanceThresholdMin, 0.5, 1.0);
        EnergyAbundanceThresholdMax = Math.Clamp(EnergyAbundanceThresholdMax, EnergyAbundanceThresholdMin, 1.0);
        EnergyConservationThresholdMin = Math.Clamp(EnergyConservationThresholdMin, 0, 0.5);
        EnergyConservationThresholdMax = Math.Clamp(EnergyConservationThresholdMax, EnergyConservationThresholdMin, 0.7);
        ThresholdInheritanceVariance = Math.Clamp(ThresholdInheritanceVariance, 0, 0.5);
        MinParticleMass = Math.Clamp(MinParticleMass, 0.1, 5.0);
        MovementSpeedMultiplierMax = Math.Clamp(MovementSpeedMultiplierMax, 1.0, 5.0);
        MovementSpeedMultiplierMin = Math.Clamp(MovementSpeedMultiplierMin, 0.1, 1.0);
        SplittingOffspringEnergyPercentage = Math.Clamp(SplittingOffspringEnergyPercentage, 0.1, 1.0);
        ReproductionMassTransferMin = Math.Clamp(ReproductionMassTransferMin, 0.1, 0.5);
        ReproductionMassTransferMax = Math.Clamp(ReproductionMassTransferMax, ReproductionMassTransferMin, 0.8);
        ReproductionEnergyTransferMin = Math.Clamp(ReproductionEnergyTransferMin, 0.1, 0.5);
        ReproductionEnergyTransferMaxPercent = Math.Clamp(ReproductionEnergyTransferMaxPercent, ReproductionEnergyTransferMin, 1.0);
        ReproductionChildMaxSizeRatio = Math.Clamp(ReproductionChildMaxSizeRatio, 0.5, 0.95);
        EatingMassTransfer = Math.Clamp(EatingMassTransfer, 0.1, 1.0);
        EatingEnergyTransfer = Math.Clamp(EatingEnergyTransfer, 0.1, 1.0);
    }
}
