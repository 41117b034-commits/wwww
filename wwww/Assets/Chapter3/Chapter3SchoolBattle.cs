using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Batch 4 is deterministic authored gameplay. No LLM, chapter completion, or
// Pihu life/death result is written here; batch 5 starts after this rendezvous.
public sealed class Chapter3SchoolBattle : MonoBehaviour
{
    public enum Stage { Inactive, AwaitingTado, Observing, Choice, Outbreak, Fighting, Down, Regroup, Complete }
    public enum Mission { None, Bawan, Pihu }
    public Stage CurrentStage {get;private set;}
    public Mission Selected {get;private set;}
    public bool Fighting=>CurrentStage==Stage.Fighting;
    public bool OwnsFlow=>CurrentStage!=Stage.Inactive&&CurrentStage!=Stage.AwaitingTado;
    public bool Complete=>CurrentStage==Stage.Complete;
    public int Step {get;private set;}
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
    Transform scenery, enemiesRoot;
    Chapter3PathMotor guideMotor=new Chapter3PathMotor(),escortMotor=new Chapter3PathMotor(),otherMotor=new Chapter3PathMotor();
    int guideIndex,escortIndex,otherIndex,otherEscortIndex;
    Vector3[] activeRoute,otherRoute;
    Vector3 safePihu,safeTado;
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
        school=owner;combat=school.flow.combat;player=school.player;ui=school.ui;
        ambient=school.GetComponentInChildren<Chapter3SchoolAmbient>(true);
        StopAllCoroutines();ClearEnemies();Selected=Mission.None;CurrentStage=Stage.AwaitingTado;Step=Retries=0;
        Health=100;EscortPressure=0;WorkProgress=0;AtCrossing=Advancing=Returning=false;noticeUntil=0;
        if(!Menu)
        {
            Menu=Chapter3MissionUI.Build(ui);
            Menu.bawan.onClick.AddListener(()=>Choose(Mission.Bawan));Menu.pihu.onClick.AddListener(()=>Choose(Mission.Pihu));
            Menu.retry.onClick.AddListener(()=>Retry());Menu.replay.onClick.AddListener(()=>school.flow.EnterSchool());
        }
        Menu.Hide();
        if(!scenery)BuildProps();scenery.gameObject.SetActive(false);
        if(ambient)ambient.ResetCalm();
        Companions=new Chapter3CompanionReaction[3];
        var actors=new[]{school.bawan,school.pihu,school.tado};
        for(int i=0;i<actors.Length;i++)
        {
            var reaction=actors[i].GetComponent<Chapter3CompanionReaction>();if(!reaction)reaction=actors[i].gameObject.AddComponent<Chapter3CompanionReaction>();
            reaction.combat=combat;reaction.actor=actors[i];reaction.ResetReaction();Companions[i]=reaction;
        }
        if(!distantShots){distantShots=gameObject.AddComponent<AudioSource>();distantShots.playOnAwake=false;distantShots.spatialBlend=0;distantShots.volume=.35f;}
    }
    public bool MeetTado()
    {
        if(CurrentStage!=Stage.AwaitingTado||Flat(player.transform.position,school.tado.transform.position)>3)return false;
        CurrentStage=Stage.Observing;StartCoroutine(Observe());return true;
    }
    IEnumerator Observe()
    {
        player.canMove=false;player.canLook=true;ui.objective.text="觀察運動會";ui.hint.text="按住右鍵環視";
        player.FocusOn(new Vector3(-4,1.25f,22));ui.Say("",null);
        var aside=new Chapter3PathMotor();
        for(float t=0;t<1.8f;t+=Time.deltaTime){aside.Move(school.tado,P(-7.5f,-5),2.2f);yield return null;}
        school.tado.Face(new Vector3(-4,1.25f,22));
        // An actual calm viewing beat before the event; cannot immediately skip
        // from arriving at the school into the mission menu.
        yield return new WaitForSeconds(5);
        yield return Say(school.tado,"運動會還在進行。西側通道有警察，東邊能借石牆遮掩。我們分頭走，到校後集合。",6.5f,false);
        yield return Say(school.bawan,"我去打通西側。比胡，你和達多走東邊。你要跟哪一隊？",5,false);
        CurrentStage=Stage.Choice;ui.Say("",null);ui.hint.text="點選任務，或按數字 1／2";ui.objective.text="決定分工";
        Menu.choice.SetActive(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Choose(Mission mission)
    {
        if(CurrentStage!=Stage.Choice||mission==Mission.None)return false;
        Selected=mission;Menu.Hide();CurrentStage=Stage.Outbreak;StartCoroutine(Outbreak());return true;
    }
    IEnumerator Outbreak()
    {
        ui.hint.text="";ui.objective.text="槍聲響起";player.canMove=false;player.FocusOn(new Vector3(-4,1.3f,25));
        if(ambient)ambient.Panic();
        for(int i=0;i<3;i++){if(combat.weapons.shotClip)distantShots.PlayOneShot(combat.weapons.shotClip,.48f-i*.10f);yield return new WaitForSeconds(.65f);}
        yield return Say(school.tado,"槍聲！別留在操場，沿兩側走！",3.5f,false);
        yield return Say(Selected==Mission.Bawan?school.bawan:school.pihu,Selected==Mission.Bawan?"跟我來，把擋路的橫木拆掉！":"你看住前面的警察。我和達多聽你的信號，別讓我們暴露在火線上！",4.5f,false);
        ResetMission();
    }
    void ResetMission()
    {
        ClearEnemies();guideMotor.Reset();escortMotor.Reset();otherMotor.Reset();Step=guideIndex=escortIndex=otherIndex=0;
        Health=100;EscortPressure=0;WorkProgress=0;AtCrossing=Advancing=Returning=false;hurtAt=-10;noticeUntil=0;
        scenery.gameObject.SetActive(true);foreach(var gate in Barricades){gate.gameObject.SetActive(Selected==Mission.Bawan);gate.localRotation=Quaternion.identity;foreach(var c in gate.GetComponentsInChildren<Collider>())c.enabled=true;}
        school.bawan.transform.position=P(-3,-8);school.pihu.transform.position=P(1,-8);school.tado.transform.position=P(2,-9.5f);
        foreach(var c in Companions)c.ResetReaction();
        activeRoute=Selected==Mission.Bawan?west[0]:new[]{P(8,-8),P(25,-4),P(30,7)};
        otherRoute=Selected==Mission.Bawan?eastSquad:school.breakthroughRoute;otherIndex=otherEscortIndex=Selected==Mission.Bawan?0:1;
        // The other squad takes its own side of the same school, without teleporting.
        combat.SchoolEncounter=this;combat.playerTarget.position=player.view.transform.position-Vector3.up*.38f;
        player.chapter=null;player.station=null;player.combat=null;player.school=school;player.speed=2.8f;
        player.Warp(new Vector3(-2,.06f,-6),new Vector3(Selected==Mission.Bawan?-23:25,1.2f,-4));
        var positions=Selected==Mission.Bawan?new[]{P(-25.7f,22),P(-28,53)}:new[]{P(29,33),P(30,61)};
        // A knife-only player must reach each guard without crossing a locked
        // barricade or the current cover checkpoint. Keep rifle positions intact.
        bool knifeOnly=school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife;
        if(knifeOnly)
            positions=Selected==Mission.Bawan?new[]{P(-25.7f,14.4f),P(-28,44.4f)}:new[]{P(29,27.8f),P(30,58.5f)};
        for(int i=0;i<positions.Length;i++)
        {
            var go=Instantiate(combat.enemyTemplates[i%combat.enemyTemplates.Length],enemiesRoot);go.name="校側警察 · "+(i+1);
            go.transform.position=positions[i];var enemy=go.GetComponent<Chapter3Enemy>();enemy.combat=combat;enemy.advances=false;enemy.attackInterval=4.5f;enemy.escortTarget=null;
            // Close-quarters staging leaves time to approach with a knife.
            // Damage, telegraph and fire cadence remain the same.
            enemy.engagementRange=knifeOnly?12:32;
            Enemies.Add(enemy);go.SetActive(true);
        }
        CurrentStage=Stage.Fighting;Menu.Hide();ui.Say("",null);ui.fade.color=Color.clear;
        combat.hud.SetActive(true);combat.downPanel.SetActive(false);combat.hurtOverlay.color=Color.clear;combat.weapons.ResetEquipment();
        player.canMove=player.canLook=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        MoveAmmo();
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
        combat.healthText.text="體力  "+Health+" / 100";
        var w=combat.weapons;combat.ammoText.text=w.Selected==Chapter3Weapons.Weapon.Rifle?"村田十八年式  "+(w.Loaded?1:0)+" / 1   備彈 "+w.Reserve:"獵刀";
        combat.crosshair.text=w.Aiming?"·":"+";
        if(w.Reloading)combat.combatText.text+="\n單發裝填  "+Mathf.CeilToInt(w.ReloadProgress*100)+"%";
        else if(!string.IsNullOrEmpty(w.Feedback))combat.combatText.text+="\n"+w.Feedback;
        combat.hurtOverlay.color=new Color(.6f,.025f,.01f,Mathf.Clamp01(1-(Time.time-hurtAt)/.7f)*.32f);
    }
    void UpdateBreakthrough()
    {
        MoveRoute(school.bawan,guideMotor,activeRoute,ref guideIndex,true,Vector3.left*1.15f);
        ui.objective.text="跟巴萬突破 · 路障 "+(Step+1)+" / 2";
        bool near=Flat(player.transform.position,Barricades[Step].position)<3.4f;
        bool guard=Enemies[Step]&&!Enemies[Step].Defeated;
        bool held=Keyboard.current?.eKey.isPressed==true;
        WorkBarricade(held,Time.deltaTime);
        if(!Fighting)return;
        ui.hint.text=near?(guard?"先清除路障後的守衛":"按住 E 拆開橫木 · 放開會中止"):"跟上巴萬 · "+combat.weapons.Controls;
        combat.combatText.text=near&&!guard?"拆除路障  "+Mathf.FloorToInt(WorkProgress/1.8f*100)+"%":"打通西側通道\n守衛 "+DefeatedCount()+" / 2  ·  路障 "+Step+" / 2";
    }
    public bool WorkBarricade(bool held,float delta)
    {
        if(!Fighting||Selected!=Mission.Bawan||Step>=2)return false;
        if(!held||delta<=0||Flat(player.transform.position,Barricades[Step].position)>3.4f||!Enemies[Step].Defeated||combat.weapons.Reloading)
        {WorkProgress=0;return false;}
        WorkProgress+=delta;
        if(WorkProgress<1.8f)return false;
        var gate=Barricades[Step];foreach(var c in gate.GetComponentsInChildren<Collider>())c.enabled=false;
        StartCoroutine(LowerGate(gate));WorkProgress=0;Step++;
        if(Step==2){StartRegroup();return true;}
        activeRoute=west[Step];guideIndex=0;guideMotor.Reset();MoveAmmo();Notice(school.bawan.DisplayName,"路開了，下一道在校舍後面。",3);return true;
    }
    IEnumerator LowerGate(Transform gate)
    {
        for(float t=0;t<.7f;t+=Time.deltaTime){gate.localRotation=Quaternion.Euler(Mathf.SmoothStep(0,86,t/.7f),0,0);yield return null;}
        gate.gameObject.SetActive(false);
    }
    void UpdateCover()
    {
        if(!AtCrossing)
        {
            MoveRoute(school.pihu,guideMotor,activeRoute,ref guideIndex,true);
            MoveRoute(school.tado,escortMotor,activeRoute,ref escortIndex,true,new Vector3(-.85f,0,-.9f));
            if(guideIndex>=activeRoute.Length&&escortIndex>=activeRoute.Length)
            {
                AtCrossing=true;safePihu=school.pihu.transform.position;safeTado=school.tado.transform.position;
                guideIndex=escortIndex=0;activeRoute=east[Step];Notice(school.pihu.DisplayName,"我們在掩體後。看清前方，安全了就叫我們走！",4);
            }
        }
        else
        {
            if(player.ActionPressed&&Flat(player.transform.position,Ammo.position)>=2)CommandEscort();
            if(Returning)
            {
                guideMotor.Move(school.pihu,safePihu,3.8f);escortMotor.Move(school.tado,safeTado,3.8f);
                if(Flat(school.pihu.transform.position,safePihu)<.2f&&Flat(school.tado.transform.position,safeTado)<.2f)Returning=false;
            }
            else if(Advancing)
            {
                MoveRoute(school.pihu,guideMotor,activeRoute,ref guideIndex,false);
                MoveRoute(school.tado,escortMotor,activeRoute,ref escortIndex,false,new Vector3(-.85f,0,-.9f));
                if(guideIndex>=activeRoute.Length&&escortIndex>=activeRoute.Length)
                {
                    Step++;Advancing=false;EscortPressure=0;
                    if(Step==2){StartRegroup();return;}
                    safePihu=school.pihu.transform.position;safeTado=school.tado.transform.position;
                    guideIndex=escortIndex=0;activeRoute=east[Step];MoveAmmo();Notice(school.pihu.DisplayName,"到這裡了。前面還有一段，你繼續看著！",3.5f);
                }
            }
        }
        foreach(var e in Enemies)e.escortTarget=AtCrossing&&(Advancing||Returning)?school.tado.transform:null;
        ui.objective.text="跟比胡掩護 · 穿越 "+(Step+1)+" / 2";
        string order=!AtCrossing?"先跟比胡到東側掩體":Returning?"隊伍撤回掩體中":Advancing?"隊伍正在穿越 · E 撤回掩體":"隊伍待命 · E 下令前進";
        bool danger=Enemies.Exists(e=>e&&!e.Defeated&&e.Telegraphing);
        combat.combatText.text=order+"\n隊伍受壓 "+EscortPressure+" / 3"+(danger?"  ·  警察正在瞄準！":"");
        ui.hint.text="E 指揮前進／撤回 · "+combat.weapons.Controls;
    }
    public bool CommandEscort()
    {
        if(!Fighting||Selected!=Mission.Pihu||!AtCrossing||Returning)return false;
        if(Flat(player.transform.position,school.pihu.transform.position)>18)
        {Notice(school.pihu.DisplayName,"離得太遠了，回到能照應我們的位置！",2.5f);return false;}
        if(Advancing){Advancing=false;Returning=true;guideIndex=escortIndex=0;guideMotor.Reset();escortMotor.Reset();}
        else{Advancing=true;Notice(school.pihu.DisplayName,"走！達多，跟上！",2);}
        return true;
    }
    public void EscortHit()
    {
        if(!Fighting||Selected!=Mission.Pihu||(!Advancing&&!Returning))return;
        EscortPressure++;Notice(school.pihu.DisplayName,"火力太強！掩護我們，先撤回去！",2.5f);
        if(EscortPressure>=3)Fail("隊伍被火力壓制","比胡與達多退回掩體重整。\n清除或壓制守衛，再下令穿越。\n重試會保留「跟比胡掩護」。");
    }
    public void Damage(int amount,Vector3 source)
    {
        if(!Fighting||amount<=0)return;Health=Mathf.Max(0,Health-amount);hurtAt=Time.time;WorkProgress=0;
        if(Health==0)Fail("你受傷倒下了","回到分隊出發前重試。\n任務選擇保留，同伴的劇情結果不變。");
    }
    void Fail(string title,string body)
    {
        CurrentStage=Stage.Down;Advancing=Returning=false;combat.weapons.Suspend();combat.hud.SetActive(false);
        player.canMove=player.canLook=false;player.view.fieldOfView=60;combat.hurtOverlay.color=Color.clear;ui.Say("",null);ui.hint.text="R 重試目前任務";
        Menu.ShowResult(title,body,true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Retry()
    {
        if(CurrentStage!=Stage.Down)return false;
        StopAllCoroutines();Retries++;ResetMission();return true;
    }
    void StartRegroup()
    {
        CurrentStage=Stage.Regroup;Advancing=Returning=false;combat.weapons.Suspend();combat.hud.SetActive(false);combat.hurtOverlay.color=Color.clear;
        foreach(var e in Enemies)e.escortTarget=null;
        player.view.fieldOfView=60;player.speed=2.8f;ui.Say("",null);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        activeRoute=Selected==Mission.Bawan?westExit:eastExit;guideIndex=escortIndex=0;guideMotor.Reset();escortMotor.Reset();
        StartCoroutine(Regroup());
    }
    IEnumerator Regroup()
    {
        ui.objective.text="到校後空地，與另一隊匯合";
        var guide=Selected==Mission.Bawan?school.bawan:school.pihu;
        while(guideIndex<activeRoute.Length||otherIndex<otherRoute.Length||Flat(player.transform.position,P(0,78))>4||Flat(school.tado.transform.position,P(0,78))>4)
        {
            MoveRoute(guide,guideMotor,activeRoute,ref guideIndex,true,Selected==Mission.Bawan?Vector3.left*1.15f:Vector3.zero);
            if(Selected==Mission.Pihu)MoveRoute(school.tado,escortMotor,activeRoute,ref escortIndex,true,new Vector3(-.85f,0,-.9f));
            MoveOtherSquad();ui.hint.text=Flat(player.transform.position,guide.transform.position)>8?"同伴在前方等你，沿校舍側道跟上。":"跟隨同伴到校後空地 · WASD 移動 · 按住右鍵環視";
            yield return null;
        }
        player.canMove=false;
        yield return Say(school.bawan,"西邊的路通了。比胡，你們都到了？",3.8f,true);
        yield return Say(school.pihu,"到了，達多也在。剛才多虧有人看著我們。",4.2f,true);
        yield return Say(school.tado,"先離開學校。沿後面那條林路走，我在前面接應。",4.2f,true);
        CurrentStage=Stage.Complete;ui.objective.text="兩隊已在校後匯合";ui.Say("",null);ui.hint.text="";
        Menu.ShowResult("兩隊匯合",(Selected==Mission.Bawan?"你和巴萬打通了西側通道。":"你掩護比胡與達多穿越了東側火線。")+"\n\n本段試玩到此。下一段將沿林路繼續前進。",false);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    void MoveOtherSquad()
    {
        var other=Selected==Mission.Bawan?school.pihu:school.bawan;
        MoveRoute(other,otherMotor,otherRoute,ref otherIndex,false);
        if(Selected==Mission.Bawan)
        {
            MoveRoute(school.tado,escortMotor,otherRoute,ref otherEscortIndex,false,new Vector3(-.85f,0,-1.4f));
        }
    }
    void MoveRoute(Chapter2Actor actor,Chapter3PathMotor motor,Vector3[] route,ref int index,bool waitForPlayer,Vector3 offset=default)
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
            if(Chapter3PathMotor.Clear(actor.transform.position,right)){motor.Move(actor,right,3.1f);return;}
            if(Chapter3PathMotor.Clear(actor.transform.position,left)){motor.Move(actor,left,3.1f);return;}
            return;
        }
        bool playerAhead=Vector3.Dot(toPlayer,target-actor.transform.position)>0;
        if(waitForPlayer&&toPlayer.magnitude>8&&!playerAhead){actor.Face(player.transform.position);return;}
        motor.Move(actor,target,waitForPlayer?(playerAhead&&toPlayer.magnitude>5?3.1f:2.1f):2.9f);
        if(Flat(actor.transform.position,target)<.23f)index++;
    }
    int DefeatedCount(){int count=0;foreach(var e in Enemies)if(e.Defeated)count++;return count;}
    public void Notice(string speaker,string line,float seconds)
    {if(!Fighting)return;noticeUntil=Time.time+seconds;ui.Say(speaker,line);}
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
        StopAllCoroutines();CurrentStage=Stage.Inactive;ClearEnemies();
        if(combat&&combat.SchoolEncounter==this)combat.SchoolEncounter=null;
        if(Menu)Menu.Hide();if(scenery)scenery.gameObject.SetActive(false);
        if(distantShots)distantShots.Stop();if(ambient)ambient.ResetCalm();
    }
    void OnDestroy(){if(Menu)Destroy(Menu.gameObject);}
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        // Keep the beginning on the school approach so the sport viewing and
        // mission choice occur before combat; rear forest belongs to batch 5.
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
        Barricades=new Transform[2];
        for(int i=0;i<2;i++)
        {
            var gate=new GameObject(i==0?"西側第一道橫木路障":"西側校後橫木路障").transform;gate.SetParent(scenery,false);gate.position=P(-27,i==0?16:46);Barricades[i]=gate;
            foreach(float x in new[]{-3.2f,3.2f})Box(gate,new Vector3(x,.8f,0),new Vector3(.20f,1.6f,.22f),wood,"路障木柱");
            foreach(float y in new[]{.50f,1.12f})Box(gate,new Vector3(0,y,0),new Vector3(6.7f,.24f,.22f),wood,"可拆橫木");
            Box(gate,new Vector3(0,.8f,-.14f),new Vector3(.18f,.92f,.11f),metal,"橫木插銷");
        }
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
