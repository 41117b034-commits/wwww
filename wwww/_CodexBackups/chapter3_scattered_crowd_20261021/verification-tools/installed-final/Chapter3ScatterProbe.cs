using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Object=UnityEngine.Object;

// Temporary Editor harness. Install only after the runtime API is ready, then
// refresh once. Delete the installed script/meta after validation. All output
// stays outside Assets. Never assigns a gameplay Stage, Step or checkpoint.
[InitializeOnLoad]
public static partial class Chapter3ScatterProbe
{
    const string Dir="_CodexBackups/chapter3_scattered_crowd_20261021/", State="Chapter3ScatterProbe";
    const string SecondKey="WusheEvent.Chapter2.Result";
    static readonly string[] Modes={"test-scatter-preview","test-scatter-tado","test-scatter-bawan","test-scatter-pihu","test-ambush-tado","test-ambush-bawan","test-ambush-pihu","test-diagnose","test-repair-shots","test-tado-rescue-rifle","test-tado-rescue-knife","test-tado-leave-rifle","test-tado-leave-knife","test-legacy-bawan","test-legacy-pihu","test-store","test-native-footing","test-checkpoint-wounded","test-checkpoint-defeated","test-legacy-bawan-knife","test-legacy-pihu-rifle"};
    [Serializable] sealed class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();}
    [Serializable] sealed class SaveItem {public string key;public bool existed;public string raw;}
    [Serializable] sealed class SaveBackup {public string capturedUtc;public SaveItem[] items;}
    static Report report;static Chapter3ScatterProbeDriver driver;
    static InputSettings originalInput,testInput;static Keyboard addedKeyboard;
    static float began,progressAt;static bool backgroundBefore;
    static Chapter3Flow tracked;static bool tracking;static Vector3 lastTado;
    static float lastSample,travel;static int jumps,samples;
    static readonly HashSet<Chapter3Ending.Stage> endingStages=new HashSet<Chapter3Ending.Stage>();
    static Transform[] audience;static Vector3[] audienceHomes;static Quaternion[] audienceRotations;static bool[] audienceActive;
    static CapsuleCollider[] audienceCapsules;static Vector3[] capsuleCenters;static float[] capsuleHeights,capsuleRadii;static int[] capsuleDirections;static int crowdMeshCount;
    static readonly FieldInfo guideIndexField=typeof(Chapter3SchoolBattle).GetField("guideIndex",BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly FieldInfo stragglerIndexField=typeof(Chapter3SchoolBattle).GetField("stragglerIndex",BindingFlags.Instance|BindingFlags.NonPublic);

    static Chapter3ScatterProbe()
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
            string cmd;try{cmd=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try
            {
                if(cmd=="audit")Audit();
                else if(cmd=="refresh")AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else if(cmd=="stop"){SessionState.EraseString(State+".mode");Cleanup();report=null;EditorApplication.isPlaying=false;}
                else if(Modes.Contains(cmd))
                {
                    if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before another scenario");
                    if(EditorUtility.scriptCompilationFailed)throw new Exception("Compilation is not clean");
                    if(SceneManager.GetActiveScene().isDirty)throw new Exception("Resolve existing unsaved scene edits first; this probe never saves them");
                    if(SceneManager.GetActiveScene().path!="Assets/Scenes/第三章.unity")EditorSceneManager.OpenScene("Assets/Scenes/第三章.unity");
                    BackupSave(cmd);SessionState.SetString(State+".mode",cmd);EditorApplication.isPlaying=true;
                }
                else throw new Exception("Unknown assault probe command: "+cmd);
            }
            catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(State+".mode","")!=""&&!driver)
            driver=new GameObject("Temporary school assault verification").AddComponent<Chapter3ScatterProbeDriver>();
    }
    public static IEnumerator Run()
    {
        report=new Report{mode=SessionState.GetString(State+".mode","")};began=Time.realtimeSinceStartup;
        tracked=null;tracking=false;travel=0;jumps=samples=0;endingStages.Clear();progressAt=0;
        backgroundBefore=Application.runInBackground;Application.runInBackground=true;Object.DontDestroyOnLoad(driver.gameObject);
        originalInput=InputSystem.settings;testInput=Object.Instantiate(originalInput);
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=testInput;if(Keyboard.current==null)addedKeyboard=InputSystem.AddDevice<Keyboard>();
        if(!SessionState.GetBool(State+".savePending",false))BackupSave(report.mode);
        report.observations.Add("Editor driver evidence only: input events use a cloned InputSettings, walking uses Chapter3Player.Move/CharacterController, aiming uses FocusOn and attacks use actual weapon.Attack. Not native keyboard, standalone Player or headset evidence.");
        report.observations.Add("Each gameplay mode starts at the existing F8 school shortcut with Carry as an explicit weapon fixture. Station opening/pickup and short combat are outside this targeted run. No Stage/Step/position is assigned to advance gameplay.");
        yield return null;yield return Guard(Scenario());
        if(report!=null){report.completed=report.errors.Count==0;Finish();}
    }
    static IEnumerator Guard(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        while(stack.Count>0)
        {
            bool moved=false;object next=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null)
            {
                report.errors.Add(error.ToString());
                var ambient=Object.FindFirstObjectByType<Chapter3SchoolAmbient>(); if(ambient){report.observations.Add("Ambient fallen "+ambient.FallenAdults+"/"+ambient.FallenChildren);for(int i=0;i<ambient.spectators.Length;i++)report.observations.Add("Guest "+i+" "+ambient.SpectatorPhase(i)+" "+ambient.spectators[i].position+" actor "+ambient.Ambusher(i%10).position);} report.observations.Add("failure route state: "+DiagnosticState(Object.FindFirstObjectByType<Chapter3Flow>()));
                try{Shot("failure");}catch(Exception shotError){report.errors.Add(shotError.ToString());}
                while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch{}}
                Finish();yield break;
            }
            if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator sub){stack.Push(sub);continue;}yield return next;
        }
    }
    static IEnumerator Scenario()
    {
        Check("stable serialized mission values",(int)Chapter3SchoolBattle.Mission.Bawan==1&&(int)Chapter3SchoolBattle.Mission.Pihu==2&&(int)Chapter3SchoolBattle.Mission.Tado==3);
        if(report.mode=="test-store"){Store();yield break;}
        var f=Object.FindFirstObjectByType<Chapter3Flow>();Check("single third chapter scene",f&&SceneManager.sceneCount==1&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity");
        if(report.mode=="test-repair-shots"){yield return CombatScenario();yield break;} if(report.mode.StartsWith("test-ambush-")){yield return AmbushPreview(f);yield break;}
        if(report.mode.StartsWith("test-scatter-")){yield return TenScenario(f);yield break;} if(report.mode=="test-diagnose"){yield return Diagnose(f);yield break;} if(report.mode=="test-native-footing"){yield return NativeFooting(f);yield break;} if(report.mode.StartsWith("test-checkpoint-")){yield return CheckpointScenario(f,report.mode.EndsWith("wounded"));yield break;}
        bool legacy=report.mode.Contains("legacy"),pihu=report.mode.Contains("pihu"),knife=report.mode.Contains("knife")||(pihu&&!report.mode.Contains("rifle"));
        var mission=legacy?(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan):Chapter3SchoolBattle.Mission.Tado;
        f.Carry(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F8);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"F8 school entry");
        CaptureCalm(f); CheckSurpriseCalm(f);
        yield return Choose(f,mission);tracked=f;
        if(legacy){yield return LegacySchool(f,pihu);yield return ResetSchool(f);yield break;}
        ResumeTracking();yield return Assault(f,knife);
        Check("Tado physically walks the assault route without discontinuity",samples>100&&travel>30&&jumps==0);
        report.observations.Add("Tado walking: "+travel.ToString("F2")+"m / "+samples+" samples / "+jumps+" jumps; retry resets excluded");tracking=false;
        yield return Rescue(f,knife,report.mode.Contains("rescue"));yield return Ending(f);
        string completed=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"");yield return ResetSchool(f);
        Check("F8 preserves the newly completed result",PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"")==completed);
    }
    [Serializable] sealed class FootingGround
    {
        public bool hit;public string collider;public Vector3 rayOrigin,point,normal;
    }
    [Serializable] sealed class FootingMesh
    {
        public string renderer,mesh;public int vertices;public Vector3 lowestWorld,skinWorldPosition,skinWorldScale;
    }
    [Serializable] sealed class FootingSample
    {
        public string label,lowestRenderer;public int frame,totalVertices;public float realtime,rigHeight,modelHeight;
        public bool walking,usingOriginalSkin;public Vector3 ownerPosition,ownerScale,modelPosition,modelScale,leftFootWorld,rightFootWorld,lowestWorld;
        public FootingGround ownerGround,leftGround,rightGround,lowestGround;
        public float meshGroundGap,leftBoneGroundGap,rightBoneGroundGap;
        public FootingMesh[] meshes;
    }
    [Serializable] sealed class FootingData
    {
        public string fixture="Normal F8 school initialization followed by explicit Ambient.ResetStraggler/MoveStraggler pose fixture; no mission playback or Stage/Step/Warp writes. BakeMesh(false) vertices transformed by each skin TransformPoint after WaitForEndOfFrame. Raw gaps are measurements, not visual acceptance thresholds.";
        public List<FootingSample> samples=new List<FootingSample>();
    }
    static IEnumerator NativeFooting(Chapter3Flow f)
    {
        report.observations.Add("Isolated native-footing pose fixture. F8 initializes the real school; Ambient.ResetStraggler explicitly places the diagnostic actor in the open forecourt. This is not a complete assault route. Neither the player nor story Stage/Step is warped or assigned, and no leg bone, mesh, transform scale or geometry is calibrated by the probe.");
        report.observations.Add("Each sample waits for end of frame, calls BakeMesh(temp,false) once per active native skin, scans every baked vertex once in world space, then destroys the temporary mesh. Bone Y and lowest mesh Y are separate measurements; no unconfirmed floating/penetration tolerance is asserted.");
        yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"footing normal school initialization");CaptureCalm(f); CheckSurpriseCalm(f);
        var ambient=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();Check("footing ambient fixture API available",ambient);
        var data=new FootingData();Vector3 start=V(4,-14),destination=V(4,-6);float moved=0;
        try
        {
            ambient.ResetStraggler(start);var owner=ambient.Straggler;
            Check("footing fixture has active original geometry",owner&&owner.gameObject.activeInHierarchy&&owner.GetComponent<Chapter3SchoolCrowdActor>().UsingOriginalSkin&&owner.GetComponent<Chapter3SchoolCrowdActor>().HasPoseGeometry);
            var actor=owner.GetComponent<Chapter3SchoolCrowdActor>();var rig=owner.GetComponentInChildren<Chapter1IncidentRig>(true);
            Check("footing fixture has original animator rig",rig&&rig.animator);
            var left=FootBone(rig.animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");var right=FootBone(rig.animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
            Check("footing left and right original foot bones found",left&&right);
            report.observations.Add("Footing fixture lane: start="+start.ToString("F3")+" destination="+destination.ToString("F3")+" speed=1.65m/s. Player stays at normal F8 spawn; only FocusOn aims the existing view for screenshots.");
            yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();
            data.samples.Add(MeasureFooting(f,actor,rig,left,right,"initial-idle-0.30s"));FootingViews(f,owner,"initial-idle");
            yield return new WaitForSeconds(.7f);yield return new WaitForEndOfFrame();
            data.samples.Add(MeasureFooting(f,actor,rig,left,right,"initial-idle-1.00s"));
            ambient.SetStragglerWaiting(false);float beganWalk=Time.realtimeSinceStartup;int sampleIndex=0;
            float[] distances={1f,3f,5f,7f};Vector3 previous=owner.position;
            while(Flat(owner.position,destination)>.22f)
            {
                if(Time.realtimeSinceStartup-beganWalk>18)throw new Exception("footing fixture walk blocked at "+owner.position+" toward "+destination);
                ambient.MoveStraggler(destination,1.65f);moved+=Flat(previous,owner.position);previous=owner.position;
                if(sampleIndex<distances.Length&&Flat(start,owner.position)>=distances[sampleIndex])
                {
                    int at=sampleIndex++;yield return new WaitForEndOfFrame();
                    data.samples.Add(MeasureFooting(f,actor,rig,left,right,"walking-distance-"+distances[at].ToString("F1")+"m"));
                    if(at==1)FootingViews(f,owner,"walking-midstride");
                }
                Progress("footing fixture walking distance="+moved.ToString("F3"));yield return null;
            }
            Check("public fixture MoveStraggler physically walks the lane",moved>7&&Flat(owner.position,destination)<=.22f&&sampleIndex==distances.Length);
            ambient.SetStragglerWaiting(true);
            // Stop issuing movement, then observe the rig settle naturally.
            // No ResetStraggler, ResetPose, Ground, IK or bone assignments here.
            float sinceStop=0;
            foreach(float delay in new[]{.25f,.5f,.75f})
            {
                yield return new WaitForSeconds(delay);sinceStop+=delay;yield return new WaitForEndOfFrame();
                data.samples.Add(MeasureFooting(f,actor,rig,left,right,"after-natural-stop-"+sinceStop.ToString("F2")+"s"));
            }
            FootingViews(f,owner,"after-natural-stop-1.50s");
            Check("footing captures initial idle moving and settled samples",data.samples.Count==9&&data.samples.All(s=>s.totalVertices>0&&s.lowestGround.hit&&s.leftGround.hit&&s.rightGround.hit));
            Check("footing samples include real rig locomotion and natural idle",data.samples.Any(s=>s.label.StartsWith("walking-distance-")&&s.walking)&&data.samples.Where(s=>s.label.StartsWith("after-natural-stop-")).All(s=>!s.walking));
            Check("pose fixture never chooses or advances a school mission",f.school.Battle.Selected==Chapter3SchoolBattle.Mission.None&&f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&f.school.Battle.Step==0&&!f.school.MetTado&&!ambient.Panicking);
            Check("pose fixture preserves original player result",OriginalSaveMatches());
        }
        finally
        {
            // Preserve partial measurements on failure and remove the fixture.
            // The normal harness cleanup also restores PlayerPrefs on errors.
            try{File.WriteAllText(Dir+"test-native-footing-samples.json",JsonUtility.ToJson(data,true));}
            finally{if(ambient)ambient.HideStraggler();}
        }
        yield return ResetSchool(f);Check("footing cleanup F8 preserves original player result",OriginalSaveMatches());
    }
    static Transform FootBone(Animator animator,HumanBodyBones bone,params string[] suffixes)
    {
        if(animator.isHuman){var mapped=animator.GetBoneTransform(bone);if(mapped)return mapped;}
        return animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>suffixes.Any(s=>t.name.EndsWith(s,StringComparison.OrdinalIgnoreCase)));
    }
    static FootingSample MeasureFooting(Chapter3Flow f,Chapter3SchoolCrowdActor actor,Chapter1IncidentRig rig,Transform left,Transform right,string label)
    {
        var owner=actor.transform;var meshes=new List<FootingMesh>();Vector3 lowest=new Vector3(0,float.PositiveInfinity,0);string lowestRenderer="";int vertices=0;
        foreach(var skin in owner.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.enabled&&s.gameObject.activeInHierarchy&&s.sharedMesh))
        {
            var baked=new Mesh{name="Temporary read-only footing sample",hideFlags=HideFlags.HideAndDontSave};
            try
            {
                skin.BakeMesh(baked,false);var points=baked.vertices;Vector3 partLowest=new Vector3(0,float.PositiveInfinity,0);
                foreach(var vertex in points)
                {
                    Vector3 world=skin.transform.TransformPoint(vertex);
                    if(!Finite(world.x)||!Finite(world.y)||!Finite(world.z))throw new Exception("Non-finite baked vertex in "+skin.name);
                    if(world.y<partLowest.y)partLowest=world;
                }
                Check(label+" baked vertices available for "+skin.name,points.Length>0&&Finite(partLowest.y));
                meshes.Add(new FootingMesh{renderer=skin.name,mesh=skin.sharedMesh.name,vertices=points.Length,lowestWorld=partLowest,skinWorldPosition=skin.transform.position,skinWorldScale=skin.transform.lossyScale});
                vertices+=points.Length;if(partLowest.y<lowest.y){lowest=partLowest;lowestRenderer=skin.name;}
            }
            finally{Object.DestroyImmediate(baked);}
        }
        Check(label+" measured finite original skin geometry",meshes.Count>0&&vertices>0&&Finite(lowest.y));
        var sample=new FootingSample{label=label,frame=Time.frameCount,realtime=Time.realtimeSinceStartup-began,totalVertices=vertices,rigHeight=rig.Height,modelHeight=actor.ModelHeight,walking=rig.walking,usingOriginalSkin=actor.UsingOriginalSkin,ownerPosition=owner.position,ownerScale=owner.lossyScale,modelPosition=rig.transform.position,modelScale=rig.transform.lossyScale,leftFootWorld=left.position,rightFootWorld=right.position,lowestWorld=lowest,lowestRenderer=lowestRenderer,meshes=meshes.ToArray()};
        sample.ownerGround=FootingGroundAt(f,owner,owner.position);sample.leftGround=FootingGroundAt(f,owner,left.position);sample.rightGround=FootingGroundAt(f,owner,right.position);sample.lowestGround=FootingGroundAt(f,owner,lowest);
        if(sample.lowestGround.hit)sample.meshGroundGap=lowest.y-sample.lowestGround.point.y;
        if(sample.leftGround.hit)sample.leftBoneGroundGap=left.position.y-sample.leftGround.point.y;
        if(sample.rightGround.hit)sample.rightBoneGroundGap=right.position.y-sample.rightGround.point.y;
        Check(label+" ground rays have usable static hits",sample.ownerGround.hit&&sample.leftGround.hit&&sample.rightGround.hit&&sample.lowestGround.hit);
        report.observations.Add(label+" frame="+sample.frame+" walking="+sample.walking+" vertices="+vertices+" actualMeshMinY="+lowest.y.ToString("F6")+" leftBoneY="+left.position.y.ToString("F6")+" rightBoneY="+right.position.y.ToString("F6")+" rigHeight="+rig.Height.ToString("F6")+" ownerY="+owner.position.y.ToString("F6")+" modelRootY="+rig.transform.position.y.ToString("F6")+" groundAtMinY="+sample.lowestGround.point.y.ToString("F6")+" meshGroundGap="+sample.meshGroundGap.ToString("F6"));
        return sample;
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static FootingGround FootingGroundAt(Chapter3Flow f,Transform owner,Vector3 point)
    {
        Vector3 origin=point+Vector3.up*3;
        foreach(var hit in Physics.RaycastAll(origin,Vector3.down,10,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
        {
            var c=hit.collider;if(c.transform.IsChildOf(owner)||c.transform.IsChildOf(f.player.transform)||c.GetComponentInParent<Chapter3Bystander>()||c.GetComponentInParent<Chapter2Actor>())continue;
            return new FootingGround{hit=true,collider=c.name,rayOrigin=origin,point=hit.point,normal=hit.normal};
        }
        return new FootingGround{hit=false,rayOrigin=origin};
    }
    static void FootingViews(Chapter3Flow f,Transform owner,string label)
    {
        f.player.FocusOn(owner.position+Vector3.up*.8f);var camera=f.player.view;
        report.observations.Add(label+" normal camera position="+camera.transform.position.ToString("F4")+" rotation="+camera.transform.eulerAngles.ToString("F4")+" verticalFov="+camera.fieldOfView.ToString("F4")+" PNG=1600x900; existing camera position/FOV unchanged.");
        Shot("footing-"+label+"-normal");Vector3 position=owner.position+new Vector3(-2.4f,.75f,2.4f),target=owner.position+Vector3.up*.6f;
        report.observations.Add(label+" diagnostic camera position="+position.ToString("F4")+" lookAt="+target.ToString("F4")+" copiedVerticalFov="+camera.fieldOfView.ToString("F4")+" PNG=1600x900; this calibrated viewpoint is not the player's normal position.");
        ShotAt("footing-"+label+"-diagnostic",position,target);
    }
    static IEnumerator Choose(Chapter3Flow f,Chapter3SchoolBattle.Mission expected)
    {
        var b=f.school.Battle;var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        yield return Walk(f.player,f.school.tado.transform.position,"meet Tado",2.8f,()=>b.CurrentStage!=Chapter3SchoolBattle.Stage.AwaitingTado);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"route menu");
        Check("all ten concealed before choice",!a.Panicking&&a.VisibleAssailants==0);
        for(int i=0;i<10;i++)Check("inside-school ambusher "+i,a.Ambusher(i)&&a.AmbushStart(i).z>5&&Mathf.Abs(a.AmbushStart(i).x)<19&&!a.Ambusher(i).gameObject.activeSelf);
        yield return new WaitForEndOfFrame();Shot("calm-twenty-guests");
        yield return Press(expected==Chapter3SchoolBattle.Mission.Tado?Key.Digit1:expected==Chapter3SchoolBattle.Mission.Bawan?Key.Digit2:Key.Digit3);
        Check("requested route chosen and ten release on same signal",b.Selected==expected&&a.Panicking&&a.VisibleAssailants==10&&a.AmbushSignalFrame>=0);
        for(int i=0;i<10;i++)Check("knife geometry on ambusher "+i,a.Ambusher(i).Find("族人手持獵刀").GetComponentsInChildren<MeshRenderer>().Length>0);
        foreach(var guard in b.Enemies)if(guard)guard.HoldFire(180);
        yield return new WaitForSeconds(4);yield return new WaitForEndOfFrame();Shot("first-wave-player-view");
        ShotAt("knife-contact-closeup",new Vector3(-13,1.7f,5),new Vector3(-10,1,9));
        yield return Wait(()=>b.Fighting,25,"outbreak to Fighting");
        yield return Wait(()=>a.AftermathVisible,25,"adult and child falls");
        Check("adult and child aftermath",a.FallenAdults>=2&&a.FallenChildren>=2);
        yield return new WaitForEndOfFrame();ShotAt("aftermath-closeup",new Vector3(-14,2,6),new Vector3(-10,.5f,9));
        report.observations.Add("Officer fire held for ambient/input observations; later route regression keeps normal damage rules.");
    }
    static IEnumerator Assault(Chapter3Flow f,bool knife)
    {
        var b=f.school.Battle;var p=f.player;
        Check("assault starts with two staged threats",b.Enemies.Count==2&&b.Enemies[0].gameObject.activeInHierarchy&&b.Enemies[1].gameObject.activeInHierarchy);
        for(int step=0;step<2;step++)
        {
            // Capture values per iteration before yielding; nested Wait/Walk
            // predicates must not depend on a later loop-variable increment.
            int currentStep=step,nextStep=step+1;
            Check("assault step "+step+" is naturally reached",b.Step==step&&b.CheckpointStep==step&&b.Fighting);
            if(step==1)yield return RetryAssault(f,1,knife);
            var guard=b.Enemies[step];Check("current guard active",guard&&guard.gameObject.activeInHierarchy&&!guard.Defeated);
            Vector3[] points=b.AssaultWaypoints.ToArray();Check("authored assault waypoints available",points.Length>0);
            foreach(var point in points)
            {
                yield return WalkFight(f,point,guard,knife,"assault "+step);
                if(b.Step!=step)break;
            }
            if(!guard.Defeated)
            {
                if(knife)yield return Walk(p,guard.transform.position+Vector3.ProjectOnPlane(p.transform.position-guard.transform.position,Vector3.up).normalized*1.35f,"last guard approach");
                yield return Kill(f,guard,knife);
            }
            if(b.Step==currentStep)yield return Walk(p,b.AssaultObjective,"assault endpoint",1.2f,()=>b.Step>currentStep);
            yield return Wait(()=>b.Step>=nextStep,35,"Tado and player complete assault stage "+currentStep);
            Check("defeated guard creates checkpoint "+nextStep,guard.Defeated&&b.Step==nextStep&&b.CheckpointStep==nextStep);Shot("checkpoint-"+nextStep);
        }
        Check("third stage waits for straggler interaction",b.Step==2&&!b.StragglerCalled&&!b.StragglerArrived);
        yield return Walk(p,b.StragglerPosition+new Vector3(-1.6f,0,-.7f),"approach straggler");
        yield return Press(Key.E);Check("E calls nearby straggler",b.StragglerCalled);
        yield return RetryAssault(f,2,knife);
        Check("third-stage retry resets straggler and both cleared guards",!b.StragglerCalled&&!b.StragglerArrived&&b.Enemies.All(e=>!e||e.Defeated||!e.gameObject.activeInHierarchy));
        yield return Walk(p,b.StragglerPosition+new Vector3(-1.6f,0,-.7f),"retry approach straggler");yield return Press(Key.E);
        Check("retry accepts actual E call again",b.StragglerCalled);Shot("straggler-called");
        CheckStraggler(f,"called after retry");
        ShotAt("straggler-close",b.StragglerPosition+new Vector3(-2.5f,1.5f,2.2f),b.StragglerPosition+Vector3.up*.85f);
        var withdrawal=b.AssaultWaypoints.ToArray();
        for(int i=0;i<withdrawal.Length;i++)
        {
            if(b.Step>2||b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete)break;
            // Intermediate waypoints allow normal capsule separation. At the
            // final waypoint, stand beside the reunion instead of on Tado's
            // (0,76) or the ally's route. (3,77) is inside both runtime <4m
            // completion radii and leaves the three leaders' bodies clear.
            bool last=i==withdrawal.Length-1;
            yield return Walk(p,last?V(3,77):withdrawal[i],"escort straggler and Tado",last ? .35f : 1.2f,
                ()=>b.Step>2||b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete);
        }
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete,35,"third stage reaches regroup");
        Check("three stages completed and straggler arrived",b.Step==3&&b.CheckpointStep==3&&b.StragglerArrived);
        CheckStraggler(f,"escaped to rear");
        yield return Walk(p,new Vector3(0,0,78),"rendezvous");
        yield return Wait(()=>b.Rescue&&b.Rescue.Active,90,"natural assault to rescue handoff");
        Check("same scene enters original rescue sequence",b.Complete&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing&&SceneManager.sceneCount==1);
        Check("all original companions reunite alive",new[]{f.school.bawan,f.school.pihu,f.school.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4&&a.FallProgress==0));Shot("reunion");
    }
    static void CheckStraggler(Chapter3Flow f,string label)
    {
        var ambient=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();var root=ambient?ambient.Straggler:null;
        Check(label+" straggler remains an active route companion",root&&root.gameObject.activeInHierarchy&&root.GetComponent<Chapter3SchoolCrowdActor>()&&root.GetComponent<Chapter3SchoolCrowdActor>().IsRouteCompanion);
        var actor=root.GetComponent<Chapter3SchoolCrowdActor>();var colliders=root.GetComponentsInChildren<Collider>(true);
        Check(label+" straggler is protected by the bystander marker",root.GetComponent<Chapter3Bystander>()&&colliders.All(c=>c.GetComponentInParent<Chapter3Bystander>()));
        Check(label+" straggler is absent from damage target components and lists",root.GetComponentsInChildren<Chapter3Enemy>(true).Length==0&&root.GetComponentsInChildren<Chapter3CompanionReaction>(true).Length==0&&f.combat.Enemies.All(e=>!e||!e.transform.IsChildOf(root)));
        var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.enabled&&s.gameObject.activeInHierarchy).ToArray();
        Check(label+" original skin geometry and bones are available",actor.UsingOriginalSkin&&actor.HasPoseGeometry&&skins.Length>0&&skins.All(s=>s.sharedMesh&&s.rootBone&&s.bones.Length>0&&s.bones.All(bone=>bone)));
        var originalMeshes=f.school.bawan.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>s.sharedMesh).Where(m=>m).ToArray();
        Check(label+" rendered skins reuse original source meshes",skins.All(s=>originalMeshes.Contains(s.sharedMesh)));
        report.observations.Add(label+" target checks are component/list inspection only; this check does not fire a shot or inject damage into the straggler.");
        report.observations.Add(label+" geometry: position="+root.position.ToString("F3")+" rootScale="+root.lossyScale.ToString("F3")+" modelHeight="+actor.ModelHeight.ToString("F3")+" originalSkin="+actor.UsingOriginalSkin+" geometryReady="+actor.HasPoseGeometry+" colliders="+colliders.Length);
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var skin=renderer as SkinnedMeshRenderer;
            report.observations.Add(label+" renderer="+renderer.name+" enabled="+renderer.enabled+" active="+renderer.gameObject.activeInHierarchy+" worldBoundsCenter="+renderer.bounds.center.ToString("F3")+" worldBoundsSize="+renderer.bounds.size.ToString("F3")+" scale="+renderer.transform.lossyScale.ToString("F3")+(skin?" localBoundsSize="+skin.localBounds.size.ToString("F3")+" bones="+skin.bones.Length+" mesh="+(skin.sharedMesh?skin.sharedMesh.name:"missing"):""));
        }
        report.observations.Add(label+" renderer bounds are culling/geometry diagnostics only. They do not assert that the skirt/legs look normal; judge deformation from the rendered images.");
    }
    static IEnumerator RetryAssault(Chapter3Flow f,int step,bool knife)
    {
        var b=f.school.Battle;int retries=b.Retries;tracking=false;
        report.observations.Add("At naturally reached Step "+step+", Damage(100) injects failure solely to exercise the real Down->R->ResetMission checkpoint path; it is not evidence of an incoming enemy shot.");
        b.Damage(100,f.player.transform.position);yield return null;
        Check("injected failure reaches Down at checkpoint "+step,b.CurrentStage==Chapter3SchoolBattle.Stage.Down&&b.CheckpointStep==step);Shot("retry-stage-"+(step+1));
        yield return Press(Key.R);yield return Wait(()=>b.Fighting,5,"retry stage "+step);
        var weapon=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        Check("retry preserves mission checkpoint and exclusive weapon",b.Selected==Chapter3SchoolBattle.Mission.Tado&&b.Step==step&&b.CheckpointStep==step&&b.Retries==retries+1&&b.Health==100&&f.CarriedWeapon==weapon&&f.combat.weapons.Selected==weapon);
        Check("retry never revives cleared prior threats",b.Enemies.Take(step).All(e=>!e||e.Defeated||!e.gameObject.activeInHierarchy));ResumeTracking();
    }
    static IEnumerator WalkFight(Chapter3Flow f,Vector3 point,Chapter3Enemy guard,bool knife,string label)
    {
        float start=Time.realtimeSinceStartup;var p=f.player;var b=f.school.Battle;int startedStep=b.Step;
        while(Flat(p.transform.position,point)>.35f)
        {
            // Completing the actual runtime checkpoint ends this walking task
            // immediately, even when Tado now occupies the old destination.
            if(b.Step>startedStep)yield break;
            if(!b.Fighting)throw new Exception("left Fighting while "+label);
            if(Time.realtimeSinceStartup-start>50)throw new Exception("stuck "+label+" player="+p.transform.position+" point="+point);
            if(!guard.Defeated&&Flat(p.transform.position,guard.transform.position)<(knife?2.1f:45f)&&CanTrace(f,guard))yield return Kill(f,guard,knife);
            if(b.Step>startedStep)yield break;
            // Reaching the NPC-occupied destination within capsule separation
            // is enough to continue the driver's route, not to pass a stage:
            // Assault() still waits for the runtime checkpoint itself.
            if(Flat(p.transform.position,point)<=1.2f&&new[]{f.school.tado,f.school.bawan,f.school.pihu}.Any(a=>Flat(a.transform.position,point)<1.2f))yield break;
            Move(p,point);Progress(label);yield return null;
        }
        if(b.Step>startedStep)yield break;
        if(!guard.Defeated&&Flat(p.transform.position,guard.transform.position)<(knife?2.1f:45f)&&CanTrace(f,guard))yield return Kill(f,guard,knife);
    }
    static IEnumerator LegacySchool(Chapter3Flow f,bool pihu)
    {
        var b=f.school.Battle;var p=f.player;
        if(pihu)
        {
            yield return Route(p,new[]{V(8,-8),V(25,-4),V(27.5f,7)},"legacy east approach");
            yield return Wait(()=>b.AtCrossing&&!b.ThreatRevealing,35,"legacy east threat ready");
            yield return Route(p,new[]{V(27.5f,14),V(27.5f,24),V(27.5f,27.4f)},"legacy east guard approach");
            yield return Kill(f,b.Enemies[0],f.CarriedWeapon==Chapter3Weapons.Weapon.Knife);yield return Walk(p,V(27.5f,22),"legacy east command range");
            yield return Press(Key.E);Check("legacy east command remains active",b.Advancing);
            yield return Wait(()=>b.Step==1,30,"legacy east checkpoint one");
        }
        else
        {
            yield return Route(p,new[]{V(-7,-8),V(-23,-4),V(-27,8),V(-27,12.9f)},"legacy west approach");
            yield return Kill(f,b.Enemies[0],f.CarriedWeapon==Chapter3Weapons.Weapon.Knife);yield return Walk(p,b.Barricades[0].position+Vector3.back*2.5f,"legacy west handle");
            yield return Hold(Key.E,2.15f);yield return Wait(()=>b.Step==1,8,"legacy west checkpoint one");
        }
        Check("legacy route retains first checkpoint and selected task",b.Step==1&&b.CheckpointStep==1&&b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan)&&b.Enemies[0].Defeated);
        Check("legacy run did not save unfinished chapter",OriginalSaveMatches());Shot("legacy-checkpoint-one");
        if(pihu)
        {
            yield return Wait(()=>!b.ThreatRevealing&&b.Enemies[1].gameObject.activeInHierarchy,15,"legacy second east threat");
            // Clear the knife-reachable guard before ordering the escort, then
            // walk back into the real command radius. No pressure suppression.
            yield return Route(p,new[]{V(27.5f,30),V(29,37),V(32,43),V(33,51.5f)},"legacy second knife guard");
            yield return Kill(f,b.Enemies[1],f.CarriedWeapon==Chapter3Weapons.Weapon.Knife);
            yield return Route(p,new[]{V(32,43),V(29,37),V(27.5f,30)},"legacy return to command radius");
            yield return Press(Key.E);Check("legacy second crossing command accepted",b.Advancing);
            yield return Route(p,new[]{V(29,37),V(32,43),V(27,53)},"legacy second crossing");
        }
        else
        {
            yield return Route(p,new[]{V(-27,35),V(-26.5f,43)},"legacy side gate staging");
            yield return Wait(()=>Flat(f.school.bawan.transform.position,b.GateWorkPosition)<.28f,35,"Bawan reaches second gate handle");
            yield return Press(Key.E);Check("legacy E commands real gate work",b.GateCommanded);
            yield return Kill(f,b.Enemies[1],f.CarriedWeapon==Chapter3Weapons.Weapon.Knife);
            yield return Wait(()=>b.GateWorkProgress>=.999f,15,"legacy gate work completes");
        }
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,35,"legacy mission regroup");
        Check("both legacy threat checkpoints completed",b.Step==2&&b.Enemies[1].Defeated);
        var exit=pihu?new[]{V(32,53),V(27.5f,54),V(27.5f,60),V(25,68),V(12,74),V(0,78)}:new[]{V(-27,54),V(-25,66),V(-12,74),V(0,78)};
        yield return Route(p,exit,"rendezvous");yield return Wait(()=>b.Rescue&&b.Rescue.Active,90,"legacy natural rescue handoff");
        Check("legacy full school route reaches original rescue",b.Complete&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing&&new[]{f.school.bawan,f.school.pihu,f.school.tado}.All(a=>Flat(a.transform.position,V(0,78))<4));Shot("legacy-reunion");
        report.observations.Add("Legacy regression covers both real guards, both authored interactions, new independent Tado route, actual regroup and natural Rescue.Advancing then F8. Legacy rescue and ending are not replayed.");
    }
    static IEnumerator Rescue(Chapter3Flow f,bool knife,bool save)
    {
        var s=f.school;var p=f.player;var b=s.Battle;var r=b.Rescue;float start=Time.realtimeSinceStartup;
        while(r.CurrentStage==Chapter3Rescue.Stage.Advancing)
        {
            if(Time.realtimeSinceStartup-start>70)throw new Exception("common rescue advance stuck");
            Move(p,s.bawan.transform.position,1.8f);Progress("common forest advance");yield return null;
        }
        yield return Wait(()=>r.CurrentStage==Chapter3Rescue.Stage.Choice,25,"natural injury choice");
        Check("Pihu injury pose and canonical location",r.Pose.BonesReady&&r.Pose.InjuryWeight==1&&Flat(s.pihu.transform.position,r.InjuryPosition)<1);Shot("injury-choice");
        yield return Press(save?Key.Digit1:Key.Digit2);
        yield return Wait(()=>r.CurrentStage==(save?Chapter3Rescue.Stage.AwaitingLift:Chapter3Rescue.Stage.Leaving),15,"rescue choice commits");
        Check("assault mission survives rescue choice",b.Selected==Chapter3SchoolBattle.Mission.Tado&&r.Selected==(save?Chapter3Rescue.Decision.Rescue:Chapter3Rescue.Decision.Leave));
        yield return Wait(()=>b.Enemies.Any(e=>e&&!e.Defeated),10,"forest threat");var guard=b.Enemies.First(e=>e&&!e.Defeated);
        // Verify the school controller has released the shared enemy list.
        // Keep the naturally reached forest position and receive a real shot.
        p.FocusOn(guard.AimPoint);
        yield return Wait(()=>guard.AttackEnabled&&guard.Telegraphing,12,"forest pursuer actually raises rifle");
        int incomingShots=guard.Shots,healthBeforeShot=r.Health;
        yield return Wait(()=>guard.Shots>incomingShots,5,"forest pursuer actually fires");
        Check("forest pursuer retains attack control after school completion",b.Complete&&r.CombatActive&&guard.AttackEnabled);
        Check("real forest pursuer bullet removes twenty health",guard.Shots==incomingShots+1&&r.Health==healthBeforeShot-20);
        report.observations.Add("Forest pursuit regression: actual telegraph -> shot, shots="+guard.Shots+", rescue health "+healthBeforeShot+" -> "+r.Health+". No HoldFire, injected incoming damage or enemy positioning is used.");
        yield return new WaitForEndOfFrame();Shot("forest-pursuer-fired");
        if(knife)yield return Walk(p,guard.transform.position+Vector3.ProjectOnPlane(p.transform.position-guard.transform.position,Vector3.up).normalized*1.35f,"forest guard approach");
        yield return Kill(f,guard,knife);
        if(save)
        {
            yield return Walk(p,r.InjuryPosition+new Vector3(1.35f,0,-.3f),"lift Pihu");yield return Hold(Key.E,2.35f);
            Check("held E actually lifts Pihu",r.Supporting&&r.Pose.SupportWeight==1&&p.speed<=1.2f&&!f.combat.weapons.Attack());Shot("supporting-Pihu");
        }
        else Check("leave branch cannot lift Pihu",!r.Supporting&&!r.WorkLift(true,10));
        foreach(var point in new[]{V(0,98),V(8,103),r.ReceptionPosition})
        {
            start=Time.realtimeSinceStartup;
            while(Flat(p.transform.position,point)>.4f&&r.CurrentStage!=Chapter3Rescue.Stage.Handoff&&r.CurrentStage!=Chapter3Rescue.Stage.Complete)
            {
                if(Time.realtimeSinceStartup-start>50)throw new Exception("rescue route stuck "+p.transform.position+" -> "+point);
                Move(p,point);Progress("rescue route");yield return null;
            }
        }
        yield return Wait(()=>r.Ending&&r.Ending.Active,35,"natural ending handoff");
        Check("rescue outcome retains physical Pihu position",save?Flat(s.pihu.transform.position,r.ReceptionPosition)<4:Flat(s.pihu.transform.position,r.InjuryPosition)<1);Shot("reception");
    }
    static IEnumerator Ending(Chapter3Flow f)
    {
        var b=f.school.Battle;var r=b.Rescue;var e=r.Ending;e.PersistResult=true;endingStages.Clear();endingStages.Add(e.CurrentStage);
        Check("ending is pending before roll call",!e.Saved&&OriginalSaveMatches());
        yield return Wait(()=>e.CurrentStage==Chapter3Ending.Stage.Conversation&&e.Conversation.Active,140,"outcome roll call and conversation");
        var record=e.Result;bool rescued=r.Selected==Chapter3Rescue.Decision.Rescue;
        Check("assault pending result matches real route",Chapter3ResultStore.IsValid(record)&&!record.completed&&record.mission==Chapter3SchoolBattle.Mission.Tado&&record.weapon==f.CarriedWeapon&&record.decision==r.Selected);
        Check("outcome and roll call obey committed choice",e.RollCallCompleted&&e.DeathConfirmed==!rescued&&record.pihuSurvived==rescued&&record.squadSurvivors==(rescued?4:3));
        Check("natural outcome roll call observed",endingStages.Contains(Chapter3Ending.Stage.Outcome)&&endingStages.Contains(Chapter3Ending.Stage.RollCall));
        Check("no save before Mona closing",!e.Saved&&OriginalSaveMatches());Shot("ending-conversation");
        var c=e.Conversation;
        foreach(string question in new[]{"小隊現在幾人？","莫那是不是一直都跟著我們？"})
        {Check("grounded conversation input accepted",c.SubmitReply(question));yield return Wait(()=>!c.WaitingForReply,6,"fixed authored answer");report.observations.Add(question+" => "+c.LastReply);}
        Check("two fixed replies unlock explicit ending button",c.CompletedTurns==2&&c.GroundedReplies==2&&f.ui.depart.gameObject.activeSelf);Shot("two-turn-transcript");
        f.ui.depart.onClick.Invoke();yield return Wait(()=>e.CurrentStage==Chapter3Ending.Stage.MonaClosing,5,"Mona explicit handoff");
        yield return Wait(()=>e.Mona&&e.Mona.gameObject.activeInHierarchy&&f.ui.fade.color.a<.05f,20,"Mona visible");Shot("Mona-closing");
        yield return Wait(()=>e.CurrentStage==Chapter3Ending.Stage.Complete,70,"natural chapter complete");
        var loaded=Chapter3ResultStore.Load();Check("completed assault save roundtrips canonical format",e.Saved&&Chapter3ResultStore.IsValid(loaded,true)&&loaded.mission==record.mission&&loaded.decision==record.decision&&loaded.weapon==record.weapon&&loaded.squadSurvivors==record.squadSurvivors);
        Shot("chapter-complete");
    }
    static IEnumerator ResetSchool(Chapter3Flow f)
    {
        tracking=false;yield return Press(Key.F8);yield return Wait(()=>f.school.Exploring,15,"F8 cleanup");var b=f.school.Battle;
        Check("F8 clears route selection checkpoint retry",b.Selected==Chapter3SchoolBattle.Mission.None&&b.Step==0&&b.CheckpointStep==0&&b.Retries==0&&b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
        Check("F8 clears assault interaction and enemies",!b.StragglerCalled&&!b.StragglerArrived&&b.Enemies.Count==2&&b.VisibleOfficers==2&&b.Enemies.All(e=>e&&!e.AttackEnabled&&e.Health==100)&&!b.ThreatRevealing);
        Check("F8 restores fresh controls and no ending",f.player.canMove&&f.player.canLook&&!f.ui.chat.activeSelf&&!f.ui.ending.activeSelf&&(!b.Rescue||!b.Rescue.Active&&(!b.Rescue.Ending||!b.Rescue.Ending.Active)));
        CheckCalm(f); CheckSurpriseCalm(f);
        yield return new WaitForSeconds(3);
        Check("old callbacks never revive mission or ending",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&b.Selected==Chapter3SchoolBattle.Mission.None&&!f.ui.chat.activeSelf);Shot("F8-clean");
    }
    static void CaptureCalm(Chapter3Flow f)
    {
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();audience=a.spectators.Where(t=>t).ToArray();
        audienceHomes=audience.Select(t=>t.position).ToArray();audienceRotations=audience.Select(t=>t.localRotation).ToArray();audienceActive=audience.Select(t=>t.gameObject.activeSelf).ToArray();
        audienceCapsules=audience.SelectMany(t=>t.GetComponentsInChildren<CapsuleCollider>(true)).ToArray();capsuleCenters=audienceCapsules.Select(c=>c.center).ToArray();
        capsuleHeights=audienceCapsules.Select(c=>c.height).ToArray();capsuleRadii=audienceCapsules.Select(c=>c.radius).ToArray();capsuleDirections=audienceCapsules.Select(c=>c.direction).ToArray();crowdMeshCount=Chapter3SchoolCrowdActor.SharedMeshCount;
        Check("initial school sports day is calm",!a.Panicking&&a.FallenAdults==0&&a.VisibleAssailants==0&&(!a.Straggler||!a.Straggler.gameObject.activeSelf));
    }
    static void CheckCalm(Chapter3Flow f)
    {
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("F8 resets panic casualties and background assault",!a.Panicking&&a.FallenAdults==0&&!a.AftermathVisible&&a.VisibleAssailants==0&&(!a.Straggler||!a.Straggler.gameObject.activeSelf));
        Check("F8 restores every original audience position and visibility",audience.Select((t,i)=>Vector3.Distance(t.position,audienceHomes[i])<.01f&&t.gameObject.activeSelf==audienceActive[i]&&Quaternion.Angle(t.localRotation,audienceRotations[i])<7f).All(x=>x));
        Check("F8 clears audience pose and fall state",audience.All(t=>!t.GetComponent<Chapter3SchoolCrowdActor>().Posing&&!t.GetComponent<Chapter3SchoolCrowdActor>().IsFallen));
        Check("F8 restores audience capsule dimensions",audienceCapsules.Select((c,i)=>Vector3.Distance(c.center,capsuleCenters[i])<.001f&&Mathf.Abs(c.height-capsuleHeights[i])<.001f&&Mathf.Abs(c.radius-capsuleRadii[i])<.001f&&c.direction==capsuleDirections[i]).All(x=>x));
        Check("F8 reuses shared crowd meshes without accumulation",Chapter3SchoolCrowdActor.SharedMeshCount==crowdMeshCount);
    }
    static void Store()
    {
        report.observations.Add("Isolated result format test with canonical pending records; not gameplay. PlayerPrefs restored on completion/error/play exit/domain reload.");
        foreach(int value in new[]{1,2,3})foreach(var decision in new[]{Chapter3Rescue.Decision.Rescue,Chapter3Rescue.Decision.Leave})foreach(var weapon in new[]{Chapter3Weapons.Weapon.Knife,Chapter3Weapons.Weapon.Rifle})
        {
            var record=Chapter3ResultStore.Build((Chapter3SchoolBattle.Mission)value,decision,weapon);string label="mission "+value+" "+decision+" "+weapon;
            Check(label+" canonical pending record",Chapter3ResultStore.IsValid(record)&&!record.completed&&record.version==2);
            Check(label+" saves",Chapter3ResultStore.SaveCompleted(record));var read=Chapter3ResultStore.Load();
            Check(label+" retains numeric identity and outcome",read!=null&&(int)read.mission==value&&read.decision==decision&&read.weapon==weapon&&read.pihuSurvived==(decision==Chapter3Rescue.Decision.Rescue)&&read.squadSurvivors==(decision==Chapter3Rescue.Decision.Rescue?4:3));
            Check(label+" save never mutates pending caller",!record.completed);
            var tado=read.actors.Single(a=>a.actorId=="tado");
            Check(label+" central Tado knowledge belongs to new story",tado.schoolSide=="central"&&tado.centralSchool==Chapter3ResultStore.KnowledgeSource.Direct&&tado.westSchool==Chapter3ResultStore.KnowledgeSource.Reported&&tado.eastSchool==Chapter3ResultStore.KnowledgeSource.Reported);
        }
        // Frozen shape and knowledge rules from before/Chapter3ResultStore.cs,
        // not produced by the new Build/Copy implementation being tested.
        foreach(int oldMission in new[]{1,2})foreach(bool rescued in new[]{true,false})
        {
            string legacy=LegacySave(oldMission,rescued);PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,legacy);
            var old=Chapter3ResultStore.Load();string label="frozen v1 mission "+oldMission+" rescued="+rescued;
            Check(label+" loads original shape",old!=null&&old.version==1&&(int)old.mission==oldMission&&old.completed);
            var tado=old.actors.Single(a=>a.actorId=="tado");
            Check(label+" preserves old opposite side",tado.schoolSide==(oldMission==1?"east":"west")&&tado.centralSchool==Chapter3ResultStore.KnowledgeSource.None);
            Check(label+" load is read-only",PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)==legacy);
            var copy=Chapter3ResultStore.Copy(old);Check(label+" copy remains independent and versioned",copy!=old&&copy.actors!=old.actors&&copy.version==1&&Chapter3ResultStore.IsValid(copy,true));
            Check(label+" can resave without migration",Chapter3ResultStore.SaveCompleted(copy)&&JToken.DeepEquals(JObject.Parse(legacy),JObject.Parse(PlayerPrefs.GetString(Chapter3ResultStore.SaveKey))));
        }
        string valid=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey);var extra=JObject.Parse(valid);extra["unknown"]=true;
        var absent=JObject.Parse(valid);absent.Remove("weapon");var unknown=JObject.Parse(valid);unknown["mission"]=4;
        var oldTado=JObject.Parse(valid);oldTado["mission"]=3;
        foreach(string bad in new[]{extra.ToString(),absent.ToString(),unknown.ToString(),oldTado.ToString(),"{"})
        {PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,bad);Check("invalid format rejected",Chapter3ResultStore.Load()==null);}
        PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,valid);Check("valid save still accepted after invalid fixtures",Chapter3ResultStore.Load()!=null);
    }
    static string LegacySave(int mission,bool rescued)
    {
        Func<string,string,bool,int,JObject> actor=(id,side,alive,outcome)=>new JObject
        {
            ["actorId"]=id,["schoolSide"]=side,["originalSquad"]=id!="mona",["alive"]=alive,
            ["westSchool"]=side=="west"?1:2,["eastSchool"]=side=="east"?1:2,
            ["injury"]=id=="mona"?2:1,["outcome"]=outcome
        };
        return new JObject
        {
            ["version"]=1,["completed"]=true,["mission"]=mission,["decision"]=rescued?1:2,["weapon"]=0,
            ["pihuSurvived"]=rescued,["squadSurvivors"]=rescued?4:3,
            ["actors"]=new JArray(actor("bawan","west",true,rescued?1:2),actor("pihu","east",rescued,rescued?1:0),actor("tado",mission==1?"east":"west",true,1),actor("mona","none",true,2))
        }.ToString(Newtonsoft.Json.Formatting.None);
    }
    static IEnumerator Kill(Chapter3Flow f,Chapter3Enemy enemy,bool knife)
    {
        var w=f.combat.weapons;int before=knife?w.Slashes:w.Shots;
        if(!knife)
        {
            if(!w.Loaded){yield return Wait(()=>w.Ready,4,"reload ready");Check("real reload accepted",w.Reload());yield return Wait(()=>!w.Reloading,5,"reload completes");}
            yield return Wait(()=>w.Ready,5,"attack cooldown ready"); Vector3 aim; string trace; bool visible=CheckpointRegionRay(f,enemy,Chapter3EnemyHitZone.BodyRegion.Body,out aim,out trace)||CheckpointRegionRay(f,enemy,Chapter3EnemyHitZone.BodyRegion.Head,out aim,out trace); Check("visible unoccluded body or head",visible); report.observations.Add("Live guard aim: "+trace); f.player.FocusOn(aim); f.player.FocusOn(aim); Physics.SyncTransforms();
            Check("rifle ray reaches actual guard",CanTrace(f,enemy));yield return ClickFire(); Check("Input System left click fires loaded rifle feedback="+w.Feedback,w.Shots==before+1);yield return new WaitForSeconds(.6f);
        }
        else
        {
            float start=Time.realtimeSinceStartup;
            while(!enemy.Defeated)
            {
                if(!f.combat.Fighting)throw new Exception("player down before knife kill");
                if(Time.realtimeSinceStartup-start>7)throw new Exception("knife cannot reach "+enemy.transform.position+" from "+f.player.transform.position);
                if(Flat(f.player.transform.position,enemy.transform.position)>1.55f)Move(f.player,enemy.transform.position,1.4f); f.player.FocusOn(enemy.AimPoint);if(w.Ready)w.Attack();yield return null;
            }
        }
        Check("guard defeated by real "+(knife?"knife swings":"rifle shot"),enemy.Defeated&&(knife?w.Slashes:w.Shots)>before);
    }
    static bool CanTrace(Chapter3Flow f,Chapter3Enemy enemy)
    {Vector3 aim;string diagnostic;return CheckpointRegionRay(f,enemy,Chapter3EnemyHitZone.BodyRegion.Body,out aim,out diagnostic)||CheckpointRegionRay(f,enemy,Chapter3EnemyHitZone.BodyRegion.Head,out aim,out diagnostic);}
    static IEnumerator Route(Chapter3Player p,IEnumerable<Vector3> points,string label){foreach(var point in points)yield return Walk(p,point,label);}
    static IEnumerator Walk(Chapter3Player p,Vector3 point,string label,float arrivalRadius=.35f,Func<bool> completed=null)
    {
        float start=Time.realtimeSinceStartup;
        // Reunion accepts the runtime's proximity condition. Never force the
        // player into the leaders at the exact centre after they have arrived.
        bool finalRendezvous=label=="rendezvous"&&Flat(point,V(0,78))<.1f;
        if(finalRendezvous)arrivalRadius=3.25f;
        while(Flat(p.transform.position,point)>arrivalRadius)
        {
            if(completed!=null&&completed())yield break;
            if(finalRendezvous&&!p.canMove&&Flat(p.transform.position,point)<4)yield break;
            if(Time.realtimeSinceStartup-start>50)throw new Exception("stuck "+label+" "+p.transform.position+" -> "+point);
            Move(p,point);Progress(label);yield return null;
        }
    }
    static void Move(Chapter3Player p,Vector3 target,float gap=0)
    {var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.FocusOn(target+Vector3.up*1.2f);if(d.magnitude>gap)p.Move(d.normalized*Mathf.Min(d.magnitude-gap,Mathf.Min(p.speed,2.8f)*Time.deltaTime));}
    static IEnumerator Press(Key key){yield return Hold(key,.12f);}
    static IEnumerator Hold(Key key,float seconds)
    {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));try{yield return new WaitForSeconds(seconds);}finally{if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());}yield return new WaitForSeconds(.15f);}
    static IEnumerator Wait(Func<bool> predicate,float seconds,string label)
    {float start=Time.realtimeSinceStartup;while(!predicate()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("timeout "+label);Progress(label);yield return null;}}
    static Vector3 V(float x,float z)=>new Vector3(x,0,z);
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void ResumeTracking(){if(!tracked)return;lastTado=tracked.school.tado.transform.position;lastSample=Time.time;tracking=true;}
    public static void ObserveFrame()
    {
        ObserveOfficerFrame(); ObserveGuidanceFrame();
        if(report==null||!tracked)return;var b=tracked.school.Battle;var e=b.Rescue?b.Rescue.Ending:null;if(e&&e.Active)endingStages.Add(e.CurrentStage);
        if(!tracking)return;var pos=tracked.school.tado.transform.position;float distance=Flat(pos,lastTado),dt=Time.time-lastSample;
        if(distance>Mathf.Max(.4f,dt*8)){jumps++;report.observations.Add("Tado discontinuity "+lastTado+" -> "+pos+" step="+b.Step);}
        travel+=distance;samples++;lastTado=pos;lastSample=Time.time;
    }
    static void Check(string label,bool value){report.checks.Add(label+"="+value);if(!value)throw new Exception(label);}
    static void Progress(string label)
    {
        if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+1;
        File.WriteAllText(Dir+"progress.txt",report.mode+" "+label+"\n"+DiagnosticState(Object.FindFirstObjectByType<Chapter3Flow>()));
    }
    static string DiagnosticState(Chapter3Flow f)
    {
        var b=f&&f.school?f.school.Battle:null;if(!b)return "battle unavailable";
        string guide=guideIndexField?.GetValue(b)?.ToString()??"unavailable",straggler=stragglerIndexField?.GetValue(b)?.ToString()??"unavailable";
        var p=f.player.transform.position;var t=f.school.tado.transform.position;var ally=b.StragglerPosition;
        var near=Physics.OverlapSphere(p+Vector3.up*.8f,1.2f,~0,QueryTriggerInteraction.Ignore)
            .Where(c=>c&&!c.transform.IsChildOf(f.player.transform)&&c.bounds.max.y>.28f)
            .Select(c=>c.name+"@"+c.transform.position.ToString("F2")).Distinct();
        return "stage="+b.CurrentStage+" step="+b.Step+" checkpoint="+b.CheckpointStep+" health="+b.Health+" player="+p.ToString("F3")+" Tado="+t.ToString("F3")+" StragglerPosition="+ally.ToString("F3")+" guideIndex="+guide+" stragglerIndex="+straggler+" called="+b.StragglerCalled+" arrived="+b.StragglerArrived+" playerTadoGap="+Flat(p,t).ToString("F3")+" tadoStragglerGap="+Flat(t,ally).ToString("F3")+" nearPlayerColliders=["+string.Join(";",near)+"]";
    }
    static void Shot(string label)
    {
        var cam=Camera.main;if(!cam)throw new Exception("No main camera for rendered screenshot");RenderShot(cam,label);
    }
    static void ShotAt(string label,Vector3 position,Vector3 target)
    {
        // Diagnostic view only: no main-camera transform/FOV, scene asset or
        // player state changes. Destroy this disabled camera after one render.
        var source=Camera.main;if(!source)throw new Exception("No source camera for diagnostic view");
        var go=new GameObject("Temporary assault diagnostic camera"){hideFlags=HideFlags.HideAndDontSave};
        try
        {
            var cam=go.AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;go.transform.position=position;go.transform.LookAt(target);
            RenderShot(cam,label);report.observations.Add(label+" is a temporary diagnostic camera view; not the player's normal view.");
        }
        finally{Object.DestroyImmediate(go);}
    }
    static void RenderShot(Camera cam,string label)
    {
        var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
        var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+report.mode+"-"+label+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void BackupSave(string mode)
    {
        if(SessionState.GetBool(State+".savePending",false))return;
        var data=new SaveBackup{capturedUtc=DateTime.UtcNow.ToString("O"),items=new[]{Chapter3ResultStore.SaveKey,SecondKey}.Select(k=>new SaveItem{key=k,existed=PlayerPrefs.HasKey(k),raw=PlayerPrefs.GetString(k,"")}).ToArray()};
        string json=JsonUtility.ToJson(data,true);File.WriteAllText(Dir+"save-backup-"+mode+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json",json);
        SessionState.SetString(State+".save",json);SessionState.SetBool(State+".savePending",true);
    }
    static bool OriginalSaveMatches()
    {var data=JsonUtility.FromJson<SaveBackup>(SessionState.GetString(State+".save",""));return data!=null&&data.items.All(i=>PlayerPrefs.HasKey(i.key)==i.existed&&(!i.existed||PlayerPrefs.GetString(i.key,"")==i.raw));}
    static void RestoreSave()
    {
        if(!SessionState.GetBool(State+".savePending",false))return;var data=JsonUtility.FromJson<SaveBackup>(SessionState.GetString(State+".save",""));
        if(data?.items==null)throw new Exception("Missing original PlayerPrefs backup; refusing ambiguous cleanup");
        foreach(var i in data.items){if(i.existed)PlayerPrefs.SetString(i.key,i.raw);else PlayerPrefs.DeleteKey(i.key);}PlayerPrefs.Save();
        if(!OriginalSaveMatches())throw new Exception("Original PlayerPrefs did not restore exactly");SessionState.SetBool(State+".savePending",false);
    }
    static void Cleanup()
    {
        if(Mouse.current!=null&&testInput)InputSystem.QueueStateEvent(Mouse.current,new MouseState()); if(Keyboard.current!=null&&testInput)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        if(originalInput)InputSystem.settings=originalInput;if(testInput)Object.DestroyImmediate(testInput);originalInput=testInput=null;
        if(addedKeyboard!=null){InputSystem.RemoveDevice(addedKeyboard);addedKeyboard=null;}RestoreSave();
    }
    static void Finish()
    {
        if(report==null)return;tracking=false;
        try{Cleanup();Check("original PlayerPrefs restored exactly",OriginalSaveMatches());}catch(Exception e){report.completed=false;report.errors.Add(e.ToString());}
        report.elapsed=Time.realtimeSinceStartup-began;report.completed=report.completed&&report.errors.Count==0;
        File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(State+".mode");report=null;
        Application.runInBackground=backgroundBefore;EditorApplication.isPlaying=false;
    }
    static void Log(string message,string trace,LogType type)
    {if(report==null)return;if(type==LogType.Warning)report.warnings.Add(message);if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+trace);}
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),"missingMeshes="+transforms.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(m=>!m.sharedMesh),"missingSkinnedMeshes="+transforms.SelectMany(t=>t.GetComponents<SkinnedMeshRenderer>()).Count(m=>!m.sharedMesh)});
    }
}
public sealed class Chapter3ScatterProbeDriver:MonoBehaviour
{
    IEnumerator Start(){yield return Chapter3ScatterProbe.Run();}
    void LateUpdate(){Chapter3ScatterProbe.ObserveFrame();}
}







