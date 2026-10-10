using UnityEngine;
using UnityEditor;

// Bake in Edit Mode, where imported mesh data is available. Runtime only loads these assets.
public static class Chapter2FearFaceAuthoring
{
    const string FearShape="Chapter2 fear";
    [MenuItem("Tools/Chapter 2/Bake Fear Faces")]
    public static void Bake()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Bake faces outside Play Mode.");
        var controller=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!controller)throw new System.InvalidOperationException("Open Chapter 2 first.");
        foreach(var source in controller.workers)
        {
            var clone=Object.Instantiate(source.gameObject);clone.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var rig=clone.AddComponent<Chapter1IncidentRig>();
                rig.Initialize(clone.GetComponentInChildren<Animator>(),false);
                foreach(var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>())
                    BakeFearFace(skin,rig);
            }
            finally { Object.DestroyImmediate(clone); }
        }
        AssetDatabase.Refresh();
        Debug.Log("[Chapter2] Fear face assets baked in Edit Mode.");
    }
    // Save separate compressed meshes once; builds read them from Resources without editing the FBX.
    static Mesh BakeFearFace(SkinnedMeshRenderer skin,Chapter1IncidentRig rig)
    {
        Mesh original=skin.sharedMesh;int slot=System.Array.IndexOf(skin.bones,rig.Head);if(slot<0)return null;
        Vector3[] vertices=original.vertices,delta=new Vector3[original.vertexCount];var weights=original.boneWeights;
        Matrix4x4 bind=original.bindposes[slot],inverse=bind.inverse;
        Vector3 f=rig.Forward,r=Vector3.Cross(Vector3.up,f);float h=rig.Height,noseZ=float.NegativeInfinity,noseY=0;
        for(int i=0;i<vertices.Length;i++)
        {
            if(HeadWeight(weights[i],slot)<.65f)continue;
            Vector3 p=rig.Head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]))-rig.Head.position;
            if(Mathf.Abs(Vector3.Dot(p,r))>h*.027f||Mathf.Abs(p.y)>h*.09f)continue;
            float z=Vector3.Dot(p,f);if(z>noseZ){noseZ=z;noseY=p.y;}
        }
        if(float.IsNegativeInfinity(noseZ))return null;
        for(int i=0;i<vertices.Length;i++)
        {
            float weight=HeadWeight(weights[i],slot);if(weight<.5f)continue;
            Vector3 p=rig.Head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]))-rig.Head.position;
            float x=Vector3.Dot(p,r)/h,y=(p.y-noseY)/h,z=Vector3.Dot(p,f);
            float face=Mathf.SmoothStep(0,1,Mathf.InverseLerp(noseZ-h*.07f,noseZ-h*.018f,z));
            float brow=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.034f)/.027f,2)-Mathf.Pow((y-.034f)/.021f,2));
            float mouth=Mathf.Exp(-Mathf.Pow(x/.040f,4)-Mathf.Pow((y+.044f)/.023f,2));
            Vector3 offset=Vector3.up*h*(.007f*brow-.012f*mouth)*face*weight;
            delta[i]=inverse.MultiplyVector(rig.Head.InverseTransformVector(offset));
        }
        Mesh mesh=Object.Instantiate(original);mesh.name=original.name;
        mesh.AddBlendShapeFrame(FearShape,100,delta,new Vector3[vertices.Length],new Vector3[vertices.Length]);
        UnityEditor.MeshUtility.SetMeshCompression(mesh,UnityEditor.ModelImporterMeshCompression.Low);
        const string folder="Assets/Chapter2/Resources/FearFaces";
        System.IO.Directory.CreateDirectory(folder);string path=folder+"/"+original.name+".asset";
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},path,false);
        UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
        float largest=0;int changed=0;foreach(var d in delta){largest=Mathf.Max(largest,d.magnitude);if(d.sqrMagnitude>0.00000001f)changed++;}
        Debug.Log("[Chapter2] Fear mesh "+original.name+" vertices="+vertices.Length+" changed="+changed+" maxLocalDelta="+largest+" bytes="+new System.IO.FileInfo(path).Length);
        Object.DestroyImmediate(mesh);return UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }
    static float HeadWeight(BoneWeight w,int slot)
    {return(w.boneIndex0==slot?w.weight0:0)+(w.boneIndex1==slot?w.weight1:0)+(w.boneIndex2==slot?w.weight2:0)+(w.boneIndex3==slot?w.weight3:0);}
}
