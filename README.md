# Conway's Game of Life - C# Implementation

A simple console-based implementation of Conway's Game of Life in C#.

## About

Conway's Game of Life is a cellular automaton devised by mathematician John Conway. It's a zero-player game where the evolution is determined by its initial state.

### Rules

1. Any live cell with 2-3 live neighbors survives
2. Any dead cell with exactly 3 live neighbors becomes alive
3. All other cells die or stay dead

## Features

- 40x20 grid with toroidal (wrap-around) boundaries
- Randomized initial state (30% cell density)
- Real-time visualization using block characters (██)
- Generation counter
- Press any key to exit

## Building

The project requires .NET 8.0 SDK:

```bash
dotnet build GameOfLife.csproj
```

## Running

### Option 1: Using the executable directly
```bash
./bin/Debug/net8.0/GameOfLife
```

### Option 2: Using dotnet
```bash
dotnet bin/Debug/net8.0/GameOfLife.dll
```

### Option 3: Using the run script
```bash
./run-game.sh
```

## Files

- `Program.cs` - Main implementation
- `GameOfLife.csproj` - Project configuration
- `bin/Debug/net8.0/GameOfLife` - Compiled executable (74KB)
- `bin/Debug/net8.0/GameOfLife.dll` - .NET assembly
- `run-game.sh` - Convenience script to run the game

## Implementation Details

The implementation uses a simple double-buffering approach:
- Current state stored in `grid`
- Next state calculated in `nextGrid`
- Grids are swapped after each generation

The game runs at approximately 10 FPS (100ms delay between generations).
