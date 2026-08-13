using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class EatingAbility : IAbility
{
    private readonly SimulationConfig _config;
    private readonly RandomGenerator _random;

    public AbilityType Type => AbilityType.Eating;
    public double EnergyCost => 0; // Eating gives energy, doesn't cost it
    public double CooldownDuration => _config.EatingCooldown;

    public EatingAbility(SimulationConfig config, RandomGenerator random)
    {
        _config = config;
        _random = random;
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities || !particle.Abilities.HasAbility(AbilitySet.Eating))
            return false;

        // Find nearby particles that can be eaten, ignoring anything already consumed this frame
        var prey = ParticleQueryUtility.FindEdiblePrey(
            particle, context.AllParticles, _config, context.ParticlesToRemove);
        return prey != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        // A predator that has itself been eaten earlier this frame cannot feed.
        if (context.ParticlesToRemove.Contains(particle.Id)) return;

        var prey = ParticleQueryUtility.FindEdiblePrey(
            particle, context.AllParticles, _config, context.ParticlesToRemove);
        if (prey == null) return;

        // Check if particles are touching
        float distance = Vector2.Distance(particle.Position, prey.Position);
        if (distance > particle.Radius + prey.Radius)
            return;

        // Claim the prey immediately so a second predator in the same frame cannot also
        // absorb it - otherwise the prey's mass and energy get duplicated.
        context.ParticlesToRemove.Add(prey.Id);

        // Transfer mass and energy using configured percentages
        double oldMass = particle.Mass;
        double massGain = prey.Mass * _config.EatingMassTransfer;
        particle.Mass += massGain;

        // Conserve momentum across the merge. The predator absorbs the prey's mass, so it
        // must also absorb its momentum: v = (m1*v1 + m2*v2) / (m1 + m2). Keeping the
        // predator's original velocity would invent momentum proportional to whatever it ate.
        Vector2 mergedMomentum = particle.Velocity * (float)oldMass + prey.Velocity * (float)massGain;
        particle.Velocity = mergedMomentum / (float)particle.Mass;

        // Update radius based on new mass (area proportional to mass)
        particle.Radius = Math.Sqrt(particle.Mass / oldMass) * particle.Radius;

        if (particle.HasAbilities && prey.HasAbilities)
        {
            // Gain energy from prey (high percentage)
            double energyGain = prey.Abilities.Energy * _config.EatingEnergyTransfer;

            // Update max energy based on new mass first
            particle.Abilities.MaxEnergy = _config.EnergyCapacityForMass(particle.Mass);

            // Add energy gain to current energy, clamped to max
            particle.Abilities.Energy = Math.Min(
                particle.Abilities.MaxEnergy,
                particle.Abilities.Energy + energyGain
            );

            // Inherit random abilities from prey (10% chance per ability)
            InheritAbilities(particle, prey);

            // Update color based on abilities (may have changed after inheritance)
            particle.Color = ColorGenerator.GetColorForAbilities(particle.Abilities);
        }
        else if (particle.HasAbilities)
        {
            // Update color even if prey has no abilities (energy changed)
            particle.Color = ColorGenerator.GetColorForAbilities(particle.Abilities);
        }
        else
        {
            // Fallback to mass-based color if no abilities
            particle.Color = ColorGenerator.GetColorForMass(
                particle.Mass,
                _config.MinMass,
                _config.MaxMass
            );
        }

        context.Audio?.Eat(particle, prey);

        // Start cooldown (prey was already claimed above)
        if (particle.Abilities.Cooldowns.TryGetValue(Type, out var eatingCooldown))
        {
            eatingCooldown.Trigger();
        }

        // Set state
        particle.Abilities.CurrentState = AbilityState.Eating;
    }


    // Chance the predator picks up each ability its prey had
    private const double ABILITY_ABSORPTION_CHANCE = 0.1;

    private void InheritAbilities(Particle predator, Particle prey)
    {
        if (!predator.HasAbilities || !prey.HasAbilities) return;

        foreach (AbilitySet ability in Enum.GetValues(typeof(AbilitySet)))
        {
            if (ability == AbilitySet.None) continue;

            // If prey has this ability and predator doesn't
            if (prey.Abilities.HasAbility(ability) &&
                !predator.Abilities.HasAbility(ability))
            {
                if (_random.NextDouble() < ABILITY_ABSORPTION_CHANCE)
                {
                    predator.Abilities.Abilities |= ability;

                    // Initialize cooldown for newly inherited ability
                    AbilityType abilityType = GetAbilityType(ability);
                    if (!predator.Abilities.Cooldowns.ContainsKey(abilityType))
                    {
                        predator.Abilities.InitializeCooldown(abilityType, GetCooldownDuration(abilityType));
                    }
                }
            }
        }
    }

    private AbilityType GetAbilityType(AbilitySet abilitySet)
    {
        return abilitySet switch
        {
            AbilitySet.Eating => AbilityType.Eating,
            AbilitySet.Splitting => AbilityType.Splitting,
            AbilitySet.Reproduction => AbilityType.Reproduction,
            AbilitySet.Phasing => AbilityType.Phasing,
            AbilitySet.Chase => AbilityType.Chase,
            AbilitySet.Flee => AbilityType.Flee,
            AbilitySet.CustomAttraction => AbilityType.CustomAttraction,
            AbilitySet.SpeedBurst => AbilityType.SpeedBurst,
            AbilitySet.EnergyTransfer => AbilityType.EnergyTransfer,
            AbilitySet.Camouflage => AbilityType.Camouflage,
            _ => AbilityType.Eating
        };
    }

    private double GetCooldownDuration(AbilityType abilityType)
    {
        return abilityType switch
        {
            AbilityType.Eating => _config.EatingCooldown,
            AbilityType.Splitting => _config.SplittingCooldown,
            AbilityType.Reproduction => _config.ReproductionCooldown,
            AbilityType.Phasing => _config.PhasingCooldown,
            AbilityType.Chase => _config.ChaseCooldown,
            AbilityType.Flee => _config.FleeCooldown,
            AbilityType.SpeedBurst => _config.SpeedBurstCooldown,
            AbilityType.CustomAttraction => 0,
            AbilityType.EnergyTransfer => 4.0,
            AbilityType.Camouflage => 12.0,
            _ => 1.0
        };
    }
}
