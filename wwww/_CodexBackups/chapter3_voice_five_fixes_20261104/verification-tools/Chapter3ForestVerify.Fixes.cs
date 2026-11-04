using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
public static partial class Chapter3ForestVerify
{
    static IEnumerator Fixes()
    {
        f.ui.voice.AllowOnline=false;
        yield return Wait(()=>f.station.CurrentLine!=null,15,"opening");
        Check("dialogue camera active",f.DialogueView.Framing);yield return null;
        Check("opening fixed camera distance",Mathf.Abs(f.DialogueView.ActualDistance-2.6f)<.02f);
        f.station.StopAllCoroutines();
        f.ui.Say("",null);var p=f.player;var home=p.view.transform.localPosition;
        foreach(var actor in new[]{f.station.bawan,f.station.pihu,f.station.mona})
        {
            p.Warp(actor.transform.position+new Vector3(0,.06f,-.6f),actor.transform.position+Vector3.up);
            p.canMove=true;var body=p.transform.position;f.ui.Say(actor.DisplayName,"鏡頭距離驗證");yield return null;yield return new WaitForEndOfFrame();
            report.observations.Add(actor.DisplayName+" framing="+f.DialogueView.Framing+" distance="+f.DialogueView.ActualDistance+" player="+p.transform.position+" camera="+p.view.transform.position);
            Check("fixed close dialogue "+actor.DisplayName,f.DialogueView.Framing&&Mathf.Abs(f.DialogueView.ActualDistance-2.6f)<.02f);
            Check("camera uses current region's actor "+actor.DisplayName,Mathf.Abs(Vector3.Distance(p.view.transform.position,actor.Rig.Head.position-Vector3.up*.15f)-2.6f)<.03f);
            Check("camera does not teleport player "+actor.DisplayName,Flat(p.transform.position,body)<.01f);
            yield return Keys(UnityEngine.InputSystem.Key.W,.25f);Check("dialogue blocks movement input",Flat(p.transform.position,body)<.01f);
            yield return Shot("dialogue-"+actor.name);
            f.ui.Say("",null);Check("eye restored",Vector3.Distance(p.view.transform.localPosition,home)<.01f&&!p.DialogueFraming);
        }
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();yield return Wait(()=>f.combat.Fighting,25,"fight");yield return new WaitForSeconds(8);
        var c=f.combat;var b=c.bawan;var reaction=b.GetComponent<Chapter3CompanionReaction>();Physics.SyncTransforms();
        Check("pose shot zones installed",reaction.HasPoseHitZones);
        Vector3 over=b.Rig.Head.position+Vector3.up*.28f;
        bool blocked=c.Trace(over+Vector3.back*1.8f,Vector3.forward,3.6f,out var topHit);
        Check("crouched empty space no longer hits friend",!blocked||topHit.collider.GetComponentInParent<Chapter3CompanionReaction>()!=reaction);
        Vector3 head=b.Rig.Head.position;
        bool bodyHit=c.Trace(head+Vector3.back*1.8f,Vector3.forward,3.6f,out var friendHit);
        Check("visible friend body still protected",bodyHit&&friendHit.collider.GetComponentInParent<Chapter3CompanionReaction>()==reaction);
        var before=p.view.transform.localPosition;c.CompanionWarning(b.DisplayName,"戰鬥警告驗證",1);
        yield return null;Check("combat callout preserves aim camera",!f.DialogueView.Framing&&Vector3.Distance(before,p.view.transform.localPosition)<.01f);
        Check("SSE success parser",Chapter3SeediqVoice.CompletedQueue("data: {\"msg\":\"process_completed\",\"event_id\":\"current\",\"success\":true,\"output\":{\"data\":[\"voice\"]}}\n","current")=="[\"voice\"]");
        Check("SSE failure parser",Chapter3SeediqVoice.CompletedQueue("data: {\"msg\":\"process_completed\",\"success\":false}\n")==null);
        Check("cancelled SSE result cannot replace current speech",Chapter3SeediqVoice.CompletedQueue("data: {\"msg\":\"process_completed\",\"event_id\":\"old\",\"success\":true,\"output\":{\"data\":[\"old\"]}}\n","current")==null);
        var chunks=Chapter3SeediqVoice.SplitSpeech(string.Join(" ",Enumerable.Repeat("seediq",140)));
        Check("TTS chunks retain every word",chunks.All(t=>t.Length<=300)&&string.Join(" ",chunks)==string.Join(" ",Enumerable.Repeat("seediq",140)));
        f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"F8");
        Check("F8 clears shot zones from previous region",!c.bawan.gameObject.activeInHierarchy);
    }
    static IEnumerator Baseline()
    {
        yield return Wait(()=>f.ui.fade.color.a<.01f,15,"opening initialized");
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();
        yield return Wait(()=>f.combat.Fighting,25,"mountain fight");
        yield return new WaitForSeconds(8);
        var c=f.combat;var actor=c.bawan;var p=f.player;
        report.observations.Add("Bawan root="+actor.transform.position+" head="+actor.Rig.Head.position+" height="+actor.Rig.Height);
        foreach(var col in actor.GetComponentsInChildren<Collider>())report.observations.Add("Bawan collider "+col.name+" "+col.bounds);
        foreach(var e in c.Enemies)report.observations.Add("officer="+e.name+" aim="+e.AimPoint);
        p.Warp(actor.transform.position+Vector3.back*2+Vector3.up*.06f,c.Enemies[0].AimPoint);
        Physics.SyncTransforms();if(c.Trace(p.view.transform.position,p.view.transform.forward,65,out var hit))report.observations.Add("aim trace="+hit.collider.name+" point="+hit.point);
        yield return Shot("baseline-mountain");
        f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var b=s.Battle;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        p.Warp(s.tado.transform.position+new Vector3(.5f,.06f,-2.5f),s.tado.transform.position+Vector3.up);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,35,"briefing");
        b.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>b.Fighting,25,"outbreak");
        var starts=a.spectators.Select(t=>t.position).ToArray();
        yield return new WaitForSeconds(20);
        for(int i=0;i<starts.Length;i++)report.observations.Add("guest="+a.spectators[i].name+" move="+Flat(starts[i],a.spectators[i].position)+" phase="+a.SpectatorPhase(i));
        yield return Shot("baseline-crowd");
        foreach(var e in b.Enemies)e.Hit(100,p.transform.position);
        yield return Wait(()=>b.Step==4,5,"four cleared");
        Check("whistle fixture",b.CallRetreat());
        // Reproduce an early player arrival; every actor still uses its real motor.
        p.Warp(new Vector3(0,.06f,78),new Vector3(-3,1,92));
        yield return Wait(()=>b.Rescue&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing,30,"forest");
        p.Warp(new Vector3(-3,.06f,92),new Vector3(0,1,100));
        for(int i=0;i<12&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing;i++){
            yield return new WaitForSeconds(10);report.observations.Add(Snapshot());
            File.WriteAllText(Dir+"baseline-progress.json",JsonUtility.ToJson(report,true));
        }
        yield return Shot("baseline-forest");
        report.observations.Add("ending approach="+b.Rescue.CurrentStage);
    }
}
