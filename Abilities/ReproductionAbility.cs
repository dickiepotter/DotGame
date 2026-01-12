using System;
using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class ReproductionAbility : IAbility
{
    private readonly SimulationConfig _config;
    private static int _nextParticleId = 20000; // Start high to avoid conflicts
    private readonly RandomGenerator _random;

    public ReproductionAbility(SimulationConfig config)
    {
        _config = config;
        _random = new RandomGenerator(config.RandomSeed + 1); // Offset seed
    }

    public AbilityType Type => AbilityType.Reproduction;
    public double EnergyCost => _config.ReproductionEnergyCost;
    public double CooldownDuration => _config.ReproductionCooldown;

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;

        // Can only reproduce if energy is sufficient (>60%)
        if (particle.EnergyPercentage < 0.6) return false;

        // Must have enough mass to give to offspring
        double minMass = _config.MinMass;
        double massToGive = particle.Mass * _config.ReproductionMassTransfer;
        if (particle.Mass - massToGive < minMass) return false;

        // Check energy requirement
        if (particle.Abilities!.Energy < EnergyCost) return false;

        // Check particle limit
        if (context.AllParticles.Count + context.ParticlesToAdd.Count >= _config.MaxParticles)
            return false;

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

        // Calculate random mass transfer (between min and max percentage)
        double massTransferPercent = _random.NextDouble(
            _config.ReproductionMassTransferMin,
            _config.ReproductionMassTransferMax);
        double massToGive = originalMass * massTransferPercent;

        // Calculate random energy transfer (between min and max percentage)
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
        particle.Abilities.MaxEnergy = parentNewMass * (_config.BaseEnergyCapacity / 10.0);

        // Update parent energy (loses the energy given to child)
        particle.Abilities.Energy = originalEnergy - energyToGive;

        // Clamp parent energy to new max
        if (particle.Abilities.Energy > particle.Abilities.MaxEnergy)
            particle.Abilities.Energy = particle.Abilities.MaxEnergy;

        // Create offspring particle
        double offspringRadius = Math.Sqrt(offspringMass / originalMass) * originalRadius;
        var offspring = new Particle
        {
            Id = System.Threading.Interlocked.Increment(ref _nextParticleId),
            Position = originalPosition,
            Velocity = originalVelocity,
            Mass = offspringMass,
            Radius = offspringRadius,
            Color = particle.Color,
            PreviousPosition = originalPosition
        };

        // Inherit abilities from parent (with some randomness)
        offspring.Abilities = InheritAbilities(particle.Abilities, offspringMass);

        // Mark offspring as birthing (invulnerable during animation)
        offspring.Abilities.IsBirthing = true;
        offspring.Abilities.BirthTimeRemaining = _config.BirthAnimationDuration;
        offspring.Abilities.ParentParticleId = particle.Id;

        // Update colors based on abilities (offspring may have different abilities)
        particle.Color = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);
        offspring.Color = Utilities.ColorGenerator.GetColorForAbilities(offspring.Abilities);

        // Apply separation impulse to push particles apart
        Vector2 separationDirection = GenerateRandomDirection();
        float separationForce = (float)_config.SplittingSeparationForce * 0.8f; // Slightly less force

        particle.Velocity += separationDirection * separationForce * 0.5f;
        offspring.Velocity -= separationDirection * separationForce;

        // Ensure particles don't overlap by offsetting positions
        float offset = (float)(particle.Radius + offspring.Radius) * 0.6f;
        particle.Position += separationDirection * offset * 0.3f;
        offspring.Position -= separationDirection * offset * 1.2f;

        // Clamp positions to boundaries
        ClampToBoundaries(particle);
        ClampToBoundaries(offspring);

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

    private ParticleAbilities InheritAbilities(ParticleAbilities parent, double offspringMass)
    {
        var offspring = new ParticleAbilities
        {
            MaxEnergy = offspringMass * (_config.BaseEnergyCapacity / 10.0),
            Type = parent.Type,
            Generation = parent.Generation + 1,
            Abilities = AbilitySet.None,
            CurrentState = AbilityState.Idle,
            VisionRange = 0
        };

        // Calculate energy for offspring: random percentage of parent's energy + random percentage of parent's mass converted to energy
        double baseEnergyTransfer = _random.NextDouble(
            _config.ReproductionEnergyTransferMin,
            _config.ReproductionEnergyTransferMaxPercent) * parent.Energy;

        // Cap energy at offspring's max energy
        offspring.Energy = Math.Min(baseEnergyTransfer, offspring.MaxEnergy);

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

        // Inherit abilities with some randomness (70% chance per ability)
        if (parent.HasAbility(AbilitySet.Eating) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Eating;

        if (parent.HasAbility(AbilitySet.Splitting) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Splitting;

        if (parent.HasAbility(AbilitySet.Reproduction) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Reproduction;

        if (parent.HasAbility(AbilitySet.Phasing) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Phasing;

        if (parent.HasAbility(AbilitySet.Chase) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Chase;

        if (parent.HasAbility(AbilitySet.Flee) && _random.NextDouble(0, 1) < 0.7)
            offspring.Abilities |= AbilitySet.Flee;

        // Initialize cooldowns for inherited abilities
        offspring.Cooldowns = new System.Collections.Generic.Dictionary<AbilityType, CooldownTimer>();

        if (offspring.HasAbility(AbilitySet.Eating))
            offspring.InitializeCooldown(AbilityType.Eating, 0.5);

        if (offspring.HasAbility(AbilitySet.Splitting))
            offspring.InitializeCooldown(AbilityType.Splitting, 5.0);

        if (offspring.HasAbility(AbilitySet.Reproduction))
            offspring.InitializeCooldown(AbilityType.Reproduction, 8.0);

        if (offspring.HasAbility(AbilitySet.Phasing))
            offspring.InitializeCooldown(AbilityType.Phasing, 10.0);

        // Calculate initial vision range
        offspring.VisionRange = offspringMass * _config.VisionRangeMultiplier;

        return offspring;
    }

    private Vector2 GenerateRandomDirection()
    {
        // Generate random angle
        double angle = _random.NextDouble(0, 1) * Math.PI * 2.0;
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
