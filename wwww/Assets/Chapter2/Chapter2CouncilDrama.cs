using UnityEngine;

// Applied after the common rig, seating and authored wrist corrections.
[DefaultExecutionOrder(4380)]
public sealed class Chapter2CouncilDrama : MonoBehaviour
{
    public enum Beat { Rest,Hilt,Concern,Fire,Resolve,EyesClosed,Declare,Cold,Expel,Stab,Planted,Draw,Pledge,Nod }
    public bool hasKnife;
    public Transform knife;
    public SkinnedMeshRenderer gripRenderer;
    public Mesh gripMesh;
    public int gripShape=-1;
    public Quaternion gripBasis=Quaternion.identity;
    public Vector3 gripCenter;
    public Vector3 pointAt;
    public Transform speechLookTarget;
    public float GazeError { get; private set; }
    public float WristBend { get; private set; }
    public Beat CurrentBeat { get; private set; }
    public bool KnifePlanted { get; private set; }
    public float KnifeTipHeight => knife ? (knife.position+knife.up*.58f).y : float.NaN;
    Chapter2Actor actor;
    Transform arm,elbow,hand,spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
    readonly Vector3[] footPositions=new Vector3[2];
    Mesh original;
    float began;
    Vector3 plantedPosition;
    Quaternion plantedRotation;
    bool drawn;
    Quaternion headBasis;
    Chapter2CouncilHands hands;

    void Start()
    {
        actor=GetComponent<Chapter2Actor>();
        var animator=actor.Rig.animator;
        arm=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbow=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hand=actor.Rig.RightHand;
        hands=GetComponent<Chapter2CouncilHands>();
        if(actor.Rig.Head)headBasis=Quaternion.Inverse(actor.Rig.Head.rotation)*Quaternion.LookRotation(actor.Rig.Forward,Vector3.up);
        spine=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Spine,"Spine","Spine1","Spine01");
        thighs[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        thighs[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        knees[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        knees[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        feet[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        feet[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        if(gripRenderer&&gripMesh){original=gripRenderer.sharedMesh;gripRenderer.sharedMesh=gripMesh;}
        if(knife)knife.gameObject.SetActive(hasKnife);
    }
    public void Play(Beat beat)
    {
        if(CurrentBeat==beat)return;
        CurrentBeat=beat;began=Time.time;
        if(beat==Beat.Resolve||beat==Beat.Draw||beat==Beat.Pledge||beat==Beat.Stab)drawn=true;
    }
    void LateUpdate()
    {
        if(!actor||!actor.Rig||!arm||!elbow||!hand)return;
        var rig=actor.Rig;Vector3 f=rig.Forward,r=Vector3.Cross(Vector3.up,f),up=Vector3.up;
        float elapsed=Time.time-began,k=Mathf.SmoothStep(0,1,elapsed/.55f),h=rig.Height;
        for(int i=0;i<2;i++)if(feet[i])footPositions[i]=feet[i].position;
        Vector3 hip=rig.Hips.position,grip=hip+r*(h*.18f)+f*(h*.13f);
        Vector3 target=grip,direction=-up;
        bool hold=hasKnife,poseArm=hasKnife;
        float lean=0,headAngle=0;
        switch(CurrentBeat)
        {
            case Beat.Hilt: target=hip+r*(h*.19f)+f*(h*.24f)+up*(h*.08f);lean=5;break;
            case Beat.Concern:
                poseArm=true;target=arm.position+f*(h*.24f)-up*(h*.12f);headAngle=8;lean=5;break;
            case Beat.Fire: headAngle=17;lean=8;break;
            case Beat.Resolve:
                target=arm.position+f*(h*.23f)+r*(h*.06f)-up*(h*.18f);direction=(up+f*.2f).normalized;lean=9;break;
            case Beat.EyesClosed:headAngle=19;lean=5;break;
            case Beat.Declare:
                poseArm=true;target=arm.position+Vector3.ClampMagnitude((up*.16f+r*.14f+f*.19f)*h,
                    (Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position))*.78f);lean=8;break;
            case Beat.Cold:headAngle=-5;lean=7;break;
            case Beat.Expel:
                poseArm=true;Vector3 toward=(pointAt-arm.position).normalized;
                target=arm.position+toward*(Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position))*.95f;
                lean=12;headAngle=-3;break;
            case Beat.Stab:
                float lift=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.6f));
                float drive=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1.15f,elapsed));
                lean=42*drive;rig.Hips.position-=up*(h*.13f*drive);
                plantedPosition=actor.transform.position+f*.49f+r*.31f+up*.5f;
                plantedRotation=Quaternion.FromToRotation(up,-up);
                target=Vector3.Lerp(grip,arm.position+f*(h*.16f)+up*(h*.2f),lift);
                target=Vector3.Lerp(target,plantedPosition,drive);direction=-up;
                if(elapsed>=1.15f)KnifePlanted=true;
                break;
            case Beat.Planted:
                hold=false;poseArm=false;lean=5;break;
            case Beat.Draw:
            case Beat.Pledge:
                target=Vector3.Lerp(grip,arm.position+f*(h*.23f)+r*(h*.12f),k);direction=(up+f*.18f).normalized;lean=6;break;
            case Beat.Nod:
                headAngle=elapsed<1.25f?Mathf.Sin(elapsed/1.25f*Mathf.PI)*19:4;lean=3;break;
        }
        if(CurrentBeat==Beat.Rest&&drawn){target=arm.position+r*(h*.14f)+f*(h*.18f)-up*(h*.15f);direction=up;}
        if(spine)spine.rotation=Quaternion.AngleAxis(lean*k,r)*spine.rotation;
        if(rig.Head)rig.Head.rotation=Quaternion.AngleAxis(headAngle*k,r)*rig.Head.rotation;
        // Apply eye contact after the seated, talking and dramatic lean layers.
        if(actor.speaking&&speechLookTarget&&rig.Head)
        {
            Vector3 gaze=(speechLookTarget.position-rig.Head.position).normalized;
            rig.Head.rotation=Quaternion.LookRotation(gaze,up)*Quaternion.Inverse(headBasis);
            GazeError=Vector3.Angle(rig.Head.rotation*(headBasis*Vector3.forward),gaze);
        }
        if(poseArm)
        {
            // A steady sword grip must not inherit the shared talking hand wave.
            Chapter1IncidentRig.Solve(arm,elbow,hand,Vector3.Lerp(hand.position,target,k),r+f*.2f-up*.3f);
            if(CurrentBeat==Beat.Expel)
            {
                if(hands)hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.LookRotation((pointAt-hand.position).normalized,up)*Quaternion.Inverse(hands.rightPalmBasis),k);
            }
        }
        if(hands&&hands.fistRenderer&&hands.fistShape>=0)
        {
            float fist=CurrentBeat==Beat.Declare?k:0;
            hands.FistWeight=fist;hands.fistRenderer.SetBlendShapeWeight(hands.fistShape,fist*100);
            if(fist>0)
            {
                Vector3 along=(hand.position-elbow.position).normalized;
                Vector3 normal=Vector3.ProjectOnPlane(f+r*.65f,along).normalized;
                OrientWrist(along,normal,hands.rightPalmBasis,fist);
            }
        }
        if(CurrentBeat==Beat.Stab)
            for(int i=0;i<2;i++)if(thighs[i]&&knees[i]&&feet[i])Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],footPositions[i],f);
        bool gripping=hasKnife&&hold&&(!KnifePlanted||CurrentBeat==Beat.Stab);
        if(gripRenderer&&gripShape>=0)gripRenderer.SetBlendShapeWeight(gripShape,gripping?100:0);
        if(gripping)
        {
            // Keep the hand in line with the forearm and let the blade follow
            // the grip. A fixed vertical blade used to force the wrist sideways.
            Vector3 fingers=(hand.position-elbow.position).normalized;
            direction=Vector3.ProjectOnPlane(direction,fingers).normalized;
            if(direction.sqrMagnitude<.01f)direction=Vector3.ProjectOnPlane(f,fingers).normalized;
            OrientWrist(fingers,Vector3.Cross(direction,fingers),gripBasis,1);
            WristBend=Vector3.Angle(hand.rotation*(gripBasis*Vector3.forward),fingers);
        }
        if(knife&&hasKnife)
        {
            var blade=knife.Find("刀刃");if(blade)blade.gameObject.SetActive(drawn);
            var sheath=knife.Find("刀鞘");if(sheath)sheath.gameObject.SetActive(!drawn);
            if(KnifePlanted)knife.SetPositionAndRotation(plantedPosition,plantedRotation);
            else if(hold)knife.SetPositionAndRotation(hand.TransformPoint(gripCenter)+direction*.025f,Quaternion.LookRotation(f,direction));
            else knife.SetPositionAndRotation(grip,Quaternion.LookRotation(f,-up));
        }
    }
    void OrientWrist(Vector3 along,Vector3 normal,Quaternion basis,float weight)
    {
        Quaternion rotation=Quaternion.LookRotation(along,normal)*Quaternion.Inverse(basis);
        Vector3 current=Vector3.ProjectOnPlane(hand.rotation*(basis*Vector3.up),along).normalized;
        float roll=Vector3.SignedAngle(current,normal,along);
        elbow.rotation=Quaternion.AngleAxis(roll*.7f*weight,along)*elbow.rotation;
        hand.rotation=Quaternion.Slerp(hand.rotation,rotation,weight);
    }
    void OnDestroy(){if(original&&gripRenderer)gripRenderer.sharedMesh=original;}
}
