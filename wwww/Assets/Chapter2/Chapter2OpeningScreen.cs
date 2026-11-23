using System;
using UnityEngine;
using UnityEngine.UI;

// Created and disposed by Chapter2Controller; do not attach manually.
public sealed class Chapter2OpeningScreen : MonoBehaviour
{
    public RawImage Film { get; private set; }
    public CanvasGroup Title { get; private set; }
    public Image Cover { get; private set; }
    public Text Status { get; private set; }
    Canvas screen;
    RectTransform filmArea;
    AspectRatioFitter fit;
    Button skipButton;
    Text skipLabel;
    bool placedInVR;
    public static Chapter2OpeningScreen Build(Chapter2Presentation ui, Action skip, string title, string subtitle)
    {
        var go = new GameObject("Chapter2 Opening Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5; // Unity built-in UI layer, also for every child.
        var s = go.AddComponent<Chapter2OpeningScreen>();
        s.screen = go.GetComponent<Canvas>(); s.screen.renderMode = RenderMode.ScreenSpaceOverlay; s.screen.sortingOrder = 500;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900);
        s.MakeImage(go.transform, "Black background", Vector2.zero, Vector2.one, Color.black);
        s.filmArea = s.Rect(go.transform, "Film area", new Vector2(0, .06f), new Vector2(1, .94f));
        var film = s.Rect(s.filmArea, "Film", Vector2.zero, Vector2.one);
        s.Film = film.gameObject.AddComponent<RawImage>(); s.Film.raycastTarget = false; s.Film.color = Color.white;
        s.fit = film.gameObject.AddComponent<AspectRatioFitter>(); s.fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; s.fit.aspectRatio = 16f / 9;
        s.Film.gameObject.SetActive(false);
        var titleRoot = s.Rect(go.transform, "Chapter title", Vector2.zero, Vector2.one);
        s.Title = titleRoot.gameObject.AddComponent<CanvasGroup>(); s.Title.alpha = 0; s.Title.blocksRaycasts = false;
        var gold = new Color(.76f, .66f, .46f);
        s.Label(titleRoot, ui.font, "第二章", 26, new Vector2(.1f, .59f), new Vector2(.9f, .65f), gold);
        s.MakeImage(titleRoot, "Gold rule", new Vector2(.43f, .565f), new Vector2(.57f, .567f), gold);
        s.Label(titleRoot, ui.font, title, 54, new Vector2(.08f, .44f), new Vector2(.92f, .55f), new Color(.94f, .91f, .84f));
        s.Label(titleRoot, ui.font, subtitle, 24, new Vector2(.08f, .35f), new Vector2(.92f, .42f), new Color(.65f, .67f, .62f));
        s.Status = s.Label(go.transform, ui.font, "", 22, new Vector2(.1f, .1f), new Vector2(.9f, .16f), new Color(.7f, .7f, .66f));
        var button = s.MakeImage(go.transform, "Skip", new Vector2(.65f, .012f), new Vector2(.97f, .055f), new Color(.06f, .07f, .06f, .8f));
        button.raycastTarget = true; s.skipButton = button.gameObject.AddComponent<Button>(); s.skipButton.onClick.AddListener(() => skip());
        s.skipLabel = s.Label(button.transform, ui.font, "略過片頭  E／空白鍵", 22, Vector2.zero, Vector2.one, new Color(.85f, .83f, .76f));
        s.Cover = s.MakeImage(go.transform, "Transition black", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        return s;
    }
    public void Configure(bool vr, Camera view)
    {
        if (!vr || placedInVR || !view) return;
        placedInVR = true; screen.renderMode = RenderMode.WorldSpace; screen.worldCamera = view;
        var r = (RectTransform)transform; r.sizeDelta = new Vector2(1600, 900); r.localScale = Vector3.one * .0023f;
        var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
        r.position = view.transform.position + forward * 2.7f; r.rotation = Quaternion.LookRotation(forward, Vector3.up);
        // Fixed in the world once at entry. Head movement does not move the screen.
        skipLabel.text = "右手扳機：略過片頭"; skipButton.interactable = false;
    }
    public void SetAspect(float aspect) { fit.aspectRatio = Mathf.Max(.1f, aspect); }
    RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var g = new GameObject(name, typeof(RectTransform)); g.layer = gameObject.layer;
        var r = (RectTransform)g.transform; r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    Image MakeImage(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    { var r = Rect(parent, name, min, max); var i = r.gameObject.AddComponent<Image>(); i.color = color; i.raycastTarget = false; return i; }
    Text Label(Transform parent, Font font, string value, int size, Vector2 min, Vector2 max, Color color)
    { var r = Rect(parent, "Label", min, max); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false; return t; }
}
