using System.Collections.Generic;
using UnityEngine;

// The falling-tree branch owns this performance; shared character animation stays unchanged.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2StartleReaction : MonoBehaviour
{
    public int StepsCompleted { get; private set; }
    public float RetreatDistance => Vector3.Distance(start,transform.position);
    public float FearWeight { get; private set; }
    Chapter2Actor actor;
    Transform spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2],arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Vector3[] anchors=new Vector3[2];
    readonly Quaternion[] footRotations=new Quaternion[2];
    readonly List<SkinnedMeshRenderer> faces=new List<SkinnedMeshRenderer>();
    readonly List<Mesh> originalFaces=new List<Mesh>();
    Vector3 start,forward,retreat;
    float began,releaseAt=-1;
    bool active;
    const string FearShape="Chapter2 fear";

    public void Prepare()
    {
        if(actor)return;
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
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh original=skin.sharedMesh;if(!original)continue;
            Mesh face=Resources.Load<Mesh>("FearFaces/"+original.name);
            if(!face||face.GetBlendShapeIndex(FearShape)<0)continue;
            originalFaces.Add(original);faces.Add(skin);skin.sharedMesh=face;
            skin.SetBlendShapeWeight(face.GetBlendShapeIndex(FearShape),0);
        }
    }

    public void Begin(Transform danger,int variation)
    {
        Prepare();start=transform.position;
        forward=Vector3.ProjectOnPlane(danger.position-start,Vector3.up).normalized;retreat=-forward;
        actor.Rig.ClearInteractionPose();actor.speaking=false;actor.Face(start+forward);
        for(int i=0;i<2;i++){anchors[i]=feet[i].position;footRotations[i]=feet[i].rotation;}
        began=Time.time+variation*.09f;StepsCompleted=0;releaseAt=-1;active=true;
    }

    public void Release(){releaseAt=Time.time;}

    void Update()
    {
        if(!active)return;
        float t=Mathf.Max(0,Time.time-began),stepTime=Mathf.Clamp((t-.25f)/.48f,0,3);
        int step=Mathf.Min(2,Mathf.FloorToInt(stepTime));float k=Mathf.SmoothStep(0,1,stepTime-step);
        float from=step==0?0:step==1?.20f:.70f,to=step==0?.20f:step==1?.70f:1.15f;
        transform.position=start+retreat*Mathf.Lerp(from,to,k);
        StepsCompleted=Mathf.FloorToInt(stepTime);
        actor.Face(transform.position+forward);actor.Rig.walking=false;
        FearWeight=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.18f));
        if(releaseAt>=0)FearWeight*=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-releaseAt)/.6f));
        foreach(var skin in faces)skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(FearShape),FearWeight*100);
        if(releaseAt>=0&&FearWeight<=0){active=false;enabled=false;}
    }

    void LateUpdate()
    {
        if(!active||Time.time<began)return;
        var rig=actor.Rig;float t=Time.time-began;
        float phase=Mathf.Clamp((t-.25f)/.48f,0,3);int step=Mathf.Min(2,Mathf.FloorToInt(phase));float k=phase-step;
        Vector3 right=Vector3.Cross(Vector3.up,forward);
        float recoil=Mathf.Sin(Mathf.Clamp01(t/.55f)*Mathf.PI);
        rig.Hips.position+=Vector3.down*rig.Height*(.035f+.022f*recoil)*FearWeight;
        if(spine)spine.rotation=Quaternion.AngleAxis((-9-12*recoil)*FearWeight,right)*spine.rotation;
        for(int i=0;i<2;i++)
        {
            float distance=i==0?(step==0?Mathf.SmoothStep(0,.55f,k):step==1?.55f:Mathf.SmoothStep(.55f,1.30f,k))
                :(step==0?0:step==1?Mathf.SmoothStep(0,1,k):1);
            float lift=i==step%2?Mathf.Sin(k*Mathf.PI)*rig.Height*.09f:0;
            Vector3 target=anchors[i]+retreat*distance+Vector3.up*lift;
            Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],target,forward);
            feet[i].rotation=footRotations[i];
            Vector3 side=right*(i==0?-1:1);
            Vector3 hand=arms[i].position+forward*rig.Height*.20f+side*rig.Height*.08f+Vector3.up*rig.Height*(.055f+.045f*recoil);
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,hand,FearWeight),side-Vector3.up*.35f);
        }
        rig.Head.rotation=Quaternion.AngleAxis(-10*FearWeight,right)*rig.Head.rotation;
    }

    void OnDestroy(){for(int i=0;i<faces.Count;i++)if(faces[i])faces[i].sharedMesh=originalFaces[i];}

}
