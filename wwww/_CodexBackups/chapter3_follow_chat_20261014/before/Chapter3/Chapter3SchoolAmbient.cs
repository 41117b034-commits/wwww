using UnityEngine;

// Authored spectators share baked meshes. Only two runners use walking rigs,
// keeping the sports-day crowd inexpensive. The outbreak disperses spectators
// and sends the two animated runners to the edge of the field.
public sealed class Chapter3SchoolAmbient : MonoBehaviour
{
    public Transform[] spectators;
    public Chapter2Actor[] runners;
    public Vector3[] runningTrack;
    Quaternion[] rotations;
    int[] waypoint;
    Chapter3PathMotor[] motors;
    float nextUpdate;
    Vector3[] homes,runnerHomes;
    Quaternion[] runnerRotations;
    public bool Panicking {get;private set;}
    bool initialized;
    void Start(){Initialize();}
    void Initialize()
    {
        if(initialized)return;initialized=true;
        rotations=new Quaternion[spectators.Length];
        homes=new Vector3[spectators.Length];
        for(int i=0;i<spectators.Length;i++)
        {rotations[i]=spectators[i].localRotation;homes[i]=spectators[i].position;if(!spectators[i].GetComponent<Chapter3Bystander>())spectators[i].gameObject.AddComponent<Chapter3Bystander>();}
        waypoint=new int[runners.Length];motors=new Chapter3PathMotor[runners.Length];
        runnerHomes=new Vector3[runners.Length];runnerRotations=new Quaternion[runners.Length];
        for(int i=0;i<runners.Length;i++)
        {motors[i]=new Chapter3PathMotor();waypoint[i]=i%runningTrack.Length;runnerHomes[i]=runners[i].transform.position;runnerRotations[i]=runners[i].transform.rotation;if(!runners[i].GetComponent<Chapter3Bystander>())runners[i].gameObject.AddComponent<Chapter3Bystander>();}
    }
    public void ResetCalm()
    {
        Initialize();Panicking=false;nextUpdate=0;
        for(int i=0;i<spectators.Length;i++){spectators[i].position=homes[i];spectators[i].localRotation=rotations[i];}
        for(int i=0;i<runners.Length;i++){runners[i].transform.SetPositionAndRotation(runnerHomes[i],runnerRotations[i]);motors[i].Reset();waypoint[i]=i%runningTrack.Length;}
    }
    public void Panic(){Initialize();Panicking=true;foreach(var motor in motors)motor.Reset();}
    void Update()
    {
        if(Time.time>=nextUpdate)
        {
            nextUpdate=Time.time+.12f;
            for(int i=0;i<spectators.Length;i++)
            {
                if(!Panicking)spectators[i].localRotation=rotations[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*.36f+i*1.8f)*3.3f,0);
                else
                {
                    float side=homes[i].x<0?-1:1;
                    Vector3 escape=homes[i]+new Vector3(side*(1.3f+i%3*.35f),0,i%2==0?.5f:-.5f);
                    Vector3 next=Vector3.MoveTowards(spectators[i].position,escape,.12f*1.6f);
                    bool clear=true;
                    foreach(var hit in Physics.SphereCastAll(spectators[i].position+Vector3.up*.7f,.22f,(next-spectators[i].position).normalized,Vector3.Distance(next,spectators[i].position),~0,QueryTriggerInteraction.Ignore))
                        if(!hit.collider.transform.IsChildOf(spectators[i])&&hit.collider.bounds.max.y>.3f){clear=false;break;}
                    if(clear)spectators[i].position=next;
                    spectators[i].rotation=Quaternion.Slerp(spectators[i].rotation,Quaternion.LookRotation(new Vector3(side,0,.3f)),.15f);
                }
            }
        }
        for(int i=0;i<runners.Length;i++)
        {
            if(Panicking){motors[i].Move(runners[i],new Vector3(-18+i*1.3f,0,25+i*3),3.6f);continue;}
            var target=runningTrack[waypoint[i]];
            if(Vector3.ProjectOnPlane(runners[i].transform.position-target,Vector3.up).magnitude<.2f)
                waypoint[i]=(waypoint[i]+1)%runningTrack.Length;
            motors[i].Move(runners[i],runningTrack[waypoint[i]],1.7f);
        }
    }
}
