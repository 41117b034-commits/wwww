using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Only scene-local council characters and a dedicated mesh copy are changed.
public static class Chapter2CouncilPoseAuthoring
{
    const string MeshPath="Assets/Chapter2/Poses/MonaRallyFist.asset";
    [MenuItem("Tools/Chapter 2/Correct Council Hands and Proportions")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        Calibrate(c.leaders[2],false);
        Calibrate(c.mona,true);
        // Absolute scale derived from the original 1.6739 m-normalized donor, so reapplying is safe.
        var fifth=c.leaders[4];
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/日治時期長輩女/tripo_convert_55823c37-f454-4808-bc43-e5329c672c81.fbx");
        var skins=source.GetComponentsInChildren<SkinnedMeshRenderer>(true);Bounds b=skins[0].bounds;
        foreach(var skin in skins)b.Encapsulate(skin.bounds);
        float scale=1.72f/b.size.y;
        fifth.transform.localScale=Vector3.Scale(source.transform.localScale,new Vector3(1.22f,.90f,1.10f)*scale);
        EditorUtility.SetDirty(fifth.transform);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
        Debug.Log("[Chapter2] Council wrists, fifth leader proportions, and rally fist authored.");
    }
    static void Calibrate(Chapter2Actor actor,bool fist)
    {
        var a=actor.GetComponentInChildren<Animator>(true);
        var hands=actor.GetComponent<Chapter2CouncilHands>()??actor.gameObject.AddComponent<Chapter2CouncilHands>();
        var left=Bone(a,HumanBodyBones.LeftHand,"L_Hand","LeftHand");
        var right=Bone(a,HumanBodyBones.RightHand,"R_Hand","RightHand");
        var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s=>Array.IndexOf(s.bones,right)>=0);
        hands.leftPalmBasis=Basis(skin,a,left,true,out _);
        hands.rightPalmBasis=Basis(skin,a,right,false,out float length);
        if(fist)hands.rightPalmBasis=hands.rightPalmBasis*Quaternion.AngleAxis(180,Vector3.forward);
        hands.alignWrists=!fist;
        if(fist){hands.fistRenderer=skin;hands.fistMesh=Bake(skin,right,hands.rightPalmBasis,length);hands.fistShape=hands.fistMesh.GetBlendShapeIndex("Council right fist");}
        EditorUtility.SetDirty(hands);
    }
    static Quaternion Basis(SkinnedMeshRenderer skin,Animator animator,Transform hand,bool left,out float length)
    {
        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;
        int slot=Array.IndexOf(skin.bones,hand);
        bool[] included=skin.bones.Select(b=>b&&(b==hand||b.IsChildOf(hand))).ToArray();
        var points=new List<Vector3>();
        for(int i=0;i<vertices.Length;i++)if(Weight(weights[i],included)>.3f)points.Add(binds[slot].MultiplyPoint3x4(vertices[i]));
        if(points.Count==0)throw new InvalidOperationException("No hand vertices: "+hand.name);
        Vector3 center=Vector3.zero;foreach(var p in points)center+=p;center/=points.Count;
        Vector3 along=center.normalized;
        var l=Bone(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        var r=Bone(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        var head=Bone(animator,HumanBodyBones.Head,"Head");
        var hips=Bone(animator,HumanBodyBones.Hips,"Hip","Hips");
        Vector3 span=(Vector3)binds[Array.IndexOf(skin.bones,r)].inverse.GetColumn(3)-(Vector3)binds[Array.IndexOf(skin.bones,l)].inverse.GetColumn(3);
        Vector3 up=(Vector3)binds[Array.IndexOf(skin.bones,head)].inverse.GetColumn(3)-(Vector3)binds[Array.IndexOf(skin.bones,hips)].inverse.GetColumn(3);
        Vector3 bodyForward=binds[slot].MultiplyVector(Vector3.Cross(span,up).normalized);
        Vector3 reference=Vector3.Cross(along,bodyForward)*(left?-1:1);
        Vector3 axis=Vector3.ProjectOnPlane(reference,along).normalized,other=Vector3.Cross(along,axis).normalized;
        float xx=0,xy=0,yy=0;
        foreach(var p in points){Vector3 d=p-center;float x=Vector3.Dot(d,axis),y=Vector3.Dot(d,other);xx+=x*x;xy+=x*y;yy+=y*y;}
        float angle=.5f*Mathf.Atan2(2*xy,xx-yy);
        Vector3 normal=-axis*Mathf.Sin(angle)+other*Mathf.Cos(angle);
        if(Vector3.Dot(normal,reference)<0)normal=-normal;
        length=points.Max(p=>Vector3.Dot(p,along));
        return Quaternion.LookRotation(along,normal);
    }
    static Mesh Bake(SkinnedMeshRenderer skin,Transform hand,Quaternion basis,float length)
    {
        Mesh original=skin.sharedMesh;var vertices=original.vertices;var normals=original.normals;var weights=original.boneWeights;
        int slot=Array.IndexOf(skin.bones,hand);var bind=original.bindposes[slot];var inverse=bind.inverse;
        bool[] included=skin.bones.Select(b=>b&&(b==hand||b.IsChildOf(hand))).ToArray();
        Vector3 along=basis*Vector3.forward,normal=basis*Vector3.up;
        var delta=new Vector3[vertices.Length];var normalDelta=new Vector3[vertices.Length];
        float knuckle=length*.42f,radius=length*.105f;
        for(int i=0;i<vertices.Length;i++)
        {
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.75f,Weight(weights[i],included)));if(weight<=0)continue;
            Vector3 point=bind.MultiplyPoint3x4(vertices[i]);float distance=Vector3.Dot(point,along);
            if(distance<=knuckle)continue;
            float curl=Mathf.Min((distance-knuckle)/radius,Mathf.PI*.98f);
            Vector3 offset=along*(knuckle+Mathf.Sin(curl)*radius-distance)+normal*(1-Mathf.Cos(curl))*radius;
            delta[i]=inverse.MultiplyVector(offset)*weight;
            Vector3 n=inverse.transpose.MultiplyVector(normals[i]).normalized;
            var turn=Quaternion.AngleAxis(curl*Mathf.Rad2Deg,Vector3.Cross(along,normal));
            normalDelta[i]=(bind.transpose.MultiplyVector(turn*n).normalized-normals[i])*weight;
        }
        var mesh=Object.Instantiate(original);mesh.name="MonaRallyFist";
        mesh.AddBlendShapeFrame("Council right fist",100,delta,normalDelta,null);
        MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);
        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},MeshPath,false);
        Object.DestroyImmediate(mesh);AssetDatabase.ImportAsset(MeshPath,ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
    }
    static float Weight(BoneWeight w,bool[] include)
    {return(include[w.boneIndex0]?w.weight0:0)+(include[w.boneIndex1]?w.weight1:0)+(include[w.boneIndex2]?w.weight2:0)+(include[w.boneIndex3]?w.weight3:0);}
    static Transform Bone(Animator a,HumanBodyBones bone,params string[] names)
    {
        if(a.isHuman&&a.GetBoneTransform(bone))return a.GetBoneTransform(bone);
        return a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Any(n=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase)||t.name.EndsWith(":"+n,StringComparison.OrdinalIgnoreCase)));
    }
}
