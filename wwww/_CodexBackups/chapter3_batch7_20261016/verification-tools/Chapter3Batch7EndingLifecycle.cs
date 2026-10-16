using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

// Targeted, injected lifecycle fixture; not an authored gameplay run or hardware
// input test. Private references/state are arranged without Ending.Begin, model
// replies, injury/death animation or result writes. BorrowMona, Suspend, OnDisable
// and the Flow region switches themselves are the real runtime implementations.
// A delayed coroutine owned by Ending is only a cancellation canary, not proof
// about all real dialogue callbacks. Card Replay/Menu are covered by full runs.
// Station entry intentionally runs Begin after Suspend, resetting Mona to its
// authored monaHome and facing the station gathering point. Only School entry
// and direct Suspend retain the exact pre-borrow local pose.
public static class Chapter3Batch7EndingLifecycle
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    sealed class MonaSnapshot
    {
        public Chapter2Actor actor;
        public Transform parent;
        public Vector3 position,scale,facing;
        public Quaternion rotation;
        public bool active;
        public MonaSnapshot(Chapter2Actor source)
        {
            actor=source;parent=source.transform.parent;position=source.transform.localPosition;
            rotation=source.transform.localRotation;scale=source.transform.localScale;
            facing=source.facing;active=source.gameObject.activeSelf;
        }
    }
    sealed class ViewSnapshot
    {
        public Vector3 position,localPosition;
        public Quaternion rotation,localRotation;
        public ViewSnapshot(Chapter3Player p)
        {
            position=p.transform.position;rotation=p.transform.rotation;
            localPosition=p.view.transform.localPosition;localRotation=p.view.transform.localRotation;
        }
    }

    public static IEnumerator Run(Chapter3Flow f,Action<string,bool> check,Action<string> shot)
    {
        Verify(check,"flow references",f && f.school && f.station && f.player && f.ui);
        Verify(check,"fresh non-refused run",!f.station.RefusalCommitted);
        string savedResult=PlayerPrefs.GetString("WusheEvent.Chapter3.Result","");
        f.EnterSchool();yield return Wait(()=>f.school.Exploring,6,"initial school arrival");
        var school=f.school;var p=f.player;
        // Wire the same component ownership used by SchoolBattle -> Rescue ->
        // Ending, while avoiding Rescue.Begin and Ending.Begin story side effects.
        var rescue=school.Battle.Rescue;
        if(!rescue)rescue=school.GetComponent<Chapter3Rescue>();
        if(!rescue)rescue=school.gameObject.AddComponent<Chapter3Rescue>();
        Property(school.Battle,"Rescue",rescue);
        Field(rescue,"school",school);Field(rescue,"battle",school.Battle);
        Field(rescue,"player",p);Field(rescue,"combat",f.combat);Field(rescue,"ui",f.ui);
        var ending=rescue.Ending;
        if(!ending)ending=school.GetComponent<Chapter3Ending>();
        if(!ending)ending=school.gameObject.AddComponent<Chapter3Ending>();
        Property(rescue,"Ending",ending);
        Field(ending,"school",school);Field(ending,"rescue",rescue);
        Field(ending,"player",p);Field(ending,"ui",f.ui);
        bool oldPersist=ending.PersistResult;ending.PersistResult=false;
        Vector3 desktopViewPosition=p.view.transform.localPosition;
        try
        {
            for(int route=0;route<2;route++)
            {
                bool station=route==1;
                string label=station?"borrowed Mona to station":"borrowed Mona to school";
                var original=new MonaSnapshot(f.station.mona);
                Property(ending,"CurrentStage",Chapter3Ending.Stage.MonaClosing);
                Invoke(ending,"BorrowMona");
                Verify(check,label+" fixture borrowed",ending.Mona==original.actor && original.actor.transform.parent==ending.transform && Read<bool>(ending,"monaBorrowed"));
                bool late=false;ending.StartCoroutine(Late(()=>late=true));
                if(station)f.EnterStation();else f.EnterSchool();
                if(station)CheckStationMona(check,label,original,f.station);
                else CheckMona(check,label,original);
                CheckClean(check,label,ending);
                if(station)
                {
                    CheckStation(check,f,label);
                    // EnterStation executes its normal preparation hook; no
                    // answer/service availability is required. Leave immediately.
                    f.EnterSchool();
                }
                yield return Wait(()=>school.Exploring,6,label+" fresh school");
                CheckSchool(check,f,label,desktopViewPosition);
                Verify(check,label+" owned coroutine cancelled",!late);
                shot?.Invoke("ending-lifecycle-mona-"+(station?"station":"school"));
            }

            for(int route=0;route<2;route++)
            {
                bool station=route==1;
                string label=station?"death cutaway to station":"death cutaway to school";
                ArmCutaway(ending,p);
                bool late=false;ending.StartCoroutine(Late(()=>late=true));
                if(station)f.EnterStation();else f.EnterSchool();
                Verify(check,label+" cutaway cleared",!Read<bool>(ending,"cutaway"));
                Verify(check,label+" camera height restored",Near(p.view.transform.localPosition,desktopViewPosition));
                CheckClean(check,label,ending);
                if(station){CheckStation(check,f,label);f.EnterSchool();}
                yield return Wait(()=>school.Exploring,6,label+" fresh school");
                CheckSchool(check,f,label,desktopViewPosition);
                Verify(check,label+" owned coroutine cancelled",!late);
            }

            var beforeDisable=new MonaSnapshot(f.station.mona);
            Property(ending,"CurrentStage",Chapter3Ending.Stage.MonaClosing);
            Invoke(ending,"BorrowMona");
            var reception=ArmCutaway(ending,p);
            bool lateDisabled=false;ending.StartCoroutine(Late(()=>lateDisabled=true));
            f.schoolRegion.SetActive(false);
            yield return null;
            Verify(check,"direct disable retains borrowed snapshot",Read<bool>(ending,"monaBorrowed") && ending.Mona==beforeDisable.actor && beforeDisable.actor.transform.parent==ending.transform);
            Verify(check,"direct disable retains cutaway snapshot",Read<bool>(ending,"cutaway"));
            f.schoolRegion.SetActive(true);
            // Run synchronously after activation, before another gameplay frame.
            ending.Suspend();
            CheckMona(check,"direct reenable explicit Suspend",beforeDisable);
            Verify(check,"explicit Suspend restores reception position",Near(p.transform.position,reception.position));
            Verify(check,"explicit Suspend restores reception rotation",Quaternion.Angle(p.transform.rotation,reception.rotation)<.01f);
            Verify(check,"explicit Suspend restores local camera",Near(p.view.transform.localPosition,reception.localPosition) && Quaternion.Angle(p.view.transform.localRotation,reception.localRotation)<.01f);
            CheckClean(check,"direct reenable explicit Suspend",ending);
            yield return new WaitForSecondsRealtime(.35f);
            Verify(check,"direct disable owned coroutine never resumes",!lateDisabled);
            Verify(check,"direct disable ending stays inactive",ending.CurrentStage==Chapter3Ending.Stage.Inactive && !f.ui.ending.activeSelf);
            shot?.Invoke("ending-lifecycle-direct-disable-restored");
            f.EnterSchool();yield return Wait(()=>school.Exploring,6,"final clean school");
            CheckSchool(check,f,"final clean school",desktopViewPosition);
            Verify(check,"lifecycle fixture does not alter saved result",PlayerPrefs.GetString("WusheEvent.Chapter3.Result","")==savedResult);
        }
        finally
        {
            if(f && f.schoolRegion && !f.schoolRegion.activeSelf)f.schoolRegion.SetActive(true);
            if(ending){ending.Suspend();ending.PersistResult=oldPersist;}
        }
    }
    static ViewSnapshot ArmCutaway(Chapter3Ending ending,Chapter3Player p)
    {
        p.Warp(new Vector3(13,.06f,106),new Vector3(11,1.2f,103));
        var reception=new ViewSnapshot(p);
        Field(ending,"receptionPlayer",reception.position);Field(ending,"receptionRotation",reception.rotation);
        Field(ending,"viewLocalPosition",reception.localPosition);Field(ending,"viewLocalRotation",reception.localRotation);
        Field(ending,"cutaway",true);Property(ending,"CurrentStage",Chapter3Ending.Stage.Outcome);
        p.Warp(new Vector3(1.3f,.06f,90.1f),new Vector3(-3,.55f,92));
        // Deliberately distinct camera-local position makes restoration observable;
        // this injected offset is a fixture, not the authored death camera pose.
        p.view.transform.localPosition=reception.localPosition+new Vector3(.12f,-.2f,.08f);
        p.canMove=p.canLook=false;return reception;
    }
    static void CheckMona(Action<string,bool> check,string label,MonaSnapshot original)
    {
        var t=original.actor.transform;
        Verify(check,label+" original parent",t.parent==original.parent);
        Verify(check,label+" original local position",Near(t.localPosition,original.position));
        Verify(check,label+" original local rotation",Quaternion.Angle(t.localRotation,original.rotation)<.01f);
        Verify(check,label+" original local scale",Near(t.localScale,original.scale));
        Verify(check,label+" original active state",original.actor.gameObject.activeSelf==original.active);
        Verify(check,label+" original facing",Near(original.actor.facing,original.facing));
    }
    static void CheckStationMona(Action<string,bool> check,string label,MonaSnapshot original,Chapter3StationController station)
    {
        var actor=original.actor;var t=actor.transform;
        Vector3 home=Read<Vector3>(station,"monaHome");
        Vector3 expectedFacing=new Vector3(0,0,-3)-home;
        Verify(check,label+" original parent",t.parent==original.parent);
        Verify(check,label+" original local scale",Near(t.localScale,original.scale));
        Verify(check,label+" original active state",actor.gameObject.activeSelf==original.active);
        // Check immediately after EnterStation, before actor Update applies its
        // ground/locomotion pose. Begin deliberately replaces the restored pose.
        Verify(check,label+" station authored home",Near(t.position,home));
        Verify(check,label+" station gathering heading",Near(actor.facing,expectedFacing));
        Verify(check,label+" visible rig matches fresh heading",actor.Rig && Vector3.Angle(Vector3.ProjectOnPlane(actor.Rig.Forward,Vector3.up),Vector3.ProjectOnPlane(expectedFacing,Vector3.up))<.1f);
    }
    static void CheckClean(Action<string,bool> check,string label,Chapter3Ending ending)
    {
        Verify(check,label+" ending inactive and no retained outcome",ending.CurrentStage==Chapter3Ending.Stage.Inactive && ending.Result==null && !ending.Saved);
        Verify(check,label+" borrowed ownership released",!Read<bool>(ending,"monaBorrowed") && !ending.Mona);
    }
    static void CheckStation(Action<string,bool> check,Chapter3Flow f,string label)
    {
        Verify(check,label+" fresh arrival controls",f.Region=="Station" && f.player.station==f.station && !f.player.school && !f.player.canMove && !f.player.canLook && f.station.CurrentStage==Chapter3StationController.Stage.Arrival);
        Verify(check,label+" fresh station spawn",Flat(f.player.transform.position,new Vector3(0,0,-18))<.02f && Mathf.Abs(f.player.speed-2.8f)<.001f && !f.ui.ending.activeSelf);
    }
    static void CheckSchool(Action<string,bool> check,Chapter3Flow f,string label,Vector3 viewPosition)
    {
        Verify(check,label+" fresh exploration controls",f.Region=="School" && f.school.Exploring && f.player.school==f.school && f.player.canMove && f.player.canLook && f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
        Verify(check,label+" fresh school camera and spawn",Flat(f.player.transform.position,f.school.spawn)<.02f && Near(f.player.view.transform.localPosition,viewPosition) && Mathf.Abs(f.player.view.fieldOfView-60)<.01f && !f.ui.ending.activeSelf);
    }
    static IEnumerator Late(Action action){yield return new WaitForSecondsRealtime(.2f);action();}
    static IEnumerator Wait(Func<bool> predicate,float seconds,string label)
    {
        float start=Time.realtimeSinceStartup;
        while(!predicate()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("ending lifecycle timeout: "+label);yield return null;}
    }
    static void Verify(Action<string,bool> check,string label,bool ok)
    {check?.Invoke("ending lifecycle "+label,ok);if(!ok)throw new Exception("ending lifecycle failed: "+label);}
    static void Field(object target,string name,object value)
    {var field=target.GetType().GetField(name,Instance);if(field==null)throw new MissingFieldException(target.GetType().Name,name);field.SetValue(target,value);}
    static T Read<T>(object target,string name)=> (T)target.GetType().GetField(name,Instance).GetValue(target);
    static void Property(object target,string name,object value)
    {var property=target.GetType().GetProperty(name,Instance);if(property==null)throw new MissingMemberException(target.GetType().Name,name);property.GetSetMethod(true).Invoke(target,new[]{value});}
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,Instance).Invoke(target,null);
    static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.001f;
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
