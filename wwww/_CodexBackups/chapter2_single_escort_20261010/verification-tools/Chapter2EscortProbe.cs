using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2EscortProbe
{
    const string Dir="_CodexBackups/chapter2_single_escort_20261010/",Key="SingleEscortProbe";
    static Chapter2Controller c;static Report report;static string label;
    static float start,exploreAt,leadAt,releaseAt;static bool release,skipped,positioned,asked,frontShot,explored;
    static Vector3 before,viewBefore,guideBefore;static Quaternion rotationBefore;static int corner=1;static bool bothShot;
    static Vector3[] followPath;static float repath;
    [Serializable]public class Report
    {
        public string label,stage,answer,error;public int errors,warnings,participants;
        public float configuredSeconds,exploreElapsed,frontDistance,frontAngle,bodyJump,viewJump,guideMaxStep,witnessRadius,leadElapsed;
        public Vector3 frontViewport,playerPosition,guidePosition,followCorner;public bool finished,playing,dirty,allInside=true,allOutside=true,hintHidden=true;
        public bool noArrowsWhileApproaching=true,guideInFront,waited,arrived,patrolsStopped,rescue1,rescue2,treesFell,expiryWaited;
        public bool pathsComplete=true;public int pathsTested;public List<string> beats=new List<string>();
    }
    static Chapter2EscortProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string trace,LogType type)
    {
        string run=SessionState.GetString(Key,"");if(run=="")return;
        File.AppendAllText(Dir+run+"-log.txt",type+" "+m+"\n");
        if(type==LogType.Warning)SessionState.SetInt(Key+"w",SessionState.GetInt(Key+"w",0)+1);
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Key+"e",SessionState.GetInt(Key+"e",0)+1);
    }
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString(Key,"");if(label=="")return;
            c=null;report=new Report{label=label};start=Time.realtimeSinceStartup;leadAt=0;followPath=null;
            skipped=positioned=asked=frontShot=explored=bothShot=false;
        }
        if(s==PlayModeStateChange.ExitingPlayMode&&report!=null)
        {report.stage=c?c.CurrentStage.ToString():"missing";SessionState.SetString(Key+"report",JsonUtility.ToJson(report));}
        if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetString(Key,"")!="")
        {
            label=SessionState.GetString(Key,"");report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.errors=SessionState.GetInt(Key+"e",0);report.warnings=SessionState.GetInt(Key+"w",0);
            report.playing=Application.isPlaying;report.dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
            File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(report,true));SessionState.EraseString(Key);report=null;
        }
    }
    static void Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));release=true;releaseAt=Time.realtimeSinceStartup+.15f;}
    static void Shot(string name)
    {
        var camera=c.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(Dir+label+"-"+name+".png",image.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(release&&Time.realtimeSinceStartup>releaseAt){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(File.Exists(Dir+"command.txt"))
        {
            var cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            try
            {
                if(cmd=="apply")
                {
                    var owner=Object.FindFirstObjectByType<Chapter2Controller>();
                    if(EditorApplication.isPlaying)throw new Exception("Exit Play before authoring");
                    owner.exploration.interactionRadius=3;owner.explorationSeconds=180;
                    owner.forestEscort=owner.GetComponent<Chapter2ForestEscort>()??owner.gameObject.AddComponent<Chapter2ForestEscort>();
                    owner.forestEscort.Initialize(owner);
                    for(int i=1;i<owner.workers.Length;i++)
                    {var actor=owner.workers[i];var pos=owner.forestEscort.WitnessHome(i);pos.y=actor.transform.position.y;actor.transform.position=pos;EditorUtility.SetDirty(actor);EditorUtility.SetDirty(actor.GetComponent<Chapter2AmbientNPC>());}
                    EditorUtility.SetDirty(owner);EditorUtility.SetDirty(owner.exploration);EditorUtility.SetDirty(owner.forestEscort);
                    EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
                }
                else if(cmd=="clear")typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(null,null);
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd.StartsWith("test:")){SessionState.SetString(Key,cmd.Substring(5));SessionState.SetInt(Key+"e",0);SessionState.SetInt(Key+"w",0);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
                File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
            }catch(Exception e){File.WriteAllText(Dir+"last-command.txt",e.ToString());}
        }
        if(!Application.isPlaying||report==null)return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;c.saveResult=false;
            if(label!="protect-full")c.explorationSeconds=15;
            report.configuredSeconds=c.explorationSeconds;
        }
        if(Time.realtimeSinceStartup-start>600){report.error="Driver timed out at "+c.CameraBeat;EditorApplication.isPlaying=false;return;}
        if(!skipped&&Time.realtimeSinceStartup-start>1){Press(UnityEngine.InputSystem.Key.Space);skipped=true;}
        if(!report.beats.Contains(c.CameraBeat))report.beats.Add(c.CameraBeat);
        var ex=c.exploration;var escort=c.forestEscort;if(!ex||!escort)return;
        report.playerPosition=c.player.transform.position;report.guidePosition=c.workers[0].transform.position;
        if(ex.Active)
        {
            if(!explored){explored=true;exploreAt=Time.unscaledTime;}
            if(!positioned)
            {
                report.participants=ex.ParticipantCount;
                foreach(var npc in c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>())
                {
                    var position=npc.transform.position;position.y=.08f;
                    c.player.Warp(position+Vector3.right*3.01f,position);report.allOutside&=!ex.TryOpen(npc);
                    c.player.Warp(position+Vector3.right*2.99f,position);report.allInside&=ex.TryOpen(npc);ex.CloseChat();
                }
                var positions=new[]{new Vector3(12,.08f,-8),new Vector3(-14,.08f,5),new Vector3(0,.08f,19),new Vector3(25,.08f,24),new Vector3(-25,.08f,-15)};
                foreach(var p in positions){report.pathsComplete&=escort.TryPath(c.workers[0].transform.position,p,out _)&&escort.TryPath(p,c.WorkerDestination(0),out _);report.pathsTested++;}
                var spawn=label.Contains("west")?positions[1]:label.Contains("behind")?positions[2]:positions[0];
                c.player.Warp(spawn,spawn+(label.Contains("west")?Vector3.left:label.Contains("behind")?Vector3.forward:Vector3.right)*5);
                if(label=="protect-full")
                {
                    var npc=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().First(n=>n.name.Contains("小孩"));
                    var p=npc.transform.position+Vector3.right*2.5f;p.y=.08f;c.player.Warp(p,npc.transform.position);ex.TryOpen(npc);
                }
                before=c.player.transform.position;viewBefore=c.player.view.transform.position;rotationBefore=c.player.view.transform.rotation;
                guideBefore=c.workers[0].transform.position;positioned=true;Shot("exploration");
            }
            report.hintHidden&=string.IsNullOrEmpty(c.ui.hint.text);
            for(int i=1;i<c.workers.Length;i++)report.witnessRadius=Mathf.Max(report.witnessRadius,Vector3.ProjectOnPlane(c.workers[i].transform.position-escort.WitnessHome(i),Vector3.up).magnitude);
            if(label=="protect-full"&&!asked&&ex.RemainingSeconds<.4f)
            {ex.SubmitQuestion("我叫阿山。這片森林對你有什麼意義？");asked=true;}
            if(ex.RemainingSeconds<=0 && ex.WaitingForReply)report.expiryWaited=true;
            if(ex.LastReply!=null){report.answer=ex.LastReply;report.error=ex.LastError;}
        }
        if(escort.Approaching)
        {
            if(report.exploreElapsed==0)report.exploreElapsed=Time.unscaledTime-exploreAt;
            report.noArrowsWhileApproaching&=c.routeGuide.VisibleArrows==0;
            report.guideMaxStep=Mathf.Max(report.guideMaxStep,Vector3.ProjectOnPlane(c.workers[0].transform.position-guideBefore,Vector3.up).magnitude);
            guideBefore=c.workers[0].transform.position;
        }
        if(c.CameraBeat=="guide-in-front"&&!frontShot)
        {
            frontShot=true;var guide=c.workers[0];report.frontViewport=c.player.view.WorldToViewportPoint(guide.Rig.Head.position);
            report.frontDistance=Vector3.ProjectOnPlane(guide.transform.position-c.player.transform.position,Vector3.up).magnitude;
            report.frontAngle=Vector3.Angle(Vector3.ProjectOnPlane(c.player.view.transform.forward,Vector3.up),Vector3.ProjectOnPlane(guide.transform.position-c.player.view.transform.position,Vector3.up));
            report.bodyJump=Vector3.Distance(before,c.player.transform.position);report.viewJump=Vector3.Distance(viewBefore,c.player.view.transform.position);
            report.guideInFront=report.frontViewport.z>0&&report.frontViewport.x>.1f&&report.frontViewport.x<.9f&&report.frontViewport.y>.1f&&report.frontViewport.y<.95f;
            Shot("guide-in-front");
        }
        if(escort.Leading || c.routeGuide.EscortArrived&&c.CurrentStage==Chapter2Controller.Stage.Follow&&!c.Introducing)
        {
            if(leadAt==0){leadAt=Time.unscaledTime;Shot("escort-start");}
            if(escort.Waiting)report.waited=true;
            // Let the guide walk away and wait, then walk the actual CharacterController along its route.
            if(Time.unscaledTime-leadAt>7)
            {
                Vector3 target=c.routeGuide.EscortArrived?c.treeApproach.position:c.workers[0].transform.position;
                if(Time.unscaledTime>repath){repath=Time.unscaledTime+.5f;escort.TryPath(c.player.transform.position,target,out followPath);corner=1;}
                if(followPath!=null&&followPath.Length>1)
                {
                    corner=Mathf.Min(corner,followPath.Length-1);
                    if(Vector3.ProjectOnPlane(followPath[corner]-c.player.transform.position,Vector3.up).magnitude<.2f&&corner<followPath.Length-1)corner++;
                    var delta=Vector3.ProjectOnPlane(followPath[corner]-c.player.transform.position,Vector3.up);
                    report.followCorner=followPath[corner];
                    if(delta.magnitude>.08f){c.player.FocusOn(target+Vector3.up*1.3f);c.player.Move(Vector3.ClampMagnitude(delta,Time.deltaTime*2.6f));}
                }
            }
        }
        if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice)
        {
            report.arrived=c.ArrivedAtTree&&c.routeGuide.EscortArrived;report.leadElapsed=Time.unscaledTime-leadAt;
            report.patrolsStopped=c.workers.Skip(1).All(w=>!w.GetComponent<Chapter2AmbientNPC>().enabled);Shot("tree-choice");
            if(label.Contains("east")){report.finished=true;EditorApplication.isPlaying=false;return;}
            if(label.Contains("west"))c.Choose(1);else c.Choose(0);
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Chopping)
        {c.player.Warp(c.treeApproach.position+Vector3.up*.08f,c.sacredTree.position);if(c.RhythmPhase>.38f&&c.RhythmPhase<.60f)c.TryChop();}
        var r1=c.workers[1].GetComponent<Chapter2GriefReaction>();var r2=c.workers[2].GetComponent<Chapter2GriefReaction>();
        if(r1&&r1.Examining&&!report.rescue1){report.rescue1=true;Shot("rescue");}if(r2&&r2.Examining)report.rescue2=true;
        if(c.CameraBeat=="checking-casualty"&&!bothShot){bothShot=true;Shot("both-examining");}
        if(c.TreeFallProgress>=1){if(!report.treesFell)Shot("tree-fallen");report.treesFell=true;}
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting){report.finished=true;EditorApplication.isPlaying=false;}
        if(Time.frameCount%60==0)File.WriteAllText(Dir+label+"-progress.json",JsonUtility.ToJson(report,true));
    }
}
