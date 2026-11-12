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

    bool vr;
    bool controlsInitialized;
    bool controlsVR;
    Transform desktopParent;
    Vector2 desktopAnchorMin, desktopAnchorMax, desktopPivot, desktopSize;
    Vector3 desktopPosition, desktopScale;
    Quaternion desktopRotation;
    RenderMode desktopMode;
    GraphicRaycaster raycaster;
    readonly List<RaycastResult> gazeHits = new List<RaycastResult>();

    public void SetObjective(string text) { if (objective) objective.text = text; }

    public void SetControls(bool useVR)
    {
        if (controlsInitialized && controlsVR == useVR) return;
        controlsInitialized = true;
        controlsVR = useVR;
        if (controls) controls.text = useVR
            ? "左搖桿移動　右搖桿轉向　選單鍵開啟選單；注視按鈕並按右扳機選取"
            : "WASD 移動　按住滑鼠右鍵環視，或點一下畫面固定滑鼠　Esc 選單";
    }

    public void ShowMenu(string title, string message, string continueText)
    {
        menuTitle.text = title;
        menuMessage.text = message;
        continueLabel.text = continueText;
        menuPanel.SetActive(true);
        if (vr) PlaceVRCanvas();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    public void SetMenuMessage(string message) { if (menuMessage) menuMessage.text = message; }

    public void HideMenu()
    {
        if (menuPanel) menuPanel.SetActive(false);
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
    }

    void LateUpdate()
    {
        // HUD follows the view; an open VR menu stays in its initial world pose
        // so the buttons do not follow the user's gaze during selection.
        if (vr && menuPanel && !menuPanel.activeSelf) PlaceVRCanvas();
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
        ui.objective = Label(heading, "沿山路前往集合地", font, 29, new Vector2(.035f, .08f), new Vector2(.97f, .53f));
        var hint = Panel(go.transform, "操作提示", new Vector2(.065f, .025f), new Vector2(.935f, .09f), new Color(.035f, .047f, .04f, .82f), false);
        ui.controls = Label(hint, "", font, 23, new Vector2(.02f, 0), new Vector2(.98f, 1));
        ui.controls.alignment = TextAnchor.MiddleCenter;
        var dim = Panel(go.transform, "選單遮罩", Vector2.zero, Vector2.one, new Color(0, 0, 0, .52f), true);
        ui.menuPanel = dim.gameObject;
        var panel = Panel(dim, "選單", new Vector2(.29f, .26f), new Vector2(.71f, .74f), new Color(.055f, .075f, .06f, .98f), true);
        ui.menuTitle = Label(panel, "暫停", font, 36, new Vector2(.08f, .77f), new Vector2(.92f, .95f));
        ui.menuTitle.alignment = TextAnchor.MiddleCenter;
        ui.menuTitle.color = new Color(.94f, .83f, .62f);
        ui.menuMessage = Label(panel, "沿山路前往集合地。", font, 25, new Vector2(.08f, .51f), new Vector2(.92f, .75f));
        ui.menuMessage.alignment = TextAnchor.MiddleCenter;
        ui.continueButton = CreateButton(panel, "繼續", font, new Vector2(.16f, .29f), new Vector2(.84f, .46f), out ui.continueLabel);
        ui.menuButton = CreateButton(panel, "返回章節選單", font, new Vector2(.16f, .075f), new Vector2(.84f, .245f), out _);
        ui.menuPanel.SetActive(false);
        ui.SetControls(false);
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
