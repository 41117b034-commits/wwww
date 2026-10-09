using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2ExploreProbe
{
    const string Dir="_CodexBackups/chapter2_exploration_llm_20261009/",Key="ExploreProbe";
    static Chapter2Controller c;
    static Chapter2AmbientNPC[] npcs;
    static string label,line;
    static float started,exploreAt,stepAt,releaseAt,positionAt;
    static int step,index;
    static bool sent,release,expirySent,wasExploring;
    static Vector3[] workers;
    static Vector3 beforeEscort;
    [Serializable] public class Answer{public string actor,question,answer,error;public float seconds;}
    [Serializable] public class Report
    {
        public string label,finalStage,expiryReply,expiryError;
        public int count,errors,warnings;
        public bool noArrowsDuringExplore=true,workersWait=true,allPrompts=true,allOutsideHidden=true,insideAccepted=true,outsideRejected=true;
        public bool typingPDoesNotSkip,expiryWaited,returnedToEscort,arrivedAtTree,dirty,playingAfterExit;
        public bool forcedDeadlineWhilePending;
        public float configuredSeconds,explorationElapsed,escortFirstJump;
        public List<string> introduction=new List<string>();
        public List<Answer> answers=new List<Answer>();
    }
    static Report report;
    static Chapter2ExploreProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string message,string trace,LogType type)
    {
        string run=SessionState.GetString(Key,"");if(run=="")return;
        File.AppendAllText(Dir+run+"-log.txt",type+" "+message+"\n");
        if(type==LogType.Warning)SessionState.SetInt(Key+"warnings",SessionState.GetInt(Key+"warnings",0)+1);
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Key+"errors",SessionState.GetInt(Key+"errors",0)+1);
    }
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString(Key,"");if(label=="")return;
            report=new Report{label=label};c=null;step=index=0;sent=expirySent=wasExploring=false;line="";started=Time.realtimeSinceStartup;
        }
        if(s==PlayModeStateChange.ExitingPlayMode&&report!=null)
        {report.finalStage=c?c.CurrentStage.ToString():"missing";SessionState.SetString(Key+"report",JsonUtility.ToJson(report));}
        if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetString(Key,"")!="")
        {
            label=SessionState.GetString(Key,"");report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;report.playingAfterExit=Application.isPlaying;
            report.errors=SessionState.GetInt(Key+"errors",0);report.warnings=SessionState.GetInt(Key+"warnings",0);
            File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(report,true));SessionState.EraseString(Key);report=null;
        }
    }
    static void Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));release=true;releaseAt=Time.realtimeSinceStartup+.16f;}
    static void Warp(Chapter2AmbientNPC npc,float distance)
    {var p=npc.transform.position+Vector3.right*distance;p.y=.08f;c.player.Warp(p,npc.transform.position);}
    static void Shot(string name)
    {
        var camera=c.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(Dir+label+"-"+name+".png",image.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
    static void Submit(string question)
    {
        c.exploration.QuestionInput.text=question;
        GameObject.Find("Send question").GetComponent<Button>().onClick.Invoke();stepAt=Time.realtimeSinceStartup;
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(release&&Time.realtimeSinceStartup>releaseAt){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            try
            {
                if(cmd=="save")
                {
                    var chapter=Object.FindFirstObjectByType<Chapter2Controller>();chapter.explorationSeconds=180;
                    var authoredExploration=chapter.GetComponent<Chapter2Exploration>()??chapter.gameObject.AddComponent<Chapter2Exploration>();
                    authoredExploration.chapter=chapter;chapter.exploration=authoredExploration;
                    EditorUtility.SetDirty(chapter);EditorSceneManager.MarkSceneDirty(chapter.gameObject.scene);EditorSceneManager.SaveScene(chapter.gameObject.scene);
                }
                else if(cmd=="conversion")
                {
                    var convert=typeof(Chapter2LocalDialogue).GetMethod("Traditional",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                    var cases=new Dictionary<string,string>{{"确保秩序。","確保秩序。"},{"森林。","森林。"},{"a","a"},{"你好！","你好！"},{"",""}};
                    var failures=new List<string>();
                    for(int repeat=0;repeat<50;repeat++)foreach(var pair in cases)
                    {var value=(string)convert.Invoke(null,new object[]{pair.Key});if(value!=pair.Value)failures.Add(value);}
                    File.WriteAllText(Dir+"conversion-check.txt",failures.Count==0?"PASS: 250 exact native conversion checks":string.Join("\n",failures));
                }
                else if(cmd=="clear")typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(null,null);
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd.StartsWith("test:"))
                {SessionState.SetString(Key,cmd.Substring(5));SessionState.SetInt(Key+"errors",0);SessionState.SetInt(Key+"warnings",0);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
                File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
            }
            catch(Exception e){File.WriteAllText(Dir+"last-command.txt",e.ToString());}
        }
        if(!EditorApplication.isPlaying||report==null)return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            if(label.StartsWith("ui-"))
            {
                var local=c.GetComponent<Chapter2LocalDialogue>()??c.gameObject.AddComponent<Chapter2LocalDialogue>();local.settings.autoStart=false;
                c.explorationSeconds=35;
            }
            if(label=="expiry-pending")c.explorationSeconds=35;
            c.saveResult=false;report.configuredSeconds=c.explorationSeconds;
            workers=c.workers.Select(w=>w.transform.position).ToArray();
        }
        if(!sent&&Time.realtimeSinceStartup-started>1){Press(UnityEngine.InputSystem.Key.Space);sent=true;}
        if(c.Introducing && c.ui.subtitle.text!=line)
        {line=c.ui.subtitle.text;report.introduction.Add(line);}
        var ex=c.exploration;if(!ex)return;
        if(ex.Active)
        {
            if(!wasExploring)
            {
                wasExploring=true;exploreAt=Time.unscaledTime;stepAt=Time.realtimeSinceStartup;
                npcs=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().OrderBy(n=>n.name).ToArray();report.count=npcs.Length;Shot("start");
            }
            report.noArrowsDuringExplore&=c.routeGuide.VisibleArrows==0;
            report.workersWait&=c.workers.Select((w,i)=>Vector3.Distance(w.transform.position,workers[i])<.01f).All(x=>x);
            if(index<npcs.Length && (ex.RemainingSeconds>12 || step>=3))
            {
                var npc=npcs[index];
                if(step==0){Warp(npc,1.8f);step=1;stepAt=Time.realtimeSinceStartup;}
                else if(step==1 && Time.realtimeSinceStartup-stepAt>.3f)
                {
                    report.allOutsideHidden&=ex.Nearest==null;
                    Warp(npc,1.501f);report.outsideRejected&=!ex.TryOpen(npc);
                    Warp(npc,1.49f);report.insideAccepted&=ex.TryOpen(npc);ex.CloseChat();
                    Warp(npc,1.15f);step=2;stepAt=Time.realtimeSinceStartup;
                }
                else if(step==2 && Time.realtimeSinceStartup-stepAt>.3f)
                {report.allPrompts&=ex.Nearest==npc;Shot("prompt-"+index);Press(UnityEngine.InputSystem.Key.E);step=3;stepAt=Time.realtimeSinceStartup;}
                else if(step==3 && Time.realtimeSinceStartup-stepAt>.5f)
                {
                    if(!ex.ChatOpen){report.allPrompts=false;ex.TryOpen(npc);}
                    if(index==0)Press(UnityEngine.InputSystem.Key.P);
                    step=4;stepAt=Time.realtimeSinceStartup;
                }
                else if(step==4 && Time.realtimeSinceStartup-stepAt>.4f)
                {
                    if(index==0)report.typingPDoesNotSkip=c.CurrentStage==Chapter2Controller.Stage.Follow && ex.ChatOpen;
                    string question=npc.name.Contains("小孩")?"我叫阿山。你在這裡做什麼？":npc.GetComponent<Chapter2Actor>().police?(npc.name.EndsWith("2")?"你知道我的名字嗎？":"你為什麼在這裡？"):"這片森林對你有什麼意義？";
                    report.answers.Add(new Answer{actor=npc.name,question=question});Submit(question);step=5;
                    if(label=="expiry-pending")
                    {
                        typeof(Chapter2Exploration).GetField("deadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(ex,Time.unscaledTime+.1f);
                        report.forcedDeadlineWhilePending=true;expirySent=true;index=npcs.Length;
                    }
                }
                else if(step==5 && !ex.WaitingForReply && Time.realtimeSinceStartup-stepAt>1)
                {
                    var answer=report.answers.Last();answer.answer=ex.LastReply;answer.error=ex.LastError;answer.seconds=Time.realtimeSinceStartup-stepAt;
                    Shot("answer-"+index);ex.CloseChat();index++;step=0;
                }
            }
            if(index>=npcs.Length && !expirySent && ex.RemainingSeconds<=2 && ex.RemainingSeconds>.2f)
            {
                Warp(npcs[0],1.15f);ex.TryOpen(npcs[0]);Submit("你還記得我叫什麼名字嗎？");expirySent=true;
            }
            if(ex.RemainingSeconds<=0 && ex.WaitingForReply)report.expiryWaited=true;
            if(expirySent && !ex.WaitingForReply){report.expiryReply=ex.LastReply;report.expiryError=ex.LastError;}
            beforeEscort=c.player.transform.position;
            File.WriteAllText(Dir+label+"-progress.json",JsonUtility.ToJson(report,true));
        }
        else if(wasExploring)
        {
            if(report.explorationElapsed==0)report.explorationElapsed=Time.unscaledTime-exploreAt;
            if(!c.Introducing && c.CurrentStage==Chapter2Controller.Stage.Follow && c.routeGuide.GuidanceEnabled)
            {
                if(!report.returnedToEscort)
                {
                    report.returnedToEscort=true;report.escortFirstJump=Vector3.Distance(c.player.transform.position,beforeEscort);Shot("escort");positionAt=Time.realtimeSinceStartup;
                }
                if(Time.realtimeSinceStartup-positionAt>.5f)
                {
                    Vector3 at=c.routeGuide.EscortArrived?c.treeApproach.position:c.workers[0].transform.position+Vector3.back*2;
                    at.y=.08f;c.player.Warp(at,c.sacredTree.position);
                }
            }
            if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice)
            {report.arrivedAtTree=true;Shot("tree-choice");EditorApplication.isPlaying=false;}
        }
        if(Time.realtimeSinceStartup-started>480)EditorApplication.isPlaying=false;
    }
}
