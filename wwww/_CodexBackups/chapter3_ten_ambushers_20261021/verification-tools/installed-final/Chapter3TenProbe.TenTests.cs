using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static partial class Chapter3TenProbe
{
    static IEnumerator ClickFire()
    {
        if(Mouse.current==null)throw new Exception("Mouse unavailable");
        InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSeconds(.12f);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSeconds(.15f);
    }
    static IEnumerator ReloadInput(Chapter3Weapons w)
    {
        yield return Wait(()=>w.Ready,4,"reload ready");
        yield return Press(Key.R);Check("R starts actual reload",w.Reloading);
        yield return Wait(()=>w.Loaded&&!w.Reloading,4,"reload finished");
    }
    static IEnumerator InputChecks(Chapter3Flow f)
    {
        var w=f.combat.weapons;var b=f.school.Battle;
        var health=b.Enemies.Select(e=>e.Health).ToArray();
        yield return Wait(()=>Flat(f.player.transform.position,f.school.tado.transform.position)>7.8f,15,"Tado naturally waits for trailing player");
        yield return new WaitForSeconds(.7f);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForEndOfFrame();
        f.player.FocusOn(f.school.tado.transform.position+Vector3.up*1.1f);Physics.SyncTransforms();
        if(f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var diagnostic))report.observations.Add("Tado trace: "+diagnostic.collider.name+" "+diagnostic.collider.transform.position);
        Check("real Tado blocks muzzle trace",f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var friend)&&friend.collider.GetComponentInParent<Chapter3CompanionReaction>());
        int shots=w.Shots;yield return ClickFire();
        report.observations.Add("Friend shot input result: shots="+shots+" -> "+w.Shots+" loaded="+w.Loaded+" feedback="+w.Feedback);
        Check("friend obstruction still produces shot ammo and feedback",w.Shots==shots+1&&!w.Loaded&&w.Feedback.Contains("同伴擋住"));
        Check("bullet never travels through Tado to police",b.Enemies.Select(e=>e.Health).SequenceEqual(health));
        yield return new WaitForSeconds(.5f);shots=w.Shots;yield return ClickFire();
        Check("empty chamber gives R explanation without false shot",w.Shots==shots&&w.Feedback.Contains("空膛"));
        yield return ReloadInput(w);
        yield return Press(Key.Escape);Check("Esc releases mouse",Cursor.lockState==CursorLockMode.None);
        f.player.FocusOn(f.player.transform.position+new Vector3(0,20,-20));shots=w.Shots;
        yield return ClickFire();Check("same click recaptures cursor and fires",w.Shots==shots+1&&Cursor.lockState==CursorLockMode.Locked);
        yield return ReloadInput(w);
    }
    static IEnumerator TenScenario(Chapter3Flow f)
    {
        var mission=report.mode.EndsWith("bawan")?Chapter3SchoolBattle.Mission.Bawan:report.mode.EndsWith("pihu")?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Tado;
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"F8 normal school");
        CaptureCalm(f);CheckSurpriseCalm(f);
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("exactly twenty active Japanese guests",a.spectators.Count(t=>t&&t.gameObject.activeInHierarchy)+a.runners.Count(t=>t&&t.gameObject.activeInHierarchy)==20);
        Check("ten adults and ten pupils",a.spectators.Count(t=>!t.name.Contains("學童"))==10&&a.spectators.Count(t=>t.name.Contains("學童"))==10);
        Check("male and female adult roles",a.spectators.Count(t=>t.name.Contains("男教師"))==5&&a.spectators.Count(t=>t.name.Contains("女教師"))==5);
        yield return Choose(f,mission);tracked=f;
        foreach(var e in f.school.Battle.Enemies)e.HoldFire(240);
        if(mission==Chapter3SchoolBattle.Mission.Tado)yield return InputChecks(f);
        var points=mission==Chapter3SchoolBattle.Mission.Bawan?new[]{V(-7,-8),V(-23,-4),V(-27,8)}:
            mission==Chapter3SchoolBattle.Mission.Pihu?new[]{V(8,-8),V(25,-4),V(27.5f,8)}:new[]{V(0,-3),V(0,3),V(-4,8)};
        foreach(var point in points)yield return Walk(f.player,point,"chosen approach");
        f.player.FocusOn(new Vector3(0,1,23));yield return new WaitForEndOfFrame();
        int visible=0;var camera=f.player.view;
        for(int i=0;i<10;i++)
        {
            var t=a.Ambusher(i);var point=t.position+Vector3.up*1.2f;var screen=camera.WorldToViewportPoint(point);
            bool blocked=Physics.RaycastAll(camera.transform.position,(point-camera.transform.position).normalized,Vector3.Distance(point,camera.transform.position),~0,QueryTriggerInteraction.Ignore)
                .Any(h=>!h.collider.transform.IsChildOf(t)&&!h.collider.transform.IsChildOf(f.player.transform)&&h.collider.bounds.max.y>.3f);
            if(screen.z>0&&screen.x>0&&screen.x<1&&screen.y>0&&screen.y<1&&!blocked)visible++;
        }
        Check("ongoing combat visible from chosen player's route",visible>=2&&a.SkirmishActive);
        report.observations.Add("Actual "+mission+" player "+f.player.transform.position+" unobstructed attackers="+visible);
        Shot("chosen-route-ten-ambushers");
        yield return Wait(()=>a.AssaultElapsed>45,60,"sustained action");
        float start=Time.time;var actors=Enumerable.Range(0,10).Select(i=>a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>()).ToArray();
        var wrists=actors.Select(t=>t.RightHand.position).ToArray();float maxWrist=0;
        while(Time.time-start<5)
        {
            yield return new WaitForEndOfFrame();
            for(int i=0;i<10;i++)maxWrist=Mathf.Max(maxWrist,Vector3.Distance(wrists[i],actors[i].RightHand.position));
            for(int i=0;i<a.spectators.Length;i++)if(a.SpectatorPhase(i)==Chapter3SchoolAmbient.CrowdPhase.Falling)
                Check("collapse follows nearby attacker "+i,Flat(a.spectators[i].position,a.Ambusher(i%10).position)<2.1f);
        }
        Check("knife-hand movement continues after entry",maxWrist>.1f);
        yield return new WaitForEndOfFrame();ShotAt("ten-actors-overview",new Vector3(0,10,2),new Vector3(0,0,22));
        yield return Wait(()=>a.FallenAdults==10&&a.FallenChildren==10,100,"all staged cuts and collapses");
        Check("running pupils switch once without duplicate bodies",a.runners.All(r=>!r.gameObject.activeInHierarchy)&&a.spectators.All(t=>t.gameObject.activeInHierarchy));
        report.observations.Add("Final staged aftermath adults="+a.FallenAdults+" children="+a.FallenChildren);
        yield return new WaitForEndOfFrame();ShotAt("adult-child-aftermath",new Vector3(0,5,5),new Vector3(10,0,15));
        if(mission==Chapter3SchoolBattle.Mission.Tado){ResumeTracking();yield return Assault(f,false);tracking=false;}
        else yield return LegacySchool(f,mission==Chapter3SchoolBattle.Mission.Pihu);
        yield return ResetSchool(f);
    }
}
