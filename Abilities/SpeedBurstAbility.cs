using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

public class SpeedBurstAbility : IAbility
{
    private readonly SimulationConfig _config;

    public SpeedBurstAbility(SimulationConfig config)
    {
        _config = config;
    }

    public AbilityType Type => AbilityType.SpeedBurst;
    public double EnergyCost => 0; // Percentage-based, calculated per particle
    public double CooldownDuration => _config.SpeedBurstCooldown;

    /// <summary>
    /// Energy price of a burst for this particle. Public so the AI can check affordability
    /// before selecting the ability - otherwise a broke particle would keep picking a burst
    /// it cannot pay for and never get around to actually fleeing.
    /// </summary>
    public static double CostFor(ParticleAbilities abilities, SimulationConfig config) =>
        abilities.MaxEnergy * config.SpeedBurstEnergyCostPercent * abilities.GetEnergyCostMult();

    /// <summary>
    /// True when a burst would help: the particle is actively hunting or running, is not
    /// already boosted, and can afford it.
    /// </summary>
    public static bool IsWorthUsing(Particle particle, SimulationConfig config)
    {
        if (!particle.HasAbilities) return false;

        var abilities = particle.Abilities;
        if (abilities.IsSpeedBoosted) return false;
        if (abilities.Energy < CostFor(abilities, config)) return false;

        return abilities.CurrentState == AbilityState.Hunting ||
               abilities.CurrentState == AbilityState.Fleeing;
    }

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        return IsWorthUsing(particle, _config);
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return;

        // Calculate and deduct energy cost
        particle.Abilities.Energy -= CostFor(particle.Abilities, _config);

        // Raise the speed ceiling for a few seconds (applied in PhysicsEngine)
        particle.Abilities.IsSpeedBoosted = true;
        particle.Abilities.SpeedBoostTimeRemaining = GameplayConstants.SPEED_BURST_DURATION;

        // Apply an immediate kick in the current direction of travel
        if (particle.Velocity.Length() > 0)
        {
            particle.Velocity *= (float)GameplayConstants.SPEED_BURST_IMPULSE;
        }

        context.Audio?.SpeedBurst(particle);

        // Trigger cooldown
        if (particle.Abilities.Cooldowns.TryGetValue(AbilityType.SpeedBurst, out var cooldown))
        {
            cooldown.Trigger();
        }
    }
}
