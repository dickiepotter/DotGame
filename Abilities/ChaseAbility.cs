using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

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
        return particle.IsHungry && ParticleQueryUtility.FindChaseTarget(particle, context.AllParticles, _config) != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        var target = ParticleQueryUtility.FindChaseTarget(particle, context.AllParticles, _config);
        if (target == null) return;

        // Apply force toward target
        Vector2 direction = target.Position - particle.Position;
        float distance = direction.Length();

        if (distance > 0.1f)
        {
            direction = Vector2.Normalize(direction);

            // Chase force scaled by hunger (hungrier = faster chase)
            float hungerMultiplier = (float)(1.0 - particle.EnergyPercentage);
            float baseForce = (float)_config.ChaseForce * (0.5f + hungerMultiplier * 0.5f);

            // Apply type synergy bonus
            float typeMult = (float)particle.Abilities.GetChaseForceMult();
            float chaseForce = baseForce * typeMult;

            // Apply force
            particle.Velocity += direction * chaseForce * (float)context.DeltaTime;
        }

        // Drain energy continuously while chasing
        if (particle.HasAbilities)
        {
            double costMult = particle.Abilities.GetEnergyCostMult();
            particle.Abilities.Energy -= _config.ChaseEnergyCost * costMult * context.DeltaTime;
            particle.Abilities.CurrentState = AbilityState.Hunting;
            particle.Abilities.TargetParticleId = target.Id;
        }
    }

}
