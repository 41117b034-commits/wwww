using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public sealed partial class Chapter2Controller : MonoBehaviour
{
    public enum Stage { Intro, Follow, TreeChoice, Chopping, Consequence, Meeting, Vote, Ending, Complete }
    [Header("Scene references")]
    public Chapter2Player player;
    [Header("第二章音樂、歷史旁白與角色配音")]
    public Chapter2AudioManager chapterAudio;
    [Tooltip("片頭影片結束後，播放原本四句背景字幕與旁白；影片已有旁白時可關閉。")]
    public bool narrateAfterOpeningFilm = true;
    public Chapter2Presentation ui;
    public Transform sacredTree, treeApproach, meetingSpawn, campfire;
    public GameObject fallenStump, axe, dayGroup, nightGroup;
    public Chapter2Actor[] workers, leaders, conservatives;
    public Chapter2Actor officer, mona;
    public Chapter2Actor tado, bawan, watan;
    public Light sun, fireLight;
    public AudioSource ambience, effects, fireAudio;
    public AudioClip forestAudio, nightAudio, chopAudio, threatAudio;
    public Chapter2RouteGuide routeGuide;
    [Header("三分鐘自由探索與路邊交談")]
    [Min(1)] public float explorationSeconds = 180;
    public Chapter2Exploration exploration;
    public Chapter2ForestEscort forestEscort;
    public Collider trunkSurface;
    [Header("Replace this clip with the final opening film")]
    public VideoClip openingFilm;
    [Min(1)] public float videoPrepareTimeout = 12;
    [Header("片頭播放呈現")]
    public string openingTitle = "新規定與秘密會議";
    public string openingSubtitle = "霧社事件 · 第二章";
    [Min(0)] public float openingTitleSeconds = 2.2f;
    [Min(.05f)] public float openingFadeSeconds = .65f;
    [Range(0, 1)] public float openingFilmVolume = .85f;
    Chapter2OpeningScreen openingScreen;
    AudioSource openingSound;
    bool openingHudEnabled, openingPausedAmbience;
    [Tooltip("片頭期間暫停場景時間，主相機只繪製 UI 層；結束、略過與停用時自動恢復。")]
    public bool reduceSceneLoadDuringOpening = true;
    bool openingPerformanceActive;
    float openingSavedTimeScale;
    Camera openingView;
    int openingSavedMask;
    CameraClearFlags openingSavedClearFlags;
    Color openingSavedBackground;
    // Kept for older editor probes; completed chapters now continue into Chapter 3.
    [HideInInspector] public bool stopEditorAfterEnding = true;
    public bool saveResult = true;
    [Header("Interaction")]
    [Min(.1f)] public float treeArrivalDistance = 1.5f;
    public float interactionDistance = .7f;
    public float rhythmPeriod = 2.6f;
    public float lineSeconds = 4.5f;
    public Stage CurrentStage { get; private set; }
    public int TreeDecision { get; private set; } = -1;
    public int MeetingDecision { get; private set; } = -1;
    public int ValidCuts { get; private set; }
    public int FailedCuts { get; private set; }
    public int ConsecutiveFailedCuts { get; private set; }
    public float Integrity { get; private set; } = 100;
    public bool VideoPlayed { get; private set; }
    public bool VideoFinished { get; private set; }
    public bool Completed => CurrentStage == Stage.Complete;
    public float TreeDistance => Vector3.ProjectOnPlane(player.transform.position - treeApproach.position, Vector3.up).magnitude;
    public bool ArrivedAtTree => TreeDistance < treeArrivalDistance;
    public bool AtTree => TreeDistance <= interactionDistance;
    public Vector3 WorkerDestination(int index)
    {
        Vector3[] offsets = { new Vector3(-1.8f, 0, -2.1f), new Vector3(2.3f, 0, -2.4f), new Vector3(-2.9f, 0, -1.3f) };
        var p = sacredTree.position + offsets[index % offsets.Length]; p.y = workers[index].transform.position.y; return p;
    }
    public float RhythmPhase => Mathf.PingPong((Time.time - chopStarted) / rhythmPeriod, 1);
    public event Action<Stage> StageChanged;
    VideoPlayer video;
    RenderTexture videoTexture;
    bool videoError, videoEnded, skip, readyToChoose, canChop;
    bool meetingShortcutUsed;
    float chopStarted, nextCut, raisedFor;
    Coroutine flow;
    readonly string[] introLines = {
        "警察要求族人集合，宣讀新的伐木與搬運規定。",
        "木材不准拖曳，必須肩扛下山。陡坡與長途搬運，使族人的生活更加艱困。",
        "警察進一步命令族人前往聖地「西仔希克」，砍伐被視為守護者的巨木。",
        "在槍口與鞭子的威逼下，族人走入森林。壓抑的憤怒，逐漸化為反抗的決心。"
    };
    void Start()
    {
        if (!player || !ui || !sacredTree || !treeApproach || !meetingSpawn || !officer || !mona || !sun)
        { Debug.LogError("[Chapter2] Required scene references are missing.", this); enabled = false; return; }
        if (!chapterAudio) chapterAudio = GetComponent<Chapter2AudioManager>();
        if (!chapterAudio) chapterAudio = gameObject.AddComponent<Chapter2AudioManager>();
        if (!exploration) exploration = GetComponent<Chapter2Exploration>();
        if (!exploration) exploration = gameObject.AddComponent<Chapter2Exploration>();
        exploration.chapter = this;
        if (!forestEscort) forestEscort = GetComponent<Chapter2ForestEscort>();
        if (!forestEscort) forestEscort = gameObject.AddComponent<Chapter2ForestEscort>();
        forestEscort.Initialize(this);
        Chapter2CastNames.Apply(this);
        ui.buttonA.onClick.AddListener(SelectA); ui.buttonB.onClick.AddListener(SelectB);
        ui.continueButton.onClick.AddListener(ContinueToChapter3);
        player.canMove = false; player.canLook = false;
        dayGroup.SetActive(true); nightGroup.SetActive(false); fallenStump.SetActive(false); axe.SetActive(false);
        ambience.clip = forestAudio; ambience.loop = true; ambience.Play();
        if (routeGuide) { routeGuide.chapter = this; player.routeGuide = routeGuide; }
        flow = StartCoroutine(Run());
    }
    void SetStage(Stage value)
    {
        CurrentStage = value;
        if (chapterAudio) chapterAudio.MusicForStage(value, TreeDecision, MeetingDecision);
        StageChanged?.Invoke(value); Debug.Log("[Chapter2] Stage=" + value);
    }
    void Update()
    {
        if (!player || !ui) return;
        ui.ConfigureVR(player.IsVR);
        if (openingScreen) openingScreen.Configure(player.IsVR, player.view);
        if ((!exploration || !exploration.ChatOpen) && Chapter2Player.Key(Chapter2Player.KeyControlName.P)) SkipToMeeting();
        if (CurrentStage == Stage.Intro && player.ActionPressed) skip = true;
        if (CurrentStage == Stage.TreeChoice || CurrentStage == Stage.Vote)
        {
            if (player.PrimaryPressed) SelectA();
            else if (player.SecondaryPressed) SelectB();
        }
        if (CurrentStage == Stage.Vote)
        {
            raisedFor = player.HandRaised ? raisedFor + Time.deltaTime : 0;
            if (raisedFor > 0.85f) SelectA();
        }
        if (CurrentStage == Stage.Chopping)
        {
            ui.SetMeter(RhythmPhase, ValidCuts);
            if (player.ActionPressed) TryChop();
        }
        if (CurrentStage == Stage.Complete && player.ActionPressed) ContinueToChapter3();
    }
    public void SelectA() { Choose(0); }
    public void SelectB() { Choose(1); }
    // Preview the council without replaying the film, escort, and logging choice.
    public void SkipToMeeting()
    {
        if (!isActiveAndEnabled || flow == null || meetingShortcutUsed || (int)CurrentStage >= (int)Stage.Meeting) return;
        meetingShortcutUsed = true;
        if (exploration) exploration.CancelExploration();
        // Includes the separate axe-swing coroutine, which can otherwise change the UI later.
        StopAllCoroutines();
        CloseOpeningScreen(false);
        if (chapterAudio) chapterAudio.StopAllAudio();
        foreach (var actor in workers) if (actor) actor.speaking = false;
        skip = true; readyToChoose = false; canChop = false; raisedFor = 0;
        if (video)
        {
            video.errorReceived -= VideoError; video.loopPointReached -= VideoEnd;
            video.Stop(); Destroy(video); video = null;
        }
        ui.videoImage.gameObject.SetActive(false); ui.videoImage.texture = null;
        if (videoTexture) { videoTexture.Release(); Destroy(videoTexture); videoTexture = null; }
        effects.Stop(); axe.SetActive(false);
        if (routeGuide) routeGuide.GuidanceEnabled = false;
        ui.HideChoices(); ui.meterPanel.SetActive(false); ui.endingPanel.SetActive(false);
        ui.Line("", ""); ui.hint.text = ""; ui.fade.color = Color.black;
        player.canMove = false; player.canLook = false;
        flow = StartCoroutine(RunMeeting());
    }
    public bool TryGetChopContact(out RaycastHit contact)
    {
        contact = default;
        Vector3 origin = player.view.transform.position; origin.y = sacredTree.position.y + 1.15f;
        Vector3 inward = sacredTree.position + Vector3.up * 1.15f - origin;
        return trunkSurface && trunkSurface.Raycast(new Ray(origin, inward.normalized), out contact, 2.9f);
    }
    public void Choose(int choice)
    {
        if (choice < 0 || choice > 1 || !readyToChoose) return;
        if (CurrentStage == Stage.TreeChoice && routeGuide.EscortArrived)
        { TreeDecision = choice; readyToChoose = false; ui.HideChoices(); }
        else if (CurrentStage == Stage.Vote)
        { MeetingDecision = choice; readyToChoose = false; ui.HideChoices(); }
    }
    public bool TryChop()
    {
        if (CurrentStage != Stage.Chopping || !canChop || Time.time < nextCut) return false;
        var tool = axe.GetComponent<Chapter2Axe>();
        if (!tool || tool.IsSwinging) return false;
        if (!AtTree)
        { ui.hint.text = "靠近巨木前的斧痕，再進行砍伐。"; return false; }
        Vector3 direction = sacredTree.position + Vector3.up * 1.2f - player.view.transform.position;
        if (Vector3.Angle(player.view.transform.forward, direction) > 40)
        { ui.hint.text = "面向巨木的樹幹，再按 E／右手扳機。"; return false; }
        ui.hint.text = "游標進入綠色區域時，按 E／空白鍵／右手扳機。避免傷及木材。";
        if (!TryGetChopContact(out RaycastHit contact))
        { ui.hint.text = "再靠近樹幹正面的黃色位置，讓斧刃能碰到木頭。"; return false; }
        nextCut = Time.time + .85f;
        float phase = RhythmPhase;
        StartCoroutine(SwingAxe(tool, contact, phase >= .32f && phase <= .68f));
        return true;
    }
    IEnumerator Run()
    {
        SetStage(Stage.Intro); ui.objective.text = "新規定"; ui.hint.text = "E／空白鍵／右手扳機  略過片頭  |  P  直達夜晚會議";
        yield return Intro();
        ui.videoImage.gameObject.SetActive(false); ui.fade.color = Color.black;
        player.Warp(new Vector3(0, .08f, -17), sacredTree.position);
        player.canMove = false; player.canLook = false;
        if (routeGuide) routeGuide.GuidanceEnabled = false;
        SetStage(Stage.Follow); ui.objective.text = "跟隨族人，前往巨木";
        ui.hint.text = "WASD 移動・按住滑鼠右鍵環顧  |  VR 左搖桿移動、右搖桿轉向";
        yield return Fade(0, 1.2f);
        yield return IntroduceForest();
        yield return exploration.Explore(explorationSeconds);
        yield return BeginForestEscort();
        yield return forestEscort.LeadToTree();
        workers[0].Face(sacredTree.position);
        if (routeGuide) routeGuide.EscortArrived = true;
        while (!ArrivedAtTree)
        { ui.objective.text = "沿黃色箭頭走近巨木"; ui.hint.text = $"距離黃色標記小於 {treeArrivalDistance:0.0} 公尺，就會自動進入劇情。"; yield return null; }
        player.canMove = false;
        forestEscort.StopWitnessPatrols();
        routeGuide.GuidanceEnabled = false;
        ui.hint.text = ""; ui.objective.text = "聆聽警察與族人的對話";
        yield return FrameTreeExchange();
        yield return Say(officer, "日本警察", "把這些樹都砍了。", 0, workers[0]);
        yield return StepForwardToOfficer();
        yield return Say(workers[0], "族人", "這棵巨木是我們的守護者……真的要砍下去嗎？", 0, officer);
        yield return FrameConfrontation();
        SetStage(Stage.TreeChoice); ui.objective.text = "面對聖地的抉擇";
        ui.hint.text = "按 1／右手主按鈕，或按 2／左手主按鈕";
        ui.Choices("你要如何回應伐木命令？", "1  保護巨樹", "2  砍伐巨樹"); readyToChoose = true;
        while (TreeDecision < 0) yield return null;
        if (TreeDecision == 0) yield return Protect(); else yield return Fell();
        yield return Fade(1, 1.6f);
        ui.Line("", ""); ui.hint.text = ""; yield return new WaitForSeconds(.6f);
        yield return RunMeeting();
    }
    IEnumerator RunMeeting()
    {
        SetNight();
        PrepareCouncilDrama();
        player.Warp(meetingSpawn.position, campfire.position);
        yield return FrameCouncilOverview(.01f);
        SetStage(Stage.Meeting); ui.chapter.text = "第二章  /  夜晚的秘密會議"; ui.objective.text = "聆聽營火旁的密議";
        ui.ShowLocationCaption("夜晚，山林深處。");
        yield return Fade(0, 1.5f);
        yield return new WaitForSeconds(3);
        for (float t = 0; t < 1; t += Time.deltaTime / .6f) { ui.ShowLocationCaption("夜晚，山林深處。", 1 - t); yield return null; }
        ui.ShowLocationCaption("");
        yield return CouncilScript();
        yield return FrameCouncilOverview();
        SetStage(Stage.Vote); ui.objective.text = "表達你的立場"; ui.hint.text = "1 支持／2 拒絕  |  VR 舉起右手支持，或按左手主按鈕拒絕";
        ui.Choices("你願意跟隨莫那·魯道嗎？", "1  同意，跟隨起義", "2  拒絕，我想活下去"); readyToChoose = true;
        while (MeetingDecision < 0) yield return null;
        SetStage(Stage.Ending); ui.hint.text = "";
        if (MeetingDecision == 0) yield return CouncilAgreement();
        else yield return CouncilRefusal();
        ui.Line("", "");
        if (MeetingDecision == 0) yield return FrameCouncilOverview(5, true);
        yield return Fade(1, 2);
        if (MeetingDecision == 0)
        {
            ui.endingPanel.SetActive(true); ui.continueButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(3);
        }
        else yield return new WaitForSeconds(.8f);
        SetStage(Stage.Complete);
        FinishChapterAndContinue();
    }
    IEnumerator Intro()
    {
        videoError = false; videoEnded = false; VideoPlayed = false; VideoFinished = false;
        openingHudEnabled = ui.canvas.enabled; ui.canvas.enabled = false;
        openingPausedAmbience = ambience && ambience.isPlaying;
        if (openingPausedAmbience) ambience.Pause();
        // SetStage(Intro) already starts Intro Music; keep it playing with the film.
        if (chapterAudio) chapterAudio.StopVoice();
        openingScreen = Chapter2OpeningScreen.Build(ui, SkipIntro, openingTitle, openingSubtitle);
        openingScreen.Configure(player.IsVR, player.view);
        BeginOpeningPerformanceMode();
        // Decoder preparation overlaps the title card, avoiding an empty loading screen.
        var clip = openingFilm;
        float prepareBegan = Time.realtimeSinceStartup;
        if (clip)
        {
            video = gameObject.AddComponent<VideoPlayer>(); video.playOnAwake = false; video.isLooping = false; video.clip = clip;
            video.waitForFirstFrame = true; video.skipOnDrop = true;
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            if (clip.audioTrackCount > 0)
            {
                openingSound = gameObject.AddComponent<AudioSource>(); openingSound.playOnAwake = false; openingSound.loop = false;
                openingSound.spatialBlend = 0; openingSound.volume = 0;
                if (effects) openingSound.outputAudioMixerGroup = effects.outputAudioMixerGroup;
                video.audioOutputMode = VideoAudioOutputMode.AudioSource; video.controlledAudioTrackCount = 1;
                video.EnableAudioTrack(0, true); video.SetTargetAudioSource(0, openingSound);
            }
            else video.audioOutputMode = VideoAudioOutputMode.None;
            int width = Mathf.Clamp((int)clip.width, 16, 1920);
            int height = Mathf.Clamp(Mathf.RoundToInt(width * (float)clip.height / Mathf.Max(1, (float)clip.width)), 16, 1920);
            videoTexture = new RenderTexture(width, height, 0); videoTexture.Create();
            var previous = RenderTexture.active; RenderTexture.active = videoTexture; GL.Clear(true, true, Color.black); RenderTexture.active = previous;
            video.renderMode = VideoRenderMode.RenderTexture; video.targetTexture = videoTexture;
            openingScreen.Film.texture = videoTexture;
            float pixelAspect = clip.pixelAspectRatioDenominator > 0 ? (float)clip.pixelAspectRatioNumerator / clip.pixelAspectRatioDenominator : 1;
            openingScreen.SetAspect((float)clip.width / Mathf.Max(1, (float)clip.height) * pixelAspect);
            video.errorReceived += VideoError; video.loopPointReached += VideoEnd; video.Prepare();
        }
        yield return OpeningTitleFade(0, 1);
        for (float t = 0; t < openingTitleSeconds && !skip; t += Time.unscaledDeltaTime) yield return null;
        yield return OpeningTitleFade(1, 0);
        if (clip && !skip)
        {
            openingScreen.Status.text = "影片準備中…";
            while (!video.isPrepared && !videoError && !skip && Time.realtimeSinceStartup - prepareBegan < Mathf.Max(1, videoPrepareTimeout)) yield return null;
            openingScreen.Status.text = "";
            if (video.isPrepared && !videoError && !skip)
            {
                openingScreen.Cover.color = Color.black;
                openingScreen.Film.gameObject.SetActive(true); video.Play();
                float firstFrameDeadline = Time.realtimeSinceStartup + 8;
                while (video.frame < 0 && !videoEnded && !videoError && !skip && Time.realtimeSinceStartup < firstFrameDeadline) yield return null;
                if (video.frame >= 0 && !videoError && !skip)
                {
                    VideoPlayed = true;
                    yield return OpeningCoverFade(1, 0);
                    float deadline = Time.realtimeSinceStartup + Mathf.Max(1, (float)clip.length) + 8;
                    while (!videoEnded && !videoError && !skip && Time.realtimeSinceStartup < deadline) yield return null;
                    VideoFinished = videoEnded;
                    yield return OpeningCoverFade(0, 1);
                }
            }
        }
        CloseOpeningScreen(true);
        // Existing narrated captions remain available, including the decoder fallback.
        if ((narrateAfterOpeningFilm || !VideoPlayed || videoError || !videoEnded) && !skip)
        {
            ui.hint.text = "E／空白鍵／右手扳機  略過片頭";
            yield return Fade(0, .7f);
            for (int i = 0; i < introLines.Length && !skip; i++)
            {
                ui.Line("新規定", introLines[i]);
                float hold = BeginChapterVoice(null, "旁白", introLines[i], 6);
                for (float t = 0; t < hold && !skip; t += Time.unscaledDeltaTime) yield return null;
                if (chapterAudio) chapterAudio.StopVoice();
            }
            yield return Fade(1, .6f);
        }
        ui.Line("", "");
    }
    IEnumerator OpeningTitleFade(float from, float to)
    {
        for (float t = 0; t < 1 && !skip; t += Time.unscaledDeltaTime / Mathf.Max(.05f, openingFadeSeconds))
        { if (!openingScreen) yield break; openingScreen.Title.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t)); yield return null; }
        if (openingScreen) openingScreen.Title.alpha = to;
    }
    IEnumerator OpeningCoverFade(float from, float to)
    {
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(.05f, openingFadeSeconds))
        {
            if (!openingScreen) yield break;
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t)); openingScreen.Cover.color = new Color(0, 0, 0, a);
            if (openingSound) openingSound.volume = openingFilmVolume * (1 - a);
            if (skip && to == 0) break;
            yield return null;
        }
        if (openingScreen) openingScreen.Cover.color = new Color(0, 0, 0, to);
    }
    void BeginOpeningPerformanceMode()
    {
        if (!reduceSceneLoadDuringOpening || openingPerformanceActive) return;
        openingPerformanceActive = true;
        openingSavedTimeScale = Time.timeScale; Time.timeScale = 0;
        openingView = player.view;
        if (openingView)
        {
            openingSavedMask = openingView.cullingMask;
            openingSavedClearFlags = openingView.clearFlags;
            openingSavedBackground = openingView.backgroundColor;
            openingView.cullingMask = 1 << 5; // Built-in UI layer used by the opening screen.
            openingView.clearFlags = CameraClearFlags.SolidColor;
            openingView.backgroundColor = Color.black;
        }
    }
    void EndOpeningPerformanceMode()
    {
        if (!openingPerformanceActive) return;
        openingPerformanceActive = false; Time.timeScale = openingSavedTimeScale;
        if (openingView)
        {
            openingView.cullingMask = openingSavedMask;
            openingView.clearFlags = openingSavedClearFlags;
            openingView.backgroundColor = openingSavedBackground;
        }
        openingView = null;
    }
    void CloseOpeningScreen(bool resume)
    {
        EndOpeningPerformanceMode();
        bool hadScreen = openingScreen;
        if (video) { video.errorReceived -= VideoError; video.loopPointReached -= VideoEnd; video.Stop(); Destroy(video); video = null; }
        if (openingSound) { openingSound.Stop(); Destroy(openingSound); openingSound = null; }
        if (openingScreen) { openingScreen.gameObject.SetActive(false); Destroy(openingScreen.gameObject); openingScreen = null; }
        if (videoTexture) { videoTexture.Release(); Destroy(videoTexture); videoTexture = null; }
        if (hadScreen && ui) { ui.fade.color = Color.black; ui.canvas.enabled = openingHudEnabled; }
        if (resume)
        {
            if (openingPausedAmbience && ambience) ambience.UnPause();
            if (chapterAudio) chapterAudio.MusicForStage(CurrentStage, TreeDecision, MeetingDecision);
        }
        openingPausedAmbience = false;
    }
    void VideoError(VideoPlayer source, string message) { videoError = true; Debug.LogWarning("[Chapter2] Opening film unavailable; using story captions. " + message); }
    void VideoEnd(VideoPlayer source) { videoEnded = true; }
    public void SkipIntro() { if (CurrentStage == Stage.Intro) skip = true; }
    IEnumerator Protect()
    {
        SetStage(Stage.Consequence); ui.objective.text = "保護巨樹"; ui.hint.text = "";
        // Prepare the hand meshes before the threat, keeping the gunshot frame smooth.
        for (int i = 1; i < workers.Length; i++)
            workers[i].gameObject.AddComponent<Chapter2GriefReaction>().Prepare();
        var blocking = workers[0].gameObject.AddComponent<Chapter2BlockingPose>();
        blocking.Prepare();
        // Speak while approaching, then hold a close confrontation at the rifle's reach.
        var start = workers[0].transform.position;
        var block = officer.transform.position + new Vector3(-1.7f, 0, .15f); block.y = start.y;
        yield return CameraShot(new Vector3(1.1f, 1.65f, 2.5f), new Vector3(1.1f, .85f, 7.4f), .65f);
        CameraBeat = "blocking";
        ui.Line(workers[0].DisplayName, "別碰它！這是我們的聖地。");
        float blockVoiceEnd = Time.unscaledTime + BeginChapterVoice(workers[0], "族人", "別碰它！這是我們的聖地。", 4.3f);
        workers[0].speaking = true; workers[0].Rig.conversationTarget = officer.Rig.Head;
        for (float t = 0; t < 1; t += Time.deltaTime / 2.1f)
        {
            workers[0].transform.position = Vector3.Lerp(start, block, Mathf.SmoothStep(0, 1, t));
            officer.Face(workers[0].transform.position); yield return null;
        }
        workers[0].transform.position = block;
        blocking.Raise();
        CameraBeat = "shooting";
        workers[0].Face(officer.transform.position); officer.Face(workers[0].transform.position);
        yield return new WaitForSeconds(2.2f);
        while (Time.unscaledTime < blockVoiceEnd) yield return null;
        if (chapterAudio) chapterAudio.StopVoice();
        workers[0].speaking = false; workers[0].Rig.conversationTarget = null;
        var rifle = officer.GetComponent<Chapter2Rifle>();
        rifle.target = workers[0];
        for (float t = 0; t < 1; t += Time.deltaTime / 1.3f) { rifle.aim = Mathf.SmoothStep(0, 1, t); yield return null; }
        rifle.aim = 1;
        yield return Say(officer, "日本警察", "退開！誰敢違抗命令？", 4, workers[0]);
        yield return Say(workers[0], "族人", "這是祖靈守護的地方……我們不能退。 ", 1.2f, officer);
        rifle.Fire();
        if (threatAudio) effects.PlayOneShot(threatAudio, .45f);
        workers[0].BeginFall(officer.transform.position);
        CameraBeat = "shot-lowering";
        // Lower immediately while the casualty collapses; witnesses wait for the fall.
        for (float t = 0; t < 1; t += Time.deltaTime / .42f)
        {
            rifle.aim = 1 - Mathf.SmoothStep(0, 1, t); rifle.lowered = Mathf.SmoothStep(0, 1, t);
            yield return null;
        }
        rifle.aim = 0; rifle.lowered = 1;
        ui.Line("族人", "槍聲過後，一名阻擋警察的族人倒下。同伴急忙上前查看。");
        float rescueVoiceEnd = Time.unscaledTime + BeginChapterVoice(null, "旁白", "槍聲過後，一名阻擋警察的族人倒下。同伴急忙上前查看。", 0);
        while (workers[0].FallProgress < 1) yield return null;
        CameraBeat = "rescue";
        for (int i = 1; i < workers.Length; i++)
        {
            Vector3 beside = workers[0].transform.position + new Vector3(i == 1 ? .65f : -.95f, 0, i == 1 ? .95f : 1.1f);
            workers[i].GetComponent<Chapter2GriefReaction>().Begin(workers[0], .12f + (i - 1) * .2f, beside);
        }
        foreach (var worker in workers)
        {
            var reaction = worker.GetComponent<Chapter2GriefReaction>();
            if (reaction) while (!reaction.Examining) yield return null;
        }
        CameraBeat = "checking-casualty";
        yield return new WaitForSeconds(2.2f);
        while (Time.unscaledTime < rescueVoiceEnd) yield return null;
        if (chapterAudio) chapterAudio.StopVoice();
        yield return OrderSurvivorsToTrees(rifle);
    }
    IEnumerator Fell()
    {
        yield return ClearChoppingSpace();
        yield return RestoreChoppingView();
        SetStage(Stage.Chopping); canChop = true; player.canMove = true; axe.SetActive(true); chopStarted = Time.time;
        if (TryGetChopContact(out RaycastHit focus)) player.FocusOn(focus.point);
        ui.Line("伐木", "握穩木柄斧。等游標進入綠色區域，再朝樹幹落斧。");
        ui.SetMeter(RhythmPhase, ValidCuts);
        ui.objective.text = "對準樹幹，小心落斧"; ui.meterPanel.SetActive(true);
        ui.hint.text = "游標進入綠色區域時，按 E／空白鍵／右手扳機。避免傷及木材。";
        while (ValidCuts < 5)
        {
            if (ConsecutiveFailedCuts >= 3)
            {
                canChop = false; player.canMove = false; ui.meterPanel.SetActive(false);
                axe.SetActive(false); yield return FrameSpeaker(officer);
                yield return Say(officer, "日本警察", "木材不能再受損！放慢動作，重新找準落點。", 4);
                yield return RestoreChoppingView(); axe.SetActive(true);
                Integrity = 100; ValidCuts = 0; ConsecutiveFailedCuts = 0; chopStarted = Time.time; canChop = true; player.canMove = true;
                ui.SetMeter(RhythmPhase, ValidCuts); ui.meterPanel.SetActive(true);
            }
            yield return null;
        }
        SetStage(Stage.Consequence); canChop = false; player.canMove = false; ui.meterPanel.SetActive(false); ui.hint.text = "";
        axe.SetActive(false); ui.objective.text = "巨木倒下";
        yield return FallingTreeWarning();
        foreach (var c in sacredTree.GetComponentsInChildren<Collider>()) c.enabled = false;
        Quaternion start = sacredTree.rotation;
        Vector3 axis = Vector3.Cross(Vector3.up, TreeFallDirection).normalized;
        for (float t = 0; t < 1; t += Time.deltaTime / 4)
        { TreeFallProgress = t; sacredTree.rotation = Quaternion.AngleAxis(82 * t * t, axis) * start; yield return null; }
        sacredTree.rotation = Quaternion.AngleAxis(82, axis) * start; TreeFallProgress = 1;
        fallenStump.SetActive(true);
        yield return new WaitForSeconds(.6f);
        foreach (var worker in workers) worker.GetComponent<Chapter2StartleReaction>().Release();
        yield return new WaitForSeconds(.65f);
        yield return FrameSpeaker(workers[0]);
        yield return Say(workers[0], "族人", "命令完成了。可是，我們該怎麼面對祖靈？", 5);
    }
    IEnumerator SwingAxe(Chapter2Axe tool, RaycastHit contact, bool accurate)
    {
        player.canMove = false;
        yield return tool.Swing(contact, () => {
            if (accurate) { ValidCuts++; ConsecutiveFailedCuts = 0; ui.Line("伐木", "斧刃切進樹皮，木屑飛散。放穩斧頭，等待下一次時機。"); }
            else { FailedCuts++; ConsecutiveFailedCuts++; Integrity = Mathf.Max(0, Integrity - 20); ui.Line("伐木", "落斧偏了，木材受到損傷。等游標進入綠色區域，再落斧。"); }
            ui.SetMeter(RhythmPhase, ValidCuts);
            if (chopAudio) effects.PlayOneShot(chopAudio, .7f);
        });
        if (CurrentStage == Stage.Chopping && canChop) player.canMove = true;
    }
    IEnumerator Say(Chapter2Actor actor, string name, string words, float seconds = 0, Chapter2Actor listener = null)
    {
        if (actor && CurrentStage >= Stage.Meeting && nightGroup.activeInHierarchy)
        { ui.Line("", ""); yield return FrameCouncilSpeaker(actor); }
        string display = actor && (name == "族人" || name == "日本警察") ? actor.DisplayName : name;
        ui.Line(display, words);
        if (actor)
        {
            actor.speaking = true;
            if (listener) { actor.Face(listener.transform.position); listener.Face(actor.transform.position); }
            if (actor.Rig) actor.Rig.conversationTarget = listener ? (listener.Rig.Head ? listener.Rig.Head : listener.transform) : player.view.transform;
        }
        yield return WaitChapterVoice(BeginChapterVoice(actor, name, words, seconds > 0 ? seconds : lineSeconds));
        if (actor) actor.speaking = false;
    }
    float BeginChapterVoice(Chapter2Actor actor, string speaker, string words, float fallback)
    {
        var role = Chapter2AudioManager.Role.Villager;
        if (speaker == "旁白") role = Chapter2AudioManager.Role.Narrator;
        else if (actor == officer || speaker == "日本警察") role = Chapter2AudioManager.Role.Officer;
        else if (actor && actor == mona) role = Chapter2AudioManager.Role.Mona;
        else if (actor && actor == tado) role = Chapter2AudioManager.Role.Tado;
        else if (actor && actor == bawan) role = Chapter2AudioManager.Role.Bawan;
        else if (actor && actor == watan) role = Chapter2AudioManager.Role.Watan;
        else if (speaker == "玩家") role = Chapter2AudioManager.Role.Player;
        else if (speaker == "巴萬與眾戰士") role = Chapter2AudioManager.Role.Warriors;
        float seconds = chapterAudio ? chapterAudio.BeginLine(role, words, fallback) : fallback;
        if (actor && !actor.police) actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(words, seconds, chapterAudio);
        else if (role == Chapter2AudioManager.Role.Warriors)
            foreach (var participant in councilCast)
                if (participant && participant.speaking) participant.GetComponent<Chapter2SpeechFace>()?.BeginLine(words, seconds, chapterAudio);
        return seconds;
    }
    IEnumerator WaitChapterVoice(float seconds)
    {
        float end = Time.unscaledTime + Mathf.Max(0, seconds);
        while (Time.unscaledTime < end || (chapterAudio && chapterAudio.VoicePlaying)) yield return null;
        if (chapterAudio) chapterAudio.StopVoice();
    }
    public void SetNight()
    {
        var loggedForest = GetComponent<Chapter2LoggedForest>();
        if (loggedForest) loggedForest.ApplyNight();
        else if (TreeDecision == 1) { sacredTree.gameObject.SetActive(false); fallenStump.SetActive(false); }
        dayGroup.SetActive(false); nightGroup.SetActive(true); sun.color = new Color(.38f, .5f, .78f); sun.intensity = .25f; sun.transform.rotation = Quaternion.Euler(35, -40, 0);
        mona.Face(campfire.position);
        foreach (var leader in leaders) leader.Face(campfire.position);
        foreach (var conservative in conservatives) conservative.Face(campfire.position);
        RenderSettings.ambientLight = new Color(.12f, .17f, .24f); RenderSettings.ambientIntensity = .45f; RenderSettings.fogColor = new Color(.025f, .04f, .065f); RenderSettings.fogDensity = .018f;
        player.view.backgroundColor = RenderSettings.fogColor; fireLight.gameObject.SetActive(true);
        ambience.clip = nightAudio; ambience.Play(); if (fireAudio) fireAudio.Play();
    }
    IEnumerator Fade(float target, float seconds)
    {
        float from = ui.fade.color.a;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(.01f, seconds)) { ui.fade.color = new Color(0, 0, 0, Mathf.Lerp(from, target, t)); yield return null; }
        ui.fade.color = new Color(0, 0, 0, target);
    }
    void OnDisable()
    {
        CloseOpeningScreen(false);
        if (exploration) exploration.CancelExploration();
        if (chapterAudio) chapterAudio.StopAllAudio();
        if (flow != null) StopCoroutine(flow);
        if (video) { video.errorReceived -= VideoError; video.loopPointReached -= VideoEnd; video.Stop(); }
        if (videoTexture) { videoTexture.Release(); Destroy(videoTexture); }
        if (ui) { ui.buttonA.onClick.RemoveListener(SelectA); ui.buttonB.onClick.RemoveListener(SelectB); ui.continueButton.onClick.RemoveListener(ContinueToChapter3); }
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    [Serializable]
    public class Chapter2Result
    {
        public string treeChoice, meetingChoice;
        public float woodIntegrity;
        public int validCuts, failedCuts, casualties;
        public bool completed;
    }
}
