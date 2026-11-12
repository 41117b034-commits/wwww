using System;
using System.Collections.Generic;
using UnityEngine;

// Batch 2 ends with the whole party physically gathered. It never writes a
// chapter result; the later gas/contact/ending batches will extend this flow.
public sealed class Chapter4Evacuation : MonoBehaviour
{
    public enum Stage { Opening, Scouting, ReadyToSignal, Following, Gathered }
    public enum BihuState { Unconfirmed, Rescued, Dead }
    [Serializable] public sealed class Leg
    {
        public string title;
        public int routeIndex;
    }
    public Chapter4SceneController chapter;
    public Chapter4Companion[] cast;
    public Leg[] legs;
    public Collider terrain;
    public Stage CurrentStage { get; private set; }
    public BihuState InheritedBihu { get; private set; }
    public int LegIndex { get; private set; }
    public int SignalCount { get; private set; }
    public bool PartyGathered => CurrentStage == Stage.Gathered;
    public bool ArrivalReady => PartyGathered && subtitleClock <= 0;
    public int ActivePartyCount => party.Count;
    public IReadOnlyList<Chapter4Companion> Party => party;
    public Vector3 ScoutTarget => PointOnRoute(LegEnd);
    public float LegEnd => lengths[legs[LegIndex].routeIndex];
    public float RouteLength => lengths[lengths.Length - 1];
    public float ScoutedDistance { get; private set; }

    readonly List<Chapter4Companion> party = new List<Chapter4Companion>();
    Vector3[] route;
    float[] lengths;
    float stageClock, subtitleClock;
    int openingLine = -1;
    bool initialized;
    Chapter4Companion speaker;
    string contextualHint;

    public void Begin()
    {
        if (!chapter || !terrain || cast == null || cast.Length == 0 || legs == null || legs.Length == 0)
            throw new InvalidOperationException("Fourth chapter evacuation references are incomplete.");
        route = Array.ConvertAll(chapter.routeCheckpoints, t => t.position);
        lengths = new float[route.Length];
        for (int i = 1; i < route.Length; i++)
            lengths[i] = lengths[i - 1] + Vector3.ProjectOnPlane(route[i] - route[i - 1], Vector3.up).magnitude;
        foreach (var leg in legs)
            if (leg.routeIndex < 1 || leg.routeIndex >= route.Length) throw new InvalidOperationException("Invalid scouting route index.");
        var previous = Chapter3ResultStore.Load();
        InheritedBihu = previous == null ? BihuState.Unconfirmed : previous.pihuSurvived ? BihuState.Rescued : BihuState.Dead;
        foreach (var actor in cast)
        {
            bool included = actor.actorId != "pihu" || InheritedBihu == BihuState.Rescued;
            actor.gameObject.SetActive(included);
            if (included) party.Add(actor);
        }
        for (int i = 0; i < party.Count; i++) party[i].Initialize(this, i > 0 ? party[i - 1] : null, i);
        chapter.presentation.EnsureStoryUI();
        chapter.presentation.SetCarryover(InheritedBihu == BihuState.Rescued ? "比胡獲救・隨傷患先行" :
            InheritedBihu == BihuState.Dead ? "比胡已於第三章死亡" : "尚無有效第三章結果・比胡狀態未確認");
        chapter.presentation.signalButton.onClick.AddListener(TrySignal);
        chapter.player.SetInputEnabled(false);
        CurrentStage = Stage.Opening;
        initialized = true;
        ShowOpeningLine(0);
        UpdateHUD();
    }

    void Update()
    {
        if (!initialized || chapter.IsPaused) return;
        float dt = Time.deltaTime;
        stageClock += dt;
        if (subtitleClock > 0)
        {
            subtitleClock -= dt;
            if (subtitleClock <= 0) ClearSpeech();
        }
        if (CurrentStage != Stage.Opening && !PartyGathered) TrackScoutedTrail();
        if (CurrentStage == Stage.Opening)
        {
            if (stageClock >= OpeningDuration(openingLine))
            {
                if (openingLine < 3) ShowOpeningLine(openingLine + 1);
                else
                {
                    ClearSpeech();
                    CurrentStage = Stage.Scouting;
                    chapter.player.SetInputEnabled(true);
                    stageClock = 0;
                }
            }
        }
        else if (CurrentStage == Stage.Scouting || CurrentStage == Stage.ReadyToSignal)
        {
            bool ready = ScoutedDistance >= LegEnd - 2.2f && AtScoutTarget();
            CurrentStage = ready ? Stage.ReadyToSignal : Stage.Scouting;
            if (chapter.player.InteractPressed) TrySignal();
        }
        else if (CurrentStage == Stage.Following)
        {
            bool all = true;
            foreach (var actor in party) all &= actor.AtDestination;
            if (all)
            {
                if (LegIndex == legs.Length - 1)
                {
                    CurrentStage = Stage.Gathered;
                    Say("bawan", "大家都到集合地了。先在這裡歇一口氣，傷患有人照應。", 5.8f);
                }
                else
                {
                    LegIndex++;
                    CurrentStage = Stage.Scouting;
                    Say("bawan", "隊伍都跟上了。我照應後方，你再往前確認下一段路。", 5.5f);
                }
                stageClock = 0;
            }
        }
        UpdateHUD();
    }

    void TrackScoutedTrail()
    {
        // Keep observing real exploration while the party is moving, so a
        // player who scouts ahead need not walk the same stretch a second time.
        // Signals still require the current party leg to have finished.
        float projected = ProjectProgress(chapter.player.transform.position, Mathf.Max(0, ScoutedDistance - 2), Mathf.Min(RouteLength, ScoutedDistance + 4));
        Vector3 point = PointOnRoute(projected);
        if (chapter.player.IsGrounded && FlatDistance(point, chapter.player.transform.position) < 3.1f &&
            Mathf.Abs(point.y - chapter.player.transform.position.y) < 1.3f)
            ScoutedDistance = Mathf.Max(ScoutedDistance, projected);
    }

    public void TrySignal()
    {
        if (!initialized || chapter.IsPaused) return;
        if (CurrentStage != Stage.ReadyToSignal || !AtScoutTarget())
        {
            if (CurrentStage == Stage.Scouting)
                Say("bawan", "先走到前面確認通路，再示意我們過去。", 3.8f);
            return;
        }
        CurrentStage = Stage.Following;
        SignalCount++;
        stageClock = 0;
        for (int i = 0; i < party.Count; i++) party[i].FollowTo(LegEnd - i * 1.45f, i * .45f);
        Say("bawan", "看見你的示意了。傷患先走，大家沿著這條路跟上！", 5f);
        UpdateHUD();
    }

    bool AtScoutTarget() => chapter.player.IsGrounded && FlatDistance(chapter.player.transform.position, ScoutTarget) <= 2.6f &&
        Mathf.Abs(chapter.player.transform.position.y - ScoutTarget.y) <= 1.3f;

    void ShowOpeningLine(int index)
    {
        openingLine = index;
        stageClock = 0;
        if (index == 0) Say("mona", "大家往山林裡撤。先讓傷患走，照應身邊的人，不要散開。", OpeningDuration(index));
        else if (index == 1)
            Say("bawan", InheritedBihu == BihuState.Rescued
                ? "比胡被你救回來了，傷還沒好。達多會照應他，讓他和傷患走在前面。"
                : InheritedBihu == BihuState.Dead
                    ? "比胡已經死了。我們記著他，也要把眼前的人帶出去。達多，先照應傷患。"
                    : "達多，先照應傷患，讓他們走在前面。大家看好身邊的人。", OpeningDuration(index));
        else if (index == 2) Say("bawan", "你先去前面探路。走過岔路，確認能通行，再抬手示意我們跟上。", OpeningDuration(index));
        else Say("bawan", "我留在隊伍旁照應。等大家走到你那裡，我會回報，再由你繼續領路。", OpeningDuration(index));
    }

    static float OpeningDuration(int index) => index == 1 ? 7.2f : 6f;

    void Say(string actorId, string text, float seconds)
    {
        ClearSpeech();
        speaker = party.Find(a => a.actorId == actorId);
        if (speaker) speaker.Speaking = true;
        chapter.presentation.ShowSpeech(speaker ? speaker.displayName : "巴萬", text);
        subtitleClock = seconds;
    }

    void ClearSpeech()
    {
        if (speaker) speaker.Speaking = false;
        speaker = null;
        chapter.presentation.HideSpeech();
    }

    void UpdateHUD()
    {
        var ui = chapter.presentation;
        bool opening = CurrentStage == Stage.Opening, following = CurrentStage == Stage.Following;
        ui.SetObjective(opening ? "聽莫那與巴萬交代撤離" : PartyGathered ? "隊伍已抵達集合地" :
            following ? "等候隊伍沿路跟上" : "探路：" + legs[LegIndex].title);
        int arrived = 0;
        foreach (var actor in party) if (actor.AtDestination) arrived++;
        ui.SetPartyStatus(opening ? "傷患先行・巴萬照應隊伍" : PartyGathered ? "全員會合　" + party.Count + "／" + party.Count :
            following ? "隊伍前進中　" + arrived + "／" + party.Count + " 抵達" :
            "第 " + (LegIndex + 1) + "／" + legs.Length + " 段　前方 " + Mathf.CeilToInt(FlatDistance(chapter.player.transform.position, ScoutTarget)) + " 公尺");
        contextualHint = CurrentStage == Stage.ReadyToSignal
            ? (chapter.player.IsVR ? "通路已確認・按右扳機示意隊伍前進" : "通路已確認・按 E 示意隊伍前進")
            : following ? "巴萬正照應隊伍。可以回看同伴，等大家抵達再探下一段。"
            : opening || PartyGathered ? "" : BranchHint();
        ui.SetInteraction(contextualHint, CurrentStage == Stage.ReadyToSignal);
        ui.SetNavigation(chapter.player.view, PointOnRoute(Mathf.Min(LegEnd, ScoutedDistance + 6)), !opening && !following && !PartyGathered);
    }

    string BranchHint()
    {
        foreach (Transform group in chapter.transform)
        {
            if (!group.name.StartsWith("支路") || group.childCount == 0) continue;
            if (FlatDistance(chapter.player.transform.position, group.GetChild(group.childCount - 1).position) < 4)
                return "這條支路被擋住了，回到岔路，沿能通行的山路探查。";
        }
        return "隊伍在後方等候。沿山路探查，確認通路後再示意。";
    }

    public Vector3 PointOnRoute(float distance)
    {
        distance = Mathf.Clamp(distance, 0, RouteLength);
        int i = 1;
        while (i < lengths.Length - 1 && lengths[i] < distance) i++;
        return Vector3.Lerp(route[i - 1], route[i], Mathf.InverseLerp(lengths[i - 1], lengths[i], distance));
    }

    public float ProjectProgress(Vector3 position, float from, float to)
    {
        float best = float.PositiveInfinity, result = from;
        for (int i = 1; i < route.Length; i++)
        {
            if (lengths[i] < from || lengths[i - 1] > to) continue;
            Vector3 segment = Vector3.ProjectOnPlane(route[i] - route[i - 1], Vector3.up);
            float t = segment.sqrMagnitude < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(position - route[i - 1], Vector3.up), segment) / segment.sqrMagnitude);
            float progress = Mathf.Clamp(Mathf.Lerp(lengths[i - 1], lengths[i], t), from, to);
            float distance = FlatDistance(position, PointOnRoute(progress));
            if (distance < best) { best = distance; result = progress; }
        }
        return result;
    }

    public static float FlatDistance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;

    void OnDestroy()
    {
        if (chapter && chapter.presentation && chapter.presentation.signalButton)
            chapter.presentation.signalButton.onClick.RemoveListener(TrySignal);
    }
}
