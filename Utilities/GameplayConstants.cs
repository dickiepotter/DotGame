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

    // Energy capacity: MaxEnergy = Mass * (BaseEnergyCapacity / ENERGY_CAPACITY_REFERENCE_MASS)
    public const double ENERGY_CAPACITY_REFERENCE_MASS = 10.0;

    // Speed burst
    public const double SPEED_BURST_IMPULSE = 1.5;  // Immediate velocity multiplier on activation
    public const double SPEED_BURST_DURATION = 3.0; // Seconds the raised speed ceiling lasts

    // Splitting: breaking the growth ratchet
    //
    // Predation is otherwise a one-way consolidation. A particle that eats grows, its
    // MaxEnergy grows with its mass, so its energy *percentage* falls - and both the AI's
    // split trigger and the split cost are expressed as percentages of MaxEnergy. The
    // result is that the particles most in need of splitting are precisely the ones that
    // can never afford to, and the population ratchets down into a handful of giants.

    /// <summary>
    /// Multiple of MaxMass beyond which a particle splits regardless of its energy
    /// percentage. At this size splitting is unambiguously the right move, so the normal
    /// "only when comfortably fed" gate is bypassed.
    /// </summary>
    public const double OVERGROWN_SPLIT_MASS_RATIO = 2.0;

    /// <summary>
    /// Ceiling on the splitting energy cost, as a multiple of the cost paid by a particle of
    /// ReferenceMass. Cost still scales with size up to this point; past it, growth can no
    /// longer price a particle out of its own escape hatch.
    /// </summary>
    public const double SPLIT_COST_CEILING_MULTIPLE = 2.0;

    /// <summary>
    /// Energy fraction the AI requires before choosing to split at normal sizes.
    /// </summary>
    public const double SPLIT_ENERGY_THRESHOLD = 0.6;

    /// <summary>
    /// Fraction of MaxMass at which the AI starts considering a split at all.
    /// </summary>
    public const double SPLIT_MASS_THRESHOLD = 0.7;
}
