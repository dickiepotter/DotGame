namespace DotGame.Utilities;

/// <summary>
/// Allocates unique particle IDs for a single simulation run.
///
/// This is deliberately an instance rather than a static counter: the renderer keys its
/// visual elements by particle Id, so two live particles sharing an Id means one of them
/// becomes invisible. A single generator per simulation guarantees every source of new
/// particles (factory, splitting, reproduction, user clicks) draws from the same sequence.
/// </summary>
public class ParticleIdGenerator
{
    private int _nextId;

    public ParticleIdGenerator(int startId = 0)
    {
        _nextId = startId;
    }

    /// <summary>
    /// Returns the next unused particle ID.
    /// </summary>
    public int Next() => _nextId++;

    /// <summary>
    /// Ensures subsequent IDs are greater than the supplied one. Used when particles are
    /// adopted from an external source (e.g. a loaded simulation state).
    /// </summary>
    public void Reserve(int usedId)
    {
        if (usedId >= _nextId)
            _nextId = usedId + 1;
    }
}
