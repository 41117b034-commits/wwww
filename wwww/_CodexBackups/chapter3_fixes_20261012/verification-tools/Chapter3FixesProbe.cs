using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3FixesProbe
{
    const string Dir="_CodexBackups/chapter3_fixes_20261012/",Key="Chapter3Fixes.mode";
    [Serializable] class Report
    {
        public string mode;public bool completed;public float elapsed;
        public List<string> checks=new List<string>(),errors=new List<string>(),stages=new List<string>();
    }
    static Report report;
    static int step,route;
    static float began,at,keyUntil,mouseUntil,lastFrame=-1;
    static Vector3 saved,prevB,prevP;
    static bool tracking;
    static string previous;
    static Chapter3FixesDriver driver;
    static Chapter3FixesProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void PrepareValidation()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/第三章.unity");Chapter3CorrectionsAuthoring.Apply();Audit();
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-chapter3Test");
        Application.runInBackground=true;
        if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();if(Mouse.current==null)InputSystem.AddDevice<Mouse>();
        SessionState.SetString(Key,index>=0?args[index+1]:"test-station");EditorApplication.isPlaying=true;
    }
    static void Log(string m,string s,LogType t){if(report!=null&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))report.errors.Add(m+"\n"+s);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string command;try{command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(command=="apply")Chapter3CorrectionsAuthoring.Apply();
                else if(command=="audit")Audit();
                else if(command=="stop"){SessionState.EraseString(Key);report=null;EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
                else if(command.StartsWith("test-")){SessionState.SetString(Key,command);EditorApplication.isPlaying=true;}
                else throw new Exception("Unknown command "+command);
                File.WriteAllText(Dir+"done-"+command+".txt",DateTime.Now.ToString("O"));
            }
            catch(Exception e){File.WriteAllText(Dir+"error-"+command+".txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(Key,"")!=""&&!driver)driver=new GameObject("Temporary Chapter 3 fixes test driver").AddComponent<Chapter3FixesDriver>();
    }
    public static void PlayTick()
    {
        if(!Application.isPlaying)return;
        string mode=SessionState.GetString(Key,"");if(mode=="")return;
        // One driver update per rendered game frame.
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try
        {
            var f=Object.FindFirstObjectByType<Chapter3Flow>();if(!f)return;
            if(report==null)
            {
                report=new Report{mode=mode};step=route=0;began=Time.realtimeSinceStartup;at=Time.time;tracking=false;previous="";
                if(Application.isBatchMode)
                {
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();if(Mouse.current==null)InputSystem.AddDevice<Mouse>();
                    InputSystem.EnableDevice(Keyboard.current);InputSystem.EnableDevice(Mouse.current);
                }
            }
            if(Time.realtimeSinceStartup-began>230)throw new Exception("Timeout step="+step+" region="+f.Region+" bawan="+f.station.bawan.transform.position+" pihu="+f.station.pihu.transform.position);
            if(Time.time>=keyUntil&&keyUntil>0){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());keyUntil=0;}
            if(Time.time>=mouseUntil&&mouseUntil>0){InputSystem.QueueStateEvent(Mouse.current,new MouseState());mouseUntil=0;}
            f.station.lineSeconds=.4f;f.combat.lineSeconds=.4f;
            string status=f.Region+":"+(f.Region=="Station"?f.station.CurrentStage.ToString():f.Region=="Combat"?f.combat.CurrentStage.ToString():f.village.CurrentStage.ToString());
            if(status!=previous){report.stages.Add(status);previous=status;}
            File.WriteAllText(Dir+"progress.txt",mode+" step="+step+" "+status+" player="+f.player.transform.position+" bawan="+(f.Region=="Station"?f.station.bawan:f.combat.bawan).transform.position+" pihu="+(f.Region=="Station"?f.station.pihu:f.combat.pihu).transform.position);
            if(mode=="test-station")Station(f);else if(mode.StartsWith("test-escort-"))Escort(f,mode);else Combat(f);
        }
        catch(Exception e){report.errors.Add(e.ToString());Finish();}
    }
    static void Station(Chapter3Flow f)
    {
        var s=f.station;var p=f.player;
        if(step==0&&f.village.CurrentStage==Chapter3Controller.Stage.Conversation)
        {Check("conversation counter hidden",!f.ui.progress.gameObject.activeInHierarchy);Shot("conversation-hidden");Press(KeyCodeF6());Next();}
        else if(step==1&&f.Region=="Station")
        {
            if(s.CurrentStage==Chapter3StationController.Stage.Approach)Follow(p,s.bawan);
            if(s.CurrentStage==Chapter3StationController.Stage.Equipment){Move(p,s.rifleFocus.position);if(Flat(p.transform.position,s.rifleFocus.position)<1.8f){Press(UnityEngine.InputSystem.Key.E);Next();}}
        }
        else if(step==2&&s.CurrentStage==Chapter3StationController.Stage.Regroup)
        {Move(p,s.bawan.transform.position);if(Flat(p.transform.position,s.bawan.transform.position)<2.5f){Press(UnityEngine.InputSystem.Key.E);Next();}}
        else if(step==3&&s.CurrentStage==Chapter3StationController.Stage.Departure&&p.canMove)
        {
            saved=p.transform.position;p.Warp(new Vector3(10,.06f,-5),new Vector3(14,1.4f,-5));Next();
        }
        else if(step==4)
        {
            p.GetComponent<CharacterController>().Move(Vector3.right*.04f);
            if(Time.time-at>1.1f){Check("fence blocks outward player movement",p.transform.position.x<10.5f);Shot("fence-collision");p.Warp(new Vector3(11.65f,.06f,-5),new Vector3(9,1.4f,-5));Next();}
        }
        else if(step==5)
        {
            p.GetComponent<CharacterController>().Move(Vector3.left*.04f);
            if(Time.time-at>1.1f){Check("fence blocks inward player movement",p.transform.position.x>11.1f);p.Warp(saved,s.bawan.transform.position+Vector3.up);prevB=s.bawan.transform.position;prevP=s.pihu.transform.position;tracking=true;Next();}
        }
        else if(step==6&&f.Region=="Station")
        {
            if(tracking)
            {
                if(!Chapter3PathMotor.Clear(prevB,s.bawan.transform.position)||!Chapter3PathMotor.Clear(prevP,s.pihu.transform.position))throw new Exception("Companion intersected station obstacle");
                prevB=s.bawan.transform.position;prevP=s.pihu.transform.position;
            }
            if(route<s.departurePath.Length)
            {
                Vector3 next=Vector3.MoveTowards(p.transform.position,s.departurePath[route],.1f);
                if(Flat(p.transform.position,s.bawan.transform.position)<3.6f||Flat(next,s.bawan.transform.position)<Flat(p.transform.position,s.bawan.transform.position))Move(p,s.departurePath[route]);
                p.FocusOn(s.bawan.transform.position+Vector3.up*1.2f);
                if(Flat(p.transform.position,s.departurePath[route])<.3f){if(route==4)Shot("fence-route-around-end");route++;}
            }
            else Follow(p,s.bawan);
        }
        if(step==6&&f.Region=="Combat")
        {Check("full station exit follows safe route",route==s.departurePath.Length);Check("single chapter scene",SceneManager.sceneCount==1);report.completed=true;Finish();}
    }
    static UnityEngine.InputSystem.Key KeyCodeF6()=>UnityEngine.InputSystem.Key.F6;
    static void Escort(Chapter3Flow f,string mode)
    {
        var c=f.combat;var p=f.player;
        if(step==0&&f.Region=="Village"){Press(UnityEngine.InputSystem.Key.F7);Next();}
        else if(step==1&&c.Fighting)
        {
            p.Warp(mode.EndsWith("ahead")?new Vector3(0,.06f,29.5f):c.spawn,new Vector3(0,1.3f,14));
            foreach(var e in c.Enemies)e.Hit(100,p.transform.position);Next();
        }
        else if(step==2&&c.CurrentStage==Chapter3Combat.Stage.Cleared&&p.canMove)
        {
            Check("camera faces forward at "+mode,p.view.transform.forward.z>.85f);
            if(mode.EndsWith("rear"))Check("leader is ahead at rear checkpoint",c.bawan.transform.position.z>p.transform.position.z+2);
            Shot(mode+"-start");prevB=c.bawan.transform.position;prevP=c.pihu.transform.position;Next();
        }
        else if(step==3&&!c.Complete)
        {
            if(!Chapter3PathMotor.Clear(prevB,c.bawan.transform.position)||!Chapter3PathMotor.Clear(prevP,c.pihu.transform.position))throw new Exception("Escort obstacle collision");
            prevB=c.bawan.transform.position;prevP=c.pihu.transform.position;
            if(!mode.EndsWith("ahead"))
            {Follow(p,c.bawan);if(Flat(p.transform.position,c.exitPoint.position)<3.5f)Move(p,c.exitPoint.position);}
        }
        if(step==3&&c.Complete)
        {Check("both companions finish escort",Flat(c.bawan.transform.position,c.exitPoint.position)<.5f&&Flat(c.pihu.transform.position,c.exitPoint.position)<2.4f);Check("still one scene",SceneManager.sceneCount==1);Shot(mode+"-complete");report.completed=true;Finish();}
    }
    static void Combat(Chapter3Flow f)
    {
        var c=f.combat;var p=f.player;var w=c.weapons;float t=Time.time-at;
        if(step==0&&f.Region=="Village"){Press(UnityEngine.InputSystem.Key.F7);Next();}
        else if(step==1&&c.Fighting)
        {
            Check("F7 enters fighting",SceneManager.sceneCount==1);foreach(var e in c.Enemies)e.enabled=false;
            p.Warp(new Vector3(-5.1f,.06f,-6.25f),c.Companions[0].AimPoint);Press(UnityEngine.InputSystem.Key.Digit1);Next();
        }
        else if(step==2&&t>.4f){Click();Next();}
        else if(step==3&&t>.28f)
        {Shot("knife-companion-reaction");Check("knife companion reacts",c.Companions[0].KnifeReactions==1);Check("knife warning subtitle",f.ui.subtitle.text.Contains("日本警察"));Next();}
        else if(step==4&&t>.6f){Press(UnityEngine.InputSystem.Key.Digit2);Next();}
        else if(step==5&&t>.4f){Click();Next();}
        else if(step==6&&t>.3f)
        {
            Check("loaded friendly shot blocked",w.Loaded&&w.Shots==0&&w.Reserve==8);Check("gun system warning",f.ui.speaker.text=="系統提醒"&&f.ui.subtitle.text.Contains("不能拿槍"));Shot("rifle-companion-warning");
            p.Warp(new Vector3(5.2f,.06f,-7.3f),c.Companions[1].AimPoint);Next();
        }
        else if(step==7&&t>.3f){Click();Next();}
        else if(step==8&&t>.55f)
        {Check("Pihu friendly shot also blocked",w.Loaded&&w.Shots==0);Press(UnityEngine.InputSystem.Key.Digit1);Next();}
        else if(step==9&&t>.4f){Click();Next();}
        else if(step==10&&t>.3f)
        {Check("Pihu knife reaction",c.Companions[1].KnifeReactions==1&&!c.pihu.fallen&&!c.bawan.fallen);Shot("pihu-reaction");Next();}
        else if(step==11&&t>1)
        {p.Warp(new Vector3(0,.06f,-8),new Vector3(0,1.5f,10));Click();Next();}
        else if(step==12&&t>.26f){Shot("knife-swing-peak");Shot("knife-swing-4x3",1200,900);Next();}
        else if(step==13&&t>.7f){Shot("knife-rest");Press(UnityEngine.InputSystem.Key.Digit2);Next();}
        else if(step==14&&t>.4f)
        {p.Warp(new Vector3(-3.2f,.06f,-4.2f),new Vector3(-3.2f,1,-2));Click();Next();}
        else if(step==15&&t>.6f)
        {Check("cover consumes round without enemy damage",w.Shots==1&&!w.Loaded&&c.Enemies.All(e=>e.Health==100));p.Warp(new Vector3(-5.1f,.06f,-6.3f),c.Companions[0].AimPoint);Click();Next();}
        else if(step==16&&t>.5f)
        {Check("empty rifle still gives companion warning",w.Feedback.Contains("夥伴")&&w.Shots==1);Press(UnityEngine.InputSystem.Key.R);Next();}
        else if(step==17&&t>1.2f){Shot("rifle-reload-arm");Next();}
        else if(step==18&&t>1.7f)
        {
            Check("reload still loads exactly one",w.Loaded&&w.Reserve==7);foreach(var e in c.Enemies)e.enabled=true;
            p.Warp(new Vector3(0,.06f,3.6f),c.Enemies[0].AimPoint);Click();Next();
        }
        else if(step==19&&t>.8f){Check("rifle enemy defeated",c.Enemies[0].Defeated);Shot("police-rifle-falling");Next();}
        else if(step==20&&t>1.5f)
        {
            RifleGround(c.Enemies[0]);p.FocusOn(c.Enemies[0].transform.position+Vector3.forward*.5f);Shot("police-rifle-ground");Press(UnityEngine.InputSystem.Key.Digit1);Next();
        }
        else if(step==21&&t>.4f){p.Warp(c.Enemies[1].transform.position+new Vector3(0,.06f,-1.25f),c.Enemies[1].AimPoint);Click();Next();}
        else if(step==22&&t>.8f){Check("enemy knife damage remains 60",c.Enemies[1].Health==40);Click();Next();}
        else if(step==23&&t>.8f){Check("knife enemy defeated",c.Enemies[1].Defeated);Press(UnityEngine.InputSystem.Key.Digit2);Next();}
        else if(step==24&&t>.4f){Press(UnityEngine.InputSystem.Key.R);Next();}
        else if(step==25&&t>2.9f){p.Warp(c.Enemies[2].transform.position+new Vector3(0,.06f,-3.5f),c.Enemies[2].AimPoint);Click();Next();}
        else if(step==26&&c.CurrentStage==Chapter3Combat.Stage.Cleared&&p.canMove)
        {
            Check("three police defeated",c.DefeatedCount==3);foreach(var e in c.Enemies)RifleGround(e);
            Check("camera faces onward",Vector3.Dot(p.view.transform.forward,Vector3.forward)>.8f);
            Check("Bawan gathered ahead",c.bawan.transform.position.z>p.transform.position.z+1.5f);
            Shot("escort-start-forward");Next();
        }
        else if(step==27&&t>4)
        {Check("guide waits when player stays behind",c.EscortWaiting);Shot("escort-waits");tracking=true;prevB=c.bawan.transform.position;prevP=c.pihu.transform.position;Next();}
        else if(step==28&&!c.Complete)
        {
            if(tracking)
            {
                if(!Chapter3PathMotor.Clear(prevB,c.bawan.transform.position)||!Chapter3PathMotor.Clear(prevP,c.pihu.transform.position))throw new Exception("Escort intersected combat obstacle");
                prevB=c.bawan.transform.position;prevP=c.pihu.transform.position;
            }
            Follow(p,c.bawan);
            if(Flat(p.transform.position,c.exitPoint.position)<3.5f)Move(p,c.exitPoint.position);
        }
        if(step==28&&c.Complete)
        {Check("escort completes with both companions",!c.bawan.fallen&&!c.pihu.fallen&&Flat(c.bawan.transform.position,c.exitPoint.position)<.5f&&Flat(c.pihu.transform.position,c.exitPoint.position)<2.4f);Shot("escort-complete");report.completed=true;Finish();}
    }
    static void RifleGround(Chapter3Enemy e)
    {
        var bounds=e.rifle.weapon.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>r.bounds).ToArray();
        float low=bounds.Min(b=>b.min.y),high=bounds.Max(b=>b.max.y);
        Check(e.name+" rifle settled low="+low+" high="+high,low>=.01f&&low<.07f&&high<.5f);
    }
    static void Move(Chapter3Player p,Vector3 target){var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);if(d.magnitude>.08f)p.Move(d.normalized*p.speed*Mathf.Min(Time.deltaTime,.04f));}
    static void Follow(Chapter3Player p,Chapter2Actor actor){if(Flat(p.transform.position,actor.transform.position)>2.1f)Move(p,actor.transform.position);if(p.canMove)p.FocusOn(actor.transform.position+Vector3.up*1.25f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Press(UnityEngine.InputSystem.Key key)
    {
        if(Application.isBatchMode)
        {
            var w=Object.FindFirstObjectByType<Chapter3Flow>().combat.weapons;
            if(key==UnityEngine.InputSystem.Key.Digit1){w.Select(Chapter3Weapons.Weapon.Knife);return;}
            if(key==UnityEngine.InputSystem.Key.Digit2){w.Select(Chapter3Weapons.Weapon.Rifle);return;}
            if(key==UnityEngine.InputSystem.Key.R){w.Reload();return;}
        }
        if(keyUntil>0)return;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));keyUntil=Time.time+.1f;
    }
    static void Click()
    {
        // A batch Editor has no focused Game View and cannot hold cursor lock.
        // Invoke the same attack entry point there; keyboard events remain injected.
        if(Application.isBatchMode)
        {
            var w=Object.FindFirstObjectByType<Chapter3Flow>().combat.weapons;
            bool ready=w.Ready;bool accepted=w.Attack();
            File.AppendAllText(Dir+"attacks.txt","step="+step+" time="+Time.time+" ready="+ready+" accepted="+accepted+" weapon="+w.Selected+" slashes="+w.Slashes+" feedback="+w.Feedback+"\n");return;
        }
        Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));mouseUntil=Time.time+.1f;
    }
    static void Next(){step++;at=Time.time;}
    static void Check(string name,bool success){report.checks.Add(name+"="+success);if(!success)throw new Exception(name);}
    static void Finish()
    {
        report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(Key);bool passed=report.completed&&report.errors.Count==0;report=null;EditorApplication.isPlaying=false;
        if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(passed?0:1);
    }
    static void Shot(string name,int width=1600,int height=900)
    {
        Canvas.ForceUpdateCanvases();var cam=Camera.main;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
        var rt=RenderTexture.GetTemporary(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=(float)width/height;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),"missingMeshes="+all.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(m=>!m.sharedMesh),"sceneCount="+SceneManager.sceneCount});
    }
}

[DefaultExecutionOrder(10000)]
public sealed class Chapter3FixesDriver : MonoBehaviour
{
    void LateUpdate(){Chapter3FixesProbe.PlayTick();}
}
