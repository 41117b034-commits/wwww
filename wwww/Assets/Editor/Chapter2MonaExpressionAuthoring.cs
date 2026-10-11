using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// This donor has no finger or facial bones. Author additive, scene-local shapes
// while mesh data is readable; playback uses only the saved mesh and weights.
public static class Chapter2MonaExpressionAuthoring
{
    const string Path="Assets/Chapter2/Poses/MonaCouncilExpressions.asset";
    [MenuItem("Tools/Chapter 2/Bake Mona Angry Point")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var actor=c.mona;var hands=actor.GetComponent<Chapter2CouncilHands>();var skin=hands.fistRenderer;
        var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Chapter2/Poses/MonaRallyFist.asset");
        var mesh=Object.Instantiate(original);mesh.name="MonaCouncilExpressions";mesh.ClearBlendShapes();
        var vertices=mesh.vertices;var weights=mesh.boneWeights;
        var hand=skin.bones.First(b=>b.name=="R_Hand");int slot=Array.IndexOf(skin.bones,hand);
        var bind=mesh.bindposes[slot];var inverse=bind.inverse;var inverseBasis=Quaternion.Inverse(hands.rightPalmBasis);
        float length=0;
        for(int i=0;i<vertices.Length;i++)if(Weight(weights[i],slot)>.5f)length=Mathf.Max(length,(inverseBasis*bind.MultiplyPoint3x4(vertices[i])).z);
        var delta=new Vector3[vertices.Length];
        original.GetBlendShapeFrameVertices(original.GetBlendShapeIndex("Council right fist"),0,delta,null,null);
        for(int i=0;i<vertices.Length;i++)
        {
            float weight=Weight(weights[i],slot);if(weight<.05f)continue;
            var q=inverseBasis*bind.MultiplyPoint3x4(vertices[i]);float x=q.x/length,z=q.z/length;
            // The thumb is on negative x. Keep the adjacent index extended,
            // leaving the other three fingers and thumb folded into the palm.
            float index=Smooth(-.19f,-.17f,x)*(1-Smooth(-.045f,-.037f,x))*Smooth(.43f,.62f,z);
            // Translate each cross-section; never collapse its thickness to a plane.
            Vector3 straight=q;straight.y+=(length*.028f-(length*.044f-.22f*Mathf.Max(0,q.z-length*.43f)))*Smooth(.43f,.68f,z);
            straight.x=Mathf.Lerp(q.x,q.x+length*.03f,Smooth(.46f,.80f,z));
            delta[i]=Vector3.Lerp(delta[i],inverse.MultiplyVector(hands.rightPalmBasis*(straight-q))*weight,index);
        }
        AddShape(mesh,"Council right point",delta);
        var head=skin.bones.First(b=>b.name=="Head");slot=Array.IndexOf(skin.bones,head);bind=mesh.bindposes[slot];inverse=bind.inverse;
        Vector3 f=Vector3.ProjectOnPlane(actor.facing,Vector3.up).normalized,r=Vector3.Cross(Vector3.up,f);
        float height=skin.bounds.size.y,noseZ=float.NegativeInfinity,noseY=0;
        for(int i=0;i<vertices.Length;i++)
        {
            if(Weight(weights[i],slot)<.7f)continue;
            Vector3 p=head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]))-head.position;
            if(Mathf.Abs(Vector3.Dot(p,r))>height*.012f||Mathf.Abs(p.y)>height*.075f)continue;
            float z=Vector3.Dot(p,f);if(z>noseZ){noseZ=z;noseY=p.y;}
        }
        delta=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            float weight=Weight(weights[i],slot);if(weight<.5f)continue;
            Vector3 p=head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]))-head.position;
            float x=(Vector3.Dot(p,r)-.008f)/height,y=(p.y-noseY)/height,z=Vector3.Dot(p,f);
            float front=Smooth(noseZ-height*.055f,noseZ-height*.022f,z);
            float brow=Gaussian((Mathf.Abs(x)-.012f)/.010f,(y-.034f)/.008f);
            float outer=Gaussian((Mathf.Abs(x)-.031f)/.010f,(y-.034f)/.009f);
            float eye=Gaussian((Mathf.Abs(x)-.0207f)/.014f,(y-.0205f)/.010f);
            float corner=Gaussian((Mathf.Abs(x)-.021f)/.014f,(y+.031f)/.012f);
            float dy=-.005f*brow+.002f*outer+Mathf.Clamp(y-.0205f,-.008f,.008f)*.50f*eye-.003f*corner;
            Vector3 offset=(Vector3.up*dy-r*Mathf.Sign(x)*.003f*brow+f*.002f*brow)*height*front*weight;
            delta[i]=inverse.MultiplyVector(head.InverseTransformVector(offset));
        }
        AddShape(mesh,"Council angry glare",delta);
        MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},Path,false);
        Object.DestroyImmediate(mesh);AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        hands.fistMesh=original;hands.refusalMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Path);
        hands.fistShape=original.GetBlendShapeIndex("Council right fist");hands.pointShape=hands.refusalMesh.GetBlendShapeIndex("Council right point");hands.angerShape=hands.refusalMesh.GetBlendShapeIndex("Council angry glare");
        EditorUtility.SetDirty(hands);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
        Chapter2CouncilStoryAuthoring.SaveBinary(hands.refusalMesh);
        Debug.Log("[Chapter2] Mona point and glare authored; face origin="+noseY+", height="+height);
    }
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static float Gaussian(float x,float y)=>Mathf.Exp(-x*x-y*y);
    static float Weight(BoneWeight w,int s)=>(w.boneIndex0==s?w.weight0:0)+(w.boneIndex1==s?w.weight1:0)+(w.boneIndex2==s?w.weight2:0)+(w.boneIndex3==s?w.weight3:0);
    static void AddShape(Mesh mesh,string name,Vector3[] delta)
    {
        var normalMesh=Object.Instantiate(mesh);var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=delta[i];normalMesh.vertices=v;normalMesh.RecalculateNormals();
        var dn=normalMesh.normals;var n=mesh.normals;
        for(int i=0;i<n.Length;i++)dn[i]=delta[i].sqrMagnitude>1e-12f?dn[i]-n[i]:Vector3.zero;
        mesh.AddBlendShapeFrame(name,100,delta,dn,null);Object.DestroyImmediate(normalMesh);
    }
}
