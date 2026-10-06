using System.Collections;
using UnityEngine;

public sealed partial class Chapter2Controller
{
    public bool Introducing { get; private set; }
    public string CameraBeat { get; private set; } = "gameplay";
    public Vector3 TreeFallDirection { get; private set; }
    public float TreeFallProgress { get; private set; }

    IEnumerator FallingTreeWarning()
    {
        // Retain the player's actual position when choosing the fall direction.
        TreeFallDirection=Vector3.ProjectOnPlane(player.transform.position-sacredTree.position,Vector3.up).normalized;
        if(TreeFallDirection.sqrMagnitude<.01f)TreeFallDirection=Vector3.back;
        foreach(var worker in workers)worker.gameObject.AddComponent<Chapter2StartleReaction>().Prepare();
        yield return CameraShot(new Vector3(0,1.95f,3.3f),new Vector3(0,1.05f,9.5f),.7f);
        CameraBeat="tree-warning";
        ui.Line("族人","退後……它要倒下了。");
        for(int i=0;i<workers.Length;i++)workers[i].GetComponent<Chapter2StartleReaction>().Begin(sacredTree,i);
        yield return new WaitForSeconds(2.25f);
        // Show the tree coming into the foreground while retaining room beside the trunk.
        yield return CameraShot(new Vector3(4.6f,2.6f,2.8f),sacredTree.position+Vector3.up*3.2f,.7f);
        CameraBeat="tree-falling";
    }

    IEnumerator IntroduceForest()
    {
        Introducing=true;
        ui.objective.text="聆聽族人介紹聖地";ui.hint.text="";
        routeGuide.GuidanceEnabled=false;
        var guide=workers[0];
        guide.Face(player.transform.position);
        yield return CameraShot(player.view.transform.position,guide.transform.position+Vector3.up*.9f,.65f);
        CameraBeat="introduction";
        yield return Say(guide,"族人","前面就是西仔希克。這片森林，守護著我們的生活。",6);
        guide.Rig.pointTarget=sacredTree;guide.Rig.pointProgress=1;
        Vector3 from=guide.Rig.Forward;
        Vector3 toward=Vector3.ProjectOnPlane(sacredTree.position-guide.transform.position,Vector3.up).normalized;
        for(float t=0;t<1;t+=Time.deltaTime/1.2f)
        {guide.Face(guide.transform.position+Vector3.Slerp(from,toward,Mathf.SmoothStep(0,1,t)));yield return null;}
        guide.Face(sacredTree.position);
        yield return Say(guide,"族人","沿著這條小徑，跟我來。不要離隊太遠。",3);
        guide.Rig.pointTarget=null;guide.Rig.pointProgress=0;
        ui.Line("","");
        // Return the camera to the player's metre-scale origin before handing back input.
        player.Warp(player.transform.position,guide.transform.position);
        player.canMove=true;player.canLook=true;
        ui.hint.text="WASD 移動・按住滑鼠右鍵環顧  |  VR 左搖桿移動、右搖桿轉向";
        routeGuide.GuidanceEnabled=true;Introducing=false;CameraBeat="gameplay";
    }

    IEnumerator FrameSpeaker(Chapter2Actor actor)
    {
        Vector3 at=actor.transform.position;
        Vector3 position=at+new Vector3(actor.police?.45f:-.35f,1.55f,-3.9f);
        actor.Face(position);
        yield return CameraShot(position,at+Vector3.up*.9f,.65f);
        CameraBeat=actor.police?"police-speaking":"villager-speaking";
    }

    IEnumerator FrameConfrontation()
    {
        yield return CameraShot(new Vector3(.15f,1.6f,3.7f),new Vector3(.15f,.85f,8.7f),.75f);
        CameraBeat="confrontation";
        workers[0].Face(officer.transform.position);officer.Face(workers[0].transform.position);
    }

    IEnumerator CameraShot(Vector3 position,Vector3 target,float seconds)
    {
        player.canMove=false;player.canLook=false;
        if(player.IsVR)
        {
            // Move the XR origin under a short fade; tracked head rotation stays in control.
            yield return Fade(1,.18f);
            player.Warp(position-Vector3.up*1.65f,target);
            yield return Fade(0,.25f);
            yield break;
        }
        var view=player.view.transform;
        Vector3 start=view.position;Quaternion rotation=view.rotation;
        Quaternion end=Quaternion.LookRotation(target-position);
        for(float t=0;t<1;t+=Time.deltaTime/seconds)
        {float k=Mathf.SmoothStep(0,1,t);view.SetPositionAndRotation(Vector3.Lerp(start,position,k),Quaternion.Slerp(rotation,end,k));yield return null;}
        view.SetPositionAndRotation(position,end);
    }

    IEnumerator RestoreChoppingView()
    {
        // Put the body at the actual interaction point, not merely a detached camera.
        Vector3 point=treeApproach.position+Vector3.up*.08f;
        if(player.IsVR)yield return Fade(1,.18f);
        player.Warp(point,sacredTree.position);
        if(TryGetChopContact(out RaycastHit hit))player.FocusOn(hit.point);
        if(player.IsVR)yield return Fade(0,.25f);
        player.canLook=true;CameraBeat="chopping";
    }
}
