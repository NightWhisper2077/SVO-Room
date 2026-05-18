using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Flashlight : MonoBehaviour
{
    public Light flashlightLight;
    public InputActionReference toggleButton;

    private XRGrabInteractable grab;
    private bool isOn;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        toggleButton.action.performed += Toggle;
        toggleButton.action.Enable();
    }

    void OnDisable()
    {
        toggleButton.action.performed -= Toggle;
        toggleButton.action.Disable();
    }

    void Toggle(InputAction.CallbackContext context)
    {
        if (!grab.isSelected) return;

        isOn = !isOn;
        flashlightLight.enabled = isOn;
    }
}