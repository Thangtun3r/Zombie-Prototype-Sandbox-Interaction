using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class EnvironmentalInteractableHighlight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int BaseOpacityId = Shader.PropertyToID("_BaseOpacity");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int EmissionAmountId = Shader.PropertyToID("_EmissionAmount");

        [SerializeField] private bool highlightEnabled = true;
        [SerializeField, Range(0f, 1f)] private float baseOpacity = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
        [SerializeField, Min(0f)] private float pulseSpeed = 0.8f;
        [SerializeField, Min(0f)] private float emissionAmount = 0.2f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.82f, 1f, 1f);
        [SerializeField] private Color flashColor = new Color(1f, 0.48f, 0.08f, 1f);
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private bool visibleInGame = true;
        [SerializeField] private bool visibleInEditor = true;
        [SerializeField] private Renderer[] overlayRenderers = new Renderer[0];

        private MaterialPropertyBlock propertyBlock;
        private bool runtimeHighlighted = true;

        public bool HighlightEnabled => highlightEnabled;
        public Material HighlightMaterial => highlightMaterial;
        public Renderer[] OverlayRenderers => overlayRenderers;

        private void OnEnable()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            SetOverlayVisibility(false);
        }

        private void OnValidate()
        {
            baseOpacity = Mathf.Clamp01(baseOpacity);
            pulseAmount = Mathf.Clamp(pulseAmount, 0f, 0.5f);
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
            emissionAmount = Mathf.Max(0f, emissionAmount);
            RefreshNow();
        }

        public void Configure(Renderer[] configuredOverlayRenderers, Material configuredMaterial)
        {
            overlayRenderers = configuredOverlayRenderers ?? new Renderer[0];
            highlightMaterial = configuredMaterial;
            RefreshNow();
        }

        public void SetHighlighted(bool highlighted)
        {
            runtimeHighlighted = highlighted;
            RefreshNow();
        }

        public void RefreshNow()
        {
            bool shouldShow = highlightEnabled
                && runtimeHighlighted
                && (Application.isPlaying ? visibleInGame : visibleInEditor);
            SetOverlayVisibility(shouldShow);
            if (!shouldShow)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            Color transparentColor = new Color(
                highlightColor.r,
                highlightColor.g,
                highlightColor.b,
                baseOpacity);

            foreach (Renderer overlayRenderer in overlayRenderers)
            {
                if (overlayRenderer == null)
                    continue;

                if (highlightMaterial != null && overlayRenderer.sharedMaterial != highlightMaterial)
                    overlayRenderer.sharedMaterial = highlightMaterial;

                overlayRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, transparentColor);
                propertyBlock.SetColor(ColorId, transparentColor);
                propertyBlock.SetColor(FlashColorId, flashColor);
                propertyBlock.SetFloat(BaseOpacityId, baseOpacity);
                propertyBlock.SetFloat(PulseAmountId, pulseAmount);
                propertyBlock.SetFloat(PulseSpeedId, pulseSpeed);
                propertyBlock.SetFloat(EmissionAmountId, emissionAmount);
                overlayRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void SetOverlayVisibility(bool visible)
        {
            foreach (Renderer overlayRenderer in overlayRenderers)
            {
                if (overlayRenderer != null)
                    overlayRenderer.forceRenderingOff = !visible;
            }
        }
    }
}
