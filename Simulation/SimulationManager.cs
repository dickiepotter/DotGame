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
    private readonly PerformanceMonitor _performanceMonitor;

    private List<Particle> _particles;
    private DateTime _lastUpdateTime;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public List<Particle> Particles => _particles;
    public ParticleRenderer Renderer => _renderer;
    public PerformanceMonitor PerformanceMonitor => _performanceMonitor;

    public SimulationManager(Canvas canvas, SimulationConfig config)
    {
        _canvas = canvas;
        _config = config;
        _physicsEngine = new PhysicsEngine(config);
        _renderer = new ParticleRenderer(canvas);
        _factory = new ParticleFactory(config);
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

        // Start performance tracking
        _performanceMonitor.StartFrame();

        // Calculate delta time
        var currentTime = DateTime.Now;
        double deltaTime = (currentTime - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = currentTime;

        // Cap delta time to prevent physics instability
        // Max 33ms = minimum 30 FPS
        deltaTime = Math.Min(deltaTime, 0.033);

        // Skip update if delta time is too small
        if (deltaTime < 0.001)
        {
            _performanceMonitor.EndFrame();
            return;
        }

        // Update physics (pass renderer for explosion effects)
        _physicsEngine.Update(_particles, deltaTime, _renderer);

        // Update rendering
        _renderer.Render(_particles, deltaTime);

        // End performance tracking
        _performanceMonitor.EndFrame();
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
            PreviousPosition = position
        };

        // Add abilities if enabled
        if (_config.UseAbilities)
        {
            particle.Abilities = CreateRandomAbilities(mass, random);
            particle.Color = ColorGenerator.GetColorForAbilities(particle.Abilities);
        }
        else
        {
            particle.Color = ColorGenerator.GetColorForMass(mass, _config.MinMass, _config.MaxMass);
        }

        _particles.Add(particle);
        _renderer.Initialize(_particles); // Re-initialize renderer to include new particle
    }

    private ParticleAbilities CreateRandomAbilities(double mass, Random random)
    {
        // Energy capacity scales with mass
        // Normalize by minimum mass so particles have comparable starting energy
        double energyCapacity = mass * (_config.BaseEnergyCapacity / _config.MinMass);

        var abilities = new ParticleAbilities
        {
            Energy = energyCapacity,
            MaxEnergy = energyCapacity,
            Type = ChooseRandomType(random),
            Generation = 0,
            Abilities = AbilitySet.None,
            CurrentState = AbilityState.Idle
        };

        // Initialize random thresholds for this particle
        abilities.EnergyToMassThreshold = random.NextDouble() *
            (_config.EnergyToMassThresholdMax - _config.EnergyToMassThresholdMin) +
            _config.EnergyToMassThresholdMin;

        abilities.MassToEnergyThreshold = random.NextDouble() *
            (_config.MassToEnergyThresholdMax - _config.MassToEnergyThresholdMin) +
            _config.MassToEnergyThresholdMin;

        abilities.EnergyAbundanceThreshold = random.NextDouble() *
            (_config.EnergyAbundanceThresholdMax - _config.EnergyAbundanceThresholdMin) +
            _config.EnergyAbundanceThresholdMin;

        abilities.EnergyConservationThreshold = random.NextDouble() *
            (_config.EnergyConservationThresholdMax - _config.EnergyConservationThresholdMin) +
            _config.EnergyConservationThresholdMin;

        abilities.MovementSpeedMultiplier = 1.0; // Start at normal speed

        // Randomly assign abilities based on probabilities
        if (random.NextDouble() < _config.EatingProbability)
            abilities.Abilities |= AbilitySet.Eating;

        if (random.NextDouble() < _config.SplittingProbability)
            abilities.Abilities |= AbilitySet.Splitting;

        if (random.NextDouble() < _config.ReproductionProbability)
            abilities.Abilities |= AbilitySet.Reproduction;

        if (random.NextDouble() < _config.PhasingProbability)
            abilities.Abilities |= AbilitySet.Phasing;

        if (random.NextDouble() < _config.ChaseProbability)
            abilities.Abilities |= AbilitySet.Chase;

        if (random.NextDouble() < _config.FleeProbability)
            abilities.Abilities |= AbilitySet.Flee;

        // Initialize cooldowns for assigned abilities
        InitializeCooldowns(abilities);

        // Calculate initial vision range
        abilities.VisionRange = mass * _config.VisionRangeMultiplier;

        return abilities;
    }

    private ParticleType ChooseRandomType(Random random)
    {
        double roll = random.NextDouble();
        double cumulative = 0;

        cumulative += _config.PredatorProbability;
        if (roll < cumulative) return ParticleType.Predator;

        cumulative += _config.HerbivoreProbability;
        if (roll < cumulative) return ParticleType.Herbivore;

        cumulative += _config.SocialProbability;
        if (roll < cumulative) return ParticleType.Social;

        cumulative += _config.SolitaryProbability;
        if (roll < cumulative) return ParticleType.Solitary;

        return ParticleType.Neutral;
    }

    private void InitializeCooldowns(ParticleAbilities abilities)
    {
        if (abilities.HasAbility(AbilitySet.Eating))
            abilities.InitializeCooldown(AbilityType.Eating, _config.EatingCooldown);

        if (abilities.HasAbility(AbilitySet.Splitting))
            abilities.InitializeCooldown(AbilityType.Splitting, _config.SplittingCooldown);

        if (abilities.HasAbility(AbilitySet.Reproduction))
            abilities.InitializeCooldown(AbilityType.Reproduction, _config.ReproductionCooldown);

        if (abilities.HasAbility(AbilitySet.Phasing))
            abilities.InitializeCooldown(AbilityType.Phasing, _config.PhasingCooldown);

        if (abilities.HasAbility(AbilitySet.SpeedBurst))
            abilities.InitializeCooldown(AbilityType.SpeedBurst, _config.SpeedBurstCooldown);

        if (abilities.HasAbility(AbilitySet.Chase))
            abilities.InitializeCooldown(AbilityType.Chase, _config.ChaseCooldown);

        if (abilities.HasAbility(AbilitySet.Flee))
            abilities.InitializeCooldown(AbilityType.Flee, _config.FleeCooldown);
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
