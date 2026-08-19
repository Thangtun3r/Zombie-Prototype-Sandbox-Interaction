using UnityEngine;
using UnityEngine.Events;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ZombieTarget))]
    public sealed class ZombieAttack : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float attackRange = 1.5f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
        [SerializeField] private UnityEvent onAttack;

        private ZombieTarget targetSource;
        private float nextAttackTime;

        public float Damage
        {
            get => damage;
            set => damage = Mathf.Max(0f, value);
        }

        public float AttackCooldown
        {
            get => attackCooldown;
            set => attackCooldown = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            targetSource = GetComponent<ZombieTarget>();
        }

        private void Update()
        {
            Transform target = targetSource.Current;
            if (target == null || Time.time < nextAttackTime)
                return;

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > attackRange * attackRange)
                return;

            nextAttackTime = Time.time + attackCooldown;
            onAttack?.Invoke();

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
        }
    }
}
