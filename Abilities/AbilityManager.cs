using System.Collections.Generic;
using System.Linq;
using DotGame.Models;

namespace DotGame.Abilities;

public class AbilityManager
{
    private readonly Dictionary<AbilityType, IAbility> _abilities;
    private readonly SimulationConfig _config;

    public AbilityManager(SimulationConfig config)
    {
        _config = config;
        _abilities = new Dictionary<AbilityType, IAbility>();

        // Register implemented abilities
        RegisterAbility(new EatingAbility(config));
        RegisterAbility(new ChaseAbility(config));
        RegisterAbility(new FleeAbility(config));
        RegisterAbility(new SplittingAbility(config));
        RegisterAbility(new ReproductionAbility(config));
        RegisterAbility(new PhasingAbility(config));
    }

    public void UpdateAbilities(List<Particle> particles, AbilityContext context)
    {
        // Update cooldowns
        UpdateCooldowns(particles, context.DeltaTime);

        // Drain passive energy
        DrainPassiveEnergy(particles, context.DeltaTime);

        // Update temporary states (phasing, speed boost, camouflage)
        UpdateTemporaryStates(particles, context.DeltaTime);

        // AI decision making & ability execution
        foreach (var particle in particles.ToList())
        {
            if (!particle.HasAbilities || !particle.Abilities.IsAlive)
                continue;

            // Update vision range
            UpdateVisionRange(particle);

            // AI decides which ability to use (will implement later)
            var decision = MakeAbilityDecision(particle, context);

            if (decision.HasValue && CanUseAbility(particle, decision.Value))
            {
                ExecuteAbility(particle, decision.Value, context);
            }
        }

        // Handle particle deaths and apply changes
        HandleDeaths(particles, context);
    }

    private void UpdateCooldowns(List<Particle> particles, double deltaTime)
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

    private void DrainPassiveEnergy(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            // Passive energy drain (cost of living)
            particle.Abilities.Energy -= _config.PassiveEnergyDrain * deltaTime;

            // Ensure energy doesn't go negative
            if (particle.Abilities.Energy < 0)
                particle.Abilities.Energy = 0;
        }
    }

    private void UpdateTemporaryStates(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            var abilities = particle.Abilities;

            // Update phasing
            if (abilities.IsPhasing)
            {
                abilities.PhasingTimeRemaining -= deltaTime;
                if (abilities.PhasingTimeRemaining <= 0)
                {
                    abilities.IsPhasing = false;
                    abilities.CurrentState = AbilityState.Idle;
                    // Restore normal color (remove transparency)
                    particle.Color = System.Windows.Media.Color.FromArgb(255,
                        particle.Color.R, particle.Color.G, particle.Color.B);
                }
            }

            // Update speed boost
            if (abilities.IsSpeedBoosted)
            {
                abilities.SpeedBoostTimeRemaining -= deltaTime;
                if (abilities.SpeedBoostTimeRemaining <= 0)
                {
                    abilities.IsSpeedBoosted = false;
                }
            }

            // Update camouflage
            if (abilities.IsCamouflaged)
            {
                abilities.CamouflageTimeRemaining -= deltaTime;
                if (abilities.CamouflageTimeRemaining <= 0)
                {
                    abilities.IsCamouflaged = false;
                    abilities.CurrentState = AbilityState.Idle;
                }
            }
        }
    }

    private void UpdateVisionRange(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // Vision range based on size and energy
        double baseVision = particle.Radius * _config.VisionRangeMultiplier;
        double energyMultiplier = particle.EnergyPercentage * 0.5 + 0.5; // 0.5 to 1.0
        particle.Abilities.VisionRange = baseVision * energyMultiplier;
    }

    private AbilityType? MakeAbilityDecision(Particle particle, AbilityContext context)
    {
        // Use AI system to decide which ability to use
        return AI.ParticleAI.DecideAbility(particle, context);
    }

    private bool CanUseAbility(Particle particle, AbilityType abilityType)
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

    private void ExecuteAbility(Particle particle, AbilityType abilityType, AbilityContext context)
    {
        if (!_abilities.TryGetValue(abilityType, out var ability))
            return;

        if (ability.CanExecute(particle, context))
        {
            ability.Execute(particle, context);
        }
    }

    private void HandleDeaths(List<Particle> particles, AbilityContext context)
    {
        // Remove dead particles (energy <= 0)
        var deadParticles = particles.Where(p => p.HasAbilities && !p.Abilities.IsAlive).ToList();
        foreach (var dead in deadParticles)
        {
            context.ParticlesToRemove.Add(dead.Id);
        }

        // Remove particles marked for removal (eaten, etc.)
        particles.RemoveAll(p => context.ParticlesToRemove.Contains(p.Id));

        // Add new particles (from splitting, reproduction)
        particles.AddRange(context.ParticlesToAdd);
        context.ParticlesToAdd.Clear();
        context.ParticlesToRemove.Clear();
    }

    public void RegisterAbility(IAbility ability)
    {
        _abilities[ability.Type] = ability;
    }
}
