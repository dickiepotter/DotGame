namespace DotGame.Models;

public static class ConfigurationPresets
{
    public static SimulationConfig Default()
    {
        // Returns the standard default configuration
        var config = new SimulationConfig();
        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig Chaos()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 150,
            MaxParticles = 2000,
            RandomSeed = Environment.TickCount,

            // High energy, fast reproduction
            BaseEnergyCapacity = 150.0,
            PassiveEnergyDrain = 0.3, // Lower drain
            EatingEnergyGain = 0.9,

            // All abilities enabled with high probabilities
            UseAbilities = true,
            EatingProbability = 0.9,
            SplittingProbability = 0.7,
            ReproductionProbability = 0.8,
            PhasingProbability = 0.5,
            ChaseProbability = 0.8,
            FleeProbability = 0.7,

            // Fast cooldowns
            SplittingCooldown = 3.0,
            ReproductionCooldown = 4.0,
            PhasingCooldown = 6.0,

            // Aggressive physics
            GravitationalConstant = 150.0,
            RestitutionCoefficient = 0.9, // Very bouncy

            // Energy-Mass Conversion (aggressive conversion rates)
            EnergyToMassConversionRate = 0.15,
            MassToEnergyConversionRate = 0.08,
            MovementSpeedMultiplierMax = 2.5,
            MovementSpeedMultiplierMin = 0.6,

            // Eating transfers (high values for chaos)
            EatingMassTransfer = 0.9,
            EatingEnergyTransfer = 0.95,

            // Reproduction (wide random ranges)
            ReproductionMassTransferMin = 0.15,
            ReproductionMassTransferMax = 0.45,
            SplittingOffspringEnergyPercentage = 0.9,

            // Balanced type distribution
            PredatorProbability = 0.3,
            HerbivoreProbability = 0.3,
            SocialProbability = 0.2,
            SolitaryProbability = 0.1,
            NeutralProbability = 0.1
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig Survival()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 40,
            MaxParticles = 500,

            // Scarce energy - survival challenge
            BaseEnergyCapacity = 80.0,
            PassiveEnergyDrain = 1.2, // High drain!
            EatingEnergyGain = 0.6, // Lower gain

            // Eating focused
            UseAbilities = true,
            EatingProbability = 0.95,
            SplittingProbability = 0.1, // Rare splitting
            ReproductionProbability = 0.2, // Rare reproduction
            PhasingProbability = 0.3,
            ChaseProbability = 0.9,
            FleeProbability = 0.9,

            // Long cooldowns
            SplittingCooldown = 10.0,
            ReproductionCooldown = 15.0,
            PhasingCooldown = 12.0,

            // Heavier gravity for challenge
            GravitationalConstant = 200.0,
            RestitutionCoefficient = 0.6,

            // Energy-Mass Conversion (survival mode - frequent mass-to-energy conversion)
            EnergyToMassConversionRate = 0.05, // Low conversion to mass
            MassToEnergyConversionRate = 0.1, // High conversion to energy when desperate
            MassToEnergyThresholdMin = 0.1, // Higher threshold = more desperate conversion
            MassToEnergyThresholdMax = 0.2,
            MinParticleMass = 0.3, // Lower minimum before death
            MovementSpeedMultiplierMax = 1.5, // Less speed variance
            MovementSpeedMultiplierMin = 0.3, // Very slow when low energy

            // Eating transfers (lower values for survival challenge)
            EatingMassTransfer = 0.75,
            EatingEnergyTransfer = 0.8,

            // Reproduction (narrow ranges, expensive)
            ReproductionMassTransferMin = 0.25,
            ReproductionMassTransferMax = 0.35,
            SplittingOffspringEnergyPercentage = 0.6,

            // Predator heavy
            PredatorProbability = 0.6,
            HerbivoreProbability = 0.2,
            SocialProbability = 0.1,
            SolitaryProbability = 0.05,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig Peaceful()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 60,
            MaxParticles = 800,

            // Plenty of energy
            BaseEnergyCapacity = 200.0,
            PassiveEnergyDrain = 0.2, // Very low drain
            EatingEnergyGain = 0.9,
            UseAmbientEnergy = true,
            AmbientEnergyGainRate = 0.8, // Higher ambient gain

            // No eating - focus on reproduction and movement
            UseAbilities = true,
            UseEating = false, // DISABLED
            EatingProbability = 0.0,
            SplittingProbability = 0.5,
            ReproductionProbability = 0.9,
            PhasingProbability = 0.4,
            ChaseProbability = 0.3, // Less chasing
            FleeProbability = 0.2, // Less fleeing

            // Moderate cooldowns
            SplittingCooldown = 5.0,
            ReproductionCooldown = 6.0,
            PhasingCooldown = 8.0,

            // Gentle physics
            GravitationalConstant = 50.0,
            DampingFactor = 0.98, // More damping
            RestitutionCoefficient = 0.7,

            // Energy-Mass Conversion (peaceful mode - frequent energy-to-mass conversion)
            EnergyToMassConversionRate = 0.12, // Higher conversion to mass
            MassToEnergyConversionRate = 0.03, // Lower mass-to-energy (rarely needed)
            EnergyToMassThresholdMin = 0.75, // Lower threshold = more frequent conversion
            EnergyToMassThresholdMax = 0.9,
            MovementSpeedMultiplierMax = 2.0,
            MovementSpeedMultiplierMin = 0.7, // Not too slow

            // Eating transfers (N/A since eating is disabled, but set defaults)
            EatingMassTransfer = 0.85,
            EatingEnergyTransfer = 0.9,

            // Reproduction (generous ranges)
            ReproductionMassTransferMin = 0.2,
            ReproductionMassTransferMax = 0.4,
            ReproductionEnergyTransferMin = 0.4,
            ReproductionEnergyTransferMaxPercent = 0.7,
            SplittingOffspringEnergyPercentage = 0.85,

            // Herbivore and social heavy
            PredatorProbability = 0.0,
            HerbivoreProbability = 0.5,
            SocialProbability = 0.4,
            SolitaryProbability = 0.05,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig PerformanceTest()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 500,
            MaxParticles = 1000,

            // Minimal physics for performance
            UseGravity = false, // DISABLED
            UseAbilities = false, // DISABLED
            UseCollisions = true,
            UseBoundaries = true,
            UseSpatialPartitioning = true, // Important for performance

            // Simple physics
            DampingFactor = 0.995,
            RestitutionCoefficient = 0.8,

            // Varied sizes for visual interest
            MinMass = 1.0,
            MaxMass = 15.0,
            MinRadius = 3.0,
            MaxRadius = 15.0,
            MaxInitialVelocity = 100.0
        };

        config.ValidateAndClamp();
        return config;
    }

    public static SimulationConfig Tutorial()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 20,
            MaxParticles = 200,

            // Clear, simple setup
            BaseEnergyCapacity = 100.0,
            PassiveEnergyDrain = 0.4,
            EatingEnergyGain = 0.8,

            // Limited abilities for learning
            UseAbilities = true,
            EatingProbability = 0.8,
            SplittingProbability = 0.3,
            ReproductionProbability = 0.3,
            PhasingProbability = 0.0, // Disabled
            ChaseProbability = 0.7,
            FleeProbability = 0.6,

            // Moderate physics
            GravitationalConstant = 100.0,
            DampingFactor = 0.995,
            RestitutionCoefficient = 0.8,

            // Clear type distribution
            PredatorProbability = 0.4,
            HerbivoreProbability = 0.4,
            SocialProbability = 0.1,
            SolitaryProbability = 0.05,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig AbundantEnergy()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 80,
            MaxParticles = 1500,

            // Extremely high energy - particles rarely die
            BaseEnergyCapacity = 300.0,
            PassiveEnergyDrain = 0.1, // Minimal drain
            EatingEnergyGain = 0.95,
            UseAmbientEnergy = true,
            AmbientEnergyGainRate = 1.5, // Strong ambient energy

            // All abilities enabled but energy costs are negligible
            UseAbilities = true,
            EatingProbability = 0.5, // Lower eating since energy is abundant
            SplittingProbability = 0.6,
            ReproductionProbability = 0.7,
            PhasingProbability = 0.5,
            ChaseProbability = 0.4, // Less aggressive
            FleeProbability = 0.3, // Less fleeing needed

            // Short cooldowns - energy is plentiful
            SplittingCooldown = 4.0,
            ReproductionCooldown = 5.0,
            PhasingCooldown = 7.0,

            // Controlled mass ranges (smaller variance)
            MinMass = 3.0,
            MaxMass = 8.0,
            MinRadius = 8.0,
            MaxRadius = 18.0,

            // Gentle physics
            GravitationalConstant = 80.0,
            DampingFactor = 0.996,
            RestitutionCoefficient = 0.75,

            // Energy-Mass Conversion (favor energy to mass since energy is abundant)
            EnergyToMassConversionRate = 0.2, // High conversion to mass
            MassToEnergyConversionRate = 0.02, // Rarely need to convert mass to energy
            EnergyToMassThresholdMin = 0.6, // Convert often
            EnergyToMassThresholdMax = 0.85,
            MinParticleMass = 0.5, // Higher minimum
            MovementSpeedMultiplierMax = 1.8,
            MovementSpeedMultiplierMin = 0.8, // Stay relatively fast

            // Eating transfers (predation has minimal effect)
            EatingMassTransfer = 0.5, // Only transfer half the mass
            EatingEnergyTransfer = 0.6, // Only transfer 60% of energy
            SizeRatioForEating = 2.0, // Must be 2x larger to eat (harder to eat)

            // Reproduction (generous)
            ReproductionMassTransferMin = 0.15,
            ReproductionMassTransferMax = 0.3,
            ReproductionEnergyTransferMin = 0.3,
            ReproductionEnergyTransferMaxPercent = 0.6,
            SplittingOffspringEnergyPercentage = 0.9,

            // Balanced distribution - no predator dominance
            PredatorProbability = 0.2,
            HerbivoreProbability = 0.3,
            SocialProbability = 0.3,
            SolitaryProbability = 0.1,
            NeutralProbability = 0.1
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig MicroWorld()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 100,
            MaxParticles = 2500,

            // Tiny particles
            MinMass = 0.5,
            MaxMass = 3.0,
            MinRadius = 2.0,
            MaxRadius = 8.0,
            MaxInitialVelocity = 120.0, // Fast moving

            // Moderate energy
            BaseEnergyCapacity = 60.0,
            PassiveEnergyDrain = 0.4,
            EatingEnergyGain = 0.85,

            // High activity
            UseAbilities = true,
            EatingProbability = 0.8,
            SplittingProbability = 0.5,
            ReproductionProbability = 0.6,
            PhasingProbability = 0.4,
            ChaseProbability = 0.75,
            FleeProbability = 0.7,

            // Fast cooldowns for micro scale
            SplittingCooldown = 3.0,
            ReproductionCooldown = 4.0,
            PhasingCooldown = 5.0,

            // Weak gravity (particles are tiny)
            GravitationalConstant = 30.0,
            DampingFactor = 0.993,
            RestitutionCoefficient = 0.85, // Bouncy

            // Energy-Mass Conversion
            EnergyToMassConversionRate = 0.08,
            MassToEnergyConversionRate = 0.08,
            MovementSpeedMultiplierMax = 2.5,
            MovementSpeedMultiplierMin = 0.5,

            // Standard transfers
            EatingMassTransfer = 0.85,
            EatingEnergyTransfer = 0.9,

            // Type distribution
            PredatorProbability = 0.35,
            HerbivoreProbability = 0.35,
            SocialProbability = 0.15,
            SolitaryProbability = 0.1,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig Giants()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 25,
            MaxParticles = 200,

            // Large particles
            MinMass = 15.0,
            MaxMass = 40.0,
            MinRadius = 20.0,
            MaxRadius = 45.0,
            MaxInitialVelocity = 30.0, // Slow moving

            // High energy capacity (giants have reserves)
            BaseEnergyCapacity = 250.0,
            PassiveEnergyDrain = 0.8, // Higher drain (big bodies)
            EatingEnergyGain = 0.85,

            // All abilities
            UseAbilities = true,
            EatingProbability = 0.9,
            SplittingProbability = 0.2, // Rare for giants
            ReproductionProbability = 0.3, // Rare for giants
            PhasingProbability = 0.2,
            ChaseProbability = 0.7,
            FleeProbability = 0.6,

            // Long cooldowns (giants are slow)
            SplittingCooldown = 12.0,
            ReproductionCooldown = 15.0,
            PhasingCooldown = 10.0,

            // Strong gravity
            GravitationalConstant = 250.0,
            DampingFactor = 0.99, // Heavy damping
            RestitutionCoefficient = 0.5, // Not very bouncy

            // Energy-Mass Conversion
            EnergyToMassConversionRate = 0.15,
            MassToEnergyConversionRate = 0.12,
            MinParticleMass = 2.0, // Higher minimum for giants
            MovementSpeedMultiplierMax = 1.5,
            MovementSpeedMultiplierMin = 0.4,

            // Standard transfers
            EatingMassTransfer = 0.8,
            EatingEnergyTransfer = 0.85,

            // Type distribution
            PredatorProbability = 0.4,
            HerbivoreProbability = 0.25,
            SocialProbability = 0.2,
            SolitaryProbability = 0.1,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig ZeroGravity()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 75,
            MaxParticles = 1000,

            // No gravity!
            UseGravity = false,
            GravitationalConstant = 0.0,

            // High restitution for bouncing fun
            RestitutionCoefficient = 0.95,
            DampingFactor = 0.999, // Very little damping

            // Standard particles
            MinMass = 2.0,
            MaxMass = 12.0,
            MinRadius = 5.0,
            MaxRadius = 20.0,
            MaxInitialVelocity = 80.0,

            // Good energy
            BaseEnergyCapacity = 150.0,
            PassiveEnergyDrain = 0.3,
            EatingEnergyGain = 0.85,
            UseAmbientEnergy = true,
            AmbientEnergyGainRate = 0.5,

            // All abilities
            UseAbilities = true,
            EatingProbability = 0.7,
            SplittingProbability = 0.5,
            ReproductionProbability = 0.6,
            PhasingProbability = 0.5,
            ChaseProbability = 0.6,
            FleeProbability = 0.6,

            // Moderate cooldowns
            SplittingCooldown = 5.0,
            ReproductionCooldown = 7.0,
            PhasingCooldown = 8.0,

            // Energy-Mass Conversion
            EnergyToMassConversionRate = 0.1,
            MassToEnergyConversionRate = 0.08,
            MovementSpeedMultiplierMax = 2.2,
            MovementSpeedMultiplierMin = 0.6,

            // Standard transfers
            EatingMassTransfer = 0.8,
            EatingEnergyTransfer = 0.85,

            // Balanced distribution
            PredatorProbability = 0.25,
            HerbivoreProbability = 0.25,
            SocialProbability = 0.25,
            SolitaryProbability = 0.15,
            NeutralProbability = 0.1
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static SimulationConfig PredatorParadise()
    {
        var config = new SimulationConfig
        {
            ParticleCount = 60,
            MaxParticles = 800,

            // High energy for constant hunting
            BaseEnergyCapacity = 120.0,
            PassiveEnergyDrain = 0.6,
            EatingEnergyGain = 0.9,

            // Predation focused
            UseAbilities = true,
            EatingProbability = 0.95,
            SplittingProbability = 0.4,
            ReproductionProbability = 0.5,
            PhasingProbability = 0.6, // Lots of phasing to escape
            ChaseProbability = 0.95, // Constant hunting
            FleeProbability = 0.95, // Constant fleeing

            // Increased chase/flee forces
            ChaseForce = 300.0,
            FleeForce = 350.0,
            ChaseEnergyCost = 0.2,
            FleeEnergyCost = 0.25,

            // Moderate cooldowns
            SplittingCooldown = 6.0,
            ReproductionCooldown = 8.0,
            PhasingCooldown = 8.0,

            // Standard mass
            MinMass = 2.0,
            MaxMass = 12.0,
            MinRadius = 5.0,
            MaxRadius = 22.0,

            // Active physics
            GravitationalConstant = 120.0,
            DampingFactor = 0.994,
            RestitutionCoefficient = 0.75,

            // High vision range
            VisionRangeMultiplier = 7.0,

            // Energy-Mass Conversion (balanced)
            EnergyToMassConversionRate = 0.1,
            MassToEnergyConversionRate = 0.08,
            MovementSpeedMultiplierMax = 2.5,
            MovementSpeedMultiplierMin = 0.5,

            // High eating transfers (winner takes all)
            EatingMassTransfer = 0.9,
            EatingEnergyTransfer = 0.95,
            SizeRatioForEating = 1.4, // Easier to eat

            // Reproduction
            ReproductionMassTransferMin = 0.2,
            ReproductionMassTransferMax = 0.35,
            SplittingOffspringEnergyPercentage = 0.7,

            // Predator dominated
            PredatorProbability = 0.7,
            HerbivoreProbability = 0.15,
            SocialProbability = 0.05,
            SolitaryProbability = 0.05,
            NeutralProbability = 0.05
        };

        config.ValidateAndClamp();
        config.NormalizeTypeProbabilities();
        return config;
    }

    public static string[] GetPresetNames()
    {
        return new[]
        {
            "Default",
            "Chaos",
            "Survival",
            "Peaceful",
            "Abundant Energy",
            "Micro World",
            "Giants",
            "Zero Gravity",
            "Predator Paradise",
            "Performance Test",
            "Tutorial"
        };
    }

    public static SimulationConfig GetPreset(string name)
    {
        return name switch
        {
            "Chaos" => Chaos(),
            "Survival" => Survival(),
            "Peaceful" => Peaceful(),
            "Abundant Energy" => AbundantEnergy(),
            "Micro World" => MicroWorld(),
            "Giants" => Giants(),
            "Zero Gravity" => ZeroGravity(),
            "Predator Paradise" => PredatorParadise(),
            "Performance Test" => PerformanceTest(),
            "Tutorial" => Tutorial(),
            _ => Default()
        };
    }
}
