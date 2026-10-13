using UnityEngine;

// Authored spectators share baked meshes. Only two runners use walking rigs,
// keeping the sports-day crowd inexpensive and independent of combat state.
public sealed class Chapter3SchoolAmbient : MonoBehaviour
{
    public Transform[] spectators;
    public Chapter2Actor[] runners;
    public Vector3[] runningTrack;
    Quaternion[] rotations;
    int[] waypoint;
    Chapter3PathMotor[] motors;
    float nextUpdate;
    void Start()
    {
        rotations=new Quaternion[spectators.Length];
        for(int i=0;i<spectators.Length;i++)rotations[i]=spectators[i].localRotation;
        waypoint=new int[runners.Length];motors=new Chapter3PathMotor[runners.Length];
        for(int i=0;i<runners.Length;i++){motors[i]=new Chapter3PathMotor();waypoint[i]=i%runningTrack.Length;}
    }
    void Update()
    {
        if(Time.time>=nextUpdate)
        {
            nextUpdate=Time.time+.12f;
            for(int i=0;i<spectators.Length;i++)
                spectators[i].localRotation=rotations[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*.36f+i*1.8f)*3.3f,0);
        }
        for(int i=0;i<runners.Length;i++)
        {
            var target=runningTrack[waypoint[i]];
            if(Vector3.ProjectOnPlane(runners[i].transform.position-target,Vector3.up).magnitude<.2f)
                waypoint[i]=(waypoint[i]+1)%runningTrack.Length;
            motors[i].Move(runners[i],runningTrack[waypoint[i]],1.7f);
        }
    }
}
