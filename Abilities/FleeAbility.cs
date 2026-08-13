using System.Numerics;
using DotGame.Models;
using DotGame.Utilities;

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
        return ParticleQueryUtility.FindThreat(particle, context.AllParticles, _config) != null;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        var threat = ParticleQueryUtility.FindThreat(particle, context.AllParticles, _config);
        if (threat == null) return;

        // Calculate direction away from threat
        Vector2 direction = particle.Position - threat.Position;
        float distance = direction.Length();

        if (distance > 0.1f)
        {
            direction = Vector2.Normalize(direction);

            // Flee force scaled by threat proximity (closer = faster flee)
            float visionRange = (float)Math.Max(particle.Abilities.VisionRange, 0.0001);
            float proximityMultiplier = 1.0f - Math.Min(1.0f, distance / visionRange);
            float baseForce = (float)_config.FleeForce * (0.5f + proximityMultiplier * 0.5f);

            // Apply type synergy bonus
            float typeMult = (float)particle.Abilities.GetFleeForceMult();
            float fleeForce = baseForce * typeMult;

            // a = F/m, calibrated so FleeForce is the acceleration felt at ReferenceMass.
            // Heavier particles are correspondingly harder to get moving.
            float massScale = (float)(_config.ReferenceMass / Math.Max(particle.Mass, 0.0001));

            particle.Velocity += direction * fleeForce * massScale * (float)context.DeltaTime;
        }

        // Drain energy continuously while fleeing
        if (particle.HasAbilities)
        {
            double costMult = particle.Abilities.GetEnergyCostMult();
            particle.Abilities.Energy -= _config.FleeEnergyCost * costMult * context.DeltaTime;
            particle.Abilities.CurrentState = AbilityState.Fleeing;
            particle.Abilities.TargetParticleId = threat.Id;
        }
    }

}
