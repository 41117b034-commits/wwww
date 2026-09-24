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
    public Transform gripTarget;
    public bool gripWithLeft = true;
    public bool resisting;
    public bool strike;
    public float strikeProgress;
    GameObject baton;
    Material batonMaterial;
    public bool batonVisible;
    public bool batonInLeftHand;
    public Transform gripPartner;
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

    public void Initialize(Animator source, bool isPolice)
    {
        if (ready) return;
        animator=source;police=isPolice;
        if(animator==null||!animator.isHuman)return;
        animator.Update(0f);
        hips=B(HumanBodyBones.Hips);head=B(HumanBodyBones.Head);
        leftThigh=B(HumanBodyBones.LeftUpperLeg);rightThigh=B(HumanBodyBones.RightUpperLeg);
        leftCalf=B(HumanBodyBones.LeftLowerLeg);rightCalf=B(HumanBodyBones.RightLowerLeg);
        leftFoot=B(HumanBodyBones.LeftFoot);rightFoot=B(HumanBodyBones.RightFoot);
        leftArm=B(HumanBodyBones.LeftUpperArm);rightArm=B(HumanBodyBones.RightUpperArm);
        leftElbow=B(HumanBodyBones.LeftLowerArm);rightElbow=B(HumanBodyBones.RightLowerArm);
        leftHand=B(HumanBodyBones.LeftHand);rightHand=B(HumanBodyBones.RightHand);
        if(hips==null||head==null||leftFoot==null||rightFoot==null)return;
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
    Transform B(HumanBodyBones b)=>animator.GetBoneTransform(b);
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
        ApplyLeg(leftThigh,leftCalf,leftFoot,leftAnkle,leftFootRotation,phase);
        ApplyLeg(rightThigh,rightCalf,rightFoot,rightAnkle,rightFootRotation,phase+Mathf.PI);
        if(locomotionWeight > 0f)
        {
            float swing=Mathf.Sin(phase)*14f*strideScale*locomotionWeight;
            leftArm.rotation=Quaternion.AngleAxis(swing,right)*leftArm.rotation;
            rightArm.rotation=Quaternion.AngleAxis(-swing,right)*rightArm.rotation;
        }
        if(resisting)
        {
            float tremble=frozen?0f:Mathf.Sin(Time.time*7f)*2f;
            var spine=B(HumanBodyBones.Spine);
            if(spine!=null)spine.rotation=Quaternion.AngleAxis(-8f+tremble,right)*spine.rotation;
            Solve(leftArm,leftElbow,leftHand,leftArm.position+Forward*Height*0.23f+right*Height*0.04f,-Forward);
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
            float offset=cycle<0.6f?Mathf.Lerp(stride,-stride,cycle/0.6f):Mathf.Lerp(-stride,stride,Mathf.SmoothStep(0,1,(cycle-0.6f)/0.4f));
            float lift=cycle<0.6f?0f:Mathf.Sin((cycle-0.6f)/0.4f*Mathf.PI)*Height*0.045f;
            target+=(Forward*offset+Vector3.up*lift)*locomotionWeight;
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
        if(pointTarget!=null)direction=Vector3.Slerp(Vector3.down,Vector3.ProjectOnPlane(pointTarget.position-transform.position,Vector3.up).normalized,pointProgress);
        Transform batonHand=batonInLeftHand?leftHand:rightHand;
        baton.transform.SetPositionAndRotation(batonHand.position+direction*Height*0.14f,Quaternion.FromToRotation(Vector3.up,direction));
    }
    void OnDestroy(){if(baton!=null)Destroy(baton);if(batonMaterial!=null)Destroy(batonMaterial);}
}
