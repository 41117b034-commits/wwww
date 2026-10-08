using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2CouncilRevisionProbe
{
    public const string Dir="_CodexBackups/chapter2_council_pose_20261008/";
    static Chapter2Controller c;
    static string label,line;
    static float lineAt,started;
    static bool sent,shot,release,overview,finished;
    const string Session="CouncilRevision",ResultKey="WusheEvent.Chapter2.Result";
    static RunReport report;
    static int sampledFrame=-1;
    static readonly Dictionary<string,Vector3> previousFingers=new Dictionary<string,Vector3>();
    static readonly Dictionary<string,Vector3> previousNormals=new Dictionary<string,Vector3>();
    static readonly Dictionary<string,bool> previousSpeaking=new Dictionary<string,bool>();
    static readonly Dictionary<string,float> previousTimes=new Dictionary<string,float>();
    [Serializable] public sealed class Check
    {
        public string name,detail;public bool passed;
        public Check(string n,bool p,string d){name=n;passed=p;detail=d;}
    }
    [Serializable] public sealed class WristMetrics
    {
        public string side;public int samples,speakingSamples,continuousPairs;
        public float maximumFingerForearmAngle,maximumPalmNormalError,maximumContinuousFingerStep,maximumContinuousNormalStep,maximumAnyNormalStep;
        public WristMetrics(string s){side=s;}
    }
    [Serializable] public sealed class ActorMetrics
    {
        public string name;public Vector3 localScale;
        public float rigHeight,shoulderWidth,hipWidth,torsoLength,leftLegLength,rightLegLength;
        public float shoulderWidthToHeight,torsoToHeight,legToHeight,heightToOtherLeadersMean,shoulderRatioToOtherLeadersMean;
        public bool seated;
    }
    [Serializable] public sealed class RunReport
    {
        public string label,branch,startedUtc,endedUtc,finalStage,playerPrefsBefore,playerPrefsAfter;
        public bool completed,playingAfterExit,timedOut,sceneDirtyAfterExit,playerPrefsExistedBefore,playerPrefsExistedAfter,playerPrefsUnchanged;
        public bool rallyObserved,standingTogetherObserved,monaStoodDuringRally,leadersStayedSeatedDuringMona=true,allLeadersStood,refusalLeadersStayedSeated=true;
        public int errors,warnings,leaderCount,monaRallySamples,settledRallySamples,endingSamples;
        public float elapsedSeconds,maximumFistWeight,minimumSettledFistWeight=100,minimumSettledElbowAngle=180,maximumSettledElbowAngle;
        public bool passed;
        public WristMetrics leftWrist=new WristMetrics("left"),rightWrist=new WristMetrics("right");
        public List<ActorMetrics> actors=new List<ActorMetrics>();
        public List<Check> checks=new List<Check>();
    }
    static readonly System.Collections.Generic.HashSet<string> shots=new System.Collections.Generic.HashSet<string>();
    static Chapter2CouncilRevisionProbe(){Directory.CreateDirectory(Dir);EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string trace,LogType t)
    {
        try
        {
            string entry=DateTime.UtcNow.ToString("o")+" "+t+" "+m+"\n"+(t==LogType.Exception?trace+"\n":"");
            File.AppendAllText(Dir+"unity-log.txt",entry);
            string active=SessionState.GetString(Session,"");if(string.IsNullOrEmpty(active))return;
            File.AppendAllText(Dir+active+"-unity-log.txt",entry);
            if(t==LogType.Warning)SessionState.SetInt(Session+".warnings",SessionState.GetInt(Session+".warnings",0)+1);
            if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)SessionState.SetInt(Session+".errors",SessionState.GetInt(Session+".errors",0)+1);
        }
        catch(IOException){}catch(UnauthorizedAccessException){} // Do not recurse through logging if diagnostic output fails.
    }
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.ExitingEditMode && !string.IsNullOrEmpty(SessionState.GetString(Session,"")))
        {
            SessionState.SetBool(Session+".prefsHad",PlayerPrefs.HasKey(ResultKey));SessionState.SetString(Session+".prefs",PlayerPrefs.GetString(ResultKey,""));
            SessionState.SetString(Session+".started",DateTime.UtcNow.ToString("o"));
        }
        if(s==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString(Session,"");c=null;sent=shot=release=overview=finished=false;line="";shots.Clear();started=Time.time;sampledFrame=-1;
            previousFingers.Clear();previousNormals.Clear();previousSpeaking.Clear();previousTimes.Clear();
            if(!string.IsNullOrEmpty(label))report=new RunReport{label=label,branch=label.Contains("refuse")?"refuse":"support",startedUtc=SessionState.GetString(Session+".started",""),
                playerPrefsExistedBefore=SessionState.GetBool(Session+".prefsHad",false),playerPrefsBefore=SessionState.GetString(Session+".prefs","")};
        }
        if(s==PlayModeStateChange.ExitingPlayMode && report!=null)
        {
            SampleRun();report.elapsedSeconds=Time.time-started;report.completed=finished||(c&&c.CurrentStage==Chapter2Controller.Stage.Complete);
            report.finalStage=c?c.CurrentStage.ToString():"controller-unavailable";SessionState.SetString(Session+".report",JsonUtility.ToJson(report));
        }
        if(s==PlayModeStateChange.EnteredEditMode)
        {
            label=SessionState.GetString(Session,"");if(string.IsNullOrEmpty(label))return;
            if(report==null){string saved=SessionState.GetString(Session+".report","");report=string.IsNullOrEmpty(saved)?new RunReport{label=label,branch=label.Contains("refuse")?"refuse":"support"}:JsonUtility.FromJson<RunReport>(saved);}
            report.endedUtc=DateTime.UtcNow.ToString("o");report.playingAfterExit=Application.isPlaying;report.sceneDirtyAfterExit=SceneManager.GetActiveScene().isDirty;
            report.playerPrefsExistedAfter=PlayerPrefs.HasKey(ResultKey);report.playerPrefsAfter=PlayerPrefs.GetString(ResultKey,"");
            report.playerPrefsUnchanged=report.playerPrefsExistedBefore==report.playerPrefsExistedAfter&&report.playerPrefsBefore==report.playerPrefsAfter;
            report.errors=SessionState.GetInt(Session+".errors",0);report.warnings=SessionState.GetInt(Session+".warnings",0);BuildChecks();
            File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(report,true));
            File.WriteAllText(Dir+label+"-completed.txt","completed="+report.completed+" playing="+Application.isPlaying+" dirty="+report.sceneDirtyAfterExit+" assertionsPassed="+report.passed+" errors="+report.errors+" warnings="+report.warnings);
            SessionState.EraseString(Session);SessionState.EraseString(Session+".report");label=null;report=null;
        }
    }
    static void Press(Key key){if(Keyboard.current==null)return;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));release=true;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(release){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;
            try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="refresh")AssetDatabase.Refresh();
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd=="inspect")File.WriteAllText(Dir+"state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" scene="+SceneManager.GetActiveScene().path);
                else if(cmd.StartsWith("test:"))
                {
                    string next=cmd.Substring(5);SessionState.SetString(Session,next);SessionState.SetInt(Session+".errors",0);SessionState.SetInt(Session+".warnings",0);
                    SessionState.EraseString(Session+".report");File.WriteAllText(Dir+next+"-unity-log.txt","");File.WriteAllText(Dir+next+"-stages.txt","");EditorApplication.isPlaying=true;
                }
                else if(cmd=="apply")typeof(Chapter2CouncilRevisionProbe).Assembly.GetType("Chapter2CouncilPoseAuthoring").GetMethod("Apply").Invoke(null,null);
                else if(cmd=="dump-hand")DumpHand();
                else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
                File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
            }catch(Exception e){Debug.LogException(e);}
        }
        if(!Application.isPlaying||string.IsNullOrEmpty(label))return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            if(label.Contains("quick"))c.lineSeconds=.3f;
            c.StageChanged+=s=>{File.AppendAllText(Dir+label+"-stages.txt",Time.time+" "+s+"\n");if(s==Chapter2Controller.Stage.Complete)finished=true;};
        }
        if(!sent&&Time.time>started+1){Press(Key.P);sent=true;}
        SampleRun();
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting && c.ui && c.ui.fade && c.ui.fade.color.a==0 && !overview)
        {overview=true;Audit();Shot(label+"-overview");}
        string nextLine=c.ui&&c.ui.subtitle?c.ui.subtitle.text:"";
        if(nextLine!=line){line=nextLine??"";lineAt=Time.time;shot=false;}
        if(!shot && line.Length>0 && Time.time-lineAt>(label.Contains("quick")?.12f:1.2f) && c.CouncilSpeaker)
        {
            shot=true;string name=label+"-"+c.CouncilSpeaker.name+"-"+(c.CurrentStage==Chapter2Controller.Stage.Ending?"ending":"speech");
            if(shots.Add(name)){Shot(name);if(c.CouncilRally)HandShot(label+"-fist-close");}
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Vote){Press(label.Contains("refuse")?Key.Digit2:Key.Digit1);}
        if(c.CouncilRally&&c.mona)
        {
            var gesture=c.mona.GetComponent<Chapter2RallyGesture>();
            if(gesture && gesture.Weight>.12f && shots.Add("rising"))Shot(label+"-rising");
            if(gesture && gesture.Weight>.98f && shots.Add("fist-held")){Shot(label+"-fist-held");HandShot(label+"-fist-held-close");}
            if(gesture && c.mona.SeatWeight==0 && Time.time-lineAt>2 && shots.Add("rally-settled"))
            {
                Shot(label+"-rally-settled");HandShot(label+"-rally-settled-close");
                float angle,fist;bool have=TryRallyMetrics(out angle,out fist);
                File.WriteAllText(Dir+label+"-rally-metrics.txt","available="+have+" elbowAngle="+angle+" fist="+fist+" seated="+c.mona.seated+" seatWeight="+c.mona.SeatWeight);
            }
        }
        if(c.CouncilStandingTogether && shots.Add("standing"))Shot(label+"-standing-start");
        if(c.CouncilStandingTogether && LeadersReady() && c.leaders.All(a=>!a.seated&&a.SeatWeight<=.001f) && shots.Add("stood"))Shot(label+"-standing-complete");
        if(Time.time-started>180){if(report!=null)report.timedOut=true;Debug.LogError("Council preview timeout");EditorApplication.isPlaying=false;}
    }
    static void BuildChecks()
    {
        report.checks.Clear();
        report.checks.Add(new Check("completed-and-left-play-mode",report.completed&&!report.playingAfterExit&&!report.timedOut,report.finalStage));
        report.checks.Add(new Check("no-runtime-errors",report.errors==0,"errors="+report.errors+" warnings="+report.warnings));
        report.checks.Add(new Check("shortcut-preserved-existing-result",report.playerPrefsUnchanged,"Read-only comparison of key existence and string contents."));
        report.checks.Add(new Check("six-leaders-present",report.leaderCount==6,"count="+report.leaderCount));
        report.checks.Add(new Check("leader-3-wrist-samples",report.leftWrist.samples>0&&report.rightWrist.samples>0&&report.rightWrist.speakingSamples>0,"left="+report.leftWrist.samples+" right="+report.rightWrist.samples+" speaking="+report.rightWrist.speakingSamples));
        report.checks.Add(new Check("leader-3-fingers-follow-forearms",report.leftWrist.maximumFingerForearmAngle<5&&report.rightWrist.maximumFingerForearmAngle<5,"maximum degrees: left="+report.leftWrist.maximumFingerForearmAngle+" right="+report.rightWrist.maximumFingerForearmAngle));
        if(report.branch=="support")
        {
            report.checks.Add(new Check("mona-stood-with-rally",report.rallyObserved&&report.monaStoodDuringRally,"settled samples="+report.settledRallySamples));
            report.checks.Add(new Check("right-fist-fully-closed",report.settledRallySamples>0&&report.minimumSettledFistWeight>=99.9f,"minimum settled blend shape="+report.minimumSettledFistWeight));
            report.checks.Add(new Check("right-elbow-remained-bent",report.settledRallySamples>0&&report.maximumSettledElbowAngle<150&&report.minimumSettledElbowAngle>20,"range="+report.minimumSettledElbowAngle+" to "+report.maximumSettledElbowAngle));
            report.checks.Add(new Check("six-leaders-seated-during-mona-rally-line",report.monaRallySamples>0&&report.leadersStayedSeatedDuringMona,"samples="+report.monaRallySamples));
            report.checks.Add(new Check("six-leaders-stood-after-mona",report.standingTogetherObserved&&report.allLeadersStood,"standing observed="+report.standingTogetherObserved));
        }
        else
        {
            report.checks.Add(new Check("refusal-had-no-rally",!report.rallyObserved&&!report.standingTogetherObserved,"rally="+report.rallyObserved+" standingTogether="+report.standingTogetherObserved));
            report.checks.Add(new Check("refusal-kept-six-leaders-seated",report.endingSamples>0&&report.refusalLeadersStayedSeated,"ending samples="+report.endingSamples));
        }
        report.passed=report.checks.All(a=>a.passed);
    }
    static bool LeadersReady(){return c&&c.leaders!=null&&c.leaders.Length==6&&c.leaders.All(a=>a);}
    static void SampleRun()
    {
        if(!c||report==null||sampledFrame==Time.frameCount)return;
        sampledFrame=Time.frameCount;report.leaderCount=c.leaders==null?0:c.leaders.Count(a=>a);
        report.rallyObserved|=c.CouncilRally;report.standingTogetherObserved|=c.CouncilStandingTogether;
        bool seated=LeadersReady()&&c.leaders.All(a=>a.seated&&a.SeatWeight>=.999f);
        if(c.CurrentStage==Chapter2Controller.Stage.Ending)
        {
            report.endingSamples++;
            if(report.branch=="refuse")report.refusalLeadersStayedSeated&=seated;
        }
        if(c.CouncilRally&&c.CouncilSpeaker==c.mona&&!c.CouncilStandingTogether)
        {
            report.monaRallySamples++;report.leadersStayedSeatedDuringMona&=seated;
            if(c.mona&&!c.mona.seated&&c.mona.SeatWeight<=.001f)report.monaStoodDuringRally=true;
            var gesture=c.mona?c.mona.GetComponent<Chapter2RallyGesture>():null;
            float angle,fist;
            if(TryRallyMetrics(out angle,out fist))
            {
                report.maximumFistWeight=Mathf.Max(report.maximumFistWeight,fist);
                if(gesture&&gesture.Weight>.98f&&!c.mona.seated&&c.mona.SeatWeight<=.001f)
                {
                    report.settledRallySamples++;report.minimumSettledFistWeight=Mathf.Min(report.minimumSettledFistWeight,fist);
                    report.minimumSettledElbowAngle=Mathf.Min(report.minimumSettledElbowAngle,angle);report.maximumSettledElbowAngle=Mathf.Max(report.maximumSettledElbowAngle,angle);
                }
            }
        }
        if(c.CouncilStandingTogether&&LeadersReady()&&c.leaders.All(a=>!a.seated&&a.SeatWeight<=.001f))report.allLeadersStood=true;
        if((c.CurrentStage==Chapter2Controller.Stage.Meeting||c.CurrentStage==Chapter2Controller.Stage.Ending)&&c.leaders!=null&&c.leaders.Length>2&&c.leaders[2]&&c.leaders[2].Rig)
        {
            var actor=c.leaders[2];var hands=actor.GetComponent<Chapter2CouncilHands>();
            if(hands&&hands.alignWrists)
            {
                Vector3 right=Vector3.Cross(Vector3.up,actor.Rig.Forward);
                SampleWrist(actor,actor.Rig.LeftHand,Bone(actor,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm"),hands.leftPalmBasis,right,report.leftWrist);
                SampleWrist(actor,actor.Rig.RightHand,Bone(actor,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm"),hands.rightPalmBasis,Vector3.Slerp(-right,Vector3.up,actor.speaking?.55f:0),report.rightWrist);
            }
        }
    }
    static void SampleWrist(Chapter2Actor actor,Transform hand,Transform elbow,Quaternion basis,Vector3 desiredNormal,WristMetrics metric)
    {
        if(!hand||!elbow||(hand.position-elbow.position).sqrMagnitude<.000001f)return;
        Vector3 along=(hand.position-elbow.position).normalized,fingers=hand.TransformDirection(basis*Vector3.forward),normal=hand.TransformDirection(basis*Vector3.up);
        Vector3 wanted=Vector3.ProjectOnPlane(desiredNormal,along).normalized;
        metric.samples++;if(actor.speaking)metric.speakingSamples++;
        metric.maximumFingerForearmAngle=Mathf.Max(metric.maximumFingerForearmAngle,Vector3.Angle(fingers,along));
        if(wanted.sqrMagnitude>.001f)metric.maximumPalmNormalError=Mathf.Max(metric.maximumPalmNormalError,Vector3.Angle(normal,wanted));
        string side=metric.side;
        if(previousFingers.ContainsKey(side))
        {
            float normalStep=Vector3.Angle(previousNormals[side],normal);metric.maximumAnyNormalStep=Mathf.Max(metric.maximumAnyNormalStep,normalStep);
            // Speech starts/stops can legitimately change the intended palm direction.
            if(previousSpeaking[side]==actor.speaking&&Time.time-previousTimes[side]<.25f)
            {
                metric.continuousPairs++;metric.maximumContinuousFingerStep=Mathf.Max(metric.maximumContinuousFingerStep,Vector3.Angle(previousFingers[side],fingers));
                metric.maximumContinuousNormalStep=Mathf.Max(metric.maximumContinuousNormalStep,normalStep);
            }
        }
        previousFingers[side]=fingers;previousNormals[side]=normal;previousSpeaking[side]=actor.speaking;previousTimes[side]=Time.time;
    }
    static Transform Bone(Chapter2Actor actor,HumanBodyBones humanoid,params string[] names)
    {
        if(!actor||!actor.Rig||!actor.Rig.animator)return null;
        var animator=actor.Rig.animator;
        if(animator.avatar&&animator.avatar.isValid&&animator.isHuman)
        {
            var found=animator.GetBoneTransform(humanoid);if(found)return found;
        }
        return animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Contains(t.name));
    }
    static bool TryRallyMetrics(out float angle,out float fist)
    {
        angle=fist=0;if(!c||!c.mona||!c.mona.Rig||!c.mona.Rig.RightHand)return false;
        var arm=Bone(c.mona,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        var elbow=Bone(c.mona,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");var h=c.mona.GetComponent<Chapter2CouncilHands>();
        if(!arm||!elbow||!h||!h.fistRenderer||!h.fistRenderer.sharedMesh||h.fistShape<0||h.fistShape>=h.fistRenderer.sharedMesh.blendShapeCount)return false;
        angle=Vector3.Angle(arm.position-elbow.position,c.mona.Rig.RightHand.position-elbow.position);fist=h.fistRenderer.GetBlendShapeWeight(h.fistShape);return true;
    }
    static float Distance(Transform a,Transform b){return a&&b?Vector3.Distance(a.position,b.position):0;}
    static void AuditProportions()
    {
        if(report==null||!c)return;report.actors.Clear();
        foreach(var actor in new[]{c.mona}.Concat(c.leaders??new Chapter2Actor[0]).Concat(c.conservatives??new Chapter2Actor[0]))
        {
            if(!actor||!actor.Rig)continue;var rig=actor.Rig;
            var la=Bone(actor,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");var ra=Bone(actor,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
            var lt=Bone(actor,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");var rt=Bone(actor,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
            var lk=Bone(actor,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");var rk=Bone(actor,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
            var lf=Bone(actor,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");var rf=Bone(actor,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
            var metric=new ActorMetrics{name=actor.name,localScale=actor.transform.localScale,rigHeight=rig.Height,seated=actor.seated,
                shoulderWidth=Distance(la,ra),hipWidth=Distance(lt,rt),leftLegLength=Distance(lt,lk)+Distance(lk,lf),rightLegLength=Distance(rt,rk)+Distance(rk,rf),
                torsoLength=la&&ra&&rig.Hips?Vector3.Distance((la.position+ra.position)*.5f,rig.Hips.position):0};
            if(metric.rigHeight>.001f){metric.shoulderWidthToHeight=metric.shoulderWidth/metric.rigHeight;metric.torsoToHeight=metric.torsoLength/metric.rigHeight;metric.legToHeight=(metric.leftLegLength+metric.rightLegLength)*.5f/metric.rigHeight;}
            report.actors.Add(metric);
        }
        var names=new HashSet<string>((c.leaders??new Chapter2Actor[0]).Where(a=>a).Select(a=>a.name));
        foreach(var actor in report.actors)
        {
            var peers=report.actors.Where(a=>a.name!=actor.name&&names.Contains(a.name)&&a.rigHeight>.001f).ToArray();if(peers.Length==0)continue;
            actor.heightToOtherLeadersMean=actor.rigHeight/peers.Average(a=>a.rigHeight);float width=peers.Average(a=>a.shoulderWidthToHeight);
            if(width>.001f)actor.shoulderRatioToOtherLeadersMean=actor.shoulderWidthToHeight/width;
        }
    }
    static void Audit()
    {
        AuditProportions();
        var report=new StringBuilder();
        foreach(var actor in new[]{c.mona}.Concat(c.leaders??new Chapter2Actor[0]).Concat(c.conservatives??new Chapter2Actor[0]))
        {
            if(!actor||!actor.Rig)continue;
            var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
            var a=actor.Rig.animator;
            report.AppendLine(actor.name+" scale="+actor.transform.localScale+" height="+actor.Rig.Height+" head="+(actor.Rig.Head?actor.Rig.Head.position.ToString():"unavailable")+" seated="+actor.seated);
            foreach(var skin in skins)report.AppendLine("mesh="+AssetDatabase.GetAssetPath(skin.sharedMesh)+" vertices="+(skin.sharedMesh?skin.sharedMesh.vertexCount:0)+" bones="+string.Join(",",skin.bones.Select(b=>b?b.name:"null")));
            foreach(var field in new[]{"rightPalmBasis","leftPalmBasis","rightPalmLength","leftPalmLength"})
            {
                var info=typeof(Chapter1IncidentRig).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                report.AppendLine(field+"="+(info==null?"unavailable":info.GetValue(actor.Rig)));
            }
        }
        File.WriteAllText(Dir+label+"-actors.txt",report.ToString());
    }
    static void DumpHand()
    {
        var controller=Object.FindFirstObjectByType<Chapter2Controller>();
        var h=controller&&controller.mona?controller.mona.GetComponent<Chapter2CouncilHands>():null;
        if(!h||!h.fistRenderer||!h.fistRenderer.sharedMesh)throw new InvalidOperationException("Fist mesh is unavailable.");
        var skin=h.fistRenderer;int slot=Array.FindIndex(skin.bones,b=>b&&b.name=="R_Hand");
        if(slot<0)throw new InvalidOperationException("Right-hand bone is unavailable.");
        var mesh=skin.sharedMesh;var v=mesh.vertices;var w=mesh.boneWeights;var n=mesh.normals;
        var bind=mesh.bindposes[slot];var basis=Quaternion.Inverse(h.rightPalmBasis);
        var s=new StringBuilder("i,x,y,z,w,nx,ny,nz\n");
        for(int i=0;i<v.Length&&i<w.Length&&i<n.Length;i++)
        {
            float weight=(w[i].boneIndex0==slot?w[i].weight0:0)+(w[i].boneIndex1==slot?w[i].weight1:0)+(w[i].boneIndex2==slot?w[i].weight2:0)+(w[i].boneIndex3==slot?w[i].weight3:0);
            if(weight<.05f)continue;
            Vector3 q=basis*bind.MultiplyPoint3x4(v[i]),normal=basis*bind.inverse.transpose.MultiplyVector(n[i]);
            s.AppendLine($"{i},{q.x},{q.y},{q.z},{weight},{normal.x},{normal.y},{normal.z}");
        }
        File.WriteAllText(Dir+"hand-points.csv",s.ToString());
    }
    static void HandShot(string name)
    {
        if(!c||!c.player||!c.player.view||!c.mona||!c.mona.Rig||!c.mona.Rig.RightHand)return;
        var view=c.player.view;var t=view.transform;var pos=t.position;var rot=t.rotation;var fov=view.fieldOfView;
        var rig=c.mona.Rig;Vector3 at=rig.RightHand.position;Vector3 right=Vector3.Cross(Vector3.up,rig.Forward);
        try{t.position=at+rig.Forward*.7f+right*.35f+Vector3.up*.1f;t.LookAt(at);view.fieldOfView=38;Shot(name);}
        finally{t.SetPositionAndRotation(pos,rot);view.fieldOfView=fov;}
    }
    public static void Shot(string name)
    {
        var camera=Camera.main;if(!camera)return;var old=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(tex);Object.DestroyImmediate(target);}
    }
}
