using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Chapter3FollowInputVerify
{
    static IEnumerator NavigationChecks()
    {
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();a.Panic();yield return Frames(2);a.BeginWithdrawal(f.player.transform);a.ReleaseWithdrawalAtAssembly();
        f.player.Warp(new Vector3(0,.06f,78),new Vector3(0,1,72));f.ui.Say("",null);f.ui.fade.color=Color.clear;
        var actor=a.Ambusher(5).GetComponent<Chapter3SchoolCrowdActor>();actor.transform.position=new Vector3(12.23f,0,37.83f);actor.FollowingWithdrawal=true;
        var motor=((Chapter3PathMotor[])Field(a,"withdrawalMotors"))[5];var target=new Vector3(17.62f,0,34.51f);
        ((Vector3[][])Field(a,"withdrawalRoutes"))[5]=new[]{target};
        Physics.SyncTransforms();motor.Move(actor,target,Chapter3Player.WalkSpeed);
        report.notes.Add("actor active="+actor.isActiveAndEnabled+" fallen="+actor.IsFallen+" following="+actor.FollowingWithdrawal+" planner direct="+Call(motor,"ClearForActor",actor.transform.position,target)+" body direct="+Call(actor,"Clear",target));
        foreach(var d in new[]{Vector3.back,Vector3.forward,Vector3.left,Vector3.right})
        {
            var start=actor.transform.position;var next=start+d*.1f;
            report.notes.Add("direction "+d+" planner="+Call(motor,"ClearForActor",start,next)+" body="+Call(actor,"Clear",next));
            foreach(var h in Physics.CapsuleCastAll(start+Vector3.up*.35f,start+Vector3.up*1.25f,.23f,d,.1f,~0,QueryTriggerInteraction.Ignore))
                if(!h.transform.IsChildOf(actor.transform))report.notes.Add("  sweep "+h.collider.name+" distance="+h.distance+" bounds="+h.collider.bounds);
        }
        Save();
        float end=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<end&&Vector3.Distance(actor.transform.position,target)>.25f){yield return null;}
        report.notes.Add("corner reproduction position="+actor.transform.position+" goal="+target+" path="+string.Join(";",(List<Vector3>)Field(motor,"path"))+" search="+Field(motor,"searching")+" iterations="+Field(motor,"iterations"));
        foreach(var p in new[]{actor.transform.position,target})
            foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.42f,p+Vector3.up*1.25f,.36f,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(actor.transform))report.notes.Add("overlap at "+p+" collider="+c.name+" pos="+c.transform.position+" bounds="+c.bounds);
        f.player.Warp(actor.transform.position+new Vector3(0,.06f,-4),actor.transform.position+Vector3.up);yield return Capture("corner-diagnostic");
        Check("recorded corner traversable",Vector3.Distance(actor.transform.position,target)<.3f);
        Check("table remains a solid obstacle",!(bool)Call(motor,"ClearForActor",new Vector3(13,0,37),new Vector3(13,0,40)));
    }
}
