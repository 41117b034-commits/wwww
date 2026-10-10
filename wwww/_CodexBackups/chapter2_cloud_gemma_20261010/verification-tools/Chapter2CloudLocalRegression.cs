using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Temporary local validation driver; removed from Assets after the trial.
[InitializeOnLoad]
public static class Chapter2CloudLocalRegression
{
    const string Dir="_CodexBackups/chapter2_cloud_gemma_20261010/local-regression/",Key="CloudLocalRegression";
    static Chapter2Controller c;
    static Report report;
    static float began,sentAt,nextAt,releaseAt;
    static bool skipped,release,waiting;
    static int index;
    static string[] questions={"你猜我叫甚麼名字"};
    [Serializable] public class Answer {public string npc,question,answer,error;public float seconds;}
    [Serializable] public class Report {public bool finished,playing,dirty;public int errors,warnings;public string stage,modelFile,error;public float configuredExploreSeconds;public List<Answer> answers=new List<Answer>();}
    static Chapter2CloudLocalRegression(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string text,string trace,LogType type)
    {
        if(!SessionState.GetBool(Key,false))return;
        File.AppendAllText(Dir+"unity-log.txt",type+" "+text+"\n");
        if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception)SessionState.SetInt(Key+"e",SessionState.GetInt(Key+"e",0)+1);
        if(type==LogType.Warning)SessionState.SetInt(Key+"w",SessionState.GetInt(Key+"w",0)+1);
    }
    static void State(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))
        {report=new Report();c=null;index=0;began=Time.realtimeSinceStartup;skipped=waiting=release=false;nextAt=0;}
        if(state==PlayModeStateChange.ExitingPlayMode&&report!=null)SessionState.SetString(Key+"report",JsonUtility.ToJson(report));
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false))
        {
            report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.playing=Application.isPlaying;report.dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
            report.errors=SessionState.GetInt(Key+"e",0);report.warnings=SessionState.GetInt(Key+"w",0);Save();
            SessionState.SetBool(Key,false);report=null;
        }
    }
    static void Save(){File.WriteAllText(Dir+"unity-result.json",JsonUtility.ToJson(report,true));}
    static void Shot(string name)
    {
        var camera=c.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Dir+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"unity-command.txt"))
        {
            var cmd=File.ReadAllText(Dir+"unity-command.txt").Trim();File.Delete(Dir+"unity-command.txt");
            if(cmd=="run") {SessionState.SetBool(Key,true);SessionState.SetInt(Key+"e",0);SessionState.SetInt(Key+"w",0);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
            if(cmd=="stop")EditorApplication.isPlaying=false;
            if(cmd=="clear")typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(null,null);
            File.WriteAllText(Dir+"unity-command-result.txt",cmd+" OK");
        }
        if(!Application.isPlaying||report==null)return;
        if(release&&Time.realtimeSinceStartup>releaseAt){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;c.saveResult=false;
            report.configuredExploreSeconds=c.explorationSeconds;c.explorationSeconds=600;
        }
        report.stage=c.CurrentStage.ToString();
        if(Time.realtimeSinceStartup-began>590){report.error="Trial timeout";Save();EditorApplication.isPlaying=false;return;}
        if(!skipped&&Time.realtimeSinceStartup-began>1)
        {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Space));releaseAt=Time.realtimeSinceStartup+.2f;release=true;skipped=true;}
        var ex=c.exploration;if(!ex||!ex.Active)return;
        report.modelFile=c.GetComponent<Chapter2LocalDialogue>().Provider;
        if(waiting)
        {
            if(ex.WaitingForReply)return;
            if(ex.LastReply==null&&ex.LastError==null)return;
            report.answers.Add(new Answer{npc=ex.Current.name,question=questions[index],answer=ex.LastReply,error=ex.LastError,seconds=Time.realtimeSinceStartup-sentAt});
            Shot("unity-answer-"+index);index++;waiting=false;nextAt=Time.realtimeSinceStartup+.5f;Save();
            if(index==questions.Length){report.finished=true;Save();EditorApplication.isPlaying=false;}
            return;
        }
        if(Time.realtimeSinceStartup<nextAt||index>=questions.Length)return;
        var actor=index<4?c.workers[0]:index<6?c.officer:c.dayGroup.GetComponentsInChildren<Chapter2Actor>().First(a=>a.isChild);
        var npc=actor.GetComponent<Chapter2AmbientNPC>();
        File.WriteAllText(Dir+"unity-persona-"+index+".txt",c.GetComponent<Chapter2LocalDialogue>().Persona(npc));
        if(ex.Current!=npc)
        {
            ex.CloseChat();var position=npc.transform.position+Vector3.right*2.2f;position.y=.08f;
            c.player.Warp(position,npc.transform.position);
            if(!ex.TryOpen(npc)){report.error="Cannot open "+actor.name;Save();EditorApplication.isPlaying=false;return;}
        }
        ex.QuestionInput.text=questions[index];
        var button=ex.QuestionInput.transform.parent.GetComponentsInChildren<Button>().First(b=>b.name=="Send question");
        button.onClick.Invoke();waiting=true;sentAt=Time.realtimeSinceStartup;Save();
    }
}

