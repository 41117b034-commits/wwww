using System;
using UnityEngine;

// Local, reversible retreat pressure and fixed performances. No ending, death
// result, persistent checkpoint, or previous-chapter save is written here.
[DefaultExecutionOrder(50)]
public sealed class Chapter4GasSequence : MonoBehaviour
{
    public enum RescueStage { Waiting, Coughing, Falling, HelperApproaching, Lifting, WalkingTogether }
    public Chapter4SceneController chapter;
    public Chapter4Atmosphere atmosphere;
    public float frontSpeed = .55f;
    public float exposurePerSecond = .09f;
    public RescueStage GumuStage { get; private set; }
    public bool ObingSeparated { get; private set; }
    public bool ObingSeated { get; private set; }
    public bool IsCollapsed { get; private set; }
    public bool CanRetry => IsCollapsed && collapseClock >= 1.6f;
    public float FrontDistance { get; private set; } = -12;
    public float Exposure { get; private set; }
    public float BlastStress { get; private set; }
    public string ImpactCause { get; private set; } = "落彈衝擊";
    public float DecisionClock { get; private set; }
    public float Elapsed { get; private set; }
    public int RetryCount { get; private set; }
    public int CheckpointCount { get; private set; }
    public const float WaveDuration = 24;
    public float WaveAge { get; private set; } = WaveDuration;
    public int GasWaves { get; private set; }
    public Vector3 GasOrigin { get; private set; }
    public float PlayerDensity { get; private set; }
    public bool GasWaveActive => WaveAge < WaveDuration;
    public float WaveStrength => WaveAge < 2 ? Mathf.SmoothStep(0, 1, WaveAge / 2) :
        WaveAge < 11 ? 1 : 1 - Mathf.SmoothStep(0, 1, (WaveAge - 11) / (WaveDuration - 11));
    bool waveHarmless;
    public int CheckpointLeg => checkpoint == null ? 0 : checkpoint.leg;
    public bool Active => initialized;
    public bool BlocksPartyAdvance => GumuStage != RescueStage.Waiting && GumuStage != RescueStage.WalkingTogether || ObingSeparated && !ObingSeated;
    public Chapter4Companion Gumu => gumu;
    public Chapter4Companion Lubi => lubi;
    public Chapter4Companion Obing => obing;

    Chapter4Evacuation flow;
    Chapter4Companion gumu, lubi, obing;
    float rescueClock, obingClock, collapseClock, grace;
    bool initialized, failureMenuShown, obingRecovered;
    Transform retryAnchor;
    Checkpoint checkpoint;
    sealed class Checkpoint
    {
        public int leg, signals;
        public float distance, front, elapsed, rescueClock, obingClock;
        public RescueStage rescue;
        public bool separated, seated, recovered;
        public Chapter4Contact.Snapshot contact;
        public Chapter4RetreatAction.Snapshot action;
        public Chapter4Companion.Snapshot[] actors;
    }

    public void Begin()
    {
        flow = chapter.evacuation;
        foreach (var actor in flow.Party)
        {
            if (actor.actorId == "gumu") gumu = actor;
            if (actor.actorId == "lubi") lubi = actor;
            if (actor.actorId == "obing") obing = actor;
        }
        if (!gumu || !lubi || !obing || !atmosphere)
            throw new InvalidOperationException("Fourth chapter gas sequence references are incomplete.");
        retryAnchor = new GameObject("撤離檢查點（執行期間）").transform;
        retryAnchor.SetParent(transform, false);
        chapter.presentation.EnsureGasUI();
        atmosphere.Begin(flow);
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (chapter.WorldFrozen || IsCollapsed) atmosphere.Tick(FrontDistance, Active, true, 0);
        if (IsCollapsed)
        {
            collapseClock += Time.unscaledDeltaTime;
            float fall = Mathf.SmoothStep(0, 1, collapseClock / 1.6f);
            chapter.player.SetCollapseVisual(fall);
            string cause = Exposure >= 1 ? "濃煙窒息" : ImpactCause;
            chapter.presentation.SetGasStatus(cause + "中倒下・從最近集結點重新撤離", 1, fall);
            if (CanRetry && !failureMenuShown)
            {
                failureMenuShown = true;
                chapter.presentation.ShowMenu("你因" + cause + "而倒下了", "回到最近的集結點。\n避開落彈範圍，接應隊伍並持續撤離。", "重試檢查點");
            }
            return;
        }
        if (!Active || chapter.WorldFrozen) return;
        float dt = Time.deltaTime;
        WaveAge = flow.PartyGathered ? WaveDuration : Mathf.Min(WaveDuration, WaveAge + dt);
        Elapsed += dt;
        bool deciding = flow.Action != null && (flow.Action.Crossing == Chapter4RetreatAction.CrossingState.Gap ||
            flow.Action.Rockfall && flow.Action.Choice == Chapter4RetreatAction.RouteChoice.Undecided);
        DecisionClock = deciding ? DecisionClock + dt : 0;
        float gap = flow.PlayerProgress - FrontDistance;
        float advance = frontSpeed * 1.7f + Mathf.Clamp((gap - 23) * .035f, 0, .5f) + Mathf.Clamp((DecisionClock - 20) * .025f, 0, .45f);
        // No permanent rear-of-party safety leash. During automatic assistance
        // and actual following the rally point offers a pocket, but an ignored
        // signal lets smoke sweep through that pocket too.
        float ceiling = flow.PartyGathered ? flow.RouteLength - 18 : flow.RouteLength;
        if (BlocksPartyAdvance || chapter.contact.CurrentState == Chapter4Contact.State.Rising)
            ceiling = Mathf.Min(ceiling, flow.RearProgress - 10, flow.PlayerProgress - 10);
        FrontDistance = Mathf.Max(FrontDistance, Mathf.Min(ceiling, FrontDistance + advance * dt));
        grace = Mathf.Max(0, grace - dt);
        UpdateGumu(dt);
        UpdateObing(dt);
        PlayerDensity = DensityAt(chapter.player.transform.position);
        atmosphere.Tick(FrontDistance, Active, false, dt);
        // A player actively trying to advance at the formation tether cannot
        // outrun a new cloud until the injured companions catch up. Preserve
        // the same recovery pocket as automatic lifting; voluntary lingering
        // elsewhere still accumulates the full wave's exposure.
        bool waitingForParty = flow.BoundaryBlocked && flow.PlayerProgress >= flow.RearProgress + 23.5f;
        bool assisting = BlocksPartyAdvance || chapter.contact.CurrentState == Chapter4Contact.State.Rising || waitingForParty;
        Exposure = Mathf.Clamp01(Exposure + (grace <= 0 && !waveHarmless && !assisting && PlayerDensity > .06f ? exposurePerSecond * PlayerDensity : -.13f) * dt);
        BlastStress = Mathf.Max(0, BlastStress - .012f * dt);
        string status = Exposure > .02f ? "毒氣中呼吸困難 " + Mathf.CeilToInt(Exposure * 100) + "%・沿通路往前離開毒氣" :
            BlastStress > .08f ? ImpactCause + " " + Mathf.CeilToInt(BlastStress * 100) + "%・利用岩石掩蔽，別停在火線" :
            GasWaveActive ? (WaveAge < 11 ? "瓦斯彈煙霧正在擴散・離開濃霧，沿路撤離" : "這波煙霧正在消散・趁視線恢復繼續撤離") :
            "煙霧暫時散去・留意上空下一次投彈";
        if (DecisionClock > 28 && deciding && Exposure <= .02f && BlastStress <= .08f) status = "追兵正逼近！抓住砲火空檔，帶著隊伍離開";
        if (GumuStage == RescueStage.WalkingTogether) status += "\n露比正扶著古慕前進";
        else if (GumuStage != RescueStage.Waiting) status += "\n古慕咳嗽倒下・露比正在照應";
        chapter.presentation.SetGasStatus(status, Mathf.Max(Exposure, BlastStress), 0, PlayerDensity);
        if (Exposure >= 1 || BlastStress >= .99f)
        {
            IsCollapsed = true; collapseClock = 0;
            chapter.Voice.Stop();
            chapter.BeginGasCollapse();
        }
    }

    public void ReleaseGasAt(Vector3 point, bool harmless)
    {
        if (!initialized || chapter.WorldFrozen || IsCollapsed || flow.PartyGathered) return;
        GasOrigin = point; WaveAge = 0; GasWaves++; waveHarmless = harmless;
        atmosphere.GasAt(point);
    }

    public float DensityAt(Vector3 point)
    {
        if (!initialized || !GasWaveActive) return 0;
        float radius = Mathf.Lerp(9, 25, Mathf.Clamp01(WaveAge / 5));
        float local = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(radius * .35f, radius, Chapter4Evacuation.FlatDistance(point, GasOrigin)));
        float progress = flow.ProjectProgress(point, 0, flow.RouteLength);
        float rear = Mathf.Clamp01((FrontDistance + 3 - progress) / 8);
        return WaveStrength * Mathf.Max(local, rear);
    }

    void UpdateGumu(float dt)
    {
        if (GumuStage == RescueStage.Waiting)
        {
            if (flow.CurrentStage != Chapter4Evacuation.Stage.Following || gumu.Progress < 18) return;
            gumu.MotionHeld = true;
            gumu.Pose.Set(Chapter4CompanionPose.State.Coughing);
            GumuStage = RescueStage.Coughing; rescueClock = 0;
            atmosphere.WispAt(gumu.transform.position);
            flow.Announce("gumu", "咳、咳……我的腳……站不穩了……", 4.4f);
            return;
        }
        rescueClock += dt;
        if (GumuStage == RescueStage.Coughing && rescueClock >= 2.3f)
        {
            GumuStage = RescueStage.Falling; rescueClock = 0;
        }
        else if (GumuStage == RescueStage.Falling)
        {
            gumu.Pose.Set(Chapter4CompanionPose.State.SideFall, Mathf.SmoothStep(0, 1, rescueClock / 1.5f));
            if (rescueClock < 1.5f) return;
            GumuStage = RescueStage.HelperApproaching; rescueClock = 0;
            lubi.SupportPartner = gumu;
            lubi.AssistTarget = gumu.transform.position + gumu.RouteRight(gumu.Progress) * .72f;
            flow.Announce("lubi", "古慕，靠著我。慢慢起來，我扶你一起走。", 5.8f);
        }
        else if (GumuStage == RescueStage.HelperApproaching)
        {
            if (!lubi.AssistTarget.HasValue || Chapter4Evacuation.FlatDistance(lubi.transform.position, lubi.AssistTarget.Value) > .16f) return;
            lubi.AssistTarget = null; lubi.MotionHeld = true;
            lubi.Rig.Face(gumu.transform.position - lubi.transform.position);
            GumuStage = RescueStage.Lifting; rescueClock = 0;
        }
        else if (GumuStage == RescueStage.Lifting)
        {
            float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.7f, 3.8f, rescueClock));
            lubi.Rig.Face(Vector3.Slerp((gumu.transform.position - lubi.transform.position).normalized, gumu.Rig.Forward, rise));
            gumu.Pose.Set(Chapter4CompanionPose.State.SideFall, 1 - rise);
            lubi.Pose.Set(Chapter4CompanionPose.State.Helping, (1 - rise * .85f) * Mathf.Clamp01(rescueClock / .65f));
            if (rescueClock < 3.8f) return;
            gumu.MotionHeld = lubi.MotionHeld = false;
            gumu.SupportFollower = lubi;
            gumu.RouteOffset = -.35f;
            gumu.walkSpeed = 1.3f; lubi.walkSpeed = 1.9f;
            gumu.Pose.Set(Chapter4CompanionPose.State.Supported);
            lubi.Pose.Set(Chapter4CompanionPose.State.Supporting);
            GumuStage = RescueStage.WalkingTogether; rescueClock = 0;
            flow.Announce("bawan", "露比已扶住古慕。她們一起跟上，你繼續看好前面的通路。", 6.2f);
        }
    }

    void UpdateObing(float dt)
    {
        if (obingRecovered) return;
        if (!ObingSeparated)
        {
            if (GumuStage != RescueStage.WalkingTogether || obing.Progress < 84 || flow.CurrentStage != Chapter4Evacuation.Stage.Following) return;
            ObingSeparated = true; obingClock = 0;
            obing.MotionHeld = true;
            obing.Pose.Set(Chapter4CompanionPose.State.Coughing);
            atmosphere.WispAt(obing.transform.position);
            flow.Announce("obing", "咳……你們先往前，我……喘一口氣……", 5f);
        }
        else if (!ObingSeated)
        {
            obingClock += dt;
            if (obingClock >= 2.4f)
                obing.Pose.Set(Chapter4CompanionPose.State.Seated, Mathf.SmoothStep(0, 1, (obingClock - 2.4f) / 1.8f));
            if (obingClock >= 4.2f)
            {
                ObingSeated = true;
                flow.Announce("bawan", "歐冰在隊末咳得厲害，坐下了。前面的人沿通路靠攏，別散開。", 6.2f);
            }
        }
    }

    public void CaptureCheckpoint(float distance)
    {
        if (!initialized || IsCollapsed) return;
        checkpoint = new Checkpoint { leg = flow.LegIndex, signals = flow.SignalCount, distance = distance,
            front = FrontDistance, elapsed = Elapsed, rescueClock = rescueClock, obingClock = obingClock,
            rescue = GumuStage, separated = ObingSeparated, seated = ObingSeated, recovered = obingRecovered,
            contact = chapter.contact ? chapter.contact.Capture() : null,
            action = flow.Action.Capture(),
            actors = new Chapter4Companion.Snapshot[flow.Party.Count] };
        for (int i = 0; i < flow.Party.Count; i++) checkpoint.actors[i] = flow.Party[i].Capture();
        CheckpointCount++;
    }

    public void RetryCheckpoint()
    {
        if (!CanRetry || checkpoint == null) return;
        flow.Action.Restore(checkpoint.action);
        for (int i = 0; i < flow.Party.Count; i++) flow.Party[i].Restore(checkpoint.actors[i]);
        GumuStage = checkpoint.rescue; ObingSeparated = checkpoint.separated; ObingSeated = checkpoint.seated; obingRecovered = checkpoint.recovered;
        if (chapter.contact) chapter.contact.Restore(checkpoint.contact);
        rescueClock = checkpoint.rescueClock; obingClock = checkpoint.obingClock;
        FrontDistance = Mathf.Min(checkpoint.front, checkpoint.distance - 18);
        Elapsed = checkpoint.elapsed; Exposure = BlastStress = DecisionClock = collapseClock = 0; grace = 6; ImpactCause = "落彈衝擊";
        WaveAge = WaveDuration; PlayerDensity = 0; waveHarmless = false;
        IsCollapsed = failureMenuShown = false; RetryCount++;
        retryAnchor.position = flow.PointOnRoute(checkpoint.distance) + Vector3.up * .06f;
        Vector3 forward = flow.PointOnRoute(checkpoint.distance + 1) - flow.PointOnRoute(checkpoint.distance);
        retryAnchor.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up));
        flow.RestoreCheckpoint(checkpoint.leg, checkpoint.signals, checkpoint.distance);
        chapter.RestoreAfterGasFailure(retryAnchor);
        atmosphere.ResetSmoke(FrontDistance);
        if (chapter.Retreat) chapter.Retreat.ResetAfterRetry();
        chapter.presentation.SetGasStatus("已回到接應處・觀察砲火空檔，沿路撤離", 0, 0);
    }

    public void MarkObingRejoined()
    {
        obingRecovered = true;
        ObingSeparated = ObingSeated = false;
    }

    public void ReceiveBlast(float strength)
    {
        if (!initialized || chapter.WorldFrozen || IsCollapsed || grace > 0) return;
        ImpactCause = "落彈衝擊";
        BlastStress = Mathf.Clamp01(BlastStress + Mathf.Max(0, strength));
    }
    public void ReceiveGunfire(float strength)
    {
        if (!initialized || chapter.WorldFrozen || IsCollapsed || grace > 0) return;
        ImpactCause = "追兵射擊";
        BlastStress = Mathf.Clamp01(BlastStress + Mathf.Max(0, strength));
    }
}
