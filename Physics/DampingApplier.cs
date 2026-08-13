using DotGame.Models;
using DotGame.Utilities;

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
        // Apply velocity damping to simulate air resistance/friction.
        //
        // DampingFactor is authored as a per-frame multiplier at the reference frame rate.
        // Applying it once per frame regardless of deltaTime would make the simulation
        // behave differently on a 144Hz monitor than on a 60Hz one, so raise it to the
        // power of the elapsed time to get the equivalent continuous decay.
        float dampingFactor = (float)Math.Pow(
            _config.DampingFactor,
            deltaTime * PhysicsConstants.DAMPING_REFERENCE_FPS);

        foreach (var particle in particles)
        {
            particle.Velocity *= dampingFactor;
        }
    }
}
