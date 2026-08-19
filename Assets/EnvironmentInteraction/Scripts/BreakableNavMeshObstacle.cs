using UnityEngine;
using UnityEngine.AI;
using ZombiePrototype;

namespace EnvironmentInteraction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshObstacle))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BreakableNavMeshObstacle : MonoBehaviour, IExplosionBreakable
    {
        [SerializeField, Min(0.1f)] private float destroyDelay = 3f;
        [SerializeField, Min(0f)] private float forceMultiplier = 2f;
        [SerializeField, Min(0f)] private float upwardModifier = 0.8f;

        private bool broken;

        public void Configure(float configuredDestroyDelay, float configuredForceMultiplier, float configuredUpwardModifier)
        {
            destroyDelay = Mathf.Max(0.1f, configuredDestroyDelay);
            forceMultiplier = Mathf.Max(0f, configuredForceMultiplier);
            upwardModifier = Mathf.Max(0f, configuredUpwardModifier);
        }

        public void BreakFromExplosion(Vector3 origin, float force)
        {
            if (broken)
                return;

            broken = true;

            DynamicNavMeshTestBlock movingBlock = GetComponent<DynamicNavMeshTestBlock>();
            if (movingBlock != null)
                movingBlock.enabled = false;

            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                obstacle.carving = false;
                obstacle.enabled = false;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            body.AddExplosionForce(
                Mathf.Max(0f, force) * forceMultiplier,
                origin,
                Mathf.Max(1f, Vector3.Distance(origin, transform.position) + 2f),
                upwardModifier,
                ForceMode.Impulse);

            Destroy(gameObject, destroyDelay);
        }
    }
}
