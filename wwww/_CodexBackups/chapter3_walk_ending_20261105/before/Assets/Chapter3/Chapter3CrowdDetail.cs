using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Only SchoolAmbient background renderers. Mesh assets, transforms, materials,
// bones, collisions and story components retain their existing ownership.
[DefaultExecutionOrder(4900)]
public sealed class Chapter3CrowdDetail : MonoBehaviour
{
    sealed class Binding
    {
        public MeshFilter filter;
        public SkinnedMeshRenderer skin;
        public Mesh original,distant;
        public Chapter3SchoolCrowdActor crowd;
        public Transform owner;
        public bool far;
        public Mesh Current=>skin?skin.sharedMesh:filter?filter.sharedMesh:null;
        public void Set(Mesh value){if(skin)skin.sharedMesh=value;else if(filter)filter.sharedMesh=value;}
    }
    readonly List<Binding> bindings=new List<Binding>();
    readonly HashSet<int> known=new HashSet<int>();
    Chapter3CrowdDetailCatalog catalog;
    Chapter3SchoolAmbient ambient;
    Camera view;
    float nextDiscovery,discoveryUntil;
    int firstFrame;
    bool discoveryStarted;
    public bool DetailEnabled=true;
    public int BindingCount=>bindings.Count;
    public bool DiscoveryStarted=>discoveryStarted;
    public int DistantCount {get{int n=0;foreach(var b in bindings)if(b.far)n++;return n;}}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;
        for(int i=0;i<SceneManager.sceneCount;i++)InstallScene(SceneManager.GetSceneAt(i));
    }
    static void SceneLoaded(Scene scene,LoadSceneMode mode){InstallScene(scene);}
    static void InstallScene(Scene scene)
    {
        if(!scene.isLoaded)return;
        var data=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");
        if(!data||!data.automatic)return;
        foreach(var root in scene.GetRootGameObjects())
            foreach(var flow in root.GetComponentsInChildren<Chapter3Flow>(true))Install(flow,data);
    }
    // Shared entry for scene installation and explicit visual/performance checks.
    public static Chapter3CrowdDetail Install(Chapter3Flow flow,Chapter3CrowdDetailCatalog data)
    {
        if(!Application.isPlaying||!flow||!flow.school||!data)return null;
        var source=flow.school.GetComponentInChildren<Chapter3SchoolAmbient>(true);
        if(!source)return null;
        var detail=source.GetComponent<Chapter3CrowdDetail>();
        if(!detail)detail=source.gameObject.AddComponent<Chapter3CrowdDetail>();
        detail.Restore();detail.bindings.Clear();detail.known.Clear();
        detail.catalog=data;detail.ambient=source;detail.view=flow.player?flow.player.view:null;
        detail.ResetDiscovery();
        return detail;
    }
    void ResetDiscovery()
    {firstFrame=Time.frameCount;nextDiscovery=0;discoveryUntil=0;discoveryStarted=false;}
    void OnEnable(){ResetDiscovery();}
    void LateUpdate()
    {
        if(!catalog||!ambient||Time.frameCount<=firstFrame+1)return;
        if(!view||!view.isActiveAndEnabled){Restore();return;}
        // Begin the bounded window only after initialization and the view are
        // usable. Slow scene startup or an inactive camera cannot consume it.
        if(!discoveryStarted)
        {discoveryStarted=true;nextDiscovery=Time.unscaledTime;discoveryUntil=Time.unscaledTime+4;}
        // Start initializes all authored crowd, their native skins and the
        // three assailants before the first mesh replacement. A few bounded
        // retries also handle delayed initialization; no global frame scan.
        if(Time.unscaledTime>=nextDiscovery&&Time.unscaledTime<=discoveryUntil)
        {RefreshRenderers();nextDiscovery=Time.unscaledTime+1;}
        float enter=Mathf.Max(2,catalog.enterDistance),leave=Mathf.Clamp(catalog.leaveDistance,1,enter-.25f);
        Vector3 eye=view.transform.position;
        foreach(var b in bindings)
        {
            if(!b.owner||!b.original||!b.distant)continue;
            Mesh current=b.Current;
            if(current!=b.original&&current!=b.distant)continue; // another owner changed the mesh
            bool allowed=DetailEnabled&&b.owner.gameObject.activeInHierarchy&&(!b.crowd||!b.crowd.IsRouteCompanion);
            float distance=(b.owner.position-eye).sqrMagnitude;
            bool far=allowed&&(b.far?distance>leave*leave:distance>enter*enter);
            if(far==b.far&&current==(far?b.distant:b.original))continue;
            b.Set(far?b.distant:b.original);b.far=far;
        }
    }
    public void RefreshRenderers()
    {
        if(!ambient||!catalog)return;
        foreach(var filter in ambient.GetComponentsInChildren<MeshFilter>(true))
        {
            if(known.Contains(filter.GetInstanceID())||!filter.sharedMesh)continue;
            var crowd=filter.GetComponentInParent<Chapter3SchoolCrowdActor>();
            // Never replace a template before its native/fallback pose exists.
            if(!crowd||!crowd.HasPoseGeometry||crowd.IsRouteCompanion)continue;
            foreach(var entry in catalog.entries)
                if(entry.source==filter.sharedMesh&&entry.distant)
                {Add(filter,null,filter.sharedMesh,entry.distant,crowd,crowd.transform);break;}
        }
        foreach(var skin in ambient.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if(known.Contains(skin.GetInstanceID())||!skin.sharedMesh)continue;
            var crowd=skin.GetComponentInParent<Chapter3SchoolCrowdActor>();
            if(crowd&&(!crowd.HasPoseGeometry||crowd.IsRouteCompanion))continue;
            var runner=skin.GetComponentInParent<Chapter2Actor>();
            bool isRunner=false;
            if(runner&&ambient.runners!=null)foreach(var candidate in ambient.runners)if(runner==candidate){isRunner=true;break;}
            if(!crowd&&!isRunner)continue;
            foreach(var entry in catalog.entries)
            {
                Mesh replacement=null;
                if(entry.source==skin.sharedMesh&&entry.distant&&entry.sourceBones==skin.bones.Length)replacement=entry.distant;
                else if(crowd&&entry.posedDistant&&skin.bones.Length==15&&skin.sharedMesh.vertexCount==entry.sourceVertices
                    &&skin.sharedMesh.name==entry.source.name+" · shared crowd pose")
                {
                    // Match the authored source reference as well as the
                    // generated pose name; names alone are not an allowlist.
                    foreach(var original in crowd.GetComponentsInChildren<MeshFilter>(true))
                        if(original.sharedMesh==entry.source||original.sharedMesh==entry.distant){replacement=entry.posedDistant;break;}
                }
                if(replacement){Add(null,skin,skin.sharedMesh,replacement,crowd,crowd?crowd.transform:runner.transform);break;}
            }
        }
    }
    void Add(MeshFilter filter,SkinnedMeshRenderer skin,Mesh original,Mesh distant,Chapter3SchoolCrowdActor crowd,Transform owner)
    {
        known.Add(skin?skin.GetInstanceID():filter.GetInstanceID());
        bindings.Add(new Binding{filter=filter,skin=skin,original=original,distant=distant,crowd=crowd,owner=owner});
    }
    public void Restore()
    {
        foreach(var b in bindings)
        {if(b.original&&b.Current==b.distant)b.Set(b.original);b.far=false;}
    }
    void OnDisable(){Restore();}
    void OnDestroy(){Restore();}
}
