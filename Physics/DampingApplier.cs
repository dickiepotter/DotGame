using DotGame.Models;

namespace DotGame.Physics;

public class DampingApplier
{
    private readonly SimulationConfig _config;

    public DampingApplier(SimulationConfig config)
    {
        _config = config;
    }

    public void ApplyDamping(List<Particle> particles, double deltaTime)
    {
        // Apply velocity damping to simulate air resistance/friction
        float dampingFactor = (float)_config.DampingFactor;

        foreach (var particle in particles)
        {
            particle.Velocity *= dampingFactor;
        }
    }
}
