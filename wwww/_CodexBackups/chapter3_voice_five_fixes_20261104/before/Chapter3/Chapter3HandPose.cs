using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Chapter 3's imported body skins are not CPU-readable in Player. Use the
// original mesh's authored palm axes instead of the shared rig's Y-axis fallback.
// Runs after the chapter's arm/weapon layers; never changes a mesh or body scale.
[DefaultExecutionOrder(4700)]
public sealed class Chapter3HandPose : MonoBehaviour
{
    struct Calibration
    {
        public string mesh;
        public Quaternion left,right;
        public float leftLength,rightLength;
        public Calibration(string mesh,Quaternion left,Quaternion right,float leftLength,float rightLength)
        {this.mesh=mesh;this.left=left;this.right=right;this.leftLength=leftLength;this.rightLength=rightLength;}
    }
    // Exported with the project's existing Chapter2CouncilPoseAuthoring.Basis;
    // each entry is tied to the original skin, not a character's display name.
    static readonly Calibration[] calibrations={
        new Calibration("tripo_node_f32c0c9b",new Quaternion(-.56084818f,.46285868f,.41603875f,.54600644f),new Quaternion(-.50439954f,-.47398746f,-.42254868f,.58512366f),.11767748f,.12778503f),
        new Calibration("tripo_node_9c8dfc1a",new Quaternion(-.54590404f,.43725383f,.40444198f,.58925778f),new Quaternion(-.56059533f,-.47098961f,-.37056637f,.57147372f),.10274562f,.12572995f),
        new Calibration("tripo_node_e2dbffa6",new Quaternion(-.43672144f,.48365858f,.56274676f,.50859106f),new Quaternion(.36652628f,.59782165f,.51166117f,-.49645814f),.11362304f,.12873864f),
        new Calibration("tripo_node_4931ae9c",new Quaternion(-.47184467f,.55826563f,.47339812f,.49152458f),new Quaternion(-.45365411f,-.54667670f,-.45147097f,.53992271f),.09895816f,.12481198f),
        new Calibration("tripo_node_b2cbe21f",new Quaternion(-.20331721f,.58261359f,.69875365f,.36189348f),new Quaternion(-.82899630f,.06695210f,.08620589f,.54849893f),.08910669f,.08719833f),
        new Calibration("tripo_node_3a511942",new Quaternion(-.56656766f,.45974332f,.41626579f,.54254949f),new Quaternion(-.52236658f,-.45308948f,-.41634250f,.59034061f),.11767730f,.12923448f),
        new Calibration("tripo_node_33432f6c",new Quaternion(-.59612566f,.73267215f,.27780035f,.17507915f),new Quaternion(.57361376f,.72172076f,.33008489f,-.20280634f),.10827613f,.11037809f),
    };
    Chapter2Actor actor;
    Chapter1IncidentRig rig;
    Chapter2Rifle rifle;
    Chapter3BawanCooperationPose cooperation;
    Chapter3InjuryPose injury;
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Quaternion[] bases=new Quaternion[2],before=new Quaternion[2];
    readonly float[] lengths=new float[2];
    bool prepared,applied;
    public bool Calibrated=>prepared;
    public string ModelId {get;private set;}
    public float LeftWristAngle=>Angle(0);
    public float RightWristAngle=>Angle(1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterChapter()
    {
        SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;
        for(int i=0;i<SceneManager.sceneCount;i++)Install(SceneManager.GetSceneAt(i));
    }
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode){Install(scene);}
    static void Install(Scene scene)
    {
        if(!scene.isLoaded)return;
        var roots=scene.GetRootGameObjects();bool chapter=false;
        foreach(var root in roots)if(root.GetComponentInChildren<Chapter3Flow>(true)){chapter=true;break;}
        if(!chapter)return;
        // Includes inactive region actors and enemy templates. Their runtime
        // clones inherit this component; no global per-frame scene searches.
        foreach(var root in roots)foreach(var source in root.GetComponentsInChildren<Chapter2Actor>(true))
            if(!source.GetComponent<Chapter3HandPose>())source.gameObject.AddComponent<Chapter3HandPose>();
    }
    public static Chapter3HandPose Attach(Chapter1IncidentRig source)
    {
        if(!source)return null;
        var pose=source.GetComponent<Chapter3HandPose>();
        if(!pose)pose=source.gameObject.AddComponent<Chapter3HandPose>();
        pose.rig=source;return pose;
    }
    bool Prepare()
    {
        if(prepared)return rig&&hands[0]&&hands[1];
        if(!actor)actor=GetComponent<Chapter2Actor>();
        if(!rig)rig=actor?actor.Rig:GetComponent<Chapter1IncidentRig>();
        if(!rig||!rig.animator||!rig.LeftHand||!rig.RightHand)return false;
        var skins=rig.animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Calibration calibration=default;bool found=false;
        foreach(var skin in skins)
        {
            if(!skin.sharedMesh)continue;
            foreach(var candidate in calibrations)
                if(skin.sharedMesh.name==candidate.mesh||skin.sharedMesh.name==candidate.mesh+" incident grip")
                {calibration=candidate;found=true;break;}
            if(found)break;
        }
        if(!found)return false;
        hands[0]=rig.LeftHand;hands[1]=rig.RightHand;
        arms[0]=Bone(HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        arms[1]=Bone(HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbows[0]=Bone(HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        elbows[1]=Bone(HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        if(!arms[0]||!arms[1]||!elbows[0]||!elbows[1])return false;
        bases[0]=calibration.left;bases[1]=calibration.right;
        lengths[0]=calibration.leftLength;lengths[1]=calibration.rightLength;
        ModelId=calibration.mesh;rifle=GetComponent<Chapter2Rifle>();
        prepared=true;return true;
    }
    Transform Bone(HumanBodyBones id,params string[] names)
    {
        if(rig.animator.isHuman){var mapped=rig.animator.GetBoneTransform(id);if(mapped)return mapped;}
        foreach(var candidate in rig.animator.GetComponentsInChildren<Transform>(true))
            foreach(string name in names)if(candidate.name.EndsWith(name,StringComparison.OrdinalIgnoreCase))return candidate;
        return null;
    }
    void LateUpdate()
    {
        applied=false;
        if(!Prepare()||!rig.isActiveAndEnabled)return;
        before[0]=hands[0].localRotation;before[1]=hands[1].localRotation;applied=true;
        if(!cooperation)cooperation=GetComponent<Chapter3BawanCooperationPose>();
        if(!injury)injury=GetComponent<Chapter3InjuryPose>();
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        if(rifle&&rifle.isActiveAndEnabled&&rifle.weapon&&rifle.weapon.gameObject.activeInHierarchy&&!(actor&&actor.fallen))
        {
            Grip(1,rifle.weapon,rifle.weapon.TransformPoint(new Vector3(0,-.045f,.24f)),right-Vector3.up*.7f,false);
            Grip(0,rifle.weapon,rifle.weapon.TransformPoint(new Vector3(0,-.028f,.50f)),-right-Vector3.up,true);
            return;
        }
        if(cooperation&&cooperation.isActiveAndEnabled&&cooperation.CoverWeight>0&&cooperation.HandWeapon)
        {
            var weapon=cooperation.HandWeapon;
            Grip(1,weapon,weapon.TransformPoint(new Vector3(.015f,-.05f,-.28f)),right-Vector3.up,false);
            Grip(0,weapon,weapon.TransformPoint(new Vector3(0,-.035f,.07f)),-right-Vector3.up,true);
            return;
        }
        for(int side=0;side<2;side++)
        {
            Vector3 along=hands[side].position-elbows[side].position;
            // Preserve the pose layer's intended palm frame, replacing only
            // the incorrect imported-mesh basis. This retains signal/strap
            // gestures and wrist roll while fixing both free hands.
            Quaternion intended=hands[side].rotation*rig.PalmFrame(side==0);
            Quaternion corrected=intended*Quaternion.Inverse(bases[side]);
            if(cooperation&&cooperation.isActiveAndEnabled&&cooperation.PushWeight>0)
            {
                // A pushing palm faces the gate, with a bounded wrist bend.
                // The arm layer owns contact position and gesture blending.
                Vector3 fingers=Vector3.RotateTowards(along.normalized,Vector3.up,65*Mathf.Deg2Rad,0);
                Quaternion desired=Orientation(side,fingers,forward);
                hands[side].rotation=Quaternion.Slerp(corrected,desired,cooperation.PushWeight);
            }
            else
            {
                if(injury&&injury.InjuryWeight>0)
                {
                    Vector3 direction=Vector3.RotateTowards(along.normalized,forward,70*Mathf.Deg2Rad,0);
                    if(injury.DeathWeight>0)
                    {
                        // Rest the relaxed palm beside the body; do not point
                        // the now-correct fingers down through the ground.
                        direction=Vector3.ProjectOnPlane(along,Vector3.up);
                        if(direction.sqrMagnitude<.0001f)direction=forward;
                    }
                    else if(injury.SupportWeight>0)
                        direction=Vector3.RotateTowards(along.normalized,forward,55*Mathf.Deg2Rad,0);
                    corrected=Quaternion.Slerp(corrected,Orientation(side,direction,Vector3.down),injury.InjuryWeight);
                }
                hands[side].rotation=corrected;
            }
        }
    }
    void Grip(int side,Transform weapon,Vector3 contact,Vector3 bend,bool support)
    {
        Transform hand=hands[side];
        Vector3 palm=bases[side]*Vector3.forward*(lengths[side]*.34f);
        Vector3 preferred=support?weapon.right:weapon.forward-weapon.up*.35f;
        Vector3 normal=support?weapon.up:-weapon.right;
        // Solve the palm contact, not only the wrist bone. Re-evaluate the
        // forearm after IK so neither hand folds back to reach a fixed grip.
        for(int pass=0;pass<3;pass++)
        {
            Vector3 along=hand.position-elbows[side].position;
            Vector3 fingers=Vector3.RotateTowards(along.normalized,preferred.normalized,55*Mathf.Deg2Rad,0);
            hand.rotation=Orientation(side,fingers,normal);
            Vector3 target=contact-hand.TransformVector(palm);
            float reach=(Vector3.Distance(arms[side].position,elbows[side].position)+Vector3.Distance(elbows[side].position,hand.position))*.98f;
            target=arms[side].position+Vector3.ClampMagnitude(target-arms[side].position,reach);
            Quaternion wrist=hand.rotation;
            Chapter1IncidentRig.Solve(arms[side],elbows[side],hand,target,bend);
            hand.rotation=wrist;
        }
        Vector3 finalAlong=hand.position-elbows[side].position;
        hand.rotation=Orientation(side,Vector3.RotateTowards(finalAlong.normalized,preferred.normalized,55*Mathf.Deg2Rad,0),normal);
    }
    Quaternion Orientation(int side,Vector3 fingers,Vector3 normal)
    {
        if(fingers.sqrMagnitude<.000001f)return hands[side].rotation;
        normal=Vector3.ProjectOnPlane(normal,fingers);
        if(normal.sqrMagnitude<.000001f)normal=Vector3.ProjectOnPlane(rig.Forward,fingers);
        if(normal.sqrMagnitude<.000001f)normal=Vector3.ProjectOnPlane(Vector3.up,fingers);
        return Quaternion.LookRotation(fingers,normal)*Quaternion.Inverse(bases[side]);
    }
    float Angle(int side)=>prepared?Vector3.Angle(hands[side].position-elbows[side].position,hands[side].rotation*(bases[side]*Vector3.forward)):float.NaN;
    void OnDisable()
    {
        if(applied)for(int i=0;i<2;i++)if(hands[i])hands[i].localRotation=before[i];
        applied=false;
    }
}
