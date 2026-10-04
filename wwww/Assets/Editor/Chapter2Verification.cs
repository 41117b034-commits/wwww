using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Temporary verification driver; removed from Assets after the checks.
[InitializeOnLoad]
public static class Chapter2Verification
{
    const string Dir="_CodexBackups/chapter2_20261004/";
    static Chapter2Controller c;
    static string label;
    static double began;
    static float stageAt, lastCutAt;
    static int lastStage=-1,badAttempts;
    static bool recorded, retrySeen, distanceGuard, duplicateGuard;
    static readonly List<string> errors=new List<string>();
    static readonly List<string> stages=new List<string>();
    static readonly HashSet<int> shots=new HashSet<int>();
    static Chapter2Verification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;}
    public static void Run(string mode)
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop current playback before testing.");
        SessionState.SetString("Chapter2Verification.mode",mode);EditorApplication.isPlaying=true;
    }
    static void PlayState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            label=SessionState.GetString("Chapter2Verification.mode","");if(label=="")return;
            c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();c.saveResult=false;
            c.StageChanged+=s=>{if(s==Chapter2Controller.Stage.Complete){Save();recorded=true;}};
            began=EditorApplication.timeSinceStartup;errors.Clear();stages.Clear();shots.Clear();lastStage=-1;badAttempts=0;recorded=false;retrySeen=false;distanceGuard=false;duplicateGuard=false;
            if(label.Contains("fallback"))c.openingFilm=null;
            Time.timeScale=3;
        }
        if(state==PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(label))
        {
            File.WriteAllText(Dir+label+"-returned-to-edit.json","{\"playing\":"+Application.isPlaying.ToString().ToLowerInvariant()+",\"completed\":"+recorded.ToString().ToLowerInvariant()+"}");
            SessionState.EraseString("Chapter2Verification.mode");label=null;c=null;Time.timeScale=1;
        }
    }
    static void Log(string message,string stack,LogType type)
    {if(c && (type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(message+"\n"+stack);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying || !c || string.IsNullOrEmpty(label))return;
        if(EditorApplication.timeSinceStartup-began>210){errors.Add("Test timeout in "+c.CurrentStage);Save();EditorApplication.isPlaying=false;return;}
        int stage=(int)c.CurrentStage;
        if(stage!=lastStage){lastStage=stage;stageAt=Time.time;stages.Add(c.CurrentStage.ToString());}
        if(c.CurrentStage==Chapter2Controller.Stage.Intro)
        {
            if(label.Contains("skip")) c.SkipIntro();
            if(!shots.Contains(stage) && EditorApplication.timeSinceStartup-began>12){Chapter2WorkProbe.Shot(label+"-intro");shots.Add(stage);}
        }
        else if(c.CurrentStage==Chapter2Controller.Stage.Follow)
        {
            Vector3 goal=c.treeApproach.position-new Vector3(0,0,2.5f);
            var motor=c.player.GetComponent<CharacterController>();Vector3 d=goal-c.player.transform.position;d.y=0;
            if(d.magnitude>.3f)motor.Move(d.normalized*3*Time.deltaTime);
            if(Time.time-stageAt>6 && !shots.Contains(stage)){Chapter2WorkProbe.Shot(label+"-forest");shots.Add(stage);}
        }
        else if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice && Time.time-stageAt>2)
        {
            if(!shots.Contains(stage)){Chapter2WorkProbe.Shot(label+"-tree-choice");shots.Add(stage);}
            int value=label.Contains("protect")?0:1;
            if(value==0)c.ui.buttonA.onClick.Invoke();else c.ui.buttonB.onClick.Invoke();
            c.Choose(1-value);duplicateGuard=c.TreeDecision==value;
        }
        else if(c.CurrentStage==Chapter2Controller.Stage.Chopping)
        {
            c.player.view.transform.LookAt(c.sacredTree.position+Vector3.up*1.5f);
            if(!distanceGuard)
            {
                Vector3 from=c.player.transform.position;c.player.Warp(new Vector3(20,.1f,0),c.sacredTree.position);distanceGuard=!c.TryChop();c.player.Warp(from,c.sacredTree.position);
            }
            bool retry=label.Contains("retry") && badAttempts<5;
            if(Time.time-lastCutAt>.65f && (retry?c.RhythmPhase<.15f:(c.RhythmPhase>.43f&&c.RhythmPhase<.57f)))
            {
                if(c.TryChop()){lastCutAt=Time.time;if(retry)badAttempts++;}
            }
            if(badAttempts>=5&&c.Integrity==100&&c.ValidCuts==0)retrySeen=true;
            if(c.ValidCuts>=2&&!shots.Contains(stage)){Chapter2WorkProbe.Shot(label+"-chopping");shots.Add(stage);}
        }
        else if(c.CurrentStage==Chapter2Controller.Stage.Meeting && Time.time-stageAt>4 && !shots.Contains(stage))
        {Chapter2WorkProbe.Shot(label+"-meeting");shots.Add(stage);}
        else if(c.CurrentStage==Chapter2Controller.Stage.Vote && Time.time-stageAt>2)
        {
            if(!shots.Contains(stage)){Chapter2WorkProbe.Shot(label+"-vote");shots.Add(stage);}
            if(label.Contains("refuse"))c.ui.buttonB.onClick.Invoke();else c.ui.buttonA.onClick.Invoke();
        }
        else if(c.CurrentStage==Chapter2Controller.Stage.Ending && c.ui.endingPanel.activeSelf && !shots.Contains(stage))
        {Chapter2WorkProbe.Shot(label+"-black-ending");shots.Add(stage);}
        if(c.Completed&&!recorded){Save();recorded=true;}
    }
    static void Save()
    {
        File.WriteAllText(Dir+label+"-result.json",JsonUtility.ToJson(new Result{label=label,completed=c.Completed,videoPlayed=c.VideoPlayed,videoFinished=c.VideoFinished,tree=c.TreeDecision,vote=c.MeetingDecision,cuts=c.ValidCuts,failures=c.FailedCuts,integrity=c.Integrity,retrySeen=retrySeen,distanceGuard=distanceGuard,duplicateGuard=duplicateGuard,errors=errors.ToArray(),stages=stages.ToArray()},true));
    }
    [Serializable]class Result{public string label;public bool completed,videoPlayed,videoFinished,retrySeen,distanceGuard,duplicateGuard;public int tree,vote,cuts,failures;public float integrity;public string[] errors,stages;}
}
