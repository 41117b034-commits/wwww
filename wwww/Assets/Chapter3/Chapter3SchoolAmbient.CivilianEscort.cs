using System.Collections.Generic;
using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    // User-requested fictional staging: two surviving adult men pursue the
    // retreating fighters briefly. They are never members of the player's
    // party, and this is not a claim about named historical teachers.
    static readonly int[] japanesePursuerSlots={0,6};
    static readonly Vector3[] defenderCover={new Vector3(-5,0,33),new Vector3(13,0,37)};
    sealed class JapanesePursuer
    {
        public int guest,target,leg;
        public readonly Chapter3PathMotor motor=new Chapter3PathMotor();
        public Vector3[] route;
        public Vector3 home,lastPosition,chasePosition,lastTargetPosition,targetForward;
        public Quaternion rotation;
        public float distance,nextChaseTarget;
        public bool completed,engaged;
    }
    readonly List<JapanesePursuer> japanesePursuers=new List<JapanesePursuer>();
    // Compatibility: Japanese bystanders no longer join withdrawal formation.
    public int CivilianFollowerCount=>0;
    public int CivilianArrivedCount=>EscapedSpectators;
    public int JapanesePursuerCount=>japanesePursuers.Count;
    public int PursuingSpectators=>CountPhase(CrowdPhase.Pursuing);
    public int PursuitCompletedCount
    {get{int count=0;foreach(var pursuer in japanesePursuers)if(pursuer.completed)count++;return count;}}
    public Transform JapanesePursuerActor(int index)=>index>=0&&index<japanesePursuers.Count?crowd[japanesePursuers[index].guest].transform:null;
    public Transform JapanesePursuitTarget(int index)=>index>=0&&index<japanesePursuers.Count?assailants[japanesePursuers[index].target].transform:null;
    public float JapanesePursuitDistance(int index)=>index>=0&&index<japanesePursuers.Count?japanesePursuers[index].distance:0;
    bool IsJapanesePursuer(int index)=>System.Array.IndexOf(japanesePursuerSlots,index)>=0;
    void UpdateJapaneseDefender(int index)
    {
        if(Withdrawing)return; // The pursuit motor owns these two living adults.
        var actor=crowd[index];int slot=System.Array.IndexOf(japanesePursuerSlots,index);
        actor.SetAfraid(true);actor.SetStartled(0);actor.SetCowering(false);actor.SetWaiting(false);
        if(Horizontal(actor.transform.position,defenderCover[slot])>.4f)
        {
            crowdPhases[index]=CrowdPhase.Fleeing;
            civilianPanicMotors[index].Patrol(actor,defenderCover[slot],2.9f,.3f);
        }
        else
        {
            crowdPhases[index]=CrowdPhase.Sheltered;
            actor.SetWaiting(true);actor.Face(assailants[slot==0?9:6].transform.position,120);
        }
    }
    void BeginCivilianWithdrawal()
    {
        CancelCivilianWithdrawal(false);
        for(int slot=0;slot<japanesePursuerSlots.Length;slot++)
        {
            int i=japanesePursuerSlots[slot];var actor=crowd[i];
            if(!actor||!actor.gameObject.activeInHierarchy||fallBegan[i]>=0)continue;
            var fullRoute=BuildWithdrawalRoute(actor.transform.position,0);
            // Follow the actual open school corners; stop the pursuit at the
            // rear clearing instead of entering the forest reception event.
            var route=new List<Vector3>();
            foreach(var point in fullRoute)
            {route.Add(point);if(Horizontal(point,new Vector3(12,0,70))<.01f)break;}
            // Leave the (22,66) -> (12,70) retreat lane free after stopping.
            // The first adult walks to the open south side of that lane.
            route[route.Count-1]=slot==0?new Vector3(16,0,65):new Vector3(18.2f,0,69.7f);
            japanesePursuers.Add(new JapanesePursuer{guest=i,target=slot==0?9:6,route=route.ToArray(),
                home=actor.transform.position,lastPosition=actor.transform.position,rotation=actor.transform.rotation,
                lastTargetPosition=assailants[slot==0?9:6].transform.position});
            actor.SetStartled(0);actor.SetAfraid(false);actor.SetWaiting(false);actor.SetCowering(false);
            actor.FollowingWithdrawal=false;crowdPhases[i]=CrowdPhase.Pursuing;
        }
    }
    void UpdateCivilianWithdrawal()
    {
        foreach(var pursuer in japanesePursuers)
        {
            var actor=crowd[pursuer.guest];var targetActor=assailants[pursuer.target];
            if(!actor||!targetActor||!actor.gameObject.activeInHierarchy)continue;
            Vector3 current=actor.transform.position,targetPosition=targetActor.transform.position;
            Vector3 targetStep=Vector3.ProjectOnPlane(targetPosition-pursuer.lastTargetPosition,Vector3.up);
            pursuer.lastTargetPosition=targetPosition;
            if(targetStep.sqrMagnitude>.000001f)pursuer.targetForward=targetStep.normalized;
            pursuer.distance+=Horizontal(current,pursuer.lastPosition);pursuer.lastPosition=current;
            actor.SetCowering(false);actor.SetWaiting(false);
            if(pursuer.completed)
            {actor.SetStrike(0);actor.SetAfraid(true);actor.Face(targetPosition,85);continue;}
            float distance=Horizontal(current,targetPosition);
            if(!pursuer.engaged)
            {
                // Both adults can initially stand ahead of their opponents.
                // Let that actual fighter pass within sight, then chase from
                // behind instead of running the exit route ahead of him.
                bool passing=targetStep.sqrMagnitude>.000001f&&distance<8&&
                    Vector3.Dot(Vector3.ProjectOnPlane(targetPosition-current,Vector3.up),pursuer.targetForward)>1;
                if(!passing)
                {actor.SetStrike(0);actor.SetWaiting(true);actor.Face(targetPosition,160);continue;}
                pursuer.engaged=true;pursuer.motor.Reset();
            }
            // A direct pursuit may already have physically crossed an earlier
            // approach point. Do not turn south and retrace it afterwards.
            while(pursuer.leg<pursuer.route.Length-1)
            {
                Vector3 passed=pursuer.route[pursuer.leg];
                // Entering the east aisle north of the last field corner is
                // sufficient. Waiting for z > 41 sent a pursuer at z = 40.8
                // back west when his target crossed into the gate segment.
                bool beyondField=current.x>=16&&current.z>36&&passed.z<=35;
                bool beyondGate=current.x>=23&&current.z>47&&passed.z<=45.5f;
                bool alongRearAisle=current.x>=23&&current.z>passed.z+.7f&&passed.x>=23&&passed.z<=61;
                if(!beyondField&&!beyondGate&&!alongRearAisle)break;
                pursuer.leg++;pursuer.motor.Reset();
            }
            if(pursuer.leg>=pursuer.route.Length)
            {
                pursuer.completed=true;crowdPhases[pursuer.guest]=CrowdPhase.Repelled;
                actor.SetStrike(0);actor.SetAfraid(true);continue;
            }
            // In an open part of the same aisle, close on the actual moving
            // fighter. The offset leaves room for a failed, non-damaging swing.
            bool sameAisle=(current.z<41&&targetPosition.z<41)||
                (current.x>22.5f&&targetPosition.x>22.5f&&current.z>46&&targetPosition.z>46);
            if(sameAisle&&distance<8&&targetPosition.z<69)
            {
                if(distance>1.65f)
                {
                    if(Time.time>=pursuer.nextChaseTarget)
                    {
                        Vector3 behind=-pursuer.targetForward;
                        if(behind.sqrMagnitude<.1f)behind=Vector3.ProjectOnPlane(current-targetPosition,Vector3.up).normalized;
                        if(behind.sqrMagnitude<.1f)behind=Vector3.back;
                        pursuer.chasePosition=targetPosition+behind*1.35f;pursuer.nextChaseTarget=Time.time+.65f;
                    }
                    pursuer.motor.Patrol(actor,pursuer.chasePosition,3.15f,.2f);actor.SetStrike(0);
                }
                else
                {
                    actor.Face(targetPosition,160);
                    float swing=Mathf.Repeat(Time.time+pursuer.guest*.3f,2.1f);
                    actor.SetStrike(swing<.8f?Mathf.Clamp(swing/.8f,.01f,.99f):0);
                }
                continue;
            }
            actor.SetStrike(0);
            // Consume already passed corners after a direct pursuit segment.
            // Each advancement still requires physical proximity or a clear
            // turn; nobody is placed at the next route point.
            Vector3 target=pursuer.route[pursuer.leg];
            if(pursuer.leg<pursuer.route.Length-1)
            {
                Vector3 corner=OpenWithdrawalCorner(target,pursuer.route[pursuer.leg+1],actor);
                if((corner-target).sqrMagnitude>.001f){target=pursuer.route[pursuer.leg]=corner;pursuer.motor.Reset();}
            }
            bool canTurn=pursuer.leg<pursuer.route.Length-1&&Horizontal(current,target)<1.2f&&Chapter3PathMotor.Clear(current,pursuer.route[pursuer.leg+1]);
            if(Horizontal(current,target)<.55f||canTurn)
            {pursuer.leg++;pursuer.motor.Reset();continue;}
            pursuer.motor.Patrol(actor,target,3.15f,.35f);
        }
    }
    void CancelCivilianWithdrawal(bool restore)
    {
        foreach(var pursuer in japanesePursuers)if(crowd[pursuer.guest])
        {
            var actor=crowd[pursuer.guest];actor.FollowingWithdrawal=false;actor.SetStrike(0);
            if(restore)
            {
                actor.ResetPose();actor.SetAnimated(true);
                actor.transform.SetPositionAndRotation(pursuer.home,pursuer.rotation);
            }
        }
        japanesePursuers.Clear();
    }
}
