using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using DotGame.Models;
using DotGame.Physics;
using DotGame.Rendering;
using DotGame.Utilities;
using DotGame.Audio;
using RP.Game.Core;
using static DotGame.Utilities.PhysicsConstants;

namespace DotGame.Simulation;

public class SimulationManager
{
    private readonly Canvas _canvas;
    private readonly SimulationConfig _config;
    private readonly PhysicsEngine _physicsEngine;
    private readonly ParticleRenderer _renderer;
    private readonly ParticleFactory _factory;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ParticleIdGenerator _idGenerator;
    private readonly SimulationAudio _audio = new();

    // The fixed-timestep clock, from RP.Game rather than hand-rolled here. It steps by integer
    // division rather than by subtracting in a loop, so a long session cannot drift, and it clamps
    // an over-long frame before banking it so a resume from suspend cannot spiral.
    private readonly FixedTimestepAccumulator _clock =
        new(PhysicsConstants.FIXED_DELTA_TIME, PhysicsConstants.FIXED_DELTA_TIME * PhysicsConstants.MAX_STEPS_PER_FRAME);

    private List<Particle> _particles;
    private DateTime _lastUpdateTime;
    private long _stepCount;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public List<Particle> Particles => _particles;
    public ParticleRenderer Renderer => _renderer;
    public PerformanceMonitor PerformanceMonitor => _performanceMonitor;

    /// <summary>Optional sound. Silent and inert until explicitly enabled.</summary>
    public SimulationAudio Audio => _audio;

    /// <summary>
    /// Number of fixed simulation steps executed since the last reset. Together with the
    /// seed this fully determines the current state.
    /// </summary>
    public long StepCount => _stepCount;

    public SimulationManager(Canvas canvas, SimulationConfig config)
    {
        _canvas = canvas;
        _config = config;

        // One ID source shared by the factory and by every ability that spawns particles.
        // The renderer keys its visuals by particle Id, so overlapping ID ranges would make
        // particles share (and lose) their ellipse.
        _idGenerator = new ParticleIdGenerator();

        _physicsEngine = new PhysicsEngine(config, _idGenerator);
        _renderer = new ParticleRenderer(canvas, new RandomGenerator(config.SeedFor(SimulationConfig.SeedStream.Effects)));
        _factory = new ParticleFactory(config, _idGenerator);
        _performanceMonitor = new PerformanceMonitor(60); // Track last 60 frames

        _particles = new List<Particle>();
        _lastUpdateTime = DateTime.Now;
    }

    public void Initialize()
    {
        // Create particles using factory
        _particles = _factory.CreateParticles();

        // Initialize renderer with particles
        _renderer.Initialize(_particles);

        // Render initial positions so particles are visible before simulation starts
        _renderer.Render(_particles);

        _lastUpdateTime = DateTime.Now;
        _clock.Reset();
        _stepCount = 0;
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

        // Reset the clock so time spent paused is not banked into the accumulator
        _lastUpdateTime = DateTime.Now;

        // Hook into WPF's rendering event (targets 60 FPS)
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;

        // Silence the ambient bed while paused; transient voices ring out naturally
        _audio.Update(new List<Particle>(), 0);
    }

    /// <summary>Releases the audio device. Called when the simulation is replaced.</summary>
    public void Shutdown()
    {
        Stop();
        _audio.Dispose();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning) return;

        // Start performance tracking
        _performanceMonitor.StartFrame();

        // Measure real elapsed time and bank it. The accumulator does the clamping, so a huge
        // first frame or a resume from suspend cannot bank more work than one frame may clear.
        var currentTime = DateTime.Now;
        double elapsed = (currentTime - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = currentTime;

        // Advance the simulation in fixed increments. A given seed and a given number of
        // steps always produce exactly the same state, independent of frame rate or of how
        // the frames happened to be paced.
        int steps = _clock.Advance(Math.Max(0, elapsed));
        for (int i = 0; i < steps; i++)
        {
            _physicsEngine.Update(_particles, FIXED_DELTA_TIME, _renderer, _audio);
            _stepCount++;
        }

        // Animate visuals by the amount of simulated time actually consumed, so the
        // effects stay in lockstep with the physics
        _renderer.Render(_particles, steps * FIXED_DELTA_TIME);

        // Ambient bed tracks the state of the population
        _audio.SetWorldWidth(_config.SimulationWidth);
        _audio.Update(_particles, steps * FIXED_DELTA_TIME);

        // End performance tracking
        _performanceMonitor.EndFrame();
    }

    // Add a new particle at the specified position.
    // Delegates entirely to the factory: this used to be a near-copy of the factory's logic
    // that had drifted apart from it, most visibly in the energy-capacity formula, which
    // gave hand-placed particles ten times the energy of the startup population.
    public void AddParticle(Vector2 position)
    {
        _particles.Add(_factory.CreateParticleAt(position));
        _renderer.Initialize(_particles); // Re-initialize renderer to include new particle
    }

    // Find a particle at or near the specified position
    public Particle? FindParticleAtPosition(Vector2 position)
    {
        return ParticleQueryUtility.FindParticleAtPosition(position, _particles);
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
