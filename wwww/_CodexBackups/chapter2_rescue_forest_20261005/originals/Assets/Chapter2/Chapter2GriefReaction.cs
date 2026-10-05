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
    Chapter2Actor actor;
    Transform spine;
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Quaternion[] palms=new Quaternion[2];
    readonly List<FistMesh> meshes=new List<FistMesh>();
    Vector3 initialForward;
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
        PrepareFists();
    }

    public void Begin(Chapter2Actor casualty,float delay)
    {
        Prepare();
        if(!actor || !actor.Rig || !casualty)return;
        Casualty=casualty;began=Time.time+delay;initialForward=actor.Rig.Forward;
        actor.speaking=false;actor.Rig.ClearInteractionPose();
        active=true;
    }

    void Update()
    {
        if(!active || !Casualty || actor.fallen)return;
        float elapsed=Time.time-began;
        TurnProgress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,1.65f,elapsed));
        Vector3 to=Vector3.ProjectOnPlane(Casualty.transform.position-transform.position,Vector3.up).normalized;
        actor.Face(transform.position+Vector3.Slerp(initialForward,to,TurnProgress));
        FistWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,2.45f,elapsed));
    }

    void LateUpdate()
    {
        if(!active || !Casualty || actor.fallen)return;
        float elapsed=Mathf.Max(0,Time.time-began);
        var rig=actor.Rig;
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        float flinch=Mathf.Sin(Mathf.Clamp01(elapsed/.55f)*Mathf.PI);
        // One aborted lean towards the fallen companion, followed by held tension.
        float reach=Mathf.Sin(Mathf.InverseLerp(1.35f,3.7f,elapsed)*Mathf.PI);
        float breath=Mathf.Sin(elapsed*2.8f)*.45f*FistWeight;
        if(spine)spine.rotation=Quaternion.AngleAxis(-5*flinch+8*reach+3*FistWeight+breath,right)*spine.rotation;
        for(int i=0;i<2;i++)
        {
            if(!arms[i] || !elbows[i] || !hands[i])continue;
            Vector3 side=right*(i==0?-1:1);
            Vector3 target=rig.Hips.position+side*rig.Height*.15f
                +forward*rig.Height*(.085f+.025f*reach)+Vector3.up*rig.Height*(.015f+.035f*flinch);
            float weight=Mathf.Max(FistWeight,flinch*.55f);
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,target,weight),side-forward*.25f);
            Vector3 along=(hands[i].position-elbows[i].position).normalized;
            Vector3 palm=Vector3.ProjectOnPlane(-side,along).normalized;
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

