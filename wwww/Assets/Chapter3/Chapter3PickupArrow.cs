using UnityEngine;
using UnityEngine.UI;

// A crisp, outlined yellow marker stays legible in sunlight and points back on screen.
public sealed class Chapter3PickupArrow : MaskableGraphic
{
    public Transform Target {get;private set;}
    Chapter3Presentation presentation;
    public static void Show(Chapter3Presentation ui,Transform target)
    {
        var arrow=ui.GetComponentInChildren<Chapter3PickupArrow>(true);
        if(!arrow)
        {
            var go=new GameObject("黃色武器指引箭頭",typeof(RectTransform),typeof(CanvasRenderer));
            go.transform.SetParent(ui.canvas.transform,false);
            arrow=go.AddComponent<Chapter3PickupArrow>();arrow.presentation=ui;
            arrow.rectTransform.sizeDelta=new Vector2(42,58);arrow.raycastTarget=false;
            go.transform.SetSiblingIndex(ui.fade.transform.GetSiblingIndex());
        }
        arrow.Target=target;arrow.gameObject.SetActive(true);
    }
    public static void Hide(Chapter3Presentation ui)
    {var a=ui.GetComponentInChildren<Chapter3PickupArrow>(true);if(a){a.Target=null;a.gameObject.SetActive(false);}}
    void LateUpdate()
    {
        if(!Target||!Target.gameObject.activeInHierarchy){gameObject.SetActive(false);return;}
        var camera=presentation.canvas.worldCamera;var canvas=(RectTransform)presentation.canvas.transform;
        Vector3 screen=camera.WorldToViewportPoint(Target.position+Vector3.up*.25f);
        Vector2 point=new Vector2((screen.x-.5f)*canvas.rect.width,(screen.y-.5f)*canvas.rect.height);
        if(screen.z<0)point=-point;
        bool off=screen.z<0||screen.x<.045f||screen.x>.955f||screen.y<.10f||screen.y>.89f;
        if(off)
        {
            if(point.sqrMagnitude<1)point=Vector2.left;
            float scale=Mathf.Min((canvas.rect.width*.5f-48)/Mathf.Max(1,Mathf.Abs(point.x)),(canvas.rect.height*.5f-95)/Mathf.Max(1,Mathf.Abs(point.y)));
            rectTransform.anchoredPosition=point*scale;
            rectTransform.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down,point));
        }
        else
        {
            rectTransform.anchoredPosition=point+Vector2.up*(38+Mathf.Sin(Time.unscaledTime*4)*5);
            rectTransform.localRotation=Quaternion.identity;
        }
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Shape(vh,1.12f,new Color32(31,24,6,255));Shape(vh,.86f,new Color32(255,220,25,255));
    }
    static void Shape(VertexHelper vh,float scale,Color32 tint)
    {
        int start=vh.currentVertCount;
        Vector2[] points={new Vector2(-7,27),new Vector2(7,27),new Vector2(7,0),new Vector2(19,0),new Vector2(0,-25),new Vector2(-19,0),new Vector2(-7,0)};
        foreach(var p in points)vh.AddVert(p*scale,tint,Vector2.zero);
        vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+6);
        vh.AddTriangle(start+3,start+4,start+5);
    }
}
