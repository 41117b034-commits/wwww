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
public static class Chapter3Batch2Probe
{
    const string Dir="_CodexBackups/chapter3_batch2_20261012/",KeyName="C3B2.mode";
    static double next;
    static Report report;
    static float began,stepAt,releaseKey,releaseMouse;
    static int step;
    static string prior;
    [Serializable] public class Report
    {
        public string mode;public bool completed;
        public List<string> stages=new List<string>(),checks=new List<string>(),errors=new List<string>();
        public float elapsed;public int sceneHandle,sceneCount,retries;
    }
    static Chapter3Batch2Probe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string m,string st,LogType t){if(report!=null&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))report.errors.Add(m+"\n"+st);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt")&&EditorApplication.timeSinceStartup>next)
        {
            next=EditorApplication.timeSinceStartup+.25;string cmd;
            try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="build")Chapter3UnifiedAuthoring.Build();
                else if(cmd=="polish")Chapter3UnifiedAuthoring.Polish();
                else if(cmd=="hands")Hands();
                else if(cmd=="refine-hands")Chapter3UnifiedAuthoring.RefineHands();
                else if(cmd=="visual")
                {
                    var c=Object.FindFirstObjectByType<Chapter3Flow>().combat;
                    c.player.Warp(new Vector3(0,.06f,-8),new Vector3(0,1.5f,8));foreach(var e in c.Enemies)e.enabled=false;
                }
                else if(cmd=="knife")Object.FindFirstObjectByType<Chapter3Flow>().combat.weapons.Select(Chapter3Weapons.Weapon.Knife);
                else if(cmd=="audit")Audit();
                else if(cmd=="shot")Shot("manual");
                else if(cmd=="stop"){SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
                else if(cmd=="play")EditorApplication.isPlaying=true;
                else if(cmd=="combat")Object.FindFirstObjectByType<Chapter3Flow>().EnterCombat();
                else if(cmd=="station")Object.FindFirstObjectByType<Chapter3Flow>().EnterStation();
                else if(cmd=="scale")Scale();
                else if(cmd.StartsWith("test-"))
                {EditorSceneManager.OpenScene("Assets/Scenes/第三章.unity");SessionState.SetString(KeyName,cmd);EditorApplication.isPlaying=true;}
                File.WriteAllText(Dir+"done-"+cmd+".txt",DateTime.Now.ToString("O"));
            }
            catch(Exception ex){File.WriteAllText(Dir+"error-"+cmd+".txt",ex.ToString());}
        }
        if(!Application.isPlaying)return;
        var mode=SessionState.GetString(KeyName,"");if(mode=="")return;
        try{Run(mode);}catch(Exception ex){if(report==null)report=new Report{mode=mode};report.errors.Add(ex.ToString());Finish();}
    }
    static void Run(string mode)
    {
        var f=Object.FindFirstObjectByType<Chapter3Flow>();if(!f)return;
        if(report==null){report=new Report{mode=mode,sceneHandle=SceneManager.GetActiveScene().handle};began=Time.realtimeSinceStartup;step=0;stepAt=Time.time;prior="";}
        if(Time.realtimeSinceStartup-began>420)throw new Exception("Timed out step "+step+" "+f.Region);
        if(releaseKey>0&&Time.time>=releaseKey){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());releaseKey=0;}
        if(releaseMouse>0&&Time.time>=releaseMouse){InputSystem.QueueStateEvent(Mouse.current,new MouseState());releaseMouse=0;}
        f.village.lineSeconds=.45f;f.station.lineSeconds=mode=="test-shortcuts"?10:.45f;f.combat.lineSeconds=.45f;
        string stage=f.Region+":"+(f.Region=="Village"?f.village.CurrentStage.ToString():f.Region=="Station"?f.station.CurrentStage.ToString():f.combat.CurrentStage.ToString());
        if(stage!=prior){prior=stage;report.stages.Add(stage);File.WriteAllText(Dir+"progress.txt",stage+" step="+step);}
        if(SceneManager.GetActiveScene().handle!=report.sceneHandle)throw new Exception("Region transition loaded a different scene");
        if(mode=="test-shortcuts"){Shortcuts(f);return;}
        if(mode!="test-full"&&f.Region=="Village")
        {if(releaseKey==0)Press(UnityEngine.InputSystem.Key.F7);return;}
        if(f.Region=="Village")
        {
            var v=f.village;
            typeof(Chapter2LocalDialogue).GetField("cloudConfigError",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(v.dialogue,"Test runtime fallback");
            if(v.CurrentStage==Chapter3Controller.Stage.Conversation)
            {
                if(!v.WaitingForReply&&v.CompletedTurns<3){v.ui.input.text="我會跟上大家。";v.ui.send.onClick.Invoke();}
                else if(v.CompletedTurns==3&&v.ui.depart.interactable)v.ui.depart.onClick.Invoke();
            }
            else if(v.CurrentStage==Chapter3Controller.Stage.Equipment){Move(v.player,new Vector3(-2.5f,0,2.5f));if(Vector3.Distance(v.player.transform.position,new Vector3(-2.5f,0,2.5f))<.6f)Press(UnityEngine.InputSystem.Key.E);}
            else if(v.CurrentStage==Chapter3Controller.Stage.Trail)Follow(v.player,v.bawan);
            return;
        }
        if(f.Region=="Station")
        {
            var s=f.station;
            if(s.CurrentStage==Chapter3StationController.Stage.Approach)Follow(s.player,s.bawan);
            if(s.CurrentStage==Chapter3StationController.Stage.Equipment){Move(s.player,new Vector3(-4.8f,0,-2));if(Vector3.Distance(s.player.transform.position,new Vector3(-4.8f,0,-2))<.5f)Press(UnityEngine.InputSystem.Key.E);}
            if(s.CurrentStage==Chapter3StationController.Stage.Regroup){Move(s.player,s.bawan.transform.position+Vector3.back*2);if(Vector3.Distance(s.player.transform.position,s.bawan.transform.position)<2.7f)Press(UnityEngine.InputSystem.Key.E);}
            if(s.CurrentStage==Chapter3StationController.Stage.Departure)Follow(s.player,s.bawan);
            return;
        }
        var c=f.combat;var w=c.weapons;var p=c.player;
        if(c.CurrentStage==Chapter3Combat.Stage.Intro)return;
        if(step==0)
        {
            Check("one scene",SceneManager.sceneCount==1);Check("one active camera",Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(x=>x.enabled)==1);
            Check("one active listener",Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1);
            Check("combat stage",c.Fighting);Check("equipped",w.Loaded&&w.Reserve==8);
            Shot(mode+"-rifle");p.Warp(new Vector3(-3.2f,.06f,-4),new Vector3(0,1,7));Next();
        }
        else if(step==1&&Time.time-stepAt>6)
        {
            Check("cover prevents damage",c.Health==100);Shot(mode+"-cover");
            p.Warp(new Vector3(0,.06f,-1),new Vector3(0,1,7));Press(UnityEngine.InputSystem.Key.Digit1);Next();
        }
        else if(step==2&&Time.time-stepAt>.7f)
        {
            Check("keyboard equips knife",w.Selected==Chapter3Weapons.Weapon.Knife);Shot(mode+"-knife");
            Cursor.lockState=CursorLockMode.Locked;Click();Next();
        }
        else if(step==3&&Time.time-stepAt>.9f)
        {
            Check("mouse slashes",w.Slashes==1);Check("knife cannot hit distant enemy",c.Enemies[0].Health==100);
            Shot(mode+"-enemy-aim");Next();
        }
        else if(step==4)
        {
            if(c.CurrentStage==Chapter3Combat.Stage.Down)
            {Check("enemy fire injures player",c.Health==0&&c.Enemies.Any(e=>e.Shots>0));Shot(mode+"-down");Press(UnityEngine.InputSystem.Key.R);Next();}
        }
        else if(step==5&&Time.time-stepAt>.5f)
        {
            Check("R checkpoint retry",c.Fighting&&c.Health==100&&c.Retries==1&&w.Loaded&&w.Reserve==8&&c.DefeatedCount==0);Check("enemies reset",c.Enemies.Count==3&&c.Enemies.All(e=>e.Health==100));
            // Test cover hit consumes exactly one round, then repeat fire is blocked.
            p.Warp(new Vector3(-3.2f,.06f,-4),new Vector3(-3.2f,.9f,-2));Cursor.lockState=CursorLockMode.Locked;Click();Next();
        }
        else if(step==6&&Time.time-stepAt>.7f)
        {Check("one round consumed",w.Shots==1&&!w.Loaded&&w.Reserve==8);Check("cover blocks player shot",c.Enemies.All(e=>e.Health==100));Click();Next();}
        else if(step==7&&Time.time-stepAt>.5f)
        {Check("empty chamber blocks shot",w.Shots==1);Press(UnityEngine.InputSystem.Key.R);Next();}
        else if(step==8&&Time.time-stepAt>1)
        {Check("reload in progress",w.Reloading&&!w.Loaded&&w.Reserve==8);Shot(mode+"-reload");Check("cannot attack or switch during reload",!w.Attack()&&!w.Select(Chapter3Weapons.Weapon.Knife));Next();}
        else if(step==9&&Time.time-stepAt>2)
        {
            Check("single round reload",!w.Reloading&&w.Loaded&&w.Reserve==7&&w.Reloads==1);
            // Rifle kill from a visible unobstructed position, using mouse input.
            p.Warp(new Vector3(0,.06f,-1),c.Enemies[0].AimPoint);Cursor.lockState=CursorLockMode.Locked;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));releaseMouse=Time.time+.7f;Next();
        }
        else if(step==10&&Time.time-stepAt>.4f)
        {Check("right mouse aims",w.Aiming&&p.view.fieldOfView<50);Shot(mode+"-aim");Click();Next();}
        else if(step==11&&Time.time-stepAt>.8f)
        {
            Check("rifle hit and enemy fall",c.Enemies[0].Defeated&&c.DefeatedCount==1);Shot(mode+"-hit");
            Press(UnityEngine.InputSystem.Key.Digit1);Next();
        }
        else if(step==12&&Time.time-stepAt>.5f)
        {
            var enemy=c.Enemies[1];p.Warp(enemy.transform.position+new Vector3(0,.06f,-1.25f),enemy.AimPoint);Cursor.lockState=CursorLockMode.Locked;Click();Next();
        }
        else if(step==13&&Time.time-stepAt>.85f)
        {Check("knife close hit",c.Enemies[1].Health==40);Shot(mode+"-knife-hit");Click();Next();}
        else if(step==14&&Time.time-stepAt>.85f)
        {Check("second knife hit defeats",c.Enemies[1].Defeated&&c.DefeatedCount==2);Press(UnityEngine.InputSystem.Key.Digit2);Next();}
        else if(step==15&&Time.time-stepAt>.5f){Press(UnityEngine.InputSystem.Key.R);Next();}
        else if(step==16&&Time.time-stepAt>3)
        {p.Warp(c.Enemies[2].transform.position+new Vector3(0,.06f,-4),c.Enemies[2].AimPoint);Cursor.lockState=CursorLockMode.Locked;Click();Next();}
        else if(step==17&&Time.time-stepAt>.8f)
        {Check("all three defeated",c.DefeatedCount==3);Next();}
        else if(step==18&&c.CurrentStage==Chapter3Combat.Stage.Cleared&&p.canMove)
        {Move(p,c.exitPoint.position);if(Vector3.Distance(Vector3.ProjectOnPlane(p.transform.position-c.exitPoint.position,Vector3.up),Vector3.zero)<2.5f)Next();}
        else if(step==19&&c.Complete)
        {Shot(mode+"-complete");Check("companions alive",!c.bawan.fallen&&!c.pihu.fallen);report.retries=c.Retries;report.completed=true;Finish();}
    }
    static void Check(string name,bool ok){report.checks.Add(name+"="+ok);if(!ok)throw new Exception("Failed: "+name);}
    static void Next(){step++;stepAt=Time.time;File.WriteAllText(Dir+"progress.txt",prior+" step="+step);}
    static void Press(UnityEngine.InputSystem.Key key){if(releaseKey>0)return;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));releaseKey=Time.time+.12f;}
    static void Click(){if(releaseMouse>0){InputSystem.QueueStateEvent(Mouse.current,new MouseState());releaseMouse=0;}InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));releaseMouse=Time.time+.10f;}
    static void Move(Chapter3Player p,Vector3 target){var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);if(d.magnitude>.1f)p.Move(d.normalized*p.speed*Mathf.Min(Time.deltaTime,.04f));}
    static void Follow(Chapter3Player p,Chapter2Actor a){if(Vector3.Distance(p.transform.position,a.transform.position)>2.3f)Move(p,a.transform.position);if(p.canMove)p.FocusOn(a.transform.position+Vector3.up*1.2f);}
    static void Finish(){report.elapsed=Time.realtimeSinceStartup-began;report.sceneCount=SceneManager.sceneCount;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
    static void Audit()
    {
        var s=SceneManager.GetActiveScene();var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var f=Object.FindFirstObjectByType<Chapter3Flow>();
        var lines=new List<string>{"scene="+s.path,"dirty="+s.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),"missingMeshes="+all.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(m=>!m.sharedMesh),"sceneCount="+SceneManager.sceneCount};
        if(f)
        {
            lines.Add("region="+f.Region);lines.Add("combat="+f.combat.CurrentStage);foreach(var e in f.combat.Enemies)lines.Add("enemy="+e.name+" pos="+e.transform.position+" health="+e.Health+" scale="+e.transform.lossyScale);
            foreach(var t in new[]{f.combat.weapons.rightHand,f.combat.weapons.leftHand})
            {
                lines.Add("HAND "+t.name+" pos="+t.position+" scale="+t.lossyScale+" rot="+t.eulerAngles);
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))lines.Add("RENDER "+r.bounds+" local="+r.GetComponent<MeshFilter>().sharedMesh.bounds+" scale="+r.transform.lossyScale);
            }
        }
        File.WriteAllLines(Dir+"audit.txt",lines);
    }
    static void Shot(string name)
    {
        Canvas.ForceUpdateCanvases();var cam=Camera.main;if(!cam)return;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
        var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Scale()
    {
        Chapter2GameViewScaleGuard.Restore();var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var prop=type.GetProperty("zoomAreaScale",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        foreach(EditorWindow view in Resources.FindObjectsOfTypeAll(type)){bool original=view.maximized;foreach(bool max in new[]{false,true,false}){view.maximized=max;Chapter2GameViewScaleGuard.Restore();File.AppendAllText(Dir+"scale.txt","max="+max+" scale="+prop.GetValue(view)+"\n");}view.maximized=original;}
    }
    static void Hands()
    {
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/手/Hands_Rigged.fbx"));
        try
        {
            var lines=go.GetComponentsInChildren<Transform>().Select(t=>t.name+" parent="+(t.parent?t.parent.name:"")+" position="+t.position+" local="+t.localPosition+" rotation="+t.localEulerAngles+" scale="+t.lossyScale).ToList();
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())lines.Add("MESH "+r.name+" "+r.bounds+" vertices="+r.sharedMesh.vertexCount+" bones="+string.Join(",",r.bones.Select(b=>b.name)));
            File.WriteAllLines(Dir+"hands.txt",lines);
        }
        finally{Object.DestroyImmediate(go);}
    }
    static void Shortcuts(Chapter3Flow f)
    {
        var c=f.combat;
        if(step==0&&f.village.CurrentStage==Chapter3Controller.Stage.Conversation)
        {Check("chat active before shortcut",f.ui.chat.activeSelf);Press(UnityEngine.InputSystem.Key.F6);Next();}
        else if(step==1&&Time.time-stepAt>.5f)
        {Check("F6 from conversation",f.Region=="Station"&&!f.ui.chat.activeSelf&&f.station.CurrentStage==Chapter3StationController.Stage.Arrival);f.station.lineSeconds=10;Next();}
        else if(step==2&&f.station.CurrentLine!=null)
        {f.station.lineSeconds=10;if(Time.time-stepAt>5.2f){Press(UnityEngine.InputSystem.Key.Space);Next();}}
        else if(step==3&&Time.time-stepAt>.6f)
        {Check("Space skips authored line",f.station.CurrentStage==Chapter3StationController.Stage.Approach);Press(UnityEngine.InputSystem.Key.F7);Next();}
        else if(step==4&&c.Fighting)
        {Check("F7 starts combat",f.Region=="Combat"&&c.Enemies.Count==3);Press(UnityEngine.InputSystem.Key.F7);Next();}
        else if(step==5&&Time.time-stepAt>1&&c.Fighting)
        {
            Check("repeat F7 resets once",c.Enemies.Count==3&&c.Health==100&&c.weapons.Loaded);
            c.player.Warp(new Vector3(-3.2f,.06f,-4),new Vector3(-3.2f,1,-2));c.weapons.Attack();Next();
        }
        else if(step==6&&Time.time-stepAt>.6f){Check("begin cancellable reload",c.weapons.Reload());Next();}
        else if(step==7&&Time.time-stepAt>.7f){Press(UnityEngine.InputSystem.Key.F6);Next();}
        else if(step==8&&Time.time-stepAt>.5f)
        {Check("F6 cancels reload and enemy update",f.Region=="Station"&&!c.weapons.Reloading&&!c.weapons.gameObject.activeSelf&&!c.Fighting&&!f.combatRegion.activeSelf);Press(UnityEngine.InputSystem.Key.F7);Next();}
        else if(step==9&&c.Fighting)
        {
            Check("clean combat restart",c.weapons.Loaded&&c.weapons.Reserve==8&&c.Health==100);
            c.player.Warp(new Vector3(-3.2f,.06f,-4),new Vector3(-3.2f,1,-2));c.weapons.Attack();Next();
        }
        else if(step==10&&Time.time-stepAt>.6f){c.weapons.Reload();Next();}
        else if(step==11&&Time.time-stepAt>2.8f)
        {Check("reload after jump",c.weapons.Reserve==7);c.player.Warp(new Vector3(-3.6f,.06f,-9.5f),c.ammoPoint.position);Press(UnityEngine.InputSystem.Key.E);Next();}
        else if(step==12&&Time.time-stepAt>.3f)
        {
            Check("E resupply",c.weapons.Reserve==8);Check("only one scene after repeated jumps",SceneManager.sceneCount==1);
            Check("no duplicate actors",Object.FindObjectsByType<Chapter3Enemy>(FindObjectsSortMode.None).Length==3);
            report.completed=true;Shot("shortcuts-final");Finish();
        }
    }
}
