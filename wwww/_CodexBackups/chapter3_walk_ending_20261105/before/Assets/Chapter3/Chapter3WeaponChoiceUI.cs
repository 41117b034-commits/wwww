using UnityEngine;
using UnityEngine.UI;

// Same proportions, colors and buttons as Chapter 2's tree decision.
public sealed class Chapter3WeaponChoiceUI : MonoBehaviour
{
    public Button knife,rifle;
    public static Chapter3WeaponChoiceUI Build(Chapter3Presentation ui)
    {
        var r=Panel(ui.canvas.transform,"選擇襯手的武器",new Vector2(.16f,.32f),new Vector2(.84f,.64f),new Color(.025f,.04f,.035f,.95f));
        var choice=r.gameObject.AddComponent<Chapter3WeaponChoiceUI>();
        Label(r,"選一把襯手的武器（只能持有一種）",ui.subtitle.font,31,new Vector2(.05f,.66f),new Vector2(.95f,.95f));
        choice.knife=Button(r,"1  刀 · 獵刀",ui.subtitle.font,new Vector2(.045f,.13f),new Vector2(.48f,.58f));
        choice.rifle=Button(r,"2  槍 · 村田十八年式步槍",ui.subtitle.font,new Vector2(.52f,.13f),new Vector2(.955f,.58f));
        choice.gameObject.SetActive(false);return choice;
    }
    static RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;g.GetComponent<Image>().color=color;return r;
    }
    static void Label(Transform parent,string text,Font font,int size,Vector2 min,Vector2 max)
    {
        var g=new GameObject("文字",typeof(RectTransform),typeof(Text));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
    }
    static Button Button(Transform parent,string text,Font font,Vector2 min,Vector2 max)
    {
        var r=Panel(parent,text,min,max,new Color(.19f,.25f,.2f,1));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var colors=b.colors;colors.highlightedColor=colors.selectedColor=new Color(1,.88f,.62f);b.colors=colors;
        Label(r,text,font,28,new Vector2(.05f,.05f),new Vector2(.95f,.95f));return b;
    }
}
