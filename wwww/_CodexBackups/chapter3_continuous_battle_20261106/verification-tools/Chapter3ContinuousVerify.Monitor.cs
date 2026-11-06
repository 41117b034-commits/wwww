using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class Chapter3ContinuousVerify
{
    static Vector3[] previousParty;
    static float partyTime,nextPartySnapshot;
    static float maxPartySpeed;
    static Vector3? endingPartyAnchor;
    static float maxEndingAnchorDrift;
    public static void MonitorParty()
    {
        if(!f||report==null||!f.school)return;
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        if(!a||!a.Withdrawing){previousParty=null;return;}
        if(f.school.Battle.Rescue&&f.school.Battle.Rescue.Ending&&f.school.Battle.Rescue.Ending.Active)
        {
            Vector3 anchor=(Vector3)Get(a,"forestFollowPosition");
            if(!endingPartyAnchor.HasValue)endingPartyAnchor=anchor;
            maxEndingAnchorDrift=Mathf.Max(maxEndingAnchorDrift,Flat(anchor,endingPartyAnchor.Value));
        }
        var people=Enumerable.Range(0,10).Select(a.Ambusher).ToArray();if(people.Any(p=>!p))return;
        if(previousParty!=null&&Time.time>partyTime)
            for(int i=0;i<10;i++)maxPartySpeed=Mathf.Max(maxPartySpeed,Flat(people[i].position,previousParty[i])/(Time.time-partyTime));
        previousParty=people.Select(p=>p.position).ToArray();partyTime=Time.time;
        if(Time.realtimeSinceStartup<nextPartySnapshot)return;nextPartySnapshot=Time.realtimeSinceStartup+5;
        var legs=(int[])Get(a,"withdrawalLeg");var routes=(Vector3[][])Get(a,"withdrawalRoutes");
        File.WriteAllText(Dir+"party-progress.txt",Snapshot()+"\nplayer="+f.player.transform.position+" anchor="+Get(a,"forestFollowPosition")+" arrived="+a.WithdrawalArrivedCount+" forest="+a.ForestFollowerCount+" maxSpeed="+maxPartySpeed+"\n"+
            string.Join("\n",people.Select((p,i)=>p.name+" "+p.position+" leg="+legs[i]+"/"+routes[i].Length+" next="+(legs[i]<routes[i].Length?routes[i][legs[i]].ToString():"forest")))+"\n"+PartyNavigation(a)+"\n"+EscortSnapshot(a));
    }
    static string PartyNavigation(Chapter3SchoolAmbient a)
    {
        var motors=(Chapter3PathMotor[])Get(a,"withdrawalMotors");var lines=new List<string>();
        for(int i=0;i<10;i++)
        {
            var actor=a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>();var m=motors[i];var path=(List<Vector3>)Get(m,"path");
            var target=(Vector3)Get(m,"goal");if((bool)Get(m,"searching"))target=(Vector3)Get(m,"searchTarget");
            var next=Vector3.MoveTowards(actor.transform.position,path.Count>0?path[0]:target,.05f);
            lines.Add(actor.name+" goal="+target+" path="+string.Join(";",path)+" search="+Get(m,"searching")+" iterations="+Get(m,"iterations")+" motorClear="+Invoke(m,"ClearForActor",actor.transform.position,next)+" bodyClear="+Invoke(actor,"Clear",next));
            foreach(var c in Physics.OverlapCapsule(target+Vector3.up*.42f,target+Vector3.up*1.25f,.36f,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(actor.transform))lines.Add(" goal overlap="+c.name+" "+c.transform.position);
            foreach(var c in Physics.OverlapCapsule(next+Vector3.up*.42f,next+Vector3.up*1.25f,.36f,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(actor.transform))lines.Add(" step overlap="+c.name+" "+c.transform.position);
        }
        return string.Join("\n",lines);
    }
}
