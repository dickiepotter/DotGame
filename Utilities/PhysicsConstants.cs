namespace DotGame.Utilities;

/// <summary>
/// Physics simulation constants for consistent behavior across the game
/// </summary>
public static class PhysicsConstants
{
    // Delta time limits
    public const double MAX_DELTA_TIME = 0.033; // 33ms = minimum 30 FPS
    public const double MIN_DELTA_TIME = 0.001; // 1ms minimum threshold

    // Velocity limits
    public const float MAX_VELOCITY_MULTIPLIER = 2.0f; // Maximum velocity relative to initial max
    public const float SPEED_BOOST_MULTIPLIER = 2.0f; // Speed boost ability multiplier

    // Gravity
    public const float MIN_GRAVITY_DISTANCE = 1.0f; // Minimum distance for gravity calculation
    public const float MAX_GRAVITY_ACCELERATION = 200.0f; // Maximum gravity force per frame

    // Collision
    public const double COLLISION_SEPARATION_FACTOR = 0.5; // How much to separate colliding particles
    public const double COLLISION_VELOCITY_TRANSFER = 0.9; // Momentum transfer efficiency
}
