using System.Collections.Generic;
using DotGame.Models;
using DotGame.Physics;
using DotGame.Rendering;

namespace DotGame.Abilities;

public class AbilityContext
{
    public List<Particle> AllParticles { get; set; }
    public SimulationConfig Config { get; set; }
    public double DeltaTime { get; set; }
    public SpatialHashGrid? SpatialGrid { get; set; }
    public List<Particle> ParticlesToAdd { get; set; }
    public HashSet<int> ParticlesToRemove { get; set; }
    public ParticleRenderer? Renderer { get; set; }

    public AbilityContext()
    {
        AllParticles = new List<Particle>();
        Config = new SimulationConfig();
        ParticlesToAdd = new List<Particle>();
        ParticlesToRemove = new HashSet<int>();
    }
}

public interface IAbility
{
    AbilityType Type { get; }
    double EnergyCost { get; }
    double CooldownDuration { get; }

    bool CanExecute(Particle particle, AbilityContext context);
    void Execute(Particle particle, AbilityContext context);
}
