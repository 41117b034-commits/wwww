using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public partial class Chapter1PerformanceController
{
    [Header("開場與章節目標")]
    public Chapter1EyeOpening openingSequence;
    [SerializeField] private Text chapterObjective;
    private Canvas chapterCanvas;
    private Text weddingDirection;
    private WeddingAction displayedAction;
    private float nextActionCheck;

    private void PrepareChapterPresentation()
    {
        if (!chapterObjective) return;
        chapterCanvas = chapterObjective.GetComponentInParent<Canvas>(true);
        if (chapterCanvas && !dialogueUI) dialogueUI = Chapter1DialogueUI.Create(chapterCanvas, chapterObjective.font);
        chapterObjective.transform.parent.gameObject.SetActive(false);
        if (weddingElder && chapterCanvas)
        {
            weddingDirection = new GameObject("WeddingDirection", typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
            weddingDirection.transform.SetParent(chapterCanvas.transform, false);
            weddingDirection.font = chapterObjective.font;
            weddingDirection.fontSize = 30;
            weddingDirection.alignment = TextAnchor.MiddleCenter;
            weddingDirection.raycastTarget = false;
            weddingDirection.rectTransform.sizeDelta = new Vector2(154f, 44f);
            var outline = weddingDirection.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            weddingDirection.gameObject.SetActive(false);
        }
        GameDialogueOverlay.Ensure(chapterCanvas);
        if (fadeCanvas)
            fadeCanvas.alpha = autoStartOnAwake || autoBeginStoryIfControllerExists ? 1f : 0f;
    }

    private IEnumerator RevealWedding()
    {
        yield return Fade(1f, 0f, 1.8f, progress =>
        {
            // Keep the raised hand visible as the scene brightens even after a slow loading frame.
            if (groomPose) groomPose.Beckon(1.4f * progress);
        });
    }

    private void UpdateChapterPresentation()
    {
        if (!chapterCanvas) return;
        ConfigureChapterCanvas(InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid);
        bool visible = !openingStoryPlaying && !chapterCompleted
            && !(weddingElder && (interactionAnimationRunning || physicalDeliveryAnimating))
            && (waitingForChoice || (!(cinematicStoryPlaying && hideGameplayHudDuringCinematic)
                && (IsFreeExplorationActive() || (policeSequenceStarted && !doorwayStaged))));
        GameObject heading = chapterObjective.transform.parent.gameObject;
        if (heading.activeSelf != visible)
        {
            heading.SetActive(visible);
            if (visible) RefreshChapterObjective();
        }
        if (!weddingDirection) return;
        bool exploring = visible && IsFreeExplorationActive();
        if (exploring && Time.unscaledTime >= nextActionCheck)
        {
            nextActionCheck = Time.unscaledTime + .1f;
            WeddingAction action = NearbyWeddingAction();
            if (action != displayedAction) RefreshChapterObjective();
        }
        UpdateWeddingDirection(exploring);
    }

    private void ConfigureChapterCanvas(bool vr)
    {
        RenderMode mode = vr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceCamera;
        if (chapterCanvas.renderMode == mode) return;
        chapterCanvas.renderMode = mode;
        RefreshChoicePanel();
        if (!vr) return;

        // The scene parents this canvas to the player camera, so head tracking
        // also positions the heading and the full-field curtain between frames.
        RectTransform rect = (RectTransform)chapterCanvas.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(1600f, 900f);
        rect.localScale = Vector3.one * .0017f;
        rect.localRotation = Quaternion.identity;
        rect.localPosition = Vector3.forward * 2.3f;
    }

    private void RefreshChapterObjective()
    {
        if (chapterObjective && chapterObjective.transform.parent.gameObject.activeSelf)
            chapterObjective.text = CurrentChapterObjective();
    }

    private string CurrentChapterObjective()
    {
        if (waitingForChoice) return doorwayStaged ? "回應木屋前的衝突" : "選擇上前阻止，或沉默觀望";
        if (!IsFreeExplorationActive()) return missionText;
        if (danceRoutineRunning) return "跟著鼓聲，與族人一起跳舞";

        int wine = Mathf.Max(1, wineTargetCount);
        int food = Mathf.Max(1, foodTargetCount);
        if (weddingElder)
        {
            displayedAction = NearbyWeddingAction();
            string press = chapterCanvas.renderMode == RenderMode.WorldSpace ? "按手把 A " : "按 E／手把 A ";
            switch (displayedAction)
            {
                case WeddingAction.TakeWine: return press + (FirstCupPending ? "拿起第一杯酒" : "拿酒");
                case WeddingAction.TakeFood: return press + "拿取烤魚";
                case WeddingAction.GiveWine: return press + (FirstCupPending ? "把酒交給長輩" : "把酒交給賓客");
                case WeddingAction.GiveFood: return press + "把烤魚交給族人";
                case WeddingAction.JoinDance: return press + "加入舞圈";
            }
            if (carriedWeddingItem != WeddingCarryItem.None)
            {
                string putBack = chapterCanvas.renderMode == RenderMode.WorldSpace ? "（B 放回）" : "（Q 放回）";
                if (carriedWeddingItem == WeddingCarryItem.Food) return $"把烤魚送給族人 {sharedFoodCount}/{food}" + putBack;
                return (FirstCupPending ? "把第一杯酒送給火堆旁的長輩" : $"把酒送給賓客 {deliveredWineCount}/{wine}") + putBack;
            }
            if (FirstCupPending) return "替新郎取一杯酒，送給火堆旁的長輩";
        }
        if (carriedWeddingItem == WeddingCarryItem.Wine)
            return $"把酒送給有黃色標記的賓客（{deliveredWineCount}/{wine}）";
        if (carriedWeddingItem == WeddingCarryItem.Food)
            return $"把食物送給有黃色標記的族人（{sharedFoodCount}/{food}）";

        if (deliveredWineCount < wine && sharedFoodCount < food && !explorationTimerFinished)
            return $"幫新郎送酒 {deliveredWineCount}/{wine}、分送食物 {sharedFoodCount}/{food}";
        if (deliveredWineCount < wine)
            return $"循標記取酒，送給賓客（{deliveredWineCount}/{wine}）";
        if (sharedFoodCount < food)
            return $"循標記取食物，分給族人（{sharedFoodCount}/{food}）";
        return danceFinished ? "婚禮任務已完成" : "到火堆旁加入舞圈";
    }

    private void UpdateWeddingDirection(bool exploring)
    {
        Transform target = null;
        string label = "";
        if (exploring && !WeddingGuidanceQuiet && displayedAction == WeddingAction.None)
        {
            if (carriedWeddingItem != WeddingCarryItem.None)
            {
                target = activeDeliveryTargetMarker ? activeDeliveryTargetMarker.transform : null;
                label = carriedWeddingItem == WeddingCarryItem.Food ? "族人" : (FirstCupPending ? "長輩" : "賓客");
            }
            else if (winePickupLocationMarker && winePickupLocationMarker.activeSelf)
            {
                target = winePickupLocationMarker.transform;
                label = "酒";
            }
            else if (foodPickupLocationMarker && foodPickupLocationMarker.activeSelf)
            {
                target = foodPickupLocationMarker.transform;
                label = "食物";
            }
            else if (AreWineAndFoodTasksComplete() && !danceFinished)
            {
                target = danceCenter;
                label = "舞圈";
            }
        }
        bool show = target && chapterCanvas.worldCamera;
        if (weddingDirection.gameObject.activeSelf != show) weddingDirection.gameObject.SetActive(show);
        if (!show) return;
        Vector3 point = chapterCanvas.worldCamera.WorldToViewportPoint(target.position);
        Vector2 offset = new Vector2(point.x - .5f, point.y - .5f);
        if (point.z < 0f) offset = -offset;
        bool offscreen = point.z <= 0f || Mathf.Abs(offset.x) > .43f || Mathf.Abs(offset.y) > .22f;
        if (offscreen)
        {
            if (offset.sqrMagnitude < .0001f) offset = Vector2.left;
            offset /= Mathf.Max(Mathf.Abs(offset.x) / .43f, Mathf.Abs(offset.y) / .22f);
            label = GetPickupIndicatorDirection(new Vector2(offset.x, -offset.y)) + " " + label;
        }
        Vector2 anchor = new Vector2(.5f, .5f) + offset;
        weddingDirection.rectTransform.anchorMin = weddingDirection.rectTransform.anchorMax = anchor;
        weddingDirection.rectTransform.anchoredPosition = Vector2.zero;
        if (weddingDirection.text != label) weddingDirection.text = label;
    }
}
