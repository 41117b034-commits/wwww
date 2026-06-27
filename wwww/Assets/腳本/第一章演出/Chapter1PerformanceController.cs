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

    private int deliveredWineCount;
    private bool danceFinished;
    private bool danceRoutineRunning;
    private bool policeSequenceStarted;
    private bool choiceResolved;

    private void Awake()
    {
        if (choiceUI != null)
        {
            choiceUI.HideInstant();
            choiceUI.Bind(this);
        }

        if (policeGroup != null)
        {
            policeGroup.SetActive(false);
            Debug.Log("[Chapter1] Police Group is assigned and hidden at start: " + policeGroup.name);
        }
        else
        {
            Debug.LogWarning("[Chapter1] Police Group is not assigned.");
        }
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            BeginChapter();
        }
    }

    private void Update()
    {
        if (debugStartPoliceWithP && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[Chapter1] Debug P key pressed.");
            StartPoliceSequence();
        }
    }

    public void BeginChapter()
    {
        deliveredWineCount = 0;
        danceFinished = false;
        policeSequenceStarted = false;
        choiceResolved = false;

        SetPlayerControl(false);
        StartCoroutine(BeginChapterRoutine());
    }

    private IEnumerator BeginChapterRoutine()
    {
        yield return Fade(1f, 0f, 1.2f);
        ShowLine("System", "1930.10.7 Wushe. The wedding fire lights the night.", 3f);
        yield return new WaitForSeconds(3f);
        SetPlayerControl(true);
    }

    public void Talk(string speaker, string line, float seconds = 3f)
    {
        ShowLine(speaker, line, seconds);
    }

    public void DeliverWine(string npcName)
    {
        deliveredWineCount++;
        ShowLine(npcName, "Thank you. May the ancestors bless the newlyweds.", 2.5f);

        if (deliveredWineCount >= wineTargetCount)
        {
            ShowLine("System", "The wine has been delivered. The drums near the dance circle grow louder.", 3f);
        }
    }

    public void ShareFood(string npcName)
    {
        ShowLine(npcName, "Thank you. I will remember this kindness.", 2.5f);
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

        danceFinished = true;
        StartCoroutine(PlayDanceRoutine(centerOverride, playerOverride));
    }

    private IEnumerator PlayDanceRoutine(Transform centerOverride, Transform playerOverride)
    {
        danceRoutineRunning = true;
        SetPlayerControl(false);
        ShowLine("\u65cf\u4eba", "\u4f86\uff0c\u8ddf\u8457\u9f13\u8072\u4e00\u8d77\u8e0f\u6b65\u3002", 2.5f);
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

        if (startPoliceAfterDance)
        {
            StartPoliceSequence();
        }
        else
        {
            SetPlayerControl(true);
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

    public void StartPoliceSequence()
    {
        if (policeSequenceStarted)
        {
            Debug.Log("[Chapter1] Police sequence already started.");
            return;
        }

        policeSequenceStarted = true;
        Debug.Log("[Chapter1] StartPoliceSequence called.");
        StartCoroutine(PoliceSequenceRoutine());
    }

    private IEnumerator PoliceSequenceRoutine()
    {
        SetPlayerControl(false);

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
            Debug.Log("[Chapter1] Police Group activated: " + policeGroup.name);
        }
        else
        {
            Debug.LogWarning("[Chapter1] Cannot activate police. Police Group is empty.");
        }

        ShowLine("System", "Heavy footsteps approach from the mountain road.", 3f);
        yield return new WaitForSeconds(2f);

        PlayDirector(policeEnterTimeline);
        yield return WaitForDirector(policeEnterTimeline, 8f);

        ShowLine("Police", "How dare you hold such a noisy wedding?", 4f);
        yield return new WaitForSeconds(4f);
        ShowLine("Groom", "We are only holding a wedding.", 3f);
        yield return new WaitForSeconds(3f);

        ShowConflictChoice();
    }

    private void ShowConflictChoice()
    {
        if (heartbeatAudio != null)
        {
            heartbeatAudio.Play();
        }

        if (choiceUI != null)
        {
            choiceUI.Show("What will you do?", "Step forward", "Stay silent");
        }
        else
        {
            ChooseIntervene();
        }
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
            ShowLine("Villager", "Enough. Stop humiliating us.", 3f);
            PlayDirector(interveneTimeline);
            yield return WaitForDirector(interveneTimeline, 8f);
        }
        else
        {
            ShowLine("Groom", "How long must we endure this?", 3f);
            PlayDirector(watchTimeline);
            yield return WaitForDirector(watchTimeline, 8f);
        }

        ShowLine("Elder", "The people will not forget what happened tonight.", 4f);
        PlayDirector(endingTimeline);
        yield return WaitForDirector(endingTimeline, 6f);
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

    private void ShowLine(string speaker, string line, float seconds)
    {
        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(speaker, line, seconds);
        }
        else
        {
            Debug.Log("[Chapter1 Dialogue] " + speaker + ": " + line);
        }
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
}
