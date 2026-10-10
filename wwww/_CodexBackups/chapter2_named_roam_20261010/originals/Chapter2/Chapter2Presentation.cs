using UnityEngine;
using UnityEngine.UI;

public sealed class Chapter2Presentation : MonoBehaviour
{
    public Font font;
    public Camera view;
    public Canvas canvas;
    public Text chapter, objective, speaker, subtitle, choiceTitle, choiceA, choiceB, hint, meterLabel, endText;
    public GameObject dialoguePanel, choicePanel, meterPanel, endingPanel;
    public Button buttonA, buttonB, continueButton;
    public RawImage videoImage;
    public Image fade, meter, cursor;
    public RectTransform safeZone;
    bool inVR;
    public void ConfigureVR(bool value)
    {
        if (value == inVR) return;
        inVR = value;
        if(value)
        {
            canvas.renderMode=RenderMode.WorldSpace;
            var r=canvas.GetComponent<RectTransform>(); r.SetParent(view.transform,false);
            r.sizeDelta=new Vector2(1600,900); r.localScale=Vector3.one*0.0023f; r.localPosition=new Vector3(0,0,2.7f); r.localRotation=Quaternion.identity;
        }
    }
    public void Line(string name, string text)
    {
        dialoguePanel.SetActive(!string.IsNullOrEmpty(text)); speaker.text=name; subtitle.text=text;
    }
    public void Choices(string title,string a,string b)
    {
        choiceTitle.text=title; choiceA.text=a; choiceB.text=b;
        choicePanel.SetActive(true);
    }
    public void HideChoices() { choicePanel.SetActive(false); }
    public void SetMeter(float phase,int hits)
    {
        int progress=Mathf.Clamp(hits*20,0,100);
        meterLabel.text=$"砍伐完成度 {progress}%     有效砍伐 {hits} / 5";
        meter.fillAmount=progress/100f;
        // This solid-color Image has no sprite, so Filled alone does not clip it.
        var bar=meter.rectTransform;
        bar.anchorMax=new Vector2(Mathf.Lerp(.06f,.94f,meter.fillAmount),bar.anchorMax.y);
        cursor.rectTransform.anchorMin=new Vector2(phase,0); cursor.rectTransform.anchorMax=new Vector2(phase,1);
    }
    public static Chapter2Presentation Build(Camera camera,Font font)
    {
        var go=new GameObject("Chapter2_HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var ui=go.AddComponent<Chapter2Presentation>(); ui.font=font;ui.view=camera;ui.canvas=go.GetComponent<Canvas>();
        ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=0.5f;ui.canvas.sortingOrder=100;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=0.5f;
        var header=ui.Panel(go.transform,"Chapter heading",new Vector2(.025f,.84f),new Vector2(.42f,.975f),new Color(.025f,.045f,.04f,.85f));
        ui.chapter=ui.Label(header,"第二章  /  新規定與秘密會議",24,new Vector2(.045f,.54f),new Vector2(.96f,.96f),new Color(.83f,.73f,.5f));
        ui.objective=ui.Label(header,"",28,new Vector2(.045f,.03f),new Vector2(.96f,.58f),Color.white);
        ui.hint=ui.Label(go.transform,"",24,new Vector2(.04f,.24f),new Vector2(.96f,.30f),Color.white,TextAnchor.MiddleCenter);
        ui.dialoguePanel=ui.Panel(go.transform,"Subtitles",new Vector2(.1f,.055f),new Vector2(.9f,.23f),new Color(.018f,.027f,.024f,.92f)).gameObject;
        ui.speaker=ui.Label(ui.dialoguePanel.transform,"",24,new Vector2(.035f,.64f),new Vector2(.965f,.96f),new Color(.89f,.76f,.48f));
        ui.subtitle=ui.Label(ui.dialoguePanel.transform,"",30,new Vector2(.035f,.07f),new Vector2(.965f,.68f),Color.white);
        ui.choicePanel=ui.Panel(go.transform,"Decision",new Vector2(.16f,.32f),new Vector2(.84f,.64f),new Color(.025f,.04f,.035f,.95f)).gameObject;
        ui.choiceTitle=ui.Label(ui.choicePanel.transform,"",31,new Vector2(.05f,.66f),new Vector2(.95f,.95f),Color.white,TextAnchor.MiddleCenter);
        ui.buttonA=ui.MakeButton(ui.choicePanel.transform,new Vector2(.045f,.13f),new Vector2(.48f,.58f),out ui.choiceA);
        ui.buttonB=ui.MakeButton(ui.choicePanel.transform,new Vector2(.52f,.13f),new Vector2(.955f,.58f),out ui.choiceB);
        ui.meterPanel=ui.Panel(go.transform,"Chopping progress",new Vector2(.25f,.34f),new Vector2(.75f,.48f),new Color(.02f,.04f,.03f,.9f)).gameObject;
        ui.meterLabel=ui.Label(ui.meterPanel.transform,"",25,new Vector2(.04f,.54f),new Vector2(.96f,.94f),Color.white,TextAnchor.MiddleCenter);
        var track=ui.Panel(ui.meterPanel.transform,"Rhythm",new Vector2(.06f,.2f),new Vector2(.94f,.43f),new Color(.25f,.25f,.22f,1));
        ui.safeZone=ui.Panel(track,"Safe band",new Vector2(.32f,0),new Vector2(.68f,1),new Color(.58f,.74f,.36f,1));
        ui.cursor=ui.Panel(track,"Timing cursor",new Vector2(0,0),new Vector2(0,1),Color.white).GetComponent<Image>(); ui.cursor.rectTransform.sizeDelta=new Vector2(5,8);
        ui.meter=ui.Panel(ui.meterPanel.transform,"Completed cuts",new Vector2(.06f,.07f),new Vector2(.94f,.12f),new Color(.87f,.72f,.42f)).GetComponent<Image>();ui.meter.type=Image.Type.Filled;ui.meter.fillMethod=Image.FillMethod.Horizontal;
        var videoPanel=ui.Panel(go.transform,"Video",Vector2.zero,Vector2.one,Color.black).gameObject;Object.DestroyImmediate(videoPanel.GetComponent<Image>());
        ui.videoImage=videoPanel.AddComponent<RawImage>();ui.videoImage.color=Color.white;ui.videoImage.raycastTarget=false;
        ui.fade=ui.Panel(go.transform,"Fade",Vector2.zero,Vector2.one,new Color(0,0,0,0)).GetComponent<Image>();ui.fade.raycastTarget=false;
        ui.endingPanel=ui.Panel(go.transform,"Chapter complete",Vector2.zero,Vector2.one,Color.black).gameObject;
        ui.endText=ui.Label(ui.endingPanel.transform,"決戰的時刻，將至。",46,new Vector2(.1f,.4f),new Vector2(.9f,.6f),new Color(.93f,.84f,.65f),TextAnchor.MiddleCenter);
        Text buttonText;ui.continueButton=ui.MakeButton(ui.endingPanel.transform,new Vector2(.36f,.18f),new Vector2(.64f,.28f),out buttonText);buttonText.text="返回章節選單";
        ui.dialoguePanel.SetActive(false);ui.choicePanel.SetActive(false);ui.meterPanel.SetActive(false);ui.videoImage.gameObject.SetActive(false);ui.endingPanel.SetActive(false);
        return ui;
    }
    RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        g.GetComponent<Image>().color=color;g.GetComponent<Image>().raycastTarget=false;return r;
    }
    Text Label(Transform parent,string value,int size,Vector2 min,Vector2 max,Color color,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var g=new GameObject("Text",typeof(RectTransform),typeof(Text));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
    }
    Button MakeButton(Transform parent,Vector2 min,Vector2 max,out Text text)
    {
        var r=Panel(parent,"Choice",min,max,new Color(.19f,.25f,.2f,1));r.GetComponent<Image>().raycastTarget=true;
        var b=r.gameObject.AddComponent<Button>();var colors=b.colors;colors.highlightedColor=new Color(1,.88f,.62f);colors.selectedColor=colors.highlightedColor;b.colors=colors;
        text=Label(r,"",28,new Vector2(.05f,.05f),new Vector2(.95f,.95f),Color.white,TextAnchor.MiddleCenter);return b;
    }
}
