using System;
using System.Collections.Generic;
using DotGame.Models;

namespace DotGame.Abilities;

/// <summary>
/// Manages energy-related logic for particles including passive drain and mass-energy conversion.
/// </summary>
public class EnergyManager
{
    private readonly SimulationConfig _config;

    public EnergyManager(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Drains passive energy from all particles over time.
    /// </summary>
    public void DrainPassiveEnergy(List<Particle> particles, double deltaTime)
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
            double baseDrain = _config.PassiveEnergyDrain * Math.Pow(particle.Mass, 0.67);
            particle.Abilities.Energy -= baseDrain * deltaTime;
        }
    }

    /// <summary>
    /// Processes bidirectional conversion between energy and mass for particles.
    /// </summary>
    public void ProcessEnergyMassConversion(List<Particle> particles, double deltaTime)
    {
        foreach (var particle in particles)
        {
            if (!particle.HasAbilities) continue;

            var abilities = particle.Abilities;
            double energyPercent = particle.EnergyPercentage;

            // Energy to Mass: Convert excess energy to mass when abundant
            if (energyPercent > abilities.EnergyToMassThreshold)
            {
                ConvertEnergyToMass(particle, abilities, deltaTime);
            }
            // Mass to Energy: Expend mass when energy is critically low
            else if (energyPercent < abilities.MassToEnergyThreshold && particle.Mass > _config.MinParticleMass)
            {
                ConvertMassToEnergy(particle, abilities, deltaTime);
            }
        }
    }

    /// <summary>
    /// Converts excess energy to mass for a particle.
    /// </summary>
    private void ConvertEnergyToMass(Particle particle, ParticleAbilities abilities, double deltaTime)
    {
        double energyToConvert = _config.EnergyToMassConversionRate * deltaTime;
        double actualConversion = Math.Min(energyToConvert,
            abilities.Energy - (abilities.MaxEnergy * abilities.EnergyToMassThreshold));

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

    /// <summary>
    /// Converts mass to energy when particle is critically low on energy.
    /// </summary>
    private void ConvertMassToEnergy(Particle particle, ParticleAbilities abilities, double deltaTime)
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

    /// <summary>
    /// Updates dynamic movement speed multipliers based on energy levels.
    /// </summary>
    public void UpdateMovementSpeed(List<Particle> particles)
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
                double targetMultiplier = 1.0 + (excessEnergy / (1.0 - abilities.EnergyAbundanceThreshold)) *
                    (_config.MovementSpeedMultiplierMax - 1.0);
                abilities.MovementSpeedMultiplier = Math.Clamp(targetMultiplier, 1.0, _config.MovementSpeedMultiplierMax);
            }
            // Decrease movement speed when energy is low (conservation mode)
            else if (energyPercent < abilities.EnergyConservationThreshold)
            {
                double energyDeficit = abilities.EnergyConservationThreshold - energyPercent;
                double targetMultiplier = 1.0 - (energyDeficit / abilities.EnergyConservationThreshold) *
                    (1.0 - _config.MovementSpeedMultiplierMin);
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
}
