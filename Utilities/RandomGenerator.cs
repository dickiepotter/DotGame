using System.Windows.Media;

namespace DotGame.Utilities;

public class RandomGenerator
{
    private readonly Random _random;

    public int Seed { get; }

    public RandomGenerator(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }

    public double NextDouble(double min, double max)
    {
        return min + _random.NextDouble() * (max - min);
    }

    /// <summary>
    /// A uniformly random unit vector. Used for separation impulses on split/reproduce.
    /// </summary>
    public System.Numerics.Vector2 NextUnitVector()
    {
        double angle = _random.NextDouble() * Math.PI * 2.0;
        return new System.Numerics.Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
    }

    public int Next(int min, int max)
    {
        return _random.Next(min, max);
    }

    public Color NextColor()
    {
        return Color.FromRgb(
            (byte)_random.Next(256),
            (byte)_random.Next(256),
            (byte)_random.Next(256)
        );
    }
}
