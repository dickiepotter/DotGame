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
  - Same seed always produces identical simulations, including visual effects
  - Fixed simulation timestep, so the outcome does not depend on frame rate or frame pacing

- **Performance Optimized**
  - Naive O(n²) collision detection for <50 particles
  - Spatial hash grid O(n) optimization for 50+ particles
  - Semi-implicit Euler integration for stable physics
  - Hardware-accelerated rendering with WPF

- **Generated Sci-Fi Sound** (on by default, fully optional)
  - Synthesised at runtime from FM, ring modulation, sweeps and a damped stereo delay
  - No audio assets; pitch follows mass, panning follows position

- **Two Render Modes**
  - Classic: outlined discs with bars and overlays
  - Luminous: additive HDR light field - no edges, particles are light sources

- **Interactive Controls**
  - Start/Stop/Reset simulation
  - Full screen (F11) with the sidebar hidden
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

Randomness is drawn from three independent streams derived from the single seed
(`SimulationConfig.SeedFor`): particle generation, ability decisions, and visual effects.
Keeping them separate means a headless run and a rendered run of the same seed produce
identical particle trajectories, and adding a draw in one place does not shift every
subsequent value elsewhere.

## Project Structure

```
DotGame/
├── Models/              # Core data structures
│   ├── Particle.cs              # Position, velocity, mass, colour, abilities
│   ├── ParticleAbilities.cs     # Per-particle energy and ability state
│   ├── SimulationConfig.cs      # Configuration settings
│   └── ConfigurationPresets.cs  # Named starting scenarios
├── Physics/             # Physics simulation engine
│   ├── PhysicsEngine.cs         # Main coordinator
│   ├── GravityCalculator.cs     # N-body gravity
│   ├── BoundaryHandler.cs       # Wall collisions
│   ├── DampingApplier.cs        # Velocity damping
│   ├── ICollisionDetector.cs    # Collision interface
│   ├── NaiveCollisionDetector.cs# O(n²) collision detection
│   └── SpatialHashGrid.cs       # O(n) optimized collision
├── Abilities/           # What particles can do, one file per ability
│   ├── IAbility.cs              # Contract + AbilityContext
│   ├── AbilityManager.cs        # Selection, execution and state, split three ways
│   └── Eating/Reproduction/Splitting/Phasing/SpeedBurst/Chase/Flee
├── AI/                  # Ability choice
│   ├── ParticleAI.cs            # Picks an ability from what is visible
│   └── VisionSystem.cs          # What a particle can see
├── Rendering/           # WPF Canvas rendering
│   ├── ParticleRenderer.cs      # Coordinator over six sub-renderers
│   └── LightField.cs …          # The Luminous HDR path
├── Simulation/          # Simulation management
│   ├── SimulationManager.cs     # Game loop coordinator
│   └── ParticleFactory.cs       # Random particle generation
├── Audio/               # Sound, on top of RP.Sound
│   ├── SimulationAudio.cs       # Events -> sounds; mass -> pitch, position -> pan
│   ├── SoundPalette.cs          # Bakes the RP.Sound palette at start-up
│   └── WaveOutDevice.cs         # WinMM output device
├── UI/                  # Input and binding
├── Utilities/           # Helper classes, constants, seeded random
├── Views/               # WPF UI
│   ├── MainWindow.xaml          # Main window layout
│   └── MainWindow.xaml.cs       # UI code-behind
└── extern/              # Shared libraries, as git submodules
    ├── Math/                    # RP.Math
    ├── Game/                    # RP.Game (engine) and RP.Game.Silk (Vulkan/OpenAL)
    └── Sound/                   # RP.Sound
```

The three libraries under `extern/` are separate repositories, checked out as siblings so their
own relative project references resolve. A fresh clone needs them pulled down too:

```bash
git clone --recurse-submodules https://github.com/dickiepotter/DotGame.git
# or, in an existing clone:
git submodule update --init --recursive
```

This project references `RP.Game`, not `RP.Game.Silk` — the engine half carries no Vulkan, no
OpenAL and no shader-compilation build step, which is what makes it reasonable for a 2D WPF
application to depend on at all.

## Physics Implementation

### Semi-Implicit Euler Integration
```
velocity += acceleration × Δt
position += velocity × Δt
```

Energy-based speed multipliers raise or lower a particle's speed *ceiling*; they are not
applied to the position step as well, so the velocity the physics sees is the velocity the
particle actually travels at.

### Fixed Timestep
The simulation advances in fixed `1/60s` increments. Each rendered frame banks the real
elapsed time in an accumulator and spends it in whole steps (capped at 5 per frame to avoid
a spiral of death after a stall). Stepping by wall-clock frame time instead would make the
outcome depend on frame pacing, so two runs of the same seed would drift apart no matter how
carefully the randomness was seeded.

Damping is additionally authored as a per-frame factor at 60 FPS and converted to the
equivalent continuous decay via `factor^(Δt × 60)`.

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

### Predation (Inelastic Merge)
When a predator consumes prey it absorbs the prey's momentum along with its mass:
```
v = (m_predator × v_predator + m_absorbed × v_prey) / (m_predator + m_absorbed)
```
Prey is claimed atomically within a frame, so two overlapping predators cannot both
consume the same particle.

### Energy Budget
Ambient gain is the simulation's only source of new energy - eating merely redistributes it
(at 90% efficiency, with 15% of prey mass discarded per meal). Passive drain scales with
`mass^0.66`, i.e. with surface area, so large particles are expensive to maintain. Both
sides of the budget are adjustable in the Energy tab: raising **Ambient Energy Gain**
supports larger populations, raising **Passive Energy Drain** suppresses them.

### Splitting and the Growth Ratchet
A particle's `MaxEnergy` scales with its mass, so as it eats and grows its energy
*percentage* falls. Because both the split trigger and the split cost were expressed as
percentages of `MaxEnergy`, the particles most in need of splitting were the ones least able
to, and predation became a one-way ratchet ending in a few giants. Two mechanisms prevent
this:

- Past `MaxMass x OVERGROWN_SPLIT_MASS_RATIO` a particle splits regardless of its energy
  percentage, and this outranks hunting in the AI (a giant is permanently "hungry", so if
  chasing won it would hunt forever and never divide).
- The split cost is capped at `SPLIT_COST_CEILING_MULTIPLE` times what a reference-sized
  particle pays, so growth cannot price a particle out of its own escape hatch.

Splitting still costs energy, so a starving particle cannot do it - correctly, since
splitting does not improve energy percentage (pool and capacity both halve) and slightly
worsens the net balance (two smaller particles have more combined surface area than one).

### Propulsion (Chase / Flee)
`ChaseForce` and `FleeForce` are accelerations calibrated at `ReferenceMass`
(`(MinMass + MaxMass) / 2`). Actual acceleration is `F × ReferenceMass / m`, so heavier
particles are correspondingly harder to accelerate.

## Full Screen

**F11** toggles full screen, **Esc** leaves it, and there is a button beside Start/Stop/Reset.
The sidebar is removed and the window loses its chrome, so the simulation runs edge to edge.
A hint appears briefly on entering - a borderless window with no visible controls is otherwise
easy to mistake for a hang.

### The world is a fixed size, scaled to the window

Resizing the window - or going full screen - **zooms** the view. It does not change how much
world there is. The canvas sits in a `Viewbox` with `Stretch="Uniform"`, so the same
simulation is drawn larger, aspect ratio intact, and circles stay circular.

Two things fall out of that:

- **A seed means the same thing at any window size.** When the world was sized from whatever
  window it opened in, the same seed produced a different simulation on a different display.
  It no longer does; the world comes from the Sim Width and Sim Height fields alone.
- **Mouse input needs no conversion.** Positions taken relative to the Canvas arrive already
  in world coordinates, because WPF maps them through the Viewbox transform. Only the tooltip,
  which is an unscaled overlay, is positioned in screen space.

The default world is shaped to the **display's** aspect ratio, so full screen fills it exactly
rather than showing bars down the sides. The configured *area* is preserved rather than a
dimension - particle density is what the ecosystem is balanced around, and stretching 800x600
out to 1067x600 would quietly make the world a third emptier. On a 16:9 display the default
works out at 924x520.

Both fields are editable and applied on Reset, so any world shape or size can be set.

## Render Modes

Two visual treatments of the same simulation, switchable live in the **Visual** tab.

### Classic
Outlined, opaque discs with energy bars, grid, trails and vision cones. Precise and
readable - the right mode for inspecting what the simulation is doing.

### Luminous (default)
Every particle is an emitter in an additive high-dynamic-range light field
(`Rendering/LightField.cs`, `Rendering/LuminousRenderer.cs`). Nothing has an edge: every
primitive is a falloff that reaches exactly zero at its radius, so particles fade into the
background instead of being cut out of it.

**Why additive, not translucent.** Light is accumulated as unbounded floating-point energy
and only converted to pixels at the end, through an exposure curve. Two overlapping lights
therefore *sum* - their overlap is genuinely brighter than either alone and rolls off toward
white. Drawing translucent ellipses instead would alpha-blend, so the nearer disc would partly
hide the further one and no combination could ever exceed the brightest single source.

Each particle emits three nested falloffs - a small lightly-whitened core, a coloured body,
and a wide halo. Their sum is what removes any sense of a boundary: brightness slides
continuously from the hot centre out to black space.

**Colour follows the legend.** A particle's colour is whatever `ColorGenerator` assigned it -
the type hue from the Visual tab legend, ability tints, and brightness already scaled by
energy. Luminous reproduces that colour rather than reinterpreting it, by solving for the
linear energy that resolves to it (`LightField.LinearForDisplay`, the exact inverse of the
exposure curve *and* the gamma encode applied on the way out). Emitting arbitrary intensities
instead lets the tone curve pull every channel toward its ceiling, which drains saturation
until a red predator and a green herbivore both read as pale cream.

**Every Visual tab overlay works in this mode too**, drawn as light rather than as shapes:
the grid as faint rules, energy bars with the same dimensions and green/yellow/red thresholds
as Classic, motion trails from the same recorded history, and vision range as a soft wash.

Ability state is expressed as light rather than as overlays:

| State | Appearance |
|---|---|
| Hunting | warm pulsing corona reaching outward |
| Fleeing | tight cold shimmer |
| Eating | brief golden flare |
| Reproducing / Splitting | soft green bloom |
| Phasing | body all but vanishes into a cold diffuse cloud |
| Speed burst | comet - luminous wake plus a hot leading edge |
| Growth / shrinkage | the particle's own light shifts cyan or orange |
| Birth | motes of light converging, then an expanding shell |
| Death | flash plus streaking sparks |

Energy drives brightness directly: a starving particle is a dim ember, a full one a small star.

**Light Settings**
- **Exposure** - brightness applied before the tone curve.
- **Glow Size** - multiplier on every emission radius.
- **Light Trails** - fraction of each frame's light retained, so movement leaves a decaying
  wake. Exposure is compensated by exactly `(1 - persistence)`, since a persistent field
  converges to `1/(1-persistence)` times the per-frame contribution; anything softer than
  that multiplies up and washes the frame out.

**Performance.** Clearing, tone-mapping and uploading are per-pixel and independent of
particle count, so at full resolution they dominate the frame rather than the lights do. The
field therefore renders to a capped buffer and is stretched up on display - visually free for
low-frequency glows, and worth roughly 12.9ms -> 8.5ms per frame on a 870k-pixel canvas.

## Sound (optional)

On by default; untick it in the **Audio** tab to run silently. There are still no audio files in
the project - every sound is synthesised, not recorded - but the synthesis itself now comes from
[`RP.Sound`](extern/Sound), the procedural audio library next door, rather than from a copy of it
living in this repository.

The division is worth stating, because it is the reason the code shrank. **How** a sound is made
is a library concern: `RP.Sound.Games.SciFi` holds the palette, and `RP.Sound.Playback` holds the
voice pool, the stereo delay and the saturator. **Which** sound a simulation event makes, and how
particle state bends it, is a game concern and is all that remains here
(`Audio/SimulationAudio.cs`, `Audio/SoundPalette.cs`). Only the output device is still local
(`Audio/WaveOutDevice.cs`), because opening one is platform work that a cross-platform library
should not carry.

One consequence is worth knowing. `RP.Sound` renders offline: a description goes in, finished
samples come out. The simulation needs a sound *now*, at a pitch that depends on a particle nobody
knew about a frame ago. So `SoundPalette` bakes the whole palette at start-up - on a background
thread, so the window opens immediately - and playback varies pitch by reading the buffer faster
or slower. Because reading far from the recorded rate audibly distorts a sound, and mass moves
pitch over a span of nearly eleven to one, each pitched event is baked at five base pitches and
playback picks the nearest. That holds every read within 27% of the rate it was written at, for
about 1.7MB and a fraction of a second at start-up.

The palette is deliberately science-fiction, built from four synthesis techniques rather than
from different pitches of the same beep:

- **FM** - a modulator at a non-integer ratio of the carrier produces partials that are not
  whole multiples of the fundamental, which is what makes a tone read as metallic or
  synthetic instead of as a plain musical note.
- **Ring modulation** - multiplying by a second oscillator replaces the fundamental with sum
  and difference tones: clangorous and unmistakably artificial.
- **Exponential pitch sweeps** - pitch is perceived logarithmically, so a geometric glide is
  the one that sounds like an even slide rather than a lurch. This is what makes a "zap".
- **A damped stereo delay** - nothing in the simulation is in a room, but a sound with no
  reflections is heard as tiny and close. Offset repeats that darken as they decay place
  every event in a large cold space, and do more for the character of the whole palette than
  any individual voice does.

Because it is generated rather than sampled, the audio can be derived from simulation state
instead of merely accompanying it:

- **Pitch follows mass.** A heavy particle speaks low and a light one high, for the same
  reason a large bell is deeper than a small one.
- **Stereo position follows screen position.** A death on the left is heard on the left.
- **The ambient drone tracks the ecosystem.** Its pitch falls as the population grows and its
  volume follows total energy, so the health of the simulation is audible without watching it.

| Event | Sound | Built from |
|---|---|---|
| Eat | energy-weapon discharge | steep downward exponential sweep + FM bite |
| Death | reactor losing containment | ring-modulated collapse, sub-octave, darkening noise |
| Birth | materialisation | rising sweep with vibrato + FM, heavy delay send |
| Split | replicator cycle | two detuned ring-modulated tones panned apart |
| Phase | transporter | deep vibrato + ring mod over a long rising sweep, smeared |
| Speed burst | thruster ignition | noise with the filter sweeping open + rising tone |

The ambient drone is three partials - fundamental, octave and a twelfth - looped seamlessly. Its
fundamental is snapped to a whole number of cycles across the buffer so the end meets the
beginning exactly; without that, a bed that plays for an hour clicks once every two seconds.

Events of the same kind are rate limited, and the voice pool is capped at 24 with new
requests dropped when it is full. Dozens of particles can eat in a single frame; without both
limits the result is a solid rasp rather than distinguishable events.

Audio is strictly a passive observer - it reads particle state and never writes it, so a run
is bit-identical with sound absent, switched off, or actively playing. If no output device is
available the feature reports why and the simulation continues in silence.

`SimulationAudio.CreateOffline()` synthesises without claiming a device, so the sound design
can be auditioned or tested by pumping `RenderTo` directly.

## Default Configuration

- **Particle Count**: 50
- **Random Seed**: 12345
- **Simulation Size**: 800×600 px of area, reshaped to the display's aspect (924×520 on 16:9)
- **Gravity Constant**: 100.0
- **Damping Factor**: 0.995 (0.5% velocity loss per 1/60s, applied continuously so behaviour is frame-rate independent)
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
