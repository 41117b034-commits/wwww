using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Temporary integration driver, copied into Assets/Editor only by the owner probe.
// Evidence boundary: UI callbacks and Input System E events; CharacterController
// Move/Follow for station travel and post-combat escort. Combat places the player
// with Warp, aims with FocusOn, pauses enemy fire with HoldFire, and damages every
// enemy through real Attack/Reload methods (never Enemy.Hit). Dialogue timeout is
// shortened on the live test instances to exercise fallback; all settings restore
// in finally. This does not establish native keyboard, human combat, VR or LLM SLA.
public static class Chapter3Batch7Prelude
{
    public static IEnumerator Run(Chapter3Flow f, bool knife, Action<string,bool> check,
        Action<string> shot, Action<string> progress)
    {
        Verify(check,"flow references present",f && f.station && f.combat && f.player && f.ui);
        var s=f.station;var c=f.combat;var p=f.player;
        float oldStationLines=s.lineSeconds,oldCombatLines=c.lineSeconds;
        float oldOpeningTimeout=15,oldReadyTimeout=15;
        bool savedTimeouts=false;
        string previousResult=PlayerPrefs.GetString("WusheEvent.Chapter3.Result","");
        var selected=knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle;
        var other=knife?Chapter3Weapons.Weapon.Rifle:Chapter3Weapons.Weapon.Knife;
        try
        {
            s.lineSeconds=.4f;c.lineSeconds=.4f;
            yield return Wait(()=>s.Opening && s.Readiness && s.Opening.Active,15,"opening conversation",progress);
            oldOpeningTimeout=s.Opening.replyTimeout;oldReadyTimeout=s.Readiness.replyTimeout;savedTimeouts=true;
            s.Opening.replyTimeout=.01f;s.Readiness.replyTimeout=.01f;
            Verify(check,"starts at station with no weapon",f.Region=="Station" && !s.HasWeapon && !f.CarriedWeapon.HasValue);
            Verify(check,"pickup and choice blocked before briefing",!s.TakeKnife() && !s.TakeRifle() && !s.ChooseWeapon(selected));
            Verify(check,"blank opening input rejected",!s.Opening.SubmitReply("  "));
            shot?.Invoke("prelude-opening");
            const string firstQuestion="我們接下來要去哪裡？";
            f.ui.input.text=firstQuestion;f.ui.send.onClick.Invoke();
            Verify(check,"opening records submitted question",f.ui.response.text.Contains("你："+firstQuestion));
            Verify(check,"opening duplicate rejected",!s.Opening.SubmitReply("再送一次"));
            yield return Wait(()=>!s.Opening.WaitingForReply,8,"first opening answer",progress);
            Verify(check,"first answer remains in opening",s.Opening.CompletedTurns==1 && s.CurrentStage==Chapter3StationController.Stage.Conversation);
            string firstExchange=f.ui.response.text;
            f.ui.input.text="我會跟上，也會照應比胡。";yield return null;
            Verify(check,"typing keeps earlier exchange",f.ui.response.text==firstExchange);
            f.ui.send.onClick.Invoke();
            yield return Wait(()=>!s.Opening.WaitingForReply,8,"second opening answer",progress);
            Verify(check,"opening completes exactly two exchanges",s.Opening.CompletedTurns==2 && !s.Opening.SubmitReply("第三次"));
            Verify(check,"both opening exchanges retained",f.ui.response.text.StartsWith(firstExchange) && f.ui.response.text.Contains("我會跟上，也會照應比胡。"));
            progress?.Invoke("prelude opening generated="+s.Opening.GeneratedReplies+" fallback="+s.Opening.FallbackReplies);
            shot?.Invoke("prelude-two-opening-exchanges");

            float began=Time.realtimeSinceStartup;
            while(s.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
            {
                Follow(p,s.bawan);
                progress?.Invoke("prelude escort and briefing "+s.CurrentStage);
                Deadline(began,80,"Mona briefing",p.transform.position);
                yield return null;
            }
            Verify(check,"weapon choice pauses movement",s.Choice.gameObject.activeSelf && !p.canMove && !p.canLook);
            shot?.Invoke("prelude-weapon-choice");
            if(knife)s.Choice.knife.onClick.Invoke();else s.Choice.rifle.onClick.Invoke();
            Verify(check,"single weapon choice committed",s.ChosenWeapon==selected && !s.ChooseWeapon(other));
            var target=knife?s.knifeFocus:s.rifleFocus;
            var arrow=f.ui.GetComponentInChildren<Chapter3PickupArrow>();
            Verify(check,"pickup arrow targets chosen weapon",arrow && arrow.Target==target);
            Verify(check,"unchosen pickup rejected",!(knife?s.TakeRifle():s.TakeKnife()));
            yield return Walk(p,new Vector3(target.position.x,0,-1.8f),"selected weapon table",progress);
            yield return Press(Key.E);
            Verify(check,"E pickup immediately opens readiness",s.Readiness.Active && s.CurrentStage==Chapter3StationController.Stage.Readiness && f.ui.chat.activeSelf);
            Verify(check,"exactly chosen weapon acquired",s.HasKnife==knife && s.HasRifle!=knife && f.CarriedWeapon==selected);
            Verify(check,"chosen model hidden and other retained",!(knife?s.knifePickup:s.riflePickup).gameObject.activeSelf && (knife?s.riflePickup:s.knifePickup).gameObject.activeSelf);
            Verify(check,"readiness cannot acquire second weapon",!(knife?s.TakeRifle():s.TakeKnife()));
            yield return null;
            Verify(check,"readiness input instruction has no pickup warning overlap",string.IsNullOrEmpty(f.ui.hint.text));
            shot?.Invoke("prelude-readiness");
            f.ui.input.text="我們要去哪裡？";f.ui.send.onClick.Invoke();
            yield return Wait(()=>!s.Readiness.WaitingForReply,8,"readiness clarification",progress);
            Verify(check,"question does not commit departure",s.CurrentStage==Chapter3StationController.Stage.Readiness && s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Unclear);
            f.ui.input.text="我準備好了";f.ui.send.onClick.Invoke();
            yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Departure && p.canMove,20,"ready departure",progress);
            Verify(check,"ready intent reaches departure",s.Readiness.LastDecision==Chapter3ReadinessConversation.Decision.Ready);
            progress?.Invoke("prelude readiness fallback="+s.Readiness.UsedFallback);

            began=Time.realtimeSinceStartup;
            while(f.Region=="Station")
            {
                Follow(p,s.bawan);
                progress?.Invoke("prelude station road waypoint "+s.Waypoint);
                Deadline(began,120,"station road",p.transform.position);
                yield return null;
            }
            Verify(check,"station naturally enters combat",f.Region=="Combat" && s.PartComplete);
            yield return Wait(()=>c.Fighting,15,"combat introduction",progress);
            var w=c.weapons;
            Verify(check,"combat preserves chosen weapon",w.Selected==selected && f.CarriedWeapon==selected && !w.Select(other));
            Verify(check,"combat has three living officers",c.Enemies.Count==3 && c.DefeatedCount==0 && c.Health==100);
            Verify(check,"combat initial health label",c.healthText.text=="生命值  100 / 100");
            if(knife)Verify(check,"knife permits first officer only",c.CanAttack(c.Enemies[0]) && !c.CanAttack(c.Enemies[1]) && !c.CanAttack(c.Enemies[2]));
            shot?.Invoke("prelude-combat-entry");
            // Controlled safety window isolates continuous story integration from
            // combat balance. Existing real-hit paths are still exercised below.
            foreach(var enemy in c.Enemies)enemy.HoldFire(180);
            for(int i=0;i<3;i++)
            {
                var enemy=c.Enemies[i];
                p.Warp(enemy.transform.position+Vector3.back*(knife?1.6f:3f),enemy.AimPoint);
                yield return null;
                if(knife)
                {
                    int before=w.Slashes;
                    began=Time.realtimeSinceStartup;
                    while(!enemy.Defeated)
                    {
                        if(!c.Fighting)throw new Exception("prelude player down during knife verification");
                        p.FocusOn(enemy.AimPoint);
                        if(w.Ready)w.Attack();
                        Deadline(began,8,"knife officer "+i,p.transform.position);
                        yield return null;
                    }
                    Verify(check,"officer "+i+" defeated by actual knife swings",w.Slashes>=before+2);
                }
                else
                {
                    yield return Wait(()=>w.Ready,4,"rifle ready before reload",progress);
                    if(!w.Loaded)
                    {
                        Verify(check,"rifle single round reload starts "+i,w.Reload());
                        yield return Wait(()=>!w.Reloading,5,"rifle reload completes",progress);
                    }
                    p.FocusOn(enemy.AimPoint);
                    Verify(check,"rifle attack accepted "+i,w.Attack());
                    yield return null;
                    Verify(check,"officer "+i+" defeated by actual rifle hit",enemy.Defeated);
                }
                shot?.Invoke("prelude-officer-"+i+"-defeated");
            }
            yield return Wait(()=>c.CurrentStage==Chapter3Combat.Stage.Cleared,5,"all officers cleared",progress);
            Verify(check,"all three defeated and combat HUD hidden",c.DefeatedCount==3 && !c.hud.activeSelf);
            Verify(check,"only carried weapon used",knife?w.Slashes>=6 && w.Shots==0:w.Shots==3 && w.Slashes==0 && w.Reloads==2);
            yield return Wait(()=>p.canMove,40,"post-combat escort ready",progress);
            shot?.Invoke("prelude-post-combat-escort");
            began=Time.realtimeSinceStartup;
            while(f.Region=="Combat")
            {
                Follow(p,c.bawan);
                progress?.Invoke("prelude following companions to school");
                Deadline(began,90,"school escort",p.transform.position);
                yield return null;
            }
            Verify(check,"clear encounter naturally enters school",f.Region=="School");
            yield return Wait(()=>f.school.Exploring,15,"school exploration",progress);
            Verify(check,"school preserves weapon",f.CarriedWeapon==selected);
            Verify(check,"prelude never commits chapter result",PlayerPrefs.GetString("WusheEvent.Chapter3.Result","")==previousResult);
            shot?.Invoke("prelude-school-arrival");
        }
        finally
        {
            if(s)s.lineSeconds=oldStationLines;
            if(c)c.lineSeconds=oldCombatLines;
            if(savedTimeouts && s)
            {
                if(s.Opening)s.Opening.replyTimeout=oldOpeningTimeout;
                if(s.Readiness)s.Readiness.replyTimeout=oldReadyTimeout;
            }
            if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        }
    }
    static void Follow(Chapter3Player p,Chapter2Actor actor)
    {
        Vector3 direction=Vector3.ProjectOnPlane(actor.transform.position-p.transform.position,Vector3.up);
        if(p.canMove && direction.magnitude>1.7f)
        {
            p.Move(direction.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));
            p.FocusOn(actor.transform.position+Vector3.up*1.3f);
        }
    }
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label,Action<string> progress)
    {
        float began=Time.realtimeSinceStartup;
        while(Flat(p.transform.position,target)>.25f)
        {
            Deadline(began,35,label,p.transform.position);
            Vector3 direction=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);
            p.Move(direction.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));
            progress?.Invoke("prelude "+label);yield return null;
        }
    }
    static IEnumerator Wait(Func<bool> done,float seconds,string label,Action<string> progress)
    {
        float began=Time.realtimeSinceStartup;
        while(!done())
        {
            Deadline(began,seconds,label,Vector3.zero);
            progress?.Invoke("prelude "+label);yield return null;
        }
    }
    static IEnumerator Press(Key key)
    {
        if(Keyboard.current==null)throw new Exception("prelude requires Input System keyboard");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));
        yield return new WaitForSecondsRealtime(.12f);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        yield return new WaitForSecondsRealtime(.12f);
    }
    static void Verify(Action<string,bool> check,string name,bool passed)
    {
        check?.Invoke("prelude "+name,passed);
        if(!passed)throw new Exception("prelude check failed: "+name);
    }
    static void Deadline(float began,float seconds,string label,Vector3 position)
    {
        if(Time.realtimeSinceStartup-began>seconds)throw new Exception("prelude timeout: "+label+" player="+position);
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
