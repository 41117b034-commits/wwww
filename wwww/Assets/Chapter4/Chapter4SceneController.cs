using UnityEngine;
using UnityEngine.SceneManagement;

// Arrival starts the chapter ending only after the physically gathered party's
// report. The ending owns the separate Chapter 4 completion transaction.
public sealed class Chapter4SceneController : MonoBehaviour
{
    public Chapter4Player player;
    public Chapter4Presentation presentation;
    public Chapter4Evacuation evacuation;
    public Chapter4GasSequence gas;
    public Chapter4Contact contact;
    public Chapter4Ending ending;
    public Chapter4Voice Voice { get; private set; }
    public Transform spawnPoint;
    public Transform gatheringPoint;
    public Transform[] routeCheckpoints;
    public float arrivalRadius = 3.2f;
    public float arrivalHeightTolerance = 1.2f;
    public float checkpointRadius = 3.5f;
    public string chapterMenuScene = "選擇章節ˊ";
    public bool HasArrived { get; private set; }
    public bool IsPaused { get; private set; }
    public bool WorldFrozen => IsPaused || ending && ending.HasStarted;
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
        Voice = gameObject.AddComponent<Chapter4Voice>();
        Voice.Initialize(this);
        if (evacuation) evacuation.Begin();
        if (gas) gas.Begin();
        if (contact) contact.Begin();
    }

    void Update()
    {
        if (!player || !presentation || changingScene) return;
        presentation.ConfigureVR(player.IsVR);
        presentation.SetControls(player.IsVR);
        if (ending && ending.CurrentStage == Chapter4Ending.Stage.SaveFailed)
        {
            if (player.RetryPressed) ending.RetrySave();
            else if (player.IsVR) presentation.UpdateGaze(player.SubmitPressed);
            return;
        }
        if (gas && gas.IsCollapsed)
        {
            if (gas.CanRetry && player.RetryPressed) gas.RetryCheckpoint();
            else if (player.IsVR) presentation.UpdateGaze(player.SubmitPressed);
            return;
        }
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
        if (ending && ending.HasStarted) return;

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
            if (ending)
            {
                if (ending.TryBegin()) HasArrived = true;
                return;
            }
            HasArrived = true;
            presentation.SetObjective(gas && gas.ObingSeparated ? "主隊已抵達集合地" : "隊伍已抵達集合地");
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
        presentation.ShowMenu(arrival ? (gas && gas.ObingSeparated ? "主隊已抵達集合地" : "隊伍已抵達集合地") : "暫停",
            arrival ? (gas && gas.ObingSeparated ? "古慕與露比已跟上，歐冰仍在後方。\n可以繼續探索，或返回章節選單。" : "可以繼續查看周圍，或返回章節選單。") :
            ending && ending.HasStarted ? "章末播放已暫停。\n繼續後會接著播放，後記結束才保存。" : "探查前方通路，再示意隊伍跟上。",
            ending && ending.HasStarted ? "繼續閱讀" : arrival || HasArrived ? "繼續探索" : "繼續");
    }

    public void Resume()
    {
        if (changingScene || !player || !presentation) return;
        if (gas && gas.IsCollapsed) { gas.RetryCheckpoint(); return; }
        if (ending && ending.CurrentStage == Chapter4Ending.Stage.SaveFailed) { ending.RetrySave(); return; }
        IsPaused = false;
        presentation.HideMenu();
        player.SetInputEnabled((!ending || !ending.HasStarted) && (!evacuation || evacuation.CurrentStage != Chapter4Evacuation.Stage.Opening));
    }

    public void ShowEndingSaveFailure()
    {
        IsPaused = true;
        player.SetInputEnabled(false);
        presentation.ShowMenu("本次結局尚未保存", "原有紀錄已保留。\n按 R 或下方按鈕重試保存。", "重試保存");
    }

    public void BeginGasCollapse()
    {
        IsPaused = true;
        player.SetInputEnabled(false);
        presentation.HideSpeech();
        presentation.SetInteraction("", false);
    }

    public void RestoreAfterGasFailure(Transform checkpoint)
    {
        HasArrived = false;
        IsPaused = false;
        recoveryPoint = checkpoint;
        player.WarpTo(checkpoint);
        player.SetCollapseVisual(0);
        presentation.HideMenu();
        player.SetInputEnabled(true);
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
