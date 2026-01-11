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

    public double NextDouble(double min, double max)
    {
        return min + _random.NextDouble() * (max - min);
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
