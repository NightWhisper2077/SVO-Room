using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FinalProject
{
    [DefaultExecutionOrder(10000)]
    public sealed class DesktopFallbackController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private float verticalSpeed = 1.4f;
        [SerializeField] private float mouseLookSensitivity = 0.12f;
        [SerializeField] private float keyboardLookSpeed = 100f;

        private float pitch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (SceneManager.GetActiveScene().name != "Start Scene")
                return;

            if (FindFirstObjectByType<DesktopFallbackController>() != null)
                return;

            var origin = GameObject.Find("XR Origin (VR)");
            if (origin == null && Camera.main != null)
                origin = Camera.main.transform.root.gameObject;

            if (origin == null)
                return;

            var controller = origin.AddComponent<DesktopFallbackController>();
            if (Camera.main != null)
                controller.cameraTransform = Camera.main.transform;
        }

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
                pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
        }

        private void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || cameraTransform == null)
                return;

            UpdateLook(keyboard);
            UpdateMovement(keyboard);
        }

        private void UpdateLook(Keyboard keyboard)
        {
            var deltaTime = Time.unscaledDeltaTime;
            var yawDelta = 0f;
            var pitchDelta = 0f;

            if (keyboard.leftArrowKey.isPressed)
                yawDelta -= keyboardLookSpeed * deltaTime;
            if (keyboard.rightArrowKey.isPressed)
                yawDelta += keyboardLookSpeed * deltaTime;
            if (keyboard.upArrowKey.isPressed)
                pitchDelta -= keyboardLookSpeed * deltaTime;
            if (keyboard.downArrowKey.isPressed)
                pitchDelta += keyboardLookSpeed * deltaTime;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var mouseDelta = mouse.delta.ReadValue();
                yawDelta += mouseDelta.x * mouseLookSensitivity;
                pitchDelta -= mouseDelta.y * mouseLookSensitivity;
            }

            if (Mathf.Abs(yawDelta) > 0.001f)
                transform.Rotate(0f, yawDelta, 0f, Space.World);

            if (Mathf.Abs(pitchDelta) > 0.001f)
            {
                pitch = Mathf.Clamp(pitch + pitchDelta, -70f, 70f);
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void UpdateMovement(Keyboard keyboard)
        {
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var direction = Vector3.zero;

            if (keyboard.wKey.isPressed)
                direction += forward;
            if (keyboard.sKey.isPressed)
                direction -= forward;
            if (keyboard.dKey.isPressed)
                direction += right;
            if (keyboard.aKey.isPressed)
                direction -= right;
            if (keyboard.eKey.isPressed)
                direction += Vector3.up * verticalSpeed;
            if (keyboard.qKey.isPressed)
                direction -= Vector3.up * verticalSpeed;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            var speed = moveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                speed *= sprintMultiplier;

            transform.position += direction.normalized * (speed * Time.unscaledDeltaTime);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
