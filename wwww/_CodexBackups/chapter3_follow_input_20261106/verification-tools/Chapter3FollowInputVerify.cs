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
public static partial class Chapter3FollowInputVerify
{
    const string Dir="_CodexBackups/chapter3_follow_input_20261106/",Key="Chapter3FollowInputVerify";
    static Chapter3FollowInputDriver driver;
    static Chapter3Flow f;
    static Report report;
    static string mode;
    [Serializable] sealed class Report {public bool completed;public List<string> checks=new List<string>(),notes=new List<string>(),errors=new List<string>();}
    static string State()=>"playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed+" scene="+SceneManager.GetActiveScene().path;
    static Chapter3FollowInputVerify()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt",State());};
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O")+" "+State());
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&!string.IsNullOrEmpty(SessionState.GetString(Key,""))&&!driver)
            driver=new GameObject("Temporary follow and input verification").AddComponent<Chapter3FollowInputDriver>();
        if(!File.Exists(Dir+"probe-command.txt"))return;
        string cmd=File.ReadAllText(Dir+"probe-command.txt").Trim();File.Delete(Dir+"probe-command.txt");
        if(cmd=="status"){File.WriteAllText(Dir+"status.txt",State());return;}
        if(cmd=="stop"){SessionState.EraseString(Key);EditorApplication.isPlaying=false;return;}
        if(cmd=="refresh"){AssetDatabase.Refresh();return;}
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
    static IEnumerator Scenario()
    {
        yield return Frames(5);f=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal scene loaded",f&&f.startAtStation);
        if(mode.StartsWith("input")){yield return InputChecks();yield break;}
        if(mode=="navigation"){yield return NavigationChecks();yield break;}
        if(mode.StartsWith("foliage-continuous")){yield return ContinuousFoliage();yield break;}
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);foreach(var e in f.combat.Enemies){e.AttackEnabled=false;e.HoldFire(1000);}
        f.ui.fade.color=Color.clear;f.ui.Say("",null);yield return new WaitForSeconds(2);
        if(mode=="foliage-audit"){yield return FoliageAudit();yield break;}
        foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("交戰道路蕨草 ")||t.name.StartsWith("交戰道路林木 ")||t.name.StartsWith("林路樹木 ")||t.name.StartsWith("林路蕨草 ")))
        {
            var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            float ground=-999;foreach(var hit in Physics.RaycastAll(new Vector3(bounds.center.x,30,bounds.center.z),Vector3.down,60))
                if(!hit.transform.IsChildOf(t)&&hit.normal.y>.6f&&hit.collider.GetComponentInParent<Chapter2Actor>()==null)ground=Mathf.Max(ground,hit.point.y);
            if(bounds.min.y>.2f||bounds.min.y-ground>.3f)report.notes.Add(t.name+" position="+t.position+" bounds="+bounds+" ground="+ground+" gap="+(bounds.min.y-ground));
        }
        foreach(var t in f.combatRegion.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("地形")||t.name=="山路地面"))report.notes.Add("terrain "+t.name+" pos="+t.position+" colliders="+t.GetComponents<Collider>().Length);
        if(mode=="plants-final")
        {
            var env=f.GetComponent<Chapter3ApproachEnvironment>();int count=0;
            foreach(Transform t in env.Root)
            {
                if(!t.name.StartsWith("林路樹木 ")&&!t.name.StartsWith("林路蕨草 "))continue;
                var rs=t.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                float ground=(float)Call(env,"PlantGround",bounds.center);
                Check(t.name+" grounded on real terrain",Mathf.Abs(bounds.min.y-ground)<.025f);count++;
            }
            Check("all 32 connecting plants checked",count==32);
        }
        yield return Capture("mountain-"+mode);
        f.player.Warp(f.combat.WorldPoint(new Vector3(0,.06f,13)),f.combat.WorldPoint(new Vector3(2,2,32)));yield return Frames(3);yield return Capture("mountain-north-"+mode);
        Save();
    }
}
public sealed class Chapter3FollowInputDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3FollowInputVerify.Run();}}
