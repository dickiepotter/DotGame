using System.Numerics;
using DotGame.Models;

namespace DotGame.Utilities;

/// <summary>
/// Utility class for common particle search and query operations.
/// Consolidates duplicate search logic scattered across ability classes.
/// </summary>
public static class ParticleQueryUtility
{
    /// <summary>
    /// Finds the closest edible prey particle within detection range.
    /// </summary>
    public static Particle? FindEdiblePrey(Particle predator, List<Particle> allParticles, SimulationConfig config)
    {
        if (!predator.IsAlive) return null;

        Particle? closestPrey = null;
        float closestDistance = float.MaxValue;

        double detectionRange = predator.HasAbilities
            ? predator.Abilities.VisionRange
            : predator.Radius * GameplayConstants.DEFAULT_DETECTION_RANGE_MULTIPLIER;

        foreach (var prey in allParticles)
        {
            if (prey.Id == predator.Id) continue;
            if (!prey.IsAlive) continue;

            // Cannot eat particles that are being born
            if (prey.HasAbilities && prey.Abilities.IsBirthing)
                continue;

            // Size check: predator must be significantly larger
            if (!CanEat(predator, prey, config))
                continue;

            float distance = Vector2.Distance(predator.Position, prey.Position);

            // Check if within detection range (including touching distance)
            if (distance <= detectionRange + predator.Radius + prey.Radius)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = prey;
                }
            }
        }

        return closestPrey;
    }

    /// <summary>
    /// Finds the closest chase target (smaller particle that can be eaten) within vision range.
    /// </summary>
    public static Particle? FindChaseTarget(Particle hunter, List<Particle> allParticles, SimulationConfig config)
    {
        if (!hunter.HasAbilities || !hunter.IsAlive) return null;

        Particle? closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in allParticles)
        {
            if (target.Id == hunter.Id) continue;
            if (!target.IsAlive) continue;

            // Only chase particles that can be eaten
            if (!CanEat(hunter, target, config))
                continue;

            float distance = Vector2.Distance(hunter.Position, target.Position);

            // Only chase if within vision range
            if (distance <= hunter.Abilities.VisionRange)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }
        }

        return closestTarget;
    }

    /// <summary>
    /// Finds the closest threat (larger particle that can eat this particle) within visible particles.
    /// </summary>
    public static Particle? FindThreat(Particle particle, List<Particle> visibleParticles, SimulationConfig config)
    {
        if (!particle.IsAlive) return null;

        Particle? closestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (var threat in visibleParticles)
        {
            if (threat.Id == particle.Id) continue;
            if (!threat.IsAlive) continue;

            // A particle is a threat if it can eat this particle
            if (CanEat(threat, particle, config))
            {
                float distance = Vector2.Distance(particle.Position, threat.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestThreat = threat;
                }
            }
        }

        return closestThreat;
    }

    /// <summary>
    /// Gets all particles visible to the observer within their vision range.
    /// </summary>
    public static List<Particle> GetVisibleParticles(Particle observer, List<Particle> allParticles)
    {
        if (!observer.HasAbilities) return new List<Particle>();

        var visibleParticles = new List<Particle>();
        double visionRange = observer.Abilities.VisionRange;

        foreach (var other in allParticles)
        {
            if (other.Id == observer.Id) continue;
            if (!other.IsAlive) continue;

            // Skip camouflaged particles (if camouflage is implemented)
            if (other.HasAbilities && other.Abilities.IsCamouflaged)
                continue;

            float distance = Vector2.Distance(observer.Position, other.Position);
            if (distance <= visionRange)
            {
                visibleParticles.Add(other);
            }
        }

        return visibleParticles;
    }

    /// <summary>
    /// Finds a particle at the specified position (useful for mouse/touch interaction).
    /// </summary>
    public static Particle? FindParticleAtPosition(Vector2 position, List<Particle> allParticles)
    {
        foreach (var particle in allParticles)
        {
            float dx = particle.Position.X - position.X;
            float dy = particle.Position.Y - position.Y;
            float distanceSquared = dx * dx + dy * dy;

            if (distanceSquared <= particle.Radius * particle.Radius)
            {
                return particle;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a predator can eat a prey particle based on size ratio.
    /// </summary>
    public static bool CanEat(Particle predator, Particle prey, SimulationConfig config)
    {
        if (!predator.IsAlive || !prey.IsAlive) return false;

        // Cannot eat particles that are being born
        if (prey.HasAbilities && prey.Abilities.IsBirthing)
            return false;

        // Size check: predator must be significantly larger
        return predator.Radius >= prey.Radius * config.SizeRatioForEating;
    }

    /// <summary>
    /// Gets all particles within a specific radius of a position.
    /// </summary>
    public static List<Particle> GetParticlesInRadius(Vector2 position, double radius, List<Particle> allParticles)
    {
        var particlesInRadius = new List<Particle>();

        foreach (var particle in allParticles)
        {
            if (!particle.IsAlive) continue;

            float distance = Vector2.Distance(position, particle.Position);
            if (distance <= radius)
            {
                particlesInRadius.Add(particle);
            }
        }

        return particlesInRadius;
    }
}
