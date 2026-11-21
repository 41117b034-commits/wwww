using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class Chapter4Presentation : MonoBehaviour
{
    public Canvas canvas;
    public Text objective;
    public Text controls;
    public GameObject menuPanel;
    public Text menuTitle;
    public Text menuMessage;
    public Text continueLabel;
    public Button continueButton;
    public Button menuButton;
    public GameObject speechPanel;
    public Text speakerName, speechText, carryover, partyStatus, interaction, navigation;
    public Button signalButton;
    public Text gasStatus;
    public Image gasVeil;
    public Button contactButton;
    public Text contactText;
    public GameObject endingPanel;
    public Text endingKicker, endingTitle, endingBody, endingFooter;
    Chapter4EpilogueFilm endingFilm;
    public Text retreatWarning;
    public Text impactTarget;
    public Text rifleStatus, rifleReticle;

    bool vr;
    bool contactStyleConfigured;
    Transform desktopParent;
    Vector2 desktopAnchorMin, desktopAnchorMax, desktopPivot, desktopSize;
    Vector3 desktopPosition, desktopScale;
    Quaternion desktopRotation;
    RenderMode desktopMode;
    GraphicRaycaster raycaster;
    readonly List<RaycastResult> gazeHits = new List<RaycastResult>();

    string currentObjective;
    string contactPrompt;
    void RefreshObjective()
    {
        if (objective) objective.text = string.IsNullOrEmpty(contactPrompt) ? currentObjective : contactPrompt;
    }
    public void SetObjective(string text)
    {
        if (currentObjective == text) return;
        currentObjective = text;
        RefreshObjective();
    }

    public void EnsureCombatUI()
    {
        if (rifleStatus) return;
        var panel = Panel(canvas.transform, "步槍與射擊回饋", new Vector2(.77f,.20f), new Vector2(.97f,.255f), new Color(.035f, .047f, .04f, .8f), false);
        rifleStatus = Label(panel, "", objective.font, 21, new Vector2(.03f, .04f), new Vector2(.97f, .96f));
        rifleStatus.color = new Color(.88f, .87f, .76f);
        rifleReticle = Label(canvas.transform, "+", objective.font, 24, new Vector2(.49f, .48f), new Vector2(.51f, .52f));
        rifleReticle.alignment = TextAnchor.MiddleCenter;
        rifleReticle.color = new Color(.91f, .90f, .81f, .85f);
        var edge = rifleReticle.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.025f,.03f,.025f,.85f); edge.effectDistance = new Vector2(1,-1);
        menuPanel.transform.SetAsLastSibling();
    }
    public void SetCombatStatus(Chapter4Rifle gun, bool visible)
    {
        if (!rifleStatus) return;
        bool showAmmo = visible && (gun.Reloading || gun.Aiming);
        rifleStatus.transform.parent.gameObject.SetActive(showAmmo);
        rifleReticle.gameObject.SetActive(visible && !vr && gun.Aiming);
        if (!showAmmo) return;
        rifleStatus.text = "步槍　" + (gun.Loaded ? "1" : "0") + "／1　備彈 " + gun.Reserve + (gun.Reloading ? "　裝填中" : "");
    }

    public void EnsureEndingUI()
    {
        if (endingPanel) return;
        contactButton = CreateButton(canvas.transform, "聯絡歐冰", objective.font, new Vector2(.52f, .72f), new Vector2(.975f, .825f), out contactText);
        contactText.fontSize = 23;
        var backdrop = Panel(canvas.transform, "個人結局與共同史實後記", Vector2.zero, Vector2.one, new Color(.025f, .037f, .03f, .98f), true);
        endingPanel = backdrop.gameObject;
        var card = Panel(backdrop, "章末文字", new Vector2(.13f, .15f), new Vector2(.87f, .86f), new Color(.055f, .075f, .06f, 1), true);
        endingTitle = Label(card, "", objective.font, 38, new Vector2(.065f, .72f), new Vector2(.935f, .88f));
        endingTitle.color = new Color(.95f, .83f, .62f);
        endingBody = Label(card, "", objective.font, 29, new Vector2(.065f, .16f), new Vector2(.935f, .71f));
        endingBody.alignment = TextAnchor.UpperLeft;
        endingBody.lineSpacing = 1.15f;
        endingFooter = Label(card, "", objective.font, 19, new Vector2(.065f, .025f), new Vector2(.935f, .14f));
        endingFooter.color = new Color(.75f, .78f, .72f);
        SetContactStatus("", false);
        endingPanel.SetActive(false);
        menuPanel.transform.SetAsLastSibling();
    }

    public void SetContactStatus(string prompt, bool ready)
    {
        if (!contactButton) return;
        if (!contactStyleConfigured)
        {
            contactButton.transform.SetParent(objective.transform.parent,false);
            Place(contactButton.transform,.035f,.08f,.97f,.53f);
            contactButton.targetGraphic.color=Color.clear;
            contactText.gameObject.SetActive(false);
            contactStyleConfigured=true;
        }
        contactButton.gameObject.SetActive(ready);
        contactButton.interactable=ready;
        contactButton.targetGraphic.raycastTarget=ready;
        string next=ready ? prompt : null;
        if (contactPrompt == next) return;
        contactPrompt=next;
        RefreshObjective();
    }

    public void ShowEnding(Chapter4SceneController chapter, string title, string body, string footer = null)
    {
        endingPanel.SetActive(true);
        if (!endingFilm)
        {
            endingPanel.GetComponent<Image>().color = new Color(.16f,.17f,.15f);
            var card = (RectTransform)endingBody.transform.parent;
            card.GetComponent<Image>().color = Color.clear;
            Place(card,0,0,1,1);
            if (endingKicker) endingKicker.gameObject.SetActive(false);
            Place(endingTitle.transform,.075f,.71f,.93f,.85f);
            Place(endingBody.transform,.078f,.215f,.83f,.68f);
            Place(endingFooter.transform,.078f,.075f,.96f,.185f);
            endingBody.lineSpacing = 1.18f;
            endingBody.resizeTextForBestFit = false;
            endingBody.fontSize = 30;
            endingBody.fontStyle = FontStyle.Normal;
            endingBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            foreach (var text in new[] { endingTitle, endingBody, endingFooter })
            {
                var shadow=text.gameObject.AddComponent<Shadow>();
                shadow.effectColor=new Color(0,0,0,.62f);
                shadow.effectDistance=new Vector2(1,-1);
            }
            endingFilm=endingPanel.AddComponent<Chapter4EpilogueFilm>();
            endingFilm.Initialize(chapter);
        }
        endingFilm.Show(chapter.ending.CurrentStage);
        endingTitle.text=title; endingBody.text=body;
        endingFooter.text=footer ?? "";
        endingFooter.gameObject.SetActive(!string.IsNullOrEmpty(footer));
        if (vr) PlaceVRCanvas();
    }

    public void EnsureGasUI()
    {
        if (gasVeil) return;
        var veil=Panel(canvas.transform,"毒氣影響",Vector2.zero,Vector2.one,Color.clear,false);
        gasVeil=veil.GetComponent<Image>();
        veil.SetAsFirstSibling();
    }

    public void SetGasVeil(float exposure,float collapse,float mist=0)
    {
        if (!gasVeil) return;
        Color tint = Color.Lerp(new Color(.16f,.19f,.13f),new Color(.58f,.59f,.51f),mist);
        tint.a=Mathf.Max(exposure*.20f,collapse*.82f,mist*.18f);
        gasVeil.color=tint;
    }

    // Saved scenes may still contain panels from the desktop prototype.
    public void EnsureRetreatUI()
    {
        if (controls) controls.transform.parent.gameObject.SetActive(false);
        if (carryover) carryover.transform.parent.gameObject.SetActive(false);
        if (gasStatus) gasStatus.transform.parent.gameObject.SetActive(false);
        if (navigation) navigation.gameObject.SetActive(false);
        if (signalButton) signalButton.gameObject.SetActive(false);
        if (retreatWarning) retreatWarning.transform.parent.gameObject.SetActive(false);
        if (impactTarget) impactTarget.transform.parent.gameObject.SetActive(false);
        Place(objective.transform.parent,.025f,.865f,.40f,.975f);
        foreach(var label in objective.transform.parent.GetComponentsInChildren<Text>()) label.fontSize=21;
        objective.fontSize=23;
        Place(speechPanel.transform,.16f,.075f,.84f,.21f);
        speechPanel.GetComponent<Image>().color=new Color(.035f,.047f,.04f,.78f);
        speakerName.fontSize=20; speechText.fontSize=25;
    }

    static void Place(Transform item, float left, float bottom, float right, float top)
    {
        var rect = (RectTransform)item;
        rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void EnsureStoryUI()
    {
        if (speechPanel) return;
        Font font = objective.font;
        var speech = Panel(canvas.transform, "撤離對話", new Vector2(.16f, .17f), new Vector2(.84f, .35f), new Color(.035f, .047f, .04f, .94f), false);
        speechPanel = speech.gameObject;
        speakerName = Label(speech, "", font, 24, new Vector2(.035f, .69f), new Vector2(.965f, .96f));
        speakerName.color = new Color(.94f, .83f, .62f);
        speechText = Label(speech, "", font, 28, new Vector2(.035f, .08f), new Vector2(.965f, .70f));
        menuPanel.transform.SetAsLastSibling();
        speechPanel.SetActive(false);
    }

    public void ShowSpeech(string name, string text)
    {
        speakerName.text = name; speechText.text = text; speechPanel.SetActive(true);
    }
    public void HideSpeech() { if (speechPanel) speechPanel.SetActive(false); }
    Canvas fadeCanvas;
    Image fadeImage;
    public float FadeAmount => fadeImage ? fadeImage.color.a : 0;

    public void SetFade(float amount)
    {
        if (!fadeImage && amount <= 0) return;
        Transform parent = canvas.transform;
        if (vr)
        {
            Camera view = canvas.worldCamera;
            if (!fadeCanvas)
            {
                var obj = new GameObject("章末全視野淡出", typeof(RectTransform), typeof(Canvas));
                fadeCanvas = obj.GetComponent<Canvas>();
                fadeCanvas.renderMode = RenderMode.WorldSpace;
                fadeCanvas.sortingOrder = 31000;
            }
            if (fadeCanvas.transform.parent != view.transform)
            {
                // Leave a generous margin around each eye, including during a head turn.
                fadeCanvas.worldCamera = view;
                float distance = Mathf.Max(.32f, view.nearClipPlane + .12f);
                var rect = (RectTransform)fadeCanvas.transform;
                rect.SetParent(view.transform, false);
                rect.localPosition = Vector3.forward * distance;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.sizeDelta = Vector2.one * (distance * 18f);
            }
            parent = fadeCanvas.transform;
        }

        if (!fadeImage)
        {
            fadeImage = Panel(parent, "黑幕", Vector2.zero, Vector2.one, Color.clear, false).GetComponent<Image>();
            fadeImage.gameObject.AddComponent<GameDialogueOverlayGraphic>();
        }
        else if (fadeImage.transform.parent != parent)
        {
            var rect = fadeImage.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        fadeImage.transform.SetAsLastSibling();
        fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(amount));
        bool visible = amount > 0 && (!menuPanel || !menuPanel.activeSelf);
        fadeImage.gameObject.SetActive(visible);
        if (fadeCanvas) fadeCanvas.enabled = vr && visible;
    }

    public void ShowMenu(string title, string message, string continueText)
    {
        menuTitle.text = title;
        menuMessage.text = message;
        continueLabel.text = continueText;
        menuPanel.SetActive(true);
        if (fadeImage) fadeImage.gameObject.SetActive(false);
        if (vr) PlaceVRCanvas();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    public void SetMenuMessage(string message) { if (menuMessage) menuMessage.text = message; }

    public void HideMenu()
    {
        if (menuPanel) menuPanel.SetActive(false);
        if (fadeImage) SetFade(FadeAmount);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ConfigureVR(bool useVR)
    {
        if (vr == useVR || !canvas || !canvas.worldCamera) return;
        var rect = (RectTransform)canvas.transform;
        if (useVR)
        {
            desktopParent = rect.parent;
            desktopMode = canvas.renderMode;
            desktopAnchorMin = rect.anchorMin;
            desktopAnchorMax = rect.anchorMax;
            desktopPivot = rect.pivot;
            desktopSize = rect.sizeDelta;
            desktopPosition = rect.anchoredPosition3D;
            desktopRotation = rect.localRotation;
            desktopScale = rect.localScale;
            canvas.renderMode = RenderMode.WorldSpace;
            rect.SetParent(null, false);
            rect.sizeDelta = new Vector2(1600, 900);
            rect.localScale = Vector3.one * .0017f;
            PlaceVRCanvas();
        }
        else
        {
            rect.SetParent(desktopParent, false);
            rect.anchorMin = desktopAnchorMin;
            rect.anchorMax = desktopAnchorMax;
            rect.pivot = desktopPivot;
            rect.sizeDelta = desktopSize;
            rect.anchoredPosition3D = desktopPosition;
            rect.localRotation = desktopRotation;
            rect.localScale = desktopScale;
            canvas.renderMode = desktopMode;
        }
        vr = useVR;
        if (fadeImage) SetFade(FadeAmount);
    }

    void LateUpdate()
    {
        // HUD follows the view; an open VR menu stays in its initial world pose
        // so the buttons do not follow the user's gaze during selection.
        if (vr && menuPanel && !menuPanel.activeSelf && (!endingPanel || !endingPanel.activeSelf)) PlaceVRCanvas();
    }

    void PlaceVRCanvas()
    {
        if (!canvas || !canvas.worldCamera) return;
        Transform cameraTransform = canvas.worldCamera.transform;
        canvas.transform.position = cameraTransform.position + cameraTransform.forward * 2.3f;
        canvas.transform.rotation = cameraTransform.rotation;
    }

    public void UpdateGaze(bool submit)
    {
        if (!vr || !menuPanel || !menuPanel.activeSelf || !EventSystem.current || !canvas.worldCamera) return;
        if (!raycaster) raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (!raycaster) return;
        var pointer = new PointerEventData(EventSystem.current) { position = canvas.worldCamera.pixelRect.center };
        gazeHits.Clear();
        raycaster.Raycast(pointer, gazeHits);
        Button target = null;
        foreach (RaycastResult hit in gazeHits)
        {
            target = hit.gameObject.GetComponentInParent<Button>();
            if (target && target.IsInteractable()) break;
            target = null;
        }
        EventSystem.current.SetSelectedGameObject(target ? target.gameObject : null);
        if (submit && target) target.onClick.Invoke();
    }

    public static Chapter4Presentation Build(Camera camera, Font font)
    {
        var go = new GameObject("第四章介面", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var ui = go.AddComponent<Chapter4Presentation>();
        ui.canvas = go.GetComponent<Canvas>();
        ui.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ui.canvas.worldCamera = camera;
        ui.canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        var heading = Panel(go.transform, "章節目標", new Vector2(.025f, .835f), new Vector2(.48f, .97f), new Color(.035f, .047f, .04f, .86f), false);
        Label(heading, "第四章　日軍鎮壓與毒氣攻擊", font, 25, new Vector2(.035f, .56f), new Vector2(.97f, .96f)).color = new Color(.92f, .81f, .58f);
        ui.objective = Label(heading, "沿山路前往岩窟", font, 29, new Vector2(.035f, .08f), new Vector2(.97f, .53f));
        var dim = Panel(go.transform, "選單遮罩", Vector2.zero, Vector2.one, new Color(0, 0, 0, .52f), true);
        ui.menuPanel = dim.gameObject;
        var panel = Panel(dim, "選單", new Vector2(.29f, .26f), new Vector2(.71f, .74f), new Color(.055f, .075f, .06f, .98f), true);
        ui.menuTitle = Label(panel, "暫停", font, 36, new Vector2(.08f, .77f), new Vector2(.92f, .95f));
        ui.menuTitle.alignment = TextAnchor.MiddleCenter;
        ui.menuTitle.color = new Color(.94f, .83f, .62f);
        ui.menuMessage = Label(panel, "沿山路前往岩窟。", font, 25, new Vector2(.08f, .51f), new Vector2(.92f, .75f));
        ui.menuMessage.alignment = TextAnchor.MiddleCenter;
        ui.continueButton = CreateButton(panel, "繼續", font, new Vector2(.16f, .29f), new Vector2(.84f, .46f), out ui.continueLabel);
        ui.menuButton = CreateButton(panel, "返回章節選單", font, new Vector2(.16f, .075f), new Vector2(.84f, .245f), out _);
        ui.menuPanel.SetActive(false);
        if (!Object.FindFirstObjectByType<EventSystem>())
        {
            var events = new GameObject("第四章介面事件", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        return ui;
    }

    static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var graphic = go.GetComponent<Image>();
        graphic.color = color;
        graphic.raycastTarget = raycast;
        return rect;
    }

    static Text Label(Transform parent, string value, Font font, int size, Vector2 min, Vector2 max)
    {
        var go = new GameObject("文字", typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.text = value;
        label.color = new Color(.94f, .94f, .89f);
        label.alignment = TextAnchor.MiddleLeft;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }

    static Button CreateButton(Transform parent, string title, Font font, Vector2 min, Vector2 max, out Text label)
    {
        var rect = Panel(parent, title, min, max, new Color(.26f, .33f, .25f, 1), true);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.12f, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.73f, .79f, .7f, 1);
        button.colors = colors;
        label = Label(rect, title, font, 27, Vector2.zero, Vector2.one);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
}
