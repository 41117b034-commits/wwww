using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;
using CommonUsages = UnityEngine.XR.CommonUsages;

// Metre-scale player shared by keyboard/mouse and tracked XR devices.
public sealed class Chapter2Player : MonoBehaviour
{
    public Camera view;
    public bool canMove;
    public bool canLook = true;
    public float speed = 2.6f;
    public Chapter2RouteGuide routeGuide;
    public Chapter2WorldBoundary worldBoundary;
    public bool IsVR { get; private set; }
    public bool PrimaryPressed { get; private set; }
    public bool SecondaryPressed { get; private set; }
    public bool ActionPressed { get; private set; }
    public bool HandRaised { get; private set; }
    CharacterController motor;
    float pitch, gravity, nextSnap;
    bool previousTrigger, previousPrimary, previousSecondary;
    bool conversationView;
    Vector3 beforeChatPosition, beforeChatViewPosition;
    Quaternion beforeChatRotation, beforeChatViewRotation;
    float beforeChatPitch;
    readonly List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    public static bool Key(KeyControlName k)
    {
        var b = Keyboard.current;
        if (b == null) return false;
        switch(k) { case KeyControlName.One: return b.digit1Key.wasPressedThisFrame; case KeyControlName.Two: return b.digit2Key.wasPressedThisFrame; case KeyControlName.E: return b.eKey.wasPressedThisFrame; case KeyControlName.P: return b.pKey.wasPressedThisFrame; default: return b.spaceKey.wasPressedThisFrame; }
    }
    public enum KeyControlName { One, Two, E, Space, P }
    void Awake() { motor = GetComponent<CharacterController>(); }
    void Start()
    {
        if (!worldBoundary) worldBoundary = GetComponent<Chapter2WorldBoundary>();
        if (!worldBoundary) worldBoundary = gameObject.AddComponent<Chapter2WorldBoundary>();
        worldBoundary.Initialize(this);
    }
    void Update()
    {
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
        IsVR = devices.Count > 0 && devices[0].isValid;
        if (IsVR)
        {
            if (devices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)) view.transform.localPosition = p + Vector3.up * 0.05f;
            if (devices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q)) view.transform.localRotation = q;
        }
        var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        right.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
        right.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
        left.TryGetFeatureValue(CommonUsages.primaryButton, out bool secondary);
        PrimaryPressed = Key(KeyControlName.One) || (primary && !previousPrimary);
        SecondaryPressed = Key(KeyControlName.Two) || (secondary && !previousSecondary);
        ActionPressed = Key(KeyControlName.E) || Key(KeyControlName.Space) || (trigger && !previousTrigger);
        previousTrigger = trigger; previousPrimary = primary; previousSecondary = secondary;
        HandRaised = IsVR && right.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 hand) && hand.y > view.transform.localPosition.y + 0.1f;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (!IsVR && canLook && Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            var d = Mouse.current.delta.ReadValue() * 0.08f;
            transform.Rotate(0, d.x, 0); pitch = Mathf.Clamp(pitch - d.y, -75, 75);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (!canMove || motor == null || !motor.enabled) return;
        Vector2 move = Vector2.zero;
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            move = new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0), (k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
        }
        if (IsVR && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) move += axis;
        if (IsVR && right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 turn) && Mathf.Abs(turn.x) > 0.75f && Time.unscaledTime > nextSnap)
        { transform.Rotate(0, Mathf.Sign(turn.x) * 30, 0); nextSnap = Time.unscaledTime + 0.35f; }
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        Vector3 velocity = (forward * move.y + Vector3.Cross(Vector3.up, forward) * move.x);
        velocity = Vector3.ClampMagnitude(velocity, 1) * speed;
        gravity = motor.isGrounded ? -1 : Mathf.Max(-15, gravity - 15 * Time.deltaTime);
        velocity.y = gravity; Move(velocity * Time.deltaTime);
    }
    public void Move(Vector3 motion)
    {
        if(!motor||!motor.enabled||!canMove)return;
        Vector3 next=transform.position+motion;
        if(routeGuide)next=routeGuide.Constrain(transform.position,next);
        if(worldBoundary && worldBoundary.enabled)next=worldBoundary.Constrain(transform.position,next);
        motor.Move(next-transform.position);
    }
    public void RestoreGroundPosition(Vector3 position)
    {
        if (motor) motor.enabled = false;
        transform.position = position; gravity = 0;
        if (motor) motor.enabled = true;
    }
    public void Warp(Vector3 position, Vector3 lookAt)
    {
        if (motor) motor.enabled = false;
        transform.position = position;
        Vector3 forward = Vector3.ProjectOnPlane(lookAt-position, Vector3.up);
        if (forward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(forward);
        if (!IsVR) { pitch = 0; view.transform.localPosition = new Vector3(0,1.65f,0); view.transform.localRotation = Quaternion.identity; }
        gravity = 0;
        if (motor) motor.enabled = true;
    }
    public void FocusOn(Vector3 target)
    {
        if(IsVR)return;
        Vector3 direction=target-view.transform.position;
        transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up));
        pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
        view.transform.localRotation=Quaternion.Euler(pitch,0,0);
    }

    // A temporary conversation shot. Keep the exploration position and restore it
    // on exit; opening a chat must not permanently move either person.
    public void BeginConversationView(Transform person, float eyeHeight)
    {
        EndConversationView();
        beforeChatPosition=transform.position; beforeChatRotation=transform.rotation;
        beforeChatViewPosition=view.transform.localPosition; beforeChatViewRotation=view.transform.localRotation;
        beforeChatPitch=pitch; conversationView=true;
        Vector3 away=Vector3.ProjectOnPlane(view.transform.position-person.position,Vector3.up).normalized;
        if(away.sqrMagnitude<.01f)away=-Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
        Vector3 eyes=person.position+Vector3.up*eyeHeight;
        Vector3 shot=eyes+away*2.35f;
        // Keep a consistent distance, selecting another side if a tree blocks it.
        foreach(float angle in new[]{0f,30f,-30f,60f,-60f,90f,-90f,180f})
        {
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*away;
            Vector3 candidate=eyes+direction*2.35f;
            bool blocked=false;
            foreach(var hit in Physics.SphereCastAll(eyes+direction*.45f,.16f,direction,1.9f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(person)&&!hit.transform.IsChildOf(transform)){blocked=true;break;}
            if(blocked)continue;
            shot=candidate;break;
        }
        if(IsVR)
        {
            // Move the rig origin, while preserving the tracked head orientation.
            RestoreGroundPosition(transform.position+Vector3.ProjectOnPlane(shot-view.transform.position,Vector3.up));
        }
        else
        {
            view.transform.position=shot;
            view.transform.rotation=Quaternion.LookRotation(eyes-Vector3.up*.40f-shot);
        }
    }
    public void EndConversationView()
    {
        if(!conversationView)return;
        conversationView=false;
        RestoreGroundPosition(beforeChatPosition);transform.rotation=beforeChatRotation;
        view.transform.localPosition=beforeChatViewPosition;view.transform.localRotation=beforeChatViewRotation;
        pitch=beforeChatPitch;
    }
}
