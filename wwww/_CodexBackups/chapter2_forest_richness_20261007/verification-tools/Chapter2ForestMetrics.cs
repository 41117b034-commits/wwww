using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2ForestMetrics
{
    const string Dir="_CodexBackups/chapter2_forest_richness_20261007/";
    static string label;
    static Chapter2AmbientNPC[] npcs;
    static Chapter2Controller chapter;
    static float next;
    static Metrics data;
    static Chapter2ForestMetrics(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
    static void State(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString("Chapter2PerformanceTest","");
            if(label=="")return;
            npcs=UnityEngine.Object.FindObjectsByType<Chapter2AmbientNPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(n=>n.name).ToArray();
            chapter=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
            next=0;data=new Metrics{label=label,npcCount=npcs.Length,names=npcs.Select(n=>n.name).ToArray(),maxTravel=new float[npcs.Length],trips=new int[npcs.Length],minFootY=100, maxFootY=-100,minimumPlayerSeparation=100};
        }
        if(state==PlayModeStateChange.EnteredEditMode&&data!=null)
        {Save();data=null;chapter=null;}
    }
    static void Tick()
    {
        if(!Application.isPlaying||!chapter||data==null||Time.time<next)return;
        next=Time.time+.2f;
        foreach(var pair in npcs.Select((npc,i)=>(npc,i)))
        {
            var npc=pair.npc;int i=pair.i;
            data.maxTravel[i]=Mathf.Max(data.maxTravel[i],npc.MaxDistanceFromHome);data.trips[i]=Mathf.Max(data.trips[i],npc.TripsCompleted);
            if(npc.gameObject.activeInHierarchy)
            {
                data.minimumPlayerSeparation=Mathf.Min(data.minimumPlayerSeparation,Vector3.ProjectOnPlane(npc.transform.position-chapter.player.transform.position,Vector3.up).magnitude);
                var animator=npc.GetComponentInChildren<Animator>();
                var l=Foot(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
                var r=Foot(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
                if(l&&r){float low=Mathf.Min(l.position.y,r.position.y);data.minFootY=Mathf.Min(data.minFootY,low);data.maxFootY=Mathf.Max(data.maxFootY,low);}
            }
        }
        if(chapter.CurrentStage==Chapter2Controller.Stage.Meeting)
        {
            data.hiddenAtNight=npcs.All(n=>!n.gameObject.activeInHierarchy);
            data.nightGround=GameObject.Find("Forest ground").GetComponent<Renderer>().sharedMaterial.name;
        }
        data.shaderErrors=ShaderUtil.GetShaderMessages(Shader.Find("Chapter2/Layered Forest Ground"))
            .Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
        data.triangles=UnityStats.triangles;data.batches=UnityStats.batches;
        if(chapter.CurrentStage==Chapter2Controller.Stage.Follow)Save();
    }
    static void Save(){File.WriteAllText(Dir+label+"-forest-metrics.json",JsonUtility.ToJson(data,true));}
    public static void Audit()
    {
        var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        string report="";
        foreach(var root in roots)
        {
            long vertices=root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh).Sum(m=>(long)m.sharedMesh.vertexCount)
                +root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(m=>m.sharedMesh).Sum(m=>(long)m.sharedMesh.vertexCount);
            report+=root.name+" vertices="+vertices+" renderers="+root.GetComponentsInChildren<Renderer>(true).Length+"\n";
        }
        var deps=AssetDatabase.GetDependencies("Assets/Scenes/第二章.unity",true);
        File.WriteAllText(Dir+"scene-dependencies.txt",string.Join("\n",deps));
        File.WriteAllText(Dir+"forest-scene-audit.txt",report);
    }
    static Transform Foot(Animator a,HumanBodyBones bone,params string[] names)
    {return a.isHuman?a.GetBoneTransform(bone):a.GetComponentsInChildren<Transform>().FirstOrDefault(t=>names.Any(n=>t.name.EndsWith(n,StringComparison.OrdinalIgnoreCase)));}
    [Serializable]class Metrics
    {
        public string label,nightGround;public string[] names,shaderErrors;
        public int npcCount,triangles,batches;public int[] trips;public float[] maxTravel;
        public float minFootY,maxFootY,minimumPlayerSeparation;public bool hiddenAtNight;
    }
}
