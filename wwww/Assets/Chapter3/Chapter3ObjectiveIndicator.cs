using UnityEngine;
using UnityEngine.UI;

// A screen-edge direction remains visible when Tado and the ground trail are
// beside/behind the camera. It never changes the player's view or position.
[DefaultExecutionOrder(5000)]
public sealed class Chapter3ObjectiveIndicator : MonoBehaviour
{
    Chapter3SchoolController school;
    RectTransform root,pointer;
    Text label;
    int shownMetres=-1;
    public bool Visible=>root&&root.gameObject.activeSelf;
    public bool Offscreen {get;private set;}
    public Vector2 ScreenPosition=>root?root.anchoredPosition:Vector2.zero;
    public void Configure(Chapter3SchoolController owner)
    {
        school=owner;if(root)return;
        var go=new GameObject("達多會合方向提示",typeof(RectTransform));root=(RectTransform)go.transform;
        root.SetParent(school.ui.canvas.transform,false);root.sizeDelta=new Vector2(240,120);
        root.SetSiblingIndex(school.ui.fade.transform.GetSiblingIndex());
        var arrow=new GameObject("黃色方向箭頭",typeof(RectTransform),typeof(CanvasRenderer),typeof(Chapter3ObjectivePointer),typeof(Outline));
        pointer=(RectTransform)arrow.transform;pointer.SetParent(root,false);pointer.sizeDelta=new Vector2(76,76);pointer.anchoredPosition=new Vector2(0,22);
        arrow.GetComponent<Chapter3ObjectivePointer>().raycastTarget=false;
        var outline=arrow.GetComponent<Outline>();outline.effectColor=new Color(.06f,.04f,0,.95f);outline.effectDistance=new Vector2(2,-2);
        var plate=new GameObject("目標距離底板",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)plate.transform;rect.SetParent(root,false);rect.sizeDelta=new Vector2(230,40);rect.anchoredPosition=new Vector2(0,-38);
        var image=plate.GetComponent<Image>();image.color=new Color(.035f,.045f,.025f,.88f);image.raycastTarget=false;
        var text=new GameObject("達多距離",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
        var tr=(RectTransform)text.transform;tr.SetParent(rect,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
        label=text.GetComponent<Text>();label.font=school.ui.hint.font;label.fontSize=26;label.alignment=TextAnchor.MiddleCenter;label.color=Chapter3YellowArrow.Color;label.raycastTarget=false;
        root.gameObject.SetActive(false);
    }
    void LateUpdate()
    {
        bool show=school&&school.Exploring&&!school.MetTado&&school.flow.Region=="School"&&
            (!school.Battle||!school.Battle.OwnsFlow)&&!school.ui.chat.activeInHierarchy;
        if(!root)return;root.gameObject.SetActive(show);if(!show)return;
        var view=school.player.view;var canvas=(RectTransform)school.ui.canvas.transform;
        Vector3 target=school.tado.transform.position+Vector3.up*1.9f;
        Vector3 screen=view.WorldToViewportPoint(target);
        Vector2 point=new Vector2((screen.x-.5f)*canvas.rect.width,(screen.y-.5f)*canvas.rect.height);
        float xLimit=Mathf.Max(50,canvas.rect.width*.5f-155),yTop=Mathf.Max(50,canvas.rect.height*.5f-230),yBottom=Mathf.Max(50,canvas.rect.height*.5f-160);
        Offscreen=screen.z<=0||Mathf.Abs(point.x)>xLimit||point.y>yTop||point.y< -yBottom;
        if(screen.z<=0)
        {
            float side=view.transform.InverseTransformPoint(target).x;
            point=new Vector2(side>0?xLimit:-xLimit,0);
        }
        if(Offscreen)
        {
            if(point.sqrMagnitude<1)point=Vector2.left;
            float scale=Mathf.Min(xLimit/Mathf.Max(1,Mathf.Abs(point.x)),(point.y>=0?yTop:yBottom)/Mathf.Max(1,Mathf.Abs(point.y)));
            root.anchoredPosition=point*Mathf.Min(1,scale);
            pointer.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down,point));
        }
        else{root.anchoredPosition=point;pointer.localRotation=Quaternion.identity;}
        pointer.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*4)*.08f);
        int metres=Mathf.CeilToInt(Vector3.ProjectOnPlane(school.tado.transform.position-school.player.transform.position,Vector3.up).magnitude);
        if(metres!=shownMetres){shownMetres=metres;label.text="達多　"+metres+" m";}
    }
    void OnDisable(){if(root)root.gameObject.SetActive(false);}
    void OnDestroy(){if(root)Destroy(root.gameObject);}
}
public sealed class Chapter3ObjectivePointer : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper helper){Chapter3YellowArrow.PopulateUi(helper,68);}
}
