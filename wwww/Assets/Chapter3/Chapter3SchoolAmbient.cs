using UnityEngine;

// Sports-day background, independent of mission completion and rescue choices.
// Panic is a visible, non-graphic staged assault, never a damage/score system.
public sealed class Chapter3SchoolAmbient : MonoBehaviour
{
    public Transform[] spectators;
    public Chapter2Actor[] runners;
    public Vector3[] runningTrack;
    Quaternion[] rotations,runnerRotations;
    Vector3[] homes,runnerHomes,escapePoints;
    bool[] activeHomes;
    int[] waypoint;
    Chapter3PathMotor[] motors;
    Chapter3SchoolCrowdActor[] crowd;
    readonly Chapter3SchoolCrowdActor[] assailants=new Chapter3SchoolCrowdActor[3];
    readonly int[] assailantSteps=new int[3];
    readonly Vector3[] assaultStarts={new Vector3(-8,0,1),new Vector3(8,0,1),new Vector3(-11,0,4)};
    readonly Vector3[][] assaultRoutes={
        new[]{new Vector3(-2,0,18),new Vector3(5.5f,0,27),new Vector3(8,0,35)},
        new[]{new Vector3(2,0,20),new Vector3(8,0,28),new Vector3(11,0,35)},
        new[]{new Vector3(-8,0,23),new Vector3(-7,0,32),new Vector3(-11,0,39)}};
    readonly Vector3[] aftermathPoints={new Vector3(3,0,27),new Vector3(-9,0,32)};
    readonly int[] casualties={-1,-1};
    readonly float[] fallBegan={-1,-1};
    Chapter3SchoolCrowdActor straggler;
    float panicBegan,nextUpdate;
    bool initialized;
    public bool Panicking {get;private set;}
    public float AssaultElapsed=>Panicking?Mathf.Max(0,Time.time-panicBegan):0;
    public Transform Straggler=>straggler?straggler.transform:null;
    public int VisibleAssailants
    {
        get{int count=0;foreach(var actor in assailants)if(actor&&actor.gameObject.activeInHierarchy)count++;return count;}
    }
    public int FallenAdults
    {
        get{int count=0;foreach(int index in casualties)if(index>=0&&crowd[index]&&crowd[index].IsFallen)count++;return count;}
    }
    public bool AftermathVisible=>Panicking&&FallenAdults==2;
    void Start(){Initialize();}
    void Initialize()
    {
        if(initialized)return;initialized=true;
        spectators=spectators??new Transform[0];runners=runners??new Chapter2Actor[0];
        runningTrack=runningTrack??new Vector3[0];
        rotations=new Quaternion[spectators.Length];homes=new Vector3[spectators.Length];
        activeHomes=new bool[spectators.Length];escapePoints=new Vector3[spectators.Length];crowd=new Chapter3SchoolCrowdActor[spectators.Length];
        Transform template=null;
        var school=GetComponentInParent<Chapter3SchoolController>();
        Chapter2Actor nativeSource=school?school.bawan:null;
        for(int i=0;i<spectators.Length;i++)
        {
            if(!spectators[i])continue;
            rotations[i]=spectators[i].localRotation;homes[i]=spectators[i].position;activeHomes[i]=spectators[i].gameObject.activeSelf;
            crowd[i]=spectators[i].name.Contains("觀賽族人")&&nativeSource
                ?Chapter3SchoolCrowdActor.AttachTo(spectators[i],nativeSource)
                :Chapter3SchoolCrowdActor.AttachTo(spectators[i]);
            if(!template&&spectators[i].name.Contains("觀賽族人"))template=spectators[i];
            float side=homes[i].x<0?-1:1;
            // Different destinations keep fleeing guests off the central route.
            escapePoints[i]=new Vector3(side*(21+i%3*.7f),0,6+(i*7)%35);
        }
        for(int eventIndex=0;eventIndex<casualties.Length;eventIndex++)
        {
            float closest=float.PositiveInfinity;
            for(int i=0;i<spectators.Length;i++)
            {
                // Only authored adult guests; schoolchildren are never chosen.
                if(!spectators[i]||!spectators[i].name.Contains("觀賽婦人")||i==casualties[0])continue;
                float distance=(homes[i]-aftermathPoints[eventIndex]).sqrMagnitude;
                if(distance<closest){closest=distance;casualties[eventIndex]=i;}
            }
        }
        waypoint=new int[runners.Length];motors=new Chapter3PathMotor[runners.Length];
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
            Material wood=FindWood();
            for(int i=0;i<assailants.Length;i++)
            {
                assailants[i]=Chapter3SchoolCrowdActor.CreateFrom(template,transform,"衝入運動場的族人 "+(i+1));
                assailants[i].transform.position=assaultStarts[i];AddBambooSpear(assailants[i],wood);
                assailants[i].gameObject.SetActive(false);
            }
            straggler=Chapter3SchoolCrowdActor.CreateFrom(template,transform,"運動場邊落後的族人");
            straggler.IsRouteCompanion=true;
            straggler.gameObject.SetActive(false);
        }
    }
    Material FindWood()
    {
        var school=GetComponentInParent<Chapter3SchoolController>();
        if(!school)return null;
        foreach(var renderer in school.GetComponentsInChildren<Renderer>(true))
            foreach(var material in renderer.sharedMaterials)
                if(material&&material.name=="School structural timber")return material;
        return null;
    }
    static void AddBambooSpear(Chapter3SchoolCrowdActor actor,Material material)
    {
        if(!actor.RightHand||!material)return;
        var spear=GameObject.CreatePrimitive(PrimitiveType.Cylinder);spear.name="族人竹槍 · 演出用無傷害";
        var collider=spear.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        actor.AttachPresentationSpear(spear.transform);
        spear.GetComponent<Renderer>().sharedMaterial=material;
    }
    public void ResetCalm()
    {
        Initialize();Panicking=false;nextUpdate=0;
        for(int i=0;i<fallBegan.Length;i++)fallBegan[i]=-1;
        for(int i=0;i<spectators.Length;i++)if(spectators[i])
        {
            crowd[i].ResetPose();spectators[i].position=homes[i];spectators[i].localRotation=rotations[i];
            if(spectators[i].gameObject.activeSelf!=activeHomes[i])spectators[i].gameObject.SetActive(activeHomes[i]);
        }
        for(int i=0;i<runners.Length;i++)if(runners[i])
        {
            runners[i].transform.SetPositionAndRotation(runnerHomes[i],runnerRotations[i]);
            runners[i].facing=runnerRotations[i]*Vector3.forward;motors[i].Reset();
            waypoint[i]=runningTrack.Length>0?i%runningTrack.Length:0;
        }
        for(int i=0;i<assailants.Length;i++)if(assailants[i])
        {assailants[i].ResetPose();assailants[i].gameObject.SetActive(false);assailants[i].transform.SetPositionAndRotation(assaultStarts[i],Quaternion.identity);assailantSteps[i]=0;}
        HideStraggler();
    }
    public void Panic()
    {
        Initialize();if(Panicking)return;
        Panicking=true;panicBegan=Time.time;
        foreach(var actor in crowd)if(actor){actor.SetAnimated(true);actor.SetAfraid(true);}
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
        if(!Panicking)
        {
            if(Time.time>=nextUpdate)
            {
                nextUpdate=Time.time+.12f;
                for(int i=0;i<spectators.Length;i++)if(spectators[i])
                    spectators[i].localRotation=rotations[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*.36f+i*1.8f)*3.3f,0);
            }
        }
        else UpdateAssault();
        for(int i=0;i<runners.Length;i++)
        {
            if(!runners[i])continue;
            if(Panicking){motors[i].Move(runners[i],new Vector3(-21-i*.8f,0,9+i*27),3.6f);continue;}
            if(runningTrack.Length==0)continue;
            if(Vector3.ProjectOnPlane(runners[i].transform.position-runningTrack[waypoint[i]],Vector3.up).magnitude<.2f)
                waypoint[i]=(waypoint[i]+1)%runningTrack.Length;
            motors[i].Move(runners[i],runningTrack[waypoint[i]],1.7f);
        }
    }
    void UpdateAssault()
    {
        float elapsed=AssaultElapsed;
        for(int i=0;i<assailants.Length;i++)
        {
            var actor=assailants[i];if(!actor||elapsed<i*.45f)continue;
            if(!actor.gameObject.activeSelf){actor.gameObject.SetActive(true);actor.SetAnimated(true);}
            int step=assailantSteps[i];
            if(step<assaultRoutes[i].Length)
            {
                // The first crossing reaches the viewer's centre within the
                // shared outbreak look. Later gestures have no hit/damage API.
                if(step==2&&elapsed<10+i)
                {actor.Face(aftermathPoints[i==2?1:0]);actor.SetStrike(Mathf.Repeat(elapsed*.65f,1));continue;}
                actor.SetStrike(0);
                if(actor.MoveTowards(assaultRoutes[i][step],step==0?4.8f:3.4f))assailantSteps[i]++;
            }
        }
        for(int i=0;i<crowd.Length;i++)
        {
            var actor=crowd[i];if(!actor||elapsed<.25f+(i%4)*.16f)continue;
            int eventIndex=i==casualties[0]?0:i==casualties[1]?1:-1;
            if(eventIndex>=0)
            {
                bool arrived=actor.MoveTowards(aftermathPoints[eventIndex],2.8f);
                if(arrived&&elapsed>=7+eventIndex*1.8f)
                {
                    // A blocked guest still performs the full collapse after
                    // arriving; the shared outbreak clock must not skip it.
                    if(fallBegan[eventIndex]<0)fallBegan[eventIndex]=Time.time;
                    actor.SetFall((Time.time-fallBegan[eventIndex])/1.7f);
                }
            }
            else actor.MoveTowards(escapePoints[i],2.3f+(i%3)*.25f);
        }
    }
}
