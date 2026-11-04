using UnityEngine;
using UnityEngine.UI;

// Keep display history separate from the model's bounded context window.
public sealed class Chapter3ConversationHistory : MonoBehaviour
{
    public ScrollRect Scroll { get; private set; }
    Text transcript;
    public static Chapter3ConversationHistory Get(Chapter3Presentation ui)
    {
        var history=ui.chat.GetComponent<Chapter3ConversationHistory>();
        if(history)return history;
        history=ui.chat.AddComponent<Chapter3ConversationHistory>();
        var viewport=new GameObject("可捲動問答紀錄",typeof(RectTransform),typeof(Image),typeof(RectMask2D));
        var rect=(RectTransform)viewport.transform;rect.SetParent(ui.chat.transform,false);
        rect.anchorMin=new Vector2(.035f,.37f);rect.anchorMax=new Vector2(.965f,.81f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        viewport.GetComponent<Image>().color=new Color(.01f,.025f,.02f,.35f);
        history.Scroll=viewport.AddComponent<ScrollRect>();history.Scroll.viewport=rect;
        history.Scroll.horizontal=false;history.Scroll.movementType=ScrollRect.MovementType.Clamped;history.Scroll.scrollSensitivity=32;
        history.transcript=ui.response;var content=ui.response.rectTransform;content.SetParent(rect,false);
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
        content.offsetMin=new Vector2(10,0);content.offsetMax=new Vector2(-10,0);
        ui.response.resizeTextForBestFit=false;ui.response.fontSize=27;ui.response.supportRichText=false;
        ui.response.alignment=TextAnchor.UpperLeft;ui.response.verticalOverflow=VerticalWrapMode.Overflow;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        history.Scroll.content=content;
        // The viewport still accepts wheel and drag events without a visible bar.
        return history;
    }
    public void Begin(string speaker,string line){transcript.text="";Append(speaker,line);}
    public void Append(string speaker,string line)
    {
        transcript.text+=(transcript.text.Length>0?"\n\n":"")+speaker+"："+line;
        Canvas.ForceUpdateCanvases();Scroll.StopMovement();Scroll.verticalNormalizedPosition=0;
    }
}
