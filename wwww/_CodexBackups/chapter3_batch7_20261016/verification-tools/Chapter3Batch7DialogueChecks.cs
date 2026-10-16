using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Temporary Editor Play Mode helper. Copy beside the main probe only for the
// validation run, then archive outside Assets. No cloud/local configuration,
// endpoint, model, credentials or PlayerPrefs values are changed by this helper.
// It holds the existing service at its in-memory preparation gate, so timeout
// and cancellation are deterministic and no generated answer is requested.
public static class Chapter3Batch7DialogueChecks
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    public static IEnumerator Run(Chapter3Flow flow,Action<string,bool> check,Action<string> progress=null,Action<string> shot=null)
    {
        int originalRate=Application.targetFrameRate,originalVsync=QualitySettings.vSyncCount;
        Chapter3OpeningConversation opening=null;
        Chapter3ReadinessConversation readiness=null;
        Chapter3EndingConversation ending=null;
        float openingTimeout=15,readinessTimeout=15;
        int callbacks=0;
        try
        {
            progress?.Invoke("Controlled preparation/timeout/cancel checks; no real model request and no settings changes.");
            flow.EnterStation();yield return null;
            var station=flow.station;station.Suspend();
            opening=station.Opening;readiness=station.Readiness;
            openingTimeout=opening.replyTimeout;readinessTimeout=readiness.replyTimeout;
            var service=Service(opening);
            check("opening owns its initialized dialogue service",service);
            bool prepared=false;int preparationFrames=0;
            HoldPreparation(service);
            service.StartCoroutine(FinishPreparation(service,()=>preparationFrames++,()=>prepared=true));
            opening.PrepareService();
            opening.Begin(station,()=>callbacks++);
            check("opening Begin keeps an in-progress warmup",(bool)Get(service,"starting"));
            yield return Wait(()=>prepared,2,"opening preparation sentinel");
            check("opening warmup keeps advancing without blocking frames",prepared&&preparationFrames>=2&&opening.Active&&!opening.WaitingForReply);
            check("preparation does not reduce frame budget",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);

            HoldPreparation(service);opening.replyTimeout=.2f;
            check("opening accepts an offline question",opening.SubmitReply("接下來要去哪裡？"));
            yield return Wait(()=>!opening.WaitingForReply,3,"opening offline fallback");
            check("opening deadline yields one authored fallback",opening.CompletedTurns==1&&opening.FallbackReplies==1&&opening.GeneratedReplies==0&&opening.LastReplyError=="timeout"&&opening.LastReplySeconds<2);
            check("opening fallback history matches visible transcript",TailMatches(opening,"接下來要去哪裡？",flow.ui.response.text,null));
            check("opening timeout releases service and frame budget",!service.Busy&&!(bool)Get(service,"starting")&&Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            HoldPreparation(service);opening.replyTimeout=.35f;
            check("opening second request starts before cancellation",opening.SubmitReply("我心裡很緊張。"));
            yield return null;yield return null;
            opening.Cancel();
            string cancelledTranscript=flow.ui.response.text;
            yield return new WaitForSecondsRealtime(.5f);
            check("opening cancelled request never completes or revives",!opening.Active&&!opening.WaitingForReply&&opening.CompletedTurns==1&&callbacks==0&&flow.ui.response.text==cancelledTranscript&&!service.Busy);
            check("opening cancel restores frame budget",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            opening.Begin(station,()=>callbacks++);
            check("opening re-entry clears prior turns and diagnostics",opening.CompletedTurns==0&&opening.LastReplyError==null&&opening.LastReplySeconds==0&&History(opening).Count==1);
            opening.Cancel();

            readiness.Begin(station,_=>callbacks++);service=Service(readiness);
            readiness.replyTimeout=.2f;HoldPreparation(service);
            check("readiness accepts an offline clarification",readiness.SubmitReply("我現在拿著什麼武器？"));
            yield return Wait(()=>!readiness.WaitingForReply,3,"readiness offline clarification");
            check("readiness unavailable service keeps uncertain intent open",readiness.Active&&readiness.UsedFallback&&readiness.LastDecision==Chapter3ReadinessConversation.Decision.Unclear&&readiness.LastReplyError=="timeout");
            check("readiness canonical history stores authored clarification",TailMatches(readiness,"我現在拿著什麼武器？",flow.ui.response.text,readiness.LastReply));
            check("readiness fallback restores frame budget",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            HoldPreparation(service);readiness.replyTimeout=.35f;
            check("readiness pending request starts before cancellation",readiness.SubmitReply("準備好了"));
            yield return null;yield return null;readiness.Cancel();
            cancelledTranscript=flow.ui.response.text;
            yield return new WaitForSecondsRealtime(.5f);
            check("readiness cancelled decision cannot advance",!readiness.Active&&!readiness.WaitingForReply&&callbacks==0&&flow.ui.response.text==cancelledTranscript&&!service.Busy);
            readiness.Begin(station,_=>callbacks++);HoldPreparation(service);readiness.replyTimeout=.2f;
            check("readiness re-entry clears diagnostics",readiness.LastReplyError==null&&readiness.LastReplySeconds==0&&History(readiness).Count==1);
            check("readiness explicit offline decision accepted",readiness.SubmitReply("準備好了"));
            yield return Wait(()=>readiness.LastDecision==Chapter3ReadinessConversation.Decision.Ready,3,"readiness explicit fallback");
            check("offline readiness retains explicit decision and shown reply",readiness.UsedFallback&&TailMatches(readiness,"準備好了",flow.ui.response.text,readiness.LastReply));
            yield return Wait(()=>!readiness.Active,7,"readiness authored response completion");
            check("offline readiness callback fires exactly once",callbacks==1);
            readiness.Cancel();

            flow.EnterSchool();yield return null;flow.school.Suspend();
            ending=flow.school.gameObject.AddComponent<Chapter3EndingConversation>();
            var result=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Pihu,Chapter3Rescue.Decision.Leave,Chapter3Weapons.Weapon.Rifle);
            // Create the service without contacting it; Begin does not submit.
            ending.Begin(flow.school,result,()=>callbacks++);ending.Cancel();service=Service(ending);
            prepared=false;preparationFrames=0;HoldPreparation(service);
            service.StartCoroutine(FinishPreparation(service,()=>preparationFrames++,()=>prepared=true));
            ending.PrepareService();ending.Begin(flow.school,result,()=>callbacks++);
            check("ending Begin keeps outcome warmup alive",(bool)Get(service,"starting"));
            yield return Wait(()=>prepared,2,"ending preparation sentinel");
            check("ending warmup does not block UI or frames",prepared&&preparationFrames>=2&&ending.Active&&!ending.WaitingForReply&&flow.ui.chat.activeSelf);
            HoldPreparation(service);ending.replyTimeout=.2f;
            check("ending accepts non-fact offline question",ending.SubmitReply("我很害怕，心裡靜不下來。"));
            yield return Wait(()=>!ending.WaitingForReply,3,"ending offline fallback");
            check("ending deadline falls back without changing result",ending.CompletedTurns==1&&ending.FallbackReplies==1&&ending.GeneratedReplies==0&&ending.LastReplyError=="timeout"&&!result.completed&&!result.pihuSurvived);
            check("ending history contains the visible authored reply",TailMatches(ending,"我很害怕，心裡靜不下來。",flow.ui.response.text,ending.LastReply));
            check("ending fallback restores frame budget",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            shot?.Invoke("dialogue-offline-ending");
            HoldPreparation(service);ending.replyTimeout=.35f;
            check("ending second request starts before cancellation",ending.SubmitReply("我想緩一口氣。"));
            yield return null;yield return null;
            flow.EnterSchool();
            yield return new WaitForSecondsRealtime(.5f);
            check("region re-entry cancels ending and cannot revive it",!ending.Active&&!ending.WaitingForReply&&ending.CompletedTurns==1&&callbacks==1&&!flow.ui.chat.activeSelf&&!service.Busy&&History(ending).Count==0);
            check("region cancellation restores frame budget",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);

            var vrBudget=new Chapter3DialogueFrameBudget();vrBudget.Begin(true);
            check("VR frame budget leaves display timing unchanged",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            vrBudget.End();
            check("VR frame budget release is a no-op",Application.targetFrameRate==originalRate&&QualitySettings.vSyncCount==originalVsync);
            progress?.Invoke("Completed injected preparation/timeout/cancellation checks. Real cold model startup, physical disconnection and headset performance remain separate validation.");
        }
        finally
        {
            if(opening){opening.Cancel();opening.replyTimeout=openingTimeout;}
            if(readiness){readiness.Cancel();readiness.replyTimeout=readinessTimeout;}
            if(ending){ending.Cancel();UnityEngine.Object.Destroy(ending);}
            Application.targetFrameRate=originalRate;QualitySettings.vSyncCount=originalVsync;
        }
    }
    static Chapter2LocalDialogue Service(object owner)=>(Chapter2LocalDialogue)Get(owner,"dialogue");
    static List<Chapter2LocalDialogue.Message> History(object owner)=>(List<Chapter2LocalDialogue.Message>)Get(owner,"history");
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Fields).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Fields).SetValue(owner,value);
    static void HoldPreparation(Chapter2LocalDialogue service)
    {
        service.CancelRequest();Set(service,"<Ready>k__BackingField",false);Set(service,"starting",true);
    }
    static IEnumerator FinishPreparation(Chapter2LocalDialogue service,Action frame,Action complete)
    {
        float until=Time.realtimeSinceStartup+.25f;
        while(Time.realtimeSinceStartup<until){frame();yield return null;}
        Set(service,"starting",false);complete();
    }
    static bool TailMatches(object owner,string question,string transcript,string answer)
    {
        var history=History(owner);if(history.Count<3)return false;
        var user=history[history.Count-2];var reply=history[history.Count-1];
        return user.role=="user"&&user.content==question&&reply.role=="assistant"&&
            (answer==null||reply.content==answer)&&transcript.EndsWith(reply.content,StringComparison.Ordinal);
    }
    static IEnumerator Wait(Func<bool> predicate,float seconds,string label)
    {
        float until=Time.realtimeSinceStartup+seconds;
        while(!predicate()&&Time.realtimeSinceStartup<until)yield return null;
        if(!predicate())throw new Exception("Timed out: "+label);
    }
}
