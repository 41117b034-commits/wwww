using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Keep editable plants in the scene, but submit repeated static meshes in groups
// during play. Source renderers are restored if this component is disabled.
public sealed class Chapter2UnderstoryInstances : MonoBehaviour
{
    sealed class Batch
    {
        public Mesh mesh;
        public Material material;
        public int submesh;
        public ShadowCastingMode shadows;
        public Matrix4x4[] matrices;
        public RenderParams parameters;
    }
    readonly List<Batch> batches=new List<Batch>();
    readonly List<MeshRenderer> sources=new List<MeshRenderer>();
    void OnEnable()
    {
        if(!Application.isPlaying||!SystemInfo.supportsInstancing)return;
        var groups=new Dictionary<(Mesh,int,Material,ShadowCastingMode),List<Matrix4x4>>();
        foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            var filter=renderer.GetComponent<MeshFilter>();
            var materials=renderer.sharedMaterials;
            if(!renderer.enabled||!filter||!filter.sharedMesh||materials.Length!=filter.sharedMesh.subMeshCount)continue;
            bool valid=true;foreach(var material in materials)if(!material||!material.enableInstancing)valid=false;
            if(!valid)continue;
            for(int sub=0;sub<materials.Length;sub++)
            {
                var key=(filter.sharedMesh,sub,materials[sub],renderer.shadowCastingMode);
                if(!groups.TryGetValue(key,out var list))groups.Add(key,list=new List<Matrix4x4>());
                list.Add(renderer.localToWorldMatrix);
            }
            sources.Add(renderer);renderer.enabled=false;
        }
        foreach(var pair in groups)
        {
            // Stay below the per-draw matrix limit, including large future edits.
            for(int first=0;first<pair.Value.Count;first+=500)
            {
                var key=pair.Key;
                var parameters=new RenderParams(key.Item3){shadowCastingMode=key.Item4,receiveShadows=true,lightProbeUsage=LightProbeUsage.Off};
                batches.Add(new Batch{mesh=key.Item1,submesh=key.Item2,material=key.Item3,shadows=key.Item4,
                    matrices=pair.Value.GetRange(first,Mathf.Min(500,pair.Value.Count-first)).ToArray(),parameters=parameters});
            }
        }
    }
    void Update()
    {
        foreach(var batch in batches)Graphics.RenderMeshInstanced(batch.parameters,batch.mesh,batch.submesh,batch.matrices);
    }
    void OnDisable()
    {
        foreach(var renderer in sources)if(renderer)renderer.enabled=true;
        sources.Clear();batches.Clear();
    }
}
