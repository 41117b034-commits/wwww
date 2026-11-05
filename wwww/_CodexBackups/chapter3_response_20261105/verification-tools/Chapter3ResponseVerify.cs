using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static partial class Chapter3ResponseVerify
{
    const string Dir="_CodexBackups/chapter3_response_20261105/",Key="Chapter3ResponseVerify.Run";
    static bool launched;
    static string State()=>"playing="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed;
    static Chapter3ResponseVerify(){EditorApplication.update+=Tick;File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O")+" "+State());EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt",State());};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&SessionState.GetBool(Key,false)&&!launched){launched=true;var go=new GameObject("Temporary response verification");Object.DontDestroyOnLoad(go);go.AddComponent<Chapter3ResponseDriver>();}
        if(!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        if(cmd=="refresh"){AssetDatabase.Refresh();return;}
        if(cmd=="build-detail"){Chapter3ActorDetailBuild.Build();return;}
        if(cmd=="rifle-detail"){Chapter3ActorDetailBuild.Rifle();return;}
        if(cmd=="status"){File.WriteAllText(Dir+"status.txt",State());return;}
        if(cmd=="final-audit")
        {
            var notes=new List<string>{State()};
            int missing=0;foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            notes.Add("missingScripts="+missing);
            var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");
            foreach(var e in catalog.entries)
            {
                if(!e.source||!e.distant||e.distant.vertexCount<3||e.distant.vertexCount>=e.sourceVertices||e.source.subMeshCount!=e.distant.subMeshCount||!e.source.bindposes.SequenceEqual(e.distant.bindposes))throw new Exception("Invalid final mesh entry");
                var a=new List<Vector4>();var b=new List<Vector4>();for(int ch=0;ch<8;ch++){e.source.GetUVs(ch,a);e.distant.GetUVs(ch,b);if(a.Count>0&&b.Count!=e.distant.vertexCount)throw new Exception("Missing final UV channel");}
                if(e.distant.normals.Length!=e.distant.vertexCount)throw new Exception("Missing final normals");
                notes.Add(e.source.name+" source="+e.sourceVertices+" distant="+e.distant.vertexCount+" submeshes="+e.distant.subMeshCount+" bones="+e.sourceBones+" posed="+(e.posedDistant?e.posedDistant.blendShapeCount:0));
            }
            notes.Add("completed="+(!EditorApplication.isPlaying&&!EditorUtility.scriptCompilationFailed&&!SceneManager.GetActiveScene().isDirty&&missing==0));File.WriteAllLines(Dir+"final-audit.txt",notes);return;
        }
        if(cmd=="gpu"&&!EditorApplication.isPlayingOrWillChangePlaymode){PlayerSettings.meshDeformation=MeshDeformation.GPUBatched;File.WriteAllText(Dir+"gpu-setting.txt",PlayerSettings.meshDeformation.ToString());return;}
        if(cmd=="cpu"&&!EditorApplication.isPlayingOrWillChangePlaymode){PlayerSettings.meshDeformation=MeshDeformation.CPU;File.WriteAllText(Dir+"gpu-setting.txt",PlayerSettings.meshDeformation.ToString());return;}
        if(cmd=="stop"){SessionState.EraseBool(Key);EditorApplication.isPlaying=false;return;}
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&!SceneManager.GetActiveScene().isDirty)
        {SessionState.SetString(Key+".mode",cmd);SessionState.SetBool(Key,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
        else File.WriteAllText(Dir+"command-error.txt",State());
    }
    [Serializable] public class Sample {public string label;public int frames,focused;public float medianMs,p95Ms,maxMs;public List<string> markers=new List<string>();}
    [Serializable] public class Report {public bool completed;public List<string> checks=new List<string>(),errors=new List<string>(),notes=new List<string>();public List<Sample> samples=new List<Sample>();}
    static Report report;static string mode;
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(obj,args);
    static void Check(string label,bool valid){if(!valid)throw new Exception(label);report.checks.Add(label);Save();}
    static void Save(){File.WriteAllText(Dir+"report-"+mode+".json",JsonUtility.ToJson(report,true));}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(text);}
    static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
    public static IEnumerator Run()
    {
        mode=SessionState.GetString(Key+".mode","baseline");report=new Report();bool background=Application.runInBackground;Application.runInBackground=true;
        Application.logMessageReceived+=Log;var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0){bool more=false;object next=null;Exception error=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        report.completed=report.errors.Count==0;Save();Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseBool(Key);EditorApplication.isPlaying=false;
    }
    static IEnumerator Scenario()
    {
        yield return Frames(12);var f=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal third chapter loaded",f&&SceneManager.GetActiveScene().name=="第三章");
        if(mode=="functional"){yield return Functional(f);yield break;}
        if(mode=="combat-pose"){yield return CombatAndPose(f);yield break;}
        report.notes.Add("GPU="+SystemInfo.graphicsDeviceName+" CPU="+SystemInfo.processorType+" RAM="+SystemInfo.systemMemorySize+" graphicsMemory="+SystemInfo.graphicsMemorySize+" deformation="+PlayerSettings.meshDeformation);
        report.notes.Add("screen="+Screen.width+"x"+Screen.height+" camera="+f.player.view.pixelWidth+"x"+f.player.view.pixelHeight+" target="+Application.targetFrameRate+" vsync="+QualitySettings.vSyncCount);
        var sign=f.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="往霧社的路標");
        report.notes.Add("sign position="+sign.position+" rotation="+sign.eulerAngles+" right="+sign.right+" text="+sign.GetComponentInChildren<TextMesh>(true).text);
        report.notes.Add("departure path="+string.Join(" / ",f.station.departurePath.Select(p=>p.ToString())));
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);foreach(var e in f.combat.Enemies)e.enabled=false;
        if(mode=="append-details"){yield return Frames(4);Chapter3ActorDetailBuild.Append(f);yield break;}
        if(mode=="final-perf")f.ui.fade.color=Color.clear;
        yield return new WaitForSeconds(3);Inventory(f,"mountain");yield return Measure("mountain optimized",5);
        if(mode=="render-diagnostic")
        {
            var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            float distance=pipeline.shadowDistance;int cascades=pipeline.shadowCascadeCount;
            pipeline.shadowDistance=22;pipeline.shadowCascadeCount=1;
            yield return Measure("shadow distance22 cascades1",5);
            var all=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);var states=all.Select(s=>s.enabled).ToArray();foreach(var s in all)s.enabled=false;
            yield return Measure("diagnostic all skins disabled",3);for(int i=0;i<all.Length;i++)all[i].enabled=states[i];
            pipeline.shadowDistance=distance;pipeline.shadowCascadeCount=cascades;
            foreach(var m in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.sharedMesh&&m.GetComponent<Renderer>()&&m.GetComponent<Renderer>().enabled).OrderByDescending(m=>m.sharedMesh.vertexCount).Take(15))report.notes.Add("static "+m.name+" mesh="+m.sharedMesh.name+" vertices="+m.sharedMesh.vertexCount+" distance="+Vector3.Distance(m.transform.position,f.player.transform.position)+" path="+AssetDatabase.GetAssetPath(m.sharedMesh));
        }
        if(mode=="final-perf")
        {
            var detail=f.GetComponent<Chapter3ActorDetail>();Check("automatic actor detail active",detail&&detail.BindingCount>0&&detail.DistantCount>0);
            detail.enabled=false;yield return Measure("mountain original body meshes",5);detail.enabled=true;yield return Measure("mountain optimized repeated",5);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+"mountain-optimized.png");
        }
        if(mode.StartsWith("cull"))
        {
            foreach(var s in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)){report.notes.Add("cull "+s.name+" bounds="+s.localBounds+" world="+s.bounds);s.updateWhenOffscreen=false;}
            yield return Measure("mountain offscreen skin culled",5);
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var old=lights.Select(l=>l.shadowCustomResolution).ToArray();var dist=QualitySettings.shadowDistance;
            foreach(var l in lights)l.shadowCustomResolution=512;QualitySettings.shadowDistance=20;
            yield return Measure("mountain smaller shadow map",4);
            for(int i=0;i<lights.Length;i++)lights[i].shadowCustomResolution=old[i];QualitySettings.shadowDistance=dist;
        }
        if(mode=="baseline")
        {
            var arms=Object.FindObjectsByType<Chapter3ViewArm>(FindObjectsSortMode.None);foreach(var a in arms)a.enabled=false;
            yield return Measure("mountain arm deform disabled",3);foreach(var a in arms)a.enabled=true;
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var shadows=lights.Select(l=>l.shadows).ToArray();foreach(var l in lights)l.shadows=LightShadows.None;
            yield return Measure("mountain shadows disabled",3);for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];
        }
        f.EnterSchool();yield return new WaitForSeconds(5);f.ui.Say("",null);f.player.FocusOn(new Vector3(0,1.5f,20));Inventory(f,"school");yield return Measure("school gate",5);
        report.notes.Add("Tado position="+f.school.tado.transform.position+" player="+f.player.transform.position);
        if(mode=="final-perf")
        {
            var detail=f.GetComponent<Chapter3ActorDetail>();detail.enabled=false;yield return Measure("school original body meshes",5);detail.enabled=true;yield return Measure("school optimized repeated",5);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+"school-optimized.png");
        }
        if(mode=="baseline")
        {
            var skins=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);var state=skins.Select(s=>s.enabled).ToArray();foreach(var s in skins)s.enabled=false;
            yield return Measure("school skins disabled",3);for(int i=0;i<skins.Length;i++)skins[i].enabled=state[i];
        }
    }
    static void Inventory(Chapter3Flow f,string label)
    {
        var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        report.notes.Add(label+" renderers="+renderers.Count(r=>r.enabled)+" skins="+renderers.OfType<SkinnedMeshRenderer>().Count(s=>s.enabled)+" colliders="+Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(c=>c.enabled)+" batches="+UnityStats.batches+" triangles="+UnityStats.triangles);
        foreach(var s in renderers.OfType<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.sharedMesh).OrderByDescending(s=>s.sharedMesh.vertexCount).Take(8))report.notes.Add(label+" skin "+s.name+" vertices="+s.sharedMesh.vertexCount+" offscreen="+s.updateWhenOffscreen+" mesh="+s.sharedMesh.name+" path="+AssetDatabase.GetAssetPath(s.sharedMesh)+" bones="+s.bones.Length+" actor="+s.GetComponentInParent<Chapter2Actor>());
        foreach(var a in Object.FindObjectsByType<Chapter3ViewArm>(FindObjectsSortMode.None))report.notes.Add("arm vertices="+a.GetComponent<MeshFilter>().sharedMesh.vertexCount);
    }
    static IEnumerator Measure(string label,float seconds)
    {
        yield return new WaitForSeconds(.5f);
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        var names=handles.Select(h=>ProfilerRecorderHandle.GetDescription(h)).Where(d=>d.UnitType==Unity.Profiling.ProfilerMarkerDataUnit.TimeNanoseconds).Select(d=>d.Name).Where(n=>System.Text.RegularExpressions.Regex.IsMatch(n,"PlayerLoop|LateUpdate|Physics|Skin|Animation|Animator|WaitFor|RenderLoop|RenderPipeline|Cull|Shadow|Camera.Render")).Distinct().ToArray();
        var recorders=names.Select(n=>Recorder.Get(n)).ToArray();var states=recorders.Select(r=>r.enabled).ToArray();foreach(var r in recorders)r.enabled=true;
        var totals=new double[names.Length];var times=new List<float>();var sample=new Sample{label=label};float end=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<end){times.Add(Time.unscaledDeltaTime);if(Application.isFocused)sample.focused++;for(int i=0;i<names.Length;i++)totals[i]+=recorders[i].elapsedNanoseconds/1000000.0;yield return null;}
        for(int i=0;i<recorders.Length;i++)recorders[i].enabled=states[i];times.Sort();sample.frames=times.Count;sample.medianMs=times[times.Count/2]*1000;sample.p95Ms=times[(int)(times.Count*.95)]*1000;sample.maxMs=times.Last()*1000;
        foreach(int i in Enumerable.Range(0,names.Length).OrderByDescending(i=>totals[i]).Take(22))if(totals[i]>0)sample.markers.Add(names[i]+"="+(totals[i]/times.Count).ToString("F3")+" ms");
        report.samples.Add(sample);Save();
    }
}
public sealed class Chapter3ResponseDriver:MonoBehaviour{IEnumerator Start(){yield return Chapter3ResponseVerify.Run();}}
