using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Chapter3TenProbe
{
    static Chapter3SchoolAmbient calmAmbientBaseline;
    static readonly Dictionary<Transform,Vector3> calmSpectatorHomes=new Dictionary<Transform,Vector3>();

    // Observe the real Outbreak after Choose. No input, stage, position,
    // clock, actor or damage writes; screenshots wait for the completed frame.
    static IEnumerator ObserveAmbientSurprise(Chapter3Flow f)
    {
        var ambient=f.school.GetComponentInChildren<Chapter3SchoolAmbient>(true);
        Check("surprise ambient exists",ambient);
        yield return Wait(()=>ambient.Panicking,5,"normal outbreak panic cue");
        Check("observation starts before the crowd has fled",ambient.FleeingSpectators==0);
        Check("all three actors released by the one choice signal",ambient.VisibleAssailants==3&&ambient.AmbushSignalFrame>=0&&Time.frameCount-ambient.AmbushSignalFrame<10);
        int count=ambient.spectators.Length;
        var firstReaction=new int[count];var firstFlight=new int[count];
        var firstReactionTime=new float[count];var firstFlightTime=new float[count];
        var reactionStart=new Vector3[count];
        for(int i=0;i<count;i++){firstReaction[i]=firstFlight[i]=-1;}
        var visibleCounts=new HashSet<int>();
        var flightFrames=new HashSet<int>();
        var visibleRush=new HashSet<int>();var rushPixels=new Dictionary<int,float>();
        var incursion=ambient.GetComponentsInChildren<Chapter3SchoolCrowdActor>(true).Where(a=>a.name.StartsWith("校內埋伏的族人")).ToArray();
        bool mixedPhases=false,mixedCalmAndReaction=false,mixedReactionAndFlight=false;
        bool chronological=true,calmStayedAtHome=true,reactionPoseSeen=false,flightMotionSeen=false;
        bool reactionShot=false;int samples=0,activeSpectators=0;
        float began=Time.realtimeSinceStartup;
        var previousPositions=new Vector3[count];
        for(int i=0;i<count;i++)if(ambient.spectators[i])
        {previousPositions[i]=ambient.spectators[i].position;if(ambient.spectators[i].gameObject.activeInHierarchy)activeSpectators++;}
        Check("authored spectators available to observe",activeSpectators>1);
        report.observations.Add("ObserveAmbientSurprise begins at actual assault elapsed="+ambient.AssaultElapsed+
            "; observing the normal Outbreak only, without actor/clock/mission fixtures.");

        do
        {
            yield return new WaitForEndOfFrame();
            if(Time.realtimeSinceStartup-began>30)throw new Exception("Ambient observation did not advance to five seconds.");
            Check("panic remains active during observation",ambient.Panicking);
            samples++;visibleCounts.Add(ambient.VisibleAssailants);
            foreach(var actor in incursion)if(actor.gameObject.activeInHierarchy)
            {
                var rushCamera=f.player.view;var middle=actor.transform.position+Vector3.up*.95f;
                var v=rushCamera.WorldToViewportPoint(middle);var top=rushCamera.WorldToViewportPoint(actor.transform.position+Vector3.up*1.65f);var bottom=rushCamera.WorldToViewportPoint(actor.transform.position);
                bool blocked=Physics.RaycastAll(rushCamera.transform.position,(middle-rushCamera.transform.position).normalized,Vector3.Distance(middle,rushCamera.transform.position),~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.transform.IsChildOf(actor.transform)&&!h.collider.transform.IsChildOf(f.player.transform)&&h.collider.bounds.max.y>.3f);
                if(v.z>0&&v.x>.05f&&v.x<.95f&&v.y>.1f&&v.y<.9f&&!blocked&&top.y-bottom.y>.03f){visibleRush.Add(actor.GetInstanceID());rushPixels[actor.GetInstanceID()]=(top.y-bottom.y)*900;}
            }
            var phases=new HashSet<Chapter3SchoolAmbient.CrowdPhase>();
            for(int i=0;i<count;i++)
            {
                var spectator=ambient.spectators[i];
                if(!spectator||!spectator.gameObject.activeInHierarchy)continue;
                var phase=ambient.SpectatorPhase(i);phases.Add(phase);
                var actor=spectator.GetComponent<Chapter3SchoolCrowdActor>();
                if(phase==Chapter3SchoolAmbient.CrowdPhase.Calm&&
                    Vector3.ProjectOnPlane(spectator.position-previousPositions[i],Vector3.up).magnitude>.02f)
                    calmStayedAtHome=false;
                if(phase==Chapter3SchoolAmbient.CrowdPhase.Reacting)
                {
                    if(firstReaction[i]<0)
                    {firstReaction[i]=Time.frameCount;firstReactionTime[i]=ambient.AssaultElapsed;reactionStart[i]=spectator.position;}
                    if(actor&&actor.StartleWeight>.05f)reactionPoseSeen=true;
                }
                if(phase==Chapter3SchoolAmbient.CrowdPhase.Fleeing||
                    phase==Chapter3SchoolAmbient.CrowdPhase.Falling||phase==Chapter3SchoolAmbient.CrowdPhase.Fallen)
                {
                    if(firstFlight[i]<0)
                    {
                        firstFlight[i]=Time.frameCount;firstFlightTime[i]=ambient.AssaultElapsed;
                        flightFrames.Add(Time.frameCount);
                        if(firstReaction[i]<0||firstReaction[i]>=firstFlight[i])chronological=false;
                    }
                    if(firstReaction[i]>=0&&Vector3.ProjectOnPlane(spectator.position-reactionStart[i],Vector3.up).magnitude>.65f)
                        flightMotionSeen=true;
                }
                previousPositions[i]=spectator.position;
            }
            mixedPhases|=phases.Count>1;
            mixedCalmAndReaction|=phases.Contains(Chapter3SchoolAmbient.CrowdPhase.Calm)&&phases.Contains(Chapter3SchoolAmbient.CrowdPhase.Reacting);
            mixedReactionAndFlight|=phases.Contains(Chapter3SchoolAmbient.CrowdPhase.Reacting)&&phases.Contains(Chapter3SchoolAmbient.CrowdPhase.Fleeing);
            if(!reactionShot&&mixedCalmAndReaction&&reactionPoseSeen)
            {
                Shot("surprise-first-reactions-player-view");reactionShot=true;
                report.observations.Add("First-reaction image captured at WaitForEndOfFrame in the actual player's current view.");
            }
        }
        while(ambient.AssaultElapsed<5);

        int observedReactions=0,observedFlights=0;
        Check("at least two rushing actors visibly cross the normal camera within five seconds",visibleRush.Count>=2);
        report.observations.Add("Actual player camera incursion: visible unobstructed actors="+visibleRush.Count+"; projected body heights at 900px="+string.Join(",",rushPixels.Values));
        for(int i=0;i<count;i++)if(ambient.spectators[i]&&ambient.spectators[i].gameObject.activeInHierarchy)
        {
            if(firstReaction[i]>=0)observedReactions++;
            if(firstFlight[i]>=0)observedFlights++;
            report.observations.Add("Crowd "+i+" "+ambient.spectators[i].name+
                " firstReactFrame="+firstReaction[i]+" at="+firstReactionTime[i]+
                " firstFlightFrame="+firstFlight[i]+" at="+firstFlightTime[i]+
                " final="+ambient.SpectatorPhase(i));
        }
        Check("different crowd phases coexist in rendered frames",mixedPhases&&mixedCalmAndReaction&&mixedReactionAndFlight);
        Check("every spectator has a rendered reaction before flight",chronological&&observedReactions==activeSpectators);
        Check("all spectators eventually begin flight",observedFlights==activeSpectators);
        Check("flight does not begin for everyone on one frame",flightFrames.Count>1);
        Check("unaware spectators stay in their watching positions",calmStayedAtHome);
        Check("reaction phase produces an actual startled pose",reactionPoseSeen);
        Check("flight phase moves spectators out of their reaction positions",flightMotionSeen);
        Check("all three ambushers act together after the same signal",visibleCounts.Count==1&&visibleCounts.Contains(3));
        for(int i=0;i<3;i++)Check("ambusher "+i+" advances from inside after cue",Flat(ambient.Ambusher(i).position,ambient.AmbushStart(i))>1&&ambient.Ambusher(i).position.z>5);
        CheckAmbientBystanders(ambient);

        var camera=f.player.view;Vector3 originalPosition=camera.transform.position;
        Quaternion originalRotation=camera.transform.rotation;float originalFov=camera.fieldOfView;
        // ShotAt creates/destroys a separate disabled diagnostic camera.
        // The user's live camera must be unchanged when it returns.
        ShotAt("surprise-scattered-flight-diagnostic",new Vector3(0,3,8),new Vector3(0,1,27));
        Check("diagnostic render preserves the live player camera",
            (camera.transform.position-originalPosition).sqrMagnitude<.000001f&&
            Quaternion.Angle(camera.transform.rotation,originalRotation)<.001f&&
            Mathf.Abs(camera.fieldOfView-originalFov)<.001f);
        report.observations.Add("Ambient frames="+samples+" visibleAssailantCounts=["+
            string.Join(",",visibleCounts)+"]; second image is a diagnostic overview, not the normal player view.");
    }

    static void CheckSurpriseCalm(Chapter3Flow f)
    {
        var ambient=f.school.GetComponentInChildren<Chapter3SchoolAmbient>(true);
        Check("calm ambient exists",ambient);
        Check("calm reset cancels the panic clock",!ambient.Panicking&&ambient.AssaultElapsed==0&&ambient.PanicOrigin==Vector3.zero);
        Check("calm reset hides assailants and aftermath",ambient.VisibleAssailants==0&&ambient.FallenAdults==0&&!ambient.AftermathVisible);
        Check("calm reset hides the recalled companion",!ambient.Straggler||!ambient.Straggler.gameObject.activeSelf);
        Check("calm reset clears reacting/fleeing counters",ambient.ReactingSpectators==0&&ambient.FleeingSpectators==0&&
            ambient.ReactingRunners==0&&ambient.FleeingRunners==0);
        bool sameInstance=calmAmbientBaseline==ambient;
        if(!sameInstance){calmAmbientBaseline=ambient;calmSpectatorHomes.Clear();}
        int visible=0;
        for(int i=0;i<ambient.spectators.Length;i++)
        {
            var spectator=ambient.spectators[i];if(!spectator)continue;
            if(spectator.gameObject.activeInHierarchy)visible++;
            var actor=spectator.GetComponent<Chapter3SchoolCrowdActor>();
            Check("spectator "+i+" resets to calm without pending delays",
                ambient.SpectatorPhase(i)==Chapter3SchoolAmbient.CrowdPhase.Calm&&
                ambient.SpectatorReactionDelay(i)==0&&ambient.SpectatorFlightDelay(i)==0);
            Check("spectator "+i+" clears reaction and casualty pose",actor&&!actor.Posing&&!actor.IsFallen&&actor.StartleWeight==0);
            if(sameInstance&&calmSpectatorHomes.TryGetValue(spectator,out var home))
                Check("spectator "+i+" returns to original watching position",(spectator.position-home).sqrMagnitude<.0001f);
            else calmSpectatorHomes[spectator]=spectator.position;
        }
        Check("calm population count matches visible spectators",ambient.CalmSpectators==visible&&visible>0);
        for(int i=0;i<ambient.runners.Length;i++)if(ambient.runners[i])
            Check("runner "+i+" resets to sports activity",ambient.RunnerPhase(i)==Chapter3SchoolAmbient.CrowdPhase.Calm);
        CheckAmbientBystanders(ambient);
        report.observations.Add(sameInstance?"Compared reset spectator positions with the earlier calm baseline.":
            "Recorded calm spectator positions; call CheckSurpriseCalm again after F8 to verify their restoration.");
    }

    static void CheckAmbientBystanders(Chapter3SchoolAmbient ambient)
    {
        int protectedActors=0;
        foreach(var actor in ambient.GetComponentsInChildren<Chapter3SchoolCrowdActor>(true))
        {
            Check(actor.name+" retains bystander protection",actor.GetComponent<Chapter3Bystander>());
            Check(actor.name+" is not a combat damage target",!actor.GetComponentInChildren<Chapter3Enemy>(true));
            foreach(var collider in actor.GetComponentsInChildren<Collider>(true))
                Check(actor.name+" collider resolves to protected bystander",collider.GetComponentInParent<Chapter3Bystander>(true));
            protectedActors++;
        }
        foreach(var runner in ambient.runners)if(runner)
        {
            Check(runner.name+" retains bystander protection",runner.GetComponent<Chapter3Bystander>());
            Check(runner.name+" is not a combat damage target",!runner.GetComponentInChildren<Chapter3Enemy>(true));
            protectedActors++;
        }
        Check("bystander checks cover authored people and staged participants",protectedActors>ambient.spectators.Length);
    }
}

