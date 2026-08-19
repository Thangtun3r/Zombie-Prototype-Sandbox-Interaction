using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerPrototype
{
    [DisallowMultipleComponent]
    public sealed class StationaryFirstPersonLook : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float verticalLookLimit = 80f;
        [SerializeField] private bool lockCursorOnEnable = true;

        private float yaw;
        private float pitch;

        private void OnEnable()
        {
            Vector3 startingAngles = transform.localEulerAngles;
            yaw = startingAngles.y;
            pitch = NormalizeAngle(startingAngles.x);

            if (lockCursorOnEnable)
                LockCursor();
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                UnlockCursor();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor();

            if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch = Mathf.Clamp(
                pitch - mouseDelta.y * mouseSensitivity,
                -verticalLookLimit,
                verticalLookLimit);

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
