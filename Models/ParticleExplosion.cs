using System.Numerics;
using System.Windows.Media;

namespace DotGame.Models;

public class ParticleExplosion
{
    public int ParticleId { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public double Radius { get; set; }
    public double TimeElapsed { get; set; }
    public double Duration { get; set; } = 0.3; // 300ms explosion animation
    public List<ExplosionFragment> Fragments { get; set; }

    public bool IsComplete => TimeElapsed >= Duration;
    public double Progress => Math.Min(1.0, TimeElapsed / Duration);

    public ParticleExplosion(Particle particle)
    {
        ParticleId = particle.Id;
        Position = particle.Position;
        Color = particle.Color;
        Radius = particle.Radius;
        TimeElapsed = 0;
        Fragments = CreateFragments(particle);
    }

    private List<ExplosionFragment> CreateFragments(Particle particle)
    {
        var fragments = new List<ExplosionFragment>();
        int fragmentCount = (int)(particle.Radius * 2); // More fragments for larger particles
        fragmentCount = Math.Max(8, Math.Min(fragmentCount, 20)); // Between 8-20 fragments

        var random = new Random();

        for (int i = 0; i < fragmentCount; i++)
        {
            double angle = (2 * Math.PI * i) / fragmentCount + random.NextDouble() * 0.3;
            double speed = particle.Radius * (2 + random.NextDouble() * 2); // Speed based on size

            var fragment = new ExplosionFragment
            {
                Position = particle.Position,
                Velocity = new Vector2(
                    (float)(Math.Cos(angle) * speed),
                    (float)(Math.Sin(angle) * speed)
                ),
                Size = particle.Radius * (0.1 + random.NextDouble() * 0.2),
                Color = particle.Color
            };

            fragments.Add(fragment);
        }

        return fragments;
    }

    public void Update(double deltaTime)
    {
        TimeElapsed += deltaTime;

        // Update fragment positions
        foreach (var fragment in Fragments)
        {
            fragment.Position += fragment.Velocity * (float)deltaTime;
        }
    }
}

public class ExplosionFragment
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public double Size { get; set; }
    public Color Color { get; set; }
}
