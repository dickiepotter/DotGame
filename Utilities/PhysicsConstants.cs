namespace DotGame.Utilities;

/// <summary>
/// Physics simulation constants for consistent behavior across the game
/// </summary>
public static class PhysicsConstants
{
    // Fixed simulation timestep.
    //
    // The simulation advances in fixed increments rather than by wall-clock frame time.
    // Stepping by real elapsed time makes the outcome depend on frame pacing, so two runs
    // of the same seed drift apart no matter how carefully the randomness is seeded.
    public const double FIXED_DELTA_TIME = 1.0 / 60.0;

    // Ceiling on catch-up steps per rendered frame. Without it, a long stall (a breakpoint,
    // a background task) queues up more work than the next frame can clear, and each frame
    // falls further behind - the classic "spiral of death".
    public const int MAX_STEPS_PER_FRAME = 5;

    // Velocity limits
    public const float MAX_VELOCITY_MULTIPLIER = 2.0f; // Maximum velocity relative to initial max
    public const float SPEED_BOOST_MULTIPLIER = 2.0f; // Speed boost ability multiplier

    // Gravity
    public const float MIN_GRAVITY_DISTANCE = 1.0f; // Minimum distance for gravity calculation
    public const float MAX_GRAVITY_ACCELERATION = 200.0f; // Maximum gravity force per frame

    // Damping
    // Damping is authored as a per-frame factor at this rate, then converted to a
    // continuous rate so the simulation behaves identically at any frame rate.
    public const double DAMPING_REFERENCE_FPS = 60.0;
}
