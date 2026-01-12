namespace DotGame.Utilities;

/// <summary>
/// Rendering and visual display constants
/// </summary>
public static class RenderingConstants
{
    // Grid
    public const double GRID_SPACING = 50.0; // Grid line spacing in pixels

    // Energy bars
    public const double ENERGY_BAR_WIDTH = 30.0;
    public const double ENERGY_BAR_HEIGHT = 4.0;
    public const double ENERGY_BAR_OFFSET = 8.0; // Distance above particle

    // Energy bar color thresholds
    public const double ENERGY_HIGH_THRESHOLD = 0.6; // Green above this
    public const double ENERGY_MEDIUM_THRESHOLD = 0.3; // Yellow above this, red below

    // Birth animation
    public const double BIRTH_MIN_OPACITY = 0.3; // Starting opacity for birthing particles
    public const double BIRTH_MAX_OPACITY = 1.0; // Final opacity for birthing particles

    // Phasing
    public const byte PHASING_OPACITY = 128; // Alpha value for phasing particles (50% transparent)

    // Trails
    public const double TRAIL_OPACITY = 0.3;
    public const double TRAIL_LINE_THICKNESS = 1.0;

    // Vision cone
    public const double VISION_CONE_OPACITY = 0.5;
    public const double VISION_CONE_LINE_THICKNESS = 2.0;

    // UI update intervals
    public const int UI_UPDATE_INTERVAL_MS = 200; // Performance stats update interval

    // FPS color thresholds
    public const double FPS_GOOD_THRESHOLD = 50.0; // Green FPS display
    public const double FPS_OK_THRESHOLD = 30.0; // Orange FPS display

    // Tooltip
    public const double TOOLTIP_OFFSET_X = 15.0;
    public const double TOOLTIP_OFFSET_Y = 15.0;
}
