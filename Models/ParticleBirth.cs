using System.Numerics;
using System.Windows.Media;

namespace DotGame.Models;

public class ParticleBirth
{
    public int ParticleId { get; set; }
    public Vector2 StartPosition { get; set; }
    public Vector2 TargetPosition { get; set; }
    public Color Color { get; set; }
    public double TargetRadius { get; set; }
    public double TimeElapsed { get; set; }
    public double Duration { get; set; }
    public List<BirthFragment> Fragments { get; set; }

    public bool IsComplete => TimeElapsed >= Duration;
    public double Progress => Math.Min(1.0, TimeElapsed / Duration);

    // Eased progress for smooth animation (ease-out cubic)
    public double EasedProgress
    {
        get
        {
            double t = Progress;
            return 1 - Math.Pow(1 - t, 3);
        }
    }

    public ParticleBirth(Particle particle, Vector2? parentPosition = null)
    {
        ParticleId = particle.Id;
        TargetPosition = particle.Position;
        StartPosition = parentPosition ?? particle.Position;
        Color = particle.Color;
        TargetRadius = particle.Radius;
        TimeElapsed = 0;
        Duration = particle.HasAbilities ? particle.Abilities.BirthTimeRemaining : 1.0;
        Fragments = CreateFragments(particle, parentPosition);
    }

    private List<BirthFragment> CreateFragments(Particle particle, Vector2? parentPosition)
    {
        var fragments = new List<BirthFragment>();
        int fragmentCount = (int)(particle.Radius * 1.5); // Fewer fragments than explosion
        fragmentCount = Math.Max(6, Math.Min(fragmentCount, 15)); // Between 6-15 fragments

        var random = new Random();
        Vector2 startPos = parentPosition ?? particle.Position;

        for (int i = 0; i < fragmentCount; i++)
        {
            double angle = (2 * Math.PI * i) / fragmentCount + random.NextDouble() * 0.2;
            double distance = particle.Radius * (1.5 + random.NextDouble() * 1.5);

            // Fragments start in a circle around the start position
            Vector2 fragmentStart = new Vector2(
                startPos.X + (float)(Math.Cos(angle) * distance),
                startPos.Y + (float)(Math.Sin(angle) * distance)
            );

            var fragment = new BirthFragment
            {
                StartPosition = fragmentStart,
                TargetPosition = particle.Position,
                Size = particle.Radius * (0.15 + random.NextDouble() * 0.15),
                Color = particle.Color
            };

            fragments.Add(fragment);
        }

        return fragments;
    }

    public void Update(double deltaTime)
    {
        TimeElapsed += deltaTime;
    }

    public Vector2 GetCurrentPosition()
    {
        // Interpolate between start and target position using eased progress
        return Vector2.Lerp(StartPosition, TargetPosition, (float)EasedProgress);
    }

    public double GetCurrentRadius()
    {
        // Start small and grow to target radius
        return TargetRadius * EasedProgress;
    }
}

public class BirthFragment
{
    public Vector2 StartPosition { get; set; }
    public Vector2 TargetPosition { get; set; }
    public double Size { get; set; }
    public Color Color { get; set; }

    public Vector2 GetCurrentPosition(double progress)
    {
        // Fragments converge toward the target position
        return Vector2.Lerp(StartPosition, TargetPosition, (float)progress);
    }
}
