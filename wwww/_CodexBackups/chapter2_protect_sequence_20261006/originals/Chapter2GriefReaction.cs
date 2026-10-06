using System.Collections.Generic;
using UnityEngine;

// Only the two witnesses in the protect branch receive this performance layer.
// Apply after the shared incident rig and Chapter2Actor have restored their poses.
[DefaultExecutionOrder(4300)]
public sealed class Chapter2GriefReaction : MonoBehaviour
{
    public float TurnProgress { get; private set; }
    public float FistWeight { get; private set; }
    public Chapter2Actor Casualty { get; private set; }
    public float RunProgress { get; private set; }
    public bool Arrived { get; private set; }
    public Vector3 Destination { get; private set; }
    Chapter2Actor actor;
    Transform spine;
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Quaternion[] palms=new Quaternion[2];
    readonly List<FistMesh> meshes=new List<FistMesh>();
    Vector3 initialForward;
    Vector3 runStart, runControl;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
    readonly Vector3[] footPositions=new Vector3[2];
    readonly Quaternion[] footRotations=new Quaternion[2];
    float runDuration,runWeight,runPhase,arrivedAt;
    float began;
    bool active;

    sealed class FistMesh
    {
        public SkinnedMeshRenderer renderer;
        public Mesh original,posed;
        public int index;
    }

    public void Prepare()
    {
        if(actor)return;
        actor=GetComponent<Chapter2Actor>();
        if(!actor || !actor.Rig)return;
        initialForward=actor.Rig.Forward;
        var animator=actor.Rig.animator;
        spine=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Spine,"Spine","Spine1");
        arms[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        arms[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbows[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        elbows[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hands[0]=actor.Rig.LeftHand;hands[1]=actor.Rig.RightHand;
        thighs[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        thighs[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        knees[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        knees[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        feet[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        feet[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        PrepareFists();
    }

    public void Begin(Chapter2Actor casualty,float delay,Vector3 destination)
    {
        Prepare();
        if(!actor || !actor.Rig || !casualty)return;
        Casualty=casualty;began=Time.time+delay;initialForward=actor.Rig.Forward;
        runStart=transform.position;Destination=new Vector3(destination.x,runStart.y,destination.z);
        // Separate lanes behind the casualty leave the body and the camera's sightline clear.
        runControl=Vector3.Lerp(runStart,Destination,.5f)+Vector3.forward*.32f;
        runDuration=Mathf.Max(1.05f,Vector3.Distance(runStart,Destination)/2.65f);
        Arrived=false;RunProgress=runPhase=runWeight=0;
        actor.speaking=false;actor.Rig.ClearInteractionPose();
        active=true;
    }

    void Update()
    {
        if(!active || !Casualty || actor.fallen)return;
        float elapsed=Time.time-began;
        TurnProgress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.48f,elapsed));
        RunProgress=Mathf.Clamp01((elapsed-.48f)/runDuration);
        float k=Mathf.SmoothStep(0,1,RunProgress);
        Vector3 position=(1-k)*(1-k)*runStart+2*(1-k)*k*runControl+k*k*Destination;
        Vector3 delta=position-transform.position;
        transform.position=position;
        runPhase+=delta.magnitude/(actor.Rig.Height*.68f)*Mathf.PI*2;
        runWeight=Mathf.MoveTowards(runWeight,RunProgress>0&&RunProgress<1?1:0,Time.deltaTime*7);
        if(RunProgress>=1&&!Arrived){Arrived=true;arrivedAt=Time.time;}
        Vector3 to=Vector3.ProjectOnPlane(Destination-runStart,Vector3.up).normalized;
        if(Arrived)to=Vector3.Slerp(to,Vector3.ProjectOnPlane(Casualty.transform.position-transform.position,Vector3.up).normalized,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-arrivedAt)/.55f)));
        actor.Face(transform.position+Vector3.Slerp(initialForward,to,TurnProgress));
        // This layer supplies a running gait, instead of speeding up the shared walking pose.
        actor.Rig.walking=false;
        FistWeight=Arrived?Mathf.Lerp(.6f,.08f,Mathf.Clamp01((Time.time-arrivedAt)/.65f)):Mathf.Clamp01(elapsed/.5f)*.6f;
    }

    void LateUpdate()
    {
        if(!active || !Casualty || actor.fallen)return;
        if(Time.time<began)return;
        float elapsed=Time.time-began;
        var rig=actor.Rig;
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        float flinch=Mathf.Sin(Mathf.Clamp01(elapsed/.55f)*Mathf.PI);
        float aid=Arrived?Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-arrivedAt)/1.05f)):0;
        int reachingHand=Destination.x>Casualty.transform.position.x?0:1;
        for(int i=0;i<2;i++)if(feet[i]){footPositions[i]=feet[i].position;footRotations[i]=feet[i].rotation;}
        // Settle over planted feet, with the pelvis close to the heels instead of a high half-sit.
        rig.Hips.position+=Vector3.up*rig.Height*((-.025f+.012f*Mathf.Cos(runPhase*2))*runWeight-.29f*aid)
            -forward*rig.Height*.03f*aid;
        if(spine)spine.rotation=Quaternion.AngleAxis(-7*flinch+15*runWeight+34*aid+Mathf.Sin(elapsed*3)*.35f*aid,right)*spine.rotation;
        for(int i=0;i<2;i++)
        {
            if(!thighs[i]||!knees[i]||!feet[i])continue;
            float phase=runPhase+i*Mathf.PI;
            Vector3 target=footPositions[i]+forward*(Mathf.Sin(phase)*rig.Height*.20f*runWeight)
                +Vector3.up*(Mathf.Max(0,Mathf.Cos(phase))*rig.Height*.13f*runWeight);
            Vector3 side=right*(i==0?-1:1);
            target+=(forward*(i==reachingHand?-.035f:.045f)+side*.022f)*rig.Height*aid;
            Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],target,forward+side*.28f*aid);
            feet[i].rotation=Quaternion.AngleAxis((i==0?-8:8)*aid,Vector3.up)*footRotations[i];
        }
        for(int i=0;i<2;i++)
        {
            if(!arms[i] || !elbows[i] || !hands[i])continue;
            Vector3 side=right*(i==0?-1:1);
            float swing=Mathf.Sin(runPhase+i*Mathf.PI);
            Vector3 target=rig.Hips.position+side*rig.Height*.17f
                +forward*rig.Height*(.12f+.16f*swing*runWeight)
                +Vector3.up*rig.Height*(.12f*runWeight+.09f*flinch);
            if(aid>0)
            {
                // One hand steadies the body on a knee; the other checks on the casualty.
                Vector3 resting=knees[i]?knees[i].position+Vector3.up*rig.Height*.025f-forward*rig.Height*.015f:target;
                Vector3 aidTarget=resting;
                if(i==reachingHand)
                {
                    // Keep the gesture low and within reach while facing the casualty.
                    aidTarget=rig.Hips.position+(forward*.32f+side*.04f)*rig.Height;
                    aidTarget.y=Mathf.Min(footPositions[0].y,footPositions[1].y)+rig.Height*.16f;
                    float reach=(Vector3.Distance(arms[i].position,elbows[i].position)+Vector3.Distance(elbows[i].position,hands[i].position))*.9f;
                    aidTarget=arms[i].position+Vector3.ClampMagnitude(aidTarget-arms[i].position,reach);
                }
                target=Vector3.Lerp(target,aidTarget,aid);
            }
            float weight=Mathf.Max(Mathf.Max(runWeight,aid),flinch*.75f);
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,target,weight),side-forward*.25f);
            Vector3 along=(hands[i].position-elbows[i].position).normalized;
            Vector3 palm=Vector3.ProjectOnPlane(Vector3.Slerp(-side,Vector3.down,aid),along).normalized;
            if(palm.sqrMagnitude>.001f)
                hands[i].rotation=Quaternion.Slerp(hands[i].rotation,Quaternion.LookRotation(along,palm)*Quaternion.Inverse(palms[i]),weight);
        }
        if(rig.Head && Casualty.Rig && Casualty.Rig.Hips)
        {
            Vector3 toward=(Casualty.Rig.Hips.position-rig.Head.position).normalized;
            Vector3 gaze=Vector3.RotateTowards(forward,toward,38*Mathf.Deg2Rad,0);
            rig.Head.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(forward,gaze),TurnProgress)*rig.Head.rotation;
        }
        foreach(var mesh in meshes)mesh.renderer.SetBlendShapeWeight(mesh.index,FistWeight*100);
    }

    void PrepareFists()
    {
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var original=skin.sharedMesh;if(!original)continue;
            var pose=Resources.Load<Chapter2GriefHandPose>("GriefHands/"+original.name);
            if(!pose || !pose.mesh || pose.fistShape<0 || pose.fistShape>=pose.mesh.blendShapeCount)
            {Debug.LogWarning("[Chapter2] Missing grief hand pose for "+original.name,this);continue;}
            palms[0]=pose.leftPalm;palms[1]=pose.rightPalm;
            skin.sharedMesh=pose.mesh;
            skin.SetBlendShapeWeight(pose.fistShape,0);
            meshes.Add(new FistMesh{renderer=skin,original=original,posed=pose.mesh,index=pose.fistShape});
        }
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)
        {if(mesh.renderer)mesh.renderer.sharedMesh=mesh.original;}
    }
}

