using System;
using UnityEngine;
using UnityEngine.Events;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    public sealed class ZombieHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private UnityEvent onDied;

        private float currentHealth;

        public event Action<float> Damaged;
        public event Action Died;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth
        {
            get => maximumHealth;
            set => maximumHealth = Mathf.Max(1f, value);
        }
        public bool IsDead { get; private set; }

        private void OnEnable()
        {
            currentHealth = maximumHealth;
            IsDead = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            float appliedDamage = Mathf.Min(currentHealth, amount);
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(appliedDamage);
            if (currentHealth > 0f)
                return;

            CompleteDeath(true);
        }

        public bool Kill(bool deactivateGameObject = true)
        {
            if (IsDead)
                return false;

            float appliedDamage = currentHealth;
            currentHealth = 0f;
            if (appliedDamage > 0f)
                Damaged?.Invoke(appliedDamage);
            CompleteDeath(deactivateGameObject);
            return true;
        }

        private void CompleteDeath(bool deactivateGameObject)
        {
            if (IsDead)
                return;

            IsDead = true;
            Died?.Invoke();
            onDied?.Invoke();
            if (deactivateGameObject)
                gameObject.SetActive(false);
        }
    }
}
