using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static partial class DialogueFireVerify
{
    const string Dir="_CodexBackups/dialogue_fire_response_20261106/",Key="DialogueFireVerify";
    static DialogueFireDriver driver;
    static Chapter3Flow f;
    static Report report;
    static string mode;
    [Serializable] sealed class Report {public bool completed;public List<string> checks=new List<string>(),notes=new List<string>(),errors=new List<string>();}
    static string State()=>"playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed+" scene="+SceneManager.GetActiveScene().path;
    static DialogueFireVerify()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt",State());};
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O")+" "+State());
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&!string.IsNullOrEmpty(SessionState.GetString(Key,""))&&!driver)
            driver=new GameObject("Temporary follow and input verification").AddComponent<DialogueFireDriver>();
        if(!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        if(cmd=="status"){File.WriteAllText(Dir+"status.txt",State());return;}
        if(cmd=="stop"){SessionState.EraseString(Key);EditorApplication.isPlaying=false;return;}
        if(cmd=="refresh"){AssetDatabase.Refresh();return;}
        if(cmd=="build-school-detail"){SchoolDetailBuild.Build();return;}
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&!SceneManager.GetActiveScene().isDirty&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity")
        {SessionState.SetString(Key,cmd);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
        else File.WriteAllText(Dir+"command-error.txt",State());
    }
    static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);
    static void Check(string label,bool ok){if(!ok)throw new Exception("FAILED: "+label);report.checks.Add(label);Save();}
    static void Save()=>File.WriteAllText(Dir+"report-"+mode+".json",JsonUtility.ToJson(report,true));
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message);}
    public static IEnumerator Run()
    {
        mode=SessionState.GetString(Key,"");report=new Report();bool background=Application.runInBackground;Application.runInBackground=true;Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0){bool more=false;object next=null;Exception error=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        report.completed=report.errors.Count==0;Save();Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseString(Key);EditorApplication.isPlaying=false;
    }
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {float end=Time.realtimeSinceStartup+seconds;while(!condition()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Frames(int count){while(count-->0)yield return null;}
    static IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var camera=f.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Dir+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(texture);}
    }
    static IEnumerator Scenario(){if(mode=="ui")yield return UIAudit();else if(mode=="input"){yield return Frames(5);f=Object.FindFirstObjectByType<Chapter3Flow>();yield return InputChecks();}else yield return Investigate();}
}
[DefaultExecutionOrder(-31000)] public sealed class DialogueFireDriver:MonoBehaviour {IEnumerator Start(){yield return DialogueFireVerify.Run();}void Update(){DialogueFireVerify.ObserveInput();}}
