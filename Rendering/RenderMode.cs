namespace DotGame.Rendering;

/// <summary>
/// How the simulation is drawn. The simulation itself is identical in either mode - only
/// the interpretation of a particle changes.
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Outlined, opaque discs with energy bars and overlays. Precise and readable; the
    /// right choice for inspecting what the simulation is actually doing.
    /// </summary>
    Classic,

    /// <summary>
    /// Every particle is an emitter in an additive HDR light field. No outlines, no opaque
    /// fills - cores blow out to white, haloes fade into the dark, and overlapping particles
    /// genuinely add their light together.
    /// </summary>
    Luminous
}
