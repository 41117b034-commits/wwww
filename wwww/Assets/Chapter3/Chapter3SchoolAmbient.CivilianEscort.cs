using System.Collections.Generic;
using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    sealed class CivilianEscort
    {
        public Chapter3SchoolCrowdActor actor;
        public readonly Chapter3PathMotor motor=new Chapter3PathMotor();
        public Vector3[] route;
        public int leg;
        public bool gathering;
        public Vector3 home;
        public Quaternion rotation;
    }
    readonly List<CivilianEscort> civilianEscorts=new List<CivilianEscort>();
    public int CivilianFollowerCount=>civilianEscorts.Count;
    public int CivilianArrivedCount
    {get{int count=0;foreach(var escort in civilianEscorts)if(escort.leg>=escort.route.Length)count++;return count;}}
    void BeginCivilianWithdrawal()
    {
        CancelCivilianWithdrawal(false);
        for(int i=0;i<crowd.Length;i++)
        {
            var actor=crowd[i];
            if(!actor||!actor.gameObject.activeInHierarchy||fallBegan[i]>=0)continue;
            int slot=civilianEscorts.Count;
            var route=BuildWithdrawalRoute(actor.transform.position,0);
            // Keep the guests behind the ten fighters, out of the narrow
            // forest entrance used by the three leaders and Pihu's event.
            float x=-10.2f+(slot%7)*3.4f,z=65.8f+(slot/7)*2.1f;
            route[route.Length-2]=new Vector3(x,0,64.8f);
            route[route.Length-1]=new Vector3(x,0,z);
            civilianEscorts.Add(new CivilianEscort{actor=actor,route=route,home=actor.transform.position,rotation=actor.transform.rotation});
            actor.SetStartled(0);actor.SetAfraid(false);actor.SetWaiting(false);
            actor.SetCowering(false);
            actor.FollowingWithdrawal=true;
        }
    }
    void UpdateCivilianWithdrawal()
    {
        for(int escortIndex=0;escortIndex<civilianEscorts.Count;escortIndex++)
        {
            var escort=civilianEscorts[escortIndex];
            var actor=escort.actor;
            if(!actor||!actor.gameObject.activeInHierarchy)continue;
            if(GatherBesideGate(actor,escort.motor,escortIndex,true,ref escort.gathering))continue;
            if(escort.leg>=escort.route.Length){actor.Face(withdrawalPlayer.position,65);continue;}
            Vector3 current=actor.transform.position,target=escort.route[escort.leg];
            if(escort.leg<escort.route.Length-1)
            {
                var corner=OpenWithdrawalCorner(target,escort.route[escort.leg+1],actor);
                if((corner-target).sqrMagnitude>.001f){target=escort.route[escort.leg]=corner;escort.motor.Reset();}
            }
            bool merge=Horizontal(target,new Vector3(17,0,34))<.01f;
            float radius=escort.leg==escort.route.Length-1?.25f:merge?1.2f:.5f;
            // Once the next segment is reachable, turn through the corner
            // instead of competing with another walker for its exact centre.
            // A capsule cast keeps the building/fence corner solid; final
            // assembly positions still require their original close arrival.
            bool canTurn=escort.leg<escort.route.Length-1&&Horizontal(current,target)<1.2f&&
                Chapter3PathMotor.Clear(current,escort.route[escort.leg+1]);
            if(Horizontal(current,target)<radius||canTurn||(merge&&current.z>=34&&current.x>=15&&current.x<=19.5f))
            {
                escort.leg++;
                if(escort.leg>=escort.route.Length)actor.FollowingWithdrawal=false;
                continue;
            }
            if((target.x>=17||target.z>=44)&&!CanFollowWithdrawal(current,ref target,5.8f))continue;
            escort.motor.Move(actor,target,Chapter3Player.WalkSpeed);
        }
    }
    void CancelCivilianWithdrawal(bool restore)
    {
        foreach(var escort in civilianEscorts)if(escort.actor)
        {
            escort.actor.FollowingWithdrawal=false;
            if(restore)
            {
                escort.actor.ResetPose();escort.actor.SetAnimated(true);
                escort.actor.transform.SetPositionAndRotation(escort.home,escort.rotation);
            }
        }
        civilianEscorts.Clear();
    }
}
