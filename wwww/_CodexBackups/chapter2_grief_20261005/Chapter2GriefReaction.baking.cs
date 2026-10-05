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
        var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
        var lengths=new float[2];
        for(int side=0;side<2;side++)
        {
            Transform hand=hands[side];if(!hand)continue;
            var points=new List<Vector3>();
            foreach(var skin in skins)
            {
                var mesh=skin.sharedMesh;if(!mesh || !mesh.isReadable)continue;
                int slot=System.Array.IndexOf(skin.bones,hand);if(slot<0)continue;
                var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;
                bool[] handBones=HandBones(skin,hand);
                for(int i=0;i<vertices.Length && i<weights.Length;i++)
                    if(Weight(weights[i],handBones)>.3f)points.Add(binds[slot].MultiplyPoint3x4(vertices[i]));
            }
            Vector3 center=Vector3.zero;foreach(var point in points)center+=point;
            if(points.Count>0)center/=points.Count;
            Vector3 along=center.sqrMagnitude>.00001f?center.normalized:Vector3.up;
            Vector3 bodyForward=hand.InverseTransformDirection(initialForward);
            Vector3 reference=Vector3.Cross(along,bodyForward)*(side==0?-1:1);
            Vector3 axis=Vector3.ProjectOnPlane(reference,along).normalized;
            if(axis.sqrMagnitude<.001f)axis=Vector3.ProjectOnPlane(Vector3.right,along).normalized;
            Vector3 other=Vector3.Cross(along,axis).normalized;
            float xx=0,xy=0,yy=0;
            foreach(var point in points)
            {Vector3 d=point-center;float x=Vector3.Dot(d,axis),y=Vector3.Dot(d,other);xx+=x*x;xy+=x*y;yy+=y*y;}
            float angle=.5f*Mathf.Atan2(2*xy,xx-yy);
            Vector3 normal=-axis*Mathf.Sin(angle)+other*Mathf.Cos(angle);
            if(Vector3.Dot(normal,reference)<0)normal=-normal;
            // These avatars have missing or partial finger chains. Close the
            // wrist and descendant finger skin together in a private mesh copy.
            palms[side]=Quaternion.LookRotation(along,normal);
            foreach(var point in points)lengths[side]=Mathf.Max(lengths[side],Vector3.Dot(point,along));
        }
        foreach(var skin in skins)
        {
            var original=skin.sharedMesh;if(!original || !original.isReadable)continue;
            var vertices=original.vertices;var normals=original.normals;var weights=original.boneWeights;var binds=original.bindposes;
            var delta=new Vector3[vertices.Length];var normalDelta=new Vector3[vertices.Length];bool changed=false;
            for(int side=0;side<2;side++)
            {
                if(!hands[side])continue;
                int slot=System.Array.IndexOf(skin.bones,hands[side]);if(slot<0 || lengths[side]<.0001f)continue;
                bool[] handBones=HandBones(skin,hands[side]);
                Vector3 along=palms[side]*Vector3.forward,normal=palms[side]*Vector3.up;
                float knuckle=lengths[side]*.4f,radius=lengths[side]*.145f;
                Matrix4x4 bind=binds[slot],inverse=bind.inverse;
                for(int i=0;i<vertices.Length && i<weights.Length;i++)
                {
                    float weight=Weight(weights[i],handBones);if(weight<=0)continue;
                    Vector3 point=bind.MultiplyPoint3x4(vertices[i]);float distance=Vector3.Dot(point,along);
                    if(distance<=knuckle)continue;
                    float curl=Mathf.Min((distance-knuckle)/radius,Mathf.PI*1.1f);
                    Vector3 offset=along*(knuckle+Mathf.Sin(curl)*radius-distance)+normal*(1-Mathf.Cos(curl))*radius;
                    delta[i]+=inverse.MultiplyVector(offset)*weight;changed=true;
                    if(i<normals.Length)
                    {
                        Vector3 localNormal=inverse.transpose.MultiplyVector(normals[i]).normalized;
                        var turn=Quaternion.AngleAxis(curl*Mathf.Rad2Deg,Vector3.Cross(along,normal));
                        normalDelta[i]+=(bind.transpose.MultiplyVector(turn*localNormal).normalized-normals[i])*weight;
                    }
                }
            }
            if(!changed)continue;
            Mesh posed=Instantiate(original);posed.name=original.name+" restrained grief fists";
            int shape=posed.blendShapeCount;
            posed.AddBlendShapeFrame("Chapter2 clenched fists",100,delta,normalDelta,null);
            skin.sharedMesh=posed;
            meshes.Add(new FistMesh{renderer=skin,original=original,posed=posed,index=shape});
        }
    }

    static bool[] HandBones(SkinnedMeshRenderer skin,Transform hand)
    {
        var bones=skin.bones;var result=new bool[bones.Length];
        for(int i=0;i<bones.Length;i++)result[i]=bones[i] && (bones[i]==hand || bones[i].IsChildOf(hand));
        return result;
    }
    static float Weight(BoneWeight value,bool[] included)
    {
        return (included[value.boneIndex0]?value.weight0:0)+(included[value.boneIndex1]?value.weight1:0)
            +(included[value.boneIndex2]?value.weight2:0)+(included[value.boneIndex3]?value.weight3:0);
    }
    void OnDestroy()
    {
        foreach(var mesh in meshes)
        {if(mesh.renderer)mesh.renderer.sharedMesh=mesh.original;if(mesh.posed)Destroy(mesh.posed);}
    }
}
