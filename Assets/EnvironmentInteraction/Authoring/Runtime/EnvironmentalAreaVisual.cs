using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class EnvironmentalAreaVisual : MonoBehaviour
    {
        [SerializeField] private Transform boxVisual;
        [SerializeField] private Transform sphereVisual;
        [SerializeField] private bool showInGame;

        public Transform BoxVisual => boxVisual;
        public Transform SphereVisual => sphereVisual;

        public void Configure(Transform configuredBoxVisual, Transform configuredSphereVisual)
        {
            boxVisual = configuredBoxVisual;
            sphereVisual = configuredSphereVisual;
            RefreshVisibility();
        }

        public void Synchronize(
            EnvironmentalAreaShape shape,
            Vector3 boxSize,
            float radius,
            Color color)
        {
            bool showBox = shape == EnvironmentalAreaShape.Box;
            SetPreviewActive(boxVisual, showBox);
            SetPreviewActive(sphereVisual, !showBox);

            SetWorldScale(boxVisual, MaxVector(boxSize, 0.05f));
            SetWorldScale(sphereVisual, Vector3.one * Mathf.Max(0.1f, radius * 2f));
            ApplyColor(boxVisual, color);
            ApplyColor(sphereVisual, color);
            RefreshVisibility();
        }

        private void OnEnable()
        {
            RefreshVisibility();
        }

        private void OnValidate()
        {
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool visible = showInGame || !Application.isPlaying;
            SetRendererVisibility(boxVisual, visible);
            SetRendererVisibility(sphereVisual, visible);
        }

        private static void SetPreviewActive(Transform preview, bool active)
        {
            if (preview != null && preview.gameObject.activeSelf != active)
                preview.gameObject.SetActive(active);
        }

        private static void SetWorldScale(Transform preview, Vector3 desiredWorldScale)
        {
            if (preview == null)
                return;

            Transform parent = preview.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            preview.localScale = new Vector3(
                DivideScale(desiredWorldScale.x, parentScale.x),
                DivideScale(desiredWorldScale.y, parentScale.y),
                DivideScale(desiredWorldScale.z, parentScale.z));
        }

        private static float DivideScale(float desired, float parentScale)
        {
            float divisor = Mathf.Max(0.0001f, Mathf.Abs(parentScale));
            return desired / divisor;
        }

        private static void ApplyColor(Transform preview, Color color)
        {
            if (preview == null)
                return;
            EnvironmentalVisualUtility.ApplyColor(preview.GetComponent<Renderer>(), color);
        }

        private static void SetRendererVisibility(Transform preview, bool visible)
        {
            if (preview == null)
                return;

            Renderer renderer = preview.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled != visible)
                renderer.enabled = visible;
        }

        private static Vector3 MaxVector(Vector3 value, float minimum)
        {
            return new Vector3(
                Mathf.Max(minimum, Mathf.Abs(value.x)),
                Mathf.Max(minimum, Mathf.Abs(value.y)),
                Mathf.Max(minimum, Mathf.Abs(value.z)));
        }
    }
}
