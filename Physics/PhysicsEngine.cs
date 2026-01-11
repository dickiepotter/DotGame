using System.Numerics;
using DotGame.Models;

namespace DotGame.Physics;

public class PhysicsEngine
{
    private readonly SimulationConfig _config;
    private readonly ICollisionDetector _collisionDetector;
    private readonly GravityCalculator _gravityCalculator;
    private readonly BoundaryHandler _boundaryHandler;
    private readonly DampingApplier _dampingApplier;

    public PhysicsEngine(SimulationConfig config)
    {
        _config = config;
        _gravityCalculator = new GravityCalculator(config);
        _boundaryHandler = new BoundaryHandler(config);
        _dampingApplier = new DampingApplier(config);

        // Choose collision detector based on config and particle count
        // Use spatial partitioning for >50 particles if enabled
        if (_config.UseSpatialPartitioning && _config.ParticleCount > 50)
        {
            _collisionDetector = new SpatialHashGrid(config, config.MaxRadius);
        }
        else
        {
            _collisionDetector = new NaiveCollisionDetector(config);
        }
    }

    public void Update(List<Particle> particles, double deltaTime)
    {
        // 1. Apply gravity forces (if enabled)
        if (_config.UseGravity)
        {
            _gravityCalculator.ApplyGravity(particles, deltaTime);
        }

        // 2. Apply damping (if enabled)
        if (_config.UseDamping)
        {
            _dampingApplier.ApplyDamping(particles, deltaTime);
        }

        // 3. Integrate motion (semi-implicit Euler)
        IntegrateMotion(particles, deltaTime);

        // 4. Handle boundary collisions (if enabled)
        if (_config.UseBoundaries)
        {
            _boundaryHandler.HandleBoundaries(particles);
        }

        // 5. Detect and resolve particle collisions (if enabled)
        if (_config.UseCollisions)
        {
            _collisionDetector.DetectAndResolve(particles);
        }
    }

    private void IntegrateMotion(List<Particle> particles, double deltaTime)
    {
        float dt = (float)deltaTime;

        foreach (var particle in particles)
        {
            // Store previous position for potential use
            particle.PreviousPosition = particle.Position;

            // Semi-implicit Euler integration
            // Velocity has already been updated by forces
            // Now update position based on velocity
            particle.Position += particle.Velocity * dt;
        }
    }
}
