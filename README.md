# DotGame - 2D Physics Particle Simulator

A C# WPF application that simulates particles (dots) with realistic physics interactions in 2D space. Each particle has unique properties and the simulation is fully deterministic using seed-based random generation.

## Features

- **Realistic Physics Simulation**
  - Elastic collision detection and response
  - N-body gravitational attraction
  - Boundary bouncing with energy loss
  - Velocity damping (friction/air resistance)

- **Unique Particle Properties**
  - Mass: Affects physics interactions and inertia
  - Radius: Visual size and collision boundaries
  - Color: Mass-based gradient (heavier = red, lighter = blue)
  - Initial Velocity: Random starting speed and direction

- **Seed-Based Reproducibility**
  - Input any seed value to recreate specific scenarios
  - Same seed always produces identical simulations

- **Performance Optimized**
  - Naive O(n²) collision detection for <50 particles
  - Spatial hash grid O(n) optimization for 50+ particles
  - Semi-implicit Euler integration for stable physics
  - Hardware-accelerated rendering with WPF

- **Interactive Controls**
  - Start/Stop/Reset simulation
  - Adjust particle count, seed, gravity strength
  - Toggle physics features on/off
  - Real-time configuration updates

## Requirements

- .NET 8.0 SDK or later
- Windows OS (WPF is Windows-only)
- Visual Studio 2022 (recommended) or Visual Studio Code

## Building the Project

### Using Visual Studio 2022

1. Open `DotGame.sln` in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Run the project (F5)

### Using Command Line

```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Run the application
dotnet run
```

### Using MSBuild

```bash
msbuild DotGame.sln /p:Configuration=Release
```

## How to Use

1. **Starting the Simulation**
   - Click the "Start" button to begin the physics simulation
   - Particles will immediately start moving and interacting

2. **Configuring the Simulation**
   - **Particle Count**: Number of particles (1-500, recommended: 50-100)
   - **Random Seed**: Integer seed for reproducibility (e.g., 12345)
   - **Gravity Strength**: Gravitational constant (default: 100.0)

3. **Physics Toggles**
   - **Gravity**: Enable/disable gravitational attraction between particles
   - **Collisions**: Enable/disable particle-to-particle collisions
   - **Boundaries**: Enable/disable wall bouncing
   - **Damping**: Enable/disable velocity damping (friction)

4. **Resetting**
   - Click "Reset" to generate new particles with current settings
   - Modify configuration values before resetting to see different behaviors

## Testing Seed Reproducibility

To verify that seeds produce reproducible simulations:

1. Set seed to `12345` and particle count to `50`
2. Click "Reset" then "Start"
3. Note the positions of particles after a few seconds
4. Click "Reset" again (keeps same seed)
5. Click "Start" and verify particles move identically

## Project Structure

```
DotGame/
├── Models/              # Core data structures
│   ├── Particle.cs      # Particle properties (position, velocity, mass, etc.)
│   └── SimulationConfig.cs  # Configuration settings
├── Physics/             # Physics simulation engine
│   ├── PhysicsEngine.cs     # Main coordinator
│   ├── GravityCalculator.cs # N-body gravity
│   ├── BoundaryHandler.cs   # Wall collisions
│   ├── DampingApplier.cs    # Velocity damping
│   ├── ICollisionDetector.cs        # Collision interface
│   ├── NaiveCollisionDetector.cs    # O(n²) collision detection
│   └── SpatialHashGrid.cs           # O(n) optimized collision
├── Rendering/           # WPF Canvas rendering
│   └── ParticleRenderer.cs  # Manages visual elements
├── Simulation/          # Simulation management
│   ├── SimulationManager.cs # Game loop coordinator
│   ├── ParticleFactory.cs   # Random particle generation
│   └── RandomGenerator.cs   # Seeded random wrapper
├── Utilities/           # Helper classes
│   └── ColorGenerator.cs    # Color generation
└── Views/               # WPF UI
    ├── MainWindow.xaml      # Main window layout
    └── MainWindow.xaml.cs   # UI code-behind
```

## Physics Implementation

### Semi-Implicit Euler Integration
```
velocity += acceleration × Δt
position += velocity × Δt
```

### Elastic Collision Response
1. Calculate collision normal: `n = normalize(b.pos - a.pos)`
2. Separate overlapping particles based on mass ratio
3. Calculate relative velocity: `vRel = b.vel - a.vel`
4. Calculate impulse: `j = -(1 + e) × dot(vRel, n) / (1/m₁ + 1/m₂)`
5. Apply impulse: `a.vel -= j×n/m₁`, `b.vel += j×n/m₂`

### Gravitational Force
```
F = G × m₁ × m₂ / r²
a₁ = F / m₁
a₂ = -F / m₂
```

## Default Configuration

- **Particle Count**: 50
- **Random Seed**: 12345
- **Simulation Size**: 800×600 pixels
- **Gravity Constant**: 100.0
- **Damping Factor**: 0.995 (0.5% energy loss per frame)
- **Restitution**: 0.8 (20% energy loss per collision)
- **Mass Range**: 1.0 - 10.0
- **Radius Range**: 5.0 - 20.0 pixels
- **Max Initial Velocity**: 50.0 pixels/second

## Performance Expectations

- **50 particles**: 60 FPS (naive collision detection)
- **100 particles**: 60 FPS (spatial hash grid)
- **200 particles**: 30-60 FPS (spatial hash grid)
- **500+ particles**: May require further optimization

## Troubleshooting

### Application won't start
- Ensure .NET 8.0 SDK is installed
- Check that all files are present in the project directory
- Try cleaning and rebuilding: `dotnet clean && dotnet build`

### Poor performance
- Reduce particle count
- Disable gravity (most expensive operation)
- Ensure spatial partitioning is enabled (automatic for >50 particles)

### Particles escape boundaries
- Ensure "Boundaries" checkbox is enabled
- Check that simulation canvas size matches config values

## Future Enhancements

Potential features for future development:
- Particle trails (motion history visualization)
- Mouse interaction (click to add/remove particles)
- Save/load simulation states
- Export simulation to video
- Different particle types (static, charged, magnetic)
- Force fields in specific regions
- Inelastic collisions (particle merging)
- Particle lifetime and decay
- Multiple preset configurations

## License

This project is created for educational purposes.

## Author

Created using Claude Code - AI-powered software development assistant.

## Acknowledgments

- Physics algorithms based on game physics literature
- Semi-implicit Euler integration for stable simulation
- Spatial hash grid optimization technique
- WPF framework for hardware-accelerated rendering
