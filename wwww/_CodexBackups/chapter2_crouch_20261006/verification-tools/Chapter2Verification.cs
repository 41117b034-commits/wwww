using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2Verification
{
    const string Dir="_CodexBackups/chapter2_crouch_20261006/";
    static Chapter2Controller c;
    static string label;
    static double began;
    static float stageAt,lastSwing,shotAt=-1;
    static int lastStage=-1,badCuts,lastImpact;
    static bool finished,limitsChecked,guardsChecked,retrySeen;
    static Result result;
    static HashSet<string> shots=new HashSet<string>();
    static List<string> errors=new List<string>();
    static Chapter2Verification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    public static void Run(string mode){if(EditorApplication.isPlaying)throw new Exception("Stop playback first.");SessionState.SetString("Chapter2PerformanceTest",mode);EditorApplication.isPlaying=true;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void BeforeStart()
    {if(SessionState.GetString("Chapter2PerformanceTest","")!=""){var controller=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();controller.saveResult=false;}}
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString("Chapter2PerformanceTest","");if(label=="")return;
            c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();began=EditorApplication.timeSinceStartup;lastStage=-1;badCuts=lastImpact=0;lastSwing=-10;
            shotAt=-1;finished=limitsChecked=guardsChecked=retrySeen=false;shots.Clear();errors.Clear();result=new Result{label=label};
            c.StageChanged+=stage=>{if(stage==Chapter2Controller.Stage.Complete){Save();finished=true;}};Time.timeScale=2;
        }
        if(s==PlayModeStateChange.EnteredEditMode&&!string.IsNullOrEmpty(label))
        {File.WriteAllText(Dir+label+"-returned-to-edit.json","{\"playing\":"+Application.isPlaying.ToString().ToLowerInvariant()+",\"completed\":"+finished.ToString().ToLowerInvariant()+"}");SessionState.EraseString("Chapter2PerformanceTest");label=null;c=null;Time.timeScale=1;}
    }
    static void Log(string message,string trace,LogType type){if(c&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(message+"\n"+trace);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!c||string.IsNullOrEmpty(label))return;
        if(EditorApplication.timeSinceStartup-began>300){errors.Add("Timeout: "+c.CurrentStage);Save();EditorApplication.isPlaying=false;return;}
        if((int)c.CurrentStage!=lastStage){lastStage=(int)c.CurrentStage;stageAt=Time.time;}
        if(c.CurrentStage==Chapter2Controller.Stage.Intro){c.SkipIntro();return;}
        if(c.Introducing)
        {
            if(c.CameraBeat=="introduction"&&c.workers[0].speaking&&!c.workers[0].Rig.pointTarget)
            {
                var direction=Vector3.ProjectOnPlane(c.player.transform.position-c.workers[0].transform.position,Vector3.up).normalized;
                result.introFacesPlayer=Vector3.Dot(direction,c.workers[0].Rig.Forward)>.95f;
                result.introStandsStill=!c.workers[0].Rig.walking&&!c.player.canMove;
                Shot("introduction-facing-player");
            }
            if(c.workers[0].Rig.pointTarget&&c.workers[0].speaking)Shot("turn-and-invite");
            return;
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Follow&&c.player.canMove)
        {
            if(!limitsChecked)
            {
                Vector3 start=c.player.transform.position;
                c.player.Move(new Vector3(-100,0,0));result.leftLimit=c.player.transform.position.x>=-3.201f;c.player.Warp(start,c.workers[0].transform.position);
                c.player.Move(new Vector3(100,0,0));result.rightLimit=c.player.transform.position.x<=.651f;
                result.guideVisibleFromRight=InFrame(c.workers[0]);Shot("right-boundary");
                c.player.Warp(start,c.workers[0].transform.position);
                c.player.Move(new Vector3(0,0,100));result.frontLimit=c.player.transform.position.z<=c.workers[0].transform.position.z+1.11f;c.player.Warp(start,c.workers[0].transform.position);
                limitsChecked=true;
            }
            Shot("follow-distance");
            if(!shots.Contains("quality-details"))
            {
                shots.Add("quality-details");var ground=GameObject.Find("Forest ground").GetComponent<Renderer>();var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                var data=c.player.view.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                File.WriteAllText(Dir+label+"-quality.txt","pipeline="+pipeline.name+" renderScale="+pipeline.renderScale+" shadowMap="+pipeline.mainLightShadowmapResolution+" AA="+data.antialiasing+" postProcessing="+data.renderPostProcessing+" ground="+ground.sharedMaterial.name+" map="+ground.sharedMaterial.mainTexture.width+" tangents="+ground.GetComponent<MeshFilter>().sharedMesh.tangents.Length+" plants="+c.dayGroup.transform.Find("Pathside undergrowth")?.childCount);
            }
            if(label.Contains("baseline")){EditorApplication.isPlaying=false;return;}
            result.distanceLabel=c.routeGuide.distanceLabel.text;
            Vector3 goal=c.treeApproach.position-(c.routeGuide.EscortArrived?Vector3.zero:Vector3.forward*1.5f);
            Vector3 delta=goal-c.player.transform.position;delta.y=0;
            if(delta.magnitude>.12f)c.player.Move(delta.normalized*Mathf.Min(c.player.speed*Time.deltaTime,delta.magnitude));
        }
        if(c.CameraBeat=="police-speaking"&&c.officer.speaking)
        {result.policeFullBody=InFrame(c.officer);Shot("police-full-body");}
        if(c.CameraBeat=="villager-speaking"&&c.workers[0].speaking)
        {result.villagerFullBody=InFrame(c.workers[0]);Shot("villager-full-body");}
        if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice&&Time.time-stageAt>1)
        {Shot("tree-choice");if(label.Contains("protect"))c.ui.buttonA.onClick.Invoke();else c.ui.buttonB.onClick.Invoke();}
        var rifle=c.officer.GetComponent<Chapter2Rifle>();
        if(c.CurrentStage==Chapter2Controller.Stage.Consequence&&c.TreeDecision==0)
        {
            if(rifle.aim>.99f&&rifle.Shots==0)
            {
                result.rightGripError=rifle.RightGripError;result.leftGripError=rifle.LeftGripError;result.aimError=rifle.AimError;
                result.shooterVisible=InFrame(c.officer);result.victimVisible=InFrame(c.workers[0]);Shot("rifle-aim");
            }
            if(rifle.Shots>0)
            {
                Time.timeScale=1;
                if(shotAt<0)shotAt=Time.time;
                foreach(float moment in new[]{.2f,.6f,1f,1.4f,1.8f,2.4f,3f,4f,6f})if(Time.time-shotAt>=moment)Shot("rescue-"+moment.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
                result.rescueArrived=c.workers[1].GetComponent<Chapter2GriefReaction>().Arrived&&c.workers[2].GetComponent<Chapter2GriefReaction>().Arrived;
                result.witnessSeparation=Mathf.Min(result.witnessSeparation,Vector3.Distance(c.workers[1].transform.position,c.workers[2].transform.position));
                File.AppendAllText(Dir+label+"-rescue-motion.csv",Time.time+","+c.workers[1].transform.position.x+","+c.workers[1].transform.position.z+","+c.workers[2].transform.position.x+","+c.workers[2].transform.position.z+"\n");
                var reaction=c.workers[1].GetComponent<Chapter2GriefReaction>();
                if(reaction && reaction.TurnProgress>.35f && reaction.TurnProgress<.7f)Shot("witness-turning");
                if(reaction && reaction.Arrived && c.workers[2].GetComponent<Chapter2GriefReaction>().Arrived && Time.time-shotAt>3.4f)
                {
                    Shot("witness-grief-wide");
                    if(shots.Add("grief-details"))
                    {
                        var report="";
                        for(int i=1;i<c.workers.Length;i++)
                        {
                            var actor=c.workers[i];var r=actor.GetComponent<Chapter2GriefReaction>();
                            var to=Vector3.ProjectOnPlane(c.workers[0].transform.position-actor.transform.position,Vector3.up).normalized;
                            report+=actor.name+" height="+actor.Rig.Height+" hips="+actor.Rig.Hips.position+" head="+actor.Rig.Head.position+" turn="+r.TurnProgress+" fists="+r.FistWeight+" facingDot="+Vector3.Dot(to,actor.Rig.Forward)+" position="+actor.transform.position+"\n";
                            foreach(var group in new[]{"thighs","knees","feet","arms","elbows","hands"})
                            {
                                var joints=(Transform[])typeof(Chapter2GriefReaction).GetField(group,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(r);
                                for(int j=0;j<joints.Length;j++)if(joints[j])report+=group+j+"="+joints[j].position+"\n";
                            }
                            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                report+=skin.name+" bones="+skin.bones.Length+" shapes="+skin.sharedMesh.blendShapeCount+"\n";
                                for(int b=0;b<skin.sharedMesh.blendShapeCount;b++)report+=skin.sharedMesh.GetBlendShapeName(b)+"="+skin.GetBlendShapeWeight(b)+"\n";
                            }
                            var view=c.player.view.transform;var pos=view.position;var rot=view.rotation;
                            view.position=actor.transform.position+actor.Rig.Forward*2.7f+Vector3.Cross(Vector3.up,actor.Rig.Forward)*.65f+Vector3.up*1.25f;
                            view.LookAt(actor.transform.position+Vector3.up*.9f);
                            Chapter2WorkProbe.Shot(label+"-witness-"+i+"-detail");view.SetPositionAndRotation(pos,rot);
                        }
                        File.WriteAllText(Dir+label+"-grief-metrics.txt",report);
                        var camera=c.player.view;var cp=camera.transform.position;var cr=camera.transform.rotation;var fov=camera.fieldOfView;
                        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var enabled=new List<Canvas>();
                        foreach(var canvas in canvases)if(canvas.enabled){enabled.Add(canvas);canvas.enabled=false;}
                        Vector3 center=(c.workers[0].transform.position+c.workers[1].transform.position+c.workers[2].transform.position)/3+Vector3.up*.4f;
                        camera.transform.position=center+(cp-center).normalized*3.6f;camera.transform.LookAt(center);camera.fieldOfView=43;
                        Chapter2WorkProbe.Shot(label+"-crouch-closeup");
                        camera.transform.SetPositionAndRotation(cp,cr);camera.fieldOfView=fov;foreach(var canvas in enabled)canvas.enabled=true;
                    }
                }
            }
            if(rifle.muzzleFlash.activeSelf)Shot("rifle-fire"); if(rifle.Shots>0 && rifle.aim==0)Shot("held-grief-after-lowering");
            if(c.workers[0].FallProgress>.3f&&c.workers[0].FallProgress<.65f)Shot("villager-falling");
            if(c.workers[0].FallProgress>=1)
            {result.fallComplete=true;result.fallGround=c.workers[0].FallenLowestPoint;result.shots=rifle.Shots;Shot("villager-on-ground");}
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Chopping)
        {
            var axe=c.axe.GetComponent<Chapter2Axe>();
            if(!guardsChecked&&c.CameraBeat=="chopping")
            {
                var start=c.player.transform.position;c.player.Warp(new Vector3(-3.8f,.08f,9),c.sacredTree.position);result.remoteChopBlocked=!c.TryChop();c.player.Warp(start,c.sacredTree.position);
                c.player.view.transform.rotation=Quaternion.LookRotation(Vector3.back);result.backwardChopBlocked=!c.TryChop();
                if(c.TryGetChopContact(out RaycastHit hit)){result.chopDistance=hit.distance;c.player.FocusOn(hit.point);}
                guardsChecked=true;Shot("axe-ready");
            }
            if(axe.Impacts>lastImpact){lastImpact=axe.Impacts;result.maxTipError=Mathf.Max(result.maxTipError,axe.LastContactError);Shot("axe-impact");}
            if(axe.Impacts>0&&!axe.IsSwinging)Shot("axe-rest");
            if(badCuts==5&&c.Integrity==100&&c.ValidCuts==0)retrySeen=true;
            bool bad=label.Contains("retry")&&badCuts<5;
            if(!axe.IsSwinging&&Time.time-lastSwing>1.05f&&(bad?c.RhythmPhase<.14f:c.RhythmPhase>.43f&&c.RhythmPhase<.57f))
            {if(c.TryChop()){lastSwing=Time.time;if(bad)badCuts++;result.repeatChopBlocked=!c.TryChop();}}
            if(axe.IsSwinging&&Time.time-lastSwing>.08f&&Time.time-lastSwing<.2f)Shot("axe-backswing");
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting){Time.timeScale=3;if(Time.time-stageAt>8){Shot("meeting");result.nightPipelineRestored=!UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name.Contains("runtime");}}
        if(c.CurrentStage==Chapter2Controller.Stage.Vote&&Time.time-stageAt>1){if(label.Contains("refuse"))c.ui.buttonB.onClick.Invoke();else c.ui.buttonA.onClick.Invoke();}
        if(c.CurrentStage==Chapter2Controller.Stage.Ending&&c.ui.endingPanel.activeSelf)Shot("black-ending");
    }
    static bool InFrame(Chapter2Actor actor)
    {
        var head=c.player.view.WorldToViewportPoint(actor.Rig.Head.position+Vector3.up*.16f);
        var feet=c.player.view.WorldToViewportPoint(actor.transform.position+Vector3.up*.02f);
        return head.z>0&&head.x>.06f&&head.x<.94f&&head.y<.83f&&feet.y>.23f&&feet.x>.06f&&feet.x<.94f;
    }
    static void Shot(string name){if(shots.Add(name))Chapter2WorkProbe.Shot(label+"-"+name);}
    static void Save()
    {
        result.completed=c.Completed;result.tree=c.TreeDecision;result.vote=c.MeetingDecision;result.cuts=c.ValidCuts;result.failedCuts=c.FailedCuts;result.retry=retrySeen;
        result.witnessReactions=UnityEngine.Object.FindObjectsByType<Chapter2GriefReaction>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;var axe=c.axe.GetComponent<Chapter2Axe>();result.impacts=axe.Impacts;result.errors=errors.ToArray();
        File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(result,true));
    }
    [Serializable]class Result
    {
        public bool rescueArrived,nightPipelineRestored;public float witnessSeparation=100;public string label,distanceLabel;public bool completed,introFacesPlayer,introStandsStill,leftLimit,rightLimit,frontLimit,guideVisibleFromRight,policeFullBody,villagerFullBody,shooterVisible,victimVisible,fallComplete,remoteChopBlocked,backwardChopBlocked,repeatChopBlocked,retry;
        public float chopDistance,maxTipError,rightGripError,leftGripError,aimError,fallGround;public int witnessReactions;public int tree,vote,cuts,failedCuts,impacts,shots;public string[] errors;
    }
}



