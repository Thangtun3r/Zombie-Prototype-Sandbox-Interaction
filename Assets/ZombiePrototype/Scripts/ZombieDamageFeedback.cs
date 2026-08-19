using System.Collections;
using UnityEngine;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ZombieHealth))]
    public sealed class ZombieDamageFeedback : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private ZombieHealth health;
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine flashRoutine;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (health != null)
                health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Damaged -= HandleDamaged;
            ClearFlash();
        }

        private void HandleDamaged(float amount)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetFlash(flashColor);
            yield return new WaitForSeconds(flashDuration);
            ClearFlash();
            flashRoutine = null;
        }

        private void SetFlash(Color color)
        {
            EnsureInitialized();
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                    continue;
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        private void EnsureInitialized()
        {
            if (health == null)
                health = GetComponent<ZombieHealth>();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }

        private void ClearFlash()
        {
            if (renderers == null)
                return;

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer != null)
                    targetRenderer.SetPropertyBlock(null);
            }
        }
    }
}
