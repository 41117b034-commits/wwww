using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static partial class Chapter3RetreatProbe
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
        Check("twenty Japanese guests including two runners",a.spectators.Count(t=>t&&t.gameObject.activeInHierarchy)+a.runners.Count(t=>t&&t.gameObject.activeInHierarchy)==20);
        Check("ten adults and ten pupils",a.spectators.Count(t=>!t.name.Contains("學童"))==10&&a.spectators.Count(t=>t.name.Contains("學童"))==10);
        var authored=(Transform[])typeof(Chapter3SchoolAmbient).GetField("authoredSpectators",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(a);
        var woman=authored.First(t=>t&&t.name.Contains("婦人"));
        var womanMeshes=woman.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).ToArray();
        var detailCatalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");
        var allowedAdultMeshes=womanMeshes.Concat(detailCatalog.entries.Where(e=>womanMeshes.Contains(e.source)).Select(e=>e.distant)).ToArray();
        report.observations.Add("Japanese adult donor "+woman.name+" meshes="+string.Join(";",womanMeshes.Select(m=>m.name+" asset="+UnityEditor.AssetDatabase.GetAssetPath(m))));
        for(int i=0;i<10;i++)
        {
            report.observations.Add("Actual adult "+i+" "+a.spectators[i].name+" native="+a.spectators[i].GetComponent<Chapter3SchoolCrowdActor>().UsingOriginalSkin+" meshes="+string.Join(";",a.spectators[i].GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh?m.sharedMesh.name+" asset="+UnityEditor.AssetDatabase.GetAssetPath(m.sharedMesh):"NULL")));
            Check("adult "+i+" uses actual Japanese civilian geometry or its catalogued LOD",a.spectators[i].GetComponentsInChildren<MeshFilter>(true).All(m=>allowedAdultMeshes.Contains(m.sharedMesh))&&!a.spectators[i].GetComponent<Chapter3SchoolCrowdActor>().UsingOriginalSkin);
        }
        yield return Choose(f,mission);tracked=f;
        foreach(var e in f.school.Battle.Enemies)e.HoldFire(240);
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
        Check("school action visible from chosen player's route",visible>=2&&a.SkirmishActive);
        report.observations.Add("Actual "+mission+" player "+f.player.transform.position+" unobstructed attackers="+visible);
        Shot("chosen-route-scattered-crowd");
        yield return Wait(()=>a.FallenAdults==8&&a.FallenChildren==4&&a.RestrainedSpectators==2,65,"twelve scattered casualties and two adult holds");
        var natives=Enumerable.Range(0,10).Select(i=>a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>()).ToArray();
        Check("all ten Indigenous participants remain standing",natives.All(n=>n.gameObject.activeInHierarchy&&!n.IsFallen&&n.UsingOriginalSkin));
        Check("only two adult holds",natives.Count(n=>n.HoldingAtNeck)==2&&a.RestrainedSpectators==2);
        Check("six survivors flee or have physically escaped",a.FleeingSpectators+a.EscapedSpectators==6);
        var bodies=a.spectators.Where(t=>t.GetComponent<Chapter3SchoolCrowdActor>().IsFallen).ToArray();
        float separation=999;for(int i=0;i<bodies.Length;i++)for(int j=i+1;j<bodies.Length;j++)separation=Mathf.Min(separation,Flat(bodies[i].position,bodies[j].position));
        Check("fallen roots remain separate",separation>2.5f);
        Check("fallen directions are not a repeated row",bodies.Select(t=>Mathf.FloorToInt(t.eulerAngles.y/30)).Distinct().Count()>=5);
        report.observations.Add("Bodies min root separation="+separation+" headings="+string.Join(",",bodies.Select(t=>t.eulerAngles.y.ToString("F0"))));
        var starts=a.spectators.Select(t=>t.position).ToArray();var holdYaw=natives.Take(2).Select(n=>n.transform.rotation).ToArray();
        yield return new WaitForSeconds(5);yield return new WaitForEndOfFrame();
        ShotAt("hold-reach-west",new Vector3(-11.5f,1.7f,20.5f),a.spectators[0].position+Vector3.up*1.1f);
        ShotAt("hold-reach-east",new Vector3(10,1.7f,30),a.spectators[1].position+Vector3.up*1.1f);
        for(int i=0;i<2;i++)
        {
            var guest=a.spectators[i].GetComponent<Chapter3SchoolCrowdActor>();var knife=natives[i].transform.Find("族人手持獵刀");
            var blade=knife.position+knife.up*.15f;
            float gap=Vector3.Distance(blade,guest.NeckPoint);
            report.observations.Add("Hold "+i+" blade center to neck="+gap+" hand="+natives[i].RightHand.position+" neck="+guest.NeckPoint);
            Check("knife visibly beside adult neck "+i,gap<.15f);
            Check("hold pose stable rather than shaking "+i,Quaternion.Angle(holdYaw[i],natives[i].transform.rotation)<2);
        }
        for(int i=14;i<20;i++)
            Check("survivor "+i+" advances or exits without an idle pair",a.SpectatorPhase(i)==Chapter3SchoolAmbient.CrowdPhase.Escaped||Flat(starts[i],a.spectators[i].position)>1);
        ShotAt("scattered-overview",new Vector3(0,12,2),new Vector3(0,0,23));
        ShotAt("adult-hold-west",new Vector3(-11.5f,1.7f,20.5f),a.spectators[0].position+Vector3.up*1.1f);
        ShotAt("adult-hold-east",new Vector3(10,1.7f,30),a.spectators[1].position+Vector3.up*1.1f);
        ShotAt("scattered-casualties",new Vector3(-5,2.5f,7),new Vector3(-11,.5f,21));
        if(!report.mode.EndsWith("preview"))
        {
            if(mission==Chapter3SchoolBattle.Mission.Tado){ResumeTracking();yield return Assault(f,false);tracking=false;}
            else yield return LegacySchool(f,mission==Chapter3SchoolBattle.Mission.Pihu);
        }
        yield return ResetSchool(f);
        Check("F8 releases both holds and restores all escape states",a.RestrainedSpectators==0&&a.EscapedSpectators==0&&natives.All(n=>!n.HoldingAtNeck));
    }
}

