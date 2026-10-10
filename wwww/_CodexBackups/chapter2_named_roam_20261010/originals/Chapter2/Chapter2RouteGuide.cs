using UnityEngine;
using UnityEngine.UI;

// The same yellow, three-part arrow language as Chapter 1, scoped to Chapter 2.
public sealed class Chapter2RouteGuide : MonoBehaviour
{
    public Chapter2Controller chapter;
    public Material yellow;
    public bool EscortArrived { get; set; }
    public bool GuidanceEnabled { get; set; } = true;
    public float leftBoundary = -3.2f, rightBoundary = .65f;
    public Text distanceLabel;
    public float DistanceMetres { get; private set; }
    public int VisibleArrows { get; private set; }
    public bool RouteBlocked { get; private set; }
    Transform[] arrows;
    Transform marker;
    Material ownedMaterial;
    Vector3[] route;
    float nextRouteUpdate;
    public Vector3 Target => EscortArrived ? chapter.treeApproach.position : chapter.workers[0].transform.position;
    bool Following => chapter && GuidanceEnabled && chapter.CurrentStage==Chapter2Controller.Stage.Follow;
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
        if(show && chapter.forestEscort && Time.unscaledTime>=nextRouteUpdate)
        {
            nextRouteUpdate=Time.unscaledTime+.35f;
            chapter.forestEscort.TryPath(from,to,out route);
        }
        if(!show)route=null;
        float routeLength=0;
        if(route!=null)for(int i=1;i<route.Length;i++)routeLength+=Vector3.Distance(route[i-1],route[i]);
        else routeLength=distance;
        DistanceMetres=distance;
        if(distanceLabel)
        {
            distanceLabel.gameObject.SetActive(show);
            distanceLabel.text=(EscortArrived?"前往巨木":"跟隨族人")+$"  {distance:0.0} 公尺";
            Vector3 point=chapter.player.view.WorldToViewportPoint(to+Vector3.up*(EscortArrived?.9f:2.5f));
            if(point.z<0)point.x=point.x<.5f?.9f:.1f;
            var anchor=new Vector2(Mathf.Clamp(point.x,.15f,.85f),Mathf.Clamp(point.y,.34f,.77f));
            distanceLabel.rectTransform.anchorMin=distanceLabel.rectTransform.anchorMax=anchor;
        }
        VisibleArrows=show?Mathf.Clamp(Mathf.CeilToInt((routeLength-.8f)/1.7f),0,arrows.Length):0;
        for(int i=0;i<arrows.Length;i++)
        {
            arrows[i].gameObject.SetActive(i<VisibleArrows);
            if(i>=VisibleArrows)continue;
            float along=Mathf.Lerp(.85f,Mathf.Max(.85f,routeLength-.55f),VisibleArrows==1?0:i/(float)(VisibleArrows-1));
            var p=from+direction*along;var facing=direction;
            if(route!=null && route.Length>1)
            {
                for(int part=1;part<route.Length;part++)
                {
                    var segment=route[part]-route[part-1];float length=segment.magnitude;
                    if(along<=length || part==route.Length-1){p=route[part-1]+segment.normalized*Mathf.Min(along,length);facing=Vector3.ProjectOnPlane(segment,Vector3.up).normalized;break;}
                    along-=length;
                }
            }
            p.y+=.075f;
            arrows[i].SetPositionAndRotation(p,Quaternion.LookRotation(facing.sqrMagnitude>.001f?facing:Vector3.forward));
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
        // Retrieval can start on either side of, or beyond, the tree. The former
        // northbound corridor could prevent following those paths. Physical world
        // collisions still apply, and the single guide waits if the player lags.
        return proposed;
    }
    void Part(Transform parent,string label,Vector3 position,Vector3 scale,float yaw)
    {
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name=label;p.transform.SetParent(parent,false);
        p.transform.localPosition=position;p.transform.localScale=scale;p.transform.localRotation=Quaternion.Euler(0,yaw,0);
        Destroy(p.GetComponent<Collider>());p.GetComponent<Renderer>().sharedMaterial=yellow;
    }
    void OnDestroy(){if(ownedMaterial)Destroy(ownedMaterial);}
}
