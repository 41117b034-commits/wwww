using UnityEngine;

// Council-only pose, applied after the shared rig and the seated/standing layer.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2RallyGesture : MonoBehaviour
{
    public float Weight {get;private set;}
    Chapter2Actor actor;
    Transform arm,elbow,hand,spine;
    Chapter2CouncilHands hands;
    float began=-1;

    public void Begin()
    {
        actor=GetComponent<Chapter2Actor>();var a=actor.Rig.animator;
        arm=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbow=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hand=actor.Rig.RightHand;
        spine=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.Spine,"Spine","Spine1","Spine01");
        hands=GetComponent<Chapter2CouncilHands>();
        began=Time.time;
    }
    void LateUpdate()
    {
        if(began<0||!arm||!elbow||!hand)return;
        var rig=actor.Rig;Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        float elapsed=Time.time-began;
        Weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.72f,elapsed));
        float anticipation=Mathf.Sin(Mathf.Clamp01(elapsed/.8f)*Mathf.PI);
        float accent=Mathf.Sin(Mathf.Clamp01((elapsed-2.3f)/.65f)*Mathf.PI);
        // Shift forward to rise, draw in the fist, then drive it up with a bent elbow.
        if(spine)spine.rotation=Quaternion.AngleAxis(14*anticipation+6*Weight+3*accent,right)*spine.rotation;
        Vector3 reach=(Vector3.up*(.16f+.035f*accent)+right*.14f+forward*.19f)*rig.Height;
        float length=Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
        Vector3 target=arm.position+Vector3.ClampMagnitude(reach,length*.78f);
        Chapter1IncidentRig.Solve(arm,elbow,hand,Vector3.Lerp(hand.position,target,Weight),right+forward*.1f-Vector3.up*.25f);
        Vector3 along=(hand.position-elbow.position).normalized;
        // The authored basis uses the back of the hand: present the knuckles
        // toward the gathering while the thumb closes against the fingers.
        Vector3 palm=Vector3.ProjectOnPlane(forward+right*.65f,along).normalized;
        if(hands)
        {
            hands.FistWeight=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.25f));
            Quaternion rotation=Quaternion.LookRotation(along,palm)*Quaternion.Inverse(hands.rightPalmBasis);
            Vector3 currentNormal=Vector3.ProjectOnPlane(hand.rotation*(hands.rightPalmBasis*Vector3.up),along).normalized;
            float roll=Vector3.SignedAngle(currentNormal,palm,along);
            // Carry most of the turn through the forearm, avoiding a twisted
            // wrist seam on this donor's single hand bone.
            elbow.rotation=Quaternion.AngleAxis(roll*.7f*Weight,along)*elbow.rotation;
            hand.rotation=Quaternion.Slerp(hand.rotation,rotation,Weight);
        }
    }
}
