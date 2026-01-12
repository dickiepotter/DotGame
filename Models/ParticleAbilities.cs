using System.Collections.Generic;

namespace DotGame.Models;

[Flags]
public enum AbilitySet
{
    None = 0,
    Eating = 1 << 0,
    Splitting = 1 << 1,
    Reproduction = 1 << 2,
    Phasing = 1 << 3,
    Chase = 1 << 4,
    Flee = 1 << 5,
    CustomAttraction = 1 << 6,
    SpeedBurst = 1 << 7,
    EnergyTransfer = 1 << 8,
    Camouflage = 1 << 9
}

public enum ParticleType
{
    Predator,    // Affinity: chase smaller, eat
    Herbivore,   // Affinity: flee from larger, reproduction focus
    Neutral,     // Balanced behavior
    Social,      // Attracted to same type
    Solitary     // Repelled by others
}

public enum AbilityState
{
    Idle,
    Hunting,
    Fleeing,
    Reproducing,
    Phasing,
    Eating,
    Camouflaged,
    Splitting
}

public enum AbilityType
{
    Eating,
    Splitting,
    Reproduction,
    Phasing,
    Chase,
    Flee,
    CustomAttraction,
    SpeedBurst,
    EnergyTransfer,
    Camouflage
}

public class CooldownTimer
{
    public double TimeRemaining { get; set; }
    public double Duration { get; set; }
    public bool IsReady => TimeRemaining <= 0;

    public CooldownTimer(double duration)
    {
        Duration = duration;
        TimeRemaining = 0; // Start ready
    }

    public void Update(double deltaTime)
    {
        if (TimeRemaining > 0)
            TimeRemaining -= deltaTime;
    }

    public void Trigger()
    {
        TimeRemaining = Duration;
    }
}

public class ParticleAbilities
{
    // Energy System
    public double Energy { get; set; }
    public double MaxEnergy { get; set; }
    public bool IsAlive => Energy > 0;

    // Particle Type/DNA
    public ParticleType Type { get; set; }
    public int Generation { get; set; } // Track offspring generations

    // Ability Flags (randomly assigned at creation)
    public AbilitySet Abilities { get; set; }

    // Vision/Detection
    public double VisionRange { get; set; } // Calculated from size/energy

    // Cooldowns & State
    public Dictionary<AbilityType, CooldownTimer> Cooldowns { get; set; }
    public AbilityState CurrentState { get; set; }

    // Phasing state
    public bool IsPhasing { get; set; }
    public double PhasingTimeRemaining { get; set; }

    // Targeting
    public int? TargetParticleId { get; set; }

    // Speed Burst
    public bool IsSpeedBoosted { get; set; }
    public double SpeedBoostTimeRemaining { get; set; }

    // Camouflage
    public bool IsCamouflaged { get; set; }
    public double CamouflageTimeRemaining { get; set; }

    // Birth/Clone animation
    public bool IsBirthing { get; set; }
    public double BirthTimeRemaining { get; set; }
    public int? ParentParticleId { get; set; }

    // Energy-Mass Conversion Thresholds (individual per particle)
    public double EnergyToMassThreshold { get; set; } // When energy > this %, convert to mass
    public double MassToEnergyThreshold { get; set; } // When energy < this %, expend mass
    public double EnergyAbundanceThreshold { get; set; } // When energy > this %, increase movement
    public double EnergyConservationThreshold { get; set; } // When energy < this %, decrease movement
    public double MovementSpeedMultiplier { get; set; } // Dynamic movement speed modifier (0.5 to 2.0)

    public ParticleAbilities()
    {
        Cooldowns = new Dictionary<AbilityType, CooldownTimer>();
        CurrentState = AbilityState.Idle;
        Generation = 0;
        MovementSpeedMultiplier = 1.0; // Default normal speed
    }

    public bool HasAbility(AbilitySet ability)
    {
        return (Abilities & ability) == ability;
    }

    public void InitializeCooldown(AbilityType abilityType, double duration)
    {
        Cooldowns[abilityType] = new CooldownTimer(duration);
    }

    // Type-based synergy bonuses
    public double GetChaseForceMult() => Type switch
    {
        ParticleType.Predator => 1.3,  // +30% chase force
        ParticleType.Herbivore => 0.7, // -30% chase force
        _ => 1.0
    };

    public double GetFleeForceMult() => Type switch
    {
        ParticleType.Herbivore => 1.2,  // +20% flee force
        ParticleType.Predator => 0.8,   // -20% flee force
        _ => 1.0
    };

    public double GetEnergyCostMult() => Type switch
    {
        ParticleType.Neutral => 0.9,    // Jack-of-all-trades: -10% all costs
        _ => 1.0
    };

    public double GetReproductionMult() => Type switch
    {
        ParticleType.Herbivore => 1.3,  // +30% reproduction energy transfer
        ParticleType.Social => 1.2,     // +20% reproduction energy transfer
        _ => 1.0
    };

    public double GetVisionMult() => Type switch
    {
        ParticleType.Predator => 1.2,   // +20% vision range
        ParticleType.Solitary => 1.1,   // +10% vision range
        _ => 1.0
    };
}
