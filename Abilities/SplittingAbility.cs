using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class SplittingAbility : IAbility
{
    private readonly SimulationConfig _config;
    private static int _nextParticleId = 10000; // Start high to avoid conflicts

    public SplittingAbility(SimulationConfig config)
    {
        _config = config;
    }

    public AbilityType Type => AbilityType.Splitting;
    public double EnergyCost => _config.SplittingEnergyCost;
    public double CooldownDuration => _config.SplittingCooldown;

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;

        // Can only split if mass is at least 2x the minimum
        double minMass = _config.MinMass;
        if (particle.Mass < minMass * 2.0) return false;

        // Check energy requirement
        if (particle.Abilities!.Energy < EnergyCost) return false;

        return true;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return;

        // Cost energy
        particle.Abilities!.Energy -= EnergyCost;

        // Store original values
        double originalMass = particle.Mass;
        double originalRadius = particle.Radius;
        double originalEnergy = particle.Abilities.Energy;
        Vector2 originalPosition = particle.Position;
        Vector2 originalVelocity = particle.Velocity;

        // Halve the original particle mass
        particle.Mass = originalMass / 2.0;
        particle.Radius = originalRadius / Math.Sqrt(2.0); // Maintain density
        particle.Abilities.MaxEnergy = particle.Mass * (_config.BaseEnergyCapacity / 10.0);

        // Parent keeps half energy (original behavior for parent)
        particle.Abilities.Energy = originalEnergy / 2.0;

        // Clamp energy to new max
        if (particle.Abilities.Energy > particle.Abilities.MaxEnergy)
            particle.Abilities.Energy = particle.Abilities.MaxEnergy;

        // Create offspring particle (clone)
        var offspring = new Particle
        {
            Id = System.Threading.Interlocked.Increment(ref _nextParticleId),
            Position = originalPosition,
            Velocity = originalVelocity,
            Mass = originalMass / 2.0,
            Radius = originalRadius / Math.Sqrt(2.0),
            Color = particle.Color,
            PreviousPosition = originalPosition
        };

        // Clone abilities
        offspring.Abilities = CloneAbilities(particle.Abilities, offspring.Mass);

        // Mark offspring as birthing (invulnerable during animation)
        offspring.Abilities.IsBirthing = true;
        offspring.Abilities.BirthTimeRemaining = _config.BirthAnimationDuration;
        offspring.Abilities.ParentParticleId = particle.Id;

        // Update colors based on abilities (they may differ slightly due to energy)
        particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
        offspring.Color = Utilities.ColorGenerator.GetColorForAbilities(offspring.Abilities);

        // Apply separation impulse to push particles apart
        Vector2 separationDirection = GenerateRandomDirection();
        float separationForce = (float)_config.SplittingSeparationForce;

        particle.Velocity += separationDirection * separationForce;
        offspring.Velocity -= separationDirection * separationForce;

        // Ensure particles don't overlap by offsetting positions slightly
        float offset = (float)(particle.Radius + offspring.Radius) * 0.6f;
        particle.Position += separationDirection * offset;
        offspring.Position -= separationDirection * offset;

        // Clamp positions to boundaries
        ClampToBoundaries(particle);
        ClampToBoundaries(offspring);

        // Add offspring to context
        context.ParticlesToAdd.Add(offspring);

        // Set state
        particle.Abilities.CurrentState = AbilityState.Splitting;
        offspring.Abilities!.CurrentState = AbilityState.Idle;

        // Trigger cooldown
        if (particle.Abilities.Cooldowns.TryGetValue(AbilityType.Splitting, out var cooldown))
        {
            cooldown.Trigger();
        }
    }

    private ParticleAbilities CloneAbilities(ParticleAbilities source, double newMass)
    {
        var clone = new ParticleAbilities
        {
            MaxEnergy = newMass * (_config.BaseEnergyCapacity / 10.0),
            Type = source.Type,
            Generation = source.Generation + 1,
            Abilities = source.Abilities, // Same ability set
            CurrentState = AbilityState.Idle,
            VisionRange = source.VisionRange
        };

        // Offspring gets fresh energy (percentage of max energy)
        clone.Energy = clone.MaxEnergy * _config.SplittingOffspringEnergyPercentage;

        // Inherit thresholds with random variance
        var random = Random.Shared;
        double variance = _config.ThresholdInheritanceVariance;

        clone.EnergyToMassThreshold = Math.Clamp(
            source.EnergyToMassThreshold + (random.NextDouble() * 2 - 1) * variance,
            _config.EnergyToMassThresholdMin, _config.EnergyToMassThresholdMax);

        clone.MassToEnergyThreshold = Math.Clamp(
            source.MassToEnergyThreshold + (random.NextDouble() * 2 - 1) * variance,
            _config.MassToEnergyThresholdMin, _config.MassToEnergyThresholdMax);

        clone.EnergyAbundanceThreshold = Math.Clamp(
            source.EnergyAbundanceThreshold + (random.NextDouble() * 2 - 1) * variance,
            _config.EnergyAbundanceThresholdMin, _config.EnergyAbundanceThresholdMax);

        clone.EnergyConservationThreshold = Math.Clamp(
            source.EnergyConservationThreshold + (random.NextDouble() * 2 - 1) * variance,
            _config.EnergyConservationThresholdMin, _config.EnergyConservationThresholdMax);

        // Clone cooldowns
        clone.Cooldowns = new System.Collections.Generic.Dictionary<AbilityType, CooldownTimer>();
        foreach (var kvp in source.Cooldowns)
        {
            clone.Cooldowns[kvp.Key] = new CooldownTimer(kvp.Value.Duration);
        }

        return clone;
    }

    private Vector2 GenerateRandomDirection()
    {
        // Generate random angle
        double angle = Random.Shared.NextDouble() * Math.PI * 2.0;
        return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
    }

    private void ClampToBoundaries(Particle particle)
    {
        float minX = (float)particle.Radius;
        float maxX = (float)(_config.SimulationWidth - particle.Radius);
        float minY = (float)particle.Radius;
        float maxY = (float)(_config.SimulationHeight - particle.Radius);

        particle.Position = new Vector2(
            Math.Clamp(particle.Position.X, minX, maxX),
            Math.Clamp(particle.Position.Y, minY, maxY)
        );
    }
}
