namespace DotGame.Utilities;

/// <summary>
/// Gameplay mechanics constants and balancing values
/// </summary>
public static class GameplayConstants
{
    // Particle ID ranges for spawned particles
    public const int SPLITTING_PARTICLE_ID_START = 10000;
    public const int REPRODUCTION_PARTICLE_ID_START = 20000;

    // Reproduction
    public const double REPRODUCTION_ENERGY_THRESHOLD = 0.6; // Minimum energy % to reproduce

    // Ability inheritance
    public const double ABILITY_INHERITANCE_CHANCE = 0.7; // 70% chance to inherit parent abilities

    // Detection
    public const double DEFAULT_DETECTION_RANGE_MULTIPLIER = 3.0;

    // Type synergy multipliers
    public static class TypeSynergy
    {
        // Chase force multipliers by type
        public const double PREDATOR_CHASE_MULT = 1.3;
        public const double HERBIVORE_CHASE_MULT = 0.7;
        public const double DEFAULT_CHASE_MULT = 1.0;

        // Flee force multipliers by type
        public const double HERBIVORE_FLEE_MULT = 1.2;
        public const double PREDATOR_FLEE_MULT = 0.8;
        public const double DEFAULT_FLEE_MULT = 1.0;

        // Energy cost multipliers by type
        public const double NEUTRAL_ENERGY_COST_MULT = 0.9;
        public const double DEFAULT_ENERGY_COST_MULT = 1.0;

        // Reproduction multipliers by type
        public const double HERBIVORE_REPRODUCTION_MULT = 1.3;
        public const double SOCIAL_REPRODUCTION_MULT = 1.2;
        public const double DEFAULT_REPRODUCTION_MULT = 1.0;

        // Vision range multipliers by type
        public const double PREDATOR_VISION_MULT = 1.2;
        public const double SOLITARY_VISION_MULT = 1.1;
        public const double DEFAULT_VISION_MULT = 1.0;
    }

    // Energy to mass conversion
    public const double ENERGY_TO_MASS_RATIO = 0.1; // 10:1 energy to mass
    public const double MASS_TO_ENERGY_RATIO = 10.0; // 1:10 mass to energy
}
