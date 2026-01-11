using System.Numerics;
using System.Windows.Media;

namespace DotGame.Models;

public class Particle
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public double Mass { get; set; }
    public double Radius { get; set; }
    public Color Color { get; set; }

    // Computed property for performance optimization
    public double InverseMass => Mass > 0 ? 1.0 / Mass : 0;

    // Store previous position for collision detection optimization if needed
    public Vector2 PreviousPosition { get; set; }

    public Particle()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        PreviousPosition = Vector2.Zero;
    }
}
