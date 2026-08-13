using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Simulation;

public class ParticleFactory
{
    private readonly SimulationConfig _config;
    private readonly RandomGenerator _random;
    private readonly ParticleIdGenerator _idGenerator;

    public ParticleFactory(SimulationConfig config, ParticleIdGenerator idGenerator)
    {
        _config = config;
        _random = new RandomGenerator(config.SeedFor(SimulationConfig.SeedStream.Particles));
        _idGenerator = idGenerator;
    }

    public List<Particle> CreateParticles()
    {
        var particles = new List<Particle>(_config.ParticleCount);

        for (int i = 0; i < _config.ParticleCount; i++)
        {
            var radius = _random.NextDouble(_config.MinRadius, _config.MaxRadius);

            // Position particles within bounds, accounting for radius
            var position = new Vector2(
                (float)_random.NextDouble(radius, _config.SimulationWidth - radius),
                (float)_random.NextDouble(radius, _config.SimulationHeight - radius)
            );

            particles.Add(CreateParticle(position, radius));
        }

        return particles;
    }

    /// <summary>
    /// Creates a single particle at rest at the given position - used when the user clicks
    /// to add one. Shares every code path with the startup population so that clicked-in
    /// particles are not subtly different creatures.
    /// </summary>
    public Particle CreateParticleAt(Vector2 position)
    {
        var radius = _random.NextDouble(_config.MinRadius, _config.MaxRadius);

        position.X = (float)Math.Clamp(position.X, radius, _config.SimulationWidth - radius);
        position.Y = (float)Math.Clamp(position.Y, radius, _config.SimulationHeight - radius);

        return CreateParticle(position, radius, Vector2.Zero);
    }

    private Particle CreateParticle(Vector2 position, double radius, Vector2? initialVelocity = null)
    {
        var mass = _random.NextDouble(_config.MinMass, _config.MaxMass);

        var velocity = initialVelocity ?? new Vector2(
            (float)_random.NextDouble(-_config.MaxInitialVelocity, _config.MaxInitialVelocity),
            (float)_random.NextDouble(-_config.MaxInitialVelocity, _config.MaxInitialVelocity)
        );

        var particle = new Particle
        {
            Id = _idGenerator.Next(),
            Position = position,
            Velocity = velocity,
            Mass = mass,
            Radius = radius,
            PreviousRadius = radius,
            PreviousPosition = position
        };

        // Add abilities if enabled
        if (_config.UseAbilities)
        {
            particle.Abilities = CreateRandomAbilities(mass, radius);
            // Set color based on abilities
            particle.Color = ColorGenerator.GetColorForAbilities(particle.Abilities);
        }
        else
        {
            // Fallback to mass-based color if abilities are disabled
            particle.Color = ColorGenerator.GetColorForMass(mass, _config.MinMass, _config.MaxMass);
        }

        return particle;
    }

    private ParticleAbilities CreateRandomAbilities(double mass, double radius)
    {
        double capacity = _config.EnergyCapacityForMass(mass);

        var abilities = new ParticleAbilities
        {
            Energy = capacity,
            MaxEnergy = capacity,
            Type = ChooseRandomType(),
            Generation = 0,
            Abilities = AbilitySet.None,
            CurrentState = AbilityState.Idle,
            HungerThreshold = _config.HungerThreshold,
            MovementSpeedMultiplier = 1.0
        };

        // Each particle gets its own metabolic set-points, drawn from the configured ranges.
        // These MUST be initialised: left at their default of 0 every comparison against
        // them is trivially true, so particles convert energy to mass on every single frame,
        // can never burn mass to survive starvation, and never enter energy-conservation
        // mode - the entire energy economy degenerates.
        abilities.EnergyToMassThreshold = _random.NextDouble(
            _config.EnergyToMassThresholdMin, _config.EnergyToMassThresholdMax);

        abilities.MassToEnergyThreshold = _random.NextDouble(
            _config.MassToEnergyThresholdMin, _config.MassToEnergyThresholdMax);

        abilities.EnergyAbundanceThreshold = _random.NextDouble(
            _config.EnergyAbundanceThresholdMin, _config.EnergyAbundanceThresholdMax);

        abilities.EnergyConservationThreshold = _random.NextDouble(
            _config.EnergyConservationThresholdMin, _config.EnergyConservationThresholdMax);

        // Randomly assign abilities based on probabilities
        if (_random.NextDouble(0, 1) < _config.EatingProbability)
            abilities.Abilities |= AbilitySet.Eating;

        if (_random.NextDouble(0, 1) < _config.SplittingProbability)
            abilities.Abilities |= AbilitySet.Splitting;

        if (_random.NextDouble(0, 1) < _config.ReproductionProbability)
            abilities.Abilities |= AbilitySet.Reproduction;

        if (_random.NextDouble(0, 1) < _config.PhasingProbability)
            abilities.Abilities |= AbilitySet.Phasing;

        if (_random.NextDouble(0, 1) < _config.ChaseProbability)
            abilities.Abilities |= AbilitySet.Chase;

        if (_random.NextDouble(0, 1) < _config.FleeProbability)
            abilities.Abilities |= AbilitySet.Flee;

        if (_random.NextDouble(0, 1) < _config.SpeedBurstProbability)
            abilities.Abilities |= AbilitySet.SpeedBurst;

        // Initialize cooldowns for assigned abilities
        InitializeCooldowns(abilities);

        // Vision scales with radius, matching VisionSystem.CalculateVisionRange
        abilities.VisionRange = radius * _config.VisionRangeMultiplier;

        return abilities;
    }

    private ParticleType ChooseRandomType()
    {
        double roll = _random.NextDouble(0, 1);
        double cumulative = 0;

        cumulative += _config.PredatorProbability;
        if (roll < cumulative) return ParticleType.Predator;

        cumulative += _config.HerbivoreProbability;
        if (roll < cumulative) return ParticleType.Herbivore;

        cumulative += _config.SocialProbability;
        if (roll < cumulative) return ParticleType.Social;

        cumulative += _config.SolitaryProbability;
        if (roll < cumulative) return ParticleType.Solitary;

        return ParticleType.Neutral;
    }

    private void InitializeCooldowns(ParticleAbilities abilities)
    {
        if (abilities.HasAbility(AbilitySet.Eating))
            abilities.InitializeCooldown(AbilityType.Eating, _config.EatingCooldown);

        if (abilities.HasAbility(AbilitySet.Splitting))
            abilities.InitializeCooldown(AbilityType.Splitting, _config.SplittingCooldown);

        if (abilities.HasAbility(AbilitySet.Reproduction))
            abilities.InitializeCooldown(AbilityType.Reproduction, _config.ReproductionCooldown);

        if (abilities.HasAbility(AbilitySet.Phasing))
            abilities.InitializeCooldown(AbilityType.Phasing, _config.PhasingCooldown);

        if (abilities.HasAbility(AbilitySet.SpeedBurst))
            abilities.InitializeCooldown(AbilityType.SpeedBurst, _config.SpeedBurstCooldown);

        if (abilities.HasAbility(AbilitySet.Chase))
            abilities.InitializeCooldown(AbilityType.Chase, _config.ChaseCooldown);

        if (abilities.HasAbility(AbilitySet.Flee))
            abilities.InitializeCooldown(AbilityType.Flee, _config.FleeCooldown);
    }
}
