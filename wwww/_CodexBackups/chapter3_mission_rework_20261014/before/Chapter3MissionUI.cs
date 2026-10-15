using UnityEngine;
using UnityEngine.UI;

// Uses the chapter's existing camera canvas/font, including its VR layout.
public sealed class Chapter3MissionUI : MonoBehaviour
{
    public GameObject choice, result;
    public Button bawan, pihu, retry, replay;
    public Text resultTitle, resultBody;
    public static Chapter3MissionUI Build(Chapter3Presentation presentation)
    {
        var root=new GameObject("學校任務選擇與檢查點",typeof(RectTransform));
        root.transform.SetParent(presentation.canvas.transform,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var ui=root.AddComponent<Chapter3MissionUI>();var font=presentation.subtitle.font;
        var choice=Panel(root.transform,"分工",new Vector2(.12f,.23f),new Vector2(.88f,.77f));ui.choice=choice.gameObject;
        Label(choice,"選擇你要承擔的任務",font,34,new Vector2(.045f,.80f),new Vector2(.955f,.96f));
        Label(choice,"兩隊分頭行動，在校後空地匯合。",font,24,new Vector2(.045f,.68f),new Vector2(.955f,.79f));
        ui.bawan=Button(choice,"1  跟巴萬突破",font,new Vector2(.04f,.42f),new Vector2(.48f,.63f));
        ui.pihu=Button(choice,"2  跟比胡掩護",font,new Vector2(.52f,.42f),new Vector2(.96f,.63f));
        Label(choice,"清除守衛 → 靠近路障\n按住 E 拆開橫木，打通西側通道。",font,25,new Vector2(.055f,.09f),new Vector2(.475f,.39f));
        Label(choice,"觀察火線 → E 下令前進／撤回\n保護比胡與達多穿越兩處空隙。",font,25,new Vector2(.535f,.09f),new Vector2(.955f,.39f));
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
