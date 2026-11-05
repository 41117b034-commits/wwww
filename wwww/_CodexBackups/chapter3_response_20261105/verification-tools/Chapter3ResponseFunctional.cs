using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static partial class Chapter3ResponseVerify
{
    static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    static IEnumerator Until(Func<bool> valid,float seconds,string label)
    {float end=Time.realtimeSinceStartup+seconds;while(!valid()&&Time.realtimeSinceStartup<end)yield return null;Check(label,valid());}
    static IEnumerator Shot(Mouse mouse,bool down)
    {InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,down));yield return null;yield return null;}
    static IEnumerator Keypress(Keyboard keyboard,Key key)
    {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
    static IEnumerator Capture(string name)
    {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Dir+name+".png");yield return null;}
    static void Arms(string name)
    {
        foreach(var arm in Object.FindObjectsByType<Chapter3ViewArm>(FindObjectsSortMode.None))
        {
            var source=(Vector3[])Field(arm,"source");var vertices=arm.GetComponent<MeshFilter>().sharedMesh.vertices;
            var weights=(float[])Field(arm,"weights");var end=(Vector3)Field(arm,"end");
            var extension=arm.transform.InverseTransformPoint(arm.view.transform.TransformPoint(new Vector3(arm.left?-.95f:.95f,-.65f,-.35f)))-end;
            float error=0;for(int i=0;i<source.Length;i++)error=Mathf.Max(error,Vector3.Distance(vertices[i],source[i]+extension*weights[i]));
            Check(name+" arm geometry "+arm.left,error<.0011f&&vertices.Length==source.Length);
            Check(name+" finite arm normals "+arm.left,arm.GetComponent<MeshFilter>().sharedMesh.normals.All(n=>!float.IsNaN(n.x)&&!float.IsNaN(n.y)&&!float.IsNaN(n.z)&&n.sqrMagnitude>.9f));
        }
    }
    static IEnumerator Functional(Chapter3Flow f)
    {
        report.notes.Add("Synthetic Input System events and explicit setup/camera Warps; not a human keyboard, VR or standalone test. Enemy firing disabled only during weapon isolation. Physics and meshes remain active.");
        var originalMouse=Mouse.current;var originalKeyboard=Keyboard.current;
        var mouse=InputSystem.AddDevice<Mouse>("Response test mouse");var keyboard=InputSystem.AddDevice<Keyboard>("Response test keyboard");
        try
        {
            f.EnterStation();yield return Frames(6);f.station.StopAllCoroutines();f.ui.fade.color=Color.clear;f.ui.Say("",null);
            var sign=f.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="往霧社的路標");
            Check("sign points along eastbound route",Vector3.Dot(sign.right,Vector3.right)>.999f);
            Check("sign moved outside road centre",Mathf.Abs(sign.position.z+98)>3);
            Check("sign original caption retained",sign.GetComponentInChildren<TextMesh>(true).text=="霧社 →");
            Check("sign colliders retained",sign.GetComponentsInChildren<Collider>().All(c=>c.enabled));
            f.player.Warp(new Vector3(-13.5f,.03f,-101),new Vector3(-9.8f,1,-94.9f));yield return Frames(3);yield return Capture("sign-direction");
            f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);foreach(var e in f.combat.Enemies)e.enabled=false;
            f.ui.fade.color=Color.clear;f.ui.Say("",null);yield return new WaitForSeconds(2);
            var w=f.combat.weapons;f.player.FocusOn(f.player.view.transform.position+Vector3.up*20+Vector3.forward*2);yield return Frames(3);
            Check("foreground for input test",Application.isFocused);Arms("rifle idle");
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;int shots=w.Shots;
            double clickAt=Time.realtimeSinceStartupAsDouble;yield return Shot(mouse,true);
            report.notes.Add("First click observed response ms="+((Time.realtimeSinceStartupAsDouble-clickAt)*1000)+" (two coroutine frame observation)");
            Check("unlocked first click fires one shot",w.Shots==shots+1&&!w.Loaded);yield return Shot(mouse,false);
            int reserve=w.Reserve;yield return Keypress(keyboard,UnityEngine.InputSystem.Key.R);
            Check("R during recoil retained",w.ReloadQueued||w.Reloading);
            yield return Until(()=>w.Reloading,3,"reload follows recoil");
            shots=w.Shots;yield return Shot(mouse,true);yield return Shot(mouse,false);
            Check("early reload click not stored",!w.AttackQueued&&w.Shots==shots);
            yield return new WaitForEndOfFrame();Arms("reload");yield return Capture("rifle-reload");
            yield return Until(()=>!w.Reloading,5,"single round reload finishes");yield return new WaitForSeconds(.4f);
            Check("early click causes no delayed shot",w.Shots==shots&&w.Loaded&&w.Reserve==reserve-1);
            yield return Shot(mouse,true);yield return Shot(mouse,false);yield return Keypress(keyboard,UnityEngine.InputSystem.Key.R);yield return Until(()=>w.Reloading,3,"second reload begins");
            yield return Until(()=>w.ReloadProgress>.945f,4,"reload reaches buffer window");shots=w.Shots;
            yield return Shot(mouse,true);yield return Shot(mouse,false);yield return Until(()=>w.Shots==shots+1,2,"late reload click fires once on completion");
            yield return new WaitForSeconds(.5f);Check("rifle never repeats while held",w.Shots==shots+1);
            f.Carry(Chapter3Weapons.Weapon.Knife);w.ResetEquipment();yield return Frames(3);Cursor.lockState=CursorLockMode.Locked;
            yield return Shot(mouse,true);yield return new WaitForSeconds(1.5f);yield return Shot(mouse,false);
            Check("held knife repeats at normal cadence",w.Slashes>=3&&w.Slashes<=4);
            int slashes=w.Slashes;yield return new WaitForSeconds(.8f);Check("release stops knife repetition",w.Slashes==slashes);
            Check("start knife buffer test",w.Attack());
            yield return Until(()=>Time.time>(float)Field(w,"busyUntil")-.18f,2,"knife approaches end of cooldown");
            yield return Shot(mouse,true);yield return Shot(mouse,false);yield return Until(()=>w.Slashes==slashes+2,2,"late knife tap retained once");
            yield return new WaitForEndOfFrame();Arms("knife swing");yield return Capture("knife-swing");
            yield return new WaitForSeconds(.8f);Check("buffered knife does not repeat",w.Slashes==slashes+2);
            w.Attack();yield return Until(()=>Time.time>(float)Field(w,"busyUntil")-.18f,2,"focus cancel setup");Check("knife request buffered",w.RequestAttack()&&w.AttackQueued);
            Call(w,"OnApplicationFocus",false);slashes=w.Slashes;yield return new WaitForSeconds(.4f);Check("focus loss clears queued attack",!w.AttackQueued&&w.Slashes==slashes);
            w.Attack();yield return Until(()=>Time.time>(float)Field(w,"busyUntil")-.18f,2,"escape cancel setup");w.RequestAttack();yield return Keypress(keyboard,UnityEngine.InputSystem.Key.Escape);slashes=w.Slashes;yield return new WaitForSeconds(.4f);Check("escape clears queue",!w.AttackQueued&&w.Slashes==slashes&&Cursor.lockState==CursorLockMode.None);
            yield return DetailChecks(f);
            f.EnterSchool();yield return new WaitForSeconds(4);f.ui.Say("",null);f.player.FocusOn(new Vector3(0,1.5f,20));yield return Frames(3);
            var indicator=f.school.GetComponent<Chapter3ObjectiveIndicator>();
            Check("gate left target remains visible on screen edge",indicator.Visible&&indicator.Offscreen&&indicator.ScreenPosition.x<0);yield return Capture("school-gate-guide");
            Vector3 player=f.player.transform.position;f.player.FocusOn(f.school.tado.transform.position+Vector3.up*1.1f);yield return Frames(3);
            Check("turning toward target moves marker above target",indicator.Visible&&!indicator.Offscreen);Check("guide never moves player",Vector3.Distance(player,f.player.transform.position)<.05f);yield return Capture("school-tado-visible");
            f.player.FocusOn(f.player.view.transform.position+Vector3.back*10);yield return Frames(3);Check("target behind keeps edge marker",indicator.Visible&&indicator.Offscreen);yield return Capture("school-target-behind");
            f.EnterSchool();yield return new WaitForSeconds(2);Check("F8 reset has single indicator",f.ui.canvas.GetComponentsInChildren<Chapter3ObjectivePointer>(true).Length==1);
            f.player.Warp(f.school.tado.transform.position+Vector3.back*2,f.school.tado.transform.position+Vector3.up);yield return Frames(3);
            Check("arriving starts Tado meeting and hides guide",f.school.MetTado&&!indicator.Visible);
            f.EnterStation();yield return Frames(3);Check("leaving school hides guide",!indicator.Visible&&!w.AttackQueued&&!w.ReloadQueued);
            f.EnterSchool();yield return new WaitForSeconds(2);Check("returning school restores marker",indicator.Visible);
        }
        finally
        {InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);if(originalMouse!=null)originalMouse.MakeCurrent();if(originalKeyboard!=null)originalKeyboard.MakeCurrent();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
    static IEnumerator DetailChecks(Chapter3Flow f)
    {
        var detail=f.GetComponent<Chapter3ActorDetail>();Check("actor detail installed",detail&&detail.BindingCount>0&&detail.DistantCount>0);
        var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");
        var skin=f.combat.bawan.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>catalog.entries.Any(e=>e.source==s.sharedMesh||e.distant==s.sharedMesh));
        var entry=catalog.entries.First(e=>e.source==skin.sharedMesh||e.distant==skin.sharedMesh);var bones=skin.bones;var materials=skin.sharedMaterials;
        Vector3 origin=f.player.transform.position;Quaternion rotation=f.player.transform.rotation;
        Vector3 target=f.combat.bawan.transform.position;
        f.player.Warp(target+Vector3.back*2,target+Vector3.up);yield return Frames(4);Check("near actor restores full original mesh",skin.sharedMesh==entry.source);yield return Capture("actor-near-original");
        f.player.Warp(target+Vector3.back*7,target+Vector3.up);yield return Frames(4);Check("far actor uses reduced mesh",skin.sharedMesh==entry.distant);yield return Capture("actor-far-detail");
        Check("detail preserves skeleton and materials",bones.SequenceEqual(skin.bones)&&materials.SequenceEqual(skin.sharedMaterials));
        detail.enabled=false;Check("disabling detail restores originals",skin.sharedMesh==entry.source);detail.enabled=true;yield return Frames(3);Check("reenabling detail reapplies distance",skin.sharedMesh==entry.distant);
        f.player.Warp(origin,origin+rotation*Vector3.forward*10);
        report.notes.Add("Actor bindings="+detail.BindingCount+" distant="+detail.DistantCount);
    }
    static IEnumerator CombatAndPose(Chapter3Flow f)
    {
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);f.ui.fade.color=Color.clear;
        foreach(var e in f.combat.Enemies){e.HoldFire(300);e.AttackEnabled=false;}
        yield return new WaitForSeconds(3);
        var w=f.combat.weapons;var enemy=f.combat.Enemies[0];var detail=f.GetComponent<Chapter3ActorDetail>();
        Check("rifle test active enemy is damageable",enemy.isActiveAndEnabled&&!enemy.Defeated);
        bool aimed=false;
        foreach(var direction in new[]{Vector3.back,Vector3.left,Vector3.right,Vector3.forward})
        {
            f.player.Warp(enemy.transform.position+direction*7,enemy.AimPoint);yield return Frames(3);Physics.SyncTransforms();
            if(f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,20,out var hit)&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy){aimed=true;break;}
        }
        Check("clear actual rifle ray to police",aimed);
        var skin=enemy.actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.sharedMesh.vertexCount>60000);
        var originalBones=skin.bones;var originalMaterials=skin.sharedMaterials;int colliders=enemy.GetComponentsInChildren<Collider>().Count(c=>c.enabled);
        Check("police uses compatible grip LOD",skin.sharedMesh.vertexCount<200000&&skin.sharedMesh.blendShapeCount==3);
        var gun=enemy.rifle.weapon.GetComponentsInChildren<MeshFilter>().OrderByDescending(m=>m.sharedMesh.vertexCount).First();
        Check("NPC rifle reduced at shooting distance",gun.sharedMesh.vertexCount<150000);
        PoseBounds(skin,enemy.actor.Rig,"standing police");yield return Capture("police-rifle-detail");
        Check("actual distant rifle shot accepted",w.RequestAttack());yield return Frames(3);Check("actual distant police hit and defeated",enemy.Defeated&&w.Shots==1&&!w.Loaded);
        yield return new WaitForSeconds(2.5f);yield return new WaitForEndOfFrame();PoseBounds(skin,enemy.actor.Rig,"fallen police");
        Check("fallen police visible with intact skeleton",skin.enabled&&skin.gameObject.activeInHierarchy&&originalBones.SequenceEqual(skin.bones)&&originalMaterials.SequenceEqual(skin.sharedMaterials));
        Check("fall disables colliders through normal defeat",colliders>0&&enemy.GetComponentsInChildren<Collider>().All(c=>!c.enabled));yield return Capture("police-fallen-detail");
        f.Carry(Chapter3Weapons.Weapon.Knife);w.ResetEquipment();enemy=f.combat.Enemies[1];aimed=false;
        foreach(var direction in new[]{Vector3.back,Vector3.left,Vector3.right,Vector3.forward})
        {
            f.player.Warp(enemy.transform.position+direction*1.4f,enemy.AimPoint);yield return Frames(3);Physics.SyncTransforms();
            if(f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,3,out var hit)&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy){aimed=true;break;}
        }
        Check("clear actual knife ray",aimed);skin=enemy.actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.sharedMesh.vertexCount>60000);
        Check("melee restores original police mesh",skin.sharedMesh.vertexCount==767047);
        Check("actual knife starts",w.RequestAttack());yield return new WaitForSeconds(.4f);Check("actual knife applies one hit",enemy.Health==40);
        yield return Until(()=>w.Ready,2,"knife ready for second cut");w.RequestAttack();yield return new WaitForSeconds(.4f);Check("second actual knife cut defeats",enemy.Defeated);
        f.EnterSchool();yield return new WaitForSeconds(3);f.player.FocusOn(f.school.tado.transform.position+Vector3.up);
        var tado=f.school.tado.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.sharedMesh.vertexCount>60000);
        var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");var entry=catalog.entries.Single(e=>e.distant==tado.sharedMesh);
        PoseBounds(tado,null,"crouched Tado",entry.source);yield return Capture("tado-crouch-detail");
        int first=detail.BindingCount;
        for(int i=0;i<3;i++){f.EnterCombat();yield return new WaitForSeconds(1.3f);f.EnterSchool();yield return new WaitForSeconds(1.3f);}
        Check("repeated shortcuts do not accumulate actor bindings",detail.BindingCount<=first+10);
        Check("F8 clears weapon buffers and hides weapon",!w.AttackQueued&&!w.ReloadQueued&&!w.gameObject.activeInHierarchy);
        report.notes.Add("Real collision traces and public weapon requests, with explicit camera Warp and enemy HoldFire/AttackEnabled=false fixtures. No enemy health or damage result was assigned by the probe.");
    }
    static void PoseBounds(SkinnedMeshRenderer skin,Chapter1IncidentRig rig,string label,Mesh authored=null)
    {
        Mesh current=skin.sharedMesh;if(rig)Check(label+" grip provenance",rig.TryGetGripMeshes(skin,out var _,out authored));
        var mesh=new Mesh();Bounds originalBounds=skin.localBounds;var weights=Enumerable.Range(0,current.blendShapeCount).Select(i=>skin.GetBlendShapeWeight(i)).ToArray();
        try
        {
            skin.BakeMesh(mesh);mesh.RecalculateBounds();Bounds distant=mesh.bounds;
            skin.sharedMesh=authored;for(int i=0;i<weights.Length;i++)skin.SetBlendShapeWeight(i,weights[i]);skin.BakeMesh(mesh);mesh.RecalculateBounds();Bounds full=mesh.bounds;
            float error=Mathf.Max(Vector3.Distance(full.min,distant.min),Vector3.Distance(full.max,distant.max))*skin.transform.lossyScale.magnitude;
            report.notes.Add(label+" conservative world bound error="+error);
            Check(label+" silhouette bounds preserved",error<.08f);
        }
        finally{skin.sharedMesh=current;skin.localBounds=originalBounds;for(int i=0;i<weights.Length;i++)skin.SetBlendShapeWeight(i,weights[i]);Object.Destroy(mesh);}
    }
}
