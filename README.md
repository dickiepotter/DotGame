# DotGame - Continuous Space Game of Life

A Rust implementation of Conway's Game of Life where cells exist in continuous 2D Euclidean space instead of a discrete grid.

## Features

- Cells move continuously through 2D space with velocity vectors
- Neighbor detection based on Euclidean distance (within a 30-unit radius)
- Classic Game of Life rules:
  - A cell survives if it has 2-3 neighbors
  - A new cell is born if exactly 3 cells are nearby
- Cells wrap around screen edges
- Visual display using Macroquad

## Controls

- **R** - Reset the simulation with new random cells

## Running

```bash
cargo run --release
```

## How it Works

Unlike traditional Game of Life on a grid, this implementation:

1. Each cell has a continuous (x, y) position and (vx, vy) velocity
2. Cells drift through space at constant velocity
3. Neighbor counting uses Euclidean distance rather than adjacent grid cells
4. New cells spawn at random positions near existing cells when the birth condition is met (exactly 3 neighbors)
5. The simulation updates every 0.5 seconds while cells move continuously

## Parameters

You can adjust these constants in `src/main.rs`:

- `NEIGHBOR_RADIUS` - Distance to count as a neighbor (default: 30.0)
- `CELL_RADIUS` - Visual size of cells (default: 4.0)
- `INITIAL_CELLS` - Starting number of cells (default: 100)
- `SPEED` - Movement speed of cells (default: 20.0)
