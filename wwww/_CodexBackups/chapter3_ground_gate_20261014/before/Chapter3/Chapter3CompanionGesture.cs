using UnityEngine;

// A readable pause to tighten the carrying strap; uses the existing rig's arm solver.
[DefaultExecutionOrder(4100)]
public sealed class Chapter3CompanionGesture:MonoBehaviour
{
    Chapter2Actor actor;
    Transform target;
    float began,ends;
    void Awake(){actor=GetComponent<Chapter2Actor>();target=new GameObject("整理背帶的手部目標").transform;target.SetParent(transform.parent,false);}
    public void AdjustStrap(float seconds){began=Time.time;ends=began+seconds;}
    void Update()
    {
        if(!actor||!actor.Rig)return;
        actor.Rig.smoothLocomotion=true;
        if(Time.time>=ends){if(actor.Rig.gripTarget==target)actor.Rig.gripTarget=null;return;}
        float weight=Mathf.Min(Mathf.Clamp01((Time.time-began)/.6f),Mathf.Clamp01((ends-Time.time)/.6f));
        Vector3 right=Vector3.Cross(Vector3.up,actor.Rig.Forward);
        Vector3 chest=actor.transform.position+Vector3.up*(1.16f+Mathf.Sin(Time.time*4)*.018f)+actor.Rig.Forward*.20f+right*.06f;
        target.position=Vector3.Lerp(actor.Rig.RightHand.position,chest,weight);
        actor.Rig.gripWithLeft=false;actor.Rig.gripTarget=target;
    }
    void OnDisable(){if(actor&&actor.Rig&&actor.Rig.gripTarget==target)actor.Rig.gripTarget=null;}
    void OnDestroy(){if(target)Destroy(target.gameObject);}
}
