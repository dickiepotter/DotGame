namespace DotGame.Models;

public class SimulationConfig
{
    // Simulation parameters
    public int ParticleCount { get; set; } = 50;
    public int RandomSeed { get; set; } = 12345;
    public double SimulationWidth { get; set; } = 800;
    public double SimulationHeight { get; set; } = 600;

    // Physics parameters
    public double GravitationalConstant { get; set; } = 100.0;
    public double DampingFactor { get; set; } = 0.995;
    public double RestitutionCoefficient { get; set; } = 0.8; // Bounciness (0-1)

    // Particle generation ranges
    public double MinMass { get; set; } = 1.0;
    public double MaxMass { get; set; } = 10.0;
    public double MinRadius { get; set; } = 5.0;
    public double MaxRadius { get; set; } = 20.0;
    public double MaxInitialVelocity { get; set; } = 50.0;

    // Feature toggles
    public bool UseGravity { get; set; } = true;
    public bool UseSpatialPartitioning { get; set; } = true;
    public bool UseCollisions { get; set; } = true;
    public bool UseBoundaries { get; set; } = true;
    public bool UseDamping { get; set; } = true;

    // Ability system toggles
    public bool UseAbilities { get; set; } = true;
    public bool UseEating { get; set; } = true;
    public bool UseSplitting { get; set; } = true;
    public bool UseReproduction { get; set; } = true;
    public bool UsePhasing { get; set; } = true;
    public bool UseChase { get; set; } = true;
    public bool UseFlee { get; set; } = true;

    // Ability parameters
    public double BaseEnergyCapacity { get; set; } = 100.0;
    public double PassiveEnergyDrain { get; set; } = 0.5; // Per second
    public double EatingEnergyGain { get; set; } = 0.8; // 80% of prey energy
    public double SizeRatioForEating { get; set; } = 1.5; // Must be 1.5x larger to eat
    public double VisionRangeMultiplier { get; set; } = 5.0; // vision = radius * multiplier
    public double HungerThreshold { get; set; } = 0.3; // Trigger hunting at 30% energy

    // Ability probabilities (for random assignment)
    public double EatingProbability { get; set; } = 0.7;
    public double SplittingProbability { get; set; } = 0.3;
    public double ReproductionProbability { get; set; } = 0.4;
    public double PhasingProbability { get; set; } = 0.2;
    public double ChaseProbability { get; set; } = 0.6;
    public double FleeProbability { get; set; } = 0.5;

    // Type distribution
    public double PredatorProbability { get; set; } = 0.3;
    public double HerbivoreProbability { get; set; } = 0.3;
    public double SocialProbability { get; set; } = 0.2;
    public double SolitaryProbability { get; set; } = 0.1;
    public double NeutralProbability { get; set; } = 0.1;

    // Chase/Flee parameters
    public double ChaseForce { get; set; } = 200.0;
    public double FleeForce { get; set; } = 250.0;
    public double ChaseEnergyCost { get; set; } = 0.1; // Per second
    public double FleeEnergyCost { get; set; } = 0.15; // Per second

    // Splitting parameters
    public double SplittingEnergyCost { get; set; } = 50.0; // 50% of current energy
    public double SplittingCooldown { get; set; } = 5.0; // Seconds
    public double SplittingSeparationForce { get; set; } = 100.0; // Push apart velocity

    // Reproduction parameters
    public double ReproductionEnergyCost { get; set; } = 40.0; // 40% of current energy
    public double ReproductionCooldown { get; set; } = 8.0; // Seconds
    public double ReproductionMassTransfer { get; set; } = 0.3; // 30% of parent mass
    public double ReproductionEnergyTransfer { get; set; } = 0.5; // 50% of parent energy (scaled)

    // Phasing parameters
    public double PhasingEnergyCost { get; set; } = 30.0; // 30% of max energy
    public double PhasingCooldown { get; set; } = 10.0; // Seconds
    public double PhasingDuration { get; set; } = 2.0; // Seconds of phasing

    // Max particles limit (prevent infinite splitting)
    public int MaxParticles { get; set; } = 1000;
}
