using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRUsages = UnityEngine.XR.CommonUsages;

// A self-contained walking rig. No chapter progress or dialogue is stored here.
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-100)]
public sealed class Chapter4Player : MonoBehaviour
{
    public const float WalkSpeed = 2.4f;
    public Camera view;
    public float speed = WalkSpeed;
    public bool canMove = true;
    public bool canLook = true;
    public float mouseSensitivity = .08f;
    public bool IsVR { get; private set; }
    public bool MenuPressed { get; private set; }
    public bool SubmitPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool RetryPressed { get; private set; }
    public bool ContactPressed { get; private set; }
    public bool IsGrounded => motor && motor.enabled && motor.isGrounded;
    public CharacterController Motor => motor;

    CharacterController motor;
    Vector3 desktopEyePosition;
    float pitch, verticalSpeed, nextSnapTime, collapseWeight;
    bool focused = true, previousMenu, previousTrigger, previousLeftTrigger, floorTracking;
    int ignoreLookThroughFrame;
    readonly List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();

    void Awake()
    {
        motor = GetComponent<CharacterController>();
        if (!view) view = GetComponentInChildren<Camera>();
        if (view)
        {
            desktopEyePosition = view.transform.localPosition;
            pitch = Mathf.DeltaAngle(0, view.transform.localEulerAngles.x);
        }
        focused = Application.isFocused;
        ReleasePointer();
    }

    void Update()
    {
        var head = XRInputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool wasVR = IsVR;
        IsVR = head.isValid;
        if (IsVR != wasVR)
        {
            ReleasePointer();
            floorTracking = false;
            xrSubsystems.Clear();
            SubsystemManager.GetSubsystems(xrSubsystems);
            foreach (var subsystem in xrSubsystems)
                if (subsystem.running && subsystem.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor)
                    floorTracking = true;
            if (!IsVR && view)
            {
                view.transform.localPosition = desktopEyePosition;
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
        }

        // Tracking remains active while the menu is open; never aim the headset
        // at a target or apply a scripted camera rotation.
        if (IsVR && view)
        {
            if (head.TryGetFeatureValue(XRUsages.devicePosition, out Vector3 position))
                view.transform.localPosition = position + (floorTracking ? Vector3.zero : desktopEyePosition);
            if (head.TryGetFeatureValue(XRUsages.deviceRotation, out Quaternion rotation))
                view.transform.localRotation = rotation;
        }
        else if (view)
            view.transform.localPosition = desktopEyePosition - Vector3.up * (desktopEyePosition.y * .62f * collapseWeight);

        var left = XRInputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = XRInputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        left.TryGetFeatureValue(XRUsages.menuButton, out bool leftMenu);
        right.TryGetFeatureValue(XRUsages.menuButton, out bool rightMenu);
        right.TryGetFeatureValue(XRUsages.triggerButton, out bool trigger);
        left.TryGetFeatureValue(XRUsages.triggerButton, out bool leftTrigger);
        bool menu = leftMenu || rightMenu;
        MenuPressed = focused && (Keyboard.current?.escapeKey.wasPressedThisFrame == true || (menu && !previousMenu));
        SubmitPressed = focused && trigger && !previousTrigger;
        InteractPressed = focused && (Keyboard.current?.eKey.wasPressedThisFrame == true || SubmitPressed);
        RetryPressed = focused && Keyboard.current?.rKey.wasPressedThisFrame == true;
        ContactPressed = focused && (Keyboard.current?.fKey.wasPressedThisFrame == true || leftTrigger && !previousLeftTrigger);
        previousLeftTrigger = leftTrigger;
        previousMenu = menu;
        previousTrigger = trigger;
        if (!focused || !view) return;

        // Escape releases the pointer before the controller opens its menu.
        if (MenuPressed) ReleasePointer();
        if (!canMove || MenuPressed) return;

        var mouse = Mouse.current;
        bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        if (!IsVR && canLook && mouse != null && !overUI)
        {
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ignoreLookThroughFrame = Time.frameCount + 1;
            }
            if (mouse.rightButton.wasPressedThisFrame)
                ignoreLookThroughFrame = Time.frameCount;
            if (Time.frameCount > ignoreLookThroughFrame &&
                (Cursor.lockState == CursorLockMode.Locked || mouse.rightButton.isPressed))
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0, delta.x, 0);
                pitch = Mathf.Clamp(pitch - delta.y, -75, 75);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
        }

        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
            input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        if (IsVR && left.TryGetFeatureValue(XRUsages.primary2DAxis, out Vector2 stick))
            input += stick.sqrMagnitude > .04f ? stick : Vector2.zero;
        if (IsVR && canLook && right.TryGetFeatureValue(XRUsages.primary2DAxis, out Vector2 turn) &&
            Mathf.Abs(turn.x) > .75f && Time.unscaledTime >= nextSnapTime)
        {
            transform.Rotate(0, Mathf.Sign(turn.x) * 30, 0);
            nextSnapTime = Time.unscaledTime + .35f;
        }

        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = transform.forward;
        Vector3 velocity = Vector3.ClampMagnitude(forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x, 1) * speed;
        verticalSpeed = IsGrounded ? -2 : Mathf.Max(-25, verticalSpeed - 20 * Time.deltaTime);
        velocity.y = verticalSpeed;
        Move(velocity * Time.deltaTime);
    }

    public void Move(Vector3 displacement)
    {
        if (canMove && motor && motor.enabled) motor.Move(displacement);
    }

    public void SetInputEnabled(bool enabled)
    {
        canMove = canLook = enabled;
        ignoreLookThroughFrame = Time.frameCount + 1;
        if (!enabled) ReleasePointer();
    }

    // Only the desktop eye lowers. XR tracking always owns the headset pose.
    public void SetCollapseVisual(float weight) { collapseWeight = Mathf.Clamp01(weight); }

    public void WarpTo(Transform destination, bool alignDesktopView = true)
    {
        if (!destination) return;
        if (!motor) motor = GetComponent<CharacterController>();
        bool wasEnabled = motor.enabled;
        motor.enabled = false;
        transform.position = destination.position;
        verticalSpeed = 0;
        // A headset can already be connected before the first Update.
        bool headsetConnected = XRInputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;
        if (alignDesktopView && !headsetConnected)
        {
            transform.rotation = Quaternion.Euler(0, destination.eulerAngles.y, 0);
            pitch = 0;
            if (view) view.transform.localRotation = Quaternion.identity;
        }
        motor.enabled = wasEnabled;
        ignoreLookThroughFrame = Time.frameCount + 1;
    }

    public void ReleasePointer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ignoreLookThroughFrame = Time.frameCount + 1;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        ReleasePointer();
    }

    void OnDisable() { ReleasePointer(); }
}
