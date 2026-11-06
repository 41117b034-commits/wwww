using UnityEngine;

// The imported actor root is not its body's centre. Fit to the actual skeleton
// instead of a standing root capsule, and keep the zones with the crouched pose.
[DefaultExecutionOrder(4800)]
public sealed class Chapter3EnemyHitZone : MonoBehaviour
{
    public enum BodyRegion { Head, Body, Leg }
    public Chapter3Enemy Enemy { get; private set; }
    public BodyRegion Region { get; private set; }
    public Vector3 Centre => transform.position;
    Transform start,end;
    Vector3 startOffset,endOffset;
    CapsuleCollider capsule;
    BoxCollider torso;
    float radius;

    public static void Install(Chapter3Enemy enemy)
    {
        if(!enemy||!enemy.actor||!enemy.actor.Rig||!enemy.actor.Rig.animator)return;
        if(enemy.GetComponentInChildren<Chapter3EnemyHitZone>(true))return;
        var rig=enemy.actor.Rig;
        var animator=rig.animator;
        Transform neck=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.Neck,"Neck","NeckTwist01","NeckTwist02");
        var thighs=new[]{
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg"),
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg")};
        var knees=new[]{
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg"),
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg")};
        var feet=new[]{
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot"),
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot")};
        var arms=new[]{
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm"),
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm")};
        var elbows=new[]{
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm"),
            Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm")};
        if(!rig.Hips||!rig.Head||!neck)return;
        for(int i=0;i<2;i++)if(!thighs[i]||!knees[i]||!feet[i]||!arms[i]||!elbows[i])return;
        float h=rig.Height;
        // Disable only the old generic actor capsule. Scenery, friendlies and
        // civilians retain their original blocking/protection colliders.
        foreach(var old in enemy.GetComponents<Collider>())old.enabled=false;
        Add(enemy,"Head",BodyRegion.Head,neck,rig.Head,Vector3.zero,Vector3.up*h*.045f,h*.085f);
        Add(enemy,"Torso",BodyRegion.Body,rig.Hips,neck,Vector3.down*h*.04f,Vector3.down*h*.025f,h*.16f,true);
        for(int i=0;i<2;i++)
        {
            string side=i==0?"Left":"Right";
            // Stop the thigh's rounded cap at the hip joint. Extending its cap
            // above that joint intercepted visible waist hits as leg damage.
            Vector3 thighInset=(knees[i].position-thighs[i].position).normalized*h*.073f;
            Add(enemy,side+" thigh",BodyRegion.Leg,thighs[i],knees[i],thighInset,Vector3.zero,h*.073f);
            Add(enemy,side+" shin",BodyRegion.Leg,knees[i],feet[i],Vector3.zero,Vector3.zero,h*.061f);
            Add(enemy,side+" foot",BodyRegion.Leg,feet[i],feet[i],Vector3.zero,rig.Forward*h*.07f,h*.055f);
            Add(enemy,side+" upper arm",BodyRegion.Body,arms[i],elbows[i],Vector3.zero,Vector3.zero,h*.06f);
            Add(enemy,side+" forearm",BodyRegion.Body,elbows[i],i==0?rig.LeftHand:rig.RightHand,Vector3.zero,Vector3.zero,h*.05f);
        }
    }
    static void Add(Chapter3Enemy enemy,string label,BodyRegion region,Transform a,Transform b,Vector3 offsetA,Vector3 offsetB,float radius,bool box=false)
    {
        if(!a||!b)return;
        var go=new GameObject("Hit zone · "+label);
        go.layer=enemy.gameObject.layer;
        go.transform.SetParent(enemy.transform,false);
        var zone=go.AddComponent<Chapter3EnemyHitZone>();
        zone.Enemy=enemy;zone.Region=region;zone.start=a;zone.end=b;
        // Offsets are expressed in each bone's current frame, so a later turn
        // or crouch follows the visible foot/head rather than a fixed world axis.
        zone.startOffset=a.InverseTransformVector(offsetA);zone.endOffset=b.InverseTransformVector(offsetB);
        zone.radius=radius;
        if(box)zone.torso=go.AddComponent<BoxCollider>();
        else {zone.capsule=go.AddComponent<CapsuleCollider>();zone.capsule.direction=1;zone.capsule.isTrigger=false;}
        zone.Align();
    }
    void LateUpdate(){Align();}
    void Align()
    {
        if((!capsule&&!torso)||!Enemy||Enemy.Defeated||!start||!end)return;
        Vector3 a=start.TransformPoint(startOffset),b=end.TransformPoint(endOffset),delta=b-a;
        Vector3 scale=transform.parent.lossyScale;
        transform.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),
            1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
        Quaternion rotation=delta.sqrMagnitude>.000001f?Quaternion.FromToRotation(Vector3.up,delta):Quaternion.identity;
        if(torso&&Enemy.actor.Rig){var forward=Vector3.ProjectOnPlane(Enemy.actor.Rig.Forward,delta.normalized);if(forward.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(forward,delta.normalized);}
        transform.SetPositionAndRotation((a+b)*.5f,rotation);
        // The rounded end of a capsule left the visible waist corners uncovered.
        // A shallow torso box reaches the belt while leaving thighs as leg zones.
        if(torso){torso.center=Vector3.zero;torso.size=new Vector3(radius*2,Mathf.Max(.01f,delta.magnitude),radius*1.55f);return;}
        capsule.center=Vector3.zero;capsule.radius=radius;
        capsule.height=Mathf.Max(radius*2,delta.magnitude+radius*2);
    }
}
