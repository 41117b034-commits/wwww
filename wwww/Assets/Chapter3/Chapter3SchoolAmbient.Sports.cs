using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    readonly Chapter3PathMotor[] sportsMotors=new Chapter3PathMotor[20];
    readonly int[] sportsLeg=new int[20];
    readonly float[] sportsPause=new float[20];
    readonly Chapter3PathMotor[] childSportsMotors={new Chapter3PathMotor(),new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly int[] childSportsLeg=new int[3];
    float sportsStarted;
    static bool WalksAtSports(int i)=>i==0||i==2||i==7||i==10||i==14||i==16;

    void ResetCalmSports()
    {
        sportsStarted=Time.time;
        for(int i=0;i<sportsMotors.Length;i++)
        {if(sportsMotors[i]==null)sportsMotors[i]=new Chapter3PathMotor();sportsMotors[i].Reset();sportsLeg[i]=0;sportsPause[i]=Time.time+(i%3)*.6f;}
        for(int i=0;i<3;i++){childSportsMotors[i].Reset();childSportsLeg[i]=0;}
        // Animation is already active during the distant approach, without
        // waiting for the school mission or restarting when the gate is reached.
        if(crowd!=null)foreach(var guest in crowd)if(guest){guest.SetAnimated(true);guest.SetAfraid(false);guest.SetWaiting(false);guest.SetApplause(0);}
        for(int i=0;i<3;i++)if(assailants[i])assailants[i].SetAnimated(true);
    }
    void UpdateCalmSports()
    {
        if(Panicking||crowd==null||homes==null)return;
        float elapsed=Time.time-sportsStarted;
        for(int i=0;i<Mathf.Min(18,crowd.Length);i++)
        {
            var guest=crowd[i];if(!guest||!guest.gameObject.activeInHierarchy)continue;
            if(WalksAtSports(i)&&Time.time>=sportsPause[i])
            {
                Vector3 offset=sportsLeg[i]==0?new Vector3(i%2==0?1.15f:-1.15f,0,2.7f):Vector3.zero;
                Vector3 target=homes[i]+offset;
                sportsMotors[i].Patrol(guest,target,i<10?.75f:1.05f,.16f);
                guest.SetApplause(0);
                if(Horizontal(guest.transform.position,target)<.22f)
                {sportsLeg[i]=1-sportsLeg[i];sportsPause[i]=Time.time+2.4f+(i%4)*.45f;sportsMotors[i].Reset();}
            }
            else
            {
                // Watch the race and clap in staggered short bursts. These
                // are ordinary spectators, not frozen combat starting poses.
                Vector3 race=crowd.Length>19&&crowd[18+i%2]?crowd[18+i%2].transform.position:new Vector3(-3,0,22);
                guest.Face(race,24);
                float phase=Mathf.Repeat(elapsed+i*1.73f,8.5f);
                float applause=Mathf.SmoothStep(0,1,Mathf.Min(phase/.5f,(3.4f-phase)/.6f));
                guest.SetApplause(applause);
            }
        }
        // The same three Indigenous children stay inside the school until
        // the whistle; this does not create another crowd outside the gate.
        for(int i=0;i<3;i++)if(assailants[i]&&assailants[i].gameObject.activeInHierarchy)
        {
            Vector3 target=assaultStarts[i]+(childSportsLeg[i]==0?new Vector3(.25f,0,1.8f+i*.3f):Vector3.zero);
            childSportsMotors[i].Patrol(assailants[i],target,.7f+i*.08f,.18f);
            if(Horizontal(assailants[i].transform.position,target)<.22f)
            {childSportsLeg[i]=1-childSportsLeg[i];childSportsMotors[i].Reset();}
        }
    }
}
