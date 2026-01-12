using System;
using System.Numerics;
using System.Linq;
using DotGame.Models;
using DotGame.AI;

namespace DotGame.Abilities;

public class PhasingAbility : IAbility
{
    private readonly SimulationConfig _config;

    public PhasingAbility(SimulationConfig config)
    {
        _config = config;
    }

    public AbilityType Type => AbilityType.Phasing;
    public double EnergyCost => 0; // Calculated dynamically based on max energy
    public double CooldownDuration => _config.PhasingCooldown;

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;

        // Already phasing
        if (particle.Abilities!.IsPhasing) return false;

        // Calculate energy cost as percentage of max energy
        double cost = particle.Abilities.MaxEnergy * _config.PhasingEnergyCostPercent;

        // Check energy requirement
        if (particle.Abilities.Energy < cost) return false;

        // Only phase when threatened and cornered
        var visible = VisionSystem.GetVisibleParticles(particle, context);
        var threat = FindThreat(particle, visible);

        if (threat == null) return false;

        // Check if cornered (threat is close and moving toward us)
        float distance = Vector2.Distance(particle.Position, threat.Position);
        float dangerDistance = (float)(particle.Abilities.VisionRange * 0.4);

        return distance < dangerDistance;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return;

        // Calculate and deduct energy cost
        double cost = particle.Abilities!.MaxEnergy * _config.PhasingEnergyCostPercent;
        particle.Abilities.Energy -= cost;

        // Enable phasing
        particle.Abilities.IsPhasing = true;
        particle.Abilities.PhasingTimeRemaining = _config.PhasingDuration;
        particle.Abilities.CurrentState = AbilityState.Phasing;

        // Make particle semi-transparent (visual feedback)
        particle.Color = System.Windows.Media.Color.FromArgb(128,
            particle.Color.R, particle.Color.G, particle.Color.B);

        // Trigger cooldown
        if (particle.Abilities.Cooldowns.TryGetValue(AbilityType.Phasing, out var cooldown))
        {
            cooldown.Trigger();
        }
    }

    private Particle? FindThreat(Particle particle, System.Collections.Generic.List<Particle> visible)
    {
        Particle? closestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (var other in visible)
        {
            // A threat is a particle that is significantly larger (can eat us)
            if (other.Radius >= particle.Radius * _config.SizeRatioForEating)
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
}
