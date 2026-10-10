using UnityEngine;
using UnityEngine.UI;

// A short, local greeting. Only an explicit reply opens the LLM conversation.
public sealed class Chapter2GreetingBubble : MonoBehaviour
{
    public Chapter2AmbientNPC Npc { get; private set; }
    public Button ReplyButton { get; private set; }
    public string Greeting { get; private set; }
    public bool Visible => gameObject.activeSelf;
    RectTransform rect, canvasRect;
    Text words;
    Chapter2Exploration owner;
    Chapter2Actor actor;
    float nextGreeting;
    static readonly string[] Greetings = { "早安！", "你好！" };

    public static Chapter2GreetingBubble Create(Chapter2Exploration owner, Chapter2AmbientNPC npc)
    {
        var go = new GameObject("Greeting · " + Chapter2Exploration.DisplayName(npc), typeof(RectTransform), typeof(CanvasRenderer), typeof(Chapter2BubbleGraphic));
        var bubble = go.AddComponent<Chapter2GreetingBubble>();
        bubble.owner = owner; bubble.Npc = npc; bubble.actor = npc.GetComponent<Chapter2Actor>();
        bubble.rect = go.GetComponent<RectTransform>();
        bubble.canvasRect = owner.chapter.ui.canvas.GetComponent<RectTransform>();
        bubble.rect.SetParent(bubble.canvasRect, false);
        bubble.rect.pivot = new Vector2(.5f, 0); bubble.rect.sizeDelta = new Vector2(252, 130);
        var graphic = go.GetComponent<Chapter2BubbleGraphic>(); graphic.color = new Color(.98f,.96f,.87f); graphic.raycastTarget = false;
        var name = bubble.Label(bubble.actor.DisplayName, 21, new Vector2(.07f,.73f),new Vector2(.93f,.96f));
        name.color = new Color(.38f,.30f,.20f);
        bubble.words = bubble.Label("", 34, new Vector2(.08f,.29f),new Vector2(.92f,.74f));
        var reply = new GameObject("Reply E",typeof(RectTransform),typeof(Image),typeof(Button));
        var r = reply.GetComponent<RectTransform>(); r.SetParent(bubble.rect,false);
        r.anchorMin=new Vector2(.56f,.07f);r.anchorMax=new Vector2(.94f,.32f);r.offsetMin=r.offsetMax=Vector2.zero;
        reply.GetComponent<Image>().color = new Color(.86f,.79f,.60f);
        bubble.ReplyButton = reply.GetComponent<Button>();bubble.ReplyButton.targetGraphic=reply.GetComponent<Image>();
        bubble.ReplyButton.onClick.AddListener(()=>owner.TryOpen(npc));
        var label=bubble.Label("回覆  E",21,Vector2.zero,Vector2.one);
        label.rectTransform.SetParent(r,false);label.alignment=TextAnchor.MiddleCenter;
        go.SetActive(false);return bubble;
    }
    Text Label(string value,int size,Vector2 min,Vector2 max)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(rect,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=go.GetComponent<Text>();t.font=owner.chapter.ui.font;t.fontSize=size;t.text=value;t.color=new Color(.16f,.13f,.09f);
        t.supportRichText=false;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
    }
    public bool PlaceAndShow(bool nearby)
    {
        if(!nearby || !Npc || !Npc.isActiveAndEnabled){gameObject.SetActive(false);return false;}
        var camera=owner.chapter.player.view;
        Vector3 head=actor.Rig && actor.Rig.Head ? actor.Rig.Head.position : Npc.transform.position+Vector3.up*(actor.isChild?1.05f:1.8f);
        Vector3 v=camera.WorldToViewportPoint(head+Vector3.up*.18f);
        bool visible=v.z>camera.nearClipPlane && v.x>0 && v.x<1 && v.y>0 && v.y<1;
        if(visible)
        {
            var delta=head-camera.transform.position;
            foreach(var hit in Physics.RaycastAll(camera.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponentInParent<Chapter2Actor>() || hit.collider.transform.IsChildOf(owner.chapter.player.transform))continue;
                visible=false;break;
            }
        }
        if(!visible){gameObject.SetActive(false);return false;}
        if(!gameObject.activeSelf && Time.unscaledTime>=nextGreeting)
        { Greeting=Greetings[Random.Range(0,Greetings.Length)];words.text=Greeting;nextGreeting=Time.unscaledTime+8;Npc.Greet(owner.chapter.player.transform); }
        float width=Mathf.Max(400,canvasRect.rect.width),height=Mathf.Max(300,canvasRect.rect.height);
        float half=rect.sizeDelta.x*.5f/width;
        rect.anchorMin=rect.anchorMax=new Vector2(Mathf.Clamp(v.x,half+.015f,1-half-.015f),Mathf.Clamp(v.y,.08f,.98f-rect.sizeDelta.y/height));
        rect.anchoredPosition=Vector2.zero;
        gameObject.SetActive(true);return true;
    }
}

// Rounded cream card with a small speech tail; generated as UI geometry, no image asset.
public sealed class Chapter2BubbleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();
        AddShape(vh,r,new Color(.20f,.17f,.12f,1),13,14);
        r.xMin+=3;r.xMax-=3;r.yMin+=3;r.yMax-=3;
        AddShape(vh,r,color,10,12);
    }
    static void AddShape(VertexHelper vh,Rect r,Color tint,float radius,float tail)
    {
        int center=vh.currentVertCount;vh.AddVert(r.center,tint,Vector2.zero);
        for(int corner=0;corner<4;corner++)
        {
            Vector2 c=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
            for(int step=0;step<=6;step++)
            {
                float angle=(corner*90+step*15)*Mathf.Deg2Rad;
                vh.AddVert(c+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);
            }
        }
        int count=28;for(int i=0;i<count;i++)vh.AddTriangle(center,center+1+i,center+1+(i+1)%count);
        int start=vh.currentVertCount;
        vh.AddVert(new Vector3(r.center.x-13,r.yMin+2),tint,Vector2.zero);
        vh.AddVert(new Vector3(r.center.x+13,r.yMin+2),tint,Vector2.zero);
        vh.AddVert(new Vector3(r.center.x,r.yMin-tail),tint,Vector2.zero);vh.AddTriangle(start,start+1,start+2);
    }
}
