using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3GuidanceProbe
{
    const string Dir="_CodexBackups/chapter3_guidance_ready_20261013/",SessionKey="Chapter3GuidanceProbe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),replies=new List<string>();}
    static Report report;static Chapter3GuidanceProbeDriver driver;static float began,progressAt;static bool expectsExit;
    static Chapter3GuidanceProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=PlayChanged;Application.logMessageReceived+=Log;File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
    static void Log(string message,string stack,LogType type){if(report==null)return;if(type==LogType.Warning)report.warnings.Add(message);if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+stack);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="audit")Audit();
                else if(cmd=="stop"){SessionState.EraseString(SessionKey);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){SessionState.SetString(SessionKey,cmd);EditorApplication.isPlaying=true;}
                else throw new Exception("Unknown command "+cmd);
            }catch(Exception e){File.WriteAllText(Dir+"error-"+cmd+".txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(SessionKey,"")!=""&&!driver)driver=new GameObject("Temporary guidance verification").AddComponent<Chapter3GuidanceProbeDriver>();
    }
    static void PlayChanged(PlayModeStateChange change)
    {
        if(change==PlayModeStateChange.ExitingPlayMode&&expectsExit&&report!=null)
        {
            var f=Object.FindFirstObjectByType<Chapter3Flow>();
            report.checks.Add("game stops Play Mode after refusal="+(f.station.CurrentStage==Chapter3StationController.Stage.Ended&&f.Region=="Station"&&!f.station.HasRifle&&f.ui.fade.color.a==1));
            report.completed=report.checks.All(s=>!s.EndsWith("=False"));report.elapsed=Time.realtimeSinceStartup-began;
            File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.SetBool("guidance-ended",true);SessionState.EraseString(SessionKey);report=null;expectsExit=false;
        }
        if(change==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("guidance-ended",false))
        {File.WriteAllText(Dir+"returned-to-edit.txt","playing="+EditorApplication.isPlaying);SessionState.EraseBool("guidance-ended");}
    }
    public static IEnumerator Run()
    {
        Application.runInBackground=true;yield return null;
        string mode=SessionState.GetString(SessionKey,"");report=new Report{mode=mode};began=Time.realtimeSinceStartup;expectsExit=false;
        yield return Guard(Scenario(Object.FindFirstObjectByType<Chapter3Flow>(),mode));
        if(report!=null){report.completed=true;Finish();}
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool moved=false;object current=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());Finish();yield break;}
            if(!moved){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
        }
    }
    static IEnumerator Scenario(Chapter3Flow f,string mode)
    {
        var p=f.player;var v=f.village;var s=f.station;
        Check("single scene",SceneManager.sceneCount==1);
        if(mode=="test-village"||mode=="test-road")
        {
            v.lineSeconds=.3f;
            if(mode=="test-road")v.replyTimeout=.01f;
            yield return Wait(()=>v.CurrentStage==Chapter3Controller.Stage.Conversation,15,"village opening");
            Check("blank reply ignored",!v.SubmitReply("  ")&&v.CompletedTurns==0);
            Check("first reply accepted",v.SubmitReply("我準備好了，我們先去哪裡？"));
            Check("duplicate while pending blocked",!v.SubmitReply("好"));
            yield return Wait(()=>!v.WaitingForReply,32,"first reply");report.replies.Add("opening1 generated="+v.GeneratedReplies+" "+v.LastReply);
            Check("one reply is not enough",v.CompletedTurns==1&&!v.Depart());
            Check("second reply accepted",v.SubmitReply("我會跟緊你，也會照應比胡。"));
            yield return Wait(()=>!v.WaitingForReply,32,"second reply");report.replies.Add("opening2 generated="+v.GeneratedReplies+" "+v.LastReply);
            Check("exactly two replies",v.CompletedTurns==2&&!v.SubmitReply("第三次"));
            yield return new WaitForSecondsRealtime(2.1f);f.ui.depart.onClick.Invoke();yield return null;
            Check("two replies unlock knife",v.CurrentStage==Chapter3Controller.Stage.Equipment&&Arrow(f));Shot("knife-arrow");
            p.FocusOn(p.transform.position+Vector3.forward*8);yield return null;Shot("knife-arrow-side");
            Check("far pickup blocked",!v.TakeKnife());
            yield return Walk(p,new Vector3(-2.55f,0,2.6f),"reach knife");yield return Press(Key.E);
            Check("E collects knife and hides arrow",v.HasKnife&&!Arrow(f));
            if(mode=="test-road")
            {
                v.StopAllCoroutines();p.canMove=true;var boundary=new Chapter3RoadBoundary(f.villageRegion.transform);
                for(int i=2;i<v.trail.Length-1;i++)for(int side=-1;side<=1;side+=2)
                {
                    Vector3 center=Vector3.Lerp(v.trail[i],v.trail[i+1],.5f);Vector3 across=Vector3.Cross(Vector3.up,v.trail[i+1]-v.trail[i]).normalized*side;
                    p.Warp(center+Vector3.up*.1f,center+Vector3.up+Vector3.forward);yield return null;
                    for(float t=0;t<1.1f;t+=Time.deltaTime){p.Move(across*2.8f*Mathf.Min(Time.deltaTime,.04f));yield return null;}
                    Check("road segment "+i+" side "+side+" stays on visible road",boundary.Contains(p.transform.position,.22f)&&Flat(center,p.transform.position)>.5f&&Flat(center,p.transform.position)<1.6f&&Mathf.Abs(p.transform.position.y-center.y)<1);
                }
                p.Warp(v.trail[5]+Vector3.up*.1f,v.trail[6]+Vector3.up*1.65f);yield return null;Shot("road-narrowed");yield break;
            }
            yield return Wait(()=>v.CurrentStage==Chapter3Controller.Stage.Trail,15,"knife dialogue");
            yield return EscortUntil(f,()=>f.Region=="Station",v.bawan,110,"village route");
            Check("village reaches station",f.Region=="Station"&&SceneManager.sceneCount==1);yield break;
        }
        yield return Press(Key.F6);s.lineSeconds=.3f;
        yield return EscortUntil(f,()=>s.CurrentStage==Chapter3StationController.Stage.Equipment,s.bawan,55,"station arrival");
        Check("rifle arrow visible",Arrow(f));Shot("rifle-arrow");
        Check("departure locked without rifle",!s.ConfirmDeparture());
        yield return Walk(p,new Vector3(-4.8f,0,-1.7f),"reach rifle");yield return Press(Key.E);
        Check("rifle hides marker",s.HasRifle&&!Arrow(f));
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Regroup,15,"rifle explanation");
        yield return Walk(p,new Vector3(-2.3f,0,-2.3f),"return to Bawan");yield return Press(Key.E);
        Check("E opens free text instead of departing",s.CurrentStage==Chapter3StationController.Stage.Readiness&&f.ui.chat.activeSelf&&f.ui.input.interactable&&!p.canMove);
        Check("shared opening listener inactive",!v.SubmitReply("好"));Shot("readiness-question");
        Check("empty readiness blocked",!s.Readiness.SubmitReply("  "));
        foreach(string no in new[]{"不好","還沒","不願意","没有","not ready"})Check("negative intent "+no,Chapter3ReadinessConversation.ExplicitDecision(no)==Chapter3ReadinessConversation.Decision.NotReady);
        foreach(string yes in new[]{"是","好","準備好了","走吧","yes"})Check("positive intent "+yes,Chapter3ReadinessConversation.ExplicitDecision(yes)==Chapter3ReadinessConversation.Decision.Ready);
        if(mode=="test-cancel")
        {
            s.Readiness.SubmitReply("好，我會跟上隊伍。");yield return Press(Key.F8);yield return new WaitForSeconds(3);
            Check("pending reply cannot advance another region",f.Region=="School"&&!s.Readiness.Active&&!s.Readiness.WaitingForReply&&!Arrow(f));
            yield return Press(Key.F6);yield return null;
            Check("station reset after cancellation",!s.HasRifle&&s.CurrentStage==Chapter3StationController.Stage.Arrival&&!f.ui.chat.activeSelf&&s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Unclear);
            yield break;
        }
        if(mode=="test-ready"||mode=="test-offline")
        {
            if(mode=="test-offline")s.Readiness.replyTimeout=.01f;
            Check("unclear reply accepted",s.Readiness.SubmitReply("我們要去哪裡？"));yield return Wait(()=>!s.Readiness.WaitingForReply,32,"clarify readiness");
            report.replies.Add("clarify fallback="+s.Readiness.UsedFallback+" decision="+s.Readiness.LastDecision+" "+s.Readiness.LastReply);
            Check("unclear stays in conversation",s.CurrentStage==Chapter3StationController.Stage.Readiness&&s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Unclear);Shot("readiness-clarify");
            f.ui.input.text=mode=="test-offline"?"好":"雖然還是有點緊張，但我已經準備好了，我會跟大家走。";f.ui.send.onClick.Invoke();
            Check("ready duplicate blocked",!s.Readiness.SubmitReply("不好"));
            yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Departure,38,"ready departure");
            report.replies.Add("ready fallback="+s.Readiness.UsedFallback+" decision="+s.Readiness.LastDecision+" "+s.Readiness.LastReply);
            Check("affirmative continues story",s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Ready&&!f.ui.chat.activeSelf&&!s.RefusalCommitted);
            if(mode=="test-offline")yield break;
            yield return EscortUntil(f,()=>f.Region=="Combat",s.bawan,100,"station departure");
            Check("ready branch reaches combat in same scene",f.Region=="Combat"&&SceneManager.sceneCount==1);yield break;
        }
        string negative=mode=="test-refuse-offline"?"不好":"我還沒準備好，我想先回家。";
        if(mode=="test-refuse-offline")s.Readiness.replyTimeout=.01f;
        s.Readiness.SubmitReply(negative);
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Refused,38,"refusal begins");
        report.replies.Add("refuse fallback="+s.Readiness.UsedFallback+" decision="+s.Readiness.LastDecision+" "+s.Readiness.LastReply);
        Check("negative enters refusal",s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.NotReady&&!p.canMove&&!f.ui.chat.activeSelf);
        yield return Press(Key.F7);Check("refusal cannot skip into combat",f.Region=="Station");
        yield return Wait(()=>!string.IsNullOrEmpty(s.CurrentLine),15,"Mona approaches");yield return new WaitForSeconds(.4f);
        Check("Mona approaches and faces player",Flat(s.mona.transform.position,p.transform.position)<2.1f&&Vector3.Dot(s.mona.Rig.Forward,Vector3.ProjectOnPlane(p.transform.position-s.mona.transform.position,Vector3.up).normalized)>.9f);
        Check("angry face and gesture active",s.mona.GetComponent<Chapter3MonaRebuke>().Rebuking);Shot("mona-rebuke");
        yield return Wait(()=>s.CurrentLine!=null&&s.CurrentLine.Contains("回家休息"),20,"final rebuke");Shot("mona-send-home");
        Check("final authored line completes before stopping",Application.isPlaying&&!s.PartComplete&&f.Region=="Station");
        expectsExit=true;
        for(float t=0;t<20;t+=Time.unscaledDeltaTime){Progress("waiting for game-owned Play Mode stop");yield return null;}
        throw new Exception("Refusal did not stop Play Mode");
    }
    static bool Arrow(Chapter3Flow f){var a=f.ui.GetComponentInChildren<Chapter3PickupArrow>();return a&&a.Target&&a.gameObject.activeInHierarchy;}
    static IEnumerator EscortUntil(Chapter3Flow f,Func<bool> done,Chapter2Actor guide,float timeout,string label)
    {
        float start=Time.realtimeSinceStartup;
        while(!done())
        {
            if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Escort stuck "+label+" player="+f.player.transform.position+" guide="+guide.transform.position);
            Vector3 delta=Vector3.ProjectOnPlane(guide.transform.position-f.player.transform.position,Vector3.up);
            if(f.player.canMove&&delta.magnitude>1.7f){f.player.Move(delta.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));f.player.FocusOn(guide.transform.position+Vector3.up*1.3f);}
            Progress(label+" "+f.player.transform.position);yield return null;
        }
    }
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label)
    {
        float start=Time.realtimeSinceStartup;
        while(Flat(p.transform.position,target)>.22f)
        {if(Time.realtimeSinceStartup-start>22)throw new Exception("Walk stuck "+label+" at "+p.transform.position);Vector3 d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.Move(d.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));Progress(label);yield return null;}
    }
    static IEnumerator Wait(Func<bool> condition,float timeout,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(.12f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.12f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string status){if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;File.WriteAllText(Dir+"progress.txt",report.mode+" "+status);}
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish(){if(report==null)return;var f=Object.FindFirstObjectByType<Chapter3Flow>();if(f&&f.station.Readiness)report.replies.Add("final fallback="+f.station.Readiness.UsedFallback+" decision="+f.station.Readiness.LastDecision+" "+f.station.Readiness.LastReply);report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(SessionKey);report=null;expectsExit=false;EditorApplication.isPlaying=false;}
    static void Shot(string name)
    {
        var cam=Camera.main;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3GuidanceProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3GuidanceProbe.Run();}}
