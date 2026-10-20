using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static partial class Chapter3FiveFixProbe
{
    // Controlled shooting fixture: production region entry and production
    // Weapons.Attack/Trace, with target placement, enemy cease-fire and story
    // progression explicitly isolated. No Hit/HitByRifle damage injection.
    static IEnumerator CombatScenario()
    {
        flow.Carry(Chapter3Weapons.Weapon.Rifle);
        flow.EnterCombat();
        yield return Wait(()=>flow.combat.Fighting,20,"mountain combat starts");
        var combat=flow.combat;
        Note("Combat fixture: enemy templates cloned into a clear near-player lane; live guards held inactive, Combat.Update disabled during ray/region checks; every damage assertion calls Weapons.Attack.");
        combat.enabled=false;
        foreach(var guard in combat.Enemies)if(guard)guard.gameObject.SetActive(false);
        yield return ProbeRegionShots("mountain",combat.WorldPoint(new Vector3(0,0,-8)));
        combat.enabled=true;

        flow.EnterSchool();
        yield return Wait(()=>flow.school.Exploring,12,"school exploration starts");
        flow.player.Warp(flow.school.tado.transform.position+Vector3.back*2,flow.school.tado.transform.position+Vector3.up);
        yield return Wait(()=>flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,20,"school mission choice");
        Check("choose Tado school assault",flow.school.Battle.Choose(Chapter3SchoolBattle.Mission.Tado));
        yield return Wait(()=>flow.school.Battle.CombatActive,25,"school assault starts");
        foreach(var guard in combat.Enemies)if(guard)guard.gameObject.SetActive(false);
        yield return ProbeRegionShots("school",new Vector3(0,0,-12));
        flow.player.canMove=flow.player.canLook=true;
        Note("Ray-region fixture is complete; this does not claim an unassisted human playthrough or headset/build validation.");
    }

    static IEnumerator ProbeRegionShots(string region,Vector3 position)
    {
        var combat=flow.combat;
        var player=flow.player;
        player.canMove=player.canLook=false;
        foreach(string targetName in new[]{"Head","Torso","Left upper arm","Right forearm","Left thigh","Right shin","Left foot"})
        {
            var go=UnityEngine.Object.Instantiate(combat.enemyTemplates[0]);
            go.name="Validation "+region+" "+targetName;
            go.transform.position=position;
            var enemy=go.GetComponent<Chapter3Enemy>();
            enemy.combat=combat;enemy.advances=false;enemy.AttackEnabled=false;go.SetActive(true);
            yield return Frames(4);
            var zones=go.GetComponentsInChildren<Chapter3EnemyHitZone>();
            Check(region+" "+targetName+" 12 skeleton hit zones",zones.Length==12);
            Check(region+" "+targetName+" old root capsule disabled",go.GetComponents<Collider>().All(c=>!c.enabled));
            var zone=zones.FirstOrDefault(z=>z.name=="Hit zone · "+targetName);
            Check(region+" "+targetName+" target zone exists",zone);
            if(!zone){UnityEngine.Object.Destroy(go);continue;}
            bool lower=zone.Region==Chapter3EnemyHitZone.BodyRegion.Leg;
            float headY=enemy.actor.Rig.Head.position.y;
            combat.weapons.ResetEquipment();
            yield return AimAndFireZone(region+" "+targetName+" first shot",enemy,zone);
            Check(region+" "+targetName+" health after first shot",enemy.Health==(lower?50:0));
            Check(region+" "+targetName+" first-shot defeated state",enemy.Defeated==!lower);
            if(lower&&!enemy.Defeated)
            {
                yield return new WaitForSeconds(.6f);
                var pose=enemy.GetComponent<Chapter3EnemyWoundPose>();
                Check(region+" "+targetName+" visible crouch weight",pose&&pose.Weight>.95f);
                Check(region+" "+targetName+" head lowered >= .25m",headY-enemy.actor.Rig.Head.position.y>.25f);
                Check(region+" "+targetName+" leg wound retained",enemy.LegWounded);
                Check(region+" "+targetName+" temporarily holding fire",enemy.HoldingFire);
                yield return Shot(region+" "+targetName+" wounded crouch");
                Check(region+" "+targetName+" real reload starts",combat.weapons.Reload());
                yield return Wait(()=>!combat.weapons.Reloading,6,region+" "+targetName+" reload finishes");
                Check(region+" "+targetName+" real reload consumed reserve",combat.weapons.Reloads==1&&combat.weapons.Reserve==7);
                zone=zones.First(z=>z.name=="Hit zone · Left foot");
                yield return AimAndFireZone(region+" "+targetName+" second shot to foot",enemy,zone);
                Check(region+" "+targetName+" second leg/foot shot defeats",enemy.Defeated&&enemy.Health==0);
            }
            UnityEngine.Object.Destroy(go);
            yield return Frames(2);
        }
        // Physical occlusion: a temporary solid box must take the ray before
        // the torso. Keeping this test outside narrative routes avoids moving
        // or disabling any actual environmental collider.
        var coverTarget=UnityEngine.Object.Instantiate(combat.enemyTemplates[0]);
        coverTarget.name="Validation occlusion target";coverTarget.transform.position=position;
        var protectedEnemy=coverTarget.GetComponent<Chapter3Enemy>();
        protectedEnemy.combat=combat;protectedEnemy.advances=false;protectedEnemy.AttackEnabled=false;coverTarget.SetActive(true);
        yield return Frames(4);
        var torso=coverTarget.GetComponentsInChildren<Chapter3EnemyHitZone>().First(z=>z.name=="Hit zone · Torso");
        player.Warp(position+Vector3.back*3+Vector3.up*.06f,torso.Centre);
        player.FocusOn(torso.Centre);
        var box=GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name="Validation bullet cover";
        box.transform.position=Vector3.Lerp(player.view.transform.position,torso.Centre,.5f);
        box.transform.localScale=new Vector3(1.2f,1.6f,.3f);
        combat.weapons.ResetEquipment();Physics.SyncTransforms();
        Check(region+" cover shot accepted",combat.weapons.Attack());
        Check(region+" solid cover blocks body damage",protectedEnemy.Health==100);
        Check(region+" cover feedback",combat.weapons.Feedback=="擊中掩體");
        UnityEngine.Object.Destroy(box);UnityEngine.Object.Destroy(coverTarget);
        yield return Frames(2);
        // The real knife fan enumerates Combat.Enemies. Register this one
        // target for the test, then remove it without touching live guards.
        var knifeTarget=UnityEngine.Object.Instantiate(combat.enemyTemplates[0]);
        knifeTarget.name="Validation knife target";knifeTarget.transform.position=position;
        var knifeEnemy=knifeTarget.GetComponent<Chapter3Enemy>();
        knifeEnemy.combat=combat;knifeEnemy.advances=false;knifeEnemy.AttackEnabled=false;
        knifeTarget.SetActive(true);combat.Enemies.Add(knifeEnemy);
        yield return Frames(4);
        flow.Carry(Chapter3Weapons.Weapon.Knife);combat.weapons.ResetEquipment();
        bool knifeVisible=false;
        foreach(float angle in new[]{180f,135f,225f,90f,270f,0f})
        {
            Vector3 offset=Quaternion.Euler(0,angle,0)*Vector3.forward*1.3f;
            Vector3 standing=knifeEnemy.transform.position+offset;standing.y=knifeEnemy.actor.GroundHeight+.06f;
            player.Warp(standing,knifeEnemy.AimPoint);
            player.FocusOn(knifeEnemy.AimPoint);Physics.SyncTransforms();
            Vector3 d=knifeEnemy.AimPoint-player.view.transform.position;
            if(d.magnitude<=2.2f&&combat.Trace(player.view.transform.position,d.normalized,d.magnitude+.1f,out var knifeHit)
                &&knifeHit.collider.GetComponentInParent<Chapter3Enemy>()==knifeEnemy){knifeVisible=true;break;}
        }
        Check(region+" knife target visible through new body zones",knifeVisible);
        if(knifeVisible)
        {
            Check(region+" first real knife attack accepted",combat.weapons.Attack());
            yield return Wait(()=>knifeEnemy.Health==40,3,region+" knife first cut applies 60");
            Check(region+" first knife leaves enemy alive",!knifeEnemy.Defeated);
            yield return new WaitForSeconds(.75f);
            Check(region+" second real knife attack accepted",combat.weapons.Attack());
            yield return Wait(()=>knifeEnemy.Defeated,3,region+" second knife defeats");
            Check(region+" two real knife slashes",combat.weapons.Slashes==2);
        }
        combat.Enemies.Remove(knifeEnemy);UnityEngine.Object.Destroy(knifeTarget);
        flow.Carry(Chapter3Weapons.Weapon.Rifle);combat.weapons.ResetEquipment();
        yield return Frames(2);
        // Friend/civilian guards already precede the enemy branch in Attack.
        // Exercise real existing actors instead of substituting tag-only mocks.
        var friend=combat.Companions.FirstOrDefault(c=>c&&c.gameObject.activeInHierarchy);
        if(friend)
        {
            combat.weapons.ResetEquipment();
            Vector3 standing=friend.transform.position+Vector3.back*2;
            standing.y=friend.actor.GroundHeight+.06f;
            player.Warp(standing,friend.AimPoint);
            player.FocusOn(friend.AimPoint);Physics.SyncTransforms();
            if(combat.Trace(player.view.transform.position,player.view.transform.forward,65,out var hit)&&hit.collider.GetComponentInParent<Chapter3CompanionReaction>()==friend)
            {
                Check(region+" friendly fire rejected",!combat.weapons.Attack());
                Check(region+" friendly fire preserves ammo",combat.weapons.Loaded&&combat.weapons.Shots==0);
            }
            else Check(region+" friend fixture visible",false);
        }
    }

    static IEnumerator AimAndFireZone(string label,Chapter3Enemy enemy,Chapter3EnemyHitZone zone)
    {
        // Search the actual capsule's interior near its exposed surface. Its
        // centre can be hidden behind the torso or adjacent shin even when the
        // arm/shoe surface is visible. Classification stays exact throughout.
        bool found=false;
        var capsule=zone.GetComponent<CapsuleCollider>();
        Vector3 axis=zone.transform.up;
        float half=Mathf.Max(0,capsule.height*.5f-capsule.radius);
        Note(label+" geometry root="+enemy.transform.position.ToString("F4")+
            " ground="+enemy.actor.GroundHeight.ToString("F4")+" head="+enemy.actor.Rig.Head.position.ToString("F4")+
            " hips="+enemy.actor.Rig.Hips.position.ToString("F4")+" zone="+zone.Centre.ToString("F4")+
            " bounds="+capsule.bounds+" radius="+capsule.radius+" half="+half);
        int attempt=0;
        foreach(float angle in new[]{180f,135f,225f,90f,270f,0f,45f,315f})
        {
            Vector3 offset=Quaternion.Euler(0,angle,0)*Vector3.forward*2.5f;
            Vector3 standing=enemy.transform.position+offset;standing.y=enemy.actor.GroundHeight+.06f;
            flow.player.Warp(standing,zone.Centre);
            var camera=flow.player.view.transform;
            foreach(float fraction in new[]{0f,.65f,-.65f,.95f,-.95f})
            {
                Vector3 axial=zone.Centre+axis*(half*fraction);
                Vector3 outward=Vector3.ProjectOnPlane(camera.position-axial,axis).normalized;
                if(outward.sqrMagnitude<.01f)outward=zone.transform.right;
                Vector3 side=Vector3.Cross(axis,outward).normalized;
                foreach(Vector3 radial in new[]{Vector3.zero,outward*.86f,side*.75f,-side*.75f})
                {
                    Vector3 point=axial+radial*capsule.radius;
                    flow.player.FocusOn(point);Physics.SyncTransforms();attempt++;
                    bool traced=flow.combat.Trace(camera.position,camera.forward,8,out var hit);
                    var hitZone=traced?hit.collider.GetComponent<Chapter3EnemyHitZone>():null;
                    string obstruction=!traced?"none":hit.collider.name+
                        (hitZone?" ["+hitZone.Region+"]":" [non-zone]")+" at "+hit.point.ToString("F4");
                    Note(label+" ray "+attempt+" angle="+angle+" axis="+fraction+
                        " radial="+radial.ToString("F2")+" eye="+camera.position.ToString("F4")+" hit="+obstruction);
                    if(traced&&hitZone==zone){found=true;break;}
                }
                if(found)break;
            }
            if(found)break;
        }
        Check(label+" exact bone zone visible through production ray",found);
        if(found)
        {
            int shots=flow.combat.weapons.Shots;
            Check(label+" weapon accepted shot",flow.combat.weapons.Attack());
            Check(label+" exactly one round fired",flow.combat.weapons.Shots==shots+1&&!flow.combat.weapons.Loaded);
        }
        yield return Frames(1);
    }
}
