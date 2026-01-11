using DotGame.Models;

namespace DotGame.Physics;

public class BoundaryHandler
{
    private readonly SimulationConfig _config;

    public BoundaryHandler(SimulationConfig config)
    {
        _config = config;
    }

    public void HandleBoundaries(List<Particle> particles)
    {
        foreach (var particle in particles)
        {
            // Left boundary
            if (particle.Position.X - particle.Radius < 0)
            {
                particle.Position = new System.Numerics.Vector2(
                    (float)particle.Radius,
                    particle.Position.Y
                );
                particle.Velocity = new System.Numerics.Vector2(
                    -particle.Velocity.X * (float)_config.RestitutionCoefficient,
                    particle.Velocity.Y
                );
            }
            // Right boundary
            else if (particle.Position.X + particle.Radius > _config.SimulationWidth)
            {
                particle.Position = new System.Numerics.Vector2(
                    (float)(_config.SimulationWidth - particle.Radius),
                    particle.Position.Y
                );
                particle.Velocity = new System.Numerics.Vector2(
                    -particle.Velocity.X * (float)_config.RestitutionCoefficient,
                    particle.Velocity.Y
                );
            }

            // Top boundary
            if (particle.Position.Y - particle.Radius < 0)
            {
                particle.Position = new System.Numerics.Vector2(
                    particle.Position.X,
                    (float)particle.Radius
                );
                particle.Velocity = new System.Numerics.Vector2(
                    particle.Velocity.X,
                    -particle.Velocity.Y * (float)_config.RestitutionCoefficient
                );
            }
            // Bottom boundary
            else if (particle.Position.Y + particle.Radius > _config.SimulationHeight)
            {
                particle.Position = new System.Numerics.Vector2(
                    particle.Position.X,
                    (float)(_config.SimulationHeight - particle.Radius)
                );
                particle.Velocity = new System.Numerics.Vector2(
                    particle.Velocity.X,
                    -particle.Velocity.Y * (float)_config.RestitutionCoefficient
                );
            }
        }
    }
}
