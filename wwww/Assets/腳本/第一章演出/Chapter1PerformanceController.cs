using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Chapter1PerformanceController : MonoBehaviour
{
    public bool IsPoliceSequenceStarted => policeSequenceStarted;

    private static readonly string[] AddedWeddingDancerNames =
    {
        "1",
        "2",
        "3",
        "4",
        "5",
        "賽德克新娘"
    };

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
    public bool normalizeNamedAddedDancerHeight = true;
    [Range(0.8f, 1.2f)] public float namedAddedDancerHeightRatio = 1f;
    [Range(0, 2)] public int weddingReservedPlayerSlots = 1;
    public bool autoFitWeddingCircleToArmReach = true;
    [Range(1.5f, 1.9f)] public float weddingNeighborSpacingArmMultiplier = 1.7f;
    public float weddingCircleRadius = 22f;
    public float weddingCircleTempoBpm = 88f;
    public float weddingCircleDegreesPerBeat = 1.65f;

    [Header("UI")]
    public Chapter1DialogueUI dialogueUI;
    public Chapter1ChoiceUI choiceUI;
    public CanvasGroup fadeCanvas;

    [Header("Audio")]
    public AudioSource weddingAmbience;

    [Tooltip("族人唱歌的背景 Audio Source。可直接拖 Hierarchy 裡的 WeddingVocals；留空時會自動尋找。")]
    public AudioSource weddingVocals;

    public AudioSource tensionAmbience;
    public AudioSource heartbeatAudio;

    [Header("Wedding Music Volume")]
    [Tooltip("婚禮鼓聲固定音量。這版預設 0.002，比原本 0.005 更小聲。")]
    [Range(0f, 1f)] public float weddingMusicVolume = 0.002f;

    [Tooltip("族人歌聲固定音量。")]
    [Range(0f, 1f)] public float weddingVocalsVolume = 0.22f;

    [Tooltip("警察事件開始前，每幀把鼓聲音量鎖住，避免 Play 後被其他設定改回 1。")]
    public bool lockWeddingMusicVolumeUntilPolice = true;

    [Tooltip("警察事件開始前，每幀把族人歌聲音量鎖住。")]
    public bool lockWeddingVocalsVolumeUntilPolice = true;

    [Tooltip("警察進場時，鼓聲與歌聲立刻一起停掉，再播放緊張音樂。")]
    public bool hardCutWeddingAudioOnPolice = true;

    [Header("Receiver Thank You Voice")]
    [Tooltip("保底『謝謝』語音。男女專用語音沒指定時才使用。")]
    public AudioClip receiverThanksVoice;

    [Tooltip("男性 NPC 收到物品時的『謝謝』語音。")]
    public AudioClip maleReceiverThanksVoice;

    [Tooltip("女性 NPC 收到物品時的『謝謝』語音。")]
    public AudioClip femaleReceiverThanksVoice;

    [Tooltip("送酒專用語音。男女專用語音都沒指定時才使用。")]
    public AudioClip wineReceiverThanksVoice;

    [Tooltip("收到食物專用語音。男女專用語音都沒指定時才使用。")]
    public AudioClip foodReceiverThanksVoice;

    [Range(0f, 1f)]
    [Tooltip("謝謝語音音量。這版預設 1，會比上一版明顯大聲。")]
    public float receiverThanksVoiceVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("0=完全2D，1=完全3D。預設0.35，仍有方向感但不會因距離衰減得太小聲。")]
    public float receiverThanksSpatialBlend = 0.35f;

    [Tooltip("3D 語音在這個距離內維持最大音量。")]
    public float receiverThanksMinDistance = 4f;

    [Tooltip("3D 語音最大可聽距離。")]
    public float receiverThanksMaxDistance = 28f;

    [Tooltip("手動指定女性 NPC。拖進來後一定使用 Female Receiver Thanks Voice。")]
    public Transform[] femaleThankYouTargets;

    [Tooltip("手動指定男性 NPC。拖進來後一定使用 Male Receiver Thanks Voice。")]
    public Transform[] maleThankYouTargets;

    [Tooltip("沒有手動指定性別時，依 NPC 名稱自動判斷『女、新娘、female、woman、girl』等關鍵字。")]
    public bool autoDetectReceiverGenderByName = true;

    [Header("Story Music Transition")]
    public bool useStoryMusicCrossfade = true;
    public float storyMusicCrossfadeSeconds = 1.6f;
    [Range(0f, 1f)] public float tensionMusicTargetVolume = 0.12f;

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

    [Header("Auto Cinematic Story - Vindictus Style")]
    [Tooltip("婚禮任務全部完成後，自動進入劇情過場，不需要玩家再按任何按鍵。")]
    public bool autoPlayCinematicStoryAfterWeddingTasks = true;

    [Tooltip("劇情過場時上下加入黑邊，畫面更像遊戲 Cutscene。")]
    public bool showCinematicLetterbox = true;

    [Range(0.06f, 0.20f)]
    public float cinematicLetterboxHeightRatio = 0.11f;

    [Tooltip("任務完成後，正式切入劇情前停一下。")]
    public float cinematicPreRollSeconds = 0.65f;

    [Tooltip("婚禮聲音突然停掉後保留短暫安靜。")]
    public float cinematicSilenceBeatSeconds = 0.55f;

    [Tooltip("鏡頭切到角色後多停一下。")]
    public float cinematicExtraShotHoldSeconds = 0.18f;

    [Tooltip("劇情時隱藏任務、倒數與互動提示，只留下字幕。")]
    public bool hideGameplayHudDuringCinematic = true;

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
    [Tooltip("開啟後，送酒/送食物必須由玩家自己走到目標旁邊互動，不會再由系統自動帶路。")]
    public bool manualWalkForItemTasks = true;

    [Header("Receiver Dialogue Camera")]
    public bool focusCameraOnReceiverDialogue = true;
    public float receiverDialoguePanSeconds = 0.38f;
    public float receiverDialogueHoldSeconds = 3f;
    public float receiverDialogueReturnSeconds = 0.3f;
    public float receiverFallbackFaceHeight = 1.55f;
    public float receiverDialogueCameraDistance = 2.4f;
    [Range(0.2f, 0.6f)]
    public float receiverDialogueUpperBodyBottomRatio = 0.4f;
    public float receiverDialogueFramePadding = 1.3f;
    public float receiverDialogueMaxCameraDistance = 8f;

    [Header("Hide Player Hands During Receiver Dialogue")]
    [Tooltip("NPC 收到酒 / 食物後進入對話鏡頭時，暫時隱藏玩家手模型，避免手跑到 NPC 頭上。")]
    public bool hidePlayerHandsDuringReceiverDialogue = true;

    [Tooltip("玩家手模型 Root。留空會自動尋找名稱 Hands_Rigged。")]
    public Transform playerHandsRoot;

    [Tooltip("自動尋找玩家手模型時使用的物件名稱。")]
    public string playerHandsObjectName = "Hands_Rigged";

    [Tooltip("只關閉 Renderer，不停用 Hands_Rigged GameObject，避免影響 XR / 手部腳本。")]
    public bool hidePlayerHandsByRendererOnly = true;

    [Header("Receiver Thank You Motion")]
    [Tooltip("NPC 收到酒或食物後，自動做感謝動作。")]
    public bool playReceiverThankYouMotion = true;

    [Tooltip("如果 Animator 裡有 Thank / Thanks / ThankYou / Bow / Wave / Talk，優先使用現成動畫。")]
    public bool preferAnimatorThankYouMotion = true;

    [Tooltip("感謝動作長度。")]
    public float receiverThankYouMotionSeconds = 1.05f;

    [Tooltip("沒有現成動畫時的點頭角度。")]
    public float receiverThankYouNodDegrees = 11f;

    [Tooltip("沒有現成動畫時，右手會微微抬起，像收到東西後致意。")]
    public float receiverThankYouHandLiftDegrees = 18f;

    [Range(1, 2)]
    public int receiverThankYouNodCount = 1;

    [Tooltip("感謝動畫完成後回到哪個 Animator State。")]
    public string receiverThankYouReturnStateName = "Idle";

    [Header("Food Receiver Celebration Motion")]
    [Tooltip("送食物時，NPC 會優先做比較開心、像慶祝的動作；沒有的話才回退到謝謝動作。")]
    public bool foodReceiverPreferCelebrateMotion = true;

    [Tooltip("診斷保底：送食物時無視 Inspector 舊值，強制播放可見慶祝動作。先保持勾選。")]
    public bool forceFoodReceiverCelebration = true;

    [Tooltip("診斷時在 Console 印出實際收到食物的 NPC 名稱與動作 Root。")]
    public bool debugFoodReceiverCelebration = true;

    [Tooltip("送食物時的動作長度。")]
    public float foodReceiverCelebrateMotionSeconds = 1.25f;

    [Tooltip("如果 Animator 有 Celebrate / Cheer / Happy / Clap / Dance，就優先播。")]
    public bool preferAnimatorCelebrateMotionForFood = true;

    [Tooltip("沒有現成動畫時，送食物時雙手抬起的角度。")]
    public float foodReceiverCelebrateHandLiftDegrees = 28f;

    [Tooltip("沒有現成動畫時，送食物時身體起伏高度。這版預設更明顯。")]
    public float foodReceiverCelebrateBounceHeight = 0.08f;

    [Tooltip("沒有現成動畫時，整個角色左右小幅擺動角度。")]
    public float foodReceiverCelebrateBodySwayDegrees = 7f;

    [Tooltip("沒有現成動畫時，角色會短暫放大一點，讓慶祝動作一定看得見。")]
    public float foodReceiverCelebrateScalePulse = 0.035f;

    [Range(1, 3)]
    [Tooltip("送食物時點頭 / 開心節奏次數。")]
    public int foodReceiverCelebrateNodCount = 2;

    [Header("Receiver Face Player On Delivery")]
    [Tooltip("玩家交付酒或食物時，NPC 會先自然轉身面向玩家。")]
    public bool faceReceiverTowardPlayerOnDelivery = true;

    [Tooltip("NPC 轉向玩家需要的時間。")]
    public float receiverTurnToPlayerSeconds = 0.32f;

    [Tooltip("交付後維持面向玩家多久，會涵蓋謝謝聲音、慶祝動作與對話。")]
    public float receiverFacePlayerHoldSeconds = 4.2f;

    [Tooltip("互動對話鏡頭期間不要再把 NPC 轉去其他方向。")]
    public bool keepReceiverFacingPlayerDuringDialogue = true;

    [Tooltip("動作結束後是否仍保持面向玩家。")]
    public bool keepReceiverFacingPlayerAfterDelivery = true;

    [Tooltip("只有模型本身正面軸仍然相反時才調整。正常保持 0。")]
    public float receiverVisualForwardYawOffset = 0f;

    [Tooltip("優先用左右肩膀位置判斷 Humanoid 的真正正面，比模型 Root.forward 更穩。")]
    public bool autoDetectReceiverForwardFromShoulders = true;

    [Tooltip("少數模型如果正面軸剛好相反，把那些 NPC 拖進來，會自動再補 180 度。")]
    public Transform[] receiverReverseFacingTargets;

    [Header("Natural Receiver Gesture")]
    [Tooltip("不要再用跳躍/縮放假裝慶祝，改成上半身、頭部與手臂的自然動作。")]
    public bool useNaturalReceiverGesture = true;

    [Tooltip("如果 Animator 有真正的慶祝 State，可在這裡填名稱；留空就自動找 Celebrate/Cheer/Happy/Clap。")]
    public string foodReceiverCustomAnimationState = "";

    [Header("Receiver NPC Animation By Gender")]
    [Tooltip("勾選後，男 NPC 與女 NPC 會播放不同 Animator State。")]
    public bool useGenderSpecificReceiverAnimation = true;

    [Tooltip("男 NPC 收到酒 / 食物後播放的 Animator State 名稱。")]
    public string maleReceiverCustomAnimationState = "男生動作";

    [Tooltip("女 NPC 收到酒 / 食物後播放的 Animator State 名稱。")]
    public string femaleReceiverCustomAnimationState = "女生動作";

    [Tooltip("如果關閉男女分開，所有收到酒/食物的 NPC 使用同一個 Animator State。")]
    public bool useSameReceiverAnimationForAllNPCs = false;

    [Tooltip("男女不分開時共用的 State 名稱。")]
    public string allReceiverCustomAnimationState = "打招呼";

    [Tooltip("如果男女不分開且不共用，送酒 NPC 使用這個 State。")]
    public string wineReceiverCustomAnimationState = "";

    [Tooltip("如果男女不分開且不共用，送食物 NPC 使用 Food Receiver Custom Animation State。")]
    public bool useFoodReceiverCustomAnimationStateWhenSeparated = true;

    [Tooltip("Animator 動作播放多久後回 Idle。")]
    public float receiverCustomAnimationSeconds = 1.8f;

    [Tooltip("找不到指定 State 時，自動改用自然上半身感謝動作，不會完全沒反應。")]
    public bool fallbackToNaturalReceiverGesture = true;

    [Tooltip("自然感謝動作長度。")]
    public float naturalReceiverGestureSeconds = 1.7f;

    [Tooltip("收到食物時輕微鞠躬/前傾角度。")]
    public float naturalReceiverChestBowDegrees = 6f;

    [Tooltip("收到食物時點頭角度。")]
    public float naturalReceiverHeadNodDegrees = 10f;

    [Tooltip("收到食物時雙手往前抬的角度。")]
    public float naturalReceiverArmForwardDegrees = 18f;

    [Tooltip("收到食物時手肘再微微彎起。")]
    public float naturalReceiverForearmDegrees = 14f;

    [Header("Physical Wedding Delivery - Recommended")]
    [Tooltip("最佳版：玩家先走到酒/食物旁拿取，再自己走到 NPC 旁交付。完全不移動 XR Origin。")]
    public bool usePhysicalWeddingDelivery = true;
    public KeyCode physicalInteractKey = KeyCode.E;
    public KeyCode physicalInteractGamepadKey = KeyCode.JoystickButton0;
    public KeyCode physicalCancelKey = KeyCode.Q;
    public float physicalPickupRange = 3.2f;
    public float physicalDeliveryRange = 2.25f;
    public float physicalGiveSeconds = 0.45f;
    public Transform winePickupPoint;
    public Transform foodPickupPoint;
    public Transform carryHoldPoint;
    public GameObject foodCarryTemplate;
    public string foodPickupObjectName = "烤魚";
    public Transform[] wineDeliveryTargets;
    public Transform[] foodDeliveryTargets;
    public Vector3 carryHoldLocalEuler = Vector3.zero;

    [Header("Delivery Target Guidance - Very Obvious")]
    public bool showDeliveryTargetMarker = true;
    [Tooltip("可選：自訂箭頭/驚嘆號 Prefab。留 None 會自動生成黃色旋轉菱形。")]
    public GameObject deliveryTargetMarkerPrefab;
    public float deliveryTargetMarkerHeight = 2.35f;
    public float deliveryTargetMarkerScale = 0.42f;
    public float deliveryTargetMarkerBobHeight = 0.16f;
    public float deliveryTargetMarkerSpinSpeed = 110f;
    public bool pulseDeliveryTargetMarker = true;
    public Color deliveryTargetMarkerColor = new Color(1f, 0.78f, 0.05f, 1f);
    public bool showDeliveryTargetNameAndDistance = true;

    [Header("Delivery Ground Arrow Path")]
    public bool showDeliveryGroundArrows;
    public int deliveryGroundArrowMaxCount = 12;
    public float deliveryGroundArrowSpacing = 2.2f;
    public float deliveryGroundArrowStartDistance = 1.2f;
    public float deliveryGroundArrowEndDistance = 1.1f;
    public float deliveryGroundArrowHeight = 0.08f;
    public float deliveryGroundArrowSize = 1.05f;
    public float deliveryGroundArrowRefreshSeconds = 0.12f;
    public float deliveryGroundArrowPulseAmount = 0.1f;
    public float deliveryGroundArrowPulseSpeed = 4.5f;
    public Color deliveryGroundArrowColor = new Color(1f, 0.82f, 0.02f, 1f);

    [Header("Delivery NPC Stationary")]
    [Tooltip("勾選後，Wine/Food Delivery Targets 裡的 NPC 不會再繞圈，會固定站著等玩家。")]
    public bool keepDeliveryTargetsStationary = true;
    [Tooltip("如果 NPC 是 DanceCirclePivot 的子物件，會把該角色 Root 從旋轉 Pivot 底下移出，保留原本世界位置。")]
    public bool detachDeliveryTargetsFromDancePivot = true;
    public string deliveryTargetIdleStateName = "Idle";

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

    [Header("New Police Scene Food Carry Presentation")]
    public float newPoliceSceneFoodViewDistance = 1.65f;
    public float newPoliceSceneFoodViewRightOffset = 0.3f;
    public float newPoliceSceneFoodViewDownOffset = 0.13f;
    public float newPoliceSceneFoodTargetSize = 0.72f;

    [Header("New Police Scene Pickup Location Guidance")]
    public bool showPickupLocationGuidance = true;
    public float pickupLocationRefreshSeconds = 0.15f;
    public float pickupLocationMarkerHeight = 1.65f;
    public float pickupLocationMarkerBobHeight = 0.12f;
    public float pickupLocationMarkerSpinSpeed = 80f;
    public Color winePickupLocationColor = new Color(1f, 0.28f, 0.06f, 1f);
    public Color foodPickupLocationColor = new Color(0.72f, 1f, 0.12f, 1f);

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

    [Header("Police Rigged Run")]
    public bool animatePoliceRunBones = true;
    public float newPoliceSceneRunDuration = 2.8f;
    public float policeRunCadence = 2.45f;
    [Range(0f, 1f)] public float policeRunLegSwing = 0.62f;
    [Range(0f, 1f)] public float policeRunKneeBend = 0.55f;
    [Range(0f, 1f)] public float policeRunArmSwing = 0.58f;
    public float policeRunForwardLeanDegrees = 9f;

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

    [Header("Police Cinematic Camera")]
    public bool usePoliceCinematicCamera = true;
    public float policeCameraPanSeconds = 0.85f;
    public float policeCameraHoldSeconds = 0.2f;
    public float policeCameraLookHeight = 1.35f;

    [Header("Police Entrance Tracking Shot")]
    [Tooltip("警察進場時，先用天空俯視鏡頭慢慢看著他們走進來。")]
    public bool usePoliceEntranceAerialIntro = true;

    [Tooltip("如果關掉天空鏡頭，才改用原本的側後方跟拍。")]
    public bool usePoliceEntranceTrackingCamera = false;

    [Tooltip("天空鏡頭高度。")]
    public float policeAerialHeight = 9.5f;

    [Tooltip("天空鏡頭略偏向山路入口的距離。")]
    public float policeAerialBackOffset = 4.2f;

    [Tooltip("天空鏡頭略偏側邊，畫面不會太正。")]
    public float policeAerialSideOffset = 1.8f;

    [Tooltip("天空鏡頭看向警察隊伍時的仰角修正。數值越大越往下看。")]
    public float policeAerialLookHeight = 1.2f;

    [Tooltip("切到天空鏡頭的速度。")]
    public float policeAerialBlendSeconds = 0.9f;

    [Tooltip("天空鏡頭會很慢地往前跟，數值越大跟得越明顯。")]
    [Range(0f, 1f)] public float policeAerialFollowStrength = 0.38f;

    [Tooltip("警察走近時，天空鏡頭會慢慢往下降並推近。")]
    public bool usePoliceAerialDescent = true;

    [Tooltip("天空鏡頭最後下降到的高度。")]
    public float policeAerialEndHeight = 4.8f;

    [Tooltip("天空鏡頭最後推近到警察後方的距離。")]
    public float policeAerialEndBackOffset = 2.6f;

    [Range(0f, 0.9f)]
    [Tooltip("警察走到整段路徑的幾成後，天空鏡頭開始明顯下降。")]
    public float policeAerialDescentStartNormalized = 0.28f;

    [Header("Police Front Reveal")]
    [Tooltip("警察走到會場後，鏡頭切到正面近景讓第一句台詞更有壓迫感。")]
    public bool usePoliceFrontReveal = true;

    public float policeFrontRevealDistance = 2.15f;
    public float policeFrontRevealHeight = 1.55f;
    public float policeFrontRevealSideOffset = 0.35f;
    public float policeFrontRevealMoveSeconds = 0.72f;
    public float policeFrontRevealHoldSeconds = 0.22f;
    public float policeFrontRevealReturnSeconds = 0.42f;

    [Tooltip("跟拍鏡頭在警察側邊的距離。")]
    public float policeTrackingSideOffset = 3.6f;

    [Tooltip("跟拍鏡頭落後警察的距離。")]
    public float policeTrackingBackOffset = 4.6f;

    [Tooltip("跟拍鏡頭高度。")]
    public float policeTrackingHeight = 1.75f;

    [Tooltip("跟拍開始時，從目前鏡頭滑到跟拍位置的速度。")]
    public float policeTrackingBlendSeconds = 0.45f;

    [Tooltip("警察進場結束後，鏡頭滑回玩家原本的劇情觀看位置。")]
    public bool returnCameraAfterPoliceEntrance = true;

    public float policeTrackingReturnSeconds = 0.7f;

    [Header("Police Natural Walk")]
    [Tooltip("優先使用角色原本 Animator 裡的 Walk 動畫，讓警察走步更自然。")]
    public bool preferAnimatorWalkForPoliceEntrance = true;

    [Tooltip("如果沒有真正的 Walk 動畫，才用程序骨架補步態。")]
    public bool useProceduralPoliceWalkFallback = true;

    [Tooltip("兩名警察不要完全同步，第二位會自然慢一點。")]
    public float policeSecondarySpeedScale = 0.94f;

    [Tooltip("走路時左右微幅開闔，畫面不會像兩個貼著滑行。")]
    public float policeNaturalLateralDrift = 0.08f;

    [Tooltip("走步節奏。若沒有真正 Walk 動畫，程序步態會依這個節奏走。")]
    public float policeWalkCadence = 1.75f;

    [Range(0f, 1f)] public float policeWalkLegSwing = 0.33f;
    [Range(0f, 1f)] public float policeWalkKneeBend = 0.26f;
    [Range(0f, 1f)] public float policeWalkArmSwing = 0.28f;
    public float policeWalkForwardLeanDegrees = 3.5f;

    [Range(0f, 1f)]
    [Tooltip("第二名警察的走路動畫從不同時間點開始，避免兩人同手同腳。")]
    public float policeSecondWalkAnimationPhase = 0.43f;

    [Tooltip("第一名警察 Walk 動畫速度。")]
    public float policeFirstWalkAnimatorSpeed = 1f;

    [Tooltip("第二名警察 Walk 動畫稍慢一點。")]
    public float policeSecondWalkAnimatorSpeed = 0.96f;

    [Header("Ceremony Cup Close Up")]
    [Tooltip("日警掃落酒杯時切到酒杯近距離特寫。")]
    public bool useCeremonyCupCloseUp = true;

    [Tooltip("酒杯特寫鏡頭與酒杯的距離。")]
    public float cupCloseUpDistance = 0.72f;

    [Tooltip("酒杯特寫鏡頭略高於酒杯的高度。")]
    public float cupCloseUpHeight = 0.24f;

    [Tooltip("切進酒杯特寫需要的時間。")]
    public float cupCloseUpMoveSeconds = 0.38f;

    [Tooltip("酒杯被打掉前先停一拍。")]
    public float cupCloseUpBeforeImpactSeconds = 0.18f;

    [Tooltip("酒杯被打掉後特寫停留時間。")]
    public float cupCloseUpAfterImpactSeconds = 0.85f;

    [Tooltip("特寫結束後回到原鏡頭的時間。")]
    public float cupCloseUpReturnSeconds = 0.42f;

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

    [Header("Police Dialogue Voice")]
    public AudioSource policeDialogueAudio;
    [Range(0f, 1f)] public float policeDialogueVolume = 1f;
    public AudioClip policeInsultVoice;
    public AudioClip groomReplyVoice;
    [Range(0f, 2f)] public float groomReplyVolumeScale = 1.4f;
    public AudioClip femaleResistVoice;
    public AudioClip playerInterveneVoice;
    public AudioClip policeWatchCommandVoice;

    [Header("Result For Later Chapters")]
    public bool saveResultToPlayerPrefs = true;
    public string conflictChoicePrefsKey = "Chapter1_ConflictChoice";
    public string peopleInjuredPrefsKey = "Chapter1_PeopleInjured";
    public string moralePrefsKey = "Chapter1_Morale";

    private enum WeddingCarryItem
    {
        None,
        Wine,
        Food
    }

    private WeddingCarryItem carriedWeddingItem = WeddingCarryItem.None;
    private GameObject physicalCarriedProp;
    private bool physicalDeliveryAnimating;
    private bool physicalDeliveryInputConsumed;

    private GameObject activeDeliveryTargetMarker;
    private Transform activeDeliveryTarget;
    private Vector3 deliveryMarkerBaseScale = Vector3.one;
    private readonly List<GameObject> activeDeliveryGroundArrows = new List<GameObject>();
    private float nextDeliveryGroundArrowRefreshTime;
    private GameObject winePickupLocationMarker;
    private GameObject foodPickupLocationMarker;
    private Transform guidedWinePickupSource;
    private Transform guidedFoodPickupSource;
    private float nextPickupLocationRefreshTime;

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
    private readonly List<Chapter1NpcGrounding> weddingNpcGrounders = new List<Chapter1NpcGrounding>();
    private bool weddingCircleConfigured;
    private float weddingPlayerGapAngleRadians;

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
    private bool cinematicStoryPlaying;

    private GUIStyle hudBoxStyle;
    private GUIStyle hudTitleStyle;
    private GUIStyle hudBodyStyle;
    private GUIStyle hudButtonStyle;
    private GUIStyle timerBoxStyle;
    private GUIStyle timerTitleStyle;
    private GUIStyle timerNumberStyle;
    private GUIStyle centerMenuStyle;
    private GUIStyle winePickupIndicatorStyle;
    private GUIStyle foodPickupIndicatorStyle;
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
        EnsureWeddingVocalsReference();
        ApplyWeddingMusicVolume();
        ApplyWeddingVocalsVolume();

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
        PrepareDeliveryTaskNPCs();
        EnsureNewPoliceSceneNpcGrounding();
        StartCoroutine(RefreshWeddingDancerSizingAfterAnimatorUpdate());

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

        physicalDeliveryInputConsumed = false;
        UpdatePhysicalWeddingDelivery();
        UpdateDeliveryTargetGuidance();
        UpdatePickupLocationGuidance();
        UpdateCenterInteractionInput();
        UpdateExplorationTimer();
        UpdateTemporaryMissionMovementClear();
        UpdateWorldInteractionPrompt();
    }

    private void LateUpdate()
    {
        // 婚禮階段固定背景音量，避免序列化舊值或其他腳本把音量改回去。
        if (!policeSequenceStarted)
        {
            if (lockWeddingMusicVolumeUntilPolice)
            {
                ApplyWeddingMusicVolume();
            }

            if (lockWeddingVocalsVolumeUntilPolice)
            {
                ApplyWeddingVocalsVolume();
            }
        }
    }

    private void EnsureWeddingVocalsReference()
    {
        if (weddingVocals != null)
        {
            return;
        }

        GameObject vocalsObject = GameObject.Find("WeddingVocals");
        if (vocalsObject != null)
        {
            weddingVocals = vocalsObject.GetComponent<AudioSource>();
        }
    }

    private void ApplyWeddingMusicVolume()
    {
        if (weddingAmbience == null)
        {
            return;
        }

        weddingAmbience.volume = Mathf.Clamp01(weddingMusicVolume);
    }

    private void ApplyWeddingVocalsVolume()
    {
        if (weddingVocals == null)
        {
            return;
        }

        weddingVocals.volume = Mathf.Clamp01(weddingVocalsVolume);
        weddingVocals.loop = true;
        weddingVocals.spatialBlend = 0f;
    }

    private void OnGUI()
    {
        if (!showFallbackHud)
        {
            return;
        }

        EnsureHudStyles();

        // 開場期間只顯示字幕，不顯示任務 HUD。
        if (openingStoryPlaying)
        {
            if (dialogueUI == null)
            {
                DrawFallbackDialogue();
            }

            return;
        }

        // 自動劇情過場：隱藏一般遊戲 HUD，只留黑邊與字幕。
        if (cinematicStoryPlaying && hideGameplayHudDuringCinematic)
        {
            DrawCinematicLetterbox();

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

        DrawPickupLocationIndicators();

        // 左上只放簡潔任務進度，不再塞長句。
        string displayMission = GetCleanMissionDisplayText();
        if (!string.IsNullOrEmpty(displayMission))
        {
            float missionWidth = Mathf.Min(Screen.width - 40f, 430f);
            float missionHeight = IsFreeExplorationActive() ? 78f : 92f;

            Rect missionBox = new Rect(20f, 20f, missionWidth, missionHeight);
            GUI.Box(missionBox, GUIContent.none, hudBoxStyle);

            string title = IsFreeExplorationActive() ? "婚禮任務" : "目前目標";

            GUI.Label(
                new Rect(
                    missionBox.x + 18f,
                    missionBox.y + 10f,
                    missionBox.width - 36f,
                    24f),
                title,
                hudTitleStyle);

            GUI.Label(
                new Rect(
                    missionBox.x + 18f,
                    missionBox.y + 35f,
                    missionBox.width - 36f,
                    missionBox.height - 40f),
                displayMission,
                hudBodyStyle);
        }

        // 互動提示只在底部中央顯示一個小框。
        if (ShouldShowCenterInteractionMenu())
        {
            DrawCenterInteractionMenu();
        }

        // 有正式 DialogueUI 時不重複畫 fallback 字幕。
        if (dialogueUI == null)
        {
            DrawFallbackDialogue();
        }

        if (waitingForChoice && choiceUI == null)
        {
            float width = Mathf.Min(Screen.width - 40f, 620f);
            float height = 178f;
            Rect box = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.Box(box, GUIContent.none, hudBoxStyle);

            GUI.Label(
                new Rect(box.x + 24f, box.y + 18f, box.width - 48f, 36f),
                choiceQuestion,
                hudTitleStyle);

            Rect optionA =
                new Rect(box.x + 24f, box.y + 68f, box.width - 48f, 40f);

            Rect optionB =
                new Rect(box.x + 24f, box.y + 116f, box.width - 48f, 40f);

            if (GUI.Button(optionA, "1　" + optionALabel, hudButtonStyle))
            {
                ChooseIntervene();
            }

            if (GUI.Button(optionB, "2　" + optionBLabel, hudButtonStyle))
            {
                ChooseWatch();
            }
        }
    }

    private void DrawCinematicLetterbox()
    {
        if (!showCinematicLetterbox)
        {
            return;
        }

        float barHeight =
            Screen.height
            * Mathf.Clamp(
                cinematicLetterboxHeightRatio,
                0.04f,
                0.25f);

        Color previousColor = GUI.color;
        GUI.color = Color.black;

        GUI.DrawTexture(
            new Rect(0f, 0f, Screen.width, barHeight),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(
                0f,
                Screen.height - barHeight,
                Screen.width,
                barHeight),
            Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    private string GetCleanMissionDisplayText()
    {
        if (!IsFreeExplorationActive())
        {
            return missionText;
        }

        int wineTarget = Mathf.Max(1, wineTargetCount);
        int foodTarget = Mathf.Max(1, foodTargetCount);

        string wine =
            deliveredWineCount >= wineTarget
                ? "送酒 " + wineTarget + "/" + wineTarget + " ✓"
                : "送酒 " + deliveredWineCount + "/" + wineTarget;

        string food =
            sharedFoodCount >= foodTarget
                ? "食物 " + foodTarget + "/" + foodTarget + " ✓"
                : "食物 " + sharedFoodCount + "/" + foodTarget;

        string dance =
            danceFinished
                ? "舞蹈 ✓"
                : (AreWineAndFoodTasksComplete() ? "舞蹈 可進行" : "舞蹈 未開放");

        return wine + "　｜　" + food + "　｜　" + dance;
    }

    private void DrawFallbackDialogue()
    {
        if (Time.time >= fallbackLineUntil || (string.IsNullOrEmpty(fallbackSpeaker) && string.IsNullOrEmpty(fallbackLine)))
        {
            return;
        }

        float width = Mathf.Min(Screen.width - 40f, 720f);
        float height = 96f;
        Rect box = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - height - 24f,
            width,
            height);

        GUI.Box(box, GUIContent.none, hudBoxStyle);

        GUI.Label(
            new Rect(box.x + 20f, box.y + 12f, box.width - 40f, 24f),
            fallbackSpeaker,
            hudTitleStyle);

        GUI.Label(
            new Rect(box.x + 20f, box.y + 39f, box.width - 40f, 46f),
            fallbackLine,
            hudBodyStyle);
    }

    public void BeginChapter()
    {
        if (storyStarted)
        {
            return;
        }

        storyStarted = true;
        openingStoryPlaying = true;
        cinematicStoryPlaying = false;

        if (physicalCarriedProp != null)
        {
            Destroy(physicalCarriedProp);
            physicalCarriedProp = null;
        }
        carriedWeddingItem = WeddingCarryItem.None;
        physicalDeliveryAnimating = false;
        physicalDeliveryInputConsumed = false;
        ClearDeliveryTargetMarker();
        SetWeddingNpcGrounding(true);

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
            ApplyWeddingMusicVolume();
            weddingAmbience.loop = true;
            weddingAmbience.Play();
        }

        EnsureWeddingVocalsReference();
        if (weddingVocals != null)
        {
            ApplyWeddingVocalsVolume();
            weddingVocals.playOnAwake = false;

            if (!weddingVocals.isPlaying)
            {
                weddingVocals.Play();
            }
        }

        openingStoryPlaying = false;

        // 到這裡才正式開始自由探索：先完成婚禮任務，再進入導火線事件。
        UnlockFreeExploration();
        SetPlayerControl(true);
        ShowLine(
            "新郎",
            "朋友，今晚人多。跟著橘紅色標記拿酒送給賓客，再跟著黃綠色標記把食物分給族人；忙完再到舞圈一起跳吧。",
            6f);
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

        if (usePhysicalWeddingDelivery)
        {
            DisableLegacyItemTaskInteractables();
        }

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
        float width = 168f;
        float height = 68f;
        float x = Screen.width - width - 20f;
        float y = 20f;

        Rect box = new Rect(x, y, width, height);
        GUI.Box(box, GUIContent.none, timerBoxStyle);

        GUI.Label(
            new Rect(x + 14f, y + 7f, width - 28f, 22f),
            explorationTimerTitle,
            timerTitleStyle);

        GUI.Label(
            new Rect(x + 14f, y + 27f, width - 28f, 32f),
            FormatTimer(explorationTimerRemaining),
            timerNumberStyle);
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

        // 實體送酒/送食物模式會先吃掉 E，避免交付同時誤觸舞蹈。
        if (usePhysicalWeddingDelivery && physicalDeliveryInputConsumed)
        {
            return;
        }

        if (IsCenterActionPressed(GetDanceInteractionKey(), centerDanceAltKey, centerDanceGamepadKey) && CanUseDanceInteraction())
        {
            JoinDance(null, playerRoot);
            return;
        }

        if (usePhysicalWeddingDelivery)
        {
            // 最佳版不再使用遠端 R/T 自動送達。
            if (IsCenterActionPressed(centerWineKey, centerWineAltKey, centerWineGamepadKey))
            {
                ShowLine("任務", "請先走到酒旁邊拿起酒，再自己走到賓客旁按 E 交付。", 2.6f);
            }
            else if (IsCenterActionPressed(centerFoodKey, centerFoodAltKey, centerFoodGamepadKey))
            {
                ShowLine("任務", "請先走到食物旁邊拿起食物，再自己走到族人旁按 E 交付。", 2.6f);
            }
            return;
        }

        if (IsCenterActionPressed(centerWineKey, centerWineAltKey, centerWineGamepadKey))
        {
            if (manualWalkForItemTasks)
            {
                ShowLine("任務", "請自己走到送酒目標旁邊，靠近後按 R / E 送酒。", 2.4f);
            }
            else
            {
                DeliverWine("賓客", FindNextInteractionTarget(true));
            }
        }
        else if (IsCenterActionPressed(centerFoodKey, centerFoodAltKey, centerFoodGamepadKey))
        {
            if (manualWalkForItemTasks)
            {
                ShowLine("任務", "請自己走到分享食物的族人旁邊，靠近後按 T / E 分享食物。", 2.4f);
            }
            else
            {
                ShareFood("族人", FindNextInteractionTarget(false));
            }
        }
    }

    private void UpdatePhysicalWeddingDelivery()
    {
        if (!usePhysicalWeddingDelivery || !IsFreeExplorationActive() || policeSequenceStarted || waitingForChoice)
        {
            return;
        }

        if (physicalCarriedProp != null && !physicalDeliveryAnimating)
        {
            PlacePhysicalCarriedProp();
        }

        if (physicalDeliveryAnimating || interactionAnimationRunning)
        {
            return;
        }

        if (IsKeyPressed(physicalCancelKey) && carriedWeddingItem != WeddingCarryItem.None)
        {
            CancelPhysicalCarry();
            physicalDeliveryInputConsumed = true;
            return;
        }

        KeyCode resolvedPhysicalInteractKey =
            physicalInteractKey == KeyCode.None ? KeyCode.E : physicalInteractKey;

        bool interactPressed =
            IsKeyPressed(resolvedPhysicalInteractKey)
            || IsKeyPressed(physicalInteractGamepadKey);

        if (!interactPressed)
        {
            return;
        }

        // 送酒與食物都完成後，E 必須留給舞蹈。
        // 不要讓實體拿取系統先把 E 吃掉，否則下面的 UpdateCenterInteractionInput()
        // 永遠收不到這次按鍵。
        if (carriedWeddingItem == WeddingCarryItem.None
            && AreWineAndFoodTasksComplete()
            && !danceFinished
            && !danceRoutineRunning)
        {
            return;
        }

        physicalDeliveryInputConsumed = true;

        if (carriedWeddingItem == WeddingCarryItem.None)
        {
            TryPickupPhysicalWeddingItem();
            return;
        }

        bool isWine = carriedWeddingItem == WeddingCarryItem.Wine;
        Transform receiver = GetDesignatedPhysicalDeliveryTarget(isWine);

        if (receiver == null)
        {
            ShowLine(
                "系統",
                isWine
                    ? "尚未設定下一位送酒 NPC。請設定 Wine Delivery Targets。"
                    : "尚未設定下一位分享食物 NPC。請設定 Food Delivery Targets。",
                2.8f);
            return;
        }

        float receiverDistance =
            GetFlatDistance(GetCurrentPlayerPosition(), receiver.position);

        if (receiverDistance > Mathf.Max(0.5f, physicalDeliveryRange))
        {
            ShowLine(
                "任務",
                "目標：" + receiver.name
                + "　距離約 " + receiverDistance.ToString("0.0") + "m"
                + "\n請走到頭上有黃色標記的 NPC 旁邊再按 E。",
                2.6f);
            return;
        }

        StartCoroutine(CompletePhysicalWeddingDelivery(receiver, isWine));
    }

    private void TryPickupPhysicalWeddingItem()
    {
        bool wineComplete = deliveredWineCount >= Mathf.Max(1, wineTargetCount);
        bool foodComplete = sharedFoodCount >= Mathf.Max(1, foodTargetCount);

        if (wineComplete && foodComplete)
        {
            ShowLine("任務", "送酒與分享食物都完成了，去舞圈加入舞蹈吧。", 2.3f);
            return;
        }

        Transform wineSource = ResolvePhysicalPickupPoint(true);
        Transform foodSource = ResolvePhysicalPickupPoint(false);

        Vector3 playerPosition = GetCurrentPlayerPosition();
        float wineDistance = wineSource != null
            ? GetPhysicalPickupDistance(wineSource, playerPosition)
            : float.MaxValue;
        float foodDistance = foodSource != null
            ? GetPhysicalPickupDistance(foodSource, playerPosition)
            : float.MaxValue;

        // 如果 Inspector 的 Food Pickup Point 指錯/太遠，
        // 但玩家眼前有「烤魚、魚、食物」等物件，優先使用最近的食物。
        if (!foodComplete)
        {
            Transform nearbyFood = FindBestScenePickupByKeywords(
                "烤魚", "魚", "食物", "烤肉", "food", "fish", "meat");

            if (nearbyFood != null)
            {
                float nearbyFoodDistance =
                    GetPhysicalPickupDistance(nearbyFood, playerPosition);

                if (foodSource == null
                    || nearbyFoodDistance + 0.35f < foodDistance)
                {
                    foodSource = nearbyFood;
                    foodPickupPoint = nearbyFood;
                    foodDistance = nearbyFoodDistance;
                }
            }
        }

        bool canPickupWine = !wineComplete && wineSource != null && wineDistance <= Mathf.Max(0.5f, physicalPickupRange);
        bool canPickupFood = !foodComplete && foodSource != null && foodDistance <= Mathf.Max(0.5f, physicalPickupRange);

        if (!canPickupWine && !canPickupFood)
        {
            if (!wineComplete && wineSource == null)
            {
                ShowLine("系統", "尚未指定 Wine Pickup Point。請在 Inspector 把酒瓶/酒甕位置拖進去。", 3f);
                return;
            }

            if (!foodComplete && foodSource == null)
            {
                ShowLine("系統", "尚未指定 Food Pickup Point。請在 Inspector 把食物位置拖進去。", 3f);
                return;
            }

            string distanceHint = "";
            if (!wineComplete && wineSource != null && wineDistance < float.MaxValue)
            {
                distanceHint = "（離酒約 " + wineDistance.ToString("0.0") + "m）";
            }
            else if (!foodComplete && foodSource != null && foodDistance < float.MaxValue)
            {
                distanceHint = "（離食物約 " + foodDistance.ToString("0.0") + "m）";
            }

            ShowLine(
                "任務",
                (!wineComplete && !foodComplete
                    ? "再靠近酒或食物一點，然後按 E 拿取。"
                    : (!wineComplete ? "再靠近酒一點，然後按 E 拿取。" : "再靠近食物一點，然後按 E 拿取。"))
                    + distanceHint,
                2.5f);
            return;
        }

        bool pickupWine = canPickupWine && (!canPickupFood || wineDistance <= foodDistance);
        PickupPhysicalWeddingItem(pickupWine);
    }

    private void PickupPhysicalWeddingItem(bool isWine)
    {
        if (physicalCarriedProp != null)
        {
            Destroy(physicalCarriedProp);
            physicalCarriedProp = null;
        }

        carriedWeddingItem = isWine ? WeddingCarryItem.Wine : WeddingCarryItem.Food;
        physicalCarriedProp = CreatePhysicalCarryProp(isWine);
        RefreshDeliveryTargetMarker();

        if (physicalCarriedProp == null)
        {
            carriedWeddingItem = WeddingCarryItem.None;
            ShowLine("系統", "拿取物件建立失敗。", 2f);
            return;
        }

        PlacePhysicalCarriedProp();

        ShowLine(
            "動作",
            isWine
                ? "你拿起一瓶酒。現在自己走到尚未收到酒的賓客旁邊。"
                : "你拿起食物。現在自己走到尚未收到食物的族人旁邊。",
            2.8f);

        SetTemporaryMission(
            isWine
                ? "手上：酒　走到賓客旁，靠近後按 E 交付。Q 可放回。"
                : "手上：食物　走到族人旁，靠近後按 E 交付。Q 可放回。",
            4f);
    }

    private GameObject CreatePhysicalCarryProp(bool isWine)
    {
        if (!isWine && foodCarryTemplate != null)
        {
            GameObject food = Instantiate(foodCarryTemplate);
            food.name = "Chapter1_Food_PhysicalCarry";
            food.SetActive(true);
            PrepareHeldProp(food);
            FitNewPoliceSceneHeldFood(food);
            return food;
        }

        GameObject prop = CreateHeldProp(isWine);
        if (prop != null)
        {
            prop.name = isWine ? "Chapter1_Wine_PhysicalCarry" : "Chapter1_Food_PhysicalCarry";
            PrepareHeldProp(prop);
        }
        return prop;
    }

    private void PlacePhysicalCarriedProp()
    {
        if (physicalCarriedProp == null)
        {
            return;
        }

        if (carryHoldPoint != null)
        {
            physicalCarriedProp.transform.position = carryHoldPoint.position;
            physicalCarriedProp.transform.rotation = carryHoldPoint.rotation * Quaternion.Euler(carryHoldLocalEuler);
            return;
        }

        // 沒指定手部 Hold Point 時，自動固定在玩家視野右下方，PC/VR 都能測。
        physicalCarriedProp.transform.position = GetCarriedPropPosition();

        Transform view = GetPlayerViewTransform();
        if (view == null)
        {
            return;
        }

        if (carriedWeddingItem == WeddingCarryItem.Wine)
        {
            Vector3 horizontalView = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (horizontalView.sqrMagnitude < 0.001f)
            {
                horizontalView = Vector3.forward;
            }

            Quaternion viewYaw = Quaternion.LookRotation(horizontalView.normalized, Vector3.up);
            physicalCarriedProp.transform.rotation =
                viewYaw * wineBottleUprightOffset * Quaternion.Euler(wineBottleHeldEulerOffset);
        }
        else
        {
            physicalCarriedProp.transform.rotation =
                Quaternion.LookRotation(view.forward, Vector3.up) * Quaternion.Euler(carryHoldLocalEuler);
        }
    }

    private IEnumerator CompletePhysicalWeddingDelivery(Transform receiver, bool isWine)
    {
        if (receiver == null || physicalCarriedProp == null)
        {
            yield break;
        }

        physicalDeliveryAnimating = true;

        // 一交付就讓真正收到物品的 NPC 自然轉過來面向玩家，
        // 並在謝謝 / 慶祝 / 對話期間持續保持這個方向。
        if (faceReceiverTowardPlayerOnDelivery)
        {
            StartCoroutine(
                HoldReceiverFacingPlayerRoutine(
                    receiver,
                    Mathf.Max(
                        receiverFacePlayerHoldSeconds,
                        receiverDialogueHoldSeconds + 1.1f)));
        }

        GameObject prop = physicalCarriedProp;
        Vector3 start = prop.transform.position;
        Vector3 end = receiver.position + Vector3.up * 1.05f;
        float duration = Mathf.Max(0.15f, physicalGiveSeconds);
        float elapsed = 0f;

        while (elapsed < duration && prop != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y += Mathf.Sin(t * Mathf.PI) * 0.08f;
            prop.transform.position = p;
            yield return null;
        }

        if (prop != null)
        {
            Destroy(prop);
        }

        physicalCarriedProp = null;
        carriedWeddingItem = WeddingCarryItem.None;
        ClearDeliveryTargetMarker();

        // 任務計數沿用原本已經測通的 DeliverWine / ShareFood，
        // 但暫時關掉舊動畫，避免又生成第二個酒瓶/食物。
        bool oldPlayInteractionAnimations = playInteractionAnimations;
        playInteractionAnimations = false;

        // 物理交付模式最保險的觸發點：
        // 這裡的 receiver 就是玩家眼前真正收到物品的 NPC。
        StartCoroutine(
            PlayReceiverUniversalAnimationRoutine(
                receiver,
                isWine));

        if (isWine)
        {
            DeliverWine(receiver.name, receiver);
        }
        else
        {
            ShareFood(receiver.name, receiver);
        }

        playInteractionAnimations = oldPlayInteractionAnimations;
        physicalDeliveryAnimating = false;
    }

    private void CancelPhysicalCarry()
    {
        if (physicalCarriedProp != null)
        {
            Destroy(physicalCarriedProp);
            physicalCarriedProp = null;
        }

        carriedWeddingItem = WeddingCarryItem.None;
        physicalDeliveryAnimating = false;
        ClearDeliveryTargetMarker();
        ShowLine("動作", "你先把手上的物品放回去了。", 1.8f);
        UpdateWeddingQuestMission();
    }

    private Transform ResolvePhysicalPickupPoint(bool isWine)
    {
        if (isWine)
        {
            // Wine Pickup Point 必須是「場景裡的物件」，不能是 Project 裡的 Prefab 資產。
            if (IsValidScenePickupTransform(winePickupPoint))
            {
                return winePickupPoint;
            }

            winePickupPoint = null;

            if (wineBottleTemplate != null
                && wineBottleTemplate.scene.IsValid()
                && wineBottleTemplate.activeInHierarchy)
            {
                winePickupPoint = wineBottleTemplate.transform;
                return winePickupPoint;
            }

            string requestedName =
                string.IsNullOrWhiteSpace(wineBottleObjectName)
                    ? "酒瓶"
                    : wineBottleObjectName.Trim();

            Transform found = FindBestScenePickupByKeywords(
                requestedName,
                "酒瓶",
                "酒",
                "wine",
                "bottle",
                "酒甕");

            if (found != null)
            {
                winePickupPoint = found;
                return winePickupPoint;
            }

            // 最後保底：如果酒就在婚禮火堆附近，但物件命名太特殊，
            // 仍允許玩家到舞圈/火堆附近按 E 測試流程。
            if (danceCenter != null)
            {
                return danceCenter;
            }

            return null;
        }

        if (IsValidScenePickupTransform(foodPickupPoint))
        {
            return foodPickupPoint;
        }

        foodPickupPoint = null;

        Transform foodFound = FindBestScenePickupByKeywords(
            string.IsNullOrWhiteSpace(foodPickupObjectName) ? "食物" : foodPickupObjectName.Trim(),
            "食物",
            "烤肉",
            "烤魚",
            "魚",
            "food",
            "meat",
            "fish",
            "pig");

        if (foodFound != null)
        {
            foodPickupPoint = foodFound;
            return foodPickupPoint;
        }

        // 食物沒指定時也以婚禮中心作為保底測試點。
        if (danceCenter != null)
        {
            return danceCenter;
        }

        return null;
    }

    private bool IsValidScenePickupTransform(Transform candidate)
    {
        return candidate != null
            && candidate.gameObject != null
            && candidate.gameObject.scene.IsValid()
            && candidate.gameObject.activeInHierarchy;
    }

    private Transform FindBestScenePickupByKeywords(params string[] keywords)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        Transform best = null;
        float bestDistance = float.MaxValue;
        Vector3 playerPosition = GetCurrentPlayerPosition();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (!IsValidScenePickupTransform(candidate))
            {
                continue;
            }

            string candidateName = candidate.name;
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                continue;
            }

            bool nameMatched = false;
            for (int k = 0; k < keywords.Length; k++)
            {
                string keyword = keywords[k];
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (candidateName.IndexOf(
                    keyword,
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nameMatched = true;
                    break;
                }
            }

            if (!nameMatched)
            {
                continue;
            }

            float distance =
                GetPhysicalPickupDistance(
                    candidate,
                    playerPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private float GetPhysicalPickupDistance(Transform source, Vector3 playerPosition)
    {
        if (source == null)
        {
            return float.MaxValue;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        float bestDistance = float.MaxValue;
        bool foundRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            foundRenderer = true;

            // 距離取「模型表面最近點」，所以就算 Pivot/Root 在很遠的位置，
            // 玩家站在烤魚旁邊仍然會被判定為靠近。
            Vector3 closestPoint = renderer.bounds.ClosestPoint(playerPosition);
            float distance = GetFlatDistance(playerPosition, closestPoint);

            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        if (foundRenderer)
        {
            return bestDistance;
        }

        return GetFlatDistance(playerPosition, source.position);
    }

    private Vector3 GetPhysicalPickupWorldPosition(Transform source)
    {
        if (source == null)
        {
            return GetCurrentPlayerPosition();
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(source.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? combinedBounds.center : source.position;
    }

    private Transform GetDesignatedPhysicalDeliveryTarget(bool isWine)
    {
        Transform[] targets = isWine ? wineDeliveryTargets : foodDeliveryTargets;
        HashSet<int> completedTargets = isWine ? deliveredWineTargets : sharedFoodTargets;

        if (targets == null || targets.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Transform candidate = targets[i];
            if (candidate == null)
            {
                continue;
            }

            if (completedTargets.Contains(candidate.GetInstanceID()))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void RefreshDeliveryTargetMarker()
    {
        bool showGroundArrows = ShouldShowDeliveryGroundArrows();
        if ((!showDeliveryTargetMarker && !showGroundArrows)
            || carriedWeddingItem == WeddingCarryItem.None)
        {
            ClearDeliveryTargetMarker();
            return;
        }

        Transform target = GetDesignatedPhysicalDeliveryTarget(
            carriedWeddingItem == WeddingCarryItem.Wine);

        if (target == null)
        {
            ClearDeliveryTargetMarker();
            return;
        }

        if (activeDeliveryTarget == target
            && (!showDeliveryTargetMarker || activeDeliveryTargetMarker != null))
        {
            RefreshDeliveryGroundArrows(true);
            return;
        }

        ClearDeliveryTargetMarker();
        activeDeliveryTarget = target;

        if (keepDeliveryTargetsStationary)
        {
            StopDeliveryTargetNPC(target);
        }

        if (showDeliveryTargetMarker && deliveryTargetMarkerPrefab != null)
        {
            activeDeliveryTargetMarker = Instantiate(deliveryTargetMarkerPrefab);
        }
        else if (showDeliveryTargetMarker)
        {
            activeDeliveryTargetMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Collider markerCollider = activeDeliveryTargetMarker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer markerRenderer = activeDeliveryTargetMarker.GetComponent<Renderer>();
            if (markerRenderer != null && markerRenderer.material != null)
            {
                Material material = markerRenderer.material;

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", deliveryTargetMarkerColor);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", deliveryTargetMarkerColor);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", deliveryTargetMarkerColor * 2.2f);
                }
            }

            activeDeliveryTargetMarker.transform.rotation =
                Quaternion.Euler(45f, 45f, 45f);
        }

        if (activeDeliveryTargetMarker != null)
        {
            activeDeliveryTargetMarker.name = "DeliveryTargetMarker_" + target.name;
            deliveryMarkerBaseScale =
                Vector3.one * Mathf.Max(0.08f, deliveryTargetMarkerScale);
            activeDeliveryTargetMarker.transform.localScale = deliveryMarkerBaseScale;
        }

        RefreshDeliveryGroundArrows(true);
    }

    private void UpdateDeliveryTargetGuidance()
    {
        bool showGroundArrows = ShouldShowDeliveryGroundArrows();
        if ((!showDeliveryTargetMarker && !showGroundArrows)
            || carriedWeddingItem == WeddingCarryItem.None
            || policeSequenceStarted
            || !IsFreeExplorationActive())
        {
            ClearDeliveryTargetMarker();
            return;
        }

        Transform expectedTarget =
            GetDesignatedPhysicalDeliveryTarget(
                carriedWeddingItem == WeddingCarryItem.Wine);

        if (expectedTarget == null)
        {
            ClearDeliveryTargetMarker();
            return;
        }

        if (activeDeliveryTarget != expectedTarget
            || (showDeliveryTargetMarker && activeDeliveryTargetMarker == null))
        {
            RefreshDeliveryTargetMarker();
        }

        if (activeDeliveryTarget == null)
        {
            return;
        }

        RefreshDeliveryGroundArrows(false);

        if (activeDeliveryTargetMarker != null)
        {
            float bob =
                Mathf.Sin(Time.time * 3.2f)
                * Mathf.Max(0f, deliveryTargetMarkerBobHeight);

            activeDeliveryTargetMarker.transform.position =
                activeDeliveryTarget.position
                + Vector3.up * (deliveryTargetMarkerHeight + bob);

            activeDeliveryTargetMarker.transform.Rotate(
                Vector3.up,
                deliveryTargetMarkerSpinSpeed * Time.deltaTime,
                Space.World);

            if (pulseDeliveryTargetMarker)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.16f;
                activeDeliveryTargetMarker.transform.localScale =
                    deliveryMarkerBaseScale * pulse;
            }
            else
            {
                activeDeliveryTargetMarker.transform.localScale =
                    deliveryMarkerBaseScale;
            }
        }
    }

    private bool ShouldShowDeliveryGroundArrows()
    {
        return showDeliveryGroundArrows || IsNewPoliceScene();
    }

    private void RefreshDeliveryGroundArrows(bool force)
    {
        if (!ShouldShowDeliveryGroundArrows()
            || activeDeliveryTarget == null
            || carriedWeddingItem == WeddingCarryItem.None)
        {
            ClearDeliveryGroundArrows();
            return;
        }

        if (!force && Time.unscaledTime < nextDeliveryGroundArrowRefreshTime)
        {
            return;
        }

        nextDeliveryGroundArrowRefreshTime =
            Time.unscaledTime + Mathf.Max(0.03f, deliveryGroundArrowRefreshSeconds);

        Vector3 playerPosition = GetCurrentPlayerPosition();
        Vector3 targetPosition = activeDeliveryTarget.position;
        Vector3 flatDirection = targetPosition - playerPosition;
        flatDirection.y = 0f;

        float distance = flatDirection.magnitude;
        float startDistance = Mathf.Max(0.25f, deliveryGroundArrowStartDistance);
        float endDistance = Mathf.Max(0.25f, deliveryGroundArrowEndDistance);
        float usableDistance = distance - startDistance - endDistance;

        if (flatDirection.sqrMagnitude < 0.01f || usableDistance <= 0f)
        {
            SetDeliveryGroundArrowCount(0);
            return;
        }

        flatDirection.Normalize();
        float spacing = Mathf.Max(0.6f, deliveryGroundArrowSpacing);
        int arrowCount = Mathf.Clamp(
            Mathf.FloorToInt(usableDistance / spacing) + 1,
            1,
            Mathf.Max(1, deliveryGroundArrowMaxCount));

        SetDeliveryGroundArrowCount(arrowCount);

        Quaternion arrowRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        float pulseAmount = Mathf.Clamp(deliveryGroundArrowPulseAmount, 0f, 0.35f);
        float baseSize = Mathf.Max(0.35f, deliveryGroundArrowSize);

        for (int i = 0; i < arrowCount; i++)
        {
            GameObject arrow = activeDeliveryGroundArrows[i];
            float pathDistance = arrowCount == 1
                ? startDistance + usableDistance * 0.5f
                : startDistance + usableDistance * i / (arrowCount - 1f);
            Vector3 arrowPosition = playerPosition + flatDirection * pathDistance;

            if (TryGetTerrainGroundY(arrowPosition, out float terrainGroundY))
            {
                arrowPosition.y = terrainGroundY + deliveryGroundArrowHeight;
            }
            else if (TryGetPhysicsGroundY(null, arrowPosition, out float physicsGroundY))
            {
                arrowPosition.y = physicsGroundY + deliveryGroundArrowHeight;
            }
            else
            {
                float pathProgress = Mathf.Clamp01(pathDistance / Mathf.Max(0.01f, distance));
                arrowPosition.y = Mathf.Lerp(playerPosition.y, targetPosition.y, pathProgress)
                    + deliveryGroundArrowHeight;
            }

            float phase = Time.unscaledTime * Mathf.Max(0f, deliveryGroundArrowPulseSpeed) - i * 0.42f;
            float pulse = 1f + Mathf.Sin(phase) * pulseAmount;
            arrow.transform.SetPositionAndRotation(arrowPosition, arrowRotation);
            arrow.transform.localScale = Vector3.one * (baseSize * pulse);
        }
    }

    private void SetDeliveryGroundArrowCount(int count)
    {
        int safeCount = Mathf.Max(0, count);

        while (activeDeliveryGroundArrows.Count < safeCount)
        {
            activeDeliveryGroundArrows.Add(CreateDeliveryGroundArrow(
                activeDeliveryGroundArrows.Count + 1));
        }

        for (int i = 0; i < activeDeliveryGroundArrows.Count; i++)
        {
            GameObject arrow = activeDeliveryGroundArrows[i];
            if (arrow != null)
            {
                arrow.SetActive(i < safeCount);
            }
        }
    }

    private GameObject CreateDeliveryGroundArrow(int index)
    {
        GameObject arrowRoot = new GameObject("DeliveryGroundArrow_" + index);
        arrowRoot.transform.SetParent(transform, true);

        CreateDeliveryGroundArrowPart(
            arrowRoot.transform,
            "Shaft",
            new Vector3(0f, 0f, -0.16f),
            new Vector3(0.22f, 0.045f, 0.76f),
            Quaternion.identity);
        CreateDeliveryGroundArrowPart(
            arrowRoot.transform,
            "HeadLeft",
            new Vector3(-0.15f, 0f, 0.31f),
            new Vector3(0.2f, 0.05f, 0.55f),
            Quaternion.Euler(0f, 45f, 0f));
        CreateDeliveryGroundArrowPart(
            arrowRoot.transform,
            "HeadRight",
            new Vector3(0.15f, 0f, 0.31f),
            new Vector3(0.2f, 0.05f, 0.55f),
            Quaternion.Euler(0f, -45f, 0f));

        return arrowRoot;
    }

    private void CreateDeliveryGroundArrowPart(
        Transform parent,
        string partName,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer == null || partRenderer.material == null)
        {
            return;
        }

        Material material = partRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", deliveryGroundArrowColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", deliveryGroundArrowColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", deliveryGroundArrowColor * 2.4f);
        }
    }

    private void ClearDeliveryGroundArrows()
    {
        for (int i = 0; i < activeDeliveryGroundArrows.Count; i++)
        {
            if (activeDeliveryGroundArrows[i] != null)
            {
                Destroy(activeDeliveryGroundArrows[i]);
            }
        }

        activeDeliveryGroundArrows.Clear();
        nextDeliveryGroundArrowRefreshTime = 0f;
    }

    private void ClearDeliveryTargetMarker()
    {
        if (activeDeliveryTargetMarker != null)
        {
            Destroy(activeDeliveryTargetMarker);
            activeDeliveryTargetMarker = null;
        }

        ClearDeliveryGroundArrows();
        activeDeliveryTarget = null;
    }

    private bool ShouldShowPickupLocationGuidance()
    {
        return showPickupLocationGuidance && IsNewPoliceScene();
    }

    private void UpdatePickupLocationGuidance()
    {
        bool shouldShow = ShouldShowPickupLocationGuidance()
            && IsFreeExplorationActive()
            && !policeSequenceStarted
            && !waitingForChoice
            && !physicalDeliveryAnimating
            && carriedWeddingItem == WeddingCarryItem.None;

        if (!shouldShow)
        {
            SetPickupLocationMarkersVisible(false, false);
            return;
        }

        bool wineComplete = deliveredWineCount >= Mathf.Max(1, wineTargetCount);
        bool foodComplete = sharedFoodCount >= Mathf.Max(1, foodTargetCount);

        if (Time.unscaledTime >= nextPickupLocationRefreshTime)
        {
            nextPickupLocationRefreshTime =
                Time.unscaledTime + Mathf.Max(0.05f, pickupLocationRefreshSeconds);

            guidedWinePickupSource = wineComplete
                ? null
                : ResolvePhysicalPickupPoint(true);
            guidedFoodPickupSource = foodComplete
                ? null
                : ResolvePhysicalPickupPoint(false);

            UpdatePickupLocationMarker(
                ref winePickupLocationMarker,
                guidedWinePickupSource,
                "WinePickupLocationMarker",
                winePickupLocationColor,
                !wineComplete);
            UpdatePickupLocationMarker(
                ref foodPickupLocationMarker,
                guidedFoodPickupSource,
                "FoodPickupLocationMarker",
                foodPickupLocationColor,
                !foodComplete);
        }

        AnimatePickupLocationMarker(winePickupLocationMarker, 0f);
        AnimatePickupLocationMarker(foodPickupLocationMarker, 1.7f);
    }

    private void UpdatePickupLocationMarker(
        ref GameObject marker,
        Transform source,
        string markerName,
        Color markerColor,
        bool taskIncomplete)
    {
        if (!taskIncomplete || source == null)
        {
            if (marker != null)
            {
                marker.SetActive(false);
            }
            return;
        }

        if (marker == null)
        {
            marker = CreatePickupLocationMarker(markerName, markerColor);
        }

        Vector3 markerPosition = GetPhysicalPickupWorldPosition(source);
        if (TryGetTerrainGroundY(markerPosition, out float terrainGroundY))
        {
            markerPosition.y = terrainGroundY + 0.07f;
        }
        else if (TryGetPhysicsGroundY(source, markerPosition, out float physicsGroundY))
        {
            markerPosition.y = physicsGroundY + 0.07f;
        }
        else
        {
            markerPosition.y = source.position.y + 0.07f;
        }

        marker.transform.position = markerPosition;
        marker.SetActive(true);
    }

    private GameObject CreatePickupLocationMarker(string markerName, Color markerColor)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(transform, true);

        CreatePickupLocationMarkerPart(
            marker.transform,
            "GroundDisc",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.025f, 0f),
            new Vector3(0.72f, 0.025f, 0.72f),
            Quaternion.identity,
            markerColor,
            1.8f);

        float beaconHeight = Mathf.Max(0.8f, pickupLocationMarkerHeight);
        float beamLength = Mathf.Max(0.45f, beaconHeight - 0.32f);
        CreatePickupLocationMarkerPart(
            marker.transform,
            "Beam",
            PrimitiveType.Cylinder,
            new Vector3(0f, beamLength * 0.5f, 0f),
            new Vector3(0.055f, beamLength * 0.5f, 0.055f),
            Quaternion.identity,
            markerColor,
            2.3f);

        CreatePickupLocationMarkerPart(
            marker.transform,
            "Beacon",
            PrimitiveType.Cube,
            new Vector3(0f, beaconHeight, 0f),
            Vector3.one * 0.34f,
            Quaternion.Euler(45f, 45f, 45f),
            markerColor,
            2.8f);

        return marker;
    }

    private void CreatePickupLocationMarkerPart(
        Transform parent,
        string partName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Color markerColor,
        float emissionMultiplier)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer == null || partRenderer.material == null)
        {
            return;
        }

        Material material = partRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", markerColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", markerColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", markerColor * emissionMultiplier);
        }
    }

    private void AnimatePickupLocationMarker(GameObject marker, float phaseOffset)
    {
        if (marker == null || !marker.activeSelf)
        {
            return;
        }

        Transform beacon = marker.transform.Find("Beacon");
        if (beacon == null)
        {
            return;
        }

        float phase = Time.unscaledTime * 3.5f + phaseOffset;
        float bob = Mathf.Sin(phase) * Mathf.Max(0f, pickupLocationMarkerBobHeight);
        float pulse = 1f + Mathf.Sin(phase * 1.35f) * 0.1f;
        beacon.localPosition =
            Vector3.up * (Mathf.Max(0.8f, pickupLocationMarkerHeight) + bob);
        beacon.localScale = Vector3.one * (0.34f * pulse);
        beacon.Rotate(
            Vector3.up,
            pickupLocationMarkerSpinSpeed * Time.unscaledDeltaTime,
            Space.World);
    }

    private void SetPickupLocationMarkersVisible(bool showWine, bool showFood)
    {
        if (winePickupLocationMarker != null)
        {
            winePickupLocationMarker.SetActive(showWine);
        }

        if (foodPickupLocationMarker != null)
        {
            foodPickupLocationMarker.SetActive(showFood);
        }
    }

    private void DrawPickupLocationIndicators()
    {
        if (!ShouldShowPickupLocationGuidance()
            || !IsFreeExplorationActive()
            || policeSequenceStarted
            || waitingForChoice
            || physicalDeliveryAnimating
            || carriedWeddingItem != WeddingCarryItem.None)
        {
            return;
        }

        Transform viewTransform = GetPlayerViewTransform();
        Camera viewCamera = viewTransform != null
            ? viewTransform.GetComponent<Camera>()
            : null;

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (viewCamera == null)
        {
            return;
        }

        Rect firstIndicator = default(Rect);
        bool drewFirstIndicator = false;

        if (winePickupLocationMarker != null
            && winePickupLocationMarker.activeSelf
            && guidedWinePickupSource != null)
        {
            float distance = GetFlatDistance(
                GetCurrentPlayerPosition(),
                GetPhysicalPickupWorldPosition(guidedWinePickupSource));
            firstIndicator = DrawPickupLocationIndicator(
                viewCamera,
                winePickupLocationMarker.transform.position
                    + Vector3.up * pickupLocationMarkerHeight,
                "酒",
                distance,
                winePickupIndicatorStyle,
                false,
                default(Rect));
            drewFirstIndicator = true;
        }

        if (foodPickupLocationMarker != null
            && foodPickupLocationMarker.activeSelf
            && guidedFoodPickupSource != null)
        {
            float distance = GetFlatDistance(
                GetCurrentPlayerPosition(),
                GetPhysicalPickupWorldPosition(guidedFoodPickupSource));
            DrawPickupLocationIndicator(
                viewCamera,
                foodPickupLocationMarker.transform.position
                    + Vector3.up * pickupLocationMarkerHeight,
                "食物",
                distance,
                foodPickupIndicatorStyle,
                drewFirstIndicator,
                firstIndicator);
        }
    }

    private Rect DrawPickupLocationIndicator(
        Camera viewCamera,
        Vector3 worldPosition,
        string itemName,
        float distance,
        GUIStyle indicatorStyle,
        bool hasAvoidRect,
        Rect avoidRect)
    {
        Vector3 screenPosition = viewCamera.WorldToScreenPoint(worldPosition);
        bool behindCamera = screenPosition.z <= 0f;

        if (behindCamera)
        {
            screenPosition.x = Screen.width - screenPosition.x;
            screenPosition.y = Screen.height - screenPosition.y;
        }

        float guiX = screenPosition.x;
        float guiY = Screen.height - screenPosition.y;
        float width = Mathf.Clamp(Screen.width * 0.12f, 112f, 148f);
        float height = 54f;
        float horizontalMargin = 14f;
        float topMargin = 108f;
        float bottomMargin = 72f;

        bool offScreen = behindCamera
            || guiX < horizontalMargin + width * 0.5f
            || guiX > Screen.width - horizontalMargin - width * 0.5f
            || guiY < topMargin + height * 0.5f
            || guiY > Screen.height - bottomMargin - height * 0.5f;

        Vector2 fromCenter = new Vector2(
            guiX - Screen.width * 0.5f,
            guiY - Screen.height * 0.5f);

        guiX = Mathf.Clamp(
            guiX,
            horizontalMargin + width * 0.5f,
            Screen.width - horizontalMargin - width * 0.5f);
        guiY = Mathf.Clamp(
            guiY,
            topMargin + height * 0.5f,
            Screen.height - bottomMargin - height * 0.5f);

        Rect indicatorRect = new Rect(
            guiX - width * 0.5f,
            guiY - height * 0.5f,
            width,
            height);

        if (hasAvoidRect && indicatorRect.Overlaps(avoidRect))
        {
            float shiftedY = avoidRect.yMax + 6f;
            if (shiftedY + height <= Screen.height - bottomMargin)
            {
                indicatorRect.y = shiftedY;
            }
            else
            {
                indicatorRect.y = Mathf.Max(topMargin, avoidRect.y - height - 6f);
            }
        }

        string direction = offScreen ? GetPickupIndicatorDirection(fromCenter) + " " : "";
        string label = direction + itemName + "  " + distance.ToString("0") + "m\nE / 手把A 拿取";
        GUI.Box(indicatorRect, label, indicatorStyle ?? hudBoxStyle);
        return indicatorRect;
    }

    private string GetPickupIndicatorDirection(Vector2 fromCenter)
    {
        if (Mathf.Abs(fromCenter.x) >= Mathf.Abs(fromCenter.y))
        {
            return fromCenter.x >= 0f ? "▶" : "◀";
        }

        return fromCenter.y >= 0f ? "▼" : "▲";
    }

    private Transform FindNearestPhysicalDeliveryTarget(bool isWine, float maxRange)
    {
        Transform[] configuredTargets = isWine ? wineDeliveryTargets : foodDeliveryTargets;
        HashSet<int> completedTargets = isWine ? deliveredWineTargets : sharedFoodTargets;

        Transform best = null;
        float bestDistance = Mathf.Max(0.5f, maxRange);
        Vector3 playerPosition = GetCurrentPlayerPosition();

        if (configuredTargets != null && configuredTargets.Length > 0)
        {
            for (int i = 0; i < configuredTargets.Length; i++)
            {
                Transform candidate = configuredTargets[i];
                if (candidate == null || completedTargets.Contains(candidate.GetInstanceID()))
                {
                    continue;
                }

                float distance = GetFlatDistance(playerPosition, candidate.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        // 沒填陣列時才回退到場景內既有 DeliverWine / ShareFood 互動物件。
        Chapter1Interactable.InteractionType wantedType = isWine
            ? Chapter1Interactable.InteractionType.DeliverWine
            : Chapter1Interactable.InteractionType.ShareFood;

        Chapter1Interactable[] interactables = FindObjectsOfType<Chapter1Interactable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            Chapter1Interactable interactable = interactables[i];
            if (interactable == null || interactable.interactionType != wantedType)
            {
                continue;
            }

            Transform candidate = interactable.transform;
            if (completedTargets.Contains(candidate.GetInstanceID()))
            {
                continue;
            }

            float distance = GetFlatDistance(playerPosition, candidate.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private string GetPhysicalWeddingDeliveryPrompt(string danceText)
    {
        int wineTarget = Mathf.Max(1, wineTargetCount);
        int foodTarget = Mathf.Max(1, foodTargetCount);
        bool wineComplete = deliveredWineCount >= wineTarget;
        bool foodComplete = sharedFoodCount >= foodTarget;

        if (carriedWeddingItem != WeddingCarryItem.None)
        {
            bool isWine = carriedWeddingItem == WeddingCarryItem.Wine;
            Transform receiver = GetDesignatedPhysicalDeliveryTarget(isWine);

            if (receiver != null)
            {
                float distance =
                    GetFlatDistance(GetCurrentPlayerPosition(), receiver.position);

                string targetLine = showDeliveryTargetNameAndDistance
                    ? "目標：" + receiver.name + "　" + distance.ToString("0.0") + "m"
                    : "請跟著黃色標記";

                string actionLine = distance <= Mathf.Max(0.5f, physicalDeliveryRange)
                    ? "E / 手把A  交付"
                    : "走向頭上有黃色標記的 NPC";

                return "手上：" + (isWine ? "酒" : "食物")
                    + "\\n" + targetLine
                    + "\\n" + actionLine
                    + "\\nQ  放回物品";
            }

            return "手上：" + (isWine ? "酒" : "食物")
                + "\\n尚未設定交付 NPC"
                + "\\nQ  放回物品";
        }

        if (wineComplete && foodComplete)
        {
            return danceText;
        }

        Transform wineSource = ResolvePhysicalPickupPoint(true);
        Transform foodSource = ResolvePhysicalPickupPoint(false);
        Vector3 playerPosition = GetCurrentPlayerPosition();

        float wineDistance = wineSource != null
            ? GetFlatDistance(playerPosition, GetPhysicalPickupWorldPosition(wineSource))
            : float.MaxValue;
        float foodDistance = foodSource != null
            ? GetFlatDistance(playerPosition, GetPhysicalPickupWorldPosition(foodSource))
            : float.MaxValue;

        if (!wineComplete && wineDistance <= Mathf.Max(0.5f, physicalPickupRange)
            && (foodComplete || wineDistance <= foodDistance))
        {
            return "送酒 " + deliveredWineCount + " / " + wineTarget
                + "\\nE / 手把A  拿起酒"
                + "\\n拿到後自己走去賓客旁";
        }

        if (!foodComplete && foodDistance <= Mathf.Max(0.5f, physicalPickupRange))
        {
            return "分享食物 " + sharedFoodCount + " / " + foodTarget
                + "\\nE / 手把A  拿起食物"
                + "\\n拿到後自己走去族人旁";
        }

        string wineLine = wineComplete
            ? "送酒 " + wineTarget + " / " + wineTarget + " ✓"
            : "送酒 " + deliveredWineCount + " / " + wineTarget + "：先走到酒旁";
        string foodLine = foodComplete
            ? "分享食物 " + foodTarget + " / " + foodTarget + " ✓"
            : "分享食物 " + sharedFoodCount + " / " + foodTarget + "：先走到食物旁";

        return wineLine + "\\n" + foodLine + "\\n" + danceText;
    }

    private void DisableLegacyItemTaskInteractables()
    {
        Chapter1Interactable[] interactables = FindObjectsOfType<Chapter1Interactable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            Chapter1Interactable interactable = interactables[i];
            if (interactable == null)
            {
                continue;
            }

            if (interactable.interactionType == Chapter1Interactable.InteractionType.DeliverWine
                || interactable.interactionType == Chapter1Interactable.InteractionType.ShareFood)
            {
                interactable.enabled = false;
            }
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
        string text = GetCleanInteractionPromptText();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        float width = Mathf.Min(Screen.width - 40f, 560f);
        float height = 58f;
        float bottomMargin =
            (Time.time < fallbackLineUntil && dialogueUI == null) ? 126f : 24f;

        Rect rect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - height - bottomMargin,
            width,
            height);

        GUI.Box(rect, text, centerMenuStyle);
    }

    private string GetCleanInteractionPromptText()
    {
        if (usePhysicalWeddingDelivery)
        {
            int wineTarget = Mathf.Max(1, wineTargetCount);
            int foodTarget = Mathf.Max(1, foodTargetCount);

            bool wineComplete = deliveredWineCount >= wineTarget;
            bool foodComplete = sharedFoodCount >= foodTarget;

            if (carriedWeddingItem != WeddingCarryItem.None)
            {
                bool isWine = carriedWeddingItem == WeddingCarryItem.Wine;
                Transform receiver = GetDesignatedPhysicalDeliveryTarget(isWine);

                if (receiver == null)
                {
                    return "尚未設定交付 NPC";
                }

                float distance =
                    GetFlatDistance(
                        GetCurrentPlayerPosition(),
                        receiver.position);

                if (distance <= Mathf.Max(0.5f, physicalDeliveryRange))
                {
                    return "[ E ]  交給 " + receiver.name;
                }

                return "目標：" + receiver.name
                    + "　" + distance.ToString("0.0") + "m"
                    + "　｜　跟著黃色標記";
            }

            if (wineComplete && foodComplete)
            {
                return danceFinished
                    ? ""
                    : "[ E ]  加入舞蹈";
            }

            Transform wineSource = ResolvePhysicalPickupPoint(true);
            Transform foodSource = ResolvePhysicalPickupPoint(false);
            Vector3 playerPosition = GetCurrentPlayerPosition();

            float wineDistance = wineSource != null
                ? GetPhysicalPickupDistance(wineSource, playerPosition)
                : float.MaxValue;

            float foodDistance = foodSource != null
                ? GetPhysicalPickupDistance(foodSource, playerPosition)
                : float.MaxValue;

            if (!foodComplete)
            {
                Transform nearbyFood = FindBestScenePickupByKeywords(
                    "烤魚", "魚", "食物", "烤肉", "food", "fish", "meat");

                if (nearbyFood != null)
                {
                    float nearbyFoodDistance =
                        GetPhysicalPickupDistance(nearbyFood, playerPosition);

                    if (foodSource == null
                        || nearbyFoodDistance + 0.35f < foodDistance)
                    {
                        foodSource = nearbyFood;
                        foodPickupPoint = nearbyFood;
                        foodDistance = nearbyFoodDistance;
                    }
                }
            }

            if (!wineComplete
                && wineDistance <= Mathf.Max(0.5f, physicalPickupRange)
                && (foodComplete || wineDistance <= foodDistance))
            {
                return "[ E ]  拿酒";
            }

            if (!foodComplete
                && foodDistance <= Mathf.Max(0.5f, physicalPickupRange))
            {
                return "[ E ]  拿食物";
            }

            if (!wineComplete && !foodComplete)
            {
                return "先到酒或食物旁拿取物品";
            }

            return !wineComplete
                ? "前往酒的位置"
                : "前往食物的位置";
        }

        if (AreWineAndFoodTasksComplete() && !danceFinished)
        {
            return "[ E ]  加入舞蹈";
        }

        return "";
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
            danceText = "舞蹈已完成";
        }
        else if (!AreWineAndFoodTasksComplete())
        {
            danceText = "完成送酒與分享食物後解鎖舞蹈";
        }
        else
        {
            danceText = "E / 1 / 手把A  加入舞蹈";
        }

        if (usePhysicalWeddingDelivery)
        {
            return GetPhysicalWeddingDeliveryPrompt(danceText);
        }

        if (manualWalkForItemTasks)
        {
            return danceText
                + "\nR / 2 / 手把B  走近賓客後送酒"
                + "\nT / 3 / 手把X  走近族人後分享食物";
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

        // NPC 收到酒後：先出聲 + 做感謝動作。
        PlayReceiverThankYouFeedback(receiverTarget, true);

        PlayGiveItemAnimation(
            true,
            "你拿起酒杯，走向賓客。",
            speaker,
            "謝謝你，這杯我收下了。願祖靈庇佑新人。",
            receiverTarget);

        int shownCount = Mathf.Min(deliveredWineCount, target);
        SetTemporaryMission("送酒任務：" + shownCount + " / " + target + " 位賓客已收到酒。", 3.5f);

        if (deliveredWineCount >= target)
        {
            StartCoroutine(ShowLineAfterDelay("新郎", "謝謝你。酒送好了，再幫忙把食物分給族人，等等一起去舞圈。", 3.8f, 4f));
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

        // NPC 收到食物後：先出聲 + 做感謝動作。
        PlayReceiverThankYouFeedback(receiverTarget, false);

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
            StartCoroutine(ShowLineAfterDelay("新郎", "辛苦了。事情都忙得差不多了，來舞圈一起跳吧。", 3.8f, 3.5f));
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
            return "靠近後按 R / 2 送酒";
        }

        if (interactionType == Chapter1Interactable.InteractionType.ShareFood)
        {
            return "靠近後按 T / 3 分享食物";
        }

        return fallbackPrompt;
    }

    private Vector3 GetReceiverVisualForward(Transform actorRoot)
    {
        if (actorRoot == null)
        {
            return Vector3.forward;
        }

        Animator animator =
            actorRoot.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator =
                actorRoot.GetComponentInParent<Animator>();
        }

        Vector3 detectedForward = Vector3.zero;

        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            // 最穩：用左右肩膀位置推算真正正面。
            if (autoDetectReceiverForwardFromShoulders)
            {
                Transform leftShoulder =
                    animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);

                Transform rightShoulder =
                    animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

                if (leftShoulder != null && rightShoulder != null)
                {
                    Vector3 rightAxis =
                        Vector3.ProjectOnPlane(
                            rightShoulder.position - leftShoulder.position,
                            Vector3.up);

                    if (rightAxis.sqrMagnitude > 0.0001f)
                    {
                        // right × up = forward
                        detectedForward =
                            Vector3.Cross(
                                rightAxis.normalized,
                                Vector3.up).normalized;
                    }
                }
            }

            // 第二順位：腳掌 -> 腳趾。
            if (detectedForward.sqrMagnitude < 0.001f)
            {
                Vector3 footForward = Vector3.zero;
                int samples = 0;

                Transform leftFoot =
                    animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform leftToes =
                    animator.GetBoneTransform(HumanBodyBones.LeftToes);
                Transform rightFoot =
                    animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform rightToes =
                    animator.GetBoneTransform(HumanBodyBones.RightToes);

                if (leftFoot != null && leftToes != null)
                {
                    Vector3 f =
                        Vector3.ProjectOnPlane(
                            leftToes.position - leftFoot.position,
                            Vector3.up);

                    if (f.sqrMagnitude > 0.0001f)
                    {
                        footForward += f.normalized;
                        samples++;
                    }
                }

                if (rightFoot != null && rightToes != null)
                {
                    Vector3 f =
                        Vector3.ProjectOnPlane(
                            rightToes.position - rightFoot.position,
                            Vector3.up);

                    if (f.sqrMagnitude > 0.0001f)
                    {
                        footForward += f.normalized;
                        samples++;
                    }
                }

                if (samples > 0 && footForward.sqrMagnitude > 0.001f)
                {
                    detectedForward = footForward.normalized;
                }
            }

            // 最後才用 Animator.forward。
            if (detectedForward.sqrMagnitude < 0.001f)
            {
                detectedForward =
                    Vector3.ProjectOnPlane(
                        animator.transform.forward,
                        Vector3.up);
            }
        }

        if (detectedForward.sqrMagnitude < 0.001f)
        {
            detectedForward =
                Vector3.ProjectOnPlane(
                    actorRoot.forward,
                    Vector3.up);
        }

        if (detectedForward.sqrMagnitude < 0.001f)
        {
            detectedForward = Vector3.forward;
        }

        float extraYaw = receiverVisualForwardYawOffset;

        if (IsTransformInReceiverList(
            actorRoot,
            receiverReverseFacingTargets))
        {
            extraYaw += 180f;
        }

        return Quaternion.AngleAxis(
            extraYaw,
            Vector3.up)
            * detectedForward.normalized;
    }

    private Quaternion GetReceiverRotationFacingPosition(
        Transform actorRoot,
        Vector3 targetPosition)
    {
        if (actorRoot == null)
        {
            return Quaternion.identity;
        }

        Vector3 toPlayer =
            Vector3.ProjectOnPlane(
                targetPosition - actorRoot.position,
                Vector3.up);

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            return actorRoot.rotation;
        }

        Vector3 visualForward =
            GetReceiverVisualForward(actorRoot);

        if (visualForward.sqrMagnitude < 0.001f)
        {
            return actorRoot.rotation;
        }

        float yaw =
            Vector3.SignedAngle(
                visualForward.normalized,
                toPlayer.normalized,
                Vector3.up);

        return Quaternion.AngleAxis(
            yaw,
            Vector3.up)
            * actorRoot.rotation;
    }

    private IEnumerator HoldReceiverFacingPlayerRoutine(
        Transform receiverTarget,
        float holdSeconds)
    {
        if (receiverTarget == null)
        {
            yield break;
        }

        Transform actorRoot =
            GetDeliveryActorRoot(receiverTarget);

        if (actorRoot == null)
        {
            actorRoot = receiverTarget;
        }

        Transform playerView =
            GetPlayerViewTransform();

        Vector3 playerPosition =
            playerView != null
                ? playerView.position
                : GetCurrentPlayerPosition();

        Quaternion startRotation =
            actorRoot.rotation;

        Quaternion facingRotation =
            GetReceiverRotationFacingPosition(
                actorRoot,
                playerPosition);

        Chapter1FaceFire[] faceFireComponents =
            actorRoot.GetComponentsInChildren<Chapter1FaceFire>(true);

        bool[] faceFireWasEnabled =
            new bool[faceFireComponents.Length];

        for (int i = 0; i < faceFireComponents.Length; i++)
        {
            Chapter1FaceFire faceFire = faceFireComponents[i];
            if (faceFire == null)
            {
                continue;
            }

            faceFireWasEnabled[i] = faceFire.enabled;
            faceFire.enabled = false;
        }

        float turnDuration =
            Mathf.Max(
                0.05f,
                receiverTurnToPlayerSeconds);

        float elapsed = 0f;

        while (elapsed < turnDuration
            && actorRoot != null)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / turnDuration));

            actorRoot.rotation =
                Quaternion.Slerp(
                    startRotation,
                    facingRotation,
                    t);

            yield return null;
        }

        if (actorRoot == null)
        {
            yield break;
        }

        actorRoot.rotation = facingRotation;

        Chapter1ReceiverFacingLock facingLock =
            actorRoot.GetComponent<Chapter1ReceiverFacingLock>();

        if (facingLock == null)
        {
            facingLock =
                actorRoot.gameObject.AddComponent<Chapter1ReceiverFacingLock>();
        }

        facingLock.LockFacing(
            facingRotation,
            Mathf.Max(
                0.5f,
                holdSeconds));

        float remaining =
            Mathf.Max(
                0f,
                holdSeconds - turnDuration);

        elapsed = 0f;

        while (elapsed < remaining
            && actorRoot != null)
        {
            elapsed += Time.deltaTime;

            // 玩家在交付期間通常不會移很遠，但仍重新取玩家位置，
            // 讓 NPC 保持自然地看著玩家，而不是看著舊位置。
            playerView = GetPlayerViewTransform();

            playerPosition =
                playerView != null
                    ? playerView.position
                    : GetCurrentPlayerPosition();

            facingRotation =
                GetReceiverRotationFacingPosition(
                    actorRoot,
                    playerPosition);

            actorRoot.rotation =
                Quaternion.Slerp(
                    actorRoot.rotation,
                    facingRotation,
                    Time.deltaTime * 9f);

            if (facingLock != null)
            {
                facingLock.UpdateTargetRotation(
                    facingRotation);
            }

            yield return null;
        }

        if (actorRoot != null)
        {
            if (keepReceiverFacingPlayerAfterDelivery)
            {
                actorRoot.rotation = facingRotation;
            }
            else
            {
                actorRoot.rotation = startRotation;
            }
        }

        for (int i = 0; i < faceFireComponents.Length; i++)
        {
            Chapter1FaceFire faceFire = faceFireComponents[i];
            if (faceFire != null)
            {
                faceFire.enabled = faceFireWasEnabled[i];
            }
        }
    }

    private string GetReceiverCustomAnimationState(
        Transform receiverTarget,
        bool isWine)
    {
        // 第一順位：男女不同動作。
        if (useGenderSpecificReceiverAnimation
            && receiverTarget != null)
        {
            bool isFemale =
                IsFemaleThankYouReceiver(
                    receiverTarget);

            string genderState =
                isFemale
                    ? femaleReceiverCustomAnimationState
                    : maleReceiverCustomAnimationState;

            if (!string.IsNullOrWhiteSpace(genderState))
            {
                return genderState.Trim();
            }
        }

        // 第二順位：全部共用同一個動作。
        if (useSameReceiverAnimationForAllNPCs
            && !string.IsNullOrWhiteSpace(allReceiverCustomAnimationState))
        {
            return allReceiverCustomAnimationState.Trim();
        }

        // 第三順位：酒 / 食物分開。
        if (isWine)
        {
            return string.IsNullOrWhiteSpace(wineReceiverCustomAnimationState)
                ? ""
                : wineReceiverCustomAnimationState.Trim();
        }

        if (useFoodReceiverCustomAnimationStateWhenSeparated
            && !string.IsNullOrWhiteSpace(foodReceiverCustomAnimationState))
        {
            return foodReceiverCustomAnimationState.Trim();
        }

        return "";
    }

    private IEnumerator PlayReceiverUniversalAnimationRoutine(
        Transform receiverTarget,
        bool isWine)
    {
        if (receiverTarget == null)
        {
            yield break;
        }

        Transform actorRoot =
            GetDeliveryActorRoot(receiverTarget);

        if (actorRoot == null)
        {
            actorRoot = receiverTarget;
        }

        Animator animator =
            actorRoot.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator =
                actorRoot.GetComponentInParent<Animator>();
        }

        float duration =
            Mathf.Max(
                0.6f,
                receiverCustomAnimationSeconds);

        string requestedState =
            GetReceiverCustomAnimationState(
                receiverTarget,
                isWine);

        if (animator != null
            && animator.runtimeAnimatorController != null)
        {
            List<string> candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(requestedState))
            {
                candidates.Add(requestedState);
            }

            // 指定 State 找不到時再找常見名稱。
            candidates.Add("Thank");
            candidates.Add("ThankYou");
            candidates.Add("Thanks");
            candidates.Add("Wave");
            candidates.Add("Bow");
            candidates.Add("Celebrate");
            candidates.Add("Cheer");
            candidates.Add("Happy");
            candidates.Add("Clap");

            for (int i = 0; i < candidates.Count; i++)
            {
                string stateName = candidates[i];

                if (string.IsNullOrWhiteSpace(stateName))
                {
                    continue;
                }

                int stateHash =
                    Animator.StringToHash(stateName);

                if (!animator.HasState(0, stateHash))
                {
                    continue;
                }

                animator.applyRootMotion = false;
                animator.CrossFade(stateHash, 0.12f, 0);

                bool debugIsFemale =
                    IsFemaleThankYouReceiver(
                        receiverTarget);

                Debug.Log(
                    "[Chapter1 ReceiverAnimation] NPC="
                    + receiverTarget.name
                    + " Gender="
                    + (debugIsFemale ? "Female" : "Male")
                    + " State="
                    + stateName);

                yield return new WaitForSeconds(duration);

                string returnState =
                    string.IsNullOrWhiteSpace(receiverThankYouReturnStateName)
                        ? deliveryTargetIdleStateName
                        : receiverThankYouReturnStateName.Trim();

                if (!string.IsNullOrWhiteSpace(returnState))
                {
                    int returnHash =
                        Animator.StringToHash(returnState);

                    if (animator.HasState(0, returnHash))
                    {
                        animator.CrossFade(
                            returnHash,
                            0.18f,
                            0);
                    }
                }

                yield break;
            }
        }

        // 沒有同名 State 或 Controller 時，Humanoid 仍做自然感謝姿勢。
        if (fallbackToNaturalReceiverGesture)
        {
            GameObject driverObject =
                animator != null
                    ? animator.gameObject
                    : actorRoot.gameObject;

            Chapter1ReceiverNaturalGestureDriver driver =
                driverObject.GetComponent<Chapter1ReceiverNaturalGestureDriver>();

            if (driver == null)
            {
                driver =
                    driverObject.AddComponent<Chapter1ReceiverNaturalGestureDriver>();
            }

            bool started =
                driver.PlayGesture(
                    animator,
                    actorRoot,
                    Mathf.Max(0.8f, naturalReceiverGestureSeconds),
                    naturalReceiverChestBowDegrees,
                    naturalReceiverHeadNodDegrees,
                    naturalReceiverArmForwardDegrees,
                    naturalReceiverForearmDegrees);

            if (started)
            {
                Debug.Log(
                    "[Chapter1 ReceiverAnimation] NPC="
                    + receiverTarget.name
                    + " 使用自然感謝動作");

                yield return new WaitForSeconds(
                    Mathf.Max(
                        0.8f,
                        naturalReceiverGestureSeconds));

                yield break;
            }
        }

        Debug.LogWarning(
            "[Chapter1 ReceiverAnimation] NPC="
            + receiverTarget.name
            + " 找不到可播放動畫。請確認 Animator Controller / Avatar / State 名稱。");
    }

    private IEnumerator ForceVisibleFoodCelebrationRoutine(Transform receiverTarget)
    {
        if (receiverTarget == null)
        {
            yield break;
        }

        Transform actorRoot =
            GetDeliveryActorRoot(receiverTarget);

        if (actorRoot == null)
        {
            actorRoot = receiverTarget;
        }

        Animator animator =
            actorRoot.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator =
                actorRoot.GetComponentInParent<Animator>();
        }

        if (debugFoodReceiverCelebration)
        {
            Debug.Log(
                "[Chapter1 FoodCelebrate] receiver="
                + receiverTarget.name
                + " actorRoot="
                + actorRoot.name
                + " animator="
                + (animator != null ? animator.name : "None"));
        }

        float duration =
            Mathf.Max(
                0.8f,
                naturalReceiverGestureSeconds);

        // 1. 只有「真的存在」的動畫 State 才播放。
        // 不再把 Talk 當慶祝動畫，避免找到一個幾乎不動的 Talk 後直接結束。
        if (animator != null
            && animator.runtimeAnimatorController != null)
        {
            List<string> candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(foodReceiverCustomAnimationState))
            {
                candidates.Add(foodReceiverCustomAnimationState.Trim());
            }

            candidates.Add("Celebrate");
            candidates.Add("Cheer");
            candidates.Add("Happy");
            candidates.Add("Clap");
            candidates.Add("Wave");
            candidates.Add("Thank");
            candidates.Add("ThankYou");
            candidates.Add("Bow");

            for (int i = 0; i < candidates.Count; i++)
            {
                string stateName = candidates[i];

                if (string.IsNullOrWhiteSpace(stateName))
                {
                    continue;
                }

                int stateHash =
                    Animator.StringToHash(stateName);

                if (!animator.HasState(0, stateHash))
                {
                    continue;
                }

                animator.applyRootMotion = false;
                animator.CrossFade(stateHash, 0.12f, 0);

                if (debugFoodReceiverCelebration)
                {
                    Debug.Log(
                        "[Chapter1 FoodCelebrate] 使用 Animator State："
                        + stateName
                        + " / NPC="
                        + receiverTarget.name);
                }

                yield return new WaitForSeconds(duration);

                string returnState =
                    string.IsNullOrWhiteSpace(receiverThankYouReturnStateName)
                        ? deliveryTargetIdleStateName
                        : receiverThankYouReturnStateName.Trim();

                if (!string.IsNullOrWhiteSpace(returnState))
                {
                    int returnHash =
                        Animator.StringToHash(returnState);

                    if (animator.HasState(0, returnHash))
                    {
                        animator.CrossFade(returnHash, 0.18f, 0);
                    }
                }

                yield break;
            }
        }

        // 2. 沒有真正動畫時，交給 LateUpdate Driver。
        // LateUpdate 發生在 Animator 更新之後、畫面 Render 之前，
        // 所以這次骨頭動作真的會出現在畫面上，不會再被 Animator 吃掉。
        GameObject driverObject =
            animator != null
                ? animator.gameObject
                : actorRoot.gameObject;

        Chapter1ReceiverNaturalGestureDriver driver =
            driverObject.GetComponent<Chapter1ReceiverNaturalGestureDriver>();

        if (driver == null)
        {
            driver =
                driverObject.AddComponent<Chapter1ReceiverNaturalGestureDriver>();
        }

        bool started =
            driver.PlayGesture(
                animator,
                actorRoot,
                duration,
                naturalReceiverChestBowDegrees,
                naturalReceiverHeadNodDegrees,
                naturalReceiverArmForwardDegrees,
                naturalReceiverForearmDegrees);

        if (debugFoodReceiverCelebration)
        {
            Debug.Log(
                "[Chapter1 FoodCelebrate] LateUpdate自然動作="
                + started
                + " / NPC="
                + receiverTarget.name);
        }

        if (started)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        // 3. 最後保底：如果模型不是 Humanoid，也至少做非常小的自然鞠躬。
        Quaternion originalRotation = actorRoot.rotation;
        float elapsed = 0f;

        while (elapsed < duration
            && actorRoot != null)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration);

            float envelope =
                Mathf.Sin(t * Mathf.PI);

            actorRoot.rotation =
                originalRotation
                * Quaternion.Euler(
                    naturalReceiverChestBowDegrees
                    * 0.55f
                    * envelope,
                    0f,
                    0f);

            yield return null;
        }

        if (actorRoot != null)
        {
            actorRoot.rotation = originalRotation;
        }
    }

    private void PlayReceiverThankYouFeedback(Transform receiverTarget, bool isWine)
    {
        if (receiverTarget == null)
        {
            return;
        }

        bool isFemale =
            IsFemaleThankYouReceiver(receiverTarget);

        AudioClip selectedClip = null;

        // 優先：男女專屬語音。
        if (isFemale && femaleReceiverThanksVoice != null)
        {
            selectedClip = femaleReceiverThanksVoice;
        }
        else if (!isFemale && maleReceiverThanksVoice != null)
        {
            selectedClip = maleReceiverThanksVoice;
        }

        // 第二順位：送酒 / 食物專用語音。
        if (selectedClip == null)
        {
            selectedClip =
                isWine
                    ? wineReceiverThanksVoice
                    : foodReceiverThanksVoice;
        }

        // 最後保底：共用謝謝語音。
        if (selectedClip == null)
        {
            selectedClip = receiverThanksVoice;
        }

        if (selectedClip != null)
        {
            Transform receiverRoot =
                GetDeliveryActorRoot(receiverTarget);

            if (receiverRoot == null)
            {
                receiverRoot = receiverTarget;
            }

            Vector3 voicePosition =
                GetReceiverFacePosition(receiverRoot);

            StartCoroutine(
                PlayReceiverThanksVoiceAtPosition(
                    selectedClip,
                    voicePosition));
        }

        // 實體交付時已經在 CompletePhysicalWeddingDelivery 精準觸發一次。
        // 非實體流程才在這裡補觸發，避免同一 NPC 動作播兩次。
        if (!physicalDeliveryAnimating)
        {
            StartCoroutine(
                PlayReceiverUniversalAnimationRoutine(
                    receiverTarget,
                    isWine));
        }
    }

    private bool IsFemaleThankYouReceiver(Transform receiverTarget)
    {
        if (receiverTarget == null)
        {
            return false;
        }

        Transform receiverRoot =
            GetDeliveryActorRoot(receiverTarget);

        if (receiverRoot == null)
        {
            receiverRoot = receiverTarget;
        }

        // 手動指定優先。
        if (IsTransformInReceiverList(receiverRoot, femaleThankYouTargets)
            || IsTransformInReceiverList(receiverTarget, femaleThankYouTargets))
        {
            return true;
        }

        if (IsTransformInReceiverList(receiverRoot, maleThankYouTargets)
            || IsTransformInReceiverList(receiverTarget, maleThankYouTargets))
        {
            return false;
        }

        if (!autoDetectReceiverGenderByName)
        {
            return false;
        }

        string combinedName =
            (receiverRoot.name + " " + receiverTarget.name)
            .ToLowerInvariant();

        string[] femaleKeywords =
        {
            "女",
            "新娘",
            "female",
            "woman",
            "girl",
            "bride",
            "mother",
            "sister",
            "mom",
            "woman"
        };

        for (int i = 0; i < femaleKeywords.Length; i++)
        {
            if (combinedName.Contains(femaleKeywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTransformInReceiverList(
        Transform candidate,
        Transform[] list)
    {
        if (candidate == null
            || list == null)
        {
            return false;
        }

        for (int i = 0; i < list.Length; i++)
        {
            Transform assigned = list[i];

            if (assigned == null)
            {
                continue;
            }

            if (candidate == assigned
                || candidate.IsChildOf(assigned)
                || assigned.IsChildOf(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator PlayReceiverThanksVoiceAtPosition(
        AudioClip clip,
        Vector3 position)
    {
        if (clip == null)
        {
            yield break;
        }

        GameObject temp =
            new GameObject("Chapter1_ReceiverThanksVoice");

        temp.transform.position = position;

        AudioSource source =
            temp.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume =
            Mathf.Clamp01(
                receiverThanksVoiceVolume);

        source.spatialBlend =
            Mathf.Clamp01(
                receiverThanksSpatialBlend);

        source.minDistance =
            Mathf.Max(
                0.1f,
                receiverThanksMinDistance);

        source.maxDistance =
            Mathf.Max(
                source.minDistance + 0.1f,
                receiverThanksMaxDistance);

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.loop = false;
        source.playOnAwake = false;
        source.dopplerLevel = 0f;
        source.priority = 64;

        source.Play();

        yield return new WaitForSeconds(
            Mathf.Max(
                0.1f,
                clip.length + 0.15f));

        if (temp != null)
        {
            Destroy(temp);
        }
    }

    private IEnumerator PlayReceiverThanks2D(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            yield break;
        }

        GameObject temp =
            new GameObject("Chapter1_ReceiverThanksAudio");

        AudioSource source =
            temp.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.loop = false;
        source.Play();

        yield return new WaitForSeconds(
            Mathf.Max(
                0.1f,
                clip.length + 0.1f));

        if (temp != null)
        {
            Destroy(temp);
        }
    }

    private IEnumerator PlayReceiverThankYouMotionRoutine(
        Transform receiverTarget,
        bool isWine)
    {
        if (receiverTarget == null)
        {
            yield break;
        }

        bool isFood = !isWine;

        Transform receiverRoot =
            GetDeliveryActorRoot(receiverTarget);

        if (receiverRoot == null)
        {
            receiverRoot = receiverTarget;
        }

        Animator animator =
            receiverRoot.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator =
                receiverRoot.GetComponentInParent<Animator>();
        }

        float duration =
            Mathf.Max(
                0.4f,
                isFood
                    ? foodReceiverCelebrateMotionSeconds
                    : receiverThankYouMotionSeconds);

        // 1. 優先播放角色本來就有的動作。
        if (animator != null
            && animator.runtimeAnimatorController != null)
        {
            string[] candidates =
                isFood
                    ? new string[]
                    {
                        "Celebrate",
                        "Cheer",
                        "Happy",
                        "Clap",
                        "Dance",
                        "Wave",
                        "Thank",
                        "Thanks",
                        "ThankYou",
                        "Bow",
                        "Talk"
                    }
                    : new string[]
                    {
                        "Thank",
                        "Thanks",
                        "ThankYou",
                        "Bow",
                        "Wave",
                        "Talk"
                    };

            bool shouldPreferAnimator =
                isFood
                    ? preferAnimatorCelebrateMotionForFood
                    : preferAnimatorThankYouMotion;

            if (shouldPreferAnimator)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    int stateHash =
                        Animator.StringToHash(candidates[i]);

                    if (!animator.HasState(0, stateHash))
                    {
                        continue;
                    }

                    animator.applyRootMotion = false;
                    animator.CrossFade(stateHash, 0.12f, 0);

                    yield return new WaitForSeconds(duration);

                    string returnState =
                        string.IsNullOrWhiteSpace(receiverThankYouReturnStateName)
                            ? deliveryTargetIdleStateName
                            : receiverThankYouReturnStateName.Trim();

                    if (!string.IsNullOrWhiteSpace(returnState))
                    {
                        int returnHash =
                            Animator.StringToHash(returnState);

                        if (animator.HasState(0, returnHash))
                        {
                            animator.CrossFade(
                                returnHash,
                                0.18f,
                                0);
                        }
                    }

                    yield break;
                }
            }
        }

        // 2. 沒有現成動畫時，直接做「一定看得到」的程式動作。
        // 之前只改骨頭，有些匯入模型會被 Animator 每幀蓋掉，所以這版同時動角色 Visual Root。
        Transform visualRoot = animator != null ? animator.transform : receiverRoot;

        Vector3 originalVisualLocalPosition =
            visualRoot != null ? visualRoot.localPosition : Vector3.zero;

        Quaternion originalVisualLocalRotation =
            visualRoot != null ? visualRoot.localRotation : Quaternion.identity;

        Vector3 originalVisualLocalScale =
            visualRoot != null ? visualRoot.localScale : Vector3.one;

        Transform head = null;
        Transform rightUpperArm = null;
        Transform rightForearm = null;
        Transform leftUpperArm = null;
        Transform leftForearm = null;

        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        }

        float elapsed = 0f;
        int beatCount =
            Mathf.Clamp(
                isFood ? foodReceiverCelebrateNodCount : receiverThankYouNodCount,
                1,
                3);

        while (elapsed < duration)
        {
            // 等 Animator 寫完，再疊我們自己的動作。
            yield return new WaitForEndOfFrame();

            if (visualRoot == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float beat = Mathf.Sin(t * Mathf.PI * beatCount);

            if (isFood)
            {
                // 整個模型做明顯的小慶祝：上下彈、左右擺、輕微縮放。
                float bounce =
                    Mathf.Max(0f, beat)
                    * Mathf.Max(0.02f, foodReceiverCelebrateBounceHeight);

                float sway =
                    Mathf.Sin(t * Mathf.PI * 2f)
                    * foodReceiverCelebrateBodySwayDegrees
                    * envelope;

                float scalePulse =
                    1f
                    + Mathf.Max(0f, beat)
                    * Mathf.Max(0f, foodReceiverCelebrateScalePulse);

                visualRoot.localPosition =
                    originalVisualLocalPosition
                    + Vector3.up * bounce;

                visualRoot.localRotation =
                    originalVisualLocalRotation
                    * Quaternion.Euler(0f, 0f, sway);

                visualRoot.localScale =
                    Vector3.Scale(
                        originalVisualLocalScale,
                        new Vector3(1f, scalePulse, 1f));

                // Humanoid 的話再疊雙手抬起，看起來更像開心收到食物。
                float armEnvelope = Mathf.Max(0f, envelope);
                float handLift = Mathf.Max(8f, foodReceiverCelebrateHandLiftDegrees);

                if (rightUpperArm != null)
                {
                    rightUpperArm.localRotation =
                        rightUpperArm.localRotation
                        * Quaternion.Euler(0f, 0f, -handLift * 0.55f * armEnvelope);
                }

                if (rightForearm != null)
                {
                    rightForearm.localRotation =
                        rightForearm.localRotation
                        * Quaternion.Euler(-handLift * 0.85f * armEnvelope, 0f, 0f);
                }

                if (leftUpperArm != null)
                {
                    leftUpperArm.localRotation =
                        leftUpperArm.localRotation
                        * Quaternion.Euler(0f, 0f, handLift * 0.55f * armEnvelope);
                }

                if (leftForearm != null)
                {
                    leftForearm.localRotation =
                        leftForearm.localRotation
                        * Quaternion.Euler(-handLift * 0.75f * armEnvelope, 0f, 0f);
                }

                if (head != null)
                {
                    head.localRotation =
                        head.localRotation
                        * Quaternion.Euler(
                            receiverThankYouNodDegrees
                            * 0.7f
                            * Mathf.Max(0f, beat),
                            0f,
                            0f);
                }
            }
            else
            {
                // 送酒維持比較克制的謝謝動作。
                visualRoot.localRotation =
                    originalVisualLocalRotation
                    * Quaternion.Euler(
                        receiverThankYouNodDegrees * 0.18f * envelope,
                        0f,
                        0f);

                if (rightUpperArm != null)
                {
                    rightUpperArm.localRotation =
                        rightUpperArm.localRotation
                        * Quaternion.Euler(
                            0f,
                            0f,
                            -receiverThankYouHandLiftDegrees * 0.45f * envelope);
                }

                if (rightForearm != null)
                {
                    rightForearm.localRotation =
                        rightForearm.localRotation
                        * Quaternion.Euler(
                            -receiverThankYouHandLiftDegrees * envelope,
                            0f,
                            0f);
                }
            }
        }

        // 一定恢復原本模型 Transform，避免角色永久歪掉或變大。
        if (visualRoot != null)
        {
            visualRoot.localPosition = originalVisualLocalPosition;
            visualRoot.localRotation = originalVisualLocalRotation;
            visualRoot.localScale = originalVisualLocalScale;
        }
    }

    private void PlayGiveItemAnimation(bool isWine, string actionLine, string receiverName, string receiverLine, Transform receiverTarget = null)
    {
        if (!playInteractionAnimations)
        {
            StartCoroutine(ReceiverDialogueOnlyRoutine(
                receiverName,
                receiverLine,
                receiverTarget));
            return;
        }

        StartCoroutine(GiveItemAnimationRoutine(isWine, actionLine, receiverName, receiverLine, receiverTarget));
    }

    private IEnumerator GiveItemAnimationRoutine(bool isWine, string actionLine, string receiverName, string receiverLine, Transform receiverTarget)
    {
        interactionAnimationRunning = true;
        bool shouldGuidePlayer =
            !usePhysicalWeddingDelivery
            && !manualWalkForItemTasks
            && guidePlayerDuringItemInteractions
            && GetDancePlayerRoot() != null;

        bool shouldLockControl =
            !manualWalkForItemTasks
            && (lockPlayerDuringInteractionAnimation || shouldGuidePlayer);

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
            if (manualWalkForItemTasks)
            {
                // 玩家已經自己走到 NPC 旁邊，只播放近距離的交付動作，不移動 XR Origin。
                Vector3 handPosition = GetCarriedPropPosition();
                prop.transform.position = handPosition;
                ShowLine("動作", isWine ? "你把酒遞給眼前的賓客。" : "你把食物遞給眼前的族人。", guidedGiveSeconds + 0.7f);
                yield return HoldPropInView(prop, 0.18f);
                yield return MovePropToPosition(
                    prop,
                    GetCarriedPropPosition(),
                    receiverPosition,
                    Mathf.Max(0.25f, guidedGiveSeconds),
                    0.12f);
            }
            else
            {
                ShowLine("動作", actionLine, interactionAnimationSeconds);
                yield return MovePropToPosition(prop, pickupPosition, receiverPosition, interactionAnimationSeconds, interactionArcHeight);
            }
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

        yield return FocusOnReceiverDuringDialogue(
            receiverName,
            receiverLine,
            receiverTarget);

        if (shouldLockControl)
        {
            SetPlayerControl(true);
        }

        interactionAnimationRunning = false;
    }

    private IEnumerator ReceiverDialogueOnlyRoutine(
        string receiverName,
        string receiverLine,
        Transform receiverTarget)
    {
        interactionAnimationRunning = true;
        yield return FocusOnReceiverDuringDialogue(
            receiverName,
            receiverLine,
            receiverTarget);
        interactionAnimationRunning = false;
    }

    private Transform GetPlayerHandsRoot()
    {
        if (playerHandsRoot != null)
        {
            return playerHandsRoot;
        }

        if (!string.IsNullOrWhiteSpace(playerHandsObjectName))
        {
            Transform found =
                FindTransformByName(
                    playerHandsObjectName.Trim());

            if (found != null)
            {
                playerHandsRoot = found;
                return playerHandsRoot;
            }
        }

        return null;
    }

    private Renderer[] HidePlayerHandRenderers(
        out bool[] originalStates)
    {
        originalStates = null;

        if (!hidePlayerHandsDuringReceiverDialogue)
        {
            return null;
        }

        Transform handsRoot =
            GetPlayerHandsRoot();

        if (handsRoot == null)
        {
            return null;
        }

        Renderer[] renderers =
            handsRoot.GetComponentsInChildren<Renderer>(true);

        originalStates =
            new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            originalStates[i] =
                renderer.enabled;

            renderer.enabled = false;
        }

        return renderers;
    }

    private void RestorePlayerHandRenderers(
        Renderer[] renderers,
        bool[] originalStates)
    {
        if (renderers == null
            || originalStates == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                renderers.Length,
                originalStates.Length);

        for (int i = 0; i < count; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer != null)
            {
                renderer.enabled =
                    originalStates[i];
            }
        }
    }

    private IEnumerator FocusOnReceiverDuringDialogue(
        string receiverName,
        string receiverLine,
        Transform receiverTarget)
    {
        Renderer[] hiddenHandRenderers = null;
        bool[] hiddenHandRendererStates = null;

        if (hidePlayerHandsDuringReceiverDialogue)
        {
            hiddenHandRenderers =
                HidePlayerHandRenderers(
                    out hiddenHandRendererStates);
        }

        float holdSeconds = Mathf.Max(0.5f, receiverDialogueHoldSeconds);
        if (!focusCameraOnReceiverDialogue || receiverTarget == null)
        {
            ShowLine(receiverName, receiverLine, holdSeconds);
            yield return new WaitForSeconds(holdSeconds);

            RestorePlayerHandRenderers(
                hiddenHandRenderers,
                hiddenHandRendererStates);

            yield break;
        }

        Transform playerRootTransform = GetDancePlayerRoot();
        Transform playerView = GetPlayerViewTransform();
        if (playerRootTransform == null || playerView == null)
        {
            ShowLine(receiverName, receiverLine, holdSeconds);
            yield return new WaitForSeconds(holdSeconds);

            RestorePlayerHandRenderers(
                hiddenHandRenderers,
                hiddenHandRendererStates);

            yield break;
        }

        Transform receiverRoot = GetDeliveryActorRoot(receiverTarget);
        if (receiverRoot == null)
        {
            receiverRoot = receiverTarget;
        }

        Vector3 originalViewPosition = playerView.position;
        Quaternion originalViewRotation = playerView.rotation;
        Vector3 originalViewLocalPosition = playerView.localPosition;
        Quaternion originalViewLocalRotation = playerView.localRotation;
        Quaternion originalReceiverRotation = receiverRoot.rotation;
        Chapter1FaceFire faceFire = receiverRoot.GetComponentInChildren<Chapter1FaceFire>(true);
        bool faceFireWasEnabled = faceFire != null && faceFire.enabled;
        if (faceFire != null)
        {
            faceFire.enabled = false;
        }

        SetPlayerControl(false);

        List<Behaviour> trackedPoseDrivers = DisableCameraTrackedPoseDrivers(playerView);
        GetReceiverDialogueCameraPose(
            receiverRoot,
            playerView,
            out Vector3 dialogueCameraPosition,
            out Quaternion dialogueCameraRotation);
        Quaternion focusedReceiverRotation =
            keepReceiverFacingPlayerDuringDialogue
                ? originalReceiverRotation
                : GetHorizontalLookRotation(
                    receiverRoot,
                    dialogueCameraPosition,
                    originalReceiverRotation);

        float panDuration = Mathf.Max(0.05f, receiverDialoguePanSeconds);
        float elapsed = 0f;
        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / panDuration));
            playerView.SetPositionAndRotation(
                Vector3.Lerp(originalViewPosition, dialogueCameraPosition, t),
                Quaternion.Slerp(originalViewRotation, dialogueCameraRotation, t));
            receiverRoot.rotation = Quaternion.Slerp(
                originalReceiverRotation,
                focusedReceiverRotation,
                t);
            yield return null;
        }

        playerView.SetPositionAndRotation(dialogueCameraPosition, dialogueCameraRotation);
        receiverRoot.rotation = focusedReceiverRotation;
        ShowLine(receiverName, receiverLine, holdSeconds);

        elapsed = 0f;
        while (elapsed < holdSeconds)
        {
            elapsed += Time.deltaTime;
            playerView.SetPositionAndRotation(dialogueCameraPosition, dialogueCameraRotation);
            receiverRoot.rotation = focusedReceiverRotation;
            yield return null;
        }

        float returnDuration = Mathf.Max(0.05f, receiverDialogueReturnSeconds);
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / returnDuration));
            playerView.SetPositionAndRotation(
                Vector3.Lerp(dialogueCameraPosition, originalViewPosition, t),
                Quaternion.Slerp(dialogueCameraRotation, originalViewRotation, t));

            if (!keepReceiverFacingPlayerDuringDialogue)
            {
                receiverRoot.rotation = Quaternion.Slerp(
                    focusedReceiverRotation,
                    originalReceiverRotation,
                    t);
            }

            yield return null;
        }

        playerView.localPosition = originalViewLocalPosition;
        playerView.localRotation = originalViewLocalRotation;

        if (!keepReceiverFacingPlayerDuringDialogue)
        {
            receiverRoot.rotation = originalReceiverRotation;
        }
        RestoreCameraTrackedPoseDrivers(trackedPoseDrivers);
        if (faceFire != null)
        {
            faceFire.enabled = faceFireWasEnabled;
        }

        RestorePlayerHandRenderers(
            hiddenHandRenderers,
            hiddenHandRendererStates);

        if (!policeSequenceStarted && !waitingForChoice && !chapterCompleted)
        {
            SetPlayerControl(true);
        }
    }

    private List<Behaviour> DisableCameraTrackedPoseDrivers(Transform playerView)
    {
        List<Behaviour> disabledDrivers = new List<Behaviour>();
        if (playerView == null)
        {
            return disabledDrivers;
        }

        Behaviour[] behaviours = playerView.GetComponents<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName.IndexOf("TrackedPoseDriver", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            behaviour.enabled = false;
            disabledDrivers.Add(behaviour);
        }

        return disabledDrivers;
    }

    private void RestoreCameraTrackedPoseDrivers(List<Behaviour> trackedPoseDrivers)
    {
        if (trackedPoseDrivers == null)
        {
            return;
        }

        for (int i = 0; i < trackedPoseDrivers.Count; i++)
        {
            if (trackedPoseDrivers[i] != null)
            {
                trackedPoseDrivers[i].enabled = true;
            }
        }
    }

    private void GetReceiverDialogueCameraPose(
        Transform receiverRoot,
        Transform playerView,
        out Vector3 cameraPosition,
        out Quaternion cameraRotation)
    {
        Vector3 facePosition = GetReceiverFacePosition(receiverRoot);
        Vector3 framingCenter = facePosition - Vector3.up * 0.28f;
        float framingHeight = Mathf.Max(0.8f, receiverFallbackFaceHeight * 0.72f);
        float framingWidth = framingHeight * 0.75f;

        bool hasVisualBounds = TryGetReceiverVisualBounds(receiverRoot, out Bounds actorBounds);
        if (TryGetReceiverSkeletonFrame(
            receiverRoot,
            hasVisualBounds ? actorBounds : default(Bounds),
            hasVisualBounds,
            out Vector3 skeletonCenter,
            out float skeletonHeight,
            out float skeletonWidth))
        {
            framingCenter = skeletonCenter;
            framingHeight = skeletonHeight;
            framingWidth = skeletonWidth;
        }
        else if (hasVisualBounds)
        {
            float bodyHeight = Mathf.Max(0.2f, actorBounds.size.y);
            float upperBodyBottom = Mathf.Lerp(
                actorBounds.min.y,
                actorBounds.max.y,
                Mathf.Clamp(receiverDialogueUpperBodyBottomRatio, 0.2f, 0.6f));
            float top = actorBounds.max.y;

            framingCenter = new Vector3(
                actorBounds.center.x,
                (upperBodyBottom + top) * 0.5f,
                actorBounds.center.z);
            framingHeight = Mathf.Max(0.4f, top - upperBodyBottom);
            framingWidth = Mathf.Max(
                actorBounds.size.x,
                actorBounds.size.z) * 0.9f;

            // Keep the head as the source of truth if clothing bounds stop below it.
            if (facePosition.y > top - bodyHeight * 0.18f)
            {
                float headTop = facePosition.y + bodyHeight * 0.1f;
                framingCenter.y = (upperBodyBottom + headTop) * 0.5f;
                framingHeight = Mathf.Max(framingHeight, headTop - upperBodyBottom);
            }
        }

        Debug.Log(
            "[Chapter1 ReceiverCamera] target=" + receiverRoot.name
            + ", center=" + framingCenter.ToString("F2")
            + ", frame=" + framingWidth.ToString("0.00")
            + "x" + framingHeight.ToString("0.00"));

        float verticalFov = 60f;
        float aspect = 16f / 9f;
        Camera viewCamera = playerView != null ? playerView.GetComponent<Camera>() : null;
        if (viewCamera != null)
        {
            verticalFov = Mathf.Clamp(viewCamera.fieldOfView, 30f, 100f);
            aspect = Mathf.Max(0.5f, viewCamera.aspect);
        }

        float padding = Mathf.Max(1.05f, receiverDialogueFramePadding);
        float verticalTangent = Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f);
        float horizontalTangent = verticalTangent * aspect;
        float heightDistance = framingHeight * 0.5f * padding / Mathf.Max(0.1f, verticalTangent);
        float widthDistance = framingWidth * 0.5f * padding / Mathf.Max(0.1f, horizontalTangent);
        float distance = Mathf.Max(
            Mathf.Max(1f, receiverDialogueCameraDistance),
            Mathf.Max(heightDistance, widthDistance));
        distance = Mathf.Min(distance, Mathf.Max(1f, receiverDialogueMaxCameraDistance));

        Vector3 cameraSide = playerView != null
            ? playerView.position - framingCenter
            : -receiverRoot.forward;
        cameraSide.y = 0f;
        if (cameraSide.sqrMagnitude < 0.01f)
        {
            cameraSide = -receiverRoot.forward;
            cameraSide.y = 0f;
        }
        if (cameraSide.sqrMagnitude < 0.01f)
        {
            cameraSide = Vector3.back;
        }

        cameraPosition = framingCenter + cameraSide.normalized * distance;
        Vector3 lookDirection = framingCenter - cameraPosition;
        cameraRotation = lookDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : (playerView != null ? playerView.rotation : Quaternion.identity);
    }

    private bool TryGetReceiverSkeletonFrame(
        Transform receiverRoot,
        Bounds actorBounds,
        bool hasVisualBounds,
        out Vector3 framingCenter,
        out float framingHeight,
        out float framingWidth)
    {
        framingCenter = Vector3.zero;
        framingHeight = 0f;
        framingWidth = 0f;

        if (!TryGetReceiverHeadBonePosition(receiverRoot, out Vector3 headPosition))
        {
            return false;
        }

        bool hasHips = TryGetReceiverHipsBonePosition(receiverRoot, out Vector3 hipsPosition);
        bool hasFeet = TryGetReceiverFeetY(receiverRoot, out float feetY);

        float estimatedBodyHeight;
        if (hasFeet && headPosition.y > feetY + 0.2f)
        {
            estimatedBodyHeight = (headPosition.y - feetY) / 0.9f;
        }
        else if (hasHips && headPosition.y > hipsPosition.y + 0.15f)
        {
            estimatedBodyHeight = (headPosition.y - hipsPosition.y) * 2.15f;
        }
        else if (hasVisualBounds)
        {
            estimatedBodyHeight = Mathf.Max(0.5f, actorBounds.size.y);
        }
        else
        {
            estimatedBodyHeight = Mathf.Max(0.5f, receiverFallbackFaceHeight);
        }

        float headTopY = headPosition.y + estimatedBodyHeight * 0.09f;
        Vector3 upperBodyBottom;
        if (hasHips)
        {
            upperBodyBottom = hipsPosition;
        }
        else
        {
            float lowerY = hasFeet
                ? Mathf.Lerp(feetY, headTopY, 0.46f)
                : headPosition.y - estimatedBodyHeight * 0.48f;
            upperBodyBottom = new Vector3(headPosition.x, lowerY, headPosition.z);
        }

        Vector3 headTop = new Vector3(headPosition.x, headTopY, headPosition.z);
        framingCenter = Vector3.Lerp(upperBodyBottom, headTop, 0.5f);
        framingHeight = Mathf.Max(0.45f, headTopY - upperBodyBottom.y);

        float estimatedWidth = estimatedBodyHeight * 0.42f;
        if (hasVisualBounds)
        {
            float rendererWidth = Mathf.Max(actorBounds.size.x, actorBounds.size.z);
            estimatedWidth = Mathf.Clamp(
                rendererWidth,
                estimatedBodyHeight * 0.28f,
                estimatedBodyHeight * 0.72f);
        }

        framingWidth = Mathf.Max(0.3f, estimatedWidth);
        return true;
    }

    private bool TryGetReceiverHeadBonePosition(Transform receiverRoot, out Vector3 headPosition)
    {
        headPosition = Vector3.zero;
        if (receiverRoot == null)
        {
            return false;
        }

        Animator animator = receiverRoot.GetComponentInChildren<Animator>(true);
        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform humanoidHead = animator.GetBoneTransform(HumanBodyBones.Head);
            if (humanoidHead != null)
            {
                headPosition = humanoidHead.position;
                return true;
            }
        }

        Transform[] descendants = receiverRoot.GetComponentsInChildren<Transform>(true);
        Transform best = null;
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];
            if (candidate == null)
            {
                continue;
            }

            string boneName = candidate.name.ToLowerInvariant();
            if (!boneName.Contains("head") && !boneName.Contains("頭"))
            {
                continue;
            }

            if (best == null || candidate.position.y > best.position.y)
            {
                best = candidate;
            }
        }

        if (best == null)
        {
            return false;
        }

        headPosition = best.position;
        return true;
    }

    private bool TryGetReceiverHipsBonePosition(Transform receiverRoot, out Vector3 hipsPosition)
    {
        hipsPosition = Vector3.zero;
        if (receiverRoot == null)
        {
            return false;
        }

        Animator animator = receiverRoot.GetComponentInChildren<Animator>(true);
        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform humanoidHips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (humanoidHips != null)
            {
                hipsPosition = humanoidHips.position;
                return true;
            }
        }

        Transform[] descendants = receiverRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];
            if (candidate == null)
            {
                continue;
            }

            string boneName = candidate.name.ToLowerInvariant();
            if (boneName.Contains("hips")
                || boneName.Contains("pelvis")
                || boneName.Contains("骨盆"))
            {
                hipsPosition = candidate.position;
                return true;
            }
        }

        return false;
    }

    private bool TryGetReceiverFeetY(Transform receiverRoot, out float feetY)
    {
        feetY = float.MaxValue;
        if (receiverRoot == null)
        {
            return false;
        }

        bool found = false;
        Animator animator = receiverRoot.GetComponentInChildren<Animator>(true);
        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null)
            {
                feetY = Mathf.Min(feetY, leftFoot.position.y);
                found = true;
            }
            if (rightFoot != null)
            {
                feetY = Mathf.Min(feetY, rightFoot.position.y);
                found = true;
            }
        }

        if (!found)
        {
            Transform[] descendants = receiverRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate == null)
                {
                    continue;
                }

                string boneName = candidate.name.ToLowerInvariant();
                if (!boneName.Contains("foot")
                    && !boneName.Contains("ankle")
                    && !boneName.Contains("腳"))
                {
                    continue;
                }

                feetY = Mathf.Min(feetY, candidate.position.y);
                found = true;
            }
        }

        return found;
    }

    private bool TryGetReceiverVisualBounds(Transform receiverRoot, out Bounds combinedBounds)
    {
        combinedBounds = default(Bounds);
        if (receiverRoot == null)
        {
            return false;
        }

        Renderer[] renderers = receiverRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null
                || !renderer.enabled
                || !renderer.gameObject.activeInHierarchy
                || renderer is ParticleSystemRenderer
                || renderer is TrailRenderer
                || renderer is LineRenderer)
            {
                continue;
            }

            string rendererName = renderer.name.ToLowerInvariant();
            if (rendererName.Contains("marker") || rendererName.Contains("arrow"))
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private Vector3 GetReceiverFacePosition(Transform receiverRoot)
    {
        if (receiverRoot == null)
        {
            return transform.position + Vector3.up * receiverFallbackFaceHeight;
        }

        if (TryGetReceiverHeadBonePosition(receiverRoot, out Vector3 headPosition))
        {
            return headPosition + Vector3.up * 0.08f;
        }

        Renderer[] renderers = receiverRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds combinedBounds = default(Bounds);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
        {
            return new Vector3(
                combinedBounds.center.x,
                combinedBounds.max.y - combinedBounds.size.y * 0.09f,
                combinedBounds.center.z);
        }

        return receiverRoot.position + Vector3.up * Mathf.Max(0.5f, receiverFallbackFaceHeight);
    }

    private Quaternion GetHorizontalLookRotation(
        Transform actor,
        Vector3 toPosition,
        Quaternion fallback)
    {
        if (actor == null)
        {
            return fallback;
        }

        Vector3 direction = Vector3.ProjectOnPlane(
            toPosition - actor.position,
            Vector3.up);
        if (direction.sqrMagnitude < 0.001f)
        {
            return fallback;
        }

        Vector3 currentFacing =
            GetReceiverVisualForward(actor);

        if (currentFacing.sqrMagnitude < 0.001f)
        {
            return fallback;
        }

        float yaw = Vector3.SignedAngle(
            currentFacing.normalized,
            direction.normalized,
            Vector3.up);
        return Quaternion.AngleAxis(yaw, Vector3.up) * fallback;
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
            float viewDistance = carriedPropViewDistance;
            float rightOffset = carriedPropViewRightOffset;
            float downOffset = carriedPropViewDownOffset;

            if (IsNewPoliceScene()
                && carriedWeddingItem == WeddingCarryItem.Food)
            {
                viewDistance = Mathf.Max(viewDistance, newPoliceSceneFoodViewDistance);
                rightOffset = newPoliceSceneFoodViewRightOffset;
                downOffset = newPoliceSceneFoodViewDownOffset;
            }

            return viewTransform.position
                + viewTransform.forward * Mathf.Max(0.35f, viewDistance)
                + viewTransform.right * rightOffset
                - viewTransform.up * downOffset;
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

    private void FitNewPoliceSceneHeldFood(GameObject food)
    {
        if (!IsNewPoliceScene() || food == null)
        {
            return;
        }

        Renderer[] renderers = food.GetComponentsInChildren<Renderer>(true);
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        float largestDimension = Mathf.Max(
            combinedBounds.size.x,
            combinedBounds.size.y,
            combinedBounds.size.z);

        if (largestDimension <= 0.0001f)
        {
            return;
        }

        float targetSize = Mathf.Max(0.2f, newPoliceSceneFoodTargetSize);
        food.transform.localScale *= targetSize / largestDimension;
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
        if (weddingCircleConfigured)
        {
            Transform circleCenter = FindTransformByName("烤乳豬舞圈中心");
            if (circleCenter != null)
            {
                center = circleCenter;
            }
        }

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

        float radius = weddingCircleConfigured
            ? Mathf.Max(0.1f, weddingCircleRadius)
            : (danceRadius > 0.1f ? danceRadius : flatOffset.magnitude);
        float startAngle = Mathf.Atan2(flatOffset.x, flatOffset.z);
        float totalRadians = danceOrbitDegrees * Mathf.Deg2Rad;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float angle;
            float stepBob;

            if (weddingCircleConfigured)
            {
                float beatsPerSecond = Mathf.Max(1f, weddingCircleTempoBpm) / 60f;
                angle = weddingPlayerGapAngleRadians
                    + Time.time
                    * beatsPerSecond
                    * weddingCircleDegreesPerBeat
                    * Mathf.Deg2Rad;
                stepBob = Mathf.Abs(Mathf.Sin(Time.time * beatsPerSecond * Mathf.PI))
                    * danceStepHeight;
            }
            else
            {
                angle = startAngle + (totalRadians * easedProgress);
                stepBob = Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 2f * danceStepFrequency))
                    * danceStepHeight;
            }

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
        SetWeddingNpcGrounding(false);
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
        // 先讓最後一個收禮 / 舞蹈動作完整結束。
        while (interactionAnimationRunning)
        {
            yield return null;
        }

        if (autoPlayCinematicStoryAfterWeddingTasks)
        {
            cinematicStoryPlaying = true;
            SetPlayerControl(false);
            SetWorldInteractionPromptVisible(false);
            ClearDeliveryTargetMarker();
            missionText = "";

            yield return new WaitForSeconds(
                Mathf.Max(0f, cinematicPreRollSeconds));
        }

        SetMission("婚禮任務完成。遠處忽然傳來急促的皮靴聲……");

        yield return new WaitForSeconds(
            Mathf.Max(0f, policeStartDelayAfterWeddingTasks));

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

        Transform circleCenter = FindTransformByName("烤乳豬舞圈中心");
        if (circleCenter == null)
        {
            circleCenter = center;
        }

        Animator[] animators = EnsureNamedAddedWeddingDancerAnimators();
        NormalizeNamedAddedWeddingDancers(animators, circleCenter);

        float neighborSpacing;
        weddingCircleRadius = CalculateWeddingCircleRadius(
            animators,
            circleCenter,
            out neighborSpacing);
        danceRadius = weddingCircleRadius;

        Vector3 playerOffset = playerRoot != null
            ? playerRoot.position - circleCenter.position
            : -circleCenter.forward;
        playerOffset.y = 0f;
        if (playerOffset.sqrMagnitude < 0.01f)
        {
            playerOffset = Vector3.back;
        }

        weddingPlayerGapAngleRadians = Mathf.Atan2(playerOffset.x, playerOffset.z);
        float playerGapAngleDegrees = weddingPlayerGapAngleRadians * Mathf.Rad2Deg;

        List<RuntimeAnimatorController> crowdControllers = new List<RuntimeAnimatorController>();

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, circleCenter))
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
        HashSet<string> namedAddedDancersFound = new HashSet<string>();
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, circleCenter))
            {
                continue;
            }

            bool isNamedAddedDancer = TryFindNamedAddedDancerRoot(
                animator.transform,
                out Transform namedAddedRoot);

            if (isNamedAddedDancer && namedAddedRoot != null)
            {
                string rootName = namedAddedRoot.name.Trim();
                for (int nameIndex = 0; nameIndex < AddedWeddingDancerNames.Length; nameIndex++)
                {
                    string expectedName = AddedWeddingDancerNames[nameIndex];
                    if (rootName == expectedName
                        || rootName.StartsWith(expectedName + " (", System.StringComparison.Ordinal))
                    {
                        namedAddedDancersFound.Add(expectedName);
                        break;
                    }
                }
            }

            if ((animator.runtimeAnimatorController == null || isNamedAddedDancer)
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

            dancer.animator = animator;
            dancer.center = circleCenter;
            dancer.roastedPigCenter = circleCenter;
            dancer.useRoastedPigAsCenter = true;
            dancer.playOnAwake = true;
            dancer.orbitSpeedDegrees = weddingCrowdOrbitSpeedDegrees;
            dancer.stepHeight = weddingCrowdStepHeight;
            dancer.stepFrequency = weddingCrowdStepFrequency;
            dancer.radialStepDistance = weddingCrowdRadialStepDistance;
            dancer.swayDegrees = weddingCrowdSwayDegrees;
            dancer.autoArrangeEvenlyAroundCenter = true;
            dancer.reservedPlayerSlots = Mathf.Max(0, weddingReservedPlayerSlots);
            dancer.groupStartAngleDegrees = playerGapAngleDegrees;
            dancer.useFixedCircleRadius = true;
            dancer.fixedCircleRadius = weddingCircleRadius;
            dancer.preventHandHoldAutoShrink = true;
            dancer.tempoBpm = weddingCircleTempoBpm;
            dancer.degreesPerBeat = weddingCircleDegreesPerBeat;
            dancer.enableHandHolding = true;
            dancer.enableProceduralHandHolding = true;
            dancer.proceduralHandHoldWeight = 1f;
            dancer.handHoldShoulderToHipRatio = 0.5f;
            dancer.maximumHandPairDistance = Mathf.Max(
                dancer.maximumHandPairDistance,
                neighborSpacing * 1.2f);
            dancer.enableProceduralStepping = true;
            dancer.faceCenter = true;
            dancer.travelFacingBlend = 0f;
            dancer.turnSmooth = Mathf.Max(12f, dancer.turnSmooth);

            Chapter1FaceFire faceFire = animator.GetComponent<Chapter1FaceFire>();
            if (faceFire == null)
            {
                faceFire = animator.gameObject.AddComponent<Chapter1FaceFire>();
            }

            faceFire.target = circleCenter;
            faceFire.owner = this;
            faceFire.turnSpeed = 14f;
            faceFire.yawOffsetDegrees = 0f;

            if (isNamedAddedDancer && namedAddedRoot != null)
            {
                dancer.enabled = true;
                dancer.SetCanCircleDance(true);
            }

            dancer.SetDancing(true);

            if (!weddingCrowdDancers.Contains(dancer))
            {
                weddingCrowdDancers.Add(dancer);
            }
        }

        // All components now exist, including the Generic-rig dancers. Recalculate
        // once more so their actual arm lengths determine a reachable hand-hold ring.
        float finalizedNeighborSpacing;
        weddingCircleRadius = CalculateWeddingCircleRadius(
            animators,
            circleCenter,
            out finalizedNeighborSpacing);
        danceRadius = weddingCircleRadius;
        for (int i = 0; i < weddingCrowdDancers.Count; i++)
        {
            Chapter1CircleDancer dancer = weddingCrowdDancers[i];
            if (dancer == null || !dancer.canCircleDance)
            {
                continue;
            }

            dancer.fixedCircleRadius = weddingCircleRadius;
            dancer.maximumHandPairDistance = Mathf.Max(
                dancer.maximumHandPairDistance,
                finalizedNeighborSpacing * 1.2f);
        }

        weddingCircleConfigured = weddingCrowdDancers.Count > 0;
        Debug.Log(
            "[Chapter1] Wedding crowd dancers started: " + weddingCrowdDancers.Count
            + ", radius: " + weddingCircleRadius.ToString("0.00")
            + ", reserved player slots: " + Mathf.Max(0, weddingReservedPlayerSlots)
            + ", added dancers found: " + namedAddedDancersFound.Count + "/"
            + AddedWeddingDancerNames.Length);

        for (int i = 0; i < AddedWeddingDancerNames.Length; i++)
        {
            if (!namedAddedDancersFound.Contains(AddedWeddingDancerNames[i]))
            {
                Debug.LogWarning(
                    "[Chapter1] Added wedding dancer was not included: "
                    + AddedWeddingDancerNames[i]);
            }
        }
    }

    private void NormalizeNamedAddedWeddingDancers(
        Animator[] animators,
        Transform center)
    {
        if (!normalizeNamedAddedDancerHeight || animators == null || center == null)
        {
            return;
        }

        List<float> referenceHeights = new List<float>();
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, center)
                || IsDeliveryTaskNPC(animator.transform)
                || TryFindNamedAddedDancerRoot(animator.transform, out _))
            {
                continue;
            }

            if (TryGetAnimatorRigHeight(animator, out float rigHeight))
            {
                referenceHeights.Add(rigHeight);
            }
        }

        if (referenceHeights.Count == 0)
        {
            Debug.LogWarning("[Chapter1] No existing dancer rig height was available for crowd normalization.");
            return;
        }

        float targetHeight = GetMedian(referenceHeights)
            * Mathf.Clamp(namedAddedDancerHeightRatio, 0.8f, 1.2f);
        HashSet<int> adjustedRoots = new HashSet<int>();

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null
                || !IsWeddingCrowdActor(animator, center)
                || IsDeliveryTaskNPC(animator.transform))
            {
                continue;
            }

            Chapter1CircleDancer existing = animator.GetComponent<Chapter1CircleDancer>();
            bool isNamedAddedDancer = TryFindNamedAddedDancerRoot(
                animator.transform,
                out Transform namedRoot);
            if (existing != null && !existing.canCircleDance && !isNamedAddedDancer)
            {
                continue;
            }

            Transform actorRoot = isNamedAddedDancer && namedRoot != null
                ? namedRoot
                : animator.transform;

            if (actorRoot == null
                || !adjustedRoots.Add(actorRoot.GetInstanceID())
                || !TryGetAnimatorRigHeight(animator, out float currentHeight))
            {
                continue;
            }

            float scaleFactor = Mathf.Clamp(targetHeight / currentHeight, 0.25f, 4f);
            if (Mathf.Abs(scaleFactor - 1f) > 0.01f)
            {
                actorRoot.localScale *= scaleFactor;
            }

            Debug.Log(
                "[Chapter1] Dancer " + actorRoot.name
                + " rig height normalized to " + targetHeight.ToString("0.00") + ".");
        }
    }

    private IEnumerator RefreshWeddingDancerSizingAfterAnimatorUpdate()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Transform center = FindTransformByName("烤乳豬舞圈中心");
        if (center == null)
        {
            center = danceCenter != null ? danceCenter : GetDanceCenter();
        }

        if (center == null)
        {
            yield break;
        }

        Animator[] animators = EnsureNamedAddedWeddingDancerAnimators();
        NormalizeNamedAddedWeddingDancers(animators, center);

        float neighborSpacing;
        weddingCircleRadius = CalculateWeddingCircleRadius(
            animators,
            center,
            out neighborSpacing);
        danceRadius = weddingCircleRadius;

        for (int i = 0; i < weddingCrowdDancers.Count; i++)
        {
            Chapter1CircleDancer dancer = weddingCrowdDancers[i];
            if (dancer == null
                || !dancer.canCircleDance
                || IsDeliveryTaskNPC(dancer.transform))
            {
                continue;
            }

            dancer.fixedCircleRadius = weddingCircleRadius;
            dancer.maximumHandPairDistance = Mathf.Max(
                dancer.maximumHandPairDistance,
                neighborSpacing * 1.2f);
        }
    }

    private bool TryGetAnimatorRigHeight(Animator animator, out float height)
    {
        height = 0f;
        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (head != null && hips != null)
            {
                float legLengthTotal = 0f;
                int legCount = 0;
                if (leftUpperLeg != null && leftLowerLeg != null && leftFoot != null)
                {
                    legLengthTotal += Vector3.Distance(hips.position, leftUpperLeg.position)
                        + Vector3.Distance(leftUpperLeg.position, leftLowerLeg.position)
                        + Vector3.Distance(leftLowerLeg.position, leftFoot.position);
                    legCount++;
                }

                if (rightUpperLeg != null && rightLowerLeg != null && rightFoot != null)
                {
                    legLengthTotal += Vector3.Distance(hips.position, rightUpperLeg.position)
                        + Vector3.Distance(rightUpperLeg.position, rightLowerLeg.position)
                        + Vector3.Distance(rightLowerLeg.position, rightFoot.position);
                    legCount++;
                }

                if (legCount > 0)
                {
                    height = Vector3.Distance(head.position, hips.position)
                        + legLengthTotal / legCount;
                }
            }
        }

        if (height <= 0.05f
            && animator != null
            && TryGetCharacterBounds(animator.transform, out Bounds bounds))
        {
            height = bounds.size.y;
        }

        return height > 0.05f;
    }

    private float CalculateWeddingCircleRadius(
        Animator[] animators,
        Transform center,
        out float neighborSpacing)
    {
        List<float> armReaches = new List<float>();
        int activeDancerCount = 0;

        if (animators != null)
        {
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (!IsWeddingCrowdActor(animator, center)
                    || IsDeliveryTaskNPC(animator.transform))
                {
                    continue;
                }

                Chapter1CircleDancer existing = animator.GetComponent<Chapter1CircleDancer>();
                if (existing != null && !existing.canCircleDance
                    && !TryFindNamedAddedDancerRoot(animator.transform, out _))
                {
                    continue;
                }

                activeDancerCount++;
                float reach = GetAnimatorArmReach(animator);
                if (reach > 0.05f)
                {
                    armReaches.Add(reach);
                }
            }
        }

        int slotCount = Mathf.Max(
            3,
            activeDancerCount + Mathf.Max(0, weddingReservedPlayerSlots));
        float radius = Mathf.Max(2.2f, weddingCircleRadius);

        if (autoFitWeddingCircleToArmReach && armReaches.Count > 0)
        {
            float medianArmReach = GetMedian(armReaches);
            neighborSpacing = medianArmReach
                * Mathf.Clamp(weddingNeighborSpacingArmMultiplier, 1.55f, 1.75f);
            float denominator = 2f * Mathf.Sin(Mathf.PI / slotCount);
            if (denominator > 0.001f)
            {
                radius = neighborSpacing / denominator;
            }

            radius = Mathf.Clamp(radius, 2.2f, Mathf.Max(30f, weddingCircleRadius * 2f));
        }

        neighborSpacing = 2f * radius * Mathf.Sin(Mathf.PI / slotCount);
        return radius;
    }

    private float GetAnimatorArmReach(Animator animator)
    {
        if (animator == null)
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        if (animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (leftUpperArm != null && leftHand != null)
            {
                total += Vector3.Distance(leftUpperArm.position, leftHand.position);
                count++;
            }

            if (rightUpperArm != null && rightHand != null)
            {
                total += Vector3.Distance(rightUpperArm.position, rightHand.position);
                count++;
            }
        }

        if (count > 0)
        {
            return total / count;
        }

        Chapter1CircleDancer dancer = animator.GetComponent<Chapter1CircleDancer>();
        return dancer != null ? dancer.GetAverageArmReach() : 0f;
    }

    private bool TryFindNamedAddedDancerRoot(
        Transform candidate,
        out Transform actorRoot)
    {
        actorRoot = null;
        Transform current = candidate;

        while (current != null && current.gameObject.scene.IsValid())
        {
            string currentName = current.name.Trim();
            for (int i = 0; i < AddedWeddingDancerNames.Length; i++)
            {
                string expectedName = AddedWeddingDancerNames[i];
                bool exactMatch = string.Equals(
                    currentName,
                    expectedName,
                    System.StringComparison.Ordinal);
                bool unityDuplicateName = currentName.StartsWith(
                    expectedName + " (",
                    System.StringComparison.Ordinal)
                    && currentName.EndsWith(")", System.StringComparison.Ordinal);

                if (exactMatch || unityDuplicateName)
                {
                    actorRoot = current;
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    private Animator[] EnsureNamedAddedWeddingDancerAnimators()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        HashSet<int> processedRoots = new HashSet<int>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null
                || !candidate.gameObject.scene.IsValid()
                || !TryFindNamedAddedDancerRoot(candidate, out Transform actorRoot)
                || actorRoot == null
                || !processedRoots.Add(actorRoot.GetInstanceID()))
            {
                continue;
            }

            Animator actorAnimator = actorRoot.GetComponentInChildren<Animator>(true);
            if (actorAnimator == null)
            {
                actorAnimator = actorRoot.gameObject.AddComponent<Animator>();
                Debug.Log(
                    "[Chapter1] Added missing Animator for wedding dancer: "
                    + actorRoot.name);
            }

            actorAnimator.enabled = true;
            actorAnimator.applyRootMotion = false;
            actorAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        return Resources.FindObjectsOfTypeAll<Animator>();
    }

    private bool TryGetCharacterBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
        if (root == null)
        {
            return false;
        }

        SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMesh == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private float GetMedian(List<float> values)
    {
        values.Sort();
        int middle = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[middle - 1] + values[middle]) * 0.5f
            : values[middle];
    }

    private void EnsureNewPoliceSceneNpcGrounding()
    {
        if (!IsNewPoliceScene())
        {
            return;
        }

        Transform center = danceCenter != null ? danceCenter : GetDanceCenter();
        if (center == null)
        {
            return;
        }

        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!IsWeddingCrowdActor(animator, center))
            {
                continue;
            }

            Chapter1NpcGrounding grounder =
                animator.GetComponent<Chapter1NpcGrounding>();
            if (grounder == null)
            {
                grounder = animator.gameObject.AddComponent<Chapter1NpcGrounding>();
            }

            grounder.Configure(animator, guidedGroundLayers);
            grounder.enabled = true;
            grounder.SnapImmediately();

            if (!weddingNpcGrounders.Contains(grounder))
            {
                weddingNpcGrounders.Add(grounder);
            }
        }

        Debug.Log("[Chapter1] Wedding NPC grounders enabled: " + weddingNpcGrounders.Count);
    }

    private void SetWeddingNpcGrounding(bool enabled)
    {
        for (int i = weddingNpcGrounders.Count - 1; i >= 0; i--)
        {
            Chapter1NpcGrounding grounder = weddingNpcGrounders[i];
            if (grounder == null)
            {
                weddingNpcGrounders.RemoveAt(i);
                continue;
            }

            grounder.enabled = enabled;
            if (enabled)
            {
                grounder.SnapImmediately();
            }
        }
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
        if (actor.parent != null && actor.parent.GetComponentInParent<Animator>() != null)
        {
            return false;
        }

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

        if (TryFindNamedAddedDancerRoot(actor, out _))
        {
            return true;
        }

        return GetFlatDistance(actor.position, center.position) <= Mathf.Max(1f, weddingCrowdDanceRange);
    }

    private void PrepareDeliveryTaskNPCs()
    {
        if (!keepDeliveryTargetsStationary)
        {
            return;
        }

        StopDeliveryTargetArray(wineDeliveryTargets);
        StopDeliveryTargetArray(foodDeliveryTargets);
    }

    private void StopDeliveryTargetArray(Transform[] targets)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            StopDeliveryTargetNPC(targets[i]);
        }
    }

    private void StopDeliveryTargetNPC(Transform target)
    {
        if (target == null)
        {
            return;
        }

        // 你目前的婚禮舞圈如果是靠 DanceCirclePivot 父物件旋轉，
        // 只關 Chapter1CircleDancer 是不夠的；子物件仍會被父物件帶著繞圈。
        // 所以先找出「角色 Root」，再把它從旋轉 Pivot 底下移出去。
        Transform actorRoot = GetDeliveryActorRoot(target);

        if (detachDeliveryTargetsFromDancePivot && actorRoot != null)
        {
            DetachActorFromDancePivot(actorRoot);
        }

        Transform searchRoot = actorRoot != null ? actorRoot : target;

        // 關掉角色自己身上的繞圈元件。
        Chapter1CircleDancer[] childDancers =
            searchRoot.GetComponentsInChildren<Chapter1CircleDancer>(true);

        for (int i = 0; i < childDancers.Length; i++)
        {
            Chapter1CircleDancer dancer = childDancers[i];
            if (dancer == null)
            {
                continue;
            }

            dancer.SetDancing(false);
            dancer.enabled = false;
        }

        Chapter1CircleDancer parentDancer =
            searchRoot.GetComponentInParent<Chapter1CircleDancer>();

        if (parentDancer != null)
        {
            parentDancer.SetDancing(false);
            parentDancer.enabled = false;
        }

        // 切回 Idle。
        Animator[] animators = searchRoot.GetComponentsInChildren<Animator>(true);

        if (animators.Length == 0)
        {
            Animator parentAnimator = searchRoot.GetComponentInParent<Animator>();
            if (parentAnimator != null)
            {
                animators = new Animator[] { parentAnimator };
            }
        }

        string idleState =
            string.IsNullOrWhiteSpace(deliveryTargetIdleStateName)
                ? "Idle"
                : deliveryTargetIdleStateName.Trim();

        int idleHash = Animator.StringToHash(idleState);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController != null)
            {
                if (animator.HasState(0, idleHash))
                {
                    animator.CrossFade(idleHash, 0.08f, 0);
                }
                else
                {
                    Debug.LogWarning(
                        "[Chapter1] 任務 NPC " + searchRoot.name
                        + " 的 Animator 找不到 Idle State："
                        + idleState);
                }
            }
        }
    }

    private Transform GetDeliveryActorRoot(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        // 如果拖到 Model / Armature / Mesh，也盡量往上找到完整角色。
        Transform current = target;
        Transform best = target;

        while (current.parent != null)
        {
            Transform parent = current.parent;
            string parentName = parent.name.ToLowerInvariant();

            if (IsDancePivotTransform(parent))
            {
                // current 就是 Dance Pivot 底下的角色 Root。
                return current;
            }

            // 遇到明顯的場景總群組就不要再往上吃。
            if (parentName.Contains("weddingnpcgroup")
                || parentName.Contains("npcgroup")
                || parentName.Contains("villagergroup")
                || parentName.Contains("crowdgroup"))
            {
                return current;
            }

            best = current;
            current = parent;
        }

        // 沒找到 Pivot 時，優先使用 Animator 所在的角色層級。
        Animator childAnimator = target.GetComponentInChildren<Animator>(true);
        if (childAnimator != null)
        {
            Transform animatorTransform = childAnimator.transform;
            Transform actor = animatorTransform;

            while (actor.parent != null
                && !IsDancePivotTransform(actor.parent)
                && actor.parent.GetComponentInParent<Animator>() == null)
            {
                actor = actor.parent;
            }

            return actor;
        }

        Animator parentAnimator = target.GetComponentInParent<Animator>();
        if (parentAnimator != null)
        {
            return parentAnimator.transform;
        }

        return best;
    }

    private bool IsDancePivotTransform(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (danceCenter != null && candidate == danceCenter)
        {
            return true;
        }

        string n = candidate.name.ToLowerInvariant();

        return n.Contains("dancecirclepivot")
            || n.Contains("dancepivot")
            || n.Contains("circlepivot")
            || (n.Contains("dance") && n.Contains("pivot"))
            || (n.Contains("舞") && n.Contains("圈"));
    }

    private void DetachActorFromDancePivot(Transform actorRoot)
    {
        if (actorRoot == null || actorRoot.parent == null)
        {
            return;
        }

        Transform parent = actorRoot.parent;

        // 往上找旋轉舞圈 Pivot。
        Transform pivot = null;
        Transform cursor = parent;

        while (cursor != null)
        {
            if (IsDancePivotTransform(cursor))
            {
                pivot = cursor;
                break;
            }

            // 不要一路爬到整個場景根節點。
            if (cursor.parent == null)
            {
                break;
            }

            cursor = cursor.parent;
        }

        if (pivot == null)
        {
            return;
        }

        // 把角色移到 Pivot 的上一層；true 會保留世界座標/旋轉/縮放，
        // 所以角色不會瞬移。
        Transform newParent = pivot.parent;
        actorRoot.SetParent(newParent, true);

        Debug.Log(
            "[Chapter1] 任務 NPC 已離開舞圈 Pivot："
            + actorRoot.name
            + "，不再跟著 "
            + pivot.name
            + " 繞圈。");
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

            if (enabled && keepDeliveryTargetsStationary && IsDeliveryTaskNPC(dancer.transform))
            {
                dancer.SetDancing(false);
                dancer.enabled = false;
                continue;
            }

            dancer.enabled = true;
            dancer.SetDancing(enabled);
        }
    }

    private bool IsDeliveryTaskNPC(Transform candidate)
    {
        return TransformBelongsToAnyDeliveryTarget(candidate, wineDeliveryTargets)
            || TransformBelongsToAnyDeliveryTarget(candidate, foodDeliveryTargets);
    }

    private bool TransformBelongsToAnyDeliveryTarget(Transform candidate, Transform[] targets)
    {
        if (candidate == null || targets == null)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            Transform actorRoot = GetDeliveryActorRoot(target);
            if (actorRoot == null)
            {
                actorRoot = target;
            }

            if (candidate == actorRoot
                || candidate.IsChildOf(actorRoot)
                || actorRoot.IsChildOf(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator CrossfadeStoryMusic(
        AudioSource from,
        AudioSource to,
        float seconds,
        float targetVolume)
    {
        float duration = Mathf.Max(0.05f, seconds);
        float fromStartVolume = from != null ? from.volume : 0f;
        float toTargetVolume = Mathf.Clamp01(targetVolume);

        if (to != null)
        {
            to.loop = true;
            to.playOnAwake = false;
            to.volume = 0f;

            if (!to.isPlaying)
            {
                to.Play();
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (from != null)
            {
                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            }

            if (to != null)
            {
                to.volume = Mathf.Lerp(0f, toTargetVolume, t);
            }

            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.volume = fromStartVolume;
        }

        if (to != null)
        {
            to.volume = toTargetVolume;
        }
    }

    private IEnumerator PoliceSequenceRoutine()
    {
        SetPlayerControl(false);
        EnsurePoliceActorsForIntrusion();
        SetMission("婚禮中斷：兩名日本警察闖入會場。");

        if (hardCutWeddingAudioOnPolice)
        {
            // 婚禮最歡樂的聲音全部切掉，留一拍安靜。
            if (weddingAmbience != null)
            {
                weddingAmbience.Stop();
            }

            if (weddingVocals != null)
            {
                weddingVocals.Stop();
            }

            if (cinematicStoryPlaying)
            {
                ShowLine(
                    "旁白",
                    "歡笑聲戛然而止。遠處，只剩皮靴踩過山路的聲音。",
                    Mathf.Max(1.2f, cinematicSilenceBeatSeconds + 0.8f));

                yield return new WaitForSeconds(
                    Mathf.Max(0f, cinematicSilenceBeatSeconds));
            }

            if (tensionAmbience != null)
            {
                tensionAmbience.loop = true;
                tensionAmbience.playOnAwake = false;
                tensionAmbience.volume = Mathf.Clamp01(tensionMusicTargetVolume);

                if (!tensionAmbience.isPlaying)
                {
                    tensionAmbience.Play();
                }
            }
        }
        else if (useStoryMusicCrossfade)
        {
            if (weddingVocals != null)
            {
                weddingVocals.Stop();
            }

            StartCoroutine(CrossfadeStoryMusic(
                weddingAmbience,
                tensionAmbience,
                storyMusicCrossfadeSeconds,
                tensionMusicTargetVolume));
        }
        else
        {
            if (weddingAmbience != null)
            {
                weddingAmbience.Stop();
            }

            if (weddingVocals != null)
            {
                weddingVocals.Stop();
            }

            if (tensionAmbience != null)
            {
                tensionAmbience.loop = true;
                tensionAmbience.volume = tensionMusicTargetVolume;
                tensionAmbience.Play();
            }
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

        // 天空俯視慢慢壓低後，切到第一名日警正面近景說第一句台詞。
        yield return CinematicPoliceFrontRevealAndFirstLine();

        yield return CinematicPanTo(groomActor, policeCameraPanSeconds);
        yield return CinematicExtraHold();

        yield return PlayPoliceVoicedLine(
            "新郎",
            "我們只是辦婚禮，沒有冒犯。",
            groomReplyVoice,
            3.2f,
            groomReplyVolumeScale);

        if (useFallbackIncidentAnimation)
        {
            if (useCeremonyCupCloseUp && ceremonyCup != null)
            {
                yield return CinematicCupCloseUpAndShoveVillagerFallback();
            }
            else
            {
                yield return CinematicPanTo(
                    ceremonyCup != null ? ceremonyCup : shovedVillagerActor,
                    policeCameraPanSeconds * 0.8f);

                yield return KnockCupAndShoveVillagerFallback();
            }
        }
        else
        {
            yield return CinematicPanTo(shovedVillagerActor, policeCameraPanSeconds * 0.8f);
            ShowLine("旁白", "警察推倒酒杯，又粗暴地推開靠近的族人。", 3.5f);
            yield return new WaitForSeconds(3.2f);
        }

        Transform harassingPolice = secondaryPoliceActor != null ? secondaryPoliceActor : primaryPoliceActor;
        if (femaleVillagerActor != null && harassingPolice != null)
        {
            yield return CinematicPanTo(femaleVillagerActor, policeCameraPanSeconds);
            yield return CinematicExtraHold();

            ShowLine("旁白", "另一名警察把目光轉向一名女性族人，伸手逼近她。周圍的族人立刻騷動起來。", 4f);
            yield return MoveActorNearTarget(harassingPolice, femaleVillagerActor.position, 1.0f, policeApproachWomanSeconds);
            PlayPoliceEventClip(struggleClip);
            yield return PlayPoliceVoicedLine(
                "女性族人",
                "放開我！",
                femaleResistVoice,
                2.5f);
        }
        else
        {
            ShowLine("旁白", "一名警察試圖騷擾女性族人，四周的怒氣瞬間升高。", 3.8f);
            yield return new WaitForSeconds(3.4f);
        }

        // 最後把視線帶回事件中心，再跳出選擇。
        Transform choiceTarget = choiceFocusPoint != null
            ? choiceFocusPoint.transform
            : (groomActor != null ? groomActor : primaryPoliceActor);
        yield return CinematicPanTo(choiceTarget, policeCameraPanSeconds * 0.85f);

        ShowConflictChoice();
    }

    private IEnumerator CinematicPoliceFrontRevealAndFirstLine()
    {
        if (!usePoliceFrontReveal
            || !usePoliceCinematicCamera
            || primaryPoliceActor == null)
        {
            yield return CinematicPanTo(
                primaryPoliceActor,
                policeCameraPanSeconds);

            yield return CinematicExtraHold();

            yield return PlayPoliceVoicedLine(
                "日警",
                "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
                policeInsultVoice,
                4f);

            yield break;
        }

        Transform playerView = GetPlayerViewTransform();
        if (playerView == null)
        {
            yield return PlayPoliceVoicedLine(
                "日警",
                "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
                policeInsultVoice,
                4f);

            yield break;
        }

        Vector3 originalPosition = playerView.position;
        Quaternion originalRotation = playerView.rotation;
        Vector3 originalLocalPosition = playerView.localPosition;
        Quaternion originalLocalRotation = playerView.localRotation;

        List<Behaviour> trackedPoseDrivers =
            DisableCameraTrackedPoseDrivers(playerView);

        Vector3 policeForward =
            Vector3.ProjectOnPlane(
                primaryPoliceActor.forward,
                Vector3.up);

        if (policeForward.sqrMagnitude < 0.01f)
        {
            policeForward =
                Vector3.ProjectOnPlane(
                    GetFireCenterPosition() - primaryPoliceActor.position,
                    Vector3.up);
        }

        if (policeForward.sqrMagnitude < 0.01f)
        {
            policeForward = Vector3.forward;
        }

        policeForward.Normalize();

        Vector3 policeRight =
            Vector3.Cross(Vector3.up, policeForward).normalized;

        Vector3 focusPoint =
            primaryPoliceActor.position
            + Vector3.up * Mathf.Max(0.8f, policeFrontRevealHeight);

        Vector3 desiredPosition =
            primaryPoliceActor.position
            + policeForward * Mathf.Max(0.8f, policeFrontRevealDistance)
            + policeRight * policeFrontRevealSideOffset
            + Vector3.up * Mathf.Max(0.8f, policeFrontRevealHeight);

        Vector3 lookDirection =
            focusPoint - desiredPosition;

        Quaternion desiredRotation =
            lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up)
                : originalRotation;

        float moveDuration =
            Mathf.Max(0.05f, policeFrontRevealMoveSeconds);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / moveDuration));

            playerView.SetPositionAndRotation(
                Vector3.Lerp(
                    originalPosition,
                    desiredPosition,
                    t),
                Quaternion.Slerp(
                    originalRotation,
                    desiredRotation,
                    t));

            yield return null;
        }

        playerView.SetPositionAndRotation(
            desiredPosition,
            desiredRotation);

        if (policeFrontRevealHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(
                policeFrontRevealHoldSeconds);
        }

        // 第一名日警的第一句話就在正面近景中說完。
        yield return PlayPoliceVoicedLine(
            "日警",
            "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？",
            policeInsultVoice,
            4f);

        Vector3 closePosition = playerView.position;
        Quaternion closeRotation = playerView.rotation;

        float returnDuration =
            Mathf.Max(0.05f, policeFrontRevealReturnSeconds);

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / returnDuration));

            playerView.SetPositionAndRotation(
                Vector3.Lerp(
                    closePosition,
                    originalPosition,
                    t),
                Quaternion.Slerp(
                    closeRotation,
                    originalRotation,
                    t));

            yield return null;
        }

        playerView.localPosition = originalLocalPosition;
        playerView.localRotation = originalLocalRotation;

        RestoreCameraTrackedPoseDrivers(trackedPoseDrivers);
    }

    private IEnumerator CinematicExtraHold()
    {
        if (!cinematicStoryPlaying)
        {
            yield break;
        }

        float seconds = Mathf.Max(0f, cinematicExtraShotHoldSeconds);
        if (seconds > 0f)
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private IEnumerator CinematicPanTo(Transform target, float seconds)
    {
        if (!usePoliceCinematicCamera || target == null)
        {
            yield break;
        }

        Transform root = GetDancePlayerRoot();
        Transform view = GetPlayerViewTransform();
        if (root == null || view == null)
        {
            yield break;
        }

        Vector3 focusPosition = target.position + Vector3.up * policeCameraLookHeight;
        Vector3 desiredViewDirection = focusPosition - view.position;
        desiredViewDirection.y = 0f;

        Vector3 currentViewDirection = view.forward;
        currentViewDirection.y = 0f;

        if (desiredViewDirection.sqrMagnitude < 0.001f || currentViewDirection.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        Quaternion currentViewYaw = Quaternion.LookRotation(currentViewDirection.normalized, Vector3.up);
        Quaternion desiredViewYaw = Quaternion.LookRotation(desiredViewDirection.normalized, Vector3.up);
        Quaternion yawDelta = desiredViewYaw * Quaternion.Inverse(currentViewYaw);

        Quaternion startRotation = root.rotation;
        Quaternion targetRotation = yawDelta * startRotation;

        float duration = Mathf.Max(0.05f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            root.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        root.rotation = targetRotation;

        if (policeCameraHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(policeCameraHoldSeconds);
        }
    }

    private bool HasAnimatorState(Transform actor, string stateName)
    {
        if (actor == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    private void PlayPoliceWalkAnimation(
        Transform actor,
        float normalizedTime,
        float animatorSpeed)
    {
        if (actor == null || string.IsNullOrWhiteSpace(policeWalkStateName))
        {
            return;
        }

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        int stateHash = Animator.StringToHash(policeWalkStateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.speed = Mathf.Max(0.25f, animatorSpeed);
        animator.Play(stateHash, 0, Mathf.Repeat(normalizedTime, 1f));
        animator.Update(0f);
    }

    private void ResetPoliceAnimatorSpeed(Transform actor)
    {
        if (actor == null)
        {
            return;
        }

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    private Vector3 GetPoliceAerialCameraPosition(
        Vector3 policeCenter,
        Vector3 pathDirection,
        Vector3 sideDirection)
    {
        return policeCenter
            - pathDirection * Mathf.Max(0.5f, policeAerialBackOffset)
            + sideDirection * policeAerialSideOffset
            + Vector3.up * Mathf.Max(2f, policeAerialHeight);
    }

    private IEnumerator AnimatePoliceEntranceFallback()
    {
        EnsurePoliceActorsForIntrusion();

        Transform firstPolice =
            primaryPoliceActor != null
                ? primaryPoliceActor
                : (policeGroup != null
                    ? policeGroup.transform
                    : null);

        if (firstPolice == null)
        {
            yield return new WaitForSeconds(3f);
            yield break;
        }

        Transform secondPolice = secondaryPoliceActor;

        Vector3 endPosition =
            policeEntranceTarget != null
                ? policeEntranceTarget.position
                : (hasPoliceOriginalTransform
                    ? policeOriginalPosition
                    : firstPolice.position);

        Quaternion endRotation =
            hasPoliceOriginalTransform
                ? policeOriginalRotation
                : firstPolice.rotation;

        Vector3 focusPosition =
            GetEntranceFocusPosition(endPosition);

        Vector3 awayFromFocus =
            endPosition - focusPosition;

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

        Vector3 startPosition =
            endPosition
            + awayFromFocus.normalized
                * Mathf.Max(1f, policeEntranceDistance);

        Vector3 pathDirection =
            endPosition - startPosition;

        pathDirection.y = 0f;

        Vector3 normalizedPath =
            pathDirection.sqrMagnitude > 0.01f
                ? pathDirection.normalized
                : Vector3.forward;

        Vector3 sideDirection =
            Vector3.Cross(
                Vector3.up,
                normalizedPath);

        if (sideDirection.sqrMagnitude < 0.01f)
        {
            sideDirection = Vector3.right;
        }

        sideDirection.Normalize();

        float halfSpacing =
            Mathf.Max(0.15f, policePairSpacing)
                * 0.5f;

        Vector3 firstStart =
            startPosition
            - sideDirection * halfSpacing;

        Vector3 firstEnd =
            endPosition
            - sideDirection * halfSpacing;

        Vector3 secondStart =
            startPosition
            + sideDirection * halfSpacing;

        Vector3 secondEnd =
            endPosition
            + sideDirection * halfSpacing;

        SetActiveIncludingParents(firstPolice);
        firstPolice.position = firstStart;

        if (secondPolice != null)
        {
            SetActiveIncludingParents(secondPolice);
            secondPolice.position = secondStart;
        }

        Quaternion walkingRotation = endRotation;

        if (rotatePoliceTowardPath
            && pathDirection.sqrMagnitude > 0.01f)
        {
            walkingRotation =
                Quaternion.LookRotation(
                    normalizedPath,
                    Vector3.up);

            firstPolice.rotation = walkingRotation;

            if (secondPolice != null)
            {
                secondPolice.rotation = walkingRotation;
            }
        }

        bool firstHasWalk =
            HasAnimatorState(firstPolice, policeWalkStateName);

        bool secondHasWalk =
            secondPolice == null
                || HasAnimatorState(secondPolice, policeWalkStateName);

        bool useAnimatorWalk =
            preferAnimatorWalkForPoliceEntrance
            && firstHasWalk
            && secondHasWalk;

        if (useAnimatorWalk)
        {
            // 兩名警察故意用不同的動畫相位與速度，避免完全同步。
            PlayPoliceWalkAnimation(
                firstPolice,
                0f,
                policeFirstWalkAnimatorSpeed);

            if (secondPolice != null)
            {
                PlayPoliceWalkAnimation(
                    secondPolice,
                    policeSecondWalkAnimationPhase,
                    policeSecondWalkAnimatorSpeed);
            }
        }
        else
        {
            // 沒有真正 Walk 動畫時才用程序步態，避免角色只是平移。
            PlayAnimatorStateIfAvailable(firstPolice, policeWalkStateName);

            if (secondPolice != null)
            {
                PlayAnimatorStateIfAvailable(secondPolice, policeWalkStateName);
            }
        }

        Chapter1PoliceRunAnimator firstRun = null;
        Chapter1PoliceRunAnimator secondRun = null;

        if (!useAnimatorWalk
            && useProceduralPoliceWalkFallback)
        {
            float cachedCadence = policeRunCadence;
            float cachedLegSwing = policeRunLegSwing;
            float cachedKneeBend = policeRunKneeBend;
            float cachedArmSwing = policeRunArmSwing;
            float cachedLean = policeRunForwardLeanDegrees;

            policeRunCadence = policeWalkCadence;
            policeRunLegSwing = policeWalkLegSwing;
            policeRunKneeBend = policeWalkKneeBend;
            policeRunArmSwing = policeWalkArmSwing;
            policeRunForwardLeanDegrees = policeWalkForwardLeanDegrees;

            firstRun = BeginPoliceRiggedRun(firstPolice, 0f);
            secondRun = BeginPoliceRiggedRun(secondPolice, Mathf.PI * 0.65f);

            policeRunCadence = cachedCadence;
            policeRunLegSwing = cachedLegSwing;
            policeRunKneeBend = cachedKneeBend;
            policeRunArmSwing = cachedArmSwing;
            policeRunForwardLeanDegrees = cachedLean;
        }

        float requestedDuration =
            IsNewPoliceScene()
                ? newPoliceSceneRunDuration
                : policeEntranceDuration;

        // 走步比跑步要更穩、更慢。
        float duration =
            Mathf.Max(1.8f, requestedDuration + 0.55f);

        float stagger =
            secondPolice != null
                ? Mathf.Max(0.12f, policeEntranceStaggerSeconds)
                : 0f;

        float totalDuration = duration + stagger;
        float elapsed = 0f;

        Transform playerView = GetPlayerViewTransform();

        bool aerialCamera =
            usePoliceCinematicCamera
            && usePoliceEntranceAerialIntro
            && playerView != null;

        bool trackingCamera =
            usePoliceCinematicCamera
            && !usePoliceEntranceAerialIntro
            && usePoliceEntranceTrackingCamera
            && playerView != null;

        Vector3 originalViewPosition =
            playerView != null ? playerView.position : Vector3.zero;

        Quaternion originalViewRotation =
            playerView != null ? playerView.rotation : Quaternion.identity;

        Vector3 originalViewLocalPosition =
            playerView != null ? playerView.localPosition : Vector3.zero;

        Quaternion originalViewLocalRotation =
            playerView != null ? playerView.localRotation : Quaternion.identity;

        List<Behaviour> trackedPoseDrivers = null;

        if ((aerialCamera || trackingCamera)
            && playerView != null)
        {
            trackedPoseDrivers =
                DisableCameraTrackedPoseDrivers(playerView);
        }

        ShowLine(
            "旁白",
            "遠處的山路上，兩名日警正朝著婚禮會場緩步逼近。",
            totalDuration);

        Vector3 aerialAnchorCenter =
            secondPolice != null
                ? (firstStart + secondStart) * 0.5f
                : firstStart;

        Vector3 aerialStartPosition =
            GetPoliceAerialCameraPosition(
                aerialAnchorCenter,
                normalizedPath,
                sideDirection);

        Vector3 aerialStartLookPoint =
            aerialAnchorCenter
            + Vector3.up * Mathf.Max(0.5f, policeAerialLookHeight);

        Quaternion aerialStartRotation =
            Quaternion.LookRotation(
                (aerialStartLookPoint - aerialStartPosition).normalized,
                Vector3.up);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            float firstProgress =
                Mathf.Clamp01(elapsed / duration);

            float secondProgress =
                secondPolice != null
                    ? Mathf.Clamp01(
                        (elapsed - stagger)
                        / (duration / Mathf.Max(0.6f, policeSecondarySpeedScale)))
                    : 0f;

            // 走步不要完全一模一樣：加非常小的左右開闔與相位差。
            float firstDrift =
                Mathf.Sin(
                    elapsed * Mathf.PI * Mathf.Max(0.6f, policeWalkCadence))
                * policeNaturalLateralDrift;

            float secondDrift =
                Mathf.Sin(
                    elapsed * Mathf.PI * Mathf.Max(0.6f, policeWalkCadence)
                    + Mathf.PI * 0.65f)
                * policeNaturalLateralDrift;

            Vector3 firstBase =
                Vector3.Lerp(
                    firstStart,
                    firstEnd,
                    Mathf.SmoothStep(0f, 1f, firstProgress));

            firstPolice.position =
                firstBase + sideDirection * firstDrift;

            if (secondPolice != null)
            {
                Vector3 secondBase =
                    Vector3.Lerp(
                        secondStart,
                        secondEnd,
                        Mathf.SmoothStep(0f, 1f, secondProgress));

                secondPolice.position =
                    secondBase - sideDirection * secondDrift;
            }

            if (firstPolice != null)
            {
                firstPolice.rotation =
                    Quaternion.Slerp(
                        firstPolice.rotation,
                        walkingRotation,
                        Time.deltaTime * 8f);
            }

            if (secondPolice != null)
            {
                secondPolice.rotation =
                    Quaternion.Slerp(
                        secondPolice.rotation,
                        walkingRotation,
                        Time.deltaTime * 8f);
            }

            if ((aerialCamera || trackingCamera)
                && playerView != null)
            {
                Vector3 policeCenter =
                    secondPolice != null
                        ? (firstPolice.position + secondPolice.position) * 0.5f
                        : firstPolice.position;

                Vector3 desiredCameraPosition;
                Vector3 lookPoint;

                if (aerialCamera)
                {
                    // 從天空慢慢看著警察進場，鏡頭只微微跟隨，不要太晃。
                    Vector3 followOffset =
                        (policeCenter - aerialAnchorCenter)
                        * Mathf.Clamp01(policeAerialFollowStrength);

                    float descentT = 0f;
                    if (usePoliceAerialDescent)
                    {
                        float startT =
                            Mathf.Clamp(
                                policeAerialDescentStartNormalized,
                                0f,
                                0.9f);

                        descentT =
                            Mathf.SmoothStep(
                                0f,
                                1f,
                                Mathf.Clamp01(
                                    (firstProgress - startT)
                                    / Mathf.Max(0.01f, 1f - startT)));
                    }

                    float currentAerialHeight =
                        Mathf.Lerp(
                            policeAerialHeight,
                            Mathf.Max(2f, policeAerialEndHeight),
                            descentT);

                    float currentAerialBackOffset =
                        Mathf.Lerp(
                            policeAerialBackOffset,
                            Mathf.Max(0.5f, policeAerialEndBackOffset),
                            descentT);

                    desiredCameraPosition =
                        aerialAnchorCenter
                        + followOffset
                        - normalizedPath * currentAerialBackOffset
                        + sideDirection * policeAerialSideOffset
                        + Vector3.up * currentAerialHeight;

                    lookPoint =
                        policeCenter
                        + Vector3.up * Mathf.Max(0.5f, policeAerialLookHeight);

                    float blend =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                elapsed / Mathf.Max(0.05f, policeAerialBlendSeconds)));

                    Quaternion desiredRotation =
                        Quaternion.LookRotation(
                            (lookPoint - desiredCameraPosition).normalized,
                            Vector3.up);

                    playerView.SetPositionAndRotation(
                        Vector3.Lerp(
                            originalViewPosition,
                            desiredCameraPosition,
                            blend),
                        Quaternion.Slerp(
                            originalViewRotation,
                            desiredRotation,
                            blend));
                }
                else
                {
                    desiredCameraPosition =
                        policeCenter
                        - normalizedPath * Mathf.Max(0.5f, policeTrackingBackOffset)
                        + sideDirection * policeTrackingSideOffset
                        + Vector3.up * policeTrackingHeight;

                    lookPoint = policeCenter + Vector3.up * 1.25f;

                    Vector3 lookDirection = lookPoint - desiredCameraPosition;

                    Quaternion desiredCameraRotation =
                        lookDirection.sqrMagnitude > 0.001f
                            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                            : playerView.rotation;

                    float blend =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                elapsed / Mathf.Max(0.05f, policeTrackingBlendSeconds)));

                    playerView.SetPositionAndRotation(
                        Vector3.Lerp(originalViewPosition, desiredCameraPosition, blend),
                        Quaternion.Slerp(originalViewRotation, desiredCameraRotation, blend));
                }
            }

            yield return null;
        }

        firstPolice.position = firstEnd;
        firstPolice.rotation = endRotation;

        EndPoliceRiggedRun(firstRun);
        ResetPoliceAnimatorSpeed(firstPolice);
        PlayAnimatorStateIfAvailable(firstPolice, policeIdleStateName);

        if (secondPolice != null)
        {
            secondPolice.position = secondEnd;
            secondPolice.rotation = endRotation;

            EndPoliceRiggedRun(secondRun);
            ResetPoliceAnimatorSpeed(secondPolice);
            PlayAnimatorStateIfAvailable(secondPolice, policeIdleStateName);
        }

        if ((aerialCamera || trackingCamera)
            && playerView != null)
        {
            if (returnCameraAfterPoliceEntrance)
            {
                Vector3 endTrackedPosition = playerView.position;
                Quaternion endTrackedRotation = playerView.rotation;

                float returnDuration =
                    Mathf.Max(0.05f, policeTrackingReturnSeconds);

                float returnElapsed = 0f;

                while (returnElapsed < returnDuration)
                {
                    returnElapsed += Time.deltaTime;

                    float t =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(returnElapsed / returnDuration));

                    playerView.SetPositionAndRotation(
                        Vector3.Lerp(endTrackedPosition, originalViewPosition, t),
                        Quaternion.Slerp(endTrackedRotation, originalViewRotation, t));

                    yield return null;
                }
            }

            playerView.localPosition = originalViewLocalPosition;
            playerView.localRotation = originalViewLocalRotation;

            RestoreCameraTrackedPoseDrivers(trackedPoseDrivers);
        }
    }

    private Chapter1PoliceRunAnimator BeginPoliceRiggedRun(Transform actor, float phaseOffset)
    {
        if (!animatePoliceRunBones || actor == null)
        {
            return null;
        }

        Animator animator = actor.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return null;
        }

        Chapter1PoliceRunAnimator runAnimator = animator.GetComponent<Chapter1PoliceRunAnimator>();
        if (runAnimator == null)
        {
            runAnimator = animator.gameObject.AddComponent<Chapter1PoliceRunAnimator>();
        }

        bool configured = runAnimator.Configure(
            animator,
            policeRunCadence,
            policeRunLegSwing,
            policeRunKneeBend,
            policeRunArmSwing,
            policeRunForwardLeanDegrees);

        if (!configured || !runAnimator.BeginRun(phaseOffset))
        {
            return null;
        }

        return runAnimator;
    }

    private static void EndPoliceRiggedRun(Chapter1PoliceRunAnimator runAnimator)
    {
        if (runAnimator != null)
        {
            runAnimator.EndRun();
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
        // 劇情自動播到這裡才停下，讓玩家做一次真正的選擇。
        cinematicStoryPlaying = false;
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

        // 玩家選完後，後續分支與結尾繼續自動播放。
        if (autoPlayCinematicStoryAfterWeddingTasks)
        {
            cinematicStoryPlaying = true;
        }

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
            yield return PlayPoliceVoicedLine(
                "你",
                "夠了！不要再羞辱我們！",
                playerInterveneVoice,
                2.8f);

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
        cinematicStoryPlaying = false;
    }

    private IEnumerator CinematicCupCloseUpAndShoveVillagerFallback()
    {
        if (ceremonyCup == null)
        {
            yield return KnockCupAndShoveVillagerFallback();
            yield break;
        }

        Transform playerView =
            GetPlayerViewTransform();

        if (playerView == null
            || !usePoliceCinematicCamera)
        {
            yield return KnockCupAndShoveVillagerFallback();
            yield break;
        }

        Vector3 originalViewPosition =
            playerView.position;

        Quaternion originalViewRotation =
            playerView.rotation;

        Vector3 originalViewLocalPosition =
            playerView.localPosition;

        Quaternion originalViewLocalRotation =
            playerView.localRotation;

        List<Behaviour> trackedPoseDrivers =
            DisableCameraTrackedPoseDrivers(
                playerView);

        Vector3 cupPosition =
            ceremonyCup.position;

        // 優先從玩家原本所在方向看酒杯，
        // 避免突然跳到桌子另一側。
        Vector3 cameraSide =
            originalViewPosition
            - cupPosition;

        cameraSide.y = 0f;

        if (cameraSide.sqrMagnitude < 0.01f
            && primaryPoliceActor != null)
        {
            cameraSide =
                -primaryPoliceActor.forward;

            cameraSide.y = 0f;
        }

        if (cameraSide.sqrMagnitude < 0.01f)
        {
            cameraSide =
                Vector3.back;
        }

        cameraSide.Normalize();

        Vector3 closeUpPosition =
            cupPosition
            + cameraSide
                * Mathf.Max(
                    0.25f,
                    cupCloseUpDistance)
            + Vector3.up
                * cupCloseUpHeight;

        Vector3 lookPoint =
            cupPosition
            + Vector3.up * 0.04f;

        Vector3 lookDirection =
            lookPoint
            - closeUpPosition;

        Quaternion closeUpRotation =
            lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up)
                : originalViewRotation;

        // 切進酒杯特寫。
        float moveDuration =
            Mathf.Max(
                0.05f,
                cupCloseUpMoveSeconds);

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / moveDuration));

            playerView.SetPositionAndRotation(
                Vector3.Lerp(
                    originalViewPosition,
                    closeUpPosition,
                    t),
                Quaternion.Slerp(
                    originalViewRotation,
                    closeUpRotation,
                    t));

            yield return null;
        }

        playerView.SetPositionAndRotation(
            closeUpPosition,
            closeUpRotation);

        ShowLine(
            "旁白",
            "日警的手掃向桌邊。",
            Mathf.Max(
                0.8f,
                cupCloseUpBeforeImpactSeconds
                + 0.55f));

        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                cupCloseUpBeforeImpactSeconds));

        // 酒杯就在鏡頭前被掃落。
        PlayPoliceEventClip(cupCrashClip);

        if (ceremonyCupRigidbody != null)
        {
            ceremonyCupRigidbody.isKinematic =
                false;

            ceremonyCupRigidbody.useGravity =
                true;

            Vector3 forceDirection =
                primaryPoliceActor != null
                    ? primaryPoliceActor.forward
                    : Vector3.forward;

            ceremonyCupRigidbody.AddForce(
                forceDirection * 1.6f
                + Vector3.up * 0.55f,
                ForceMode.Impulse);
        }
        else
        {
            ceremonyCup.Rotate(
                Vector3.forward,
                78f,
                Space.Self);
        }

        // 酒杯落下後繼續維持特寫一小段時間。
        float impactHold =
            Mathf.Max(
                0.1f,
                cupCloseUpAfterImpactSeconds);

        elapsed = 0f;

        while (elapsed < impactHold)
        {
            elapsed += Time.deltaTime;

            Vector3 dynamicLookPoint =
                ceremonyCup != null
                    ? ceremonyCup.position
                    : lookPoint;

            Vector3 dynamicDirection =
                dynamicLookPoint
                - playerView.position;

            if (dynamicDirection.sqrMagnitude
                > 0.001f)
            {
                playerView.rotation =
                    Quaternion.LookRotation(
                        dynamicDirection.normalized,
                        Vector3.up);
            }

            yield return null;
        }

        // 回到原鏡頭。
        Vector3 closeEndPosition =
            playerView.position;

        Quaternion closeEndRotation =
            playerView.rotation;

        float returnDuration =
            Mathf.Max(
                0.05f,
                cupCloseUpReturnSeconds);

        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / returnDuration));

            playerView.SetPositionAndRotation(
                Vector3.Lerp(
                    closeEndPosition,
                    originalViewPosition,
                    t),
                Quaternion.Slerp(
                    closeEndRotation,
                    originalViewRotation,
                    t));

            yield return null;
        }

        playerView.localPosition =
            originalViewLocalPosition;

        playerView.localRotation =
            originalViewLocalRotation;

        RestoreCameraTrackedPoseDrivers(
            trackedPoseDrivers);

        // 酒杯特寫結束，立刻帶到被推開的族人。
        if (shovedVillagerActor != null)
        {
            yield return CinematicPanTo(
                shovedVillagerActor,
                policeCameraPanSeconds * 0.65f);
        }

        ShowLine(
            "旁白",
            "一名族人上前質問，立刻被粗暴地推開。",
            2.8f);

        if (shovedVillagerActor != null)
        {
            Vector3 away =
                shovedVillagerActor.position
                - (primaryPoliceActor != null
                    ? primaryPoliceActor.position
                    : GetFireCenterPosition());

            away.y = 0f;

            if (away.sqrMagnitude < 0.01f)
            {
                away =
                    -shovedVillagerActor.forward;
            }

            Vector3 destination =
                shovedVillagerActor.position
                + away.normalized
                    * Mathf.Max(
                        0.3f,
                        shoveDistance);

            yield return MoveTransform(
                shovedVillagerActor,
                destination,
                0.45f);
        }
        else
        {
            yield return new WaitForSeconds(
                0.7f);
        }
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
        yield return PlayPoliceVoicedLine(
            "日警",
            "都給我安靜。你們最好記住自己的身分。",
            policeWatchCommandVoice,
            3.2f);

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
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
    }

    private void EnsurePoliceDialogueAudioSource()
    {
        if (policeDialogueAudio == null)
        {
            policeDialogueAudio = gameObject.AddComponent<AudioSource>();
        }

        policeDialogueAudio.playOnAwake = false;
        policeDialogueAudio.loop = false;

        // 過場對話先使用 2D，確保玩家不會因角色距離而聽不到。
        policeDialogueAudio.spatialBlend = 0f;
        policeDialogueAudio.volume = Mathf.Clamp01(policeDialogueVolume);
    }

    private IEnumerator PlayPoliceVoicedLine(
        string speaker,
        string line,
        AudioClip voiceClip,
        float fallbackSeconds,
        float volumeScale = 1f)
    {
        float duration = voiceClip != null
            ? Mathf.Max(0.1f, voiceClip.length)
            : Mathf.Max(0.1f, fallbackSeconds);

        ShowLine(speaker, line, duration + 0.1f);

        if (voiceClip == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        EnsurePoliceDialogueAudioSource();

        if (policeDialogueAudio == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        policeDialogueAudio.Stop();
        policeDialogueAudio.clip = null;
        policeDialogueAudio.volume = Mathf.Clamp01(policeDialogueVolume);
        policeDialogueAudio.PlayOneShot(voiceClip, Mathf.Max(0f, volumeScale));

        while (policeDialogueAudio != null && policeDialogueAudio.isPlaying)
        {
            yield return null;
        }
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
        if (!IsFreeExplorationActive() || danceFinished || danceRoutineRunning)
        {
            return false;
        }

        if (requireWineAndFoodBeforeDance && !AreWineAndFoodTasksComplete())
        {
            return false;
        }

        return true;
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

        // 正式遊戲版：關掉跟著鏡頭的巨大 3D TextMesh 提示。
        // 互動提示統一放在螢幕底部的小型 HUD。
        showWorldInteractionPrompt = false;
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

    private bool IsNewPoliceScene()
    {
        return gameObject.scene.IsValid()
            && string.Equals(gameObject.scene.name, "第一章新版警察", System.StringComparison.Ordinal);
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

        Texture2D background =
            MakeTexture(new Color(0.025f, 0.025f, 0.025f, 0.68f));

        hudBoxStyle = new GUIStyle(GUI.skin.box);
        hudBoxStyle.normal.background = background;
        hudBoxStyle.padding = new RectOffset(16, 16, 10, 10);

        hudTitleStyle = new GUIStyle(GUI.skin.label);
        hudTitleStyle.normal.textColor = new Color(1f, 0.86f, 0.52f, 1f);
        hudTitleStyle.fontSize = Mathf.Clamp(Screen.height / 46, 16, 22);
        hudTitleStyle.fontStyle = FontStyle.Bold;
        hudTitleStyle.wordWrap = true;

        hudBodyStyle = new GUIStyle(GUI.skin.label);
        hudBodyStyle.normal.textColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        hudBodyStyle.fontSize = Mathf.Clamp(Screen.height / 58, 14, 19);
        hudBodyStyle.wordWrap = true;

        hudButtonStyle = new GUIStyle(GUI.skin.button);
        hudButtonStyle.fontSize = Mathf.Clamp(Screen.height / 50, 16, 21);
        hudButtonStyle.alignment = TextAnchor.MiddleLeft;
        hudButtonStyle.padding = new RectOffset(18, 18, 8, 8);
        hudButtonStyle.wordWrap = true;

        timerBoxStyle = new GUIStyle(GUI.skin.box);
        timerBoxStyle.normal.background = background;
        timerBoxStyle.padding = new RectOffset(14, 14, 10, 10);

        timerTitleStyle = new GUIStyle(GUI.skin.label);
        timerTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.68f, 1f);
        timerTitleStyle.fontSize = Mathf.Clamp(Screen.height / 60, 13, 17);
        timerTitleStyle.fontStyle = FontStyle.Bold;
        timerTitleStyle.alignment = TextAnchor.MiddleRight;

        timerNumberStyle = new GUIStyle(GUI.skin.label);
        timerNumberStyle.normal.textColor = Color.white;
        timerNumberStyle.fontSize = Mathf.Clamp(Screen.height / 38, 22, 30);
        timerNumberStyle.fontStyle = FontStyle.Bold;
        timerNumberStyle.alignment = TextAnchor.MiddleRight;

        centerMenuStyle = new GUIStyle(GUI.skin.box);
        centerMenuStyle.normal.background = background;
        centerMenuStyle.normal.textColor = Color.white;
        centerMenuStyle.fontSize = Mathf.Clamp(Screen.height / 50, 15, 21);
        centerMenuStyle.fontStyle = FontStyle.Bold;
        centerMenuStyle.alignment = TextAnchor.MiddleCenter;
        centerMenuStyle.padding = new RectOffset(16, 16, 8, 8);
        centerMenuStyle.wordWrap = false;

        winePickupIndicatorStyle = new GUIStyle(GUI.skin.box);
        winePickupIndicatorStyle.normal.background =
            MakeTexture(new Color(0.5f, 0.1f, 0.025f, 0.88f));
        winePickupIndicatorStyle.normal.textColor = Color.white;
        winePickupIndicatorStyle.fontSize = Mathf.Clamp(Screen.height / 55, 14, 19);
        winePickupIndicatorStyle.fontStyle = FontStyle.Bold;
        winePickupIndicatorStyle.alignment = TextAnchor.MiddleCenter;
        winePickupIndicatorStyle.padding = new RectOffset(8, 8, 5, 5);
        winePickupIndicatorStyle.wordWrap = true;

        foodPickupIndicatorStyle = new GUIStyle(GUI.skin.box);
        foodPickupIndicatorStyle.normal.background =
            MakeTexture(new Color(0.27f, 0.42f, 0.035f, 0.88f));
        foodPickupIndicatorStyle.normal.textColor = Color.white;
        foodPickupIndicatorStyle.fontSize = Mathf.Clamp(Screen.height / 55, 14, 19);
        foodPickupIndicatorStyle.fontStyle = FontStyle.Bold;
        foodPickupIndicatorStyle.alignment = TextAnchor.MiddleCenter;
        foodPickupIndicatorStyle.padding = new RectOffset(8, 8, 5, 5);
        foodPickupIndicatorStyle.wordWrap = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}

/// <summary>
/// 在 Animator 完成當幀骨架更新後，再疊加自然感謝姿勢。
/// 重點：用 LateUpdate，而不是 WaitForEndOfFrame。
/// </summary>
public class Chapter1ReceiverNaturalGestureDriver : MonoBehaviour
{
    private Animator animator;
    private Transform actorRoot;

    private Transform chest;
    private Transform head;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftForearm;
    private Transform rightForearm;

    private float duration;
    private float elapsed;
    private float chestBowDegrees;
    private float headNodDegrees;
    private float armForwardDegrees;
    private float forearmDegrees;

    private bool playing;

    public bool PlayGesture(
        Animator sourceAnimator,
        Transform sourceActorRoot,
        float seconds,
        float chestBow,
        float headNod,
        float armForward,
        float forearmBend)
    {
        animator = sourceAnimator;
        actorRoot = sourceActorRoot;

        if (animator == null
            || animator.avatar == null
            || !animator.avatar.isValid
            || !animator.isHuman)
        {
            playing = false;
            return false;
        }

        chest =
            animator.GetBoneTransform(
                HumanBodyBones.UpperChest);

        if (chest == null)
        {
            chest =
                animator.GetBoneTransform(
                    HumanBodyBones.Chest);
        }

        head =
            animator.GetBoneTransform(
                HumanBodyBones.Head);

        leftUpperArm =
            animator.GetBoneTransform(
                HumanBodyBones.LeftUpperArm);

        rightUpperArm =
            animator.GetBoneTransform(
                HumanBodyBones.RightUpperArm);

        leftForearm =
            animator.GetBoneTransform(
                HumanBodyBones.LeftLowerArm);

        rightForearm =
            animator.GetBoneTransform(
                HumanBodyBones.RightLowerArm);

        if (chest == null
            && head == null
            && leftUpperArm == null
            && rightUpperArm == null
            && leftForearm == null
            && rightForearm == null)
        {
            playing = false;
            return false;
        }

        duration = Mathf.Max(0.5f, seconds);
        elapsed = 0f;

        chestBowDegrees = chestBow;
        headNodDegrees = headNod;
        armForwardDegrees = armForward;
        forearmDegrees = forearmBend;

        playing = true;
        enabled = true;

        return true;
    }

    private void LateUpdate()
    {
        if (!playing)
        {
            return;
        }

        elapsed += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                elapsed / duration);

        // 一個自然的「收到 → 致意 → 回到放鬆」包絡。
        float envelope =
            Mathf.Sin(t * Mathf.PI);

        // 一次主要點頭，加一個很小的二次回應。
        float nod =
            Mathf.Max(
                0f,
                Mathf.Sin(t * Mathf.PI * 1.8f))
            * envelope;

        Vector3 rightAxis =
            actorRoot != null
                ? Vector3.ProjectOnPlane(
                    actorRoot.right,
                    Vector3.up)
                : Vector3.right;

        Vector3 forwardAxis =
            actorRoot != null
                ? Vector3.ProjectOnPlane(
                    actorRoot.forward,
                    Vector3.up)
                : Vector3.forward;

        if (rightAxis.sqrMagnitude < 0.001f)
        {
            rightAxis = Vector3.right;
        }
        else
        {
            rightAxis.Normalize();
        }

        if (forwardAxis.sqrMagnitude < 0.001f)
        {
            forwardAxis = Vector3.forward;
        }
        else
        {
            forwardAxis.Normalize();
        }

        // 胸口只前傾一點，不整個人跳。
        if (chest != null)
        {
            chest.rotation =
                Quaternion.AngleAxis(
                    chestBowDegrees * envelope,
                    rightAxis)
                * chest.rotation;
        }

        // 頭部自然點一下。
        if (head != null)
        {
            head.rotation =
                Quaternion.AngleAxis(
                    headNodDegrees * nod,
                    rightAxis)
                * head.rotation;
        }

        // 雙臂稍微往前，像收到食物後自然回應。
        float armAmount =
            armForwardDegrees * envelope;

        if (leftUpperArm != null)
        {
            leftUpperArm.rotation =
                Quaternion.AngleAxis(
                    -armAmount,
                    rightAxis)
                * leftUpperArm.rotation;

            leftUpperArm.rotation =
                Quaternion.AngleAxis(
                    -3f * envelope,
                    forwardAxis)
                * leftUpperArm.rotation;
        }

        if (rightUpperArm != null)
        {
            rightUpperArm.rotation =
                Quaternion.AngleAxis(
                    -armAmount,
                    rightAxis)
                * rightUpperArm.rotation;

            rightUpperArm.rotation =
                Quaternion.AngleAxis(
                    3f * envelope,
                    forwardAxis)
                * rightUpperArm.rotation;
        }

        float forearmAmount =
            forearmDegrees * envelope;

        if (leftForearm != null)
        {
            leftForearm.rotation =
                Quaternion.AngleAxis(
                    -forearmAmount,
                    rightAxis)
                * leftForearm.rotation;
        }

        if (rightForearm != null)
        {
            rightForearm.rotation =
                Quaternion.AngleAxis(
                    -forearmAmount,
                    rightAxis)
                * rightForearm.rotation;
        }

        if (elapsed >= duration)
        {
            playing = false;
        }
    }
}

/// <summary>
/// 避免 NPC 已經轉向玩家後，又被其他 Update / LateUpdate 腳本轉回火堆或原方向。
/// </summary>
public class Chapter1ReceiverFacingLock : MonoBehaviour
{
    private Quaternion targetRotation;
    private float untilTime;
    private bool locked;

    public void LockFacing(
        Quaternion rotation,
        float seconds)
    {
        targetRotation = rotation;
        untilTime = Time.time + Mathf.Max(0.1f, seconds);
        locked = true;
        enabled = true;
    }

    public void UpdateTargetRotation(
        Quaternion rotation)
    {
        targetRotation = rotation;
    }

    private void LateUpdate()
    {
        if (!locked)
        {
            return;
        }

        if (Time.time >= untilTime)
        {
            locked = false;
            return;
        }

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 18f);
    }
}

