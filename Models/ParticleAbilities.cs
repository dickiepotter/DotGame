using System.Collections.Generic;
using static DotGame.Utilities.GameplayConstants.TypeSynergy;

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

    // Targeting
    public int? TargetParticleId { get; set; }

    // Temporary States (using TimedState pattern)
    public TimedState PhasingState { get; private set; }
    public TimedState SpeedBoostState { get; private set; }
    public TimedState CamouflageState { get; private set; }
    public TimedState BirthState { get; private set; }

    // Compatibility properties (delegates to TimedState)
    public bool IsPhasing
    {
        get => PhasingState.IsActive;
        set
        {
            if (value && !PhasingState.IsActive)
                PhasingState.Activate(0); // Activate with 0 duration if set directly
            else if (!value)
                PhasingState.Deactivate();
        }
    }

    public double PhasingTimeRemaining
    {
        get => PhasingState.TimeRemaining;
        set
        {
            if (value > 0)
                PhasingState.Activate(value);
            else
                PhasingState.Deactivate();
        }
    }

    public bool IsSpeedBoosted
    {
        get => SpeedBoostState.IsActive;
        set
        {
            if (value && !SpeedBoostState.IsActive)
                SpeedBoostState.Activate(0);
            else if (!value)
                SpeedBoostState.Deactivate();
        }
    }

    public double SpeedBoostTimeRemaining
    {
        get => SpeedBoostState.TimeRemaining;
        set
        {
            if (value > 0)
                SpeedBoostState.Activate(value);
            else
                SpeedBoostState.Deactivate();
        }
    }

    public bool IsCamouflaged
    {
        get => CamouflageState.IsActive;
        set
        {
            if (value && !CamouflageState.IsActive)
                CamouflageState.Activate(0);
            else if (!value)
                CamouflageState.Deactivate();
        }
    }

    public double CamouflageTimeRemaining
    {
        get => CamouflageState.TimeRemaining;
        set
        {
            if (value > 0)
                CamouflageState.Activate(value);
            else
                CamouflageState.Deactivate();
        }
    }

    public bool IsBirthing
    {
        get => BirthState.IsActive;
        set
        {
            if (value && !BirthState.IsActive)
                BirthState.Activate(0);
            else if (!value)
                BirthState.Deactivate();
        }
    }

    public double BirthTimeRemaining
    {
        get => BirthState.TimeRemaining;
        set
        {
            if (value > 0)
                BirthState.Activate(value);
            else
                BirthState.Deactivate();
        }
    }

    // Birth parent tracking
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

        // Initialize timed states
        PhasingState = new TimedState();
        SpeedBoostState = new TimedState();
        CamouflageState = new TimedState();
        BirthState = new TimedState();
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
        ParticleType.Predator => PREDATOR_CHASE_MULT,
        ParticleType.Herbivore => HERBIVORE_CHASE_MULT,
        _ => DEFAULT_CHASE_MULT
    };

    public double GetFleeForceMult() => Type switch
    {
        ParticleType.Herbivore => HERBIVORE_FLEE_MULT,
        ParticleType.Predator => PREDATOR_FLEE_MULT,
        _ => DEFAULT_FLEE_MULT
    };

    public double GetEnergyCostMult() => Type switch
    {
        ParticleType.Neutral => NEUTRAL_ENERGY_COST_MULT,
        _ => DEFAULT_ENERGY_COST_MULT
    };

    public double GetReproductionMult() => Type switch
    {
        ParticleType.Herbivore => HERBIVORE_REPRODUCTION_MULT,
        ParticleType.Social => SOCIAL_REPRODUCTION_MULT,
        _ => DEFAULT_REPRODUCTION_MULT
    };

    public double GetVisionMult() => Type switch
    {
        ParticleType.Predator => PREDATOR_VISION_MULT,
        ParticleType.Solitary => SOLITARY_VISION_MULT,
        _ => DEFAULT_VISION_MULT
    };
}
