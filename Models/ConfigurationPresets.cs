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

    public static string[] GetPresetNames()
    {
        return new[]
        {
            "Default",
            "Chaos",
            "Survival",
            "Peaceful",
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
            "Performance Test" => PerformanceTest(),
            "Tutorial" => Tutorial(),
            _ => Default()
        };
    }
}
