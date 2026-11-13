using UnityEngine;
using UnityEngine.UI;

// Uses the chapter's existing camera canvas/font, including its VR layout.
public sealed class Chapter3MissionUI : MonoBehaviour
{
    public GameObject choice, result, actionPrompt;
    public Button tado, bawan, pihu, retry, replay;
    public Text resultTitle, resultBody, actionKey, actionTitle, actionDetail;
    public static Chapter3MissionUI Build(Chapter3Presentation presentation)
    {
        var root=new GameObject("學校任務選擇與檢查點",typeof(RectTransform));
        root.transform.SetParent(presentation.canvas.transform,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var ui=root.AddComponent<Chapter3MissionUI>();var font=presentation.subtitle.font;
        var choice=Panel(root.transform,"分工",new Vector2(.12f,.32f),new Vector2(.88f,.70f));ui.choice=choice.gameObject;
        Label(choice,"選擇你要跟隨的同伴",font,34,new Vector2(.04f,.73f),new Vector2(.96f,.95f));
        ui.tado=Button(choice,"1  跟隨達多",font,new Vector2(.035f,.31f),new Vector2(.335f,.64f));
        ui.bawan=Button(choice,"2  跟隨巴萬",font,new Vector2(.35f,.31f),new Vector2(.65f,.64f));
        ui.pihu=Button(choice,"3  跟隨比胡",font,new Vector2(.665f,.31f),new Vector2(.965f,.64f));
        Label(choice,"按 1／2／3，或點選同伴。",font,24,new Vector2(.04f,.05f),new Vector2(.96f,.24f));
        var action=Panel(root.transform,"目前操作提示",new Vector2(.29f,.32f),new Vector2(.79f,.46f));ui.actionPrompt=action.gameObject;
        action.GetComponent<Image>().raycastTarget=false;
        var key=Panel(action,"按鍵",new Vector2(.025f,.18f),new Vector2(.15f,.82f));
        key.GetComponent<Image>().color=new Color(.78f,.64f,.32f,1);key.GetComponent<Image>().raycastTarget=false;
        ui.actionKey=Label(key,"E",font,44,Vector2.zero,Vector2.one);ui.actionKey.alignment=TextAnchor.MiddleCenter;
        ui.actionKey.color=new Color(.08f,.10f,.07f);
        ui.actionTitle=Label(action,"",font,36,new Vector2(.18f,.43f),new Vector2(.97f,.94f));
        ui.actionDetail=Label(action,"",font,24,new Vector2(.18f,.06f),new Vector2(.97f,.43f));
        var end=Panel(root.transform,"任務結果",new Vector2(.2f,.25f),new Vector2(.8f,.75f));ui.result=end.gameObject;
        ui.resultTitle=Label(end,"",font,34,new Vector2(.06f,.75f),new Vector2(.94f,.95f));
        ui.resultBody=Label(end,"",font,27,new Vector2(.06f,.30f),new Vector2(.94f,.72f));
        ui.retry=Button(end,"R  重試目前任務",font,new Vector2(.06f,.08f),new Vector2(.49f,.25f));
        ui.replay=Button(end,"從頭重玩第三章",font,new Vector2(.53f,.08f),new Vector2(.94f,.25f));
        ui.Hide();return ui;
    }
    public void Hide(){choice.SetActive(false);result.SetActive(false);HideAction();}
    public void HideAction(){actionPrompt.SetActive(false);}
    public void ShowAction(string key,string title,string detail)
    {actionKey.text=key;actionTitle.text=title;actionDetail.text=detail;actionPrompt.SetActive(true);}
    public void ShowResult(string title,string body,bool failed)
    {choice.SetActive(false);HideAction();result.SetActive(true);resultTitle.text=title;resultBody.text=body;retry.gameObject.SetActive(failed);replay.gameObject.SetActive(!failed);}
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
