using UnityEngine;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class EnvironmentalTemporarySlow : MonoBehaviour
    {
        private ZombieMovement movement;
        private float originalSpeed;
        private float restoreTime;
        private bool applied;

        public static void ApplyTo(ZombieMovement target, float multiplier, float duration)
        {
            if (target == null || duration <= 0f)
                return;

            EnvironmentalTemporarySlow slow = target.GetComponent<EnvironmentalTemporarySlow>();
            if (slow == null)
                slow = target.gameObject.AddComponent<EnvironmentalTemporarySlow>();
            slow.Apply(target, multiplier, duration);
        }

        private void Update()
        {
            if (applied && Time.time >= restoreTime)
            {
                Restore();
                Destroy(this);
            }
        }

        private void OnDisable()
        {
            Restore();
        }

        private void Apply(ZombieMovement target, float multiplier, float duration)
        {
            if (!applied)
            {
                movement = target;
                originalSpeed = target.MoveSpeed;
                applied = true;
            }

            float clampedMultiplier = Mathf.Clamp01(multiplier);
            movement.MoveSpeed = Mathf.Min(movement.MoveSpeed, originalSpeed * clampedMultiplier);
            if (clampedMultiplier <= 0f)
                movement.StopImmediately();
            restoreTime = Mathf.Max(restoreTime, Time.time + duration);
        }

        private void Restore()
        {
            if (!applied)
                return;
            if (movement != null)
                movement.MoveSpeed = originalSpeed;
            applied = false;
        }
    }
}
