using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using DotGame.Models;
using DotGame.Physics;
using DotGame.Rendering;
using DotGame.Utilities;

namespace DotGame.Simulation;

public class SimulationManager
{
    private readonly Canvas _canvas;
    private readonly SimulationConfig _config;
    private readonly PhysicsEngine _physicsEngine;
    private readonly ParticleRenderer _renderer;
    private readonly ParticleFactory _factory;

    private List<Particle> _particles;
    private DateTime _lastUpdateTime;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public List<Particle> Particles => _particles;

    public SimulationManager(Canvas canvas, SimulationConfig config)
    {
        _canvas = canvas;
        _config = config;
        _physicsEngine = new PhysicsEngine(config);
        _renderer = new ParticleRenderer(canvas);
        _factory = new ParticleFactory(config);

        _particles = new List<Particle>();
        _lastUpdateTime = DateTime.Now;
    }

    public void Initialize()
    {
        // Create particles using factory
        _particles = _factory.CreateParticles();

        // Initialize renderer with particles
        _renderer.Initialize(_particles);

        _lastUpdateTime = DateTime.Now;
    }

    public void Reset()
    {
        Stop();
        Initialize();
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _lastUpdateTime = DateTime.Now;

        // Hook into WPF's rendering event (targets 60 FPS)
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning) return;

        // Calculate delta time
        var currentTime = DateTime.Now;
        double deltaTime = (currentTime - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = currentTime;

        // Cap delta time to prevent physics instability
        // Max 33ms = minimum 30 FPS
        deltaTime = Math.Min(deltaTime, 0.033);

        // Skip update if delta time is too small
        if (deltaTime < 0.001) return;

        // Update physics
        _physicsEngine.Update(_particles, deltaTime);

        // Update rendering
        _renderer.Render(_particles);
    }

    // Add a new particle at the specified position
    public void AddParticle(Vector2 position)
    {
        // Generate random properties for the new particle
        var random = new Random();
        var radius = random.NextDouble() * (_config.MaxRadius - _config.MinRadius) + _config.MinRadius;
        var mass = random.NextDouble() * (_config.MaxMass - _config.MinMass) + _config.MinMass;

        // Ensure particle is within bounds
        position.X = Math.Clamp(position.X, (float)radius, (float)(_config.SimulationWidth - radius));
        position.Y = Math.Clamp(position.Y, (float)radius, (float)(_config.SimulationHeight - radius));

        // Create new particle with zero initial velocity
        var particle = new Particle
        {
            Id = _particles.Count > 0 ? _particles.Max(p => p.Id) + 1 : 0,
            Position = position,
            Velocity = Vector2.Zero,
            Mass = mass,
            Radius = radius,
            Color = ColorGenerator.GetColorForMass(mass, _config.MinMass, _config.MaxMass),
            PreviousPosition = position
        };

        _particles.Add(particle);
        _renderer.Initialize(_particles); // Re-initialize renderer to include new particle
    }

    // Find a particle at or near the specified position
    public Particle? FindParticleAtPosition(Vector2 position)
    {
        foreach (var particle in _particles)
        {
            float dx = particle.Position.X - position.X;
            float dy = particle.Position.Y - position.Y;
            float distanceSquared = dx * dx + dy * dy;

            if (distanceSquared <= particle.Radius * particle.Radius)
            {
                return particle;
            }
        }

        return null;
    }

    // Apply an impulse (instantaneous force) to a particle in a specific direction
    public void ApplyImpulseToParticle(Particle particle, Vector2 impulse)
    {
        if (particle == null) return;

        // Apply impulse by changing velocity
        // Impulse = mass × change in velocity, so change in velocity = impulse / mass
        particle.Velocity += impulse * (float)particle.InverseMass;
    }
}
