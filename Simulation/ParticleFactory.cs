using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Simulation;

public class ParticleFactory
{
    private readonly SimulationConfig _config;
    private readonly RandomGenerator _random;

    public ParticleFactory(SimulationConfig config)
    {
        _config = config;
        _random = new RandomGenerator(config.RandomSeed);
    }

    public List<Particle> CreateParticles()
    {
        var particles = new List<Particle>(_config.ParticleCount);

        for (int i = 0; i < _config.ParticleCount; i++)
        {
            var radius = _random.NextDouble(_config.MinRadius, _config.MaxRadius);
            var mass = _random.NextDouble(_config.MinMass, _config.MaxMass);

            // Position particles within bounds, accounting for radius
            var position = new Vector2(
                (float)_random.NextDouble(radius, _config.SimulationWidth - radius),
                (float)_random.NextDouble(radius, _config.SimulationHeight - radius)
            );

            var velocity = new Vector2(
                (float)_random.NextDouble(-_config.MaxInitialVelocity, _config.MaxInitialVelocity),
                (float)_random.NextDouble(-_config.MaxInitialVelocity, _config.MaxInitialVelocity)
            );

            var particle = new Particle
            {
                Id = i,
                Position = position,
                Velocity = velocity,
                Mass = mass,
                Radius = radius,
                Color = ColorGenerator.GetColorForMass(mass, _config.MinMass, _config.MaxMass),
                PreviousPosition = position
            };

            // Add abilities if enabled
            if (_config.UseAbilities)
            {
                particle.Abilities = CreateRandomAbilities(mass);
            }

            particles.Add(particle);
        }

        return particles;
    }

    private ParticleAbilities CreateRandomAbilities(double mass)
    {
        var abilities = new ParticleAbilities
        {
            Energy = mass * (_config.BaseEnergyCapacity / 10.0),
            MaxEnergy = mass * (_config.BaseEnergyCapacity / 10.0),
            Type = ChooseRandomType(),
            Generation = 0,
            Abilities = AbilitySet.None,
            CurrentState = AbilityState.Idle
        };

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

        // Initialize cooldowns for assigned abilities
        InitializeCooldowns(abilities);

        // Calculate initial vision range
        abilities.VisionRange = mass * _config.VisionRangeMultiplier;

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
            abilities.InitializeCooldown(AbilityType.Eating, 0.5);

        if (abilities.HasAbility(AbilitySet.Splitting))
            abilities.InitializeCooldown(AbilityType.Splitting, 5.0);

        if (abilities.HasAbility(AbilitySet.Reproduction))
            abilities.InitializeCooldown(AbilityType.Reproduction, 8.0);

        if (abilities.HasAbility(AbilitySet.Phasing))
            abilities.InitializeCooldown(AbilityType.Phasing, 10.0);

        // Chase and Flee don't have cooldowns (continuous abilities)
    }
}
