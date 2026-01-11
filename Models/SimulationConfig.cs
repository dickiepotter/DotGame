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
}
