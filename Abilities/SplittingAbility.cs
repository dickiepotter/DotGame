using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class SplittingAbility : IAbility
{
    private readonly SimulationConfig _config;
    private readonly RandomGenerator _random;
    private readonly ParticleIdGenerator _idGenerator;

    public SplittingAbility(SimulationConfig config, RandomGenerator random, ParticleIdGenerator idGenerator)
    {
        _config = config;
        _random = random;
        _idGenerator = idGenerator;
    }

    public AbilityType Type => AbilityType.Splitting;

    // The real cost is a percentage of the particle's own capacity, so it is charged in
    // CanExecute/Execute rather than through this flat interface value.
    public double EnergyCost => 0;
    public double CooldownDuration => _config.SplittingCooldown;

    /// <summary>
    /// Energy price of splitting. Scales with the particle's own capacity, but is capped at
    /// a multiple of what a reference-sized particle pays.
    ///
    /// Without the cap the cost is purely proportional to MaxEnergy, which itself grows with
    /// mass - so an overgrown particle's split cost outruns the energy it can realistically
    /// hold, and it can never split no matter how oversized it becomes.
    /// </summary>
    private static double CostFor(ParticleAbilities abilities, SimulationConfig config)
    {
        double proportional = abilities.MaxEnergy * config.SplittingEnergyCostPercent;

        double ceiling = config.EnergyCapacityForMass(config.ReferenceMass)
                         * config.SplittingEnergyCostPercent
                         * GameplayConstants.SPLIT_COST_CEILING_MULTIPLE;

        return Math.Min(proportional, ceiling) * abilities.GetEnergyCostMult();
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;

        // Respect the population ceiling. Reproduction already did this; splitting did not,
        // which is exactly the runaway the MaxParticles setting exists to prevent.
        if (context.AllParticles.Count + context.ParticlesToAdd.Count >= _config.MaxParticles)
            return false;

        // Can only split if mass is at least 2x the minimum
        double minMass = _config.MinMass;
        if (particle.Mass < minMass * 2.0) return false;

        // Check energy requirement
        if (particle.Abilities!.Energy < CostFor(particle.Abilities, _config)) return false;

        return true;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return;

        // Cost energy
        particle.Abilities!.Energy -= CostFor(particle.Abilities, _config);

        // Store original values
        double originalMass = particle.Mass;
        double originalRadius = particle.Radius;
        double originalEnergy = particle.Abilities.Energy;
        Vector2 originalPosition = particle.Position;
        Vector2 originalVelocity = particle.Velocity;

        // Split the remaining energy between the two halves. Previously the offspring was
        // handed a fresh percentage of its own MaxEnergy, unrelated to what the parent had,
        // which let splitting create energy from nothing.
        double energyPool = particle.Abilities.Energy;
        double offspringEnergy = energyPool * _config.SplittingOffspringEnergyPercentage;
        double parentEnergy = energyPool - offspringEnergy;

        // Halve the original particle mass
        particle.Mass = originalMass / 2.0;
        particle.Radius = originalRadius / Math.Sqrt(2.0); // Maintain density
        particle.Abilities.MaxEnergy = _config.EnergyCapacityForMass(particle.Mass);
        particle.Abilities.Energy = Math.Min(parentEnergy, particle.Abilities.MaxEnergy);

        // Create offspring particle (clone)
        var offspring = new Particle
        {
            Id = _idGenerator.Next(),
            Position = originalPosition,
            Velocity = originalVelocity,
            Mass = originalMass / 2.0,
            Radius = originalRadius / Math.Sqrt(2.0),
            Color = particle.Color,
            PreviousPosition = originalPosition
        };

        // Clone abilities
        offspring.Abilities = CloneAbilities(particle.Abilities, offspring.Mass, offspringEnergy);

        // Mark offspring as birthing (invulnerable during animation)
        offspring.Abilities.IsBirthing = true;
        offspring.Abilities.BirthTimeRemaining = _config.BirthAnimationDuration;
        offspring.Abilities.ParentParticleId = particle.Id;

        // Update colors based on abilities (they may differ slightly due to energy)
        particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
        offspring.Color = Utilities.ColorGenerator.GetColorForAbilities(offspring.Abilities);

        // Apply separation impulse to push particles apart. Equal and opposite impulses
        // divided by each half's mass, so the pair's total momentum is unchanged.
        Vector2 separationDirection = _random.NextUnitVector();
        float separationImpulse = (float)_config.SplittingSeparationForce;

        particle.Velocity += separationDirection * separationImpulse * (float)particle.InverseMass;
        offspring.Velocity -= separationDirection * separationImpulse * (float)offspring.InverseMass;

        // Ensure particles don't overlap by offsetting positions slightly
        float offset = (float)(particle.Radius + offspring.Radius) * 0.6f;
        particle.Position += separationDirection * offset;
        offspring.Position -= separationDirection * offset;

        // Clamp positions to boundaries
        ClampToBoundaries(particle);
        ClampToBoundaries(offspring);

        context.Audio?.Split(particle);

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

    private ParticleAbilities CloneAbilities(ParticleAbilities source, double newMass, double energy)
    {
        var clone = new ParticleAbilities
        {
            MaxEnergy = _config.EnergyCapacityForMass(newMass),
            Type = source.Type,
            Generation = source.Generation + 1,
            Abilities = source.Abilities, // Same ability set
            CurrentState = AbilityState.Idle,
            VisionRange = source.VisionRange,
            HungerThreshold = source.HungerThreshold,
            MovementSpeedMultiplier = 1.0
        };

        // Offspring receives its share of the parent's energy pool (see Execute)
        clone.Energy = Math.Min(energy, clone.MaxEnergy);

        // Inherit thresholds with random variance
        double variance = _config.ThresholdInheritanceVariance;

        clone.EnergyToMassThreshold = Math.Clamp(
            source.EnergyToMassThreshold + (_random.NextDouble() * 2 - 1) * variance,
            _config.EnergyToMassThresholdMin, _config.EnergyToMassThresholdMax);

        clone.MassToEnergyThreshold = Math.Clamp(
            source.MassToEnergyThreshold + (_random.NextDouble() * 2 - 1) * variance,
            _config.MassToEnergyThresholdMin, _config.MassToEnergyThresholdMax);

        clone.EnergyAbundanceThreshold = Math.Clamp(
            source.EnergyAbundanceThreshold + (_random.NextDouble() * 2 - 1) * variance,
            _config.EnergyAbundanceThresholdMin, _config.EnergyAbundanceThresholdMax);

        clone.EnergyConservationThreshold = Math.Clamp(
            source.EnergyConservationThreshold + (_random.NextDouble() * 2 - 1) * variance,
            _config.EnergyConservationThresholdMin, _config.EnergyConservationThresholdMax);

        // Clone cooldowns
        clone.Cooldowns = new System.Collections.Generic.Dictionary<AbilityType, CooldownTimer>();
        foreach (var kvp in source.Cooldowns)
        {
            clone.Cooldowns[kvp.Key] = new CooldownTimer(kvp.Value.Duration);
        }

        return clone;
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
