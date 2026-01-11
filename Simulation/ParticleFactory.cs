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

            particles.Add(new Particle
            {
                Id = i,
                Position = position,
                Velocity = velocity,
                Mass = mass,
                Radius = radius,
                Color = ColorGenerator.GetColorForMass(mass, _config.MinMass, _config.MaxMass),
                PreviousPosition = position
            });
        }

        return particles;
    }
}
