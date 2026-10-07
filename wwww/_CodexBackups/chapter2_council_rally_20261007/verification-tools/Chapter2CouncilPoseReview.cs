using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2CouncilPoseReview
{
    static Chapter2Controller c;
    static double started;
    static int step;
    const string Dir="_CodexBackups/chapter2_council_rally_20261007/";
    static Chapter2CouncilPoseReview(){EditorApplication.update+=Tick;}
    public static void Run(){SessionState.SetBool("CouncilPoseReview",true);c=null;step=0;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!Application.isPlaying||!SessionState.GetBool("CouncilPoseReview",false))return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            c.StopAllCoroutines();c.saveResult=false;c.SetNight();c.ui.fade.color=Color.clear;c.ui.Line("","");
            var seated=c.leaders[2];
            var chair=c.nightGroup.GetComponentsInChildren<Transform>().Where(x=>x.name=="Council seat").OrderBy(x=>Vector3.Distance(x.position,seated.transform.position)).First();
            chair.position-=seated.facing.normalized*.25f;chair.localScale=new Vector3(.45f,.22f,.45f);
            c.player.canMove=c.player.canLook=false;started=EditorApplication.timeSinceStartup;step=0;return;
        }
        var actor=c.leaders[2];if(!actor.Rig||!actor.Rig.Head)return;
        Vector3 p=actor.transform.position;
        Vector3 inward=Vector3.ProjectOnPlane(c.campfire.position-p,Vector3.up).normalized;
        Vector3 camera=p+inward*2.2f+Vector3.up*1.35f;
        actor.Face(camera);c.player.view.transform.SetPositionAndRotation(camera,Quaternion.LookRotation(p+Vector3.up-camera));c.player.view.fieldOfView=50;
        double t=EditorApplication.timeSinceStartup-started;
        if(step==0&&t>2)
        {
            Chapter2WorkProbe.Shot("pose-idle");
            var animator=actor.GetComponentInChildren<Animator>();var report=new StringBuilder();
            report.AppendLine("human="+animator.isHuman+" head="+actor.Rig.Head.name);
            foreach(var bone in actor.GetComponentsInChildren<Transform>())if(bone.name.ToLowerInvariant().Contains("head")||bone.name.ToLowerInvariant().Contains("neck"))
                report.AppendLine(bone.name+" local="+bone.localPosition+" rotation="+bone.localEulerAngles+" world="+bone.position);
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<skin.bones.Length;i++)
                if(skin.bones[i].name.ToLowerInvariant().Contains("head")||skin.bones[i].name.ToLowerInvariant().Contains("neck"))
                    report.AppendLine(skin.bones[i].name+" bind="+(skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse)+" actual="+skin.bones[i].localToWorldMatrix);
            File.WriteAllText(Dir+"pose-bones.txt",report.ToString());actor.speaking=true;step++;
        }
        if(step==1&&t>4)
        {
            Chapter2WorkProbe.Shot("pose-speaking");actor.enabled=false;actor.Rig.enabled=false;
            var matrices=new System.Collections.Generic.Dictionary<Transform,Matrix4x4>();
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<skin.bones.Length;i++)matrices[skin.bones[i]]=skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse;
            foreach(var bone in actor.GetComponentsInChildren<Transform>())if(matrices.TryGetValue(bone,out var matrix))
            {
                var local=bone.parent.worldToLocalMatrix*matrix;
                bone.localPosition=local.GetColumn(3);bone.localRotation=local.rotation;bone.localScale=local.lossyScale;
            }
            step++;
        }
        if(step==2&&t>6){Chapter2WorkProbe.Shot("pose-bind-restored");step++;SessionState.EraseBool("CouncilPoseReview");EditorApplication.isPlaying=false;}
    }
}
