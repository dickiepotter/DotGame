using DotGame.Models;

namespace DotGame.Abilities;

public class SpeedBurstAbility : IAbility
{
    private readonly SimulationConfig _config;

    public SpeedBurstAbility(SimulationConfig config)
    {
        _config = config;
    }

    public AbilityType Type => AbilityType.SpeedBurst;
    public double EnergyCost => 0; // Calculated dynamically
    public double CooldownDuration => 7.0; // 7 seconds cooldown

    public bool CanExecute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return false;
        if (particle.Abilities.IsSpeedBoosted) return false; // Already boosted

        // Calculate energy cost (20% of max energy)
        double cost = particle.Abilities.MaxEnergy * 0.2 * particle.Abilities.GetEnergyCostMult();

        // Need enough energy
        if (particle.Abilities.Energy < cost) return false;

        // Use speed burst when chasing or fleeing
        return particle.Abilities.CurrentState == AbilityState.Hunting ||
               particle.Abilities.CurrentState == AbilityState.Fleeing;
    }

    public void Execute(Particle particle, AbilityContext context)
    {
        if (!particle.HasAbilities) return;

        // Calculate and deduct energy cost
        double cost = particle.Abilities.MaxEnergy * 0.2 * particle.Abilities.GetEnergyCostMult();
        particle.Abilities.Energy -= cost;

        // Activate speed boost
        particle.Abilities.IsSpeedBoosted = true;
        particle.Abilities.SpeedBoostTimeRemaining = 3.0; // 3 seconds

        // Apply immediate velocity boost (double current velocity)
        if (particle.Velocity.Length() > 0)
        {
            particle.Velocity *= 1.5f; // 50% immediate boost
        }

        // Trigger cooldown
        if (particle.Abilities.Cooldowns.TryGetValue(AbilityType.SpeedBurst, out var cooldown))
        {
            cooldown.Trigger();
        }
    }
}
