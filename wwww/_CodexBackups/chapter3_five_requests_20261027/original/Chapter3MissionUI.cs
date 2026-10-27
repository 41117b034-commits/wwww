using UnityEngine;
using UnityEngine.UI;

// Uses the chapter's existing camera canvas/font, including its VR layout.
public sealed class Chapter3MissionUI : MonoBehaviour
{
    public GameObject choice, result;
    public Button tado, bawan, pihu, retry, replay;
    public Text resultTitle, resultBody;
    public static Chapter3MissionUI Build(Chapter3Presentation presentation)
    {
        var root=new GameObject("學校任務選擇與檢查點",typeof(RectTransform));
        root.transform.SetParent(presentation.canvas.transform,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var ui=root.AddComponent<Chapter3MissionUI>();var font=presentation.subtitle.font;
        var choice=Panel(root.transform,"分工",new Vector2(.08f,.20f),new Vector2(.92f,.80f));ui.choice=choice.gameObject;
        Label(choice,"選擇你要跟隨的同伴",font,34,new Vector2(.04f,.83f),new Vector2(.96f,.97f));
        Label(choice,"跟緊同伴，彼此照應；離開會場後在校後匯合。",font,24,new Vector2(.04f,.72f),new Vector2(.96f,.83f));
        ui.tado=Button(choice,"1  跟達多進入會場",font,new Vector2(.035f,.48f),new Vector2(.335f,.69f));
        ui.bawan=Button(choice,"2  跟巴萬打通側門",font,new Vector2(.35f,.48f),new Vector2(.65f,.69f));
        ui.pihu=Button(choice,"3  跟比胡照應穿越",font,new Vector2(.665f,.48f),new Vector2(.965f,.69f));
        Label(choice,"跟上達多，穿過混亂的會場。\n吹哨通知全體族人撤退。",font,25,new Vector2(.045f,.16f),new Vector2(.325f,.45f));
        Label(choice,"移開門閂，再協助巴萬開門。\n照應同伴通過學校側邊。",font,25,new Vector2(.36f,.16f),new Vector2(.64f,.45f));
        Label(choice,"看住通道，照應比胡穿越。\nE 示意前進／撤回。",font,25,new Vector2(.675f,.16f),new Vector2(.955f,.45f));
        Label(choice,"沿用你攜帶的獵刀或步槍。",font,22,new Vector2(.04f,.035f),new Vector2(.96f,.13f));
        var end=Panel(root.transform,"任務結果",new Vector2(.2f,.25f),new Vector2(.8f,.75f));ui.result=end.gameObject;
        ui.resultTitle=Label(end,"",font,34,new Vector2(.06f,.75f),new Vector2(.94f,.95f));
        ui.resultBody=Label(end,"",font,27,new Vector2(.06f,.30f),new Vector2(.94f,.72f));
        ui.retry=Button(end,"R  重試目前任務",font,new Vector2(.06f,.08f),new Vector2(.49f,.25f));
        ui.replay=Button(end,"重新選擇任務",font,new Vector2(.53f,.08f),new Vector2(.94f,.25f));
        ui.Hide();return ui;
    }
    public void Hide(){choice.SetActive(false);result.SetActive(false);}
    public void ShowResult(string title,string body,bool failed)
    {choice.SetActive(false);result.SetActive(true);resultTitle.text=title;resultBody.text=body;retry.gameObject.SetActive(failed);replay.gameObject.SetActive(!failed);}
    static RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        g.GetComponent<Image>().color=new Color(.025f,.045f,.038f,.97f);return r;
    }
    static Text Label(Transform parent,string text,Font font,int size,Vector2 min,Vector2 max)
    {
        var g=new GameObject("文字",typeof(RectTransform),typeof(Text));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=new Color(.95f,.88f,.70f);t.raycastTarget=false;
        t.alignment=TextAnchor.MiddleLeft;t.supportRichText=false;return t;
    }
    static Button Button(Transform parent,string text,Font font,Vector2 min,Vector2 max)
    {
        var r=Panel(parent,text,min,max);r.GetComponent<Image>().color=new Color(.26f,.33f,.23f,1);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        Label(r,text,font,27,new Vector2(.04f,0),new Vector2(.96f,1)).alignment=TextAnchor.MiddleCenter;return b;
    }
}
