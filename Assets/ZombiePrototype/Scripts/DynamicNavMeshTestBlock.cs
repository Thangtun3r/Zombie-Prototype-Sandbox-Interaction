using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using EnvironmentInteraction;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshObstacle))]
    public sealed class DynamicNavMeshTestBlock : MonoBehaviour, IExplosionBreakable
    {
        [SerializeField] private Vector3 blockedPosition = new Vector3(0f, 1f, -1.5f);
        [SerializeField] private Vector3 openPosition = new Vector3(12f, 1f, -1.5f);
        [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
        [SerializeField] private bool startsBlocked = true;

        private bool isBlocked;

        private void Awake()
        {
            isBlocked = startsBlocked;
            transform.position = isBlocked ? blockedPosition : openPosition;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
                isBlocked = !isBlocked;

            Vector3 destination = isBlocked ? blockedPosition : openPosition;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime);
        }

        public void Configure(Vector3 blocked, Vector3 open, float speed, bool beginBlocked)
        {
            blockedPosition = blocked;
            openPosition = open;
            moveSpeed = Mathf.Max(0.1f, speed);
            startsBlocked = beginBlocked;
            isBlocked = startsBlocked;
            transform.position = isBlocked ? blockedPosition : openPosition;
        }

        public void BreakFromExplosion(Vector3 origin, float force)
        {
            BreakableNavMeshObstacle breakable = GetComponent<BreakableNavMeshObstacle>();
            if (breakable == null)
                breakable = gameObject.AddComponent<BreakableNavMeshObstacle>();

            breakable.Configure(3f, 4f, 1.1f);
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.mass = 8f;
            breakable.BreakFromExplosion(origin, force);
        }
    }
}
