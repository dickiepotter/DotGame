# DotGame Refactoring Plan

## Overview
This document outlines a comprehensive refactoring plan to improve code readability and enforce separation of concerns across the DotGame codebase.

## Current Status

### ✅ Completed (Phase 1)
- **Constants Classes Created**:
  - `PhysicsConstants.cs` - Physics simulation parameters
  - `RenderingConstants.cs` - Visual display parameters
  - `GameplayConstants.cs` - Gameplay mechanics and balancing
- **Files Updated to Use Constants**:
  - PhysicsEngine.cs
  - SimulationManager.cs
  - GravityCalculator.cs
  - ParticleRenderer.cs (partial)

**Commit**: `c97fb17` - "Refactor: Add constants classes and eliminate magic numbers (Phase 1)"

---

## Remaining Work

### Phase 2: Complete Constants Migration (2-3 hours)

#### Files Still Needing Updates:
1. **MainWindow.xaml.cs** - Update UI timers and FPS thresholds
   - Line 50: `200` → `RenderingConstants.UI_UPDATE_INTERVAL_MS`
   - Lines 305-310: FPS thresholds → `FPS_GOOD_THRESHOLD`, `FPS_OK_THRESHOLD`
   - Lines 429-430: Tooltip offsets → `TOOLTIP_OFFSET_X`, `TOOLTIP_OFFSET_Y`

2. **ReproductionAbility.cs** - Update gameplay constants
   - Line 11: `20000` → `GameplayConstants.REPRODUCTION_PARTICLE_ID_START`
   - Line 29: `0.6` → `GameplayConstants.REPRODUCTION_ENERGY_THRESHOLD`
   - Lines 187-203: `0.7` → `GameplayConstants.ABILITY_INHERITANCE_CHANCE`

3. **EatingAbility.cs** - Update gameplay constants
   - Line 150: `0.1` → `GameplayConstants.ABILITY_INHERITANCE_CHANCE`
   - Line 119: `3` → `GameplayConstants.DEFAULT_DETECTION_RANGE_MULTIPLIER`

4. **SplittingAbility.cs** - Update particle ID start
   - Line 11: `10000` → `GameplayConstants.SPLITTING_PARTICLE_ID_START`

5. **ParticleAbilities.cs** - Update type synergy multipliers
   - Lines 148-179: Replace all magic multipliers with `GameplayConstants.TypeSynergy.*`

6. **AbilityManager.cs** - Update phasing opacity
   - Line 209: `128` → `RenderingConstants.PHASING_OPACITY`

---

### Phase 3: Create Utility Classes (6-8 hours)

#### 3.1 ParticleQueryUtility (2-3 hours)
**Purpose**: Consolidate duplicate particle search logic found in 4+ files

**New File**: `Utilities/ParticleQueryUtility.cs`

**Methods**:
```csharp
public static class ParticleQueryUtility
{
    public static Particle? FindEdiblePrey(Particle predator, List<Particle> allParticles, SimulationConfig config);
    public static Particle? FindChaseTarget(Particle hunter, List<Particle> allParticles, SimulationConfig config);
    public static Particle? FindThreat(Particle particle, List<Particle> visible, SimulationConfig config);
    public static List<Particle> GetVisibleParticles(Particle observer, List<Particle> allParticles);
    public static Particle? FindParticleAtPosition(Vector2 position, List<Particle> allParticles);
    public static bool CanEat(Particle predator, Particle prey, SimulationConfig config);
}
```

**Files to Update**:
- EatingAbility.cs - Replace `FindEdibleParticle` with utility call
- ChaseAbility.cs - Replace `FindTarget` with utility call
- FleeAbility.cs - Use `FindThreat`
- ParticleAI.cs - Use utility methods
- SimulationManager.cs - Use `FindParticleAtPosition`

---

#### 3.2 TimedState Class (1-2 hours)
**Purpose**: Replace repeated boolean + timer pattern

**New File**: `Utilities/TimedState.cs`

**Implementation**:
```csharp
public class TimedState
{
    public bool IsActive { get; private set; }
    public double TimeRemaining { get; private set; }
    public double Duration { get; private set; }

    public void Activate(double duration);
    public void Update(double deltaTime);
    public void Deactivate();
}
```

**Impact**:
- ParticleAbilities.cs: Replace 4 boolean+timer pairs with `TimedState` instances
  - IsPhasing/PhasingTimeRemaining → `PhasingState`
  - IsSpeedBoosted/SpeedBoostTimeRemaining → `SpeedBoostState`
  - IsCamouflaged/CamouflageTimeRemaining → `CamouflageState`
  - IsBirthing/BirthTimeRemaining → `BirthState`
- AbilityManager.cs: Simplify `UpdateTemporaryStates` method (lines 219-271)

---

#### 3.3 ConfigUIBinder (3-4 hours)
**Purpose**: Extract 300+ lines of UI-Config sync from MainWindow

**New File**: `UI/ConfigUIBinder.cs`

**Methods**:
```csharp
public class ConfigUIBinder
{
    public void UpdateConfigFromUI(SimulationConfig config);
    public void PopulateUIFromConfig(SimulationConfig config);
    public void HandleSliderValueChanged(Slider slider);
}
```

**Impact**:
- Reduces MainWindow.xaml.cs from 997 lines to ~700 lines
- Extracts methods from lines 106-282 (UpdateConfigFromUI) and 610-736 (PopulateUIFromConfig)

---

#### 3.4 SimulationInputHandler (2-3 hours)
**Purpose**: Extract mouse/touch input handling from MainWindow

**New File**: `Input/SimulationInputHandler.cs`

**Methods**:
```csharp
public class SimulationInputHandler
{
    public void HandleMouseLeftButtonDown(MouseButtonEventArgs e);
    public void HandleMouseRightButtonDown(MouseButtonEventArgs e);
    public void HandleMouseMove(MouseEventArgs e);
    public void HandleMouseRightButtonUp(MouseButtonEventArgs e);
    public void HandleTouchDown(TouchEventArgs e);
    public void HandleTouchMove(TouchEventArgs e);
    public void HandleTouchUp(TouchEventArgs e);

    public Particle? HoveredParticle { get; }
    public Point LastMousePosition { get; }
}
```

**Impact**:
- Extracts ~200 lines from MainWindow.xaml.cs (lines 336-500, 894-995)

---

#### 3.5 ParticleTooltipManager (1 hour)
**Purpose**: Extract tooltip logic from MainWindow

**New File**: `UI/ParticleTooltipManager.cs`

**Methods**:
```csharp
public class ParticleTooltipManager
{
    public void Show(Particle particle, Point mousePosition);
    public void Hide();
}
```

**Impact**:
- Extracts ~70 lines from MainWindow.xaml.cs (lines 422-490)

---

### Phase 4: Split ParticleRenderer (8-10 hours)
**Purpose**: Break 617-line class into 6 focused classes

**Current Issues**:
- 8 distinct responsibilities in one class
- Difficult to test individual components
- Hard to maintain and extend

**New Structure**:

#### 4.1 RenderingCore.cs
- Core particle rendering (ellipse management, position/size updates)
- Extract from lines 58-102, 204-251

#### 4.2 GridRenderer.cs
- Grid overlay rendering
- Extract from lines 25, 104-147, 183-195

#### 4.3 TrailRenderer.cs
- Particle trail rendering
- Extract from lines 14-15, 302-361

#### 4.4 EnergyBarRenderer.cs
- Energy bar rendering and positioning
- Extract from lines 31, 149-179, 396-430

#### 4.5 VisionConeRenderer.cs
- Vision cone overlay
- Extract from lines 28, 363-394

#### 4.6 AnimationRenderer.cs
- Explosion and birth animations
- Extract from lines 16-22, 432-616

#### 4.7 ParticleRenderer.cs (Coordinator)
- Coordinate all sub-renderers
- Single entry point for SimulationManager
- ~100 lines total (down from 617)

**Testing Strategy**:
- Test after EACH sub-renderer creation
- Visual verification (screenshots before/after)
- Performance testing (FPS monitoring)
- Toggle all visual settings

---

### Phase 5: Split AbilityManager (6-8 hours)
**Purpose**: Break 377-line class into 3 focused managers

**Current Issues**:
- "God Manager" with 8+ responsibilities
- Difficult to test energy systems separately from ability execution
- State management mixed with execution logic

**New Structure**:

#### 5.1 AbilityStateManager.cs
- Cooldown management
- Temporary state updates (phasing, speed, camouflage, birth)
- Color updates
- Extract from lines 69-80, 196-271

#### 5.2 EnergyManager.cs
- Passive energy drain
- Energy-mass conversion
- Movement speed multipliers
- Vision range updates
- Extract from lines 82-194, 273-282

#### 5.3 AbilityExecutor.cs
- Ability decision making
- Ability execution
- Ability validation
- Extract from lines 284-343, 372-375

#### 5.4 AbilityManager.cs (Coordinator)
- Coordinate all sub-managers
- Orchestrate update pipeline
- ~100 lines total (down from 377)

---

## Priority Order

### Recommended Implementation Sequence:
1. **Phase 2** (Constants Migration) - Complete the foundation
2. **Phase 3.1-3.2** (ParticleQueryUtility, TimedState) - Needed by Phase 5
3. **Phase 5** (AbilityManager Split) - Core gameplay systems
4. **Phase 4** (ParticleRenderer Split) - Complex but isolated
5. **Phase 3.3-3.5** (UI Utilities) - Lower priority, UI-focused

### Alternative: Impact-First Sequence:
1. **Phase 2** (Constants) - 2-3 hours, high value
2. **Phase 3.3** (ConfigUIBinder) - 3-4 hours, removes 300 lines from MainWindow
3. **Phase 4** (ParticleRenderer) - 8-10 hours, major architectural improvement
4. **Phase 3.1-3.2** (Utilities) - 3-4 hours, enables Phase 5
5. **Phase 5** (AbilityManager) - 6-8 hours, core systems
6. **Phase 3.4-3.5** (Input/Tooltip) - 3-4 hours, polish

---

## Testing Checklist

### After Each Phase:
- ✅ Code compiles without errors
- ✅ All existing functionality works
- ✅ No visual regressions
- ✅ No performance degradation
- ✅ Unit tests pass (if applicable)

### Phase-Specific Tests:

**Phase 2 (Constants)**:
- Run simulation, verify behavior unchanged
- Test all visual settings
- Performance monitoring shows consistent FPS

**Phase 3.1 (ParticleQueryUtility)**:
- Test eating behavior (can eat correct targets)
- Test chase/flee targeting
- Test vision systems
- Test click-to-add particles

**Phase 3.2 (TimedState)**:
- Test phasing (transparency, collision immunity)
- Test speed boost (2x speed, duration)
- Test camouflage (if implemented)
- Test birth animations

**Phase 3.3 (ConfigUIBinder)**:
- Load/save settings
- Change all sliders
- Apply all presets
- Verify config sync

**Phase 3.4 (SimulationInputHandler)**:
- Mouse drag particles
- Click to add particles
- Touch interactions (if available)
- Hover tooltips

**Phase 4 (ParticleRenderer Split)**:
- Toggle all visual settings (grid, trails, energy bars, vision cones)
- Test explosions and birth animations
- Verify particle rendering (colors, sizes, positions)
- Performance test: measure FPS impact

**Phase 5 (AbilityManager Split)**:
- Test all abilities (eating, splitting, reproduction, phasing, chase, flee, speed burst)
- Test energy systems (passive drain, mass conversion)
- Test state transitions
- Test particle lifecycles (birth to death)

---

## Estimated Time

| Phase | Estimated Time | Cumulative |
|-------|----------------|------------|
| Phase 2 | 2-3 hours | 2-3 hours |
| Phase 3.1 | 2-3 hours | 4-6 hours |
| Phase 3.2 | 1-2 hours | 5-8 hours |
| Phase 3.3 | 3-4 hours | 8-12 hours |
| Phase 3.4 | 2-3 hours | 10-15 hours |
| Phase 3.5 | 1 hour | 11-16 hours |
| Phase 4 | 8-10 hours | 19-26 hours |
| Phase 5 | 6-8 hours | 25-34 hours |
| **Total** | **25-34 hours** | |

*Note: Original estimate was 40-60 hours for all refactoring. Current plan is more focused and efficient.*

---

## Files Modified Summary

### New Files Created (14):
1. `Utilities/PhysicsConstants.cs` ✅
2. `Utilities/RenderingConstants.cs` ✅
3. `Utilities/GameplayConstants.cs` ✅
4. `Utilities/ParticleQueryUtility.cs`
5. `Utilities/TimedState.cs`
6. `UI/ConfigUIBinder.cs`
7. `Input/SimulationInputHandler.cs`
8. `UI/ParticleTooltipManager.cs`
9. `Rendering/RenderingCore.cs`
10. `Rendering/GridRenderer.cs`
11. `Rendering/TrailRenderer.cs`
12. `Rendering/EnergyBarRenderer.cs`
13. `Rendering/VisionConeRenderer.cs`
14. `Rendering/AnimationRenderer.cs`
15. `Abilities/AbilityStateManager.cs`
16. `Abilities/EnergyManager.cs`
17. `Abilities/AbilityExecutor.cs`

### Files Significantly Modified:
- PhysicsEngine.cs ✅
- SimulationManager.cs ✅
- GravityCalculator.cs ✅
- ParticleRenderer.cs ✅ (partial)
- MainWindow.xaml.cs
- ParticleAbilities.cs
- AbilityManager.cs
- ReproductionAbility.cs
- EatingAbility.cs
- SplittingAbility.cs
- ChaseAbility.cs
- FleeAbility.cs
- ParticleAI.cs

---

## Key Benefits

### Readability:
- Named constants replace magic numbers
- Single-responsibility classes
- Clear separation of concerns
- Easier to understand code flow

### Maintainability:
- Changes isolated to focused classes
- Less duplication (DRY principle)
- Easier to locate bugs
- Simpler to add features

### Testability:
- Smaller, focused classes easier to unit test
- Clear dependencies between components
- Mockable interfaces
- Integration points well-defined

### Performance:
- No performance degradation expected
- Potential improvements from better organization
- Easier to profile and optimize specific systems

---

## Next Steps

1. **Complete Phase 2** - Update remaining files to use constants (2-3 hours)
2. **Create ParticleQueryUtility** - Consolidate search logic (2-3 hours)
3. **Create TimedState** - Replace boolean+timer patterns (1-2 hours)
4. **Choose**: Either tackle AbilityManager (Phase 5) or ConfigUIBinder (Phase 3.3) next based on priority

Each phase can be committed independently, allowing for incremental progress and easier review.

---

## Contact & Questions

This refactoring plan was created through analysis of the DotGame codebase. Each phase builds on previous work, but phases can also be tackled independently based on priority.

For questions or to discuss alternative approaches, refer to commit `c97fb17` which demonstrates the pattern established for constants refactoring.

**Status**: Phase 1 Complete ✅ | Phase 2-5 Pending
**Last Updated**: 2026-01-12
