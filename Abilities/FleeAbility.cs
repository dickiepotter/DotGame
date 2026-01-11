using System.Numerics;
using DotGame.Models;

namespace DotGame.Abilities;

public class FleeAbility : IAbility
{
    private readonly SimulationConfig _config;

    public AbilityType Type => AbilityType.Flee;
    public double EnergyCost => 0; // Energy is drained continuously
    public double CooldownDuration => 0; // Continuous ability

    public FleeAbility(SimulationConfig config)
    {
        _config = config;
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities || !particle.Abilities.HasAbility(AbilitySet.Flee))
            return false;

        // Flee from larger predators
        return FindThreat(particle, context) != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        var threat = FindThreat(particle, context);
        if (threat == null) return;

        // Calculate direction away from threat
        Vector2 direction = particle.Position - threat.Position;
        float distance = direction.Length();

        if (distance > 0.1f)
        {
            direction = Vector2.Normalize(direction);

            // Flee force scaled by threat proximity (closer = faster flee)
            float proximityMultiplier = 1.0f - Math.Min(1.0f, distance / (float)particle.Abilities.VisionRange);
            float fleeForce = (float)_config.FleeForce * (0.5f + proximityMultiplier * 0.5f);

            // Apply force
            particle.Velocity += direction * fleeForce * (float)context.DeltaTime;
        }

        // Drain energy continuously while fleeing
        if (particle.HasAbilities)
        {
            particle.Abilities.Energy -= _config.FleeEnergyCost * context.DeltaTime;
            particle.Abilities.CurrentState = AbilityState.Fleeing;
            particle.Abilities.TargetParticleId = threat.Id;
        }
    }

    private Particle? FindThreat(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return null;

        Particle? closestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (var other in context.AllParticles)
        {
            if (other.Id == particle.Id) continue;
            if (!other.IsAlive) continue;

            // A threat is a particle that is significantly larger (can eat us)
            if (other.Radius < particle.Radius * _config.SizeRatioForEating)
                continue;

            float distance = Vector2.Distance(particle.Position, other.Position);

            // Only flee if within vision range
            if (distance <= particle.Abilities.VisionRange)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestThreat = other;
                }
            }
        }

        return closestThreat;
    }
}
