using UnityEngine;

// Shared presentation for all three choices. Placement and timing are fictional
// staging; these gestures never inflict player damage or award combat credit.
public sealed partial class Chapter3SchoolAmbient
{
    readonly int[] skirmishGuests={-1,-1,-1};
    readonly Vector3[] skirmishPoints={new Vector3(-12,0,18),new Vector3(12,0,25),new Vector3(-10,0,35)};
    readonly bool[] skirmishArrived=new bool[3];
    bool skirmishActive;
    public int AmbushSignalFrame {get;private set;}=-1;
    public bool SkirmishActive=>Panicking&&skirmishActive;
    public Vector3 AmbushStart(int index)=>assaultStarts[index];
    public Transform Ambusher(int index)=>assailants[index]?assailants[index].transform:null;
    public int SkirmishPairsReady {get;private set;}

    void ConfigureSkirmish()
    {
        for(int pair=0;pair<skirmishGuests.Length;pair++)
        {
            float best=float.PositiveInfinity;
            for(int i=0;i<crowd.Length;i++)
            {
                if(!crowd[i]||!activeHomes[i]||spectatorKinds[i]!=SpectatorKind.Guest||i==casualties[0]||i==casualties[1])continue;
                bool used=false;for(int p=0;p<pair;p++)if(skirmishGuests[p]==i)used=true;
                float distance=(homes[i]-skirmishPoints[pair]).sqrMagnitude;
                if(!used&&distance<best){best=distance;skirmishGuests[pair]=i;}
            }
        }
    }
    void ResetSkirmish()
    {
        skirmishActive=false;AmbushSignalFrame=-1;SkirmishPairsReady=0;
        for(int i=0;i<skirmishArrived.Length;i++)skirmishArrived[i]=false;
    }
    public void EndSkirmish()
    {
        skirmishActive=false;SkirmishPairsReady=0;
        foreach(var actor in assailants)if(actor)actor.SetStrike(0);
    }
    float StrikeBeat(int pair)
    {
        float beat=Mathf.Repeat(AssaultElapsed-pair*.65f,3.8f);
        return beat<1.1f?Mathf.Clamp(beat/1.1f,.01f,.99f):0;
    }
    void UpdateAmbush(float elapsed)
    {
        SkirmishPairsReady=0;
        for(int i=0;i<assailants.Length;i++)
        {
            var actor=assailants[i];if(!actor||!skirmishActive)continue;
            int step=assailantSteps[i];
            if(step<assaultRoutes[i].Length)
            {
                actor.SetStrike(0);
                if(actor.MoveTowards(assaultRoutes[i][step],3.8f))assailantSteps[i]++;
                continue;
            }
            if(!AftermathVisible)
            {actor.Face(aftermathPoints[i==2?1:0]);actor.SetStrike(StrikeBeat(i));continue;}
            if(!skirmishArrived[i])
            {actor.SetStrike(0);skirmishArrived[i]=actor.MoveTowards(skirmishPoints[i],2.8f);continue;}
            int guest=skirmishGuests[i];if(guest<0)continue;
            actor.Face(crowd[guest].transform.position,100);
            bool near=(crowd[guest].transform.position-actor.transform.position).sqrMagnitude<6.25f;
            actor.SetStrike(near?StrikeBeat(i):0);
            if(near)SkirmishPairsReady++;
        }
    }
    bool UpdateSkirmishGuest(int index)
    {
        if(!skirmishActive)return false;
        int pair=System.Array.IndexOf(skirmishGuests,index);if(pair<0)return false;
        var guest=crowd[index];var target=skirmishPoints[pair]+Vector3.forward*1.65f;
        if(guest.MoveTowards(target,2.6f))
        {
            guest.Face(assailants[pair].transform.position,100);
            guest.SetStartled(skirmishArrived[pair]?Mathf.Sin(StrikeBeat(pair)*Mathf.PI)*.65f:0);
        }
        return true;
    }
}
