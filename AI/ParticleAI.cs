using System.Numerics;
using System.Linq;
using DotGame.Models;
using DotGame.Abilities;

namespace DotGame.AI;

public static class ParticleAI
{
    public static AbilityType? DecideAbility(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return null;

        var abilities = particle.Abilities;

        // Get visible particles
        var visible = VisionSystem.GetVisibleParticles(particle, context);

        // Priority 1: SURVIVAL - Phase through danger if cornered
        var threat = FindThreat(particle, visible, context.Config);
        if (threat != null)
        {
            float distance = Vector2.Distance(particle.Position, threat.Position);
            float dangerDistance = (float)(particle.Abilities.VisionRange * 0.4);

            // Try to phase if very close and have the ability
            if (distance < dangerDistance && abilities.HasAbility(AbilitySet.Phasing))
            {
                return AbilityType.Phasing;
            }

            // Otherwise flee
            if (abilities.HasAbility(AbilitySet.Flee))
            {
                return AbilityType.Flee;
            }
        }

        // Priority 2: OPPORTUNISTIC - Eat if touching prey
        var prey = FindEdiblePrey(particle, visible, context.Config);
        if (prey != null && abilities.HasAbility(AbilitySet.Eating))
        {
            float distance = Vector2.Distance(particle.Position, prey.Position);
            if (distance <= particle.Radius + prey.Radius + 1.0f) // Touching or very close
            {
                return AbilityType.Eating;
            }
        }

        // Priority 3: HUNGER - Chase when hungry
        if (particle.IsHungry && prey != null && abilities.HasAbility(AbilitySet.Chase))
        {
            return AbilityType.Chase;
        }

        // Priority 4: REPRODUCTION - Reproduce when high energy
        if (particle.EnergyPercentage > 0.8 &&
            abilities.HasAbility(AbilitySet.Reproduction))
        {
            return AbilityType.Reproduction;
        }

        // Priority 5: SPLITTING - Split when too large and decent energy
        if (particle.Mass > context.Config.MaxMass * 0.7 &&
            particle.EnergyPercentage > 0.6 &&
            abilities.HasAbility(AbilitySet.Splitting))
        {
            return AbilityType.Splitting;
        }

        // Default: no ability
        return null;
    }

    private static Particle? FindThreat(Particle particle, System.Collections.Generic.List<Particle> visible, SimulationConfig config)
    {
        Particle? closestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (var other in visible)
        {
            // A threat is a particle that is significantly larger (can eat us)
            if (other.Radius >= particle.Radius * config.SizeRatioForEating)
            {
                float distance = Vector2.Distance(particle.Position, other.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestThreat = other;
                }
            }
        }

        return closestThreat;
    }

    private static Particle? FindEdiblePrey(Particle particle, System.Collections.Generic.List<Particle> visible, SimulationConfig config)
    {
        Particle? closestPrey = null;
        float closestDistance = float.MaxValue;

        foreach (var other in visible)
        {
            // Prey is a particle we can eat (we're significantly larger)
            if (particle.Radius >= other.Radius * config.SizeRatioForEating)
            {
                float distance = Vector2.Distance(particle.Position, other.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = other;
                }
            }
        }

        return closestPrey;
    }
}
