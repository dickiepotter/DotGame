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
        RegisterAbility(new SpeedBurstAbility(config));
    }

    public void UpdateAbilities(List<Particle> particles, AbilityContext context)
    {
        // Update cooldowns
        UpdateCooldowns(particles, context.DeltaTime);

        // Drain passive energy
        DrainPassiveEnergy(particles, context.DeltaTime);

        // Process energy-mass conversion
        ProcessEnergyMassConversion(particles, context.DeltaTime);

        // Update dynamic movement speed multipliers
        UpdateMovementSpeed(particles);

        // Update particle colors based on current energy/abilities
        UpdateColors(particles);

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

            // Apply ambient energy gain (encourages movement and survival)
            if (_config.UseAmbientEnergy)
            {
                double ambientGain = _config.AmbientEnergyGainRate * deltaTime;
                particle.Abilities.Energy = Math.Min(
                    particle.Abilities.MaxEnergy,
                    particle.Abilities.Energy + ambientGain
                );
            }

            // Larger particles drain more energy (surface area ~ mass^(2/3))
            // This creates natural size balance - bigger isn't always better
            double drainMultiplier = Math.Pow(particle.Mass, 0.66);
            double drain = _config.PassiveEnergyDrain * drainMultiplier * deltaTime;
            particle.Abilities.Energy -= drain;

            // Ensure energy doesn't go negative
            if (particle.Abilities.Energy < 0)
                particle.Abilities.Energy = 0;
        }
    }

    private void ProcessEnergyMassConversion(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            var abilities = particle.Abilities;
            double energyPercent = particle.EnergyPercentage;

            // Energy to Mass: Convert excess energy to mass when abundant
            if (energyPercent > abilities.EnergyToMassThreshold)
            {
                double energyToConvert = _config.EnergyToMassConversionRate * deltaTime;
                double actualConversion = Math.Min(energyToConvert, abilities.Energy - (abilities.MaxEnergy * abilities.EnergyToMassThreshold));

                if (actualConversion > 0)
                {
                    abilities.Energy -= actualConversion;
                    double massGain = actualConversion * 0.1; // Conversion rate: 10:1 energy to mass
                    particle.Mass += massGain;

                    // Update radius based on new mass
                    particle.Radius = Math.Sqrt(particle.Mass / (particle.Mass - massGain)) * particle.Radius;

                    // Update max energy based on new mass
                    abilities.MaxEnergy = particle.Mass * (_config.BaseEnergyCapacity / 10.0);
                }
            }
            // Mass to Energy: Expend mass when energy is critically low
            else if (energyPercent < abilities.MassToEnergyThreshold && particle.Mass > _config.MinParticleMass)
            {
                double massToConvert = _config.MassToEnergyConversionRate * deltaTime;
                double availableMass = particle.Mass - _config.MinParticleMass;
                double actualConversion = Math.Min(massToConvert, availableMass);

                if (actualConversion > 0)
                {
                    double oldMass = particle.Mass;
                    particle.Mass -= actualConversion;
                    double energyGain = actualConversion * 10.0; // Conversion rate: 1:10 mass to energy

                    // Update radius based on new mass
                    particle.Radius = Math.Sqrt(particle.Mass / oldMass) * particle.Radius;

                    // Update max energy based on new mass
                    abilities.MaxEnergy = particle.Mass * (_config.BaseEnergyCapacity / 10.0);

                    // Add energy (clamped to max)
                    abilities.Energy = Math.Min(abilities.MaxEnergy, abilities.Energy + energyGain);
                }
            }
        }
    }

    private void UpdateMovementSpeed(List<Particle> particles)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            var abilities = particle.Abilities;
            double energyPercent = particle.EnergyPercentage;

            // Increase movement speed when energy is abundant
            if (energyPercent > abilities.EnergyAbundanceThreshold)
            {
                double excessEnergy = energyPercent - abilities.EnergyAbundanceThreshold;
                double targetMultiplier = 1.0 + (excessEnergy / (1.0 - abilities.EnergyAbundanceThreshold)) * (_config.MovementSpeedMultiplierMax - 1.0);
                abilities.MovementSpeedMultiplier = Math.Clamp(targetMultiplier, 1.0, _config.MovementSpeedMultiplierMax);
            }
            // Decrease movement speed when energy is low (conservation mode)
            else if (energyPercent < abilities.EnergyConservationThreshold)
            {
                double energyDeficit = abilities.EnergyConservationThreshold - energyPercent;
                double targetMultiplier = 1.0 - (energyDeficit / abilities.EnergyConservationThreshold) * (1.0 - _config.MovementSpeedMultiplierMin);
                abilities.MovementSpeedMultiplier = Math.Clamp(targetMultiplier, _config.MovementSpeedMultiplierMin, 1.0);
            }
            // Normal movement speed in between thresholds
            else
            {
                // Smoothly transition back to 1.0
                abilities.MovementSpeedMultiplier = 1.0;
            }
        }
    }

    private void UpdateColors(List<Particle> particles)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            // Update color based on current abilities and energy level
            // This creates dynamic visual feedback as particles gain/lose energy
            var newColor = Utilities.ColorGenerator.GetColorForAbilities(particle.Abilities);

            // Preserve alpha channel for phasing particles (semi-transparent)
            if (particle.Abilities.IsPhasing)
            {
                particle.Color = System.Windows.Media.Color.FromArgb(128,
                    newColor.R, newColor.G, newColor.B);
            }
            else
            {
                particle.Color = newColor;
            }
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
                    // Color will be restored automatically by UpdateColors
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

            // Update birth animation
            if (abilities.IsBirthing)
            {
                abilities.BirthTimeRemaining -= deltaTime;
                if (abilities.BirthTimeRemaining <= 0)
                {
                    abilities.IsBirthing = false;
                    abilities.ParentParticleId = null;
                }
            }
        }
    }

    private void UpdateVisionRange(Particle particle)
    {
        if (!particle.HasAbilities) return;

        // Vision range based on size, energy, and type synergy
        double baseVision = particle.Radius * _config.VisionRangeMultiplier;
        double energyMultiplier = particle.EnergyPercentage * 0.5 + 0.5; // 0.5 to 1.0
        double typeMult = particle.Abilities.GetVisionMult(); // Type-based bonus
        particle.Abilities.VisionRange = baseVision * energyMultiplier * typeMult;
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

        // Create explosions for all particles being removed
        if (context.Renderer != null)
        {
            foreach (var particleId in context.ParticlesToRemove)
            {
                var particle = particles.FirstOrDefault(p => p.Id == particleId);
                if (particle != null)
                {
                    context.Renderer.AddExplosion(particle);
                }
            }
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
