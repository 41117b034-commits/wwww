using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Chapter2KnifeGripAuthoring
{
    public static void Apply(Chapter2Actor actor,Chapter2CouncilDrama drama)
    {
        var animator=actor.GetComponentInChildren<Animator>(true);
        Transform hand=Bone(animator,HumanBodyBones.RightHand,"R_Hand","RightHand");
        var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s=>Array.IndexOf(s.bones,hand)>=0);
        // Always build from the imported skin, never compound the previous deformation.
        Mesh source=skin.sharedMesh;int slot=Array.IndexOf(skin.bones,hand);
        var vertices=source.vertices;var weights=source.boneWeights;var bind=source.bindposes[slot];var inv=bind.inverse;
        bool[] included=skin.bones.Select(b=>b&&(b==hand||b.IsChildOf(hand))).ToArray();
        var points=new List<Vector3>();
        for(int i=0;i<vertices.Length;i++)if(Weight(weights[i],included)>.4f)points.Add(bind.MultiplyPoint3x4(vertices[i]));
        Vector3 mean=Vector3.zero;foreach(var p in points)mean+=p;mean/=points.Count;Vector3 along=mean.normalized;
        var leftArm=Bone(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        var rightArm=Bone(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        var head=Bone(animator,HumanBodyBones.Head,"Head");
        var hips=Bone(animator,HumanBodyBones.Hips,"Hip","Hips");
        Vector3 span=(Vector3)source.bindposes[Array.IndexOf(skin.bones,rightArm)].inverse.GetColumn(3)-(Vector3)source.bindposes[Array.IndexOf(skin.bones,leftArm)].inverse.GetColumn(3);
        Vector3 vertical=(Vector3)source.bindposes[Array.IndexOf(skin.bones,head)].inverse.GetColumn(3)-(Vector3)source.bindposes[Array.IndexOf(skin.bones,hips)].inverse.GetColumn(3);
        Vector3 front=bind.MultiplyVector(Vector3.Cross(span,vertical).normalized);
        Vector3 refNormal=Vector3.Cross(front,along).normalized;
        Vector3 axis=Vector3.ProjectOnPlane(refNormal,along).normalized,other=Vector3.Cross(along,axis).normalized;
        float xx=0,xy=0,yy=0;
        foreach(var p in points){var d=p-mean;float x=Vector3.Dot(d,axis),y=Vector3.Dot(d,other);xx+=x*x;xy+=x*y;yy+=y*y;}
        float angle=.5f*Mathf.Atan2(2*xy,xx-yy);Vector3 normal=-axis*Mathf.Sin(angle)+other*Mathf.Cos(angle);
        if(Vector3.Dot(normal,refNormal)<0)normal=-normal;
        Quaternion basis=Quaternion.LookRotation(along,normal),inverse=Quaternion.Inverse(basis);
        float length=points.Max(p=>Vector3.Dot(p,along));
        drama.gripBasis=basis;drama.gripCenter=basis*(new Vector3(0,-.09f,.48f)*length);drama.gripRenderer=skin;
        string path="Assets/Chapter2/Props/Grip_"+actor.name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            var delta=new Vector3[vertices.Length];var deformed=(Vector3[])vertices.Clone();
            float start=.43f*length,radius=.11f*length;
            for(int i=0;i<vertices.Length;i++)
            {
                float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.75f,Weight(weights[i],included)));if(weight==0)continue;
                Vector3 q=inverse*bind.MultiplyPoint3x4(vertices[i]),closed=q;float s=Mathf.Max(0,q.z-start);
                if(s>0)
                {
                    float theta=Mathf.Min(Mathf.PI*1.15f,s/radius);
                    float blend=Mathf.SmoothStep(0,1,s/(length*.13f));
                    closed.y=Mathf.Lerp(q.y,-radius+(radius+q.y)*Mathf.Cos(theta),blend);
                    closed.z=Mathf.Lerp(q.z,start+(radius+q.y)*Mathf.Sin(theta),blend);
                }
                delta[i]=inv.MultiplyVector(basis*(closed-q))*weight;deformed[i]+=delta[i];
            }
            var normalsMesh=Object.Instantiate(source);normalsMesh.vertices=deformed;normalsMesh.RecalculateNormals();
            var nd=normalsMesh.normals;var oldNormals=source.normals;for(int i=0;i<nd.Length;i++)nd[i]-=oldNormals[i];Object.DestroyImmediate(normalsMesh);
            mesh=Object.Instantiate(source);mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);mesh.AddBlendShapeFrame("Knife grip",100,delta,nd,null);
            MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},path,false);Object.DestroyImmediate(mesh);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
        drama.gripMesh=mesh;drama.gripShape=mesh.GetBlendShapeIndex("Knife grip");
    }
    static float Weight(BoneWeight w,bool[] b)=>(b[w.boneIndex0]?w.weight0:0)+(b[w.boneIndex1]?w.weight1:0)+(b[w.boneIndex2]?w.weight2:0)+(b[w.boneIndex3]?w.weight3:0);
    static Transform Bone(Animator a,HumanBodyBones bone,params string[] names)
    {
        if(a.isHuman&&a.GetBoneTransform(bone))return a.GetBoneTransform(bone);
        return a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Any(n=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase)||t.name.EndsWith(":"+n,StringComparison.OrdinalIgnoreCase)));
    }
}
