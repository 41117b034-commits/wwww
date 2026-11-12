using UnityEngine;
using UnityEngine.SceneManagement;

// Arrival remains local. The evacuation reads Chapter 3 once; neither component
// writes the previous result or marks Chapter 4 complete.
public sealed class Chapter4SceneController : MonoBehaviour
{
    public Chapter4Player player;
    public Chapter4Presentation presentation;
    public Chapter4Evacuation evacuation;
    public Transform spawnPoint;
    public Transform gatheringPoint;
    public Transform[] routeCheckpoints;
    public float arrivalRadius = 3.2f;
    public float arrivalHeightTolerance = 1.2f;
    public float checkpointRadius = 3.5f;
    public string chapterMenuScene = "選擇章節ˊ";
    public bool HasArrived { get; private set; }
    public bool IsPaused { get; private set; }
    public int ReachedCheckpointCount { get; private set; }

    Transform recoveryPoint;
    float recoveryFloor;
    bool changingScene;

    void Start()
    {
        if (!player || !player.view || !spawnPoint || !gatheringPoint || !presentation)
        {
            Debug.LogError("Fourth chapter scene references are incomplete.", this);
            if (player) player.SetInputEnabled(false);
            enabled = false;
            return;
        }
        presentation.continueButton.onClick.AddListener(Resume);
        presentation.menuButton.onClick.AddListener(ReturnToChapterMenu);
        recoveryFloor = Mathf.Min(spawnPoint.position.y, gatheringPoint.position.y);
        if (routeCheckpoints != null)
            foreach (Transform point in routeCheckpoints)
                if (point) recoveryFloor = Mathf.Min(recoveryFloor, point.position.y);
        recoveryFloor -= 12;
        recoveryPoint = spawnPoint;
        player.WarpTo(spawnPoint);
        player.SetInputEnabled(true);
        presentation.SetObjective("沿山路前往集合地");
        presentation.HideMenu();
        if (evacuation) evacuation.Begin();
    }

    void Update()
    {
        if (!player || !presentation || changingScene) return;
        presentation.ConfigureVR(player.IsVR);
        presentation.SetControls(player.IsVR);
        if (player.MenuPressed)
        {
            if (IsPaused) Resume();
            else OpenMenu(false);
        }
        if (IsPaused)
        {
            if (player.IsVR) presentation.UpdateGaze(player.SubmitPressed);
            return;
        }

        // Recovery is only for leaving the physical walkable surface. It does
        // not count as arrival or save a story outcome.
        if (player.transform.position.y < recoveryFloor)
        {
            player.WarpTo(recoveryPoint, false);
            return;
        }
        if (routeCheckpoints != null && ReachedCheckpointCount < routeCheckpoints.Length)
        {
            Transform point = routeCheckpoints[ReachedCheckpointCount];
            if (!point) ReachedCheckpointCount++;
            else if (player.IsGrounded && Near(point, checkpointRadius, arrivalHeightTolerance))
            {
                recoveryPoint = point;
                ReachedCheckpointCount++;
            }
        }
        if (!HasArrived && (!evacuation || evacuation.ArrivalReady) && player.IsGrounded && Near(gatheringPoint, arrivalRadius, arrivalHeightTolerance))
        {
            HasArrived = true;
            presentation.SetObjective(evacuation ? "隊伍已抵達集合地" : "已抵達集合地");
            OpenMenu(true);
        }
    }

    bool Near(Transform target, float radius, float heightTolerance)
    {
        if (!target) return false;
        Vector3 difference = player.transform.position - target.position;
        return Mathf.Abs(difference.y) <= heightTolerance &&
               difference.x * difference.x + difference.z * difference.z <= radius * radius;
    }

    void OpenMenu(bool arrival)
    {
        IsPaused = true;
        player.SetInputEnabled(false);
        presentation.ShowMenu(arrival ? (evacuation ? "隊伍已抵達集合地" : "已抵達集合地") : "暫停",
            arrival ? "可以繼續查看周圍，或返回章節選單。" : "探查前方通路，再示意隊伍跟上。",
            arrival || HasArrived ? "繼續探索" : "繼續");
    }

    public void Resume()
    {
        if (changingScene || !player || !presentation) return;
        IsPaused = false;
        presentation.HideMenu();
        player.SetInputEnabled(!evacuation || evacuation.CurrentStage != Chapter4Evacuation.Stage.Opening);
    }

    public void ReturnToChapterMenu()
    {
        if (changingScene) return;
        if (!Application.CanStreamedLevelBeLoaded(chapterMenuScene))
        {
            if (presentation) presentation.SetMenuMessage("暫時無法返回章節選單，請繼續探索或稍後再試。");
            Debug.LogError("Chapter menu scene is not available in the build: " + chapterMenuScene, this);
            return;
        }
        changingScene = true;
        player.ReleasePointer();
        SceneManager.LoadScene(chapterMenuScene, LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (!presentation) return;
        presentation.continueButton.onClick.RemoveListener(Resume);
        presentation.menuButton.onClick.RemoveListener(ReturnToChapterMenu);
    }
}
