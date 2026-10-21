using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// TEMPORARY. Backup only until the owner installs it. Public gameplay actions,
// CharacterController travel and read-only reflection: never Warp or stage writes.
[InitializeOnLoad]
public static class Chapter3CompactWalkProbe
{
    const string Dir="_CodexBackups/chapter3_four_repairs_20261021/walk/", State="Chapter3CompactWalkProbe";
    static readonly string[] Modes={"test-walk-rifle","test-walk-knife","test-walk-shortcuts"};
    static readonly string[] Keys={"WusheEvent.Chapter2.Result",Chapter3ResultStore.SaveKey};
    static readonly string[] Protected={"Assets/Scenes/第一章新版警察.unity","Assets/Scenes/第二章.unity","Assets/Scenes/第三章.unity","ProjectSettings/EditorBuildSettings.asset"};
    [Serializable] sealed class Save {public Item[] items;}
    [Serializable] sealed class Item {public string key,raw;public bool existed;}
    [Serializable] sealed class Report {public string mode,startedUtc;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();public List<Frame> frameSamples=new List<Frame>();public List<AudioOutput> perSourceOutput=new List<AudioOutput>();}
    [Serializable] sealed class AudioOutput {public int frame,sourceId,timeSamples;public float time,peak,rms,volume;public bool reveal,finite;}
    [Serializable] sealed class Frame {public int frame,waypoint,audioTimeSamples;public float time,fade,groundY,playerGroundGap,leadGroundGap,rearGroundGap,audioVolume;public Vector3 player,lead,rear;public bool canMove,canLook,controller,waiting,audio,reveal,groundFound,leadGroundFound,rearGroundFound;public string region;}
    [Serializable] sealed class Inspection {public string utc,scene,capturePhase;public bool playing,dirty,compileFailed;public int frame,missingScripts;public string[] roots,components,audio,sources,heads;}
    static Report report;static Chapter3CompactWalkProbeDriver driver;static Chapter3Flow flow;static WalkAccess walk;
    static InputSettings originalInput,testInput;static Keyboard addedKeyboard;static Mouse addedMouse;
    static bool tracking,oldBackground;static float began,progressAt,sampleAt,maxFade,minPlayerY,maxPlayerY,travel;static int samples,jumps,locks,missingGround;
    static Vector3 previousPlayer,previousLead,previousRear;static float previousTime,audioAt=-1,revealAt=-1,handoffPositionError,handoffRotationError;static string phase,previousRegion;static bool handoffObserved,pendingAudioShot,pendingRevealShot;
    static readonly float[] outputBuffer=new float[1024];
    static float outputBegan=-1,outputAt=-1,outputPeak,outputRms;static int outputFrames,nonzeroOutputFrames;static bool outputDone,outputFinite;
    static Dictionary<string,string> hashes;

    static Chapter3CompactWalkProbe()
    {
        Directory.CreateDirectory(Dir);EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)Cleanup();if(s==PlayModeStateChange.EnteredEditMode)Audit();};
        AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;EditorApplication.quitting+=Cleanup;
        if(!EditorApplication.isPlayingOrWillChangePlaymode)RestoreSave();
        File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string path=Dir+"command.txt";
        if(File.Exists(path))
        {
            string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try
            {
                if(command=="audit")Audit();
                else if(command=="inspect")Inspect();
                else if(command=="refresh")AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else if(command=="stop"){SessionState.EraseString(State+".mode");Cleanup();report=null;EditorApplication.isPlaying=false;}
                else if(Modes.Contains(command))
                {
                    if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("Require stable compiled Edit Mode");
                    var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/第三章.unity")throw new Exception("Require existing clean third-chapter scene; this probe does not save/open scenes");
                    BackupSave(command);SessionState.SetString(State+".mode",command);EditorApplication.isPlaying=true;
                }
                else throw new Exception("Unknown command "+command);
            }
            catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(State+".mode","")!=""&&!driver)
            driver=new GameObject("Temporary continuous school walk verification").AddComponent<Chapter3CompactWalkProbeDriver>();
    }
    public static IEnumerator Run()
    {
        report=new Report{mode=SessionState.GetString(State+".mode",""),startedUtc=DateTime.UtcNow.ToString("O")};began=Time.realtimeSinceStartup;
        oldBackground=Application.runInBackground;Application.runInBackground=true;Object.DontDestroyOnLoad(driver);progressAt=0;phase="starting";
        originalInput=InputSystem.settings;testInput=Object.Instantiate(originalInput);testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInput;
        if(Keyboard.current==null)addedKeyboard=InputSystem.AddDevice<Keyboard>();if(Mouse.current==null)addedMouse=InputSystem.AddDevice<Mouse>();
        hashes=Protected.Where(File.Exists).ToDictionary(p=>p,Hash);
        report.observations.Add("Editor driver only. F7 + Carry are explicit starting fixtures. Real Attack/Reload and CharacterController.Move; no Warp, Enemy.Hit, Stage/Step writes, native input, VR or player-build claim. Enemy HoldFire isolates the walk; retry injects Damage(100). FocusOn is explicit steering only.");
        yield return null;yield return Guard(Scenario());
        if(report!=null){report.completed=report.errors.Count==0;Finish();}
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null)
            {
                report.errors.Add(failure.ToString());report.observations.Add("Failure: "+Describe());
                try{report.observations.Add("Failure image is immediate best-effort capture; pose/LateUpdate phase is unspecified and it is not visual completion evidence.");Render(flow?flow.player.view:Camera.main,"failure");}catch(Exception e){report.errors.Add(e.ToString());}
                while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch{}}Finish();yield break;
            }
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator sub){stack.Push(sub);continue;}yield return next;
        }
    }
    static IEnumerator Scenario()
    {
        flow=Object.FindFirstObjectByType<Chapter3Flow>();Check("one existing third-chapter flow",flow&&SceneManager.sceneCount==1);SourceCheck();
        bool knife=report.mode.EndsWith("knife");
        if(report.mode=="test-walk-shortcuts")
        {
            foreach(var key in new[]{Key.F6,Key.F7,Key.F8})
            {
                yield return ClearCombat(false,key==Key.F7);yield return FollowRoute(true);
                tracking=false;var old=walk;var lead=old.Lead;var rear=old.Rear;
                yield return Press(key);yield return Wait(()=>flow.Region==(key==Key.F6?"Station":key==Key.F7?"Combat":"School"),5,"shortcut region");
                Check(key+" cancels route",!old.Active);yield return Frames(2);
                var oldAudio=old.AudioSource;Check(key+" stops approach cue",oldAudio&&(!oldAudio.isPlaying||oldAudio.volume<=.0001f));
                if(key!=Key.F7)Check(key+" hides the old combat actors",!lead.gameObject.activeInHierarchy&&!rear.gameObject.activeInHierarchy);
                string region=flow.Region;int transitions=flow.Transitions;yield return new WaitForSecondsRealtime(2);
                Check(key+" no stale route transition",flow.Region==region&&flow.Transitions==transitions&&!old.Active);
                report.observations.Add(key+" cleanup oldLeadActive="+(lead&&lead.gameObject.activeInHierarchy)+" oldRearActive="+(rear&&rear.gameObject.activeInHierarchy)+" "+Describe());
                yield return Shot("cleanup-"+key);
            }
        }
        else
        {
            yield return ClearCombat(knife,false);yield return FollowRoute(false);yield return SchoolMission();
            tracking=false;yield return Press(Key.F8);yield return Wait(()=>flow.Region=="School"&&flow.school.Exploring,8,"F8 final cleanup");
            Check("F8 leaves route inactive",!walk.Active);Check("F8 returns to awaiting Tado",flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
        }
        Check("chapter result untouched before completion",SaveMatches());
        foreach(var pair in hashes)Check("protected bytes unchanged "+pair.Key,File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);
    }
    static IEnumerator ClearCombat(bool knife,bool retry)
    {
        tracking=false;flow.Carry(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F7);
        yield return Wait(()=>flow.Region=="Combat"&&flow.combat.Fighting,30,"normal F7 combat introduction");
        var combat=flow.combat;var weapon=combat.weapons;Check("three live guards",combat.Enemies.Count==3&&combat.DefeatedCount==0);
        if(retry)
        {
            combat.Damage(100,flow.player.transform.position+Vector3.forward);Check("injected damage enters Down",combat.CurrentStage==Chapter3Combat.Stage.Down);
            yield return Press(Key.R);yield return Wait(()=>combat.Fighting,5,"actual R retry");
            Check("retry resets health and enemies",combat.Health==100&&combat.DefeatedCount==0&&combat.Retries==1);
        }
        foreach(var e in combat.Enemies)e.HoldFire(240);
        foreach(var enemy in combat.Enemies.ToArray())
        {
            float start=Time.realtimeSinceStartup;
            while(!enemy.Defeated)
            {
                Deadline(start,65,"walking/attacking guard "+enemy.name);
                bool visible=Visible(enemy);float distance=Vector3.Distance(flow.player.view.transform.position,enemy.AimPoint);
                if((knife&&distance>1.85f)||(!knife&&!visible))
                {Move(enemy.transform.position-Vector3.forward*(knife?1.55f:3f));yield return null;continue;}
                flow.player.FocusOn(enemy.AimPoint);
                if(weapon.Ready)
                {
                    if(!knife&&!weapon.Loaded)weapon.Reload();
                    else
                    {
                        if(combat.DefeatedCount==2&&!tracking)BeginTracking();
                        weapon.Attack();
                    }
                }
                yield return null;
            }
            Check("actual weapon defeated "+enemy.name,enemy.Defeated);yield return Shot("guard-"+combat.DefeatedCount);
        }
        yield return Wait(()=>combat.CurrentStage==Chapter3Combat.Stage.Cleared,4,"combat naturally cleared");
        walk=new WalkAccess(flow);Check("route observation API exists",walk.Exists);
        yield return Wait(()=>walk.Active,15,"continuous route starts");
        Check("chosen single weapon retained",flow.CarriedWeapon==(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle));
        Check("one route component on flow",flow.GetComponents<MonoBehaviour>().Count(c=>c&&c.GetType().Name=="Chapter3SchoolApproach")==1);
        Check("one route audio owner on flow",flow.GetComponents<MonoBehaviour>().Count(c=>c&&c.GetType().Name=="Chapter3ApproachAudio")==1);
        Check("route exposes actual audio source",walk.AudioSource&&walk.AudioSource.clip);
        AudioSourcesCheck("route begin");
        Inspect();
        Check("combat clear leaves full player control",flow.player.canMove&&flow.player.canLook);yield return Shot("walk-start");
        var body=flow.player.GetComponent<CharacterController>();
        foreach(var actor in new[]{walk.Lead,walk.Rear})
        {
            var colliders=actor.GetComponentsInChildren<CapsuleCollider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            Check(actor.name+" retains solid body collision",colliders.Length>0&&colliders.All(c=>!Physics.GetIgnoreCollision(body,c)));
        }
    }
    static IEnumerator FollowRoute(bool halfway)
    {
        var points=walk.Points;float pathLength=0;for(int i=1;i<points.Length;i++)pathLength+=Vector3.Distance(points[i-1],points[i]);Check("compact connecting trail under 36 metres",pathLength<36&&pathLength>25);report.observations.Add("Connector length="+pathLength+" m; prior authored route was 92.99 m");Check("route supplies world waypoints",points.Length>=3);report.observations.Add("WorldWaypoints="+string.Join(";",points.Select(v=>v.ToString("F3"))));
        yield return ControlInputs();
        float waitStart=Time.realtimeSinceStartup;
        while(!walk.Waiting&&walk.Active){Deadline(waitStart,60,"leader gathers then waits when player stops");yield return CapturePendingShots();yield return null;}
        Check("leader waits for stopped player",walk.Waiting);var stopLead=walk.Lead.position;float rearGapBefore=Flat(walk.Rear.position,walk.Lead.position);
        yield return new WaitForSecondsRealtime(.8f);
        Check("waiting leader stays nearby",Flat(stopLead,walk.Lead.position)<.35f);
        // The rear follows the actually walked breadcrumbs around cover. Its
        // straight-line gap may briefly grow while rounding that corner.
        yield return Wait(()=>Flat(walk.Rear.position,walk.Lead.position)<3.4f,15,"rear reaches waiting leader along trail");
        Check("rear catches waiting leader along traversed trail",Flat(walk.Rear.position,walk.Lead.position)<3.4f);
        report.observations.Add("WAIT_REAR initialGap="+rearGapBefore+" finalGap="+Flat(walk.Rear.position,walk.Lead.position));
        yield return Shot("companions-wait");
        // Backtrack with actual movement, then follow the authored polyline.
        Vector3 back=flow.player.transform.position-Vector3.ProjectOnPlane(walk.Lead.position-flow.player.transform.position,Vector3.up).normalized*1.2f;
        yield return Walk(back,.25f,"actual backtrack");
        bool resumed=false;int first=NearestSegment(points,flow.player.transform.position);
        for(int i=first;i<points.Length&&walk.Active;i++)
        {
            yield return WalkRoutePoint(points,i);
            if(!walk.Waiting)resumed=true;
            yield return Shot("route-"+i);
            if(i==first+1||i==points.Length-2)yield return DiagnosticShot("route-side-"+i,flow.player.transform.position+Vector3.up*5-flow.player.view.transform.right*5,walk.Lead.position+Vector3.up);
            if(halfway&&i>=Mathf.Min(points.Length-2,first+2)){Check("shortcut interruption occurs mid-route",walk.Active);yield break;}
        }
        yield return Wait(()=>flow.Region=="School"&&flow.school.Exploring,12,"natural school arrival");yield return Frames(2);
        Check("companions resume after catchup",resumed);Check("route completes through natural arrival",walk.Completed&&!walk.Active);
        Check("arrival hides old combat actor instances",!walk.Lead.gameObject.activeInHierarchy&&!walk.Rear.gameObject.activeInHierarchy);
        Check("arrival activates school actor instances",flow.school.bawan.gameObject.activeInHierarchy&&flow.school.pihu.gameObject.activeInHierarchy);
        Check("first school handoff was observed",handoffObserved);Check("first school handoff preserves companion positions",handoffPositionError<.5f);
        Check("first school handoff preserves companion transform rotations",handoffRotationError<1f);
        // The root's explicit handoff changes actor identity. The school actors
        // may already have taken two ordinary follow steps by this observation.
        Check("handoff does not reset Bawan to school spawn",Flat(walk.Lead.position,flow.school.bawan.transform.position)<2f);
        Check("handoff does not reset Pihu to school spawn",Flat(walk.Rear.position,flow.school.pihu.transform.position)<2f);
        report.observations.Add("Actor identity handoff: old/new Bawan="+walk.Lead.position+" / "+flow.school.bawan.transform.position+" old/new Pihu="+walk.Rear.position+" / "+flow.school.pihu.transform.position+". Exact first-handoff frame is separately sampled by ObserveFrame.");
        Check("arrival preserves movement and look",flow.player.canMove&&flow.player.canLook);Check("arrival camera remains original",flow.player.view==Camera.main);
        EndTracking();Check("no fade during continuous travel",maxFade<.015f);Check("no locked input or disabled controller",locks==0);Check("no discontinuous player or companion steps",jumps==0);
        Check("continuous route has substantial walked distance",travel>10&&samples>15);Check("audio starts before school reveal",audioAt>=0&&revealAt>audioAt);
        Check("per-source output sampled on several frames",outputFrames>=3);
        Check("per-source output has finite nonzero waveform",outputFinite&&nonzeroOutputFrames>0&&outputPeak>0&&outputRms>0);
        Check("per-source nonzero output precedes school reveal",outputAt>=0&&revealAt>outputAt);
        report.observations.Add("PER_SOURCE_OUTPUT_SUMMARY frames="+outputFrames+" nonzeroFrames="+nonzeroOutputFrames+" peak="+outputPeak.ToString("R")+" maxRms="+outputRms.ToString("R")+" firstNonzeroTime="+outputAt+" revealAt="+revealAt+". AudioSource.GetOutputData channel 0, at most 20 realtime seconds; not final AudioListener mix, hardware audibility, or human listening evidence. No audio or listener settings changed.");
        Check("walk has grounded samples",missingGround==0);Check("player actually traverses a rise and fall",maxPlayerY-minPlayerY>.35f&&maxPlayerY-flow.player.transform.position.y>.2f);
        report.observations.Add("Travel="+travel+" heightRange="+(maxPlayerY-minPlayerY)+" maxFade="+maxFade+" samples="+samples+" audioAt="+audioAt+" revealAt="+revealAt);
        yield return Shot("natural-school-arrival");
    }
    static IEnumerator ControlInputs()
    {
        flow.player.FocusOn(walk.Lead.position+Vector3.up*1.2f);
        foreach(var key in new[]{Key.W,Key.S,Key.A,Key.D})
        {var start=flow.player.transform.position;yield return Hold(key,.22f);Check("queued "+key+" moves controller",Flat(start,flow.player.transform.position)>.04f);}
        var rotation=flow.player.view.transform.rotation;MouseEvent(new Vector2(36,0),true);yield return Frames(2);MouseEvent(Vector2.zero,false);yield return Frames(2);
        Check("queued mouse delta changes view",Quaternion.Angle(rotation,flow.player.view.transform.rotation)>.5f);
        rotation=flow.player.view.transform.rotation;yield return Frames(3);Check("no forced view turn with no look input",Quaternion.Angle(rotation,flow.player.view.transform.rotation)<.25f);
    }
    static IEnumerator SchoolMission()
    {
        Check("arrival retains original task entry",flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
        yield return Walk(flow.school.tado.transform.position,2.5f,"walk to Tado",()=>flow.school.MetTado);
        yield return Wait(()=>flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,30,"natural Tado briefing");yield return Shot("school-three-missions");
        yield return Press(Key.Digit1);yield return Wait(()=>flow.school.Battle.Fighting,30,"selected Tado begins school mission");
        Check("Tado choice preserved",flow.school.Battle.Selected==Chapter3SchoolBattle.Mission.Tado);yield return Shot("school-mission-start");
        AudioSourcesCheck("school mission");
    }
    static bool Visible(Chapter3Enemy enemy)
    {var origin=flow.player.view.transform.position;return flow.combat.Trace(origin,(enemy.AimPoint-origin).normalized,65,out var hit)&&hit.collider&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy;}
    static IEnumerator Walk(Vector3 target,float radius,string label,Func<bool> done=null)
    {
        float start=Time.realtimeSinceStartup;
        while(Flat(flow.player.transform.position,target)>radius)
        {yield return CapturePendingShots();if(done!=null&&done())yield break;Deadline(start,60,label);Move(target);Progress(label);yield return null;}
    }
    static IEnumerator WalkRoutePoint(Vector3[] points,int index)
    {
        float start=Time.realtimeSinceStartup;
        while(walk.Active&&Flat(flow.player.transform.position,points[index])>1.1f)
        {
            yield return CapturePendingShots();Deadline(start,100,"follow route "+index);
            int leaderIndex=walk.Index;
            if(leaderIndex>index)Move(points[index]);
            else if(leaderIndex==index)
            {
                var direction=Vector3.ProjectOnPlane(points[index]-walk.Lead.position,Vector3.up);
                if(direction.sqrMagnitude<.01f&&index>0)direction=Vector3.ProjectOnPlane(points[index]-points[index-1],Vector3.up);
                var toLeader=Vector3.ProjectOnPlane(walk.Lead.position-flow.player.transform.position,Vector3.up);
                // Keep following behind the moving leader; never march ahead
                // at player speed and make the guide's wait rule self-trigger.
                if(toLeader.magnitude>1.8f&&Vector3.Dot(toLeader,direction)>=-.05f)Move(walk.Lead.position);
            }
            Progress("follow route "+index);yield return null;
        }
    }
    static void Move(Vector3 target)
    {var p=flow.player;var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.FocusOn(target+Vector3.up*1.2f);if(d.sqrMagnitude>.001f)p.Move(d.normalized*Mathf.Min(d.magnitude,Mathf.Min(p.speed,2.8f)*Mathf.Min(Time.deltaTime,.08f)));}
    static int NearestSegment(Vector3[] p,Vector3 position){int found=0;float d=float.PositiveInfinity;for(int i=0;i<p.Length-1;i++){float n=Flat(p[i],position);if(n<d){d=n;found=i;}}return found;}
    static void BeginTracking()
    {
        tracking=true;walk=new WalkAccess(flow);samples=jumps=locks=missingGround=0;travel=maxFade=0;audioAt=revealAt=-1;sampleAt=0;pendingAudioShot=pendingRevealShot=false;
        outputBegan=outputAt=-1;outputPeak=outputRms=0;outputFrames=nonzeroOutputFrames=0;outputDone=false;outputFinite=true;
        previousPlayer=flow.player.transform.position;previousLead=flow.combat.bawan.transform.position;previousRear=flow.combat.pihu.transform.position;previousTime=Time.time;minPlayerY=maxPlayerY=previousPlayer.y;
        previousRegion=flow.Region;handoffPositionError=handoffRotationError=0;handoffObserved=false;
    }
    static void EndTracking(){ObserveFrame();tracking=false;}
    public static void ObserveFrame()
    {
        if(report==null||!tracking||!flow)return;
        var lead=flow.Region=="School"?flow.school.bawan.transform:walk!=null&&walk.Exists?walk.Lead:flow.combat.bawan.transform;
        var rear=flow.Region=="School"?flow.school.pihu.transform:walk!=null&&walk.Exists?walk.Rear:flow.combat.pihu.transform;
        if(!lead||!rear)return;var p=flow.player;Vector3 pos=p.transform.position;float dt=Mathf.Max(.001f,Time.time-previousTime),distance=Vector3.Distance(pos,previousPlayer);
        if(flow.Region=="School"&&previousRegion!="School")
        {
            handoffObserved=true;handoffPositionError=Mathf.Max(Vector3.Distance(lead.position,walk.Lead.position),Vector3.Distance(rear.position,walk.Rear.position));
            float leadAngle=Quaternion.Angle(lead.rotation,walk.Lead.rotation),rearAngle=Quaternion.Angle(rear.rotation,walk.Rear.rotation);
            handoffRotationError=Mathf.Max(leadAngle,rearAngle);
            report.observations.Add("FIRST_HANDOFF frame="+Time.frameCount+" maxCompanionPositionError="+handoffPositionError+" player="+previousPlayer+" -> "+pos+" BawanFacingAngle="+Vector3.Angle(flow.combat.bawan.facing,flow.school.bawan.facing)+" PihuFacingAngle="+Vector3.Angle(flow.combat.pihu.facing,flow.school.pihu.facing)+" BawanTransformAngle="+leadAngle+" PihuTransformAngle="+rearAngle+" oldBawanRotation="+walk.Lead.rotation.ToString("F5")+" newBawanRotation="+lead.rotation.ToString("F5")+" oldPihuRotation="+walk.Rear.rotation.ToString("F5")+" newPihuRotation="+rear.rotation.ToString("F5"));
        }
        if(distance>Mathf.Max(.22f,dt*(p.speed+1.5f))||Vector3.Distance(lead.position,previousLead)>Mathf.Max(.3f,dt*5)||Vector3.Distance(rear.position,previousRear)>Mathf.Max(.3f,dt*5))
        {jumps++;report.observations.Add("Discontinuity frame="+Time.frameCount+" dt="+dt+" player="+previousPlayer+" -> "+pos+" lead="+previousLead+" -> "+lead.position+" rear="+previousRear+" -> "+rear.position);}
        travel+=Flat(pos,previousPlayer);samples++;var body=p.GetComponent<CharacterController>();
        if(!p.canMove||!p.canLook||!body.enabled)locks++;maxFade=Mathf.Max(maxFade,flow.ui.fade.color.a);minPlayerY=Mathf.Min(minPlayerY,pos.y);maxPlayerY=Mathf.Max(maxPlayerY,pos.y);
        var source=walk!=null&&walk.Exists?walk.AudioSource:null;
        bool audio=source&&source.clip&&source.isPlaying&&source.volume>.0001f&&walk.AudioStarted,reveal=walk!=null&&walk.Exists&&walk.Revealed;
        if(audio&&audioAt<0){audioAt=Time.time;pendingAudioShot=true;report.observations.Add("FIRST_SOURCE_PLAYING frame="+Time.frameCount+" clip="+source.clip.name+" volume="+source.volume+" timeSamples="+source.timeSamples+" spatialBlend="+source.spatialBlend+" reveal="+reveal+". Source playback evidence, not AudioListener output.");}if(reveal&&revealAt<0){revealAt=Time.time;pendingRevealShot=true;}
        if(audio&&!outputDone)SampleSourceOutput(source,reveal);
        float ground=Ground(pos,p.transform),lg=Ground(lead.position,lead),rg=Ground(rear.position,rear);if(float.IsNaN(ground))missingGround++;
        if(Time.unscaledTime>=sampleAt)
        {
            sampleAt=Time.unscaledTime+.15f;report.frameSamples.Add(new Frame{frame=Time.frameCount,time=Time.time,player=pos,lead=lead.position,rear=rear.position,fade=flow.ui.fade.color.a,canMove=p.canMove,canLook=p.canLook,controller=body.enabled,region=flow.Region,groundFound=!float.IsNaN(ground),leadGroundFound=!float.IsNaN(lg),rearGroundFound=!float.IsNaN(rg),groundY=float.IsNaN(ground)?0:ground,playerGroundGap=float.IsNaN(ground)?0:pos.y-ground,leadGroundGap=float.IsNaN(lg)?0:lead.position.y-lg,rearGroundGap=float.IsNaN(rg)?0:rear.position.y-rg,waiting=walk!=null&&walk.Exists&&walk.Waiting,waypoint=walk!=null&&walk.Exists?walk.Index:-1,audio=audio,reveal=reveal,audioVolume=source?source.volume:0,audioTimeSamples=source&&source.clip?source.timeSamples:0});
        }
        previousPlayer=pos;previousLead=lead.position;previousRear=rear.position;previousTime=Time.time;previousRegion=flow.Region;Progress(phase??"monitor");
    }
    static void SampleSourceOutput(AudioSource source,bool reveal)
    {
        if(outputBegan<0)outputBegan=Time.realtimeSinceStartup;
        try
        {
            // Read this source only. Do not mute other sources or modify the
            // listener, project audio preferences, source volume or playback.
            source.GetOutputData(outputBuffer,0);
            float peak=0;double sum=0;bool finite=true;
            foreach(float value in outputBuffer)
            {
                if(float.IsNaN(value)||float.IsInfinity(value)){finite=false;continue;}
                peak=Mathf.Max(peak,Mathf.Abs(value));sum+=(double)value*value;
            }
            float rms=(float)Math.Sqrt(sum/outputBuffer.Length);outputFrames++;outputFinite&=finite;
            outputPeak=Mathf.Max(outputPeak,peak);outputRms=Mathf.Max(outputRms,rms);
            if(finite&&peak>0&&rms>0){nonzeroOutputFrames++;if(outputAt<0)outputAt=Time.time;}
            report.perSourceOutput.Add(new AudioOutput{frame=Time.frameCount,sourceId=source.GetInstanceID(),timeSamples=source.timeSamples,time=Time.time,peak=peak,rms=rms,volume=source.volume,reveal=reveal,finite=finite});
        }
        catch(Exception e){outputFinite=false;outputDone=true;report.observations.Add("PER_SOURCE_OUTPUT_READ_FAILED "+e);}
        if(Time.realtimeSinceStartup-outputBegan>=20)outputDone=true;
    }
    static float Ground(Vector3 position,Transform owner)
    {foreach(var h in Physics.RaycastAll(position+Vector3.up*2,Vector3.down,6,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))if(!h.collider.transform.IsChildOf(owner)&&!(h.collider is CharacterController)&&!h.collider.GetComponentInParent<Chapter2Actor>()&&h.normal.y>.5f)return h.point.y;return float.NaN;}
    static IEnumerator CapturePendingShots()
    {
        if(pendingAudioShot){pendingAudioShot=false;yield return Shot("audio-start-normal-camera");report.observations.Add("Audio screenshot captured at end of frame "+Time.time+" reveal="+walk.Revealed+"; actual audibility requires separate listener/manual evidence.");}
        if(pendingRevealShot){pendingRevealShot=false;yield return Shot("school-reveal-normal-camera");}
    }
    static IEnumerator Wait(Func<bool> done,float seconds,string label){float t=Time.realtimeSinceStartup;while(!done()){yield return CapturePendingShots();Deadline(t,seconds,label);Progress(label);yield return null;}}
    static IEnumerator Frames(int count){for(int i=0;i<count;i++){yield return null;yield return new WaitForEndOfFrame();}}
    static IEnumerator Press(Key key){yield return Hold(key,.12f);}
    static IEnumerator Hold(Key key,float seconds){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));try{yield return new WaitForSecondsRealtime(seconds);}finally{if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());}yield return Frames(2);}
    static void MouseEvent(Vector2 delta,bool right){var m=Mouse.current;if(m!=null)InputSystem.QueueStateEvent(m,new MouseState{position=m.position.ReadValue(),delta=delta}.WithButton(MouseButton.Right,right));}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Deadline(float start,float seconds,string label){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("Timeout "+label+" "+Describe());}
    static void Progress(string label){phase=label;if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+1;File.WriteAllText(Dir+"progress.txt",report.mode+" "+label+"\n"+Describe());}
    static string Describe()=>!flow?"no flow":"region="+flow.Region+" combat="+flow.combat.CurrentStage+" player="+flow.player.transform.position+" route="+(walk==null?"none":walk.Describe());
    static IEnumerator Shot(string label)
    {
        // Explicitly render after all actor/rig/face LateUpdates. A coroutine
        // resumed by yield null can otherwise capture the new actor position
        // with the preceding frame's world-space facial cut property block.
        yield return new WaitForEndOfFrame();
        Render(flow?flow.player.view:Camera.main,label);
        Inspect("WaitForEndOfFrame after all LateUpdates",label);
        report.observations.Add("END_OF_FRAME_SHOT "+label+" frame="+Time.frameCount);
    }
    static IEnumerator DiagnosticShot(string label,Vector3 position,Vector3 target)
    {
        yield return new WaitForEndOfFrame();
        var go=new GameObject("Temporary route diagnostic camera"){hideFlags=HideFlags.HideAndDontSave};
        try{var c=go.AddComponent<Camera>();c.CopyFrom(flow.player.view);c.enabled=false;go.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));Render(c,label);report.observations.Add(label+" uses temporary diagnostic view at "+position+" target "+target+" after WaitForEndOfFrame frame="+Time.frameCount);}
        finally{Object.DestroyImmediate(go);}
        Inspect("WaitForEndOfFrame diagnostic view after all LateUpdates",label);
    }
    static void Render(Camera camera,string label)
    {
        if(!camera)throw new Exception("No capture camera");var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();camera.targetTexture=rt;camera.aspect=1600f/900;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Dir+report.mode+"-"+label+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);}
    }
    static void BackupSave(string mode)
    {if(SessionState.GetBool(State+".savePending",false))throw new Exception("Unrestored previous save backup");var save=new Save{items=Keys.Select(k=>new Item{key=k,existed=PlayerPrefs.HasKey(k),raw=PlayerPrefs.GetString(k,"")}).ToArray()};string json=JsonUtility.ToJson(save,true);File.WriteAllText(Dir+"save-backup-"+mode+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json",json);SessionState.SetString(State+".save",json);SessionState.SetBool(State+".savePending",true);}
    static bool SaveMatches(){var save=JsonUtility.FromJson<Save>(SessionState.GetString(State+".save",""));return save!=null&&save.items.All(i=>PlayerPrefs.HasKey(i.key)==i.existed&&(!i.existed||PlayerPrefs.GetString(i.key,"")==i.raw));}
    static void RestoreSave(){if(!SessionState.GetBool(State+".savePending",false))return;var save=JsonUtility.FromJson<Save>(SessionState.GetString(State+".save",""));if(save?.items==null)throw new Exception("Missing save backup");foreach(var i in save.items){if(i.existed)PlayerPrefs.SetString(i.key,i.raw);else PlayerPrefs.DeleteKey(i.key);}PlayerPrefs.Save();if(!SaveMatches())throw new Exception("Save restore mismatch");SessionState.SetBool(State+".savePending",false);}
    static void Cleanup(){tracking=false;if(testInput){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());MouseEvent(Vector2.zero,false);}if(originalInput)InputSystem.settings=originalInput;if(testInput)Object.DestroyImmediate(testInput);originalInput=testInput=null;if(addedKeyboard!=null)InputSystem.RemoveDevice(addedKeyboard);if(addedMouse!=null)InputSystem.RemoveDevice(addedMouse);addedKeyboard=null;addedMouse=null;RestoreSave();}
    static void Finish(){if(report==null)return;try{Cleanup();Check("original PlayerPrefs restored",SaveMatches());}catch(Exception e){report.errors.Add(e.ToString());}report.elapsed=Time.realtimeSinceStartup-began;report.completed=report.completed&&report.errors.Count==0;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(State+".mode");report=null;Application.runInBackground=oldBackground;EditorApplication.isPlaying=false;}
    static void Log(string message,string stack,LogType kind){if(report==null)return;if(kind==LogType.Warning)report.warnings.Add(message);if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)report.errors.Add(message+"\n"+stack);}
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static void Audit(){var s=SceneManager.GetActiveScene();File.WriteAllText(Dir+"audit.json","{\"scene\":\""+s.path+"\",\"dirty\":"+s.isDirty.ToString().ToLowerInvariant()+",\"playing\":"+Application.isPlaying.ToString().ToLowerInvariant()+",\"compileFailed\":"+EditorUtility.scriptCompilationFailed.ToString().ToLowerInvariant()+"}");}
    static string[] ApproachFiles()=>Directory.GetFiles("Assets/Chapter3","*Approach*",SearchOption.AllDirectories).OrderBy(p=>p).Select(p=>p.Replace('\\','/')+" bytes="+new FileInfo(p).Length+" sha256="+Hash(p)).ToArray();
    static void SourceCheck()
    {
        foreach(string path in Directory.GetFiles("Assets/Chapter3","*Approach*",SearchOption.AllDirectories))
            Check("new approach file below 10 MiB "+path,new FileInfo(path).Length<10L*1024*1024);
        report.observations.Add("Approach source inventory (10 MiB is a per-file review guard, not a rendering-performance target): "+string.Join(";",ApproachFiles()));
    }
    static void AudioSourcesCheck(string label)
    {
        var source=walk.AudioSource;Check(label+" configured approach source exists",source&&source.clip);
        var sources=flow.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AudioSource>(true)).ToArray();
        var matching=sources.Where(s=>s.clip==source.clip).ToArray();
        Check(label+" cue clip assigned to exactly one source",matching.Length==1&&matching[0]==source);
        long bytes=(long)source.clip.samples*source.clip.channels*sizeof(float);
        Check(label+" generated cue decoded below 10 MiB",bytes<10L*1024*1024);
        report.observations.Add(label+" audio source="+source.GetInstanceID()+" clip="+source.clip.name+" samples="+source.clip.samples+" frequency="+source.clip.frequency+" channels="+source.clip.channels+" decodedBytes="+bytes+" matchingSources="+matching.Length);
    }
    static void Inspect(string capturePhase="Editor command or unspecified frame phase",string shotLabel=null)
    {
        var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var result=new Inspection{utc=DateTime.UtcNow.ToString("O"),scene=scene.path,capturePhase=capturePhase,frame=Time.frameCount,playing=Application.isPlaying,dirty=scene.isDirty,compileFailed=EditorUtility.scriptCompilationFailed,
            roots=scene.GetRootGameObjects().Select(g=>g.name+" active="+g.activeInHierarchy+" position="+g.transform.position).ToArray(),
            missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            components=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c&&c.GetType().Name.Contains("Approach")).Select(c=>c.GetType().Name+" owner="+c.name+" id="+c.GetInstanceID()+" active="+c.isActiveAndEnabled).ToArray(),
            audio=all.SelectMany(t=>t.GetComponents<AudioSource>()).Select(a=>a.name+" id="+a.GetInstanceID()+" active="+a.isActiveAndEnabled+" playing="+a.isPlaying+" loop="+a.loop+" volume="+a.volume+" clip="+(a.clip?a.clip.name:"null")+" samples="+(a.clip?a.clip.samples:0)+" channels="+(a.clip?a.clip.channels:0)+" timeSamples="+(a.clip?a.timeSamples:0)).ToArray(),sources=ApproachFiles(),heads=HeadInspection(scene)};
        File.WriteAllText(Dir+"inspect"+(report!=null?"-"+report.mode+"-"+Time.frameCount:"")+(shotLabel!=null?"-"+shotLabel:"")+".json",JsonUtility.ToJson(result,true));
    }
    static string[] HeadInspection(Scene scene)
    {
        var result=new List<string>();
        var f=scene.GetRootGameObjects().Select(g=>g.GetComponentInChildren<Chapter3Flow>(true)).FirstOrDefault(x=>x);
        if(!f)return new[]{"No Chapter3Flow"};
        foreach(var actor in new[]{f.combat.bawan,f.combat.pihu,f.school.bawan,f.school.pihu})
        {
            if(!actor){result.Add("Missing actor");continue;}
            var owner=actor.transform;var speech=actor.GetComponent<Chapter2SpeechFace>();
            result.Add("ACTOR "+Hierarchy(owner)+" id="+actor.GetInstanceID()+" active="+actor.gameObject.activeInHierarchy+" pos="+owner.position.ToString("F4")+" scale="+owner.lossyScale.ToString("F4")+" speech="+(speech?speech.GetInstanceID():0)+" speechEnabled="+(speech&&speech.isActiveAndEnabled)+" surfaceCount="+(speech?.surfaces?.Length??0));
            if(!speech||speech.surfaces==null)continue;
            for(int i=0;i<speech.surfaces.Length;i++)
            {
                var s=speech.surfaces[i];if(s==null){result.Add("SURFACE "+i+" null");continue;}
                var block=new MaterialPropertyBlock();if(s.body)s.body.GetPropertyBlock(block);
                result.Add("SURFACE "+i+" owner="+actor.GetInstanceID()+" head="+Hierarchy(s.head)+" headId="+(s.head?s.head.GetInstanceID():0)+" headUnderOwner="+(s.head&&s.head.IsChildOf(owner))+" headWorld="+(s.head?s.head.position.ToString("F4"):"null")+" originLocal="+s.origin.ToString("F4")+" expectedCutWorld="+(s.head?s.head.TransformPoint(s.origin).ToString("F4"):"null")+" actualCutWorld="+block.GetVector("_StoryCutOrigin").ToString("F4")+" cutEnabled="+block.GetFloat("_StoryCutEnabled")+" cutAsset="+(s.cut?AssetDatabase.GetAssetPath(s.cut):"null"));
                result.Add(SkinInspection("BODY",s.body,owner));result.Add(SkinInspection("PATCH",s.renderer,owner));
            }
        }
        return result.ToArray();
    }
    static string SkinInspection(string label,SkinnedMeshRenderer skin,Transform owner)
    {
        if(!skin)return label+" null";
        return label+" "+Hierarchy(skin.transform)+" id="+skin.GetInstanceID()+" underOwner="+skin.transform.IsChildOf(owner)+" enabled="+skin.enabled+" forceOff="+skin.forceRenderingOff+" active="+skin.gameObject.activeInHierarchy+" visible="+skin.isVisible+" updateOffscreen="+skin.updateWhenOffscreen+" pos="+skin.transform.position.ToString("F4")+" scale="+skin.transform.lossyScale.ToString("F4")+" mesh="+(skin.sharedMesh?skin.sharedMesh.name:"null")+" meshId="+(skin.sharedMesh?skin.sharedMesh.GetInstanceID():0)+" meshAsset="+(skin.sharedMesh?AssetDatabase.GetAssetPath(skin.sharedMesh):"null")+" localBounds="+skin.localBounds+" worldBounds="+skin.bounds+" rootBone="+Hierarchy(skin.rootBone)+" rootBoneUnderOwner="+(skin.rootBone&&skin.rootBone.IsChildOf(owner))+" bones="+skin.bones.Length+" externalBones="+string.Join(";",skin.bones.Where(b=>b&&!b.IsChildOf(owner)).Select(Hierarchy))+" materials="+string.Join(";",skin.sharedMaterials.Select(m=>m?m.name+" shader="+(m.shader?m.shader.name:"null")+" asset="+AssetDatabase.GetAssetPath(m):"null"));
    }
    static string Hierarchy(Transform t){if(!t)return "null";var path=t.name;while(t.parent){t=t.parent;path=t.name+"/"+path;}return path;}

    // Agreed API, read-only reflection so the backup can be compiled before
    // the production component lands. Missing observations always fail.
    sealed class WalkAccess
    {
        readonly Component component;
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        public WalkAccess(Chapter3Flow f){component=f.GetComponents<MonoBehaviour>().FirstOrDefault(c=>c&&c.GetType().Name=="Chapter3SchoolApproach");}
        public bool Exists=>component;
        object Read(params string[] names){if(!component)throw new Exception("Approach component missing; finalize WalkAccess");foreach(string n in names){var t=component.GetType();var p=t.GetProperty(n,Flags);if(p!=null)return p.GetValue(component);var f=t.GetField(n,Flags);if(f!=null)return f.GetValue(component);}throw new Exception("Missing read-only route API "+string.Join("/",names));}
        public bool Active=>Exists&&(bool)Read("Active");public bool Completed=>(bool)Read("Completed");public bool Waiting=>(bool)Read("Waiting");public bool AudioStarted=>(bool)Read("AudioStarted");public bool Revealed=>(bool)Read("SchoolRevealed");public int Index=>Convert.ToInt32(Read("CurrentWaypoint"));
        public Vector3[] Points{get{var value=Read("WorldWaypoints");if(value is IEnumerable<Vector3> vectors)return vectors.ToArray();throw new Exception("WorldWaypoints is not a Vector3 enumerable");}}
        static Transform TransformOf(object value){if(value is Transform t)return t;if(value is Component c)return c.transform;throw new Exception("Route actor unavailable");}
        public Transform Lead=>TransformOf(Read("LeadActor"));public Transform Rear=>TransformOf(Read("RearActor"));
        public AudioSource AudioSource
        {get{var cue=Read("AudioCue") as Component;if(!cue)return null;var type=cue.GetType();var p=type.GetProperty("Source",Flags);if(p!=null)return p.GetValue(cue) as AudioSource;var f=type.GetField("Source",Flags);if(f!=null)return f.GetValue(cue) as AudioSource;throw new Exception("AudioCue.Source read-only API missing");}}
        public string Describe()=>!Exists?"missing":component.GetType().Name+" active="+Active+" point="+Index+" waiting="+Waiting;
    }
}
[DefaultExecutionOrder(5000)]
public sealed class Chapter3CompactWalkProbeDriver:MonoBehaviour
{IEnumerator Start(){yield return Chapter3CompactWalkProbe.Run();}void LateUpdate(){Chapter3CompactWalkProbe.ObserveFrame();}}
