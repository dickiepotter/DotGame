using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class EatingAbility : IAbility
{
    private readonly SimulationConfig _config;

    public AbilityType Type => AbilityType.Eating;
    public double EnergyCost => 0; // Eating gives energy, doesn't cost it
    public double CooldownDuration => 0.5; // Half second between bites

    public EatingAbility(SimulationConfig config)
    {
        _config = config;
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities || !particle.Abilities.HasAbility(AbilitySet.Eating))
            return false;

        // Find nearby particles that can be eaten
        var prey = FindEdibleParticle(particle, context);
        return prey != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        var prey = FindEdibleParticle(particle, context);
        if (prey == null) return;

        // Check if particles are touching
        float distance = Vector2.Distance(particle.Position, prey.Position);
        if (distance > particle.Radius + prey.Radius)
            return;

        // Transfer mass and energy using configured percentages
        double oldMass = particle.Mass;
        double massGain = prey.Mass * _config.EatingMassTransfer;
        particle.Mass += massGain;

        // Update radius based on new mass (area proportional to mass)
        particle.Radius = Math.Sqrt(particle.Mass / oldMass) * particle.Radius;

        if (particle.HasAbilities && prey.HasAbilities)
        {
            // Gain energy from prey (high percentage)
            double energyGain = prey.Abilities.Energy * _config.EatingEnergyTransfer;

            // Update max energy based on new mass first
            particle.Abilities.MaxEnergy = particle.Mass * (_config.BaseEnergyCapacity / 10.0);

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

        // Mark prey for removal
        context.ParticlesToRemove.Add(prey.Id);

        // Start cooldown
        if (particle.Abilities.Cooldowns.ContainsKey(Type))
        {
            particle.Abilities.Cooldowns[Type].TimeRemaining = CooldownDuration;
        }

        // Set state
        particle.Abilities.CurrentState = AbilityState.Eating;
    }

    private Particle? FindEdibleParticle(Particle particle, AbilityContext context)
    {
        Particle? closestPrey = null;
        float closestDistance = float.MaxValue;

        foreach (var other in context.AllParticles)
        {
            if (other.Id == particle.Id) continue;
            if (!other.IsAlive) continue;

            // Cannot eat particles that are being born
            if (other.HasAbilities && other.Abilities.IsBirthing)
                continue;

            // Size check: predator must be significantly larger (1.5x+)
            if (particle.Radius < other.Radius * _config.SizeRatioForEating)
                continue;

            float distance = Vector2.Distance(particle.Position, other.Position);

            // Check if within vision range + touching distance
            double detectionRange = particle.HasAbilities ?
                particle.Abilities.VisionRange : particle.Radius * 3;

            if (distance <= detectionRange + particle.Radius + other.Radius)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = other;
                }
            }
        }

        return closestPrey;
    }

    private void InheritAbilities(Particle predator, Particle prey)
    {
        if (!predator.HasAbilities || !prey.HasAbilities) return;

        var random = new Random();
        var preyAbilities = prey.Abilities.Abilities;

        // 10% chance to inherit each ability the prey has
        foreach (AbilitySet ability in Enum.GetValues(typeof(AbilitySet)))
        {
            if (ability == AbilitySet.None) continue;

            // If prey has this ability and predator doesn't
            if (prey.Abilities.HasAbility(ability) &&
                !predator.Abilities.HasAbility(ability))
            {
                if (random.NextDouble() < 0.1) // 10% chance
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
            AbilityType.Eating => 0.5,
            AbilityType.Splitting => 5.0,
            AbilityType.Reproduction => 8.0,
            AbilityType.Phasing => 10.0,
            AbilityType.Chase => 0,
            AbilityType.Flee => 0,
            AbilityType.CustomAttraction => 0,
            AbilityType.SpeedBurst => 6.0,
            AbilityType.EnergyTransfer => 4.0,
            AbilityType.Camouflage => 12.0,
            _ => 1.0
        };
    }
}
