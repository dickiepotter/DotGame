using System.Numerics;
using DotGame.Models;

namespace DotGame.Physics;

public class SpatialHashGrid : ICollisionDetector
{
    private readonly SimulationConfig _config;
    private readonly double _cellSize;
    private Dictionary<int, List<Particle>> _grid;

    public SpatialHashGrid(SimulationConfig config, double maxParticleRadius)
    {
        _config = config;
        // Optimal cell size is 2x the maximum particle radius
        _cellSize = maxParticleRadius * 2;
        _grid = new Dictionary<int, List<Particle>>();
    }

    public void DetectAndResolve(List<Particle> particles)
    {
        // Clear and rebuild the grid
        BuildGrid(particles);

        // Check collisions only within same and neighboring cells
        HashSet<(int, int)> checkedPairs = new();

        foreach (var particle in particles)
        {
            var candidates = GetNearbyParticles(particle);

            foreach (var other in candidates)
            {
                // Ensure we don't check the same pair twice
                int minId = Math.Min(particle.Id, other.Id);
                int maxId = Math.Max(particle.Id, other.Id);

                if (particle.Id != other.Id && !checkedPairs.Contains((minId, maxId)))
                {
                    checkedPairs.Add((minId, maxId));

                    if (AreColliding(particle, other))
                    {
                        ResolveCollision(particle, other);
                    }
                }
            }
        }
    }

    private void BuildGrid(List<Particle> particles)
    {
        _grid.Clear();

        foreach (var particle in particles)
        {
            int cellHash = GetCellHash(particle.Position);

            if (!_grid.ContainsKey(cellHash))
                _grid[cellHash] = new List<Particle>();

            _grid[cellHash].Add(particle);
        }
    }

    private List<Particle> GetNearbyParticles(Particle particle)
    {
        var nearby = new List<Particle>();

        // Get cell coordinates
        int cellX = (int)(particle.Position.X / _cellSize);
        int cellY = (int)(particle.Position.Y / _cellSize);

        // Check 9 cells (current + 8 neighbors)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int neighborX = cellX + dx;
                int neighborY = cellY + dy;
                int hash = HashCell(neighborX, neighborY);

                if (_grid.TryGetValue(hash, out var cellParticles))
                {
                    nearby.AddRange(cellParticles);
                }
            }
        }

        return nearby;
    }

    private int GetCellHash(Vector2 position)
    {
        int x = (int)(position.X / _cellSize);
        int y = (int)(position.Y / _cellSize);
        return HashCell(x, y);
    }

    private int HashCell(int x, int y)
    {
        // Use cantor pairing-like hash function
        // This produces a unique hash for each (x, y) pair
        return x * 73856093 ^ y * 19349663;
    }

    private bool AreColliding(Particle a, Particle b)
    {
        float dx = a.Position.X - b.Position.X;
        float dy = a.Position.Y - b.Position.Y;
        float distanceSquared = dx * dx + dy * dy;

        float radiusSum = (float)(a.Radius + b.Radius);
        return distanceSquared < radiusSum * radiusSum;
    }

    private void ResolveCollision(Particle a, Particle b)
    {
        // Same collision resolution as NaiveCollisionDetector
        Vector2 delta = b.Position - a.Position;
        float distance = delta.Length();

        if (distance < 0.0001f)
        {
            delta = new Vector2(0.01f, 0.01f);
            distance = delta.Length();
        }

        Vector2 normal = delta / distance;

        float overlap = (float)(a.Radius + b.Radius) - distance;
        if (overlap > 0)
        {
            float totalMass = (float)(a.Mass + b.Mass);
            Vector2 separation = normal * overlap;

            a.Position -= separation * (float)(b.Mass / totalMass);
            b.Position += separation * (float)(a.Mass / totalMass);
        }

        Vector2 relativeVelocity = b.Velocity - a.Velocity;
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

        if (velocityAlongNormal > 0)
            return;

        float restitution = (float)_config.RestitutionCoefficient;
        float impulseScalar = -(1 + restitution) * velocityAlongNormal;
        impulseScalar /= (float)(a.InverseMass + b.InverseMass);

        Vector2 impulse = normal * impulseScalar;
        a.Velocity -= impulse * (float)a.InverseMass;
        b.Velocity += impulse * (float)b.InverseMass;
    }
}
