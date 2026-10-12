using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3PlayProbe
{
    [Serializable] public class Report
    {
        public string mode;
        public bool completed,blankBlocked,earlyDepartureBlocked,duplicateBlocked,tooFarPickupBlocked,waitingObserved,imeConfirmationBlocked,keyboardPickup,boundaryDoesNotSnap;
        public int turns,generated,fallback;
        public float elapsed;
        public List<string> replies=new List<string>(),errors=new List<string>(),lines=new List<string>();
    }
    static Report report;
    static int submitted,recorded,step,pickupFrame;
    static float started,trailBegan,pihuShotAfter;
    static string previousLine;
    static bool ready;
    const string Key="Chapter3PlayProbe.mode";
    static Chapter3PlayProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    public static void Begin(string mode)
    {if(EditorApplication.isPlaying)throw new Exception("Stop first");SessionState.SetString(Key,mode);EditorApplication.isPlaying=true;}
    static void Log(string message,string stack,LogType type)
    {if(report!=null&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))report.errors.Add(message);}
    static void Tick()
    {
        string mode=SessionState.GetString(Key,"");if(mode==""||!Application.isPlaying||EditorApplication.isCompiling)return;
        var c=Object.FindFirstObjectByType<Chapter3Controller>();if(!c)return;
        try
        {
            if(!ready)
            {
                ready=true;report=new Report{mode=mode};started=Time.realtimeSinceStartup;submitted=recorded=step=0;pickupFrame=-1;pihuShotAfter=0;previousLine=null;
                if(mode=="test-fallback")typeof(Chapter2LocalDialogue).GetField("cloudConfigError",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c.dialogue,"Simulated unavailable service for verification");
                report.earlyDepartureBlocked=!c.Depart();
                Vector3 edge=new Vector3(12,0,13.99f);report.boundaryDoesNotSnap=Vector3.Distance(edge,c.Constrain(edge,new Vector3(12,0,14.1f)))<.12f;
            }
            if(Time.realtimeSinceStartup-started>300)throw new Exception("Full route test timed out");
            if(c.CurrentLine!=null&&previousLine!=c.CurrentLine){previousLine=c.CurrentLine;report.lines.Add(previousLine);}
            if(c.CurrentStage==Chapter3Controller.Stage.Conversation)
            {
                if(submitted==0)
                {
                    Chapter3WorkProbe.Shot(mode+"-opening");
                    report.blankBlocked=!c.SubmitReply("   ");
                    report.earlyDepartureBlocked&=!c.Depart();
                    var composition=typeof(Chapter3Controller).GetField("composing",BindingFlags.Instance|BindingFlags.NonPublic);
                    composition.SetValue(c,true);c.ui.input.onSubmit.Invoke("輸入法確認字");report.imeConfirmationBlocked=!c.WaitingForReply&&c.CompletedTurns==0;composition.SetValue(c,false);
                }
                if(c.CompletedTurns>recorded)
                {recorded=c.CompletedTurns;report.replies.Add(c.LastReply);Chapter3WorkProbe.Shot(mode+"-reply-"+recorded);}
                if(!c.WaitingForReply && c.CompletedTurns<3 && submitted==c.CompletedTurns)
                {
                    string[] questions={"我有點緊張，不知道前面會發生什麼。","如果我跟不上，你們會等我嗎？","好，我會跟著你和比胡走。"};
                    c.ui.input.text=questions[submitted];c.ui.send.onClick.Invoke();submitted++;
                    report.duplicateBlocked=submitted==1?!c.SubmitReply("不能重複送出"):report.duplicateBlocked;
                }
                else if(c.CompletedTurns==3&&c.ui.depart.interactable)c.ui.depart.onClick.Invoke();
            }
            if(c.CurrentStage==Chapter3Controller.Stage.Equipment)
            {
                if(pickupFrame<0)
                {
                    report.tooFarPickupBlocked=!c.TakeKnife();
                    c.player.Warp(new Vector3(-2.5f,.06f,2.5f),c.knifePickup.position+Vector3.up*.7f);
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.E));pickupFrame=Time.frameCount;
                }
                else if(Time.frameCount>pickupFrame+8)throw new Exception("E key pickup rejected");
            }
            if(pickupFrame>=0&&Time.frameCount>pickupFrame+2)
            {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());report.keyboardPickup=c.HasKnife;if(c.HasKnife)Chapter3WorkProbe.Shot(mode+"-knife");pickupFrame=-2;}
            if(c.CurrentStage==Chapter3Controller.Stage.Trail)
            {
                if(step==0){step=1;trailBegan=Time.realtimeSinceStartup;}
                if(c.CompanionsWaiting)report.waitingObserved=true;
                if(Time.realtimeSinceStartup-trailBegan>9)
                {
                    Vector3 target=c.bawan.transform.position;
                    Vector3 delta=Vector3.ProjectOnPlane(target-c.player.transform.position,Vector3.up);
                    if(delta.magnitude>2.5f)c.player.Move(delta.normalized*(c.player.speed*Mathf.Min(Time.deltaTime,.04f)));
                    if(c.CurrentLine==null)c.player.FocusOn(target+Vector3.up*1.3f);
                }
                if(c.TrailPoint>=4&&step==1){Chapter3WorkProbe.Shot(mode+"-forest");step=2;}
                if(c.CurrentLine=="等等我，這條帶子又鬆了。"&&step==2)
                {if(pihuShotAfter==0)pihuShotAfter=Time.realtimeSinceStartup+1;else if(Time.realtimeSinceStartup>=pihuShotAfter){Chapter3WorkProbe.Shot(mode+"-pihu");step=3;}}
            }
            if(c.PartComplete)
            {
                report.completed=true;report.turns=c.CompletedTurns;report.generated=c.GeneratedReplies;report.fallback=c.FallbackReplies;report.elapsed=Time.realtimeSinceStartup-started;
                Chapter3WorkProbe.Shot(mode+"-arrival");Finish();
            }
        }
        catch(Exception e){if(report==null)report=new Report{mode=mode};report.errors.Add(e.ToString());Finish();}
    }
    static void Finish()
    {
        File.WriteAllText(Chapter3WorkProbe.Dir+report.mode+".json",JsonUtility.ToJson(report,true));
        SessionState.EraseString(Key);ready=false;report=null;EditorApplication.isPlaying=false;
    }
}
