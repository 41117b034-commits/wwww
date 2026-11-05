using UnityEngine;

// Apply after the shared actor pose and before its rifle and corrected hand
// grips. Keep both soles planted while the first leg hit bends the knees.
[DefaultExecutionOrder(4250)]
public sealed class Chapter3EnemyWoundPose : MonoBehaviour
{
    public float Weight { get; private set; }
    Chapter3Enemy enemy;
    Chapter1IncidentRig rig;
    Transform bodyRoot,spine;
    readonly Transform[] thighs=new Transform[2],knees=new Transform[2],feet=new Transform[2];
    readonly Vector3[] anchors=new Vector3[2];
    readonly Quaternion[] soles=new Quaternion[2];
    bool prepared;
    bool Prepare()
    {
        if(prepared)return enemy&&rig;
        enemy=GetComponent<Chapter3Enemy>();
        if(!enemy||!enemy.actor||!enemy.actor.Rig)return false;
        rig=enemy.actor.Rig;
        var animator=rig.animator;
        if(!animator||!rig.Hips||!rig.Head)return false;
        bodyRoot=rig.Hips;
        while(bodyRoot&&!rig.Head.IsChildOf(bodyRoot)&&bodyRoot.parent&&bodyRoot.parent!=enemy.transform&&bodyRoot.parent!=animator.transform)
            bodyRoot=bodyRoot.parent;
        if(!bodyRoot||!rig.Head.IsChildOf(bodyRoot))return false;
        spine=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Spine,"Spine","Spine1","Waist");
        thighs[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        thighs[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        knees[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        knees[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        feet[0]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        feet[1]=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        for(int i=0;i<2;i++)if(!thighs[i]||!knees[i]||!feet[i])return false;
        prepared=true;return true;
    }
    void LateUpdate()
    {
        if(!Prepare()||!enemy.LegWounded)return;
        if(enemy.Defeated){Weight=0;return;}
        Weight=Mathf.MoveTowards(Weight,1,Time.deltaTime*3.5f);
        for(int i=0;i<2;i++){anchors[i]=feet[i].position;soles[i]=feet[i].rotation;}
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        bodyRoot.position-=(Vector3.up*rig.Height*.24f+forward*rig.Height*.035f)*Weight;
        if(spine)spine.rotation=Quaternion.AngleAxis(12*Weight,right)*spine.rotation;
        for(int i=0;i<2;i++)
        {
            Chapter1IncidentRig.Solve(thighs[i],knees[i],feet[i],anchors[i],forward+right*(i==0?-.18f:.18f));
            feet[i].rotation=soles[i];
        }
    }
}
