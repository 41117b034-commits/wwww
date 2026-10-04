using UnityEngine;

[DefaultExecutionOrder(4200)]
public sealed class Chapter2Actor : MonoBehaviour
{
    public bool police;
    public bool seated;
    public bool fallen;
    public bool speaking;
    public Vector3 facing = Vector3.back;
    public Chapter1IncidentRig Rig { get; private set; }
    Vector3 previous;
    Transform leftThigh,rightThigh,leftKnee,rightKnee,leftFoot,rightFoot;
    void Start()
    {
        var animator = GetComponentInChildren<Animator>();
        Rig = gameObject.AddComponent<Chapter1IncidentRig>();
        Rig.Initialize(animator, police);
        Rig.batonVisible = false;
        Rig.Face(facing); Rig.Ground(0);
        leftThigh=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        rightThigh=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        leftKnee=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        rightKnee=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        leftFoot=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        rightFoot=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        previous = transform.position;
    }
    void Update()
    {
        if (!Rig) return;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position-previous,Vector3.up);
        Rig.walking = delta.magnitude > 0.0005f;
        if (Rig.walking) Rig.Face(delta);
        Rig.speakingWeight = speaking ? 0.65f : 0;
        if (!fallen) Rig.Ground(0);
        previous = transform.position;
    }
    void LateUpdate()
    {
        if (!Rig || !Rig.Hips) return;
        if (seated && leftFoot && rightFoot)
        {
            Vector3 l=leftFoot.position+Rig.Forward*.2f,r=rightFoot.position+Rig.Forward*.2f;
            Rig.Hips.position += Vector3.down * 0.38f;
            Chapter1IncidentRig.Solve(leftThigh,leftKnee,leftFoot,l,Rig.Forward);
            Chapter1IncidentRig.Solve(rightThigh,rightKnee,rightFoot,r,Rig.Forward);
        }
        if (fallen) { Rig.Hips.position+=Vector3.down*.7f;Rig.Hips.rotation = Quaternion.AngleAxis(80, transform.forward) * Rig.Hips.rotation; }
        if (speaking && Rig.Head) Rig.Head.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time*2.1f)*3, transform.right)*Rig.Head.rotation;
    }
    public void Face(Vector3 point) { facing = point-transform.position; if(Rig) Rig.Face(facing); }
}
