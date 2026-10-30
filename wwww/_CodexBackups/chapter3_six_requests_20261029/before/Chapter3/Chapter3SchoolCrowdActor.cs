using System;
using System.Collections.Generic;
using UnityEngine;

// Native participants keep the existing model's real bones and skin weights.
// Only transforms, shared skins and the gait rig are copied, never an actor's
// gameplay/voice hierarchy. Other baked guests share a small background rig.
[DefaultExecutionOrder(4600)]
public sealed class Chapter3SchoolCrowdActor : MonoBehaviour
{
    struct MeshKey : IEquatable<MeshKey>
    {
        public Mesh source;
        public Matrix4x4 local;
        public Bounds body;
        public bool Equals(MeshKey other)=>source==other.source&&local.Equals(other.local)&&body.Equals(other.body);
        public override bool Equals(object other)=>other is MeshKey key&&Equals(key);
        public override int GetHashCode()=>source.GetHashCode()^local.GetHashCode()^body.GetHashCode();
    }
    sealed class SharedPose
    {
        public Mesh mesh;
        public int users;
        public Vector3[] samples;
        public BoneWeight[] sampleWeights;
    }
    sealed class Part
    {
        public MeshRenderer original;
        public bool originalEnabled;
        public SkinnedMeshRenderer posed;
        public MeshKey key;
        public SharedPose shared;
    }
    struct CapsuleState
    {
        public CapsuleCollider collider;
        public Vector3 center;
        public float height,radius;
        public int direction;
    }
    static readonly Dictionary<MeshKey,SharedPose> meshes=new Dictionary<MeshKey,SharedPose>();
    static readonly int[] parents={-1,0,1,1,3,4,1,6,7,0,9,10,0,12,13};
    readonly List<Part> parts=new List<Part>();
    readonly List<CapsuleState> capsules=new List<CapsuleState>();
    readonly Transform[] bones=new Transform[15];
    readonly Vector3[] bindPositions=new Vector3[15];
    readonly Matrix4x4[] poseMatrices=new Matrix4x4[15];
    readonly Collider[] overlaps=new Collider[32];
    readonly RaycastHit[] sweepHits=new RaycastHit[32];
    static readonly float[] detours={35f,-35f,65f,-65f,90f,-90f};
    readonly List<MeshRenderer> nativeOriginals=new List<MeshRenderer>();
    readonly List<bool> nativeOriginalEnabled=new List<bool>();
    readonly List<SkinnedMeshRenderer> nativeSkins=new List<SkinnedMeshRenderer>();
    Chapter2Actor nativeTemplate;
    Chapter1IncidentRig nativeRig;
    Chapter3SchoolCrowdActor restrainedGuest;
    Transform nativeRoot,nativeUpperArm,nativeElbow,presentationSpear,presentationKnife;
    Vector3 nativePrevious;
    Transform boneRoot;
    Bounds body;
    float height,phase,walkWeight,lastMovement=-100,fall,strike,startle,fallSide=1;
    bool initialized,animated,afraid,waiting,finalFallPoseReady,holdingAtNeck;
    Vector3 navigationGoal,detourPoint;
    float bestRemaining,progressAt,nextDetour;
    bool navigationReady,detouring,waitingAtObstacle;
    public bool WaitingAtObstacle=>waitingAtObstacle;
    public bool IsRouteCompanion {get;set;}
    public bool FollowingWithdrawal {get;set;}
    public bool IsFallen=>fall>=.999f;
    public bool Posing=>animated;
    public float StartleWeight=>startle;
    public bool HasPoseGeometry=>parts.Count>0||nativeSkins.Count>0;
    public bool UsingOriginalSkin=>nativeRig;
    public Transform RightHand=>nativeRig?nativeRig.RightHand:bones[8];
    public Vector3 NeckPoint=>bones[2]?bones[2].TransformPoint(0,-height*.065f,0):transform.TransformPoint(body.center.x,body.min.y+height*.805f,body.center.z);
    public bool HoldingAtNeck=>holdingAtNeck&&restrainedGuest;
    public float ModelHeight=>height;
    public static int SharedMeshCount=>meshes.Count;

    public static Chapter3SchoolCrowdActor AttachTo(Transform target)
    {
        var actor=target.GetComponent<Chapter3SchoolCrowdActor>();
        if(!actor)actor=target.gameObject.AddComponent<Chapter3SchoolCrowdActor>();
        actor.Initialize();return actor;
    }
    public static Chapter3SchoolCrowdActor AttachTo(Transform target,Chapter2Actor source)
    {
        var actor=target.GetComponent<Chapter3SchoolCrowdActor>();
        if(!actor)actor=target.gameObject.AddComponent<Chapter3SchoolCrowdActor>();
        if(!actor.InitializeNative(source))actor.Initialize();
        return actor;
    }
    public static Chapter3SchoolCrowdActor CreateFrom(Transform template,Transform parent,string name)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);
        root.localScale=template.localScale;
        foreach(var source in template.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=source.GetComponent<MeshRenderer>();
            if(!source.sharedMesh||!renderer)continue;
            Matrix4x4 local=RelativeMatrix(source.transform,template);
            var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;
            part.SetParent(root,false);part.localPosition=local.GetColumn(3);
            part.localRotation=local.rotation;part.localScale=local.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
        var sourceCapsule=template.GetComponent<CapsuleCollider>();
        if(sourceCapsule)
        {
            var capsule=root.gameObject.AddComponent<CapsuleCollider>();
            capsule.center=sourceCapsule.center;capsule.height=sourceCapsule.height;
            capsule.radius=sourceCapsule.radius;capsule.direction=sourceCapsule.direction;
        }
        var templateActor=template.GetComponent<Chapter3SchoolCrowdActor>();
        return templateActor&&templateActor.nativeTemplate?AttachTo(root,templateActor.nativeTemplate):AttachTo(root);
    }
    static Matrix4x4 RelativeMatrix(Transform part,Transform root)
    {
        Matrix4x4 value=Matrix4x4.identity;
        while(part&&part!=root)
        {value=Matrix4x4.TRS(part.localPosition,part.localRotation,part.localScale)*value;part=part.parent;}
        return value;
    }
    bool InitializeNative(Chapter2Actor source)
    {
        if(initialized)return nativeRig;
        if(!source)return false;
        var sourceAnimator=source.GetComponentInChildren<Animator>(true);
        var sourceSkins=source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if(!sourceAnimator||sourceSkins.Length==0)return false;
        Bounds donorBounds=default;bool donorFirst=true;
        foreach(var skin in sourceSkins)if(skin.sharedMesh&&skin.enabled)
        {if(donorFirst){donorBounds=skin.bounds;donorFirst=false;}else donorBounds.Encapsulate(skin.bounds);}
        Vector3 donorScale=source.transform.lossyScale,ownerScale=transform.lossyScale;
        if(donorFirst||donorBounds.size.y<.01f||Mathf.Abs(ownerScale.x)<.0001f
            ||Mathf.Abs(ownerScale.y)<.0001f||Mathf.Abs(ownerScale.z)<.0001f)return false;
        bool first=true;
        foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(!filter.sharedMesh||!renderer)continue;
            nativeOriginals.Add(renderer);nativeOriginalEnabled.Add(renderer.enabled);
            Matrix4x4 matrix=RelativeMatrix(filter.transform,transform);Bounds bounds=filter.sharedMesh.bounds;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 point=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,
                    new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                if(first){body=new Bounds(point,Vector3.zero);first=false;}else body.Encapsulate(point);
            }
        }
        if(first)return false;
        initialized=true;nativeTemplate=source;height=body.size.y;
        if(!GetComponent<Chapter3Bystander>())gameObject.AddComponent<Chapter3Bystander>();

        // Preserve this authored spectator's height and proportions. The child
        // model has the donor's original hierarchy/bind transforms, uniformly
        // fitted to the existing baked silhouette; the crowd root is untouched.
        float fit=height*Mathf.Abs(transform.lossyScale.y)/Mathf.Max(.01f,donorBounds.size.y);
        nativeRoot=new GameObject("Original crowd skeleton").transform;nativeRoot.SetParent(transform,false);
        nativeRoot.localScale=new Vector3(donorScale.x/ownerScale.x,donorScale.y/ownerScale.y,donorScale.z/ownerScale.z)*fit;
        nativeRoot.rotation=source.transform.rotation;
        var cloned=new Dictionary<Transform,Transform>{{source.transform,nativeRoot}};
        Transform CopyBone(Transform original)
        {
            if(!original)return null;
            if(cloned.TryGetValue(original,out var copy))return copy;
            if(!original.IsChildOf(source.transform))return null;
            var parent=CopyBone(original.parent);
            copy=new GameObject(original.name).transform;copy.SetParent(parent,false);
            copy.localPosition=original.localPosition;copy.localRotation=original.localRotation;copy.localScale=original.localScale;
            cloned.Add(original,copy);return copy;
        }
        var animatorTransform=CopyBone(sourceAnimator.transform);
        // Avatar mappings may include a toe/finger that has no weighted vertex.
        if(sourceAnimator.isHuman)
            for(int bone=0;bone<(int)HumanBodyBones.LastBone;bone++)CopyBone(sourceAnimator.GetBoneTransform((HumanBodyBones)bone));
        foreach(var original in sourceSkins)
        {
            if(!original.sharedMesh||!original.enabled)continue;
            var skin=CopyBone(original.transform).gameObject.AddComponent<SkinnedMeshRenderer>();
            var sourceBones=original.bones;var copiedBones=new Transform[sourceBones.Length];
            for(int i=0;i<sourceBones.Length;i++)copiedBones[i]=CopyBone(sourceBones[i]);
            skin.sharedMesh=original.sharedMesh;skin.sharedMaterials=original.sharedMaterials;
            skin.bones=copiedBones;skin.rootBone=CopyBone(original.rootBone);skin.localBounds=original.localBounds;
            skin.quality=original.quality;skin.shadowCastingMode=original.shadowCastingMode;skin.receiveShadows=original.receiveShadows;
            for(int i=0;i<original.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,original.GetBlendShapeWeight(i));
            nativeSkins.Add(skin);
        }
        var animator=animatorTransform.gameObject.AddComponent<Animator>();
        animator.avatar=sourceAnimator.avatar;animator.applyRootMotion=false;
        // No animation controller, speech, rifle, reaction, or story component.
        // The existing non-police gait reads the original weights for its soles
        // and hands; it does not create the police grip mesh/material variants.
        nativeRig=nativeRoot.gameObject.AddComponent<Chapter1IncidentRig>();
        nativeRig.Initialize(animator,false);nativeRig.smoothLocomotion=true;
        nativeRig.batonVisible=false;nativeRig.Face(transform.forward);nativeRig.Ground(0);
        Chapter3HandPose.Attach(nativeRig);
        nativeUpperArm=FindNativeBone(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        nativeElbow=FindNativeBone(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        nativePrevious=transform.position;
        foreach(var capsule in GetComponents<CapsuleCollider>())
            capsules.Add(new CapsuleState{collider=capsule,center=capsule.center,height=capsule.height,radius=capsule.radius,direction=capsule.direction});
        SetAnimated(false);return true;
    }
    static Transform FindNativeBone(Animator animator,HumanBodyBones bone,params string[] names)
    {
        if(animator.isHuman){var mapped=animator.GetBoneTransform(bone);if(mapped)return mapped;}
        foreach(var candidate in animator.GetComponentsInChildren<Transform>(true))
            foreach(string name in names)if(candidate.name.EndsWith(name,StringComparison.OrdinalIgnoreCase))return candidate;
        return null;
    }
    public void AttachPresentationSpear(Transform spear)
    {
        presentationSpear=spear;spear.SetParent(transform,false);
        float worldHeight=height*Mathf.Abs(transform.lossyScale.y);Vector3 scale=transform.lossyScale;
        spear.localScale=new Vector3(worldHeight*.015f/scale.x,worldHeight*.50f/scale.y,worldHeight*.015f/scale.z);
    }
    public void AttachPresentationKnife(Transform knife)
    {
        presentationKnife=knife;knife.SetParent(transform,false);
        // Keep the existing handheld knife's world size regardless of the
        // crowd template's authored scale.
        var scale=transform.lossyScale;knife.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
    }
    void Update()
    {
        if(!nativeRig||!animated)return;
        Vector3 delta=Vector3.ProjectOnPlane(transform.position-nativePrevious,Vector3.up);nativePrevious=transform.position;
        nativeRig.walking=delta.sqrMagnitude>.00000025f;nativeRig.Face(transform.forward);nativeRig.Ground(0);
        nativeRig.strike=strike>0;nativeRig.strikeProgress=strike;
    }
    void Initialize()
    {
        if(initialized)return;
        initialized=true;
        if(!GetComponent<Chapter3Bystander>())gameObject.AddComponent<Chapter3Bystander>();
        var filters=GetComponentsInChildren<MeshFilter>(true);bool first=true;
        foreach(var filter in filters)
        {
            if(!filter.sharedMesh||!filter.GetComponent<MeshRenderer>())continue;
            Matrix4x4 matrix=RelativeMatrix(filter.transform,transform);Bounds bounds=filter.sharedMesh.bounds;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 point=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,
                    new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                if(first){body=new Bounds(point,Vector3.zero);first=false;}else body.Encapsulate(point);
            }
        }
        if(first)return;
        height=body.size.y;BuildBones();
        foreach(var filter in filters)
        {
            var source=filter.GetComponent<MeshRenderer>();
            if(!source||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
            Matrix4x4 local=RelativeMatrix(filter.transform,transform);
            var key=new MeshKey{source=filter.sharedMesh,local=local,body=body};
            if(!meshes.TryGetValue(key,out var shared)){shared=WeightMesh(key);meshes.Add(key,shared);}
            shared.users++;
            var posed=new GameObject("Crowd pose · "+filter.name).AddComponent<SkinnedMeshRenderer>();
            posed.transform.SetParent(transform,false);posed.transform.localPosition=local.GetColumn(3);
            posed.transform.localRotation=local.rotation;posed.transform.localScale=local.lossyScale;
            posed.sharedMesh=shared.mesh;posed.sharedMaterials=source.sharedMaterials;
            posed.bones=bones;posed.rootBone=bones[0];posed.quality=SkinQuality.Bone2;
            posed.shadowCastingMode=source.shadowCastingMode;posed.receiveShadows=source.receiveShadows;
            // Fall and raised arms extend outside the standing mesh bounds.
            posed.localBounds=new Bounds(local.inverse.MultiplyPoint3x4(body.center),Vector3.one*height*2.8f);
            posed.enabled=false;
            parts.Add(new Part{original=source,originalEnabled=source.enabled,posed=posed,key=key,shared=shared});
        }
        foreach(var capsule in GetComponents<CapsuleCollider>())
            capsules.Add(new CapsuleState{collider=capsule,center=capsule.center,height=capsule.height,radius=capsule.radius,direction=capsule.direction});
    }
    void BuildBones()
    {
        boneRoot=new GameObject("Crowd pose bones").transform;boneRoot.SetParent(transform,false);
        Vector3 center=new Vector3(body.center.x,body.min.y,body.center.z);
        Vector3 P(float x,float y)=>center+new Vector3(x*height,y*height,0);
        bindPositions[0]=P(0,.53f);bindPositions[1]=P(0,.66f);bindPositions[2]=P(0,.87f);
        for(int side=0;side<2;side++)
        {
            float sign=side==0?-1:1;int arm=3+side*3,leg=9+side*3;
            bindPositions[arm]=P(sign*.16f,.77f);bindPositions[arm+1]=P(sign*.20f,.60f);bindPositions[arm+2]=P(sign*.23f,.43f);
            bindPositions[leg]=P(sign*.075f,.50f);bindPositions[leg+1]=P(sign*.075f,.28f);bindPositions[leg+2]=P(sign*.075f,.055f);
        }
        for(int i=0;i<bones.Length;i++)
        {
            bones[i]=new GameObject("Crowd bone "+i).transform;
            bones[i].SetParent(parents[i]<0?boneRoot:bones[parents[i]],false);
            bones[i].localPosition=bindPositions[i]-(parents[i]<0?Vector3.zero:bindPositions[parents[i]]);
        }
    }
    SharedPose WeightMesh(MeshKey key)
    {
        var mesh=Instantiate(key.source);mesh.name=key.source.name+" · shared crowd pose";
        var vertices=mesh.vertices;var weights=new BoneWeight[vertices.Length];
        for(int i=0;i<vertices.Length;i++)weights[i]=Weight(key.local.MultiplyPoint3x4(vertices[i]));
        var bindposes=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++)bindposes[i]=Matrix4x4.Translate(-bindPositions[i])*key.local;
        mesh.bindposes=bindposes;mesh.boneWeights=weights;
        int count=Mathf.Min(vertices.Length,1536);var samples=new Vector3[count];var sampleWeights=new BoneWeight[count];
        for(int i=0;i<count;i++){int index=i*(vertices.Length-1)/Mathf.Max(1,count-1);samples[i]=vertices[index];sampleWeights[i]=weights[index];}
        return new SharedPose{mesh=mesh,samples=samples,sampleWeights=sampleWeights};
    }
    BoneWeight Weight(Vector3 point)
    {
        float y=(point.y-body.min.y)/height,x=(point.x-body.center.x)/height;
        int arm=x<0?3:6,leg=x<0?9:12;
        // Soft two-bone joints preserve the silhouette of the existing baked
        // people. No changes to vertex positions, proportions, or root scale.
        if(y<.51f&&Mathf.Abs(x)<.18f)
        {
            if(y<.18f)return Blend(leg+2,leg+1,Mathf.InverseLerp(.08f,.18f,y));
            if(y<.35f)return Blend(leg+1,leg,Mathf.InverseLerp(.25f,.35f,y));
            return Blend(leg,0,Mathf.InverseLerp(.46f,.53f,y));
        }
        if(Mathf.Abs(x)>.155f&&y>.37f&&y<.84f)
        {
            if(y<.52f)return Blend(arm+2,arm+1,Mathf.InverseLerp(.43f,.53f,y));
            return Blend(arm+1,arm,Mathf.InverseLerp(.60f,.70f,y));
        }
        return y>.77f?Blend(1,2,Mathf.InverseLerp(.77f,.86f,y)):Blend(0,1,Mathf.InverseLerp(.52f,.66f,y));
    }
    static BoneWeight Blend(int a,int b,float weight)=>new BoneWeight{boneIndex0=a,boneIndex1=b,weight0=1-weight,weight1=weight};
    public void SetAnimated(bool value)
    {
        Initialize();animated=value;
        foreach(var part in parts){if(part.original)part.original.enabled=part.originalEnabled&&!value;if(part.posed)part.posed.enabled=part.originalEnabled&&value;}
        for(int i=0;i<nativeOriginals.Count;i++)if(nativeOriginals[i])nativeOriginals[i].enabled=nativeOriginalEnabled[i]&&!value;
        foreach(var skin in nativeSkins)if(skin)skin.enabled=value;
        if(nativeRig){nativeRig.enabled=value;nativeRig.walking=false;nativeRig.strike=false;nativePrevious=transform.position;}
    }
    public void SetAfraid(bool value){afraid=value;}
    public void SetStartled(float weight){startle=Mathf.Clamp01(weight);}
    public void SetWaiting(bool value){waiting=value;}
    public void SetFall(float progress){fall=Mathf.Clamp01(progress);if(fall<1)finalFallPoseReady=false;}
    public void SetStrike(float progress){strike=Mathf.Clamp01(progress);}
    public void SetFallSide(float side){fallSide=side<0?-1:1;}
    public void HoldAtNeck(Chapter3SchoolCrowdActor guest,bool posing)
    {restrainedGuest=guest;holdingAtNeck=posing;}
    public void ResetPose()
    {
        phase=walkWeight=fall=strike=startle=0;lastMovement=-100;afraid=waiting=finalFallPoseReady=false;SetAnimated(false);
        navigationReady=detouring=waitingAtObstacle=false;nextDetour=0;
        restrainedGuest=null;holdingAtNeck=false;
        foreach(var saved in capsules)if(saved.collider)
        {saved.collider.center=saved.center;saved.collider.height=saved.height;saved.collider.radius=saved.radius;saved.collider.direction=saved.direction;}
    }
    public void Face(Vector3 point,float degreesPerSecond=360)
    {
        Vector3 delta=point-transform.position;delta.y=0;
        if(delta.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),degreesPerSecond*Time.deltaTime);
    }
    public bool MoveTowards(Vector3 target,float speed,bool faceTravel=true,float arrivalRadius=.2f)
    {
        target.y=transform.position.y;Vector3 delta=target-transform.position;
        float arrivalSquared=arrivalRadius*arrivalRadius;
        if(delta.sqrMagnitude<arrivalSquared){detouring=waitingAtObstacle=false;return true;}
        if(!navigationReady||(target-navigationGoal).sqrMagnitude>.64f)
        {
            navigationReady=true;navigationGoal=target;detouring=waitingAtObstacle=false;
            bestRemaining=delta.magnitude;progressAt=Time.time;nextDetour=0;
        }
        if(delta.magnitude<bestRemaining-.2f){bestRemaining=delta.magnitude;progressAt=Time.time;waitingAtObstacle=false;}
        // A frightened guest may shelter at a blocked bench. Never perform an
        // endless left/right walking loop when the intended exit is unreachable.
        if(!IsRouteCompanion&&Time.time-progressAt>5)waitingAtObstacle=true;
        if(waitingAtObstacle)
        {
            if(!Clear(target))return false;
            waitingAtObstacle=false;detouring=false;progressAt=Time.time;
        }
        float distance=Mathf.Min(delta.magnitude,Mathf.Max(0,speed)*Time.deltaTime);
        if(detouring&&(detourPoint-transform.position).sqrMagnitude<.04f)detouring=false;
        if(detouring)distance=Mathf.Min(distance,Vector3.Distance(transform.position,detourPoint));
        Vector3 direction=(detouring?detourPoint-transform.position:delta).normalized;
        Vector3 next=transform.position+direction*distance;
        if(!Clear(next))
        {
            // Retain a chosen bypass until it is reached. Re-evaluating +/-35
            // degrees every frame used to turn actors back and forth in place.
            if(Time.time<nextDetour)return false;
            nextDetour=Time.time+.65f;
            bool found=false;
            foreach(float angle in detours)
            {
                direction=Quaternion.Euler(0,angle,0)*delta.normalized;
                var candidate=transform.position+direction*Mathf.Min(1.6f,delta.magnitude);
                if(Clear(candidate)){detourPoint=candidate;detouring=true;next=Vector3.MoveTowards(transform.position,candidate,distance);found=true;break;}
            }
            if(!found)return false;
        }
        transform.position=next;if(faceTravel)Face(next+direction);lastMovement=Time.time;
        phase+=distance/Mathf.Max(.4f,height*Mathf.Abs(transform.lossyScale.y)*.80f)*Mathf.PI*2;
        return (target-next).sqrMagnitude<arrivalSquared;
    }
    // Follow the shared static-obstacle planner, but still yield to actual
    // people at each proposed step. Never choose a new side every frame.
    internal bool StepAlongWithdrawal(Vector3 next)
    {
        if(!FollowingWithdrawal||!Clear(next))return false;
        Vector3 delta=next-transform.position;float distance=delta.magnitude;
        if(distance<.00001f)return true;
        transform.position=next;Face(next+delta.normalized);lastMovement=Time.time;
        phase+=distance/Mathf.Max(.4f,height*Mathf.Abs(transform.lossyScale.y)*.80f)*Mathf.PI*2;
        return true;
    }
    bool Clear(Vector3 next)
    {
        int count=Physics.OverlapCapsuleNonAlloc(next+Vector3.up*.35f,next+Vector3.up*1.25f,.23f,overlaps,~0,QueryTriggerInteraction.Ignore);
        if(count==overlaps.Length)return false;
        for(int i=0;i<count;i++)if(Obstacle(overlaps[i],next))return false;
        Vector3 delta=next-transform.position;
        if(delta.sqrMagnitude<.000001f)return true;
        count=Physics.CapsuleCastNonAlloc(transform.position+Vector3.up*.35f,transform.position+Vector3.up*1.25f,.23f,
            delta.normalized,sweepHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(count==sweepHits.Length)return false;
        for(int i=0;i<count;i++)if(Obstacle(sweepHits[i].collider,next))return false;
        return true;
    }
    bool Obstacle(Collider collider,Vector3 next)
    {
        if(collider.transform.IsChildOf(transform)||collider.bounds.max.y<.28f)return false;
        var bystander=collider.GetComponentInParent<Chapter3Bystander>();
        var actor=collider.GetComponentInParent<Chapter2Actor>();
        if(bystander||actor)
        {
            // Route companions follow the leader's constrained corridor.
            // Background crowds instead yield to people, while permitting an
            // already touching pair to separate rather than lock together.
            if(IsRouteCompanion)return false;
            var other=bystander?bystander.transform:actor.transform;
            float before=Vector3.ProjectOnPlane(transform.position-other.position,Vector3.up).sqrMagnitude;
            float after=Vector3.ProjectOnPlane(next-other.position,Vector3.up).sqrMagnitude;
            float clearance=bystander?.72f:.85f;
            // A controlled, adult-only presentation pair can stand closer.
            // This exception never applies to fences, other guests or leaders.
            if(restrainedGuest&&other==restrainedGuest.transform)clearance=.40f;
            return after<clearance*clearance&&after<=before+.00001f;
        }
        // Yield to a player ahead. An already overlapping actor may step away,
        // but never walk deeper through the player to reach the next waypoint.
        if(collider is CharacterController&&
            (next-collider.transform.position).sqrMagnitude>(transform.position-collider.transform.position).sqrMagnitude)return false;
        return true;
    }
    void LateUpdate()
    {
        if(!animated)return;
        if(nativeRig)
        {
            if(startle>0&&nativeRig.Head)
                nativeRig.Head.rotation=Quaternion.AngleAxis(-8*startle,Vector3.Cross(Vector3.up,nativeRig.Forward))*nativeRig.Head.rotation;
            // IK rotates the real upper arm/forearm; the original skin weights
            // keep the tunic and feet attached to their correct bone chains.
            if(HoldingAtNeck&&nativeUpperArm&&nativeElbow&&nativeRig.RightHand)
            {
                Vector3 target=restrainedGuest.NeckPoint+restrainedGuest.transform.right*.19f+restrainedGuest.transform.forward*.04f;
                Transform hand=nativeRig.RightHand;Vector3 before=hand.position-nativeElbow.position;Quaternion wrist=hand.rotation;
                Chapter1IncidentRig.Solve(nativeUpperArm,nativeElbow,hand,target,transform.right+Vector3.down*.25f);
                hand.rotation=Quaternion.FromToRotation(before,hand.position-nativeElbow.position)*wrist;
            }
            else if((waiting||afraid||startle>0)&&strike<=0&&nativeUpperArm&&nativeElbow&&nativeRig.RightHand&&nativeRig.Head)
            {
                Vector3 right=Vector3.Cross(Vector3.up,nativeRig.Forward);
                Vector3 target=nativeRig.Head.position+right*nativeRig.Height*.18f+nativeRig.Forward*nativeRig.Height*.10f-Vector3.up*nativeRig.Height*.08f;
                Transform hand=nativeRig.RightHand;Vector3 before=hand.position-nativeElbow.position;Quaternion wrist=hand.rotation;
                float reach=(Vector3.Distance(nativeUpperArm.position,nativeElbow.position)+Vector3.Distance(nativeElbow.position,hand.position))*.94f;
                target=nativeUpperArm.position+Vector3.ClampMagnitude(target-nativeUpperArm.position,reach);
                if(!waiting&&!afraid)target=Vector3.Lerp(hand.position,target,startle);
                Chapter1IncidentRig.Solve(nativeUpperArm,nativeElbow,hand,target,right+nativeRig.Forward*.2f);
                hand.rotation=Quaternion.FromToRotation(before,hand.position-nativeElbow.position)*wrist;
            }
            if(presentationSpear&&nativeRig.RightHand)
            {
                Vector3 direction=Vector3.Slerp(Vector3.up,nativeRig.Forward,strike>0?.3f+.35f*Mathf.Sin(strike*Mathf.PI):.18f);
                presentationSpear.SetPositionAndRotation(nativeRig.RightHand.position+direction*nativeRig.Height*.20f,Quaternion.FromToRotation(Vector3.up,direction));
            }
            if(presentationKnife&&nativeRig.RightHand)
            {
                float cut=strike>0?Mathf.Sin(strike*Mathf.PI):0;
                Vector3 direction=HoldingAtNeck?-restrainedGuest.transform.right:Vector3.Slerp(Vector3.up,nativeRig.Forward*.7f-Vector3.up*.7f,cut);
                Vector3 bladeNormal=HoldingAtNeck?restrainedGuest.transform.forward:Vector3.Cross(Vector3.up,nativeRig.Forward);
                presentationKnife.SetPositionAndRotation(nativeRig.RightHand.position,Quaternion.LookRotation(bladeNormal,direction));
            }
            return;
        }
        // Finished background casualties retain the already-verified final
        // bones/collider; no repeated vertex sampling during the forest ending.
        if(!boneRoot||finalFallPoseReady)return;
        for(int i=0;i<bones.Length;i++){bones[i].localRotation=Quaternion.identity;bones[i].localPosition=bindPositions[i]-(parents[i]<0?Vector3.zero:bindPositions[parents[i]]);}
        walkWeight=Mathf.MoveTowards(walkWeight,Time.time-lastMovement<.08f&&fall<=0?1:0,Time.deltaTime*7);
        float stride=Mathf.Sin(phase)*walkWeight;
        bones[0].localPosition+=Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*height*.012f*walkWeight);
        bones[1].localRotation=Quaternion.Euler(9*walkWeight+(afraid?8:0)-6*startle,0,0);
        bones[2].localRotation=Quaternion.Euler(-8*startle,0,0);
        for(int side=0;side<2;side++)
        {
            int arm=3+side*3,leg=9+side*3;float sign=side==0?-1:1,step=stride*sign;
            bones[leg].localRotation=Quaternion.Euler(step*30,0,0);
            bones[leg+1].localRotation=Quaternion.Euler(-Mathf.Max(0,-step)*48,0,0);
            bones[leg+2].localRotation=Quaternion.Euler(-step*12,0,0);
            bones[arm].localRotation=Quaternion.Euler(-step*24-(afraid?48:0)-25*startle,0,sign*(afraid?10:3));
            bones[arm+1].localRotation=Quaternion.Euler(-25-(afraid?35:0)-20*startle,0,0);
        }
        if(waiting)
        {bones[6].localRotation=Quaternion.Euler(-105,0,15);bones[7].localRotation=Quaternion.Euler(-25,0,-10);}
        if(strike>0)
        {bones[6].localRotation=Quaternion.Euler(-40-70*Mathf.Sin(strike*Mathf.PI),0,12);bones[7].localRotation=Quaternion.Euler(-30+25*Mathf.Sin(strike*Mathf.PI),0,0);}
        if(fall>0)PoseFall();
    }
    void PoseFall()
    {
        float collapse=Mathf.SmoothStep(0,1,fall),bend=Mathf.Sin(collapse*Mathf.PI);
        bones[0].localPosition=bindPositions[0]-Vector3.up*(height*.39f*collapse);
        bones[0].localRotation=Quaternion.Euler(-7*bend,0,87*collapse*fallSide);
        bones[1].localRotation=Quaternion.Euler(18*bend,0,0);
        for(int side=0;side<2;side++)
        {
            int leg=9+side*3,arm=3+side*3;
            bones[leg].localRotation=Quaternion.Euler(22*bend+side*5*collapse,0,0);
            bones[leg+1].localRotation=Quaternion.Euler(-42*bend-10*collapse,0,0);
            bones[arm].localRotation=Quaternion.Euler(-55*bend-14*collapse,0,side==0?-12:18);
            bones[arm+1].localRotation=Quaternion.Euler(-40*bend-15*collapse,0,0);
        }
        // Sample the shared geometry instead of repeatedly baking a high-poly
        // character. This keeps the non-graphic collapse resting on the floor.
        for(int i=0;i<bones.Length;i++)poseMatrices[i]=RelativeMatrix(bones[i],transform)*Matrix4x4.Translate(-bindPositions[i]);
        float lowest=float.PositiveInfinity;
        foreach(var part in parts)
            for(int i=0;i<part.shared.samples.Length;i++)
            {
                Vector3 vertex=part.key.local.MultiplyPoint3x4(part.shared.samples[i]);BoneWeight weight=part.shared.sampleWeights[i];
                Vector3 posed=poseMatrices[weight.boneIndex0].MultiplyPoint3x4(vertex)*weight.weight0+poseMatrices[weight.boneIndex1].MultiplyPoint3x4(vertex)*weight.weight1;
                lowest=Mathf.Min(lowest,posed.y);
            }
        if(!float.IsPositiveInfinity(lowest))bones[0].localPosition+=Vector3.up*Mathf.Max(0,body.min.y+height*.012f-lowest);
        if(fall>=.95f)
            foreach(var saved in capsules)if(saved.collider)
            {saved.collider.direction=0;saved.collider.center=new Vector3(body.center.x-height*.02f*fallSide,body.min.y+height*.17f,body.center.z);saved.collider.height=height;saved.collider.radius=height*.14f;}
        if(fall>=1)finalFallPoseReady=true;
    }
    void OnDisable(){if(initialized)ResetPose();}
    void OnDestroy()
    {
        for(int i=0;i<nativeOriginals.Count;i++)if(nativeOriginals[i])nativeOriginals[i].enabled=nativeOriginalEnabled[i];
        if(nativeRoot)Destroy(nativeRoot.gameObject);
        if(presentationSpear)Destroy(presentationSpear.gameObject);
        if(presentationKnife)Destroy(presentationKnife.gameObject);
        foreach(var part in parts)
        {
            if(part.original)part.original.enabled=part.originalEnabled;
            if(part.posed)Destroy(part.posed.gameObject);
            if(--part.shared.users==0){meshes.Remove(part.key);if(part.shared.mesh)Destroy(part.shared.mesh);}
        }
        if(boneRoot)Destroy(boneRoot.gameObject);
    }
}
