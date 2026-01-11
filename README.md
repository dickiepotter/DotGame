# DotGame - Evolutionary Game of Life Simulation

A Rust-based physics simulation that extends Conway's Game of Life from a grid to continuous 2D Euclidean space, with interactive dots that have diverse behaviors and properties.

## Features

### Core Mechanics
- **Continuous Space**: Dots move freely in 2D space (not grid-based)
- **Real Physics**: Velocity, momentum, friction, elastic collisions, spin, and wall bouncing
- **Multiple Dot Types**: 14 different dot types with unique behaviors
- **Classic Mode**: Toggle back to traditional Game of Life rules
- **Configuration**: Save/load JSON configs to customize all physics parameters
- **Interactive**: Click to place dots, drag to paint, right-click to remove
- **Visual Feedback**: Color-coded dots with optional aura visualization

### Dot Types

Each dot type has a unique color and behavior:

1. **Classic** (Gray) - Standard Game of Life behavior in continuous space
2. **Predator** (Red) - Hunts and eats Prey and Classic dots
3. **Prey** (Yellow) - Flees from Predators
4. **Absorber** (Purple) - Absorbs other dots to grow larger
5. **Transformer** (Orange) - Can change type by absorbing others
6. **Repulsor** (Dark Blue) - Pushes all nearby dots away
7. **Attractor** (Magenta) - Pulls all nearby dots toward it
8. **Chaser** (Pink) - Actively chases different dot types
9. **Protector** (Sky Blue) - Shields dots of the same type from predators
10. **Ghost** (Translucent) - Passes through other dots without collision
11. **Bouncer** (Dark Green) - Strongly repels dots on contact
12. **Social** (Lime) - Attracted to dots of the same type
13. **Grower** (Brown) - Continuously grows over time
14. **Divider** (Cyan) - Splits into multiple dots when large enough

## Controls

### Keyboard

- **Space**: Pause/Resume simulation
- **Tab**: Cycle through dot types (changes what you place)
- **A**: Toggle aura visualization (shows interaction radius)
- **G**: Toggle classic Game of Life mode
- **C**: Clear all dots
- **R**: Spawn 100 random dots
- **S**: Save current configuration to `dotgame_config.json`
- **L**: Load configuration from `dotgame_config.json`
- **1**: Increase max speed
- **2**: Decrease max speed

### Mouse

- **Left Click/Drag**: Place dots of the selected type
- **Right Click/Drag**: Remove dots near cursor

## Physics Properties

Each dot has the following physical properties:

- **Position & Velocity**: Standard 2D physics
- **Mass**: Affects collision responses (grows with radius)
- **Radius**: Visual size and collision boundary
- **Spin**: Rotation from wall/collision impacts
- **Energy**: Decreases over time; dots die at 0 energy
- **Age**: Tracks lifetime

### Interaction Forces

- **Attraction**: Pulls dots together (Attractor, Social)
- **Repulsion**: Pushes dots apart (Repulsor, Bouncer, Prey fleeing)
- **Chase**: Active pursuit of other dots (Chaser, Predator)
- **Protection**: Defensive shielding (Protector)
- **Eating**: Absorption and growth (Predator, Absorber)
- **Division**: Reproductive splitting (Divider)

## Configuration

The game uses a `DotConfig` structure with these tunable parameters:

```json
{
  "max_speed": 5.0,
  "attraction_strength": 0.5,
  "repulsion_strength": 2.0,
  "friction": 0.98,
  "bounce_damping": 0.8,
  "interaction_radius": 50.0,
  "eating_radius": 15.0,
  "growth_rate": 0.01,
  "divide_size": 20.0
}
```

Press **S** to save your current settings or **L** to load saved settings.

## Building and Running

### Prerequisites

- Rust 1.70+ (install from [rustup.rs](https://rustup.rs))

### Build

```bash
cargo build --release
```

### Run

```bash
cargo run --release
```

The `--release` flag is recommended for optimal performance (60 FPS target).

## Game Modes

### Extended Mode (Default)

All 14 dot types are active with their unique behaviors. This creates a complex ecosystem where dots interact, compete, and evolve.

### Classic Game of Life Mode

Press **G** to toggle. In this mode:
- Only Classic dots are used
- Conway's rules apply: dots survive with 2-3 neighbors, reproduce with exactly 3
- Adapted for continuous space using interaction radius
- Updates every 30 frames for visibility

## Performance

- **Target**: 60 FPS (30 FPS acceptable)
- **Max Dots**: 1000 concurrent dots
- **Optimization**: Dead dots cleaned periodically; physics calculated per frame
- **Rendering**: Macroquad handles efficient 2D rendering

## Learning Rust with DotGame

This project demonstrates several Rust concepts:

- **Ownership & Borrowing**: Dot collections and mutable references
- **Enums & Pattern Matching**: DotType with behavioral matching
- **Structs & Impl Blocks**: Clean OOP-style organization
- **Traits**: Serde serialization
- **Iterators**: Efficient dot processing
- **Async**: Macroquad's async game loop
- **External Crates**: Graphics (macroquad), randomization (rand), serialization (serde)

## Architecture

The code is organized into clear modules:

- **Constants**: Screen size, limits, default values
- **DotType Enum**: All dot type definitions and colors
- **DotConfig**: Serializable physics configuration
- **Dot Struct**: Individual dot with physics and behavior
- **GameState**: Main game logic, updates, and rendering
- **Main Loop**: Input handling and frame updates

## Tips for Experimentation

1. **Create Ecosystems**: Mix Predators, Prey, and Protectors
2. **Test Physics**: Use Attractors and Repulsors to create force fields
3. **Watch Evolution**: Absorbers can grow massive by eating others
4. **Social Dynamics**: Social dots cluster together beautifully
5. **Chaos**: Add Bouncers and Chasers for unpredictable motion
6. **Modify Config**: Save different configurations for different scenarios

## Future Enhancements

Ideas for extending the project:

- More dot types (magnetic, explosive, teleporting)
- UI panel for live config editing
- Save/load simulation states
- Trails/particle effects
- Sound effects
- Multi-threaded physics for >1000 dots
- Spatial hashing for collision optimization

## License

This is a learning project - feel free to use, modify, and extend!

## Credits

Built with:
- [macroquad](https://github.com/not-fl3/macroquad) - Simple game framework
- [rand](https://github.com/rust-random/rand) - Random number generation
- [serde](https://github.com/serde-rs/serde) - Serialization framework

---

**Enjoy watching the dots evolve!** 🎮✨
