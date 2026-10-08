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
        var delta=new Vector3[vertices.Length];var normalDelta=new Vector3[vertices.Length];
        // Bend around each finger's centreline, not the wrist plane. The palm
        // plane is oblique on this donor; treating q.y as radial thickness
        // turned its palmar surfaces inside out. Finger-specific lengths let
        // the shorter little finger close as far as the middle finger.
        float[] fingerX={-.10f,.03f,.155f,.245f};
        float[] closedFingerX={-.055f,.04f,.135f,.22f};
        float[] fingerEnd={.976f,1,.934f,.82f};
        float[] fingerCentre={.044f,.072f,.031f,-.005f};
        float[] fingerSlope={-.22f,-.26f,-.32f,-.36f};
        Vector3 thumbPivot=new Vector3(-.19f,-.055f,.37f)*length;
        Vector3 thumbAlong=new Vector3(-.38f,-.1f,.92f).normalized;
        Vector3 thumbClosed=Vector3.right;
        Vector3 thumbAxis=Vector3.Cross(thumbAlong,thumbClosed).normalized;
        Vector3 thumbToward=Vector3.Cross(thumbAxis,thumbAlong);
        float thumbRadius=length*.09f,thumbAngle=Mathf.Acos(Mathf.Clamp(Vector3.Dot(thumbAlong,thumbClosed),-1,1));
        Quaternion inverseBasis=Quaternion.Inverse(basis);
        var deformed=(Vector3[])vertices.Clone();
        for(int i=0;i<vertices.Length;i++)
        {
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.75f,Weight(weights[i],included)));if(weight<=0)continue;
            Vector3 point=bind.MultiplyPoint3x4(vertices[i]);
            Vector3 q=inverseBasis*point,curled=q;
            int finger=0;while(finger<2&&q.x/length>fingerX[finger+1])finger++;
            float between=Mathf.InverseLerp(fingerX[finger],fingerX[finger+1],q.x/length);
            float tip=Mathf.Lerp(fingerEnd[finger],fingerEnd[finger+1],between)*length;
            float centre=Mathf.Lerp(fingerCentre[finger],fingerCentre[finger+1],between)*length;
            float slope=Mathf.Lerp(fingerSlope[finger],fingerSlope[finger+1],between);
            float start=length*.43f,arc=Mathf.Max(0,q.z-start);
            if(arc>0)
            {
                float radius=length*.075f;
                float angle=Mathf.PI*1.24f*Mathf.Clamp01(arc/(tip-start));
                float thickness=Mathf.Clamp((q.y-(centre+slope*arc))*.75f,-radius*.72f,radius*.72f);
                float y=length*.026f-radius*(1-Mathf.Cos(angle))+thickness*Mathf.Cos(angle);
                float z=start+(radius+thickness)*Mathf.Sin(angle);
                float root=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,length*.15f,arc));
                curled.y=Mathf.Lerp(q.y,y,root);curled.z=Mathf.Lerp(q.z,z,root);
                // Adduct the four digits as separate volumes. Scaling all x
                // coordinates shrank their widths and left the visible gaps.
                int nearest=0;
                for(int digit=1;digit<fingerX.Length;digit++)
                    if(Mathf.Abs(q.x-fingerX[digit]*length)<Mathf.Abs(q.x-fingerX[nearest]*length))nearest=digit;
                float packed=closedFingerX[nearest]*length+(q.x-fingerX[nearest]*length)*1.12f;
                float adduct=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f*length,.68f*length,q.z));
                curled.x=Mathf.Lerp(q.x,packed,adduct);
            }
            // Oppose the thumb along a continuous bend, with a broad blend at
            // its fleshy base. A rigid rotation blended across the narrow web
            // folded the base back through itself. Above the web the mask
            // narrows to the actual gap, leaving the index surface intact.
            Vector3 relative=q-thumbPivot;
            float along=Vector3.Dot(relative,thumbAlong);
            float turn=Mathf.Clamp(along/thumbRadius,0,thumbAngle);
            float tail=Mathf.Max(0,along-thumbRadius*thumbAngle);
            Vector3 thumbCentre=thumbPivot+thumbAlong*(thumbRadius*Mathf.Sin(turn))
                +thumbToward*(thumbRadius*(1-Mathf.Cos(turn)))+thumbClosed*tail;
            // Set the distal thumb across the folded index/middle, flush to
            // their palmar side, instead of leaving an open thumb/index ring.
            float press=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f*length,.27f*length,along));
            thumbCentre+=new Vector3(.06f,-.10f,-.03f)*(length*press);
            Vector3 foldedThumb=along<=0?q:thumbCentre
                +(thumbToward*Mathf.Cos(turn)-thumbAlong*Mathf.Sin(turn))*Vector3.Dot(relative,thumbToward)
                +thumbAxis*Vector3.Dot(relative,thumbAxis);
            float boundary=(-.07f-.13f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f*length,.58f*length,q.z)))*length;
            float width=(.025f+.15f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f*length,.62f*length,q.z))))*length;
            float thumb=Mathf.SmoothStep(0,1,Mathf.InverseLerp(boundary+width,boundary-width,q.x))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f*length,.48f*length,q.z));
            Vector3 closed=Vector3.Lerp(curled,foldedThumb,thumb);
            delta[i]=inverse.MultiplyVector(basis*(closed-q))*weight;
            deformed[i]+=delta[i];
        }
        // Use the actual deformed triangles for the final normal directions;
        // rotating the old normals misses the changing bend radius/web blend.
        var normalMesh=Object.Instantiate(original);normalMesh.vertices=deformed;normalMesh.RecalculateNormals();
        var closedNormals=normalMesh.normals;
        for(int i=0;i<vertices.Length;i++)if(delta[i].sqrMagnitude>1e-12f)normalDelta[i]=closedNormals[i]-normals[i];
        Object.DestroyImmediate(normalMesh);
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
