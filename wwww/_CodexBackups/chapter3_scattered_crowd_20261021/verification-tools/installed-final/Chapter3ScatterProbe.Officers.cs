using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Chapter3ScatterProbe
{
    static Chapter3Flow officerFlow;
    static readonly int[] officerIds=new int[2];
    static readonly Vector3[] officerLast=new Vector3[2];
    static readonly HashSet<Chapter3SchoolBattle.OfficerPhase>[] phases={
        new HashSet<Chapter3SchoolBattle.OfficerPhase>(),new HashSet<Chapter3SchoolBattle.OfficerPhase>()};
    static bool officerWatching,officerPersistent,officerContinuous,officerQuiet,arrivalPreserved;
    static Vector3 arrivalPlayer,arrivalBawan,arrivalPihu,arrivalTado;
    static Chapter3SchoolBattle.Stage priorOfficerStage;
    static float lastOfficerTime;
    static int officerFrames;
    static void StartOfficerObservation(Chapter3Flow f)
    {
        var b=f.school.Battle;
        Check("two calm officers already attend the gathering",b.VisibleOfficers==2&&b.Enemies.Count==2);
        for(int i=0;i<2;i++)
        {
            Check("calm officer "+i+" does not aim or shoot",b.OfficerState(i)==Chapter3SchoolBattle.OfficerPhase.Calm&&!b.Enemies[i].AttackEnabled&&!b.Enemies[i].Telegraphing&&b.Enemies[i].Shots==0);
            officerIds[i]=b.Enemies[i].GetInstanceID();officerLast[i]=b.Enemies[i].transform.position;
            phases[i].Clear();phases[i].Add(b.OfficerState(i));
        }
        officerFlow=f;officerWatching=true;officerPersistent=officerContinuous=officerQuiet=arrivalPreserved=true;
        lastOfficerTime=Time.time;officerFrames=0;priorOfficerStage=b.CurrentStage;
        arrivalPlayer=f.player.transform.position;arrivalBawan=f.school.bawan.transform.position;
        arrivalPihu=f.school.pihu.transform.position;arrivalTado=f.school.tado.transform.position;
    }
    static void ObserveOfficerFrame()
    {
        if(!officerWatching||!officerFlow)return;
        var f=officerFlow;var b=f.school.Battle;
        float maxStep=Mathf.Max(.55f,(Time.time-lastOfficerTime)*3.5f);
        for(int i=0;i<2;i++)
        {
            var e=b.Enemies.Count>i?b.Enemies[i]:null;
            officerPersistent&=e&&e.GetInstanceID()==officerIds[i]&&e.gameObject.activeInHierarchy;
            if(!e)continue;
            phases[i].Add(b.OfficerState(i));
            officerContinuous&=Flat(e.transform.position,officerLast[i])<=maxStep;
            officerLast[i]=e.transform.position;
            if(b.CurrentStage!=Chapter3SchoolBattle.Stage.Fighting)
                officerQuiet&=e.Shots==0&&!e.AttackEnabled&&!e.Telegraphing;
        }
        if(b.Fighting&&priorOfficerStage==Chapter3SchoolBattle.Stage.Outbreak)
        {
            arrivalPreserved=Flat(f.player.transform.position,arrivalPlayer)<.1f&&
                Flat(f.school.bawan.transform.position,arrivalBawan)<maxStep&&
                Flat(f.school.pihu.transform.position,arrivalPihu)<maxStep&&
                Flat(f.school.tado.transform.position,arrivalTado)<maxStep;
        }
        priorOfficerStage=b.CurrentStage;lastOfficerTime=Time.time;officerFrames++;
    }
    static void FinishOfficerObservation()
    {
        ObserveOfficerFrame();officerWatching=false;
        Check("officers remain the same visible instances through outbreak",officerPersistent);
        Check("responding officers move continuously without teleport",officerContinuous);
        Check("officers do not attack while player is in outbreak presentation",officerQuiet);
        Check("first fighting handoff preserves player and companion arrival positions",arrivalPreserved);
        for(int i=0;i<2;i++)
        {
            Check("officer "+i+" visibly notices before moving",phases[i].Contains(Chapter3SchoolBattle.OfficerPhase.Noticing)&&phases[i].Contains(Chapter3SchoolBattle.OfficerPhase.Moving));
            Check("officer "+i+" actually walks",officerFlow.school.Battle.OfficerWalkedDistance(i)>1);
        }
        report.observations.Add("Officer outbreak observation frames="+officerFrames+
            "; same instances, transforms and AttackEnabled/shot counters monitored in LateUpdate.");
    }
}
