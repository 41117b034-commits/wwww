using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2Verification
{
    const string Dir="_CodexBackups/chapter2_logged_council_20261007/";
    static Chapter2Controller c;
    static string label;
    static double began;
    static float stageAt,lastSwing,shotAt=-1;
    static int lastStage=-1,badCuts,lastImpact;
    static bool finished,limitsChecked,guardsChecked,retrySeen,capturingFaces;
    static Result result;
    static HashSet<string> shots=new HashSet<string>();
    static List<string> errors=new List<string>();
    static bool arrivalTested;static float arrivalHold=-1;
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
            arrivalTested=false;arrivalHold=-1;
            c.StageChanged+=stage=>{if(stage==Chapter2Controller.Stage.Complete){Save();finished=true;}};Time.timeScale=2;
        }
        if(s==PlayModeStateChange.EnteredEditMode&&!string.IsNullOrEmpty(label))
        {File.WriteAllText(Dir+label+"-returned-to-edit.json","{\"playing\":"+Application.isPlaying.ToString().ToLowerInvariant()+",\"completed\":"+finished.ToString().ToLowerInvariant()+"}");SessionState.EraseString("Chapter2PerformanceTest");label=null;c=null;Time.timeScale=1;}
    }
    static void Log(string message,string trace,LogType type){if(c&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(message+"\n"+trace);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!c||string.IsNullOrEmpty(label)||capturingFaces)return;
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
            if(c.routeGuide.EscortArrived&&!arrivalTested)
            {
                Vector3 before=c.player.transform.position;
                c.player.Warp(c.treeApproach.position+new Vector3(0,.08f,-1.51f),c.sacredTree.position);result.outsideArrivalBlocked=!c.ArrivedAtTree;
                c.player.Warp(c.treeApproach.position+new Vector3(0,.08f,-1.5f),c.sacredTree.position);result.exactBoundaryBlocked=!c.ArrivedAtTree;
                c.player.Warp(c.treeApproach.position+new Vector3(0,.08f,-1.49f),c.sacredTree.position);result.insideArrivalAccepted=c.ArrivedAtTree;
                c.player.Warp(c.treeApproach.position+new Vector3(0,.08f,-1.51f),c.sacredTree.position);
                arrivalTested=true;arrivalHold=Time.time;
            }
            if(arrivalHold>=0)
            {
                if(Time.time-arrivalHold<.6f)return;
                Shot("arrival-just-outside");
                c.player.Move(Vector3.forward*.02f);arrivalHold=-2;
                result.arrivalTriggerDistance=c.TreeDistance;
                return;
            }
            if(arrivalHold==-2)return;
            Vector3 goal=c.treeApproach.position-(c.routeGuide.EscortArrived?Vector3.zero:Vector3.forward*1.5f);
            Vector3 delta=goal-c.player.transform.position;delta.y=0;
            if(delta.magnitude>.12f)c.player.Move(delta.normalized*Mathf.Min(c.player.speed*Time.deltaTime,delta.magnitude));
        }
        if(c.CameraBeat=="police-speaking"&&c.officer.speaking)
        {result.policeFullBody=InFrame(c.officer);Shot("police-full-body");result.newPoliceLine=c.ui.subtitle.text=="把這些樹都砍了。";}
        if(c.CameraBeat=="villager-speaking"&&c.workers[0].speaking)
        {result.villagerFullBody=InFrame(c.workers[0]);Shot("villager-full-body");}
        if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice&&Time.time-stageAt>1)
        {Shot("tree-choice");if(label.Contains("protect"))c.ui.buttonA.onClick.Invoke();else c.ui.buttonB.onClick.Invoke();}
        var rifle=c.officer.GetComponent<Chapter2Rifle>();
        if(c.CurrentStage==Chapter2Controller.Stage.Consequence&&c.TreeDecision==0)
        {
            Time.timeScale=1;
            CheckProtectSequence(rifle);
            if(rifle.aim>.99f&&rifle.Shots==0)
            {
                result.blockDistance=Vector3.ProjectOnPlane(c.workers[0].transform.position-c.officer.transform.position,Vector3.up).magnitude;result.rightGripError=rifle.RightGripError;result.leftGripError=rifle.LeftGripError;result.aimError=rifle.AimError;
                result.shooterVisible=InFrame(c.officer);result.victimVisible=InFrame(c.workers[0]);Shot("rifle-aim");
            }
            if(c.CameraBeat=="blocking")Shot("approach-police");
            if(rifle.Shots>0)
            {
                Time.timeScale=1;
                if(shotAt<0)shotAt=Time.time;
                if(result.captionDelay<0&&c.ui.subtitle.text.StartsWith("槍聲過後")){result.captionDelay=Time.time-shotAt;Shot("early-shot-caption");}
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
        if(c.CurrentStage==Chapter2Controller.Stage.Consequence&&c.TreeDecision==1)
        {
            Time.timeScale=1;
            var reactions=UnityEngine.Object.FindObjectsByType<Chapter2StartleReaction>(FindObjectsSortMode.None);
            if(c.CameraBeat=="tree-warning")
            {
                Shot("tree-warning");
                if(reactions.Length==3&&System.Array.TrueForAll(reactions,r=>r.FearWeight>.95f))
                {
                    Shot("startled-retreat");
                    if(label.Contains("faces")&&shots.Add("fear-closeups"))c.StartCoroutine(CaptureFearFaces());
                }
            }
            if(reactions.Length==3)
            {
                result.retreatSteps=new int[3];result.retreatDistances=new float[3];
                for(int i=0;i<3;i++){var r=c.workers[i].GetComponent<Chapter2StartleReaction>();result.retreatSteps[i]=r.StepsCompleted;result.retreatDistances[i]=r.RetreatDistance;}
                if(c.CameraBeat=="tree-warning")Shot("retreat-step-"+result.retreatSteps[0]);
                if(result.retreatSteps[0]==3&&result.retreatSteps[1]==3&&result.retreatSteps[2]==3)Shot("retreat-complete");
            }
            if(c.TreeFallProgress>.15f)Shot("tree-falling-start");
            if(c.TreeFallProgress>.55f)Shot("tree-falling-middle");
            if(c.TreeFallProgress>=1)
            {
                Shot("tree-fallen-toward-player");
                result.fallDirectionDot=Vector3.Dot(Vector3.ProjectOnPlane(c.sacredTree.up,Vector3.up).normalized,c.TreeFallDirection);
                result.playerDirectionDot=Vector3.Dot(Vector3.ProjectOnPlane(c.player.transform.position-c.sacredTree.position,Vector3.up).normalized,c.TreeFallDirection);
            }
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
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting){Time.timeScale=1;if(Time.time-stageAt>8){Shot("meeting");result.nightPipelineRestored=!UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name.Contains("runtime");result.nightTreeCleared=c.GetComponent<Chapter2LoggedForest>().Applied&&Mathf.Abs(Vector3.Dot(c.sacredTree.up,Vector3.up))<.01f;}}
        if(c.CurrentStage==Chapter2Controller.Stage.Vote&&Time.time-stageAt>1){if(label.Contains("refuse"))c.ui.buttonB.onClick.Invoke();else c.ui.buttonA.onClick.Invoke();}
        if(c.CurrentStage==Chapter2Controller.Stage.Ending&&c.ui.endingPanel.activeSelf)Shot("black-ending");
    }
    static IEnumerator CaptureFearFaces()
    {
        capturingFaces=true;float timeScale=Time.timeScale;Time.timeScale=0;
        var camera=c.player.view;var cp=camera.transform.position;var cr=camera.transform.rotation;var fov=camera.fieldOfView;bool hud=c.ui.canvas.enabled;c.ui.canvas.enabled=false;
        foreach(var actor in c.workers){actor.enabled=false;actor.Rig.enabled=false;actor.GetComponent<Chapter2StartleReaction>().enabled=false;}
        string report="";
        for(int i=0;i<c.workers.Length;i++)
        {
            var rig=c.workers[i].Rig;camera.transform.position=rig.Head.position+rig.Forward*1.1f+Vector3.up*.20f;camera.transform.LookAt(rig.Head.position+Vector3.up*.03f);camera.fieldOfView=36;
            var skins=c.workers[i].GetComponentsInChildren<SkinnedMeshRenderer>();var weights=new float[skins.Length];
            for(int j=0;j<skins.Length;j++){int shape=skins[j].sharedMesh.GetBlendShapeIndex("Chapter2 fear");if(shape>=0){weights[j]=skins[j].GetBlendShapeWeight(shape);skins[j].SetBlendShapeWeight(shape,0);}}
            yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
            Chapter2WorkProbe.Shot(label+"-neutral-face-"+i);
            for(int j=0;j<skins.Length;j++){int shape=skins[j].sharedMesh.GetBlendShapeIndex("Chapter2 fear");if(shape>=0)skins[j].SetBlendShapeWeight(shape,weights[j]);report+=i+" mesh="+skins[j].sharedMesh.name+" fearShape="+shape+" weight="+weights[j]+"\n";}
            yield return null;yield return new WaitForEndOfFrame();
            Chapter2WorkProbe.Shot(label+"-fear-face-"+i);
        }
        File.WriteAllText(Dir+label+"-fear-faces.txt",report);
        camera.transform.SetPositionAndRotation(cp,cr);camera.fieldOfView=fov;c.ui.canvas.enabled=hud;
        foreach(var actor in c.workers){actor.enabled=true;actor.Rig.enabled=true;actor.GetComponent<Chapter2StartleReaction>().enabled=true;}
        Time.timeScale=timeScale;capturingFaces=false;
    }
    static bool InFrame(Chapter2Actor actor)
    {
        var head=c.player.view.WorldToViewportPoint(actor.Rig.Head.position+Vector3.up*.16f);
        var feet=c.player.view.WorldToViewportPoint(actor.transform.position+Vector3.up*.02f);
        return head.z>0&&head.x>.06f&&head.x<.94f&&head.y<.83f&&feet.y>.23f&&feet.x>.06f&&feet.x<.94f;
    }
    static void CheckProtectSequence(Chapter2Rifle rifle)
    {
        var block=c.workers[0].GetComponent<Chapter2BlockingPose>();
        if(block&&block.Weight>.99f&&!c.workers[0].fallen)
        {
            result.bothHandsRaised=c.workers[0].Rig.LeftHand.position.y<c.workers[0].Rig.Head.position.y-.1f&&c.workers[0].Rig.RightHand.position.y<c.workers[0].Rig.Head.position.y-.1f;
            result.handSeparation=Vector3.Distance(c.workers[0].Rig.LeftHand.position,c.workers[0].Rig.RightHand.position);
            Shot("blocking-both-hands");
            if(shots.Add("blocking-bones"))File.WriteAllText(Dir+label+"-blocking-bones.txt","left="+c.workers[0].Rig.LeftHand.position+" right="+c.workers[0].Rig.RightHand.position+" head="+c.workers[0].Rig.Head.position+" mesh="+c.workers[0].GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name);
        }
        if(rifle.Shots<=0)return;
        float since=shotAt<0?0:Time.time-shotAt;
        if(rifle.aim==0&&rifle.lowered==1&&result.loweredAt<0){result.loweredAt=since;result.loweredMuzzleY=rifle.weapon.forward.y;Shot("rifle-lowered-immediately");}
        var a=c.workers[1].GetComponent<Chapter2GriefReaction>();var b=c.workers[2].GetComponent<Chapter2GriefReaction>();
        if(a.RunProgress>0&&result.rescueStartFallProgress<0)result.rescueStartFallProgress=c.workers[0].FallProgress;
        if(c.CameraBeat=="checking-casualty"){result.checkedBeforeOrder=a.Examining&&b.Examining;Shot("checking-before-order");}
        if(c.CameraBeat=="forced-order")
        {
            result.orderSubtitle=c.ui.subtitle.text;result.orderWhileCrouched=!a.Standing&&!b.Standing;result.orderAim=rifle.aim;
            if(!shots.Contains("order-first")){Shot("order-first");result.orderAt=since;}
            if(since-result.orderAt>2.4f)Shot("order-second");
        }
        if(c.CameraBeat=="survivors-standing")
        {
            Shot("standing-up");
            if(result.standAt<0)result.standAt=since;
            if(since-result.standAt>.65f)Shot("standing-middle");
        }
        if(c.CameraBeat.StartsWith("survivors-short-walk"))
        {
            result.stoodBeforeWalking=a.Standing&&b.Standing;
            if(result.walkAt<0)result.walkAt=since;
            foreach(float moment in new[]{.4f,1.1f,1.7f,2.1f})if(since-result.walkAt>moment)Shot("short-walk-"+moment.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
            var walks=new[]{c.workers[1].GetComponent<Chapter2ReluctantWalk>(),c.workers[2].GetComponent<Chapter2ReluctantWalk>()};
            if(c.ui.fade.color.a>.01f&&result.fadeAt<0)
            {
                result.fadeAt=since;result.stepsAtFade=new[]{walks[0].StepsCompleted,walks[1].StepsCompleted};
                result.distanceAtFade=new[]{walks[0].DistanceTravelled,walks[1].DistanceTravelled};Shot("fade-after-two-steps");
            }
            result.finalSteps=new[]{walks[0].StepsCompleted,walks[1].StepsCompleted};
            result.finalWalkDistances=new[]{walks[0].DistanceTravelled,walks[1].DistanceTravelled};
            if(c.ui.fade.color.a>.2f)Shot("third-step-in-fade");
            File.AppendAllText(Dir+label+"-short-walk.csv",Time.time+","+c.ui.fade.color.a+","+walks[0].StepsCompleted+","+walks[1].StepsCompleted+","+walks[0].DistanceTravelled+","+walks[1].DistanceTravelled+"\n");
        }
        File.AppendAllText(Dir+label+"-sequence.csv",Time.time+","+c.CameraBeat+","+rifle.aim+","+rifle.lowered+","+rifle.weapon.forward.y+","+a.Standing+","+b.Standing+","+c.ui.fade.color.a+"\n");
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
        public float handSeparation,standAt=-1,fadeAt=-1;public int[] stepsAtFade,finalSteps;public float[] distanceAtFade,finalWalkDistances;
        public bool outsideArrivalBlocked,exactBoundaryBlocked,insideArrivalAccepted,bothHandsRaised,checkedBeforeOrder,orderWhileCrouched,stoodBeforeWalking,returnedToTrees,fadedAfterReturn;
        public float arrivalTriggerDistance,loweredAt=-1,loweredMuzzleY,rescueStartFallProgress=-1,orderAim,orderAt,walkAt=-1;public string orderSubtitle;
        public bool nightTreeCleared;public bool newPoliceLine;public float blockDistance,captionDelay=-1,fallDirectionDot,playerDirectionDot;public int[] retreatSteps;public float[] retreatDistances;public bool rescueArrived,nightPipelineRestored;public float witnessSeparation=100;public string label,distanceLabel;public bool completed,introFacesPlayer,introStandsStill,leftLimit,rightLimit,frontLimit,guideVisibleFromRight,policeFullBody,villagerFullBody,shooterVisible,victimVisible,fallComplete,remoteChopBlocked,backwardChopBlocked,repeatChopBlocked,retry;
        public float chopDistance,maxTipError,rightGripError,leftGripError,aimError,fallGround;public int witnessReactions;public int tree,vote,cuts,failedCuts,impacts,shots;public string[] errors;
    }
}



