namespace DotGame.Models;

/// <summary>
/// Represents a temporary state with a duration that automatically expires after a time period.
/// Used to simplify management of time-based states like phasing, speed boost, camouflage, etc.
/// </summary>
public class TimedState
{
    private double _timeRemaining;

    /// <summary>
    /// Gets whether this state is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the time remaining before this state expires. Returns 0 if not active.
    /// </summary>
    public double TimeRemaining
    {
        get => IsActive ? _timeRemaining : 0;
        private set => _timeRemaining = value;
    }

    /// <summary>
    /// Gets the progress of this state from 0 (just started) to 1 (about to expire).
    /// Returns 1 if not active.
    /// </summary>
    public double Progress
    {
        get
        {
            if (!IsActive || Duration <= 0) return 1.0;
            return 1.0 - (TimeRemaining / Duration);
        }
    }

    /// <summary>
    /// Gets the total duration of this state when activated.
    /// </summary>
    public double Duration { get; private set; }

    /// <summary>
    /// Activates this state with the specified duration.
    /// </summary>
    /// <param name="duration">How long the state should remain active in seconds.</param>
    public void Activate(double duration)
    {
        IsActive = true;
        Duration = duration;
        TimeRemaining = duration;
    }

    /// <summary>
    /// Immediately deactivates this state, regardless of remaining time.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        TimeRemaining = 0;
    }

    /// <summary>
    /// Updates this state by the given time delta. Automatically deactivates when time expires.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    /// <returns>True if the state just expired this frame, false otherwise.</returns>
    public bool Update(double deltaTime)
    {
        if (!IsActive) return false;

        TimeRemaining -= deltaTime;

        if (TimeRemaining <= 0)
        {
            Deactivate();
            return true; // State just expired
        }

        return false; // Still active
    }

    /// <summary>
    /// Creates a new inactive timed state.
    /// </summary>
    public TimedState()
    {
        IsActive = false;
        TimeRemaining = 0;
        Duration = 0;
    }
}
