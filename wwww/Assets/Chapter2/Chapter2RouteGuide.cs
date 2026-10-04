using UnityEngine;

// The same yellow, three-part arrow language as Chapter 1, scoped to Chapter 2.
public sealed class Chapter2RouteGuide : MonoBehaviour
{
    public Chapter2Controller chapter;
    public Material yellow;
    public bool EscortArrived { get; set; }
    public int VisibleArrows { get; private set; }
    public bool RouteBlocked { get; private set; }
    Transform[] arrows;
    Transform marker;
    Material ownedMaterial;
    public Vector3 Target => EscortArrived ? chapter.treeApproach.position : chapter.workers[0].transform.position;
    bool Following => chapter && chapter.CurrentStage==Chapter2Controller.Stage.Follow;
    void Start()
    {
        if(!yellow){ownedMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ownedMaterial.color=new Color(1,.82f,.02f);yellow=ownedMaterial;}
        arrows=new Transform[12];
        for(int i=0;i<arrows.Length;i++)
        {
            var root=new GameObject("Chapter2 yellow ground arrow "+i).transform;root.SetParent(transform,false);arrows[i]=root;
            Part(root,"Shaft",new Vector3(0,0,-.16f),new Vector3(.22f,.045f,.76f),0);
            Part(root,"Head left",new Vector3(-.15f,0,.31f),new Vector3(.2f,.05f,.55f),45);
            Part(root,"Head right",new Vector3(.15f,0,.31f),new Vector3(.2f,.05f,.55f),-45);
        }
        marker=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;marker.name="Follow this villager · yellow marker";marker.SetParent(transform,false);
        Destroy(marker.GetComponent<Collider>());marker.GetComponent<Renderer>().sharedMaterial=yellow;
    }
    void LateUpdate()
    {
        if(arrows==null||!chapter)return;
        bool show=Following || (chapter.CurrentStage==Chapter2Controller.Stage.Chopping&&!chapter.AtTree);
        Vector3 from=chapter.player.transform.position,to=Target;from.y=to.y=0;
        Vector3 direction=to-from;float distance=direction.magnitude;direction=direction.normalized;
        VisibleArrows=show?Mathf.Clamp(Mathf.CeilToInt((distance-.8f)/1.7f),0,arrows.Length):0;
        for(int i=0;i<arrows.Length;i++)
        {
            arrows[i].gameObject.SetActive(i<VisibleArrows);
            if(i>=VisibleArrows)continue;
            float along=Mathf.Lerp(.85f,Mathf.Max(.85f,distance-.55f),VisibleArrows==1?0:i/(float)(VisibleArrows-1));
            var p=from+direction*along;p.y=.075f;
            arrows[i].SetPositionAndRotation(p,Quaternion.LookRotation(direction));
            arrows[i].localScale=Vector3.one*(.72f+.045f*Mathf.Sin(Time.unscaledTime*4.5f-i*.45f));
        }
        marker.gameObject.SetActive(Following);
        if(Following)
        {
            marker.position=to+Vector3.up*((EscortArrived?.5f:2.15f)+.09f*Mathf.Sin(Time.unscaledTime*3));
            marker.rotation=Quaternion.Euler(0,Time.unscaledTime*60,45);marker.localScale=Vector3.one*.24f;
        }
    }
    public Vector3 Constrain(Vector3 current,Vector3 proposed)
    {
        RouteBlocked=false;
        if(!Following)return proposed;
        // A walking corridor, not camera control: players can still turn and look around.
        float furthest=EscortArrived?chapter.treeApproach.position.z+.65f:chapter.workers[0].transform.position.z+1.1f;
        var limited=proposed;limited.x=Mathf.Clamp(limited.x,-3.2f,3.2f);limited.z=Mathf.Clamp(limited.z,-18,furthest);
        RouteBlocked=(limited-proposed).sqrMagnitude>.000001f;
        if(RouteBlocked)chapter.ui.hint.text="請留在隊伍的小徑上，沿黃色箭頭跟上族人。";
        return limited;
    }
    void Part(Transform parent,string label,Vector3 position,Vector3 scale,float yaw)
    {
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name=label;p.transform.SetParent(parent,false);
        p.transform.localPosition=position;p.transform.localScale=scale;p.transform.localRotation=Quaternion.Euler(0,yaw,0);
        Destroy(p.GetComponent<Collider>());p.GetComponent<Renderer>().sharedMaterial=yellow;
    }
    void OnDestroy(){if(ownedMaterial)Destroy(ownedMaterial);}
}
