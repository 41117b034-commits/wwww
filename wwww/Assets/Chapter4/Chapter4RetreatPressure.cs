using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Dramatised fire pressure, not a reconstruction of a named historical sortie.
// Scene-owned and reversible. No outcome or old-chapter data is written here.
public sealed class Chapter4RetreatPressure : MonoBehaviour
{
    public enum FlightState { Patrol, Approach, Strike, Rejoin }
    enum EffectSound { AirRush, Impact, ShellWhistle, Gas }
    public FlightState Flight { get; private set; }
    public int AttackRuns { get; private set; }
    public int BombsReleased { get; private set; }
    public bool AirAttackActive => Flight == FlightState.Approach || Flight == FlightState.Strike;
    public const float BlastRadius = 5.5f;
    public const float WarningDuration = 4.5f;
    public float ImpactPulse { get; private set; }
    public bool IsTelegraphing => warningRemaining > 0;
    public bool GasIncoming => IsTelegraphing && warningGas;
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
    AudioClip airRushClip, impactClip, whistleClip, gasClip;
    AudioSource whistle, wingmanEngine;
    AudioLowPassFilter leadAirFilter, wingAirFilter;
    Vector3 flightOrigin, flightForward, flightRight;
    float warningRemaining, nextImpact, flightClock, flashClock;
    public const float FlightCycleSeconds = 36;
    bool warningHarmless, warningGas, attackGas, initialized, paused;
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

        airRushClip = BuildSound("Aircraft pass - air rush", WarningDuration, EffectSound.AirRush);
        impactClip = BuildSound("Forest shell impact", 2.5f, EffectSound.Impact);
        whistleClip = BuildSound("Incoming shell cue", WarningDuration, EffectSound.ShellWhistle);
        gasClip = BuildSound("Gas shell release - dramatised", 3.5f, EffectSound.Gas);
        Aircraft = EngineSource("遠方飛機聲", "Biplane_Main", out leadAirFilter);
        wingmanEngine = EngineSource("另一架日軍飛機引擎", "Biplane_Wing", out wingAirFilter);
        Impacts = Source("近處落彈聲", impactClip, .65f, false);
        whistle = Source("落彈預警聲", whistleClip, .42f, false);
        flash = new GameObject("落彈瞬光", typeof(Light)).GetComponent<Light>();
        flash.transform.SetParent(transform, false); flash.type = LightType.Point;
        flash.color = new Color(1, .69f, .35f); flash.range = 16; flash.shadows = LightShadows.None; flash.enabled = false;
        BuildAircraft();
        Aircraft.transform.position = plane.position;
        wingmanEngine.transform.position = Wingman.transform.position;
        var bomb = GameObject.CreatePrimitive(PrimitiveType.Capsule); bomb.name = "空襲落彈・演出示意";
        Destroy(bomb.GetComponent<Collider>()); fallingBomb = bomb.transform; fallingBomb.SetParent(transform, false);
        fallingBomb.localScale = new Vector3(.3f, .58f, .3f);
        bombMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")); bombMaterial.color = new Color(.12f, .13f, .12f);
        bomb.GetComponent<Renderer>().sharedMaterial = bombMaterial; bomb.SetActive(false);
        Marker = new GameObject("落彈範圍與中心標記").AddComponent<Chapter4ImpactMarker>();
        Marker.transform.SetParent(transform, false); Marker.Initialize(flow.terrain);
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

    AudioSource EngineSource(string name, string recording, out AudioLowPassFilter airFilter)
    {
        var clip = Resources.Load<AudioClip>("Chapter4Aircraft/" + recording);
        var source = Source(name, clip, 0, true);
        source.spatialBlend = 1;
        source.dopplerLevel = 1;
        source.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
        source.minDistance = 20;
        source.maxDistance = 220;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        airFilter = source.gameObject.AddComponent<AudioLowPassFilter>();
        airFilter.lowpassResonanceQ = 1;
        airFilter.cutoffFrequency = 1100;
        return source;
    }

    void MixEngine(AudioSource source, AudioLowPassFilter airFilter, float gain, bool sheltered, float dt)
    {
        float distance = Vector3.Distance(chapter.player.view.transform.position, source.transform.position);
        float proximity = Mathf.InverseLerp(155, 25, distance);
        float cutoff = sheltered ? 550 : Mathf.Lerp(850, 6200, proximity * proximity);
        airFilter.cutoffFrequency = Mathf.Lerp(airFilter.cutoffFrequency, cutoff, 1 - Mathf.Exp(-6 * dt));
        source.volume = Mathf.MoveTowards(source.volume, sheltered ? 0 : gain, dt * (sheltered ? 1 : 1.6f));
        if (sheltered)
        {
            if (source.volume <= 0 && source.isPlaying) source.Stop();
        }
        else if (!source.isPlaying && source.clip)
        {
            source.time = Mathf.Repeat(flightClock, source.clip.length);
            source.Play();
        }
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
        float mist = chapter.gas.PlayerDensity;
        RenderSettings.fogDensity = .017f + mist * .055f;
        RenderSettings.fogColor = Color.Lerp(new Color(.34f, .36f, .33f), new Color(.55f, .56f, .48f), mist * .75f);
        Elapsed += dt; ImpactPulse = Mathf.MoveTowards(ImpactPulse, 0, dt * 1.25f);
        flashClock = Mathf.Max(0, flashClock - dt); flash.enabled = flashClock > 0;
        if (flash.enabled) flash.intensity = flashClock * 20;
        bool sheltered = flow.PlayerInShelter || flow.PartyGathered;
        if (plane.gameObject.activeSelf == sheltered)
        {
            plane.gameObject.SetActive(!sheltered);
            Wingman.gameObject.SetActive(!sheltered);
        }
        if (!sheltered) UpdateAircraft(dt);
        float duck = chapter.Voice.Speaking ? .58f : 1;
        MixEngine(Aircraft, leadAirFilter, .88f * duck, sheltered, dt);
        MixEngine(wingmanEngine, wingAirFilter, .65f * duck, sheltered, dt);
        if (sheltered)
        {
            warningRemaining = 0;
            HideMarker(); fallingBomb.gameObject.SetActive(false); whistle.Stop(); return;
        }
        Impacts.volume = Mathf.MoveTowards(Impacts.volume, .65f * duck, dt);
        if (warningRemaining > 0)
        {
            warningRemaining -= dt;
            if (warningRemaining <= 0) Detonate();
            else Marker.Pulse(warningRemaining);
        }
        else if (!AirAttackActive && Flight == FlightState.Patrol && (flow.Action == null || !flow.Action.ControlsShells))
        {
            nextImpact -= dt;
            if (nextImpact <= 0) BeginAttackRun();
        }
    }

    void BeginAttackRun()
    {
        float player = flow.ProjectProgress(chapter.player.transform.position, 0, flow.RouteLength);
        float distance = Mathf.Clamp(player + 10, 2, flow.ApproachLength - 12);
        Vector3 direction = flow.PointOnRoute(distance + 1) - flow.PointOnRoute(distance - 1);
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        attackPoint = flow.PointOnRoute(distance) + right * (AttackRuns % 2 == 0 ? 1.8f : -1.8f);
        attackGas = AttackRuns % 2 == 0;
        Vector3 runForward = flightForward * (AttackRuns % 2 == 0 ? 1 : -1);
        runStart = attackPoint - runForward * 42 + Vector3.up * 31;
        runEnd = attackPoint + runForward * 70 + Vector3.up * 35;
        Flight = FlightState.Approach; runClock = bombClock = 0; bombReleased = false;
    }

    public bool ScheduleImpact(Vector3 point, bool harmless, bool aircraft = false, bool gas = false)
    {
        if (!initialized || flow.PlayerInShelter || IsTelegraphing || chapter.WorldFrozen || chapter.gas.IsCollapsed || AirAttackActive && !aircraft) return false;
        ImpactPosition = point;
        if (flow.terrain.Raycast(new Ray(point + Vector3.up * 20, Vector3.down), out var hit, 40)) ImpactPosition = hit.point;
        warningRemaining = WarningDuration; warningHarmless = harmless; warningGas = gas;
        Marker.Show(ImpactPosition, BlastRadius);
        whistle.transform.position = ImpactPosition + Vector3.up * 5;
        whistle.clip = aircraft ? airRushClip : whistleClip;
        whistle.Play();
        chapter.gas.atmosphere.BlastAt(ImpactPosition, true); return true;
    }

    void Detonate()
    {
        HideMarker();
        ImpactCount++;
        fallingBomb.gameObject.SetActive(false);
        if (warningGas)
        {
            // A gas landing releases a cloud, not the explosive shell's blast
            // damage/fire flash. First sortie teaches the visible wave safely.
            ImpactPulse = .25f; flashClock = 0;
            Impacts.transform.position = ImpactPosition; Impacts.PlayOneShot(gasClip, .7f);
            chapter.gas.ReleaseGasAt(ImpactPosition, warningHarmless);
            nextImpact = 9;
            return;
        }
        ImpactPulse = 1; flashClock = .28f;
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
            if (Vector3.Distance(plane.position, runStart) < .1f && ScheduleImpact(attackPoint, AttackRuns == 0, true, attackGas))
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
                fallingBomb.rotation = Quaternion.FromToRotation(Vector3.up, bombStart - ImpactPosition);
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

    void BuildAircraft()
    {
        Airframe = new GameObject("日軍雙座複葉偵察機（時代外形示意）").AddComponent<Chapter4JapaneseAircraft>();
        plane = Airframe.transform; plane.SetParent(transform, false); Airframe.Build();
        plane.localScale = Vector3.one * 1.5f;
        Wingman = new GameObject("日軍複葉機・山谷另一側巡航").AddComponent<Chapter4JapaneseAircraft>();
        Wingman.transform.SetParent(transform, false); Wingman.Build(); Wingman.transform.localScale = Vector3.one * 1.15f;
        Vector3 start = flow.BasePoint(0), end = flow.BasePoint(flow.ApproachLength);
        flightOrigin = (start + end) * .5f;
        flightForward = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;
        flightRight = Vector3.Cross(Vector3.up, flightForward);
        FlyCircuit(Airframe, -Mathf.PI * .5f, 0, 0);
        FlyCircuit(Wingman, Mathf.PI * .5f, 7, 0);
    }
    void HideMarker()
    { Marker.Hide(); }

    public void ResetAfterRetry()
    {
        warningRemaining = 0; nextImpact = 7; ImpactPulse = flashClock = 0;
        warningGas = attackGas = false;
        RenderSettings.fogDensity = .017f; RenderSettings.fogColor = new Color(.34f, .36f, .33f);
        Flight = FlightState.Rejoin; runClock = bombClock = 0; bombReleased = false; fallingBomb.gameObject.SetActive(false);
        HideMarker();
        whistle.Stop(); Impacts.Stop(); flash.enabled = false;
    }

    static AudioClip BuildSound(string name, float seconds, EffectSound kind)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)]; var random = new System.Random(1930 + (int)kind);
        float low = 0, rumble = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, noise = (float)random.NextDouble() * 2 - 1;
            low = Mathf.Lerp(low, noise, .09f);
            float value;
            switch (kind)
            {
                case EffectSound.AirRush:
                    rumble = Mathf.Lerp(rumble, low, .022f);
                    value = (low - .75f * rumble) * 1.35f * Mathf.SmoothStep(.08f, 1, t / seconds);
                    break;
                case EffectSound.Impact:
                    value = (low * 2.6f + .27f * Mathf.Sin(t * 2 * Mathf.PI * 48)) * Mathf.Exp(-t * 2.2f) * Mathf.Min(1, t * 140);
                    break;
                case EffectSound.ShellWhistle:
                    value = Mathf.Sin(2 * Mathf.PI * (1250 * t - 115 * t * t)) * .2f * Mathf.Sin(Mathf.PI * t / seconds);
                    break;
                default:
                    value = (low * .8f + noise * .12f) * Mathf.Exp(-t * .85f) * Mathf.Min(1, t * 25);
                    break;
            }
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
        foreach (var clip in new[] { airRushClip, impactClip, whistleClip, gasClip }) if (clip) Destroy(clip);
        if (bombMaterial) Destroy(bombMaterial);
    }
}
