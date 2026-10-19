using UnityEngine;

// A third playable responsibility inside the same school: enter the event,
// advance with Tado, then call a separated ally back and withdraw together.
public sealed partial class Chapter3SchoolBattle
{
    public bool StragglerCalled { get; private set; }
    public bool StragglerArrived => StragglerCalled && ambient && ambient.Straggler
        && Flat(ambient.Straggler.position, P(0, 75)) < 3;
    public Vector3 StragglerPosition => ambient && ambient.Straggler ? ambient.Straggler.position : P(12, 32);
    public Vector3 AssaultObjective => Step == 2 && !StragglerCalled ? StragglerPosition
        : activeRoute != null && activeRoute.Length > 0 ? activeRoute[activeRoute.Length - 1] : P(0, 76);
    public Vector3[] AssaultWaypoints => Step == 2 && !StragglerCalled ? new[] { StragglerPosition } : activeRoute;
    public Chapter3RouteGuide AssaultGuide { get; private set; }

    static readonly Vector3[][] assaultApproach = {
        new[] { P(0,-3), P(0,3), P(-4,8), P(-5,16) },
        new[] { P(-4,23), P(-3,29), P(4,32), P(9,33) }
    };
    static readonly Vector3[] assaultWithdrawal = {
        P(13,33), P(18,44.8f), P(24.2f,45.4f), P(24.2f,61), P(22,66), P(12,72), P(0,76)
    };
    static readonly Vector3[] assaultBackgroundRoute = {
        P(0,-3), P(0,3), P(-4,8), P(-5,16), P(-4,23), P(-3,29), P(4,32),
        P(13,33), P(18,44.8f), P(24.2f,45.4f), P(24.2f,61), P(22,66), P(12,72), P(0,76)
    };
    bool OtherSquadsArrived => otherRoute != null && otherIndex >= otherRoute.Length
        && otherEscortIndex >= (Selected == Mission.Tado ? eastSquad.Length : assaultBackgroundRoute.Length);

    Transform assaultProps;
    int stragglerIndex;
    float nextAssaultCall;

    void ResetAssaultMission()
    {
        StopAssault(); ClearEnemies(); ClearSchoolPoses(); StopBreakthrough();
        guideMotor.Reset(); tadoMotor.Reset(); otherMotor.Reset(); threatMotor.Reset();
        Step = CheckpointStep; guideIndex = 0;
        Health = 100; EscortPressure = 0; WorkProgress = 0; hurtAt = -10; noticeUntil = 0;
        AtCrossing = Advancing = Returning = ThreatRevealing = CrossingArrived = false;
        StragglerCalled = false; stragglerIndex = 0; nextAssaultCall = 0;
        scenery.gameObject.SetActive(true); ResetBreakthrough(0);
        foreach (var cover in threatCovers) cover.gameObject.SetActive(false);
        BuildAssaultProps(); assaultProps.gameObject.SetActive(true);

        school.bawan.transform.position = Step == 0 ? P(-3,-8) : checkpointBawan;
        school.pihu.transform.position = Step == 0 ? P(3,-8) : checkpointPihu;
        school.tado.transform.position = Step == 0 ? P(0,-5) : checkpointTado;
        foreach (var companion in Companions) companion.ResetReaction();
        activeRoute = Step < 2 ? assaultApproach[Step] : assaultWithdrawal;
        otherRoute = school.breakthroughRoute;
        otherIndex = Step == 0 ? 1 : checkpointOtherIndex;
        otherEscortIndex = Step == 0 ? 0 : checkpointOtherEscortIndex;

        combat.SchoolEncounter = this;
        player.chapter = null; player.station = null; player.combat = null; player.school = school; player.speed = 2.8f;
        player.Warp(Step == 0 ? new Vector3(-2,.06f,-6) : checkpointPlayer,
            Step == 0 ? new Vector3(0,1.2f,12) : Step == 1 ? new Vector3(4,1.2f,32) : new Vector3(12,1.2f,32));
        combat.playerTarget.position = player.view.transform.position - Vector3.up * .38f;

        bool knife = school.flow.CarriedWeapon == Chapter3Weapons.Weapon.Knife;
        var positions = new[] { P(-1,knife ? 18.5f : 21), P(5,knife ? 33 : 36) };
        for (int i = 0; i < positions.Length; i++)
        {
            if (i < CheckpointStep) { Enemies.Add(null); continue; }
            var go = Instantiate(combat.enemyTemplates[i % combat.enemyTemplates.Length], enemiesRoot);
            go.name = "會場通道警察 · " + (i + 1); go.transform.position = positions[i];
            var enemy = go.GetComponent<Chapter3Enemy>();
            enemy.combat = combat; enemy.advances = false; enemy.AttackEnabled = true;
            enemy.escortTarget = null; enemy.attackInterval = 5.2f; enemy.engagementRange = knife ? 12 : 28;
            Enemies.Add(enemy); go.SetActive(i == Step);
        }
        CurrentStage = Stage.Fighting; Menu.Hide(); ui.Say("",null); ui.fade.color = Color.clear;
        combat.hud.SetActive(true); combat.downPanel.SetActive(false); combat.hurtOverlay.color = Color.clear;
        combat.weapons.ResetEquipment(); player.canMove = player.canLook = true;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        PrepareAssaultStage();
        if (Step > 0) Notice(school.tado.DisplayName,"從這裡繼續，前一段已經通過了。",3);
    }

    void BuildAssaultProps()
    {
        if (assaultProps) return;
        assaultProps = new GameObject("攻入學校 · 會場掩蔽物").transform;
        assaultProps.SetParent(school.transform,false);
        Material wood = null, metal = null;
        foreach (var renderer in scenery.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material && material.name == "Weathered school cedar") wood = material;
                if (material && material.name == "Weathered iron") metal = material;
            }
        if (!metal) metal = wood;
        Crate(assaultProps,P(-7,10),new Vector3(2.6f,1.45f,1.1f),wood,metal,"會場入口厚木箱");
        Crate(assaultProps,P(1,24),new Vector3(2.6f,1.45f,1.1f),wood,metal,"會場內厚木箱");
    }

    void PrepareAssaultStage()
    {
        Ammo.position = Step == 0 ? P(-8,7) : P(8,29);
        if (!AssaultGuide) AssaultGuide = gameObject.AddComponent<Chapter3RouteGuide>();
        if (Step == 2 && ambient)
        {
            ambient.ResetStraggler(P(12,32)); ambient.SetStragglerWaiting(true);
            AssaultGuide.Show(player.transform.position,new[] { P(12,32) },player.transform,ambient.Straggler);
        }
        else
        {
            if (ambient) ambient.HideStraggler();
            AssaultGuide.Show(player.transform.position,activeRoute,player.transform,school.tado.transform);
        }
    }

    void UpdateAssault()
    {
        if (Step >= 3) return;
        if (Step < 2)
        {
            MoveRoute(school.tado,guideMotor,activeRoute,ref guideIndex,true);
            var guard = Step < Enemies.Count ? Enemies[Step] : null;
            bool clear = guard && guard.Defeated;
            bool gathered = guideIndex >= activeRoute.Length && Flat(player.transform.position,AssaultObjective) < 4;
            ui.objective.text = Step == 0 ? "跟達多攻入學校 · 1 / 3 進入會場" : "跟達多攻入學校 · 2 / 3 穿過會場";
            ui.hint.text = clear ? "跟上達多，到前方集合位置 · WASD 移動" : "利用木箱掩蔽，處理阻路警察 · " + combat.weapons.Controls;
            combat.combatText.text = clear ? "通路已清 · 等你與達多到位" : "看住通道警察 · 不要離開隊伍";
            if (gathered && clear) AdvanceAssaultStage();
            return;
        }

        ui.objective.text = "跟達多攻入學校 · 3 / 3 收攏同伴，撤到校後";
        if (!StragglerCalled)
        {
            school.tado.Face(StragglerPosition);
            if (Time.time >= nextAssaultCall)
            {
                nextAssaultCall = Time.time + 12;
                Notice(school.tado.DisplayName,"還有一個人落在那邊！靠近叫他跟上，我們一起從校舍旁撤出去。",5);
            }
            ui.hint.text = Flat(player.transform.position,StragglerPosition) <= 3 ? "E 叫落後族人跟上" : "沿黃色箭頭靠近落後族人，再按 E";
            combat.combatText.text = "先收攏同伴，再一起撤出";
            if (player.ActionPressed && Flat(player.transform.position,Ammo.position) >= 2) CallStraggler();
            return;
        }

        // Tado waits for both the player and the separated ally. Do not strand
        // the ally at a corner or let the stage complete just by running ahead.
        bool allyClose = Flat(school.tado.transform.position,StragglerPosition) < 6;
        if (allyClose) MoveRoute(school.tado,guideMotor,activeRoute,ref guideIndex,true);
        else school.tado.Face(StragglerPosition);
        MoveAssaultStraggler();
        ui.hint.text = "跟隨達多撤出 · 同伴落後時會等候 · WASD 移動";
        combat.combatText.text = StragglerArrived ? "同伴已到校後安全位置" : "落後族人已跟上 · 帶他一起到校後";
        if (guideIndex >= activeRoute.Length && StragglerArrived && Flat(player.transform.position,P(0,76)) < 4)
        {
            Step = CheckpointStep = 3; AssaultGuide.Hide();
            ambient.SetStragglerWaiting(true); StartRegroup();
        }
    }

    public bool CallStraggler()
    {
        if (!Fighting || Selected != Mission.Tado || Step != 2 || StragglerCalled
            || !ambient || !ambient.Straggler || Flat(player.transform.position,StragglerPosition) > 3) return false;
        StragglerCalled = true; stragglerIndex = guideIndex = 0; guideMotor.Reset();
        ambient.SetStragglerWaiting(false);
        AssaultGuide.Show(player.transform.position,activeRoute,player.transform,school.tado.transform);
        Notice("族人","我在這裡！我跟你們一起走。",3.5f);
        return true;
    }

    void MoveAssaultStraggler()
    {
        if (!ambient || !ambient.Straggler || stragglerIndex >= activeRoute.Length) return;
        if (stragglerIndex > guideIndex) return;
        Vector3 current = ambient.Straggler.position;
        float separation = Flat(current,school.tado.transform.position);
        if (separation <= 1.4f) { ambient.SetStragglerWaiting(true); return; }
        Vector3 target = activeRoute[stragglerIndex];
        // Yield to the player's body, while keeping the authored route intact.
        if (Flat(current,player.transform.position) < 1.2f
            && Vector3.Dot(target-current,player.transform.position-current) > 0)
        { ambient.SetStragglerWaiting(true); return; }
        ambient.SetStragglerWaiting(false);
        ambient.MoveStraggler(target,Mathf.Min(3,Mathf.Max(0,separation-1.4f)/Mathf.Max(Time.deltaTime,.0001f)));
        if (Flat(ambient.Straggler.position,target) < .25f) stragglerIndex++;
    }

    void AdvanceAssaultStage()
    {
        Step++; CheckpointStep = Step;
        checkpointBawan = school.bawan.transform.position; checkpointPihu = school.pihu.transform.position;
        checkpointTado = school.tado.transform.position;
        checkpointPlayer = Step == 1 ? new Vector3(-5,.06f,14) : new Vector3(8,.06f,32);
        checkpointOtherIndex = otherIndex; checkpointOtherEscortIndex = otherEscortIndex;
        guideIndex = 0; guideMotor.Reset();
        activeRoute = Step < 2 ? assaultApproach[Step] : assaultWithdrawal;
        if (Step < Enemies.Count && Enemies[Step]) Enemies[Step].gameObject.SetActive(true);
        PrepareAssaultStage();
        Notice(school.tado.DisplayName,Step == 1 ? "通了！跟我穿過會場，別落在後面。" : "準備撤出！先把落後的族人叫過來。",4);
    }

    Vector3 ConstrainAssault(Vector3 current,Vector3 desired)
    {
        // Keep checkpoints sequential without invisible bounds through a target.
        if (Step == 0) desired.z = Mathf.Min(desired.z,24);
        else if (Step == 1 || !StragglerCalled) desired.z = Mathf.Min(desired.z,42);
        return desired;
    }

    void StopAssault()
    {
        if (AssaultGuide) AssaultGuide.Hide();
        if (assaultProps) assaultProps.gameObject.SetActive(false);
        if (ambient) ambient.HideStraggler();
        StragglerCalled = false; stragglerIndex = 0;
    }
}
