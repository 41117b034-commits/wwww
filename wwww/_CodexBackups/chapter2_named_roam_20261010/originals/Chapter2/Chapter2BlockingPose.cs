using UnityEngine;

// A two-handed stop gesture owned only by the sacred-tree confrontation.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2BlockingPose : MonoBehaviour
{
    public float Weight { get; private set; }
    Chapter2Actor actor;
    Transform spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Quaternion[] palms={Quaternion.identity,Quaternion.identity};
    float began=-1;
    bool calibrated;

    public void Prepare()
    {
        actor=GetComponent<Chapter2Actor>();var animator=actor.Rig.animator;
        spine=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Spine,"Spine","Spine1");
        thighs[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        thighs[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        knees[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        knees[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        feet[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        feet[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        arms[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        arms[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbows[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        elbows[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hands[0]=actor.Rig.LeftHand;hands[1]=actor.Rig.RightHand;
    }

    public void Raise(){began=Time.time;}

    void LateUpdate()
    {
        if(began<0||!actor||actor.fallen)return;
        Weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.65f));
        var rig=actor.Rig;Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        if(!calibrated)
        {
            // The preceding rig layer aligns free fingers along each forearm and
            // palms inward. Recover that local basis without reading model meshes.
            for(int i=0;i<2;i++)
            {
                Vector3 fingers=(hands[i].position-elbows[i].position).normalized;
                Vector3 normal=Vector3.ProjectOnPlane(i==0?right:-right,fingers).normalized;
                palms[i]=Quaternion.Inverse(hands[i].rotation)*Quaternion.LookRotation(fingers,normal);
            }
            calibrated=true;
        }
        // Brace the body between the officer and the tree, with a wider stance.
        Vector3 leftFoot=feet[0].position,rightFoot=feet[1].position;
        Quaternion leftRotation=feet[0].rotation,rightRotation=feet[1].rotation;
        rig.Hips.position+=(-Vector3.up*.025f+forward*.015f)*rig.Height*Weight;
        if(spine)
        {
            spine.rotation=Quaternion.AngleAxis(30*Weight,Vector3.up)*spine.rotation;
            spine.rotation=Quaternion.AngleAxis(8*Weight,right)*spine.rotation;
        }
        Chapter1IncidentRig.Solve(thighs[0],knees[0],feet[0],leftFoot-right*rig.Height*.045f*Weight-forward*rig.Height*.035f*Weight,forward);
        Chapter1IncidentRig.Solve(thighs[1],knees[1],feet[1],rightFoot+right*rig.Height*.045f*Weight+forward*rig.Height*.035f*Weight,forward);
        feet[0].rotation=leftRotation;feet[1].rotation=rightRotation;
        Vector3 shieldForward=Quaternion.AngleAxis(30*Weight,Vector3.up)*forward;
        Vector3 shieldRight=Vector3.Cross(Vector3.up,shieldForward);
        if(rig.Head)rig.Head.rotation=Quaternion.AngleAxis(-30*Weight,Vector3.up)*rig.Head.rotation;
        for(int i=0;i<2;i++)
        {
            if(!arms[i]||!elbows[i]||!hands[i])continue;
            Vector3 side=shieldRight*(i==0?-1:1);
            // Spread the arms across the path below shoulder level, rather than
            // holding the palms beside the head as in a surrender gesture.
            Vector3 reach=(side*.29f+shieldForward*(i==0?.08f:.14f)-Vector3.up*(i==0?.03f:.065f))*rig.Height;
            float length=Vector3.Distance(arms[i].position,elbows[i].position)+Vector3.Distance(elbows[i].position,hands[i].position);
            Vector3 wrist=arms[i].position+Vector3.ClampMagnitude(reach,length*.94f);
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,wrist,Weight),side-Vector3.up*.6f);
            Quaternion rotation=Quaternion.LookRotation((side+Vector3.up*.12f).normalized,shieldForward)*Quaternion.Inverse(palms[i]);
            hands[i].rotation=Quaternion.Slerp(hands[i].rotation,rotation,Weight);
        }
    }
}
