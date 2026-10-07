using UnityEngine;

// A two-handed stop gesture owned only by the sacred-tree confrontation.
[DefaultExecutionOrder(4350)]
public sealed class Chapter2BlockingPose : MonoBehaviour
{
    public float Weight { get; private set; }
    Chapter2Actor actor;
    readonly Transform[] arms=new Transform[2],elbows=new Transform[2],hands=new Transform[2];
    readonly Quaternion[] palms={Quaternion.identity,Quaternion.identity};
    float began=-1;
    bool calibrated;

    public void Prepare()
    {
        actor=GetComponent<Chapter2Actor>();var animator=actor.Rig.animator;
        arms[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        arms[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        elbows[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        elbows[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        hands[0]=actor.Rig.LeftHand;hands[1]=actor.Rig.RightHand;
    }

    public void Raise(){began=Time.time;}

    void LateUpdate()
    {
        if(began<0||!actor||actor.fallen)return;
        Weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.65f));
        var rig=actor.Rig;Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        if(!calibrated)
        {
            // The preceding rig layer aligns free fingers along each forearm and
            // palms inward. Recover that local basis without reading model meshes.
            for(int i=0;i<2;i++)
            {
                Vector3 fingers=(hands[i].position-elbows[i].position).normalized;
                Vector3 normal=Vector3.ProjectOnPlane(i==0?right:-right,fingers).normalized;
                palms[i]=Quaternion.Inverse(hands[i].rotation)*Quaternion.LookRotation(fingers,normal);
            }
            calibrated=true;
        }
        for(int i=0;i<2;i++)
        {
            if(!arms[i]||!elbows[i]||!hands[i])continue;
            Vector3 side=right*(i==0?-1:1);
            // Offset the heights so both palms remain readable in the side-on shot.
            Vector3 wrist=arms[i].position+(forward*(i==0?.22f:.28f)+side*.085f+Vector3.up*(i==0?.18f:.045f))*rig.Height;
            Chapter1IncidentRig.Solve(arms[i],elbows[i],hands[i],Vector3.Lerp(hands[i].position,wrist,Weight),side-Vector3.up*.6f);
            // Fingers upward and open palms toward the officer communicate a clear stop.
            Quaternion rotation=Quaternion.LookRotation((Vector3.up+forward*.15f).normalized,forward)*Quaternion.Inverse(palms[i]);
            hands[i].rotation=Quaternion.Slerp(hands[i].rotation,rotation,Weight);
        }
    }
}
