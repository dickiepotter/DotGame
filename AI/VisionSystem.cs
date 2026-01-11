using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using DotGame.Models;
using DotGame.Abilities;

namespace DotGame.AI;

public static class VisionSystem
{
    public static double CalculateVisionRange(Particle particle, SimulationConfig config)
    {
        if (!particle.HasAbilities) return 0;

        // Vision range based on size and energy
        double baseVision = particle.Radius * config.VisionRangeMultiplier;
        double energyMultiplier = particle.EnergyPercentage * 0.5 + 0.5; // 0.5 to 1.0

        return baseVision * energyMultiplier;
    }

    public static List<Particle> GetVisibleParticles(Particle observer, AbilityContext context)
    {
        if (!observer.HasAbilities) return new List<Particle>();

        var visionRange = observer.Abilities.VisionRange;
        var visible = new List<Particle>();

        foreach (var other in context.AllParticles)
        {
            if (other.Id == observer.Id) continue;
            if (!other.IsAlive) continue;

            // Reduced visibility for camouflaged particles
            double effectiveVisionRange = visionRange;
            if (other.HasAbilities && other.Abilities.IsCamouflaged)
            {
                effectiveVisionRange *= 0.3;
            }

            float distance = Vector2.Distance(observer.Position, other.Position);
            if (distance <= effectiveVisionRange)
            {
                visible.Add(other);
            }
        }

        return visible;
    }
}
