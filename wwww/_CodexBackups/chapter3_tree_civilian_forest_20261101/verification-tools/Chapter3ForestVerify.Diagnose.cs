using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
public static partial class Chapter3ForestVerify
{
    static IEnumerator Diagnose()
    {
        yield return Wait(()=>f.station.CurrentLine!=null,15,"station initialized");
        foreach(var t in f.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("駐在所林木 ")))
        {
            if(!t.gameObject.activeInHierarchy)continue;
            float distance=float.PositiveInfinity;Vector3 closest=Vector3.zero;
            var path=f.station.departurePath;
            for(int i=1;i<path.Length;i++)
            {var origin=path[i-1];var d=path[i]-origin;var q=origin+d*Mathf.Clamp01(Vector3.Dot(t.position-origin,d)/d.sqrMagnitude);float n=Flat(q,t.position);if(n<distance){distance=n;closest=q;}}
            if(distance<4)report.observations.Add("Station tree "+t.name+" root="+t.position+" distance="+distance+" closest="+closest);
        }
        report.observations.Add("departure="+string.Join(";",f.station.departurePath.Select(p=>p.ToString())));
        if(mode=="inventory")yield break;
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var b=s.Battle;var p=f.player;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        p.Warp(s.tado.transform.position+new Vector3(.5f,.06f,-2.5f),s.tado.transform.position+Vector3.up);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,30,"briefing");
        b.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>b.Fighting,25,"outbreak");
        yield return new WaitForSeconds(55);
        report.observations.Add("Before whistle escaped="+a.EscapedSpectators+" visible="+a.spectators.Count(t=>t.gameObject.activeSelf));
        foreach(var e in b.Enemies)e.Hit(100,p.transform.position);
        yield return Wait(()=>b.Step==4,4,"cleared");
        p.Warp(new Vector3(12,.06f,34),new Vector3(17,1,34));Check("whistle fixture",b.CallRetreat());
        p.Warp(new Vector3(0,.06f,78),new Vector3(-3,1,92));
        yield return Wait(()=>b.Rescue&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing,25,"rescue");
        p.Warp(new Vector3(-3,.06f,92),new Vector3(0,1,100));
        yield return new WaitForSeconds(55);
        report.observations.Add("Player occupies injury marker: "+Snapshot()+" player="+p.transform.position+" hint="+f.ui.hint.text);
        report.observations.Add("Crowd after retreat escaped="+a.EscapedSpectators+" active="+a.spectators.Count(t=>t.gameObject.activeSelf));
        Check("old forest occupied marker deadlock",b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing);
        yield return Shot("forest-stalled-before");
    }
}
