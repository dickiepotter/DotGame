using System.Collections.Generic;
using System.Linq;
using DotGame.AI;
using DotGame.Models;

namespace DotGame.Abilities;

/// <summary>
/// Handles ability execution, AI decision-making, and particle lifecycle management.
/// </summary>
public class AbilityExecutor
{
    private readonly Dictionary<AbilityType, IAbility> _abilities;
    private readonly SimulationConfig _config;

    public AbilityExecutor(SimulationConfig config)
    {
        _config = config;
        _abilities = new Dictionary<AbilityType, IAbility>();
    }

    /// <summary>
    /// Registers an ability implementation.
    /// </summary>
    public void RegisterAbility(IAbility ability)
    {
        _abilities[ability.Type] = ability;
    }

    /// <summary>
    /// Makes AI decision about which ability to use for a particle.
    /// </summary>
    public AbilityType? MakeAbilityDecision(Particle particle, AbilityContext context)
    {
        // Use AI system to decide which ability to use
        return ParticleAI.DecideAbility(particle, context);
    }

    /// <summary>
    /// Checks if a particle can use a specific ability.
    /// </summary>
    public bool CanUseAbility(Particle particle, AbilityType abilityType)
    {
        if (!particle.HasAbilities) return false;

        var abilities = particle.Abilities;

        // Check if particle has this ability
        AbilitySet requiredAbility = abilityType switch
        {
            AbilityType.Eating => AbilitySet.Eating,
            AbilityType.Splitting => AbilitySet.Splitting,
            AbilityType.Reproduction => AbilitySet.Reproduction,
            AbilityType.Phasing => AbilitySet.Phasing,
            AbilityType.Chase => AbilitySet.Chase,
            AbilityType.Flee => AbilitySet.Flee,
            AbilityType.CustomAttraction => AbilitySet.CustomAttraction,
            AbilityType.SpeedBurst => AbilitySet.SpeedBurst,
            AbilityType.EnergyTransfer => AbilitySet.EnergyTransfer,
            AbilityType.Camouflage => AbilitySet.Camouflage,
            _ => AbilitySet.None
        };

        if (!abilities.HasAbility(requiredAbility))
            return false;

        // Check cooldown
        if (abilities.Cooldowns.TryGetValue(abilityType, out var cooldown))
        {
            if (!cooldown.IsReady)
                return false;
        }

        // Check if ability is registered
        if (!_abilities.ContainsKey(abilityType))
            return false;

        // Check energy requirement
        var ability = _abilities[abilityType];
        if (abilities.Energy < ability.EnergyCost)
            return false;

        return true;
    }

    /// <summary>
    /// Executes a specific ability for a particle.
    /// </summary>
    public void ExecuteAbility(Particle particle, AbilityType abilityType, AbilityContext context)
    {
        if (!_abilities.TryGetValue(abilityType, out var ability))
            return;

        if (ability.CanExecute(particle, context))
        {
            ability.Execute(particle, context);
        }
    }

    /// <summary>
    /// Handles particle deaths and removal/addition of particles.
    /// </summary>
    public void HandleDeaths(List<Particle> particles, AbilityContext context)
    {
        // Remove dead particles (energy <= 0)
        var deadParticles = particles.Where(p => p.HasAbilities && !p.Abilities.IsAlive).ToList();
        foreach (var dead in deadParticles)
        {
            context.ParticlesToRemove.Add(dead.Id);
        }

        // Create explosions ONLY for particles that died from energy depletion (not eaten particles)
        if (context.Renderer != null)
        {
            foreach (var dead in deadParticles)
            {
                context.Renderer.AddExplosion(dead);
            }
        }

        // Remove particles marked for removal (eaten, energy death, etc.)
        particles.RemoveAll(p => context.ParticlesToRemove.Contains(p.Id));

        // Add new particles (from splitting, reproduction)
        particles.AddRange(context.ParticlesToAdd);
        context.ParticlesToAdd.Clear();
        context.ParticlesToRemove.Clear();
    }
}
