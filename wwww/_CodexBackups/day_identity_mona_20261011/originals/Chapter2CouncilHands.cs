using UnityEngine;

// Authored palm axes work even when the imported FBX mesh is not CPU-readable in Play Mode.
[DefaultExecutionOrder(4325)]
public sealed class Chapter2CouncilHands : MonoBehaviour
{
    public bool alignWrists;
    public Quaternion leftPalmBasis=Quaternion.identity,rightPalmBasis=Quaternion.identity;
    public SkinnedMeshRenderer fistRenderer;
    public Mesh fistMesh;
    public int fistShape=-1;
    public float FistWeight {get;set;}
    Chapter2Actor actor;
    Transform leftElbow,rightElbow;
    Mesh original;
    void Start()
    {
        actor=GetComponent<Chapter2Actor>();
        var animator=actor.Rig.animator;
        leftElbow=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        rightElbow=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
        if(fistRenderer&&fistMesh){original=fistRenderer.sharedMesh;fistRenderer.sharedMesh=fistMesh;}
    }
    void LateUpdate()
    {
        if(!actor||!actor.Rig)return;
        if(alignWrists)
        {
            Vector3 right=Vector3.Cross(Vector3.up,actor.Rig.Forward);
            Align(actor.Rig.LeftHand,leftElbow,leftPalmBasis,right);
            Align(actor.Rig.RightHand,rightElbow,rightPalmBasis,Vector3.Slerp(-right,Vector3.up,actor.speaking?.55f:0));
        }
        if(fistRenderer&&fistShape>=0)fistRenderer.SetBlendShapeWeight(fistShape,100*Mathf.Clamp01(FistWeight));
    }
    static void Align(Transform hand,Transform elbow,Quaternion basis,Vector3 normal)
    {
        if(!hand||!elbow)return;
        Vector3 along=(hand.position-elbow.position).normalized;
        Vector3 palm=Vector3.ProjectOnPlane(normal,along).normalized;
        if(palm.sqrMagnitude>.001f)hand.rotation=Quaternion.LookRotation(along,palm)*Quaternion.Inverse(basis);
    }
    void OnDestroy(){if(original&&fistRenderer)fistRenderer.sharedMesh=original;}
}
