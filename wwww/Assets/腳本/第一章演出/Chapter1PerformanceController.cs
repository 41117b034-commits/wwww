using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Chapter1PerformanceController : MonoBehaviour
{
    public enum ConflictChoice
    {
        Intervene,
        Watch
    }

    [Header("Player Control")]
    public MonoBehaviour playerController;
    public CharacterController characterController;

    [Header("Performance Timeline")]
    public PlayableDirector danceTimeline;
    public PlayableDirector policeEnterTimeline;
    public PlayableDirector interveneTimeline;
    public PlayableDirector watchTimeline;
    public PlayableDirector endingTimeline;

    [Header("Scene Objects")]
    public GameObject policeGroup;
    public GameObject weddingNpcGroup;
    public GameObject choiceFocusPoint;

    [Header("Immersive Dance")]
    public Transform playerRoot;
    public Transform danceCenter;
    public float danceDuration = 9f;
    public float danceRadius = 3.2f;
    public float danceOrbitDegrees = 330f;
    public float danceStepHeight = 0.1f;
    public float danceStepFrequency = 2.4f;
    public float danceLookAtHeight = 1.25f;
    public float danceFollowSharpness = 14f;
    public bool startPoliceAfterDance = false;

    [Header("Wedding Crowd Dance")]
    public bool autoStartWeddingCrowdDance = true;
    public float weddingCrowdDanceRange = 95f;
    public float weddingCrowdOrbitSpeedDegrees = -34f;
    public float weddingCrowdStepHeight = 0.12f;
    public float weddingCrowdStepFrequency = 2.2f;
    public float weddingCrowdRadialStepDistance = 0.12f;
    public float weddingCrowdSwayDegrees = 5f;

    [Header("UI")]
    public Chapter1DialogueUI dialogueUI;
    public Chapter1ChoiceUI choiceUI;
    public CanvasGroup fadeCanvas;

    [Header("Audio")]
    public AudioSource weddingAmbience;
    public AudioSource tensionAmbience;
    public AudioSource heartbeatAudio;

    [Header("Opening Narration")]
    public AudioSource narrationAudio;
    public AudioClip introVoice1;
    public AudioClip introVoice2;
    [Range(0f, 1f)] public float narrationVolume = 1f;
    public float introLineGap = 0.35f;

    [Header("Quest Progress")]
    public int wineTargetCount = 3;
    public int foodTargetCount = 2;
    public bool requireWineAndFoodBeforeDance = true;
    public bool requireAllWeddingTasksBeforePolice = true;
    public bool autoStartPoliceAfterWeddingTasks = true;
    public float policeStartDelayAfterWeddingTasks = 1.5f;
    public bool showWeddingQuestProgress = true;
    public bool autoStartOnAwake = false;

    [Header("Debug")]
    public bool debugStartPoliceWithP = true;
    public bool debugStartDanceWithJ = true;

    [Header("Chapter 1 Story Flow")]
    public bool autoBeginStoryIfControllerExists = true;
    public bool autoFindMissingReferences = true;
    public bool requireWineBeforeDance = false;
    public bool allowChoiceHotkeys = true;
    public bool showFallbackHud = true;
    public bool lockPlayerDuringPoliceEntrance = false;
    public float defaultDialogueSeconds = 3f;

    [Header("Free Exploration Timer")]
    public bool useExplorationTimer = true;
    public float explorationDurationSeconds = 180f;
    public bool showExplorationTimer = true;
    public bool pauseCountdownDuringDance = false;
    public string explorationTimerTitle = "自由探索";

    [Header("Auto Exploration Interactions")]
    public bool autoCreateMissingExplorationInteractions = true;
    public float autoInteractionDistance = 4.5f;
    public float autoInteractionMarkerSize = 0.7f;
    public bool showCenterInteractionMenu = true;
    public bool showWorldInteractionPrompt = true;
    public float centerInteractionRange = 18f;
    public bool requireFireDistanceForCenterMenu = true;
    public bool preferControllerPositionForFireCenter = true;
    public float maxDanceCenterOffsetForFireCenter = 18f;
    public KeyCode centerDanceKey = KeyCode.E;
    public KeyCode centerWineKey = KeyCode.R;
    public KeyCode centerFoodKey = KeyCode.T;
    public KeyCode centerDanceAltKey = KeyCode.Alpha1;
    public KeyCode centerWineAltKey = KeyCode.Alpha2;
    public KeyCode centerFoodAltKey = KeyCode.Alpha3;
    public KeyCode centerDanceGamepadKey = KeyCode.JoystickButton0;
    public KeyCode centerWineGamepadKey = KeyCode.JoystickButton1;
    public KeyCode centerFoodGamepadKey = KeyCode.JoystickButton2;
    public bool reserveEForDanceAtFireCenter = true;

    [Header("Interaction Animation")]
    public bool playInteractionAnimations = true;
    public float interactionAnimationSeconds = 2.4f;
    public float interactionArcHeight = 1.15f;
    public float heldPropScale = 0.28f;
    public bool lockPlayerDuringInteractionAnimation = false;
    public bool guidePlayerDuringItemInteractions = true;
    public float guidedWalkToPickupSeconds = 1.15f;
    public float guidedWalkToReceiverSeconds = 1.65f;
    public float guidedGiveSeconds = 0.55f;
    public float guidedPickupDistance = 1.15f;
    public float guidedReceiverDistance = 1.35f;
    public bool keepGuidedPlayerAboveGround = true;
    public LayerMask guidedGroundLayers = ~0;
    public float guidedGroundRaycastHeight = 25f;
    public float guidedGroundRaycastDistance = 80f;
    public float guidedGroundClearance = 0.06f;
    public float firstPersonHoldSeconds = 0.85f;
    public float carriedPropViewDistance = 0.95f;
    public float carriedPropViewRightOffset = 0.22f;
    public float carriedPropViewDownOffset = 0.05f;
    public Color winePropColor = new Color(0.45f, 0.12f, 0.08f, 1f);
    public Color foodPropColor = new Color(0.95f, 0.58f, 0.18f, 1f);

    [Header("Wine Bottle Model")]
    public GameObject wineBottleTemplate;
    public string wineBottleObjectName = "酒瓶";
    public bool hideWineBottleSourceWhileCarried = true;
    public float wineBottleTargetSize = 0.38f;
    public Vector3 wineBottleHeldEulerOffset = Vector3.zero;

    [Header("Police Entrance Fallback")]
    public bool animatePoliceEntranceWithoutTimeline = true;
    public Transform policeEntranceTarget;
    public float policeEntranceDistance = 12f;
    public float policeEntranceDuration = 4f;
    public bool rotatePoliceTowardPath = true;

    [Header("Police Intrusion Staging")]
    public string primaryPoliceObjectName = "警察";
    public Transform primaryPoliceActor;
    public Transform secondaryPoliceActor;
    public bool createSecondPoliceFromPrimary = true;
    public float policePairSpacing = 1.25f;
    public float policeEntranceStaggerSeconds = 0.25f;
    public bool guidePlayerToWitnessPoint = true;
    public float witnessRunDistance = 9f;
    public float witnessRunSeconds = 1.5f;
    public float witnessLookAtHeight = 1.35f;

    [Header("Police Incident Scene References")]
    public Transform groomActor;
    public Transform shovedVillagerActor;
    public Transform femaleVillagerActor;
    public Transform hutEntrancePoint;
    public Transform policeExitPoint;
    public Transform ceremonyCup;
    public Rigidbody ceremonyCupRigidbody;
    public Transform[] interveneVillagers;
    public Transform[] casualtyVillagers;

    [Header("Police Incident Fallback Animation")]
    public bool useFallbackIncidentAnimation = true;
    public float policeApproachWomanSeconds = 1.1f;
    public float dragToHutSeconds = 2.4f;
    public float policeExitSeconds = 4f;
    public float shoveDistance = 0.9f;
    public float fallbackFallAngle = 78f;
    public string policeWalkStateName = "Walk";
    public string policeIdleStateName = "Idle";
    public string villagerFallStateName = "Fall";

    [Header("Police Incident Audio")]
    public AudioSource policeEventAudio;
    public AudioClip cupCrashClip;
    public AudioClip struggleClip;
    public AudioClip gunshotClip;
    public AudioClip painfulCryClip;

    [Header("Result For Later Chapters")]
    public bool saveResultToPlayerPrefs = true;
    public string conflictChoicePrefsKey = "Chapter1_ConflictChoice";
    public string peopleInjuredPrefsKey = "Chapter1_PeopleInjured";
    public string moralePrefsKey = "Chapter1_Morale";

    private int deliveredWineCount;
    private int sharedFoodCount;
    private readonly HashSet<int> deliveredWineTargets = new HashSet<int>();
    private readonly HashSet<int> sharedFoodTargets = new HashSet<int>();
    private bool policeStartQueued;
    private int peopleInjured;
    private int morale;
    private bool danceFinished;
    private bool danceRoutineRunning;
    private bool policeSequenceStarted;
    private bool choiceResolved;
    private bool storyStarted;
    private bool waitingForChoice;
    private bool chapterCompleted;
    private bool freeExplorationUnlocked;
    private bool explorationTimerRunning;
    private bool explorationTimerFinished;
    private bool startPoliceWhenDanceEnds;
    private bool autoInteractionsCreated;
    private float explorationTimerRemaining;
    private ConflictChoice lastChoice;
    private Vector3 policeOriginalPosition;
    private Quaternion policeOriginalRotation;
    private bool hasPoliceOriginalTransform;
    private GameObject runtimeSecondPolice;
    private Quaternion wineBottleUprightOffset = Quaternion.identity;
    private readonly List<Chapter1CircleDancer> weddingCrowdDancers = new List<Chapter1CircleDancer>();

    private string fallbackSpeaker = "";
    private string fallbackLine = "";
    private float fallbackLineUntil;
    private string missionText = "";
    private string choiceQuestion = "";
    private string optionALabel = "";
    private string optionBLabel = "";
    private Coroutine clearMissionRoutine;
    private bool clearMissionWhenPlayerMoves;
    private string movementSensitiveMissionText = "";
    private Vector3 temporaryMissionStartPlayerPosition;
    private bool interactionAnimationRunning;
    private bool openingStoryPlaying;

    private GUIStyle hudBoxStyle;
    private GUIStyle hudTitleStyle;
    private GUIStyle hudBodyStyle;
    private GUIStyle hudButtonStyle;
    private GUIStyle timerBoxStyle;
    private GUIStyle timerTitleStyle;
    private GUIStyle timerNumberStyle;
    private GUIStyle centerMenuStyle;
    private GameObject worldPromptObject;
    private TextMesh worldPromptText;

    private readonly string[] villagerTalkLines =
    {
        "今晚一定要喝醉！",
        "願祖靈庇佑新人。",
        "火堆亮起來，祖靈會看見我們的歌聲。",
        "今天是新人的日子，先把煩惱放在山路外吧。"
    };

    private int talkLineIndex;

    private void Awake()
    {
        if (autoFindMissingReferences)
        {
            AutoFindReferences();
        }

        RepairInteractionHudRuntimeDefaults();
        EnsureNarrationAudioSource();

        if (choiceUI != null)
        {
            choiceUI.HideInstant();
            choiceUI.Bind(this);
        }

        if (policeGroup != null)
        {
            policeOriginalPosition = policeGroup.transform.position;
            policeOriginalRotation = policeGroup.transform.rotation;
            hasPoliceOriginalTransform = true;
            policeGroup.SetActive(false);
            Debug.Log("[Chapter1] Police group hidden at start: " + policeGroup.name);
        }
        else
        {
            Debug.LogWarning("[Chapter1] Police group is not assigned.");
        }
    }

    private void Start()
    {
        EnsureWeddingCrowdDancers();

        if (autoStartOnAwake || autoBeginStoryIfControllerExists)
        {
            BeginChapter();
        }
        else
        {
            // 如果不播放開場故事，就直接進入自由探索。
            openingStoryPlaying = false;
            SetMission("自由探索婚禮：與族人交談、幫新郎送酒，或靠近舞圈加入舞蹈。");
            UnlockFreeExploration();
            SetPlayerControl(true);
        }
    }

    private void Update()
    {
        if (debugStartPoliceWithP && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[Chapter1] Debug P key pressed.");
            StartPoliceSequenceInternal(true);
        }

        if (debugStartDanceWithJ && Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("[Chapter1] Debug J key pressed.");
            JoinDance(null, playerRoot);
        }

        if (waitingForChoice && allowChoiceHotkeys)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ChooseIntervene();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                ChooseWatch();
            }
        }

        UpdateCenterInteractionInput();
        UpdateExplorationTimer();
        UpdateTemporaryMissionMovementClear();
        UpdateWorldInteractionPrompt();
    }

    private void OnGUI()
    {
        if (!showFallbackHud)
        {
            return;
        }

        EnsureHudStyles();

        // 開場故事期間，只允許「故事字幕」出現。
        // 若已經有 Chapter1DialogueUI，就完全不畫舊版 OnGUI 字幕，避免兩層字幕重疊。
        if (openingStoryPlaying)
        {
            if (dialogueUI == null)
            {
                DrawFallbackDialogue();
            }

            return;
        }

        if (showExplorationTimer && explorationTimerRunning)
        {
            DrawExplorationTimer();
        }

        if (ShouldShowCenterInteractionMenu())
        {
            DrawCenterInteractionMenu();
        }

        if (!string.IsNullOrEmpty(missionText))
        {
            float missionWidth = Mathf.Min(Screen.width - 40f, 560f);
            GUI.Box(new Rect(20f, 20f, missionWidth, 74f), GUIContent.none, hudBoxStyle);
            GUI.Label(new Rect(40f, 30f, missionWidth - 40f, 24f), "目前目標", hudTitleStyle);
            GUI.Label(new Rect(40f, 54f, missionWidth - 40f, 34f), missionText, hudBodyStyle);
        }

        // 有正式的 DialogueUI 時，不再額外畫 fallback 對話框。
        if (dialogueUI == null)
        {
            DrawFallbackDialogue();
        }

        if (waitingForChoice && choiceUI == null)
        {
            float width = Mathf.Min(Screen.width - 40f, 680f);
            float height = 190f;
            Rect box = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(box, GUIContent.none, hudBoxStyle);
            GUI.Label(new Rect(box.x + 24f, box.y + 22f, box.width - 48f, 36f), choiceQuestion, hudTitleStyle);

            Rect optionA = new Rect(box.x + 24f, box.y + 76f, box.width - 48f, 42f);
            Rect optionB = new Rect(box.x + 24f, box.y + 126f, box.width - 48f, 42f);

            if (GUI.Button(optionA, "1  " + optionALabel, hudButtonStyle))
            {
                ChooseIntervene();
            }

            if (GUI.Button(optionB, "2  " + optionBLabel, hudButtonStyle))
            {
                ChooseWatch();
            }
        }
    }

    private void DrawFallbackDialogue()
    {
        if (Time.time >= fallbackLineUntil || (string.IsNullOrEmpty(fallbackSpeaker) && string.IsNullOrEmpty(fallbackLine)))
        {
            return;
        }

        float width = Mathf.Min(Screen.width - 40f, 840f);
        float height = 118f;
        Rect box = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 36f, width, height);
        GUI.Box(box, GUIContent.none, hudBoxStyle);
        GUI.Label(new Rect(box.x + 24f, box.y + 18f, box.width - 48f, 28f), fallbackSpeaker, hudTitleStyle);
        GUI.Label(new Rect(box.x + 24f, box.y + 50f, box.width - 48f, 52f), fallbackLine, hudBodyStyle);
    }

    public void BeginChapter()
    {
        if (storyStarted)
        {
            return;
        }

        storyStarted = true;
        openingStoryPlaying = true;
        deliveredWineCount = 0;
        sharedFoodCount = 0;
        deliveredWineTargets.Clear();
        sharedFoodTargets.Clear();
        policeStartQueued = false;
        peopleInjured = 0;
        morale = 0;
        danceFinished = false;
        policeSequenceStarted = false;
        choiceResolved = false;
        waitingForChoice = false;
        chapterCompleted = false;
        freeExplorationUnlocked = false;
        explorationTimerRunning = false;
        explorationTimerFinished = false;
        startPoliceWhenDanceEnds = false;
        explorationTimerRemaining = Mathf.Max(1f, explorationDurationSeconds);

        // 開場故事開始前先清掉所有遊戲 HUD / 提示，避免畫面同時塞滿任務與互動資訊。
        missionText = "";
        fallbackSpeaker = "";
        fallbackLine = "";
        fallbackLineUntil = 0f;
        SetWorldInteractionPromptVisible(false);

        if (choiceUI != null)
        {
            choiceUI.HideInstant();
        }

        if (dialogueUI != null)
        {
            dialogueUI.HideInstant();
        }

        SetPlayerControl(false);
        StartCoroutine(BeginChapterRoutine());
    }

    private IEnumerator BeginChapterRoutine()
    {
        openingStoryPlaying = true;
        SetPlayerControl(false);
        SetWorldInteractionPromptVisible(false);

        yield return Fade(1f, 0f, 1.2f);

        // 第一段：年代與場景介紹。字幕顯示時間會自動配合配音長度。
        yield return PlayOpeningNarrationLine(
            "字幕",
            "1930.10.7，霧社。火光照亮婚禮，鼓聲和歌聲在山間回盪。",
            introVoice1,
            4f);

        yield return new WaitForSeconds(Mathf.Max(0f, introLineGap));

        // 第二段：把玩家帶進婚禮現場。
        yield return PlayOpeningNarrationLine(
            "旁白",
            "你睜開眼，看見族人圍著火堆歌舞。今晚本該只是祝福新人的夜晚。",
            introVoice2,
            4.5f);

        yield return new WaitForSeconds(Mathf.Max(0f, introLineGap));

        // 故事講完，先把字幕確實收掉，再開啟遊戲 HUD。
        if (dialogueUI != null)
        {
            dialogueUI.HideInstant();
        }

        fallbackSpeaker = "";
        fallbackLine = "";
        fallbackLineUntil = 0f;

        if (weddingAmbience != null && !weddingAmbience.isPlaying)
        {
            weddingAmbience.Play();
        }

        openingStoryPlaying = false;

        // 到這裡才正式開始自由探索：先完成婚禮任務，再進入導火線事件。
        UnlockFreeExploration();
        SetPlayerControl(true);
        ShowLine("新郎", "朋友，今晚人多。幫我把酒送給幾位賓客，也把火堆旁的食物分給族人；忙完再到舞圈一起跳吧。", 5f);
        UpdateWeddingQuestMission();
    }

    private IEnumerator PlayOpeningNarrationLine(string speaker, string line, AudioClip clip, float fallbackSeconds)
    {
        float duration = clip != null ? Mathf.Max(0.1f, clip.length) : Mathf.Max(0.1f, fallbackSeconds);
        ShowLine(speaker, line, duration + 0.1f);

        if (clip == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        EnsureNarrationAudioSource();
        if (narrationAudio == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        narrationAudio.Stop();
        narrationAudio.clip = clip;
        narrationAudio.volume = narrationVolume;
        narrationAudio.loop = false;
        narrationAudio.Play();

        while (narrationAudio != null && narrationAudio.isPlaying)
        {
            yield return null;
        }
    }

    private void EnsureNarrationAudioSource()
    {
        if (narrationAudio == null)
        {
            narrationAudio = gameObject.AddComponent<AudioSource>();
        }

        if (narrationAudio != null)
        {
            narrationAudio.playOnAwake = false;
            narrationAudio.loop = false;
            narrationAudio.spatialBlend = 0f;
            narrationAudio.volume = narrationVolume;
        }
    }

    private void UnlockFreeExploration()
    {
        if (freeExplorationUnlocked)
        {
            return;
        }

        freeExplorationUnlocked = true;
        CreateMissingExplorationInteractions();
        StartExplorationTimer();
    }

    private void StartExplorationTimer()
    {
        if (!useExplorationTimer)
        {
            UpdateWeddingQuestMission();
            return;
        }

        explorationTimerRemaining = Mathf.Max(1f, explorationDurationSeconds);
        explorationTimerFinished = false;
        explorationTimerRunning = true;
        startPoliceWhenDanceEnds = false;
        UpdateWeddingQuestMission();
    }

    private void UpdateExplorationTimer()
    {
        if (!explorationTimerRunning || explorationTimerFinished || policeSequenceStarted)
        {
            return;
        }

        if (pauseCountdownDuringDance && danceRoutineRunning)
        {
            return;
        }

        explorationTimerRemaining -= Time.deltaTime;

        if (explorationTimerRemaining > 0f)
        {
            return;
        }

        explorationTimerRemaining = 0f;
        explorationTimerFinished = true;
        explorationTimerRunning = false;

        // 正式流程改成「完成婚禮任務」才觸發警察劇情，倒數不再強制開始事件。
        if (requireAllWeddingTasksBeforePolice)
        {
            UpdateWeddingQuestMission();
            TryStartPoliceAfterWeddingTasks();
            return;
        }

        if (danceRoutineRunning)
        {
            startPoliceWhenDanceEnds = true;
            return;
        }

        StartPoliceSequence();
    }

    private void DrawExplorationTimer()
    {
        float width = 220f;
        float height = 78f;
        float x = Screen.width - width - 20f;
        float y = 20f;
        Rect box = new Rect(x, y, width, height);
        GUI.Box(box, GUIContent.none, timerBoxStyle);
        GUI.Label(new Rect(x + 16f, y + 10f, width - 32f, 24f), explorationTimerTitle, timerTitleStyle);
        GUI.Label(new Rect(x + 16f, y + 34f, width - 32f, 34f), FormatTimer(explorationTimerRemaining), timerNumberStyle);
    }

    private string FormatTimer(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    private void UpdateCenterInteractionInput()
    {
        if (!ShouldAllowCenterInteractionInput())
        {
            return;
        }

        if (IsCenterActionPressed(GetDanceInteractionKey(), centerDanceAltKey, centerDanceGamepadKey) && CanUseDanceInteraction())
        {
            JoinDance(null, playerRoot);
        }
        else if (IsCenterActionPressed(centerWineKey, centerWineAltKey, centerWineGamepadKey))
        {
            DeliverWine("賓客", FindNextInteractionTarget(true));
        }
        else if (IsCenterActionPressed(centerFoodKey, centerFoodAltKey, centerFoodGamepadKey))
        {
            ShareFood("族人", FindNextInteractionTarget(false));
        }
    }

    private bool IsCenterActionPressed(KeyCode primaryKey, KeyCode altKey, KeyCode gamepadKey)
    {
        return IsKeyPressed(primaryKey) || IsKeyPressed(altKey) || IsKeyPressed(gamepadKey);
    }

    private KeyCode GetDanceInteractionKey()
    {
        // Older copies of the chapter scene serialized this field as None even
        // though the HUD says E. Keep the keyboard prompt and actual input aligned.
        return centerDanceKey == KeyCode.None ? KeyCode.E : centerDanceKey;
    }

    private bool IsKeyPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    private bool ShouldShowCenterInteractionMenu()
    {
        if (!showCenterInteractionMenu || !IsFreeExplorationActive())
        {
            return false;
        }

        return !requireFireDistanceForCenterMenu || IsPlayerNearFireCenter();
    }

    private bool ShouldAllowCenterInteractionInput()
    {
        if (!IsFreeExplorationActive())
        {
            return false;
        }

        return !requireFireDistanceForCenterMenu || IsPlayerNearFireCenter();
    }

    private bool IsPlayerNearFireCenter()
    {
        Vector3 center = GetFireCenterPosition();
        Vector3 player = GetCurrentPlayerPosition();
        center.y = player.y;
        return Vector3.Distance(center, player) <= Mathf.Max(12f, centerInteractionRange);
    }

    private Vector3 GetFireCenterPosition()
    {
        if (preferControllerPositionForFireCenter && transform != null)
        {
            if (danceCenter == null || GetFlatDistance(transform.position, danceCenter.position) > Mathf.Max(1f, maxDanceCenterOffsetForFireCenter))
            {
                return transform.position;
            }
        }

        if (danceCenter != null)
        {
            return danceCenter.position;
        }

        if (choiceFocusPoint != null)
        {
            return choiceFocusPoint.transform.position;
        }

        return transform.position;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 GetCurrentPlayerPosition()
    {
        if (playerRoot != null)
        {
            return playerRoot.position;
        }

        if (characterController != null)
        {
            return characterController.transform.position;
        }

        if (playerController != null)
        {
            return playerController.transform.position;
        }

        Transform viewTransform = GetPlayerViewTransform();
        if (viewTransform != null)
        {
            return viewTransform.position;
        }

        Transform player = GetDancePlayerRoot();
        return player != null ? player.position : transform.position;
    }

    private void DrawCenterInteractionMenu()
    {
        string text = GetCenterInteractionPromptText();
        float width = Mathf.Min(Screen.width - 40f, 380f);
        float height = 104f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 28f, width, height);
        GUI.Box(rect, text, centerMenuStyle);
    }

    private void UpdateWorldInteractionPrompt()
    {
        if (!showWorldInteractionPrompt || !ShouldShowCenterInteractionMenu())
        {
            SetWorldInteractionPromptVisible(false);
            return;
        }

        EnsureWorldInteractionPrompt();
        if (worldPromptObject == null || worldPromptText == null)
        {
            return;
        }

        Transform cameraTransform = GetPlayerViewTransform();
        if (cameraTransform != null && worldPromptObject.transform.parent != cameraTransform)
        {
            worldPromptObject.transform.SetParent(cameraTransform, false);
        }

        if (worldPromptObject.transform.parent != null)
        {
            worldPromptObject.transform.localPosition = new Vector3(0f, -0.42f, 1.75f);
            worldPromptObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            worldPromptObject.transform.position = GetCurrentPlayerPosition() + Vector3.up * 1.45f + Vector3.forward * 1.2f;
        }

        worldPromptText.text = GetCenterInteractionPromptText();
        SetWorldInteractionPromptVisible(true);
    }

    private void EnsureWorldInteractionPrompt()
    {
        if (worldPromptObject != null && worldPromptText != null)
        {
            return;
        }

        worldPromptObject = new GameObject("Chapter1_WorldInteractionPrompt");
        worldPromptText = worldPromptObject.AddComponent<TextMesh>();
        worldPromptText.anchor = TextAnchor.MiddleCenter;
        worldPromptText.alignment = TextAlignment.Center;
        worldPromptText.fontSize = 52;
        worldPromptText.characterSize = 0.035f;
        worldPromptText.color = Color.white;
        worldPromptText.text = GetCenterInteractionPromptText();

        Renderer promptRenderer = worldPromptObject.GetComponent<Renderer>();
        if (promptRenderer != null)
        {
            promptRenderer.sortingOrder = 1000;
        }
    }

    private void SetWorldInteractionPromptVisible(bool visible)
    {
        if (worldPromptObject != null && worldPromptObject.activeSelf != visible)
        {
            worldPromptObject.SetActive(visible);
        }
    }

    private string GetCenterInteractionPromptText()
    {
        string danceText;
        if (danceFinished)
        {
            danceText = "E / 1 / 手把A  舞蹈已完成";
        }
        else if (!AreWineAndFoodTasksComplete())
        {
            danceText = "E / 1 / 手把A  先完成送酒與分享食物";
        }
        else
        {
            danceText = "E / 1 / 手把A  加入舞蹈";
        }

        return danceText + "\nR / 2 / 手把B  送酒\nT / 3 / 手把X  分享食物";
    }

    public void Talk(string speaker, string line, float seconds = -1f)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            line = GetNextVillagerTalkLine();
        }

        if (string.IsNullOrWhiteSpace(speaker))
        {
            speaker = "族人";
        }

        ShowLine(speaker, line, seconds > 0f ? seconds : defaultDialogueSeconds);
    }

    public void DeliverWine(string npcName, Transform receiverTarget = null)
    {
        if (interactionAnimationRunning)
        {
            ShowLine("系統", "先等目前的互動動作結束。", 1.5f);
            return;
        }

        int target = Mathf.Max(1, wineTargetCount);
        if (deliveredWineCount >= target)
        {
            ShowLine("新郎", "酒已經送得差不多了，去看看還有沒有族人需要食物。", 2.5f);
            UpdateWeddingQuestMission();
            return;
        }

        if (receiverTarget == null)
        {
            receiverTarget = FindNextInteractionTarget(true);
        }

        if (receiverTarget != null)
        {
            int id = receiverTarget.GetInstanceID();
            if (deliveredWineTargets.Contains(id))
            {
                ShowLine("賓客", "我已經拿到酒了，先送給其他人吧。", 2.4f);
                return;
            }
            deliveredWineTargets.Add(id);
        }

        deliveredWineCount++;
        morale++;

        string speaker = string.IsNullOrWhiteSpace(npcName) ? "賓客" : npcName;
        PlayGiveItemAnimation(
            true,
            "你拿起酒杯，走向賓客。",
            speaker,
            "哈哈，這杯我收下了。願祖靈庇佑新人。",
            receiverTarget);

        int shownCount = Mathf.Min(deliveredWineCount, target);
        SetTemporaryMission("送酒任務：" + shownCount + " / " + target + " 位賓客已收到酒。", 3.5f);

        if (deliveredWineCount >= target)
        {
            StartCoroutine(ShowLineAfterDelay("新郎", "謝謝你。酒送好了，再幫忙把食物分給族人，等等一起去舞圈。", 2.6f, 4f));
        }

        TryStartPoliceAfterWeddingTasks();
    }

    public void ShareFood(string npcName, Transform receiverTarget = null)
    {
        if (interactionAnimationRunning)
        {
            ShowLine("系統", "先等目前的互動動作結束。", 1.5f);
            return;
        }

        int target = Mathf.Max(1, foodTargetCount);
        if (sharedFoodCount >= target)
        {
            ShowLine("族人", "食物已經分得差不多了，謝謝你。", 2.3f);
            UpdateWeddingQuestMission();
            return;
        }

        if (receiverTarget == null)
        {
            receiverTarget = FindNextInteractionTarget(false);
        }

        if (receiverTarget != null)
        {
            int id = receiverTarget.GetInstanceID();
            if (sharedFoodTargets.Contains(id))
            {
                ShowLine("族人", "我已經拿到食物了，分給其他人吧。", 2.4f);
                return;
            }
            sharedFoodTargets.Add(id);
        }

        sharedFoodCount++;
        morale++;

        string speaker = string.IsNullOrWhiteSpace(npcName) ? "族人" : npcName;
        PlayGiveItemAnimation(
            false,
            "你從火堆旁拿起食物，走向族人。",
            speaker,
            "謝謝你。今晚能這樣相聚，已經很難得。",
            receiverTarget);

        int shownCount = Mathf.Min(sharedFoodCount, target);
        SetTemporaryMission("分享食物：" + shownCount + " / " + target + " 位族人已收到食物。", 3.5f);

        if (sharedFoodCount >= target)
        {
            StartCoroutine(ShowLineAfterDelay("新郎", "辛苦了。事情都忙得差不多了，來舞圈一起跳吧。", 2.4f, 3.5f));
        }

        TryStartPoliceAfterWeddingTasks();
    }

    private void CreateMissingExplorationInteractions()
    {
        if (autoInteractionsCreated)
        {
            return;
        }

        autoInteractionsCreated = true;
        Vector3 anchor = GetAutoInteractionAnchor();
        Vector3 forward = GetAutoInteractionForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (right.sqrMagnitude < 0.01f)
        {
            right = Vector3.right;
        }

        int wineCount = Mathf.Max(1, wineTargetCount);
        if (!HasInteractionType(Chapter1Interactable.InteractionType.DeliverWine))
        {
            for (int i = 0; i < wineCount; i++)
            {
                float side = i - (wineCount - 1) * 0.5f;
                Vector3 position = anchor + forward * 5.5f + right * side * 2.2f;
                CreateAutoInteraction(
                    "Auto_WineGuest_" + (i + 1),
                    position,
                    Chapter1Interactable.InteractionType.DeliverWine,
                    "賓客",
                    "按 E 送酒",
                    new Color(0.55f, 0.12f, 0.1f, 1f));
            }
        }

        if (!HasInteractionType(Chapter1Interactable.InteractionType.ShareFood))
        {
            int foodCount = Mathf.Max(1, foodTargetCount);
            for (int i = 0; i < foodCount; i++)
            {
                float side = i - (foodCount - 1) * 0.5f;
                Vector3 position = anchor - forward * (4f + i * 0.35f) + right * side * 2.6f;
                CreateAutoInteraction(
                    "Auto_FoodShare_" + (i + 1),
                    position,
                    Chapter1Interactable.InteractionType.ShareFood,
                    "族人",
                    "按 E 分享食物",
                    new Color(0.85f, 0.45f, 0.12f, 1f));
            }
        }
    }

    private bool HasInteractionType(Chapter1Interactable.InteractionType interactionType)
    {
        Chapter1Interactable[] interactables = FindObjectsOfType<Chapter1Interactable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] != null && interactables[i].interactionType == interactionType)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateAutoInteraction(
        string objectName,
        Vector3 position,
        Chapter1Interactable.InteractionType interactionType,
        string speaker,
        string prompt,
        Color markerColor)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = objectName;
        marker.transform.SetParent(transform);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * Mathf.Max(0.2f, autoInteractionMarkerSize);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            markerCollider.isTrigger = true;
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.material.color = markerColor;
        }

        Chapter1Interactable interactable = marker.AddComponent<Chapter1Interactable>();
        interactable.controller = this;
        interactable.playerRoot = playerRoot;
        interactable.interactionType = interactionType;
        interactable.interactKey = GetAutoInteractionKey(interactionType);
        interactable.alsoUseEKey = interactionType != Chapter1Interactable.InteractionType.DeliverWine
            && interactionType != Chapter1Interactable.InteractionType.ShareFood;
        interactable.disableAfterUse = true;
        interactable.hidePromptAfterUse = true;
        interactable.showPrompt = true;
        interactable.promptText = GetAutoInteractionPrompt(interactionType, prompt);
        interactable.useDistanceCheck = true;
        interactable.interactRange = Mathf.Max(1.5f, autoInteractionDistance);
        interactable.speakerName = speaker;
        interactable.rotateDialogueLines = false;
    }

    private KeyCode GetAutoInteractionKey(Chapter1Interactable.InteractionType interactionType)
    {
        if (interactionType == Chapter1Interactable.InteractionType.DeliverWine)
        {
            return centerWineKey == KeyCode.None || centerWineKey == KeyCode.E ? KeyCode.R : centerWineKey;
        }

        if (interactionType == Chapter1Interactable.InteractionType.ShareFood)
        {
            return centerFoodKey == KeyCode.None || centerFoodKey == KeyCode.E ? KeyCode.T : centerFoodKey;
        }

        return GetDanceInteractionKey();
    }

    private string GetAutoInteractionPrompt(Chapter1Interactable.InteractionType interactionType, string fallbackPrompt)
    {
        if (interactionType == Chapter1Interactable.InteractionType.DeliverWine)
        {
            return "按 R / 2 送酒";
        }

        if (interactionType == Chapter1Interactable.InteractionType.ShareFood)
        {
            return "按 T / 3 分享食物";
        }

        return fallbackPrompt;
    }

    private void PlayGiveItemAnimation(bool isWine, string actionLine, string receiverName, string receiverLine, Transform receiverTarget = null)
    {
        if (!playInteractionAnimations)
        {
            ShowLine(receiverName, receiverLine, 3f);
            return;
        }

        StartCoroutine(GiveItemAnimationRoutine(isWine, actionLine, receiverName, receiverLine, receiverTarget));
    }

    private IEnumerator GiveItemAnimationRoutine(bool isWine, string actionLine, string receiverName, string receiverLine, Transform receiverTarget)
    {
        interactionAnimationRunning = true;
        bool shouldGuidePlayer = guidePlayerDuringItemInteractions && GetDancePlayerRoot() != null;
        bool shouldLockControl = lockPlayerDuringInteractionAnimation || shouldGuidePlayer;

        if (shouldLockControl)
        {
            SetPlayerControl(false);
        }

        GameObject prop = CreateHeldProp(isWine);
        Vector3 pickupPosition = GetItemPickupPosition(isWine);
        Vector3 receiverPosition = GetItemReceiverPosition(isWine, receiverTarget);
        prop.transform.position = pickupPosition;

        Renderer[] sourceRenderers = null;
        bool[] sourceRendererStates = null;
        if (isWine
            && hideWineBottleSourceWhileCarried
            && wineBottleTemplate != null
            && wineBottleTemplate.scene.IsValid())
        {
            sourceRenderers = wineBottleTemplate.GetComponentsInChildren<Renderer>(true);
            sourceRendererStates = new bool[sourceRenderers.Length];
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (sourceRenderers[i] == null)
                {
                    continue;
                }

                sourceRendererStates[i] = sourceRenderers[i].enabled;
                sourceRenderers[i].enabled = false;
            }
        }

        if (shouldGuidePlayer)
        {
            string pickupLine = isWine ? "系統帶你到火堆旁，拿起一瓶酒。" : "系統帶你到火堆旁，拿起食物。";
            string walkLine = isWine ? "你拿著酒走到賓客旁邊。" : "你拿著食物走到族人旁邊。";
            ShowLine("動作", pickupLine, guidedWalkToPickupSeconds + 0.6f);

            Vector3 currentPlayerPosition = GetCurrentPlayerPosition();
            Vector3 pickupCameraPosition = GetApproachCameraPosition(pickupPosition, currentPlayerPosition, guidedPickupDistance);
            yield return MovePlayerCameraToPosition(pickupCameraPosition, pickupPosition, guidedWalkToPickupSeconds, null);

            Vector3 handPosition = GetCarriedPropPosition();
            yield return MovePropToPosition(prop, pickupPosition, handPosition, 0.35f, 0.15f);
            yield return HoldPropInView(prop, firstPersonHoldSeconds);

            ShowLine("動作", walkLine, guidedWalkToReceiverSeconds + 0.6f);
            Vector3 receiverCameraPosition = GetApproachCameraPosition(receiverPosition, pickupPosition, guidedReceiverDistance);
            yield return MovePlayerCameraToPosition(receiverCameraPosition, receiverPosition, guidedWalkToReceiverSeconds, prop);
            yield return HoldPropInView(prop, 0.25f);

            yield return MovePropToPosition(prop, GetCarriedPropPosition(), receiverPosition, guidedGiveSeconds, 0.12f);
        }
        else
        {
            ShowLine("動作", actionLine, interactionAnimationSeconds);
            yield return MovePropToPosition(prop, pickupPosition, receiverPosition, interactionAnimationSeconds, interactionArcHeight);
        }

        prop.transform.position = receiverPosition;
        Destroy(prop, 0.15f);

        if (sourceRenderers != null && sourceRendererStates != null)
        {
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (sourceRenderers[i] != null)
                {
                    sourceRenderers[i].enabled = sourceRendererStates[i];
                }
            }
        }

        ShowLine(receiverName, receiverLine, 3f);

        if (shouldLockControl)
        {
            SetPlayerControl(true);
        }

        interactionAnimationRunning = false;
    }

    private IEnumerator MovePropToPosition(GameObject prop, Vector3 startPosition, Vector3 endPosition, float seconds, float arcHeight)
    {
        float duration = Mathf.Max(0.05f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position.y += Mathf.Sin(eased * Mathf.PI) * arcHeight;
            prop.transform.position = position;
            prop.transform.Rotate(Vector3.up, 140f * Time.deltaTime, Space.World);
            yield return null;
        }

        prop.transform.position = endPosition;
    }

    private IEnumerator HoldPropInView(GameObject prop, float seconds)
    {
        if (prop == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.05f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            PlaceCarriedProp(prop);
            yield return null;
        }
    }

    private IEnumerator MovePlayerCameraToPosition(Vector3 targetCameraPosition, Vector3 lookAtPosition, float seconds, GameObject carriedProp)
    {
        Transform root = GetDancePlayerRoot();
        if (root == null)
        {
            yield break;
        }

        Vector3 startRootPosition = GetSafeGuidedRootPosition(root, root.position, root.position);
        root.position = startRootPosition;
        Vector3 startCameraPosition = GetCurrentPlayerPosition();
        targetCameraPosition.y = startCameraPosition.y;

        Vector3 targetRootPosition = startRootPosition + (targetCameraPosition - startCameraPosition);
        targetRootPosition = GetSafeGuidedRootPosition(root, targetRootPosition, startRootPosition);
        Quaternion startRotation = root.rotation;
        Quaternion targetRotation = startRotation;
        Vector3 faceDirection = lookAtPosition - targetCameraPosition;
        faceDirection.y = 0f;

        if (faceDirection.sqrMagnitude > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
        }

        float duration = Mathf.Max(0.05f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 nextRootPosition = Vector3.Lerp(startRootPosition, targetRootPosition, eased);
            root.position = GetSafeGuidedRootPosition(root, nextRootPosition, root.position);
            root.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);

            if (carriedProp != null)
            {
                PlaceCarriedProp(carriedProp);
            }

            yield return null;
        }

        root.position = GetSafeGuidedRootPosition(root, targetRootPosition, root.position);
        root.rotation = targetRotation;

        if (carriedProp != null)
        {
            PlaceCarriedProp(carriedProp);
        }
    }

    private Vector3 GetSafeGuidedRootPosition(Transform root, Vector3 candidatePosition, Vector3 fallbackPosition)
    {
        if (!keepGuidedPlayerAboveGround)
        {
            return candidatePosition;
        }

        float clearance = GetGuidedRootGroundClearance(root);
        if (TryGetTerrainGroundY(candidatePosition, out float terrainGroundY))
        {
            candidatePosition.y = terrainGroundY + clearance;
            return candidatePosition;
        }

        if (TryGetPhysicsGroundY(root, candidatePosition, out float physicsGroundY))
        {
            candidatePosition.y = physicsGroundY + clearance;
            return candidatePosition;
        }

        candidatePosition.y = fallbackPosition.y;
        return candidatePosition;
    }

    private float GetGuidedRootGroundClearance(Transform root)
    {
        float clearance = Mathf.Max(0.02f, guidedGroundClearance);
        if (characterController == null || root == null)
        {
            return clearance;
        }

        Transform controllerTransform = characterController.transform;
        bool controllerBelongsToRoot = controllerTransform == root
            || controllerTransform.IsChildOf(root)
            || root.IsChildOf(controllerTransform);

        if (!controllerBelongsToRoot)
        {
            return clearance;
        }

        float controllerBottomOffset = characterController.height * 0.5f - characterController.center.y + characterController.skinWidth;
        return Mathf.Max(clearance, controllerBottomOffset);
    }

    private bool TryGetTerrainGroundY(Vector3 position, out float groundY)
    {
        groundY = 0f;
        bool foundGround = false;
        Terrain[] terrains = Terrain.activeTerrains;

        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = position.x >= terrainPosition.x && position.x <= terrainPosition.x + terrainSize.x;
            bool insideZ = position.z >= terrainPosition.z && position.z <= terrainPosition.z + terrainSize.z;
            if (!insideX || !insideZ)
            {
                continue;
            }

            float sampledY = terrainPosition.y + terrain.SampleHeight(position);
            if (!foundGround || sampledY > groundY)
            {
                groundY = sampledY;
                foundGround = true;
            }
        }

        return foundGround;
    }

    private bool TryGetPhysicsGroundY(Transform root, Vector3 position, out float groundY)
    {
        groundY = 0f;
        Vector3 origin = position + Vector3.up * Mathf.Max(0.5f, guidedGroundRaycastHeight);
        float distance = Mathf.Max(1f, guidedGroundRaycastHeight + guidedGroundRaycastDistance);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, guidedGroundLayers, QueryTriggerInteraction.Ignore);
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == null || hit.normal.y < 0.25f)
            {
                continue;
            }

            if (root != null && (hit.transform == root || hit.transform.IsChildOf(root)))
            {
                continue;
            }

            if (!foundGround || hit.point.y > groundY)
            {
                groundY = hit.point.y;
                foundGround = true;
            }
        }

        return foundGround;
    }

    private Vector3 GetApproachCameraPosition(Vector3 focusPosition, Vector3 fromPosition, float distance)
    {
        Vector3 approachDirection = fromPosition - focusPosition;
        approachDirection.y = 0f;

        if (approachDirection.sqrMagnitude < 0.01f && Camera.main != null)
        {
            approachDirection = -Camera.main.transform.forward;
            approachDirection.y = 0f;
        }

        if (approachDirection.sqrMagnitude < 0.01f)
        {
            approachDirection = Vector3.back;
        }

        Vector3 position = focusPosition + approachDirection.normalized * Mathf.Max(0.3f, distance);
        position.y = GetCurrentPlayerPosition().y;
        return position;
    }

    private Vector3 GetCarriedPropPosition()
    {
        Transform viewTransform = GetPlayerViewTransform();
        if (viewTransform != null)
        {
            return viewTransform.position
                + viewTransform.forward * Mathf.Max(0.35f, carriedPropViewDistance)
                + viewTransform.right * carriedPropViewRightOffset
                - viewTransform.up * carriedPropViewDownOffset;
        }

        Transform root = GetDancePlayerRoot();
        if (root != null)
        {
            return root.position + root.forward * 0.65f + Vector3.up * 1.15f + root.right * 0.22f;
        }

        return GetCurrentPlayerPosition() + Vector3.up * 1f;
    }

    private void PlaceCarriedProp(GameObject prop)
    {
        if (prop == null)
        {
            return;
        }

        prop.transform.position = GetCarriedPropPosition();

        Transform viewTransform = GetPlayerViewTransform();
        if (viewTransform != null)
        {
            if (prop.name == "Chapter1_WineBottle_Animation")
            {
                Vector3 horizontalView = Vector3.ProjectOnPlane(viewTransform.forward, Vector3.up);
                if (horizontalView.sqrMagnitude < 0.001f)
                {
                    horizontalView = Vector3.ProjectOnPlane(viewTransform.right, Vector3.up);
                }

                if (horizontalView.sqrMagnitude < 0.001f)
                {
                    horizontalView = Vector3.forward;
                }

                Quaternion viewYaw = Quaternion.LookRotation(horizontalView.normalized, Vector3.up);
                prop.transform.rotation = viewYaw
                    * wineBottleUprightOffset
                    * Quaternion.Euler(wineBottleHeldEulerOffset);
            }
            else
            {
                prop.transform.rotation = Quaternion.LookRotation(viewTransform.forward, Vector3.up);
            }
        }
    }

    private GameObject CreateHeldProp(bool isWine)
    {
        if (isWine)
        {
            GameObject sourceBottle = FindWineBottleTemplate();
            if (sourceBottle != null)
            {
                CaptureWineBottleUprightOffset(sourceBottle.transform);
                GameObject bottle = Instantiate(sourceBottle);
                bottle.name = "Chapter1_WineBottle_Animation";
                bottle.transform.SetParent(null, true);
                bottle.transform.localScale = sourceBottle.transform.lossyScale;
                bottle.SetActive(true);
                PrepareHeldProp(bottle);
                FitHeldWineBottle(bottle);
                return bottle;
            }

            Debug.LogWarning("[Chapter1] No scene object containing '" + wineBottleObjectName + "' was found. Using the cylinder fallback.");
        }

        PrimitiveType primitive = isWine ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
        GameObject prop = GameObject.CreatePrimitive(primitive);
        prop.name = isWine ? "Chapter1_WineBottle_Animation" : "Chapter1_Food_Animation";

        Collider propCollider = prop.GetComponent<Collider>();
        if (propCollider != null)
        {
            propCollider.enabled = false;
        }

        float scale = Mathf.Max(0.08f, heldPropScale);
        prop.transform.localScale = isWine
            ? new Vector3(scale * 0.35f, scale * 1.45f, scale * 0.35f)
            : Vector3.one * scale;

        Renderer propRenderer = prop.GetComponent<Renderer>();
        if (propRenderer != null)
        {
            propRenderer.material.color = isWine ? winePropColor : foodPropColor;
        }

        return prop;
    }

    private void CaptureWineBottleUprightOffset(Transform sourceBottle)
    {
        Vector3 horizontalReference = Vector3.ProjectOnPlane(sourceBottle.forward, Vector3.up);
        if (horizontalReference.sqrMagnitude < 0.001f)
        {
            horizontalReference = Vector3.ProjectOnPlane(sourceBottle.right, Vector3.up);
        }

        if (horizontalReference.sqrMagnitude < 0.001f)
        {
            horizontalReference = Vector3.forward;
        }

        Quaternion sourceYaw = Quaternion.LookRotation(horizontalReference.normalized, Vector3.up);
        wineBottleUprightOffset = Quaternion.Inverse(sourceYaw) * sourceBottle.rotation;
    }

    private GameObject FindWineBottleTemplate()
    {
        if (wineBottleTemplate != null && wineBottleTemplate.name != "Chapter1_WineBottle_Animation")
        {
            return wineBottleTemplate;
        }

        string requestedName = string.IsNullOrWhiteSpace(wineBottleObjectName) ? "酒瓶" : wineBottleObjectName.Trim();
        Transform exactMatch = FindTransformByName(requestedName);
        if (exactMatch != null && exactMatch.name != "Chapter1_WineBottle_Animation")
        {
            wineBottleTemplate = exactMatch.gameObject;
            Debug.Log("[Chapter1] Using scene wine bottle: " + wineBottleTemplate.name);
            return wineBottleTemplate;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        Transform bestMatch = null;
        float bestDistance = float.MaxValue;
        Vector3 pickupCenter = GetFireCenterPosition();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null
                || !candidate.gameObject.scene.IsValid()
                || candidate.name == "Chapter1_WineBottle_Animation"
                || candidate.name.IndexOf(requestedName, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            float distance = (candidate.position - pickupCenter).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;
            }
        }

        if (bestMatch != null)
        {
            wineBottleTemplate = bestMatch.gameObject;
            Debug.Log("[Chapter1] Using scene wine bottle: " + wineBottleTemplate.name);
        }

        return wineBottleTemplate;
    }

    private void PrepareHeldProp(GameObject prop)
    {
        Collider[] colliders = prop.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = prop.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].useGravity = false;
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }

        Chapter1Interactable[] interactables = prop.GetComponentsInChildren<Chapter1Interactable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            interactables[i].enabled = false;
        }
    }

    private void FitHeldWineBottle(GameObject bottle)
    {
        Renderer[] renderers = bottle.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(bottle.transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        float largestDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        if (largestDimension <= 0.0001f)
        {
            return;
        }

        float scaleMultiplier = Mathf.Max(0.08f, wineBottleTargetSize) / largestDimension;
        bottle.transform.localScale *= scaleMultiplier;
    }

    private Vector3 GetItemPickupPosition(bool isWine)
    {
        Vector3 center = GetFireCenterPosition();
        Vector3 player = GetCurrentPlayerPosition();
        Vector3 toPlayer = player - center;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f)
        {
            toPlayer = Vector3.forward;
        }

        Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
        float sideOffset = isWine ? -0.7f : 0.7f;
        return center + toPlayer.normalized * 0.8f + side * sideOffset + Vector3.up * 0.85f;
    }

    private Vector3 GetItemReceiverPosition(bool isWine, Transform receiverTarget)
    {
        if (receiverTarget != null)
        {
            return receiverTarget.position + Vector3.up * 1.05f;
        }

        Transform target = FindNextInteractionTarget(isWine);
        if (target != null)
        {
            return target.position + Vector3.up * 1.05f;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position + Camera.main.transform.forward * 1.2f + Vector3.down * 0.25f;
        }

        return GetCurrentPlayerPosition() + Vector3.up * 1f;
    }

    private Transform FindNextInteractionTarget(bool isWine)
    {
        string prefix = isWine ? "Auto_WineGuest_" : "Auto_FoodShare_";
        int count = isWine ? Mathf.Max(1, wineTargetCount) : Mathf.Max(1, foodTargetCount);
        int progress = isWine ? deliveredWineCount : sharedFoodCount;

        // 依目前進度找「下一位」尚未互動的目標。
        // 例如進度 0 -> 1、進度 1 -> 2、進度 2 -> 3。
        int index = (progress % count) + 1;

        GameObject target = GameObject.Find(prefix + index);
        if (target != null)
        {
            return target.transform;
        }

        target = GameObject.Find(prefix + "1");
        if (target != null)
        {
            return target.transform;
        }

        Transform existingTarget = FindExistingInteractionTarget(isWine, true);
        if (existingTarget != null)
        {
            return existingTarget;
        }

        return FindExistingInteractionTarget(isWine, false);
    }

    private Transform FindExistingInteractionTarget(bool isWine, bool requireActive)
    {
        Chapter1Interactable.InteractionType targetType = isWine
            ? Chapter1Interactable.InteractionType.DeliverWine
            : Chapter1Interactable.InteractionType.ShareFood;
        Chapter1Interactable[] interactables = FindObjectsOfType<Chapter1Interactable>(true);
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;
        Vector3 center = GetFireCenterPosition();

        for (int i = 0; i < interactables.Length; i++)
        {
            Chapter1Interactable interactable = interactables[i];
            if (interactable == null || interactable.interactionType != targetType)
            {
                continue;
            }

            if (requireActive && (!interactable.isActiveAndEnabled || !interactable.gameObject.activeInHierarchy))
            {
                continue;
            }

            float distance = (interactable.transform.position - center).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = interactable.transform;
            }
        }

        return bestTarget;
    }

    private Vector3 GetAutoInteractionAnchor()
    {
        return GetFireCenterPosition();
    }

    private Vector3 GetAutoInteractionForward()
    {
        if (danceCenter != null)
        {
            Vector3 toPlayer = playerRoot != null ? playerRoot.position - danceCenter.position : danceCenter.forward;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                return toPlayer.normalized;
            }

            return danceCenter.forward;
        }

        if (playerRoot != null)
        {
            Vector3 forward = playerRoot.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
        }

        return Vector3.forward;
    }

    public void JoinDance()
    {
        JoinDance(null, null);
    }

    public void JoinDance(Transform centerOverride)
    {
        JoinDance(centerOverride, null);
    }

    public void JoinDance(Transform centerOverride, Transform playerOverride)
    {
        if (danceFinished || danceRoutineRunning)
        {
            return;
        }

        if (requireWineAndFoodBeforeDance && !AreWineAndFoodTasksComplete())
        {
            ShowLine("新郎", GetDanceLockedLine(), 3.5f);
            UpdateWeddingQuestMission();
            return;
        }

        if (requireWineBeforeDance && deliveredWineCount < Mathf.Max(1, wineTargetCount))
        {
            ShowLine("新郎", "朋友，先幫我把酒送給大家，再一起來跳舞。", 3f);
            return;
        }

        danceFinished = true;
        StartCoroutine(PlayDanceRoutine(centerOverride, playerOverride));
    }

    private IEnumerator PlayDanceRoutine(Transform centerOverride, Transform playerOverride)
    {
        danceRoutineRunning = true;
        SetPlayerControl(false);
        SetMission("加入舞蹈：跟著鼓聲踏步，感受婚禮短暫的安寧。");
        ShowLine("族人", "來，跟著鼓聲一起踏步。今晚讓祖靈聽見我們的歌。", 3f);
        yield return new WaitForSeconds(1f);

        PlayDirector(danceTimeline);

        Transform root = playerOverride != null ? playerOverride : GetDancePlayerRoot();
        Transform center = centerOverride != null ? centerOverride : GetDanceCenter();
        if (center != null && GetFlatDistance(center.position, GetFireCenterPosition()) > Mathf.Max(1f, maxDanceCenterOffsetForFireCenter))
        {
            center = GetDanceCenter();
        }

        if (root != null && center != null)
        {
            yield return DanceAroundCenter(root, center);
        }
        else
        {
            yield return WaitForDirector(danceTimeline, danceDuration);
        }

        danceRoutineRunning = false;
        SetPlayerControl(true);
        UpdateWeddingQuestMission();

        if (requireAllWeddingTasksBeforePolice)
        {
            TryStartPoliceAfterWeddingTasks();
            if (!policeStartQueued && !policeSequenceStarted)
            {
                SetTemporaryMission("舞蹈完成。還有婚禮任務尚未完成。", 4f, true);
            }
            yield break;
        }

        if (startPoliceWhenDanceEnds)
        {
            startPoliceWhenDanceEnds = false;
            StartPoliceSequence();
        }
        else if (startPoliceAfterDance && !useExplorationTimer)
        {
            SetMission("舞蹈結束。婚禮仍在繼續，但山路上傳來皮靴聲。");
            StartPoliceSequence();
        }
        else
        {
            SetTemporaryMission("舞蹈結束。你可以繼續自由探索。", 4f, true);
        }
    }

    private IEnumerator DanceAroundCenter(Transform root, Transform center)
    {
        float duration = Mathf.Max(1f, danceDuration);
        float sharpness = Mathf.Max(1f, danceFollowSharpness);
        float elapsed = 0f;
        float baseHeight = root.position.y;
        Vector3 flatOffset = root.position - center.position;
        flatOffset.y = 0f;

        if (flatOffset.sqrMagnitude < 0.25f)
        {
            flatOffset = -center.forward;
            flatOffset.y = 0f;

            if (flatOffset.sqrMagnitude < 0.01f)
            {
                flatOffset = Vector3.back;
            }
        }

        float radius = danceRadius > 0.1f ? danceRadius : flatOffset.magnitude;
        float startAngle = Mathf.Atan2(flatOffset.x, flatOffset.z);
        float totalRadians = danceOrbitDegrees * Mathf.Deg2Rad;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float angle = startAngle + (totalRadians * easedProgress);
            float stepBob = Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 2f * danceStepFrequency)) * danceStepHeight;

            Vector3 centerPosition = center.position;
            Vector3 targetPosition = centerPosition + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            targetPosition.y = baseHeight + stepBob;
            root.position = Vector3.Lerp(root.position, targetPosition, Time.deltaTime * sharpness);

            Vector3 lookDirection = (centerPosition + Vector3.up * danceLookAtHeight) - root.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                root.rotation = Quaternion.Slerp(root.rotation, targetRotation, Time.deltaTime * sharpness);
            }

            yield return null;
        }
    }

    public void StartPoliceSequence()
    {
        StartPoliceSequenceInternal(false);
    }

    private void StartPoliceSequenceInternal(bool force)
    {
        if (policeSequenceStarted)
        {
            Debug.Log("[Chapter1] Police sequence already started.");
            return;
        }

        if (!force && requireAllWeddingTasksBeforePolice && !AreWeddingTasksComplete())
        {
            Debug.Log("[Chapter1] Police sequence blocked until wedding tasks are complete.");
            ShowLine("系統", "先完成送酒、分享食物與舞蹈，導火線事件才會發生。", 3f);
            UpdateWeddingQuestMission();
            return;
        }

        policeStartQueued = false;
        SetWeddingCrowdDancing(false);
        policeSequenceStarted = true;
        freeExplorationUnlocked = false;
        explorationTimerRunning = false;
        explorationTimerFinished = true;
        Debug.Log("[Chapter1] StartPoliceSequence called.");
        StartCoroutine(PoliceSequenceRoutine());
    }

    private bool AreWineAndFoodTasksComplete()
    {
        return deliveredWineCount >= Mathf.Max(1, wineTargetCount)
            && sharedFoodCount >= Mathf.Max(1, foodTargetCount);
    }

    private bool AreWeddingTasksComplete()
    {
        return AreWineAndFoodTasksComplete() && danceFinished && !danceRoutineRunning;
    }

    private string GetDanceLockedLine()
    {
        bool wineDone = deliveredWineCount >= Mathf.Max(1, wineTargetCount);
        bool foodDone = sharedFoodCount >= Mathf.Max(1, foodTargetCount);
        if (!wineDone && !foodDone)
        {
            return "朋友，先幫我把酒送給賓客，也把食物分給族人，再一起來跳舞。";
        }
        if (!wineDone)
        {
            return "還有幾位賓客沒有酒，送完再一起跳舞。";
        }
        return "先把食物分給族人，忙完再一起跳舞。";
    }

    private string GetWeddingQuestMissionText()
    {
        int wineTarget = Mathf.Max(1, wineTargetCount);
        int foodTarget = Mathf.Max(1, foodTargetCount);
        string danceState = danceFinished ? "完成" : (AreWineAndFoodTasksComplete() ? "可加入" : "未開放");
        return "婚禮任務：送酒 " + Mathf.Min(deliveredWineCount, wineTarget) + " / " + wineTarget
            + "｜分享食物 " + Mathf.Min(sharedFoodCount, foodTarget) + " / " + foodTarget
            + "｜舞蹈 " + danceState;
    }

    private void UpdateWeddingQuestMission()
    {
        if (!showWeddingQuestProgress || policeSequenceStarted || chapterCompleted)
        {
            return;
        }
        SetMission(GetWeddingQuestMissionText());
    }

    private void TryStartPoliceAfterWeddingTasks()
    {
        if (!autoStartPoliceAfterWeddingTasks || policeSequenceStarted || policeStartQueued || !AreWeddingTasksComplete())
        {
            return;
        }

        policeStartQueued = true;
        StartCoroutine(StartPoliceAfterWeddingTasksDelay());
    }

    private IEnumerator StartPoliceAfterWeddingTasksDelay()
    {
        SetMission("婚禮任務完成。鼓聲正熱烈時，山路上忽然傳來急促的皮靴聲……");
        yield return new WaitForSeconds(Mathf.Max(0f, policeStartDelayAfterWeddingTasks));
        if (!policeSequenceStarted)
        {
            StartPoliceSequenceInternal(false);
        }
    }

    private void EnsureWeddingCrowdDancers()
    {
        if (!autoStartWeddingCrowdDance)
        {
            return;
        }

        // DanceTrigger marks the wedding circle. The interaction system may use a
        // fallback center, so the crowd intentionally prefers this scene marker.
        Transform center = danceCenter != null ? danceCenter : GetDanceCenter();
        if (center == null)
        {
            Debug.LogWarning("[Chapter1] Wedding crowd dance skipped because no dance center was found.");
            return;
        }

        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        List<RuntimeAnimatorController> crowdControllers = new List<RuntimeAnimatorController>();

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, center))
            {
                continue;
            }

            RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
            if (runtimeController != null
                && runtimeController.name.StartsWith("VillagerAnimator")
                && !crowdControllers.Contains(runtimeController))
            {
                crowdControllers.Add(runtimeController);
            }
        }

        int fallbackControllerIndex = 0;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, center))
            {
                continue;
            }

            if (animator.runtimeAnimatorController == null
                && animator.avatar != null
                && animator.avatar.isHuman
                && crowdControllers.Count > 0)
            {
                animator.runtimeAnimatorController = crowdControllers[fallbackControllerIndex % crowdControllers.Count];
                fallbackControllerIndex++;
            }

            animator.applyRootMotion = false;
            Chapter1CircleDancer dancer = animator.GetComponent<Chapter1CircleDancer>();
            bool created = dancer == null;
            if (created)
            {
                dancer = animator.gameObject.AddComponent<Chapter1CircleDancer>();
            }

            dancer.center = center;
            dancer.playOnAwake = true;
            dancer.orbitSpeedDegrees = weddingCrowdOrbitSpeedDegrees;
            dancer.stepHeight = weddingCrowdStepHeight;
            dancer.stepFrequency = weddingCrowdStepFrequency;
            dancer.radialStepDistance = weddingCrowdRadialStepDistance;
            dancer.swayDegrees = weddingCrowdSwayDegrees;
            dancer.faceCenter = true;
            dancer.SetDancing(true);

            if (!weddingCrowdDancers.Contains(dancer))
            {
                weddingCrowdDancers.Add(dancer);
            }
        }

        Debug.Log("[Chapter1] Wedding crowd dancers started: " + weddingCrowdDancers.Count);
    }

    private bool IsWeddingCrowdActor(Animator animator, Transform center)
    {
        if (animator == null
            || !animator.gameObject.scene.IsValid()
            || !animator.gameObject.activeInHierarchy
            || animator.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
        {
            return false;
        }

        Transform actor = animator.transform;
        if (playerRoot != null && (actor.IsChildOf(playerRoot) || playerRoot.IsChildOf(actor)))
        {
            return false;
        }

        if (policeGroup != null && (actor.IsChildOf(policeGroup.transform) || policeGroup.transform.IsChildOf(actor)))
        {
            return false;
        }

        if (primaryPoliceActor != null && (actor.IsChildOf(primaryPoliceActor) || primaryPoliceActor.IsChildOf(actor)))
        {
            return false;
        }

        string actorName = actor.name.ToLowerInvariant();
        if (actorName.Contains("police")
            || actorName.Contains("警察")
            || actorName.Contains("xr origin")
            || actorName.Contains("controller"))
        {
            return false;
        }

        return GetFlatDistance(actor.position, center.position) <= Mathf.Max(1f, weddingCrowdDanceRange);
    }

    private void SetWeddingCrowdDancing(bool enabled)
    {
        for (int i = weddingCrowdDancers.Count - 1; i >= 0; i--)
        {
            Chapter1CircleDancer dancer = weddingCrowdDancers[i];
            if (dancer == null)
            {
                weddingCrowdDancers.RemoveAt(i);
                continue;
            }

            dancer.SetDancing(enabled);
        }
    }

    private IEnumerator PoliceSequenceRoutine()
    {
        SetPlayerControl(false);
        EnsurePoliceActorsForIntrusion();
        SetMission("婚禮中斷：兩名日本警察闖入會場。");

        if (weddingAmbience != null)
        {
            weddingAmbience.Stop();
        }

        if (tensionAmbience != null)
        {
            tensionAmbience.Play();
        }

        ShowLine("旁白", "鼓聲突然慢了下來。山路傳來急促的皮靴聲，兩名日本警察闖進婚禮會場。", 4.5f);
        yield return MovePlayerToWitnessPoint();
        yield return new WaitForSeconds(0.35f);

        if (animatePoliceEntranceWithoutTimeline)
        {
            yield return AnimatePoliceEntranceFallback();
        }
        else if (policeEnterTimeline != null)
        {
            PlayDirector(policeEnterTimeline);
            yield return WaitForDirector(policeEnterTimeline, 8f);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        ShowLine("日警", "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？", 4f);
        yield return new WaitForSeconds(4f);

        ShowLine("新郎", "我們只是辦婚禮，沒有冒犯。", 3.2f);
        yield return new WaitForSeconds(3.2f);

        if (useFallbackIncidentAnimation)
        {
            yield return KnockCupAndShoveVillagerFallback();
        }
        else
        {
            ShowLine("旁白", "警察推倒酒杯，又粗暴地推開靠近的族人。", 3.5f);
            yield return new WaitForSeconds(3.2f);
        }

        Transform harassingPolice = secondaryPoliceActor != null ? secondaryPoliceActor : primaryPoliceActor;
        if (femaleVillagerActor != null && harassingPolice != null)
        {
            ShowLine("旁白", "另一名警察把目光轉向一名女性族人，伸手逼近她。周圍的族人立刻騷動起來。", 4f);
            yield return MoveActorNearTarget(harassingPolice, femaleVillagerActor.position, 1.0f, policeApproachWomanSeconds);
            ShowLine("女性族人", "放開我！", 2.5f);
            PlayPoliceEventClip(struggleClip);
            yield return new WaitForSeconds(2.3f);
        }
        else
        {
            ShowLine("旁白", "一名警察試圖騷擾女性族人，四周的怒氣瞬間升高。", 3.8f);
            yield return new WaitForSeconds(3.4f);
        }

        ShowConflictChoice();
    }

    private IEnumerator AnimatePoliceEntranceFallback()
    {
        EnsurePoliceActorsForIntrusion();

        Transform firstPolice = primaryPoliceActor != null ? primaryPoliceActor : (policeGroup != null ? policeGroup.transform : null);
        if (firstPolice == null)
        {
            yield return new WaitForSeconds(3f);
            yield break;
        }

        Transform secondPolice = secondaryPoliceActor;
        Vector3 endPosition = policeEntranceTarget != null ? policeEntranceTarget.position : (hasPoliceOriginalTransform ? policeOriginalPosition : firstPolice.position);
        Quaternion endRotation = hasPoliceOriginalTransform ? policeOriginalRotation : firstPolice.rotation;
        Vector3 focusPosition = GetEntranceFocusPosition(endPosition);
        Vector3 awayFromFocus = endPosition - focusPosition;
        awayFromFocus.y = 0f;

        if (awayFromFocus.sqrMagnitude < 0.01f)
        {
            awayFromFocus = -GetAutoInteractionForward();
            awayFromFocus.y = 0f;
        }

        if (awayFromFocus.sqrMagnitude < 0.01f)
        {
            awayFromFocus = Vector3.back;
        }

        Vector3 startPosition = endPosition + awayFromFocus.normalized * Mathf.Max(1f, policeEntranceDistance);
        Vector3 pathDirection = endPosition - startPosition;
        pathDirection.y = 0f;

        Vector3 sideDirection = Vector3.Cross(Vector3.up, pathDirection.sqrMagnitude > 0.01f ? pathDirection.normalized : Vector3.forward);
        if (sideDirection.sqrMagnitude < 0.01f)
        {
            sideDirection = Vector3.right;
        }

        sideDirection.Normalize();
        float halfSpacing = Mathf.Max(0.15f, policePairSpacing) * 0.5f;
        Vector3 firstStart = startPosition - sideDirection * halfSpacing;
        Vector3 firstEnd = endPosition - sideDirection * halfSpacing;
        Vector3 secondStart = startPosition + sideDirection * halfSpacing;
        Vector3 secondEnd = endPosition + sideDirection * halfSpacing;

        SetActiveIncludingParents(firstPolice);
        firstPolice.position = firstStart;
        PlayAnimatorStateIfAvailable(firstPolice, policeWalkStateName);

        if (secondPolice != null)
        {
            SetActiveIncludingParents(secondPolice);
            secondPolice.position = secondStart;
            PlayAnimatorStateIfAvailable(secondPolice, policeWalkStateName);
        }

        Quaternion walkingRotation = endRotation;
        if (rotatePoliceTowardPath && pathDirection.sqrMagnitude > 0.01f)
        {
            walkingRotation = Quaternion.LookRotation(pathDirection.normalized, Vector3.up);
            firstPolice.rotation = walkingRotation;
            if (secondPolice != null)
            {
                secondPolice.rotation = walkingRotation;
            }
        }

        float duration = Mathf.Max(0.5f, policeEntranceDuration);
        float stagger = secondPolice != null ? Mathf.Max(0f, policeEntranceStaggerSeconds) : 0f;
        float totalDuration = duration + stagger;
        float elapsed = 0f;

        ShowLine("旁白", "兩名日警突然從山路闖入婚禮會場，族人的歌聲瞬間停下。", totalDuration);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float firstProgress = Mathf.Clamp01(elapsed / duration);
            firstPolice.position = Vector3.Lerp(firstStart, firstEnd, Mathf.SmoothStep(0f, 1f, firstProgress));

            if (secondPolice != null)
            {
                float secondProgress = Mathf.Clamp01((elapsed - stagger) / duration);
                secondPolice.position = Vector3.Lerp(secondStart, secondEnd, Mathf.SmoothStep(0f, 1f, secondProgress));
            }

            yield return null;
        }

        firstPolice.position = firstEnd;
        firstPolice.rotation = endRotation;
        PlayAnimatorStateIfAvailable(firstPolice, policeIdleStateName);

        if (secondPolice != null)
        {
            secondPolice.position = secondEnd;
            secondPolice.rotation = endRotation;
            PlayAnimatorStateIfAvailable(secondPolice, policeIdleStateName);
        }
    }

    private void EnsurePoliceActorsForIntrusion()
    {
        if (primaryPoliceActor == null)
        {
            Transform namedPolice = FindTransformByName(primaryPoliceObjectName);
            if (namedPolice != null)
            {
                primaryPoliceActor = namedPolice;
            }
            else if (policeGroup != null)
            {
                primaryPoliceActor = policeGroup.transform;
            }
        }

        if (policeGroup == null && primaryPoliceActor != null)
        {
            policeGroup = primaryPoliceActor.gameObject;
        }

        if (primaryPoliceActor != null && !hasPoliceOriginalTransform)
        {
            policeOriginalPosition = primaryPoliceActor.position;
            policeOriginalRotation = primaryPoliceActor.rotation;
            hasPoliceOriginalTransform = true;
        }

        if (secondaryPoliceActor == null)
        {
            secondaryPoliceActor = FindSecondaryPoliceActor();
        }

        if (secondaryPoliceActor == null && createSecondPoliceFromPrimary && primaryPoliceActor != null)
        {
            if (runtimeSecondPolice == null)
            {
                runtimeSecondPolice = Instantiate(primaryPoliceActor.gameObject, primaryPoliceActor.parent);
                runtimeSecondPolice.name = "Chapter1_SecondPolice";
            }

            secondaryPoliceActor = runtimeSecondPolice.transform;
        }

        if (secondaryPoliceActor != null && secondaryPoliceActor == primaryPoliceActor)
        {
            secondaryPoliceActor = null;
        }
    }

    private void SetActiveIncludingParents(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }

    private Transform FindSecondaryPoliceActor()
    {
        string[] names = { "警察 (1)", "警察2", "警察 2", "Police2", "SecondPolice", "Chapter1_SecondPolice" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform candidate = FindTransformByName(names[i]);
            if (candidate != null && candidate != primaryPoliceActor)
            {
                return candidate;
            }
        }

        return null;
    }

    private IEnumerator MovePlayerToWitnessPoint()
    {
        if (!guidePlayerToWitnessPoint)
        {
            yield break;
        }

        Transform root = GetDancePlayerRoot();
        if (root == null)
        {
            yield break;
        }

        Vector3 fireCenter = GetFireCenterPosition();
        Vector3 startPosition = GetSafeGuidedRootPosition(root, root.position, root.position);
        root.position = startPosition;
        Vector3 outward = startPosition - fireCenter;
        outward.y = 0f;

        if (outward.sqrMagnitude < 0.01f)
        {
            outward = -GetAutoInteractionForward();
            outward.y = 0f;
        }

        if (outward.sqrMagnitude < 0.01f)
        {
            outward = Vector3.back;
        }

        Vector3 targetPosition = fireCenter + outward.normalized * Mathf.Max(2f, witnessRunDistance);
        targetPosition.y = startPosition.y;
        targetPosition = GetSafeGuidedRootPosition(root, targetPosition, startPosition);

        Vector3 lookDirection = (fireCenter + Vector3.up * witnessLookAtHeight) - targetPosition;
        lookDirection.y = 0f;
        Quaternion startRotation = root.rotation;
        Quaternion targetRotation = lookDirection.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : startRotation;

        float duration = Mathf.Max(0.1f, witnessRunSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, eased);
            root.position = GetSafeGuidedRootPosition(root, nextPosition, root.position);
            root.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        root.position = GetSafeGuidedRootPosition(root, targetPosition, root.position);
        root.rotation = targetRotation;
    }

    private Vector3 GetEntranceFocusPosition(Vector3 fallbackPosition)
    {
        if (choiceFocusPoint != null)
        {
            return choiceFocusPoint.transform.position;
        }

        return GetFireCenterPosition();
    }

    private void ShowConflictChoice()
    {
        SetPlayerControl(false);
        waitingForChoice = true;
        choiceQuestion = "你要怎麼做？";
        optionALabel = "上前阻止";
        optionBLabel = "沉默觀望";
        SetMission("導火線事件：選擇上前阻止，或沉默觀望。可按 1 / 2。");

        if (heartbeatAudio != null)
        {
            heartbeatAudio.Play();
        }

        if (choiceUI != null)
        {
            choiceUI.Show(choiceQuestion, optionALabel, optionBLabel);
        }

        ShowLine("系統", "你要怎麼做？按 1 上前阻止，按 2 沉默觀望。", 8f);
    }

    public void ChooseIntervene()
    {
        ResolveChoice(ConflictChoice.Intervene);
    }

    public void ChooseWatch()
    {
        ResolveChoice(ConflictChoice.Watch);
    }

    public void ResolveChoice(ConflictChoice choice)
    {
        if (choiceResolved)
        {
            return;
        }

        choiceResolved = true;
        waitingForChoice = false;
        lastChoice = choice;

        if (choiceUI != null)
        {
            choiceUI.Hide();
        }

        StartCoroutine(ResolveChoiceRoutine(choice));
    }

    private IEnumerator ResolveChoiceRoutine(ConflictChoice choice)
    {
        if (heartbeatAudio != null)
        {
            heartbeatAudio.Stop();
        }

        if (choice == ConflictChoice.Intervene)
        {
            morale += 2;
            peopleInjured += Mathf.Max(1, casualtyVillagers != null && casualtyVillagers.Length > 0 ? casualtyVillagers.Length : 2);
            SetMission("分支：你選擇上前阻止。族人的憤怒被點燃，場面急速失控。");
            ShowLine("你", "夠了！不要再羞辱我們！", 2.8f);
            yield return new WaitForSeconds(2.4f);

            if (interveneTimeline != null)
            {
                PlayDirector(interveneTimeline);
                yield return WaitForDirector(interveneTimeline, 8f);
            }
            else
            {
                yield return InterveneFallbackRoutine();
            }
        }
        else
        {
            morale -= 1;
            peopleInjured += 1;
            SetMission("分支：你選擇沉默觀望。壓抑沒有消失，只是更深地留在族人心裡。");

            if (watchTimeline != null)
            {
                PlayDirector(watchTimeline);
                yield return WaitForDirector(watchTimeline, 8f);
            }
            else
            {
                yield return WatchFallbackRoutine();
            }
        }

        ShowLine("族人長者", "今天的事，族人不會忘記。", 3.5f);

        if (endingTimeline != null)
        {
            PlayDirector(endingTimeline);
            yield return WaitForDirector(endingTimeline, 6f);
        }
        else
        {
            yield return EndingFallbackRoutine();
        }

        ShowLine("字幕", "族人望著日警下山的背影。憤怒留在每個人的眼神裡，卻沒有人知道下一步該怎麼辦。", 5f);
        SetMission("第一章結尾：族人望著日警下山的背影，憤恨與無力留在婚禮現場。");
        SaveChapterResult();
        chapterCompleted = true;
        yield return new WaitForSeconds(3f);
        yield return Fade(0f, 1f, 1.5f);
    }

    private IEnumerator KnockCupAndShoveVillagerFallback()
    {
        ShowLine("旁白", "警察冷笑著抬手，把桌邊的酒杯掃倒。", 2.8f);
        PlayPoliceEventClip(cupCrashClip);

        if (ceremonyCupRigidbody != null)
        {
            ceremonyCupRigidbody.isKinematic = false;
            ceremonyCupRigidbody.useGravity = true;
            Vector3 forceDirection = primaryPoliceActor != null ? primaryPoliceActor.forward : Vector3.forward;
            ceremonyCupRigidbody.AddForce(forceDirection * 1.6f + Vector3.up * 0.55f, ForceMode.Impulse);
        }
        else if (ceremonyCup != null)
        {
            ceremonyCup.Rotate(Vector3.forward, 78f, Space.Self);
        }

        yield return new WaitForSeconds(1.2f);
        ShowLine("旁白", "一名族人上前質問，立刻被粗暴地推開。", 2.8f);

        if (shovedVillagerActor != null)
        {
            Vector3 away = shovedVillagerActor.position - (primaryPoliceActor != null ? primaryPoliceActor.position : GetFireCenterPosition());
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -shovedVillagerActor.forward;
            Vector3 destination = shovedVillagerActor.position + away.normalized * Mathf.Max(0.3f, shoveDistance);
            yield return MoveTransform(shovedVillagerActor, destination, 0.45f);
        }
        else
        {
            yield return new WaitForSeconds(0.7f);
        }
    }

    private IEnumerator WatchFallbackRoutine()
    {
        ShowLine("日警", "都給我安靜。你們最好記住自己的身分。", 3.2f);
        yield return new WaitForSeconds(2.8f);

        Transform draggingPolice = secondaryPoliceActor != null ? secondaryPoliceActor : primaryPoliceActor;
        if (femaleVillagerActor != null && draggingPolice != null && hutEntrancePoint != null)
        {
            ShowLine("旁白", "你沒有上前。警察強行拉著女性族人往小木屋走去。", 3.8f);
            yield return MoveTwoActorsToPoint(draggingPolice, femaleVillagerActor, hutEntrancePoint.position, dragToHutSeconds);
            femaleVillagerActor.gameObject.SetActive(false);
            PlayPoliceEventClip(painfulCryClip);
            ShowLine("旁白", "木屋門關上後，裡面傳出痛苦的叫喊聲。屋外的人全都僵在原地。", 5f);
            yield return new WaitForSeconds(4.6f);
        }
        else
        {
            PlayPoliceEventClip(painfulCryClip);
            ShowLine("旁白", "你沉默地站在原地。警察把女性族人帶向木屋，屋內隨後傳出痛苦的叫喊聲。", 5f);
            yield return new WaitForSeconds(4.6f);
        }
    }

    private IEnumerator InterveneFallbackRoutine()
    {
        ShowLine("旁白", "幾名族人憤而衝上前，聯手把警察推開，混亂中拳腳相向。", 4f);
        yield return MoveInterveningVillagersTowardPolice(1.0f);
        yield return new WaitForSeconds(1.3f);

        PlayPoliceEventClip(gunshotClip);
        ShowLine("旁白", "砰——槍聲突然響起。幾名族人在混亂中倒下，所有人瞬間停住。", 4.5f);
        MakeCasualtiesFall();
        yield return new WaitForSeconds(4f);
    }

    private IEnumerator EndingFallbackRoutine()
    {
        ShowLine("旁白", "兩名警察整理衣服，轉身沿著山路離開。婚禮現場只剩火堆與沉默。", 4.5f);

        Transform first = primaryPoliceActor;
        Transform second = secondaryPoliceActor;
        Vector3 target;
        if (policeExitPoint != null)
        {
            target = policeExitPoint.position;
        }
        else
        {
            Vector3 center = GetFireCenterPosition();
            Vector3 away = first != null ? first.position - center : Vector3.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            target = center + away.normalized * 12f;
        }

        yield return MovePolicePairToExit(first, second, target, policeExitSeconds);
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator MoveInterveningVillagersTowardPolice(float seconds)
    {
        if (interveneVillagers == null || interveneVillagers.Length == 0 || primaryPoliceActor == null)
        {
            yield return new WaitForSeconds(seconds);
            yield break;
        }

        Vector3[] starts = new Vector3[interveneVillagers.Length];
        Vector3[] ends = new Vector3[interveneVillagers.Length];
        for (int i = 0; i < interveneVillagers.Length; i++)
        {
            Transform actor = interveneVillagers[i];
            if (actor == null) continue;
            starts[i] = actor.position;
            Vector3 dir = actor.position - primaryPoliceActor.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.right * (i + 1);
            ends[i] = primaryPoliceActor.position + dir.normalized * (0.7f + i * 0.18f);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < interveneVillagers.Length; i++)
            {
                if (interveneVillagers[i] != null)
                    interveneVillagers[i].position = Vector3.Lerp(starts[i], ends[i], t);
            }
            yield return null;
        }
    }

    private void MakeCasualtiesFall()
    {
        if (casualtyVillagers == null) return;
        for (int i = 0; i < casualtyVillagers.Length; i++)
        {
            Transform casualty = casualtyVillagers[i];
            if (casualty == null) continue;
            Animator animator = casualty.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(villagerFallStateName))
            {
                int hash = Animator.StringToHash(villagerFallStateName);
                if (animator.HasState(0, hash))
                {
                    animator.Play(hash, 0, 0f);
                    continue;
                }
            }
            casualty.Rotate(Vector3.forward, fallbackFallAngle, Space.Self);
        }
    }

    private IEnumerator MoveActorNearTarget(Transform actor, Vector3 targetPosition, float stopDistance, float seconds)
    {
        if (actor == null) yield break;
        Vector3 direction = targetPosition - actor.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) yield break;
        Vector3 destination = targetPosition - direction.normalized * Mathf.Max(0.2f, stopDistance);
        yield return MoveTransform(actor, destination, seconds);
    }

    private IEnumerator MoveTransform(Transform target, Vector3 destination, float seconds)
    {
        if (target == null) yield break;
        Vector3 start = target.position;
        Quaternion startRot = target.rotation;
        Vector3 flat = destination - start;
        flat.y = 0f;
        Quaternion endRot = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : startRot;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            target.position = Vector3.Lerp(start, destination, t);
            target.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        target.position = destination;
    }

    private IEnumerator MoveTwoActorsToPoint(Transform police, Transform villager, Vector3 point, float seconds)
    {
        if (police == null || villager == null) yield break;
        Vector3 policeOffset = police.position - villager.position;
        policeOffset.y = 0f;
        if (policeOffset.sqrMagnitude < 0.05f) policeOffset = Vector3.right * 0.65f;
        policeOffset = policeOffset.normalized * 0.65f;
        Vector3 policeTarget = point + policeOffset;
        Vector3 villagerTarget = point;
        Vector3 pStart = police.position;
        Vector3 vStart = villager.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            police.position = Vector3.Lerp(pStart, policeTarget, t);
            villager.position = Vector3.Lerp(vStart, villagerTarget, t);
            yield return null;
        }
        police.position = policeTarget;
        villager.position = villagerTarget;
    }

    private IEnumerator MovePolicePairToExit(Transform first, Transform second, Vector3 target, float seconds)
    {
        if (first == null && second == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, seconds));
            yield break;
        }

        Vector3 firstStart = first != null ? first.position : Vector3.zero;
        Vector3 secondStart = second != null ? second.position : Vector3.zero;
        Vector3 side = Vector3.right * Mathf.Max(0.4f, policePairSpacing * 0.5f);
        Vector3 firstTarget = target - side;
        Vector3 secondTarget = target + side;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, seconds);
        PlayAnimatorStateIfAvailable(first, policeWalkStateName);
        PlayAnimatorStateIfAvailable(second, policeWalkStateName);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (first != null) first.position = Vector3.Lerp(firstStart, firstTarget, t);
            if (second != null) second.position = Vector3.Lerp(secondStart, secondTarget, t);
            yield return null;
        }
        PlayAnimatorStateIfAvailable(first, policeIdleStateName);
        PlayAnimatorStateIfAvailable(second, policeIdleStateName);
    }

    private void PlayAnimatorStateIfAvailable(Transform actor, string stateName)
    {
        if (actor == null || string.IsNullOrWhiteSpace(stateName)) return;
        Animator animator = actor.GetComponentInChildren<Animator>();
        if (animator == null) return;
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
    }

    private void PlayPoliceEventClip(AudioClip clip)
    {
        if (clip == null) return;
        if (policeEventAudio == null)
        {
            policeEventAudio = gameObject.AddComponent<AudioSource>();
            policeEventAudio.playOnAwake = false;
            policeEventAudio.spatialBlend = 0f;
        }
        policeEventAudio.PlayOneShot(clip);
    }

    public void SetPlayerControl(bool enabled)
    {
        if (playerController != null && playerController != this)
        {
            playerController.enabled = enabled;
        }

        if (characterController != null)
        {
            characterController.enabled = enabled;
        }
    }

    public bool IsChapterCompleted()
    {
        return chapterCompleted;
    }

    public ConflictChoice GetLastChoice()
    {
        return lastChoice;
    }

    public int GetDeliveredWineCount()
    {
        return deliveredWineCount;
    }

    public int GetSharedFoodCount()
    {
        return sharedFoodCount;
    }

    public int GetPeopleInjured()
    {
        return peopleInjured;
    }

    public int GetMorale()
    {
        return morale;
    }

    public bool IsFreeExplorationActive()
    {
        if (!freeExplorationUnlocked || policeSequenceStarted || waitingForChoice || chapterCompleted)
        {
            return false;
        }

        return true;
    }

    public bool CanUseDanceInteraction()
    {
        return IsFreeExplorationActive() && !danceFinished && !danceRoutineRunning;
    }

    public bool ShouldReserveEForDanceAtFireCenter()
    {
        return reserveEForDanceAtFireCenter && ShouldShowCenterInteractionMenu() && CanUseDanceInteraction();
    }

    private void ShowLine(string speaker, string line, float seconds)
    {
        if (dialogueUI != null)
        {
            // 有正式字幕 UI 時，只使用它，避免 OnGUI 又畫一次造成重疊。
            dialogueUI.ShowLine(speaker, line, seconds);
            fallbackSpeaker = "";
            fallbackLine = "";
            fallbackLineUntil = 0f;
        }
        else
        {
            // 沒有綁 DialogueUI 才使用舊版 fallback HUD。
            fallbackSpeaker = speaker;
            fallbackLine = line;
            fallbackLineUntil = Time.time + Mathf.Max(0.5f, seconds);
        }

        Debug.Log("[Chapter1 Dialogue] " + speaker + ": " + line);
    }

    private void SetMission(string text)
    {
        missionText = text;
        Debug.Log("[Chapter1 Mission] " + text);
    }

    private void SetTemporaryMission(string text, float seconds, bool clearWhenPlayerMoves = false)
    {
        SetMission(text);

        if (clearMissionRoutine != null)
        {
            StopCoroutine(clearMissionRoutine);
        }

        clearMissionWhenPlayerMoves = clearWhenPlayerMoves;
        movementSensitiveMissionText = clearWhenPlayerMoves ? text : "";
        temporaryMissionStartPlayerPosition = GetCurrentPlayerPosition();
        clearMissionRoutine = StartCoroutine(ClearMissionAfterSeconds(text, seconds));
    }

    private IEnumerator ClearMissionAfterSeconds(string expectedText, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, seconds));

        if (missionText == expectedText)
        {
            missionText = "";
        }

        if (movementSensitiveMissionText == expectedText)
        {
            clearMissionWhenPlayerMoves = false;
            movementSensitiveMissionText = "";
        }

        clearMissionRoutine = null;
    }

    private void UpdateTemporaryMissionMovementClear()
    {
        if (!clearMissionWhenPlayerMoves)
        {
            return;
        }

        if (missionText != movementSensitiveMissionText)
        {
            clearMissionWhenPlayerMoves = false;
            movementSensitiveMissionText = "";
            return;
        }

        Vector3 currentPlayerPosition = GetCurrentPlayerPosition();
        Vector3 movement = currentPlayerPosition - temporaryMissionStartPlayerPosition;
        movement.y = 0f;

        if (movement.sqrMagnitude < 0.09f)
        {
            return;
        }

        missionText = "";
        clearMissionWhenPlayerMoves = false;
        movementSensitiveMissionText = "";

        if (clearMissionRoutine != null)
        {
            StopCoroutine(clearMissionRoutine);
            clearMissionRoutine = null;
        }
    }

    private IEnumerator ShowLineAfterDelay(string speaker, string line, float delay, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        ShowLine(speaker, line, seconds);
    }

    private string GetNextVillagerTalkLine()
    {
        if (villagerTalkLines.Length == 0)
        {
            return "願祖靈庇佑新人。";
        }

        string line = villagerTalkLines[talkLineIndex % villagerTalkLines.Length];
        talkLineIndex++;
        return line;
    }

    private void SaveChapterResult()
    {
        if (!saveResultToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetString(conflictChoicePrefsKey, lastChoice.ToString());
        PlayerPrefs.SetInt(peopleInjuredPrefsKey, peopleInjured);
        PlayerPrefs.SetInt(moralePrefsKey, morale);
        PlayerPrefs.Save();
    }

    private void AutoFindReferences()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<Chapter1DialogueUI>(true);
        }

        if (choiceUI == null)
        {
            choiceUI = FindObjectOfType<Chapter1ChoiceUI>(true);
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PCPlayerController>(true);
        }

        if (characterController == null)
        {
            characterController = FindObjectOfType<CharacterController>(true);
        }

        if (playerRoot == null)
        {
            playerRoot = FindTransformByName("XR Origin (VR)");
        }

        if (playerRoot == null)
        {
            playerRoot = FindTransformByName("XR Origin");
        }

        if (playerRoot == null && playerController != null)
        {
            playerRoot = playerController.transform;
        }

        if (danceCenter == null)
        {
            danceCenter = FindTransformByName("DanceTrigger");
        }

        if (primaryPoliceActor == null && !string.IsNullOrWhiteSpace(primaryPoliceObjectName))
        {
            primaryPoliceActor = FindTransformByName(primaryPoliceObjectName);
        }


        if (groomActor == null)
        {
            groomActor = FindTransformByName("新郎");
        }

        if (femaleVillagerActor == null)
        {
            femaleVillagerActor = FindTransformByName("新娘");
            if (femaleVillagerActor == null) femaleVillagerActor = FindTransformByName("女性族人");
        }

        if (shovedVillagerActor == null)
        {
            shovedVillagerActor = FindTransformByName("被推族人");
        }

        if (ceremonyCup == null)
        {
            ceremonyCup = FindTransformByName("酒杯");
        }
        if (ceremonyCupRigidbody == null && ceremonyCup != null)
        {
            ceremonyCupRigidbody = ceremonyCup.GetComponent<Rigidbody>();
        }

        if (hutEntrancePoint == null)
        {
            hutEntrancePoint = FindTransformByName("小木屋入口");
            if (hutEntrancePoint == null) hutEntrancePoint = FindTransformByName("HutEntrancePoint");
        }

        if (policeExitPoint == null)
        {
            policeExitPoint = FindTransformByName("PoliceExitPoint");
        }

        if (policeGroup == null)
        {
            Transform police = FindTransformByName("PoliceIntrusionSequence");
            if (police == null)
            {
                police = FindTransformByName("PoliceGroup");
            }

            if (police == null && primaryPoliceActor != null)
            {
                police = primaryPoliceActor;
            }

            if (police != null)
            {
                policeGroup = police.gameObject;
            }
        }
    }

    private void RepairInteractionHudRuntimeDefaults()
    {
        showFallbackHud = true;
        showCenterInteractionMenu = true;
        showWorldInteractionPrompt = true;
        // 不再在遊戲開始時強制恢復火堆距離限制。
        // 這樣 Inspector 中取消 Require Fire Distance For Center Menu 才會真的生效。
        requireFireDistanceForCenterMenu = false;
        autoCreateMissingExplorationInteractions = true;
        startPoliceAfterDance = false;
        requireWineBeforeDance = true;
        requireWineAndFoodBeforeDance = true;
        requireAllWeddingTasksBeforePolice = true;
        autoStartPoliceAfterWeddingTasks = true;
        foodTargetCount = Mathf.Max(1, foodTargetCount);
        centerDanceKey = GetDanceInteractionKey();
        if (wineBottleTargetSize <= 0f || wineBottleTargetSize > 0.4f)
        {
            wineBottleTargetSize = 0.38f;
        }
        centerInteractionRange = Mathf.Max(centerInteractionRange, 18f);
        autoInteractionDistance = Mathf.Max(autoInteractionDistance, 4.5f);
        heldPropScale = Mathf.Max(heldPropScale, 0.42f);
        carriedPropViewDistance = Mathf.Max(carriedPropViewDistance, 0.95f);
        firstPersonHoldSeconds = Mathf.Max(firstPersonHoldSeconds, 0.85f);
    }

    private Transform GetPlayerViewTransform()
    {
        if (playerRoot != null)
        {
            Camera[] childCameras = playerRoot.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < childCameras.Length; i++)
            {
                Camera childCamera = childCameras[i];
                if (childCamera != null && childCamera.isActiveAndEnabled)
                {
                    return childCamera.transform;
                }
            }

            if (childCameras.Length > 0 && childCameras[0] != null)
            {
                return childCameras[0].transform;
            }
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private Transform GetDancePlayerRoot()
    {
        if (playerRoot != null)
        {
            return playerRoot;
        }

        if (characterController != null)
        {
            return characterController.transform;
        }

        if (playerController != null)
        {
            return playerController.transform;
        }

        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin != null)
        {
            return xrOrigin.transform;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.root;
        }

        return null;
    }

    private Transform GetDanceCenter()
    {
        Vector3 fireCenter = GetFireCenterPosition();
        float allowedOffset = Mathf.Max(1f, maxDanceCenterOffsetForFireCenter);

        if (danceCenter != null && GetFlatDistance(danceCenter.position, fireCenter) <= allowedOffset)
        {
            return danceCenter;
        }

        if (choiceFocusPoint != null && GetFlatDistance(choiceFocusPoint.transform.position, fireCenter) <= allowedOffset)
        {
            return choiceFocusPoint.transform;
        }

        return transform;
    }

    private Transform FindTransformByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            return target.transform;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null
                && candidate.name == objectName
                && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private void PlayDirector(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.time = 0;
        director.Play();
    }

    private IEnumerator WaitForDirector(PlayableDirector director, float fallbackSeconds)
    {
        if (director == null)
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        while (director.state == PlayState.Playing)
        {
            yield return null;
        }
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (fadeCanvas == null)
        {
            yield break;
        }

        fadeCanvas.blocksRaycasts = to > 0.01f;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / seconds);
            yield return null;
        }

        fadeCanvas.alpha = to;
        fadeCanvas.blocksRaycasts = to > 0.01f;
    }

    private void EnsureHudStyles()
    {
        if (hudBoxStyle != null)
        {
            return;
        }

        Texture2D background = MakeTexture(new Color(0.03f, 0.03f, 0.03f, 0.78f));

        hudBoxStyle = new GUIStyle(GUI.skin.box);
        hudBoxStyle.normal.background = background;
        hudBoxStyle.padding = new RectOffset(18, 18, 14, 14);

        hudTitleStyle = new GUIStyle(GUI.skin.label);
        hudTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.68f, 1f);
        hudTitleStyle.fontSize = Mathf.Clamp(Screen.height / 34, 18, 28);
        hudTitleStyle.fontStyle = FontStyle.Bold;
        hudTitleStyle.wordWrap = true;

        hudBodyStyle = new GUIStyle(GUI.skin.label);
        hudBodyStyle.normal.textColor = Color.white;
        hudBodyStyle.fontSize = Mathf.Clamp(Screen.height / 42, 16, 24);
        hudBodyStyle.wordWrap = true;

        hudButtonStyle = new GUIStyle(GUI.skin.button);
        hudButtonStyle.fontSize = Mathf.Clamp(Screen.height / 38, 17, 25);
        hudButtonStyle.alignment = TextAnchor.MiddleLeft;
        hudButtonStyle.padding = new RectOffset(18, 18, 8, 8);
        hudButtonStyle.wordWrap = true;

        timerBoxStyle = new GUIStyle(GUI.skin.box);
        timerBoxStyle.normal.background = background;
        timerBoxStyle.padding = new RectOffset(14, 14, 10, 10);

        timerTitleStyle = new GUIStyle(GUI.skin.label);
        timerTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.68f, 1f);
        timerTitleStyle.fontSize = Mathf.Clamp(Screen.height / 48, 14, 20);
        timerTitleStyle.fontStyle = FontStyle.Bold;
        timerTitleStyle.alignment = TextAnchor.MiddleRight;

        timerNumberStyle = new GUIStyle(GUI.skin.label);
        timerNumberStyle.normal.textColor = Color.white;
        timerNumberStyle.fontSize = Mathf.Clamp(Screen.height / 30, 24, 38);
        timerNumberStyle.fontStyle = FontStyle.Bold;
        timerNumberStyle.alignment = TextAnchor.MiddleRight;

        centerMenuStyle = new GUIStyle(GUI.skin.box);
        centerMenuStyle.normal.background = background;
        centerMenuStyle.normal.textColor = Color.white;
        centerMenuStyle.fontSize = Mathf.Clamp(Screen.height / 52, 14, 20);
        centerMenuStyle.alignment = TextAnchor.MiddleLeft;
        centerMenuStyle.padding = new RectOffset(18, 18, 10, 10);
        centerMenuStyle.wordWrap = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}






