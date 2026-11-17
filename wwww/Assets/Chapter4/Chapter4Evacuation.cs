using System;
using System.Collections.Generic;
using UnityEngine;

// Local evacuation progress; Chapter4Contact owns Obing's acknowledgement and
// reunion, while Chapter4Ending owns the final completion transaction.
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
    public bool Regrouping => regrouping;
    public bool OpeningRush => initialized && party.Exists(actor => !actor.JoinedRoute);
    public Chapter4RetreatAction Action { get; private set; }
    public float PlayerProgress => initialized ? ProjectProgress(chapter.player.transform.position, 0, RouteLength) : 0;
    public float RearProgress
    {
        get { float rear = RouteLength; foreach (var actor in party) if (RequiredForArrival(actor)) rear = Mathf.Min(rear, actor.Progress); return rear; }
    }
    public float AdvanceLimit => Mathf.Min(Action != null ? Action.PlayerLimit : RouteLength, Mathf.Max(18, RearProgress + 24));
    public bool BoundaryBlocked => Time.time - blockedAt < .7f;
    public int BlockedSteps { get; private set; }
    public bool ArrivalReady => PartyGathered && !chapter.Voice.Busy;
    public int ActivePartyCount => party.Count;
    public IReadOnlyList<Chapter4Companion> Party => party;
    public Vector3 ScoutTarget => PointOnRoute(LegEnd);
    public float LegEnd => lengths[legs[LegIndex].routeIndex];
    public float RouteLength => lengths[lengths.Length - 1];
    public float ScoutedDistance { get; private set; }
    public int RequiredPartyCount => party.FindAll(RequiredForArrival).Count;

    readonly List<Chapter4Companion> party = new List<Chapter4Companion>();
    Vector3[] route;
    float[] lengths;
    float stageClock;
    float blockedAt = -100;
    bool initialized;
    bool regrouping;
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
        chapter.player.Evacuation = this;
        chapter.player.SetInputEnabled(true);
        CurrentStage = Stage.Opening;
        initialized = true;
        Action = new Chapter4RetreatAction(this);
        // People flee as the first order is spoken. No dialogue input lock.
        for (int i = 0; i < party.Count; i++) party[i].FollowTo(Mathf.Max(1, 12 - i * 1.2f), i * .18f);
        Say("mona", "大家往山林裡撤。先讓傷患走，照應身邊的人，不要散開。", 0);
        UpdateHUD();
    }

    void Update()
    {
        if (!initialized || chapter.WorldFrozen || chapter.gas.IsCollapsed) return;
        float dt = Time.deltaTime;
        stageClock += dt;
        if (!PartyGathered) TrackScoutedTrail();
        if (Action != null && !PartyGathered) Action.Tick(dt);
        if (CurrentStage == Stage.Opening)
        {
            if (stageClock >= 1.5f)
            {
                CurrentStage = Stage.Following;
                stageClock = 0;
                if (chapter.gas) chapter.gas.CaptureCheckpoint(0);
            }
        }
        else if (!PartyGathered)
        {
            CurrentStage = Stage.Following;
            if (chapter.player.InteractPressed) TrySignal();
            float target = Mathf.Min(RouteLength, Action.PartyLimit, Mathf.Max(Action.MinimumPartyAdvance, PlayerProgress + 1.5f));
            for (int i = 0, slot = 0; i < party.Count; i++)
            {
                if (!RequiredForArrival(party[i])) continue;
                if (!party[i].IndependentFollow) party[i].FollowTo(Mathf.Max(0, target - slot * 1.45f), 0);
                slot++;
            }
            if (LegIndex < legs.Length - 1 && PlayerProgress >= LegEnd && RearProgress >= LegEnd - 12 && !chapter.gas.BlocksPartyAdvance)
            {
                LegIndex++; stageClock = 0;
                chapter.gas.CaptureCheckpoint(PlayerProgress);
                RegroupDialogue(LegIndex);
            }
            bool all = PlayerProgress >= RouteLength - 2.6f && (!chapter.gas || !chapter.gas.BlocksPartyAdvance) &&
                (!chapter.contact || !chapter.contact.Responded || chapter.contact.Reunited);
            foreach (var actor in party) if (RequiredForArrival(actor)) all &= actor.AtDestination;
            if (all)
            {
                if (LegIndex == legs.Length - 1)
                {
                    CurrentStage = Stage.Gathered;
                    Say("bawan", chapter.gas && chapter.gas.ObingSeparated
                        ? "主隊到集合地了。古慕和露比都跟上了，歐冰還在後方。先讓傷患歇一口氣。"
                        : "大家都到集合地了。先在這裡歇一口氣，傷患有人照應。", 6.5f);
                    Say("lubi", "古慕，坐穩了再鬆手。我在你旁邊，你先慢慢把氣喘勻。", 8);
                }
                stageClock = 0;
            }
        }
        UpdateHUD();
    }

    void TrackScoutedTrail()
    {
        float projected = ProjectProgress(chapter.player.transform.position, Mathf.Max(0, ScoutedDistance - 2), Mathf.Min(RouteLength, ScoutedDistance + 4));
        Vector3 point = PointOnRoute(projected);
        if (chapter.player.IsGrounded && FlatDistance(point, chapter.player.transform.position) < 3.1f &&
            Mathf.Abs(point.y - chapter.player.transform.position.y) < 1.3f)
            ScoutedDistance = Mathf.Max(ScoutedDistance, projected);
    }

    public void TrySignal()
    {
        if (!initialized || chapter.WorldFrozen) return;
        if (Action == null || !Action.Signal()) return;
        CurrentStage = Stage.Following;
        if (chapter.contact) chapter.contact.NotifySignal(LegIndex);
        SignalCount++;
        stageClock = 0;
        if (!chapter.contact || chapter.contact.CurrentState != Chapter4Contact.State.Rising)
            Say("bawan", "看見你的示意了。傷患先走，大家沿著這條路跟上！", 5f);
        UpdateHUD();
    }

    bool AtScoutTarget() => chapter.player.IsGrounded && FlatDistance(chapter.player.transform.position, ScoutTarget) <= 2.6f &&
        Mathf.Abs(chapter.player.transform.position.y - ScoutTarget.y) <= 1.3f;

    public bool RequiredForArrival(Chapter4Companion actor) => !chapter.gas || !chapter.gas.ObingSeparated || actor.actorId != "obing" || chapter.contact && chapter.contact.Responded;

    public void Announce(string actorId, string text, float seconds) { Say(actorId, text, seconds); }

    public void RestoreCheckpoint(int leg, int signals, float scouted)
    {
        LegIndex = leg; SignalCount = signals; ScoutedDistance = scouted;
        CurrentStage = Stage.Following; stageClock = 0; regrouping = false;
        ClearSpeech();
        UpdateHUD();
    }

    void Say(string actorId, string text, float seconds)
    {
        chapter.Voice.Enqueue(actorId, text, seconds);
    }

    void ClearSpeech()
    {
        chapter.Voice.Stop();
    }

    void RegroupDialogue(int leg)
    {
        regrouping = false;
        if (leg == 2)
            Say("tado", "傷患都還跟著。前面岩肩路窄，別讓隊伍在轉彎處斷開，我會在前面照看。", 0);
        else if (leg == 3)
            Say("lubi", "古慕，已經看見集合地了。身體靠著我，腳步別亂，我們一起走完。", 0);
    }

    // Enforce the active fire corridor for every desktop/XR/fixture Move.
    // Restrict displacement, never teleport the player back after crossing.
    public Vector3 ConstrainPlayerStep(Vector3 from, Vector3 displacement)
    {
        if (!initialized || PartyGathered) return displacement;
        Vector3 horizontal = Vector3.ProjectOnPlane(displacement, Vector3.up);
        if (Allowed(from + horizontal, from)) return displacement;
        float low = 0, high = 1;
        for (int i = 0; i < 9; i++)
        {
            float mid = (low + high) * .5f;
            if (Allowed(from + horizontal * mid, from)) low = mid; else high = mid;
        }
        blockedAt = Time.time; BlockedSteps++;
        return horizontal * low + Vector3.up * displacement.y;
    }

    bool Allowed(Vector3 candidate, Vector3 from)
    {
        float progress = ProjectProgress(candidate, 0, RouteLength);
        float current = ProjectProgress(from, 0, RouteLength);
        if (progress > AdvanceLimit && progress > current + .001f) return false;
        float distance = FlatDistance(candidate, PointOnRoute(progress));
        float oldDistance = FlatDistance(from, PointOnRoute(current));
        if (Action != null && Action.Rockfall && progress > Chapter4RetreatAction.ForkStart && progress < Chapter4RetreatAction.ForkEnd)
            return Action.BranchDistance(candidate) <= 1.8f || Action.BranchDistance(candidate) < Action.BranchDistance(from);
        bool camp = LegIndex == 0 && progress < 3 && FlatDistance(candidate, PointOnRoute(0)) < 12;
        return camp || distance <= 4.6f || distance < oldDistance;
    }

    void UpdateHUD()
    {
        var ui = chapter.presentation;
        bool separated = chapter.gas && chapter.gas.ObingSeparated;
        ui.SetObjective(PartyGathered ? "隊伍已抵達集合地" : Action.Objective);
        string status = PartyGathered ? "主隊集結　" + RequiredPartyCount + "／" + RequiredPartyCount :
            "隊伍持續跟隨・距隊尾 " + Mathf.CeilToInt(Mathf.Max(0, PlayerProgress - RearProgress)) + " 公尺";
        if (chapter.RearGuard && !string.IsNullOrEmpty(chapter.RearGuard.Status)) status = chapter.RearGuard.Status;
        ui.SetPartyStatus(status + (separated ? "・歐冰落在後方" : ""));
        bool tether = PlayerProgress >= RearProgress + 23.7f && !PartyGathered;
        contextualHint = PartyGathered ? "" : tether ? "隊伍落在後方了・在附近岩後接應，別讓同伴看不見你" : Action.Hint;
        ui.SetInteraction(contextualHint, !PartyGathered && Action.CanSignal);
        ui.SetNavigation(chapter.player.view, Action.NavigationTarget, !PartyGathered,
            Action.Rockfall && Action.Choice == Chapter4RetreatAction.RouteChoice.Undecided ? "從岩石兩側繞行" : "沿路撤離");
    }
    public Vector3 PointOnRoute(float distance) => Action == null ? BasePoint(distance) : Action.MapPoint(distance, BasePoint(distance));

    public Vector3 BasePoint(float distance)
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
            Vector3 a = PointOnRoute(lengths[i - 1]), b = PointOnRoute(lengths[i]);
            Vector3 segment = Vector3.ProjectOnPlane(b - a, Vector3.up);
            float t = segment.sqrMagnitude < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(position - a, Vector3.up), segment) / segment.sqrMagnitude);
            float progress = Mathf.Clamp(Mathf.Lerp(lengths[i - 1], lengths[i], t), from, to);
            float distance = FlatDistance(position, PointOnRoute(progress));
            if (distance < best) { best = distance; result = progress; }
        }
        return result;
    }

    public static float FlatDistance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;

    void OnDestroy()
    {
        Action?.Dispose();
        if (chapter && chapter.presentation && chapter.presentation.signalButton)
            chapter.presentation.signalButton.onClick.RemoveListener(TrySignal);
    }
}
