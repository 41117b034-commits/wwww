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
    int openingLine = -1;
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
        chapter.player.SetInputEnabled(false);
        CurrentStage = Stage.Opening;
        initialized = true;
        ShowOpeningLine(0);
        UpdateHUD();
    }

    void Update()
    {
        if (!initialized || chapter.WorldFrozen) return;
        float dt = Time.deltaTime;
        stageClock += dt;
        if (CurrentStage != Stage.Opening && !PartyGathered) TrackScoutedTrail();
        if (CurrentStage == Stage.Opening)
        {
            if (!chapter.Voice.Busy)
            {
                if (openingLine < 5) ShowOpeningLine(openingLine + 1);
                else
                {
                    ClearSpeech();
                    CurrentStage = Stage.Scouting;
                    chapter.player.SetInputEnabled(true);
                    stageClock = 0;
                    if (chapter.gas) chapter.gas.CaptureCheckpoint(0);
                }
            }
        }
        else if (CurrentStage == Stage.Scouting || CurrentStage == Stage.ReadyToSignal)
        {
            bool ready = !regrouping && ScoutedDistance >= LegEnd - 2.2f && AtScoutTarget();
            if (regrouping && !chapter.Voice.Busy) regrouping = false;
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
                    Say("gumu", "我到這裡了……露比，謝謝你一直扶著我。先看看其他人有沒有需要照應。", 10);
                    Say("mona", "先讓傷患休息，其他人守住通路。到了集合地，仍然要留意山下的動靜。", 11);
                }
                else
                {
                    float checkpointDistance = LegEnd;
                    LegIndex++;
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
        else if (index == 3) Say("bawan", "我留在隊伍旁照應。等大家走到你那裡，我會回報，再由你繼續領路。", OpeningDuration(index));
        else if (index == 4) Say("tado", "傷患跟著我，腳步放穩。走到轉彎處先看清前一個人，別只顧著往聲音的方向跑。", 12);
        else Say("obing", "我在隊伍後面照看。岔路若看不見大家，請把方向喊給我聽，我會循著通路跟上。", 12);
    }

    static float OpeningDuration(int index) => index == 1 ? 7.2f : 6f;

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
        regrouping = true;
        if (leg == 1)
        {
            Say("tado", "先在這段較寬的路邊站穩。前面的人不要擠在一起，留出讓傷患轉身的地方。", 11);
            Say("lubi", "古慕，把重量靠過來。腳下有突出的石頭，我們一步一步跨過去，不用趕著追。", 11);
            Say("gumu", "我還能走。只是胸口悶，腳也使不上力……讓我靠著你，慢慢走。", 10);
            Say("bawan", "前面是岔路。你去確認上坡那條通路，也留意歐冰的聲音；找到方向，可以喊給她聽。", 12);
        }
        else if (leg == 2)
        {
            Say("lubi", "這一段終於走過來了。古慕，先站穩，抓好我的肩，我不會把你留在這裡。", 11);
            Say("gumu", "我聽得見。你先換一口氣，我會抓緊。等大家排好，我們再一起走。", 10);
            Say("tado", "傷患都還跟著。前面岩肩路窄，別讓隊伍在轉彎處斷開，我會在前面照看。", 11);
            Say("bawan", "再往前就是最後的岩肩。看清通往集合地的方向，再示意隊伍過去。", 10);
        }
        else
        {
            Say("mona", "前面的人放慢，留意後方。讓傷患先到平坦的地方，不要在最後一段散開。", 11);
            Say("lubi", "古慕，已經看見集合地了。身體靠著我，腳步別亂，我們一起走完。", 10);
            Say("gumu", "我看見了……我跟著你。到了那裡，讓我們先坐一下，再清點身邊的人。", 11);
        }
    }

    void UpdateHUD()
    {
        var ui = chapter.presentation;
        bool opening = CurrentStage == Stage.Opening, following = CurrentStage == Stage.Following;
        bool separated = chapter.gas && chapter.gas.ObingSeparated;
        ui.SetObjective(opening ? "聽莫那與巴萬交代撤離" : regrouping ? "傷患整隊・聽同伴交代下一段通路" : PartyGathered ? (separated ? "主隊已抵達集合地" : "隊伍已抵達集合地") :
            following ? "等候隊伍沿路跟上" : "探路：" + legs[LegIndex].title);
        int arrived = 0, required = 0;
        foreach (var actor in party) if (RequiredForArrival(actor)) { required++; if (actor.AtDestination) arrived++; }
        ui.SetPartyStatus((opening ? "傷患先行・巴萬照應隊伍" : PartyGathered ? "主隊集結　" + required + "／" + required :
            following ? "隊伍前進中　" + arrived + "／" + required + " 抵達" :
            "第 " + (LegIndex + 1) + "／" + legs.Length + " 段　前方 " + Mathf.CeilToInt(FlatDistance(chapter.player.transform.position, ScoutTarget)) + " 公尺") +
            (separated ? "・歐冰在後方" : ""));
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
