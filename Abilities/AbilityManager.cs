using System.Collections.Generic;
using System.Linq;
using DotGame.Models;

namespace DotGame.Abilities;

/// <summary>
/// Coordinates ability management using specialized managers.
/// Delegates specific responsibilities to focused manager classes.
/// </summary>
public class AbilityManager
{
    private readonly SimulationConfig _config;

    // Specialized managers
    private readonly AbilityStateManager _stateManager;
    private readonly EnergyManager _energyManager;
    private readonly AbilityExecutor _executor;

    public AbilityManager(SimulationConfig config)
    {
        _config = config;

        // Initialize specialized managers
        _stateManager = new AbilityStateManager(config);
        _energyManager = new EnergyManager(config);
        _executor = new AbilityExecutor(config);

        // Register implemented abilities
        RegisterAbility(new EatingAbility(config));
        RegisterAbility(new ChaseAbility(config));
        RegisterAbility(new FleeAbility(config));
        RegisterAbility(new SplittingAbility(config));
        RegisterAbility(new ReproductionAbility(config));
        RegisterAbility(new PhasingAbility(config));
        RegisterAbility(new SpeedBurstAbility(config));
    }

    /// <summary>
    /// Updates all ability-related logic for particles.
    /// </summary>
    public void UpdateAbilities(List<Particle> particles, AbilityContext context)
    {
        // Update cooldowns
        _stateManager.UpdateCooldowns(particles, context.DeltaTime);

        // Drain passive energy
        _energyManager.DrainPassiveEnergy(particles, context.DeltaTime);

        // Process energy-mass conversion
        _energyManager.ProcessEnergyMassConversion(particles, context.DeltaTime);

        // Update dynamic movement speed multipliers
        _energyManager.UpdateMovementSpeed(particles);

        // Update particle colors based on current energy/abilities
        _stateManager.UpdateColors(particles);

        // Update temporary states (phasing, speed boost, camouflage)
        _stateManager.UpdateTemporaryStates(particles, context.DeltaTime);

        // AI decision making & ability execution
        foreach (var particle in particles.ToList())
        {
            if (!particle.HasAbilities || !particle.Abilities.IsAlive)
                continue;

            // Update vision range
            _stateManager.UpdateVisionRange(particle);

            // AI decides which ability to use
            var decision = _executor.MakeAbilityDecision(particle, context);

            if (decision.HasValue && _executor.CanUseAbility(particle, decision.Value))
            {
                _executor.ExecuteAbility(particle, decision.Value, context);
            }
        }

        // Handle particle deaths and apply changes
        _executor.HandleDeaths(particles, context);
    }

    /// <summary>
    /// Registers an ability implementation.
    /// </summary>
    public void RegisterAbility(IAbility ability)
    {
        _executor.RegisterAbility(ability);
    }
}
