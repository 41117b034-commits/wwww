using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Install together with the main temporary probe only after stopping Play.
// These scenarios test runtime checkpoint snapshots, not a full assault replay.
public static partial class Chapter3RetreatProbe
{
    static IEnumerator CheckpointScenario(Chapter3Flow f,bool wounded)
    {
        string label=wounded?"future-leg-wound":"future-defeated";
        tracking=false;
        report.observations.Add(label+": Carry(Rifle) and F8 are explicit setup fixtures. HoldFire(300) on the two real officers suppresses incoming damage so checkpoint state can be isolated. Every officer wound/kill below comes from the actual combat.Trace and weapons.Attack; no Hit, RestoreEncounterState, Step assignment, SaveOfficerCheckpoint or reflection state writes are used.");
        report.observations.Add(label+": player approaches and completes both stages through Chapter3Player.Move/CharacterController and the authored waypoints. FocusOn only aims the current camera. No actor or player Warp/position fixture is used. Damage(100) later injects player failure solely to exercise the real Down -> R checkpoint retry.");
        f.Carry(Chapter3Weapons.Weapon.Rifle);
        yield return Press(Key.F8);
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,label+" F8 school entry");
        CaptureCalm(f);CheckSurpriseCalm(f);
        yield return Choose(f,Chapter3SchoolBattle.Mission.Tado);
        var b=f.school.Battle;var p=f.player;var w=f.combat.weapons;
        Check(label+" initial two officers remain present",b.Step==0&&b.CheckpointStep==0&&b.Enemies.Count==2&&b.Enemies.All(e=>e&&!e.Defeated));
        foreach(var officer in b.Enemies)officer.HoldFire(300);
        var first=b.Enemies[0];var future=b.Enemies[1];
        int futureId=future.GetInstanceID();
        report.observations.Add(label+": before shot, future officer phase="+b.OfficerState(1)+", walked="+b.OfficerWalkedDistance(1).ToString("F3")+"m, position="+future.transform.position.ToString("F3")+". Tado's short response route can already be Ready by Fighting; this scenario checks that injury/death keeps it stopped, not that this particular shot interrupts an unfinished walk.");
        yield return Route(p,new[]{V(0,-3),V(0,3),V(3,7),V(3,12)},label+" ordinary shooting approach");
        yield return CheckpointRifleShot(f,future,wounded?Chapter3EnemyHitZone.BodyRegion.Leg:Chapter3EnemyHitZone.BodyRegion.Head,label+" future officer");
        int expectedHealth=wounded?50:0;
        Check(label+" real shot gives requested future state",future.Health==expectedHealth&&future.LegWounded==wounded&&future.Defeated==!wounded);
        Check(label+" future damage does not skip first checkpoint",b.Step==0&&b.CheckpointStep==0&&!first.Defeated);
        if(wounded)
            yield return Wait(()=>future.GetComponent<Chapter3EnemyWoundPose>()&&future.GetComponent<Chapter3EnemyWoundPose>().Weight>.95f,4,label+" initial visible crouch");
        else
            yield return Wait(()=>future.actor.fallen&&future.actor.FallProgress>=.99f,5,label+" initial fallen pose");
        Vector3 stoppedAt=future.transform.position;
        yield return new WaitForSeconds(1.1f);
        Check(label+" future officer stays stopped and cannot attack",Flat(future.transform.position,stoppedAt)<.025f&&
            b.OfficerState(1)==(wounded?Chapter3SchoolBattle.OfficerPhase.Wounded:Chapter3SchoolBattle.OfficerPhase.Defeated)&&
            !future.AttackEnabled&&future.Shots==0&&future.Health==expectedHealth);
        yield return new WaitForEndOfFrame();
        Shot(label+"-before-checkpoint");
        ShotAt(label+"-future-pose-before",future.transform.position+new Vector3(-3,1.7f,-2.8f),future.transform.position+Vector3.up*.8f);

        yield return CheckpointRifleShot(f,first,Chapter3EnemyHitZone.BodyRegion.Head,label+" current officer");
        Check(label+" first officer is defeated by actual rifle",first.Defeated);
        yield return Walk(p,V(-5,16),label+" first natural gathering",1.2f,()=>b.Step>0);
        yield return Wait(()=>b.Step==1,35,label+" natural checkpoint one");
        Check(label+" future instance survives stage transition",b.CheckpointStep==1&&b.Enemies[1]==future&&future.GetInstanceID()==futureId&&future.Health==expectedHealth);
        Check(label+" checkpoint snapshots future health wound and death",
            b.CheckpointOfficerHealth(1)==expectedHealth&&b.CheckpointOfficerLegWounded(1)==wounded&&b.CheckpointOfficerDefeated(1)==!wounded);
        Check(label+" completed first officer is recorded defeated",b.CheckpointOfficerDefeated(0));
        report.observations.Add(label+": natural checkpoint snapshot health="+b.CheckpointOfficerHealth(1)+", legWounded="+b.CheckpointOfficerLegWounded(1)+", defeated="+b.CheckpointOfficerDefeated(1)+", position="+future.transform.position.ToString("F3"));
        int retries=b.Retries;
        b.Damage(100,p.transform.position);
        yield return null;
        Check(label+" injected failure reaches Down without losing checkpoint",b.CurrentStage==Chapter3SchoolBattle.Stage.Down&&b.CheckpointStep==1);
        yield return Press(Key.R);
        yield return Wait(()=>b.Fighting,5,label+" real R retry");
        yield return null;yield return new WaitForEndOfFrame();
        Check(label+" retry preserves route checkpoint weapon and retry count",b.Selected==Chapter3SchoolBattle.Mission.Tado&&b.Step==1&&b.CheckpointStep==1&&b.Retries==retries+1&&b.Health==100&&f.CarriedWeapon==Chapter3Weapons.Weapon.Rifle&&w.Selected==Chapter3Weapons.Weapon.Rifle);
        Check(label+" completed earlier officer never respawns",!b.Enemies[0]||b.Enemies[0].Defeated);
        var restored=b.Enemies[1];
        Check(label+" checkpoint rebuilds a fresh future instance",restored&&restored.GetInstanceID()!=futureId&&restored.gameObject.activeInHierarchy);
        Check(label+" restored health is not reset by Start",restored.Health==expectedHealth&&restored.LegWounded==wounded&&restored.Defeated==!wounded&&restored.Shots==0);
        Check(label+" restored wounded or defeated officer stays at saved position",Flat(restored.transform.position,stoppedAt)<.025f);
        if(wounded)
        {
            restored.HoldFire(300);
            yield return Wait(()=>restored.GetComponent<Chapter3EnemyWoundPose>()&&restored.GetComponent<Chapter3EnemyWoundPose>().Weight>.95f,4,label+" restored crouch pose");
            var zones=restored.GetComponentsInChildren<Chapter3EnemyHitZone>(true);
            Check(label+" restored crouching officer retains twelve active hit zones",zones.Length==12&&zones.All(z=>z.GetComponent<Collider>().enabled&&!z.GetComponent<Collider>().isTrigger));
            Check(label+" restored wound cannot resume route walking",b.OfficerState(1)==Chapter3SchoolBattle.OfficerPhase.Wounded&&!restored.actor.fallen);
        }
        else
        {
            yield return Wait(()=>restored.actor.fallen&&restored.actor.FallProgress>=.99f,5,label+" restored fallen pose");
            Check(label+" restored corpse disables every collider and rifle",restored.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)&&(!restored.rifle||!restored.rifle.enabled));
            Check(label+" restored corpse is defeated with no attack",b.OfficerState(1)==Chapter3SchoolBattle.OfficerPhase.Defeated&&!restored.AttackEnabled&&restored.Health==0);
        }
        yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();
        Check(label+" saved future state remains stable after pose initialization",restored.Health==expectedHealth&&Flat(restored.transform.position,stoppedAt)<.025f);
        ShotAt(label+"-future-pose-restored",restored.transform.position+new Vector3(-3,1.7f,-2.8f),restored.transform.position+Vector3.up*.8f);
        Check(label+" snapshot itself remains unchanged after retry",b.CheckpointOfficerHealth(1)==expectedHealth&&b.CheckpointOfficerLegWounded(1)==wounded&&b.CheckpointOfficerDefeated(1)==!wounded);

        int shotsAfterRetry=w.Shots;
        if(wounded)
        {
            yield return Route(p,new[]{V(-4,20),V(3,20)},label+" restored wound firing approach");
            yield return CheckpointRifleShot(f,restored,Chapter3EnemyHitZone.BodyRegion.Head,label+" finish restored wounded officer");
            Check(label+" restored officer still accepts normal damage",restored.Defeated&&w.Shots==shotsAfterRetry+1);
        }
        else
            Check(label+" previously defeated future officer needs no repeat shot",restored.Defeated&&w.Shots==shotsAfterRetry);
        foreach(var point in b.AssaultWaypoints.ToArray())
        {
            if(b.Step>1)break;
            yield return Walk(p,point,label+" second natural gathering",1.2f,()=>b.Step>1);
        }
        yield return Wait(()=>b.Step==2,35,label+" natural checkpoint two after retry");
        Check(label+" retry permits normal onward progression",b.Fighting&&b.Step==2&&b.CheckpointStep==2&&restored.Defeated&&!b.StragglerCalled);
        Check(label+" second natural checkpoint records defeat",b.CheckpointOfficerDefeated(1));
        Check(label+" partial mission leaves persisted result untouched",OriginalSaveMatches());
        yield return ResetSchool(f);
        Check(label+" cleanup clears runtime future-state snapshot",b.CheckpointOfficerHealth(0)==100&&b.CheckpointOfficerHealth(1)==100&&!b.CheckpointOfficerLegWounded(0)&&!b.CheckpointOfficerLegWounded(1)&&!b.CheckpointOfficerDefeated(0)&&!b.CheckpointOfficerDefeated(1));
    }

    static IEnumerator CheckpointRifleShot(Chapter3Flow f,Chapter3Enemy enemy,Chapter3EnemyHitZone.BodyRegion region,string label)
    {
        var w=f.combat.weapons;
        yield return Wait(()=>w.Ready,5,label+" rifle ready");
        if(!w.Loaded)
        {
            yield return Press(Key.R);
            yield return Wait(()=>w.Loaded&&!w.Reloading,6,label+" actual R single-round reload");
        }
        yield return new WaitForEndOfFrame();
        Vector3 point;string trace;
        bool clear=CheckpointRegionRay(f,enemy,region,out point,out trace);
        report.observations.Add(label+" initial ray diagnostic: "+trace);
        if(!clear)
        {
            // Remain in the reachable centre of the current authored area.
            // This is actual walking, not a camera or target positioning fixture.
            yield return Walk(f.player,V(3,18),label+" clearer normal sight line",.45f);
            yield return new WaitForEndOfFrame();
            clear=CheckpointRegionRay(f,enemy,region,out point,out trace);
            report.observations.Add(label+" walked sight-line diagnostic: "+trace);
        }
        Check(label+" Trace reaches requested live hit region",clear);
        int beforeShots=w.Shots,beforeHealth=enemy.Health;
        f.player.FocusOn(point);Physics.SyncTransforms();
        bool actual=f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var hit);
        var zone=actual&&hit.collider?hit.collider.GetComponent<Chapter3EnemyHitZone>():null;
        Check(label+" focused weapon ray confirms exact target and region",zone&&zone.Enemy==enemy&&zone.Region==region);
        report.observations.Add(label+" actual Attack ray collider="+hit.collider.name+", zone="+zone.Region+
            ", origin="+f.player.view.transform.position.ToString("F3")+", hit="+hit.point.ToString("F3")+
            ", enemyRoot="+enemy.transform.position.ToString("F3")+", healthBefore="+beforeHealth);
        Check(label+" actual weapon Attack accepted",w.Attack());
        yield return new WaitForSeconds(.6f);
        Check(label+" one real rifle shot applies expected region damage",w.Shots==beforeShots+1&&enemy.Health==Mathf.Max(0,beforeHealth-(region==Chapter3EnemyHitZone.BodyRegion.Leg?50:100)));
    }

    static bool CheckpointRegionRay(Chapter3Flow f,Chapter3Enemy enemy,Chapter3EnemyHitZone.BodyRegion region,out Vector3 point,out string diagnostic)
    {
        point=Vector3.zero;Physics.SyncTransforms();
        Vector3 origin=f.player.view.transform.position;
        var blockers=new HashSet<string>();
        foreach(var zone in enemy.GetComponentsInChildren<Chapter3EnemyHitZone>(true).Where(z=>z.Region==region))
        {
            var collider=zone.GetComponent<Collider>();if(!collider||!collider.enabled)continue;
            Bounds bounds=collider.bounds;
            var candidates=new[]{bounds.center,Vector3.Lerp(collider.ClosestPoint(origin),bounds.center,.15f),
                bounds.center+Vector3.up*bounds.extents.y*.45f,bounds.center-Vector3.up*bounds.extents.y*.45f};
            foreach(var candidate in candidates)
            {
                if(!f.combat.Trace(origin,(candidate-origin).normalized,65,out var hit))
                {blockers.Add("miss toward "+zone.name);continue;}
                var hitZone=hit.collider.GetComponent<Chapter3EnemyHitZone>();
                if(hitZone&&hitZone.Enemy==enemy&&hitZone.Region==region)
                {
                    point=candidate;diagnostic="clear "+hit.collider.name+" "+hitZone.Region+" from "+origin.ToString("F3")+"; earlier blockers=["+string.Join("; ",blockers)+"]";
                    return true;
                }
                blockers.Add(hit.collider.name+" zone="+(hitZone?hitZone.Region.ToString():"none")+" enemy="+(hitZone&&hitZone.Enemy?hitZone.Enemy.name:"none"));
            }
        }
        diagnostic="no visible "+region+" from "+origin.ToString("F3")+" toward "+enemy.transform.position.ToString("F3")+"; blockers=["+string.Join("; ",blockers)+"]";
        return false;
    }
}
