using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class Chapter3RetreatProbe
{
    static Chapter3SchoolAmbient watchedGroup;
    static Vector3[] priorGroup;
    static float groupDistance,largestGroupStep;
    static int groupJumps;
    public static void ObserveGroup()
    {
        if(!watchedGroup||!watchedGroup.Withdrawing||priorGroup==null)return;
        for(int i=0;i<10;i++)
        {
            Vector3 now=watchedGroup.Ambusher(i).position;
            float step=Flat(now,priorGroup[i]);groupDistance+=step;largestGroupStep=Mathf.Max(largestGroupStep,step);
            if(step>3.2f*Time.deltaTime+.08f)groupJumps++;
            priorGroup[i]=now;
        }
    }
    static void WatchGroup(Chapter3SchoolAmbient a)
    {watchedGroup=a;priorGroup=Enumerable.Range(0,10).Select(i=>a.Ambusher(i).position).ToArray();groupDistance=largestGroupStep=0;groupJumps=0;}
    static IEnumerator GroupScenario(Chapter3Flow f)
    {
        bool knife=report.mode.EndsWith("knife");
        f.Carry(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle);
        yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"normal school entry");
        CaptureCalm(f);CheckSurpriseCalm(f);
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        var b=f.school.Battle;var p=f.player;
        Check("no early retreat or whistle",!b.CallRetreat()&&!a.Withdrawing&&b.RetreatSignalCount==0);
        yield return Choose(f,Chapter3SchoolBattle.Mission.Tado);tracked=f;
        foreach(var e in b.Enemies)e.HoldFire(240);
        yield return Wait(()=>Flat(p.transform.position,f.school.tado.transform.position)>7.8f,15,"Tado clears the initial firing line");
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

        Check("third stage waits for whole-school signal",b.Step==2&&!b.RetreatCalled&&!a.Withdrawing&&(!a.Straggler||!a.Straggler.gameObject.activeInHierarchy));
        if(!knife)
        {
            yield return Walk(p,b.Ammo.position+Vector3.right,"ammo E priority",.3f);
            yield return Press(Key.E);
            Check("ammo E never sends a retreat signal",!b.RetreatCalled&&b.RetreatSignalCount==0);
        }
        yield return Walk(p,new Vector3(11.5f,0,31.8f),"rally area");
        var originals=Enumerable.Range(0,10).Select(i=>a.Ambusher(i).GetInstanceID()).ToArray();
        p.FocusOn(new Vector3(0,1,23));yield return Press(Key.E);
        Check("actual E starts collective retreat once",b.RetreatCalled&&a.Withdrawing&&b.RetreatSignalCount==1&&a.WithdrawalSignalFrame>=0);
        Check("signal plays audible nonzero whistle",b.RetreatSignalSource&&b.RetreatSignalSource.isPlaying&&!b.RetreatSignalSource.mute&&b.RetreatSignalSource.volume>0&&!AudioListener.pause);
        var clip=(AudioClip)typeof(Chapter3SchoolBattle).GetField("retreatWhistle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);
        var pcm=new float[clip.samples];clip.GetData(pcm,0);
        Check("whistle contains sound and bounded amplitude",clip.length>1.4f&&pcm.Max(v=>Mathf.Abs(v))>.1f&&pcm.Max(v=>Mathf.Abs(v))<.8f);
        Check("all ten original participants stop attacks and holds",!a.SkirmishActive&&a.RestrainedSpectators==0&&Enumerable.Range(0,10).All(i=>a.Ambusher(i).GetInstanceID()==originals[i]&&!a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>().HoldingAtNeck));
        Check("no replacement straggler spawned",!a.Straggler||!a.Straggler.gameObject.activeInHierarchy);
        yield return Press(Key.E);Check("repeated E cannot replay signal",b.RetreatSignalCount==1&&!b.CallRetreat());
        WatchGroup(a);yield return new WaitForSeconds(5);yield return new WaitForEndOfFrame();
        Shot("whole-school-response");ShotAt("school-participants-withdrawing",new Vector3(2,12,44),new Vector3(0,0,24));
        Check("the group does not abandon a stationary player",Enumerable.Range(0,10).All(i=>a.Ambusher(i).position.z<52));
        if(report.mode.EndsWith("reset"))
        {watchedGroup=null;yield return ResetSchool(f);Check("F8 cancels signal and collective movement",!a.Withdrawing&&a.WithdrawalSignalFrame<0&&b.RetreatSignalCount==0);yield break;}
        if(!knife)
        {
            watchedGroup=null;yield return RetryAssault(f,2,false);
            Check("retry restores participants inside school and requires a fresh signal",!a.Withdrawing&&!b.RetreatCalled&&a.VisibleAssailants==10&&Enumerable.Range(0,10).All(i=>a.Ambusher(i).position.z<45));
            yield return Walk(p,new Vector3(11.5f,0,31.8f),"retry rally area");yield return Press(Key.E);
            Check("fresh E after retry restarts all ten",b.RetreatCalled&&a.Withdrawing&&b.RetreatSignalCount==1);WatchGroup(a);
        }
        var withdrawal=b.AssaultWaypoints.ToArray();
        for(int i=0;i<withdrawal.Length;i++)
        {
            if(b.Step>2||b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete)break;
            bool last=i==withdrawal.Length-1;
            yield return Walk(p,last?V(3,77):withdrawal[i],"whole group follows player",last?.35f:1.2f,()=>b.Step>2||b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete);
            yield return new WaitForEndOfFrame();
            if(i==1||i==3)ShotAt("retreat-corridor-"+i,new Vector3(29,5,i==1?42:61),new Vector3(18,1,i==1?37:52));
        }
        Check("arriving first cannot skip missing participants",a.WithdrawalArrived||b.Step==2);
        yield return Wait(()=>a.WithdrawalArrived,150,"all ten physically reach rear");
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup||b.Complete,35,"whole-group regroup");
        Check("completion requires all ten outside school",b.Step==3&&b.RetreatArrived&&b.RetreatArrivedCount==10&&Enumerable.Range(0,10).All(i=>a.Ambusher(i).position.z>70&&Flat(a.Ambusher(i).position,a.WithdrawalDestination(i))<.6f));
        Check("same ten actors walk without teleporting",groupJumps==0&&groupDistance>400&&Enumerable.Range(0,10).All(i=>a.Ambusher(i).GetInstanceID()==originals[i]));
        float min=999;for(int i=0;i<10;i++)for(int j=i+1;j<10;j++)min=Mathf.Min(min,Flat(a.Ambusher(i).position,a.Ambusher(j).position));
        Check("rear group keeps separate standing places",min>1.2f);
        report.observations.Add("Retreat walked total="+groupDistance+" max frame step="+largestGroupStep+" jumps="+groupJumps+" rear min separation="+min);
        yield return new WaitForEndOfFrame();ShotAt("all-ten-rear-rendezvous",new Vector3(2,5,82),new Vector3(0,1,73));
        yield return Walk(p,new Vector3(0,0,78),"rendezvous");
        yield return Wait(()=>b.Rescue&&b.Rescue.Active,90,"unchanged rescue handoff");
        Check("three leaders and ten participants are outside alive",new[]{f.school.bawan,f.school.pihu,f.school.tado}.All(t=>t.transform.position.z>72&&t.FallProgress==0)&&Enumerable.Range(0,10).All(i=>a.Ambusher(i).gameObject.activeInHierarchy&&!a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>().IsFallen));
        watchedGroup=null;yield return ResetSchool(f);
        Check("F8 clears group routes and whistle",!a.Withdrawing&&a.WithdrawalSignalFrame<0&&!b.RetreatCalled&&b.RetreatSignalCount==0);
    }

    static string GroupState(Chapter3SchoolAmbient a)
    {
        if(!a||!a.Withdrawing)return " no active withdrawal";
        var legs=(int[])typeof(Chapter3SchoolAmbient).GetField("withdrawalLeg",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a);
        var routes=(Vector3[][])typeof(Chapter3SchoolAmbient).GetField("withdrawalRoutes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a);
        return " group="+string.Join(";",Enumerable.Range(0,10).Select(i=>i+":"+a.Ambusher(i).position+" leg="+legs[i]+" next="+(legs[i]<routes[i].Length?routes[i][legs[i]].ToString()+GroupCollision(a,i,routes[i][legs[i]]):"arrived")));
    }
    static string GroupCollision(Chapter3SchoolAmbient a,int i,Vector3 target)
    {
        var owner=a.Ambusher(i);var actor=owner.GetComponent<Chapter3SchoolCrowdActor>();var delta=target-owner.position;delta.y=0;
        bool yielding=(bool)typeof(Chapter3SchoolAmbient).GetMethod("YieldToWithdrawal",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(a,new object[]{i,delta.normalized});
        var hits=Physics.CapsuleCastAll(owner.position+Vector3.up*.35f,owner.position+Vector3.up*1.25f,.23f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        return " yield="+yielding+" wait="+actor.WaitingAtObstacle+" hits="+string.Join(",",hits.Where(h=>!h.collider.transform.IsChildOf(owner)&&h.collider.bounds.max.y>.28f).Select(h=>h.collider.name+"@"+h.collider.transform.position+" d="+h.distance));
    }
}

