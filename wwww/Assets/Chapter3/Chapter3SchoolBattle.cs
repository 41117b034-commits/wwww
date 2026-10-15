using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// School missions hand off to the forest rescue after the physical rendezvous.
public sealed partial class Chapter3SchoolBattle : MonoBehaviour
{
    public enum Stage { Inactive, AwaitingTado, Observing, Choice, Outbreak, Fighting, Down, Regroup, Complete }
    public enum Mission { None, Bawan, Pihu }
    public Stage CurrentStage {get;private set;}
    public Mission Selected {get;private set;}
    public bool Fighting=>CurrentStage==Stage.Fighting;
    public bool CombatActive=>Fighting||(Rescue&&Rescue.CombatActive);
    public Chapter3Rescue Rescue {get;private set;}
    public bool OwnsFlow=>CurrentStage!=Stage.Inactive&&CurrentStage!=Stage.AwaitingTado;
    public bool Complete=>CurrentStage==Stage.Complete;
    public int Step {get;private set;}
    public int CheckpointStep {get;private set;}
    public bool ThreatRevealing {get;private set;}
    public bool CrossingArrived {get;private set;}
    public bool CompletionBlockedByThreat=>Fighting&&Selected==Mission.Pihu&&CrossingArrived&&!CurrentThreatCleared;
    public int Health {get;private set;}=100;
    public int EscortPressure {get;private set;}
    public int Retries {get;private set;}
    public bool Advancing {get;private set;}
    public bool Returning {get;private set;}
    public bool AtCrossing {get;private set;}
    public float WorkProgress {get;private set;}
    public Chapter3MissionUI Menu {get;private set;}
    public readonly List<Chapter3Enemy> Enemies=new List<Chapter3Enemy>();
    public Chapter3CompanionReaction[] Companions {get;private set;}
    public Transform[] Barricades {get;private set;}
    public Transform Ammo {get;private set;}
    Chapter3SchoolController school;
    Chapter3Combat combat;
    Chapter3Player player;
    Chapter3Presentation ui;
    Chapter3SchoolAmbient ambient;
    Chapter3SchoolCompanionPose pihuPose;
    readonly Chapter3PathMotor threatMotor=new Chapter3PathMotor();
    Transform[] threatCovers;
    Vector3[] threatPositions;
    bool[] threatIntroduced=new bool[2];
    Vector3 checkpointBawan,checkpointPihu,checkpointTado,checkpointPlayer;
    int checkpointOtherIndex,checkpointOtherEscortIndex;
    int observedEnemyShots;
    bool dangerWasVisible;
    float dangerPoseUntil,nextCoverCall;
    bool CurrentThreatCleared=>Step<Enemies.Count&&Enemies[Step]&&Enemies[Step].Defeated;
    Transform scenery, enemiesRoot;
    Chapter3PathMotor guideMotor=new Chapter3PathMotor(),tadoMotor=new Chapter3PathMotor(),otherMotor=new Chapter3PathMotor();
    int guideIndex,otherIndex,otherEscortIndex;
    Vector3[] activeRoute,otherRoute;
    Vector3 safePihu;
    float noticeUntil,hurtAt=-10;
    AudioSource distantShots;
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    static readonly Vector3[][] west={new[]{P(-7,-8),P(-23,-4),P(-27,8),P(-27,13)},new[]{P(-27,24),P(-27,35),P(-27,43)}};
    static readonly Vector3[][] east={new[]{P(27.5f,8),P(27.5f,14),P(30,23)},new[]{P(27.5f,24),P(27.5f,30),P(29,37),P(32,43),P(32,53)}};
    static readonly Vector3[] westExit={P(-27,54),P(-25,66),P(-12,74),P(-1.6f,78)};
    static readonly Vector3[] eastExit={P(27.5f,54),P(27.5f,60),P(25,68),P(12,74),P(1.6f,78)};
    static readonly Vector3[] eastSquad={P(8,-8),P(25,-4),P(27.5f,7),P(27.5f,14),P(27.5f,24),P(27.5f,30),P(29,37),P(32,43),P(32,53),P(27.5f,54),P(27.5f,60),P(25,68),P(12,74),P(1.6f,78)};
    public void Begin(Chapter3SchoolController owner)
    {
        if(Rescue)Rescue.Suspend();
        school=owner;combat=school.flow.combat;player=school.player;ui=school.ui;
        ambient=school.GetComponentInChildren<Chapter3SchoolAmbient>(true);
        StopAllCoroutines();StopBreakthrough();ClearSchoolPoses();ClearEnemies();Selected=Mission.None;CurrentStage=Stage.AwaitingTado;Step=Retries=CheckpointStep=0;
        ThreatRevealing=CrossingArrived=false;
        Health=100;EscortPressure=0;WorkProgress=0;AtCrossing=Advancing=Returning=false;noticeUntil=0;
        if(!Menu)
        {
            Menu=Chapter3MissionUI.Build(ui);
            Menu.bawan.onClick.AddListener(()=>Choose(Mission.Bawan));Menu.pihu.onClick.AddListener(()=>Choose(Mission.Pihu));
            Menu.retry.onClick.AddListener(()=>Retry());Menu.replay.onClick.AddListener(()=>school.flow.EnterSchool());
        }
        Menu.Hide();
        if(!scenery)BuildProps();ResetBreakthrough(0);scenery.gameObject.SetActive(false);
        if(ambient)ambient.ResetCalm();
        Companions=new Chapter3CompanionReaction[3];
        var actors=new[]{school.bawan,school.pihu,school.tado};
        for(int i=0;i<actors.Length;i++)
        {
            var reaction=actors[i].GetComponent<Chapter3CompanionReaction>();if(!reaction)reaction=actors[i].gameObject.AddComponent<Chapter3CompanionReaction>();
            reaction.combat=combat;reaction.actor=actors[i];reaction.ResetReaction();Companions[i]=reaction;
        }
        pihuPose=school.pihu.GetComponent<Chapter3SchoolCompanionPose>();if(!pihuPose)pihuPose=school.pihu.gameObject.AddComponent<Chapter3SchoolCompanionPose>();
        if(!distantShots){distantShots=gameObject.AddComponent<AudioSource>();distantShots.playOnAwake=false;distantShots.spatialBlend=0;distantShots.volume=.35f;}
    }
    public bool MeetTado()
    {
        if(CurrentStage!=Stage.AwaitingTado||Flat(player.transform.position,school.tado.transform.position)>3)return false;
        CurrentStage=Stage.Observing;StartCoroutine(Briefing());return true;
    }
    IEnumerator Briefing()
    {
        player.canMove=false;player.canLook=true;ui.objective.text="與達多商量分工";ui.hint.text="空白鍵略過當句台詞";
        // Proximity starts the conversation immediately, without a viewing task.
        yield return Say(school.tado,"巴萬走左側，比胡走右側。你選一邊跟上，我去另一邊支援。",6.5f,true);
        yield return Say(school.bawan,"左邊要拆橫木、開側門；右邊要掩護比胡穿越。最後在校後集合。",5,true);
        CurrentStage=Stage.Choice;ui.Say("",null);ui.hint.text="點選任務，或按數字 1／2";ui.objective.text="決定分工";
        Menu.choice.SetActive(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Choose(Mission mission)
    {
        if(CurrentStage!=Stage.Choice||mission==Mission.None)return false;
        Selected=mission;CheckpointStep=0;Menu.Hide();CurrentStage=Stage.Outbreak;StartCoroutine(Outbreak());return true;
    }
    IEnumerator Outbreak()
    {
        ui.hint.text="";ui.objective.text="槍聲響起";player.canMove=false;player.FocusOn(new Vector3(-4,1.3f,25));
        if(ambient)ambient.Panic();
        for(int i=0;i<3;i++){if(combat.weapons.shotClip)distantShots.PlayOneShot(combat.weapons.shotClip,.48f-i*.10f);yield return new WaitForSeconds(.65f);}
        yield return Say(school.tado,Selected==Mission.Bawan?"你跟巴萬走左邊，我和比胡走右邊。校後會合！":"你跟比胡走右邊，我去左邊支援巴萬。校後會合！",4.5f,true);
        yield return Say(Selected==Mission.Bawan?school.bawan:school.pihu,Selected==Mission.Bawan?"跟我來，把擋路的橫木拆掉！":"你看住前面的警察。我聽你的信號前進，必要時叫我撤回！",4.5f,false);
        ResetMission();
    }
    void ResetMission()
    {
        ClearEnemies();ClearSchoolPoses();StopBreakthrough();guideMotor.Reset();tadoMotor.Reset();otherMotor.Reset();threatMotor.Reset();
        Step=CheckpointStep;guideIndex=0;otherIndex=otherEscortIndex=0;
        Health=100;EscortPressure=0;WorkProgress=0;AtCrossing=Advancing=Returning=false;hurtAt=-10;noticeUntil=0;
        ThreatRevealing=CrossingArrived=dangerWasVisible=false;dangerPoseUntil=nextCoverCall=0;observedEnemyShots=0;
        threatIntroduced=new bool[2];scenery.gameObject.SetActive(true);ResetBreakthrough(CheckpointStep);
        school.bawan.transform.position=Step==0?P(-3,-8):checkpointBawan;
        school.pihu.transform.position=Step==0?P(1,-8):checkpointPihu;
        school.tado.transform.position=Step==0?P(Selected==Mission.Bawan?2:-4,-9.5f):checkpointTado;
        foreach(var c in Companions)c.ResetReaction();
        activeRoute=Selected==Mission.Bawan?west[Step]:Step==0?new[]{P(8,-8),P(25,-4),P(30,7)}:east[Step];
        otherRoute=Selected==Mission.Bawan?eastSquad:school.breakthroughRoute;
        otherIndex=Step==0?(Selected==Mission.Bawan?0:1):checkpointOtherIndex;
        otherEscortIndex=Step==0?otherIndex:checkpointOtherEscortIndex;
        if(Selected==Mission.Pihu&&Step>0){AtCrossing=true;safePihu=checkpointPihu;}
        // The other squad takes its own side of the same school, without teleporting.
        combat.SchoolEncounter=this;combat.playerTarget.position=player.view.transform.position-Vector3.up*.38f;
        player.chapter=null;player.station=null;player.combat=null;player.school=school;player.speed=2.8f;
        player.Warp(Step==0?new Vector3(-2,.06f,-6):checkpointPlayer,
            Step==0?new Vector3(Selected==Mission.Bawan?-23:25,1.2f,-4):Selected==Mission.Bawan?new Vector3(-27,1.2f,43):new Vector3(29,1.2f,37));
        var positions=Selected==Mission.Bawan?new[]{P(-25.7f,22),P(-28,53)}:new[]{P(29,33),P(34.5f,59)};
        // A knife-only player must reach each guard without crossing a locked
        // barricade or the current cover checkpoint. Keep rifle positions intact.
        bool knifeOnly=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife;
        if(knifeOnly)
            positions=Selected==Mission.Bawan?new[]{P(-25.7f,14.4f),P(-28,44.4f)}:new[]{P(29,27.8f),P(34.5f,52)};
        threatPositions=positions;
        for(int i=0;i<positions.Length;i++)
        {
            if(i<CheckpointStep){Enemies.Add(null);continue;}
            var go=Instantiate(combat.enemyTemplates[i%combat.enemyTemplates.Length],enemiesRoot);go.name="校側警察 · "+(i+1);
            go.transform.position=positions[i];var enemy=go.GetComponent<Chapter3Enemy>();enemy.combat=combat;enemy.advances=false;enemy.attackInterval=4.5f;enemy.escortTarget=null;
            // Close-quarters staging leaves time to approach with a knife.
            // Damage, telegraph and fire cadence remain the same.
            enemy.engagementRange=knifeOnly?12:32;
            Enemies.Add(enemy);go.SetActive(Selected==Mission.Bawan&&i==Step);
        }
        for(int i=0;i<threatCovers.Length;i++)
        {threatCovers[i].gameObject.SetActive(Selected==Mission.Pihu);threatCovers[i].position=positions[i]+new Vector3(2,0,-.85f);}
        CurrentStage=Stage.Fighting;Menu.Hide();ui.Say("",null);ui.fade.color=Color.clear;
        combat.hud.SetActive(true);combat.downPanel.SetActive(false);combat.hurtOverlay.color=Color.clear;combat.weapons.ResetEquipment();
        player.canMove=player.canLook=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        MoveAmmo();
        if(Selected==Mission.Pihu&&Step>0)RevealCurrentThreat();
        if(Step>0)Notice(Selected==Mission.Bawan?school.bawan.DisplayName:school.pihu.DisplayName,"從這裡繼續，前一段已經通過了。",3);
    }
    void Update()
    {
        if(!school||CurrentStage==Stage.Inactive)return;
        if(CurrentStage==Stage.Choice)
        {
            if(Keyboard.current?.digit1Key.wasPressedThisFrame==true)Choose(Mission.Bawan);
            else if(Keyboard.current?.digit2Key.wasPressedThisFrame==true)Choose(Mission.Pihu);
        }
        if(CurrentStage==Stage.Down){if(Keyboard.current?.rKey.wasPressedThisFrame==true)Retry();return;}
        if(!Fighting)return;
        if(noticeUntil>0&&Time.time>=noticeUntil){noticeUntil=0;ui.Say("",null);}
        combat.playerTarget.position=player.view.transform.position-Vector3.up*.38f;
        if(Selected==Mission.Bawan)UpdateBreakthrough();else UpdateCover();
        if(!Fighting)return;
        MoveOtherSquad();
        bool supply=combat.weapons.Selected==Chapter3Weapons.Weapon.Rifle&&Flat(player.transform.position,Ammo.position)<2;
        if(supply&&player.ActionPressed)combat.weapons.Refill();
        if(supply)ui.hint.text="E 補充備彈   ·   R 單發裝填";
        player.speed=combat.weapons.Aiming?1.7f:2.8f;
        combat.healthText.text="生命值  "+Health+" / 100";
        var w=combat.weapons;combat.ammoText.text=w.Selected==Chapter3Weapons.Weapon.Rifle?"村田十八年式  "+(w.Loaded?1:0)+" / 1   備彈 "+w.Reserve:"獵刀";
        combat.crosshair.text=w.Aiming?"·":"+";
        if(w.Reloading)combat.combatText.text+="\n單發裝填  "+Mathf.CeilToInt(w.ReloadProgress*100)+"%";
        else if(!string.IsNullOrEmpty(w.Feedback))combat.combatText.text+="\n"+w.Feedback;
        combat.hurtOverlay.color=new Color(.6f,.025f,.01f,Mathf.Clamp01(1-(Time.time-hurtAt)/.7f)*.32f);
    }
    public void Damage(int amount,Vector3 source)
    {
        if(Rescue&&Rescue.Active){Rescue.Damage(amount,source);return;}
        if(!Fighting||amount<=0)return;Health=Mathf.Max(0,Health-amount);hurtAt=Time.time;WorkProgress=0;
        if(Health==0)Fail("你受傷倒下了",CheckpointStep>0?"從第二段安全位置重試。\n第一段已完成，任務與武器選擇保留。":"從目前任務起點重試。\n任務與武器選擇保留。");
    }
    void Fail(string title,string body)
    {
        CurrentStage=Stage.Down;Advancing=Returning=false;ThreatRevealing=false;StopAllCoroutines();StopBreakthrough();ClearSchoolPoses();combat.weapons.Suspend();combat.hud.SetActive(false);
        player.canMove=player.canLook=false;player.view.fieldOfView=60;combat.hurtOverlay.color=Color.clear;ui.Say("",null);ui.hint.text="R 重試目前任務";
        Menu.ShowResult(title,body,true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Retry()
    {
        if(Rescue&&Rescue.Active)return Rescue.Retry();
        if(CurrentStage!=Stage.Down)return false;
        StopAllCoroutines();Retries++;ResetMission();return true;
    }
    void StartRegroup()
    {
        // Never freeze an unresolved guard into an idle bystander at handoff.
        if(Step<2||Enemies.Exists(e=>e&&!e.Defeated))return;
        StopBreakthrough();ClearSchoolPoses();
        CurrentStage=Stage.Regroup;Advancing=Returning=false;combat.weapons.Suspend();combat.hud.SetActive(false);combat.hurtOverlay.color=Color.clear;
        foreach(var e in Enemies)if(e)e.escortTarget=null;
        player.view.fieldOfView=60;player.speed=2.8f;ui.Say("",null);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        activeRoute=Selected==Mission.Bawan?westExit:eastExit;guideIndex=0;guideMotor.Reset();
        StartCoroutine(Regroup());
    }
    IEnumerator Regroup()
    {
        ui.objective.text="到校後空地，與另一隊匯合";
        var guide=Selected==Mission.Bawan?school.bawan:school.pihu;
        while(guideIndex<activeRoute.Length||otherIndex<otherRoute.Length||otherEscortIndex<otherRoute.Length||Flat(player.transform.position,P(0,78))>4||Flat(school.tado.transform.position,P(0,78))>4)
        {
            MoveRoute(guide,guideMotor,activeRoute,ref guideIndex,true,Selected==Mission.Bawan?Vector3.left*1.15f:Vector3.zero);
            MoveOtherSquad();ui.hint.text=Flat(player.transform.position,guide.transform.position)>8?"同伴在前方等你，沿校舍側道跟上。":"跟隨同伴到校後空地 · WASD 移動 · 按住右鍵環視";
            yield return null;
        }
        player.canMove=false;
        yield return Say(school.bawan,Selected==Mission.Bawan?"我們把左邊打通了。比胡、達多，你們右邊還好嗎？":"我和達多把左邊打通了。比胡，你們右邊還好嗎？",4.2f,true);
        yield return Say(school.pihu,Selected==Mission.Bawan?"我和達多都到了，右邊的路也通了。":"到了！剛才他替我看住警察，我才能穿過右邊。",4.2f,true);
        yield return Say(school.tado,"先離開學校。沿後面那條林路走，我在前面接應。",4.2f,true);
        CurrentStage=Stage.Complete;ClearEnemies();Menu.Hide();
        if(!Rescue)Rescue=gameObject.AddComponent<Chapter3Rescue>();
        Rescue.Begin(school,this);
    }
    void MoveOtherSquad()
    {
        var other=Selected==Mission.Bawan?school.pihu:school.bawan;
        MoveRoute(other,otherMotor,otherRoute,ref otherIndex,false);
        // Tado always supports the unselected side with an independent motor;
        // the player's cover orders and checkpoint transitions cannot move him.
        if(otherEscortIndex>otherIndex)return;
        // The offset route can be shorter around a corner. Spend only the gap
        // beyond the formation spacing, so even a long frame cannot catch up
        // through the leader. The leader keeps moving if Tado needs to wait.
        float distance=Flat(school.tado.transform.position,other.transform.position);
        float maxSpeed=Mathf.Max(0,distance-1.3f)/Mathf.Max(Time.deltaTime,.0001f);
        MoveRoute(school.tado,tadoMotor,otherRoute,ref otherEscortIndex,false,new Vector3(-.85f,0,-1.4f),maxSpeed);
    }
    void MoveRoute(Chapter2Actor actor,Chapter3PathMotor motor,Vector3[] route,ref int index,bool waitForPlayer,Vector3 offset=default,float maxSpeed=float.PositiveInfinity)
    {
        if(index>=route.Length)return;
        var target=route[index]+offset;
        // Give a player standing on a waypoint room instead of walking a face
        // through the first-person camera. Keep the route's collision checks.
        var separation=Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up);
        if(separation.magnitude<1.05f)
        {if(separation.sqrMagnitude<.01f)separation=Vector3.left;target=player.transform.position+separation.normalized*1.05f;target.y=0;}
        var toPlayer=Vector3.ProjectOnPlane(player.transform.position-actor.transform.position,Vector3.up);
        var direction=Vector3.ProjectOnPlane(target-actor.transform.position,Vector3.up).normalized;
        float along=Vector3.Dot(toPlayer,direction);
        // A waypoint can be beyond the player even if the waypoint itself is
        // clear. Sidestep before crossing the player's body, then resume it.
        if(toPlayer.magnitude<2.2f&&along>0&&along<Flat(actor.transform.position,target)&&
            (toPlayer-direction*along).magnitude<1.05f)
        {
            var side=Vector3.Cross(Vector3.up,direction)*1.25f;
            var right=player.transform.position+side;right.y=0;var left=player.transform.position-side;left.y=0;
            if(Chapter3PathMotor.Clear(actor.transform.position,right)){motor.Move(actor,right,Mathf.Min(3.1f,maxSpeed));return;}
            if(Chapter3PathMotor.Clear(actor.transform.position,left)){motor.Move(actor,left,Mathf.Min(3.1f,maxSpeed));return;}
            return;
        }
        bool playerAhead=Vector3.Dot(toPlayer,target-actor.transform.position)>0;
        if(waitForPlayer&&toPlayer.magnitude>8&&!playerAhead){actor.Face(player.transform.position);return;}
        float speed=waitForPlayer?(playerAhead&&toPlayer.magnitude>5?3.1f:2.1f):2.9f;
        motor.Move(actor,target,Mathf.Min(speed,maxSpeed));
        if(Flat(actor.transform.position,target)<.23f)index++;
    }
    void CompleteMissionStep()
    {
        if(!Fighting||Step>=2||!CurrentThreatCleared)return;
        if(Selected==Mission.Pihu&&!CrossingArrived)return;
        Step++;CheckpointStep=Step;
        if(Step==2){StartRegroup();return;}
        checkpointBawan=school.bawan.transform.position;checkpointPihu=school.pihu.transform.position;checkpointTado=school.tado.transform.position;
        checkpointPlayer=Selected==Mission.Bawan?new Vector3(-27,.06f,19):new Vector3(27.5f,.06f,22.5f);
        checkpointOtherIndex=otherIndex;checkpointOtherEscortIndex=otherEscortIndex;
        guideIndex=0;guideMotor.Reset();EscortPressure=0;MoveAmmo();
        if(Selected==Mission.Bawan)
        {activeRoute=west[Step];Enemies[Step].gameObject.SetActive(true);}
        else
        {
            safePihu=checkpointPihu;activeRoute=east[Step];Advancing=Returning=CrossingArrived=false;
            pihuPose.ReportSafe();RevealCurrentThreat();
        }
    }
    void ClearSchoolPoses(){if(pihuPose)pihuPose.Clear();}
    int DefeatedCount(){int count=0;for(int i=0;i<Enemies.Count;i++)if(i<CheckpointStep||(Enemies[i]&&Enemies[i].Defeated))count++;return count;}
    public void Notice(string speaker,string line,float seconds)
    {if(Rescue&&Rescue.Active){Rescue.Notice(speaker,line,seconds);return;}if(!Fighting)return;noticeUntil=Time.time+seconds;ui.Say(speaker,line);}
    IEnumerator Say(Chapter2Actor actor,string line,float seconds,bool focus)
    {
        if(focus)player.FocusOn(actor.transform.position+Vector3.up*1.25f);
        actor.Face(player.transform.position);actor.speaking=true;ui.Say(actor.DisplayName,line);
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,seconds,null);
        yield return Chapter3Flow.WaitForLine(seconds);actor.speaking=false;ui.Say("",null);
    }
    void MoveAmmo()
    {Ammo.position=Selected==Mission.Bawan?P(-29,Step==0?9:38):P(Step==0?33:32,Step==0?4:21);}
    void ClearEnemies()
    {foreach(var e in Enemies)if(e){e.gameObject.SetActive(false);Destroy(e.gameObject);}Enemies.Clear();}
    public void Suspend()
    {
        if(Rescue)Rescue.Suspend();
        StopAllCoroutines();StopBreakthrough();ClearSchoolPoses();ThreatRevealing=CrossingArrived=false;CurrentStage=Stage.Inactive;ClearEnemies();
        if(combat&&combat.SchoolEncounter==this)combat.SchoolEncounter=null;
        if(Menu)Menu.Hide();if(scenery)scenery.gameObject.SetActive(false);
        if(distantShots)distantShots.Stop();if(ambient)ambient.ResetCalm();
    }
    void OnDestroy(){if(Menu)Destroy(Menu.gameObject);}
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        if(Rescue&&Rescue.Active)return Rescue.Constrain(current,desired);
        // Keep the approach before the mission briefing and choice;
        // the rear forest belongs to batch 5.
        if(CurrentStage==Stage.AwaitingTado)desired.z=Mathf.Min(desired.z,-1);
        if(Fighting&&Step<2)desired.z=Mathf.Min(desired.z,Selected==Mission.Bawan?(Step==0?23:55):(Step==0?29:60));
        if(CurrentStage!=Stage.Inactive)desired.z=Mathf.Min(desired.z,84);
        return desired;
    }
    void BuildProps()
    {
        scenery=new GameObject("學校交戰 · 路障與掩體").transform;scenery.SetParent(school.transform,false);
        enemiesRoot=new GameObject("學校交戰 · 巡警").transform;enemiesRoot.SetParent(school.transform,false);
        Material wood=null,metal=null;
        foreach(var renderer in school.GetComponentsInChildren<Renderer>(true))foreach(var mat in renderer.sharedMaterials)
        {if(mat&&mat.name=="Weathered school cedar")wood=mat;if(mat&&mat.name=="Weathered iron")metal=mat;}
        // Materials are already included by the serialized school; no runtime
        // Shader.Find dependency or copied high-poly mesh assets.
        if(!metal)metal=wood;
        BuildBreakthroughProps(wood,metal);
        threatCovers=new Transform[2];
        for(int i=0;i<2;i++)threatCovers[i]=Crate(scenery,Vector3.zero,new Vector3(1.4f,1.8f,.65f),wood,metal,"警察出場掩體"+(i+1));
        foreach(float z in new[]{10,26,56})Crate(scenery,new Vector3(30.5f,0,z),new Vector3(3.5f,1.7f,1.1f),wood,metal,"東側厚木掩體");
        Crate(scenery,new Vector3(-29,0,5),new Vector3(1.7f,1.5f,1.2f),wood,metal,"西側入口掩體");
        Crate(scenery,new Vector3(-24.6f,0,34),new Vector3(1.7f,1.5f,1.2f),wood,metal,"西側中段掩體");
        Ammo=Crate(scenery,P(-29,9),new Vector3(.8f,.65f,.65f),wood,metal,"備用彈藥箱");
        var label=new GameObject("彈藥箱標示",typeof(TextMesh)).GetComponent<TextMesh>();label.transform.SetParent(Ammo,false);label.transform.localPosition=new Vector3(0,.94f,0);
        label.text="彈藥";label.font=ui.subtitle.font;label.fontSize=64;label.characterSize=.085f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(1,.84f,.25f);
        label.GetComponent<Renderer>().sharedMaterial=label.font.material;
    }
    static Transform Crate(Transform parent,Vector3 position,Vector3 size,Material wood,Material iron,string name)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=position;
        Box(root,Vector3.up*size.y/2,size,wood,name);
        foreach(float x in new[]{-.32f,.32f})Box(root,new Vector3(size.x*x,size.y/2,-size.z/2-.012f),new Vector3(.06f,size.y,.025f),iron,"木箱箍帶");return root;
    }
    static Transform Box(Transform parent,Vector3 p,Vector3 size,Material mat,string name)
    {
        var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent,false);box.transform.localPosition=p;box.transform.localScale=size;
        if(mat)box.GetComponent<Renderer>().sharedMaterial=mat;return box.transform;
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}

