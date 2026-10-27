using UnityEngine;

// A third playable responsibility inside the same school: enter the event,
// advance with Tado, then signal every participant to withdraw together.
public sealed partial class Chapter3SchoolBattle
{
    public bool RetreatCalled { get; private set; }
    public bool RetreatArrived => RetreatCalled && ambient && ambient.WithdrawalArrived;
    public int RetreatArrivedCount => ambient ? ambient.WithdrawalArrivedCount : 0;
    public int RetreatSignalCount {get;private set;}
    public AudioSource RetreatSignalSource => retreatSignal;
    // Keep the earlier public mission queries compatible with saved tooling.
    public bool StragglerCalled => RetreatCalled;
    public bool StragglerArrived => RetreatArrived;
    public Vector3 StragglerPosition => P(12,32);
    public Vector3 AssaultObjective => Step == AssaultCombatStages && !StragglerCalled ? StragglerPosition
        : activeRoute != null && activeRoute.Length > 0 ? activeRoute[activeRoute.Length - 1] : P(0, 76);
    public Vector3[] AssaultWaypoints => Step == AssaultCombatStages && !StragglerCalled ? new[] { StragglerPosition } : activeRoute;
    public Chapter3RouteGuide AssaultGuide { get; private set; }

    static readonly Vector3[][] assaultApproach = {
        new[] { P(0,-3), P(0,3), P(-4,8), P(-5,16) },
        new[] { P(-4,23), P(-3,29), P(4,32), P(9,33) },
        new[] { P(7,34), P(2,36), P(-6,36), P(-9,34) },
        new[] { P(-6,37), P(2,38), P(8,37), P(10,37), P(12,36) }
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
    float nextAssaultCall;
    AudioSource retreatSignal;
    AudioClip retreatWhistle;

    void ResetAssaultMission(bool preserveArrival=false)
    {
        StopAssault(); ClearSchoolPoses(); StopBreakthrough();
        guideMotor.Reset(); tadoMotor.Reset(); otherMotor.Reset(); threatMotor.Reset();
        Step = CheckpointStep; guideIndex = 0;
        Health = 100; EscortPressure = 0; WorkProgress = 0; hurtAt = -10; noticeUntil = 0;
        AtCrossing = Advancing = Returning = ThreatRevealing = CrossingArrived = false;
        RetreatCalled = false; nextAssaultCall = 0;
        scenery.gameObject.SetActive(true); ResetBreakthrough(CheckpointStep);
        BuildAssaultProps(); assaultProps.gameObject.SetActive(true);

        if(!preserveArrival)
        {
            school.bawan.transform.position = Step == 0 ? P(-3,-8) : checkpointBawan;
            school.pihu.transform.position = Step == 0 ? P(3,-8) : checkpointPihu;
            school.tado.transform.position = Step == 0 ? P(0,-5) : checkpointTado;
        }
        foreach (var companion in Companions) companion.ResetReaction();
        activeRoute = Step < AssaultCombatStages ? assaultApproach[Step] : assaultWithdrawal;
        otherRoute = school.breakthroughRoute;
        otherIndex = Step == 0 ? 1 : checkpointOtherIndex;
        otherEscortIndex = Step == 0 ? 0 : checkpointOtherEscortIndex;

        combat.SchoolEncounter = this;
        player.chapter = null; player.station = null; player.combat = null; player.school = school; player.speed = 2.8f;
        if(!preserveArrival)player.Warp(Step == 0 ? new Vector3(-2,.06f,-6) : checkpointPlayer,
            Step == 0 ? new Vector3(0,1.2f,12) : Step == 1 ? new Vector3(4,1.2f,32) : new Vector3(12,1.2f,32));
        combat.playerTarget.position = player.view.transform.position - Vector3.up * .38f;

        if(!preserveArrival){ResetOfficersForCheckpoint();RestoreCampusOfficers();}
        foreach(var officer in Enemies)if(officer)officer.attackInterval=5.2f;
        CurrentStage = Stage.Fighting; Menu.Hide(); ui.Say("",null); ui.fade.color = Color.clear;
        if(ammoLabel)ammoLabel.gameObject.SetActive(true);
        combat.hud.SetActive(true); combat.downPanel.SetActive(false); combat.hurtOverlay.color = Color.clear;
        combat.weapons.ResetEquipment(); player.canMove = player.canLook = true;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        PrepareAssaultStage();
        if (Step > 0) Notice(school.tado.DisplayName,"從這裡繼續，前一段已經通過了。",3);
    }

    void BuildAssaultProps()
    {
        if (assaultProps) return;
        assaultProps = new GameObject("運動會 · 會場用品").transform;
        assaultProps.SetParent(scenery,false);
        Material wood = null, metal = null;
        foreach (var renderer in scenery.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material && material.name == "Weathered school cedar") wood = material;
                if (material && material.name == "Weathered iron") metal = material;
            }
        if (!metal) metal = wood;
        EventSupplies(assaultProps,P(-7,10),wood,metal,"會場入口器材與桌子");
        EventSupplies(assaultProps,P(1,24),wood,metal,"運動場邊活動用品");
    }

    void PrepareAssaultStage()
    {
        Ammo.position = Step == 0 ? P(-8,7) : Step == 1 ? P(8,29) : Step == 2 ? P(-7,32) : P(10,36);
        if (!AssaultGuide) AssaultGuide = gameObject.AddComponent<Chapter3RouteGuide>();
        if (Step == AssaultCombatStages && ambient)
        {
            ambient.HideStraggler();AssaultGuide.Hide();
        }
        else
        {
            if (ambient) ambient.HideStraggler();
            AssaultGuide.Show(player.transform.position,activeRoute,player.transform,school.tado.transform);
        }
    }

    void UpdateAssault()
    {
        if (Step > AssaultCombatStages) return;
        if (Step < AssaultCombatStages)
        {
            MoveRoute(school.tado,guideMotor,activeRoute,ref guideIndex,true);
            bool clear = AssaultStageCleared();
            bool gathered = guideIndex >= activeRoute.Length && Flat(player.transform.position,AssaultObjective) < 4;
            ui.objective.text = Step == 0 ? "跟緊達多，進入混亂的會場" : Step == 1 ? "照應同伴，穿過運動場" : Step == 2 ? "支援西側族人，擊倒兩名警察" : "深入校舍前方，支援東側族人";
            ui.hint.text = clear ? "跟上達多，到前方集合位置 · WASD 移動" : "留意警察的動作，借器材箱掩蔽 · " + combat.weapons.Controls;
            combat.combatText.text = clear ? "通路已清 · 等你與達多到位" : Step < 2 ? "看住通道警察 · 不要離開隊伍" : "校內支援 · 警察已擊倒  "+CampusDefeatedCount+" / 4";
            if (gathered && clear) AdvanceAssaultStage();
            return;
        }

        ui.objective.text = RetreatCalled ? "帶全體族人撤出學校，到校後集合" : "發出撤退信號，帶全體族人撤到校後";
        if (!StragglerCalled)
        {
            school.tado.Face(player.transform.position);
            if (Time.time >= nextAssaultCall)
            {
                nextAssaultCall = Time.time + 12;
                Notice(school.tado.DisplayName,"吹哨叫大家撤退！把會場裡的人都帶走，我們從校舍旁退出去。",5);
            }
            ui.hint.text = "E 吹口哨，通知所有族人撤退";
            combat.combatText.text = "發出信號後，全體族人會停手並跟隊撤出";
            if (player.ActionPressed) CallRetreat();
            return;
        }

        bool catchingUp=ambient.WithdrawalNeedsCatchup(school.tado.transform.position);
        if (!catchingUp) MoveRoute(school.tado,guideMotor,activeRoute,ref guideIndex,true);
        else school.tado.Face(player.transform.position);
        ui.hint.text = catchingUp ? "族人正在跟上，稍等大家再一起走 · WASD 移動" : "沿黃色箭頭與達多撤出 · 全體族人跟隨 · WASD 移動";
        combat.combatText.text = RetreatArrived ? "校內族人已全數撤到校後" : "族人到達校後  "+RetreatArrivedCount+" / "+Chapter3SchoolAmbient.AmbusherCount;
        if (guideIndex >= activeRoute.Length && StragglerArrived && Flat(player.transform.position,P(0,76)) < 4)
        {
            Step = CheckpointStep = AssaultCombatStages+1; AssaultGuide.Hide();
            StartRegroup();
        }
    }

    public bool CallStraggler()=>CallRetreat();
    public bool CallRetreat()
    {
        if (!Fighting || Selected != Mission.Tado || Step != AssaultCombatStages || StragglerCalled
            || Enemies.Exists(e=>e&&!e.Defeated) || !ambient || (combat.weapons.Selected==Chapter3Weapons.Weapon.Rifle&&Flat(player.transform.position,Ammo.position)<2)) return false;
        if(!ambient.BeginWithdrawal(player.transform))return false;
        RetreatCalled = true; guideIndex = 0; guideMotor.Reset();
        PlayRetreatWhistle();
        AssaultGuide.Show(player.transform.position,activeRoute,player.transform,school.tado.transform);
        Notice("你","（口哨聲）大家撤退！跟上我們，到校後集合！",4.5f);
        return true;
    }

    void PlayRetreatWhistle()
    {
        if(!retreatSignal){retreatSignal=gameObject.AddComponent<AudioSource>();retreatSignal.playOnAwake=false;retreatSignal.spatialBlend=0;retreatSignal.volume=.65f;}
        if(!retreatWhistle)
        {
            const int rate=22050;var samples=new float[(int)(rate*1.6f)];
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,start=t<.3f?0:t<.65f?.38f:.76f;
                float duration=start<.7f?.24f:.68f,u=t-start;
                if(u<0||u>=duration)continue;
                float envelope=Mathf.Min(1,u/.025f)*Mathf.Min(1,(duration-u)/.06f);
                float phase=2*Mathf.PI*(1850*u+22*u*u+.18f*Mathf.Sin(2*Mathf.PI*12*u));
                samples[i]=.35f*envelope*(Mathf.Sin(phase)+.12f*Mathf.Sin(phase*2));
            }
            retreatWhistle=AudioClip.Create("全員撤退 · 兩短一長口哨",samples.Length,1,rate,false);retreatWhistle.SetData(samples,0);
        }
        retreatSignal.PlayOneShot(retreatWhistle);RetreatSignalCount++;
    }

    void AdvanceAssaultStage()
    {
        Step++; CheckpointStep = Step;
        SaveOfficerCheckpoint();
        checkpointBawan = school.bawan.transform.position; checkpointPihu = school.pihu.transform.position;
        checkpointTado = school.tado.transform.position;
        checkpointPlayer = player.transform.position;
        checkpointOtherIndex = otherIndex; checkpointOtherEscortIndex = otherEscortIndex;
        guideIndex = 0; guideMotor.Reset();
        activeRoute = Step < AssaultCombatStages ? assaultApproach[Step] : assaultWithdrawal;
        PrepareAssaultStage();
        Notice(school.tado.DisplayName,Step == 1 ? "通了！跟我穿過會場，別落在後面。" : Step == 2 ? "西邊的族人還在跟警察交戰，跟我過去幫他們！" : Step == 3 ? "這邊打通了！校舍前還有人被壓住，過去支援！" : "各處的警察都倒下了！吹哨叫所有人跟我們一起撤出去。",4);
    }

    Vector3 ConstrainAssault(Vector3 current,Vector3 desired)
    {
        // Keep checkpoints sequential without invisible bounds through a target.
        if (Step == 0) desired.z = Mathf.Min(desired.z,24);
        else if (Step < AssaultCombatStages || !StragglerCalled) desired.z = Mathf.Min(desired.z,43);
        return desired;
    }

    void StopAssault()
    {
        if (AssaultGuide) AssaultGuide.Hide();
        if (ambient) ambient.HideStraggler();
        if(ambient)ambient.CancelWithdrawal(CurrentStage==Stage.Down);
        if(retreatSignal)retreatSignal.Stop();
        RetreatCalled = false;RetreatSignalCount=0;
    }
}
