using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;
using static DotGame.Utilities.GameplayConstants;

namespace DotGame.Abilities;

public class ReproductionAbility : IAbility
{
    private readonly SimulationConfig _config;
    private readonly RandomGenerator _random;
    private readonly ParticleIdGenerator _idGenerator;

    public ReproductionAbility(SimulationConfig config, RandomGenerator random, ParticleIdGenerator idGenerator)
    {
        _config = config;
        _random = random;
        _idGenerator = idGenerator;
    }

    public AbilityType Type => AbilityType.Reproduction;

    // The real cost is a percentage of the particle's own capacity, charged in
    // CanExecute/Execute rather than through this flat interface value.
    public double EnergyCost => 0;
    public double CooldownDuration => _config.ReproductionCooldown;

    /// <summary>
    /// Energy cost of bearing offspring. Types with a reproduction affinity (Herbivore,
    /// Social) pay proportionally less - this is what GetReproductionMult is for.
    /// </summary>
    private static double CostFor(ParticleAbilities abilities, SimulationConfig config) =>
        abilities.MaxEnergy * config.ReproductionEnergyCostPercent
            * abilities.GetEnergyCostMult() / abilities.GetReproductionMult();

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;

        // Can only reproduce if energy is sufficient
        if (particle.EnergyPercentage < REPRODUCTION_ENERGY_THRESHOLD) return false;

        // Must have enough mass to give to offspring
        double minMass = _config.MinMass;
        double massToGive = particle.Mass * _config.ReproductionMassTransfer;
        if (particle.Mass - massToGive < minMass) return false;

        // Check energy requirement
        if (particle.Abilities!.Energy < CostFor(particle.Abilities, _config)) return false;

        // Check particle limit
        if (context.AllParticles.Count + context.ParticlesToAdd.Count >= _config.MaxParticles)
            return false;

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

        // Calculate random mass transfer (between min and max percentage)
        double massTransferPercent = _random.NextDouble(
            _config.ReproductionMassTransferMin,
            _config.ReproductionMassTransferMax);
        double massToGive = originalMass * massTransferPercent;

        // Calculate random energy transfer (between min and max percentage).
        // This is drawn exactly ONCE and is the single source of truth for the transfer:
        // whatever the parent loses is what the offspring receives. Previously Execute and
        // InheritAbilities each drew their own percentage, so the two never agreed and
        // energy silently vanished on every birth.
        double energyTransferPercent = _random.NextDouble(
            _config.ReproductionEnergyTransferMin,
            _config.ReproductionEnergyTransferMaxPercent);
        double energyToGive = originalEnergy * energyTransferPercent;

        // Ensure child is smaller than parent (enforce size constraint)
        double maxOffspringMass = originalMass * _config.ReproductionChildMaxSizeRatio;
        if (massToGive > maxOffspringMass)
            massToGive = maxOffspringMass;

        double parentNewMass = originalMass - massToGive;
        double offspringMass = massToGive;

        // Update parent mass and radius
        particle.Mass = parentNewMass;
        particle.Radius = originalRadius * Math.Sqrt(parentNewMass / originalMass);
        particle.Abilities.MaxEnergy = _config.EnergyCapacityForMass(parentNewMass);

        // Update parent energy (loses the energy given to child)
        particle.Abilities.Energy = Math.Min(
            originalEnergy - energyToGive,
            particle.Abilities.MaxEnergy);

        // Create offspring particle
        double offspringRadius = Math.Sqrt(offspringMass / originalMass) * originalRadius;
        var offspring = new Particle
        {
            Id = _idGenerator.Next(),
            Position = originalPosition,
            Velocity = originalVelocity,
            Mass = offspringMass,
            Radius = offspringRadius,
            Color = particle.Color,
            PreviousPosition = originalPosition
        };

        // Inherit abilities from parent, carrying exactly the energy the parent gave up
        offspring.Abilities = InheritAbilities(particle.Abilities, offspringMass, energyToGive, offspringRadius);

        // Mark offspring as birthing (invulnerable during animation)
        offspring.Abilities.IsBirthing = true;
        offspring.Abilities.BirthTimeRemaining = _config.BirthAnimationDuration;
        offspring.Abilities.ParentParticleId = particle.Id;

        // Update colors based on abilities (offspring may have different abilities)
        particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
        offspring.Color = Utilities.ColorGenerator.GetColorForAbilities(offspring.Abilities);

        // Apply separation impulse to push particles apart. Equal and opposite impulses
        // divided by mass, so birth does not create net momentum for the pair.
        Vector2 separationDirection = _random.NextUnitVector();
        float separationImpulse = (float)_config.SplittingSeparationForce * 0.8f; // Gentler than a split

        particle.Velocity += separationDirection * separationImpulse * (float)particle.InverseMass;
        offspring.Velocity -= separationDirection * separationImpulse * (float)offspring.InverseMass;

        // Ensure particles don't overlap by offsetting positions
        float offset = (float)(particle.Radius + offspring.Radius) * 0.6f;
        particle.Position += separationDirection * offset * 0.3f;
        offspring.Position -= separationDirection * offset * 1.2f;

        // Clamp positions to boundaries
        ClampToBoundaries(particle);
        ClampToBoundaries(offspring);

        context.Audio?.Birth(offspring);

        // Add offspring to context
        context.ParticlesToAdd.Add(offspring);

        // Set state
        particle.Abilities.CurrentState = AbilityState.Reproducing;
        offspring.Abilities!.CurrentState = AbilityState.Idle;

        // Trigger cooldown
        if (particle.Abilities.Cooldowns.TryGetValue(AbilityType.Reproduction, out var cooldown))
        {
            cooldown.Trigger();
        }
    }

    private ParticleAbilities InheritAbilities(ParticleAbilities parent, double offspringMass,
        double transferredEnergy, double offspringRadius)
    {
        var offspring = new ParticleAbilities
        {
            MaxEnergy = _config.EnergyCapacityForMass(offspringMass),
            Type = parent.Type,
            Generation = parent.Generation + 1,
            Abilities = AbilitySet.None,
            CurrentState = AbilityState.Idle,
            HungerThreshold = parent.HungerThreshold,
            MovementSpeedMultiplier = 1.0
        };

        // The offspring receives exactly the energy the parent gave up. Any surplus beyond
        // the offspring's smaller capacity is lost as the metabolic cost of birth.
        offspring.Energy = Math.Min(transferredEnergy, offspring.MaxEnergy);

        // Vision scales with radius, matching VisionSystem.CalculateVisionRange
        offspring.VisionRange = offspringRadius * _config.VisionRangeMultiplier;

        // Inherit thresholds with random variance
        double variance = _config.ThresholdInheritanceVariance;

        offspring.EnergyToMassThreshold = Math.Clamp(
            parent.EnergyToMassThreshold + (_random.NextDouble(0, 1) * 2 - 1) * variance,
            _config.EnergyToMassThresholdMin, _config.EnergyToMassThresholdMax);

        offspring.MassToEnergyThreshold = Math.Clamp(
            parent.MassToEnergyThreshold + (_random.NextDouble(0, 1) * 2 - 1) * variance,
            _config.MassToEnergyThresholdMin, _config.MassToEnergyThresholdMax);

        offspring.EnergyAbundanceThreshold = Math.Clamp(
            parent.EnergyAbundanceThreshold + (_random.NextDouble(0, 1) * 2 - 1) * variance,
            _config.EnergyAbundanceThresholdMin, _config.EnergyAbundanceThresholdMax);

        offspring.EnergyConservationThreshold = Math.Clamp(
            parent.EnergyConservationThreshold + (_random.NextDouble(0, 1) * 2 - 1) * variance,
            _config.EnergyConservationThresholdMin, _config.EnergyConservationThresholdMax);

        // Inherit abilities with some randomness
        if (parent.HasAbility(AbilitySet.Eating) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Eating;

        if (parent.HasAbility(AbilitySet.Splitting) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Splitting;

        if (parent.HasAbility(AbilitySet.Reproduction) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Reproduction;

        if (parent.HasAbility(AbilitySet.Phasing) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Phasing;

        if (parent.HasAbility(AbilitySet.Chase) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Chase;

        if (parent.HasAbility(AbilitySet.Flee) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.Flee;

        if (parent.HasAbility(AbilitySet.SpeedBurst) && _random.NextDouble(0, 1) < ABILITY_INHERITANCE_CHANCE)
            offspring.Abilities |= AbilitySet.SpeedBurst;

        // Initialize cooldowns for inherited abilities
        offspring.Cooldowns = new System.Collections.Generic.Dictionary<AbilityType, CooldownTimer>();

        if (offspring.HasAbility(AbilitySet.Eating))
            offspring.InitializeCooldown(AbilityType.Eating, _config.EatingCooldown);

        if (offspring.HasAbility(AbilitySet.Splitting))
            offspring.InitializeCooldown(AbilityType.Splitting, _config.SplittingCooldown);

        if (offspring.HasAbility(AbilitySet.Reproduction))
            offspring.InitializeCooldown(AbilityType.Reproduction, _config.ReproductionCooldown);

        if (offspring.HasAbility(AbilitySet.Phasing))
            offspring.InitializeCooldown(AbilityType.Phasing, _config.PhasingCooldown);

        if (offspring.HasAbility(AbilitySet.SpeedBurst))
            offspring.InitializeCooldown(AbilityType.SpeedBurst, _config.SpeedBurstCooldown);

        if (offspring.HasAbility(AbilitySet.Chase))
            offspring.InitializeCooldown(AbilityType.Chase, _config.ChaseCooldown);

        if (offspring.HasAbility(AbilitySet.Flee))
            offspring.InitializeCooldown(AbilityType.Flee, _config.FleeCooldown);

        return offspring;
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
