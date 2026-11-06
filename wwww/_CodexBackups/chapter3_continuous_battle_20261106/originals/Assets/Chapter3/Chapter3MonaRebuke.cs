using UnityEngine;

// Chapter-3-only pose/expression override, after the shared speech face has applied its mouth.
[DefaultExecutionOrder(4510)]
public sealed class Chapter3MonaRebuke : MonoBehaviour
{
    public bool Rebuking {get;set;}
    public Transform listener;
    Chapter2Actor actor;
    Chapter2SpeechFace face;
    float weight;
    void Awake(){actor=GetComponent<Chapter2Actor>();face=GetComponent<Chapter2SpeechFace>();}
    void Update()
    {
        if(!actor.Rig)return;
        if(Rebuking&&listener)actor.Face(listener.position);
        actor.Rig.pointTarget=Rebuking?listener:null;
        actor.Rig.pointProgress=Mathf.MoveTowards(actor.Rig.pointProgress,Rebuking?1:0,Time.deltaTime*3);
    }
    void LateUpdate()
    {
        weight=Mathf.MoveTowards(weight,Rebuking?1:0,Time.deltaTime*3);
        if(weight<=0||!face||face.surfaces==null)return;
        foreach(var surface in face.surfaces)
        {
            if(!surface.renderer||!surface.renderer.sharedMesh)continue;
            int index=surface.renderer.sharedMesh.GetBlendShapeIndex(Chapter2SpeechFace.Angry);
            if(index>=0)surface.renderer.SetBlendShapeWeight(index,weight*100);
        }
    }
    void OnDisable(){Rebuking=false;weight=0;if(actor&&actor.Rig){actor.Rig.pointTarget=null;actor.Rig.pointProgress=0;}}
}
