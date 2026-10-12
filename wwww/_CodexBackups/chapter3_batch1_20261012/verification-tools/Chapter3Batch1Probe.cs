using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3Batch1Probe
{
    public const string Dir="_CodexBackups/chapter3_batch1_20261012/";
    const string Key="C3Batch1.Test";
    static double next;
    static float started,stationStarted,keyReleased;
    static Report report;
    static string priorStage,priorLine;
    static bool stationReady,blankDone;
    [Serializable] public class Report
    {
        public string mode;
        public bool completed,transitioned,earlyRifleBlocked,farRifleBlocked,duplicateRifleBlocked,earlyExitBlocked,waitingObserved,keyPickup,keyDeparture,boundarySlides;
        public float elapsed;
        public List<string> stages=new List<string>(),lines=new List<string>(),errors=new List<string>();
    }
    static Chapter3Batch1Probe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string m,string stack,LogType kind){if(report!=null&&stationReady&&(kind==LogType.Exception||kind==LogType.Error||kind==LogType.Assert))report.errors.Add(m+"\n"+stack);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt")&&EditorApplication.timeSinceStartup>=next)
        {
            next=EditorApplication.timeSinceStartup+.3;string cmd;
            try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="build")Chapter3StationAuthoring.Build();
                else if(cmd=="play")EditorApplication.isPlaying=true;
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd=="clear-test"){SessionState.EraseString(Key);report=null;stationReady=false;}
                else if(cmd=="shot")Shot("manual");
                else if(cmd=="audit")Audit();
                else if(cmd=="cast")Cast();
                else if(cmd=="polish")Chapter3StationAuthoring.PolishExisting();
                else if(cmd=="refresh")AssetDatabase.Refresh();
                else if(cmd=="open-village")Open("Assets/Scenes/第三章.unity");
                else if(cmd=="open-station")Open(Chapter3StationAuthoring.ScenePath);
                else if(cmd=="test-station"||cmd=="test-full"||cmd=="test-native")
                {Open(cmd=="test-full"?"Assets/Scenes/第三章.unity":Chapter3StationAuthoring.ScenePath);SessionState.SetString(Key,cmd);EditorApplication.isPlaying=true;}
                else if(cmd=="native-pickup")
                {var c=Object.FindFirstObjectByType<Chapter3StationController>();c.player.Warp(new Vector3(-4.8f,.03f,-2),c.rifleFocus.position);}
                else if(cmd=="scale"){Chapter2GameViewScaleGuard.Restore();Scale();}
                File.WriteAllText(Dir+"done-"+cmd+".txt",DateTime.Now.ToString("O"));
            }
            catch(Exception e){File.WriteAllText(Dir+"error-"+cmd+".txt",e.ToString());}
        }
        if(!Application.isPlaying)return;
        string mode=SessionState.GetString(Key,"");if(mode=="")return;
        try{Run(mode);}catch(Exception e){if(report==null)report=new Report{mode=mode};report.errors.Add(e.ToString());Finish();}
    }
    static void Open(string path)
    {if(Application.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Editor must be stopped with saved scene.");EditorSceneManager.OpenScene(path);}
    static void Run(string mode)
    {
        if(report==null){report=new Report{mode=mode};started=Time.realtimeSinceStartup;stationStarted=0;stationReady=false;priorStage=priorLine="";blankDone=false;}
        if(Time.realtimeSinceStartup-started>420)throw new Exception("Timed out");
        if(keyReleased>0&&Time.realtimeSinceStartup>keyReleased){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());keyReleased=0;}
        var village=Object.FindFirstObjectByType<Chapter3Controller>();
        if(village)
        {
            village.lineSeconds=.65f;
            typeof(Chapter2LocalDialogue).GetField("cloudConfigError",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(village.dialogue,"Integration test uses scripted fallback");
            if(village.CurrentStage==Chapter3Controller.Stage.Conversation)
            {
                if(!village.WaitingForReply && village.CompletedTurns<3){village.ui.input.text="我會跟著你們，一起走。";village.ui.send.onClick.Invoke();}
                else if(village.CompletedTurns==3&&village.ui.depart.interactable)village.ui.depart.onClick.Invoke();
            }
            else if(village.CurrentStage==Chapter3Controller.Stage.Equipment)
            {Move(village.player,new Vector3(-2.5f,0,2.5f));if(Vector3.Distance(village.player.transform.position,new Vector3(-2.5f,0,2.5f))<.5f)PressE();}
            else if(village.CurrentStage==Chapter3Controller.Stage.Trail)Follow(village.player,village.bawan);
            return;
        }
        var c=Object.FindFirstObjectByType<Chapter3StationController>();if(!c)return;
        if(!stationReady)
        {
            stationReady=true;stationStarted=Time.realtimeSinceStartup;report.transitioned=mode=="test-full";
            report.earlyRifleBlocked=!c.TakeRifle();report.earlyExitBlocked=!c.ConfirmDeparture();
            Vector3 before=new Vector3(9.9f,0,-10),attempt=new Vector3(11,0,-9.8f),after=c.Constrain(before,attempt);
            report.boundarySlides=Mathf.Abs(after.x-before.x)<.001f&&Mathf.Abs(after.z-attempt.z)<.001f;
            Shot(mode+"-arrival");
        }
        c.lineSeconds=1.1f;
        string stage=c.CurrentStage.ToString();
        if(stage!=priorStage){priorStage=stage;report.stages.Add(stage);Shot(mode+"-"+stage);}
        if(c.CurrentLine!=null&&c.CurrentLine!=priorLine)
        {priorLine=c.CurrentLine;report.lines.Add(priorLine);if(priorLine.Contains("帶上槍"))Shot(mode+"-mona");}
        if(c.CompanionsWaiting)report.waitingObserved=true;
        if(c.CurrentStage==Chapter3StationController.Stage.Approach && Time.realtimeSinceStartup-stationStarted>15)Follow(c.player,c.bawan);
        else if(c.CurrentStage==Chapter3StationController.Stage.Equipment&&!c.HasRifle)
        {
            if(!blankDone){report.farRifleBlocked=!c.TakeRifle();blankDone=true;}
            Move(c.player,new Vector3(-4.8f,0,-2));
            if(Vector3.Distance(c.player.transform.position,new Vector3(-4.8f,0,-2))<.5f&&mode!="test-native")PressE();
        }
        else if(c.CurrentStage==Chapter3StationController.Stage.Regroup)
        {
            report.keyPickup=c.HasRifle;report.duplicateRifleBlocked=!c.TakeRifle();
            Move(c.player,c.bawan.transform.position+new Vector3(0,0,-2));
            if(Vector3.Distance(c.player.transform.position,c.bawan.transform.position)<2.7f&&mode!="test-native")PressE();
        }
        else if(c.CurrentStage==Chapter3StationController.Stage.Departure){report.keyDeparture=true;Follow(c.player,c.bawan);}
        if(c.PartComplete){report.completed=true;report.elapsed=Time.realtimeSinceStartup-started;Shot(mode+"-complete");Finish();}
    }
    static void PressE(){if(keyReleased>0)return;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.E));keyReleased=Time.realtimeSinceStartup+.12f;}
    static void Move(Chapter3Player p,Vector3 target)
    {Vector3 d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);if(d.magnitude>.12f)p.Move(d.normalized*p.speed*Mathf.Min(Time.deltaTime,.04f));}
    static void Follow(Chapter3Player p,Chapter2Actor a)
    {Vector3 d=Vector3.ProjectOnPlane(a.transform.position-p.transform.position,Vector3.up);if(d.magnitude>2.3f)Move(p,a.transform.position);if(p.canMove)p.FocusOn(a.transform.position+Vector3.up*1.25f);}
    static void Finish(){File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(Key);report=null;stationReady=false;EditorApplication.isPlaying=false;}
    public static void Shot(string name)
    {
        Canvas.ForceUpdateCanvases();var cam=Camera.main;if(!cam)return;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
        var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var c=Object.FindFirstObjectByType<Chapter3StationController>();
        File.WriteAllText(Dir+"audit.txt","scene="+scene.path+"\nplaying="+Application.isPlaying+"\ndirty="+scene.isDirty+"\ncompileFailed="+EditorUtility.scriptCompilationFailed+"\nmissingScripts="+all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+"\nmissingMeshes="+all.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(m=>!m.sharedMesh)+"\nactors="+all.Count(t=>t.GetComponent<Chapter2Actor>())+"\nstate="+(c?c.CurrentStage.ToString():"village")+"\nrifle="+(c&&c.HasRifle)+"\n");
    }
    static void Cast()
    {
        var lines=new List<string>();
        if(!Application.isPlaying){var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");try{var cc=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();for(int i=0;i<cc.leaders.Length;i++)lines.Add("DONOR "+i+" "+cc.leaders[i].DisplayName+" "+string.Join(",",cc.leaders[i].GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>AssetDatabase.GetAssetPath(r.sharedMesh))));}finally{EditorSceneManager.ClosePreviewScene(preview);}}
        foreach(var a in Object.FindObjectsByType<Chapter2Actor>(FindObjectsSortMode.None))
        {
            lines.Add(a.name+" pos="+a.transform.position+" scale="+a.transform.lossyScale);
            foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))lines.Add("  "+r.name+" enabled="+r.enabled+" active="+r.gameObject.activeSelf+" mesh="+AssetDatabase.GetAssetPath(r.sharedMesh)+" bounds="+r.bounds+" root="+(r.rootBone?r.rootBone.name:"null"));
        }
        foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))lines.Add("sign "+t.text+" size="+t.GetComponent<Renderer>().bounds.size);
        File.WriteAllLines(Dir+"cast.txt",lines);
    }
    static void Scale()
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var property=type.GetProperty("zoomAreaScale",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        foreach(EditorWindow view in Resources.FindObjectsOfTypeAll(type))
        {var scale=(Vector2)property.GetValue(view);File.AppendAllText(Dir+"preview-scale.txt",DateTime.Now.ToString("O")+" maximized="+view.maximized+" x="+scale.x+" y="+scale.y+"\n");}
    }
}
