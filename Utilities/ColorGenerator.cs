using System.Windows.Media;

namespace DotGame.Utilities;

public static class ColorGenerator
{
    public static Color RandomColor(RandomGenerator random)
    {
        return random.NextColor();
    }

    public static Color GetColorForMass(double mass, double minMass, double maxMass)
    {
        // Generate color based on mass - heavier particles are redder, lighter are bluer
        double normalized = (mass - minMass) / (maxMass - minMass);

        byte red = (byte)(normalized * 255);
        byte blue = (byte)((1 - normalized) * 255);
        byte green = (byte)(128);

        return Color.FromRgb(red, green, blue);
    }
}
