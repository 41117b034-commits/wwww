using UnityEngine;

// Council-only pose, applied after the shared rig and the seated/standing layer.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2RallyGesture : MonoBehaviour
{
    public float Weight {get;private set;}
    Chapter2Actor actor;
    Transform arm,elbow,hand,spine;
    Quaternion palmBasis;
    bool calibrated;
    float began=-1;

    public void Begin()
    {
        actor=GetComponent<Chapter2Actor>();var a=actor.Rig.animator;
        arm=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbow=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hand=actor.Rig.RightHand;
        spine=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.Spine,"Spine","Spine1");
        began=Time.time;
    }
    void LateUpdate()
    {
        if(began<0||!arm||!elbow||!hand)return;
        var rig=actor.Rig;Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        if(!calibrated)
        {
            Vector3 along=(hand.position-elbow.position).normalized;
            palmBasis=Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(along,Vector3.ProjectOnPlane(-right,along).normalized);
            calibrated=true;
        }
        float elapsed=Time.time-began;
        Weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.85f));
        float beat=elapsed>1?Mathf.Sin((elapsed-1)*3.2f)*.025f:0;
        if(spine)spine.rotation=Quaternion.AngleAxis(-3*Weight,right)*spine.rotation;
        // A single high open hand reads as a call to rally; the other arm stays low.
        Vector3 reach=(Vector3.up*(.39f+beat)+right*.12f+forward*.1f)*rig.Height;
        float length=Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
        Vector3 target=arm.position+Vector3.ClampMagnitude(reach,length*.94f);
        Chapter1IncidentRig.Solve(arm,elbow,hand,Vector3.Lerp(hand.position,target,Weight),right+forward*.25f);
        Quaternion rotation=Quaternion.LookRotation((Vector3.up+right*.12f).normalized,forward)*Quaternion.Inverse(palmBasis);
        hand.rotation=Quaternion.Slerp(hand.rotation,rotation,Weight);
    }
}
