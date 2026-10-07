using System.Collections;
using UnityEngine;

public sealed partial class Chapter2Controller
{
    public Chapter2Actor CouncilSpeaker { get; private set; }

    IEnumerator FrameCouncilSpeaker(Chapter2Actor actor)
    {
        Vector3 at=actor.transform.position;
        Vector3 inward=Vector3.ProjectOnPlane(campfire.position-at,Vector3.up).normalized;
        Vector3 side=Vector3.Cross(Vector3.up,inward);
        Vector3 position=at+inward*2.35f+side*.3f;
        // The two standing dissenters share a row; view diagonally to keep the other out of frame.
        if(System.Array.IndexOf(conservatives,actor)>=0)
            position=at+new Vector3(1.65f,0,-1.85f);
        position.y=at.y+(actor.seated?1.3f:1.65f);
        Vector3 target=at+Vector3.up*(actor.seated?.9f:1.15f);
        actor.Face(position);
        yield return CouncilCameraShot(position,target,50,.7f);
        CouncilSpeaker=actor;CameraBeat="council-speaker";
    }

    IEnumerator FrameCouncilOverview(float seconds=.8f,bool ending=false)
    {
        CouncilSpeaker=null;ui.Line("","");
        Vector3 position=ending?new Vector3(0,3.65f,-8):meetingSpawn.position+Vector3.up*1.65f;
        yield return CouncilCameraShot(position,campfire.position+Vector3.up*.85f,64,seconds);
        CameraBeat=ending?"council-pullback":"council-overview";
    }

    IEnumerator CouncilCameraShot(Vector3 position,Vector3 target,float fov,float seconds)
    {
        player.canMove=false;player.canLook=false;
        if(player.IsVR){yield return CameraShot(position,target,seconds);yield break;}
        var view=player.view.transform;
        Vector3 start=view.position,center=campfire.position;
        Vector3 from=Vector3.ProjectOnPlane(start-center,Vector3.up);
        Vector3 to=Vector3.ProjectOnPlane(position-center,Vector3.up);
        Quaternion rotation=view.rotation,end=Quaternion.LookRotation(target-position);
        float startFov=player.view.fieldOfView;
        // Follow an arc outside the fire instead of crossing through its flames.
        for(float t=0;t<1;t+=Time.deltaTime/Mathf.Max(.01f,seconds))
        {
            float k=Mathf.SmoothStep(0,1,t);
            Vector3 p=center+Vector3.Slerp(from,to,k);p.y=Mathf.Lerp(start.y,position.y,k);
            view.SetPositionAndRotation(p,Quaternion.Slerp(rotation,end,k));
            player.view.fieldOfView=Mathf.Lerp(startFov,fov,k);
            yield return null;
        }
        view.SetPositionAndRotation(position,end);player.view.fieldOfView=fov;
    }
}
