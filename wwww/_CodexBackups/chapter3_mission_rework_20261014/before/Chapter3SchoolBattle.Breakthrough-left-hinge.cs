using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// The west route alternates jobs: the player removes the first barricade under
// Bawan's covering fire, then protects Bawan while he opens the rear side gate.
public sealed partial class Chapter3SchoolBattle
{
    public float GateWorkProgress { get; private set; }
    public bool GateWorking { get; private set; }
    public bool GateCommanded { get; private set; }
    public bool GatePaused { get; private set; }
    public bool BawanCovering => Fighting && Selected == Mission.Bawan && Step == 0 && Time.time < bawanCoverUntil;
    public int BawanCoverBursts { get; private set; }
    public Transform SideGateLeaf => sideGateLeaf;
    public Vector3 GateWorkPosition => sideGateLeaf ? sideGateLeaf.TransformPoint(new Vector3(.95f, 0, -.85f)) : P(-29.2f, 45.15f);
    Transform sideGateLeaf, gateLeftContact, gateRightContact, bawanCoverWeapon;
    Chapter3BawanCooperationPose bawanCooperationPose;
    readonly Chapter3PathMotor gateWorkMotor = new Chapter3PathMotor();
    AudioSource bawanCoverAudio;
    float bawanCoverUntil = -1, nextBawanCover, bawanCoverBegan, gateDangerUntil, nextGateWarning;
    bool bawanCoverFired;
    int gateObservedShots;
    const float GateWorkSeconds = 5f;

    Chapter3Enemy BreakthroughGuard(int index) => index < Enemies.Count ? Enemies[index] : null;

    void UpdateBreakthrough()
    {
        if (Step >= 2) return;
        EnsureBawanCooperationPose();
        if (Step == 0)
        {
            MoveRoute(school.bawan, guideMotor, activeRoute, ref guideIndex, true, Vector3.left * 1.15f);
            var guard = BreakthroughGuard(0);
            UpdateBawanCover(guard);
            bool near = Flat(player.transform.position, Barricades[0].position) < 3.4f;
            bool guarded = !guard || !guard.Defeated;
            WorkBarricade(Keyboard.current?.eKey.isPressed == true, Time.deltaTime);
            if (!Fighting || Step != 0) return;
            ui.objective.text = "跟巴萬突破 · 你拆第一道橫木";
            ui.hint.text = near ? guarded ? "巴萬會掩護你 · 先清除守衛，再拆橫木" : "按住 E 拆橫木 · 放開會中止"
                : "跟巴萬到第一道橫木 · " + combat.weapons.Controls;
            combat.combatText.text = guarded
                ? "巴萬負責掩護 · 你處理守衛" + (BawanCovering ? "\n巴萬正在壓制警察，趁現在接近！" : "")
                : "第一道橫木  " + Mathf.FloorToInt(WorkProgress / 1.8f * 100) + "%";
            return;
        }
        UpdateSideGate();
    }

    public bool WorkBarricade(bool held, float delta)
    {
        if (!Fighting || Selected != Mission.Bawan || Step != 0) return false;
        var guard = BreakthroughGuard(0);
        if (!held || delta <= 0 || Flat(player.transform.position, Barricades[0].position) > 3.4f
            || !guard || !guard.Defeated || combat.weapons.Reloading)
        { WorkProgress = 0; return false; }
        WorkProgress += delta;
        if (WorkProgress < 1.8f) return false;
        foreach (var collider in Barricades[0].GetComponentsInChildren<Collider>()) collider.enabled = false;
        StartCoroutine(LowerFirstBarricade(Barricades[0]));
        WorkProgress = 0;
        StopBawanCover();
        CompleteMissionStep();
        Notice(school.bawan.DisplayName, "橫木拆開了。下一道側門交給我，你守住通道！", 4);
        return true;
    }

    IEnumerator LowerFirstBarricade(Transform barrier)
    {
        for (float t = 0; t < .7f; t += Time.deltaTime)
        { barrier.localRotation = Quaternion.Euler(Mathf.SmoothStep(0, 86, t / .7f), 0, 0); yield return null; }
        barrier.gameObject.SetActive(false);
    }

    void UpdateBawanCover(Chapter3Enemy guard)
    {
        bool canCover = guard && !guard.Defeated && guard.AttackEnabled && guideIndex >= activeRoute.Length
            && Flat(player.transform.position, Barricades[0].position) < 7f;
        if (canCover && Time.time >= nextBawanCover && HasBawanCoverLine(guard))
        {
            bawanCoverBegan = Time.time; bawanCoverUntil = Time.time + 3.2f;
            nextBawanCover = Time.time + 8f; bawanCoverFired = false;
            Notice(school.bawan.DisplayName, "我看著警察！趁他低頭，靠近橫木！", 3.2f);
            school.bawan.GetComponent<Chapter2SpeechFace>()?.BeginLine("我看著警察！趁他低頭，靠近橫木！", 3.2f, null);
        }
        bool covering = canCover && Time.time < bawanCoverUntil;
        if (covering)
        {
            school.bawan.Face(guard.transform.position); school.bawan.speaking = true;
            bawanCooperationPose.SetCover(guard.transform, 1);
            if (!bawanCoverFired && Time.time >= bawanCoverBegan + .4f && HasBawanCoverLine(guard))
            {
                bawanCoverFired = true; BawanCoverBursts++;
                guard.HoldFire(2.8f);
                bawanCooperationPose.FireCoverShot();
                if (combat.weapons.shotClip) bawanCoverAudio.PlayOneShot(combat.weapons.shotClip, .45f);
            }
        }
        else
        {
            school.bawan.speaking = false;
            bawanCooperationPose.SetCover(null, 0);
        }
    }

    bool HasBawanCoverLine(Chapter3Enemy guard)
    {
        Vector3 origin = school.bawan.transform.position + Vector3.up * 1.38f;
        Vector3 target = guard.transform.position + Vector3.up * 1.38f;
        Vector3 delta = target - origin;
        foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(school.bawan.transform)
                || hit.collider.transform.IsChildOf(guard.transform)) continue;
            return false;
        }
        return true;
    }

    void UpdateSideGate()
    {
        StopBawanCover();
        if (guideIndex < activeRoute.Length)
            MoveRoute(school.bawan, guideMotor, activeRoute, ref guideIndex, true, Vector3.left * 1.15f);
        bool arrived = guideIndex >= activeRoute.Length;
        Vector3 workPosition = GateWorkPosition; workPosition.y = 0;
        bool playerBlocking = Flat(player.transform.position, workPosition) < 1.05f;
        if (arrived && !playerBlocking) gateWorkMotor.Move(school.bawan, workPosition, 1.9f);
        bool atHandle = arrived && !playerBlocking && Flat(school.bawan.transform.position, workPosition) < .28f;
        if (atHandle) school.bawan.Face(sideGateLeaf.TransformPoint(new Vector3(1.1f, 0, .4f)));
        if (player.ActionPressed && Flat(player.transform.position, Ammo.position) >= 2) CommandSideGate();

        var guard = BreakthroughGuard(1);
        if (guard && guard.Shots > gateObservedShots)
        { gateObservedShots = guard.Shots; gateDangerUntil = Time.time + 1.2f; }
        bool threat = guard && !guard.Defeated && (guard.Telegraphing || Time.time < gateDangerUntil);
        bool protectedByPlayer = Flat(player.transform.position, Barricades[1].position) < 13;
        GatePaused = GateCommanded && GateWorkProgress < 1 && (threat || !atHandle || !protectedByPlayer);
        GateWorking = GateCommanded && GateWorkProgress < 1 && !GatePaused;
        if (GateWorking)
        {
            GateWorkProgress = Mathf.Clamp01(GateWorkProgress + Time.deltaTime / GateWorkSeconds);
            ApplySideGateRotation();
        }
        if (GateCommanded && threat && Time.time >= nextGateWarning)
        {
            nextGateWarning = Time.time + 4;
            Notice(school.bawan.DisplayName, "警察抬槍了！我先停一下，替我守住通道！", 3.2f);
        }
        bawanCooperationPose.SetPush(gateLeftContact, gateRightContact, GateWorking ? 1f : GateCommanded && atHandle ? .35f : 0f);
        ui.objective.text = "跟巴萬突破 · 巴萬開側門，你守通道";
        ui.hint.text = !arrived ? "跟巴萬到校舍後側 · " + combat.weapons.Controls
            : playerBlocking ? "讓出門邊工作位置，站到通道旁掩護巴萬"
            : !GateCommanded ? "靠近巴萬，短按 E 請他開側門 · " + combat.weapons.Controls
            : "守住通道、處理警察 · 巴萬會繼續推門 · " + combat.weapons.Controls;
        string status = !GateCommanded ? "巴萬到位後，按 E 讓他開門" : GatePaused ? "巴萬暫停 · 已保留開門進度" : GateWorkProgress >= 1 ? "側門已打開" : "巴萬正在推開側門";
        combat.combatText.text = status + "\n側門  " + Mathf.RoundToInt(GateWorkProgress * 100) + "%"
            + (guard && !guard.Defeated ? " · 守衛仍在通道前方" : " · 守衛已清除");
        if (GateWorkProgress >= 1 && guard && guard.Defeated)
        {
            GateWorking = GatePaused = false;
            bawanCooperationPose.ResetPose();
            CompleteMissionStep();
        }
    }

    public bool CommandSideGate()
    {
        if (!Fighting || Selected != Mission.Bawan || Step != 1 || GateCommanded
            || guideIndex < activeRoute.Length || Flat(school.bawan.transform.position, GateWorkPosition) > .35f
            || Flat(player.transform.position, school.bawan.transform.position) > 4f) return false;
        GateCommanded = true;
        Notice(school.bawan.DisplayName, "我來推門。看住前面的警察，別讓他開槍！", 4);
        return true;
    }

    void ApplySideGateRotation()
    { if (sideGateLeaf) sideGateLeaf.localRotation = Quaternion.Euler(0, -100f * Mathf.SmoothStep(0, 1, GateWorkProgress), 0); }

    void ResetBreakthrough(int checkpoint)
    {
        StopBreakthrough();
        GateWorkProgress = checkpoint >= 2 ? 1 : 0;
        BawanCoverBursts = 0; nextBawanCover = 0; nextGateWarning = gateDangerUntil = 0; gateObservedShots = 0;
        if (Barricades == null) return;
        for (int i = 0; i < Barricades.Length; i++)
        {
            Barricades[i].gameObject.SetActive(Selected == Mission.Bawan && (i > 0 || checkpoint == 0));
            Barricades[i].localRotation = Quaternion.identity;
            foreach (var collider in Barricades[i].GetComponentsInChildren<Collider>(true)) collider.enabled = true;
        }
        ApplySideGateRotation();
    }

    void StopBawanCover()
    {
        bawanCoverUntil = -1;
        if (bawanCooperationPose) bawanCooperationPose.SetCover(null, 0);
        if (school && school.bawan) school.bawan.speaking = false;
    }

    void StopBreakthrough()
    {
        StopBawanCover(); GateWorking = GatePaused = GateCommanded = false; gateWorkMotor.Reset();
        if (bawanCooperationPose) bawanCooperationPose.ResetPose();
        if (bawanCoverAudio) bawanCoverAudio.Stop();
    }

    void EnsureBawanCooperationPose()
    {
        if (bawanCooperationPose) return;
        bawanCooperationPose = school.bawan.GetComponent<Chapter3BawanCooperationPose>();
        if (!bawanCooperationPose) bawanCooperationPose = school.bawan.gameObject.AddComponent<Chapter3BawanCooperationPose>();
        bawanCooperationPose.Configure(school.bawan, bawanCoverWeapon);
        var sound = new GameObject("巴萬合作掩護槍聲"); sound.transform.SetParent(school.bawan.transform, false);
        bawanCoverAudio = sound.AddComponent<AudioSource>();
        bawanCoverAudio.playOnAwake = false; bawanCoverAudio.spatialBlend = .8f; bawanCoverAudio.maxDistance = 35;
    }

    void BuildBreakthroughProps(Material wood, Material metal)
    {
        Barricades = new Transform[2];
        var first = new GameObject("西側第一道可拆橫木").transform; first.SetParent(scenery, false); first.position = P(-27, 16); Barricades[0] = first;
        foreach (float x in new[] { -3.2f, 3.2f }) Box(first, new Vector3(x, .8f, 0), new Vector3(.20f, 1.6f, .22f), wood, "路障木柱");
        foreach (float y in new[] { .50f, 1.12f }) Box(first, new Vector3(0, y, 0), new Vector3(6.7f, .24f, .22f), wood, "可拆橫木");
        Box(first, new Vector3(0, .8f, -.14f), new Vector3(.18f, .92f, .11f), metal, "橫木插銷");

        var gate = new GameObject("西側校後側門 · 巴萬推開").transform; gate.SetParent(scenery, false); gate.position = P(-27, 46); Barricades[1] = gate;
        foreach (float x in new[] { -3.2f, 3.2f }) Box(gate, new Vector3(x, .90f, 0), new Vector3(.22f, 1.80f, .28f), wood, "側門門柱");
        sideGateLeaf = new GameObject("側門門扇與左鉸鏈").transform; sideGateLeaf.SetParent(gate, false); sideGateLeaf.localPosition = new Vector3(-3.1f, 0, 0);
        // The lower boards block walking; the upper opening preserves the sight
        // line to the guard so the player must actually protect the gate worker.
        Box(sideGateLeaf, new Vector3(3.1f, .48f, 0), new Vector3(6.2f, .94f, .18f), wood, "側門下半木板");
        foreach (float x in new[] { .06f, 6.14f }) Box(sideGateLeaf, new Vector3(x, 1.05f, 0), new Vector3(.12f, 1.0f, .18f), wood, "側門豎框");
        Box(sideGateLeaf, new Vector3(3.1f, 1.48f, 0), new Vector3(6.2f, .13f, .18f), wood, "側門上框");
        foreach (float y in new[] { .25f, 1.25f }) Box(sideGateLeaf, new Vector3(0, y, 0), new Vector3(.11f, .18f, .31f), metal, "側門鐵鉸鏈");
        Box(sideGateLeaf, new Vector3(1.15f, 1.04f, -.13f), new Vector3(.66f, .09f, .12f), metal, "側門推把");
        gateLeftContact = new GameObject("巴萬左手推門接觸點").transform; gateLeftContact.SetParent(sideGateLeaf, false); gateLeftContact.localPosition = new Vector3(.95f, 1.06f, -.18f);
        gateRightContact = new GameObject("巴萬右手推門接觸點").transform; gateRightContact.SetParent(sideGateLeaf, false); gateRightContact.localPosition = new Vector3(1.30f, 1.06f, -.18f);
        var source = school.flow.station ? school.flow.station.riflePickup : combat.weapons.rifle;
        if (source)
        {
            bawanCoverWeapon = Instantiate(source.gameObject, scenery).transform; bawanCoverWeapon.name = "巴萬掩護用村田十八年式";
            bawanCoverWeapon.localScale = Vector3.one;
            foreach (var collider in bawanCoverWeapon.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; Destroy(collider); }
            bawanCoverWeapon.gameObject.SetActive(false);
        }
    }
}
