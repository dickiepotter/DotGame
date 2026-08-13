using System.Numerics;
using System.Linq;
using DotGame.Models;
using DotGame.Abilities;
using DotGame.Utilities;

namespace DotGame.AI;

public static class ParticleAI
{
    // Energy fraction above which a particle considers reproducing
    private const double REPRODUCTION_ENERGY_THRESHOLD_AI = 0.8;

    public static AbilityType? DecideAbility(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return null;

        var abilities = particle.Abilities;

        // Get visible particles
        var visible = VisionSystem.GetVisibleParticles(particle, context);

        // Priority 0: COMMITTED EFFORT - a particle already running or hunting spends a burst
        // to close (or open) the gap. Checked before anything else because the burst is a
        // one-off with a long cooldown, and IsWorthUsing already confirms it is affordable.
        if (abilities.HasAbility(AbilitySet.SpeedBurst) &&
            SpeedBurstAbility.IsWorthUsing(particle, context.Config))
        {
            return AbilityType.SpeedBurst;
        }

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
        var prey = ParticleQueryUtility.FindEdiblePrey(
            particle, visible, context.Config, context.ParticlesToRemove);
        if (prey != null && abilities.HasAbility(AbilitySet.Eating))
        {
            float distance = Vector2.Distance(particle.Position, prey.Position);
            if (distance <= particle.Radius + prey.Radius + 1.0f) // Touching or very close
            {
                return AbilityType.Eating;
            }
        }

        // Priority 3: OVERGROWN - split once far past the intended size range, whatever the
        // energy level. This sits above hunting deliberately: a giant is permanently hungry
        // (its MaxEnergy scales with its mass, so its energy percentage is always low), and
        // if chasing outranked splitting it would hunt forever and never divide - which is
        // what turned predation into a one-way ratchet down to a handful of giants.
        if (abilities.HasAbility(AbilitySet.Splitting) &&
            particle.Mass > context.Config.MaxMass * GameplayConstants.OVERGROWN_SPLIT_MASS_RATIO)
        {
            return AbilityType.Splitting;
        }

        // Priority 4: HUNGER - Chase when hungry
        if (particle.IsHungry && prey != null && abilities.HasAbility(AbilitySet.Chase))
        {
            return AbilityType.Chase;
        }

        // Priority 5: REPRODUCTION - Reproduce when high energy
        if (particle.EnergyPercentage > REPRODUCTION_ENERGY_THRESHOLD_AI &&
            abilities.HasAbility(AbilitySet.Reproduction))
        {
            return AbilityType.Reproduction;
        }

        // Priority 6: SPLITTING - Split when moderately large and comfortably fed
        if (particle.Mass > context.Config.MaxMass * GameplayConstants.SPLIT_MASS_THRESHOLD &&
            particle.EnergyPercentage > GameplayConstants.SPLIT_ENERGY_THRESHOLD &&
            abilities.HasAbility(AbilitySet.Splitting))
        {
            return AbilityType.Splitting;
        }

        // Default: no ability
        return null;
    }
}
