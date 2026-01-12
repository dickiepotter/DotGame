using System.Windows.Media;
using DotGame.Models;

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

    public static Color GetColorForAbilities(ParticleAbilities abilities)
    {
        // Base color on particle type
        Color baseColor = abilities.Type switch
        {
            ParticleType.Predator => Color.FromRgb(200, 50, 50),    // Red - aggressive
            ParticleType.Herbivore => Color.FromRgb(100, 200, 100), // Green - peaceful
            ParticleType.Social => Color.FromRgb(100, 150, 230),    // Blue - cooperative
            ParticleType.Solitary => Color.FromRgb(180, 100, 200),  // Purple - independent
            ParticleType.Neutral => Color.FromRgb(180, 180, 180),   // Gray - balanced
            _ => Color.FromRgb(128, 128, 128)
        };

        // Modify color based on abilities
        int r = baseColor.R;
        int g = baseColor.G;
        int b = baseColor.B;

        // Eating ability adds red tint (hunter)
        if (abilities.HasAbility(AbilitySet.Eating))
        {
            r = Math.Min(255, r + 40);
        }

        // Splitting ability adds green tint (growth)
        if (abilities.HasAbility(AbilitySet.Splitting))
        {
            g = Math.Min(255, g + 30);
        }

        // Reproduction ability adds warmth (yellow tint)
        if (abilities.HasAbility(AbilitySet.Reproduction))
        {
            r = Math.Min(255, r + 20);
            g = Math.Min(255, g + 20);
        }

        // Phasing ability adds blue/cyan tint (ethereal)
        if (abilities.HasAbility(AbilitySet.Phasing))
        {
            b = Math.Min(255, b + 40);
            g = Math.Min(255, g + 20);
        }

        // Chase ability intensifies existing color
        if (abilities.HasAbility(AbilitySet.Chase))
        {
            r = Math.Min(255, (int)(r * 1.1));
            g = Math.Min(255, (int)(g * 1.1));
            b = Math.Min(255, (int)(b * 1.1));
        }

        // Flee ability reduces saturation slightly (cautious)
        if (abilities.HasAbility(AbilitySet.Flee))
        {
            r = (r + 128) / 2;
            g = (g + 128) / 2;
            b = (b + 128) / 2;
        }

        // Apply energy gradient: bright = high energy, dark = low energy
        // Use a more visible gradient range (0.5 to 1.0) for better visual feedback
        double energyFactor = abilities.Energy / abilities.MaxEnergy;
        energyFactor = Math.Clamp(energyFactor, 0, 1);

        // Map 0-100% energy to 50-100% brightness for visibility
        double brightnessFactor = 0.5 + (energyFactor * 0.5);

        r = (int)(r * brightnessFactor);
        g = (int)(g * brightnessFactor);
        b = (int)(b * brightnessFactor);

        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
}
