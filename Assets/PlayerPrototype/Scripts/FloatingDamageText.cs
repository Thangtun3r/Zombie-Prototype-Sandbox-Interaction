using UnityEngine;

namespace PlayerPrototype
{
    [DisallowMultipleComponent]
    public sealed class FloatingDamageText : MonoBehaviour
    {
        private TextMesh mainText;
        private TextMesh shadowText;
        private Transform viewCamera;
        private Color mainColor;
        private Color shadowColor;
        private float duration;
        private float riseSpeed;
        private float elapsed;
        private bool critical;

        public static void Spawn(Vector3 position, float damage, bool isCritical)
        {
            GameObject root = new GameObject(isCritical ? "Critical Damage Text" : "Damage Text");
            root.transform.position = position + Vector3.up * 0.22f +
                                      new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);

            FloatingDamageText floatingText = root.AddComponent<FloatingDamageText>();
            floatingText.Initialize(damage, isCritical);
        }

        private void Initialize(float damage, bool isCritical)
        {
            critical = isCritical;
            duration = critical ? 1.05f : 0.82f;
            riseSpeed = critical ? 1.15f : 0.82f;
            viewCamera = Camera.main != null ? Camera.main.transform : null;

            string label = critical
                ? "CRIT! " + Mathf.RoundToInt(damage)
                : Mathf.RoundToInt(damage).ToString();
            mainColor = critical
                ? new Color(1f, 0.68f, 0.06f, 1f)
                : Color.white;
            shadowColor = new Color(0.08f, 0.01f, 0.01f, 0.78f);

            shadowText = CreateText(
                "Shadow",
                label,
                shadowColor,
                critical ? 0.045f : 0.034f,
                99);
            shadowText.transform.localPosition = new Vector3(0.018f, -0.018f, 0.012f);

            mainText = CreateText(
                "Value",
                label,
                mainColor,
                critical ? 0.045f : 0.034f,
                100);
            mainText.transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one * (critical ? 0.72f : 0.86f);
        }

        private TextMesh CreateText(string objectName, string value, Color color, float characterSize, int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = value;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;

            MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = sortingOrder;
            return textMesh;
        }

        private void LateUpdate()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            if (viewCamera == null && Camera.main != null)
                viewCamera = Camera.main.transform;
            if (viewCamera != null)
            {
                Vector3 awayFromCamera = transform.position - viewCamera.position;
                if (awayFromCamera.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
            }

            float popScale = critical
                ? Mathf.Lerp(0.72f, 1.12f, Mathf.Clamp01(progress * 5f))
                : Mathf.Lerp(0.86f, 1f, Mathf.Clamp01(progress * 6f));
            if (progress > 0.35f)
                popScale = Mathf.Lerp(popScale, 0.94f, (progress - 0.35f) / 0.65f);
            transform.localScale = Vector3.one * popScale;

            float fade = 1f - Mathf.InverseLerp(0.55f, 1f, progress);
            Color fadedMain = mainColor;
            fadedMain.a *= fade;
            mainText.color = fadedMain;

            Color fadedShadow = shadowColor;
            fadedShadow.a *= fade;
            shadowText.color = fadedShadow;

            if (progress >= 1f)
                Destroy(gameObject);
        }
    }
}
