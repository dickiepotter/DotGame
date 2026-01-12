using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Physics;

public class GravityCalculator
{
    private readonly SimulationConfig _config;

    public GravityCalculator(SimulationConfig config)
    {
        _config = config;
    }

    public void ApplyGravity(List<Particle> particles, double deltaTime)
    {
        // Calculate gravitational forces between all pairs of particles
        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                ApplyGravitationalForce(particles[i], particles[j], deltaTime);
            }
        }
    }

    private void ApplyGravitationalForce(Particle a, Particle b, double deltaTime)
    {
        Vector2 direction = b.Position - a.Position;
        float distanceSquared = direction.LengthSquared();

        // Avoid division by zero and excessive forces at close range
        // Minimum distance threshold prevents singularity
        if (distanceSquared < PhysicsConstants.MIN_GRAVITY_DISTANCE * PhysicsConstants.MIN_GRAVITY_DISTANCE)
            return;

        float distance = MathF.Sqrt(distanceSquared);
        direction /= distance; // Normalize

        // F = G * m1 * m2 / r²
        float forceMagnitude = (float)(_config.GravitationalConstant * a.Mass * b.Mass / distanceSquared);

        Vector2 force = direction * forceMagnitude;

        // Calculate accelerations
        Vector2 accelA = force * (float)a.InverseMass;
        Vector2 accelB = -force * (float)b.InverseMass;

        // Clamp gravity acceleration per frame to prevent it from dominating other forces
        if (accelA.Length() > PhysicsConstants.MAX_GRAVITY_ACCELERATION)
        {
            accelA = Vector2.Normalize(accelA) * PhysicsConstants.MAX_GRAVITY_ACCELERATION;
        }
        if (accelB.Length() > PhysicsConstants.MAX_GRAVITY_ACCELERATION)
        {
            accelB = Vector2.Normalize(accelB) * PhysicsConstants.MAX_GRAVITY_ACCELERATION;
        }

        // Apply acceleration (F = ma -> a = F/m)
        // Using semi-implicit Euler: update velocity based on force
        a.Velocity += accelA * (float)deltaTime;
        b.Velocity += accelB * (float)deltaTime;
    }
}
