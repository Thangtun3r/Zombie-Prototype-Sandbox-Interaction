using UnityEngine;

namespace EnvironmentInteraction
{
    public interface IExplosionBreakable
    {
        void BreakFromExplosion(Vector3 origin, float force);
    }
}
