using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class Chapter3RetreatProbe
{
    static readonly HashSet<string> guideSamples=new HashSet<string>();
    static void ObserveGuidanceFrame()
    {
        if(report==null||!tracked)return;
        var b=tracked.school.Battle;if(!b)return;
        var r=b.Rescue;
        var guide=r&&r.Active?r.FollowGuide:b.Selected==Chapter3SchoolBattle.Mission.Tado&&b.Fighting?b.AssaultGuide:b.FollowGuide;
        string label=report.mode+" "+(r&&r.Active?"rescue "+r.CurrentStage:b.Selected+" "+b.CurrentStage+" step="+b.Step);
        if(guide&&guide.VisibleMarks>0&&guideSamples.Add(label))
        {Check("visible follow arrows "+label,true);report.observations.Add(label+" yellow arrows="+guide.VisibleMarks);Shot("arrows-"+label.Replace(" ","-"));}
    }
    static void CheckAmbushBeforeChoice(Chapter3Flow f)
    {
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("no assault before route selection",!a.Panicking&&a.AmbushSignalFrame<0&&!a.SkirmishActive);
        for(int i=0;i<3;i++)Check("concealed ambusher "+i+" already inside school",a.Ambusher(i)&&!a.Ambusher(i).gameObject.activeSelf&&a.AmbushStart(i).z>5&&Mathf.Abs(a.AmbushStart(i).x)<19&&Flat(a.Ambusher(i).position,a.AmbushStart(i))<.01f);
        Check("ambushers start spread across the school",Flat(a.AmbushStart(0),a.AmbushStart(1))>15&&Flat(a.AmbushStart(1),a.AmbushStart(2))>15);
    }
    static IEnumerator ObservePersistentBattle(Chapter3Flow f)
    {
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        foreach(var enemy in f.school.Battle.Enemies)if(enemy)enemy.HoldFire(100);
        report.observations.Add("Persistent ambient/crowd observation temporarily holds the two mission officers' fire; it does not modify the panic clock, crowd positions, chosen route, stages or steps.");
        float began=Time.realtimeSinceStartup;
        while(a.SkirmishPairsReady<3)
        {
            if(Time.realtimeSinceStartup-began>45)throw new Exception("persistent pairs stalled: "+a.SkirmishPairsReady+" positions="+string.Join(";",Enumerable.Range(0,3).Select(i=>a.Ambusher(i).position.ToString())));
            Progress("persistent skirmish readiness");yield return new WaitForEndOfFrame();
        }
        Check("all three fights still run after opening speeches",a.SkirmishActive&&f.school.Battle.Fighting&&a.AssaultElapsed>12&&a.SkirmishPairsReady==3);
        var people=a.GetComponentsInChildren<Chapter3SchoolCrowdActor>().Where(c=>!c.IsFallen&&c.gameObject.activeInHierarchy).ToArray();
        var initial=people.Select(c=>c.transform.position).ToArray();var previous=(Vector3[])initial.Clone();
        var heading=people.Select(c=>c.transform.eulerAngles.y).ToArray();var turnSign=new int[people.Length];var reversals=new int[people.Length];var distance=new float[people.Length];
        var maxRadius=new float[people.Length];float observationStart=Time.time;
        var wrist=a.Ambusher(0).GetComponent<Chapter3SchoolCrowdActor>().RightHand;
        Vector3 firstWrist=wrist.position;float wristMotion=0;
        while(Time.time-observationStart<8)
        {
            yield return new WaitForEndOfFrame();Progress("stationary crowd eight-second motion sample");
            wristMotion=Mathf.Max(wristMotion,Vector3.Distance(firstWrist,wrist.position));
            for(int i=0;i<people.Length;i++)
            {
                var t=people[i].transform;float turn=Mathf.DeltaAngle(heading[i],t.eulerAngles.y);int sign=turn>4?1:turn< -4?-1:0;
                if(sign!=0){if(turnSign[i]!=0&&sign!=turnSign[i])reversals[i]++;turnSign[i]=sign;}
                distance[i]+=Flat(t.position,previous[i]);maxRadius[i]=Mathf.Max(maxRadius[i],Flat(t.position,initial[i]));previous[i]=t.position;heading[i]=t.eulerAngles.y;
            }
        }
        Check("ongoing weapon/arm attack animation",wristMotion>.12f);
        for(int i=0;i<people.Length;i++)
        {
            report.observations.Add("Motion "+people[i].name+" distance="+distance[i]+" maxRadius="+maxRadius[i]+" yawReversals="+reversals[i]+" shelter="+people[i].WaitingAtObstacle);
            Check("no repeated in-place left/right oscillation "+people[i].name,!(maxRadius[i]<.8f&&distance[i]>1.4f&&reversals[i]>3));
        }
        ShotAt("persistent-battle-overview",new Vector3(0,4,7),new Vector3(0,1,26));
        ShotAt("settled-crowd-corner",new Vector3(21,1.8f,8),new Vector3(12,1,25));
    }
    static IEnumerator AmbushPreview(Chapter3Flow f)
    {
        var mission=report.mode.EndsWith("bawan")?Chapter3SchoolBattle.Mission.Bawan:report.mode.EndsWith("pihu")?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Tado;
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"school preview");
        CaptureCalm(f);CheckSurpriseCalm(f);yield return Choose(f,mission);tracked=f;
        foreach(var enemy in f.school.Battle.Enemies)if(enemy)enemy.HoldFire(180);
        var points=mission==Chapter3SchoolBattle.Mission.Bawan?new[]{V(-7,-8),V(-23,-4),V(-27,8)}:
            mission==Chapter3SchoolBattle.Mission.Pihu?new[]{V(8,-8),V(25,-4),V(27.5f,8)}:new[]{V(0,-3),V(0,3),V(-4,8)};
        bool arrowsSeen=false;
        foreach(var target in points)
        {
            float start=Time.realtimeSinceStartup;
            while(Flat(f.player.transform.position,target)>.5f)
            {
                if(Time.realtimeSinceStartup-start>45)throw new Exception("preview walk stalled");
                Move(f.player,target);yield return new WaitForEndOfFrame();
                var guide=mission==Chapter3SchoolBattle.Mission.Tado?f.school.Battle.AssaultGuide:f.school.Battle.FollowGuide;
                if(guide&&guide.VisibleMarks>0)arrowsSeen=true;
            }
        }
        Check("yellow arrows visible while following chosen leader",arrowsSeen);
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        f.player.FocusOn(new Vector3(0,1.1f,25));yield return new WaitForEndOfFrame();
        int visible=0;
        for(int i=0;i<3;i++)
        {
            var target=a.Ambusher(i).position+Vector3.up*1.45f;var cam=f.player.view;var v=cam.WorldToViewportPoint(target);
            bool blocked=Physics.RaycastAll(cam.transform.position,(target-cam.transform.position).normalized,Vector3.Distance(target,cam.transform.position),~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.transform.IsChildOf(a.Ambusher(i))&&!h.collider.transform.IsChildOf(f.player.transform)&&h.collider.bounds.max.y>.3f);
            if(v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1&&!blocked)visible++;
        }
        Check("selected route sees ongoing fight inside school",a.SkirmishActive&&a.SkirmishPairsReady>=2&&visible>=1);
        report.observations.Add("Selected route actual player view: "+mission+" visible ambushers="+visible+" player="+f.player.transform.position);
        Shot("chosen-route-ongoing-battle");
        if(mission!=Chapter3SchoolBattle.Mission.Tado)
        {yield return LegacySchool(f,mission==Chapter3SchoolBattle.Mission.Pihu);Check("regroup clears battle follow arrows",f.school.Battle.FollowGuide.VisibleMarks==0);}
        yield return ResetSchool(f);
    }
}
