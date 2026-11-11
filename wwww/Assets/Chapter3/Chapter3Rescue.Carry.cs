using System.Collections;
using UnityEngine;

public sealed partial class Chapter3Rescue
{
    public bool CarryTransition {get;private set;}
    public Vector3 CarryFacing=>carryForward;
    Vector3 carryForward=Vector3.forward;
    static Vector3 CarryPosition(Vector3 carrier,Vector3 forward)
    {var point=carrier-forward*.58f;point.y=0;return point;}

    IEnumerator LiftOntoBack(Vector3 target)
    {
        CarryTransition=true;player.canMove=false;
        yield return TransferCarry(target,0,1,1.15f);
        CarryTransition=false;player.canMove=true;
        Pose.SetCarried(1,player.transform,carryForward);
    }
    bool TryPutDownPosition(out Vector3 target)
    {
        Vector3 right=Vector3.Cross(Vector3.up,carryForward),start=school.pihu.transform.position;
        foreach(var offset in new[]{right*1.2f+carryForward*.35f,-right*1.2f+carryForward*.35f,right*.95f-carryForward*.45f,-right*.95f-carryForward*.45f,-carryForward*1.3f})
        {
            target=player.transform.position+offset;target.y=0;
            if(Chapter3PathMotor.Clear(start,target)&&Chapter3PathMotor.Clear(target,target))return true;
        }
        target=start;return false;
    }
    IEnumerator PutDownCarried(Vector3 target)
    {
        CarryTransition=true;player.canMove=false;FollowGuide.Hide();
        yield return TransferCarry(target,1,0,1.1f);
        Pose.SetCarried(0,player.transform,carryForward);
        CurrentStage=Stage.AwaitingLift;LiftProgress=0;CarryTransition=false;
        combat.weapons.gameObject.SetActive(true);player.speed=Chapter3Player.WalkSpeed;player.canMove=true;
        Notice(school.pihu.DisplayName,"我在這裡等你……",2.5f);
    }
    IEnumerator TransferCarry(Vector3 target,float from,float to,float seconds)
    {
        Vector3 start=school.pihu.transform.position;
        // A continuous transfer of the original actor, not a second avatar
        // attached to the camera. No view pitch is inherited by the carried body.
        for(float time=0;time<seconds;time+=Time.deltaTime)
        {
            float blend=Mathf.SmoothStep(0,1,time/seconds);
            school.pihu.transform.position=Vector3.Lerp(start,target,blend);
            Pose.SetCarried(Mathf.Lerp(from,to,blend),player.transform,carryForward);
            yield return null;
        }
        school.pihu.transform.position=target;Pose.SetCarried(to,player.transform,carryForward);
    }
}
