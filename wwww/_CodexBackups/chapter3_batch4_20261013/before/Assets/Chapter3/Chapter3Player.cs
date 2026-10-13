using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

// Chapter 3 has its own walkable corridor, without Chapter 2's logging boundary.
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-100)]
public sealed class Chapter3Player : MonoBehaviour
{
    public Camera view;
    public Chapter3Controller chapter;
    public Chapter3StationController station;
    public Chapter3Combat combat;
    public Chapter3SchoolController school;
    public bool canMove, canLook;
    public float speed = 2.8f;
    public bool IsVR { get; private set; }
    public bool ActionPressed { get; private set; }
    CharacterController motor;
    float pitch, gravity, nextSnap;
    bool previousTrigger;
    void Awake() { motor = GetComponent<CharacterController>(); }
    void Update()
    {
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        IsVR = head.isValid;
        if (IsVR)
        {
            if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)) view.transform.localPosition = p;
            if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q)) view.transform.localRotation = q;
        }
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        right.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
        ActionPressed = Keyboard.current?.eKey.wasPressedThisFrame == true || (trigger && !previousTrigger);
        previousTrigger = trigger;
        if (!IsVR && canLook && (Mouse.current?.rightButton.isPressed == true || (combat && Cursor.lockState==CursorLockMode.Locked)))
        {
            var d = Mouse.current.delta.ReadValue() * .08f;
            transform.Rotate(0, d.x, 0); pitch = Mathf.Clamp(pitch - d.y, -70, 70);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (!canMove) return;
        Vector2 move = Vector2.zero;
        var k = Keyboard.current;
        if (k != null) move = new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0), (k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
        if (IsVR && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) move += axis;
        if (IsVR && right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 turn) && Mathf.Abs(turn.x) > .75f && Time.unscaledTime > nextSnap)
        { transform.Rotate(0, Mathf.Sign(turn.x) * 30, 0); nextSnap = Time.unscaledTime + .35f; }
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        Vector3 velocity = Vector3.ClampMagnitude(forward * move.y + Vector3.Cross(Vector3.up, forward) * move.x, 1) * speed;
        gravity = motor.isGrounded ? -1 : Mathf.Max(-15, gravity - 15 * Time.deltaTime);
        velocity.y = gravity;
        Move(velocity * Time.deltaTime);
    }
    public void Move(Vector3 motion)
    {
        if (!canMove || !motor || !motor.enabled) return;
        Vector3 next = transform.position + motion;
        if (chapter) next = chapter.Constrain(transform.position, next);
        else if (station) next = station.Constrain(transform.position, next);
        else if (combat) next = combat.Constrain(transform.position, next);
        else if (school) next = school.Constrain(transform.position, next);
        motor.Move(next - transform.position);
        if (transform.position.y < -3) Warp(school?school.spawn:combat?combat.spawn:new Vector3(0,.1f,0),school?school.spawn+Vector3.forward*12:Vector3.forward*3);
    }
    public void Warp(Vector3 position, Vector3 lookAt)
    {
        if (!motor) motor = GetComponent<CharacterController>();
        motor.enabled = false; transform.position = position; gravity = 0; motor.enabled = true;
        FocusOn(lookAt);
    }
    public void FocusOn(Vector3 position)
    {
        if (IsVR) return;
        Vector3 direction = position - view.transform.position;
        if (Vector3.ProjectOnPlane(direction, Vector3.up).sqrMagnitude < .001f) return;
        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(direction, Vector3.up));
        pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
        view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}
