using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Temporary validation harness. Archive outside Assets after the Editor checks.
[InitializeOnLoad]
public static class Chapter3MissionReworkProbe
{
    const string Dir="_CodexBackups/chapter3_mission_rework_20261014/",KeyName="Chapter3MissionReworkProbe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();}
    static Report report;static Chapter3MissionReworkProbeDriver driver;static float began,progressAt;static bool sawEscortTelegraph;
    static Chapter3MissionReworkProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
    static void Log(string m,string stack,LogType t){if(report==null)return;if(t==LogType.Warning)report.warnings.Add(m);if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)report.errors.Add(m+"\n"+stack);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="audit")Audit();
                else if(cmd=="stop"){SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){SessionState.SetString(KeyName,cmd);EditorApplication.isPlaying=true;}
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary mission rework verification").AddComponent<Chapter3MissionReworkProbeDriver>();
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
        Check("main chapter scene loaded",f&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity");
        bool pihu=mode.Contains("pihu"),knife=mode.Contains("knife");
        var expectedWeapon=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        f.Carry(expectedWeapon);yield return Press(Key.Space);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"Space school entry");
        var s=f.school;var p=f.player;var b=s.Battle;var ambient=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("sports day remains calm before mission",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!ambient.Panicking);
        yield return new WaitForSeconds(.4f);yield return Press(Key.Space);
        yield return Walk(p,new Vector3(-4.7f,0,-7.2f),"meet Tado");yield return Press(Key.E);
        Check("Tado interaction begins observation",b.CurrentStage==Chapter3SchoolBattle.Stage.Observing);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,22,"mission choice");
        if(pihu)b.Menu.pihu.onClick.Invoke();else yield return Press(Key.Digit1);
        Check("outbreak follows chosen mission",b.CurrentStage==Chapter3SchoolBattle.Stage.Outbreak&&ambient.Panicking);
        yield return Wait(()=>b.Fighting,15,"mission starts");
        Check("fresh mission begins before first checkpoint",b.Step==0&&b.CheckpointStep==0&&b.Retries==0);
        Check("exclusive inventory follows selected weapon",f.CarriedWeapon==expectedWeapon&&f.combat.weapons.Selected==expectedWeapon&&!f.combat.weapons.Select(knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife));
        Check("both authored guards present",b.Enemies.Count==2);
        var future=b.Enemies[1];int futureHealth=future.Health;
        Check("second guard remains inactive before first checkpoint",!future.gameObject.activeInHierarchy&&!future.Defeated);
        future.Hit(100,p.transform.position);yield return null;
        Check("inactive second guard rejects premature damage",future.Health==futureHealth&&!future.Defeated&&future.Shots==0);
        if(pihu)yield return PihuMission(f,knife);else yield return BawanMission(f,knife);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,12,"mission completion unlocks regroup");
        Check("completed checkpoint and current threat permit regroup",b.Step==2&&b.CheckpointStep>=1&&b.Enemies[1].Defeated&&(!b.Enemies[0]||b.Enemies[0].Defeated||!b.Enemies[0].gameObject.activeInHierarchy));
        var exit=pihu?new[]{new Vector3(32,0,53),new Vector3(27.5f,0,54),new Vector3(27.5f,0,60),new Vector3(25,0,68),new Vector3(12,0,74),new Vector3(0,0,78)}:new[]{new Vector3(-27,0,54),new Vector3(-25,0,66),new Vector3(-12,0,74),new Vector3(0,0,78)};
        yield return Route(p,exit,"walk to rendezvous");yield return Wait(()=>b.Complete&&b.Rescue&&b.Rescue.Active,65,"natural rescue handoff");
        Check("both mission routes reunite three uninjured companions",new[]{s.bawan,s.pihu,s.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4&&a.FallProgress==0));
        Check("reworked mission naturally enters existing fifth batch",b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing&&b.Rescue.Selected==Chapter3Rescue.Decision.None&&!b.Menu.result.activeSelf&&f.CarriedWeapon==expectedWeapon);
        report.observations.Add("retries="+b.Retries+" mission health="+b.Health);Shot(mode+"-reunion");
        yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"school replay");
        Check("F8 resets mission checkpoint and branch",b.Selected==Chapter3SchoolBattle.Mission.None&&b.Step==0&&b.CheckpointStep==0&&b.Retries==0);
        Check("F8 clears threat and side gate work state",!b.ThreatRevealing&&!b.CrossingArrived&&!b.CompletionBlockedByThreat&&!b.GateWorking&&b.GateWorkProgress==0&&!b.Rescue.Active);
    }
    static IEnumerator BawanMission(Chapter3Flow f,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;
        yield return Route(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-27,0,12.9f)},"first west barricade");
        Check("first barricade rejects work with living guard",!b.WorkBarricade(true,.3f)&&b.WorkProgress==0);
        yield return Wait(()=>b.BawanCoverBursts>0&&b.Enemies[0].HoldingFire,30,"Bawan actually suppresses guard");
        Check("Bawan covering shot grants safe window",b.Enemies[0].SafeWindowSeconds>0&&!b.Enemies[0].Telegraphing);Shot(report.mode+"-Bawan-cover");
        if(knife){yield return Walk(p,new Vector3(-25.7f,0,13),"first knife guard");yield return Cut(f,b.Enemies[0]);}
        else yield return Shoot(f,b.Enemies[0]);
        yield return Walk(p,b.Barricades[0].position+Vector3.back*2.5f,"first barricade handle");
        yield return Hold(Key.E,.55f);Check("first barricade still requires uninterrupted hold",b.Step==0&&b.WorkProgress==0);
        yield return Hold(Key.E,2.1f);yield return new WaitForSeconds(.8f);
        Check("first held interaction creates checkpoint one",b.Step==1&&b.CheckpointStep==1&&!b.Barricades[0].gameObject.activeSelf);
        Check("second guard enters only after first checkpoint",b.Enemies[1].gameObject.activeInHierarchy&&!b.Enemies[1].Defeated);
        Check("second gate cannot be dismantled by old hold action",!b.WorkBarricade(true,5));
        yield return ReachSideGate(f);yield return Press(Key.E);
        Check("short E asks Bawan to open side gate",b.GateCommanded);
        yield return Wait(()=>b.GateWorkProgress>.08f,15,"Bawan begins gate work");
        float early=b.GateWorkProgress;yield return new WaitForSeconds(.5f);
        Check("gate work persists after E is released",!Keyboard.current.eKey.isPressed&&b.GateCommanded&&b.GateWorkProgress>=early);Shot(report.mode+"-Bawan-working");
        b.Damage(100,p.transform.position);Check("side gate death enters retry state",b.CurrentStage==Chapter3SchoolBattle.Stage.Down);
        yield return RetryCheckpoint(f,knife,false);
        Check("west retry keeps first opening and resets unfinished gate",!b.Barricades[0].gameObject.activeSelf&&b.GateWorkProgress==0&&!b.GateCommanded);
        yield return ReachSideGate(f);yield return Press(Key.E);Check("side gate can be commanded after retry",b.GateCommanded);
        // Controlled diagnostic isolation: prevent incoming aim from pausing
        // the five-second work, then verify the living guard still blocks success.
        b.Enemies[1].HoldFire(8);report.observations.Add("gate completion predicate isolated with guard.HoldFire(8); failure retry uses injected Damage(100)");
        yield return Wait(()=>b.GateWorkProgress>=.999f,15,"full gate work with living guard");
        Check("fully opened gate cannot complete while guard lives",b.Fighting&&b.Step==1&&!b.Enemies[1].Defeated&&b.GateWorkProgress>=.999f);Shot(report.mode+"-gate-awaits-guard");
        if(knife){yield return Walk(p,new Vector3(-28,0,43),"second knife guard");yield return Cut(f,b.Enemies[1]);}
        else yield return Shoot(f,b.Enemies[1]);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,10,"protected side gate complete");
        Check("west completion needs opened gate and defeated guard",b.Step==2&&b.GateWorkProgress>=.999f&&b.Enemies[1].Defeated);
    }
    static IEnumerator ReachSideGate(Chapter3Flow f)
    {
        var p=f.player;var b=f.school.Battle;
        yield return Route(p,new[]{new Vector3(-27,0,35),new Vector3(-26.5f,0,43)},"reach side gate staging");
        yield return Wait(()=>Flat(f.school.bawan.transform.position,b.GateWorkPosition)<.28f,35,"Bawan at gate handle");
        Check("player beside gate leaves Bawan work space",Flat(p.transform.position,b.GateWorkPosition)>1.05f&&Flat(p.transform.position,f.school.bawan.transform.position)<4);
    }
    static IEnumerator RetryCheckpoint(Chapter3Flow f,bool knife,bool pihu)
    {
        var b=f.school.Battle;int retries=b.Retries;
        yield return Press(Key.R);yield return null;
        var weapon=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        Check("retry keeps second-stage checkpoint and restores health",b.Fighting&&b.Step==1&&b.CheckpointStep==1&&b.Health==100&&b.Retries==retries+1&&b.EscortPressure==0);
        Check("retry keeps task and exclusive carried weapon",b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&f.CarriedWeapon==weapon&&f.combat.weapons.Selected==weapon&&!f.combat.weapons.Select(knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife));
        Check("retry never replays first guard",b.Enemies.Count==2&&(!b.Enemies[0]||b.Enemies[0].Defeated||!b.Enemies[0].gameObject.activeInHierarchy)&&!b.Enemies[1].Defeated);
        Shot(report.mode+"-checkpoint-"+b.Retries);
    }
    static IEnumerator PihuMission(Chapter3Flow f,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;
        yield return Route(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,7)},"reach first east cover");
        yield return Wait(()=>b.AtCrossing,25,"first pair reaches cover");
        yield return ReadyThreat(f,knife,"first");
        Check("first guard reveal cannot unlock future guard",b.Step==0&&!b.Enemies[1].gameObject.activeInHierarchy);
        yield return Press(Key.E);Check("E orders first crossing",b.Advancing&&!b.Returning);
        var pihuPose=s.pihu.GetComponent<Chapter3SchoolCompanionPose>();var tadoPose=s.tado.GetComponent<Chapter3SchoolCompanionPose>();
        Check("forward command and companion acknowledgment are visible",pihuPose&&tadoPose&&pihuPose.BonesReady&&tadoPose.BonesReady&&pihuPose.ForwardSignalWeight>.01f&&tadoPose.SafeReportWeight>.01f);
        yield return new WaitForSeconds(.4f);yield return Press(Key.E);
        Check("second E recalls advancing pair",b.Returning&&!b.Advancing);
        yield return Wait(()=>!b.Returning,12,"first pair returns to safe cover");
        Check("recall keeps first stage and checkpoint uncompleted",b.Step==0&&b.CheckpointStep==0&&!b.CrossingArrived);
        if(knife)
        {
            yield return Route(p,new[]{new Vector3(27.5f,0,14),new Vector3(27.5f,0,24),new Vector3(27.5f,0,27.4f)},"first east knife guard");
            yield return Cut(f,b.Enemies[0]);yield return Walk(p,new Vector3(27.5f,0,22),"return to first command range");
        }
        else{yield return Walk(p,new Vector3(27.5f,0,12),"first east firing lane");yield return Shoot(f,b.Enemies[0]);}
        Check("defeated guard alone does not advance first stage",b.Step==0&&b.CheckpointStep==0&&!b.CrossingArrived);
        yield return Press(Key.E);Check("safe crossing accepts new command",b.Advancing);
        yield return Wait(()=>b.Step==1,25,"both companions finish first crossing");
        Check("first safe crossing saves checkpoint one",b.CheckpointStep==1&&b.Enemies[0].Defeated&&Flat(s.pihu.transform.position,new Vector3(30,0,23))<.5f&&Flat(s.tado.transform.position,new Vector3(29.15f,0,22.1f))<.5f);
        yield return new WaitForSeconds(.2f);
        Check("safe crossing visibly reports success",pihuPose.SafeReportWeight>.01f&&tadoPose.SafeReportWeight>.01f);
        yield return ReadyThreat(f,knife,"second");
        // Controlled predicate isolation: companions may reach the endpoint
        // while this still-living guard is prevented from firing for this pass.
        b.Enemies[1].HoldFire(60);report.observations.Add("second crossing completion predicate isolated with guard.HoldFire(60); no native keyboard claim");
        yield return Walk(p,new Vector3(27.5f,0,22.5f),"second command range");
        yield return Press(Key.E);Check("second living threat still allows forward order",b.Advancing&&b.Step==1&&!b.Enemies[1].Defeated);
        yield return EastSecondRoute(p);
        yield return Wait(()=>b.CrossingArrived,25,"pair arrives before second guard is defeated");
        Check("pair at endpoint cannot bypass living second guard",b.Fighting&&b.Step==1&&b.CheckpointStep==1&&b.CompletionBlockedByThreat&&!b.Enemies[1].Defeated&&Flat(s.pihu.transform.position,new Vector3(32,0,53))<.5f&&Flat(s.tado.transform.position,new Vector3(31.15f,0,52.1f))<.5f);
        Shot(report.mode+"-crossing-awaits-guard");
        b.Damage(100,p.transform.position);Check("east second-stage death enters retry",b.CurrentStage==Chapter3SchoolBattle.Stage.Down);
        report.observations.Add("second-stage death injected with Damage(100) to isolate checkpoint restoration");
        yield return RetryCheckpoint(f,knife,true);
        Check("east retry restores first safe cover",Flat(p.transform.position,new Vector3(27.5f,0,22.5f))<.6f&&Flat(s.pihu.transform.position,new Vector3(30,0,23))<.5f&&Flat(s.tado.transform.position,new Vector3(29.15f,0,22.1f))<.5f&&b.AtCrossing&&!b.CrossingArrived);
        yield return ReadyThreat(f,knife,"retry second");
        var liveGuard=b.Enemies[1];int priorShots=liveGuard.Shots;sawEscortTelegraph=false;
        yield return Press(Key.E);Check("retry permits same second crossing",b.Advancing);
        yield return Wait(()=>b.EscortPressure>0,28,"real second guard shot pressures escort");
        Check("real enemy telegraph and shot increase escort pressure",sawEscortTelegraph&&liveGuard.Shots>priorShots&&b.EscortPressure>0&&b.Fighting&&(b.Advancing||b.Returning));
        Check("both companions react under fire",pihuPose.UnderFire||tadoPose.UnderFire||pihuPose.UnderFireWeight>.01f||tadoPose.UnderFireWeight>.01f);
        Shot(report.mode+"-escort-under-fire");
        int injectedPressure=0;
        while(b.Fighting&&b.EscortPressure<3&&injectedPressure<3){b.EscortHit();injectedPressure++;yield return null;}
        report.observations.Add("observed actual escort shot, then injected "+injectedPressure+" EscortHit calls to reach three-pressure retry threshold");
        Check("three pressures fail only current second crossing",b.CurrentStage==Chapter3SchoolBattle.Stage.Down&&b.Step==1&&b.CheckpointStep==1&&b.EscortPressure==3);
        yield return RetryCheckpoint(f,knife,true);yield return ReadyThreat(f,knife,"pressure retry second");
        yield return Press(Key.E);Check("pressure retry resumes second crossing",b.Advancing);
        yield return EastSecondRoute(p);
        if(knife)
        {
            yield return Route(p,new[]{new Vector3(27.5f,0,57.2f),new Vector3(28.6f,0,58.5f)},"second east knife guard");
            yield return Cut(f,b.Enemies[1]);
        }
        else yield return Shoot(f,b.Enemies[1]);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,25,"second cleared crossing completes");
        Check("east completion needs second threat cleared and pair arrived",b.Step==2&&b.Enemies[1].Defeated&&Flat(s.pihu.transform.position,new Vector3(32,0,53))<.8f&&Flat(s.tado.transform.position,new Vector3(31.15f,0,52.1f))<.8f);
    }
    static IEnumerator ReadyThreat(Chapter3Flow f,bool knife,string label)
    {
        var b=f.school.Battle;var guard=b.Enemies[b.Step];
        yield return Wait(()=>guard.gameObject.activeInHierarchy,5,label+" guard activates");
        bool observedReveal=b.ThreatRevealing;Vector3 revealStart=guard.transform.position;
        if(observedReveal)Check(label+" guard walks out before attacking",!guard.AttackEnabled&&!guard.Telegraphing);
        yield return Wait(()=>!b.ThreatRevealing,10,label+" guard finishes reveal");
        Vector3 position=b.Step==0?new Vector3(29,0,knife?27.8f:33):new Vector3(30,0,knife?58.5f:61);
        Check(label+" guard reaches authored combat position",Flat(guard.transform.position,position)<.4f&&guard.AttackEnabled&&!guard.Defeated);
        if(observedReveal)Check(label+" reveal includes physical movement",Flat(revealStart,guard.transform.position)>.15f);
        report.observations.Add(label+" guard reveal observed="+observedReveal+" safe window="+guard.SafeWindowSeconds.ToString("F2"));
        if(label=="first"||label=="second")Shot(report.mode+"-"+label+"-threat");
    }
    static IEnumerator EastSecondRoute(Chapter3Player p)
    {
        yield return Route(p,new[]{new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27,0,53)},"second east crossing");
    }
    static IEnumerator Shoot(Chapter3Flow f,Chapter3Enemy enemy)
    {
        var w=f.combat.weapons;if(!w.Loaded){yield return Wait(()=>w.Ready,3,"reload ready");Check("reload starts",w.Reload());yield return Wait(()=>!w.Reloading,4,"single reload");}
        f.player.FocusOn(enemy.AimPoint);yield return new WaitForSeconds(.15f);
        if(!f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var hit)||hit.collider.GetComponentInParent<Chapter3Enemy>()!=enemy)
            throw new Exception("shot blocked by "+(hit.collider?hit.collider.name:"nothing")+" from "+f.player.transform.position+" to "+enemy.transform.position);
        Check("rifle attack accepted",w.Attack());yield return new WaitForSeconds(.55f);Check("real rifle shot defeats guard",enemy.Defeated);
    }
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
    static IEnumerator Work(Chapter3SchoolBattle b)
    {yield return Walk(Object.FindFirstObjectByType<Chapter3Player>(),b.Barricades[b.Step].position+Vector3.back*2.5f,"reach barricade handle");yield return new WaitForSeconds(.5f);yield return Hold(Key.E,2.2f);}
    static IEnumerator Route(Chapter3Player p,Vector3[] points,string label){foreach(var point in points)yield return Walk(p,point,label);}
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label)
    {
        float start=Time.realtimeSinceStartup;p.FocusOn(target+Vector3.up*1.25f);
        while(Flat(p.transform.position,target)>.3f)
        {
            if(label.Contains("guard")&&p.school&&p.school.Battle.Enemies.Any(e=>e&&!e.Defeated&&Flat(e.transform.position,target)<2&&Flat(e.transform.position,p.transform.position)<1.85f))yield break;
            if(label=="walk to rendezvous"&&!p.canMove&&Flat(p.transform.position,target)<4&&p.school&&p.school.Battle.CurrentStage>=Chapter3SchoolBattle.Stage.Regroup)yield break;
            if(Time.realtimeSinceStartup-start>35)throw new Exception("stuck "+label+" at "+p.transform.position+" target "+target);
            MoveToward(p,target,Mathf.Min(p.speed,2.8f),0);Progress(label+" "+p.transform.position);yield return null;
        }
    }
    static void MoveToward(Chapter3Player p,Vector3 target,float speed,float gap)
    {var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);if(d.magnitude>gap)p.Move(d.normalized*Mathf.Min(d.magnitude-gap,speed*Mathf.Min(Time.deltaTime,.04f)));}
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key){yield return Hold(key,.12f);}
    static IEnumerator Hold(Key key,float seconds){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(seconds);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.15f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string s)
    {
        var f=Object.FindFirstObjectByType<Chapter3Flow>();
        if(f&&f.school&&f.school.Battle&&f.school.Battle.Fighting&&f.school.Battle.Selected==Chapter3SchoolBattle.Mission.Pihu)
            foreach(var e in f.school.Battle.Enemies)if(e&&e.escortTarget&&e.Telegraphing)sawEscortTelegraph=true;
        if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;
        File.WriteAllText(Dir+"progress.txt",report.mode+" "+s+(f&&f.school.Battle?"\nstate "+f.school.Battle.CurrentStage+" step "+f.school.Battle.Step+" checkpoint "+f.school.Battle.CheckpointStep+" revealing "+f.school.Battle.ThreatRevealing+" arrived "+f.school.Battle.CrossingArrived+" blocked "+f.school.Battle.CompletionBlockedByThreat+" gate "+f.school.Battle.GateWorkProgress+" hp "+f.school.Battle.Health+" B "+f.school.bawan.transform.position+" P "+f.school.pihu.transform.position+" T "+f.school.tado.transform.position:""));
    }
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish(){if(report==null)return;report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
    static void Shot(string name)
    {
        var cam=Camera.main;if(!cam)return;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void ShotPose(string name,Chapter2Actor actor)
    {
        var cam=Camera.main;if(!cam)return;Vector3 previous=cam.transform.position;Quaternion rotation=cam.transform.rotation;
        var ui=Object.FindFirstObjectByType<Chapter3Flow>().ui.canvas;bool visible=ui.enabled;
        try{ui.enabled=false;cam.transform.position=actor.transform.position+new Vector3(-2.6f,1.4f,-3.1f);cam.transform.LookAt(actor.transform.position+Vector3.up*.75f);Shot(name);}
        finally{ui.enabled=visible;cam.transform.SetPositionAndRotation(previous,rotation);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh),"missingSkinnedMeshes="+t.SelectMany(x=>x.GetComponents<SkinnedMeshRenderer>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3MissionReworkProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3MissionReworkProbe.Run();}}




