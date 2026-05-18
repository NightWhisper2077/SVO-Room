using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FinalProject
{
    [DefaultExecutionOrder(10001)]
    public sealed class DesktopHandGrabController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float grabDistance = 4f;
        [SerializeField] private float grabRadius = 0.18f;
        [SerializeField] private float handSideOffset = 0.28f;
        [SerializeField] private float handHeightOffset = -0.18f;
        [SerializeField] private float holdDistance = 0.85f;
        [SerializeField] private float minHoldDistance = 0.45f;
        [SerializeField] private float maxHoldDistance = 2.4f;
        [SerializeField] private float scrollSensitivity = 0.0015f;

        private Transform handAnchor;
        private Renderer handRenderer;
        private Rigidbody grabbedRigidbody;
        private Transform grabbedTransform;
        private bool previousUseGravity;
        private bool previousIsKinematic;
        private bool useRightHand = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (SceneManager.GetActiveScene().name != "Start Scene")
                return;

            if (FindFirstObjectByType<DesktopHandGrabController>() != null)
                return;

            var origin = GameObject.Find("XR Origin (VR)");
            if (origin == null && Camera.main != null)
                origin = Camera.main.transform.root.gameObject;

            if (origin == null)
                return;

            var controller = origin.AddComponent<DesktopHandGrabController>();
            if (Camera.main != null)
                controller.cameraTransform = Camera.main.transform;
        }

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            CreateHandAnchor();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.zKey.wasPressedThisFrame)
                useRightHand = false;
            if (keyboard.xKey.wasPressedThisFrame)
                useRightHand = true;

            UpdateHoldDistance();
            UpdateHandPose();

            if (keyboard.gKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
                ToggleGrab();

            if (keyboard.tKey.wasPressedThisFrame)
                ActivateHeldObject();

            UpdateHeldObjectPose();
        }

        private void CreateHandAnchor()
        {
            var anchorObject = new GameObject("Desktop Grab Hand Anchor");
            handAnchor = anchorObject.transform;

            if (cameraTransform != null)
                handAnchor.SetParent(cameraTransform, false);
            else
                handAnchor.SetParent(transform, false);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Desktop Grab Hand Visual";
            visual.transform.SetParent(handAnchor, false);
            visual.transform.localScale = Vector3.one * 0.12f;

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            handRenderer = visual.GetComponent<Renderer>();
            if (handRenderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = new Color(1f, 0.55f, 0.08f, 0.95f);
                handRenderer.sharedMaterial = material;
            }
        }

        private void UpdateHoldDistance()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f)
                holdDistance = Mathf.Clamp(holdDistance + scroll * scrollSensitivity, minHoldDistance, maxHoldDistance);
        }

        private void UpdateHandPose()
        {
            if (handAnchor == null)
                return;

            var side = useRightHand ? handSideOffset : -handSideOffset;
            handAnchor.localPosition = new Vector3(side, handHeightOffset, holdDistance);
            handAnchor.localRotation = Quaternion.identity;

            if (handRenderer != null)
                handRenderer.material.color = useRightHand ? new Color(1f, 0.55f, 0.08f, 0.95f) : new Color(0.1f, 0.75f, 1f, 0.95f);
        }

        private void ToggleGrab()
        {
            if (grabbedTransform != null)
            {
                Release();
                return;
            }

            TryGrab();
        }

        private void TryGrab()
        {
            if (!TryFindGrabbable(out var target, out var targetRigidbody))
                return;

            grabbedTransform = target;
            grabbedRigidbody = targetRigidbody;

            if (grabbedRigidbody != null)
            {
                previousUseGravity = grabbedRigidbody.useGravity;
                previousIsKinematic = grabbedRigidbody.isKinematic;
                grabbedRigidbody.useGravity = false;
                grabbedRigidbody.isKinematic = true;
            }

            UpdateHeldObjectPose();
        }

        private bool TryFindGrabbable(out Transform target, out Rigidbody targetRigidbody)
        {
            target = null;
            targetRigidbody = null;

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.SphereCast(ray, grabRadius, out var hit, grabDistance, ~0, QueryTriggerInteraction.Ignore))
                return false;

            if (hit.transform.IsChildOf(transform))
                return false;

            var interactable = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (interactable != null)
            {
                target = interactable.transform;
                targetRigidbody = interactable.GetComponent<Rigidbody>();
                return true;
            }

            targetRigidbody = hit.collider.attachedRigidbody ?? hit.collider.GetComponentInParent<Rigidbody>();
            if (targetRigidbody != null && !targetRigidbody.transform.IsChildOf(transform))
            {
                target = targetRigidbody.transform;
                return true;
            }

            return false;
        }

        private void UpdateHeldObjectPose()
        {
            if (grabbedTransform == null || handAnchor == null)
                return;

            grabbedTransform.position = handAnchor.position;
            grabbedTransform.rotation = handAnchor.rotation;
        }

        private void ActivateHeldObject()
        {
            if (grabbedTransform == null)
                return;

            grabbedTransform.SendMessage("PlayVideo", SendMessageOptions.DontRequireReceiver);
        }

        private void Release()
        {
            if (grabbedRigidbody != null)
            {
                grabbedRigidbody.useGravity = previousUseGravity;
                grabbedRigidbody.isKinematic = previousIsKinematic;
            }

            grabbedRigidbody = null;
            grabbedTransform = null;
        }
    }
}
