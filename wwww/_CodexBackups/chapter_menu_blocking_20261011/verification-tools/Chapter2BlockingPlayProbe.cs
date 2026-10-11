using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2BlockingPlayProbe
{
    const string Dir="_CodexBackups/chapter_menu_blocking_20261011/",Key="Chapter2BlockingPlayProbe";
    static Chapter2Controller c;static Report report;static float started,releaseAt,repath;static bool released=true,skipped,chosen,voted;
    static Vector3[] path;static int corner;static HashSet<string> shots=new HashSet<string>();
    [Serializable]public class Report
    {
        public bool completed,stopped,dirty,chapter2Loaded,nightIntroSeen,introGoneBeforeDialogue,guideOutsideChoppingView,postFallFaceVisible,postFallBodyVisible;
        public int errors,warnings,cuts;public float elapsed,minTrunkClearance=999,refusalMinFireDistance=999,maxRefusalSideDrift;
        public string label,prefsBefore,prefsAfter;public List<string> beats=new List<string>();public List<Vector3> refusalRoute=new List<Vector3>();
        public Vector3 chopHeadViewport,postFallHeadViewport;public Vector3[] workersAfterFall;public float[] trunkClearances;
    }
    static Chapter2BlockingPlayProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string trace,LogType t){if(report==null)return;if(t==LogType.Warning)report.warnings++;if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)report.errors++;}
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetString(Key,"")!="")
        {c=null;report=new Report{label=SessionState.GetString(Key,""),prefsBefore=PlayerPrefs.GetString("WusheEvent.Chapter2.Result","")};started=Time.realtimeSinceStartup;skipped=chosen=voted=false;shots.Clear();}
        if(s==PlayModeStateChange.ExitingPlayMode&&report!=null){report.elapsed=Time.realtimeSinceStartup-started;report.completed=c&&c.Completed;SessionState.SetString(Key+"report",JsonUtility.ToJson(report));}
        if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetString(Key,"")!="")
        {report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));report.stopped=!Application.isPlaying;report.dirty=SceneManager.GetActiveScene().isDirty;report.prefsAfter=PlayerPrefs.GetString("WusheEvent.Chapter2.Result","");File.WriteAllText(Dir+report.label+"-result.json",JsonUtility.ToJson(report,true));SessionState.EraseString(Key);report=null;}
    }
    static void Press(Key k){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(k));released=false;releaseAt=Time.realtimeSinceStartup+.15f;}
    static void Shot(string name){if(shots.Add(name))ChapterMenuBlockingProbe.Shot(report.label+"-"+name);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"day-command.txt"))
        {string label=File.ReadAllText(Dir+"day-command.txt").Trim();File.Delete(Dir+"day-command.txt");SessionState.SetString(Key,label);EditorApplication.isPlaying=true;}
        if(!Application.isPlaying||report==null)return;
        if(!released&&Time.realtimeSinceStartup>releaseAt){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());released=true;}
        if(Time.realtimeSinceStartup-started>540){Debug.LogError("Blocking probe timed out");EditorApplication.isPlaying=false;return;}
        if(!c)
        {c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;report.chapter2Loaded=c.gameObject.scene.path=="Assets/Scenes/第二章.unity";c.saveResult=false;c.explorationSeconds=3;started=Time.realtimeSinceStartup;}
        if(!skipped&&Time.realtimeSinceStartup-started>1){Press(UnityEngine.InputSystem.Key.Space);skipped=true;}
        if(!report.beats.Contains(c.CameraBeat))report.beats.Add(c.CameraBeat);
        var escort=c.forestEscort;
        if(escort&&(escort.Leading||c.routeGuide.EscortArrived&&c.CurrentStage==Chapter2Controller.Stage.Follow&&!c.Introducing))
        {
            Vector3 target=c.routeGuide.EscortArrived?c.treeApproach.position:c.workers[0].transform.position;
            if(Time.unscaledTime>repath){repath=Time.unscaledTime+.4f;escort.TryPath(c.player.transform.position,target,out path);corner=1;}
            if(path!=null&&path.Length>1)
            {corner=Mathf.Min(corner,path.Length-1);if(Vector3.ProjectOnPlane(path[corner]-c.player.transform.position,Vector3.up).magnitude<.2f&&corner<path.Length-1)corner++;
                var d=Vector3.ProjectOnPlane(path[corner]-c.player.transform.position,Vector3.up);if(d.magnitude>.06f){c.player.FocusOn(target+Vector3.up*1.3f);c.player.Move(Vector3.ClampMagnitude(d,Time.deltaTime*2.6f));}}
        }
        if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice&&!chosen){chosen=true;Press(UnityEngine.InputSystem.Key.Digit2);}
        if(c.CurrentStage==Chapter2Controller.Stage.Chopping)
        {
            if(!shots.Contains("chopping"))
            {report.chopHeadViewport=c.player.view.WorldToViewportPoint(c.workers[0].Rig.Head.position);var v=report.chopHeadViewport;report.guideOutsideChoppingView=v.z<=0||v.x<0||v.x>1;Shot("chopping");}
            c.player.Warp(c.treeApproach.position+Vector3.up*.08f,c.sacredTree.position);
            if(c.RhythmPhase>.42f&&c.RhythmPhase<.58f)c.TryChop();report.cuts=c.ValidCuts;
        }
        if(c.TreeFallProgress==1&&c.ui.subtitle.text.StartsWith("命令完成")&&!shots.Contains("fallen"))
        {
            report.workersAfterFall=c.workers.Select(a=>a.transform.position).ToArray();report.trunkClearances=new float[c.workers.Length];
            Vector3 trunkTop=c.sacredTree.position+c.sacredTree.up*20;
            for(int i=0;i<c.workers.Length;i++)
            {
                Vector3 body=c.workers[i].transform.position+Vector3.up*.9f;
                Vector3 segment=trunkTop-c.sacredTree.position;Vector3 near=c.sacredTree.position+segment*Mathf.Clamp01(Vector3.Dot(body-c.sacredTree.position,segment)/segment.sqrMagnitude);
                float clearance=Vector3.Distance(body,near)-2.32f-.35f;report.trunkClearances[i]=clearance;report.minTrunkClearance=Mathf.Min(report.minTrunkClearance,clearance);
            }
            Vector3 head=c.workers[0].Rig.Head.position;report.postFallHeadViewport=c.player.view.WorldToViewportPoint(head);var v=report.postFallHeadViewport;
            report.postFallFaceVisible=v.z>0&&v.x>.05f&&v.x<.95f&&v.y>.05f&&v.y<.95f;
            report.postFallBodyVisible=VisiblePastTree(head)&&VisiblePastTree(c.workers[0].Rig.Hips.position);Shot("fallen");
        }
        var intro=c.ui.canvas.transform.Find("夜景地點介紹");
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting&&intro&&intro.gameObject.activeSelf&&c.ui.fade.color.a==0){report.nightIntroSeen=true;Shot("night-intro");}
        if(c.CouncilLineIndex>0&&shots.Add("intro-check"))report.introGoneBeforeDialogue=intro&&!intro.gameObject.activeSelf;
        if(c.CurrentStage==Chapter2Controller.Stage.Vote&&!voted){voted=true;Press(report.label.Contains("refuse")?UnityEngine.InputSystem.Key.Digit2:UnityEngine.InputSystem.Key.Digit1);}
        if(c.CameraBeat=="council-player-walk")
        {Vector3 p=c.player.transform.position;report.refusalRoute.Add(p);report.maxRefusalSideDrift=Mathf.Max(report.maxRefusalSideDrift,Mathf.Abs(p.x-1.55f));report.refusalMinFireDistance=Mathf.Min(report.refusalMinFireDistance,Vector3.ProjectOnPlane(p-c.campfire.position,Vector3.up).magnitude);if(p.z>-.5f)Shot("refusal-walk");}
    }
    static bool VisiblePastTree(Vector3 target)
    {
        Ray ray=new Ray(c.player.view.transform.position,(target-c.player.view.transform.position).normalized);float distance=Vector3.Distance(target,ray.origin);
        // Raycast the disabled trunk colliders explicitly, temporarily restoring their state.
        foreach(var collider in c.sacredTree.GetComponentsInChildren<Collider>())
        {bool old=collider.enabled;collider.enabled=true;Physics.SyncTransforms();bool blocked=collider.Raycast(ray,out _,distance);collider.enabled=old;if(blocked)return false;}
        return true;
    }
}
