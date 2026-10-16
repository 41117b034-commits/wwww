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
using Newtonsoft.Json.Linq;
using Object=UnityEngine.Object;

// Temporary validation harness. Archive outside Assets after the Editor checks.
[InitializeOnLoad]
public static partial class Chapter3Batch7Probe
{
    const string Dir="_CodexBackups/chapter3_batch7_20261016/",KeyName="Chapter3Batch7Probe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();}
    static Report report;static Chapter3Batch7ProbeDriver driver;static float began,progressAt;static bool sawEscortTelegraph;
    static InputSettings originalInput,testInput;
    static Chapter3Flow trackedFlow;static bool trackTado,tadoLeft,tadoRight,tadoWrongSide,remoteTadoReacted,wrongEscortTarget;
    static Vector3 lastTado;static float lastTadoTime,tadoDistance,tadoMinLeaderGap;static int tadoJumps,tadoSamples;
    static readonly HashSet<Chapter3Ending.Stage> endingStages=new HashSet<Chapter3Ending.Stage>();
    static bool collapseMidShot,collapseEndShot;
    static bool endingTracked,tadoConfirmedInjury,tadoReturned;static Vector3 endingTadoLast;static float endingTadoTime,endingTadoTravel;static int endingTadoJumps;
    const string SavePending=KeyName+".savePending",SaveHad=KeyName+".saveHad",SaveRaw=KeyName+".saveRaw";
    [Serializable] class SaveBackup {public string key;public bool existed;public string raw;public string capturedUtc;}
    static Chapter3Batch7Probe()
    {
        EditorApplication.update+=Tick;Application.logMessageReceived+=Log;Directory.CreateDirectory(Dir);
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode){RestoreInput();RestoreSave();}};
        AssemblyReloadEvents.beforeAssemblyReload+=()=>{RestoreInput();RestoreSave();};EditorApplication.quitting+=()=>{RestoreInput();RestoreSave();};
        if(!EditorApplication.isPlayingOrWillChangePlaymode)RestoreSave();
        File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));
    }
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
                else if(cmd=="refresh")AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else if(cmd=="fit"){Chapter2GameViewScaleGuard.Restore();Audit();}
                else if(cmd=="main"){PrepareScenarioScene("test-main");Audit();}
                else if(cmd=="build")BuildPlayer();
                else if(cmd=="preflight")BuildPreflight();
                else if(cmd=="stop"){RestoreSave();SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){if(EditorApplication.isPlaying)throw new Exception("Stop Play before starting another scenario");BackupSave(cmd);PrepareScenarioScene(cmd);SessionState.SetString(KeyName,cmd);EditorApplication.isPlaying=true;}
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary batch seven verification").AddComponent<Chapter3Batch7ProbeDriver>();
    }
    public static IEnumerator Run()
    {
        Application.runInBackground=true;Object.DontDestroyOnLoad(driver.gameObject);
        originalInput=InputSystem.settings;testInput=Object.Instantiate(originalInput);
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=testInput;
        yield return null;report=new Report{mode=SessionState.GetString(KeyName,"")};began=Time.realtimeSinceStartup;
        report.observations.Add("Input System uses a temporary cloned settings object with background input enabled for driver events. Original project settings restored; not native keyboard evidence.");
        if(!SessionState.GetBool(SavePending,false))BackupSave(report.mode);
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
        if(mode.StartsWith("test-entry-")){yield return EntryScenario(mode);yield break;}
        if(mode=="test-dialogue"){yield return DialogueScenario(f);yield break;}
        if(mode=="test-lifecycle"){yield return LifecycleScenario(f);yield break;}
        if(mode=="test-audio"){yield return Chapter3Batch7AudioChecks.Run(f,Check,m=>report.observations.Add(m));yield break;}
        if(mode=="test-ending-lifecycle"){yield return Chapter3Batch7EndingLifecycle.Run(f,Check,Shot);yield break;}
        if(mode=="test-store"){StoreScenario();yield break;}
        Check("main chapter scene loaded",f&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity");
        if(mode=="test-pose"){yield return PoseOnly(f);yield break;}
        if(mode=="test-facts"){yield return FactsOnly(f);yield break;}
        if(mode=="test-live-llm"||mode=="test-cancel"){yield return ConversationOnly(f,mode=="test-cancel");yield break;}
        report.observations.Add("Full school mission and rescue route driven by CharacterController, Input System events and UI/public weapon callbacks; controlled damage/pressure and HoldFire isolate retry/completion gates. Not native keyboard, human, VR or standalone-build evidence.");
        bool pihu=mode.Contains("pihu"),knife=mode.Contains("knife");
        endingStages.Clear();endingTracked=tadoConfirmedInjury=tadoReturned=false;endingTadoTravel=0;endingTadoJumps=0;
        var expectedWeapon=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        yield return Chapter3Batch7Prelude.Run(f,knife,Check,Shot,Progress);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"natural whole-chapter school entry");
        var s=f.school;var p=f.player;var b=s.Battle;var ambient=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("sports day remains calm before mission",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!ambient.Panicking);
        yield return Walk(p,s.tado.transform.position+Vector3.back*3.7f,"outside Tado conversation range");yield return new WaitForSeconds(.4f);
        Check("outside three metres never starts Tado dialogue",Flat(p.transform.position,s.tado.transform.position)>3.3f&&b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!s.MetTado);
        yield return ApproachTado(f);
        Check("near Tado starts dialogue without E",b.CurrentStage==Chapter3SchoolBattle.Stage.Observing&&s.MetTado&&!Keyboard.current.eKey.isPressed&&!p.canMove);
        Check("Tado speaks immediately without sport viewing delay",f.ui.subtitlePanel.activeSelf&&f.ui.speaker.text==s.tado.DisplayName&&!string.IsNullOrWhiteSpace(f.ui.subtitle.text)&&s.tado.speaking&&!f.ui.objective.text.Contains("觀察運動會"));
        Check("automatic dialogue hides arrival arrows",s.ArrivalGuide.VisibleMarks==0);Shot(mode+"-automatic-Tado-dialogue");
        string openingLine=f.ui.subtitle.text;yield return new WaitForSeconds(.65f);
        Check("remaining near Tado keeps the same dialogue",Flat(p.transform.position,s.tado.transform.position)<3.15f&&b.CurrentStage==Chapter3SchoolBattle.Stage.Observing&&f.ui.subtitle.text==openingLine);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,15,"automatic dialogue reaches mission choice");
        yield return new WaitForSeconds(.65f);
        Check("repeated near frames never restart mission choice",b.CurrentStage==Chapter3SchoolBattle.Stage.Choice&&b.Menu.choice.activeSelf&&!Keyboard.current.eKey.isPressed);
        if(pihu)b.Menu.pihu.onClick.Invoke();else yield return Press(Key.Digit1);
        Check("outbreak follows chosen mission",b.CurrentStage==Chapter3SchoolBattle.Stage.Outbreak&&ambient.Panicking);
        yield return Wait(()=>b.Fighting,15,"mission starts");
        trackedFlow=f;tadoLeft=tadoRight=tadoWrongSide=remoteTadoReacted=wrongEscortTarget=false;tadoDistance=0;tadoMinLeaderGap=float.PositiveInfinity;tadoJumps=tadoSamples=0;ResumeTadoTracking();
        Check("fresh mission begins before first checkpoint",b.Step==0&&b.CheckpointStep==0&&b.Retries==0);
        Check("exclusive inventory follows selected weapon",f.CarriedWeapon==expectedWeapon&&f.combat.weapons.Selected==expectedWeapon&&!f.combat.weapons.Select(knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife));
        Check("both authored guards present",b.Enemies.Count==2);
        var future=b.Enemies[1];int futureHealth=future.Health;
        Check("second guard remains inactive before first checkpoint",!future.gameObject.activeInHierarchy&&!future.Defeated);
        future.Hit(100,p.transform.position);yield return null;
        Check("inactive second guard rejects premature damage",future.Health==futureHealth&&!future.Defeated&&future.Shots==0);
        if(pihu)yield return PihuMission(f,knife);else yield return BawanMission(f,knife);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,12,"mission completion unlocks regroup");
        CheckTadoRoute(pihu,"completed mission");
        if(pihu)Check("east guard targeting and reactions never leak to remote Tado",!wrongEscortTarget&&!remoteTadoReacted);
        Check("completed checkpoint and current threat permit regroup",b.Step==2&&b.CheckpointStep>=1&&b.Enemies[1].Defeated&&(!b.Enemies[0]||b.Enemies[0].Defeated||!b.Enemies[0].gameObject.activeInHierarchy));
        var exit=pihu?new[]{new Vector3(32,0,53),new Vector3(27.5f,0,54),new Vector3(27.5f,0,60),new Vector3(25,0,68),new Vector3(12,0,74),new Vector3(0,0,78)}:new[]{new Vector3(-27,0,54),new Vector3(-25,0,66),new Vector3(-12,0,74),new Vector3(0,0,78)};
        yield return Route(p,exit,"walk to rendezvous");yield return Wait(()=>b.Complete&&b.Rescue&&b.Rescue.Active,65,"natural rescue handoff");
        Check("both mission routes reunite three uninjured companions",new[]{s.bawan,s.pihu,s.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4&&a.FallProgress==0));
        Check("reworked mission naturally enters fifth batch",b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing&&b.Rescue.Selected==Chapter3Rescue.Decision.None&&!b.Menu.result.activeSelf&&f.CarriedWeapon==expectedWeapon);
        Check("Tado walks his route and reunion without teleporting",tadoSamples>100&&tadoDistance>35&&tadoJumps==0);
        report.observations.Add("Tado minimum other-leader horizontal gap="+tadoMinLeaderGap.ToString("F3")+"m during Fighting/Regroup; retry restoration excluded");
        Check("Tado stays at least one metre from the other leader throughout mission and regroup",!float.IsInfinity(tadoMinLeaderGap)&&tadoMinLeaderGap>=1f);
        report.observations.Add("Tado travel="+tadoDistance.ToString("F2")+"m samples="+tadoSamples+" jumps="+tadoJumps+" left="+tadoLeft+" right="+tadoRight);
        report.observations.Add("retries="+b.Retries+" mission health="+b.Health);Shot(mode+"-reunion");trackTado=false;
        yield return RescueScenario(f,mode,pihu,knife);
        yield return EndingScenario(f,mode);
        string completedSave=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey);
        yield return ReplayScenario(f,mode,completedSave);
        f=Object.FindFirstObjectByType<Chapter3Flow>();s=f.school;
        yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"school replay");b=s.Battle;
        Check("F8 resets mission checkpoint and branch",b.Selected==Chapter3SchoolBattle.Mission.None&&b.Step==0&&b.CheckpointStep==0&&b.Retries==0);
        Check("F8 clears threat and side gate work state",!b.ThreatRevealing&&!b.CrossingArrived&&!b.CompletionBlockedByThreat&&!b.GateWorking&&b.GateWorkProgress==0&&(!b.Rescue||!b.Rescue.Active));
        Check("F8 has no active ending or injury from completed run",!b.Rescue||(!b.Rescue.Active&&(!b.Rescue.Ending||!b.Rescue.Ending.Active)));
        Check("F8 preserves newly completed result",PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)==completedSave);
        Check("F8 resets automatic Tado meeting",!s.MetTado&&b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&Flat(s.tado.transform.position,new Vector3(-4.7f,0,-5))<.1f);
        yield return ApproachTado(f);
        Check("replay can automatically meet Tado again",s.MetTado&&b.CurrentStage==Chapter3SchoolBattle.Stage.Observing&&f.ui.speaker.text==s.tado.DisplayName&&!string.IsNullOrWhiteSpace(f.ui.subtitle.text));
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
        yield return Wait(()=>r.Ending&&r.Ending.Active,30,"natural reception starts sixth batch");r.Ending.PersistResult=true;
        Check("rescue branch reaches ending in same scene",SceneManager.sceneCount==1&&r.Selected==decision&&Flat(p.transform.position,r.ReceptionPosition)<5);
        Check(save?"rescued Pihu physically reaches reception":"left Pihu remains at injury site",save?Flat(s.pihu.transform.position,r.ReceptionPosition)<4:Flat(s.pihu.transform.position,r.InjuryPosition)<1);
        Check("completed branch does not accept repeat choice or retry",!r.Choose(decision)&&!r.Retry());Shot(mode+"-reception");
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
    static IEnumerator EndingScenario(Chapter3Flow f,string mode)
    {
        var s=f.school;var r=s.Battle.Rescue;var e=r.Ending;var conversation=e.Conversation;
        bool rescued=r.Selected==Chapter3Rescue.Decision.Rescue,persist=true;
        e.PersistResult=persist;
        Check("ending begins from reception without early save",e.Active&&!e.Saved&&OriginalSaveMatches());
        yield return Wait(()=>e.CurrentStage==Chapter3Ending.Stage.Conversation&&conversation.Active,130,"outcome and roll call reach conversation");
        ObserveFrame();
        Check("ending naturally presents outcome and roll call",endingStages.Contains(Chapter3Ending.Stage.Outcome)&&endingStages.Contains(Chapter3Ending.Stage.RollCall));
        Check("ending holds a valid pending result",Chapter3ResultStore.IsValid(e.Result)&&!e.Result.completed&&e.Result.mission==s.Battle.Selected&&e.Result.decision==r.Selected&&e.Result.weapon==f.CarriedWeapon);
        CheckKnowledge(e.Result,mode+" reception");
        Check("outcome matches explicit rescue choice",e.Result.pihuSurvived==rescued&&e.Result.squadSurvivors==(rescued?4:3)&&e.RollCallCompleted&&e.DeathConfirmed==!rescued);
        Check("outcome pose matches explicit choice",rescued?r.Pose.DeathWeight==0&&r.Pose.InjuryWeight>.5f:r.Pose.DeathWeight==1);
        Check("Pihu physically remains at the correct outcome location",rescued?Flat(s.pihu.transform.position,r.ReceptionPosition)<5:Flat(s.pihu.transform.position,r.InjuryPosition)<1);
        if(!rescued)
        {
            Check("Tado physically confirms the left companion then returns",tadoConfirmedInjury&&tadoReturned&&endingTadoTravel>30&&endingTadoJumps==0);
            report.observations.Add("Leave confirmation: Tado travel="+endingTadoTravel.ToString("F2")+"m injuryVisit="+tadoConfirmedInjury+" returned="+tadoReturned+" jumps="+endingTadoJumps);
        }
        Check("outcome and roll call do not save prematurely",!e.Saved&&OriginalSaveMatches());
        Shot(mode+"-ending-conversation");ShotPose(mode+"-final-Pihu-pose",s.pihu);
        conversation.replyTimeout=.01f;
        Check("conversation rejects empty text and early closing",!conversation.SubmitReply("  ")&&!conversation.SubmitReply("……")&&!conversation.ContinueToMona());
        yield return new WaitForSeconds(.2f);
        int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
        Check("first emotional reply starts",conversation.SubmitReply("我很害怕，心裡一直靜不下來。"));
        Check("busy conversation rejects overlapping reply",conversation.WaitingForReply&&!conversation.SubmitReply("我還想說話。")&&!conversation.ContinueToMona());
        yield return Wait(()=>!conversation.WaitingForReply,5,"forced fallback reply");
        RecordReply(conversation,"forced fallback");
        Check("short timeout yields one visible authored fallback",conversation.CompletedTurns==1&&conversation.FallbackReplies==1&&conversation.GeneratedReplies==0&&!string.IsNullOrEmpty(conversation.LastReplyError)&&!string.IsNullOrWhiteSpace(conversation.LastReply));
        Check("reply completion restores previous frame budget",Application.targetFrameRate==rate&&QualitySettings.vSyncCount==vsync);
        string question=mode=="test-bawan-rescue-rifle"?"小隊現在幾人？":mode=="test-bawan-leave-knife"?"比胡的結果是什麼？":mode=="test-pihu-rescue-knife"?"你親眼見到右邊的事嗎？":"莫那是不是一直都跟著我們？";
        Check("second fixed-fact question starts",conversation.SubmitReply(question));
        yield return Wait(()=>!conversation.WaitingForReply,5,"grounded branch reply");
        RecordReply(conversation,"fixed fact");
        Check("two replies expose deliberate Mona handoff",conversation.CompletedTurns==2&&conversation.FallbackReplies==1&&conversation.GroundedReplies==1&&f.ui.depart.gameObject.activeSelf&&!f.ui.input.gameObject.activeSelf&&!f.ui.send.gameObject.activeSelf);
        string reply=conversation.LastReply;
        bool grounded=mode=="test-bawan-rescue-rifle"?reply.Contains("四人")&&reply.Contains("莫那不算"):mode=="test-bawan-leave-knife"?reply.Contains("達多")&&reply.Contains("死")&&reply.Contains("告訴我"):mode=="test-pihu-rescue-knife"?reply.Contains("我與達多走左側")&&reply.Contains("聽你們說"):reply.Contains("沒有跟我們到學校")&&reply.Contains("報告");
        Check("fixed reply respects branch and character knowledge",grounded);
        Check("dialogue cannot mutate result or save",Chapter3ResultStore.IsValid(e.Result)&&!e.Result.completed&&!e.Saved&&OriginalSaveMatches());
        yield return Press(Key.Space);
        Check("Space does not bypass ending transcript",e.CurrentStage==Chapter3Ending.Stage.Conversation&&conversation.Active&&f.ui.depart.gameObject.activeSelf);
        Shot(mode+"-two-turn-transcript");f.ui.depart.onClick.Invoke();yield return null;
        Check("button starts Mona closing once",e.CurrentStage==Chapter3Ending.Stage.MonaClosing&&!conversation.Active&&!conversation.WaitingForReply);
        yield return Wait(()=>e.Mona&&e.Mona.gameObject.activeInHierarchy,8,"Mona arrives after the authored fade");
        Check("Mona closing still precedes result save",!e.Saved&&OriginalSaveMatches());yield return Wait(()=>f.ui.fade.color.a<.05f&&e.Mona.speaking,15,"Mona visible and speaking");Shot(mode+"-Mona-closing");
        yield return Wait(()=>e.CurrentStage==Chapter3Ending.Stage.Complete,60,"Mona closing and chapter completion");
        ObserveFrame();
        Check("natural ending reaches complete after Mona",endingStages.Contains(Chapter3Ending.Stage.Conversation)&&endingStages.Contains(Chapter3Ending.Stage.MonaClosing)&&endingStages.Contains(Chapter3Ending.Stage.Complete)&&!conversation.Active);
        if(persist)
        {
            var saved=Chapter3ResultStore.Load();
            Check("normal controller persists only its completed authored result",e.Saved&&saved!=null&&saved.completed&&saved.mission==e.Result.mission&&saved.decision==e.Result.decision&&saved.weapon==e.Result.weapon&&saved.pihuSurvived==e.Result.pihuSurvived);
            CheckKnowledge(saved,"controller saved result");
            Check("completed result is durably readable",PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey));
        }
        else Check("disabled persistence leaves player result intact",!e.Saved&&OriginalSaveMatches());
        Shot(mode+"-chapter-complete");
    }
    static IEnumerator PoseOnly(Chapter3Flow f)
    {
        report.observations.Add("Isolated pose rendering; not a mission or ending-flow run.");
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.Space);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"pose school entry");
        var actor=f.school.pihu;actor.transform.position=new Vector3(-3,.03f,92);actor.Face(actor.transform.position+Vector3.back);
        var pose=actor.GetComponent<Chapter3InjuryPose>();if(!pose)pose=actor.gameObject.AddComponent<Chapter3InjuryPose>();pose.Configure(actor);
        pose.SetInjured(1);yield return new WaitForSeconds(2);Check("pose bones prepared",pose.BonesReady);
        foreach(float weight in new[]{0f,.25f,.5f,.75f,1f})
        {
            pose.SetDying(weight);yield return new WaitForSeconds(.3f);ShotPose("test-pose-weight-"+weight.ToString("F2"),actor);
            f.player.Warp(new Vector3(1.3f,.06f,90.1f),actor.transform.position+Vector3.up*.55f);Shot("test-pose-camera-"+weight.ToString("F2"));
        }
        var bones=actor.Rig.animator.GetComponentsInChildren<Transform>();var positions=bones.Select(x=>x.position).ToArray();var rotations=bones.Select(x=>x.rotation).ToArray();
        yield return new WaitForSeconds(2);
        Check("settled body is still without breathing",bones.Select((x,i)=>Vector3.Distance(x.position,positions[i])<.001f&&Quaternion.Angle(x.rotation,rotations[i])<.1f).All(x=>x));
        Check("settled death suppresses speech",pose.DeathWeight==1&&!actor.speaking);
        report.observations.Add("head="+actor.Rig.Head.position+" hips="+actor.Rig.Hips.position+" ground="+actor.GroundHeight+" height="+actor.Rig.Height);
        pose.ResetPose();yield return new WaitForSeconds(.4f);Check("pose reset clears all three layers",pose.DeathWeight==0&&pose.InjuryWeight==0&&pose.SupportWeight==0);ShotPose("test-pose-reset",actor);
        pose.SetInjured(1);yield return new WaitForSeconds(.4f);ShotPose("test-pose-injury-restored",actor);
        Check("pose-only test preserves save",OriginalSaveMatches());
    }
    static IEnumerator FactsOnly(Chapter3Flow f)
    {
        report.observations.Add("Isolated fixed-fact response cases using canonical records; not model generation or full flow.");
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.Space);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"facts school entry");
        var conversation=f.school.gameObject.AddComponent<Chapter3EndingConversation>();
        foreach(var decision in new[]{Chapter3Rescue.Decision.Rescue,Chapter3Rescue.Decision.Leave})
        foreach(var question in new[]{"達多還活著嗎？","巴萬受傷了嗎？","莫那知道比胡的情況嗎？","巴萬，你知道比胡的結果嗎？"})
        {
            var record=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Pihu,decision,Chapter3Weapons.Weapon.Rifle);
            conversation.Begin(f.school,record,()=>{});yield return new WaitForSeconds(.15f);
            Check("fact accepted "+question+decision,conversation.SubmitReply(question));yield return Wait(()=>!conversation.WaitingForReply,3,"fixed reply");
            string answer=conversation.LastReply;report.observations.Add(question+" "+decision+" => "+answer);
            bool correct=question.StartsWith("達多")?answer.Contains("達多")&&answer.Contains("活"):
                question.StartsWith("巴萬受")?answer.Contains("我")&&answer.Contains("傷")&&!answer.Contains("死"):
                question.StartsWith("莫那")?answer.Contains("報告")&&(answer.Contains("尚未")||answer.Contains("還沒")):
                answer.Contains("比胡")&&(decision==Chapter3Rescue.Decision.Rescue?answer.Contains("活"):answer.Contains("死"));
            Check("role and timing grounded "+question+decision,correct&&conversation.GroundedReplies==1&&conversation.GeneratedReplies==0&&OriginalSaveMatches());conversation.Cancel();
        }
    }
    static IEnumerator ConversationOnly(Chapter3Flow f,bool cancel)
    {
        report.observations.Add("Isolated ending-conversation component test with a canonical pending record. School mission, rescue, roll call and Mona sequence are not replayed in this mode.");
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.Space);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"isolated conversation school entry");
        var s=f.school;var conversation=s.gameObject.AddComponent<Chapter3EndingConversation>();
        report.observations.Add("Conversation is independently attached to school.gameObject. F8 cancellation exercises school-region OnDisable, not an interrupted full Chapter3Ending sequence.");
        Check("isolated conversation starts before any ending",conversation&&!conversation.Active&&(!s.Battle.Rescue||!s.Battle.Rescue.Ending||!s.Battle.Rescue.Ending.Active));
        var record=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Pihu,Chapter3Rescue.Decision.Leave,Chapter3Weapons.Weapon.Rifle);
        int callback=0;conversation.replyTimeout=15;
        if(!cancel)
        {
            float serviceStart=Time.realtimeSinceStartup;conversation.PrepareService();
            yield return Wait(()=>conversation.ServiceReady,65,"existing local service prepares before isolated conversation");
            report.observations.Add("Service preparation before input: "+(Time.realtimeSinceStartup-serviceStart).ToString("F2")+"s; normal ending starts preparation during outcome/roll call.");
        }
        conversation.Begin(s,record,()=>callback++);
        yield return new WaitForSeconds(.25f);
        Check("standalone conversation activates safe input UI",conversation.Active&&f.ui.chat.activeSelf&&!f.player.canMove&&(!s.Battle.Rescue||!s.Battle.Rescue.Ending||!s.Battle.Rescue.Ending.Active));
        int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
        Check("nonfixed emotional prompt accepted",conversation.SubmitReply("我很害怕，心裡一直靜不下來。"));
        if(cancel)
        {
            yield return Wait(()=>conversation.WaitingForReply&&Application.targetFrameRate==20,3,"live pending request lowers desktop frame budget");
            Check("cancel test has a pending nonfixed request",!f.player.IsVR&&conversation.WaitingForReply&&conversation.CompletedTurns==0&&QualitySettings.vSyncCount==0);
            Shot("test-cancel-pending");yield return Press(Key.F8);
            yield return Wait(()=>s.Exploring,12,"F8 cancels pending ending request");
            Check("F8 cancels pending request and restores controls and frame budget",!conversation.Active&&!conversation.WaitingForReply&&f.player.canMove&&!f.ui.chat.activeSelf&&Application.targetFrameRate==rate&&QualitySettings.vSyncCount==vsync&&callback==0);
            yield return new WaitForSecondsRealtime(16);
            Check("late reply cannot revive cancelled UI or advance ending",!conversation.Active&&!conversation.WaitingForReply&&conversation.CompletedTurns==0&&!f.ui.chat.activeSelf&&callback==0&&Application.targetFrameRate==rate&&QualitySettings.vSyncCount==vsync&&(!s.Battle.Rescue||!s.Battle.Rescue.Ending||!s.Battle.Rescue.Ending.Active));
            Check("cancelled conversation never saves",OriginalSaveMatches());Shot("test-cancel-restored");yield break;
        }
        yield return Wait(()=>!conversation.WaitingForReply,19,"first actual model reply");RecordReply(conversation,"live turn one");
        Check("second nonfixed emotional prompt accepted",conversation.SubmitReply("我想先緩一口氣，把這些感受慢慢說清楚。"));
        yield return Wait(()=>!conversation.WaitingForReply,19,"second actual model reply");RecordReply(conversation,"live turn two");
        Check("live test completes two nonfixed replies",conversation.CompletedTurns==2&&conversation.GroundedReplies==0&&conversation.GeneratedReplies+conversation.FallbackReplies==2);
        Check("live requests restore frame budget and preserve result",Application.targetFrameRate==rate&&QualitySettings.vSyncCount==vsync&&OriginalSaveMatches()&&!record.completed);
        Shot("test-live-llm-transcript");
        bool generated=conversation.GeneratedReplies>=1;
        yield return Press(Key.Space);Check("live transcript waits for button",conversation.Active&&callback==0);
        f.ui.depart.onClick.Invoke();yield return null;
        Check("live conversation button invokes callback exactly once",!conversation.Active&&callback==1&&!conversation.ContinueToMona()&&OriginalSaveMatches());
        Check("at least one real model response passes grounding filter",generated);
    }
    static void RecordReply(Chapter3EndingConversation conversation,string label)
    {
        report.observations.Add(label+": turns="+conversation.CompletedTurns+" generated="+conversation.GeneratedReplies+" fallback="+conversation.FallbackReplies+" grounded="+conversation.GroundedReplies+" seconds="+conversation.LastReplySeconds.ToString("F2")+" error="+(conversation.LastReplyError??"none")+" reply="+conversation.LastReply);
    }
    static void CheckKnowledge(Chapter3ResultStore.Record record,string label)
    {
        bool rescued=record.decision==Chapter3Rescue.Decision.Rescue;
        var actors=record.actors;var direct=Chapter3ResultStore.KnowledgeSource.Direct;var reported=Chapter3ResultStore.KnowledgeSource.Reported;var none=Chapter3ResultStore.KnowledgeSource.None;
        var b=actors[0];var p=actors[1];var t=actors[2];var m=actors[3];
        Check(label+" west and east knowledge stays with actual participants",b.actorId=="bawan"&&b.schoolSide=="west"&&b.westSchool==direct&&b.eastSchool==reported&&p.actorId=="pihu"&&p.schoolSide=="east"&&p.westSchool==reported&&p.eastSchool==direct);
        bool tadoWest=record.mission==Chapter3SchoolBattle.Mission.Pihu;
        Check(label+" Tado remembers the player's opposite mission side",t.actorId=="tado"&&t.schoolSide==(tadoWest?"west":"east")&&t.westSchool==(tadoWest?direct:reported)&&t.eastSchool==(tadoWest?reported:direct)&&t.outcome==direct);
        Check(label+" original squad and outcome are authoritative",b.originalSquad&&p.originalSquad&&t.originalSquad&&b.alive&&t.alive&&p.alive==rescued&&record.pihuSurvived==rescued&&record.squadSurvivors==(rescued?4:3)&&b.injury==direct&&p.injury==direct&&t.injury==direct&&b.outcome==(rescued?direct:reported)&&p.outcome==(rescued?direct:none));
        Check(label+" Mona is alive outside original squad and learns by report",m.actorId=="mona"&&m.alive&&!m.originalSquad&&m.schoolSide=="none"&&m.westSchool==reported&&m.eastSchool==reported&&m.injury==reported&&m.outcome==reported);
        if(!rescued)Check(label+" leave report distinguishes direct confirmation from hearsay",Chapter3ResultStore.KnowledgeSummary(record,"tado").Contains("親眼確認死亡")&&Chapter3ResultStore.KnowledgeSummary(record,"bawan").Contains("沒有目擊死亡瞬間"));
    }
    static void StoreScenario()
    {
        report.observations.Add("Isolated PlayerPrefs round-trip/validation test. Original SaveKey is backed up to evidence and restored in finally; this mode does not play the chapter.");
        try
        {
            PlayerPrefs.DeleteKey(Chapter3ResultStore.SaveKey);Check("missing result loads as no completion",Chapter3ResultStore.Load()==null);
            foreach(var mission in new[]{Chapter3SchoolBattle.Mission.Bawan,Chapter3SchoolBattle.Mission.Pihu})
            foreach(var decision in new[]{Chapter3Rescue.Decision.Rescue,Chapter3Rescue.Decision.Leave})
            {
                var weapon=(mission==Chapter3SchoolBattle.Mission.Bawan)==(decision==Chapter3Rescue.Decision.Rescue)?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife;
                var pending=Chapter3ResultStore.Build(mission,decision,weapon);string label=mission+" "+decision;
                Check(label+" canonical build is pending and valid",pending!=null&&!pending.completed&&Chapter3ResultStore.IsValid(pending)&&!Chapter3ResultStore.IsValid(pending,true));
                CheckKnowledge(pending,label);
                Check(label+" saves completed result",Chapter3ResultStore.SaveCompleted(pending));
                var loaded=Chapter3ResultStore.Load();
                Check(label+" round-trip preserves authoritative choices",loaded!=null&&loaded.completed&&loaded.mission==mission&&loaded.decision==decision&&loaded.weapon==weapon&&loaded.pihuSurvived==pending.pihuSurvived&&loaded.squadSurvivors==pending.squadSurvivors);
                Check(label+" saving never mutates caller pending snapshot",!pending.completed&&loaded!=pending&&loaded.actors!=pending.actors);
            }
            Check("invalid choices never create a result",Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.None,Chapter3Rescue.Decision.Rescue,Chapter3Weapons.Weapon.Knife)==null&&Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Bawan,Chapter3Rescue.Decision.None,Chapter3Weapons.Weapon.Knife)==null);
            var canonical=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Pihu,Chapter3Rescue.Decision.Leave,Chapter3Weapons.Weapon.Knife);
            Check("canonical corruption fixture saves",Chapter3ResultStore.SaveCompleted(canonical));
            string valid=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey);
            foreach(var kind in new[]{"Mona direct witness","wrong Tado side","dead Pihu knows outcome","wrong survivor count"})
            {
                var bad=Chapter3ResultStore.Build(canonical.mission,canonical.decision,canonical.weapon);
                if(kind=="Mona direct witness")bad.actors[3].injury=Chapter3ResultStore.KnowledgeSource.Direct;
                else if(kind=="wrong Tado side")bad.actors[2].schoolSide="east";
                else if(kind=="dead Pihu knows outcome")bad.actors[1].outcome=Chapter3ResultStore.KnowledgeSource.Direct;
                else bad.squadSurvivors=4;
                Check(kind+" rejected without replacing prior save",!Chapter3ResultStore.IsValid(bad)&&!Chapter3ResultStore.SaveCompleted(bad)&&PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)==valid);
            }
            var missing=JObject.Parse(valid);missing.Remove("weapon");
            var version=JObject.Parse(valid);version["version"]=99;
            var extra=JObject.Parse(valid);extra["playerInput"]="must never be persisted";
            var nullActors=JObject.Parse(valid);nullActors["actors"]=JValue.CreateNull();
            var corrupt=new Dictionary<string,string>{{"malformed","{broken"},{"unfinished",JsonUtility.ToJson(canonical)},{"missing weapon",missing.ToString()},{"duplicate version","{\"version\":1,"+valid.Substring(1)},{"unknown version",version.ToString()},{"extra free input",extra.ToString()},{"null actors",nullActors.ToString()},{"oversized",new string('x',8193)}};
            foreach(var pair in corrupt)
            {PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,pair.Value);Check(pair.Key+" loads as no valid completion",Chapter3ResultStore.Load()==null);}
            PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,JObject.Parse(valid).ToString());
            Check("valid pretty JSON still loads",Chapter3ResultStore.Load()!=null);
        }
        finally{RestoreSave();}
        Check("store test restores exact original key and content",OriginalSaveMatches());
    }
    static void BackupSave(string mode)
    {
        if(SessionState.GetBool(SavePending,false))return;
        bool had=PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey);string raw=had?PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,""):"";
        var backup=new SaveBackup{key=Chapter3ResultStore.SaveKey,existed=had,raw=raw,capturedUtc=DateTime.UtcNow.ToString("O")};
        SessionState.SetBool(KeyName+".secondHad",PlayerPrefs.HasKey(SecondKey));SessionState.SetString(KeyName+".secondRaw",PlayerPrefs.GetString(SecondKey,""));
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"save-backup-"+mode+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json",JsonUtility.ToJson(backup,true));
        SessionState.SetBool(SaveHad,had);SessionState.SetString(SaveRaw,raw);SessionState.SetBool(SavePending,true);
    }
    static bool OriginalSaveMatches()
    {
        bool had=SessionState.GetBool(SaveHad,false);
        return PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey)==had&&(!had||PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"")==SessionState.GetString(SaveRaw,""));
    }
    static void RestoreSave()
    {
        if(!SessionState.GetBool(SavePending,false))return;
        if(SessionState.GetBool(SaveHad,false))PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,SessionState.GetString(SaveRaw,""));else PlayerPrefs.DeleteKey(Chapter3ResultStore.SaveKey);
        if(SessionState.GetBool(KeyName+".secondHad",false))PlayerPrefs.SetString(SecondKey,SessionState.GetString(KeyName+".secondRaw",""));else PlayerPrefs.DeleteKey(SecondKey);
        PlayerPrefs.Save();if(!OriginalSaveMatches())throw new Exception("original chapter result was not restored");
        bool secondHad=SessionState.GetBool(KeyName+".secondHad",false);
        if(PlayerPrefs.HasKey(SecondKey)!=secondHad||(secondHad&&PlayerPrefs.GetString(SecondKey)!=SessionState.GetString(KeyName+".secondRaw","")))throw new Exception("original second chapter result was not restored");
        SessionState.SetBool(SavePending,false);
    }
    static IEnumerator BawanMission(Chapter3Flow f,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;
        yield return Route(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-27,0,12.9f)},"first west barricade");
        CheckTadoRoute(false,"west first barricade");
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
        trackTado=false;yield return Press(Key.R);yield return null;ResumeTadoTracking();
        var weapon=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        Check("retry keeps second-stage checkpoint and restores health",b.Fighting&&b.Step==1&&b.CheckpointStep==1&&b.Health==100&&b.Retries==retries+1&&b.EscortPressure==0);
        Check("retry keeps task and exclusive carried weapon",b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&f.CarriedWeapon==weapon&&f.combat.weapons.Selected==weapon&&!f.combat.weapons.Select(knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife));
        Check("retry never replays first guard",b.Enemies.Count==2&&(!b.Enemies[0]||b.Enemies[0].Defeated||!b.Enemies[0].gameObject.activeInHierarchy)&&!b.Enemies[1].Defeated);
        Check("retry retains Tado with the opposite route",pihu?f.school.tado.transform.position.x<8:f.school.tado.transform.position.x>-8);
        Shot(report.mode+"-checkpoint-"+b.Retries);
    }
    static IEnumerator PihuMission(Chapter3Flow f,bool knife)
    {
        var p=f.player;var s=f.school;var b=s.Battle;
        yield return Route(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,7)},"reach first east cover");
        yield return Wait(()=>b.AtCrossing,25,"Pihu reaches first cover while Tado goes west");
        CheckTadoRoute(true,"east first cover");
        yield return ReadyThreat(f,knife,"first");
        Check("first guard reveal cannot unlock future guard",b.Step==0&&!b.Enemies[1].gameObject.activeInHierarchy);
        yield return Press(Key.E);Check("E orders first crossing",b.Advancing&&!b.Returning);
        var pihuPose=s.pihu.GetComponent<Chapter3SchoolCompanionPose>();var tadoPose=s.tado.GetComponent<Chapter3SchoolCompanionPose>();
        Check("Pihu alone gives forward command response",pihuPose&&pihuPose.BonesReady&&pihuPose.ForwardSignalWeight>.01f&&TadoPoseClear(tadoPose));
        Check("crossing guard targets Pihu only",b.Enemies[0].escortTarget==s.pihu.transform);
        ShotPose(report.mode+"-forward-signal",s.pihu);
        yield return new WaitForSeconds(.4f);yield return Press(Key.E);
        Check("second E recalls advancing Pihu",b.Returning&&!b.Advancing);
        yield return Wait(()=>!b.Returning,12,"Pihu returns to first safe cover");
        Check("recall keeps first stage and checkpoint uncompleted",b.Step==0&&b.CheckpointStep==0&&!b.CrossingArrived);
        if(knife)
        {
            yield return Route(p,new[]{new Vector3(27.5f,0,14),new Vector3(27.5f,0,24),new Vector3(27.5f,0,27.4f)},"first east knife guard");
            yield return Cut(f,b.Enemies[0]);yield return Walk(p,new Vector3(27.5f,0,22),"return to first command range");
        }
        else{yield return Walk(p,new Vector3(27.5f,0,12),"first east firing lane");yield return Shoot(f,b.Enemies[0]);}
        Check("defeated guard alone does not advance first stage",b.Step==0&&b.CheckpointStep==0&&!b.CrossingArrived);
        yield return Press(Key.E);Check("safe crossing accepts new command",b.Advancing);
        yield return Wait(()=>b.Step==1,25,"Pihu finishes first crossing without remote Tado");
        Check("Pihu alone saves checkpoint one while Tado remains far west",b.CheckpointStep==1&&b.Enemies[0].Defeated&&Flat(s.pihu.transform.position,new Vector3(30,0,23))<.5f&&Flat(s.tado.transform.position,s.pihu.transform.position)>15&&s.tado.transform.position.x<8);
        yield return Wait(()=>pihuPose.SafeReportWeight>.01f,3,"Pihu safe report reaches rendered frame");
        Check("safe crossing report excludes remote Tado",pihuPose.SafeReportWeight>.01f&&TadoPoseClear(tadoPose));
        ShotPose(report.mode+"-safe-report",s.pihu);
        yield return ReadyThreat(f,knife,"second");
        // Controlled predicate isolation: Pihu may reach the endpoint
        // while this still-living guard is prevented from firing for this pass.
        b.Enemies[1].HoldFire(60);report.observations.Add("second crossing completion predicate isolated with guard.HoldFire(60); no native keyboard claim");
        yield return Walk(p,new Vector3(27.5f,0,22.5f),"second command range");
        yield return Press(Key.E);Check("second living threat still allows forward order",b.Advancing&&b.Step==1&&!b.Enemies[1].Defeated);
        yield return EastSecondRoute(p);
        yield return Wait(()=>b.CrossingArrived,25,"Pihu arrives before second guard is defeated");
        Check("Pihu at endpoint cannot bypass living second guard",b.Fighting&&b.Step==1&&b.CheckpointStep==1&&b.CompletionBlockedByThreat&&!b.Enemies[1].Defeated&&Flat(s.pihu.transform.position,new Vector3(32,0,53))<.5f&&Flat(s.tado.transform.position,s.pihu.transform.position)>15&&s.tado.transform.position.x<8);
        Check("remote Tado is not required at either east checkpoint",TadoPoseClear(tadoPose)&&!wrongEscortTarget);
        Shot(report.mode+"-crossing-awaits-guard");
        b.Damage(100,p.transform.position);Check("east second-stage death enters retry",b.CurrentStage==Chapter3SchoolBattle.Stage.Down);
        report.observations.Add("second-stage death injected with Damage(100) to isolate checkpoint restoration");
        yield return RetryCheckpoint(f,knife,true);
        Check("east retry restores Pihu safe cover and Tado west checkpoint",Flat(p.transform.position,new Vector3(27.5f,0,22.5f))<.6f&&Flat(s.pihu.transform.position,new Vector3(30,0,23))<.5f&&Flat(s.tado.transform.position,s.pihu.transform.position)>15&&s.tado.transform.position.x<8&&b.AtCrossing&&!b.CrossingArrived);
        yield return ReadyThreat(f,knife,"retry second");
        var liveGuard=b.Enemies[1];int priorShots=liveGuard.Shots;sawEscortTelegraph=false;
        yield return Press(Key.E);Check("retry permits same second crossing",b.Advancing);
        yield return Wait(()=>b.EscortPressure>0,28,"real second guard shot pressures escort");
        Check("real enemy telegraph and shot pressure Pihu only",sawEscortTelegraph&&liveGuard.Shots>priorShots&&liveGuard.escortTarget==s.pihu.transform&&b.EscortPressure>0&&b.Fighting&&(b.Advancing||b.Returning));
        Check("only Pihu reacts to east gunfire",(pihuPose.UnderFire||pihuPose.UnderFireWeight>.01f)&&TadoPoseClear(tadoPose));
        Shot(report.mode+"-escort-under-fire");
        ShotPose(report.mode+"-duck-pose",s.pihu);
        int injectedPressure=0;
        while(b.Fighting&&b.EscortPressure<3&&injectedPressure<3){b.EscortHit();injectedPressure++;yield return null;}
        report.observations.Add("observed actual escort shot, then injected "+injectedPressure+" EscortHit calls to reach three-pressure retry threshold");
        Check("three pressures fail only current second crossing",b.CurrentStage==Chapter3SchoolBattle.Stage.Down&&b.Step==1&&b.CheckpointStep==1&&b.EscortPressure==3);
        yield return RetryCheckpoint(f,knife,true);yield return ReadyThreat(f,knife,"pressure retry second");
        yield return Press(Key.E);Check("pressure retry resumes second crossing",b.Advancing);
        yield return EastSecondRoute(p);
        yield return Wait(()=>b.CrossingArrived,25,"Pihu finishes second crossing without remote Tado");
        Check("Pihu arrives before final threat is cleared",b.Fighting&&b.Step==1&&!b.Enemies[1].Defeated&&Flat(s.pihu.transform.position,new Vector3(32,0,53))<.5f&&Flat(s.tado.transform.position,s.pihu.transform.position)>15);
        if(knife)
        {
            yield return Route(p,new[]{new Vector3(32,0,50),new Vector3(33,0,51.5f)},"second east knife guard");
            yield return Cut(f,b.Enemies[1]);
        }
        else {yield return Walk(p,new Vector3(27.5f,0,57.2f),"second east firing lane");yield return Shoot(f,b.Enemies[1]);}
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,25,"second cleared crossing completes");
        Check("east completion needs second threat cleared and Pihu arrived",b.Step==2&&b.CheckpointStep==2&&b.Enemies[1].Defeated);
    }
    static IEnumerator ReadyThreat(Chapter3Flow f,bool knife,string label)
    {
        var b=f.school.Battle;var guard=b.Enemies[b.Step];
        yield return Wait(()=>guard.gameObject.activeInHierarchy,5,label+" guard activates");
        bool observedReveal=b.ThreatRevealing;Vector3 revealStart=guard.transform.position;
        if(observedReveal)Check(label+" guard walks out before attacking",!guard.AttackEnabled&&!guard.Telegraphing);
        yield return Wait(()=>!b.ThreatRevealing,10,label+" guard finishes reveal");
        Vector3 position=b.Step==0?new Vector3(29,0,knife?27.8f:33):new Vector3(34.5f,0,knife?52:59);
        Check(label+" guard reaches authored combat position",Flat(guard.transform.position,position)<.4f&&guard.AttackEnabled&&!guard.Defeated);
        if(observedReveal)Check(label+" reveal includes physical movement",Flat(revealStart,guard.transform.position)>.15f);
        report.observations.Add(label+" guard reveal observed="+observedReveal+" safe window="+guard.SafeWindowSeconds.ToString("F2"));
        if(label=="first"||label=="second")Shot(report.mode+"-"+label+"-threat");
    }
    static IEnumerator EastSecondRoute(Chapter3Player p)
    {
        yield return Route(p,new[]{new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27,0,53)},"second east crossing");
    }
    static IEnumerator ApproachTado(Chapter3Flow f)
    {
        var s=f.school;var p=f.player;float start=Time.realtimeSinceStartup;
        p.FocusOn(s.tado.transform.position+Vector3.up*1.25f);
        while(s.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado)
        {
            if(Time.realtimeSinceStartup-start>15)throw new Exception("automatic Tado conversation did not start at "+p.transform.position);
            // Stop moving as soon as the runtime consumes proximity. Never
            // call MeetTado or inject E; the remaining path may be move-locked.
            if(Flat(p.transform.position,s.tado.transform.position)>2.85f)
                MoveToward(p,s.tado.transform.position,Mathf.Min(p.speed,2.8f),2.85f);
            Progress("approach Tado without interaction key");yield return null;
        }
    }
    static bool TadoPoseClear(Chapter3SchoolCompanionPose pose)
    {return !pose||(!pose.UnderFire&&pose.ForwardSignalWeight<=.01f&&pose.UnderFireWeight<=.01f&&pose.SafeReportWeight<=.01f);}
    static void ResumeTadoTracking()
    {
        if(!trackedFlow)return;
        lastTado=trackedFlow.school.tado.transform.position;lastTadoTime=Time.time;trackTado=true;
    }
    public static void ObserveFrame()
    {
        if(report==null||!trackedFlow||!trackedFlow.school||!trackedFlow.school.Battle)return;
        ObserveEndingFrame();if(!trackTado)return;
        var s=trackedFlow.school;var b=s.Battle;var position=s.tado.transform.position;
        float distance=Flat(position,lastTado),elapsed=Time.time-lastTadoTime;
        if(distance>Mathf.Max(.35f,elapsed*8))
        {tadoJumps++;report.observations.Add("unexpected Tado step "+lastTado+" -> "+position+" dt="+elapsed+" stage="+b.CurrentStage);}
        tadoDistance+=distance;tadoSamples++;lastTado=position;lastTadoTime=Time.time;
        if(b.Fighting||b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup)
        {
            var otherLeader=b.Selected==Chapter3SchoolBattle.Mission.Pihu?s.bawan:s.pihu;
            tadoMinLeaderGap=Mathf.Min(tadoMinLeaderGap,Flat(position,otherLeader.transform.position));
        }
        if(b.Fighting)
        {
            if(position.z>0&&position.x<-20)tadoLeft=true;
            if(position.z>0&&position.x>20)tadoRight=true;
            bool pihu=b.Selected==Chapter3SchoolBattle.Mission.Pihu;
            if(position.z>0&&(pihu?position.x>8:position.x<-8))tadoWrongSide=true;
            if(pihu)
            {
                if(!TadoPoseClear(s.tado.GetComponent<Chapter3SchoolCompanionPose>()))remoteTadoReacted=true;
                foreach(var e in b.Enemies)if(e&&e.gameObject.activeInHierarchy&&!e.Defeated&&e.escortTarget)
                {
                    if(e.escortTarget!=s.pihu.transform)wrongEscortTarget=true;
                    if(e.Telegraphing)sawEscortTelegraph=true;
                }
            }
        }
    }
    static void ObserveEndingFrame()
    {
        var s=trackedFlow.school;var r=s.Battle.Rescue;var e=r?r.Ending:null;
        if(!e||!e.Active)return;
        if(endingStages.Add(e.CurrentStage))
        {
            report.observations.Add("ending stage="+e.CurrentStage+" time="+(Time.realtimeSinceStartup-began).ToString("F2")+" Tado="+s.tado.transform.position+" Pihu="+s.pihu.transform.position+" death="+r.Pose.DeathWeight+" saved="+e.Saved);
            if(e.CurrentStage==Chapter3Ending.Stage.Outcome||e.CurrentStage==Chapter3Ending.Stage.RollCall)Shot(report.mode+"-stage-"+e.CurrentStage);
        }
        if(r.Selected!=Chapter3Rescue.Decision.Leave||e.CurrentStage!=Chapter3Ending.Stage.Outcome)return;
        if(!collapseMidShot&&r.Pose.DeathWeight>=.5f){collapseMidShot=true;Shot(report.mode+"-collapse-half");}
        if(!collapseEndShot&&r.Pose.DeathWeight>=1){collapseEndShot=true;Shot(report.mode+"-collapse-final");}
        Vector3 position=s.tado.transform.position;
        if(!endingTracked){endingTracked=true;endingTadoLast=position;endingTadoTime=Time.time;}
        float distance=Flat(position,endingTadoLast),elapsed=Time.time-endingTadoTime;
        if(distance>Mathf.Max(.4f,elapsed*8))
        {endingTadoJumps++;report.observations.Add("unexpected ending Tado step "+endingTadoLast+" -> "+position+" dt="+elapsed);}
        endingTadoTravel+=distance;endingTadoLast=position;endingTadoTime=Time.time;
        if(Flat(position,r.InjuryPosition)<2.2f)tadoConfirmedInjury=true;
        if(tadoConfirmedInjury&&Flat(position,r.ReceptionPosition)<3)tadoReturned=true;
    }
    static void CheckTadoRoute(bool playerEast,string label)
    {
        ObserveFrame();var s=trackedFlow.school;
        Check(label+" sends Tado along the opposite physical side",(playerEast?tadoLeft:tadoRight)&&!tadoWrongSide&&tadoJumps==0);
        var leader=playerEast?s.bawan:s.pihu;
        Check(label+" keeps Tado with the other leader",Flat(s.tado.transform.position,leader.transform.position)<8);
        report.observations.Add(label+" Tado="+s.tado.transform.position+" other leader="+leader.transform.position);
        ShotPose(report.mode+"-Tado-"+(playerEast?"west":"east")+"-"+label.Replace(' ','-'),s.tado);
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
        File.WriteAllText(Dir+"progress.txt",report.mode+" "+s+(f&&f.school.Battle?"\nstate "+f.school.Battle.CurrentStage+" step "+f.school.Battle.Step+" checkpoint "+f.school.Battle.CheckpointStep+" revealing "+f.school.Battle.ThreatRevealing+" arrived "+f.school.Battle.CrossingArrived+" blocked "+f.school.Battle.CompletionBlockedByThreat+" gate "+f.school.Battle.GateWorkProgress+" hp "+f.school.Battle.Health+" B "+f.school.bawan.transform.position+" P "+f.school.pihu.transform.position+" T "+f.school.tado.transform.position+(f.school.Battle.Rescue&&f.school.Battle.Rescue.Ending?"\nending "+f.school.Battle.Rescue.Ending.CurrentStage+" death "+f.school.Battle.Rescue.Pose.DeathWeight+" saved "+f.school.Battle.Rescue.Ending.Saved:""):""));
    }
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish()
    {
        if(report==null)return;
        try{RestoreSave();bool restored=OriginalSaveMatches();report.checks.Add("final cleanup restores original player result="+restored);if(!restored)throw new Exception("original result differs after final cleanup");}
        catch(Exception e){report.completed=false;report.errors.Add("Save restoration failed: "+e);}
        Time.timeScale=1;
        RestoreInput();
        report.elapsed=Time.realtimeSinceStartup-began;
        File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));
        SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;
    }
    static void RestoreInput()
    {
        if(originalInput)InputSystem.settings=originalInput;
        if(testInput)Object.DestroyImmediate(testInput);
        originalInput=testInput=null;
    }
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
        try{ui.enabled=false;cam.transform.position=actor.transform.position+new Vector3(-2.6f,1.4f,3.1f);cam.transform.LookAt(actor.transform.position+Vector3.up*.75f);Shot(name);}
        finally{ui.enabled=visible;cam.transform.SetPositionAndRotation(previous,rotation);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh),"missingSkinnedMeshes="+t.SelectMany(x=>x.GetComponents<SkinnedMeshRenderer>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3Batch7ProbeDriver:MonoBehaviour
{
    IEnumerator Start(){yield return Chapter3Batch7Probe.Run();}
    void LateUpdate(){Chapter3Batch7Probe.ObserveFrame();}
}





