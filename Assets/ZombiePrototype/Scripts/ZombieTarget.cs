using UnityEngine;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    public sealed class ZombieTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool findMainCamera = true;
        [SerializeField] private bool findPlayerByTag = true;
        [SerializeField, Min(0.1f)] private float searchInterval = 1f;

        private float nextSearchTime;

        public Transform Current => target;

        private void Awake()
        {
            TryFindTarget();
        }

        private void Update()
        {
            if (target != null || !findPlayerByTag || Time.time < nextSearchTime)
                return;

            TryFindTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void TryFindTarget()
        {
            nextSearchTime = Time.time + searchInterval;

            if (findMainCamera && Camera.main != null)
            {
                target = Camera.main.transform;
                return;
            }

            if (!findPlayerByTag)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }
    }
}
