using System;
using System.Collections.Generic;
using UnityEngine;

// Native participants keep the existing model's real bones and skin weights.
// Only transforms, shared skins and the gait rig are copied, never an actor's
// gameplay/voice hierarchy. Other baked guests share a small background rig.
[DefaultExecutionOrder(5000)]
public sealed class Chapter3SchoolCrowdActor : MonoBehaviour
{
    internal const float MovementRadius=.23f,MovementBottom=.35f,MovementTop=1.25f;
    enum GeometryProfile { Legacy,SeediqChild,CampusBoy,CampusGirl,CampusTeacher }
    enum ClothPart : byte { None,Upper,Skirt,Torso }
    struct MeshKey : IEquatable<MeshKey>
    {
        public Mesh source;
        public Matrix4x4 local;
        public Bounds body;
        public GeometryProfile profile;
        public string clothing;
        public bool Equals(MeshKey other)=>source==other.source&&local.Equals(other.local)&&body.Equals(other.body)
            &&profile==other.profile&&clothing==other.clothing;
        public override bool Equals(object other)=>other is MeshKey key&&Equals(key);
        public override int GetHashCode()=>source.GetHashCode()^local.GetHashCode()^body.GetHashCode()
            ^(int)profile^(clothing==null?0:clothing.GetHashCode());
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
        public MeshKey detailKey;
        public SharedPose detail;
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
    Chapter3SchoolAmbient ambient;
    bool longGarment,skirtGarment,teacherGarment;
    Chapter1IncidentRig nativeRig;
    Chapter3SchoolCrowdActor restrainedGuest;
    Transform nativeRoot,nativeUpperArm,nativeElbow,presentationSpear,presentationKnife;
    Transform meleeSpine;
    Transform[] nativeShelterBones;
    readonly Vector3[] shelterFeet=new Vector3[2];
    readonly Quaternion[] shelterFootRotations=new Quaternion[2];
    float meleeGuard;
    int meleeVariant;
    public void SetMeleePose(float guard,int variant){meleeGuard=guard;meleeVariant=variant;}
    Vector3 nativePrevious;
    Transform boneRoot;
    Bounds body;
    float height,phase,walkWeight,lastMovement=-100,fall,strike,startle,fallSide=1;
    bool initialized,animated,afraid,waiting,finalFallPoseReady,holdingAtNeck;
    bool cowering;
    float cowerWeight;
    Mesh groundProbe;
    readonly List<Vector3> groundProbeVertices=new List<Vector3>();
    int calibratedGroundMesh,calibratedGroundPose;
    float calibratedGroundSurface,renderedGroundOffset;
    bool groundCalibrated;
    public void SetCowering(bool value){cowering=value;}
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
    public static Chapter3SchoolCrowdActor CreateFrom(Transform template,Transform parent,string name,Chapter2Actor nativeSource=null,float bakedYaw=0)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);
        root.localScale=template.localScale;
        Quaternion heading=Quaternion.Euler(0,bakedYaw,0);
        foreach(var source in template.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=source.GetComponent<MeshRenderer>();
            if(!source.sharedMesh||!renderer)continue;
            Matrix4x4 local=RelativeMatrix(source.transform,template);
            var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;
            part.SetParent(root,false);part.localPosition=heading*(Vector3)local.GetColumn(3);
            part.localRotation=heading*local.rotation;part.localScale=local.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
        var sourceCapsule=template.GetComponent<CapsuleCollider>();
        if(sourceCapsule)
        {
            var capsule=root.gameObject.AddComponent<CapsuleCollider>();
            capsule.center=heading*sourceCapsule.center;capsule.height=sourceCapsule.height;
            capsule.radius=sourceCapsule.radius;capsule.direction=sourceCapsule.direction;
        }
        var templateActor=template.GetComponent<Chapter3SchoolCrowdActor>();
        return nativeSource?AttachTo(root,nativeSource):templateActor&&templateActor.nativeTemplate?AttachTo(root,templateActor.nativeTemplate):AttachTo(root);
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
        longGarment=name.Contains("日籍女教師")||name.Contains("婦人")||name.Contains("女性");
        skirtGarment=name.Contains("日籍女學童");
        ambient=GetComponentInParent<Chapter3SchoolAmbient>();
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
        nativeRig.batonVisible=false;nativeRig.Face(transform.forward);nativeRig.Ground(ambient?ambient.CorridorGroundHeight(transform.position):0);
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
        nativeRig.walking=delta.sqrMagnitude>.00000025f;nativeRig.Face(transform.forward);nativeRig.Ground(ambient?ambient.CorridorGroundHeight(transform.position):0);
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
        height=body.size.y;
        ambient=GetComponentInParent<Chapter3SchoolAmbient>();
        longGarment=name.Contains("日籍女教師")||name.Contains("婦人")||name.Contains("女性");
        skirtGarment=name.Contains("日籍女學童");
        teacherGarment=name.Contains("日籍男教師");
        BuildBones();
        foreach(var filter in filters)
        {
            var source=filter.GetComponent<MeshRenderer>();
            if(!source||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
            Matrix4x4 local=RelativeMatrix(filter.transform,transform);
            var profile=Profile(filter.sharedMesh);
            var key=new MeshKey{source=filter.sharedMesh,local=local,body=body,profile=profile,
                clothing=ClothingLayout(filter.sharedMesh,source.sharedMaterials,profile)};
            if(!meshes.TryGetValue(key,out var shared)){shared=WeightMesh(key);meshes.Add(key,shared);}
            shared.users++;
            var posed=new GameObject("Crowd pose · "+filter.name).AddComponent<SkinnedMeshRenderer>();
            posed.transform.SetParent(transform,false);posed.transform.localPosition=local.GetColumn(3);
            posed.transform.localRotation=local.rotation;posed.transform.localScale=local.lossyScale;
            posed.sharedMesh=shared.mesh;posed.sharedMaterials=source.sharedMaterials;
            posed.bones=bones;posed.rootBone=bones[0];posed.quality=IsCampus(profile)?SkinQuality.Bone4:SkinQuality.Bone2;
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
            if(teacherGarment)
            {
                // Measured jacket shoulder, elbow and cuff locations. The old
                // shared pupil wrist sat in the middle of the teacher's palm.
                bindPositions[arm]=P(sign*.146f,.798f);
                bindPositions[arm+1]=P(sign*.193f,.674f);
                bindPositions[arm+2]=P(sign*.241f,.504f);
            }
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
        var clothing=IsCampus(key.profile)?ClassifyClothing(key,vertices):null;
        for(int i=0;i<vertices.Length;i++)
        {
            var point=key.local.MultiplyPoint3x4(vertices[i]);
            weights[i]=clothing==null?Weight(point,key.profile):CampusWeight(point,clothing[i],key.profile);
        }
        var bindposes=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++)bindposes[i]=Matrix4x4.Translate(-bindPositions[i])*key.local;
        mesh.bindposes=bindposes;mesh.boneWeights=weights;
        int count=Mathf.Min(vertices.Length,1536);var samples=new Vector3[count];var sampleWeights=new BoneWeight[count];
        for(int i=0;i<count;i++){int index=i*(vertices.Length-1)/Mathf.Max(1,count-1);samples[i]=vertices[index];sampleWeights[i]=weights[index];}
        return new SharedPose{mesh=mesh,samples=samples,sampleWeights=sampleWeights};
    }
    internal Mesh PoseSource(SkinnedMeshRenderer skin)
    {foreach(var part in parts)if(part.posed==skin)return part.key.source;return null;}
    internal Mesh DistantPose(SkinnedMeshRenderer skin,Mesh source)
    {
        if(!source||!source.isReadable)return null;
        foreach(var part in parts)if(part.posed==skin)
        {
            if(part.detail!=null)return part.detailKey.source==source?part.detail.mesh:null;
            // Bind to this actor's original body bounds and local orientation.
            // Shared once per geometry/rig, never reweighted during movement.
            var key=new MeshKey{source=source,local=part.key.local,body=part.key.body,profile=part.key.profile,
                clothing=ClothingLayout(source,part.original.sharedMaterials,part.key.profile)};
            if(!meshes.TryGetValue(key,out var shared)){shared=WeightMesh(key);meshes.Add(key,shared);}
            shared.users++;part.detailKey=key;part.detail=shared;return shared.mesh;
        }
        return null;
    }
    GeometryProfile Profile(Mesh mesh)
    {
        if(mesh.name.StartsWith("CampusBoy",StringComparison.Ordinal))return GeometryProfile.CampusBoy;
        if(mesh.name.StartsWith("CampusGirl",StringComparison.Ordinal))return GeometryProfile.CampusGirl;
        if(mesh.name.StartsWith("CampusTeacher",StringComparison.Ordinal))return GeometryProfile.CampusTeacher;
        return name.Contains("賽德克孩童")?GeometryProfile.SeediqChild:GeometryProfile.Legacy;
    }
    static bool IsCampus(GeometryProfile profile)=>profile>=GeometryProfile.CampusBoy;
    static string ClothingLayout(Mesh mesh,Material[] materials,GeometryProfile profile)
    {
        if(!IsCampus(profile))return null;
        var layout=new char[mesh.subMeshCount];
        for(int i=0;i<layout.Length;i++)
        {
            string label=i<materials.Length&&materials[i]?materials[i].name:string.Empty;
            layout[i]=profile==GeometryProfile.CampusGirl&&label.Contains("Campus_Navy")?'S'
                :label.Contains("Campus_Linen")||label.Contains("Campus_Charcoal")||label.Contains("Campus_Navy")?'U':'-';
        }
        return new string(layout);
    }
    ClothPart[] ClassifyClothing(MeshKey key,Vector3[] vertices)
    {
        // Material alone is insufficient: the teacher's jacket and trousers
        // share charcoal, and the pupils' socks share linen with their shirts.
        // Connected cloth pieces are classified once, when the shared skin is
        // made. Chest details stay on the torso; calves are never in the skirt mask.
        int count=vertices.Length;var component=new int[count];var top=new float[count];var bottom=new float[count];var left=new float[count];var right=new float[count];
        for(int i=0;i<count;i++){component[i]=i;top[i]=float.NegativeInfinity;bottom[i]=left[i]=float.PositiveInfinity;right[i]=float.NegativeInfinity;}
        int Root(int index)
        {
            while(component[index]!=index){component[index]=component[component[index]];index=component[index];}
            return index;
        }
        var triangles=new int[key.source.subMeshCount][];
        for(int s=0;s<triangles.Length;s++)
        {
            if(key.clothing[s]=='-')continue;
            var indices=triangles[s]=key.source.GetTriangles(s);
            for(int i=0;i+2<indices.Length;i+=3)
            {int root=Root(indices[i]);component[Root(indices[i+1])]=root;component[Root(indices[i+2])]=root;}
        }
        for(int i=0;i<count;i++)
        {
            int root=Root(i);float y=(key.local.MultiplyPoint3x4(vertices[i]).y-key.body.min.y)/Mathf.Max(.001f,key.body.size.y);
            top[root]=Mathf.Max(top[root],y);bottom[root]=Mathf.Min(bottom[root],y);
            float x=(key.local.MultiplyPoint3x4(vertices[i]).x-key.body.center.x)/Mathf.Max(.001f,key.body.size.y);
            left[root]=Mathf.Min(left[root],x);right[root]=Mathf.Max(right[root],x);
        }
        var result=new ClothPart[count];
        for(int s=0;s<triangles.Length;s++)
        {
            if(triangles[s]==null)continue;
            foreach(int index in triangles[s])
            {
                int root=Root(index);
                result[index]=key.clothing[s]=='S'&&top[root]<.57f&&bottom[root]>.20f?ClothPart.Skirt
                    :top[root]>.56f?(left[root]>-.145f&&right[root]<.145f?ClothPart.Torso:ClothPart.Upper):ClothPart.None;
            }
        }
        return result;
    }
    static float SmoothRange(float low,float high,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(low,high,value));
    BoneWeight CampusWeight(Vector3 point,ClothPart clothing,GeometryProfile profile)
    {
        float y=(point.y-body.min.y)/height,x=(point.x-body.center.x)/height;
        int arm=x<0?3:6,leg=x<0?9:12;
        if(clothing==ClothPart.Skirt)
        {
            // The sewn skirt stays continuous across its centre. A small,
            // smoothly shared thigh influence lets the hem follow the gait;
            // the independently classified calves keep their full leg rig.
            float sway=.15f*(1-SmoothRange(.27f,.50f,y));
            return MixWeights(CampusBlend(0,0,0),CampusBlend(9,12,SmoothRange(-.16f,.16f,x)),sway);
        }
        if(clothing==ClothPart.None&&y<.53f&&Mathf.Abs(x)<.18f)
        {
            if(y<.18f)return CampusBlend(leg+2,leg+1,SmoothRange(.08f,.18f,y));
            if(y<.35f)return CampusBlend(leg+1,leg,SmoothRange(.25f,.35f,y));
            return CampusBlend(leg,0,SmoothRange(.40f,.49f,y));
        }
        BoneWeight torso=y>.77f?CampusBlend(1,2,SmoothRange(.77f,.86f,y)):CampusBlend(0,1,SmoothRange(.53f,.69f,y));
        if(clothing==ClothPart.Torso)return torso;
        if(profile==GeometryProfile.CampusTeacher)
        {
            // The complete neck and collar share this same continuous blend.
            // Arms get almost full limb weight outside the jacket armhole;
            // hands and cuff edges share a wrist instead of stretching apart.
            float shoulderBand=SmoothRange(.70f,.80f,y);
            float teacherArmShare=SmoothRange(Mathf.Lerp(.145f,.095f,shoulderBand),
                Mathf.Lerp(.165f,.17f,shoulderBand),Mathf.Abs(x))*(1-SmoothRange(.81f,.86f,y));
            BoneWeight teacherSleeve=y<.59f?CampusBlend(arm+2,arm+1,SmoothRange(.505f,.57f,y))
                :CampusBlend(arm+1,arm,SmoothRange(.63f,.73f,y));
            return MixWeights(torso,teacherSleeve,teacherArmShare);
        }
        float shoulder=SmoothRange(.42f,.72f,y);
        float armShare=SmoothRange(Mathf.Lerp(.16f,.04f,shoulder),Mathf.Lerp(.19f,.22f,shoulder),Mathf.Abs(x))
            *(1-SmoothRange(.81f,.87f,y));
        BoneWeight sleeve=y<.54f?CampusBlend(arm+2,arm+1,SmoothRange(.44f,.54f,y))
            :CampusBlend(arm+1,arm,SmoothRange(.59f,.71f,y));
        return MixWeights(torso,sleeve,armShare);
    }
    static BoneWeight CampusBlend(int a,int b,float weight)=>weight>.5f?Blend(b,a,1-weight):Blend(a,b,weight);
    static BoneWeight MixWeights(BoneWeight torso,BoneWeight limb,float amount)
    {
        amount=Mathf.Clamp01(amount);
        int b0=torso.boneIndex0,b1=torso.boneIndex1,b2=limb.boneIndex0,b3=limb.boneIndex1;
        float w0=torso.weight0*(1-amount),w1=torso.weight1*(1-amount),w2=limb.weight0*amount,w3=limb.weight1*amount;
        // Unity expects descending weights; all four also participate in the
        // CPU support-plane sample below. No per-vertex temporary arrays.
        SortPair(ref b0,ref w0,ref b1,ref w1);SortPair(ref b2,ref w2,ref b3,ref w3);
        SortPair(ref b0,ref w0,ref b2,ref w2);SortPair(ref b1,ref w1,ref b3,ref w3);SortPair(ref b1,ref w1,ref b2,ref w2);
        return new BoneWeight{boneIndex0=b0,boneIndex1=b1,boneIndex2=b2,boneIndex3=b3,weight0=w0,weight1=w1,weight2=w2,weight3=w3};
    }
    static void SortPair(ref int firstBone,ref float firstWeight,ref int secondBone,ref float secondWeight)
    {
        if(firstWeight>=secondWeight)return;
        int bone=firstBone;firstBone=secondBone;secondBone=bone;
        float weight=firstWeight;firstWeight=secondWeight;secondWeight=weight;
    }
    BoneWeight Weight(Vector3 point,GeometryProfile profile)
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
        if(Mathf.Abs(x)>(profile==GeometryProfile.SeediqChild?.105f:.155f)&&y>.37f&&y<.84f)
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
        meleeGuard=0;meleeVariant=0;
        cowering=false;cowerWeight=0;groundCalibrated=false;renderedGroundOffset=0;
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
        return FollowingWithdrawal&&StepAlongPath(next);
    }
    internal bool StepAlongPath(Vector3 next)
    {
        if(IsFallen||!isActiveAndEnabled||!Clear(next))return false;
        Vector3 delta=next-transform.position;float distance=delta.magnitude;
        if(distance<.00001f)return true;
        transform.position=next;Face(next+delta.normalized);lastMovement=Time.time;
        phase+=distance/Mathf.Max(.4f,height*Mathf.Abs(transform.lossyScale.y)*.80f)*Mathf.PI*2;
        return true;
    }
    bool Clear(Vector3 next)
    {
        int count=Physics.OverlapCapsuleNonAlloc(next+Vector3.up*MovementBottom,next+Vector3.up*MovementTop,MovementRadius,overlaps,~0,QueryTriggerInteraction.Ignore);
        if(count==overlaps.Length)return false;
        for(int i=0;i<count;i++)if(Obstacle(overlaps[i],next))return false;
        Vector3 delta=next-transform.position;
        if(delta.sqrMagnitude<.000001f)return true;
        count=Physics.CapsuleCastNonAlloc(transform.position+Vector3.up*MovementBottom,transform.position+Vector3.up*MovementTop,MovementRadius,
            delta.normalized,sweepHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(count==sweepHits.Length)return false;
        for(int i=0;i<count;i++)if(Obstacle(sweepHits[i].collider,next))return false;
        return true;
    }
    bool Obstacle(Collider collider,Vector3 next)
    {
        if(collider.transform.IsChildOf(transform)||collider.bounds.max.y<.28f)return false;
        // The authored veranda is a 30 cm walking surface, not a wall.
        // Only this named slab is stepable; railings, posts and walls stay solid.
        if(collider.name=="外廊木地板"&&collider.bounds.max.y<=.32f)return false;
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
        cowerWeight=Mathf.MoveTowards(cowerWeight,cowering&&!FollowingWithdrawal?1:0,Time.deltaTime*3);
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
            if(meleeGuard>0&&nativeUpperArm&&nativeElbow&&nativeRig.RightHand)
            {
                Vector3 forward=nativeRig.Forward,right=Vector3.Cross(Vector3.up,forward);
                float wind=strike>0?Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.36f,strike)):0;
                float cut=strike>0?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.68f,strike)):0;
                float recover=strike>0?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,strike)):0;
                Vector3 guard=nativeUpperArm.position+forward*.28f+right*.05f-Vector3.up*(meleeGuard>.8f?-.03f:.15f);
                Vector3 raised=nativeUpperArm.position+Vector3.up*.28f-forward*.07f;
                Vector3 follow=nativeUpperArm.position+forward*.44f-right*.18f-Vector3.up*.23f;
                if(meleeVariant>0)
                {
                    float side=meleeVariant==1?1:-1;
                    raised=nativeUpperArm.position+right*side*.28f+Vector3.up*.10f;
                    follow=nativeUpperArm.position+forward*.43f-right*side*.25f-Vector3.up*.1f;
                }
                Vector3 hand=Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(guard,raised,wind),follow,cut),guard,recover);
                Vector3 forearm=nativeRig.RightHand.position-nativeElbow.position;Quaternion wrist=nativeRig.RightHand.rotation;
                Chapter1IncidentRig.Solve(nativeUpperArm,nativeElbow,nativeRig.RightHand,hand,right-Vector3.up*.3f);
                nativeRig.RightHand.rotation=Quaternion.FromToRotation(forearm,nativeRig.RightHand.position-nativeElbow.position)*wrist;
                if(!meleeSpine)meleeSpine=FindNativeBone(nativeRig.animator,HumanBodyBones.Spine,"Spine","Spine1","Waist");
                if(meleeSpine)meleeSpine.rotation=Quaternion.AngleAxis((wind*10-cut*22)*(1-recover)*(meleeVariant==2?-1:1),Vector3.up)*meleeSpine.rotation;
            }
            if(cowerWeight>0&&fall<=0&&PrepareNativeShelter())PoseShelter(nativeShelterBones,true);
            if(presentationSpear&&nativeRig.RightHand)
            {
                Vector3 direction=Vector3.Slerp(Vector3.up,nativeRig.Forward,strike>0?.3f+.35f*Mathf.Sin(strike*Mathf.PI):.18f);
                presentationSpear.SetPositionAndRotation(nativeRig.RightHand.position+direction*nativeRig.Height*.20f,Quaternion.FromToRotation(Vector3.up,direction));
            }
            if(presentationKnife&&nativeRig.RightHand)
            {
                float cut=strike>0?Mathf.Sin(strike*Mathf.PI):0;
                Vector3 direction=HoldingAtNeck?-restrainedGuest.transform.right:Vector3.Slerp(Vector3.up,nativeRig.Forward*.7f-Vector3.up*.7f,cut);
                if(meleeGuard>0&&strike<=0)direction=(Vector3.up+Vector3.Cross(Vector3.up,nativeRig.Forward)*.55f).normalized;
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
            // A long robe uses shorter steps instead of stretching its hem
            // through the ground with the pupil's much wider running gait.
            bones[leg].localRotation=Quaternion.Euler(step*(longGarment?16:skirtGarment?22:30),0,0);
            bones[leg+1].localRotation=Quaternion.Euler(-Mathf.Max(0,-step)*(longGarment?28:skirtGarment?36:48),0,0);
            bones[leg+2].localRotation=Quaternion.Euler(-step*12,0,0);
            // The existing baked sleeves were weighted for lowered arms.
            // Raising both shoulders by 135 degrees pulled the robe and
            // torso into spikes; fear is now read through a restrained lean
            // and bent elbows, keeping the upper arms beside the ribs.
            bones[arm].localRotation=Quaternion.Euler(-step*(longGarment?8:18)-(afraid?18:0)-12*startle,0,sign*(afraid?4:2));
            bones[arm+1].localRotation=Quaternion.Euler(-12-(afraid?18:0)-12*startle,0,0);
        }
        if(waiting)
        {
            // One hand stays just in front of the chest instead of an
            // overhead wave that twists wide sleeves through the head.
            bones[6].localRotation=Quaternion.Euler(-28,0,4);
            bones[7].localRotation=Quaternion.Euler(-36,0,0);
        }
        if(cowerWeight>0)PoseShelter(bones,false);
        if(meleeGuard>0)
        {
            // Visible defensive hands and a short counter-shove for the
            // campus guests. Keep wide sleeves below the old overhead arc.
            float reach=strike>0?Mathf.Sin(strike*Mathf.PI):0;
            float guard=Mathf.Clamp01(meleeGuard);
            float upper=(longGarment?18:24)+guard*10,elbow=(longGarment?26:40)+guard*24;
            bones[3].localRotation=Quaternion.Euler(-upper,0,-4);
            bones[4].localRotation=Quaternion.Euler(-elbow,0,0);
            bones[6].localRotation=Quaternion.Euler(-upper-reach*(longGarment?14:22),0,4+reach*4);
            bones[7].localRotation=Quaternion.Euler(-elbow+reach*25,0,0);
            bones[1].localRotation*=Quaternion.Euler(-3*meleeGuard,reach*8,0);
            if(teacherGarment)PoseTeacherFight();
        }
        else if(strike>0)
        {bones[6].localRotation=Quaternion.Euler(-40-70*Mathf.Sin(strike*Mathf.PI),0,12);bones[7].localRotation=Quaternion.Euler(-30+25*Mathf.Sin(strike*Mathf.PI),0,0);}
        if(fall>0)PoseFall();else GroundSyntheticPose();
    }
    void PoseTeacherFight()
    {
        // Guard, brace, counter and recover, with one hand protecting the
        // upper chest while the other reaches. IK retains arm segment length.
        float extension=strike>0?Mathf.Sin(strike*Mathf.PI):0;
        float brace=meleeGuard>.8f?1:0;
        int attackingSide=meleeVariant%2;
        bones[0].localPosition+=Vector3.down*height*.025f;
        bones[1].localRotation=Quaternion.Euler(5+brace*5,extension*(attackingSide==0?-12:12),0);
        bones[2].localRotation=Quaternion.Euler(-4-brace*3,0,0);
        for(int side=0;side<2;side++)
        {
            int arm=3+side*3,leg=9+side*3;float sign=side==0?-1:1;
            bones[leg].localRotation=Quaternion.Euler(side==0?13:-7,0,-sign*3);
            bones[leg+1].localRotation=Quaternion.Euler(-17,0,0);
            bones[leg+2].localRotation=Quaternion.Euler(8,0,0);
            float reach=side==attackingSide?extension:0;
            Vector3 target=transform.TransformPoint(new Vector3(body.center.x+sign*height*(.15f-reach*.035f),
                body.min.y+height*(.735f+brace*.035f-reach*.055f),body.center.z+height*(.19f+reach*.20f)));
            Vector3 hint=transform.right*sign-transform.up*.65f+transform.forward*.15f;
            Chapter1IncidentRig.Solve(bones[arm],bones[arm+1],bones[arm+2],target,hint);
            bones[arm+2].localRotation=Quaternion.Euler(8,0,0);
        }
    }
    bool PrepareNativeShelter()
    {
        if(nativeShelterBones!=null)return true;
        if(!nativeRig||!nativeRig.animator||!nativeRig.Hips||!nativeRig.Head)return false;
        var animator=nativeRig.animator;
        var joints=new Transform[15];
        joints[0]=nativeRig.Hips;
        while(joints[0]&&!nativeRig.Head.IsChildOf(joints[0])&&joints[0].parent&&joints[0].parent!=nativeRoot)
            joints[0]=joints[0].parent;
        joints[1]=FindNativeBone(animator,HumanBodyBones.Spine,"Spine","Spine1","Waist");joints[2]=nativeRig.Head;
        joints[3]=FindNativeBone(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        joints[4]=FindNativeBone(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");joints[5]=nativeRig.LeftHand;
        joints[6]=nativeUpperArm;joints[7]=nativeElbow;joints[8]=nativeRig.RightHand;
        joints[9]=FindNativeBone(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        joints[10]=FindNativeBone(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        joints[11]=FindNativeBone(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        joints[12]=FindNativeBone(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        joints[13]=FindNativeBone(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        joints[14]=FindNativeBone(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        foreach(var joint in joints)if(!joint)return false;
        if(!nativeRig.Head.IsChildOf(joints[0]))return false;
        nativeShelterBones=joints;return true;
    }
    void PoseShelter(Transform[] joints,bool originalSkin)
    {
        float weight=cowerWeight,worldHeight=originalSkin?nativeRig.Height:height*Mathf.Abs(transform.lossyScale.y);
        Vector3 forward=originalSkin?nativeRig.Forward:transform.forward;
        Vector3 right=Vector3.Cross(Vector3.up,forward).normalized;
        for(int side=0;side<2;side++)
        {
            int foot=11+side*3;shelterFeet[side]=joints[foot].position;shelterFootRotations[side]=joints[foot].rotation;
            // A small stagger reads as balance, without twisting either knee
            // away from its foot or forcing the child's legs into a deep fold.
            shelterFeet[side]+=forward*worldHeight*(side==0?.025f:-.015f)*weight;
        }
        joints[0].position-=Vector3.up*(worldHeight*(longGarment?.035f:skirtGarment?.045f:.065f)*weight);
        joints[1].rotation=Quaternion.AngleAxis((longGarment?10:14)*weight,right)*joints[1].rotation;
        joints[2].rotation=Quaternion.AngleAxis(8*weight,right)*joints[2].rotation;
        for(int side=0;side<2;side++)
        {
            int leg=9+side*3;
            Chapter1IncidentRig.Solve(joints[leg],joints[leg+1],joints[leg+2],shelterFeet[side],forward);
            joints[leg+2].rotation=shelterFootRotations[side];
        }
        Vector3 hips=(joints[9].position+joints[12].position)*.5f;
        for(int side=0;side<2;side++)
        {
            int arm=3+side*3,leg=9+side*3;float sign=side==0?-1:1;
            Vector3 target=longGarment
                ?hips+Vector3.up*worldHeight*(side==0?.18f:.24f)+forward*worldHeight*.13f+right*sign*worldHeight*.045f
                :Vector3.Lerp(joints[leg].position,joints[leg+1].position,.55f)+Vector3.up*worldHeight*.035f+forward*worldHeight*.02f;
            Vector3 oldForearm=joints[arm+2].position-joints[arm+1].position;
            Quaternion wrist=joints[arm+2].rotation;
            float reach=(Vector3.Distance(joints[arm].position,joints[arm+1].position)+Vector3.Distance(joints[arm+1].position,joints[arm+2].position))*.94f;
            target=joints[arm].position+Vector3.ClampMagnitude(target-joints[arm].position,reach);
            Chapter1IncidentRig.Solve(joints[arm],joints[arm+1],joints[arm+2],Vector3.Lerp(joints[arm+2].position,target,weight),
                Vector3.down+right*sign*.18f+forward*.10f);
            joints[arm+2].rotation=Quaternion.FromToRotation(oldForearm,joints[arm+2].position-joints[arm+1].position)*wrist;
            // Native wrists have already passed Chapter3HandPose's mesh-axis
            // calibration (4700). Preserve that frame while following the
            // forearm instead of replacing it with the rig's fallback basis.
        }
    }
    void PoseFall()
    {
        float collapse=Mathf.SmoothStep(0,1,fall),bend=Mathf.Sin(collapse*Mathf.PI);
        float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.95f,collapse));
        bones[0].localPosition=bindPositions[0]-Vector3.up*(height*.39f*collapse);
        bones[0].localRotation=Quaternion.Euler(-7*bend,0,87*collapse*fallSide);
        bones[1].localRotation=Quaternion.Euler(18*bend,0,0);
        for(int side=0;side<2;side++)
        {
            int leg=9+side*3,arm=3+side*3;float sign=side==0?-1:1;
            bool lowerArm=sign*fallSide<0;
            bones[leg].localRotation=Quaternion.Euler(22*bend+side*5*collapse,0,0);
            bones[leg+1].localRotation=Quaternion.Euler(-42*bend-10*collapse,0,0);
            // Let the arms fold beside the torso as the body settles. The
            // previous spread hand became the lowest vertex and held the
            // entire head and chest above ground during support correction.
            // The original kimono's wide sleeves need more inward shoulder
            // rotation and a shallower elbow fold: otherwise a low hand/sleeve
            // supports the whole body above the floor despite valid clearance.
            float settledShoulderX=longGarment?(lowerArm?-12:-20):(lowerArm?-32:-42);
            float settledShoulderZ=-sign*(longGarment?(lowerArm?72:58):20);
            float settledElbowX=longGarment?(lowerArm?-22:-35):(lowerArm?-45:-65);
            bones[arm].localRotation=Quaternion.Slerp(
                Quaternion.Euler(-55*bend-14*collapse,0,side==0?-12:18),
                Quaternion.Euler(settledShoulderX,0,settledShoulderZ),settle);
            bones[arm+1].localRotation=Quaternion.Slerp(
                Quaternion.Euler(-40*bend-15*collapse,0,0),
                Quaternion.Euler(settledElbowX,0,0),settle);
        }
        // The final fall shares the rounded pose-cache bucket with a still
        // moving fall. Calibrate the settled kimono once before freezing it.
        if(longGarment&&fall>=1&&!finalFallPoseReady)groundCalibrated=false;
        GroundSyntheticPose();
        if(fall>=.95f)
            foreach(var saved in capsules)if(saved.collider)
            {saved.collider.direction=0;saved.collider.center=new Vector3(body.center.x-height*.02f*fallSide,body.min.y+height*.17f,body.center.z);saved.collider.height=height;saved.collider.radius=height*.14f;}
        if(fall>=1)finalFallPoseReady=true;
    }
    void GroundSyntheticPose()
    {
        // Walking and frightened poses need the same support correction as
        // fallen bodies. Never lower the hips while leaving the feet below
        // the yard/veranda. Sampling shared skin data avoids per-frame BakeMesh.
        for(int i=0;i<bones.Length;i++)poseMatrices[i]=RelativeMatrix(bones[i],transform)*Matrix4x4.Translate(-bindPositions[i]);
        float lowest=float.PositiveInfinity;
        foreach(var part in parts)
        {
            var pose=part.detail!=null&&part.posed.sharedMesh==part.detail.mesh?part.detail:part.shared;
            for(int i=0;i<pose.samples.Length;i++)
            {
                Vector3 vertex=part.key.local.MultiplyPoint3x4(pose.samples[i]);BoneWeight weight=pose.sampleWeights[i];
                Vector3 posed=poseMatrices[weight.boneIndex0].MultiplyPoint3x4(vertex)*weight.weight0+poseMatrices[weight.boneIndex1].MultiplyPoint3x4(vertex)*weight.weight1;
                if(weight.weight2>0)posed+=poseMatrices[weight.boneIndex2].MultiplyPoint3x4(vertex)*weight.weight2;
                if(weight.weight3>0)posed+=poseMatrices[weight.boneIndex3].MultiplyPoint3x4(vertex)*weight.weight3;
                lowest=Mathf.Min(lowest,posed.y);
            }
        }
        if(float.IsPositiveInfinity(lowest))return;
        float surface=ambient?ambient.CorridorGroundHeight(transform.position):0;
        float localFloor=transform.InverseTransformPoint(new Vector3(transform.position.x,surface+.018f,transform.position.z)).y;
        bones[0].localPosition+=Vector3.up*(localFloor-lowest);
        CorrectRenderedGround(surface);
    }
    void CorrectRenderedGround(float surface)
    {
        int meshStamp=17;
        foreach(var part in parts)if(part.posed&&part.posed.enabled&&part.posed.sharedMesh)
            meshStamp=unchecked(meshStamp*31+part.posed.sharedMesh.GetInstanceID());
        int pose=(walkWeight>.1f?1:0)+(waiting?2:0)+Mathf.RoundToInt(cowerWeight*4)*4+Mathf.RoundToInt(fall*4)*32;
        if(!groundCalibrated||meshStamp!=calibratedGroundMesh||pose!=calibratedGroundPose||Mathf.Abs(surface-calibratedGroundSurface)>.001f)
        {
            // Calibrate the rendered skin only when LOD, support plane or
            // posture changes. Keep the cheap shared samples for every frame.
            // Measure the real root/renderer response rather than relying
            // on an assumed scale for the baked female donor.
            float minimum=RenderedLowestY();
            renderedGroundOffset=0;
            if(!float.IsPositiveInfinity(minimum))
            {
                Vector3 before=bones[0].position;
                bones[0].position=before+Vector3.up*.025f;
                float lifted=RenderedLowestY();
                bones[0].position=before;
                float response=(lifted-minimum)/.025f;
                if(response>.1f&&response<10)
                    renderedGroundOffset=(surface+.018f-minimum)/response;
            }
            groundCalibrated=true;calibratedGroundMesh=meshStamp;
            calibratedGroundPose=pose;calibratedGroundSurface=surface;
            // Retain only an empty probe object between occasional checks.
            // High-poly near LOD vertex buffers must not remain per person.
            if(groundProbe)groundProbe.Clear();
            groundProbeVertices.Clear();groundProbeVertices.TrimExcess();
        }
        bones[0].position+=Vector3.up*renderedGroundOffset;
    }
    float RenderedLowestY()
    {
        if(!groundProbe)groundProbe=new Mesh{name="Crowd ground calibration",hideFlags=HideFlags.HideAndDontSave};
        float minimum=float.PositiveInfinity;
        foreach(var part in parts)
        {
            var skin=part.posed;
            if(!skin||!skin.enabled||!skin.gameObject.activeInHierarchy||!skin.sharedMesh)continue;
            skin.BakeMesh(groundProbe,false);groundProbe.GetVertices(groundProbeVertices);
            foreach(var vertex in groundProbeVertices)
                minimum=Mathf.Min(minimum,skin.transform.TransformPoint(vertex).y);
        }
        return minimum;
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
            if(part.detail!=null&&--part.detail.users==0){meshes.Remove(part.detailKey);if(part.detail.mesh)Destroy(part.detail.mesh);}
        }
        if(groundProbe)Destroy(groundProbe);
        if(boneRoot)Destroy(boneRoot.gameObject);
    }
}
