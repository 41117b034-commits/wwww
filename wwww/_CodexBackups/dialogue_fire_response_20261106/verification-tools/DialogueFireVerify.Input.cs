using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static partial class DialogueFireVerify
{
    static IEnumerator Click(Mouse mouse,bool down)
    {InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,down));yield return null;}
    static IEnumerator Press(Keyboard keyboard,Key key)
    {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
    static IEnumerator InputChecks()
    {
        var oldMouse=Mouse.current;var oldKeyboard=Keyboard.current;
        var mouse=InputSystem.AddDevice<Mouse>("Chapter3 response probe");var keyboard=InputSystem.AddDevice<Keyboard>("Chapter3 response keys");
        try
        {
            for(int area=0;area<2;area++)
            {
                string label=area==0?"mountain":"school";
                f.Carry(Chapter3Weapons.Weapon.Rifle);
                if(area==0)
                {f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);}
                else
                {
                    f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school exploring");
                    f.player.Warp(f.school.tado.transform.position+new Vector3(.5f,.06f,-2.5f),f.school.tado.transform.position+Vector3.up);
                    yield return Wait(()=>f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,40,"real Tado briefing");
                    Check("choose Tado",f.school.Battle.Choose(Chapter3SchoolBattle.Mission.Tado));
                    yield return Wait(()=>f.school.Battle.Fighting,25,"school combat starts");
                    f.player.Warp(new Vector3(-5,.06f,16),new Vector3(0,20,20));
                }
                foreach(var e in f.combat.Enemies){e.HoldFire(1000);e.AttackEnabled=false;}
                f.ui.fade.color=Color.clear;f.ui.Say("",null);f.player.FocusOn(f.player.view.transform.position+Vector3.up*20+Vector3.forward*2);
                yield return new WaitForSeconds(1);
                var w=f.combat.weapons;
                Check(label+" focused and ready",Application.isFocused&&w.Ready&&w.Loaded);
                int pulseShots=w.Shots;InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
                Check(label+" press and release within one input update fires",w.Shots==pulseShots+1);
                yield return new WaitForSeconds(.6f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.R));InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                Check(label+" short R pulse starts reload",w.Reloading);yield return Wait(()=>!w.Reloading,5,"pulse reload");w.ResetEquipment();yield return Frames(2);
                int shots=w.Shots;Cursor.lockState=CursorLockMode.None;
                int inputFrame=Time.frameCount;yield return Click(mouse,true);
                Check(label+" ready click fires on next input frame",w.Shots==shots+1&&!w.Loaded&&Time.frameCount-inputFrame<=1);
                yield return Click(mouse,false);yield return Press(keyboard,UnityEngine.InputSystem.Key.R);
                Check(label+" recoil R retained",w.ReloadQueued||w.Reloading);
                yield return Wait(()=>w.Reloading,3,"reload started");
                shots=w.Shots;yield return Click(mouse,true);yield return Click(mouse,false);
                Check(label+" early reload click rejected explicitly",!w.AttackQueued&&w.Shots==shots);
                yield return Wait(()=>!w.Reloading,5,"reload complete");
                Check(label+" exactly one cartridge loaded",w.Loaded&&w.Reloads==1&&w.Reserve==7);
                yield return Click(mouse,true);yield return Click(mouse,false);yield return Press(keyboard,UnityEngine.InputSystem.Key.R);
                yield return Wait(()=>w.Reloading,3,"second reload");
                yield return Wait(()=>w.ReloadProgress>.94f,4,"late reload press window");
                shots=w.Shots;yield return Click(mouse,true);
                bool accepted=w.AttackQueued||w.Shots==shots+1;
                Check(label+" late trigger accepted",accepted);
                bool queued=w.AttackQueued;
                // One deliberate 400 ms rendering hitch crosses the old 250 ms
                // expiry. Simulation clock and real Update still perform firing.
                System.Threading.Thread.Sleep(400);
                yield return Click(mouse,false);yield return Frames(2);
                report.notes.Add(label+" queued before hitch="+queued+" shots after="+w.Shots+" expected="+(shots+1));
                if(mode=="input-before")Check(label+" old accepted trigger lost across hitch",queued&&w.Shots==shots&&w.Loaded);
                else Check(label+" accepted trigger survives hitch exactly once",w.Shots==shots+1&&!w.Loaded);
                yield return new WaitForSeconds(.6f);Check(label+" rifle never repeats",w.Shots==(mode=="input-before"?shots:shots+1));
                yield return Capture(label+"-input-"+mode);
                if(mode=="input-before")continue;
                w.ResetEquipment();w.Attack();yield return new WaitForSeconds(.5f);w.Reload();
                yield return Wait(()=>w.ReloadProgress>.94f,4,"focus cancellation window");
                Check(label+" queue for focus test",w.RequestAttack()&&w.AttackQueued);Call(w,"OnApplicationFocus",false);
                shots=w.Shots;yield return new WaitForSeconds(.5f);Check(label+" focus cancels buffered trigger",!w.AttackQueued&&w.Shots==shots);
                f.Carry(Chapter3Weapons.Weapon.Knife);w.ResetEquipment();Cursor.lockState=CursorLockMode.Locked;
                yield return Click(mouse,true);yield return new WaitForSeconds(1.5f);yield return Click(mouse,false);
                int slashes=w.Slashes;Check(label+" held knife repeats",slashes>=3);yield return new WaitForSeconds(.8f);Check(label+" released knife stops",w.Slashes==slashes);
                if(area==1)
                {
                    f.Carry(Chapter3Weapons.Weapon.Rifle);w.ResetEquipment();var b=f.school.Battle;var enemy=b.Enemies.First(e=>e&&!e.Defeated);bool aimed=false;
                    foreach(var direction in new[]{Vector3.back,Vector3.left,Vector3.right,Vector3.forward})
                    {
                        f.player.Warp(enemy.transform.position+direction*5,enemy.AimPoint);yield return Frames(2);Physics.SyncTransforms();
                        if(f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,20,out var hit)&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy){aimed=true;break;}
                    }
                    Check("school actual police ray acquired",aimed);yield return Click(mouse,true);yield return Click(mouse,false);
                    Check("school click damages actual police",enemy.Defeated||enemy.Health<100);
                    f.player.Warp(new Vector3(12,.06f,34),new Vector3(17,1,44));foreach(var e in b.Enemies)if(e&&!e.Defeated)e.Hit(100,f.player.transform.position);
                    yield return Wait(()=>b.Step==4,3,"four cleared");Check("retreat starts before retry",b.CallRetreat());yield return new WaitForSeconds(2);
                    var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();Check("all ten retreaters remain visible",a.Withdrawing&&a.VisibleAssailants==10);
                    b.Damage(100,Vector3.zero);yield return Frames(2);Check("retreat failure shown",b.CurrentStage==Chapter3SchoolBattle.Stage.Down);
                    yield return Press(keyboard,UnityEngine.InputSystem.Key.R);yield return Wait(()=>b.Fighting,5,"retry");
                    Check("retry clears old party and requires new signal",!b.RetreatCalled&&!a.Withdrawing&&!a.FollowingIntoForest);
                    f.player.Warp(new Vector3(12,.06f,34),new Vector3(17,1,44));Check("fresh whistle after retry",b.CallRetreat());
                }
            }
            yield return Press(keyboard,UnityEngine.InputSystem.Key.F8);yield return Wait(()=>f.school.Exploring,20,"F8 reset");Check("F8 clears buffered input and forest following",!f.combat.weapons.AttackQueued&&!f.combat.weapons.ReloadQueued&&!f.school.GetComponentInChildren<Chapter3SchoolAmbient>().FollowingIntoForest);
            report.notes.Add("Actual Input System mouse/key events; isolated targets held fire; player Warps set sightlines. A 400 ms frame hitch is deliberate. No manual keyboard, VR or standalone claim.");
        }
        finally
        {InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);oldMouse?.MakeCurrent();oldKeyboard?.MakeCurrent();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
