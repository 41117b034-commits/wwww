using UnityEngine;
using UnityEngine.UI;

// Chapter 1's shaft/two-wing arrow projected into UI, including offscreen guidance.
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
            arrow.rectTransform.sizeDelta=new Vector2(48,64);arrow.raycastTarget=false;
            go.transform.SetSiblingIndex(ui.fade.transform.GetSiblingIndex());
        }
        arrow.Target=target;arrow.color=Chapter3YellowArrow.Color;arrow.gameObject.SetActive(true);
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
            rectTransform.anchoredPosition=point+Vector2.up*38;
            rectTransform.localRotation=Quaternion.identity;
        }
        rectTransform.localScale=Vector3.one*Chapter3YellowArrow.PulseScale(Time.unscaledTime);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        Chapter3YellowArrow.PopulateUi(vh,48);
    }
}
