using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Chapter2SpeechFaceAuthoring
{
    const string Folder="Assets/Chapter2/Poses/Speech";
    static readonly Dictionary<Mesh,Chapter2FaceCut> cache=new Dictionary<Mesh,Chapter2FaceCut>();
    [MenuItem("Tools/Chapter 2/Bake Story Speech and Expressions")]
    public static void Apply(){
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before baking speech.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();cache.Clear();
        foreach(var actor in c.dayGroup.GetComponentsInChildren<Chapter2Actor>(true).Concat(c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true))){
            if(actor.police)continue;Bake(actor);GC.Collect();
        }
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void Bake(Chapter2Actor actor){
        var animator=actor.GetComponentInChildren<Animator>(true);
        var head=Bone(animator,HumanBodyBones.Head,"Head");
        var left=Bone(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        var right=Bone(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        if(!head||!left||!right)throw new InvalidOperationException("No face rig: "+actor.name);
        Vector3 f=Vector3.Cross(right.position-left.position,Vector3.up).normalized,r=Vector3.Cross(Vector3.up,f);
        var face=actor.GetComponent<Chapter2SpeechFace>()??actor.gameObject.AddComponent<Chapter2SpeechFace>();
        if(face.surfaces!=null)foreach(var old in face.surfaces)if(old.renderer)Object.DestroyImmediate(old.renderer.gameObject);
        foreach(var t in head.GetComponentsInChildren<Transform>(true))if(t.name=="Story mouth interior")Object.DestroyImmediate(t.gameObject);
        var surfaces=new List<Chapter2SpeechFace.Surface>();
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
            int slot=Array.IndexOf(skin.bones,head);if(slot<0)continue;
            var source=skin.sharedMesh;float h=skin.bounds.size.y;
            var vertices=source.vertices;var weights=source.boneWeights;var bind=source.bindposes[slot];
            float nz=float.NegativeInfinity,ny=0,nx=0;
            for(int i=0;i<vertices.Length;i++){
                if(Weight(weights[i],slot)<.7f)continue;
                var p=head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]))-head.position;
                if(Mathf.Abs(Vector3.Dot(p,r))>h*.012f||Mathf.Abs(p.y)>h*.070f)continue;
                float z=Vector3.Dot(p,f);if(z>nz){nz=z;ny=p.y;nx=Vector3.Dot(p,r);}
            }
            if(float.IsNegativeInfinity(nz))throw new InvalidOperationException("Cannot calibrate face: "+actor.name);
            float mouthOffset=.018f;
            if(!cache.TryGetValue(source,out var cut)){
                cut=BuildPatch(source,skin,head,f,r,h,nx,ny,nz,mouthOffset);cache[source]=cut;
            }
            var go=new GameObject("Story face");go.transform.SetParent(skin.transform,false);
            var patch=go.AddComponent<SkinnedMeshRenderer>();patch.sharedMesh=cut.face;patch.bones=skin.bones;patch.rootBone=skin.rootBone;
            patch.sharedMaterials=skin.sharedMaterials.Select(m=>SpeechMaterial(m,true)).ToArray();
            skin.sharedMaterials=skin.sharedMaterials.Select(m=>SpeechMaterial(m,false)).ToArray();
            patch.updateWhenOffscreen=true;patch.localBounds=skin.localBounds;
            surfaces.Add(new Chapter2SpeechFace.Surface{body=skin,renderer=patch,cut=cut,head=head,origin=head.InverseTransformPoint(head.position+r*nx+Vector3.up*ny+f*nz),right=head.InverseTransformVector(r*h),up=head.InverseTransformVector(Vector3.up*h),forward=head.InverseTransformVector(f*h)});
        }
        face.surfaces=surfaces.ToArray();EditorUtility.SetDirty(face);
    }
    static Chapter2FaceCut BuildPatch(Mesh source,SkinnedMeshRenderer skin,Transform head,Vector3 f,Vector3 r,float h,float nx,float ny,float nz,float mouthOffset){
        var v=source.vertices;var w=source.boneWeights;int slot=Array.IndexOf(skin.bones,head);var bind=source.bindposes[slot];var inv=bind.inverse;
        var selected=new bool[v.Length];
        for(int i=0;i<v.Length;i++){
            var p=head.TransformPoint(bind.MultiplyPoint3x4(v[i]))-head.position;
            selected[i]=Weight(w[i],slot)>.7f&&Mathf.Abs(Vector3.Dot(p,r)-nx)<h*.078f&&p.y>ny-h*.085f&&p.y<ny+h*.082f&&Vector3.Dot(p,f)>nz-h*.072f;
        }
        var cut=ScriptableObject.CreateInstance<Chapter2FaceCut>();cut.removed=new Chapter2FaceCut.Submesh[source.subMeshCount];
        var indices=new List<int>();var map=new Dictionary<int,int>();var submeshes=new List<int[]>();
        for(int sub=0;sub<source.subMeshCount;sub++){
            var all=source.GetTriangles(sub);var removed=new List<int>();var triangles=new List<int>();
            for(int j=0;j<all.Length;j+=3){
                if(!selected[all[j]]&&!selected[all[j+1]]&&!selected[all[j+2]])continue;
                removed.Add(j);
                for(int k=0;k<3;k++){int original=all[j+k];if(!map.TryGetValue(original,out int mapped)){mapped=indices.Count;map[original]=mapped;indices.Add(original);}triangles.Add(mapped);}
            }
            cut.removed[sub]=new Chapter2FaceCut.Submesh{triangleStarts=removed.ToArray()};submeshes.Add(triangles.ToArray());
        }
        var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=indices.Select(i=>v[i]).ToArray();var normals=source.normals;mesh.normals=indices.Select(i=>normals[i]).ToArray();
        var uv=source.uv;mesh.uv=indices.Select(i=>uv[i]).ToArray();var tangents=source.tangents;if(tangents.Length==v.Length)mesh.tangents=indices.Select(i=>tangents[i]).ToArray();
        mesh.boneWeights=indices.Select(i=>w[i]).ToArray();mesh.bindposes=source.bindposes;mesh.subMeshCount=source.subMeshCount;
        for(int sub=0;sub<source.subMeshCount;sub++)mesh.SetTriangles(submeshes[sub],sub);mesh.RecalculateBounds();
        var jaw=new Vector3[indices.Count];var angry=new Vector3[indices.Count];var worry=new Vector3[indices.Count];var closed=new Vector3[indices.Count];
        for(int j=0;j<indices.Count;j++){
            int i=indices[j];var p=head.TransformPoint(bind.MultiplyPoint3x4(v[i]))-head.position;
            float x=(Vector3.Dot(p,r)-nx)/h,y=(p.y-ny)/h,z=Vector3.Dot(p,f);
            float front=Smooth(nz-h*.058f,nz-h*.018f,z)*Weight(w[i],slot);
            float lip=G(x/.025f,(y+mouthOffset+.011f)/.026f),lower=Smooth(-mouthOffset+.003f,-mouthOffset-.003f,y);
            Vector3 jd=(Vector3.down*(.009f*lip*lower)+f*(.002f*lip*lower))*h*front;
            float brow=G((Mathf.Abs(x)-.014f)/.012f,(y-.031f)/.009f),outer=G((Mathf.Abs(x)-.032f)/.013f,(y-.030f)/.010f),eye=G((Mathf.Abs(x)-.024f)/.015f,(y-.018f)/.009f);
            Vector3 ad=(-Vector3.up*.0055f*brow-r*Mathf.Sign(x)*.0025f*brow+Vector3.up*.0012f*outer)*h*front;
            Vector3 wd=(Vector3.up*(.0055f*brow-.002f*outer)-Vector3.up*.002f*G((Mathf.Abs(x)-.018f)/.012f,(y+.027f)/.01f))*h*front;
            Vector3 cd=Vector3.down*Mathf.Clamp(y-.018f,-.007f,.010f)*.85f*eye*h*front;
            jaw[j]=inv.MultiplyVector(head.InverseTransformVector(jd));angry[j]=inv.MultiplyVector(head.InverseTransformVector(ad));worry[j]=inv.MultiplyVector(head.InverseTransformVector(wd));closed[j]=inv.MultiplyVector(head.InverseTransformVector(cd));
        }
        mesh.AddBlendShapeFrame(Chapter2SpeechFace.Jaw,100,jaw,null,null);mesh.AddBlendShapeFrame(Chapter2SpeechFace.Angry,100,angry,null,null);mesh.AddBlendShapeFrame(Chapter2SpeechFace.Worried,100,worry,null,null);mesh.AddBlendShapeFrame(Chapter2SpeechFace.Closed,100,closed,null,null);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);string key=guid+"_"+id,path=Folder+"/"+key+".asset";mesh.name=key;
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},path,false);Object.DestroyImmediate(mesh);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);cut.face=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        string cutPath=Folder+"/"+key+"_cut.asset";var existing=AssetDatabase.LoadAssetAtPath<Chapter2FaceCut>(cutPath);
        if(existing){EditorUtility.CopySerialized(cut,existing);Object.DestroyImmediate(cut);return existing;}AssetDatabase.CreateAsset(cut,cutPath);return cut;
    }
    public static void UpdateMaterials(){
        foreach(var face in Object.FindObjectsByType<Chapter2SpeechFace>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            foreach(var surface in face.surfaces){surface.renderer.sharedMaterials=surface.body.sharedMaterials.Select(m=>SpeechMaterial(m,true)).ToArray();surface.body.sharedMaterials=surface.body.sharedMaterials.Select(m=>SpeechMaterial(m,false)).ToArray();}
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
    static Material SpeechMaterial(Material source,bool face){
        if(!face&&source.shader.name=="Chapter2/Story Body"){source.renderQueue=2600;return source;}
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
        string path=Folder+"/"+(face?"Face_":"Body_")+guid+"_"+id+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(source);mat.shader=Shader.Find(face?"Chapter2/Story Face":"Chapter2/Story Body");mat.renderQueue=face?2599:2600;AssetDatabase.CreateAsset(mat,path);}return mat;
    }
    static Transform Bone(Animator a,HumanBodyBones bone,params string[] names){if(a.isHuman&&a.GetBoneTransform(bone))return a.GetBoneTransform(bone);return a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Any(n=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase)||t.name.EndsWith(":"+n,StringComparison.OrdinalIgnoreCase)));}
    static float G(float x,float y)=>Mathf.Exp(-x*x-y*y);
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static float Weight(BoneWeight w,int s)=>(w.boneIndex0==s?w.weight0:0)+(w.boneIndex1==s?w.weight1:0)+(w.boneIndex2==s?w.weight2:0)+(w.boneIndex3==s?w.weight3:0);
}
