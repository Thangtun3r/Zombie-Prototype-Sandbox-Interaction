using UnityEngine;
using UnityEngine.UI;

namespace PlayerPrototype
{
    [DisallowMultipleComponent]
    public sealed class PistolHUD : MonoBehaviour
    {
        [SerializeField] private HitscanPistol pistol;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text reloadText;
        [SerializeField] private Graphic hitmarker;
        [SerializeField] private Graphic[] crosshairGraphics;
        [SerializeField] private CircularCrosshairGraphic shotgunCrosshair;
        [SerializeField, Min(0.01f)] private float hitmarkerDuration = 0.12f;
        [SerializeField] private Color normalCrosshairColor = Color.white;
        [SerializeField] private Color hitCrosshairColor = Color.red;

        private float hideHitmarkerTime;
        private Camera aimCamera;

        private void OnEnable()
        {
            if (pistol == null && Camera.main != null)
                pistol = Camera.main.GetComponent<HitscanPistol>();

            if (pistol != null)
            {
                aimCamera = pistol.GetComponent<Camera>();
                pistol.HitConfirmed += ShowHitmarker;
                pistol.WeaponChanged += UpdateCrosshairMode;
            }

            if (hitmarker != null)
                hitmarker.enabled = false;

            SetCrosshairColor(normalCrosshairColor);
            UpdateCrosshairMode();
        }

        private void OnDisable()
        {
            if (pistol != null)
            {
                pistol.HitConfirmed -= ShowHitmarker;
                pistol.WeaponChanged -= UpdateCrosshairMode;
            }
        }

        private void Update()
        {
            if (pistol == null)
                return;

            if (ammoText != null)
                ammoText.text = pistol.CurrentWeaponName + "  " +
                                pistol.CurrentAmmo.ToString("00") + " / " +
                                pistol.MagazineSize.ToString("00");

            if (reloadText != null)
                reloadText.gameObject.SetActive(pistol.IsReloading);

            UpdateShotgunSpreadRing();

            if (Time.time >= hideHitmarkerTime)
            {
                if (hitmarker != null && hitmarker.enabled)
                    hitmarker.enabled = false;
                SetCrosshairColor(normalCrosshairColor);
            }
        }

        public void Configure(
            HitscanPistol gun,
            Text ammo,
            Text reload,
            Graphic hitGraphic,
            Graphic[] crosshair)
        {
            pistol = gun;
            ammoText = ammo;
            reloadText = reload;
            hitmarker = hitGraphic;
            crosshairGraphics = crosshair;
        }

        public void ConfigureShotgunCrosshair(CircularCrosshairGraphic circularCrosshair)
        {
            shotgunCrosshair = circularCrosshair;
            UpdateCrosshairMode();
        }

        private void ShowHitmarker()
        {
            if (hitmarker != null)
            {
                hitmarker.enabled = true;
                hitmarker.color = hitCrosshairColor;
            }
            SetCrosshairColor(hitCrosshairColor);
            hideHitmarkerTime = Time.time + hitmarkerDuration;
        }

        private void UpdateCrosshairMode()
        {
            bool useShotgunRing = pistol != null &&
                                  pistol.CurrentWeapon != null &&
                                  pistol.CurrentWeapon.PelletCount > 1;

            if (crosshairGraphics != null)
            {
                foreach (Graphic graphic in crosshairGraphics)
                {
                    if (graphic != null)
                        graphic.gameObject.SetActive(!useShotgunRing);
                }
            }

            if (shotgunCrosshair != null)
                shotgunCrosshair.gameObject.SetActive(useShotgunRing);

            UpdateShotgunSpreadRing();
        }

        private void UpdateShotgunSpreadRing()
        {
            if (shotgunCrosshair == null ||
                pistol == null ||
                pistol.CurrentWeapon == null ||
                !shotgunCrosshair.gameObject.activeSelf)
            {
                return;
            }

            if (aimCamera == null)
                aimCamera = pistol.GetComponent<Camera>();
            if (aimCamera == null || aimCamera.pixelHeight <= 0)
                return;

            float halfVerticalFov = aimCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float spreadRadians = pistol.CurrentWeapon.SpreadAngle * Mathf.Deg2Rad;
            float pixelsPerTangent = aimCamera.pixelHeight * 0.5f / Mathf.Tan(halfVerticalFov);
            float radiusInPixels = Mathf.Tan(spreadRadians) * pixelsPerTangent;

            Canvas canvas = shotgunCrosshair.canvas;
            float canvasScale = canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
            float radiusInCanvasUnits = radiusInPixels / canvasScale;
            shotgunCrosshair.Radius = radiusInCanvasUnits;

            RectTransform ringRect = shotgunCrosshair.rectTransform;
            float requiredSize = (radiusInCanvasUnits + shotgunCrosshair.Thickness) * 2f;
            Vector2 size = Vector2.one * requiredSize;
            if ((ringRect.sizeDelta - size).sqrMagnitude > 0.01f)
                ringRect.sizeDelta = size;
        }

        private void SetCrosshairColor(Color color)
        {
            if (crosshairGraphics != null)
            {
                foreach (Graphic graphic in crosshairGraphics)
                {
                    if (graphic != null)
                        graphic.color = color;
                }
            }

            if (shotgunCrosshair != null)
                shotgunCrosshair.color = color;
        }
    }
}
