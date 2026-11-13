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
    public float FrontDistance { get; private set; } = -18;
    public float Exposure { get; private set; }
    public float Elapsed { get; private set; }
    public int RetryCount { get; private set; }
    public int CheckpointCount { get; private set; }
    public int CheckpointLeg => checkpoint == null ? 0 : checkpoint.leg;
    public bool Active => initialized && flow.CurrentStage != Chapter4Evacuation.Stage.Opening;
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
        atmosphere.Tick(FrontDistance, Active, chapter.WorldFrozen, Time.deltaTime);
        if (IsCollapsed)
        {
            collapseClock += Time.unscaledDeltaTime;
            float fall = Mathf.SmoothStep(0, 1, collapseClock / 1.6f);
            chapter.player.SetCollapseVisual(fall);
            chapter.presentation.SetGasStatus("毒氣使你倒下・可從最近的集結點重新撤離", 1, fall);
            if (CanRetry && !failureMenuShown)
            {
                failureMenuShown = true;
                chapter.presentation.ShowMenu("你受毒氣影響倒下了", "回到最近的集結點，\n與隊伍一起重新撤離。", "重試檢查點");
            }
            return;
        }
        if (!Active || chapter.WorldFrozen) return;
        float dt = Time.deltaTime;
        Elapsed += dt;
        // The longer voiced regrouping never lets the visual front overtake
        // the wounded formation. Players can still enter the smoke behind it.
        float rear = float.PositiveInfinity;
        foreach (var actor in flow.Party) if (flow.RequiredForArrival(actor)) rear = Mathf.Min(rear, actor.Progress);
        FrontDistance = Mathf.Min(flow.RouteLength - 28, Mathf.Max(FrontDistance, Mathf.Min(rear - 12, FrontDistance + frontSpeed * dt)));
        grace = Mathf.Max(0, grace - dt);
        UpdateGumu(dt);
        UpdateObing(dt);
        float playerDistance = flow.ProjectProgress(chapter.player.transform.position, 0, flow.RouteLength);
        float density = Mathf.Clamp01((FrontDistance + 3 - playerDistance) / 8);
        Exposure = Mathf.Clamp01(Exposure + (grace <= 0 && density > 0 ? exposurePerSecond * density : -.13f) * dt);
        string status = Exposure > .02f ? "煙霧中呼吸困難 " + Mathf.CeilToInt(Exposure * 100) + "%・沿通路往前離開濃煙" :
            "後方攻擊持續・煙霧正沿山路逼近";
        if (GumuStage == RescueStage.WalkingTogether) status += "\n露比正扶著古慕前進";
        else if (GumuStage != RescueStage.Waiting) status += "\n古慕咳嗽倒下・露比正在照應";
        chapter.presentation.SetGasStatus(status, Exposure, 0);
        if (Exposure >= 1)
        {
            IsCollapsed = true; collapseClock = 0;
            chapter.Voice.Stop();
            chapter.BeginGasCollapse();
        }
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
            gumu.walkSpeed = 1.0f; lubi.walkSpeed = 1.65f;
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
            if (GumuStage != RescueStage.WalkingTogether || obing.Progress < 28 || flow.CurrentStage != Chapter4Evacuation.Stage.Following) return;
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
            actors = new Chapter4Companion.Snapshot[flow.Party.Count] };
        for (int i = 0; i < flow.Party.Count; i++) checkpoint.actors[i] = flow.Party[i].Capture();
        CheckpointCount++;
    }

    public void RetryCheckpoint()
    {
        if (!CanRetry || checkpoint == null) return;
        for (int i = 0; i < flow.Party.Count; i++) flow.Party[i].Restore(checkpoint.actors[i]);
        GumuStage = checkpoint.rescue; ObingSeparated = checkpoint.separated; ObingSeated = checkpoint.seated; obingRecovered = checkpoint.recovered;
        if (chapter.contact) chapter.contact.Restore(checkpoint.contact);
        rescueClock = checkpoint.rescueClock; obingClock = checkpoint.obingClock;
        FrontDistance = Mathf.Min(checkpoint.front, checkpoint.distance - 18);
        Elapsed = checkpoint.elapsed; Exposure = collapseClock = 0; grace = 6;
        IsCollapsed = failureMenuShown = false; RetryCount++;
        retryAnchor.position = flow.PointOnRoute(checkpoint.distance) + Vector3.up * .06f;
        Vector3 forward = flow.PointOnRoute(checkpoint.distance + 1) - flow.PointOnRoute(checkpoint.distance);
        retryAnchor.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up));
        flow.RestoreCheckpoint(checkpoint.leg, checkpoint.signals, checkpoint.distance);
        chapter.RestoreAfterGasFailure(retryAnchor);
        atmosphere.ResetSmoke(FrontDistance);
        chapter.presentation.SetGasStatus("已回到集結點・確認通路後再示意隊伍前進", 0, 0);
    }

    public void MarkObingRejoined()
    {
        obingRecovered = true;
        ObingSeparated = ObingSeated = false;
    }
}
