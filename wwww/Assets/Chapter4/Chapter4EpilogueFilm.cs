using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class Chapter4EpilogueFilm : MonoBehaviour
{
    public const string FilmCredit = "清流部落，2015　｜　安安女王《霧社事件的餘生後裔》節錄／PeoPo　CC BY 3.0 TW";
    public const string ArchiveCredit = "日治時期霧社　｜　臺北折井寫真館《霧社全景》／國立臺灣歷史博物館典藏";
    const string Folder = "Chapter4Epilogue/";

    Chapter4SceneController chapter;
    RawImage screen, postcard;
    Texture2D filmStill;
    Text credit;
    VideoPlayer projector;
    bool historical, failed, requestedPlay;
    float photoElapsed;
    const float PostcardHold = 13f, Dissolve = 2f;

    public void Initialize(Chapter4SceneController owner, Text sourceCredit)
    {
        chapter = owner;
        credit = sourceCredit;
        filmStill = Resources.Load<Texture2D>(Folder + "QingliuStill");
        // Put the shade over both media and behind all of the existing text.
        Layer("映幕暗角").texture = Resources.Load<Texture2D>(Folder + "ProjectorShade");
        postcard = Layer("霧社明信片");
        postcard.texture = Resources.Load<Texture2D>(Folder + "WushePanorama");
        screen = Layer("章末映幕");
        screen.texture = filmStill;
        projector = gameObject.AddComponent<VideoPlayer>();
        projector.playOnAwake = false;
        projector.isLooping = true;
        projector.audioOutputMode = VideoAudioOutputMode.None;
        projector.renderMode = VideoRenderMode.APIOnly;
        projector.waitForFirstFrame = true;
        projector.source = VideoSource.VideoClip;
        projector.clip = Resources.Load<VideoClip>(Folder + "Qingliu");
        projector.errorReceived += OnVideoError;
        if (projector.clip) projector.Prepare();
    }

    RawImage Layer(string label)
    {
        var view = new GameObject(label, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        var rect = view.rectTransform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        view.raycastTarget = false;
        return view;
    }

    public void Show(bool useHistoricalPhotograph)
    {
        historical = useHistoricalPhotograph;
        photoElapsed = 0;
        Refresh();
    }

    void Update()
    {
        if (!chapter) return;
        if (historical && !chapter.IsPaused) photoElapsed += Time.unscaledDeltaTime;
        Refresh();
    }

    void Refresh()
    {
        if (!failed && projector.isPrepared)
        {
            // Keep the silent reel running beneath the postcard. Seeking a
            // paused Media Foundation decoder can freeze its displayed frame.
            bool play = !chapter.IsPaused;
            if (requestedPlay != play)
            {
                if (play) projector.Play(); else projector.Pause();
                requestedPlay = play;
            }
            if (projector.frame >= 0 && projector.texture)
                screen.texture = projector.texture;
        }
        bool decoded = !failed && projector.isPrepared && projector.frame >= 0 && projector.texture;
        float opacity = historical && postcard.texture
            ? (decoded ? 1 - Mathf.SmoothStep(0, 1, (photoElapsed - PostcardHold) / Dissolve) : 1) : 0;
        postcard.enabled = opacity > 0;
        if (postcard.enabled)
        {
            postcard.color = new Color(1, 1, 1, opacity);
            Fit(postcard, 1.06f, .5f - .4f * Mathf.Cos(photoElapsed * .07f));
        }
        string label = opacity >= .5f ? ArchiveCredit : FilmCredit;
        if (credit.text != label) credit.text = label;
        Fit(screen, 1, .5f);
    }

    static void Fit(RawImage image, float zoom, float position)
    {
        // Centre-crop the image to the current canvas without stretching it or
        // letting a world-space VR panel spill outside its own bounds.
        var texture = image.texture;
        var size = image.rectTransform.rect.size;
        if (!texture || size.x <= 0 || size.y <= 0) return;
        float ratio = (size.x / size.y) / ((float)texture.width / texture.height);
        float width = Mathf.Min(1, ratio) / zoom, height = Mathf.Min(1, 1 / ratio) / zoom;
        image.uvRect = new Rect((1 - width) * position, (1 - height) * .5f, width, height);
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        failed = true;
        screen.texture = filmStill;
        Debug.LogWarning("第四章結局影片無法解碼，使用已收錄的靜態影像。" + message, this);
    }

    void OnDestroy()
    {
        if (projector) projector.errorReceived -= OnVideoError;
    }
}
