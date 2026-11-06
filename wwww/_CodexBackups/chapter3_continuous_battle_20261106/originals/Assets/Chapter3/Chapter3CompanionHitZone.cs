using UnityEngine;

// Bullet-only shapes follow the posed skeleton. The original movement capsule
// remains solid, but is no longer an invisible standing target above a crouch.
[DefaultExecutionOrder(4800)]
public sealed class Chapter3CompanionHitZone : MonoBehaviour
{
    Transform a,b;
    CapsuleCollider shape;
    float radius;
    Vector3 tip;
    public static bool Install(Chapter3CompanionReaction owner)
    {
        var rig=owner.actor?owner.actor.Rig:null;
        if(!rig||!rig.animator||!rig.Head||!rig.Hips)return false;
        var animator=rig.animator;
        Transform Bone(HumanBodyBones bone,string name)=>Chapter1WeddingRigBones.Resolve(animator,bone,name);
        var neck=Bone(HumanBodyBones.Neck,"Neck");if(!neck)return false;
        float h=rig.Height;
        Add(owner,"head",neck,rig.Head,h*.085f,Vector3.up*h*.05f);
        Add(owner,"body",rig.Hips,neck,h*.15f,Vector3.zero);
        foreach(bool left in new[]{true,false})
        {
            var thigh=Bone(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg,left?"L_Thigh":"R_Thigh");
            var knee=Bone(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg,left?"L_Calf":"R_Calf");
            var foot=Bone(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot,left?"L_Foot":"R_Foot");
            var arm=Bone(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm,left?"L_Upperarm":"R_Upperarm");
            var elbow=Bone(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm,left?"L_Forearm":"R_Forearm");
            Add(owner,"thigh",thigh,knee,h*.075f,Vector3.zero);
            Add(owner,"shin",knee,foot,h*.06f,Vector3.zero);
            Add(owner,"upper arm",arm,elbow,h*.06f,Vector3.zero);
            Add(owner,"forearm",elbow,left?rig.LeftHand:rig.RightHand,h*.05f,Vector3.zero);
        }
        return true;
    }
    static void Add(Chapter3CompanionReaction owner,string label,Transform a,Transform b,float radius,Vector3 tip)
    {
        if(!a||!b)return;
        var go=new GameObject("Companion shot zone · "+label);go.transform.SetParent(owner.transform,false);
        var zone=go.AddComponent<Chapter3CompanionHitZone>();zone.a=a;zone.b=b;zone.radius=radius;zone.tip=b.InverseTransformVector(tip);
        zone.shape=go.AddComponent<CapsuleCollider>();zone.shape.isTrigger=true;zone.shape.direction=1;zone.Align();
    }
    void LateUpdate(){Align();}
    void Align()
    {
        if(!a||!b||!shape)return;
        Vector3 start=a.position,end=b.TransformPoint(tip),delta=end-start,scale=transform.parent.lossyScale;
        transform.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
        transform.SetPositionAndRotation((start+end)*.5f,delta.sqrMagnitude>.00001f?Quaternion.FromToRotation(Vector3.up,delta):Quaternion.identity);
        shape.radius=radius;shape.height=Mathf.Max(radius*2,delta.magnitude+radius*2);
    }
}
