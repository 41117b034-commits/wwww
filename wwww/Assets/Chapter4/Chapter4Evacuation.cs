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
    Chapter4RockShelter shelter;
    public bool PlayerInShelter => shelter && shelter.Sheltered(chapter.player.transform.position);
    public bool PlayerAtRefuge => CaveExit != null && CaveExit.Outside(chapter.player.transform.position);
    public bool Inside(Chapter4Companion actor) => shelter && shelter.Sheltered(actor.transform.position);
    public int DepartedCount => party.Count;
    public int FallenCount => party.FindAll(a => a.Fallen).Count;
    public int InsideCount => party.FindAll(a => RequiredForArrival(a) && CaveExit.Outside(a.transform.position)).Count;
    public string FallenNames => string.Join("、", party.FindAll(a => a.Fallen).ConvertAll(a => a.displayName.Split('·')[0]));
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
    public bool ArrivalReady => PlayerAtRefuge && LegIndex == legs.Length - 1 && !chapter.gas.BlocksPartyAdvance &&
        (!chapter.contact || !chapter.contact.Responded || chapter.contact.Reunited);
    public bool FinishingEscape => chapter.ending && chapter.ending.HasStarted && !PartyGathered;
    public int ActivePartyCount => party.Count - FallenCount;
    public IReadOnlyList<Chapter4Companion> Party => party;
    public Vector3 ScoutTarget => PointOnRoute(LegEnd);
    public float LegEnd => lengths[legs[LegIndex].routeIndex];
    public float ApproachLength { get; private set; }
    public Chapter4CaveExit CaveExit { get; private set; }
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
    bool leavingCave;
    bool restedInCave;
    public void Begin()
    {
        if (!chapter || !terrain || cast == null || cast.Length == 0 || legs == null || legs.Length == 0)
            throw new InvalidOperationException("Fourth chapter evacuation references are incomplete.");
        shelter = FindFirstObjectByType<Chapter4RockShelter>();
        if (!shelter) throw new InvalidOperationException("Missing Chapter 4 cave chamber.");
        int chamberIndex = chapter.routeCheckpoints.Length - 1;
        if (shelter.escapeTrail != null && shelter.escapeTrail.Length > 0)
        {
            var fullRoute = new List<Transform>(chapter.routeCheckpoints);
            fullRoute.AddRange(shelter.escapeTrail);
            chapter.routeCheckpoints = fullRoute.ToArray();
            chapter.gatheringPoint = fullRoute[fullRoute.Count - 1];
            legs[legs.Length - 1].routeIndex = fullRoute.Count - 1;
        }
        route = Array.ConvertAll(chapter.routeCheckpoints, t => t.position);
        lengths = new float[route.Length];
        for (int i = 1; i < route.Length; i++)
            lengths[i] = lengths[i - 1] + Vector3.ProjectOnPlane(route[i] - route[i - 1], Vector3.up).magnitude;
        ApproachLength = lengths[chamberIndex];
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
        chapter.player.Evacuation = this;
        chapter.player.SetInputEnabled(true);
        CurrentStage = Stage.Opening;
        initialized = true;
        var rock = Array.Find(FindObjectsByType<MeshFilter>(FindObjectsSortMode.None),
            m => m.name == "風化岩石" && m.sharedMesh && m.GetComponent<MeshRenderer>());
        if (!rock) throw new InvalidOperationException("Missing Chapter 4 weathered stone.");
        Action = new Chapter4RetreatAction(this, rock);
        CaveExit = new Chapter4CaveExit(this, shelter, rock, party.Find(a => a.actorId == "mona"));
        // People flee as the first order is spoken. No dialogue input lock.
        for (int i = 0; i < party.Count; i++) party[i].FollowTo(Mathf.Max(1, 12 - i * 1.2f), i * .18f);
        Say("mona", "日本軍警從霧社追上山了，槍聲就在後頭！帶上傷患，跟我往山裡走！", 0);
        UpdateHUD();
    }

    void Update()
    {
        if (!initialized || chapter.IsPaused || chapter.gas.IsCollapsed) return;
        if (chapter.ending && chapter.ending.HasStarted)
        {
            // The player has crossed the lip. The remaining survivors keep the
            // destinations they received when the slab moved, even during the film.
            if (!PartyGathered && PartyOnForestTrail()) CurrentStage = Stage.Gathered;
            return;
        }
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
            if (!leavingCave && PlayerInShelter && PlayerProgress >= ApproachLength - 6)
            {
                leavingCave = true;
                Say("mona", "再往裡走一點。外頭的腳步聲遠了，先讓受傷的人坐下。", 5);
            }
            CaveExit.Tick(leavingCave, dt);
            if (leavingCave && !CaveExit.Open && !restedInCave && !chapter.Voice.Busy &&
                chapter.gas.Gumu.Progress > CaveExit.QueueEnd - 8 && !chapter.gas.Gumu.Walking &&
                Inside(chapter.gas.Lubi) && !chapter.gas.Lubi.Walking)
            {
                restedInCave = true;
                Say("lubi", "古慕，坐穩了再鬆手。我在你旁邊，你先慢慢把氣喘勻。", 8);
            }
            if (!leavingCave && chapter.player.InteractPressed) TrySignal();
            float target = leavingCave ? RouteLength : Mathf.Min(RouteLength, Action.PartyLimit, Mathf.Max(Action.MinimumPartyAdvance, PlayerProgress + 1.5f));
            if (!CaveExit.Open) target = Mathf.Min(target, CaveExit.QueueEnd);
            for (int i = 0, slot = 0; i < party.Count; i++)
            {
                if (!RequiredForArrival(party[i])) continue;
                float position = Mathf.Max(0, target - slot * 1.45f);
                if (party[i].OnForwardFlank) position = leavingCave ? RouteLength - 1 : Mathf.Min(Action.PartyLimit, RouteLength - 1, PlayerProgress + 4.8f);
                if (!CaveExit.Open) position = Mathf.Min(position, CaveExit.QueueEnd);
                if (!party[i].IndependentFollow) party[i].FollowTo(position, 0);
                slot++;
            }
            if (LegIndex < legs.Length - 1 && PlayerProgress >= LegEnd && RearProgress >= LegEnd - 12 && !chapter.gas.BlocksPartyAdvance)
            {
                LegIndex++; stageClock = 0;
                chapter.gas.CaptureCheckpoint(PlayerProgress);
                RegroupDialogue(LegIndex);
            }
            if (ArrivalReady && PartyOnForestTrail())
            {
                CurrentStage = Stage.Gathered;
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

    public bool RequiredForArrival(Chapter4Companion actor) => !actor.Fallen && (!chapter.gas || !chapter.gas.ObingSeparated || actor.actorId != "obing" || chapter.contact && chapter.contact.Responded);

    bool PartyOnForestTrail()
    {
        foreach (var actor in party)
            if (RequiredForArrival(actor) && (!actor.AtDestination || !shelter.AtRefuge(actor.transform.position)))
                return false;
        return true;
    }

    public void Announce(string actorId, string text, float seconds) { Say(actorId, text, seconds); }

    public void RestoreCheckpoint(int leg, int signals, float scouted)
    {
        LegIndex = leg; SignalCount = signals; ScoutedDistance = scouted;
        CurrentStage = Stage.Following; stageClock = 0; regrouping = false;
        leavingCave = restedInCave = false;
        CaveExit.Reset();
        ClearSpeech();
        UpdateHUD();
    }

    void Say(string actorId, string text, float seconds)
    {
        var actor = Array.Find(cast, a => a.actorId == actorId);
        if (actor && !actor.Fallen) chapter.Voice.Enqueue(actorId, text, seconds);
    }

    void ClearSpeech()
    {
        chapter.Voice.Stop();
    }

    void RegroupDialogue(int leg)
    {
        regrouping = false;
        if (leg == 2)
            Say("tado", "前面那個彎很窄。看著前一個人的背，跟上來！", 0);
        else if (leg == 3)
            Say("lubi", "古慕，看見那個洞口了嗎？再靠過來一點，我陪你走進去。", 0);
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
        if (!CaveExit.Open && progress > CaveExit.BlockedAt && progress > current + .001f) return false;
        if (progress > AdvanceLimit && progress > current + .001f) return false;
        if (shelter && shelter.Sheltered(candidate)) return true;
        float distance = FlatDistance(candidate, PointOnRoute(progress));
        float oldDistance = FlatDistance(from, PointOnRoute(current));
        if (Action != null && Action.Rockfall && progress > Chapter4RetreatAction.ForkStart && progress < Chapter4RetreatAction.ForkEnd)
        {
            float excess = Action.BranchDistance(candidate) - Chapter4RetreatAction.CorridorWidth(progress);
            return excess <= 0 || excess < Action.BranchDistance(from) - Chapter4RetreatAction.CorridorWidth(current);
        }
        bool camp = LegIndex == 0 && progress < 3 && FlatDistance(candidate, PointOnRoute(0)) < 12;
        return camp || distance <= 4.6f || distance < oldDistance;
    }

    void UpdateHUD()
    {
        string objective;
        if (PartyGathered) objective = "族人已逃出岩窟";
        else if (PlayerAtRefuge) objective = "已逃出岩窟";
        else if (PlayerInShelter) objective = CaveExit.Objective;
        else objective = Action.Objective;
        chapter.presentation.SetObjective(objective);
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
    }
}
