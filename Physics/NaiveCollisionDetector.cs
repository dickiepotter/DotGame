using System.Numerics;
using DotGame.Models;

namespace DotGame.Physics;

public class NaiveCollisionDetector : ICollisionDetector
{
    private readonly SimulationConfig _config;

    public NaiveCollisionDetector(SimulationConfig config)
    {
        _config = config;
    }

    public void DetectAndResolve(List<Particle> particles)
    {
        // O(n²) collision detection - simple but works well for < 100 particles
        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                if (AreColliding(particles[i], particles[j]))
                {
                    ResolveCollision(particles[i], particles[j]);
                }
            }
        }
    }

    private bool AreColliding(Particle a, Particle b)
    {
        // Calculate distance between particles
        float dx = a.Position.X - b.Position.X;
        float dy = a.Position.Y - b.Position.Y;
        float distanceSquared = dx * dx + dy * dy;

        // Check if distance is less than sum of radii
        float radiusSum = (float)(a.Radius + b.Radius);
        return distanceSquared < radiusSum * radiusSum;
    }

    private void ResolveCollision(Particle a, Particle b)
    {
        // Calculate collision normal
        Vector2 delta = b.Position - a.Position;
        float distance = delta.Length();

        // Avoid division by zero
        if (distance < 0.0001f)
        {
            // Particles are exactly on top of each other - push them apart randomly
            delta = new Vector2(0.01f, 0.01f);
            distance = delta.Length();
        }

        Vector2 normal = delta / distance; // Normalize

        // Separate overlapping particles
        float overlap = (float)(a.Radius + b.Radius) - distance;
        if (overlap > 0)
        {
            float totalMass = (float)(a.Mass + b.Mass);
            Vector2 separation = normal * overlap;

            // Move particles apart proportional to their masses (lighter particle moves more)
            a.Position -= separation * (float)(b.Mass / totalMass);
            b.Position += separation * (float)(a.Mass / totalMass);
        }

        // Calculate relative velocity
        Vector2 relativeVelocity = b.Velocity - a.Velocity;

        // Calculate velocity along collision normal
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

        // Don't resolve if velocities are separating
        if (velocityAlongNormal > 0)
            return;

        // Calculate impulse scalar using coefficient of restitution
        float restitution = (float)_config.RestitutionCoefficient;
        float impulseScalar = -(1 + restitution) * velocityAlongNormal;
        impulseScalar /= (float)(a.InverseMass + b.InverseMass);

        // Apply impulse
        Vector2 impulse = normal * impulseScalar;
        a.Velocity -= impulse * (float)a.InverseMass;
        b.Velocity += impulse * (float)b.InverseMass;
    }
}
