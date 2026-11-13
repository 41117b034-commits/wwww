using UnityEngine;
using UnityEngine.Rendering;

// Scene-owned smoke and distant attack ambience. The continuous loop is
// synthesized locally; the rifle report reuses an existing project recording.
public sealed class Chapter4Atmosphere : MonoBehaviour
{
    public Material smokeMaterial;
    public AudioClip rifleReport;
    public AudioSource Barrage { get; private set; }
    public AudioSource Rifles { get; private set; }
    public int ParticleCount
    {
        get { int count = 0; if (banks != null) foreach (var bank in banks) count += bank.particleCount; return count; }
    }
    Chapter4Evacuation flow;
    ParticleSystem[] banks;
    Material instanceMaterial;
    Texture2D softParticle;
    AudioClip barrageClip;
    float shotClock;
    bool playing, paused;

    public void Begin(Chapter4Evacuation owner)
    {
        flow = owner;
        instanceMaterial = new Material(smokeMaterial);
        softParticle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        softParticle.name = "Runtime soft retreat smoke";
        softParticle.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
            float alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 1.6f);
            pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
        }
        softParticle.SetPixels(pixels); softParticle.Apply(false, true);
        instanceMaterial.SetTexture("_BaseMap", softParticle);
        banks = new ParticleSystem[3];
        for (int i = 0; i < banks.Length; i++)
        {
            var go = new GameObject("後方推進煙霧 " + (i + 1)); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 12; main.startLifetime = new ParticleSystem.MinMaxCurve(8, 13);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .36f); main.startSize = new ParticleSystem.MinMaxCurve(6, 11);
            main.startColor = new Color(.55f, .58f, .46f, .48f); main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.useUnscaledTime = false;
            var emission = ps.emission; emission.rateOverTime = 4;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(15, 1.4f, 6);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.y = .2f; velocity.space = ParticleSystemSimulationSpace.World;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.9f, .2f), new GradientAlphaKey(.7f, .7f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = instanceMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false; renderer.maxParticleSize = .9f;
            banks[i] = ps;
        }
        var sound = new GameObject("後方山林攻擊聲"); sound.transform.SetParent(transform, false);
        Barrage = sound.AddComponent<AudioSource>(); Rifles = sound.AddComponent<AudioSource>();
        foreach (var source in new[] { Barrage, Rifles })
        {
            source.playOnAwake = false; source.spatialBlend = .75f; source.dopplerLevel = 0;
            source.minDistance = 16; source.maxDistance = 230; source.rolloffMode = AudioRolloffMode.Linear;
            source.priority = 180;
        }
        barrageClip = BuildBarrage();
        Barrage.clip = barrageClip; Barrage.loop = true; Barrage.volume = .35f;
        Rifles.volume = .18f;
        ResetSmoke(-18);
    }

    public void Tick(float front, bool active, bool pause, float dt)
    {
        if (banks == null) return;
        Place(front);
        if (!active) return;
        if (!playing)
        {
            playing = true; Barrage.Play();
            foreach (var ps in banks) ps.Play();
        }
        if (pause != paused)
        {
            paused = pause;
            if (paused) { Barrage.Pause(); Rifles.Pause(); foreach (var ps in banks) ps.Pause(); }
            else { Barrage.UnPause(); Rifles.UnPause(); foreach (var ps in banks) ps.Play(); }
        }
        if (paused) return;
        shotClock -= dt;
        if (shotClock <= 0 && rifleReport)
        {
            Rifles.PlayOneShot(rifleReport); shotClock = 3.4f;
        }
    }

    void Place(float front)
    {
        for (int i = 0; i < banks.Length; i++) banks[i].transform.position = Extended(front - i * 12) + Vector3.up * 1.4f;
        if (Barrage) Barrage.transform.position = Extended(front - 25) + Vector3.up * 3;
    }

    Vector3 Extended(float distance)
    {
        if (distance >= 0) return flow.PointOnRoute(distance);
        return flow.PointOnRoute(0) + (flow.PointOnRoute(1) - flow.PointOnRoute(0)).normalized * distance;
    }

    public void WispAt(Vector3 position)
    {
        if (banks == null) return;
        var emit = new ParticleSystem.EmitParams { position = position + Vector3.up * .9f,
            startSize = 5, startLifetime = 5, startColor = new Color(.64f, .66f, .54f, .25f), velocity = Vector3.up * .12f };
        banks[0].Emit(emit, 4);
    }

    public void ResetSmoke(float front)
    {
        if (banks == null) return;
        Place(front);
        foreach (var ps in banks)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (playing && !paused) ps.Play();
        }
        shotClock = 1;
    }

    static AudioClip BuildBarrage()
    {
        const int rate = 22050, seconds = 12;
        var values = new float[rate * seconds]; var random = new System.Random(193010);
        float filtered = 0;
        var strikes = new[] { .6f, 4.4f, 8.1f };
        for (int i = 0; i < values.Length; i++)
        {
            float t = i / (float)rate;
            filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2 - 1, .055f);
            float envelope = .06f;
            foreach (float start in strikes)
            {
                float age = t - start;
                if (age >= 0) envelope += Mathf.Min(1, age / .045f) * Mathf.Exp(-age * 1.5f) * .7f;
            }
            float edge = Mathf.Min(1, Mathf.Min(t, seconds - t) / .12f);
            values[i] = Mathf.Clamp((filtered * .75f + Mathf.Sin(t * 2 * Mathf.PI * 47) * .12f) * envelope * edge, -.65f, .65f);
        }
        var clip = AudioClip.Create("Distant retreat barrage - procedural", values.Length, 1, rate, false);
        clip.SetData(values, 0); return clip;
    }

    void OnDestroy()
    {
        if (Barrage) Barrage.Stop(); if (Rifles) Rifles.Stop();
        if (barrageClip) Destroy(barrageClip);
        if (instanceMaterial) Destroy(instanceMaterial);
        if (softParticle) Destroy(softParticle);
    }
}
