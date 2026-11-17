using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Dramatised fire pressure, not a reconstruction of a named historical sortie.
// Scene-owned and reversible. No outcome or old-chapter data is written here.
public sealed class Chapter4RetreatPressure : MonoBehaviour
{
    public enum FlightState { Patrol, Approach, Strike, Rejoin }
    public FlightState Flight { get; private set; }
    public int AttackRuns { get; private set; }
    public int BombsReleased { get; private set; }
    public bool AirAttackActive => Flight == FlightState.Approach || Flight == FlightState.Strike;
    public const float BlastRadius = 5.5f;
    public const float WarningDuration = 4.5f;
    public float ImpactPulse { get; private set; }
    public bool IsTelegraphing => warningRemaining > 0;
    public float SecondsToImpact => Mathf.Max(0, warningRemaining);
    public bool PlayerInsideBlast => Chapter4Evacuation.FlatDistance(ImpactPosition, chapter.player.transform.position) < BlastRadius;
    public Chapter4ImpactMarker Marker { get; private set; }
    public Chapter4JapaneseAircraft Airframe { get; private set; }
    public Chapter4JapaneseAircraft Wingman { get; private set; }
    public Vector3 ImpactPosition { get; private set; }
    public int ImpactCount { get; private set; }
    public int CoveredImpacts { get; private set; }
    public int AircraftPasses { get; private set; }
    public float Elapsed { get; private set; }
    public AudioSource Aircraft { get; private set; }
    public AudioSource Impacts { get; private set; }
    Chapter4SceneController chapter;
    Chapter4Evacuation flow;
    Transform plane;
    Light flash;
    AudioClip engineClip, impactClip, whistleClip;
    AudioSource whistle, wingmanEngine;
    Vector3 flightOrigin, flightForward, flightRight;
    float warningRemaining, nextImpact, flightClock, flashClock;
    public const float FlightCycleSeconds = 36;
    bool warningHarmless, initialized, paused;
    Color oldFogColor, oldAmbient;
    float oldFogDensity, oldAmbientIntensity;
    bool oldFog;
    FogMode oldFogMode;
    Light sun;
    float oldSunIntensity;
    Color oldSunColor;
    readonly RaycastHit[] coverHits = new RaycastHit[32];
    Transform fallingBomb;
    Material bombMaterial;
    Vector3 attackPoint, runStart, runEnd, bombStart;
    float runClock, bombClock;
    bool bombReleased;

    public void Initialize(Chapter4SceneController owner)
    {
        chapter = owner; flow = owner.evacuation;
        chapter.presentation.EnsureRetreatUI();
        oldFog = RenderSettings.fog; oldFogMode = RenderSettings.fogMode;
        oldFogColor = RenderSettings.fogColor; oldFogDensity = RenderSettings.fogDensity;
        oldAmbient = RenderSettings.ambientLight; oldAmbientIntensity = RenderSettings.ambientIntensity;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.34f, .36f, .33f); RenderSettings.fogDensity = .017f;
        RenderSettings.ambientLight = oldAmbient * .74f; RenderSettings.ambientIntensity = oldAmbientIntensity * .72f;
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional && light.gameObject.scene == gameObject.scene)
            { sun = light; oldSunIntensity = sun.intensity; oldSunColor = sun.color; sun.intensity *= .58f; sun.color = new Color(.76f, .79f, .75f); break; }

        engineClip = BuildSound("Distant aircraft engine", 12, 0);
        impactClip = BuildSound("Forest shell impact", 2.5f, 1);
        whistleClip = BuildSound("Incoming shell cue", WarningDuration, 2);
        Aircraft = Source("遠方飛機聲", engineClip, .62f, true);
        wingmanEngine = Source("另一架日軍飛機引擎", engineClip, .48f, true);
        Impacts = Source("近處落彈聲", impactClip, .65f, false);
        whistle = Source("落彈預警聲", whistleClip, .42f, false);
        flash = new GameObject("落彈瞬光", typeof(Light)).GetComponent<Light>();
        flash.transform.SetParent(transform, false); flash.type = LightType.Point;
        flash.color = new Color(1, .69f, .35f); flash.range = 16; flash.shadows = LightShadows.None; flash.enabled = false;
        BuildAircraft();
        var bomb = GameObject.CreatePrimitive(PrimitiveType.Capsule); bomb.name = "空襲落彈・演出示意";
        Destroy(bomb.GetComponent<Collider>()); fallingBomb = bomb.transform; fallingBomb.SetParent(transform, false);
        fallingBomb.localScale = new Vector3(.23f, .48f, .23f);
        bombMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")); bombMaterial.color = new Color(.12f, .13f, .12f);
        bomb.GetComponent<Renderer>().sharedMaterial = bombMaterial; bomb.SetActive(false);
        Marker = new GameObject("落彈範圍與中心標記").AddComponent<Chapter4ImpactMarker>();
        Marker.transform.SetParent(transform, false); Marker.Initialize(flow.terrain);
        chapter.presentation.SetImpactTarget(chapter.player.view, Vector3.zero, 0, false, false);
        nextImpact = 2;
        initialized = true;
    }

    AudioSource Source(string name, AudioClip clip, float volume, bool loop)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false; source.clip = clip; source.loop = loop; source.volume = volume;
        source.spatialBlend = .85f; source.dopplerLevel = .25f;
        source.minDistance = 15; source.maxDistance = 180; source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    void Update()
    {
        if (!initialized) return;
        bool freeze = chapter.WorldFrozen || chapter.gas.IsCollapsed;
        if (freeze != paused)
        {
            paused = freeze;
            foreach (var source in new[] { Aircraft, wingmanEngine, Impacts, whistle })
                if (paused) source.Pause(); else source.UnPause();
        }
        if (paused)
        {
            if (chapter.gas.IsCollapsed) HideMarker();
            return;
        }
        float dt = Time.deltaTime;
        Elapsed += dt; ImpactPulse = Mathf.MoveTowards(ImpactPulse, 0, dt * 1.25f);
        flashClock = Mathf.Max(0, flashClock - dt); flash.enabled = flashClock > 0;
        if (flash.enabled) flash.intensity = flashClock * 20;
        if (flow.PartyGathered)
        {
            chapter.presentation.SetThreatCue("", 0); warningRemaining = 0;
            HideMarker(); fallingBomb.gameObject.SetActive(false); Aircraft.Stop(); wingmanEngine.Stop(); whistle.Stop(); plane.gameObject.SetActive(false); Wingman.gameObject.SetActive(false); return;
        }
        UpdateAircraft(dt);
        float duck = chapter.Voice.Speaking ? .58f : 1;
        Aircraft.volume = Mathf.MoveTowards(Aircraft.volume, (AirAttackActive ? .95f : .62f) * duck, dt);
        Aircraft.pitch = Mathf.MoveTowards(Aircraft.pitch, AirAttackActive ? 1.18f : 1, dt * .12f);
        wingmanEngine.volume = Mathf.MoveTowards(wingmanEngine.volume, .48f * duck, dt);
        Impacts.volume = Mathf.MoveTowards(Impacts.volume, .65f * duck, dt);
        if (warningRemaining > 0)
        {
            warningRemaining -= dt;
            if (warningRemaining <= 0) Detonate();
            else { Marker.Pulse(warningRemaining); chapter.presentation.SetImpactTarget(chapter.player.view, ImpactPosition, warningRemaining, true, PlayerInsideBlast); }
        }
        else if (!AirAttackActive && Flight == FlightState.Patrol && (flow.Action == null || !flow.Action.ControlsShells))
        {
            nextImpact -= dt;
            if (nextImpact <= 0) BeginAttackRun();
        }
        string cue = "";
        if (IsTelegraphing)
        {
            Vector3 direction = ImpactPosition - chapter.player.transform.position;
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(chapter.player.view.transform.forward, Vector3.up), Vector3.ProjectOnPlane(direction, Vector3.up), Vector3.up);
            string where = Mathf.Abs(angle) > 110 ? "後方" : angle < -25 ? "左前方" : angle > 25 ? "右前方" : "前方";
            cue = (Flight == FlightState.Strike ? "飛機投彈・" : "砲擊・") + where + " " + warningRemaining.ToString("0.0") + " 秒・" + (PlayerInsideBlast ? "立刻移出警戒線！" : "已在範圍外，繼續撤離");
        }
        else if (ImpactPulse > .35f) cue = chapter.player.IsVR ? "砲火逼近！握住右握把低身、沿通路撤離" : "砲火逼近！按住 Ctrl 低身、沿通路撤離";
        else if (Flight == FlightState.Approach) cue = "引擎聲逼近！飛機正在轉入山谷・留意前方空地";
        else if (flow.BoundaryBlocked) cue = "別落下同伴・留在撤離通路接應";
        else if (plane.gameObject.activeSelf && Mathf.Repeat(flightClock, 18) < 5) cue = AircraftDirectionCue();
        chapter.presentation.SetThreatCue(cue, IsTelegraphing ? .8f : ImpactPulse);
    }

    void BeginAttackRun()
    {
        float player = flow.ProjectProgress(chapter.player.transform.position, 0, flow.RouteLength);
        float distance = Mathf.Clamp(player + 10, 2, flow.RouteLength - 12);
        Vector3 direction = flow.PointOnRoute(distance + 1) - flow.PointOnRoute(distance - 1);
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        attackPoint = flow.PointOnRoute(distance) + right * (AttackRuns % 2 == 0 ? 1.8f : -1.8f);
        Vector3 runForward = flightForward * (AttackRuns % 2 == 0 ? 1 : -1);
        runStart = attackPoint - runForward * 42 + Vector3.up * 31;
        runEnd = attackPoint + runForward * 70 + Vector3.up * 35;
        Flight = FlightState.Approach; runClock = bombClock = 0; bombReleased = false;
    }

    public bool ScheduleImpact(Vector3 point, bool harmless, bool aircraft = false)
    {
        if (!initialized || IsTelegraphing || chapter.WorldFrozen || chapter.gas.IsCollapsed || AirAttackActive && !aircraft) return false;
        ImpactPosition = point;
        if (flow.terrain.Raycast(new Ray(point + Vector3.up * 20, Vector3.down), out var hit, 40)) ImpactPosition = hit.point;
        warningRemaining = WarningDuration; warningHarmless = harmless;
        Marker.Show(ImpactPosition, BlastRadius);
        whistle.transform.position = ImpactPosition + Vector3.up * 5; whistle.Play();
        chapter.gas.atmosphere.BlastAt(ImpactPosition, true); return true;
    }

    void Detonate()
    {
        HideMarker();
        ImpactCount++; ImpactPulse = 1; flashClock = .28f;
        flash.transform.position = ImpactPosition + Vector3.up * 2;
        Impacts.transform.position = ImpactPosition; Impacts.Play();
        chapter.gas.atmosphere.BlastAt(ImpactPosition, false);
        fallingBomb.gameObject.SetActive(false);
        if (Flight == FlightState.Strike)
        {
            // Off-path dust columns are scenery only; damage always uses the marked centre/radius.
            chapter.gas.atmosphere.BlastAt(ImpactPosition + flightRight * 12 - flightForward * 7, false);
            chapter.gas.atmosphere.BlastAt(ImpactPosition - flightRight * 14 + flightForward * 9, false);
        }
        float distance = Chapter4Evacuation.FlatDistance(ImpactPosition, chapter.player.transform.position);
        if (!warningHarmless && distance < BlastRadius)
        {
            bool covered = HasSolidCover();
            if (covered) CoveredImpacts++;
            else chapter.gas.ReceiveBlast(.34f * (chapter.player.IsBracing ? .28f : 1));
        }
        nextImpact = 9;
    }

    bool HasSolidCover()
    {
        Vector3 from = ImpactPosition + Vector3.up * .9f;
        Vector3 to = chapter.player.transform.position + Vector3.up * (chapter.player.IsBracing ? .75f : 1.35f);
        int count = Physics.RaycastNonAlloc(from, (to - from).normalized, coverHits, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var collider = coverHits[i].collider;
            if (!collider || collider == chapter.player.Motor || collider.GetComponent<Chapter4Companion>()) continue;
            return true;
        }
        return false;
    }

    void UpdateAircraft(float dt)
    {
        flightClock += dt;
        AircraftPasses = 1 + Mathf.FloorToInt(flightClock / FlightCycleSeconds);
        // Both aircraft remain real world objects throughout the retreat. Their
        // fixed valley circuit neither follows the camera nor respawns behind it.
        float angle = flightClock / FlightCycleSeconds * Mathf.PI * 2 - Mathf.PI * .5f;
        if (Flight == FlightState.Patrol) FlyCircuit(Airframe, angle, 0, dt);
        else if (Flight == FlightState.Approach)
        {
            MoveAircraft(runStart, 25, dt);
            if (Vector3.Distance(plane.position, runStart) < .1f && ScheduleImpact(attackPoint, AttackRuns == 0, true))
            { Flight = FlightState.Strike; AttackRuns++; runClock = 0; }
        }
        else if (Flight == FlightState.Strike)
        {
            runClock += dt;
            MoveAircraft(runEnd, 19, dt);
            if (!bombReleased && runClock >= 2.2f && IsTelegraphing)
            {
                bombReleased = true; BombsReleased++; bombClock = 0; bombStart = plane.position - Vector3.up;
                fallingBomb.position = bombStart; fallingBomb.gameObject.SetActive(true);
            }
            if (bombReleased && fallingBomb.gameObject.activeSelf)
            {
                bombClock += dt;
                float t = Mathf.Clamp01(bombClock / (WarningDuration - 2.2f));
                fallingBomb.position = Vector3.Lerp(bombStart, ImpactPosition, t * t);
            }
            if (runClock >= 6 && !IsTelegraphing) Flight = FlightState.Rejoin;
        }
        else
        {
            Vector3 target = CircuitPoint(angle, 0);
            MoveAircraft(target, 29, dt);
            if (Vector3.Distance(plane.position, target) < .1f) Flight = FlightState.Patrol;
        }
        if (Flight != FlightState.Patrol) Airframe.Tick(dt);
        FlyCircuit(Wingman, flightClock / FlightCycleSeconds * Mathf.PI * 2 + Mathf.PI * .5f, 7, dt);
        Aircraft.transform.position = plane.position;
        wingmanEngine.transform.position = Wingman.transform.position;
        if (!Aircraft.isPlaying) Aircraft.Play();
        if (!wingmanEngine.isPlaying) wingmanEngine.Play();
    }

    void FlyCircuit(Chapter4JapaneseAircraft aircraft, float angle, float extraHeight, float dt)
    {
        aircraft.transform.position = CircuitPoint(angle, extraHeight);
        Vector3 tangent = -flightRight * (31 * Mathf.Sin(angle)) + flightForward * (78 * Mathf.Cos(angle));
        aircraft.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0, 0, -12);
        aircraft.Tick(dt);
    }
    Vector3 CircuitPoint(float angle, float extraHeight)
    {
        Vector3 point = flightOrigin + flightRight * (31 * Mathf.Cos(angle)) + flightForward * (78 * Mathf.Sin(angle));
        float height = flightOrigin.y + 39 + extraHeight + Mathf.Sin(angle * 2) * 2;
        if (flow.terrain.Raycast(new Ray(point + Vector3.up * 100, Vector3.down), out var ground, 180)) height = Mathf.Max(height, ground.point.y + 30);
        point.y = height; return point;
    }
    void MoveAircraft(Vector3 target, float speed, float dt)
    {
        Vector3 delta = target - plane.position;
        if (delta.sqrMagnitude > .001f)
            plane.rotation = Quaternion.RotateTowards(plane.rotation, Quaternion.LookRotation(delta, Vector3.up), dt * 80);
        plane.position = Vector3.MoveTowards(plane.position, target, speed * dt);
    }

    string AircraftDirectionCue()
    {
        Vector3 eye = chapter.player.view.transform.position;
        Vector3 target = (plane.position - eye).sqrMagnitude <= (Wingman.transform.position - eye).sqrMagnitude ? plane.position : Wingman.transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(chapter.player.view.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < .01f) forward = chapter.player.transform.forward;
        float angle = Vector3.SignedAngle(forward, Vector3.ProjectOnPlane(target - eye, Vector3.up), Vector3.up);
        string where = Mathf.Abs(angle) > 125 ? "後方" : angle < -35 ? "左側" : angle > 35 ? "右側" : "前方";
        return "日軍飛機在" + where + "上空・抬頭可見機身";
    }

    void BuildAircraft()
    {
        Airframe = new GameObject("日軍雙座複葉偵察機（時代外形示意）").AddComponent<Chapter4JapaneseAircraft>();
        plane = Airframe.transform; plane.SetParent(transform, false); Airframe.Build();
        plane.localScale = Vector3.one * 1.5f;
        Wingman = new GameObject("日軍複葉機・山谷另一側巡航").AddComponent<Chapter4JapaneseAircraft>();
        Wingman.transform.SetParent(transform, false); Wingman.Build(); Wingman.transform.localScale = Vector3.one * 1.15f;
        Vector3 start = flow.BasePoint(0), end = flow.BasePoint(flow.RouteLength);
        flightOrigin = (start + end) * .5f;
        flightForward = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;
        flightRight = Vector3.Cross(Vector3.up, flightForward);
        FlyCircuit(Airframe, -Mathf.PI * .5f, 0, 0);
        FlyCircuit(Wingman, Mathf.PI * .5f, 7, 0);
    }
    void HideMarker()
    { Marker.Hide(); chapter.presentation.SetImpactTarget(chapter.player.view, ImpactPosition, 0, false, false); }

    public void ResetAfterRetry()
    {
        warningRemaining = 0; nextImpact = 7; ImpactPulse = flashClock = 0;
        Flight = FlightState.Rejoin; runClock = bombClock = 0; bombReleased = false; fallingBomb.gameObject.SetActive(false);
        HideMarker();
        whistle.Stop(); Impacts.Stop(); flash.enabled = false;
        chapter.presentation.SetThreatCue("已回到接應處・留意砲火空檔，帶著同伴撤離", 0);
    }

    static AudioClip BuildSound(string name, float seconds, int kind)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)]; var random = new System.Random(1930 + kind);
        float low = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, noise = (float)random.NextDouble() * 2 - 1;
            low = Mathf.Lerp(low, noise, .09f);
            float value = kind == 0 ? (.22f * Mathf.Sin(t * 2 * Mathf.PI * 93) + .15f * Mathf.Sin(t * 2 * Mathf.PI * 141) + low * .5f) * (.7f + .3f * Mathf.Sin(t * 2 * Mathf.PI * 11))
                : kind == 1 ? (low * 2.6f + .27f * Mathf.Sin(t * 2 * Mathf.PI * 48)) * Mathf.Exp(-t * 2.2f) * Mathf.Min(1, t * 140)
                : Mathf.Sin(2 * Mathf.PI * (1250 * t - 115 * t * t)) * .2f * Mathf.Sin(Mathf.PI * t / seconds);
            samples[i] = Mathf.Clamp(value * Mathf.Min(1, Mathf.Min(t, seconds - t) * 30), -.8f, .8f);
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
    }

    void OnDestroy()
    {
        if (!initialized) return;
        if (SceneManager.GetActiveScene() == gameObject.scene)
        {
            RenderSettings.fog = oldFog; RenderSettings.fogMode = oldFogMode;
            RenderSettings.fogColor = oldFogColor; RenderSettings.fogDensity = oldFogDensity;
            RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientIntensity = oldAmbientIntensity;
        }
        if (sun) { sun.intensity = oldSunIntensity; sun.color = oldSunColor; }
        foreach (var source in new[] { Aircraft, wingmanEngine, Impacts, whistle }) if (source) source.Stop();
        foreach (var clip in new[] { engineClip, impactClip, whistleClip }) if (clip) Destroy(clip);
        if (bombMaterial) Destroy(bombMaterial);
    }
}
