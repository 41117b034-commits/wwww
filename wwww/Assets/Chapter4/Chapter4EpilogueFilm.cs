using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class Chapter4EpilogueFilm : MonoBehaviour
{
    const string Folder = "Chapter4Epilogue/";
    const string FilmCredit = "2015 年，清流部落　｜　安安女王《霧社事件的餘生後裔》／PeoPo　CC BY 3.0 TW";
    sealed class Shot
    {
        public readonly float start, focusY;
        public readonly string resource, caption;
        public Shot(float start, string resource, string caption, float focusY = .5f)
        {
            this.start = start; this.resource = resource; this.caption = caption; this.focusY = focusY;
        }
    }

    static readonly Shot[] personal =
    {
        new Shot(0, "WushePanorama", "日治時期，霧社全景　｜　臺北折井寫真館／國立臺灣歷史博物館"),
        new Shot(.36f, "WusheStreet", "日治時期，霧社街道　｜　國立臺灣歷史博物館典藏"),
        new Shot(.70f, "MhebuMountain", "1930 年，從霧卡散附近望向馬海僕富士山　｜　《霧社討伐寫真帖》", .74f)
    };
    static readonly Shot[] history1930 =
    {
        new Shot(0, "SchoolCourtyard", "1930 年 10 月 29 日，事發兩天後的霧社公學校操場　｜　《霧社討伐寫真帖》"),
        new Shot(.25f, "PuliAirfield", "1930 年，埔里臨時飛行場的日軍飛行員與偵察機　｜　《霧社討伐寫真帖》"),
        new Shot(.39f, "AfterAirRaid", "1930 年，日軍飛機投彈後的山區　｜　《霧社討伐寫真帖》"),
        new Shot(.53f, "TruwanSmoke", "1930 年，塔羅灣社遭日軍砲擊後升起的濃煙　｜　《霧社討伐寫真帖》"),
        new Shot(.66f, "MhebuAerial", "1930 年，日軍從空中拍下馬赫坡岩窟一帶　｜　《霧社討伐寫真帖》"),
        new Shot(.83f, "BoaalonVillage", "1930 年，日軍占領後的波阿倫社　｜　《第一第二霧社事件誌》")
    };
    static readonly Shot[] history1931 =
    {
        new Shot(0, "RodovRuins", "1931 年，羅多夫社遇襲後留下的屋址　｜　《第一第二霧社事件誌》"),
        new Shot(.25f, "Survivors1931", "1931 年，第二次霧社事件後被收容的倖存者　｜　《第一第二霧社事件誌》", .68f),
        new Shot(.53f, "Kawanakajima1931", "1931 年，川中島，被召集參加「和解式」的族人　｜　《第一第二霧社事件誌》", .62f),
        new Shot(.70f, "QingliuStill", FilmCredit)
    };

    Chapter4SceneController chapter;
    RawImage front, back;
    Text credit;
    VideoPlayer projector;
    Shot[] sequence;
    Texture2D[] stills;
    Texture2D finalFrame;
    float progress;
    bool startedVideo, videoFinished, videoFailed;

    public void Initialize(Chapter4SceneController owner, Text sourceCredit)
    {
        chapter = owner;
        credit = sourceCredit;
        Layer("映幕暗角").texture = Resources.Load<Texture2D>(Folder + "ProjectorShade");
        front = Layer("本張史料");
        back = Layer("前張史料");
        finalFrame = Resources.Load<Texture2D>(Folder + "QingliuLast");
        projector = gameObject.AddComponent<VideoPlayer>();
        projector.playOnAwake = false;
        projector.isLooping = false;
        projector.audioOutputMode = VideoAudioOutputMode.None;
        projector.renderMode = VideoRenderMode.APIOnly;
        projector.waitForFirstFrame = true;
        projector.clip = Resources.Load<VideoClip>(Folder + "Qingliu");
        projector.errorReceived += VideoError;
        projector.loopPointReached += VideoEnded;
        if (projector.clip) projector.Prepare();
    }

    RawImage Layer(string label)
    {
        var view = new GameObject(label, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        var rect = view.rectTransform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        view.raycastTarget = false;
        return view;
    }

    public void Show(Chapter4Ending.Stage stage)
    {
        sequence = stage == Chapter4Ending.Stage.Personal ? personal :
            stage == Chapter4Ending.Stage.History1930 ? history1930 : history1931;
        stills = new Texture2D[sequence.Length];
        for (int i = 0; i < sequence.Length; i++)
            stills[i] = Resources.Load<Texture2D>(Folder + sequence[i].resource);
        progress = 0;
        Refresh(1);
    }

    void Update()
    {
        if (sequence == null) return;
        if (chapter.IsPaused)
        {
            if (projector.isPlaying) projector.Pause();
            return;
        }
        var voice = chapter.Voice;
        if (voice.SpeakerRole != "narrator") return;
        var output = voice.Output;
        float duration = output.clip ? output.clip.length : Mathf.Max(.1f, voice.Duration);
        float clock = output.clip && output.isPlaying ? output.time : voice.Elapsed;
        // A language change can restart the line. Hold the displayed image
        // until that recording catches up instead of replaying earlier photos.
        progress = Mathf.Max(progress, Mathf.Clamp01(clock / duration));
        Refresh(duration);
    }

    void Refresh(float duration)
    {
        int i = 0;
        while (i + 1 < sequence.Length && progress >= sequence[i+1].start) i++;
        var shot = sequence[i];
        float secondsIn = (progress - shot.start) * duration;
        float blend = i == 0 ? 1 : Mathf.SmoothStep(0, 1, secondsIn / .48f);
        bool showFilm = sequence == history1931 && i == sequence.Length - 1;
        Texture current = stills[i];
        if (showFilm && !videoFailed)
        {
            if (videoFinished && finalFrame) current = finalFrame;
            else if (projector.isPrepared)
            {
                if (!startedVideo || projector.isPaused) { projector.Play(); startedVideo = true; }
                if (projector.frame >= 0 && projector.texture) current = projector.texture;
            }
        }
        front.texture = current;
        front.color = new Color(1, 1, 1, blend);
        float end = i+1 < sequence.Length ? sequence[i+1].start : 1;
        float travel = Mathf.InverseLerp(shot.start, end, progress);
        Fit(front, showFilm ? 1 : Mathf.Lerp(1,1.035f,travel), shot.focusY);
        back.enabled = i > 0 && blend < 1;
        if (back.enabled)
        {
            back.texture = stills[i-1];
            Fit(back, 1.035f, sequence[i-1].focusY);
        }
        credit.text = blend < .5f ? sequence[i-1].caption : shot.caption;
    }

    static void Fit(RawImage image, float zoom, float focusY)
    {
        var texture = image.texture;
        var size = image.rectTransform.rect.size;
        if (!texture || size.x <= 0 || size.y <= 0) return;
        float ratio = (size.x / size.y) / ((float)texture.width / texture.height);
        float width = Mathf.Min(1, ratio) / zoom, height = Mathf.Min(1, 1 / ratio) / zoom;
        image.uvRect = new Rect((1 - width) * .5f, (1 - height) * focusY, width, height);
    }

    void VideoEnded(VideoPlayer source) { videoFinished = true; }
    void VideoError(VideoPlayer source, string message)
    {
        videoFailed = true;
        Debug.LogWarning("清流部落影片無法解碼，顯示同一段紀錄片的劇照。" + message, this);
    }
    void OnDestroy()
    {
        if (!projector) return;
        projector.errorReceived -= VideoError;
        projector.loopPointReached -= VideoEnded;
    }
}
