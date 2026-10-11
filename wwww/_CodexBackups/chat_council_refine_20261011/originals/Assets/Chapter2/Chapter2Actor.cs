using UnityEngine;

[DefaultExecutionOrder(4200)]
public sealed class Chapter2Actor : MonoBehaviour
{
    [Header("人物身分")]
    public string characterName;
    public string romanizedName;
    [TextArea] public string dialogueRole;
    public bool isChild;
    public string DisplayName => string.IsNullOrWhiteSpace(characterName) ? name : characterName;
    public bool police;
    public bool seated;
    public bool fallen;
    public bool speaking;
    [Min(.1f)] public float seatedHipHeight=.54f;
    public Vector3 facing = Vector3.back;
    // Only the roaming guide follows terrain height; authored story actors keep zero.
    public float GroundHeight { get; set; }
    public Chapter1IncidentRig Rig { get; private set; }
    public float FallProgress { get; private set; }
    public float FallenLowestPoint { get; private set; }
    public float SeatWeight { get; private set; }
    float standStarted=-1,standDuration;
    Vector3 standOrigin,standForward;
    float fallStarted;
    Vector3 fallDirection;
    SkinnedMeshRenderer[] skins;
    Mesh fallMesh;
    readonly System.Collections.Generic.List<Vector3> fallVertices=new System.Collections.Generic.List<Vector3>();
    float finalLift=-1;
    Vector3 previous;
    Transform leftThigh,rightThigh,leftKnee,rightKnee,leftFoot,rightFoot;
    void Start()
    {
        var animator = GetComponentInChildren<Animator>();
        Rig = gameObject.AddComponent<Chapter1IncidentRig>();
        Rig.Initialize(animator, police);
        Rig.batonVisible = false;
        Rig.Face(facing); Rig.Ground(0);
        leftThigh=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperLeg,"L_Thigh","LeftUpLeg");
        rightThigh=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperLeg,"R_Thigh","RightUpLeg");
        leftKnee=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerLeg,"L_Calf","LeftLeg");
        rightKnee=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerLeg,"R_Calf","RightLeg");
        leftFoot=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftFoot,"L_Foot","LeftFoot");
        rightFoot=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightFoot,"R_Foot","RightFoot");
        previous = transform.position;
        skins=GetComponentsInChildren<SkinnedMeshRenderer>();
    }
    void Update()
    {
        if (!Rig) return;
        if(standStarted>=0)
        {
            float progress=Mathf.Clamp01((Time.time-standStarted)/standDuration);
            transform.position=standOrigin+standForward*(.46f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.95f,progress)));
        }
        Vector3 delta = Vector3.ProjectOnPlane(transform.position-previous,Vector3.up);
        Rig.walking = standStarted<0 && !fallen && delta.magnitude > 0.0005f;
        if (Rig.walking) Rig.Face(delta);
        Rig.speakingWeight = speaking && !fallen ? 0.65f : 0;
        if (!fallen) Rig.Ground(GroundHeight);
        previous = transform.position;
    }
    void LateUpdate()
    {
        if (!Rig || !Rig.Hips) return;
        SeatWeight=seated?1:0;
        if(standStarted>=0)
        {
            SeatWeight=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-standStarted)/standDuration));
            if(SeatWeight<=0){seated=false;standStarted=-1;}
        }
        if (SeatWeight>0 && leftFoot && rightFoot)
        {
            float footReach=.2f*seatedHipHeight/.54f;
            Vector3 l=leftFoot.position+Rig.Forward*(footReach*SeatWeight),r=rightFoot.position+Rig.Forward*(footReach*SeatWeight);
            // Council stumps are 0.44 m high. Different donor rigs have different
            // pelvis heights; keep the seated pelvis above the seat surface.
            float seatDrop=Mathf.Max(0,Rig.Hips.position.y-seatedHipHeight);
            Rig.Hips.position += Vector3.down * (seatDrop*SeatWeight);
            Chapter1IncidentRig.Solve(leftThigh,leftKnee,leftFoot,l,Rig.Forward);
            Chapter1IncidentRig.Solve(rightThigh,rightKnee,rightFoot,r,Rig.Forward);
        }
        if (fallen) PoseFall();
        if (speaking && Rig.Head) Rig.Head.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time*2.1f)*3, transform.right)*Rig.Head.rotation;
    }
    public void Face(Vector3 point) { facing = point-transform.position; if(Rig) Rig.Face(facing); }
    public void StandFromSeat(float delay=0,float duration=1.25f)
    {
        if(!seated)return;
        standStarted=Time.time+delay;standDuration=Mathf.Max(.1f,duration);
        standOrigin=transform.position;standForward=Rig.Forward;
    }
    public void BeginFall(Vector3 source)
    {
        if(fallen)return;
        fallDirection=Vector3.ProjectOnPlane(transform.position-source,Vector3.up).normalized;
        fallen=true;speaking=false;fallStarted=Time.time;
        Rig.pointTarget=null;Rig.conversationTarget=null;
        fallMesh=new Mesh();
    }
    void PoseFall()
    {
        FallProgress=Mathf.Clamp01((Time.time-fallStarted)/1.9f);
        float drop=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,1,FallProgress));
        Vector3 axis=Vector3.Cross(Vector3.up,fallDirection);
        float flinch=Mathf.Sin(Mathf.Clamp01(FallProgress/.24f)*Mathf.PI)*10;
        Rig.Hips.position+=fallDirection*(.48f*drop)+Vector3.down*(.72f*drop);
        Rig.Hips.rotation=Quaternion.AngleAxis(88*drop+flinch,axis)*Rig.Hips.rotation;
        // Bend knees during the collapse, then keep the whole skin above the floor.
        float bend=Mathf.Sin(drop*Mathf.PI);
        if(leftKnee)leftKnee.rotation=Quaternion.AngleAxis(-8*drop-20*bend,axis)*leftKnee.rotation;
        if(rightKnee)rightKnee.rotation=Quaternion.AngleAxis(-16*drop-25*bend,axis)*rightKnee.rotation;
        if(FallProgress>=1&&finalLift>=0){Rig.Hips.position+=Vector3.up*finalLift;return;}
        if(!fallMesh)fallMesh=new Mesh();
        float low=float.PositiveInfinity;
        foreach(var skin in skins)
        {
            skin.BakeMesh(fallMesh);
            fallMesh.GetVertices(fallVertices);
            foreach(var vertex in fallVertices)low=Mathf.Min(low,skin.transform.TransformPoint(vertex).y);
        }
        float lift=Mathf.Max(0,.025f-low);
        Rig.Hips.position+=Vector3.up*lift;
        FallenLowestPoint=low+lift;
        if(FallProgress>=1)finalLift=lift;
    }
    void OnDestroy(){if(fallMesh)Destroy(fallMesh);}
}
