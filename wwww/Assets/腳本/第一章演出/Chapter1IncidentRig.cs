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
        animator.enabled=false;
        var old=GetComponent<Chapter1PoliceRunAnimator>();if(old!=null)old.enabled=false;
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
        RelaxArm(leftArm,leftElbow,leftHand,forward);
        RelaxArm(rightArm,rightElbow,rightHand,forward);
        leftAnkle=transform.InverseTransformPoint(leftFoot.position);
        rightAnkle=transform.InverseTransformPoint(rightFoot.position);
        leftFootRotation=Quaternion.Inverse(transform.rotation)*leftFoot.rotation;
        rightFootRotation=Quaternion.Inverse(transform.rotation)*rightFoot.rotation;
        ankleClearance=Height*0.035f;
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
        Transform arm=gripWithLeft?leftArm:rightArm;
        Transform partnerArm=partner.gripWithLeft?partner.leftArm:partner.rightArm;
        // Both hands share a reachable, fixed wrist position while the woman
        // pulls away with her body. Head movement must not move the grip target.
        StationaryGrip=partner.StationaryGrip=(arm.position+partnerArm.position)*0.5f
            -Vector3.up*Mathf.Min(Height,partner.Height)*0.10f;
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
            hips.position += (Vector3.down * Height * (0.022f + 0.003f * Mathf.Cos(phase * 2f))
                + right * Height * 0.006f * Mathf.Sin(phase)) * locomotionWeight;
            hips.rotation = Quaternion.AngleAxis(3f * Mathf.Sin(phase) * locomotionWeight, Vector3.up) * hips.rotation;
            var spine = B(HumanBodyBones.Spine);
            if(spine != null) spine.rotation = Quaternion.AngleAxis(-5f * Mathf.Sin(phase) * locomotionWeight, Vector3.up) * spine.rotation;
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
                ? WalkFootOffset(phase) * 20f * strideScale * locomotionWeight
                : Mathf.Sin(phase)*14f*strideScale*locomotionWeight;
            leftArm.rotation=Quaternion.AngleAxis(swing,right)*leftArm.rotation;
            float rightSwing=relaxedPoliceWalk?WalkFootOffset(phase+Mathf.PI)*20f*strideScale*locomotionWeight:-swing;
            rightArm.rotation=Quaternion.AngleAxis(rightSwing,right)*rightArm.rotation;
        }
        if(resisting)
        {
            float tremble=frozen?0f:Mathf.Sin(strugglingInPlace?StrugglePhase:Time.time*7f)*(strugglingInPlace?4f:2f);
            var spine=B(HumanBodyBones.Spine);
            if(spine!=null)spine.rotation=Quaternion.AngleAxis((strugglingInPlace?-4f:-8f)+tremble,right)*spine.rotation;
            Transform freeArm=gripWithLeft?rightArm:leftArm, freeElbow=gripWithLeft?rightElbow:leftElbow, freeHand=gripWithLeft?rightHand:leftHand;
            Vector3 reach=freeArm.position+Forward*Height*0.23f+right*Height*0.04f;
            Vector3 elbowPole=-Forward;
            if(strugglingInPlace && !frozen)
            {
                float effort=StrugglePhase;
                Vector3 freeSide=gripWithLeft?right:-right;
                if(spine!=null)spine.rotation=Quaternion.AngleAxis(Mathf.Sin(effort)*3f,Vector3.up)*spine.rotation;
                head.rotation=Quaternion.AngleAxis(-tremble*0.35f,right)*head.rotation;
                // The free hand protects and pulls at the held wrist. Keep the
                // elbow below the shoulder instead of folding it behind the head.
                Vector3 wrist=StationaryGrip??(gripWithLeft?leftHand.position:rightHand.position);
                reach=wrist-Forward*Height*(0.045f+0.012f*Mathf.Sin(effort))
                    +freeSide*Height*0.022f+Vector3.up*Height*0.012f;
                float armLength=Vector3.Distance(freeArm.position,freeElbow.position)
                    +Vector3.Distance(freeElbow.position,freeHand.position);
                reach=freeArm.position+Vector3.ClampMagnitude(reach-freeArm.position,armLength*0.94f);
                elbowPole=Vector3.down+freeSide*0.35f;
            }
            Solve(freeArm,freeElbow,freeHand,reach,elbowPole);
        }
        if(gripTarget!=null)
        {
            Transform arm=gripWithLeft?leftArm:rightArm, elbow=gripWithLeft?leftElbow:rightElbow, hand=gripWithLeft?leftHand:rightHand;
            Solve(arm,elbow,hand,gripTarget.position,-Forward+Vector3.down*0.4f);
        }
        if(gripPartner!=null)
        {
            Vector3 grip=(transform.position+gripPartner.position)*0.5f+Vector3.up*Height*0.72f;
            var partnerRig=gripPartner.GetComponent<Chapter1IncidentRig>();
            if(partnerRig!=null && partnerRig.Head!=null)
                grip.y=(head.position.y+partnerRig.Head.position.y)*0.5f-Mathf.Min(Height,partnerRig.Height)*0.20f;
            if(StationaryGrip.HasValue)grip=StationaryGrip.Value;
            Transform arm=gripWithLeft?leftArm:rightArm, elbow=gripWithLeft?leftElbow:rightElbow, hand=gripWithLeft?leftHand:rightHand;
            Solve(arm,elbow,hand,grip,Vector3.down);
        }
        if(pointTarget!=null)
        {
            Vector3 dir=Vector3.ProjectOnPlane(pointTarget.position-transform.position,Vector3.up).normalized;
            Solve(rightArm,rightElbow,rightHand,Vector3.Lerp(rightHand.position,rightArm.position+dir*Height*0.27f,pointProgress),Vector3.down);
        }
        if(police && batonVisible && !strike && pointTarget==null)
        {
            // Separate the held baton from the dark trouser silhouette.
            Vector3 side=batonInLeftHand?-right:right;
            Transform arm=batonInLeftHand?leftArm:rightArm,elbow=batonInLeftHand?leftElbow:rightElbow,hand=batonInLeftHand?leftHand:rightHand;
            Vector3 hold=arm.position-Vector3.up*Height*0.30f+side*Height*0.09f+Forward*Height*0.055f;
            if(relaxedPoliceWalk)
            {
                float handSwing=WalkFootOffset(phase+(batonInLeftHand?0f:Mathf.PI));
                hold=arm.position-Vector3.up*Height*0.30f+side*Height*0.045f
                    +Forward*Height*(0.025f-handSwing*0.065f*locomotionWeight);
            }
            Solve(arm,elbow,hand,hold,-Forward+side*0.4f);
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
        if(conversationTarget != null && speakingWeight > 0f && stumbleWeight <= 0f)
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
        UpdateBaton();
    }
    void ApplyLeg(Transform thigh,Transform calf,Transform foot,Vector3 ankle,Quaternion rotation,float p)
    {
        Vector3 target=transform.TransformPoint(ankle);
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
            float lift=cycle<0.6f?0f:Mathf.Sin((cycle-0.6f)/0.4f*Mathf.PI)*Height*(relaxedPoliceWalk?0.032f:0.045f);
            target+=(Forward*offset+Vector3.up*lift)*locomotionWeight;
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
        foot.rotation=transform.rotation*rotation;
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
        return cycle<0.6f?Mathf.Lerp(1f,-1f,cycle/0.6f)
            :Mathf.Lerp(-1f,1f,Mathf.SmoothStep(0f,1f,(cycle-0.6f)/0.4f));
    }
    public void BeginDepartureWalk(float cycleOffset)
    {
        relaxedPoliceWalk=true;
        smoothLocomotion=true;
        strideScale=0.72f;
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
        b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;
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
        if(pointTarget!=null)direction=Vector3.Slerp(Vector3.down,Vector3.ProjectOnPlane(pointTarget.position-transform.position,Vector3.up).normalized,pointProgress);
        Transform batonHand=batonInLeftHand?leftHand:rightHand;
        baton.transform.SetPositionAndRotation(batonHand.position+direction*Height*0.14f,Quaternion.FromToRotation(Vector3.up,direction));
    }
    void OnDestroy(){if(baton!=null)Destroy(baton);if(batonMaterial!=null)Destroy(batonMaterial);}
}
