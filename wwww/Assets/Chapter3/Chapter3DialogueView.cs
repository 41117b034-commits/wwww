using UnityEngine;

// A presentation camera only: the player's collision body never jumps when a
// nearby speaker takes a turn. Combat callouts retain the aiming camera.
[DefaultExecutionOrder(4900)]
public sealed class Chapter3DialogueView : MonoBehaviour
{
    public const float Distance=2.6f;
    Chapter3Flow flow;
    Chapter2Actor actor;
    Vector3 home,side;
    Quaternion homeLook,homeHeading;
    bool framed;
    public bool Framing=>framed;
    public float ActualDistance {get;private set;}
    bool ContinuousEnding
    {
        get
        {
            if(!flow||flow.Region!="School"||!flow.school)return false;
            var ending=flow.school.GetComponent<Chapter3Ending>();return ending&&ending.Active;
        }
    }
    public void Configure(Chapter3Flow owner){flow=owner;}
    public void Speak(string speaker,string text)
    {
        if(!flow||string.IsNullOrEmpty(text)||string.IsNullOrEmpty(speaker)){Release();return;}
        // Weapon warnings must never pull the crosshair away from a target.
        if(flow.player.IsVR||flow.combat.Fighting||ContinuousEnding){Release();return;}
        Chapter2Actor found=FindSpeaker(flow,speaker);
        if(!found){Release();return;}
        if(!framed){home=flow.player.view.transform.localPosition;homeLook=flow.player.view.transform.localRotation;homeHeading=flow.player.transform.rotation;}
        actor=found;framed=true;
        side=Vector3.ProjectOnPlane(flow.player.transform.position-actor.transform.position,Vector3.up).normalized;
        if(side.sqrMagnitude<.01f)side=-actor.facing.normalized;
        if(side.sqrMagnitude<.01f)side=Vector3.back;
        flow.player.DialogueFraming=true;
    }
    public static Chapter2Actor FindSpeaker(Chapter3Flow flow,string name)
    {
        if(!flow)return null;
        // Preview scenery can keep duplicate casts active in other regions.
        // Resolve the authoritative story cast, never the first matching name.
        Chapter2Actor[] cast;
        if(flow.Region=="Station")cast=new[]{flow.station.bawan,flow.station.pihu,flow.station.mona};
        else if(flow.Region=="Combat")cast=new[]{flow.combat.bawan,flow.combat.pihu};
        else
        {
            var ending=flow.school.GetComponent<Chapter3Ending>();
            cast=new[]{flow.school.bawan,flow.school.pihu,flow.school.tado,ending?ending.Mona:null};
        }
        foreach(var candidate in cast)if(candidate&&candidate.gameObject.activeInHierarchy&&candidate.DisplayName==name)return candidate;
        return null;
    }
    void LateUpdate()
    {
        if(!framed)return;
        if(!actor||!actor.gameObject.activeInHierarchy||flow.player.IsVR||flow.combat.Fighting||ContinuousEnding||
            (!flow.ui.subtitlePanel.activeInHierarchy&&!flow.ui.chat.activeInHierarchy)) {Release();return;}
        var rig=actor.Rig;
        Vector3 focus=rig&&rig.Head?rig.Head.position-Vector3.up*.15f:actor.transform.position+Vector3.up*1.35f;
        Vector3 selected=focus+side*Distance;bool clear=false;
        // Keep the same radius when a wall/tree obstructs the initial camera.
        foreach(float angle in new[]{0f,30f,-30f,60f,-60f,90f,-90f,135f,-135f,180f})
        {
            Vector3 candidate=focus+Quaternion.Euler(0,angle,0)*side*Distance;
            if(!Clear(focus,candidate))continue;
            selected=candidate;clear=true;break;
        }
        if(!clear){Release();return;}
        flow.player.view.transform.SetPositionAndRotation(selected,Quaternion.LookRotation(focus-selected));
        ActualDistance=Vector3.Distance(selected,focus);
    }
    bool Clear(Vector3 focus,Vector3 point)
    {
        foreach(var hit in Physics.SphereCastAll(focus,.15f,(point-focus).normalized,Distance,~0,QueryTriggerInteraction.Ignore))
        {
            var c=hit.collider;
            if(c.transform.IsChildOf(flow.player.transform)||c.GetComponentInParent<Chapter2Actor>()||c.GetComponentInParent<Chapter3SchoolCrowdActor>())continue;
            if(c.bounds.max.y<.28f)continue;
            return false;
        }
        return true;
    }
    public void Release()
    {
        if(framed&&flow&&flow.player)
        {
            var player=flow.player;player.view.transform.localPosition=home;player.transform.rotation=homeHeading;player.view.transform.localRotation=homeLook;
            player.FocusOn(player.view.transform.position+player.view.transform.forward*10);player.DialogueFraming=false;
        }
        framed=false;actor=null;ActualDistance=0;
    }
    void OnDisable(){Release();}
}
