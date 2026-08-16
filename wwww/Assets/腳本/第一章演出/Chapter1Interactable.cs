using UnityEngine;

public class Chapter1Interactable : MonoBehaviour
{
    public enum InteractionType
    {
        Talk,
        DeliverWine,
        ShareFood,
        JoinDance,
        StartPoliceSequence,
        BeginChapter,
        MissionHint
    }

    [Header("References")]
    public Chapter1PerformanceController controller;
    public Transform playerRoot;

    [Header("Interaction")]
    public InteractionType interactionType = InteractionType.Talk;
    public KeyCode interactKey = KeyCode.F;
    public bool alsoUseEKey = true;
    public bool autoFindController = true;
    public bool disableAfterUse = false;
    public bool hidePromptAfterUse = false;

    [Header("Prompt")]
    public bool showPrompt = true;
    public string promptText = "\u6309 E / \u624b\u628a\u89f8\u767c \u52a0\u5165\u821e\u8e48";
    public bool restrictDancePromptToFreeExploration = true;
    public float dancePromptRange = 4.5f;
    public float promptMaxWidth = 380f;
    public float promptHeight = 46f;
    public float promptBottomMargin = 36f;

    [Header("XR Fallback Detection")]
    public bool useDistanceCheck = true;
    public float interactRange = 4f;
    public string playerTag = "Player";

    [Header("Dialogue")]
    public string speakerName = "族人";
    [TextArea(2, 4)]
    public string line = "願祖靈庇佑新人。";
    [TextArea(2, 4)]
    public string missionHint = "自由探索婚禮：與族人交談、幫新郎送酒，或靠近舞圈加入舞蹈。";
    public bool rotateDialogueLines = true;
    [TextArea(2, 4)]
    public string[] extraLines =
    {
        "今晚一定要喝醉！",
        "願祖靈庇佑新人。",
        "火堆亮起來，祖靈會看見我們的歌聲。"
    };

    private bool playerInside;
    private bool used;
    private Transform cachedPlayer;
    private bool promptVisible;
    private GUIStyle promptStyle;
    private int extraLineIndex;

    private void Reset()
    {
        Collider hitbox = GetComponent<Collider>();
        if (hitbox != null)
        {
            hitbox.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (controller == null && autoFindController)
        {
            controller = FindObjectOfType<Chapter1PerformanceController>(true);
        }

        if (playerRoot != null)
        {
            cachedPlayer = playerRoot;
        }
    }

    private void Update()
    {
        if (used || controller == null)
        {
            promptVisible = false;
            return;
        }

        bool pressedInspectorKey = Input.GetKeyDown(interactKey);
        bool pressedEKey = alsoUseEKey && Input.GetKeyDown(KeyCode.E);
        if (ShouldLetCenterMenuUseE())
        {
            pressedEKey = false;
            if (interactKey == KeyCode.E)
            {
                pressedInspectorKey = false;
            }
        }

        bool canInteract = CanInteractNow();
        promptVisible = showPrompt && canInteract;

        if ((pressedInspectorKey || pressedEKey) && canInteract)
        {
            Execute();
        }
    }

    private bool CanInteractNow()
    {
        if (!PassesStoryGate())
        {
            return false;
        }

        bool mustCheckDistance = useDistanceCheck || IsDanceInteraction();

        if (playerInside && !mustCheckDistance)
        {
            return true;
        }

        if (!mustCheckDistance)
        {
            return false;
        }

        if (!TryGetPlayerPosition(out Vector3 playerPosition))
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, playerPosition);
        return distance <= GetEffectiveInteractRange();
    }

    private bool TryGetPlayerPosition(out Vector3 position)
    {
        if (Camera.main != null)
        {
            position = Camera.main.transform.position;
            return true;
        }

        Transform player = GetPlayerTransform();
        if (player != null)
        {
            position = player.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private Transform GetPlayerTransform()
    {
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            cachedPlayer = playerObject.transform;
            return cachedPlayer;
        }

        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin != null)
        {
            cachedPlayer = xrOrigin.transform;
            return cachedPlayer;
        }

        xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            cachedPlayer = xrOrigin.transform;
            return cachedPlayer;
        }

        if (Camera.main != null)
        {
            cachedPlayer = Camera.main.transform.root;
        }

        return cachedPlayer;
    }

    private void Execute()
    {
        switch (interactionType)
        {
            case InteractionType.Talk:
                controller.Talk(speakerName, GetDialogueLine());
                break;
            case InteractionType.DeliverWine:
                controller.DeliverWine(speakerName, transform);
                break;
            case InteractionType.ShareFood:
                controller.ShareFood(speakerName, transform);
                break;
            case InteractionType.JoinDance:
                controller.JoinDance(transform, playerRoot);
                break;
            case InteractionType.StartPoliceSequence:
                if (gameObject.name.Contains("DanceTrigger"))
                {
                    controller.JoinDance(transform, playerRoot);
                }
                else
                {
                    controller.StartPoliceSequence();
                }
                break;
            case InteractionType.BeginChapter:
                controller.BeginChapter();
                break;
            case InteractionType.MissionHint:
                controller.Talk("提示", missionHint, 3.5f);
                break;
        }

        if (disableAfterUse)
        {
            used = true;
            promptVisible = false;
            gameObject.SetActive(false);
        }
        else if (ShouldHidePromptAfterUse())
        {
            used = true;
            playerInside = false;
            promptVisible = false;
        }
    }

    private void OnGUI()
    {
        if (!promptVisible)
        {
            return;
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.wordWrap = true;
            promptStyle.normal.textColor = Color.white;
            promptStyle.fontSize = Mathf.Clamp(Screen.height / 48, 14, 20);
            promptStyle.padding = new RectOffset(12, 12, 6, 6);
        }

        float width = Mathf.Min(Screen.width - 40f, Mathf.Max(220f, promptMaxWidth));
        float height = Mathf.Max(34f, promptHeight);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - promptBottomMargin, width, height);
        GUI.Box(rect, GetPromptText(), promptStyle);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            playerInside = false;
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private string GetDialogueLine()
    {
        if (!rotateDialogueLines || extraLines == null || extraLines.Length == 0)
        {
            return line;
        }

        string nextLine = extraLines[extraLineIndex % extraLines.Length];
        extraLineIndex++;
        return string.IsNullOrWhiteSpace(nextLine) ? line : nextLine;
    }

    private string GetPromptText()
    {
        if (ShouldLetCenterMenuUseE())
        {
            if (interactionType == InteractionType.DeliverWine)
            {
                return "按 R / 2 送酒";
            }

            if (interactionType == InteractionType.ShareFood)
            {
                return "按 T / 3 分享食物";
            }
        }

        bool hasCustomPrompt = !string.IsNullOrWhiteSpace(promptText);
        bool defaultDancePromptOnNonDance = hasCustomPrompt
            && promptText.Contains("加入舞蹈")
            && interactionType != InteractionType.JoinDance
            && interactionType != InteractionType.StartPoliceSequence;

        if (hasCustomPrompt && !defaultDancePromptOnNonDance)
        {
            return promptText;
        }

        switch (interactionType)
        {
            case InteractionType.DeliverWine:
                return "按 E 送酒";
            case InteractionType.ShareFood:
                return "按 E 分享食物";
            case InteractionType.JoinDance:
                return "按 E 加入舞蹈";
            case InteractionType.StartPoliceSequence:
                return "按 E 觸發劇情";
            case InteractionType.BeginChapter:
                return "按 E 開始第一章";
            case InteractionType.MissionHint:
                return "按 E 查看提示";
            default:
                return "按 E 交談";
        }
    }

    private bool ShouldHidePromptAfterUse()
    {
        if (hidePromptAfterUse)
        {
            return true;
        }

        return interactionType == InteractionType.JoinDance
            || interactionType == InteractionType.StartPoliceSequence
            || interactionType == InteractionType.BeginChapter;
    }

    private bool PassesStoryGate()
    {
        if (controller == null)
        {
            return false;
        }

        // 「開始第一章」本身可以在自由探索前使用。
        if (interactionType == InteractionType.BeginChapter)
        {
            return true;
        }

        // 開場故事、日警劇情、選擇畫面、章節結尾期間，
        // 全部世界互動提示都關閉，避免字幕還在播時 E / R / T 提示一起跑出來。
        if (!controller.IsFreeExplorationActive())
        {
            return false;
        }

        // 舞蹈還有額外限制：只能在自由探索中，而且只能完成一次。
        if (IsDanceInteraction() && restrictDancePromptToFreeExploration)
        {
            return controller.CanUseDanceInteraction();
        }

        return true;
    }

    private bool IsDanceInteraction()
    {
        if (interactionType == InteractionType.JoinDance)
        {
            return true;
        }

        return interactionType == InteractionType.StartPoliceSequence && gameObject.name.Contains("DanceTrigger");
    }

    private bool ShouldLetCenterMenuUseE()
    {
        if (controller == null)
        {
            return false;
        }

        if (interactionType != InteractionType.DeliverWine && interactionType != InteractionType.ShareFood)
        {
            return false;
        }

        return controller.ShouldReserveEForDanceAtFireCenter();
    }

    private float GetEffectiveInteractRange()
    {
        if (!IsDanceInteraction())
        {
            return interactRange;
        }

        return Mathf.Min(interactRange, Mathf.Max(0.5f, dancePromptRange));
    }
}
