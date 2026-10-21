using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Authored rescue branch. Choice is retained on retry; the ending commits the
// chapter result only after the outcome, roll call, conversation and Mona's close.
public sealed class Chapter3Rescue : MonoBehaviour
{
    public enum Stage { Inactive, Advancing, Injury, Choice, AwaitingLift, Supporting, Leaving, Handoff, Down, Complete }
    public enum Decision { None, Rescue, Leave }
    public Stage CurrentStage {get;private set;}
    public Decision Selected {get;private set;}
    public bool Active=>CurrentStage!=Stage.Inactive;
    public bool Supporting=>CurrentStage==Stage.Supporting;
    public bool CombatActive=>CurrentStage==Stage.AwaitingLift||Supporting||CurrentStage==Stage.Leaving;
    public float LiftProgress {get;private set;}
    public int Health {get;private set;}=100;
    public int Retries {get;private set;}
    public bool Delivered {get;private set;}
    public bool CompanionsWaiting {get;private set;}
    public Vector3 InjuryPosition=>new Vector3(-3,0,92);
    public Vector3 ReceptionPosition=>new Vector3(13,0,106);
    public Button ChoiceRescue {get;private set;}
    public Button ChoiceLeave {get;private set;}
    public Chapter3InjuryPose Pose {get;private set;}
    public Chapter3Ending Ending {get;private set;}
    Chapter3SchoolController school;
    Chapter3SchoolBattle battle;
    Chapter3Player player;
    Chapter3Combat combat;
    Chapter3Presentation ui;
    GameObject choicePanel;
    readonly Chapter3PathMotor bawanMotor=new Chapter3PathMotor(),pihuMotor=new Chapter3PathMotor(),tadoMotor=new Chapter3PathMotor();
    int bawanIndex,pihuIndex,tadoIndex;
    float hurtAt=-10,noticeUntil;
    Vector3 supportRight=Vector3.right,lastPlayerPosition;
    Collider[] pihuColliders;
    bool[] colliderEnabled;
    AudioSource shot;
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    static readonly Vector3[] approach={P(0,82),P(0,85),P(-3,92)};
    static readonly Vector3[] exit={P(-3,94),P(0,98),P(8,103),P(13,106)};

    public void Begin(Chapter3SchoolController owner,Chapter3SchoolBattle encounter)
    {
        Suspend();school=owner;battle=encounter;player=school.player;combat=school.flow.combat;ui=school.ui;
        Selected=Decision.None;Retries=0;Health=100;Delivered=false;LiftProgress=0;
        Pose=school.pihu.GetComponent<Chapter3InjuryPose>();if(!Pose)Pose=school.pihu.gameObject.AddComponent<Chapter3InjuryPose>();
        Pose.Configure(school.pihu);Pose.ResetPose();
        pihuColliders=school.pihu.GetComponentsInChildren<Collider>(true);colliderEnabled=new bool[pihuColliders.Length];
        for(int i=0;i<pihuColliders.Length;i++)colliderEnabled[i]=pihuColliders[i].enabled;
        if(!choicePanel)BuildChoice();choicePanel.SetActive(false);battle.Menu.Hide();
        bawanIndex=pihuIndex=tadoIndex=0;bawanMotor.Reset();pihuMotor.Reset();tadoMotor.Reset();
        combat.weapons.Suspend();combat.hud.SetActive(false);ui.Say("",null);
        CurrentStage=Stage.Advancing;player.speed=2.8f;player.canMove=player.canLook=true;
        player.FocusOn(P(0,85)+Vector3.up*1.2f);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        ui.objective.text="與巴萬、比胡、達多沿林路離開學校";
    }
    void Update()
    {
        if(!Active||!school)return;
        if(CurrentStage==Stage.Advancing){AdvanceTogether();return;}
        if(CurrentStage==Stage.Choice)
        {
            if(Keyboard.current?.digit1Key.wasPressedThisFrame==true)Choose(Decision.Rescue);
            else if(Keyboard.current?.digit2Key.wasPressedThisFrame==true)Choose(Decision.Leave);
            return;
        }
        if(CurrentStage==Stage.Down){if(Keyboard.current?.rKey.wasPressedThisFrame==true)Retry();return;}
        if(!CombatActive)return;
        if(noticeUntil>0&&Time.time>=noticeUntil){noticeUntil=0;ui.Say("",null);}
        combat.playerTarget.position=player.view.transform.position-Vector3.up*.38f;
        MoveAlong(school.tado,tadoMotor,exit,ref tadoIndex,3.2f,false,Vector3.right*1.1f);
        if(Selected==Decision.Leave)
            MoveAlong(school.bawan,bawanMotor,exit,ref bawanIndex,2.25f,true,Vector3.left*1.1f);
        else
        {
            Vector3 escort=player.transform.position+Vector3.left*1.8f+Vector3.forward*2;
            if(Flat(school.bawan.transform.position,escort)>1)bawanMotor.Move(school.bawan,escort,2.4f);
            else school.bawan.Face(school.pihu.transform.position);
        }
        if(CurrentStage==Stage.AwaitingLift)
        {
            bool near=Flat(player.transform.position,school.pihu.transform.position)<=2;
            WorkLift(Keyboard.current?.eKey.isPressed==true,Time.deltaTime);
            if(!Supporting)
            {
                player.speed=combat.weapons.Aiming?1.7f:2.8f;
                ui.objective.text="救援比胡 · 靠近並扶起他";
                ui.hint.text=Flat(player.transform.position,school.pihu.transform.position)<.65f?"退後半步，站到比胡身旁再按住 E 扶起。":near?"按住 E 扶起比胡 · 放開會中止 · "+combat.weapons.Controls:"靠近比胡 · 先處理前方警察，再扶他離開";
            }
        }
        else if(Supporting)
        {
            UpdateSupport();
            if(player.ActionPressed)PutDown();
            ui.objective.text="扶著比胡前往林間接應處";
            ui.hint.text="WASD 扶行 · 按住右鍵環視 · E 放下比胡後可使用武器";
        }
        else
        {player.speed=combat.weapons.Aiming?1.7f:2.8f;ui.objective.text="跟上巴萬，前往林間接應處";ui.hint.text="比胡留在林路轉折處 · "+combat.weapons.Controls;}
        UpdateHud();
        bool playerArrived=Flat(player.transform.position,ReceptionPosition)<2.6f;
        bool pihuArrived=Selected==Decision.Leave||(Supporting&&Flat(school.pihu.transform.position,ReceptionPosition)<3.5f);
        if(playerArrived&&pihuArrived&&tadoIndex>=exit.Length&&Flat(school.bawan.transform.position,ReceptionPosition)<5)
            StartCoroutine(Handoff());
    }
    void AdvanceTogether()
    {
        // Everyone walks the same established rear route before the fixed event.
        MoveAlong(school.bawan,bawanMotor,approach,ref bawanIndex,2.1f,true,Vector3.left*1.15f);
        MoveAlong(school.pihu,pihuMotor,approach,ref pihuIndex,2.05f,true,Vector3.zero);
        MoveAlong(school.tado,tadoMotor,approach,ref tadoIndex,2.2f,true,Vector3.right*1.15f);
        CompanionsWaiting=Flat(player.transform.position,school.pihu.transform.position)>7;
        ui.hint.text=Flat(player.transform.position,InjuryPosition)<1.1f?"讓出林路轉折處，等同伴一起過來。":CompanionsWaiting?"同伴在林路上等你，走近後繼續。":"跟上同伴 · WASD 移動 · 按住右鍵環視";
        if(bawanIndex>=approach.Length&&pihuIndex>=approach.Length&&tadoIndex>=approach.Length&&Flat(player.transform.position,InjuryPosition)<5)
            StartCoroutine(Injury());
    }
    IEnumerator Injury()
    {
        CurrentStage=Stage.Injury;CompanionsWaiting=false;player.canMove=false;player.canLook=true;
        school.pihu.transform.position=InjuryPosition;
        ui.objective.text="比胡受傷了";ui.hint.text="";player.FocusOn(InjuryPosition+Vector3.up*.9f);
        if(!shot){shot=gameObject.AddComponent<AudioSource>();shot.playOnAwake=false;shot.spatialBlend=0;shot.volume=.4f;}
        if(combat.weapons.shotClip)shot.PlayOneShot(combat.weapons.shotClip);
        SetPihuCollision(false);
        for(float t=0;t<1.35f;t+=Time.deltaTime){Pose.SetInjured(Mathf.SmoothStep(0,1,t/1.35f));yield return null;}
        Pose.SetInjured(1);
        yield return Say(school.pihu,"啊……我的腿！我走不動了……",3.6f);
        yield return Say(school.bawan,"比胡！前面還有警察。你扶他，我替你看著路！",4.5f);
        yield return Say(school.tado,"我先到前面的接應棚。把他帶過來！",3.5f);
        CurrentStage=Stage.Choice;ui.objective.text="決定是否救援比胡";ui.hint.text="選擇 1／2，或點選按鈕";
        choicePanel.SetActive(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Choose(Decision decision)
    {
        if(CurrentStage!=Stage.Choice||decision==Decision.None)return false;
        Selected=decision;choicePanel.SetActive(false);ResetCheckpoint(false);return true;
    }
    void ResetCheckpoint(bool retry)
    {
        ClearGuard();bawanMotor.Reset();pihuMotor.Reset();tadoMotor.Reset();bawanIndex=tadoIndex=0;
        Health=100;hurtAt=-10;noticeUntil=0;LiftProgress=0;Delivered=false;
        Pose.SetSupported(0,player.transform);Pose.SetInjured(1);SetPihuCollision(false);
        school.pihu.transform.position=InjuryPosition;school.pihu.Face(InjuryPosition+Vector3.back);
        if(retry)
        {
            school.bawan.transform.position=P(-5,93);school.tado.transform.position=P(-.8f,94);
            player.Warp(P(-3.7f,90.5f)+Vector3.up*.06f,InjuryPosition+Vector3.up*.7f);
        }
        var go=Instantiate(combat.enemyTemplates[0],school.transform);go.name="林路追來的警察";go.transform.position=P(1.5f,97);
        var enemy=go.GetComponent<Chapter3Enemy>();enemy.combat=combat;enemy.advances=false;enemy.attackInterval=7;enemy.engagementRange=17;enemy.escortTarget=null;
        battle.Enemies.Add(enemy);go.SetActive(true);
        CurrentStage=Selected==Decision.Rescue?Stage.AwaitingLift:Stage.Leaving;
        battle.Menu.Hide();combat.weapons.ResetEquipment();combat.hud.SetActive(true);combat.downPanel.SetActive(false);combat.hurtOverlay.color=Color.clear;
        player.canMove=player.canLook=true;player.speed=2.8f;player.view.fieldOfView=60;
        ui.Say("",null);ui.fade.color=Color.clear;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        if(Selected==Decision.Rescue)Notice(school.bawan.DisplayName,"扶住他，別急。我跟著你們。",3.5f);
        else Notice(school.bawan.DisplayName,"……跟上。別再有人落在後面。",3.5f);
    }
    public bool WorkLift(bool held,float delta)
    {
        if(CurrentStage!=Stage.AwaitingLift)return false;
        float distance=Flat(player.transform.position,school.pihu.transform.position);
        if(!held||delta<=0||distance>2||distance<.65f||combat.weapons.Reloading||Time.time<hurtAt+.5f)
        {LiftProgress=0;Pose.SetSupported(0,player.transform);return false;}
        LiftProgress=Mathf.Clamp01(LiftProgress+delta/2);
        Pose.SetSupported(LiftProgress,player.transform);
        if(LiftProgress<1)return false;
        CurrentStage=Stage.Supporting;combat.weapons.Suspend();player.view.fieldOfView=60;player.speed=1.15f;
        supportRight=Vector3.ProjectOnPlane(school.pihu.transform.position-player.transform.position,Vector3.up).normalized;
        if(supportRight.sqrMagnitude<.1f)supportRight=Vector3.right;
        lastPlayerPosition=player.transform.position;SetPihuCollision(false);ui.Say("",null);noticeUntil=0;
        return true;
    }
    void UpdateSupport()
    {
        Vector3 delta=Vector3.ProjectOnPlane(player.transform.position-lastPlayerPosition,Vector3.up);
        if(delta.magnitude>.001f)
        {
            Vector3 nextRight=Vector3.Cross(Vector3.up,delta.normalized);
            // Keep the supported side stable when the player backs up.
            if(Vector3.Dot(nextRight,supportRight)<0)nextRight=-nextRight;
            supportRight=Vector3.RotateTowards(supportRight,nextRight,Time.deltaTime*1.4f,0);
        }
        Vector3 target=player.transform.position+supportRight*.85f;target.y=0;
        if(Chapter3PathMotor.Clear(school.pihu.transform.position,target))pihuMotor.Move(school.pihu,target,1.65f);
        Pose.SetSupported(1,player.transform);player.speed=1.15f;lastPlayerPosition=player.transform.position;
    }
    public bool PutDown()
    {
        if(!Supporting)return false;
        CurrentStage=Stage.AwaitingLift;LiftProgress=0;Pose.SetSupported(0,player.transform);combat.weapons.gameObject.SetActive(true);player.speed=2.8f;
        Notice(school.pihu.DisplayName,"我在這裡等你……",2.5f);return true;
    }
    public void Damage(int amount,Vector3 source)
    {
        if(!CombatActive||amount<=0)return;
        Health=Mathf.Max(0,Health-amount);hurtAt=Time.time;
        if(CurrentStage==Stage.AwaitingLift){LiftProgress=0;Pose.SetSupported(0,player.transform);}
        if(Health>0)return;
        CurrentStage=Stage.Down;combat.weapons.Suspend();combat.hud.SetActive(false);combat.hurtOverlay.color=Color.clear;
        player.canMove=player.canLook=false;player.view.fieldOfView=60;ui.Say("",null);ui.hint.text="R 從林路檢查點重試";
        Pose.SetSupported(0,player.transform);
        battle.Menu.ShowResult("你受傷倒下了",Selected==Decision.Rescue?"回到比胡受傷處。\n救援選擇與攜帶的武器會保留。\n先處理警察，再扶比胡走向接應處。":"回到林路檢查點。\n留下比胡的選擇與攜帶的武器會保留。",true);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool Retry()
    {
        if(CurrentStage!=Stage.Down)return false;
        StopAllCoroutines();Retries++;ResetCheckpoint(true);return true;
    }
    IEnumerator Handoff()
    {
        CurrentStage=Stage.Handoff;player.canMove=false;player.canLook=true;player.speed=2.8f;
        combat.weapons.Suspend();combat.hud.SetActive(false);combat.hurtOverlay.color=Color.clear;ui.hint.text="";ui.Say("",null);
        if(Selected==Decision.Rescue)
        {
            // Tado comes to the actual pair before taking over; no offscreen warp.
            Vector3 target=ReceptionStand();
            float until=Time.time+6;
            while(Flat(school.tado.transform.position,target)>.3f&&Time.time<until){tadoMotor.Move(school.tado,target,2);yield return null;}
            school.tado.Face(school.pihu.transform.position);
            player.FocusOn(school.pihu.transform.position+Vector3.up*.9f);
            yield return Say(school.tado,"到了，我接住他。你先喘口氣。",3.8f);
            for(float t=0;t<1.2f;t+=Time.deltaTime){Pose.SetSupported(1-Mathf.SmoothStep(0,1,t/1.2f),player.transform);yield return null;}
            Pose.SetSupported(0,player.transform);Delivered=true;
            yield return Say(school.pihu,"謝謝……我還撐得住。",3.5f);
        }
        else
        {
            player.FocusOn(school.tado.transform.position+Vector3.up*1.2f);
            yield return Say(school.tado,"你們到了……比胡呢？",3.5f);
            yield return Say(school.bawan,"他還留在後面的林路上。",3.5f);
        }
        CurrentStage=Stage.Complete;ClearGuard();
        if(!Ending)Ending=gameObject.GetComponent<Chapter3Ending>();
        if(!Ending)Ending=gameObject.AddComponent<Chapter3Ending>();
        Ending.Begin(school,this);
    }
    void UpdateHud()
    {
        combat.healthText.text="生命值  "+Health+" / 100";
        var w=combat.weapons;combat.ammoText.text=Supporting?"正在扶行 · 武器收起":w.Selected==Chapter3Weapons.Weapon.Knife?"獵刀":"村田十八年式  "+(w.Loaded?1:0)+" / 1   備彈 "+w.Reserve;
        combat.combatText.text=Supporting?"比胡靠著你移動\n前往接應棚 · E 可放下":CurrentStage==Stage.AwaitingLift?"扶起比胡  "+Mathf.RoundToInt(LiftProgress*100)+"%":"沿林路前進，跟同伴保持距離";
        if(!Supporting)
        {
            if(w.Reloading)combat.combatText.text+="\n單發裝填  "+Mathf.CeilToInt(w.ReloadProgress*100)+"%";
            else if(!string.IsNullOrEmpty(w.Feedback))combat.combatText.text+="\n"+w.Feedback;
        }
        foreach(var e in battle.Enemies)if(e&&!e.Defeated&&e.Telegraphing)combat.combatText.text+="\n警察正在瞄準！";
        combat.crosshair.text=Supporting?"":w.Aiming?"·":"+";
        combat.hurtOverlay.color=new Color(.6f,.025f,.01f,Mathf.Clamp01(1-(Time.time-hurtAt)/.7f)*.32f);
    }
    public void Notice(string speaker,string line,float seconds)
    {if(!CombatActive)return;ui.Say(speaker,line);noticeUntil=Time.time+seconds;}
    IEnumerator Say(Chapter2Actor actor,string line,float duration)
    {
        actor.Face(player.transform.position);actor.speaking=true;ui.Say(actor.DisplayName,line);
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,duration,null);
        yield return Chapter3Flow.WaitForLine(duration);actor.speaking=false;ui.Say("",null);
    }
    void MoveAlong(Chapter2Actor actor,Chapter3PathMotor motor,Vector3[] route,ref int index,float speed,bool waits,Vector3 offset)
    {
        if(index>=route.Length)return;
        var target=route[index]+offset;
        var toPlayer=Vector3.ProjectOnPlane(player.transform.position-actor.transform.position,Vector3.up);
        if(waits&&toPlayer.magnitude>7&&Vector3.Dot(toPlayer,target-actor.transform.position)<0){actor.Face(player.transform.position);return;}
        if(Flat(target,player.transform.position)<1.05f)
        {
            if(CurrentStage==Stage.Advancing&&actor==school.pihu&&index==route.Length-1){actor.Face(player.transform.position);return;}
            var apart=Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up).normalized;
            if(apart.sqrMagnitude<.1f)apart=Vector3.left;
            target=player.transform.position+apart*1.05f;target.y=0;
        }
        Vector3 direction=Vector3.ProjectOnPlane(target-actor.transform.position,Vector3.up).normalized;
        float along=Vector3.Dot(toPlayer,direction);
        if(toPlayer.magnitude<2.2f&&along>0&&along<Flat(actor.transform.position,target)&&(toPlayer-direction*along).magnitude<1.05f)
        {
            Vector3 side=Vector3.Cross(Vector3.up,direction)*1.25f;
            foreach(var candidate in new[]{player.transform.position+side,player.transform.position-side})
                if(Chapter3PathMotor.Clear(actor.transform.position,candidate)){motor.Move(actor,candidate,speed);return;}
            return;
        }
        motor.Move(actor,target,speed);if(Flat(actor.transform.position,target)<.25f)index++;
    }
    Vector3 ReceptionStand()
    {
        Vector3 center=school.pihu.transform.position;
        foreach(var offset in new[]{Vector3.forward,Vector3.right,Vector3.left,Vector3.back})
        {
            Vector3 candidate=center+offset*1.25f;
            if(Flat(candidate,player.transform.position)>1&&Chapter3PathMotor.Clear(school.tado.transform.position,candidate))return candidate;
        }
        // The companion has already physically reached the reception waypoint.
        // If the player occupies every nearby approach, speak from that safe spot.
        return school.tado.transform.position;
    }
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        if(CurrentStage==Stage.Advancing){desired.z=Mathf.Min(desired.z,94);return desired;}
        if(!CombatActive)return desired;
        // Keep the pair on the existing 4.3 m forest road; the body sweep still
        // handles rocks, posts and the school scenery.
        Vector3 closest=school.rescueRoute[0];float distance=float.PositiveInfinity;
        for(int i=1;i<school.rescueRoute.Length;i++)
        {
            var a=school.rescueRoute[i-1];var d=school.rescueRoute[i]-a;d.y=0;
            var point=a+d*Mathf.Clamp01(Vector3.Dot(desired-a,d)/d.sqrMagnitude);point.y=desired.y;
            float sq=(point-desired).sqrMagnitude;if(sq<distance){distance=sq;closest=point;}
        }
        float width=Supporting?1.35f:2.05f;
        if(distance>width*width)desired=closest+Vector3.ClampMagnitude(desired-closest,width);
        if(Supporting)
        {
            var pihuTarget=desired+supportRight*.85f;pihuTarget.y=0;
            if(!Chapter3PathMotor.Clear(school.pihu.transform.position,pihuTarget)||Flat(pihuTarget,school.pihu.transform.position)>1.65f)
            {desired.x=current.x;desired.z=current.z;}
        }
        return desired;
    }
    void SetPihuCollision(bool enabled)
    {
        if(pihuColliders==null)return;
        for(int i=0;i<pihuColliders.Length;i++)if(pihuColliders[i])pihuColliders[i].enabled=enabled&&colliderEnabled[i];
    }
    void ClearGuard()
    {
        if(!battle)return;
        foreach(var enemy in battle.Enemies)if(enemy){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
        battle.Enemies.Clear();
    }
    public void Suspend()
    {
        if(Ending)Ending.Suspend();
        StopAllCoroutines();CurrentStage=Stage.Inactive;Selected=Decision.None;Delivered=false;LiftProgress=0;CompanionsWaiting=false;
        if(Pose)Pose.ResetPose();SetPihuCollision(true);if(choicePanel)choicePanel.SetActive(false);if(shot)shot.Stop();
        if(school){school.bawan.speaking=school.pihu.speaking=school.tado.speaking=false;}
        if(player){player.speed=2.8f;player.view.fieldOfView=60;}
    }
    void OnDestroy(){if(choicePanel)Destroy(choicePanel);}
    void BuildChoice()
    {
        var panel=Panel(ui.canvas.transform,"比胡救援選擇",new Vector2(.14f,.27f),new Vector2(.86f,.74f));choicePanel=panel.gameObject;
        Label(panel,"比胡受傷，無法繼續行走",32,new Vector2(.05f,.76f),new Vector2(.95f,.97f));
        ChoiceRescue=Button(panel,"1  救援比胡",new Vector2(.05f,.46f),new Vector2(.48f,.69f));
        ChoiceLeave=Button(panel,"2  留下比胡，繼續前進",new Vector2(.52f,.46f),new Vector2(.95f,.69f));
        Label(panel,"靠近後按住 E 扶起，帶他到接應處。\n若途中倒下，重試仍保留救援選擇。",23,new Vector2(.055f,.10f),new Vector2(.475f,.42f));
        Label(panel,"讓比胡留在受傷處，\n跟巴萬走向接應棚。",23,new Vector2(.535f,.10f),new Vector2(.945f,.42f));
        ChoiceRescue.onClick.AddListener(()=>Choose(Decision.Rescue));ChoiceLeave.onClick.AddListener(()=>Choose(Decision.Leave));
    }
    RectTransform Panel(Transform parent,string label,Vector2 min,Vector2 max)
    {
        var g=new GameObject(label,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;g.GetComponent<Image>().color=new Color(.025f,.045f,.038f,.97f);return r;
    }
    void Label(Transform parent,string value,int size,Vector2 min,Vector2 max)
    {
        var g=new GameObject(value,typeof(RectTransform),typeof(Text));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.font=ui.subtitle.font;t.fontSize=size;t.color=new Color(.95f,.88f,.70f);t.text=value;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;
    }
    Button Button(Transform parent,string value,Vector2 min,Vector2 max)
    {
        var r=Panel(parent,value,min,max);r.GetComponent<Image>().color=new Color(.26f,.33f,.23f,1);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();Label(r,value,26,new Vector2(.04f,0),new Vector2(.96f,1));return b;
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
