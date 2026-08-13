using System.Numerics;
using DotGame.Models;
using DotGame.Abilities;
using DotGame.Rendering;
using DotGame.Utilities;

namespace DotGame.Physics;

public class PhysicsEngine
{
    // Above this many particles the naive O(n^2) sweep is slower than the spatial hash.
    private const int SPATIAL_PARTITIONING_THRESHOLD = 50;

    private readonly SimulationConfig _config;
    private readonly NaiveCollisionDetector _naiveDetector;
    private readonly SpatialHashGrid _spatialDetector;
    private readonly GravityCalculator _gravityCalculator;
    private readonly BoundaryHandler _boundaryHandler;
    private readonly DampingApplier _dampingApplier;
    private readonly AbilityManager? _abilityManager;

    public PhysicsEngine(SimulationConfig config, ParticleIdGenerator idGenerator)
    {
        _config = config;
        _gravityCalculator = new GravityCalculator(config);
        _boundaryHandler = new BoundaryHandler(config);
        _dampingApplier = new DampingApplier(config);

        // Initialize ability manager if abilities are enabled
        if (_config.UseAbilities)
        {
            _abilityManager = new AbilityManager(config, idGenerator);
        }

        // Both detectors are kept live; the choice is made per frame from the *current*
        // population, which grows and shrinks as particles breed, split and get eaten.
        _naiveDetector = new NaiveCollisionDetector(config);
        _spatialDetector = new SpatialHashGrid(config, config.MaxRadius);
    }

    private ICollisionDetector SelectCollisionDetector(int particleCount)
    {
        return _config.UseSpatialPartitioning && particleCount > SPATIAL_PARTITIONING_THRESHOLD
            ? _spatialDetector
            : _naiveDetector;
    }

    public void Update(List<Particle> particles, double deltaTime, ParticleRenderer? renderer = null,
        DotGame.Audio.SimulationAudio? audio = null)
    {
        var collisionDetector = SelectCollisionDetector(particles.Count);

        // 0. Update abilities (before physics)
        if (_config.UseAbilities && _abilityManager != null)
        {
            var context = new AbilityContext
            {
                AllParticles = particles,
                Config = _config,
                DeltaTime = deltaTime,
                SpatialGrid = collisionDetector as SpatialHashGrid,
                ParticlesToAdd = new List<Particle>(),
                ParticlesToRemove = new HashSet<int>(),
                Renderer = renderer,
                Audio = audio
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
            collisionDetector.DetectAndResolve(particles);
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

            // Speed multipliers raise or lower the particle's *speed ceiling*; they are not
            // applied to the position step as well. Doing both would square their effect and
            // would also mean the particle travelled at a different speed than the one the
            // collision and gravity code sees.
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

            // Semi-implicit Euler integration.
            // Velocity has already been updated by forces; advance position by it.
            particle.Position += particle.Velocity * dt;
        }
    }
}
