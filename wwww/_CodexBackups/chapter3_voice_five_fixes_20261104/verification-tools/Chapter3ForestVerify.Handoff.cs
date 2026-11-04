using System;
using System.Collections;
using UnityEngine;
public static partial class Chapter3ForestVerify
{
    static IEnumerator HandoffProbe()
    {
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school fixture");
        f.school.StopAllCoroutines();f.school.enabled=false;f.ui.Say("",null);
        var environment=f.GetComponent<Chapter3ApproachEnvironment>();environment.SetVisible(true);
        var actor=f.school.pihu;var start=new Vector3(-1.88f,0,-21.29f);actor.transform.position=start;actor.GroundHeight=0;
        f.player.Warp(new Vector3(7,.06f,-18),start+Vector3.up);Physics.SyncTransforms();
        report.observations.Add("Isolated fixture restores the observed continuous-handoff Pihu position. No production position is changed.");
        report.observations.Add("flat clear="+Chapter3PathMotor.Clear(start,start)+" actual slope height="+environment.GroundHeight(start));
        foreach(var c in Physics.OverlapCapsule(start+Vector3.up*.42f,start+Vector3.up*1.25f,.32f,~0,QueryTriggerInteraction.Ignore))
            if(!c.GetComponentInParent<Chapter2Actor>())report.observations.Add("overlap="+c.name+" "+c.bounds);
        var motor=new Chapter3PathMotor();
        for(float t=0;t<5;t+=Time.deltaTime){motor.Move(actor,new Vector3(8,0,-8),2.9f);yield return null;}
        Check("old slope handoff reproduces blocked flat motor",Flat(actor.transform.position,start)<.1f);
        Vector3 end=new Vector3(-1.4f,0,-17.8f);float deadline=Time.time+12,maxStep=0;
        while(Flat(actor.transform.position,end)>.2f){if(Time.time>deadline)throw new Exception("slope exit blocked "+actor.transform.position);var before=actor.transform.position;Invoke(f.Approach,"MoveActor",actor,end,2.4f,motor);maxStep=Mathf.Max(maxStep,Flat(before,actor.transform.position));yield return null;}
        Check("slope-aware walk reaches flat apron without teleport",maxStep<.25f&&Chapter3PathMotor.Clear(actor.transform.position,actor.transform.position));
        start=actor.transform.position;motor.Reset();
        for(float t=0;t<2;t+=Time.deltaTime){motor.Move(actor,new Vector3(8,0,-8),2.9f);yield return null;}
        Check("school motor proceeds from flat apron",Flat(actor.transform.position,start)>2);
        report.observations.Add("Pihu slope exit largest frame step="+maxStep+" final="+actor.transform.position);yield return Shot("handoff-fixed");
    }
}
