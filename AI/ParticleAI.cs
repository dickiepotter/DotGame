using System.Numerics;
using System.Linq;
using DotGame.Models;
using DotGame.Abilities;
using DotGame.Utilities;

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
        var threat = ParticleQueryUtility.FindThreat(particle, visible, context.Config);
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
        var prey = ParticleQueryUtility.FindEdiblePrey(particle, visible, context.Config);
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
}
