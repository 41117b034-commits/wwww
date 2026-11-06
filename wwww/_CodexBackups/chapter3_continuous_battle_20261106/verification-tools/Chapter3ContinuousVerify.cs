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
public static partial class Chapter3ContinuousVerify
{
    const string Dir="_CodexBackups/chapter3_continuous_battle_20261106/",SessionKey="Chapter3ContinuousVerify";
    static Chapter3ContinuousDriver driver;
    static Chapter3Flow f;static Report report;static string mode;static bool background;
    [Serializable] sealed class Report {public bool completed;public List<string> checks=new List<string>(),observations=new List<string>(),errors=new List<string>();}
    static Chapter3ContinuousVerify()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            var c=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            if(c.StartsWith("run-")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity"&&!SceneManager.GetActiveScene().isDirty)
            {SessionState.SetString(SessionKey,c.Substring(4));EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
            else if(c=="fix-rocks"&&!EditorApplication.isPlayingOrWillChangePlaymode&&!SceneManager.GetActiveScene().isDirty){FixRocks();}
            else if(c=="audit"){File.WriteAllText(Dir+"audit.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);}
            else if(c=="stop"){SessionState.EraseString(SessionKey);EditorApplication.isPlaying=false;}
            else File.WriteAllText(Dir+"command-error.txt","Not clean compiled third chapter in Edit Mode");
        }
        if(Application.isPlaying&&!string.IsNullOrEmpty(SessionState.GetString(SessionKey,""))&&!driver)
            driver=new GameObject("Temporary opening and access verification").AddComponent<Chapter3ContinuousDriver>();
    }
    public static IEnumerator Run()
    {
        mode=SessionState.GetString(SessionKey,"");report=new Report();background=Application.runInBackground;Application.runInBackground=true;
        Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(ContinuousScenario());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){report.errors.Add(failure.ToString());yield return Shot("failure-"+mode);break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        report.completed=report.errors.Count==0;File.WriteAllText(Dir+"report-"+mode+".json",JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseString(SessionKey);EditorApplication.isPlaying=false;
    }
    static void Log(string m,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception)report.errors.Add(m);}
    static IEnumerator Keys(Key key,float seconds)
    {
        float until=Time.time+seconds;
        while(Time.time<until){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;}
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
    }
    static void Move(Vector3 target)
    {var delta=Vector3.ProjectOnPlane(target-f.player.transform.position,Vector3.up);f.player.Move(delta.normalized*Mathf.Min(delta.magnitude,2.8f*Time.deltaTime));f.player.FocusOn(target+Vector3.up*1.1f);}
    static IEnumerator Walk(Vector3 target,float radius,float timeout)
    {
        float until=Time.realtimeSinceStartup+timeout;
        while(Flat(f.player.transform.position,target)>radius){if(Time.realtimeSinceStartup>until)throw new Exception("Walk blocked at "+f.player.transform.position+" toward "+target);Move(target);yield return null;}
        Check("walked to "+target,true);
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Check(string name,bool passed){if(!passed)throw new Exception("FAILED: "+name);report.checks.Add(name);File.WriteAllText(Dir+"progress.txt",name);}
    static IEnumerator Wait(Func<bool> condition,float timeout,string label)
    {float until=Time.realtimeSinceStartup+timeout;while(!condition()){if(Time.realtimeSinceStartup>until)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Shot(string name)
    {
        if(!f)yield break;yield return new WaitForEndOfFrame();var camera=f.player.view;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Dir+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(texture);}
    }
}
public sealed class Chapter3ContinuousDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3ContinuousVerify.Run();} void LateUpdate(){Chapter3ContinuousVerify.MonitorParty();Chapter3ContinuousVerify.MonitorContinuity();}}
