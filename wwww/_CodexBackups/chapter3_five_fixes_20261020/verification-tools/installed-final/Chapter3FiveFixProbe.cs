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

// Temporary, task-scoped Editor verification. Archive outside Assets after use.
[InitializeOnLoad]
public static partial class Chapter3FiveFixProbe
{
    const string Dir="_CodexBackups/chapter3_five_fixes_20261020/", State="Chapter3FiveFixProbe";
    static Chapter3Flow flow;
    static Chapter3FiveFixDriver driver;
    static Report report;
    static float began;
    static bool oldBackground;
    static InputSettings originalInput,testInput;
    static Keyboard addedKeyboard;
    [Serializable] sealed class Report
    {
        public string mode,started;
        public bool completed;
        public float elapsed;
        public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();
    }
    [Serializable] sealed class Save {public string key,value;public bool existed;}
    [Serializable] sealed class Saves {public Save[] values;}
    static Chapter3FiveFixProbe()
    {
        Directory.CreateDirectory(Dir);
        EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)Cleanup();if(s==PlayModeStateChange.EnteredEditMode)Audit();};
        AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
        if(!EditorApplication.isPlayingOrWillChangePlaymode)RestoreSave();
        File.WriteAllText(Dir+"probe-ready.txt",DateTime.UtcNow.ToString("O"));
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string command;
            try{command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(command=="audit")Audit();
                else if(command=="inspect")Inspect();
                else if(command=="capture"&&driver)driver.StartCoroutine(Shot("native-input"));
                else if(command=="finish-native"&&report!=null&&report.mode=="native-input"){report.completed=report.errors.Count==0;Finish();}
                else if(command=="stop"){SessionState.EraseString(State);Cleanup();report=null;EditorApplication.isPlaying=false;}
                else if(command.StartsWith("test-")||command=="diagnose"||command=="native-input")
                {
                    if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("Require stable compiled Edit Mode");
                    var scene=SceneManager.GetActiveScene();
                    if(scene.path!="Assets/Scenes/第三章.unity"||scene.isDirty)throw new Exception("Require current clean third chapter");
                    BackupSave();SessionState.SetString(State,command);EditorApplication.isPlaying=true;
                }
                else throw new Exception("Unknown command: "+command);
            }
            catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&!driver&&!string.IsNullOrEmpty(SessionState.GetString(State,"")))
            driver=new GameObject("Temporary five requested fixes verification").AddComponent<Chapter3FiveFixDriver>();
    }
    public static IEnumerator Run()
    {
        report=new Report{mode=SessionState.GetString(State,""),started=DateTime.UtcNow.ToString("O")};began=Time.realtimeSinceStartup;
        oldBackground=Application.runInBackground;Application.runInBackground=true;
        originalInput=InputSystem.settings;testInput=Object.Instantiate(originalInput);
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInput;
        if(Keyboard.current==null)addedKeyboard=InputSystem.AddDevice<Keyboard>();
        yield return Frames(2);flow=Object.FindFirstObjectByType<Chapter3Flow>();
        Note("Editor driver verification; targeted setup/pose/input fixtures are explicitly reported. No VR or standalone claim.");
        IEnumerator scenario;
        if(report.mode=="test-input"||report.mode=="native-input")scenario=InputScenario();
        else if(report.mode=="diagnose")scenario=Diagnose();
        else
        {
            string name=report.mode=="test-movement"?"MovementScenario":"CombatScenario";
            var method=typeof(Chapter3FiveFixProbe).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
            scenario=method!=null?(IEnumerator)method.Invoke(null,null):Missing(name);
        }
        yield return Guard(scenario);
        if(report!=null)
        {
            report.completed=report.errors.Count==0;
            if(report.mode=="native-input"&&report.completed){File.WriteAllText(Dir+"native-ready.txt","Press physical E in Game view; inspect/capture when ready.");yield break;}
            Finish();
        }
    }
    static IEnumerator Missing(string name){throw new Exception("Missing scenario "+name);}
    static IEnumerator Guard(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null)
            {
                report.errors.Add(failure.ToString());Inspect();Finish();yield break;
            }
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator sub)stack.Push(sub);else yield return next;
        }
    }
    static IEnumerator InputScenario()
    {
        yield return Press(Key.F6);
        yield return Wait(()=>flow.station.Opening&&flow.station.Opening.Active,20,"opening conversation");
        var station=flow.station;var ui=station.ui;
        station.Opening.replyTimeout=.01f;
        Check("first opening answer accepted",station.Opening.SubmitReply("我會和同伴彼此照應。"));
        yield return Wait(()=>!station.Opening.WaitingForReply,10,"first fallback");
        Check("second opening answer accepted",station.Opening.SubmitReply("準備好了。"));
        yield return Wait(()=>station.CurrentStage==Chapter3StationController.Stage.Approach,25,"escort starts");
        bool welcome=false,shot=false;float start=Time.realtimeSinceStartup;
        while(station.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
        {
            if(Time.realtimeSinceStartup-start>100)throw new Exception("Opening escort/briefing timed out");
            if(flow.player.canMove)Move(station.bawan.transform.position-Vector3.forward*2.3f);
            if(station.CurrentLine!=null&&station.CurrentLine.Contains("歡迎"))
            {
                welcome=true;
                if(!shot){yield return Shot("mona-welcome");shot=true;}
            }
            yield return null;
        }
        Check("Mona welcome shown during actual briefing",welcome);
        Check("rifle selection accepted",station.ChooseWeapon(Chapter3Weapons.Weapon.Rifle));
        Note("Pickup-only positioning fixture: Warp beside selected table, then real E through Chapter3Player/Station Update.");
        flow.player.Warp(station.rifleFocus.position+Vector3.back*.8f,station.rifleFocus.position);
        yield return Frames(2);
        if(report.mode=="native-input"){Inspect();yield return Shot("native-before-E");yield break;}
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));
        yield return Wait(()=>station.Readiness.Active,3,"E pickup opens readiness");
        yield return Frames(3);
        Check("holding E blocks field focus",station.Readiness.WaitingForOpeningRelease&&!ui.input.interactable&&!ui.input.isFocused);
        Check("holding E cannot submit",!station.Readiness.SubmitReply("ready"));
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        yield return Wait(()=>!station.Readiness.WaitingForOpeningRelease&&ui.input.isFocused,4,"release arms input");
        Check("new readiness field is empty",ui.input.text=="");
        ui.input.ProcessEvent(new Event{type=EventType.KeyDown,character='e',keyCode=KeyCode.E});
        Check("deliberate later E text is preserved",ui.input.text=="e");
        ui.input.SetTextWithoutNotify("我準備好了");
        Check("Chinese answer string retained (not a native IME test)",ui.input.text=="我準備好了");
        yield return Shot("readiness-clean-input");
        station.Readiness.replyTimeout=.01f;
        Check("ready answer accepted after release",station.Readiness.SubmitReply(ui.input.text));
        yield return Wait(()=>station.CurrentStage==Chapter3StationController.Stage.Departure,12,"readiness accepted");
        Check("ready result advances departure",station.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Ready);
        var movement=typeof(Chapter3FiveFixProbe).GetMethod("ObserveStationDeparture",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
        if(movement!=null)yield return (IEnumerator)movement.Invoke(null,null);
        yield return Press(Key.F6);yield return Wait(()=>flow.station.Opening.Active,15,"reset station for cancellation");
        station.Opening.Cancel();ui.chat.SetActive(false);
        Input.imeCompositionMode=IMECompositionMode.Auto;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;
        ui.chat.SetActive(true);ui.input.interactable=true;ui.input.ActivateInputField();
        station.Readiness.Begin(station,_=>{});
        Check("second begin owns release gate",station.Readiness.WaitingForOpeningRelease);
        yield return new WaitForEndOfFrame();
        Check("pending old activation cannot refocus during gate",!ui.input.isFocused&&!ui.input.enabled&&Input.imeCompositionMode==IMECompositionMode.Off);
        station.Readiness.Cancel();InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        Check("cancellation restores field and IME gate",ui.input.enabled&&!station.Readiness.WaitingForOpeningRelease&&Input.imeCompositionMode==IMECompositionMode.Auto);
    }
    static IEnumerator Diagnose()
    {
        yield return Press(Key.F8);yield return Wait(()=>flow.Region=="School"&&flow.school.Exploring,15,"school inspection");Inspect();
        yield return Shot("fence-F8");
        flow.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F7);
        yield return Wait(()=>flow.Region=="Combat",10,"combat inspection");
        var environment=flow.GetComponent<Chapter3ApproachEnvironment>();
        Note("Approach fixture: "+(environment?environment.name:"missing"));
    }
    static void Check(string label,bool passed){if(!passed)throw new Exception("FAILED: "+label);report?.checks.Add(label);}
    static void Note(string text){report?.observations.Add(text);}
    static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Press(Key key)
    {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
    static void Move(Vector3 target)
    {var d=Vector3.ProjectOnPlane(target-flow.player.transform.position,Vector3.up);flow.player.Move(Vector3.ClampMagnitude(d,flow.player.speed*Time.deltaTime)+Vector3.down*.05f);}
    static IEnumerator Walk(Vector3 target,float within,string label)
    {float start=Time.realtimeSinceStartup;while(Flat(flow.player.transform.position,target)>within){if(Time.realtimeSinceStartup-start>60)throw new Exception("Walk timeout "+label);Move(target);yield return null;}}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var camera=flow.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Dir+(report?.mode??"manual")+"-"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(texture);}
    }
    static void Inspect()
    {
        if(!flow)flow=Object.FindFirstObjectByType<Chapter3Flow>();
        var lines=new List<string>{"playing="+Application.isPlaying,"region="+(flow?flow.Region:"none")};
        if(flow)
        {
            lines.Add("station="+flow.station.CurrentStage);
            lines.Add("input="+flow.station.ui.input.text+" composition="+Input.compositionString+" focused="+flow.station.ui.input.isFocused+" ime="+Input.imeCompositionMode+" imeSelected="+(Keyboard.current?.imeSelected.isPressed==true));
            lines.Add("waitingRelease="+(flow.station.Readiness&&flow.station.Readiness.WaitingForOpeningRelease));
            foreach(var c in flow.schoolRegion.GetComponentsInChildren<Collider>(true).Where(c=>c.name.Contains("柵")||Mathf.Abs(c.transform.position.z+23)<1))
                lines.Add("COLLIDER "+c.name+" enabled="+c.enabled+" active="+c.gameObject.activeInHierarchy+" pos="+c.transform.position+" bounds="+c.bounds);
            foreach(var m in flow.schoolRegion.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name.Contains("幾何")||m.name.Contains("柵")||m.name.Contains("Geometry")))
                lines.Add("MESH "+m.name+" readable="+(m.sharedMesh&&m.sharedMesh.isReadable)+" mesh="+(m.sharedMesh?m.sharedMesh.name:"null"));
        }
        File.WriteAllLines(Dir+"inspect.txt",lines);
    }
    static void BackupSave()
    {
        var save=new Saves{values=new[]{"WusheEvent.Chapter2.Result",Chapter3ResultStore.SaveKey}.Select(k=>new Save{key=k,value=PlayerPrefs.GetString(k,""),existed=PlayerPrefs.HasKey(k)}).ToArray()};
        string json=JsonUtility.ToJson(save,true);SessionState.SetString(State+".save",json);SessionState.SetBool(State+".restore",true);File.WriteAllText(Dir+"save-before.json",json);
    }
    static void RestoreSave()
    {
        if(!SessionState.GetBool(State+".restore",false))return;
        var data=JsonUtility.FromJson<Saves>(SessionState.GetString(State+".save",""));
        foreach(var s in data.values){if(s.existed)PlayerPrefs.SetString(s.key,s.value);else PlayerPrefs.DeleteKey(s.key);}
        PlayerPrefs.Save();SessionState.SetBool(State+".restore",false);
    }
    static void Cleanup()
    {
        if(testInput){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());}
        if(originalInput)InputSystem.settings=originalInput;
        if(testInput)Object.DestroyImmediate(testInput);testInput=originalInput=null;
        if(addedKeyboard!=null)InputSystem.RemoveDevice(addedKeyboard);addedKeyboard=null;
        RestoreSave();
    }
    static void Finish()
    {
        if(report==null)return;
        Cleanup();report.elapsed=Time.realtimeSinceStartup-began;
        File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));
        SessionState.EraseString(State);report=null;Application.runInBackground=oldBackground;EditorApplication.isPlaying=false;
    }
    static void Log(string message,string stack,LogType kind)
    {if(report==null)return;if(kind==LogType.Warning)report.warnings.Add(message);if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)report.errors.Add(message+"\n"+stack);}
    static void Audit()
    {
        var s=SceneManager.GetActiveScene();
        File.WriteAllText(Dir+"audit.json",JsonUtility.ToJson(new AuditResult{scene=s.path,dirty=s.isDirty,playing=Application.isPlaying,compileFailed=EditorUtility.scriptCompilationFailed,
            missingScripts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))},true));
    }
    [Serializable] sealed class AuditResult {public string scene;public bool dirty,playing,compileFailed;public int missingScripts;}
}
public sealed class Chapter3FiveFixDriver:MonoBehaviour
{IEnumerator Start(){yield return Chapter3FiveFixProbe.Run();}}
