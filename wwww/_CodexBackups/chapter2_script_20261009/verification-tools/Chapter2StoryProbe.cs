using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2StoryProbe
{
    const string Dir="_CodexBackups/chapter2_script_20261009/",Key="Chapter2StoryProbe",Prefs="WusheEvent.Chapter2.Result";
    static Chapter2Controller c;
    static Report report;
    static string label,line="",cue="";
    static float started,lineAt,releaseAt;
    static bool sent,shot,release,finished,voted;
    static readonly HashSet<string> shots=new HashSet<string>();
    [Serializable] public class Line { public string speaker,text,cue;public float time;public int visualLines; }
    [Serializable] public class Report
    {
        public string label,prefsBefore,prefsAfter,finalStage;
        public bool completed,playingAfterExit,dirty,allStanding,endingCard,approached,knifePlanted,prefsUnchanged;
        public float elapsed,knifeTipHeight,minApproachFireDistance=999;
        public int errors,warnings;
        public List<Line> lines=new List<Line>();public List<string> stages=new List<string>();
    }
    static Chapter2StoryProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string trace,LogType t)
    {
        if(string.IsNullOrEmpty(SessionState.GetString(Key,"")))return;
        File.AppendAllText(Dir+SessionState.GetString(Key,"")+"-log.txt",t+" "+m+"\n");
        if(t==LogType.Warning)SessionState.SetInt(Key+"warnings",SessionState.GetInt(Key+"warnings",0)+1);
        if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)SessionState.SetInt(Key+"errors",SessionState.GetInt(Key+"errors",0)+1);
    }
    static void State(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString(Key,"");if(label=="")return;
            c=null;line=cue="";sent=shot=release=finished=voted=false;shots.Clear();started=Time.time;
            report=new Report{label=label,prefsBefore=PlayerPrefs.GetString(Prefs,"")};
        }
        if(state==PlayModeStateChange.ExitingPlayMode&&report!=null)
        {
            report.completed=finished||(c&&c.Completed);report.finalStage=c?c.CurrentStage.ToString():"missing";report.elapsed=Time.time-started;
            SessionState.SetString(Key+"report",JsonUtility.ToJson(report));
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            label=SessionState.GetString(Key,"");if(label=="")return;
            report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.playingAfterExit=Application.isPlaying;report.dirty=SceneManager.GetActiveScene().isDirty;
            report.prefsAfter=PlayerPrefs.GetString(Prefs,"");report.prefsUnchanged=report.prefsBefore==report.prefsAfter;
            report.errors=SessionState.GetInt(Key+"errors",0);report.warnings=SessionState.GetInt(Key+"warnings",0);
            File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(report,true));
            SessionState.EraseString(Key);report=null;label="";
        }
    }
    static void Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));release=true;releaseAt=Time.realtimeSinceStartup+.2f;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(release&&Time.realtimeSinceStartup>releaseAt){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            try
            {
                if(cmd=="apply")Chapter2CouncilStoryAuthoring.Apply();
                else if(cmd=="console-clear")typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public).Invoke(null,null);
                else if(cmd=="open")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/第二章.unity");
                else if(cmd=="binary")
                {
                    foreach(string path in Directory.GetFiles("Assets","*.asset",SearchOption.AllDirectories))
                        if(new FileInfo(path).Length>=100000000||path.Contains("MonaRallyFist.asset")||path.Contains("GriefHands")&&path.EndsWith("-mesh.asset")||path.Contains("IncidentHutOpening.asset"))
                        {
                            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            if(mesh)Chapter2CouncilStoryAuthoring.SaveBinary(mesh);
                        }
                }
                else if(cmd=="refresh")AssetDatabase.Refresh();
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd=="inspect")File.WriteAllText(Dir+"state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" stage="+(c?c.CurrentStage.ToString():"none"));
                else if(cmd.StartsWith("test:"))
                {
                    label=cmd.Substring(5);SessionState.SetString(Key,label);SessionState.SetInt(Key+"errors",0);SessionState.SetInt(Key+"warnings",0);
                    EditorApplication.isPlaying=true;
                }
                else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
                File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
            }catch(Exception e){File.WriteAllText(Dir+"last-command.txt",e.ToString());Debug.LogException(e);}
        }
        if(!Application.isPlaying||report==null)return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            c.StageChanged+=s=>{report.stages.Add(s.ToString());if(s==Chapter2Controller.Stage.Complete)finished=true;};
        }
        if(!sent&&Time.time-started>1){Press(UnityEngine.InputSystem.Key.P);sent=true;}
        if(c.CurrentStage<Chapter2Controller.Stage.Meeting)return;
        if(c.ui.fade.color.a==0&&shots.Add("overview"))Shot(label+"-overview");
        if(c.CouncilCue!=cue){cue=c.CouncilCue;File.AppendAllText(Dir+label+"-cues.txt",Time.time+" "+cue+"\n");}
        string next=c.ui.subtitle.text;
        if(next!=line)
        {
            line=next;lineAt=Time.time;shot=false;
            if(line.Length>0)report.lines.Add(new Line{speaker=c.ui.speaker.text,text=line,cue=cue,time=Time.time});
        }
        if(!shot&&line.Length>0&&Time.time-lineAt>.85f)
        {
            shot=true;Canvas.ForceUpdateCanvases();report.lines[report.lines.Count-1].visualLines=c.ui.subtitle.cachedTextGenerator.lineCount;
            Shot(label+"-line-"+report.lines.Count.ToString("00"));
            File.WriteAllText(Dir+label+"-progress.json",JsonUtility.ToJson(report,true));
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Vote&&!voted){Shot(label+"-choice");Press(label.Contains("refuse")?UnityEngine.InputSystem.Key.Digit2:UnityEngine.InputSystem.Key.Digit1);voted=true;}
        var drama=c.tado?c.tado.GetComponent<Chapter2CouncilDrama>():null;
        if(drama&&drama.CurrentBeat==Chapter2CouncilDrama.Beat.Stab&&shots.Add("stab-start"))Shot(label+"-stab-start");
        if(drama&&drama.KnifePlanted)
        {
            report.knifePlanted=true;report.knifeTipHeight=drama.KnifeTipHeight;
            if(shots.Add("knife-planted"))Shot(label+"-knife-planted");
        }
        if(c.CouncilStandingTogether&&c.leaders.All(a=>!a.seated))
        {report.allStanding=true;if(shots.Add("standing"))Shot(label+"-standing");}
        if(c.ui.endingPanel.activeSelf){report.endingCard=true;if(shots.Add("card"))Shot(label+"-end-card");}
        report.approached|=c.RefusalApproachComplete;
        if(c.CameraBeat=="council-player-walk")report.minApproachFireDistance=Mathf.Min(report.minApproachFireDistance,Vector3.ProjectOnPlane(c.player.view.transform.position-c.campfire.position,Vector3.up).magnitude);
        if(c.CameraBeat=="council-pullback"&&shots.Add("pullback"))Shot(label+"-pullback");
        if(c.ui.fade.color.a>.99f&&c.CurrentStage==Chapter2Controller.Stage.Ending&&shots.Add("black"))Shot(label+"-black");
        if(Time.time-started>340){Debug.LogError("Story test timeout");EditorApplication.isPlaying=false;}
    }
    static void Shot(string name)
    {
        var c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
        var cam=c.player.view;var old=cam.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
}
