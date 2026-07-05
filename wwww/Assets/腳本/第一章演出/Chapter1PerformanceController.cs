using System.Collections;
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
    public bool startPoliceAfterDance = true;

    [Header("UI")]
    public Chapter1DialogueUI dialogueUI;
    public Chapter1ChoiceUI choiceUI;
    public CanvasGroup fadeCanvas;

    [Header("Audio")]
    public AudioSource weddingAmbience;
    public AudioSource tensionAmbience;
    public AudioSource heartbeatAudio;

    [Header("Quest Progress")]
    public int wineTargetCount = 3;
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
    public float fallbackUnlockFreeExplorationAfterSeconds = 10f;
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
    public float centerInteractionRange = 5f;
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
    public Color winePropColor = new Color(0.45f, 0.12f, 0.08f, 1f);
    public Color foodPropColor = new Color(0.95f, 0.58f, 0.18f, 1f);

    [Header("Police Entrance Fallback")]
    public bool animatePoliceEntranceWithoutTimeline = true;
    public Transform policeEntranceTarget;
    public float policeEntranceDistance = 12f;
    public float policeEntranceDuration = 4f;
    public bool rotatePoliceTowardPath = true;

    [Header("Result For Later Chapters")]
    public bool saveResultToPlayerPrefs = true;
    public string conflictChoicePrefsKey = "Chapter1_ConflictChoice";
    public string peopleInjuredPrefsKey = "Chapter1_PeopleInjured";
    public string moralePrefsKey = "Chapter1_Morale";

    private int deliveredWineCount;
    private int sharedFoodCount;
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

    private GUIStyle hudBoxStyle;
    private GUIStyle hudTitleStyle;
    private GUIStyle hudBodyStyle;
    private GUIStyle hudButtonStyle;
    private GUIStyle timerBoxStyle;
    private GUIStyle timerTitleStyle;
    private GUIStyle timerNumberStyle;
    private GUIStyle centerMenuStyle;

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
        if (autoStartOnAwake || autoBeginStoryIfControllerExists)
        {
            BeginChapter();
        }
        else
        {
            SetMission("自由探索婚禮：與族人交談、幫新郎送酒，或靠近舞圈加入舞蹈。");
        }

        StartCoroutine(EnsureFreeExplorationFallback());
    }

    private void Update()
    {
        if (debugStartPoliceWithP && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[Chapter1] Debug P key pressed.");
            StartPoliceSequence();
        }

        if (debugStartDanceWithJ && Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("[Chapter1] Debug J key pressed.");
            JoinDance(danceCenter, playerRoot);
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
    }

    private void OnGUI()
    {
        if (!showFallbackHud)
        {
            return;
        }

        EnsureHudStyles();

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

        if (Time.time < fallbackLineUntil && (!string.IsNullOrEmpty(fallbackSpeaker) || !string.IsNullOrEmpty(fallbackLine)))
        {
            float width = Mathf.Min(Screen.width - 40f, 840f);
            float height = 118f;
            Rect box = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 36f, width, height);
            GUI.Box(box, GUIContent.none, hudBoxStyle);
            GUI.Label(new Rect(box.x + 24f, box.y + 18f, box.width - 48f, 28f), fallbackSpeaker, hudTitleStyle);
            GUI.Label(new Rect(box.x + 24f, box.y + 50f, box.width - 48f, 52f), fallbackLine, hudBodyStyle);
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

    public void BeginChapter()
    {
        if (storyStarted)
        {
            return;
        }

        storyStarted = true;
        deliveredWineCount = 0;
        sharedFoodCount = 0;
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

        SetPlayerControl(false);
        StartCoroutine(BeginChapterRoutine());
    }

    private IEnumerator BeginChapterRoutine()
    {
        yield return Fade(1f, 0f, 1.2f);

        ShowLine("字幕", "1930.10.7，霧社。火光照亮婚禮，鼓聲和歌聲在山間回盪。", 4f);
        yield return new WaitForSeconds(3.8f);

        ShowLine("旁白", "你睜開眼，看見族人圍著火堆歌舞。今晚本該只是祝福新人的夜晚。", 4.5f);
        yield return new WaitForSeconds(4.2f);

        if (weddingAmbience != null && !weddingAmbience.isPlaying)
        {
            weddingAmbience.Play();
        }

        SetMission("自由探索婚禮：與族人交談、幫新郎送酒，或靠近舞圈加入舞蹈。");
        UnlockFreeExploration();
        SetPlayerControl(true);
    }

    private IEnumerator EnsureFreeExplorationFallback()
    {
        float waitSeconds = Mathf.Max(1f, fallbackUnlockFreeExplorationAfterSeconds);
        yield return new WaitForSeconds(waitSeconds);

        if (freeExplorationUnlocked || policeSequenceStarted || waitingForChoice || chapterCompleted)
        {
            yield break;
        }

        if (!storyStarted)
        {
            storyStarted = true;
        }

        ShowLine("系統", "自由探索開始。靠近族人、酒桌或火堆旁的互動點，按 E 互動。", 3f);
        UnlockFreeExploration();
        SetPlayerControl(true);
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
            return;
        }

        explorationTimerRemaining = Mathf.Max(1f, explorationDurationSeconds);
        explorationTimerFinished = false;
        explorationTimerRunning = true;
        startPoliceWhenDanceEnds = false;
        SetMission("自由探索倒數開始：在時間結束前可以送酒、分享食物、與族人互動或加入舞蹈。");
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

        if (danceRoutineRunning)
        {
            startPoliceWhenDanceEnds = true;
            SetMission("倒數結束：舞蹈結束後，導火線事件就會發生。");
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
        if (!ShouldShowCenterInteractionMenu())
        {
            return;
        }

        if (IsCenterActionPressed(centerDanceKey, centerDanceAltKey, centerDanceGamepadKey) && CanUseDanceInteraction())
        {
            JoinDance(danceCenter, playerRoot);
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

    private bool IsKeyPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    private bool ShouldShowCenterInteractionMenu()
    {
        return showCenterInteractionMenu
            && IsFreeExplorationActive()
            && IsPlayerNearFireCenter();
    }

    private bool IsPlayerNearFireCenter()
    {
        Vector3 center = GetFireCenterPosition();
        Vector3 player = GetCurrentPlayerPosition();
        return Vector3.Distance(center, player) <= Mathf.Max(1f, centerInteractionRange);
    }

    private Vector3 GetFireCenterPosition()
    {
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

    private Vector3 GetCurrentPlayerPosition()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        if (playerRoot != null)
        {
            return playerRoot.position;
        }

        Transform player = GetDancePlayerRoot();
        return player != null ? player.position : transform.position;
    }

    private void DrawCenterInteractionMenu()
    {
        string danceText = CanUseDanceInteraction() ? "E / 1 / 手把A  加入舞蹈" : "E / 1 / 手把A  舞蹈已完成";
        string text = danceText + "\nR / 2 / 手把B  送酒\nT / 3 / 手把X  分享食物";
        float width = Mathf.Min(Screen.width - 40f, 380f);
        float height = 104f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 28f, width, height);
        GUI.Box(rect, text, centerMenuStyle);
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

        deliveredWineCount++;
        morale++;

        string speaker = string.IsNullOrWhiteSpace(npcName) ? "賓客" : npcName;
        PlayGiveItemAnimation(
            true,
            "你拿起酒杯，走向賓客。",
            speaker,
            "哈哈，這杯我收下了。願祖靈庇佑新人。",
            receiverTarget);

        int target = Mathf.Max(1, wineTargetCount);
        int shownCount = Mathf.Min(deliveredWineCount, target);
        SetTemporaryMission("送酒任務：" + shownCount + " / " + target + " 位賓客已收到酒。", 3.5f);

        if (deliveredWineCount >= target)
        {
            StartCoroutine(ShowLineAfterDelay("新郎", "謝謝你，朋友。鼓聲越來越熱，去舞圈那邊吧。", 2.6f, 3.5f));
        }
    }

    public void ShareFood(string npcName, Transform receiverTarget = null)
    {
        if (interactionAnimationRunning)
        {
            ShowLine("系統", "先等目前的互動動作結束。", 1.5f);
            return;
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
        SetTemporaryMission("你把食物分享給族人。", 3.5f);
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
            CreateAutoInteraction(
                "Auto_FoodShare_1",
                anchor - forward * 4f + right * 2.4f,
                Chapter1Interactable.InteractionType.ShareFood,
                "族人",
                "按 E 分享食物",
                new Color(0.85f, 0.45f, 0.12f, 1f));

            CreateAutoInteraction(
                "Auto_FoodShare_2",
                anchor - forward * 4.5f - right * 2.4f,
                Chapter1Interactable.InteractionType.ShareFood,
                "族人",
                "按 E 分享食物",
                new Color(0.85f, 0.45f, 0.12f, 1f));
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
        interactable.alsoUseEKey = true;
        interactable.disableAfterUse = true;
        interactable.hidePromptAfterUse = true;
        interactable.showPrompt = true;
        interactable.promptText = prompt;
        interactable.useDistanceCheck = true;
        interactable.interactRange = Mathf.Max(1.5f, autoInteractionDistance);
        interactable.speakerName = speaker;
        interactable.rotateDialogueLines = false;
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

        if (lockPlayerDuringInteractionAnimation)
        {
            SetPlayerControl(false);
        }

        GameObject prop = CreateHeldProp(isWine);
        Vector3 startPosition = GetItemPickupPosition(isWine);
        Vector3 endPosition = GetItemReceiverPosition(isWine, receiverTarget);
        prop.transform.position = startPosition;

        ShowLine("動作", actionLine, interactionAnimationSeconds);

        float duration = Mathf.Max(0.4f, interactionAnimationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position.y += Mathf.Sin(eased * Mathf.PI) * interactionArcHeight;
            prop.transform.position = position;
            prop.transform.Rotate(Vector3.up, 140f * Time.deltaTime, Space.World);
            yield return null;
        }

        prop.transform.position = endPosition;
        Destroy(prop, 0.15f);

        ShowLine(receiverName, receiverLine, 3f);

        if (lockPlayerDuringInteractionAnimation)
        {
            SetPlayerControl(true);
        }

        interactionAnimationRunning = false;
    }

    private GameObject CreateHeldProp(bool isWine)
    {
        PrimitiveType primitive = isWine ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
        GameObject prop = GameObject.CreatePrimitive(primitive);
        prop.name = isWine ? "Chapter1_WineCup_Animation" : "Chapter1_Food_Animation";

        Collider propCollider = prop.GetComponent<Collider>();
        if (propCollider != null)
        {
            propCollider.enabled = false;
        }

        float scale = Mathf.Max(0.08f, heldPropScale);
        prop.transform.localScale = isWine
            ? new Vector3(scale * 0.55f, scale * 0.75f, scale * 0.55f)
            : Vector3.one * scale;

        Renderer propRenderer = prop.GetComponent<Renderer>();
        if (propRenderer != null)
        {
            propRenderer.material.color = isWine ? winePropColor : foodPropColor;
        }

        return prop;
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
        int count = isWine ? Mathf.Max(1, wineTargetCount) : 2;
        int progress = isWine ? deliveredWineCount : sharedFoodCount;
        int index = ((Mathf.Max(1, progress) - 1) % count) + 1;

        GameObject target = GameObject.Find(prefix + index);
        if (target != null)
        {
            return target.transform;
        }

        target = GameObject.Find(prefix + "1");
        return target != null ? target.transform : null;
    }

    private Vector3 GetAutoInteractionAnchor()
    {
        if (danceCenter != null)
        {
            return danceCenter.position;
        }

        if (playerRoot != null)
        {
            return playerRoot.position;
        }

        Transform player = GetDancePlayerRoot();
        return player != null ? player.position : transform.position;
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
        if (policeSequenceStarted)
        {
            Debug.Log("[Chapter1] Police sequence already started.");
            return;
        }

        policeSequenceStarted = true;
        freeExplorationUnlocked = false;
        explorationTimerRunning = false;
        explorationTimerFinished = true;
        Debug.Log("[Chapter1] StartPoliceSequence called.");
        StartCoroutine(PoliceSequenceRoutine());
    }

    private IEnumerator PoliceSequenceRoutine()
    {
        SetPlayerControl(!lockPlayerDuringPoliceEntrance);
        SetMission("婚禮中斷：遠處傳來皮靴聲，日警正朝會場走來。");

        if (weddingAmbience != null)
        {
            weddingAmbience.Stop();
        }

        if (tensionAmbience != null)
        {
            tensionAmbience.Play();
        }

        if (policeGroup != null)
        {
            policeGroup.SetActive(true);
            Debug.Log("[Chapter1] Police group activated: " + policeGroup.name);
        }
        else
        {
            Debug.LogWarning("[Chapter1] Cannot activate police. Police group is empty.");
        }

        ShowLine("旁白", "鼓聲慢了下來。幾名族人望向山路，笑聲像被夜風壓住。", 4f);
        yield return new WaitForSeconds(1f);

        if (policeEnterTimeline != null)
        {
            PlayDirector(policeEnterTimeline);
            yield return WaitForDirector(policeEnterTimeline, 8f);
        }
        else if (animatePoliceEntranceWithoutTimeline)
        {
            yield return AnimatePoliceEntranceFallback();
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        ShowLine("日警", "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？", 4f);
        yield return new WaitForSeconds(4f);

        ShowLine("新郎", "我們只是辦婚禮，沒有冒犯。", 3.2f);
        yield return new WaitForSeconds(3.2f);

        ShowLine("旁白", "酒杯被推倒，火光照在族人握緊的手上。有人低聲喊著：住手。", 4.5f);
        yield return new WaitForSeconds(4f);

        ShowConflictChoice();
    }

    private IEnumerator AnimatePoliceEntranceFallback()
    {
        if (policeGroup == null)
        {
            yield return new WaitForSeconds(3f);
            yield break;
        }

        Transform policeTransform = policeGroup.transform;
        Vector3 endPosition = policeEntranceTarget != null ? policeEntranceTarget.position : policeOriginalPosition;
        Quaternion endRotation = hasPoliceOriginalTransform ? policeOriginalRotation : policeTransform.rotation;

        Vector3 focusPosition = GetEntranceFocusPosition(endPosition);
        Vector3 awayFromFocus = endPosition - focusPosition;
        awayFromFocus.y = 0f;

        if (awayFromFocus.sqrMagnitude < 0.01f)
        {
            awayFromFocus = -policeTransform.forward;
            awayFromFocus.y = 0f;
        }

        if (awayFromFocus.sqrMagnitude < 0.01f)
        {
            awayFromFocus = Vector3.back;
        }

        Vector3 startPosition = endPosition + awayFromFocus.normalized * Mathf.Max(1f, policeEntranceDistance);
        float duration = Mathf.Max(0.5f, policeEntranceDuration);
        float elapsed = 0f;

        policeTransform.position = startPosition;

        if (rotatePoliceTowardPath)
        {
            Vector3 pathDirection = endPosition - startPosition;
            pathDirection.y = 0f;

            if (pathDirection.sqrMagnitude > 0.01f)
            {
                policeTransform.rotation = Quaternion.LookRotation(pathDirection.normalized, Vector3.up);
            }
        }

        ShowLine("旁白", "兩名日警從山路走入婚禮會場，族人的歌聲逐漸停下。", duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            policeTransform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);
            yield return null;
        }

        policeTransform.position = endPosition;
        policeTransform.rotation = endRotation;
    }

    private Vector3 GetEntranceFocusPosition(Vector3 fallbackPosition)
    {
        if (choiceFocusPoint != null)
        {
            return choiceFocusPoint.transform.position;
        }

        if (danceCenter != null)
        {
            return danceCenter.position;
        }

        if (playerRoot != null)
        {
            return playerRoot.position;
        }

        Transform player = GetDancePlayerRoot();
        return player != null ? player.position : fallbackPosition + Vector3.forward;
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
        if (choice == ConflictChoice.Intervene)
        {
            morale += 2;
            peopleInjured += 2;
            SetMission("分支：你選擇上前阻止。族人的憤怒被點燃，場面急速失控。");
            ShowLine("你", "夠了。不要再羞辱我們。", 3f);
            yield return new WaitForSeconds(2.8f);

            ShowLine("旁白", "幾名族人跟著衝上前，日警的手摸向槍套。畫面在混亂聲中壓暗。", 4.5f);
            PlayDirector(interveneTimeline);
            yield return WaitForDirector(interveneTimeline, 8f);
        }
        else
        {
            morale -= 1;
            peopleInjured += 1;
            SetMission("分支：你選擇沉默觀望。壓抑沒有消失，只是更深地留在族人心裡。");
            ShowLine("新郎", "我們還要忍到什麼時候？", 3f);
            yield return new WaitForSeconds(2.8f);

            ShowLine("旁白", "你沒有上前。火堆旁只剩下低沉的喘息、退後的腳步，和無法說出口的屈辱。", 4.5f);
            PlayDirector(watchTimeline);
            yield return WaitForDirector(watchTimeline, 8f);
        }

        ShowLine("族人長者", "今天的事，族人不會忘記。", 4f);
        PlayDirector(endingTimeline);
        yield return WaitForDirector(endingTimeline, 6f);

        ShowLine("字幕", "壓抑，正在接近臨界點。", 4f);
        SetMission("第一章結尾：族人望著日警下山的背影，憤怒與無力留在婚禮現場。");
        SaveChapterResult();
        chapterCompleted = true;
        yield return new WaitForSeconds(2f);
        yield return Fade(0f, 1f, 1.5f);
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
            dialogueUI.ShowLine(speaker, line, seconds);
        }

        fallbackSpeaker = speaker;
        fallbackLine = line;
        fallbackLineUntil = Time.time + Mathf.Max(0.5f, seconds);

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

        if (policeGroup == null)
        {
            Transform police = FindTransformByName("PoliceIntrusionSequence");
            if (police == null)
            {
                police = FindTransformByName("PoliceGroup");
            }

            if (police != null)
            {
                policeGroup = police.gameObject;
            }
        }
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
        if (danceCenter != null)
        {
            return danceCenter;
        }

        if (choiceFocusPoint != null)
        {
            return choiceFocusPoint.transform;
        }

        return null;
    }

    private Transform FindTransformByName(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.transform : null;
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
