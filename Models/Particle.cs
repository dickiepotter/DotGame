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

    // Store previous radius to track growth/shrinkage for visual effects
    public double PreviousRadius { get; set; }

    // Ability system (nullable for backward compatibility)
    public ParticleAbilities? Abilities { get; set; }

    // Computed properties for abilities
    public bool HasAbilities => Abilities != null;
    public double EnergyPercentage => HasAbilities ? Abilities.Energy / Abilities.MaxEnergy : 1.0;
    public bool IsHungry => HasAbilities && Abilities.Energy < Abilities.MaxEnergy * 0.3;
    public bool IsAlive => !HasAbilities || Abilities.IsAlive;

    public Particle()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        PreviousPosition = Vector2.Zero;
    }
}
