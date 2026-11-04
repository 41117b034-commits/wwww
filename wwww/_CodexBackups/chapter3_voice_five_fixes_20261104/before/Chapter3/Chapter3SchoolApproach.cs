using System.Collections.Generic;
using UnityEngine;

// This short connecting trail compresses distance for gameplay. Its bends and
// local slope are level design, not a reconstruction of the historical route.
[DefaultExecutionOrder(4100)]
public sealed class Chapter3SchoolApproach : MonoBehaviour
{
    public bool Active {get;private set;}
    public bool Completed {get;private set;}
    public bool Waiting {get;private set;}
    public int CurrentWaypoint {get;private set;}
    public bool AudioStarted=>audioCue&&audioCue.Started;
    public bool SchoolRevealed {get;private set;}
    public Chapter2Actor LeadActor=>flow?flow.combat.bawan:null;
    public Chapter2Actor RearActor=>flow?flow.combat.pihu:null;
    public Vector3[] WorldWaypoints=>environment?environment.Waypoints:System.Array.Empty<Vector3>();
    public Chapter3ApproachAudio AudioCue=>audioCue;
    public Chapter3RouteGuide FollowGuide {get;private set;}
    Chapter3Flow flow;
    Chapter3ApproachEnvironment environment;
    Chapter3ApproachAudio audioCue;
    readonly Chapter3PathMotor rallyLeader=new Chapter3PathMotor(),rallyRear=new Chapter3PathMotor();
    readonly List<Vector3> trail=new List<Vector3>();
    readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
    readonly Dictionary<Collider,bool> hiddenBodies=new Dictionary<Collider,bool>();
    AudioSource schoolForest;
    float forestVolume, began, lineUntil;
    bool prepared,schoolSunEnabled,heardLine,revealLine,gathering,openingReply;
    int rearIndex,handoffFrame=-1;
    Vector3 leadFacing,rearFacing,rally;

    public void Initialize(Chapter3Flow owner)
    {
        flow=owner;
        // Relocate this region before any encounter starts. Every authored
        // school route stays in its original coordinates; the player never
        // moves because of a late origin shift.
        flow.combatRegion.transform.position=new Vector3(0,0,Chapter3ApproachEnvironment.CombatOriginZ);
        environment=gameObject.AddComponent<Chapter3ApproachEnvironment>();
        environment.Build(flow);
        audioCue=gameObject.AddComponent<Chapter3ApproachAudio>();
        audioCue.Configure(flow.school);
        FollowGuide=gameObject.AddComponent<Chapter3RouteGuide>();
    }
    public void Prepare()
    {
        Active=Completed=Waiting=SchoolRevealed=false;heardLine=revealLine=openingReply=false;
        CurrentWaypoint=0;rearIndex=0;trail.Clear();rallyLeader.Reset();rallyRear.Reset();
        environment.SetVisible(true);
        flow.schoolRegion.SetActive(true);
        flow.school.PrepareApproach();
        foreach(var actor in new[]{flow.school.bawan,flow.school.pihu})
        {
            foreach(var r in actor.GetComponentsInChildren<Renderer>(true)){hidden[r]=r.enabled;r.enabled=false;}
            foreach(var c in actor.GetComponentsInChildren<Collider>(true)){hiddenBodies[c]=c.enabled;c.enabled=false;}
        }
        if(flow.school.sun){schoolSunEnabled=flow.school.sun.enabled;flow.school.sun.enabled=false;}
        schoolForest=flow.schoolRegion.GetComponent<AudioSource>();
        if(schoolForest){forestVolume=schoolForest.volume;schoolForest.volume=0;}
        prepared=true;
    }
    public void BeginWalk()
    {
        if(!prepared||Active)return;
        Active=true;Completed=false;began=Time.time;
        gathering=true;
        rally=flow.combat.WorldPoint(new Vector3(-1,0,Mathf.Clamp(
            flow.player.transform.position.z-flow.combat.transform.position.z+2,-3,25)));
        flow.player.canMove=flow.player.canLook=true;
        flow.ui.fade.color=Color.clear;
        // Preserve the player's current camera heading and mouse mode.
        trail.Add(RearActor.transform.position);trail.Add(LeadActor.transform.position);
        leadFacing=LeadActor.facing;rearFacing=RearActor.facing;
        if(LeadActor.Rig)LeadActor.Rig.smoothLocomotion=true;
        if(RearActor.Rig)RearActor.Rig.smoothLocomotion=true;
        Say(LeadActor,"前面的路通了，跟我沿林路走。比胡，照看後面的人。",4.5f);
        flow.ui.objective.text="跟隨巴萬穿過林路，前往學校";
        audioCue.Begin(flow.player.view.transform);
        FollowGuide.Show(flow.player.transform.position,new[]{rally},flow.player.transform,LeadActor.transform);
    }
    void Update()
    {
        if(!Active)return;
        var player=flow.player;
        if(lineUntil>0&&Time.time>=lineUntil)
        {
            lineUntil=0;flow.ui.Say("",null);LeadActor.speaking=RearActor.speaking=false;
            if(!heardLine&&!openingReply)
            {openingReply=true;Say(RearActor,"我跟在後面。走吧，到校門旁和達多會合。",3.5f);}
        }
        float gap=Flat(player.transform.position,LeadActor.transform.position);
        float rearGap=Flat(RearActor.transform.position,LeadActor.transform.position);
        Waiting=!gathering&&((gap>(Waiting?4.8f:6.2f)&&player.transform.position.z<LeadActor.transform.position.z-1)||rearGap>8);
        var points=WorldWaypoints;
        Vector3 target=gathering?rally:points[CurrentWaypoint];
        if(!Waiting)
        {
            MoveActor(LeadActor,target,1.9f,rallyLeader);
            if(Flat(LeadActor.transform.position,target)<.25f)
            {
                if(gathering)
                {
                    gathering=false;
                    FollowGuide.Show(flow.player.transform.position,points,flow.player.transform,LeadActor.transform,
                        p=>p.z<Chapter3ApproachEnvironment.TrailStartZ?0:environment.GroundHeight(p));
                }
                else if(CurrentWaypoint<points.Length-1)CurrentWaypoint++;
            }
        }
        if(trail.Count==0||Flat(trail[trail.Count-1],LeadActor.transform.position)>.7f)
            trail.Add(LeadActor.transform.position);
        // The rear companion follows the same walked bend, never a chord
        // through the hillside. He keeps closing the gap when the leader waits.
        int end=trail.Count-1;
        while(end>0&&Flat(trail[end],LeadActor.transform.position)<2.2f)end--;
        rearIndex=Mathf.Min(rearIndex,end);
        if(Flat(RearActor.transform.position,trail[rearIndex])<.35f&&rearIndex<end)rearIndex++;
        if(rearGap>2.4f)
        {
            Vector3 rearTarget=trail[rearIndex];
            if(end==0&&Flat(RearActor.transform.position,rearTarget)<.4f)
                rearTarget=Vector3.MoveTowards(LeadActor.transform.position,RearActor.transform.position,2.3f);
            MoveActor(RearActor,rearTarget,2.4f,rallyRear);
        }
        else if(!RearActor.speaking)RearActor.Face(player.transform.position);

        float hearing=Mathf.InverseLerp(-44,-28,player.transform.position.z);
        // Establish the sports cue while the field is still behind the bend;
        // starting a barely audible source is not enough to lead the player.
        audioCue.SetProgress(Mathf.InverseLerp(-44,-35,player.transform.position.z));
        if(hearing>.12f&&!heardLine)
        {
            heardLine=true;
            Say(RearActor,"聽，前面是運動會的哨聲。沿這條路繼續走。",4.3f);
        }
        SchoolRevealed=player.transform.position.z>-28;
        if(SchoolRevealed&&!revealLine&&lineUntil<=0)
        {
            revealLine=true;Say(LeadActor,"看見操場了。從前面的校門過去。",4.2f);
        }
        flow.ui.objective.text=SchoolRevealed?"走到校門旁，與達多會合":heardLine?"沿林路緩坡前進，循聲接近學校":"跟隨巴萬穿過林路，前往學校";
        flow.ui.hint.text=Waiting?"同伴停下等你，走近後繼續 · WASD 移動 · 右鍵環視":"跟上巴萬 · WASD 移動 · 右鍵環視";
        if(CurrentWaypoint==points.Length-1&&player.transform.position.z>=points[points.Length-1].z-1&&environment.Walkable(player.transform.position)&&
            Flat(LeadActor.transform.position,points[points.Length-1])<.5f&&Flat(RearActor.transform.position,points[points.Length-1])<4)
            flow.CompleteSchoolApproach();
    }
    void LateUpdate()
    {
        if(Completed&&Time.frameCount==handoffFrame)
        {
            // Actor.Update can interpret its new handoff position as a walking
            // delta. Restore the carried heading after that update and before
            // the speech mask / camera render, so no one-frame turn is visible.
            flow.school.bawan.Face(flow.school.bawan.transform.position+LeadActor.facing);
            flow.school.pihu.Face(flow.school.pihu.transform.position+RearActor.facing);
        }
        if(!Active)return;
        Face(LeadActor,ref leadFacing,Waiting);
        Face(RearActor,ref rearFacing,false);
    }
    void Face(Chapter2Actor actor,ref Vector3 facing,bool wait)
    {
        Vector3 desired=wait?flow.player.transform.position-actor.transform.position:actor.facing;
        desired.y=0;if(desired.sqrMagnitude<.001f)return;
        facing=Vector3.RotateTowards(facing.normalized,desired.normalized,140*Mathf.Deg2Rad*Time.deltaTime,0);
        actor.Face(actor.transform.position+facing);
    }
    void MoveActor(Chapter2Actor actor,Vector3 target,float speed,Chapter3PathMotor flatMotor)
    {
        Vector3 current=actor.transform.position;
        // Existing cover-aware navigation remains in the flat encounter lane.
        if(current.z<Chapter3ApproachEnvironment.TrailStartZ-.3f&&target.z<=Chapter3ApproachEnvironment.TrailStartZ)
        {
            flatMotor.Move(actor,target,speed);
            // Flat navigation ignores characters when searching. Reject its
            // tentative step if the player occupies it; do not walk through
            // the camera while gathering beside the cleared encounter.
            if(Flat(actor.transform.position,flow.player.transform.position)<.66f)
                actor.transform.position=current;
            return;
        }
        Vector3 delta=Vector3.ProjectOnPlane(target-current,Vector3.up);
        if(delta.sqrMagnitude<.008f)return;
        Vector3 next=Vector3.MoveTowards(current,current+delta,speed*Mathf.Min(Time.deltaTime,.1f));
        next.y=environment.GroundHeight(next);
        Vector3 motion=next-current;
        foreach(var hit in Physics.CapsuleCastAll(current+Vector3.up*.48f,current+Vector3.up*1.3f,.32f,
            motion.normalized,motion.magnitude,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.GetComponentInParent<Chapter2Actor>()||hit.collider is CharacterController)continue;
            if(hit.normal.y>.65f)continue; // Walkable slope, not a wall.
            if(hit.distance>.001f)return;
        }
        if(Flat(next,flow.player.transform.position)<.66f)return;
        actor.transform.position=next;actor.GroundHeight=next.y;actor.Face(next+delta.normalized);
    }
    void Say(Chapter2Actor actor,string line,float duration)
    {
        LeadActor.speaking=RearActor.speaking=false;actor.speaking=true;
        flow.ui.Say(actor.DisplayName,line);lineUntil=Time.time+duration;
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,duration,null);
    }
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        if(current.z<=Chapter3ApproachEnvironment.TrailStartZ&&desired.z<=Chapter3ApproachEnvironment.TrailStartZ)
        {desired.x=Mathf.Clamp(desired.x,-7,7);desired.z=Mathf.Max(Chapter3ApproachEnvironment.CombatOriginZ-12,desired.z);return desired;}
        return environment.Constrain(current,desired);
    }
    public void Finish()
    {
        if(FollowGuide)FollowGuide.Hide();
        Active=false;Completed=true;Waiting=false;
        handoffFrame=Time.frameCount;
        LeadActor.speaking=RearActor.speaking=false;
        RestoreSchool();
        audioCue.Complete();
        // Keep the connector visible behind the arriving player, including
        // while looking back. Only an explicit region reset removes it.
    }
    void RestoreSchool()
    {
        foreach(var pair in hidden)if(pair.Key)pair.Key.enabled=pair.Value;
        foreach(var pair in hiddenBodies)if(pair.Key)pair.Key.enabled=pair.Value;
        hidden.Clear();hiddenBodies.Clear();
        if(prepared&&flow.school.sun)flow.school.sun.enabled=schoolSunEnabled;
        if(prepared&&schoolForest)schoolForest.volume=forestVolume;
        prepared=false;
    }
    public void Suspend()
    {
        if(FollowGuide)FollowGuide.Hide();
        Active=Completed=Waiting=SchoolRevealed=false;
        if(LeadActor)LeadActor.speaking=false;if(RearActor)RearActor.speaking=false;
        RestoreSchool();
        if(audioCue)audioCue.Suspend();
        if(environment)environment.SetVisible(false);
        trail.Clear();rearIndex=0;lineUntil=0;rallyLeader.Reset();rallyRear.Reset();
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
