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
    public bool OpeningRush => initialized && LegIndex == 0 && SignalCount == 0;
    public float AdvanceLimit => LegEnd + 1.2f;
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
        // People flee as the first order is spoken. No dialogue input lock.
        for (int i = 0; i < party.Count; i++) party[i].FollowTo(Mathf.Max(1, 12 - i * 1.2f), i * .18f);
        Say("mona", "大家往山林裡撤。先讓傷患走，照應身邊的人，不要散開。", 0);
        UpdateHUD();
    }

    void Update()
    {
        if (!initialized || chapter.WorldFrozen) return;
        float dt = Time.deltaTime;
        stageClock += dt;
        if (!PartyGathered) TrackScoutedTrail();
        if (CurrentStage == Stage.Opening)
        {
            if (stageClock >= 1.5f)
            {
                CurrentStage = Stage.Scouting;
                stageClock = 0;
                Say("bawan", "你先去前面探路。走過岔路，確認能通行，再抬手示意我們跟上。", 0);
                if (chapter.gas) chapter.gas.CaptureCheckpoint(0);
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
            bool all = (!chapter.gas || !chapter.gas.BlocksPartyAdvance) &&
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
                else
                {
                    float checkpointDistance = LegEnd;
                    LegIndex++;
                    ScoutedDistance = Mathf.Min(ScoutedDistance, checkpointDistance);
                    CurrentStage = Stage.Scouting;
                    Say("bawan", chapter.gas && chapter.gas.ObingSeparated
                        ? "主隊跟上了。露比正扶著古慕，歐冰還在後方。你先確認下一段通路。"
                        : "隊伍都跟上了。我照應後方，你再往前確認下一段路。", 6f);
                    if (chapter.gas) chapter.gas.CaptureCheckpoint(checkpointDistance);
                    RegroupDialogue(LegIndex);
                }
                stageClock = 0;
            }
        }
        UpdateHUD();
    }

    void TrackScoutedTrail()
    {
        // Future sections cannot be banked before the party really arrives.
        float projected = ProjectProgress(chapter.player.transform.position, Mathf.Max(0, ScoutedDistance - 2), Mathf.Min(LegEnd, ScoutedDistance + 4));
        Vector3 point = PointOnRoute(projected);
        if (chapter.player.IsGrounded && FlatDistance(point, chapter.player.transform.position) < 3.1f &&
            Mathf.Abs(point.y - chapter.player.transform.position.y) < 1.3f)
            ScoutedDistance = Mathf.Max(ScoutedDistance, projected);
    }

    public void TrySignal()
    {
        if (!initialized || chapter.WorldFrozen) return;
        if (CurrentStage != Stage.ReadyToSignal || !AtScoutTarget())
        {
            if (CurrentStage == Stage.Scouting)
                Say("bawan", "先走到前面確認通路，再示意我們過去。", 3.8f);
            return;
        }
        CurrentStage = Stage.Following;
        if (chapter.contact) chapter.contact.NotifySignal(LegIndex);
        SignalCount++;
        stageClock = 0;
        for (int i = 0; i < party.Count; i++)
            if (RequiredForArrival(party[i])) party[i].FollowTo(LegEnd - i * 1.45f, i * .45f);
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
        CurrentStage = Stage.Scouting; stageClock = 0; regrouping = false;
        ClearSpeech();
        Say("bawan", "在這裡重新集結。確認前面的通路，再示意大家跟上。", 5.8f);
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
        if (leg == 1)
            Say("bawan", "前面是岔路。你去確認上坡那條通路，也留意歐冰的聲音；找到方向，可以喊給她聽。", 0);
        else if (leg == 2)
            Say("tado", "傷患都還跟著。前面岩肩路窄，別讓隊伍在轉彎處斷開，我會在前面照看。", 0);
        else
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
        bool camp = LegIndex == 0 && progress < 3 && FlatDistance(candidate, PointOnRoute(0)) < 12;
        return camp || distance <= 4.6f || distance < oldDistance;
    }

    void UpdateHUD()
    {
        var ui = chapter.presentation;
        bool opening = CurrentStage == Stage.Opening, following = CurrentStage == Stage.Following;
        bool separated = chapter.gas && chapter.gas.ObingSeparated;
        string[] orders = { "穿過落彈林路，到前方彎道示意", "避開低處濃煙，確認上坡通路", "穿過岩肩，接應後方同伴", "帶隊離開火線，前往集合地" };
        ui.SetObjective(opening ? "砲擊逼近！立刻離開營地" : PartyGathered ? (separated ? "主隊已抵達集合地" : "隊伍已抵達集合地") :
            following ? "留在彎道接應隊伍，避開落彈" : orders[Mathf.Clamp(LegIndex, 0, 3)]);
        int arrived = 0, required = 0;
        foreach (var actor in party) if (RequiredForArrival(actor)) { required++; if (actor.AtDestination) arrived++; }
        ui.SetPartyStatus((opening ? "隊伍正在撤離・邊走邊聽交代" : PartyGathered ? "主隊集結　" + required + "／" + required :
            following ? "隊伍前進中　" + arrived + "／" + required + " 抵達" :
            "第 " + (LegIndex + 1) + "／" + legs.Length + " 段　前方 " + Mathf.CeilToInt(FlatDistance(chapter.player.transform.position, ScoutTarget)) + " 公尺") +
            (separated ? "・歐冰在後方" : ""));
        contextualHint = CurrentStage == Stage.ReadyToSignal
            ? (chapter.player.IsVR ? "通路已確認・按右扳機示意隊伍前進" : "通路已確認・按 E 示意隊伍前進")
            : BoundaryBlocked ? "留在撤離通路！本段隊伍到齊前，前方火線尚未解除。"
            : following ? (chapter.player.IsVR ? "留在這段接應・握住右握把壓低身體，避開落彈" : "留在這段接應・按住 Ctrl 壓低身體，避開落彈")
            : opening ? "砲火已到營地！沿箭頭撤離，對話中也能行動。" : PartyGathered ? "" : BranchHint();
        ui.SetInteraction(contextualHint, CurrentStage == Stage.ReadyToSignal);
        string nearLabel = CurrentStage == Stage.ReadyToSignal
            ? (chapter.player.IsVR ? "按右扳機接應隊伍" : "按 E 接應隊伍")
            : following ? "回看隊伍・留意落彈" : "";
        ui.SetNavigation(chapter.player.view, following ? ScoutTarget : PointOnRoute(Mathf.Min(LegEnd, ScoutedDistance + 6)), !PartyGathered, nearLabel);
    }

    string BranchHint()
    {
        foreach (Transform group in chapter.transform)
        {
            if (!group.name.StartsWith("支路") || group.childCount == 0) continue;
            if (FlatDistance(chapter.player.transform.position, group.GetChild(group.childCount - 1).position) < 4)
                return "這條支路被擋住了，回到岔路，沿能通行的山路探查。";
        }
        return "後方濃煙正在逼近。沿箭頭確認這段通路，按 E 接應隊伍。";
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
