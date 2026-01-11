using System.Numerics;
using DotGame.Models;

namespace DotGame.Abilities;

public class ChaseAbility : IAbility
{
    private readonly SimulationConfig _config;

    public AbilityType Type => AbilityType.Chase;
    public double EnergyCost => 0; // Energy is drained continuously, not on execute
    public double CooldownDuration => 0; // Continuous ability, no cooldown

    public ChaseAbility(SimulationConfig config)
    {
        _config = config;
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities || !particle.Abilities.HasAbility(AbilitySet.Chase))
            return false;

        // Chase when hungry and see smaller prey
        return particle.IsHungry && FindTarget(particle, context) != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        var target = FindTarget(particle, context);
        if (target == null) return;

        // Apply force toward target
        Vector2 direction = target.Position - particle.Position;
        float distance = direction.Length();

        if (distance > 0.1f)
        {
            direction = Vector2.Normalize(direction);

            // Chase force scaled by hunger (hungrier = faster chase)
            float hungerMultiplier = (float)(1.0 - particle.EnergyPercentage);
            float chaseForce = (float)_config.ChaseForce * (0.5f + hungerMultiplier * 0.5f);

            // Apply force
            particle.Velocity += direction * chaseForce * (float)context.DeltaTime;
        }

        // Drain energy continuously while chasing
        if (particle.HasAbilities)
        {
            particle.Abilities.Energy -= _config.ChaseEnergyCost * context.DeltaTime;
            particle.Abilities.CurrentState = AbilityState.Hunting;
            particle.Abilities.TargetParticleId = target.Id;
        }
    }

    private Particle? FindTarget(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return null;

        Particle? closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var other in context.AllParticles)
        {
            if (other.Id == particle.Id) continue;
            if (!other.IsAlive) continue;

            // Only chase particles that are smaller (can be eaten)
            if (particle.Radius < other.Radius * _config.SizeRatioForEating)
                continue;

            float distance = Vector2.Distance(particle.Position, other.Position);

            // Only chase if within vision range
            if (distance <= particle.Abilities.VisionRange)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = other;
                }
            }
        }

        return closestTarget;
    }
}
