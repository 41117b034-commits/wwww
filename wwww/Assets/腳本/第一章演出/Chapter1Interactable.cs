using UnityEngine;

public class Chapter1Interactable : MonoBehaviour
{
    public enum InteractionType
    {
        Talk,
        DeliverWine,
        ShareFood,
        JoinDance,
        StartPoliceSequence
    }

    [Header("References")]
    public Chapter1PerformanceController controller;
    public Transform playerRoot;

    [Header("Interaction")]
    public InteractionType interactionType = InteractionType.Talk;
    public KeyCode interactKey = KeyCode.F;
    public bool alsoUseEKey = true;
    public bool disableAfterUse = false;

    [Header("Prompt")]
    public bool showPrompt = true;
    public string promptText = "\u6309 E / \u624b\u628a\u89f8\u767c \u52a0\u5165\u821e\u8e48";

    [Header("XR Fallback Detection")]
    public bool useDistanceCheck = true;
    public float interactRange = 4f;
    public string playerTag = "Player";

    [Header("Dialogue")]
    public string speakerName = "Villager";
    [TextArea(2, 4)]
    public string line = "May the ancestors bless the newlyweds.";

    private bool playerInside;
    private bool used;
    private Transform cachedPlayer;
    private bool promptVisible;
    private GUIStyle promptStyle;

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
        bool canInteract = CanInteractNow();
        promptVisible = showPrompt && canInteract;

        if ((pressedInspectorKey || pressedEKey) && canInteract)
        {
            Execute();
        }
    }

    private bool CanInteractNow()
    {
        if (playerInside)
        {
            return true;
        }

        if (!useDistanceCheck)
        {
            return false;
        }

        Transform player = GetPlayerTransform();
        if (player == null)
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= interactRange;
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
                controller.Talk(speakerName, line);
                break;
            case InteractionType.DeliverWine:
                controller.DeliverWine(speakerName);
                break;
            case InteractionType.ShareFood:
                controller.ShareFood(speakerName);
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
        }

        if (disableAfterUse)
        {
            used = true;
            gameObject.SetActive(false);
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
            promptStyle.fontSize = Mathf.Clamp(Screen.height / 34, 18, 28);
            promptStyle.padding = new RectOffset(18, 18, 10, 10);
        }

        float width = Mathf.Min(Screen.width - 40f, 560f);
        float height = 64f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 48f, width, height);
        GUI.Box(rect, promptText, promptStyle);
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
}
