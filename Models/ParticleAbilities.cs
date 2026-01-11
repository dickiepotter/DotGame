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

    public ParticleAbilities()
    {
        Cooldowns = new Dictionary<AbilityType, CooldownTimer>();
        CurrentState = AbilityState.Idle;
        Generation = 0;
    }

    public bool HasAbility(AbilitySet ability)
    {
        return (Abilities & ability) == ability;
    }

    public void InitializeCooldown(AbilityType abilityType, double duration)
    {
        Cooldowns[abilityType] = new CooldownTimer(duration);
    }
}
