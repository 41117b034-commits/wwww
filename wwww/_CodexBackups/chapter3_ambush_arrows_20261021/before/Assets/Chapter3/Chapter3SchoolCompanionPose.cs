using System.Collections.Generic;
using UnityEngine;

// A school-only layer over the shared gait. It never moves the actor root,
// changes colliders, or carries a gesture into the fifth-batch injury layer.
[DefaultExecutionOrder(4550)]
public sealed class Chapter3SchoolCompanionPose : MonoBehaviour
{
    public bool BonesReady=>prepared;
    public bool UnderFire {get;private set;}
    public float ForwardSignalWeight {get;private set;}
    public float UnderFireWeight {get;private set;}
    public float SafeReportWeight {get;private set;}
    Chapter2Actor actor;
    Chapter1IncidentRig rig;
    Chapter3SchoolController school;
    Chapter3InjuryPose injury;
    Transform bodyRoot,spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Vector3[] footTargets=new Vector3[2];
    readonly Quaternion[] footRotations=new Quaternion[2];
    Transform[] changedBones;
    Vector3[] originalPositions;
    Quaternion[] originalRotations;
    float forwardBegan,forwardUntil=-1,reportBegan,reportUntil=-1;
    bool prepared,poseApplied;

    void Awake(){actor=GetComponent<Chapter2Actor>();school=GetComponentInParent<Chapter3SchoolController>();}
    public void SignalForward(float seconds)
    {
        if(seconds<=0)return;
        UnderFire=false;reportUntil=-1;forwardBegan=Time.time;forwardUntil=Time.time+seconds;
    }
    public void SetUnderFire(bool value)
    {
        if(value&&!UnderFire)forwardUntil=reportUntil=-1;
        UnderFire=value;
    }
    public void ReportSafe(float seconds=2.4f)
    {
        if(seconds<=0)return;
        UnderFire=false;forwardUntil=-1;reportBegan=Time.time;reportUntil=Time.time+seconds;
    }
    public void Clear()
    {
        RestorePose();UnderFire=false;ForwardSignalWeight=UnderFireWeight=SafeReportWeight=0;
        forwardUntil=reportUntil=-1;
    }
    public void Suspend(){Clear();}
    void OnDisable(){Clear();}

    void LateUpdate()
    {
        // Rig.LateUpdate (4000) has already rebuilt this frame's unmodified gait.
        // Do not restore a previous-frame capture over that fresh pose.
        poseApplied=false;
        if(!Prepare())return;
        if(!injury)injury=GetComponent<Chapter3InjuryPose>();
        if(actor.fallen||(injury&&injury.InjuryWeight>0)||
            (school&&school.Battle&&(school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Inactive||
                (school.Battle.Rescue&&school.Battle.Rescue.Active))))
        {Clear();return;}
        ForwardSignalWeight=Envelope(forwardBegan,forwardUntil);
        SafeReportWeight=Envelope(reportBegan,reportUntil);
        UnderFireWeight=Mathf.MoveTowards(UnderFireWeight,UnderFire?1:0,Time.deltaTime*4);
        float crouch=UnderFireWeight;
        float signal=ForwardSignalWeight*(1-crouch),report=SafeReportWeight*(1-crouch);
        if(Mathf.Max(crouch,Mathf.Max(signal,report))<=0)return;
        CapturePose();
        float height=rig.Height;
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);

        // Lower the shared Pelvis/Waist parent, then keep the original soles on
        // the ground using knee IK. Walking still comes from the existing gait.
        for(int i=0;i<2;i++){footTargets[i]=feet[i].position;footRotations[i]=feet[i].rotation;}
        bodyRoot.position-=Vector3.up*(height*.20f*crouch);
        if(spine)spine.rotation=Quaternion.AngleAxis(23*crouch,right)*spine.rotation;
        if(crouch>0)
            for(int i=0;i<2;i++)
            {
                Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],footTargets[i],forward+right*(i==0?-.18f:.18f));
                feet[i].rotation=footRotations[i];
            }

        if(crouch>0)
        {
            // Arms come visibly in front of the head/chest rather than simply
            // bending the spine while leaving the hands hanging at the sides.
            PoseArm(0,rig.Head.position-right*height*.13f+forward*height*.12f-Vector3.up*height*.10f,-right-forward,crouch);
            PoseArm(1,rig.Hips.position+right*height*.13f+forward*height*.22f+Vector3.up*height*.18f,right-forward,crouch);
        }
        if(signal>0)
        {
            float wave=.5f+.5f*Mathf.Sin((Time.time-forwardBegan)*Mathf.PI*3.6f);
            Vector3 hand=rig.Head.position+right*height*.20f+
                forward*height*Mathf.Lerp(.10f,.30f,wave)+Vector3.up*height*Mathf.Lerp(.16f,.04f,wave);
            PoseArm(1,hand,right+forward*.2f,signal);
        }
        if(report>0)
        {
            // A held raised hand and a single nod read differently from the
            // repeated overhead beckon used to command the next crossing.
            Vector3 hand=rig.Head.position+right*height*.23f+forward*height*.16f-Vector3.up*height*.07f;
            PoseArm(1,hand,right+forward*.2f,report);
            float nod=Mathf.Sin(Mathf.Clamp01((Time.time-reportBegan)/.85f)*Mathf.PI)*12;
            rig.Head.rotation=Quaternion.AngleAxis(nod*report,right)*rig.Head.rotation;
        }
        if(crouch>0)rig.Head.rotation=Quaternion.AngleAxis(10*crouch,right)*rig.Head.rotation;
    }
    static float Envelope(float began,float until)
    {
        if(Time.time>=until)return 0;
        return Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Clamp01((Time.time-began)/.18f),Mathf.Clamp01((until-Time.time)/.28f)));
    }
    void PoseArm(int side,Vector3 target,Vector3 bend,float weight)
    {
        Vector3 originalForearm=hands[side].position-elbows[side].position;
        Quaternion originalWrist=hands[side].rotation;
        float reach=(Vector3.Distance(arms[side].position,elbows[side].position)+Vector3.Distance(elbows[side].position,hands[side].position))*.96f;
        target=arms[side].position+Vector3.ClampMagnitude(target-arms[side].position,reach);
        Chapter1IncidentRig.Solve(arms[side],elbows[side],hands[side],Vector3.Lerp(hands[side].position,target,weight),bend);
        hands[side].rotation=Quaternion.FromToRotation(originalForearm,hands[side].position-elbows[side].position)*originalWrist;
    }
    bool Prepare()
    {
        if(prepared)return actor&&rig;
        if(!actor)actor=GetComponent<Chapter2Actor>();
        if(!actor||!actor.Rig||!actor.Rig.animator||!actor.Rig.Hips||!actor.Rig.Head)return false;
        rig=actor.Rig;var animator=rig.animator;
        spine=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Spine,"Spine","Spine1","Waist");
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
        hands[0]=rig.LeftHand;hands[1]=rig.RightHand;
        for(int i=0;i<2;i++)if(!thighs[i]||!knees[i]||!feet[i]||!arms[i]||!elbows[i]||!hands[i])return false;
        bodyRoot=rig.Hips;
        while(bodyRoot&&!rig.Head.IsChildOf(bodyRoot)&&bodyRoot.parent&&bodyRoot.parent!=actor.transform&&bodyRoot.parent!=animator.transform)
            bodyRoot=bodyRoot.parent;
        if(!bodyRoot||!rig.Head.IsChildOf(bodyRoot))return false;
        var bones=new List<Transform>{bodyRoot};
        if(spine&&!bones.Contains(spine))bones.Add(spine);
        if(!bones.Contains(rig.Head))bones.Add(rig.Head);
        for(int i=0;i<2;i++)foreach(var bone in new[]{thighs[i],knees[i],feet[i],arms[i],elbows[i],hands[i]})if(!bones.Contains(bone))bones.Add(bone);
        changedBones=bones.ToArray();originalPositions=new Vector3[changedBones.Length];originalRotations=new Quaternion[changedBones.Length];
        prepared=true;return true;
    }
    void CapturePose()
    {
        for(int i=0;i<changedBones.Length;i++){originalPositions[i]=changedBones[i].localPosition;originalRotations[i]=changedBones[i].localRotation;}
        poseApplied=true;
    }
    void RestorePose()
    {
        if(!poseApplied||changedBones==null)return;
        for(int i=0;i<changedBones.Length;i++)if(changedBones[i])changedBones[i].SetLocalPositionAndRotation(originalPositions[i],originalRotations[i]);
        poseApplied=false;
    }
}
