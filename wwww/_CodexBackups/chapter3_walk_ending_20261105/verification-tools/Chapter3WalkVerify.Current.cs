using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

public static partial class Chapter3WalkVerify
{
    static object Get(object o,string field)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Set(object o,string prop,object value)=>o.GetType().GetField("<"+prop+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static object Invoke(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static IEnumerator CurrentScenario()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();
        Check("formal third chapter",f&&f.startAtStation);

        var tree=f.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="駐在所林木 83");
        Check("station tree moved off connector",tree.position.x>0&&tree.position.z< -96);
        yield return Wait(()=>f.ui.fade.color.a<.01f,15,"opening fade complete");
        f.player.Warp(new Vector3(-5,.06f,-96),new Vector3(0,1,-91));yield return Shot("tree-verge-"+mode);
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var b=s.Battle;var p=f.player;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        if(mode.StartsWith("crowd")){yield return CrowdShow();yield break;}
        p.Warp(s.tado.transform.position+new Vector3(.5f,.06f,-2.5f),s.tado.transform.position+Vector3.up);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Observing,10,"Tado talking");
        yield return new WaitForSeconds(1);yield return Shot("gaze-"+mode);
        report.observations.Add("Tado head="+s.tado.Rig.Head.position+" camera="+p.view.transform.position);
        if(mode=="before")
        {
            yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"choice");
            b.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>b.Fighting,25,"fight");
            p.Warp(new Vector3(12,.06f,34),new Vector3(-5,1,28));
            foreach(var e in b.Enemies)e.Hit(100,p.transform.position);
            yield return new WaitForSeconds(4);
            Check("old four-clear rally gate reproduced",b.CampusDefeatedCount==4&&b.Step<4);
            report.observations.Add("All four defeated while player stays ahead; step="+b.Step+" Tado="+s.tado.transform.position);
            while(b.Step<4)Invoke(b,"AdvanceAssaultStage");
            Check("whistle fixture",b.CallRetreat());p.Warp(new Vector3(0,.06f,78),new Vector3(0,1,65));
            yield return new WaitForSeconds(2);
            Check("old player-arrival gate reproduced",b.Fighting&&b.RetreatArrivedCount<10&&(!b.Rescue||!b.Rescue.Active));
            report.observations.Add("Player at reunion: "+p.transform.position+" arrived="+b.RetreatArrivedCount+" stage="+b.CurrentStage);
            yield return Shot("arrival-blocked-before");
            yield return CrowdShow();
            yield break;
        }
        var pose=s.tado.GetComponent<Chapter3SchoolCompanionPose>();
        var face=s.tado.Rig.Head.TransformDirection((Vector3)Get(pose,"headForward"));
        Check("Tado looks at player's camera while speaking",Vector3.Dot(face,(p.view.transform.position-s.tado.Rig.Head.position).normalized)>.97f);
        Check("briefing keeps crouch and relaxed hands",pose.UnderFireWeight>.9f&&s.tado.Rig.LeftHand.position.y<s.tado.Rig.Head.position.y-.3f);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,25,"choice");
        b.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>b.Fighting,25,"fight");
        yield return new WaitForSeconds(55);
        Check("women and children do not disappear before whistle",a.EscapedSpectators==0&&a.spectators.All(t=>t.gameObject.activeSelf));
        p.Warp(new Vector3(12,.06f,34),new Vector3(-5,1,28));
        var tadoStart=s.tado.transform.position;var playerAhead=p.transform.position;
        foreach(var e in b.Enemies)e.Hit(100,p.transform.position);
        yield return Wait(()=>b.Step==4,3,"four cleared automatically");
        yield return new WaitForSeconds(2);
        report.observations.Add("No player movement input after clearing: "+playerAhead+" -> "+p.transform.position+" step="+b.Step);
        Check("all four clear without returning to old rally",Flat(p.transform.position,playerAhead)<2&&b.Step==4);
        yield return Wait(()=>Flat(tadoStart,s.tado.transform.position)>1,15,"Tado starts moving forward "+tadoStart);
        report.observations.Add("Tado before="+tadoStart+" after="+s.tado.transform.position);
        Check("Tado advances on foot after four defeated",Flat(tadoStart,s.tado.transform.position)>1);
        yield return Shot("automatic-advance-"+mode);
        yield return Keys(Key.E,.15f);Check("E whistle starts retreat",b.RetreatCalled&&b.RetreatSignalCount==1);
        int survivorCount=a.spectators.Count(t=>!t.GetComponent<Chapter3SchoolCrowdActor>().IsFallen);
        Check("all surviving guests join withdrawal",a.CivilianFollowerCount==survivorCount&&survivorCount>6);
        var guestStart=a.spectators.Select(t=>t.position).ToArray();
        var people=(Chapter3SchoolCrowdActor[])Get(a,"assailants");
        // Hold two participants behind as a fixture; their actual motion resumes after reunion.
        var late=people.Take(2).ToArray();foreach(var c in late)c.FollowingWithdrawal=false;
        foreach(var point in new[]{new Vector3(12,0,31),new Vector3(17,0,31),new Vector3(17,0,34),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(24.2f,0,61),new Vector3(22,0,66),new Vector3(12,0,72)})
            yield return StoryWalk(point,.7f,25);
        yield return StoryWalk(new Vector3(0,0,78),3.8f,20,()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,3,"player arrival immediately starts reunion");
        Check("reunion begins before all NPCs arrive",b.RetreatArrivedCount<10);
        Check("survivors remain visible during withdrawal",a.EscapedSpectators==0&&a.spectators.All(t=>t.gameObject.activeSelf));
        report.observations.Add("Physical player arrival="+p.transform.position+" villagers="+b.RetreatArrivedCount+"/10 Tado="+s.tado.transform.position);
        yield return Shot("player-arrival-"+mode);
        yield return Wait(()=>b.Rescue&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing,22,"reunion reaches forest route");
        var lateStart=late.Select(c=>c.transform.position).ToArray();foreach(var c in late)c.FollowingWithdrawal=true;
        if(mode=="escort")
        {
            for(int tick=0;tick<12;tick++)
            {
                yield return new WaitForSeconds(10);
                System.IO.File.WriteAllText(Dir+"escort-progress.txt",EscortSnapshot(a));
            }
            report.observations.Add(EscortSnapshot(a));yield return Shot("escort-rear");yield break;
        }
        var ending=s.gameObject.GetComponent<Chapter3Ending>();if(!ending)ending=s.gameObject.AddComponent<Chapter3Ending>();ending.PersistResult=false;
        string previous=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"");bool hadPrevious=PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey);
        yield return Forest(mode=="leave");
        float settleUntil=Time.realtimeSinceStartup+50;
        while((a.CivilianArrivedCount<survivorCount||!a.WithdrawalArrived)&&Time.realtimeSinceStartup<settleUntil)yield return null;
        report.observations.Add(EscortSnapshot(a));
        Check("all surviving civilians physically reach final assembly slots",a.CivilianArrivedCount==survivorCount);
        Check("all ten participants physically reach final assembly slots",a.WithdrawalArrived);
        Check("survivors remain visible through chapter ending",a.EscapedSpectators==0&&a.spectators.All(t=>t.gameObject.activeSelf));
        Check("survivors physically followed toward rear",a.spectators.Where((t,i)=>!t.GetComponent<Chapter3SchoolCrowdActor>().IsFallen&&t.position.z>60&&Flat(t.position,guestStart[i])>8).Count()==survivorCount);
        report.observations.Add("Guests following="+a.CivilianFollowerCount+" arrived="+a.CivilianArrivedCount+" visible="+a.spectators.Count(t=>t.gameObject.activeSelf));
        Check("late participants keep walking after player leaves reunion",late.Where((c,i)=>Flat(c.transform.position,lateStart[i])>3).Count()==2);
        Check("verification preserves chapter save",PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey)==hadPrevious&&PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"")==previous);
        yield return Keys(Key.F8,.12f);yield return Wait(()=>s.Exploring,20,"F8 reset");
        Check("F8 clears rescue ending and withdrawal",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!b.Rescue.Active&&!ending.Active&&!a.Withdrawing&&a.CivilianFollowerCount==0);
        report.observations.Add("Editor fixture defeated four school officers and forest guard with Hit(100); actual CharacterController walking, E whistle/lift, proximity transitions and authored ending used. Save persistence disabled only on temporary ending component.");
    }
    static string EscortSnapshot(Chapter3SchoolAmbient ambient)
    {
        var lines=new System.Collections.Generic.List<string>();
        foreach(var item in (System.Collections.IEnumerable)Get(ambient,"civilianEscorts"))
        {
            var type=item.GetType();var actor=(Chapter3SchoolCrowdActor)type.GetField("actor").GetValue(item);
            var route=(Vector3[])type.GetField("route").GetValue(item);var leg=(int)type.GetField("leg").GetValue(item);
            lines.Add(actor.name+" position="+actor.transform.position+" leg="+leg+"/"+route.Length+" target="+(leg<route.Length?route[leg].ToString():"arrived"));
            if(leg<route.Length)
            {
                var motor=(Chapter3PathMotor)type.GetField("motor").GetValue(item);var path=(System.Collections.Generic.List<Vector3>)Get(motor,"path");
                var next=Vector3.MoveTowards(actor.transform.position,path.Count>0?path[0]:route[leg],.05f);
                lines.Add("  path="+string.Join(";",path)+" search="+Get(motor,"searching")+" iterations="+Get(motor,"iterations")+" motorClear="+Invoke(motor,"ClearForActor",actor.transform.position,next)+" bodyClear="+Invoke(actor,"Clear",next));
                foreach(var collider in Physics.OverlapCapsule(next+Vector3.up*.35f,next+Vector3.up*1.25f,.3f))
                    if(!collider.transform.IsChildOf(actor.transform))lines.Add("  overlap "+collider.name+" "+collider.transform.position+" bounds="+collider.bounds+" blocks="+Invoke(actor,"Obstacle",collider,next));
            }
        }
        return string.Join("\n",lines);
    }
    static void StoryMove(Vector3 target)
    {var p=f.player;var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.Move(d.normalized*Mathf.Min(d.magnitude,p.speed*Time.deltaTime));p.FocusOn(target+Vector3.up*1.1f);}
    static IEnumerator StoryWalk(Vector3 target,float radius,float timeout,Func<bool> done=null)
    {
        float until=Time.realtimeSinceStartup+timeout;
        while(Flat(f.player.transform.position,target)>radius&&(done==null||!done()))
        {if(Time.realtimeSinceStartup>until)throw new Exception("Story walk blocked "+f.player.transform.position+" -> "+target+" "+Snapshot());StoryMove(target);yield return null;}
        Check("walked actual controller to "+target,true);
    }
    static string Snapshot()
    {var s=f.school;var b=s.Battle;var r=b.Rescue;return "battle="+b.CurrentStage+" rescue="+(r?r.CurrentStage.ToString():"none")+" B="+s.bawan.transform.position+" P="+s.pihu.transform.position+" T="+s.tado.transform.position+(r?" indices="+Get(r,"bawanIndex")+","+Get(r,"pihuIndex")+","+Get(r,"tadoIndex"):"")+" school indices="+Get(b,"guideIndex")+","+Get(b,"otherIndex")+","+Get(b,"otherEscortIndex");}
    static IEnumerator Forest(bool leave)
    {
        var s=f.school;var b=s.Battle;var p=f.player;var r=b.Rescue;
        // Hold only the rescue update while the actual player controller walks
        // ahead, reproducing a stationary player occupying the injury marker.
        r.enabled=false;
        foreach(var point in new[]{new Vector3(0,0,82),new Vector3(0,0,85),new Vector3(-3,0,92)})
            yield return StoryWalk(point,.15f,20);
        r.enabled=true;Check("player occupies old blocked injury point",Flat(p.transform.position,new Vector3(-3,0,92))<.2f);
        float until=Time.realtimeSinceStartup+110,maxStep=0;Vector3 previousPihu=s.pihu.transform.position;
        while(r.CurrentStage==Chapter3Rescue.Stage.Advancing)
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("Forest approach stuck "+Snapshot()+" player="+p.transform.position);
            maxStep=Mathf.Max(maxStep,Flat(previousPihu,s.pihu.transform.position));previousPihu=s.pihu.transform.position;
            yield return null;
        }
        Check("forest reaches injury naturally",r.CurrentStage==Chapter3Rescue.Stage.Injury);
        Check("Pihu arrives beside occupied marker without teleport",maxStep<1.1f&&Flat(s.pihu.transform.position,p.transform.position)>1&&Flat(s.pihu.transform.position,r.InjuryPosition)<.2f);
        report.observations.Add("Player="+p.transform.position+" actual injury="+r.InjuryPosition+" largest Pihu frame step="+maxStep);
        yield return Wait(()=>r.CurrentStage==Chapter3Rescue.Stage.Choice,25,"injury choice");yield return Shot("injury-choice-"+mode);
        yield return Keys(leave?Key.Digit2:Key.Digit1,.12f);
        Check("keyboard rescue choice",r.Selected==(leave?Chapter3Rescue.Decision.Leave:Chapter3Rescue.Decision.Rescue));
        foreach(var e in b.Enemies.Where(e=>e&&!e.Defeated))e.Hit(100,p.transform.position);
        if(!leave)
        {
            yield return StoryWalk(r.InjuryPosition+new Vector3(1.35f,0,-.3f),.2f,15);
            yield return Keys(Key.E,2.35f);Check("held E lifts Pihu",r.Supporting&&p.speed<=1.2f);
            yield return Shot("supporting-"+mode);
        }
        foreach(var point in new[]{new Vector3(0,0,98),new Vector3(8,0,103),r.ReceptionPosition})
            yield return StoryWalk(point,.4f,55,()=>r.CurrentStage==Chapter3Rescue.Stage.Handoff||r.CurrentStage==Chapter3Rescue.Stage.Complete);
        yield return Wait(()=>r.Ending&&r.Ending.Active,35,"reception handoff");
        Check("choice preserves physical Pihu outcome",leave?Flat(s.pihu.transform.position,r.InjuryPosition)<1:Flat(s.pihu.transform.position,r.ReceptionPosition)<4);
        var ending=r.Ending;yield return Shot("reception-"+mode);
        bool sawRollCall=false,sawChat=false;
        until=Time.realtimeSinceStartup+240;
        while(ending.CurrentStage!=Chapter3Ending.Stage.Complete)
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("Ending timeout: "+ending.CurrentStage);
            sawRollCall|=ending.CurrentStage==Chapter3Ending.Stage.RollCall;
            sawChat|=ending.CurrentStage==Chapter3Ending.Stage.Conversation||f.ui.chat.activeSelf;
            yield return null;
        }
        Check("Mona rollcall completes without ending chat",sawRollCall&&!sawChat&&ending.Conversation==null&&ending.RollCallCompleted);
        Check("branch outcome preserved",ending.DeathConfirmed==leave&&ending.Result.squadSurvivors==(leave?3:4));
        Check("actual central party counted",ending.Result.centralNpcCount==11&&ending.Result.LeftCount==1&&ending.Result.RightCount==1&&ending.Result.CentralCount==12);
        report.observations.Add(ending.LeftReport+" / "+ending.CentralReport+" / "+ending.RightReport);
        Check("chapter ending is playable to completion",ending.CurrentStage==Chapter3Ending.Stage.Complete&&f.ui.ending.activeSelf&&!ending.Saved);
        yield return Shot("chapter-complete-"+mode);
    }
    static IEnumerator CrowdShow()
    {
        var s=f.school;var p=f.player;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        a.enabled=false;s.enabled=false;p.canMove=false;
        foreach(var t in a.spectators)t.gameObject.SetActive(false);
        var pair=new[]{a.spectators[0].GetComponent<Chapter3SchoolCrowdActor>(),a.spectators[10].GetComponent<Chapter3SchoolCrowdActor>()};
        for(int i=0;i<pair.Length;i++)
        {
            pair[i].gameObject.SetActive(true);pair[i].ResetPose();pair[i].SetAnimated(true);
            pair[i].transform.SetPositionAndRotation(new Vector3(10+i*2,0,-18),Quaternion.identity);
            report.observations.Add(pair[i].name+" skin="+pair[i].UsingOriginalSkin);
        }
        p.Warp(new Vector3(11,.06f,-13.8f),new Vector3(11,1,-18));yield return new WaitForSeconds(.4f);yield return Shot("crowd-forward-"+mode);
        for(float t=0;t<1.5f;t+=Time.deltaTime){foreach(var c in pair)c.MoveTowards(c.transform.position+Vector3.forward*4,1.4f);yield return null;}
        yield return Shot("crowd-walking-"+mode);
    }}
