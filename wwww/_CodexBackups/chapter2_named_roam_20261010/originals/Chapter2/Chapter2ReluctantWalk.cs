using UnityEngine;

// Three small, planted steps following the casualty check, continuing into the fade.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2ReluctantWalk : MonoBehaviour
{
    public int StepsCompleted { get; private set; }
    public float DistanceTravelled => Vector3.Distance(start,transform.position);
    Chapter2Actor actor;
    Transform spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2],arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Vector3[] anchors=new Vector3[2],turnedFeet=new Vector3[2];
    readonly Quaternion[] footRotations=new Quaternion[2],palms=new Quaternion[2];
    Vector3 start,forward,initialForward;
    float began;
    bool active,calibrated;
    const float TurnSeconds=.8f,StepSeconds=.68f,StepLength=.32f;

    public void Begin(Vector3 direction,float delay)
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
        start=transform.position;initialForward=actor.Rig.Forward;
        forward=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        Quaternion turn=Quaternion.FromToRotation(initialForward,forward);
        for(int i=0;i<2;i++)
        {
            anchors[i]=feet[i].position;footRotations[i]=feet[i].rotation;
            turnedFeet[i]=start+turn*(anchors[i]-start);
        }
        began=Time.time+delay;active=true;
        actor.speaking=false;actor.Rig.ClearInteractionPose();
    }

    void Update()
    {
        if(!active)return;
        float time=Mathf.Max(0,Time.time-began);
        float phase=Mathf.Clamp((time-TurnSeconds)/StepSeconds,0,3);
        int step=Mathf.Min(2,Mathf.FloorToInt(phase));
        float k=phase-step;
        float from=step==0?0:step==1?StepLength*.5f:StepLength*1.5f;
        float to=StepLength*(step+.5f);
        // Carry the body's speed across footfalls. Restarting SmoothStep on every
        // footfall makes the torso stop three times even when the feet keep moving.
        float inSpeed=step==0?0:StepLength;
        float outSpeed=step==2?0:StepLength;
        float distance=(2*k*k*k-3*k*k+1)*from+(k*k*k-2*k*k+k)*inSpeed
            +(-2*k*k*k+3*k*k)*to+(k*k*k-k*k)*outSpeed;
        transform.position=start+forward*distance;
        actor.Face(transform.position+Vector3.Slerp(initialForward,forward,Mathf.SmoothStep(0,1,Mathf.Clamp01(time/TurnSeconds))));
        actor.Rig.walking=false;
        StepsCompleted=Mathf.FloorToInt(phase);
    }

    void LateUpdate()
    {
        if(!active)return;
        var rig=actor.Rig;Vector3 right=Vector3.Cross(Vector3.up,rig.Forward);
        if(!calibrated)
        {
            for(int i=0;i<2;i++)
            {
                Vector3 along=(hands[i].position-elbows[i].position).normalized;
                palms[i]=Quaternion.Inverse(hands[i].rotation)*Quaternion.LookRotation(along,Vector3.ProjectOnPlane(i==0?right:-right,along).normalized);
            }
            calibrated=true;
        }
        float time=Mathf.Max(0,Time.time-began);
        float phase=Mathf.Clamp((time-TurnSeconds)/StepSeconds,0,3);
        int step=Mathf.Min(2,Mathf.FloorToInt(phase));float k=phase-step;
        float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.3f));
        float sway=Mathf.Sin(phase*Mathf.PI);
        rig.Hips.position+=(Vector3.down*(.018f+.007f*Mathf.Abs(sway))+right*.012f*sway)*rig.Height*settle;
        if(spine)spine.rotation=Quaternion.AngleAxis(5*settle,right)*Quaternion.AngleAxis(-2*sway,Vector3.up)*spine.rotation;
        for(int i=0;i<2;i++)
        {
            float turn=Mathf.Clamp01((time/TurnSeconds-i*.5f)*2);
            Vector3 target=Vector3.Lerp(anchors[i],turnedFeet[i],Mathf.SmoothStep(0,1,turn));
            target+=Vector3.up*Mathf.Sin(turn*Mathf.PI)*.035f;
            if(time>=TurnSeconds)
            {
                float distance=i==0?(step==0?Mathf.SmoothStep(0,StepLength,k):step==1?StepLength:Mathf.SmoothStep(StepLength,StepLength*3,k))
                    :(step==0?0:step==1?Mathf.SmoothStep(0,StepLength*2,k):StepLength*2);
                float lift=i==step%2?Mathf.Sin(k*Mathf.PI)*.055f:0;
                target=turnedFeet[i]+forward*distance+Vector3.up*lift;
            }
            Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],target,rig.Forward);
            feet[i].rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(initialForward,forward),Mathf.SmoothStep(0,1,turn))*footRotations[i];
            Vector3 side=right*(i==0?-1:1);
            Vector3 wrist=rig.Hips.position+side*rig.Height*.17f+rig.Forward*rig.Height*(.025f+.045f*sway*(i==0?-1:1));
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,wrist,settle),side-rig.Forward*.3f);
            Vector3 fingers=(hands[i].position-elbows[i].position).normalized;
            Quaternion rotation=Quaternion.LookRotation(fingers,Vector3.ProjectOnPlane(-side,fingers).normalized)*Quaternion.Inverse(palms[i]);
            hands[i].rotation=Quaternion.Slerp(hands[i].rotation,rotation,settle);
        }
        if(rig.Head)rig.Head.rotation=Quaternion.AngleAxis(9*settle,right)*rig.Head.rotation;
    }
}
