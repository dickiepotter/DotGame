using System.Collections.Generic;
using System.Windows.Media;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.Abilities;

/// <summary>
/// Manages particle state including cooldowns, temporary states, colors, and vision range.
/// </summary>
public class AbilityStateManager
{
    private readonly SimulationConfig _config;

    public AbilityStateManager(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Updates all cooldown timers for particles.
    /// </summary>
    public void UpdateCooldowns(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            foreach (var cooldown in particle.Abilities.Cooldowns.Values)
            {
                cooldown.Update(deltaTime);
            }
        }
    }

    /// <summary>
    /// Updates temporary states like phasing, speed boost, camouflage, and birth.
    /// </summary>
    public void UpdateTemporaryStates(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            var abilities = particle.Abilities;

            // Update phasing - restore idle state when expired
            if (abilities.PhasingState.Update(deltaTime))
            {
                abilities.CurrentState = AbilityState.Idle;
                // Color will be restored automatically by UpdateColors
            }

            // Update speed boost - automatically handles expiration
            abilities.SpeedBoostState.Update(deltaTime);

            // Update camouflage - restore idle state when expired
            if (abilities.CamouflageState.Update(deltaTime))
            {
                abilities.CurrentState = AbilityState.Idle;
            }

            // Update birth animation - clear parent reference when expired
            if (abilities.BirthState.Update(deltaTime))
            {
                abilities.ParentParticleId = null;
            }
        }
    }

    /// <summary>
    /// Updates particle colors based on abilities and current state.
    /// </summary>
    public void UpdateColors(List<Particle> particles)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            // Update color based on current abilities and energy level
            // This creates dynamic visual feedback as particles gain/lose energy
            var newColor = ColorGenerator.GetColorForAbilities(particle.Abilities);

            // Preserve alpha channel for phasing particles (semi-transparent)
            if (particle.Abilities.IsPhasing)
            {
                particle.Color = Color.FromArgb(RenderingConstants.PHASING_OPACITY,
                    newColor.R, newColor.G, newColor.B);
            }
            else
            {
                particle.Color = newColor;
            }
        }
    }

    /// <summary>
    /// Updates vision range for a particle based on size, energy, and type.
    /// </summary>
    public void UpdateVisionRange(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // Vision range based on size, energy, and type synergy
        double baseVision = particle.Radius * _config.VisionRangeMultiplier;
        double energyMultiplier = particle.EnergyPercentage * 0.5 + 0.5; // 0.5 to 1.0
        double typeMult = particle.Abilities.GetVisionMult(); // Type-based bonus
        particle.Abilities.VisionRange = baseVision * energyMultiplier * typeMult;
    }
}
