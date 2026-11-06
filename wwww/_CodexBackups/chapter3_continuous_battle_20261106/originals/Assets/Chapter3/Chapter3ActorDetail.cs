using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Native story/combat bodies are extremely dense. Keep the original near the
// player, and use authored, bone-compatible derivatives beyond five metres.
// Separate face renderers, transforms, poses and colliders are untouched.
[DefaultExecutionOrder(4950)]
public sealed class Chapter3ActorDetail : MonoBehaviour
{
    sealed class Binding
    {
        public SkinnedMeshRenderer skin;
        public MeshFilter filter;
        public Mesh original,distant;
        public bool far;
        public int id;
        public Mesh Current=>skin?skin.sharedMesh:filter?filter.sharedMesh:null;
        public void Set(Mesh mesh)
        {
            if(filter){filter.sharedMesh=mesh;return;}
            Bounds bounds=skin.localBounds;
            var weights=new float[skin.sharedMesh.blendShapeCount];for(int i=0;i<weights.Length;i++)weights[i]=skin.GetBlendShapeWeight(i);
            skin.sharedMesh=mesh;skin.localBounds=bounds;for(int i=0;i<weights.Length;i++)skin.SetBlendShapeWeight(i,weights[i]);
        }
    }
    readonly List<Binding> bindings=new List<Binding>();
    readonly HashSet<int> known=new HashSet<int>();
    Chapter3Flow flow;
    Chapter3CrowdDetailCatalog catalog;
    string region;
    float nextDiscovery,discoveryUntil;
    public int BindingCount=>bindings.Count;
    public int DistantCount {get{int n=0;foreach(var b in bindings)if(b.far)n++;return n;}}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
        for(int i=0;i<SceneManager.sceneCount;i++)Loaded(SceneManager.GetSceneAt(i),LoadSceneMode.Additive);
    }
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(!scene.isLoaded)return;
        foreach(var root in scene.GetRootGameObjects())foreach(var f in root.GetComponentsInChildren<Chapter3Flow>(true))
        {
            if(f.GetComponent<Chapter3ActorDetail>())continue;
            var data=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");
            if(!data||!data.automatic)continue;
            var detail=f.gameObject.AddComponent<Chapter3ActorDetail>();detail.flow=f;detail.catalog=data;
        }
    }
    void LateUpdate()
    {
        if(!flow||!flow.player||!catalog)return;
        var battle=flow.school?flow.school.Battle:null;
        string state=flow.Region+(battle?"/"+battle.CurrentStage+"/"+battle.Step+"/"+(battle.Rescue?battle.Rescue.CurrentStage.ToString():""):"");
        if(region!=state){region=state;nextDiscovery=0;discoveryUntil=Time.unscaledTime+4;}
        if(Time.unscaledTime>=nextDiscovery&&Time.unscaledTime<=discoveryUntil){Discover();nextDiscovery=Time.unscaledTime+1;}
        Vector3 eye=flow.player.view.transform.position;
        foreach(var b in bindings)
        {
            if(!b.skin&&!b.filter)continue;
            if(b.Current!=b.original&&b.Current!=b.distant)continue;
            float limit=b.far?catalog.leaveDistance:catalog.enterDistance;
            var owner=b.skin?b.skin.transform:b.filter.transform;
            Vector3 centre=b.skin?b.skin.bounds.center:owner.position;
            bool far=owner.gameObject.activeInHierarchy&&(centre-eye).sqrMagnitude>limit*limit;
            if(far==b.far)continue;
            // localBounds and the bone array belong to the pose system.
            b.Set(far?b.distant:b.original);b.far=far;
        }
    }
    void Discover()
    {
        for(int i=bindings.Count-1;i>=0;i--)if(!bindings[i].skin&&!bindings[i].filter){known.Remove(bindings[i].id);bindings.RemoveAt(i);}
        foreach(var root in flow.gameObject.scene.GetRootGameObjects())
        foreach(var actor in root.GetComponentsInChildren<Chapter2Actor>(true))
        {
            if(!actor.isActiveAndEnabled||!actor.Rig)continue;
            // Existing background LOD owns spectators and runners.
            if(actor.GetComponentInParent<Chapter3SchoolAmbient>())continue;
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(known.Contains(skin.GetInstanceID()))continue;
                Mesh source=skin.sharedMesh,posed=null;
                if(actor.Rig&&actor.Rig.TryGetGripMeshes(skin,out var authored,out var grip)){source=authored;posed=grip;}
                foreach(var e in catalog.entries)
                    if(e.source==source&&e.sourceBones==skin.bones.Length)
                    {
                        Mesh distant=posed?e.posedDistant:e.distant;
                        if(!distant||skin.sharedMesh!=(posed?posed:source)||distant.blendShapeCount!=skin.sharedMesh.blendShapeCount)continue;
                        bindings.Add(new Binding{skin=skin,original=skin.sharedMesh,distant=distant,id=skin.GetInstanceID()});known.Add(skin.GetInstanceID());break;
                    }
            }
            var rifle=actor.GetComponent<Chapter2Rifle>();
            if(rifle&&rifle.weapon)foreach(var filter in rifle.weapon.GetComponentsInChildren<MeshFilter>(true))
            {
                if(known.Contains(filter.GetInstanceID()))continue;
                foreach(var e in catalog.entries)if(e.source==filter.sharedMesh&&e.sourceBones==0&&e.distant)
                {bindings.Add(new Binding{filter=filter,original=e.source,distant=e.distant,id=filter.GetInstanceID()});known.Add(filter.GetInstanceID());break;}
            }
        }
    }
    void OnDisable()
    {
        foreach(var b in bindings){if((b.skin||b.filter)&&b.Current==b.distant)b.Set(b.original);b.far=false;}
        region=null;
    }
}
