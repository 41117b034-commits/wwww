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
public static class Chapter3Batch5Probe
{
    const string Dir="_CodexBackups/chapter3_batch5_20261014/",KeyName="Chapter3Batch5Probe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();}
    static Report report;static Chapter3Batch5ProbeDriver driver;static float began,progressAt;
    static Chapter3Batch5Probe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
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
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary fifth batch verification").AddComponent<Chapter3Batch5ProbeDriver>();
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
        Check("main chapter flow exists",f&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity");
        if(mode=="test-space"){yield return SpaceEntry(f);yield break;}
        bool pihu=mode.Contains("pihu"),knife=mode.Contains("knife");
        f.Carry(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle);
        yield return Press(Key.Space);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"Space enters sports day");
        Check("Space shortcut preserves selected inventory",f.CarriedWeapon==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle));
        yield return SchoolMission(f,pihu,knife);
        yield return RescueScenario(f,mode,pihu,knife);
    }
    static IEnumerator SpaceEntry(Chapter3Flow f)
    {
        yield return Press(Key.Space);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"initial Space shortcut");
        Check("Space enters calm sports day",f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!f.school.GetComponentInChildren<Chapter3SchoolAmbient>().Panicking);
        int changes=f.Transitions;yield return Press(Key.Space);Check("Space in school does not restart area",f.Transitions==changes);
        yield return Press(Key.F6);yield return Wait(()=>f.station.Opening.Active,12,"station opening chat");
        f.ui.input.text="問題 包含空白";f.ui.input.ActivateInputField();yield return null;
        changes=f.Transitions;yield return Press(Key.Space);
        Check("Space in active LLM input does not jump",f.Region=="Station"&&f.station.Opening.Active&&f.ui.chat.activeSelf&&f.Transitions==changes);
        Shot("space-chat-protected");
        yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"F8 alias");
        Check("F8 still reaches calm school",f.school.Battle.Selected==Chapter3SchoolBattle.Mission.None);
        yield return Press(Key.F7);yield return Wait(()=>f.Region=="Combat",8,"F7 combat");
        yield return Press(Key.Space);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"Space from combat");
        Check("combat transition clears old weapon service",!f.combat.SchoolEncounter&&!f.combat.weapons.gameObject.activeSelf);
        Check("single scene camera listener",SceneManager.sceneCount==1&&Camera.allCameras.Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1&&Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
        Shot("space-sports-entry");
    }
    static IEnumerator SchoolMission(Chapter3Flow f,bool pihu,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;var ambient=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("calm sports day before mission",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!ambient.Panicking);
        Check("mission cannot be chosen early",!b.Choose(Chapter3SchoolBattle.Mission.Bawan));
        yield return new WaitForSeconds(.4f);yield return Press(Key.Space);
        yield return Walk(p,new Vector3(-4.7f,0,-7.2f),"meet Tado");yield return Press(Key.E);
        Check("E begins watching sport",b.CurrentStage==Chapter3SchoolBattle.Stage.Observing);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,22,"mission choice");
        if(pihu)b.Menu.pihu.onClick.Invoke();else yield return Press(Key.Digit1);
        Check("outbreak follows choice",b.CurrentStage==Chapter3SchoolBattle.Stage.Outbreak&&ambient.Panicking);
        yield return Wait(()=>b.Fighting,15,"school combat");
        Check("selected weapon retained in mission",f.combat.weapons.Selected==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle)&&!f.combat.weapons.Select(knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife));
        if(knife)yield return KnifeMission(f,pihu);else yield return RifleMission(f,pihu);
        var exit=pihu?new[]{new Vector3(32,0,53),new Vector3(27.5f,0,54),new Vector3(27.5f,0,60),new Vector3(25,0,68),new Vector3(12,0,74),new Vector3(0,0,78)}:new[]{new Vector3(-27,0,54),new Vector3(-25,0,66),new Vector3(-12,0,74),new Vector3(0,0,78)};
        yield return Route(p,exit,"walk to rendezvous");
        yield return Wait(()=>b.Complete,65,"both squads reunite");
        Check("mission retained at reunion",b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan));
        Check("all three companions alive at reunion",new[]{s.bawan,s.pihu,s.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4&&a.FallProgress==0));
        Check("old reunion completion card removed",!b.Menu.result.activeSelf&&!f.combat.weapons.gameObject.activeSelf);
        report.observations.Add("mission health at reunion="+b.Health);Shot(report.mode+"-reunion");
    }
    static IEnumerator RifleMission(Chapter3Flow f,bool pihu)
    {
        var p=f.player;var s=f.school;var b=s.Battle;
        if(pihu)
        {
            yield return Route(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,7)},"east approach");
            yield return Wait(()=>b.AtCrossing,35,"first cover readiness");
            yield return Walk(p,new Vector3(27.5f,0,12),"peek first guard");yield return Shoot(f,b.Enemies[0]);
            yield return Press(Key.E);Check("first E crossing command accepted",b.Advancing);yield return Wait(()=>b.Step==1,35,"first crossing");
            yield return Route(p,new[]{new Vector3(27.5f,0,24),new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27,0,53)},"second cover position");
            yield return Shoot(f,b.Enemies[1]);
            yield return Route(p,new[]{new Vector3(32,0,43),new Vector3(29,0,37)},"return to command range");
            yield return Wait(()=>b.AtCrossing,35,"second cover readiness");yield return Press(Key.E);Check("second E crossing command accepted",b.Advancing);
            yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,35,"second crossing");
            Check("cover route completes two crossings",b.Step==2&&b.EscortPressure==0);
        }
        else
        {
            yield return Route(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-27,0,12.9f)},"west first barricade");
            yield return Shoot(f,b.Enemies[0]);yield return Hold(Key.E,2.1f);Check("first barricade opens by hold E",b.Step==1);yield return new WaitForSeconds(.7f);
            yield return Route(p,new[]{new Vector3(-27,0,24),new Vector3(-27,0,35),new Vector3(-27,0,43)},"west second barricade");
            yield return Shoot(f,b.Enemies[1]);yield return Hold(Key.E,2.1f);
            Check("breakthrough route reaches regroup",b.Step==2&&b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup);
        }
    }
    static IEnumerator KnifeMission(Chapter3Flow f,bool pihu)
    {
        var p=f.player;var b=f.school.Battle;
        if(!pihu)
        {
            yield return Route(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-25.7f,0,13)},"west guard one");
            yield return Cut(f,b.Enemies[0]);yield return Work(b);Check("knife opens first barricade",b.Step==1);
            yield return Route(p,new[]{new Vector3(-27,0,24),new Vector3(-27,0,35),new Vector3(-28,0,43)},"west guard two");
            yield return Cut(f,b.Enemies[1]);yield return Work(b);Check("knife opens second barricade",b.Step==2);
        }
        else
        {
            yield return Route(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,8),new Vector3(27.5f,0,14),new Vector3(27.5f,0,24),new Vector3(27.5f,0,27.4f)},"east guard one");
            yield return Cut(f,b.Enemies[0]);yield return Walk(p,new Vector3(27.5f,0,22),"return to first command range");
            yield return Wait(()=>b.AtCrossing,35,"first escort cover");yield return Press(Key.E);Check("knife first E crossing",b.Advancing);yield return Wait(()=>b.Step==1,35,"first escort arrival");
            yield return Route(p,new[]{new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27.5f,0,53),new Vector3(27.5f,0,57.2f),new Vector3(28.6f,0,58.5f)},"east guard two");
            yield return Cut(f,b.Enemies[1]);yield return Route(p,new[]{new Vector3(27.5f,0,53),new Vector3(32,0,43),new Vector3(29,0,37)},"return to escort command");
            yield return Wait(()=>b.AtCrossing,35,"second escort cover");yield return Press(Key.E);Check("knife second E crossing",b.Advancing);yield return Wait(()=>b.Step==2,35,"second escort arrival");
        }
        Check("both guards defeated by actual knife swings",b.Enemies.All(e=>e.Defeated)&&f.combat.weapons.Slashes>=4&&f.combat.weapons.Shots==0);
    }
    static IEnumerator RescueScenario(Chapter3Flow f,string mode,bool pihu,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;var r=b.Rescue;
        bool save=mode.Contains("rescue");
        Check("batch five starts naturally after reunion",r&&r.Active&&r.CurrentStage==Chapter3Rescue.Stage.Advancing);
        Check("rescue choice cannot be made before injury",r.Selected==Chapter3Rescue.Decision.None&&!r.Choose(Chapter3Rescue.Decision.Rescue)&&!r.WorkLift(true,10));
        Vector3 reunion=s.pihu.transform.position;float start=Time.realtimeSinceStartup;
        while(r.CurrentStage==Chapter3Rescue.Stage.Advancing)
        {
            MoveToward(p,s.bawan.transform.position,2.8f,1.8f);p.FocusOn(s.bawan.transform.position+Vector3.up*1.1f);
            if(Time.realtimeSinceStartup-start>60)throw new Exception("common advance stuck at player="+p.transform.position+" guide="+s.bawan.transform.position+" Pihu="+s.pihu.transform.position);
            Progress("common route after reunion");yield return null;
        }
        yield return Wait(()=>r.CurrentStage==Chapter3Rescue.Stage.Choice,20,"injury and rescue choice");
        Check("injury occurs after walking away from reunion",Flat(reunion,s.pihu.transform.position)>8);
        Check("Pihu injured at fixed forest turn",Flat(s.pihu.transform.position,r.InjuryPosition)<1&&r.Pose.BonesReady&&r.Pose.InjuryWeight==1&&r.Pose.SupportWeight==0);
        Check("choice waits for player",r.Selected==Chapter3Rescue.Decision.None&&!p.canMove);
        Shot(mode+"-injury-choice");ShotPose(mode+"-injured-pose",s.pihu);yield return new WaitForSeconds(3);
        Check("no hidden choice countdown",r.CurrentStage==Chapter3Rescue.Stage.Choice&&r.Selected==Chapter3Rescue.Decision.None);
        if(save)yield return Press(Key.Digit1);else r.ChoiceLeave.onClick.Invoke();
        yield return Wait(()=>r.CurrentStage==(save?Chapter3Rescue.Stage.AwaitingLift:Chapter3Rescue.Stage.Leaving),12,"committed rescue branch");
        var decision=save?Chapter3Rescue.Decision.Rescue:Chapter3Rescue.Decision.Leave;
        Check("choice committed once",r.Selected==decision&&!r.Choose(save?Chapter3Rescue.Decision.Leave:Chapter3Rescue.Decision.Rescue));
        Check("battle mission and carried weapon retained",b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&f.CarriedWeapon==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle));
        yield return ForestGuard(f,knife,!save&&!knife);
        if(save)
        {
            var weapon=f.combat.weapons;bool storedLoaded=weapon.Loaded;int storedReserve=weapon.Reserve,storedShots=weapon.Shots,storedSlashes=weapon.Slashes;
            yield return Walk(p,r.InjuryPosition+new Vector3(1.35f,0,-.3f),"approach injured Pihu");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return new WaitForSeconds(.65f);
            Check("holding E visibly advances lift",r.LiftProgress>.15f&&r.LiftProgress<.8f&&!r.Supporting);Shot(mode+"-lift-progress");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.2f);
            Check("releasing E resets incomplete lift",r.LiftProgress==0&&r.CurrentStage==Chapter3Rescue.Stage.AwaitingLift);
            yield return Hold(Key.E,2.35f);Check("held E raises Pihu into support",r.Supporting&&r.CurrentStage==Chapter3Rescue.Stage.Supporting&&r.Pose.SupportWeight==1);
            Check("supported movement slows player and holsters weapon",p.speed<=1.2f&&!f.combat.weapons.Attack());
            var initialP=p.transform.position;var initialPihu=s.pihu.transform.position;
            p.FocusOn(new Vector3(0,1.2f,98));yield return Hold(Key.W,.8f);
            Check("real W moves player slowly with Pihu",Flat(initialP,p.transform.position)>.15f&&Flat(initialP,p.transform.position)<1.4f&&Flat(initialPihu,s.pihu.transform.position)>.1f&&Flat(p.transform.position,s.pihu.transform.position)<2.2f);
            Check("supporting hand stays near player shoulder",Vector3.Distance(r.Pose.SupportHandPosition,p.view.transform.position-Vector3.up*.27f)<.85f);
            Check("supported Pihu sample avoids static scenery",Chapter3PathMotor.Clear(s.pihu.transform.position,s.pihu.transform.position));
            Shot(mode+"-supported");ShotPose(mode+"-supported-pose",s.pihu);yield return Press(Key.E);
            Check("E puts injured companion down",!r.Supporting&&r.CurrentStage==Chapter3Rescue.Stage.AwaitingLift&&r.Selected==Chapter3Rescue.Decision.Rescue);
            Check("put down restores weapon without refilling or resetting counters",weapon.gameObject.activeSelf&&weapon.Loaded==storedLoaded&&weapon.Reserve==storedReserve&&weapon.Shots==storedShots&&weapon.Slashes==storedSlashes);
            r.Damage(100,p.transform.position);yield return null;
            Check("rescue failure offers retry without changing choice",r.CurrentStage==Chapter3Rescue.Stage.Down&&r.Selected==Chapter3Rescue.Decision.Rescue);Shot(mode+"-retry");
            int retries=r.Retries;yield return Press(Key.R);yield return null;
            Check("retry retains rescue mission and exclusive inventory",r.CurrentStage==Chapter3Rescue.Stage.AwaitingLift&&r.Selected==Chapter3Rescue.Decision.Rescue&&r.Retries==retries+1&&r.Health==100&&r.LiftProgress==0&&!r.Supporting&&b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&f.CarriedWeapon==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle));
            Check("retry restores injury checkpoint",Flat(s.pihu.transform.position,r.InjuryPosition)<1&&Flat(p.transform.position,r.InjuryPosition)<5);
            yield return ForestGuard(f,knife);
            yield return Walk(p,r.InjuryPosition+new Vector3(1.35f,0,-.3f),"retry approach injured Pihu");yield return Hold(Key.E,2.35f);
            Check("retry can lift again",r.Supporting);
            yield return RescueRoute(f,true);
        }
        else
        {
            Check("leave branch cannot lift",!r.WorkLift(true,10)&&!r.Supporting);
            r.Damage(100,p.transform.position);yield return null;
            Check("leave failure keeps committed decision",r.CurrentStage==Chapter3Rescue.Stage.Down&&r.Selected==Chapter3Rescue.Decision.Leave);
            int retries=r.Retries;yield return Press(Key.R);yield return null;
            Check("leave retry retains mission and exclusive weapon",r.CurrentStage==Chapter3Rescue.Stage.Leaving&&r.Selected==Chapter3Rescue.Decision.Leave&&r.Retries==retries+1&&r.Health==100&&b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&f.CarriedWeapon==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle)&&f.combat.weapons.Selected==f.CarriedWeapon.Value);
            Check("leave retry restores injured Pihu and one guard",Flat(s.pihu.transform.position,r.InjuryPosition)<1&&b.Enemies.Count==1&&!b.Enemies[0].Defeated&&!r.WorkLift(true,10));
            yield return ForestGuard(f,knife);
            yield return RescueRoute(f,false);
        }
        yield return Wait(()=>r.CurrentStage==Chapter3Rescue.Stage.Complete,25,"forest reception handoff");
        Check("rescue branch reaches reception in same scene",SceneManager.sceneCount==1&&r.Selected==decision&&Flat(s.tado.transform.position,r.ReceptionPosition)<4&&Flat(p.transform.position,r.ReceptionPosition)<5);
        Check(save?"rescued Pihu physically reaches reception":"left Pihu remains at injury site",save?Flat(s.pihu.transform.position,r.ReceptionPosition)<4:Flat(s.pihu.transform.position,r.InjuryPosition)<1);
        Check("completed branch does not accept repeat choice or retry",!r.Choose(decision)&&!r.Retry());Shot(mode+"-reception");
        yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"F8 school replay after rescue");
        Check("F8 clears rescue decision and pose",s.Battle.Rescue.Selected==Chapter3Rescue.Decision.None&&!s.Battle.Rescue.Active&&s.Battle.Selected==Chapter3SchoolBattle.Mission.None&&s.pihu.FallProgress==0&&s.Battle.Rescue.Pose.InjuryWeight==0&&s.Battle.Rescue.Pose.SupportWeight==0);
        Check("replay restores normal player speed",Mathf.Abs(p.speed-2.8f)<.01f&&p.canMove&&p.canLook);
    }
    static IEnumerator ForestGuard(Chapter3Flow f,bool knife,bool receiveRealShot=false)
    {
        var r=f.school.Battle.Rescue;
        yield return Wait(()=>f.school.Battle.Enemies.Any(e=>e&&!e.Defeated),8,"forest guard");
        var e=f.school.Battle.Enemies.First(x=>x&&!x.Defeated);
        Check("forest guard uses rescue combat health",r.CombatActive&&f.combat.Fighting);
        if(receiveRealShot)
        {
            // Walk into the existing clear lane only when the starting bodies
            // or scenery block the guard's actual ray; do not inject damage.
            if(!f.combat.ClearLine(e.AimPoint,f.combat.playerTarget.position,e))
                yield return Walk(f.player,new Vector3(-.1f,0,95.6f),"stand in forest guard sight");
            int health=r.Health,shots=e.Shots;
            yield return Wait(()=>r.Health<health&&e.Shots>shots,14,"actual forest guard shot");
            Check("actual guard bullet removes twenty health",r.Health==health-20&&e.Shots==shots+1);
            f.player.FocusOn(e.AimPoint);Shot(report.mode+"-actual-guard-shot");
        }
        if(knife)
        {
            var approach=e.transform.position+Vector3.ProjectOnPlane(f.player.transform.position-e.transform.position,Vector3.up).normalized*1.4f;
            yield return Walk(f.player,approach,"rescue guard approach");yield return Cut(f,e);
        }
        else yield return Shoot(f,e);
        Check("forest guard defeated through carried weapon",e.Defeated);report.observations.Add("forest guard health="+r.Health+" shots="+e.Shots);
    }
    static IEnumerator RescueRoute(Chapter3Flow f,bool supporting)
    {
        var p=f.player;var r=f.school.Battle.Rescue;
        foreach(var point in new[]{new Vector3(0,0,98),new Vector3(8,0,103),r.ReceptionPosition})
        {
            float start=Time.realtimeSinceStartup;
            while(Flat(p.transform.position,point)>.4f)
            {
                if(r.CurrentStage==Chapter3Rescue.Stage.Handoff||r.CurrentStage==Chapter3Rescue.Stage.Complete)yield break;
                if(r.CurrentStage==Chapter3Rescue.Stage.Down)throw new Exception("player down on rescue route");
                if(Time.realtimeSinceStartup-start>40)throw new Exception("stuck rescue route at "+p.transform.position+" target "+point+" Pihu="+f.school.pihu.transform.position);
                MoveToward(p,point,p.speed,0);p.FocusOn(point+Vector3.up*1.15f);Progress((supporting?"supporting Pihu":"leaving injury site")+" toward "+point);yield return null;
            }
        }
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
            if(label.Contains("guard")&&p.school&&p.school.Battle.Enemies.Any(e=>!e.Defeated&&Flat(e.transform.position,target)<2&&Flat(e.transform.position,p.transform.position)<1.85f))yield break;
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
        if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;var f=Object.FindFirstObjectByType<Chapter3Flow>();
        File.WriteAllText(Dir+"progress.txt",report.mode+" "+s+(f&&f.school.Battle?"\nstate "+f.school.Battle.CurrentStage+" step "+f.school.Battle.Step+" hp "+f.school.Battle.Health+" B "+f.school.bawan.transform.position+" P "+f.school.pihu.transform.position+" T "+f.school.tado.transform.position:""));
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
public sealed class Chapter3Batch5ProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3Batch5Probe.Run();}}

