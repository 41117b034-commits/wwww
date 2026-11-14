using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Dramatised fire pressure, not a reconstruction of a named historical sortie.
// Scene-owned and reversible. No outcome or old-chapter data is written here.
public sealed class Chapter4RetreatPressure : MonoBehaviour
{
    public float ImpactPulse { get; private set; }
    public bool IsTelegraphing => warningRemaining > 0;
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
    AudioSource whistle;
    Material planeMaterial;
    float warningRemaining, nextImpact, nextGate, flightClock, nextFlight, flashClock;
    bool warningHarmless, initialized, paused;
    Color oldFogColor, oldAmbient;
    float oldFogDensity, oldAmbientIntensity;
    bool oldFog;
    FogMode oldFogMode;
    Light sun;
    float oldSunIntensity;
    Color oldSunColor;
    readonly RaycastHit[] coverHits = new RaycastHit[32];

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
        whistleClip = BuildSound("Incoming shell cue", 2.8f, 2);
        Aircraft = Source("遠方飛機聲", engineClip, .62f, true);
        Impacts = Source("近處落彈聲", impactClip, .65f, false);
        whistle = Source("落彈預警聲", whistleClip, .42f, false);
        flash = new GameObject("落彈瞬光", typeof(Light)).GetComponent<Light>();
        flash.transform.SetParent(transform, false); flash.type = LightType.Point;
        flash.color = new Color(1, .69f, .35f); flash.range = 16; flash.shadows = LightShadows.None; flash.enabled = false;
        BuildAircraft();
        nextImpact = .35f; nextGate = 6; nextFlight = 0;
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
            foreach (var source in new[] { Aircraft, Impacts, whistle })
                if (paused) source.Pause(); else source.UnPause();
        }
        if (paused) return;
        float dt = Time.deltaTime;
        Elapsed += dt; ImpactPulse = Mathf.MoveTowards(ImpactPulse, 0, dt * 1.25f);
        flashClock = Mathf.Max(0, flashClock - dt); flash.enabled = flashClock > 0;
        if (flash.enabled) flash.intensity = flashClock * 20;
        if (flow.PartyGathered)
        {
            chapter.presentation.SetThreatCue("", 0); warningRemaining = 0;
            Aircraft.Stop(); whistle.Stop(); plane.gameObject.SetActive(false); return;
        }
        UpdateAircraft(dt);
        float duck = chapter.Voice.Speaking ? .58f : 1;
        Aircraft.volume = Mathf.MoveTowards(Aircraft.volume, .62f * duck, dt);
        Impacts.volume = Mathf.MoveTowards(Impacts.volume, .65f * duck, dt);
        if (warningRemaining > 0)
        {
            warningRemaining -= dt;
            if (warningRemaining <= 0) Detonate();
        }
        else
        {
            nextImpact -= dt;
            if (nextImpact <= 0) Warn();
        }
        // The next section visibly remains under fire until the party arrives.
        nextGate -= dt;
        if (nextGate <= 0 && flow.LegIndex < flow.legs.Length - 1)
        {
            var point = flow.PointOnRoute(flow.AdvanceLimit + 4);
            chapter.gas.atmosphere.BlastAt(point, false);
            nextGate = 8;
        }
        string cue = "";
        if (IsTelegraphing)
        {
            Vector3 direction = ImpactPosition - chapter.player.transform.position;
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(chapter.player.view.transform.forward, Vector3.up), Vector3.ProjectOnPlane(direction, Vector3.up), Vector3.up);
            string where = Mathf.Abs(angle) > 110 ? "後方" : angle < -25 ? "左前方" : angle > 25 ? "右前方" : "前方";
            cue = where + "落彈！離開塵柱範圍　" + Mathf.CeilToInt(warningRemaining) + " 秒";
        }
        else if (ImpactPulse > .35f) cue = chapter.player.IsVR ? "砲火逼近！握住右握把低身、沿通路撤離" : "砲火逼近！按住 Ctrl 低身、沿通路撤離";
        else if (flow.BoundaryBlocked) cue = "前方仍在落彈・先接回這一段的隊伍";
        else if (Elapsed < 9) cue = "1930 年秋・霧社山區\n日軍砲擊山林，立刻帶隊撤離";
        chapter.presentation.SetThreatCue(cue, IsTelegraphing ? .8f : ImpactPulse);
    }

    void Warn()
    {
        float player = flow.ProjectProgress(chapter.player.transform.position, 0, flow.RouteLength);
        float distance = Mathf.Clamp(player + (ImpactCount == 0 ? 8 : -1.5f), 2, flow.LegEnd - 2);
        Vector3 direction = flow.PointOnRoute(distance + 1) - flow.PointOnRoute(distance - 1);
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        ImpactPosition = flow.PointOnRoute(distance) + right * (ImpactCount % 2 == 0 ? 1.8f : -1.8f);
        if (flow.terrain.Raycast(new Ray(ImpactPosition + Vector3.up * 20, Vector3.down), out var hit, 40))
            ImpactPosition = new Vector3(ImpactPosition.x, hit.point.y, ImpactPosition.z);
        warningRemaining = 2.8f; warningHarmless = ImpactCount == 0;
        whistle.transform.position = ImpactPosition + Vector3.up * 5; whistle.Play();
        chapter.gas.atmosphere.BlastAt(ImpactPosition, true);
    }

    void Detonate()
    {
        ImpactCount++; ImpactPulse = 1; flashClock = .28f;
        flash.transform.position = ImpactPosition + Vector3.up * 2;
        Impacts.transform.position = ImpactPosition; Impacts.Play();
        chapter.gas.atmosphere.BlastAt(ImpactPosition, false);
        float distance = Chapter4Evacuation.FlatDistance(ImpactPosition, chapter.player.transform.position);
        if (!warningHarmless && distance < 5.5f)
        {
            bool covered = HasSolidCover();
            if (covered) CoveredImpacts++;
            else chapter.gas.ReceiveBlast(.34f * (chapter.player.IsBracing ? .28f : 1));
        }
        nextImpact = 10.5f;
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
        nextFlight -= dt;
        if (nextFlight <= 0)
        {
            nextFlight = 36; flightClock = 0; AircraftPasses++;
            plane.gameObject.SetActive(true); Aircraft.Play();
        }
        flightClock += dt;
        Vector3 center = flow.PointOnRoute(flow.ProjectProgress(chapter.player.transform.position, 0, flow.RouteLength) + 24);
        plane.position = center + new Vector3(-82 + flightClock * 14, 34, 10);
        Aircraft.transform.position = plane.position;
        if (flightClock > 12) { plane.gameObject.SetActive(false); Aircraft.Stop(); }
    }

    void BuildAircraft()
    {
        plane = new GameObject("遠方軍機輪廓（戲劇化示意）").transform; plane.SetParent(transform, false);
        plane.rotation = Quaternion.Euler(0, 90, 0);
        planeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        planeMaterial.color = new Color(.13f, .15f, .13f);
        Part("機身", new Vector3(0, 0, 0), new Vector3(.55f, .55f, 5));
        Part("上翼", new Vector3(0, .65f, -.2f), new Vector3(6.8f, .12f, 1.15f));
        Part("下翼", new Vector3(0, -.2f, -.2f), new Vector3(6.4f, .12f, 1.1f));
        Part("尾翼", new Vector3(0, .1f, -2), new Vector3(2.5f, .12f, .65f));
        Part("尾舵", new Vector3(0, .45f, -2), new Vector3(.1f, .9f, .7f));
    }

    void Part(string name, Vector3 position, Vector3 size)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
        Destroy(part.GetComponent<Collider>()); part.transform.SetParent(plane, false);
        part.transform.localPosition = position; part.transform.localScale = size;
        var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = planeMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    public void ResetAfterRetry()
    {
        warningRemaining = 0; nextImpact = 7; nextGate = 6; ImpactPulse = flashClock = 0;
        whistle.Stop(); Impacts.Stop(); flash.enabled = false;
        chapter.presentation.SetThreatCue("已回到集結點・沿通路撤離，及時示意隊伍", 0);
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
        foreach (var source in new[] { Aircraft, Impacts, whistle }) if (source) source.Stop();
        foreach (var clip in new[] { engineClip, impactClip, whistleClip }) if (clip) Destroy(clip);
        if (planeMaterial) Destroy(planeMaterial);
    }
}
