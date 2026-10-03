using System.Collections.Generic;
using UnityEngine;

// Work in world-space limb planes: the imported police avatar faces -X and its
// bone axes are not the axes of the prefab. Never add Euler rotations to them.
[DefaultExecutionOrder(4000)]
public sealed class Chapter1IncidentRig : MonoBehaviour
{
    public Animator animator;
    public bool police;
    public bool walking;
    public bool frozen;
    public float strideScale = 1f;
    public float Height { get; private set; }
    public Vector3 Forward => transform.TransformDirection(localForward).normalized;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;
    public Transform Head => head;
    public Transform Hips => hips;
    Transform hips, head, leftThigh, rightThigh, leftCalf, rightCalf, leftFoot, rightFoot;
    Transform leftArm, rightArm, leftElbow, rightElbow, leftHand, rightHand;
    Transform[] bones;
    Vector3[] positions;
    Quaternion[] rotations;
    Vector3 localForward, leftAnkle, rightAnkle, previousPosition;
    Quaternion leftFootRotation, rightFootRotation;
    Vector3 leftShoulderRest, rightShoulderRest;
    Quaternion leftPalmBasis, rightPalmBasis;
    Vector3 leftPalmCenter, rightPalmCenter, rightToeLocal;
    float leftPalmLength, rightPalmLength;
    readonly Dictionary<Transform,List<Vector3>> limbVertices=new Dictionary<Transform,List<Vector3>>();
    readonly Dictionary<Transform,Quaternion> meshBases=new Dictionary<Transform,Quaternion>();
    sealed class GripMesh
    {
        public SkinnedMeshRenderer renderer;
        public Mesh original, posed;
        public int left, right;
    }
    readonly List<GripMesh> gripMeshes=new List<GripMesh>();
    float phase, ankleClearance;
    bool ready;
    Vector3[] blendPositions;
    Quaternion[] blendRotations;
    float poseBlendSeconds, poseBlendElapsed;
    public Transform gripTarget;
    public bool gripWithLeft = true;
    public bool resisting;
    public bool strugglingInPlace;
    public bool relaxedPoliceWalk;
    public bool strike;
    public float strikeProgress;
    public bool kicking;
    public float kickProgress;
    public Vector3 kickContact;
    public Vector3 KickFootPosition => rightFoot != null ? rightFoot.position : transform.position;
    public Vector3 KickToePosition => rightFoot != null ? rightFoot.TransformPoint(rightToeLocal) : KickFootPosition;
    GameObject baton;
    Material batonMaterial;
    public bool batonVisible;
    public bool batonInLeftHand;
    public Transform gripPartner;
    public Vector3? StationaryGrip { get; set; }
    public float pointProgress;
    public Transform pointTarget;
    public Transform conversationTarget;
    [Range(0f, 1f)] public float speakingWeight;
    public Chapter1IncidentRig pushTarget;
    [Range(0f, 1f)] public float pushWeight;
    [Range(0f, 1f)] public float stumbleWeight;
    [Range(0f, 1f)] public float stumbleProgress;
    public bool smoothLocomotion;
    float locomotionWeight;
    Vector3 pushRearAnkle;
    bool pushing;

    public void Initialize(Animator source, bool isPolice, float blendSeconds = 0f)
    {
        if (ready) return;
        animator=source;police=isPolice;
        if(animator==null)return;
        animator.Update(0f);
        hips=B(HumanBodyBones.Hips);head=B(HumanBodyBones.Head);
        leftThigh=B(HumanBodyBones.LeftUpperLeg);rightThigh=B(HumanBodyBones.RightUpperLeg);
        leftCalf=B(HumanBodyBones.LeftLowerLeg);rightCalf=B(HumanBodyBones.RightLowerLeg);
        leftFoot=B(HumanBodyBones.LeftFoot);rightFoot=B(HumanBodyBones.RightFoot);
        leftArm=B(HumanBodyBones.LeftUpperArm);rightArm=B(HumanBodyBones.RightUpperArm);
        leftElbow=B(HumanBodyBones.LeftLowerArm);rightElbow=B(HumanBodyBones.RightLowerArm);
        leftHand=B(HumanBodyBones.LeftHand);rightHand=B(HumanBodyBones.RightHand);
        if(hips==null||head==null||leftFoot==null||rightFoot==null
            ||leftThigh==null||rightThigh==null||leftCalf==null||rightCalf==null
            ||leftArm==null||rightArm==null||leftElbow==null||rightElbow==null
            ||leftHand==null||rightHand==null)return;
        bones=animator.GetComponentsInChildren<Transform>(true);
        if(blendSeconds>0f)
        {
            poseBlendSeconds=blendSeconds;poseBlendElapsed=0f;
            blendPositions=new Vector3[bones.Length];blendRotations=new Quaternion[bones.Length];
            for(int i=0;i<bones.Length;i++){blendPositions[i]=bones[i].localPosition;blendRotations[i]=bones[i].localRotation;}
        }
        Height=(head.position.y-Mathf.Min(leftFoot.position.y,rightFoot.position.y))*1.09f;
        Vector3 forward=police?-transform.right:Vector3.Cross(rightArm.position-leftArm.position,Vector3.up).normalized;
        localForward=transform.InverseTransformDirection(forward);
        CacheLimbGeometry(forward);
        animator.enabled=false;
        var old=GetComponent<Chapter1PoliceRunAnimator>();if(old!=null)old.enabled=false;
        var resistance=GetComponent<Chapter1VictimResistanceMotion>();if(resistance!=null)resistance.StopResistance();
        var incidentMotion=GetComponent<Chapter1PoliceIncidentMotion>();if(incidentMotion!=null)incidentMotion.StopMotion(true);
        var grounder=GetComponent<Chapter1NpcGrounding>();if(grounder!=null)grounder.enabled=false;

        // Level pelvis height against the shorter leg, preserving the source rig's
        // joint translations and lengths. This removes the asymmetric bent stance.
        float lengthL=Vector3.Distance(leftThigh.position,leftCalf.position)+Vector3.Distance(leftCalf.position,leftFoot.position);
        float lengthR=Vector3.Distance(rightThigh.position,rightCalf.position)+Vector3.Distance(rightCalf.position,rightFoot.position);
        if(police)
        {
            // The supplied police skeleton has unequal calf segments. Calibrate
            // both sides to their mean length once, before solving the gait.
            float upperL=Vector3.Distance(leftThigh.position,leftCalf.position),upperR=Vector3.Distance(rightThigh.position,rightCalf.position);
            float lowerL=Vector3.Distance(leftCalf.position,leftFoot.position),lowerR=Vector3.Distance(rightCalf.position,rightFoot.position);
            leftCalf.localPosition*=(upperL+upperR)*0.5f/upperL;
            rightCalf.localPosition*=(upperL+upperR)*0.5f/upperR;
            leftFoot.localPosition*=(lowerL+lowerR)*0.5f/lowerL;
            rightFoot.localPosition*=(lowerL+lowerR)*0.5f/lowerR;
            lengthL=lengthR=(upperL+upperR+lowerL+lowerR)*0.5f;
            // The idle pose also rolls the pelvis. Equal calf lengths alone do
            // not level the two hip sockets, so one leg still reaches sooner.
            Vector3 hipSpan=rightThigh.position-leftThigh.position;
            Vector3 levelSpan=Vector3.ProjectOnPlane(hipSpan,Vector3.up);
            if(levelSpan.sqrMagnitude>0.0001f)
                hips.rotation=Quaternion.FromToRotation(hipSpan,levelSpan)*hips.rotation;
        }
        float footY=Mathf.Min(leftFoot.position.y,rightFoot.position.y);
        float desiredHip=footY+Mathf.Min(lengthL,lengthR)*0.99f;
        hips.position+=Vector3.up*(desiredHip-(leftThigh.position.y+rightThigh.position.y)*0.5f);
        Vector3 l=leftThigh.position;l.y=footY;
        Vector3 r=rightThigh.position;r.y=footY;
        Solve(leftThigh,leftCalf,leftFoot,l,forward);
        Solve(rightThigh,rightCalf,rightFoot,r,forward);
        FlattenFoot(leftFoot,B(HumanBodyBones.LeftToes),forward);
        FlattenFoot(rightFoot,B(HumanBodyBones.RightToes),forward);
        LevelMeshFoot(leftFoot,forward);
        LevelMeshFoot(rightFoot,forward);
        RelaxArm(leftArm,leftElbow,leftHand,forward);
        RelaxArm(rightArm,rightElbow,rightHand,forward);
        leftAnkle=transform.InverseTransformPoint(leftFoot.position);
        rightAnkle=transform.InverseTransformPoint(rightFoot.position);
        leftFootRotation=Quaternion.Inverse(transform.rotation)*leftFoot.rotation;
        rightFootRotation=Quaternion.Inverse(transform.rotation)*rightFoot.rotation;
        leftShoulderRest=transform.InverseTransformPoint(leftArm.position);
        rightShoulderRest=transform.InverseTransformPoint(rightArm.position);
        leftPalmBasis=PalmBasis(true);
        rightPalmBasis=PalmBasis(false);
        leftPalmLength=MeasurePalmLength(true);
        rightPalmLength=MeasurePalmLength(false);
        float leftSole=MeshSoleOffset(leftFoot),rightSole=MeshSoleOffset(rightFoot);
        // Keep each sole on the same plane even if the generated foot meshes
        // have different distances from their ankle pivots.
        if(police)
        {
            // The generated ankle pivots also differ relative to the shoe soles.
            // Balance the shoe skin offsets, keeping both ankle targets level;
            // raising just one ankle creates a permanently bent, limping leg.
            ankleClearance=(leftSole+rightSole)*0.5f+0.02f;
            BuildPoliceGripMeshes(leftSole,rightSole);
        }
        else
        {
            leftAnkle+=transform.InverseTransformVector(Vector3.up*(leftSole-Mathf.Min(leftSole,rightSole)));
            rightAnkle+=transform.InverseTransformVector(Vector3.up*(rightSole-Mathf.Min(leftSole,rightSole)));
            ankleClearance=Mathf.Min(leftSole,rightSole)+0.02f;
        }
        bones=animator.GetComponentsInChildren<Transform>(true);
        positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        previousPosition=transform.position;ready=true;
    }
    Transform B(HumanBodyBones b)
    {
        if(animator.isHuman)return animator.GetBoneTransform(b);
        switch(b)
        {
            case HumanBodyBones.Hips:return Chapter1WeddingRigBones.Resolve(animator,b,"Hips","Pelvis");
            case HumanBodyBones.Head:return Chapter1WeddingRigBones.Resolve(animator,b,"Head");
            case HumanBodyBones.Spine:return Chapter1WeddingRigBones.Resolve(animator,b,"Spine","Spine1");
            case HumanBodyBones.LeftUpperLeg:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Thigh","LeftUpLeg");
            case HumanBodyBones.RightUpperLeg:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Thigh","RightUpLeg");
            case HumanBodyBones.LeftLowerLeg:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Calf","LeftLeg");
            case HumanBodyBones.RightLowerLeg:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Calf","RightLeg");
            case HumanBodyBones.LeftFoot:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Foot","LeftFoot");
            case HumanBodyBones.RightFoot:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Foot","RightFoot");
            case HumanBodyBones.LeftToes:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Toe0","LeftToeBase","L_Toe");
            case HumanBodyBones.RightToes:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Toe0","RightToeBase","R_Toe");
            case HumanBodyBones.LeftUpperArm:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Upperarm","LeftArm");
            case HumanBodyBones.RightUpperArm:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Upperarm","RightArm");
            case HumanBodyBones.LeftLowerArm:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Forearm","LeftForeArm");
            case HumanBodyBones.RightLowerArm:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Forearm","RightForeArm");
            case HumanBodyBones.LeftHand:return Chapter1WeddingRigBones.Resolve(animator,b,"L_Hand","LeftHand");
            case HumanBodyBones.RightHand:return Chapter1WeddingRigBones.Resolve(animator,b,"R_Hand","RightHand");
            default:return null;
        }
    }
    static void FlattenFoot(Transform foot,Transform toes,Vector3 forward)
    {
        if(toes==null)return;
        Vector3 direction=toes.position-foot.position;
        if(direction.sqrMagnitude>0.0001f)foot.rotation=Quaternion.FromToRotation(direction,forward)*foot.rotation;
    }
    void RelaxArm(Transform arm,Transform elbow,Transform hand,Vector3 forward)
    {
        if(arm==null||elbow==null||hand==null)return;
        float length=Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
        Solve(arm,elbow,hand,arm.position+Vector3.down*length*0.94f+forward*length*0.12f,-forward);
    }
    public void Face(Vector3 direction)
    {
        direction=Vector3.ProjectOnPlane(direction,Vector3.up);
        if(direction.sqrMagnitude>0.001f) transform.rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(localForward,Vector3.up));
    }
    public void Ground(float groundY)
    {
        if(!ready)return;
        float localFootY=Mathf.Min(transform.TransformPoint(leftAnkle).y,transform.TransformPoint(rightAnkle).y)-transform.position.y;
        var p=transform.position;p.y=groundY+ankleClearance-localFootY;transform.position=p;
    }
    public void AnchorPartnerGrip()
    {
        var partner=gripPartner!=null?gripPartner.GetComponent<Chapter1IncidentRig>():null;
        if(partner==null)return;
        // Both hands share a reachable, fixed wrist position while the woman
        // pulls away with her body. Head movement must not move the grip target.
        StationaryGrip=partner.StationaryGrip=PartnerGripPoint(partner);
    }
    Vector3 PartnerGripPoint(Chapter1IncidentRig partner)
    {
        // Read the neutral shoulders, not the partner's last rendered pose.
        // This keeps the wrist stable regardless of LateUpdate ordering.
        Vector3 shoulder=transform.TransformPoint(gripWithLeft?leftShoulderRest:rightShoulderRest);
        Vector3 other=partner.transform.TransformPoint(partner.gripWithLeft?partner.leftShoulderRest:partner.rightShoulderRest);
        return (shoulder+other)*0.5f-Vector3.up*Mathf.Min(Height,partner.Height)*0.13f;
    }
    Quaternion PalmBasis(bool left)
    {
        Transform hand=left?leftHand:rightHand;
        Transform middle=B(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        Transform index=B(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
        Transform little=B(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
        if(middle==null||index==null||little==null)
        {
            // These Tripo avatars end at Hand and Foot: no finger or toe bones.
            // Recover the palm direction from vertices bound to the wrist.
            Vector3 center=left?leftPalmCenter:rightPalmCenter;
            Vector3 meshFingers=center.sqrMagnitude>0.00001f?center.normalized:Vector3.up;
            Vector3 bodyForward=meshBases.TryGetValue(hand,out Quaternion basis)?basis*Vector3.forward:Vector3.forward;
            // The minimum-variance axis is a plane normal with two possible
            // signs. Use the anatomical palm side, not the back of the hand:
            // palms face inward in an A pose and downward in a T pose.
            Vector3 reference=Vector3.Cross(meshFingers,bodyForward)*(left?-1f:1f);
            Vector3 axis=Vector3.ProjectOnPlane(reference,meshFingers).normalized;
            if(axis.sqrMagnitude<0.001f)axis=Vector3.ProjectOnPlane(Vector3.right,meshFingers).normalized;
            Vector3 other=Vector3.Cross(meshFingers,axis).normalized;
            float xx=0f,xy=0f,yy=0f;
            foreach(var point in limbVertices[hand])
            {
                Vector3 d=point-center;float x=Vector3.Dot(d,axis),y=Vector3.Dot(d,other);
                xx+=x*x;xy+=x*y;yy+=y*y;
            }
            float angle=0.5f*Mathf.Atan2(2f*xy,xx-yy);
            Vector3 meshNormal=-axis*Mathf.Sin(angle)+other*Mathf.Cos(angle);
            if(Vector3.Dot(meshNormal,reference)<0f)meshNormal=-meshNormal;
            return Quaternion.LookRotation(meshFingers,meshNormal);
        }
        Vector3 fingers=hand.InverseTransformDirection(middle.position-hand.position).normalized;
        Vector3 across=hand.InverseTransformDirection(index.position-little.position);
        Vector3 normal=Vector3.Cross(across,fingers).normalized*(left?1f:-1f);
        return Quaternion.LookRotation(fingers,normal);
    }
    void OrientHand(bool left,Vector3 fingers,Vector3 palmNormal)
    {
        if(fingers.sqrMagnitude<0.0001f)return;
        palmNormal=Vector3.ProjectOnPlane(palmNormal,fingers).normalized;
        if(palmNormal.sqrMagnitude<0.001f)palmNormal=Vector3.ProjectOnPlane(Forward,fingers).normalized;
        Transform hand=left?leftHand:rightHand;
        hand.rotation=Quaternion.LookRotation(fingers,palmNormal)*Quaternion.Inverse(left?leftPalmBasis:rightPalmBasis);
    }
    void CurlHand(bool left,float amount)
    {
        if(!animator.isHuman)return;
        HumanBodyBones first=left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal;
        HumanBodyBones end=left?HumanBodyBones.LeftLittleDistal:HumanBodyBones.RightLittleDistal;
        for(int id=(int)first;id<=(int)end;id++)
        {
            var finger=B((HumanBodyBones)id);
            if(finger==null||finger.childCount==0)continue;
            Vector3 axis=Vector3.Cross(finger.GetChild(0).position-finger.position,
                (left?leftHand:rightHand).TransformDirection((left?leftPalmBasis:rightPalmBasis)*Vector3.up));
            if(axis.sqrMagnitude>0.0001f)
                finger.rotation=Quaternion.AngleAxis(55f*amount,axis.normalized)*finger.rotation;
        }
    }
    void CacheLimbGeometry(Vector3 forward)
    {
        foreach(var bone in new[]{leftHand,rightHand,leftFoot,rightFoot})limbVertices[bone]=new List<Vector3>();
        foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh mesh=renderer.sharedMesh;
            if(mesh==null||!mesh.isReadable)continue;
            Vector3[] vertices=mesh.vertices;
            BoneWeight[] weights=mesh.boneWeights;
            Matrix4x4[] bindposes=mesh.bindposes;
            Transform[] rigBones=renderer.bones;
            Vector3 sourceForward=renderer.transform.InverseTransformDirection(forward);
            Vector3 sourceUp=renderer.transform.InverseTransformDirection(Vector3.up);
            int leftShoulder=System.Array.IndexOf(rigBones,leftArm),rightShoulder=System.Array.IndexOf(rigBones,rightArm);
            int sourceHead=System.Array.IndexOf(rigBones,head),sourceHips=System.Array.IndexOf(rigBones,hips);
            if(leftShoulder>=0&&rightShoulder>=0&&sourceHead>=0&&sourceHips>=0)
            {
                Vector3 span=bindposes[rightShoulder].inverse.MultiplyPoint3x4(Vector3.zero)-bindposes[leftShoulder].inverse.MultiplyPoint3x4(Vector3.zero);
                sourceUp=(bindposes[sourceHead].inverse.MultiplyPoint3x4(Vector3.zero)-bindposes[sourceHips].inverse.MultiplyPoint3x4(Vector3.zero)).normalized;
                sourceForward=Vector3.Cross(span,sourceUp).normalized;
            }
            var slots=new Dictionary<int,Transform>();
            for(int i=0;i<rigBones.Length&&i<bindposes.Length;i++)
            {
                Transform bone=rigBones[i];
                if(bone==null||!limbVertices.ContainsKey(bone))continue;
                slots[i]=bone;
                Vector3 bindForward=bindposes[i].MultiplyVector(sourceForward).normalized;
                Vector3 bindUp=bindposes[i].MultiplyVector(sourceUp).normalized;
                meshBases[bone]=Quaternion.LookRotation(bindForward,bindUp);
            }
            for(int i=0;i<vertices.Length&&i<weights.Length;i++)
            {
                BoneWeight w=weights[i];
                int slot=w.boneIndex0;float strength=w.weight0;
                if(w.weight1>strength){slot=w.boneIndex1;strength=w.weight1;}
                if(w.weight2>strength){slot=w.boneIndex2;strength=w.weight2;}
                if(w.weight3>strength){slot=w.boneIndex3;strength=w.weight3;}
                if(strength>0.45f&&slots.TryGetValue(slot,out Transform bone))
                    limbVertices[bone].Add(bindposes[slot].MultiplyPoint3x4(vertices[i]));
            }
        }
        foreach(bool left in new[]{true,false})
        {
            Transform hand=left?leftHand:rightHand;
            Vector3 center=Vector3.zero;
            foreach(var vertex in limbVertices[hand])center+=vertex;
            if(limbVertices[hand].Count>0)center/=limbVertices[hand].Count;
            if(left)leftPalmCenter=center;else rightPalmCenter=center;
        }
        CalibrateFootGeometry(leftFoot);
        CalibrateFootGeometry(rightFoot);
        if(meshBases.TryGetValue(rightFoot,out Quaternion footBasis))
        {
            Vector3 localForwardAxis=footBasis*Vector3.forward;
            float furthest=float.NegativeInfinity;
            foreach(var vertex in limbVertices[rightFoot])
            {
                float projection=Vector3.Dot(vertex,localForwardAxis);
                if(projection>furthest){furthest=projection;rightToeLocal=vertex;}
            }
            // Centre the toe across its width, rather than using a corner of
            // the shoe as the point that contacts a jar.
            rightToeLocal=localForwardAxis*Mathf.Max(0f,furthest);
        }
        else rightToeLocal=rightFoot.InverseTransformVector(forward*Height*0.055f);
    }
    void CalibrateFootGeometry(Transform foot)
    {
        var points=limbVertices[foot];
        if(points.Count<30||!meshBases.TryGetValue(foot,out Quaternion reference))return;
        // Bind axes describe the skeleton, not the generated shoe. In these
        // meshes the woman's toes point sideways and the left police shoe is
        // pitched down. Find the broad supporting sole, then its heel-to-toe
        // axis, once at setup; never guess these from the animated root yaw.
        Vector3 center=Vector3.zero,min=points[0],max=points[0];
        foreach(var p in points){center+=p;min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
        center/=points.Count;
        float band=(max-min).magnitude*0.015f;
        int step=Mathf.Max(1,points.Count/1200),bestScore=-1;
        float bestPitch=0f,bestRoll=0f;
        Vector3 soleUp=reference*Vector3.up;
        for(int pass=0;pass<2;pass++)
        {
            float pitchMin=pass==0?-65f:bestPitch-4f,pitchMax=pass==0?65f:bestPitch+4f;
            float rollMin=pass==0?-35f:bestRoll-4f,rollMax=pass==0?35f:bestRoll+4f;
            float increment=pass==0?5f:1f;
            for(float pitch=pitchMin;pitch<=pitchMax;pitch+=increment)
            for(float roll=rollMin;roll<=rollMax;roll+=increment)
            {
                Vector3 normal=reference*(Quaternion.AngleAxis(pitch,Vector3.right)*Quaternion.AngleAxis(roll,Vector3.forward)*Vector3.up);
                float bottom=float.PositiveInfinity;
                for(int i=0;i<points.Count;i+=step)bottom=Mathf.Min(bottom,Vector3.Dot(points[i],normal));
                int score=0;
                for(int i=0;i<points.Count;i+=step)if(Vector3.Dot(points[i],normal)<bottom+band)score++;
                if(score<=bestScore)continue;
                bestScore=score;bestPitch=pitch;bestRoll=roll;soleUp=normal;
            }
        }
        Vector3 toe=Vector3.ProjectOnPlane(center,soleUp).normalized;
        if(toe.sqrMagnitude<0.01f)toe=Vector3.ProjectOnPlane(reference*Vector3.forward,soleUp).normalized;
        // Power iteration of the covariance in the sole plane gives the long
        // axis without treating the boot's tall ankle cuff as its toe direction.
        for(int iteration=0;iteration<10;iteration++)
        {
            Vector3 next=Vector3.zero;
            for(int i=0;i<points.Count;i+=step)
            {
                Vector3 d=Vector3.ProjectOnPlane(points[i]-center,soleUp);
                next+=d*Vector3.Dot(d,toe);
            }
            if(next.sqrMagnitude<1e-12f)break;
            toe=next.normalized;
        }
        if(Vector3.Dot(toe,center)<0f)toe=-toe;
        meshBases[foot]=Quaternion.LookRotation(toe,soleUp);
    }
    void LevelMeshFoot(Transform foot,Vector3 forward)
    {
        if(meshBases.TryGetValue(foot,out Quaternion basis))
            foot.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.Inverse(basis);
    }
    float MeshSoleOffset(Transform foot)
    {
        float minimum=0f;
        foreach(var vertex in limbVertices[foot])minimum=Mathf.Min(minimum,foot.TransformVector(vertex).y);
        return minimum<0f?-minimum:Height*0.025f;
    }
    float MeasurePalmLength(bool left)
    {
        Transform hand=left?leftHand:rightHand;
        Vector3 fingers=(left?leftPalmBasis:rightPalmBasis)*Vector3.forward;
        float length=0f;
        foreach(var point in limbVertices[hand])length=Mathf.Max(length,Vector3.Dot(point,fingers));
        return length;
    }
    Vector3 ClosedPalmCenter(bool left)
    {
        Quaternion basis=left?leftPalmBasis:rightPalmBasis;
        return basis*(new Vector3(0f,0.16f,0.43f)*(left?leftPalmLength:rightPalmLength));
    }
    void BuildPoliceGripMeshes(float leftSole,float rightSole)
    {
        foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh original=renderer.sharedMesh;
            if(original==null||!original.isReadable)continue;
            var vertices=original.vertices;
            var normals=original.normals;
            var weights=original.boneWeights;
            var bindposes=original.bindposes;
            var rigBones=renderer.bones;
            var leftDelta=new Vector3[vertices.Length];
            var rightDelta=new Vector3[vertices.Length];
            var leftNormals=new Vector3[vertices.Length];
            var rightNormals=new Vector3[vertices.Length];
            var soleDelta=new Vector3[vertices.Length];
            float mean=(leftSole+rightSole)*0.5f;
            for(int i=0;i<vertices.Length&&i<weights.Length;i++)
            {
                BoneWeight w=weights[i];
                for(int influence=0;influence<4;influence++)
                {
                    int slot=influence==0?w.boneIndex0:influence==1?w.boneIndex1:influence==2?w.boneIndex2:w.boneIndex3;
                    float weight=influence==0?w.weight0:influence==1?w.weight1:influence==2?w.weight2:w.weight3;
                    if(weight<=0f||slot>=rigBones.Length||slot>=bindposes.Length)continue;
                    Transform bone=rigBones[slot];
                    if(bone==leftFoot||bone==rightFoot)
                    {
                        float shift=(bone==leftFoot?leftSole:rightSole)-mean;
                        Vector3 localShift=bone.InverseTransformVector(Vector3.up*shift);
                        soleDelta[i]+=bindposes[slot].inverse.MultiplyVector(localShift)*weight;
                    }
                    if(bone!=leftHand&&bone!=rightHand)continue;
                    bool left=bone==leftHand;
                    Quaternion basis=left?leftPalmBasis:rightPalmBasis;
                    Vector3 fingers=basis*Vector3.forward,normal=basis*Vector3.up;
                    Vector3 point=bindposes[slot].MultiplyPoint3x4(vertices[i]);
                    float length=left?leftPalmLength:rightPalmLength,knuckle=length*0.43f;
                    float along=Vector3.Dot(point,fingers);
                    if(along<=knuckle||length<0.0001f)continue;
                    float radius=length*0.16f;
                    float curl=Mathf.Min((along-knuckle)/radius,Mathf.PI*1.03f);
                    Vector3 delta=fingers*(knuckle+Mathf.Sin(curl)*radius-along)
                        +normal*(1f-Mathf.Cos(curl))*radius;
                    Vector3 meshDelta=bindposes[slot].inverse.MultiplyVector(delta)*weight;
                    if(left)leftDelta[i]+=meshDelta;else rightDelta[i]+=meshDelta;
                    if(i<normals.Length)
                    {
                        Vector3 localNormal=bindposes[slot].inverse.transpose.MultiplyVector(normals[i]).normalized;
                        Quaternion turn=Quaternion.AngleAxis(curl*Mathf.Rad2Deg,Vector3.Cross(fingers,normal));
                        Vector3 normalDelta=(bindposes[slot].transpose.MultiplyVector(turn*localNormal).normalized-normals[i])*weight;
                        if(left)leftNormals[i]+=normalDelta;else rightNormals[i]+=normalDelta;
                    }
                }
            }
            var posed=Instantiate(original);posed.name=original.name+" incident grip";
            int l=posed.blendShapeCount;
            posed.AddBlendShapeFrame("Incident left grip",100f,leftDelta,leftNormals,null);
            int r=posed.blendShapeCount;
            posed.AddBlendShapeFrame("Incident right grip",100f,rightDelta,rightNormals,null);
            int sole=posed.blendShapeCount;
            posed.AddBlendShapeFrame("Incident level soles",100f,soleDelta,null,null);
            renderer.sharedMesh=posed;
            renderer.SetBlendShapeWeight(sole,100f);
            gripMeshes.Add(new GripMesh{renderer=renderer,original=original,posed=posed,left=l,right=r});
        }
    }
    public void ClearInteractionPose()
    {
        gripPartner=gripTarget=pointTarget=conversationTarget=null;
        StationaryGrip=null;
        speakingWeight=pushWeight=stumbleWeight=0f;
        pushTarget=null;
        resisting=strugglingInPlace=strike=kicking=false;
    }
    void LateUpdate()
    {
        if(!ready)return;
        for(int i=0;i<bones.Length;i++)if(bones[i]!=null&&bones[i]!=transform)bones[i].SetLocalPositionAndRotation(positions[i],rotations[i]);
        float distance=Vector3.ProjectOnPlane(transform.position-previousPosition,Vector3.up).magnitude;
        previousPosition=transform.position;
        locomotionWeight = smoothLocomotion
            ? Mathf.MoveTowards(locomotionWeight, walking && !frozen ? 1f : 0f, Time.deltaTime * 6f)
            : (walking && !frozen ? 1f : 0f);
        Vector3 right=Vector3.Cross(Vector3.up,Forward);
        if(pushTarget != null && !pushing)
        {
            pushRearAnkle = transform.TransformPoint(leftAnkle);
            pushing = true;
        }
        if(pushTarget == null) pushing = false;
        if(pushWeight > 0f)
        {
            Transform spine = B(HumanBodyBones.Spine);
            if(spine != null) spine.rotation = Quaternion.AngleAxis(13f * pushWeight, right) * spine.rotation;
        }
        if(stumbleWeight > 0f)
        {
            hips.position -= Vector3.up * Height * 0.075f * stumbleWeight;
            hips.rotation = Quaternion.AngleAxis(-20f * stumbleWeight, right) * hips.rotation;
            Transform spine = B(HumanBodyBones.Spine);
            if(spine != null) spine.rotation = Quaternion.AngleAxis(-9f * stumbleWeight, right) * spine.rotation;
            head.rotation = Quaternion.AngleAxis(8f * stumbleWeight, right) * head.rotation;
        }
        // During the 60% stance phase the foot travels exactly opposite to the
        // actor's displacement. Match phase advance to the authored stride.
        if(walking&&!frozen)phase+=Mathf.Min(distance,Height*0.2f)/Mathf.Max(0.1f,Height*0.23f*strideScale/0.6f)*Mathf.PI*2f;
        if(relaxedPoliceWalk && locomotionWeight > 0f)
        {
            // Leave enough knee flexion for the stride to reach the floor.
            // Moving a nearly straight leg otherwise clamps IK above the ground.
            hips.position += (Vector3.down * Height * (0.027f + 0.002f * Mathf.Cos(phase * 2f))
                + right * Height * 0.0025f * Mathf.Sin(phase)) * locomotionWeight;
            hips.rotation = Quaternion.AngleAxis(2f * Mathf.Sin(phase) * locomotionWeight, Vector3.up) * hips.rotation;
            var spine = B(HumanBodyBones.Spine);
            if(spine != null) spine.rotation = Quaternion.AngleAxis(-3f * Mathf.Sin(phase) * locomotionWeight, Vector3.up) * spine.rotation;
        }
        if(kicking)
        {
            float effort=Mathf.Sin(Mathf.PI*kickProgress);
            hips.position+=(-right*Height*0.025f-Vector3.up*Height*0.025f)*effort;
            var spine=B(HumanBodyBones.Spine);
            if(spine!=null)spine.rotation=Quaternion.AngleAxis(-5f*effort,right)*spine.rotation;
        }
        if(strugglingInPlace && !frozen)
        {
            float effort = StrugglePhase;
            hips.position += Vector3.down * Height * (0.007f + 0.003f * Mathf.Sin(effort))
                - Forward * Height * (0.008f + 0.004f * Mathf.Sin(effort))
                + right * Height * 0.003f * Mathf.Sin(effort);
        }
        ApplyLeg(leftThigh,leftCalf,leftFoot,leftAnkle,leftFootRotation,phase);
        ApplyLeg(rightThigh,rightCalf,rightFoot,rightAnkle,rightFootRotation,phase+Mathf.PI);
        if(locomotionWeight > 0f)
        {
            float swing=relaxedPoliceWalk
                ? Mathf.Cos(phase) * 14f * strideScale * locomotionWeight
                : Mathf.Sin(phase)*14f*strideScale*locomotionWeight;
            leftArm.rotation=Quaternion.AngleAxis(swing,right)*leftArm.rotation;
            float rightSwing=-swing;
            rightArm.rotation=Quaternion.AngleAxis(rightSwing,right)*rightArm.rotation;
        }
        if(resisting)
        {
            float tremble=frozen?0f:Mathf.Sin(strugglingInPlace?StrugglePhase:Time.time*7f)*(strugglingInPlace?4f:2f);
            var spine=B(HumanBodyBones.Spine);
            if(spine!=null)spine.rotation=Quaternion.AngleAxis((strugglingInPlace?-4f:-8f)+tremble,right)*spine.rotation;
            Transform freeArm=gripWithLeft?rightArm:leftArm, freeElbow=gripWithLeft?rightElbow:leftElbow, freeHand=gripWithLeft?rightHand:leftHand;
            float effort=frozen?0f:StrugglePhase;
            Vector3 freeSide=gripWithLeft?right:-right;
            Vector3 wrist=StationaryGrip??(gripWithLeft?leftHand.position:rightHand.position);
            var partner=gripPartner!=null?gripPartner.GetComponent<Chapter1IncidentRig>():null;
            if(partner!=null&&!StationaryGrip.HasValue)wrist=PartnerGripPoint(partner);
            Vector3 reach=wrist-Forward*Height*(0.055f+0.008f*Mathf.Sin(effort))
                +freeSide*Height*0.035f-Vector3.up*Height*0.025f;
            float armLength=Vector3.Distance(freeArm.position,freeElbow.position)+Vector3.Distance(freeElbow.position,freeHand.position);
            reach=freeArm.position+Vector3.ClampMagnitude(reach-freeArm.position,armLength*0.90f);
            Vector3 elbowPole=Vector3.down+freeSide*0.25f;
            if(strugglingInPlace && !frozen)
            {
                if(spine!=null)spine.rotation=Quaternion.AngleAxis(Mathf.Sin(effort)*3f,Vector3.up)*spine.rotation;
                head.rotation=Quaternion.AngleAxis(-tremble*0.35f,right)*head.rotation;
                // The free hand protects and pulls at the held wrist. Keep the
                // elbow below the shoulder instead of folding it behind the head.
            }
            Solve(freeArm,freeElbow,freeHand,reach,elbowPole);
            OrientHand(!gripWithLeft,freeHand.position-freeElbow.position,-Forward+Vector3.down*0.4f);
        }
        if(gripTarget!=null)
        {
            Transform arm=gripWithLeft?leftArm:rightArm, elbow=gripWithLeft?leftElbow:rightElbow, hand=gripWithLeft?leftHand:rightHand;
            Solve(arm,elbow,hand,gripTarget.position,-Forward+Vector3.down*0.4f);
        }
        if(gripPartner!=null)
        {
            var partnerRig=gripPartner.GetComponent<Chapter1IncidentRig>();
            Vector3 grip=partnerRig!=null?PartnerGripPoint(partnerRig):gripPartner.position+Vector3.up*Height*0.6f;
            if(StationaryGrip.HasValue)grip=StationaryGrip.Value;
            Transform arm=gripWithLeft?leftArm:rightArm, elbow=gripWithLeft?leftElbow:rightElbow, hand=gripWithLeft?leftHand:rightHand;
            Solve(arm,elbow,hand,grip,Vector3.down);
            OrientHand(gripWithLeft,hand.position-elbow.position,Vector3.down);
            if(police)CurlHand(gripWithLeft,0.7f);
        }
        if(pointTarget!=null)
        {
            Vector3 dir=Vector3.ProjectOnPlane(pointTarget.position-transform.position,Vector3.up).normalized;
            // Keep the elbow below the chest and the wrist near its neutral
            // angle while presenting the baton toward the crowd.
            Solve(rightArm,rightElbow,rightHand,Vector3.Lerp(rightHand.position,rightArm.position+dir*Height*0.16f-Vector3.up*Height*0.24f,pointProgress),Vector3.down);
        }
        if(police && batonVisible && !strike && pointTarget==null)
        {
            // Separate the held baton from the dark trouser silhouette.
            Vector3 side=batonInLeftHand?-right:right;
            Transform arm=batonInLeftHand?leftArm:rightArm,elbow=batonInLeftHand?leftElbow:rightElbow,hand=batonInLeftHand?leftHand:rightHand;
            Vector3 hold=arm.position-Vector3.up*Height*0.30f+side*Height*0.09f+Forward*Height*0.055f;
            if(relaxedPoliceWalk)
            {
                float handSwing=Mathf.Cos(phase+(batonInLeftHand?0f:Mathf.PI));
                hold=arm.position-Vector3.up*Height*0.30f+side*Height*0.045f
                    +Forward*Height*(0.025f-handSwing*0.055f*locomotionWeight);
            }
            Solve(arm,elbow,hand,hold,-Forward+side*0.4f);
        }
        if(police && relaxedPoliceWalk && gripPartner==null && !strike && pointTarget==null)
        {
            // Solve the free arm too: the imported idle pose places its wrist
            // against the hip even when the shoulder is swung for locomotion.
            bool left=!batonInLeftHand;
            Transform arm=left?leftArm:rightArm, elbow=left?leftElbow:rightElbow, hand=left?leftHand:rightHand;
            Vector3 side=left?-right:right;
            float length=Vector3.Distance(arm.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
            float swing=Mathf.Cos(phase+(left?0f:Mathf.PI))*locomotionWeight;
            Vector3 target=arm.position-Vector3.up*length*0.91f+side*Height*0.065f
                +Forward*Height*(0.025f-swing*0.065f);
            Solve(arm,elbow,hand,target,-Forward+side*0.65f);
        }
        if(strike)
        {
            Transform arm=batonInLeftHand?leftArm:rightArm,elbow=batonInLeftHand?leftElbow:rightElbow,hand=batonInLeftHand?leftHand:rightHand;
            Vector3 raised=arm.position+Vector3.up*Height*0.27f-Forward*Height*0.12f;
            Vector3 hit=arm.position+Forward*Height*0.34f-Vector3.up*Height*0.06f;
            Vector3 target=strikeProgress<0.42f?Vector3.Lerp(hand.position,raised,Mathf.SmoothStep(0,1,strikeProgress/0.42f)):
                Vector3.Lerp(raised,hit,Mathf.SmoothStep(0,1,(strikeProgress-0.42f)/0.30f));
            Solve(arm,elbow,hand,target,(batonInLeftHand?-right:right)+Vector3.up*0.3f);
        }
        if(conversationTarget != null && speakingWeight > 0f && stumbleWeight <= 0f && gripPartner==null && !resisting && !strike && !kicking)
        {
            float beat = Mathf.Sin(Time.time * 2.7f);
            Vector3 gesture = rightArm.position + Forward * Height * (0.24f + beat * 0.018f)
                + right * Height * 0.055f - Vector3.up * Height * 0.075f;
            Solve(rightArm, rightElbow, rightHand, Vector3.Lerp(rightHand.position, gesture, speakingWeight), Vector3.down);
            Transform spine = B(HumanBodyBones.Spine);
            if(spine != null) spine.rotation = Quaternion.AngleAxis((2f + beat) * speakingWeight, right) * spine.rotation;
            head.rotation = Quaternion.AngleAxis(beat * 2f * speakingWeight, right) * head.rotation;
        }
        if(stumbleWeight > 0f)
        {
            Vector3 leftCatch = leftArm.position - right * Height * 0.27f
                + Forward * Height * 0.07f - Vector3.up * Height * 0.12f;
            Vector3 rightCatch = rightArm.position + right * Height * 0.30f
                + Forward * Height * 0.08f - Vector3.up * Height * 0.10f;
            Solve(leftArm,leftElbow,leftHand,Vector3.Lerp(leftHand.position,leftCatch,stumbleWeight),-Forward);
            Solve(rightArm,rightElbow,rightHand,Vector3.Lerp(rightHand.position,rightCatch,stumbleWeight),-Forward);
        }
        if(pushTarget != null && pushTarget.Head != null && pushWeight > 0f)
        {
            Vector3 chest = pushTarget.Head.position - Vector3.up * pushTarget.Height * 0.18f
                + pushTarget.Forward * pushTarget.Height * 0.055f;
            Solve(rightArm,rightElbow,rightHand,Vector3.Lerp(rightHand.position,chest,pushWeight),Vector3.down);
        }
        if(blendRotations!=null)
        {
            poseBlendElapsed+=Time.deltaTime;
            float blend=Mathf.SmoothStep(0f,1f,poseBlendElapsed/poseBlendSeconds);
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null&&bones[i]!=transform)
            {
                bones[i].localPosition=Vector3.Lerp(blendPositions[i],bones[i].localPosition,blend);
                bones[i].localRotation=Quaternion.Slerp(blendRotations[i],bones[i].localRotation,blend);
            }
            if(blend>=1f){blendRotations=null;blendPositions=null;}
        }
        // The source wrists retain the pose used when the rig was initialized.
        // Align free hands with the forearms and face the palms toward the
        // thighs, including the woman after release and both departing police.
        foreach(bool left in new[]{true,false})
        {
            bool held=gripPartner!=null&&gripWithLeft==left;
            bool protecting=resisting&&gripWithLeft!=left;
            bool batonHand=police&&batonVisible&&batonInLeftHand==left;
            if(held||protecting||batonHand)continue;
            Transform hand=left?leftHand:rightHand,elbow=left?leftElbow:rightElbow;
            OrientHand(left,hand.position-elbow.position,left?right:-right);
        }
        UpdateBaton();
        foreach(var mesh in gripMeshes)
        {
            mesh.renderer.SetBlendShapeWeight(mesh.left,batonVisible&&batonInLeftHand?100f:gripPartner!=null&&gripWithLeft?75f:0f);
            mesh.renderer.SetBlendShapeWeight(mesh.right,batonVisible&&!batonInLeftHand?100f:gripPartner!=null&&!gripWithLeft?75f:0f);
        }
    }
    void ApplyLeg(Transform thigh,Transform calf,Transform foot,Vector3 ankle,Quaternion rotation,float p)
    {
        Vector3 target=transform.TransformPoint(ankle);
        Quaternion footRotation=transform.rotation*rotation;
        if(pushing && pushWeight > 0f)
        {
            if(foot == leftFoot) target = Vector3.Lerp(target, pushRearAnkle, pushWeight);
            else target += Forward * Height * 0.10f * pushWeight
                + Vector3.up * Height * 0.025f * Mathf.Sin(pushWeight * Mathf.PI);
        }
        if(locomotionWeight > 0f)
        {
            float cycle=Mathf.Repeat(p/(2f*Mathf.PI),1f);
            float stride=Height*0.115f*strideScale;
            float offset=WalkFootOffset(p)*stride;
            float lift=cycle<0.6f?0f:Mathf.Pow(Mathf.Sin((cycle-0.6f)/0.4f*Mathf.PI),2f)*Height*(relaxedPoliceWalk?0.026f:0.035f);
            target+=(Forward*offset+Vector3.up*lift)*locomotionWeight;
        }
        if(kicking && foot==rightFoot)
        {
            Vector3 rest=target;
            // A low forward kick: keep the knee in the forward plane and let
            // the ankle extend slightly instead of holding a rigid flat boot.
            float extension=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0.30f,0.46f,kickProgress))
                *(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0.62f,1f,kickProgress)));
            footRotation=Quaternion.AngleAxis(10f*extension,Vector3.Cross(Vector3.up,Forward))*footRotation;
            Vector3 windup=rest-Forward*Height*0.015f+Vector3.up*Height*0.105f;
            Vector3 toeOffset=footRotation*Vector3.Scale(rightToeLocal,foot.lossyScale);
            Vector3 contact=kickContact-toeOffset;
            if(kickProgress<0.32f)target=Vector3.Lerp(rest,windup,Mathf.SmoothStep(0,1,kickProgress/0.32f));
            else if(kickProgress<0.46f)target=Vector3.Lerp(windup,contact,Mathf.SmoothStep(0,1,(kickProgress-0.32f)/0.14f));
            else if(kickProgress<0.62f)target=contact;
            else target=Vector3.Lerp(contact,rest,Mathf.SmoothStep(0,1,(kickProgress-0.62f)/0.38f));
        }
        if(strugglingInPlace && !frozen)
        {
            // Small recovery steps while the opposite foot stays planted;
            // resistance comes from pulling back, not crouching and high knees.
            float effort=StrugglePhase+(foot==rightFoot?Mathf.PI:0f);
            float lift=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(effort)),2f);
            target+=(-Forward*Height*0.025f+Vector3.up*Height*0.015f)*lift;
        }
        if(stumbleWeight > 0f)
        {
            // Two uneven backward recovery steps, with the planted foot opposing
            // root travel and the other foot lifting over the ground.
            float cycle=Mathf.Repeat(stumbleProgress*2f+(foot==rightFoot?0.5f:0f),1f);
            float stride=Height*0.12f;
            float offset=cycle<0.6f?Mathf.Lerp(-stride,stride,cycle/0.6f)
                :Mathf.Lerp(stride,-stride,Mathf.SmoothStep(0,1,(cycle-0.6f)/0.4f));
            float lift=cycle<0.6f?0f:Mathf.Sin((cycle-0.6f)/0.4f*Mathf.PI)*Height*0.075f;
            target+=(Forward*offset+Vector3.up*lift)*stumbleWeight;
        }
        Solve(thigh,calf,foot,target,Forward);
        foot.rotation=footRotation;
        if(relaxedPoliceWalk && locomotionWeight>0f)
        {
            float cycle=Mathf.Repeat(p/(2f*Mathf.PI),1f);
            float swing=cycle<0.6f?0f:Mathf.Sin((cycle-0.6f)/0.4f*Mathf.PI);
            foot.rotation=Quaternion.AngleAxis(-6f*swing*locomotionWeight,Vector3.Cross(Vector3.up,Forward))*foot.rotation;
        }
    }
    float StrugglePhase => Time.time*3.1f+0.22f*Mathf.Sin(Time.time*1.2f);

    static float WalkFootOffset(float p)
    {
        float cycle=Mathf.Repeat(p/(2f*Mathf.PI),1f);
        if(cycle<0.6f)return Mathf.Lerp(1f,-1f,cycle/0.6f);
        float t=(cycle-0.6f)/0.4f;
        // Match the stance velocity at toe-off and landing; SmoothStep used
        // zero endpoint velocity and caused a hitch at both boundaries.
        float tangent=-2f*0.4f/0.6f;
        return -(2f*t*t*t-3f*t*t+1f)+(t*t*t-2f*t*t+t)*tangent
            +(-2f*t*t*t+3f*t*t)+(t*t*t-t*t)*tangent;
    }
    public void BeginDepartureWalk(float cycleOffset)
    {
        relaxedPoliceWalk=true;
        smoothLocomotion=true;
        strideScale=0.95f;
        phase=cycleOffset*Mathf.PI*2f;
        previousPosition=transform.position;
    }
    public static void Solve(Transform a,Transform b,Transform c,Vector3 target,Vector3 bend)
    {
        if(a==null||b==null||c==null)return;
        float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
        Vector3 delta=target-a.position;float raw=delta.magnitude;
        if(raw<0.0001f)return;
        Vector3 dir=delta/raw;float d=Mathf.Clamp(raw,Mathf.Abs(l1-l2)+0.001f,l1+l2-0.001f);
        float x=(l1*l1+d*d-l2*l2)/(2f*d),y=Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));
        bend=Vector3.ProjectOnPlane(bend,dir).normalized;
        if(bend.sqrMagnitude<0.01f)bend=Vector3.ProjectOnPlane(Vector3.right,dir).normalized;
        Vector3 elbow=a.position+dir*x+bend*y;
        a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
        Vector3 reachable=a.position+dir*d;
        b.rotation=Quaternion.FromToRotation(c.position-b.position,reachable-b.position)*b.rotation;
    }
    void UpdateBaton()
    {
        if(!police)return;
        if(baton==null&&batonVisible)
        {
            baton=GameObject.CreatePrimitive(PrimitiveType.Cylinder);baton.name="Incident_Baton";
            var collider=baton.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            batonMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            batonMaterial.color=new Color(0.24f,0.115f,0.047f);
            batonMaterial.SetFloat("_Smoothness",0.18f);
            baton.GetComponent<Renderer>().sharedMaterial=batonMaterial;
            baton.transform.localScale=new Vector3(Height*0.018f,Height*0.17f,Height*0.018f);
            baton.transform.SetParent(transform,true);
        }
        if(baton==null)return;
        baton.SetActive(batonVisible);baton.GetComponent<Renderer>().enabled=batonVisible;if(!batonVisible)return;
        Vector3 side=Vector3.Cross(Vector3.up,Forward)*(batonInLeftHand?-1f:1f);
        Vector3 direction=strike?Vector3.Slerp(Vector3.up,Forward,Mathf.SmoothStep(0,1,(strikeProgress-0.42f)/0.30f)):(Vector3.down+Forward*0.25f+side*0.55f).normalized;
        if(relaxedPoliceWalk && !strike)
            direction=(Vector3.down+side*0.18f+Forward*(0.08f-WalkFootOffset(phase+(batonInLeftHand?0f:Mathf.PI))*0.16f*locomotionWeight)).normalized;
        if(pointTarget!=null)direction=Vector3.Slerp(Vector3.down,(Vector3.ProjectOnPlane(pointTarget.position-transform.position,Vector3.up).normalized+Vector3.up*0.5f).normalized,pointProgress);
        Transform batonHand=batonInLeftHand?leftHand:rightHand;
        // The wrist pivot is behind the palm. Put the handle through the palm,
        // orient the knuckles to it, then close the fingers around the wood.
        Transform elbow=batonInLeftHand?leftElbow:rightElbow;
        Vector3 forearm=(batonHand.position-elbow.position).normalized;
        Vector3 fingers=Vector3.ProjectOnPlane(forearm,direction).normalized;
        if(fingers.sqrMagnitude<0.01f)fingers=Forward;
        // Choose the wrist direction nearest the forearm. The old fixed cross
        // product turned the hanging fist back toward the elbow, especially
        // with the baton in the left hand. Limit wrist deviation and let the
        // baton angle follow the resulting grip.
        fingers=Vector3.RotateTowards(forearm,fingers,(pointTarget!=null?35f:50f)*Mathf.Deg2Rad,0f).normalized;
        direction=Vector3.ProjectOnPlane(direction,fingers).normalized;
        OrientHand(batonInLeftHand,fingers,Vector3.Cross(fingers,direction)*(batonInLeftHand?-1f:1f));
        CurlHand(batonInLeftHand,1f);
        Vector3 palm=batonHand.TransformPoint(ClosedPalmCenter(batonInLeftHand));
        baton.transform.SetPositionAndRotation(palm+direction*Height*0.13f,Quaternion.FromToRotation(Vector3.up,direction));
    }
    void OnDestroy()
    {
        if(baton!=null)Destroy(baton);if(batonMaterial!=null)Destroy(batonMaterial);
        foreach(var mesh in gripMeshes)
        {
            if(mesh.renderer!=null&&mesh.renderer.sharedMesh==mesh.posed)mesh.renderer.sharedMesh=mesh.original;
            if(mesh.posed!=null)Destroy(mesh.posed);
        }
    }
}
