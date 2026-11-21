using System;
using UnityEngine;
using UnityEngine.XR;

// Chapter-owned equipment. The prefab reuses the project's existing rifle and hands.
public sealed class Chapter4Rifle : MonoBehaviour
{
    public Transform rifle, rightHand, leftHand, bolt, muzzle, cartridge, spentCase;
    public GameObject flash;
    public AudioSource audioSource;
    public AudioClip shotClip;
    public bool Loaded { get; private set; } = true;
    public bool Reloading => reloadClock > 0;
    public int Reserve { get; private set; } = 10;
    public int Shots { get; private set; }
    public int Reloads { get; private set; }
    public string Feedback { get; private set; } = "跟著隊伍撤離・必要時回身射擊";
    public bool Aiming { get; private set; }
    public bool Ready => owner && !owner.Frozen && owner.Chapter.player.canMove &&
        !owner.Chapter.evacuation.CaveExit.Carrying && busy <= 0 && !Reloading;
    public Ray AimRay { get; private set; }
    Chapter4RearGuard owner;
    float busy, reloadClock, recoil, aim, feedbackClock = 5;
    Vector3 boltHome;
    bool pausedAudio;
    const float ReloadSeconds = 2.2f;

    public void Initialize(Chapter4RearGuard combat)
    {
        owner = combat; boltHome = bolt ? bolt.localPosition : Vector3.zero;
        transform.SetParent(owner.Chapter.player.view.transform, false);
        transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
        gameObject.SetActive(true); if (flash) flash.SetActive(false);
    }
    public void Tick(float dt)
    {
        if (!owner) return;
        bool frozen = owner.Frozen;
        if (pausedAudio != frozen) { pausedAudio = frozen; if (frozen) audioSource.Pause(); else audioSource.UnPause(); }
        if (frozen || owner.Chapter.evacuation.CaveExit.Carrying) { Aiming = false; return; }
        var p = owner.Chapter.player;
        busy = Mathf.Max(0, busy - dt); recoil = Mathf.Max(0, recoil - dt);
        feedbackClock -= dt; if (feedbackClock <= 0) Feedback = "";
        if (Reloading)
        {
            reloadClock = Mathf.Max(0, reloadClock - dt);
            if (!Reloading) { Reserve--; Loaded = true; Reloads++; Notice("已裝入一發"); }
        }
        Aiming = p.AimHeld && !Reloading;
        UpdateAim();
        if (p.ReloadPressed) Reload();
        if (p.FirePressed) Fire();
    }
    void UpdateAim()
    {
        var p = owner.Chapter.player;
        var camera = p.view.transform;
        AimRay = new Ray(camera.position, camera.forward);
        if (!p.IsVR) { transform.SetParent(camera, false); transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity; return; }
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
            Vector3 trackingOffset = camera.localPosition - headPosition;
            transform.SetParent(p.transform, false);
            transform.localPosition = position + trackingOffset;
            transform.localRotation = rotation;
            AimRay = new Ray(transform.position, transform.forward);
        }
    }
    public bool Fire()
    {
        if (!Ready) return false;
        if (!Loaded) { Notice(Reserve > 0 ? "空膛・按 R 裝填" : "彈藥用盡・到備彈木箱按 E 補充"); return false; }
        UpdateAim(); Loaded = false; Shots++; recoil = .24f; busy = .42f;
        audioSource.PlayOneShot(shotClip, .65f);
        Notice(owner.ResolveShot(AimRay), 2.2f);
        return true;
    }
    public bool Reload()
    {
        if (!Ready || Loaded || Reserve <= 0) return false;
        reloadClock = ReloadSeconds; Aiming = false; Notice("正在裝填……", ReloadSeconds); return true;
    }
    public bool Refill()
    {
        if (!owner || owner.Frozen || Reserve >= 10) return false;
        Reserve = 10; Notice("已補充備彈・空膛時按 R 裝填", 3); return true;
    }
    public void Notice(string text, float seconds = 1.6f) { Feedback = text; feedbackClock = seconds; }
    void LateUpdate()
    {
        if (!owner || !rifle) return;
        var p = owner.Chapter.player;
        bool visible = !owner.Chapter.ending.HasStarted && !owner.Chapter.gas.IsCollapsed;
        Transform stone = owner.Chapter.evacuation.CaveExit?.HeldStone;
        rifle.gameObject.SetActive(visible && !stone);
        rightHand.gameObject.SetActive(visible);
        leftHand.gameObject.SetActive(visible);
        if (!visible) return;
        if (stone)
        {
            var facing = Quaternion.Euler(0, p.view.transform.eulerAngles.y, 0);
            var side = facing * Vector3.right * .18f;
            var underside = stone.position - Vector3.up * .075f - facing * Vector3.forward * .045f;
            rightHand.SetPositionAndRotation(underside + side, facing * Quaternion.Euler(-64, -12, -25));
            leftHand.SetPositionAndRotation(underside - side, facing * Quaternion.Euler(-64, 12, 65));
            return;
        }
        float dt = owner.Frozen ? 0 : Time.deltaTime;
        aim = Mathf.MoveTowards(aim, Aiming ? 1 : 0, dt * 6);
        if (!p.IsVR) p.view.fieldOfView = Mathf.Lerp(60, 46, aim);
        float progress = Reloading ? 1 - reloadClock / ReloadSeconds : 0;
        float tilt = Mathf.Sin(progress * Mathf.PI), kick = recoil / .24f;
        rifle.localPosition = p.IsVR ? new Vector3(0, -.065f, .32f) :
            Vector3.Lerp(new Vector3(.27f, -.32f, .85f), new Vector3(0, -.115f, .95f), aim);
        rifle.localPosition += new Vector3(0, -tilt * .08f, -kick * .065f);
        rifle.localRotation = Quaternion.Euler(-kick * 3, -tilt * 13, tilt * 18);
        if (flash) flash.SetActive(recoil > .185f && !owner.Frozen);
        if (bolt)
        {
            float open = Reloading ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.08f, .26f, progress)) *
                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, .94f, progress))) : 0;
            bolt.localPosition = boltHome + Vector3.back * (open * .10f);
        }
        if (cartridge)
        {
            cartridge.gameObject.SetActive(Reloading && progress > .36f && progress < .74f);
            cartridge.localPosition = Vector3.Lerp(new Vector3(.09f, .14f, -.30f), new Vector3(0, .065f, -.24f), Mathf.InverseLerp(.36f, .70f, progress));
        }
        if (spentCase)
        {
            float t = Mathf.InverseLerp(.22f, .38f, progress);
            spentCase.gameObject.SetActive(Reloading && progress > .22f && progress < .38f);
            spentCase.localPosition = new Vector3(.04f + t * .4f, .08f + Mathf.Sin(t * Mathf.PI) * .12f - t * .15f, -.28f);
        }
        Vector3 grip = rifle.TransformPoint(new Vector3(.015f, -.055f, -.30f));
        rightHand.SetPositionAndRotation(Vector3.Lerp(grip, rifle.TransformPoint(new Vector3(.085f, .06f, -.26f)), tilt), rifle.rotation * Quaternion.Euler(-78, 0, 0));
        leftHand.SetPositionAndRotation(rifle.TransformPoint(new Vector3(0, -.045f, .14f)), rifle.rotation * Quaternion.Euler(-90, 0, 90));
    }
    [Serializable] public sealed class Snapshot { public bool loaded; public int reserve, shots, reloads; public float reload, busy; }
    public Snapshot Capture() => new Snapshot { loaded = Loaded, reserve = Reserve, shots = Shots, reloads = Reloads, reload = reloadClock, busy = busy };
    public void Restore(Snapshot s)
    {
        if (s == null) return;
        Loaded = s.loaded; Reserve = s.reserve; Shots = s.shots; Reloads = s.reloads;
        reloadClock = s.reload; busy = s.busy; recoil = aim = 0; Aiming = false;
        audioSource.Stop(); Notice("回到接應處・沿路撤離", 4);
    }
}
