using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3OpeningProbe
{
    const string Dir="_CodexBackups/chapter3_station_opening_20261013/",KeyName="Chapter3OpeningProbe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),replies=new List<string>();}
    static Report report;static Chapter3OpeningProbeDriver driver;static float began,progressAt;static bool expectsExit;
    static Chapter3OpeningProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
    static void Log(string m,string stack,LogType t){if(report==null)return;if(t==LogType.Warning)report.warnings.Add(m);if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)report.errors.Add(m+"\n"+stack);}
    static void State(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode&&expectsExit&&report!=null){Check("game stops Play after refusal",true);report.completed=true;Finish();}}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="author"){Chapter3StationOpeningAuthoring.Apply();Audit();File.WriteAllText(Dir+"author-done.txt",DateTime.Now.ToString("O"));}
                else if(cmd=="audit")Audit();
                else if(cmd=="archive"){EditorSceneManager.OpenScene(Chapter3StationOpeningAuthoring.ArchivePath);SessionState.SetString(KeyName,"test-archive");EditorApplication.isPlaying=true;}
                else if(cmd=="main"){EditorSceneManager.OpenScene(Chapter3StationOpeningAuthoring.MainPath);Audit();}
                else if(cmd=="stop"){SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){SessionState.SetString(KeyName,cmd);EditorApplication.isPlaying=true;}
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary station opening verification").AddComponent<Chapter3OpeningProbeDriver>();
    }
    public static IEnumerator Run()
    {
        Application.runInBackground=true;yield return null;report=new Report{mode=SessionState.GetString(KeyName,"")};began=Time.realtimeSinceStartup;
        yield return Guard(Scenario(Object.FindFirstObjectByType<Chapter3Flow>(),report.mode));
        if(report!=null){report.completed=true;Finish();}
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool moved=false;object value=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());Shot("failure-"+report.mode);Finish();yield break;}
            if(!moved){stack.Pop();continue;}if(value is IEnumerator sub){stack.Push(sub);continue;}yield return value;
        }
    }
    static IEnumerator Scenario(Chapter3Flow f,string mode)
    {
        if(mode=="test-archive")
        {
            var v=Object.FindFirstObjectByType<Chapter3Controller>();Check("archive has village and no main flow",v&&!f);
            v.replyTimeout=.01f;v.lineSeconds=.3f;
            yield return Wait(()=>v.CurrentStage==Chapter3Controller.Stage.Conversation,12,"archive opening");Shot("archive-opening");
            v.SubmitReply("好");yield return Wait(()=>!v.WaitingForReply,5,"archive reply 1");v.SubmitReply("好");yield return Wait(()=>!v.WaitingForReply,5,"archive reply 2");
            yield return new WaitForSeconds(2.2f);Check("archive pickup unlocked",v.Depart());
            yield return Walk(v.player,new Vector3(-2.55f,0,2.6f),"archive table");yield return Press(Key.E);
            Check("archive retains original knife",v.HasKnife);float start=Time.time;
            while(!v.PartComplete){Follow(v.player,v.bawan);if(Time.time-start>55)throw new Exception("archive route timeout");yield return null;}
            Check("archive ends with replay card",v.ui.ending.activeSelf);Shot("archive-ending");
            int previous=v.GetInstanceID();Object.DontDestroyOnLoad(driver.gameObject);v.ui.replay.onClick.Invoke();
            yield return Wait(()=>{var fresh=Object.FindFirstObjectByType<Chapter3Controller>();return fresh&&fresh.GetInstanceID()!=previous&&fresh.CurrentStage==Chapter3Controller.Stage.Conversation;},15,"archive replay");
            Check("archive replays without Build Settings entry",SceneManager.GetActiveScene().path==Chapter3StationOpeningAuthoring.ArchivePath&&!Object.FindFirstObjectByType<Chapter3Flow>());yield break;
        }
        if(mode.StartsWith("test-school-")){yield return KnifeMission(f,mode.Contains("pihu"));yield break;}
        var s=f.station;var p=f.player;s.lineSeconds=.3f;f.combat.lineSeconds=.3f;
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Conversation,12,"station opening");
        Check("Play begins at station without village",f.startAtStation&&f.Region=="Station"&&!f.village&&!f.villageRegion&&p.station==s&&p.chapter==null);
        Check("one scene camera and listener",SceneManager.sceneCount==1&&Camera.allCameras.Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1&&Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
        Check("opening matches arrival viewpoint",Flat(p.transform.position,new Vector3(0,0,-18))<.05f&&Flat(s.bawan.transform.position,new Vector3(0,0,-15))<.05f);
        Check("knife right rifle left",Vector3.Dot(s.knifeFocus.position-p.transform.position,p.transform.right)>0&&Vector3.Dot(s.rifleFocus.position-p.transform.position,p.transform.right)<0);
        Shot(mode+"-opening");
        Check("cannot collect or choose before briefing",!s.TakeKnife()&&!s.TakeRifle()&&!s.ConfirmDeparture()&&!s.ChooseWeapon(Chapter3Weapons.Weapon.Knife));
        if(mode=="test-cancel-opening")
        {
            s.Opening.SubmitReply("我會跟上");yield return Press(Key.F8);yield return new WaitForSeconds(3);
            Check("opening cancellation cannot advance school",f.Region=="School"&&!s.Opening.Active&&!s.Opening.WaitingForReply&&!Arrow(f));
            yield return Press(Key.F6);yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Conversation,10,"opening restarted");
            Check("reentry resets opening and weapons",s.Opening.CompletedTurns==0&&!s.HasWeapon&&!f.CarriedWeapon.HasValue&&!s.Choice.gameObject.activeSelf);yield break;
        }
        s.Opening.replyTimeout=mode=="test-live"?25:.01f;
        Check("empty opening rejected",!s.Opening.SubmitReply("  "));
        f.ui.input.text="我們已經到駐在所了嗎？";f.ui.send.onClick.Invoke();
        Check("duplicate opening rejected",!s.Opening.SubmitReply("再送一次"));
        yield return Wait(()=>!s.Opening.WaitingForReply,32,"opening answer one");
        Check("one answer still stays at opening",s.Opening.CompletedTurns==1&&s.CurrentStage==Chapter3StationController.Stage.Conversation);
        report.replies.Add(f.ui.response.text);f.ui.input.text="我會跟緊你，也照應比胡。";f.ui.send.onClick.Invoke();
        yield return Wait(()=>!s.Opening.WaitingForReply,32,"opening answer two");
        Check("exactly two exchanges",s.Opening.CompletedTurns==2&&!s.Opening.SubmitReply("第三次"));report.replies.Add(f.ui.response.text);
        report.replies.Add("generated="+s.Opening.GeneratedReplies+" fallback="+s.Opening.FallbackReplies);
        Check("no continue button after two answers",!f.ui.depart.gameObject.activeSelf);
        var lines=new HashSet<string>();float escortStart=Time.time;
        while(s.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
        {
            if(!string.IsNullOrEmpty(s.CurrentLine))lines.Add(s.CurrentLine);Follow(p,s.bawan);Progress("escort and briefing "+s.CurrentStage);
            if(Time.time-escortStart>60)throw new Exception("Mona escort timeout "+p.transform.position+" "+s.bawan.transform.position);yield return null;
        }
        Check("Mona explains police attack",lines.Any(l=>l.Contains("日本警察")));
        Check("choice visible and world paused",s.Choice.gameObject.activeSelf&&!p.canMove&&!p.canLook&&!Arrow(f));Shot(mode+"-choice");
        bool knife=mode.Contains("knife")||mode=="test-refuse";
        var selected=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        var other=knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife;
        if(knife)s.Choice.knife.onClick.Invoke();else yield return Press(Key.Digit2);
        Check("choice commits once",s.ChosenWeapon==selected&&!s.ChooseWeapon(other));
        var target=knife?s.knifeFocus:s.rifleFocus;var wrong=knife?s.rifleFocus:s.knifeFocus;
        Check("yellow arrow targets chosen weapon",Arrow(f)&&f.ui.GetComponentInChildren<Chapter3PickupArrow>().Target==target);
        yield return null;Shot(mode+"-arrow");Check("far pickup rejected",!(knife?s.TakeKnife():s.TakeRifle()));
        yield return Walk(p,new Vector3(wrong.position.x,0,-1.8f),"unchosen table");
        Check("wrong choice cannot be picked up",!(knife?s.TakeRifle():s.TakeKnife())&&!s.HasWeapon);
        yield return Walk(p,new Vector3(target.position.x,0,-1.8f),"chosen table");yield return Press(Key.E);
        Check("pickup immediately opens readiness without second E",s.HasWeapon&&s.Readiness.Active&&s.CurrentStage==Chapter3StationController.Stage.Readiness&&f.ui.chat.activeSelf&&!p.canMove);
        Check("inventory contains exactly one weapon",s.HasKnife==knife&&s.HasRifle!=knife&&f.CarriedWeapon==selected);
        Check("picked model hidden other remains",!(knife?s.knifePickup:s.riflePickup).gameObject.activeSelf&&(knife?s.riflePickup:s.knifePickup).gameObject.activeSelf&&!Arrow(f));
        Check("readiness names carried weapon",s.Readiness.LastReply.Contains(knife?"獵刀":"步槍"));yield return new WaitForSeconds(3);Shot(mode+"-readiness");
        Check("second weapon blocked during readiness",!(knife?s.TakeRifle():s.TakeKnife()));
        if(mode=="test-cancel-ready")
        {
            s.Readiness.SubmitReply("準備好了");yield return Press(Key.F8);yield return new WaitForSeconds(3);
            Check("readiness cancellation safe",f.Region=="School"&&!s.Readiness.Active&&!s.Choice.gameObject.activeSelf&&!Arrow(f));
            yield return Press(Key.F6);yield return Wait(()=>s.Opening.Active,10,"reenter after readiness");Check("equipment reset on F6",!s.HasWeapon&&!f.CarriedWeapon.HasValue);yield break;
        }
        s.Readiness.replyTimeout=mode=="test-live"?25:.01f;
        if(mode.Contains("refuse"))
        {
            s.Readiness.SubmitReply("還沒準備好");yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Refused,35,"refuse");
            yield return Press(Key.F7);Check("refusal cannot be skipped",f.Region=="Station");
            yield return Wait(()=>s.CurrentLine!=null&&s.CurrentLine.Contains((knife?"刀":"槍")+"都拿了"),15,"weapon rebuke");Shot(mode+"-rebuke");
            Check("Mona close and angry",Flat(s.mona.transform.position,p.transform.position)<2.1f&&s.mona.GetComponent<Chapter3MonaRebuke>().Rebuking);
            yield return Wait(()=>s.CurrentLine!=null&&s.CurrentLine.Contains("把"+(knife?"刀":"槍")+"留下"),18,"return weapon");Shot(mode+"-refusal");
            expectsExit=true;yield return new WaitForSeconds(20);throw new Exception("game did not end");
        }
        if(mode!="test-live")
        {
            s.Readiness.SubmitReply("我們要去哪裡？");yield return Wait(()=>!s.Readiness.WaitingForReply,8,"unclear response");
            Check("unclear reply stays in chat",s.CurrentStage==Chapter3StationController.Stage.Readiness&&s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Unclear);
        }
        f.ui.input.text="我準備好了";f.ui.send.onClick.Invoke();
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Departure,35,"ready");report.replies.Add("ready fallback="+s.Readiness.UsedFallback+" "+s.Readiness.LastReply);
        yield return Wait(()=>p.canMove,10,"can walk after ready");
        yield return Walk(p,new Vector3(wrong.position.x,0,-1.8f),"unused table after ready");yield return new WaitForEndOfFrame();
        Check("proximity warns only one weapon",f.ui.hint.text.Contains("只能持有一種武器"));Shot(mode+"-single-weapon-warning");
        yield return Press(Key.E);Check("E cannot collect unused weapon",s.HasKnife==knife&&s.HasRifle!=knife);
        float departStart=Time.time;
        while(f.Region=="Station"){Follow(p,s.bawan);Progress("departure "+s.Waypoint);if(Time.time-departStart>100)throw new Exception("departure stuck "+p.transform.position);yield return null;}
        yield return Wait(()=>f.combat.Fighting,15,"combat");var w=f.combat.weapons;
        Check("combat keeps chosen weapon",w.Selected==selected&&f.CarriedWeapon==selected);Check("cannot switch to unowned weapon",!w.Select(other));Shot(mode+"-combat");
        yield return Wait(()=>w.Ready,3,"attack ready");Check("chosen weapon attacks",w.Attack());yield return new WaitForSeconds(.8f);
        Check("only chosen attack counter changes",knife?w.Slashes==1&&w.Shots==0:w.Shots==1&&w.Slashes==0);
        f.combat.Damage(100,p.transform.position);yield return Press(Key.R);
        Check("combat retry retains weapon",f.combat.Fighting&&w.Selected==selected&&!w.Select(other));
        yield return Press(Key.F8);yield return Wait(()=>f.school.Exploring,15,"school entry");
        var b=f.school.Battle;
        yield return Walk(p,new Vector3(-4.7f,0,-7.2f),"Tado");yield return Press(Key.E);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"school choice");b.Choose(knife?Chapter3SchoolBattle.Mission.Bawan:Chapter3SchoolBattle.Mission.Pihu);
        yield return Wait(()=>b.Fighting,15,"school fight");
        Check("school retains chosen weapon",w.Selected==selected&&!w.Select(other));b.Damage(100,p.transform.position);yield return Press(Key.R);
        Check("school retry retains choice",b.Fighting&&w.Selected==selected&&!w.Select(other));
    }
    static IEnumerator KnifeMission(Chapter3Flow f,bool pihu)
    {
        f.Carry(Chapter3Weapons.Weapon.Knife);f.EnterSchool();var p=f.player;
        yield return Wait(()=>f.school.Exploring,12,"knife mission entrance");yield return new WaitForSeconds(.5f);yield return Press(Key.Space);
        yield return Walk(p,new Vector3(-4.7f,0,-7.2f),"Tado");yield return Press(Key.E);var b=f.school.Battle;
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"mission choice");b.Choose(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan);
        yield return Wait(()=>b.Fighting,15,"knife mission");var w=f.combat.weapons;
        Check("knife only mission inventory",w.Selected==Chapter3Weapons.Weapon.Knife&&!w.Select(Chapter3Weapons.Weapon.Rifle));
        if(!pihu)
        {
            yield return Route(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-25.7f,0,13)},"west guard one");
            yield return Cut(f,b.Enemies[0]);yield return PressWork(b);Check("knife clears first barricade",b.Step==1);
            yield return Route(p,new[]{new Vector3(-27,0,24),new Vector3(-27,0,35),new Vector3(-28,0,43)},"west guard two");
            yield return Cut(f,b.Enemies[1]);yield return PressWork(b);Check("knife clears second barricade",b.Step==2);
        }
        else
        {
            yield return Route(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,8),new Vector3(27.5f,0,14),new Vector3(27.5f,0,24),new Vector3(27.5f,0,27.4f)},"east guard one");
            yield return Cut(f,b.Enemies[0]);
            yield return Walk(p,new Vector3(27.5f,0,22),"return to first command range");
            yield return Wait(()=>b.AtCrossing,35,"first escort cover");yield return Press(Key.E);Check("knife branch can command first crossing",b.Advancing);
            yield return Wait(()=>b.Step==1,35,"first escort arrival");
            yield return Route(p,new[]{new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27.5f,0,53),new Vector3(27.5f,0,57.2f),new Vector3(28.6f,0,58.5f)},"east guard two");
            yield return Cut(f,b.Enemies[1]);
            yield return Route(p,new[]{new Vector3(27.5f,0,53),new Vector3(32,0,43),new Vector3(29,0,37)},"return to escort command");
            yield return Wait(()=>b.AtCrossing,35,"second escort cover");yield return Press(Key.E);Check("knife branch can command second crossing",b.Advancing);
            yield return Wait(()=>b.Step==2,35,"second escort arrival");
        }
        Check("both school enemies defeated by knife",b.Enemies.All(e=>e.Defeated)&&w.Slashes>=4&&w.Shots==0&&b.Retries==0);
        Shot(report.mode+"-cleared");
        var exit=pihu?new[]{new Vector3(32,0,53),new Vector3(27.5f,0,54),new Vector3(27.5f,0,60),new Vector3(25,0,68),new Vector3(12,0,74),new Vector3(0,0,78)}:new[]{new Vector3(-27,0,54),new Vector3(-25,0,66),new Vector3(-12,0,74),new Vector3(0,0,78)};
        yield return Route(p,exit,"knife route reunion");yield return Wait(()=>b.Complete,65,"knife reunion");
        Check("knife branch reaches three-person reunion",b.Complete&&new[]{f.school.bawan,f.school.pihu,f.school.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4));
        report.checks.Add("remaining health="+b.Health);Shot(report.mode+"-reunion");
    }
    static IEnumerator Route(Chapter3Player p,Vector3[] points,string label){foreach(var point in points)yield return Walk(p,point,label);}
    static IEnumerator Cut(Chapter3Flow f,Chapter3Enemy enemy)
    {
        float start=Time.time;int slashes=f.combat.weapons.Slashes;
        while(!enemy.Defeated)
        {
            if(!f.combat.Fighting)throw new Exception("player down before knife kill");
            if(Time.time-start>6)throw new Exception("knife unreachable "+enemy.transform.position+" player "+f.player.transform.position);
            f.player.FocusOn(enemy.AimPoint);if(f.combat.weapons.Ready)f.combat.weapons.Attack();yield return null;
        }
        Check("guard killed by actual knife swings",f.combat.weapons.Slashes>=slashes+2);
    }
    static IEnumerator PressWork(Chapter3SchoolBattle b)
    {yield return Walk(Object.FindFirstObjectByType<Chapter3Player>(),b.Barricades[b.Step].position+Vector3.back*2.5f,"reach barricade handle");yield return new WaitForSeconds(.5f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return new WaitForSeconds(2.2f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.2f);}
    static bool Arrow(Chapter3Flow f){var a=f.ui.GetComponentInChildren<Chapter3PickupArrow>();return a&&a.Target&&a.gameObject.activeInHierarchy;}
    static void Follow(Chapter3Player p,Chapter2Actor actor){var d=Vector3.ProjectOnPlane(actor.transform.position-p.transform.position,Vector3.up);if(p.canMove&&d.magnitude>1.7f){p.Move(d.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));p.FocusOn(actor.transform.position+Vector3.up*1.3f);}}
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label)
    {float start=Time.realtimeSinceStartup;while(Flat(p.transform.position,target)>.25f){
        if(label.Contains("guard")&&p.school&&p.school.Battle.Enemies.Any(e=>!e.Defeated&&Flat(e.transform.position,target)<2&&Flat(e.transform.position,p.transform.position)<1.85f))yield break;
        if(label=="knife route reunion"&&!p.canMove&&Flat(p.transform.position,target)<4)yield break;if(Time.realtimeSinceStartup-start>30)throw new Exception("stuck "+label+" "+p.transform.position);var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.Move(d.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));Progress(label);yield return null;}}
    static IEnumerator Wait(Func<bool> done,float timeout,string label){float start=Time.realtimeSinceStartup;while(!done()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(.12f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.12f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string s){if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;File.WriteAllText(Dir+"progress.txt",report.mode+" "+s);}
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish(){if(report==null)return;report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(KeyName);report=null;expectsExit=false;EditorApplication.isPlaying=false;}
    static void Shot(string name)
    {
        var cam=Camera.main;if(!cam)return;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3OpeningProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3OpeningProbe.Run();}}
