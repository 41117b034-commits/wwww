using UnityEngine;

// Sports-day background, independent of mission completion and rescue choices.
// Panic is a visible, non-graphic staged assault, never a damage/score system.
public sealed partial class Chapter3SchoolAmbient : MonoBehaviour
{
    public enum CrowdPhase { Calm, Reacting, Fleeing, Falling, Fallen }
    enum SpectatorKind { Guest, Child, Indigenous, Police }
    public Transform[] spectators;
    public Chapter2Actor[] runners;
    public Vector3[] runningTrack;
    Quaternion[] rotations,runnerRotations;
    Vector3[] homes,runnerHomes,escapePoints,hesitationPoints;
    bool[] activeHomes;
    int[] waypoint;
    Chapter3PathMotor[] motors;
    Chapter3SchoolCrowdActor[] crowd;
    CrowdPhase[] crowdPhases,runnerPhases;
    SpectatorKind[] spectatorKinds;
    float[] reactionDelays,flightDelays,runnerReactionDelays,runnerFlightDelays;
    public const int AmbusherCount=10;
    readonly Chapter3SchoolCrowdActor[] assailants=new Chapter3SchoolCrowdActor[AmbusherCount];
    readonly Vector3[] assaultStarts=new Vector3[AmbusherCount];
    Chapter3SchoolCrowdActor straggler;
    float panicBegan;
    bool initialized;
    public bool Panicking {get;private set;}
    public Vector3 PanicOrigin {get;private set;}
    public float AssaultElapsed=>Panicking?Mathf.Max(0,Time.time-panicBegan):0;
    public int CalmSpectators=>CountPhase(CrowdPhase.Calm);
    public int ReactingSpectators=>CountPhase(CrowdPhase.Reacting);
    public int FleeingSpectators=>CountPhase(CrowdPhase.Fleeing);
    public int ReactingRunners=>CountRunnerPhase(CrowdPhase.Reacting);
    public int FleeingRunners=>CountRunnerPhase(CrowdPhase.Fleeing);
    public CrowdPhase SpectatorPhase(int index)=>crowdPhases!=null&&index>=0&&index<crowdPhases.Length?crowdPhases[index]:CrowdPhase.Calm;
    public CrowdPhase RunnerPhase(int index)=>runnerPhases!=null&&index>=0&&index<runnerPhases.Length?runnerPhases[index]:CrowdPhase.Calm;
    public float SpectatorReactionDelay(int index)=>reactionDelays!=null&&index>=0&&index<reactionDelays.Length?reactionDelays[index]:0;
    public float SpectatorFlightDelay(int index)=>flightDelays!=null&&index>=0&&index<flightDelays.Length?flightDelays[index]:0;
    public Transform Straggler=>straggler?straggler.transform:null;
    public int VisibleAssailants
    {
        get{int count=0;foreach(var actor in assailants)if(actor&&actor.gameObject.activeInHierarchy)count++;return count;}
    }
    public int FallenAdults
    {
        get{return CountFallen(SpectatorKind.Guest);}
    }
    public int FallenChildren=>CountFallen(SpectatorKind.Child);
    public int JapaneseAdults=>10;
    public int JapaneseChildren=>10;
    public bool AftermathVisible=>Panicking&&FallenAdults>=2&&FallenChildren>=2;
    int CountFallen(SpectatorKind kind)
    {
        int count=0;if(crowd!=null)for(int i=0;i<crowd.Length;i++)
            if(spectatorKinds[i]==kind&&crowd[i]&&crowd[i].IsFallen)count++;
        return count;
    }
    int CountPhase(CrowdPhase phase)
    {
        int count=0;if(crowdPhases!=null)for(int i=0;i<crowdPhases.Length;i++)
            if(crowd[i]&&crowd[i].gameObject.activeInHierarchy&&crowdPhases[i]==phase)count++;
        return count;
    }
    int CountRunnerPhase(CrowdPhase phase)
    {
        int count=0;if(runnerPhases!=null)for(int i=0;i<runnerPhases.Length;i++)
            if(runners[i]&&runners[i].gameObject.activeInHierarchy&&runnerPhases[i]==phase)count++;
        return count;
    }
    void Start(){Initialize();}
    void Initialize()
    {
        if(initialized)return;initialized=true;
        spectators=spectators??new Transform[0];runners=runners??new Chapter2Actor[0];
        runningTrack=runningTrack??new Vector3[0];
        Transform template=PreparePopulation();
        // The authored running lane crossed the entrance equipment table.
        // Keep both pupils in the open track before the signal and handoff.
        runningTrack=new[]{new Vector3(-4,0,14),new Vector3(-4,0,32),new Vector3(-2.4f,0,32),new Vector3(-2.4f,0,14)};
        for(int i=0;i<runners.Length;i++)if(runners[i])runners[i].transform.position=runningTrack[(i*2)%runningTrack.Length];
        rotations=new Quaternion[spectators.Length];homes=new Vector3[spectators.Length];
        activeHomes=new bool[spectators.Length];escapePoints=new Vector3[spectators.Length];hesitationPoints=new Vector3[spectators.Length];crowd=new Chapter3SchoolCrowdActor[spectators.Length];
        crowdPhases=new CrowdPhase[spectators.Length];spectatorKinds=new SpectatorKind[spectators.Length];
        reactionDelays=new float[spectators.Length];flightDelays=new float[spectators.Length];
        int leftSlot=0,rightSlot=0;
        var school=GetComponentInParent<Chapter3SchoolController>();
        Chapter2Actor nativeSource=school?school.bawan:null;
        for(int i=0;i<spectators.Length;i++)
        {
            if(!spectators[i])continue;
            rotations[i]=spectators[i].localRotation;homes[i]=spectators[i].position;activeHomes[i]=spectators[i].gameObject.activeSelf;
            string identity=spectators[i].name;
            spectatorKinds[i]=identity.Contains("學童")?SpectatorKind.Child:identity.Contains("觀賽族人")?SpectatorKind.Indigenous:
                identity.Contains("警察")?SpectatorKind.Police:SpectatorKind.Guest;
            // The two actual officers already attend the event. Do not render
            // duplicate uniformed guests that reject every shot as civilians.
            if(spectatorKinds[i]==SpectatorKind.Police){activeHomes[i]=false;spectators[i].gameObject.SetActive(false);}
            crowd[i]=spectators[i].name.Contains("觀賽族人")&&nativeSource
                ?Chapter3SchoolCrowdActor.AttachTo(spectators[i],nativeSource)
                :Chapter3SchoolCrowdActor.AttachTo(spectators[i]);
            if(!template&&spectators[i].name.Contains("觀賽族人"))template=spectators[i];
            float side=homes[i].x<0?-1:1;
            // Different destinations keep fleeing guests off the central route.
            // Separate reachable waiting spots inside the perimeter. Targets
            // beyond the solid fence used to funnel several people together.
            int slot=activeHomes[i]?(side<0?leftSlot++:rightSlot++):0;
            escapePoints[i]=new Vector3(side*(17.2f+(slot%2)*1.1f),0,7+slot*3.5f);
        }
        waypoint=new int[runners.Length];motors=new Chapter3PathMotor[runners.Length];
        runnerPhases=new CrowdPhase[runners.Length];runnerReactionDelays=new float[runners.Length];runnerFlightDelays=new float[runners.Length];
        runnerHomes=new Vector3[runners.Length];runnerRotations=new Quaternion[runners.Length];
        for(int i=0;i<runners.Length;i++)
        {
            motors[i]=new Chapter3PathMotor();waypoint[i]=runningTrack.Length>0?i%runningTrack.Length:0;
            if(!runners[i])continue;
            runnerHomes[i]=runners[i].transform.position;runnerRotations[i]=runners[i].transform.rotation;
            if(!runners[i].GetComponent<Chapter3Bystander>())runners[i].gameObject.AddComponent<Chapter3Bystander>();
        }
        if(template)
        {
            template.gameObject.SetActive(true);
            Chapter3SchoolCrowdActor.AttachTo(template,nativeSource);
            template.gameObject.SetActive(false);
            for(int i=0;i<assailants.Length;i++)
            {
                assailants[i]=Chapter3SchoolCrowdActor.CreateFrom(template,transform,"校內埋伏的族人 "+(i+1));
                assaultStarts[i]=Pocket(i)+Vector3.back*2.6f;
                assailants[i].transform.position=assaultStarts[i];AddKnife(assailants[i],school);
                assailants[i].gameObject.SetActive(false);
            }
            straggler=Chapter3SchoolCrowdActor.CreateFrom(template,transform,"運動場邊落後的族人");
            straggler.IsRouteCompanion=true;
            straggler.gameObject.SetActive(false);
        }
        ConfigureSkirmish();
    }
    public void ResetCalm()
    {
        Initialize();Panicking=false;panicBegan=0;PanicOrigin=Vector3.zero;ResetSkirmish();
        for(int i=0;i<spectators.Length;i++)if(spectators[i])
        {
            crowdPhases[i]=CrowdPhase.Calm;reactionDelays[i]=flightDelays[i]=0;hesitationPoints[i]=homes[i];
            crowd[i].ResetPose();spectators[i].position=homes[i];spectators[i].localRotation=rotations[i];
            if(spectators[i].gameObject.activeSelf!=activeHomes[i])spectators[i].gameObject.SetActive(activeHomes[i]);
        }
        for(int i=0;i<runners.Length;i++)if(runners[i])
        {
            runners[i].gameObject.SetActive(true);
            runnerPhases[i]=CrowdPhase.Calm;runnerReactionDelays[i]=runnerFlightDelays[i]=0;
            runners[i].transform.SetPositionAndRotation(runnerHomes[i],runnerRotations[i]);
            runners[i].facing=runnerRotations[i]*Vector3.forward;motors[i].Reset();
            waypoint[i]=runningTrack.Length>0?i%runningTrack.Length:0;
        }
        for(int i=0;i<assailants.Length;i++)if(assailants[i])
        {assailants[i].ResetPose();assailants[i].gameObject.SetActive(false);assailants[i].transform.SetPositionAndRotation(assaultStarts[i],Quaternion.identity);}
        HideStraggler();
    }
    public void Panic()
    {Panic(new Vector3(0,0,4));}
    public void Panic(Vector3 origin)
    {
        Initialize();if(Panicking)return;
        Panicking=true;panicBegan=Time.time;PanicOrigin=origin;
        SwitchRunningPupils();
        // These actors already occupy separate positions INSIDE the school.
        // One signal releases all ten in the same frame, on every route.
        AmbushSignalFrame=Time.frameCount;skirmishActive=true;
        foreach(var actor in assailants)if(actor){actor.gameObject.SetActive(true);actor.SetAnimated(true);}
        // Staged perception is a gameplay presentation, not a reconstruction
        // of named victims' individual movements. Nobody reacts before the cue.
        for(int i=0;i<crowd.Length;i++)if(crowd[i])
        {
            float distance=Vector3.ProjectOnPlane(crowd[i].transform.position-origin,Vector3.up).magnitude;
            Vector3 away=Vector3.ProjectOnPlane(crowd[i].transform.position-origin,Vector3.up).normalized;
            hesitationPoints[i]=crowd[i].transform.position+away*(distance<17?.42f:0);
            float role=spectatorKinds[i]==SpectatorKind.Police?.02f:spectatorKinds[i]==SpectatorKind.Child?.32f:
                spectatorKinds[i]==SpectatorKind.Indigenous?.12f:.22f;
            reactionDelays[i]=.12f+Mathf.Min(1.65f,distance*.035f)+role+(i%5)*.105f;
            flightDelays[i]=reactionDelays[i]+.48f+(i%4)*.14f+(spectatorKinds[i]==SpectatorKind.Child?.18f:0);
        }
        for(int i=0;i<runners.Length;i++)if(runners[i])
        {
            float distance=Vector3.ProjectOnPlane(runners[i].transform.position-origin,Vector3.up).magnitude;
            runnerReactionDelays[i]=.32f+Mathf.Min(1.5f,distance*.035f)+i*.16f;
            runnerFlightDelays[i]=runnerReactionDelays[i]+.78f+i*.18f;
        }
        foreach(var motor in motors)motor.Reset();
    }
    public void Suspend(){if(initialized)ResetCalm();}
    void OnDisable(){Suspend();}
    void OnDestroy()
    {
        // Normally these are children of the unloading school. Also release
        // them if only this component is removed during a validation fixture.
        foreach(var actor in assailants)if(actor)Destroy(actor.gameObject);
        if(straggler)Destroy(straggler.gameObject);
        if(crowd!=null)foreach(var actor in crowd)if(actor)Destroy(actor);
        ReleasePopulation();
    }
    public void ResetStraggler(Vector3 position)
    {
        Initialize();if(!straggler)return;
        straggler.ResetPose();straggler.transform.SetPositionAndRotation(position,Quaternion.identity);
        straggler.gameObject.SetActive(true);straggler.SetAnimated(true);straggler.SetWaiting(true);
    }
    public bool MoveStraggler(Vector3 position,float speed)
    {return straggler&&straggler.gameObject.activeSelf&&straggler.MoveTowards(position,speed);}
    public void SetStragglerWaiting(bool value){if(straggler)straggler.SetWaiting(value);}
    public void HideStraggler(){if(straggler){straggler.ResetPose();straggler.gameObject.SetActive(false);}}
    void Update()
    {
        if(!initialized)Initialize();
        if(Panicking)UpdateAssault();
        for(int i=0;i<runners.Length;i++)
        {
            if(!runners[i]||!runners[i].gameObject.activeInHierarchy)continue;
            if(runners[i].Rig)runners[i].Rig.smoothLocomotion=true;
            if(Panicking&&AssaultElapsed>=runnerReactionDelays[i])
            {
                if(AssaultElapsed<runnerFlightDelays[i])
                {
                    if(runnerPhases[i]!=CrowdPhase.Reacting){runnerPhases[i]=CrowdPhase.Reacting;motors[i].Reset();}
                    Vector3 direction=Vector3.ProjectOnPlane(PanicOrigin-runners[i].transform.position,Vector3.up);
                    Vector3 facing=Vector3.RotateTowards(runners[i].facing.normalized,direction.normalized,140*Mathf.Deg2Rad*Time.deltaTime,0);
                    runners[i].Face(runners[i].transform.position+facing);
                }
                else
                {
                    runnerPhases[i]=CrowdPhase.Fleeing;
                    motors[i].Move(runners[i],new Vector3(-14.5f-i*1.2f,0,5+i*30),3.6f);
                }
                continue;
            }
            if(runningTrack.Length==0)continue;
            if(Vector3.ProjectOnPlane(runners[i].transform.position-runningTrack[waypoint[i]],Vector3.up).magnitude<.2f)
                waypoint[i]=(waypoint[i]+1)%runningTrack.Length;
            motors[i].Move(runners[i],runningTrack[waypoint[i]],1.7f);
        }
    }
    void UpdateAssault()
    {
        float elapsed=AssaultElapsed;
        UpdateAmbush(elapsed);
        for(int i=0;i<crowd.Length;i++)
        {
            var actor=crowd[i];if(!actor||!actor.gameObject.activeInHierarchy||elapsed<reactionDelays[i])continue;
            if(crowdPhases[i]==CrowdPhase.Calm)
            {crowdPhases[i]=CrowdPhase.Reacting;actor.SetAnimated(true);}
            if(elapsed<flightDelays[i])
            {
                // A short look and hesitation precede flight. Reuse the
                // existing bones; no extra crowd, mesh, material or physics work.
                float reaction=Mathf.InverseLerp(reactionDelays[i],flightDelays[i],elapsed);
                if(reaction>.55f)actor.MoveTowards(hesitationPoints[i],.85f,false);
                actor.Face(PanicOrigin,140);
                actor.SetStartled(Mathf.Sin(reaction*Mathf.PI));
                continue;
            }
            if(crowdPhases[i]==CrowdPhase.Reacting)
            {crowdPhases[i]=CrowdPhase.Fleeing;actor.SetStartled(0);actor.SetAfraid(true);}
            if(UpdateSkirmishGuest(i))continue;
            actor.MoveTowards(escapePoints[i],2.3f+(i%3)*.25f);
        }
    }
}
