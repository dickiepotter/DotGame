using System.Numerics;
using DotGame.Models;
using DotGame.Abilities;
using DotGame.Rendering;
using DotGame.Utilities;

namespace DotGame.Physics;

public class PhysicsEngine
{
    private readonly SimulationConfig _config;
    private readonly ICollisionDetector _collisionDetector;
    private readonly GravityCalculator _gravityCalculator;
    private readonly BoundaryHandler _boundaryHandler;
    private readonly DampingApplier _dampingApplier;
    private readonly AbilityManager? _abilityManager;

    public PhysicsEngine(SimulationConfig config)
    {
        _config = config;
        _gravityCalculator = new GravityCalculator(config);
        _boundaryHandler = new BoundaryHandler(config);
        _dampingApplier = new DampingApplier(config);

        // Initialize ability manager if abilities are enabled
        if (_config.UseAbilities)
        {
            _abilityManager = new AbilityManager(config);
        }

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

    public void Update(List<Particle> particles, double deltaTime, ParticleRenderer? renderer = null)
    {
        // 0. Update abilities (before physics)
        if (_config.UseAbilities && _abilityManager != null)
        {
            var context = new AbilityContext
            {
                AllParticles = particles,
                Config = _config,
                DeltaTime = deltaTime,
                SpatialGrid = _collisionDetector as SpatialHashGrid,
                ParticlesToAdd = new List<Particle>(),
                ParticlesToRemove = new HashSet<int>(),
                Renderer = renderer
            };

            _abilityManager.UpdateAbilities(particles, context);
        }

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
            // Skip phasing particles in collisions
            _collisionDetector.DetectAndResolve(particles);
        }
    }

    private void IntegrateMotion(List<Particle> particles, double deltaTime)
    {
        float dt = (float)deltaTime;
        float maxVelocity = (float)_config.MaxInitialVelocity * PhysicsConstants.MAX_VELOCITY_MULTIPLIER;

        foreach (var particle in particles)
        {
            // Store previous position for potential use
            particle.PreviousPosition = particle.Position;

            // Apply speed multipliers (both speed boost and energy-based)
            float velocityMultiplier = 1.0f;
            if (particle.HasAbilities)
            {
                // Speed boost ability (temporary speed increase)
                if (particle.Abilities.IsSpeedBoosted)
                {
                    velocityMultiplier = PhysicsConstants.SPEED_BOOST_MULTIPLIER;
                }
                // Energy-based dynamic speed multiplier (stacks with speed boost)
                velocityMultiplier *= (float)particle.Abilities.MovementSpeedMultiplier;
            }

            // Clamp velocity to prevent extreme speeds
            float speed = particle.Velocity.Length();
            float effectiveMaxVelocity = maxVelocity * velocityMultiplier;
            if (speed > effectiveMaxVelocity)
            {
                particle.Velocity = Vector2.Normalize(particle.Velocity) * effectiveMaxVelocity;
            }

            // Semi-implicit Euler integration
            // Velocity has already been updated by forces
            // Now update position based on velocity (with all multipliers applied)
            particle.Position += particle.Velocity * velocityMultiplier * dt;
        }
    }
}
