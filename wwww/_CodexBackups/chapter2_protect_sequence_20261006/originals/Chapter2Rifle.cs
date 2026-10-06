using UnityEngine;

// Chapter 2 only: shoulder stock, two hand grips, barrel aim and a single report.
[DefaultExecutionOrder(4300)]
public sealed class Chapter2Rifle : MonoBehaviour
{
    public Transform weapon, muzzle;
    public GameObject muzzleFlash;
    public int modelVersion;
    public Chapter2Actor target;
    [Range(0,1)] public float aim;
    public int Shots { get; private set; }
    public float RightGripError { get; private set; }
    public float LeftGripError { get; private set; }
    public float AimError { get; private set; }
    Chapter2Actor actor;
    Transform la,le,ra,re;
    Vector3 leftPalm,rightPalm;
    float firedAt=-100;
    bool calibrated;
    void Start()
    {
        actor=GetComponent<Chapter2Actor>();
        var a=GetComponentInChildren<Animator>();
        la=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        le=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        ra=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        re=Chapter1WeddingRigBones.Resolve(a,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
    }
    public void Fire(){Shots++;firedAt=Time.time;}
    void LateUpdate()
    {
        if(!actor||!actor.Rig||!ra||!la||!weapon)return;
        var rig=actor.Rig;
        if(!calibrated)
        {leftPalm=Palm(rig.LeftHand);rightPalm=Palm(rig.RightHand);calibrated=true;}
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        float since=Time.time-firedAt;
        float recoil=since<.28f?Mathf.Sin(Mathf.Clamp01(since/.28f)*Mathf.PI):0;
        Vector3 stock=Vector3.Lerp(rig.Hips.position+right*.18f+Vector3.up*.02f,ra.position-right*.08f-forward*.10f-Vector3.up*.08f,aim);
        Vector3 destination=target?target.transform.position+Vector3.up*1.17f:stock+forward*4;
        Vector3 direction=Vector3.Slerp((forward*.5f+Vector3.up*.85f).normalized,(destination-stock).normalized,aim);
        stock-=direction*(recoil*.055f);
        weapon.SetPositionAndRotation(stock,Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(-recoil*3,0,0));
        Grip(rig.RightHand,ra,re,rightPalm,weapon.TransformPoint(new Vector3(0,-.045f,.24f)),right-Vector3.up*.7f,false);
        Grip(rig.LeftHand,la,le,leftPalm,weapon.TransformPoint(new Vector3(0,-.028f,.50f)),-right-Vector3.up,true);
        RightGripError=Vector3.Distance(rig.RightHand.TransformPoint(rightPalm),weapon.TransformPoint(new Vector3(0,-.045f,.24f)));
        LeftGripError=Vector3.Distance(rig.LeftHand.TransformPoint(leftPalm),weapon.TransformPoint(new Vector3(0,-.028f,.50f)));
        AimError=Vector3.Angle(weapon.forward,destination-muzzle.position);
        if(muzzleFlash)muzzleFlash.SetActive(since>=0&&since<.12f);
    }
    void Grip(Transform hand,Transform upper,Transform elbow,Vector3 palm,Vector3 point,Vector3 bend,bool left)
    {
        Vector3 fingers=left?weapon.right:Vector3.down;
        if(palm.sqrMagnitude>.00001f)hand.rotation=Quaternion.FromToRotation(hand.TransformDirection(palm).normalized,fingers)*hand.rotation;
        Quaternion rotation=hand.rotation;
        Chapter1IncidentRig.Solve(upper,elbow,hand,point-hand.TransformVector(palm),bend);
        hand.rotation=rotation;
    }
    Vector3 Palm(Transform hand)
    {
        Vector3 total=Vector3.zero;int count=0;
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            int slot=System.Array.IndexOf(skin.bones,hand);if(slot<0)continue;
            var mesh=skin.sharedMesh;if(!mesh.isReadable)continue;
            var v=mesh.vertices;var w=mesh.boneWeights;var bind=mesh.bindposes[slot];
            for(int i=0;i<v.Length;i++)if(w[i].boneIndex0==slot&&w[i].weight0>.5f){total+=bind.MultiplyPoint3x4(v[i]);count++;}
        }
        return count>0?total/count*.65f:hand.InverseTransformVector(Vector3.down*.045f);
    }
}
