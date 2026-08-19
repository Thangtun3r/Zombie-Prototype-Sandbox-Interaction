using UnityEngine;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    public sealed class ExplosionRadiusVisual : MonoBehaviour
    {
        private Material runtimeMaterial;
        private float radius;
        private float duration;
        private float elapsed;
        private Color startColor;

        public void Initialize(float blastRadius, float visualDuration, Color color)
        {
            radius = Mathf.Max(0.1f, blastRadius);
            duration = Mathf.Max(0.05f, visualDuration);
            startColor = color;

            Renderer sphereRenderer = GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            runtimeMaterial = new Material(shader)
            {
                name = "Tank Blast Radius (Runtime)",
                color = startColor,
                renderQueue = 3000
            };

            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", startColor);
            if (runtimeMaterial.HasProperty("_Surface"))
                runtimeMaterial.SetFloat("_Surface", 1f);
            if (runtimeMaterial.HasProperty("_Blend"))
                runtimeMaterial.SetFloat("_Blend", 0f);
            if (runtimeMaterial.HasProperty("_SrcBlend"))
                runtimeMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (runtimeMaterial.HasProperty("_DstBlend"))
                runtimeMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (runtimeMaterial.HasProperty("_ZWrite"))
                runtimeMaterial.SetFloat("_ZWrite", 0f);
            runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            sphereRenderer.sharedMaterial = runtimeMaterial;
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;
            transform.localScale = Vector3.one * 0.05f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.one * (radius * 2f * eased);

            Color color = startColor;
            color.a *= 1f - progress;
            if (runtimeMaterial != null)
            {
                runtimeMaterial.color = color;
                if (runtimeMaterial.HasProperty("_BaseColor"))
                    runtimeMaterial.SetColor("_BaseColor", color);
            }

            if (progress >= 1f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
        }
    }
}
