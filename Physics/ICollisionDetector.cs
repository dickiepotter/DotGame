using DotGame.Models;

namespace DotGame.Physics;

public interface ICollisionDetector
{
    void DetectAndResolve(List<Particle> particles);
}
