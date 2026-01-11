using System.Windows.Controls;
using System.Windows.Media;
using DotGame.Models;
using DotGame.Physics;
using DotGame.Rendering;

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
}
