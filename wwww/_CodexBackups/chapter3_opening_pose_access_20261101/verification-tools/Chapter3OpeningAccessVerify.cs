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
public static class Chapter3OpeningAccessVerify
{
    const string Dir="_CodexBackups/chapter3_opening_pose_access_20261101/",SessionKey="Chapter3OpeningAccessVerify";
    static Chapter3OpeningAccessDriver driver;
    static Chapter3Flow f;static Report report;static string mode;static bool background;
    [Serializable] sealed class Report {public bool completed;public List<string> checks=new List<string>(),observations=new List<string>(),errors=new List<string>();}
    static Chapter3OpeningAccessVerify()
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
            {SessionState.SetString(SessionKey,c.Substring(4));EditorApplication.isPlaying=true;}
            else if(c=="stop"){SessionState.EraseString(SessionKey);EditorApplication.isPlaying=false;}
            else File.WriteAllText(Dir+"command-error.txt","Not clean compiled third chapter in Edit Mode");
        }
        if(Application.isPlaying&&!string.IsNullOrEmpty(SessionState.GetString(SessionKey,""))&&!driver)
            driver=new GameObject("Temporary opening and access verification").AddComponent<Chapter3OpeningAccessDriver>();
    }
    public static IEnumerator Run()
    {
        mode=SessionState.GetString(SessionKey,"");report=new Report();background=Application.runInBackground;Application.runInBackground=true;
        Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
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
    static IEnumerator Scenario()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal scene",f&&f.startAtStation);
        if(mode=="opening") {yield return Opening();yield break;}
        f.Carry(Chapter3Weapons.Weapon.Knife);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school ready");
        var s=f.school;var b=s.Battle;var p=f.player;
        p.Warp(s.tado.transform.position+new Vector3(0,.06f,-2.5f),s.tado.transform.position+Vector3.up*1.1f);
        report.observations.Add("School start fixture: enter via shortcut and place player near Tado; normal proximity dialogue follows.");
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Observing,8,"Tado briefing");
        yield return new WaitForSeconds(1);yield return Shot("tado-"+mode);
        var rig=s.tado.Rig;
        report.observations.Add("Tado height="+rig.Height+" head="+rig.Head.position+" hips="+rig.Hips.position+" hands="+rig.LeftHand.position+","+rig.RightHand.position);
        if(mode!="before")
        {
            Check("Tado remains crouched",s.tado.GetComponent<Chapter3SchoolCompanionPose>().UnderFireWeight>.9f);
            Check("Tado hands below chest",rig.LeftHand.position.y<rig.Head.position.y-.3f&&rig.RightHand.position.y<rig.Head.position.y-.3f);
            Check("hands kept apart",Vector3.Distance(rig.LeftHand.position,rig.RightHand.position)>.25f);
        }
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"normal mission choice");
        Check("choose Tado",b.Choose(Chapter3SchoolBattle.Mission.Tado));yield return Wait(()=>b.Fighting,25,"fight begins");
        Check("movement enabled",p.canMove&&p.canLook);Check("four officers",b.Enemies.Count==4);
        Check("Tado cover cleared",!s.tado.GetComponent<Chapter3SchoolCompanionPose>().UnderFire);
        if(mode=="before")
        {
            var start=new Vector3(5,.06f,23);p.Warp(start,new Vector3(5,1,34));Physics.SyncTransforms();
            yield return Keys(Key.W,2);report.observations.Add("Before: W 2 seconds at z23 -> "+p.transform.position+" step="+b.Step);
            Check("old boundary reproduced",p.transform.position.z<=24.02f&&p.transform.position.z>23.9f);
            report.observations.Add("Constrain desired z30 -> "+s.Constrain(new Vector3(2,0,24),new Vector3(2,0,30)));
            yield return Shot("blocked-before");yield break;
        }
        yield return Walk(new Vector3(-4,0,-18),1,20);
        yield return Walk(new Vector3(0,0,-6),.6f,15);
        yield return Walk(new Vector3(0,0,5),.6f,15);
        yield return Walk(new Vector3(5,0,23),.5f,20);
        p.FocusOn(new Vector3(5,p.view.transform.position.y,40));var before=p.transform.position;
        yield return Keys(Key.W,2.5f);Check("W crosses old z24 wall",p.transform.position.z>28);
        var after=p.transform.position;yield return Keys(Key.S,.5f);Check("S moves back",p.transform.position.z<after.z-.8f);
        after=p.transform.position;yield return Keys(Key.A,.5f);Check("A moves left",p.transform.position.x<after.x-.8f);
        after=p.transform.position;yield return Keys(Key.D,.5f);Check("D moves right",p.transform.position.x>after.x+.8f);
        report.observations.Add("WASD Input System events used actual Chapter3Player.Update and CharacterController; old-wall crossing "+before+" -> "+p.transform.position);
        Check("visiting later groups cannot skip first fight",b.Step==0&&b.Enemies.All(e=>!e.Defeated));
        yield return Walk(new Vector3(5,0,32),.7f,12);
        yield return Walk(new Vector3(8,0,35),.7f,12);
        var enemy=b.Enemies[3];float until=Time.realtimeSinceStartup+18;
        while(Flat(p.transform.position,enemy.transform.position)>1.7f)
        {if(Time.realtimeSinceStartup>until)throw new Exception("Cannot walk into last melee group: "+p.transform.position+" enemy="+enemy.transform.position);Move(enemy.transform.position);yield return null;}
        Check("walk reaches fourth group before first clear",Flat(p.transform.position,enemy.transform.position)<1.8f&&b.Step==0);
        yield return Shot("last-group-reached");
        Check("retreat blocked until four cleared",!b.CallRetreat());
        // Keep the test on input and traversal. Contact uses the actual knife entry.
        until=Time.realtimeSinceStartup+16;
        while(!enemy.Defeated)
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("Knife failed to hit accessible fourth officer: "+f.combat.weapons.Feedback);
            if(Flat(p.transform.position,enemy.transform.position)>1.2f)Move(enemy.transform.position);
            p.FocusOn(enemy.AimPoint);if(f.combat.weapons.Ready)f.combat.weapons.Attack();yield return null;
        }
        Check("knife defeats fourth officer without warp",enemy.Defeated);Check("out of order defeat cannot skip stage",b.Step==0);
        b.Damage(100,p.transform.position);Check("retry accepted",b.Retry());yield return null;Check("retry keeps knife",f.combat.weapons.Selected==Chapter3Weapons.Weapon.Knife);
        Check("retry has no old interior bound",s.Constrain(new Vector3(2,0,24),new Vector3(2,0,30)).z==30);
        f.EnterSchool();yield return Wait(()=>s.Exploring,20,"F8 reset");Check("F8 restores normal briefing",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
        Check("F8 restores cover",s.tado.GetComponent<Chapter3SchoolCompanionPose>().UnderFire);
    }
    static IEnumerator Opening()
    {
        yield return Wait(()=>f.station.CurrentLine!=null,15,"fixed opening line");
        Check("brief fixed introduction",f.station.CurrentLine=="我們到馬赫坡駐在所了。莫那在前面，我帶你過去，聽他安排。跟我來。");
        Check("no opening chat",!f.ui.chat.activeSelf&&!f.station.Opening.Active);yield return Shot("opening-introduction");
        var b=f.station.bawan.transform.position;
        yield return Wait(()=>f.station.CurrentStage==Chapter3StationController.Stage.Approach,12,"automatic escort");
        yield return new WaitForSeconds(1.5f);
        Check("guide starts walking automatically",Flat(b,f.station.bawan.transform.position)>1);
        Check("player free to follow",f.player.canMove&&f.player.canLook);
        Check("opening made no LLM exchanges",f.station.Opening.CompletedTurns==0&&f.station.Opening.GeneratedReplies==0&&!f.ui.chat.activeSelf);
        yield return Shot("automatic-escort");
        float until=Time.realtimeSinceStartup+50;
        while(f.station.CurrentStage!=Chapter3StationController.Stage.Briefing)
        {if(Time.realtimeSinceStartup>until)throw new Exception("Escort timeout");if(Flat(f.player.transform.position,f.station.bawan.transform.position)>2.2f)Move(f.station.bawan.transform.position);yield return null;}
        yield return Wait(()=>f.station.CurrentStage==Chapter3StationController.Stage.WeaponChoice,40,"Mona briefing and weapon choice");
        Check("choose knife after escort",f.station.ChooseWeapon(Chapter3Weapons.Weapon.Knife));
        f.player.Warp(f.station.knifeFocus.position+new Vector3(0,.06f,-1.4f),f.station.knifeFocus.position);
        Check("pickup works",f.station.TakeKnife());
        Check("readiness conversation retained",f.station.CurrentStage==Chapter3StationController.Stage.Readiness&&f.ui.chat.activeSelf);
        report.observations.Add("Weapon pickup uses a table-side positioning fixture; no LLM answer submitted.");
        f.EnterStation();yield return Wait(()=>f.station.CurrentLine!=null,15,"restart fixed opening");
        Check("F6 still bypasses opening chat",!f.ui.chat.activeSelf&&!f.station.Opening.Active);
        f.EnterSchool();yield return Wait(()=>f.school.Exploring,15,"shortcut during intro");
        Check("intro cancelled on shortcut",f.Region=="School"&&!f.station.bawan.speaking);
    }
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
public sealed class Chapter3OpeningAccessDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3OpeningAccessVerify.Run();}}
