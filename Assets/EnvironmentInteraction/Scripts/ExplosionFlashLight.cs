using UnityEngine;

namespace EnvironmentInteraction
{
    [DisallowMultipleComponent]
    public sealed class ExplosionFlashLight : MonoBehaviour
    {
        private Light flash;
        private float duration;
        private float startIntensity;
        private float elapsed;

        public void Initialize(Light target, float lifetime)
        {
            flash = target;
            duration = Mathf.Max(0.02f, lifetime);
            startIntensity = flash != null ? flash.intensity : 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (flash != null)
                flash.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
