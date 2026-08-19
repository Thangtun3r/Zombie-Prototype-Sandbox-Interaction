using UnityEngine;

namespace ZombiePrototype
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ZombieHealth), typeof(ZombieMovement), typeof(ZombieAttack))]
    public sealed class ZombieArchetypeBinding : MonoBehaviour
    {
        [SerializeField] private ZombieArchetype archetype;

        public ZombieArchetype Archetype
        {
            get => archetype;
            set
            {
                archetype = value;
                ApplyArchetype();
            }
        }

        private void Awake()
        {
            ApplyArchetype();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyArchetype();
        }

        public void ApplyArchetype()
        {
            if (archetype == null)
                return;

            ZombieHealth health = GetComponent<ZombieHealth>();
            ZombieMovement movement = GetComponent<ZombieMovement>();
            ZombieAttack attack = GetComponent<ZombieAttack>();
            Rigidbody body = GetComponent<Rigidbody>();
            ZombieDeathExplosion explosion = GetComponent<ZombieDeathExplosion>();

            health.MaximumHealth = archetype.Health;
            movement.MoveSpeed = archetype.MoveSpeed;
            movement.KnockbackDuration = archetype.KnockbackDuration;
            attack.Damage = archetype.AttackDamage;
            attack.AttackCooldown = archetype.AttackCooldown;

            if (body != null)
                body.mass = archetype.BodyMass;

            if (explosion != null)
            {
                explosion.Configure(archetype);
                explosion.enabled = archetype.ExplodesOnDeath;
            }
        }
    }
}
