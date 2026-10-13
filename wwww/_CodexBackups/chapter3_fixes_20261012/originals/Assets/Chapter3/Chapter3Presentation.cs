using UnityEngine;
using UnityEngine.UI;

public sealed class Chapter3Presentation : MonoBehaviour
{
    public Canvas canvas;
    public Text objective, hint, speaker, subtitle, response, progress, status;
    public GameObject chat, subtitlePanel, ending;
    public InputField input;
    public Button send, depart, replay, menu;
    public Image fade;
    bool vr;
    public void ConfigureVR(bool enabled)
    {
        if (vr == enabled) return;
        vr = enabled;
        canvas.renderMode = enabled ? RenderMode.WorldSpace : RenderMode.ScreenSpaceCamera;
        if (enabled)
        {
            var r = (RectTransform)canvas.transform;
            r.SetParent(canvas.worldCamera.transform, false); r.sizeDelta = new Vector2(1600,900);
            r.localPosition = new Vector3(0,0,2.7f); r.localRotation = Quaternion.identity; r.localScale = Vector3.one * .0023f;
        }
    }
    public void Say(string name, string line)
    {
        subtitlePanel.SetActive(!string.IsNullOrEmpty(line)); speaker.text = name; subtitle.text = line;
    }
    public static Chapter3Presentation Build(Camera camera, Font font)
    {
        var go = new GameObject("第三章字幕與交談", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var ui = go.AddComponent<Chapter3Presentation>();
        ui.canvas = go.GetComponent<Canvas>(); ui.canvas.renderMode = RenderMode.ScreenSpaceCamera;
        ui.canvas.worldCamera = camera; ui.canvas.planeDistance = .5f; ui.canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
        var title = Panel(go.transform,"章節與目標",new Vector2(.028f,.835f),new Vector2(.46f,.966f),new Color(.025f,.04f,.035f,.82f));
        Label(title,"第三章   霧社突襲與戰鬥",font,25,new Vector2(.035f,.57f),new Vector2(.97f,.98f)).color = new Color(.9f,.78f,.5f);
        ui.objective = Label(title,"部落與山林",font,29,new Vector2(.035f,.06f),new Vector2(.97f,.55f));
        ui.hint = Label(go.transform,"",font,24,new Vector2(.1f,.03f),new Vector2(.9f,.09f)); ui.hint.alignment = TextAnchor.MiddleCenter;
        var sub = Panel(go.transform,"劇情字幕",new Vector2(.14f,.11f),new Vector2(.86f,.29f),new Color(.025f,.035f,.03f,.94f));
        ui.subtitlePanel = sub.gameObject;
        ui.speaker = Label(sub,"",font,26,new Vector2(.035f,.64f),new Vector2(.965f,.98f)); ui.speaker.color = new Color(.93f,.8f,.5f);
        ui.subtitle = Label(sub,"",font,30,new Vector2(.035f,.08f),new Vector2(.965f,.67f));
        var chat = Panel(go.transform,"巴萬出發前交談",new Vector2(.1f,.03f),new Vector2(.9f,.42f),new Color(.024f,.037f,.032f,.97f));
        ui.chat = chat.gameObject;
        Label(chat,"巴萬·拿威",font,29,new Vector2(.035f,.82f),new Vector2(.5f,.98f)).color = new Color(.94f,.81f,.54f);
        ui.progress = Label(chat,"交談  0 / 3",font,24,new Vector2(.74f,.82f),new Vector2(.97f,.98f)); ui.progress.alignment = TextAnchor.MiddleRight;
        ui.response = Label(chat,"",font,29,new Vector2(.035f,.38f),new Vector2(.965f,.81f)); ui.response.alignment = TextAnchor.UpperLeft;
        ui.response.resizeTextForBestFit = true; ui.response.resizeTextMinSize = 23; ui.response.resizeTextMaxSize = 29;
        var field = Panel(chat,"輸入回答",new Vector2(.035f,.14f),new Vector2(.795f,.32f),new Color(.12f,.16f,.14f,1));
        ui.input = field.gameObject.AddComponent<InputField>(); ui.input.targetGraphic = field.GetComponent<Image>();
        ui.input.textComponent = Label(field,"",font,26,new Vector2(.02f,.02f),new Vector2(.98f,.98f));
        var placeholder = Label(field,"告訴巴萬你的想法……",font,25,new Vector2(.02f,.02f),new Vector2(.98f,.98f)); placeholder.color = new Color(.65f,.7f,.65f);
        ui.input.placeholder = placeholder; ui.input.characterLimit = 160; ui.input.lineType = InputField.LineType.MultiLineSubmit;
        ui.send = Button(chat,"送出",font,new Vector2(.82f,.14f),new Vector2(.965f,.32f));
        ui.depart = Button(chat,"跟巴萬出發",font,new Vector2(.67f,.14f),new Vector2(.965f,.32f));
        ui.status = Label(chat,"",font,20,new Vector2(.035f,.015f),new Vector2(.965f,.125f)); ui.status.color = new Color(.77f,.8f,.73f);
        ui.fade = Panel(go.transform,"淡入淡出",Vector2.zero,Vector2.one,Color.black).GetComponent<Image>(); ui.fade.raycastTarget = false;
        var end = Panel(go.transform,"山路銜接點",new Vector2(.19f,.26f),new Vector2(.81f,.74f),new Color(.025f,.04f,.035f,.98f));
        ui.ending = end.gameObject;
        Label(end,"部落與山林",font,35,new Vector2(.07f,.75f),new Vector2(.93f,.94f)).color = new Color(.94f,.81f,.54f);
        Label(end,"隊伍繼續前往駐在所。\n\n目前可遊玩的第一部分到這裡。",font,29,new Vector2(.07f,.31f),new Vector2(.93f,.72f));
        ui.replay = Button(end,"重玩本段",font,new Vector2(.07f,.09f),new Vector2(.46f,.25f));
        ui.menu = Button(end,"返回章節選擇",font,new Vector2(.54f,.09f),new Vector2(.93f,.25f));
        ui.chat.SetActive(false); ui.depart.gameObject.SetActive(false); ui.subtitlePanel.SetActive(false); ui.ending.SetActive(false);
        ui.fade.color = Color.clear;
        return ui;
    }
    static RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var g = new GameObject(name,typeof(RectTransform),typeof(Image)); var r = (RectTransform)g.transform;
        r.SetParent(parent,false); r.anchorMin=min; r.anchorMax=max; r.offsetMin=r.offsetMax=Vector2.zero;
        g.GetComponent<Image>().color=color; return r;
    }
    static Text Label(Transform parent,string value,Font font,int size,Vector2 min,Vector2 max)
    {
        var g = new GameObject("文字",typeof(RectTransform),typeof(Text)); var r=(RectTransform)g.transform;
        r.SetParent(parent,false); r.anchorMin=min; r.anchorMax=max; r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=new Color(.94f,.94f,.88f);
        t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=false;return t;
    }
    static Button Button(Transform parent,string label,Font font,Vector2 min,Vector2 max)
    {
        var r=Panel(parent,label,min,max,new Color(.30f,.36f,.25f,1));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var t=Label(r,label,font,25,Vector2.zero,Vector2.one);t.alignment=TextAnchor.MiddleCenter;return b;
    }
}
