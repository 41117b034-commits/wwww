using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The authored outcome owns the facts. The conversation can only respond to them.
public sealed class Chapter3Ending : MonoBehaviour
{
    public enum Stage { Inactive, Outcome, RollCall, Conversation, MonaClosing, Complete }
    public Stage CurrentStage {get;private set;}
    public bool Active=>CurrentStage!=Stage.Inactive;
    public Chapter3ResultStore.Record Result {get;private set;}
    public Chapter3EndingConversation Conversation {get;private set;}
    public Chapter2Actor Mona {get;private set;}
    public bool PersistResult=true;
    public bool Saved {get;private set;}
    public bool DeathConfirmed {get;private set;}
    public bool RollCallCompleted {get;private set;}
    Chapter3SchoolController school;
    Chapter3Rescue rescue;
    Chapter3Player player;
    Chapter3Presentation ui;
    readonly Chapter3PathMotor tadoMotor=new Chapter3PathMotor(),monaMotor=new Chapter3PathMotor();
    Transform monaParent;
    Vector3 monaLocalPosition,monaLocalScale,monaFacing;
    Quaternion monaLocalRotation;
    bool monaActive,monaBorrowed,cutaway;
    Vector3 receptionPlayer,viewLocalPosition;
    Quaternion receptionRotation,viewLocalRotation;
    Text endTitle,endBody;
    Button retrySave;
    string previousTitle,previousBody,previousReplay;
    bool endingLabelsChanged;
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);

    public void Begin(Chapter3SchoolController owner,Chapter3Rescue branch)
    {
        Suspend();school=owner;rescue=branch;player=owner.player;ui=owner.ui;
        Result=Chapter3ResultStore.Build(owner.Battle.Selected,branch.Selected,
            owner.flow.CarriedWeapon??Chapter3Weapons.Weapon.Rifle);
        if(Result==null)return;
        Saved=DeathConfirmed=RollCallCompleted=false;
        if(!Conversation)Conversation=gameObject.GetComponent<Chapter3EndingConversation>();
        if(!Conversation)Conversation=gameObject.AddComponent<Chapter3EndingConversation>();
        Conversation.PrepareService();
        player.canMove=false;player.canLook=true;player.view.fieldOfView=60;
        school.flow.combat.weapons.Suspend();school.flow.combat.hud.SetActive(false);
        owner.Battle.Menu.Hide();ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);
        ui.hint.text="空白鍵略過當句台詞";
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        tadoMotor.Reset();monaMotor.Reset();CurrentStage=Stage.Outcome;
        StartCoroutine(ResolveOutcome());
    }
    IEnumerator ResolveOutcome()
    {
        if(Result.pihuSurvived)
        {
            ui.objective.text="林間接應 · 比胡生還";
            player.FocusOn(school.pihu.transform.position+Vector3.up*.8f);
            yield return Say(school.tado,"比胡還活著。先讓他坐著休息，我會照看他。",4.8f);
            yield return Say(school.pihu,"我還在……謝謝你沒有把我留在那裡。",4.8f);
            yield return Say(school.bawan,"人帶回來了。先把大家清點清楚。",3.8f);
        }
        else
        {
            ui.objective.text="達多返回林路確認比胡的情況";
            yield return Say(school.tado,"我回去找他。你們在這裡等。",3.8f);
            // This is a cinematic cut, not a gameplay teleport or a new scene.
            // Keep headset tracking untouched; VR remains at the reception.
            if(!player.IsVR)
            {
                yield return Fade(true,.65f);
                receptionPlayer=player.transform.position;receptionRotation=player.transform.rotation;
                viewLocalPosition=player.view.transform.localPosition;viewLocalRotation=player.view.transform.localRotation;
                cutaway=true;player.canLook=false;
                player.Warp(P(1.3f,90.1f)+Vector3.up*.06f,rescue.InjuryPosition+Vector3.up*.55f);
                ui.objective.text="林路 · 稍後";
                yield return Fade(false,.65f);
            }
            yield return Walk(school.tado,tadoMotor,new[]{P(8,103),P(0,98),P(-1,94),P(-1.75f,92)},3.2f);
            school.tado.Face(school.pihu.transform.position);
            yield return Say(school.tado,"比胡……我回來了。",3.2f,false,false);
            for(float t=0;t<2.8f;t+=Time.deltaTime)
            {rescue.Pose.SetDying(t/2.8f);yield return null;}
            rescue.Pose.SetDying(1);DeathConfirmed=true;
            yield return new WaitForSeconds(1.4f);
            yield return Say(school.tado,"比胡……",2.8f,false,false);
            if(cutaway)
            {
                yield return Fade(true,.65f);RestoreView();
                ui.objective.text="林間接應 · 等待達多回報";
                yield return Fade(false,.65f);
            }
            yield return Walk(school.tado,tadoMotor,new[]{P(-1,94),P(0,98),P(8,103),P(14.2f,105)},3.2f);
            yield return Say(school.tado,"我找到比胡了。他已經死了。",4.8f);
            yield return Say(school.bawan,"……記住他。我們一起出發，卻沒能一起回來。",4.8f);
        }
        CurrentStage=Stage.RollCall;ui.objective.text="清點同行的小隊";
        yield return Say(school.bawan,Result.mission==Chapter3SchoolBattle.Mission.Tado
            ?"你跟達多攻進學校，我走左側，比胡走右側。三路在校後匯合，才一起撤退。"
            :Result.mission==Chapter3SchoolBattle.Mission.Bawan
                ?"你和我走左側，達多攻進學校，比胡走右側。三路在校後匯合，才一起撤退。"
                :"你掩護比胡走右側，我走左側，達多攻進學校。三路在校後匯合，才一起撤退。",5.8f);
        yield return Say(school.tado,Result.pihuSurvived
            ?"我們這一隊四個人都到了。比胡受了傷，接下來需要照應。"
            :"到這裡的只有你、巴萬和我。四個人出發，三個人回來。",5.2f);
        string weapon=Result.weapon==Chapter3Weapons.Weapon.Knife?"獵刀":"村田十八年式步槍";
        yield return Say(school.bawan,"你的"+weapon+"還在。先收好，這裡讓傷者和同伴歇一歇。",4.6f);
        RollCallCompleted=true;CurrentStage=Stage.Conversation;
        ui.objective.text="林間暫歇 · 與巴萬交談";ui.hint.text="輸入想說的話，按 Enter 或「送出」";
        player.FocusOn(school.bawan.transform.position+Vector3.up*1.25f);
        Conversation.Begin(school,Result,()=>
        {if(CurrentStage==Stage.Conversation)StartCoroutine(CloseWithMona());});
    }
    IEnumerator CloseWithMona()
    {
        CurrentStage=Stage.MonaClosing;ui.objective.text="莫那抵達接應處";ui.hint.text="空白鍵略過當句台詞";
        yield return Fade(true,.8f);
        BorrowMona();
        if(!Mona)
        {
            yield return Fade(false,.5f);
            ui.objective.text="接應尚未完成";ui.hint.text="莫那尚未到場，結果未保存。按 F8 重進運動會。";
            yield break;
        }
        ui.objective.text="林間接應處 · 稍後";
        yield return Fade(false,.8f);
        if(Mona)
        {
            yield return Walk(Mona,monaMotor,new[]{P(16.5f,102),P(15.3f,104)},1.8f);
            yield return Say(Mona,"人都到齊了嗎？把你們的情況告訴我。",4.5f);
            yield return Say(school.bawan,Result.mission==Chapter3SchoolBattle.Mission.Bawan
                ?"他和我打通左側。中央和右側的經過，是在校後匯合時聽達多、比胡回報的。"
                :"我打通左側。中央和右側的經過，是在校後匯合時聽他們回報的。",5.7f);
            yield return Say(school.tado,Result.mission==Chapter3SchoolBattle.Mission.Tado
                ?"他跟我從中央攻進學校；比胡帶右側。三路在校後匯合，才一起撤進林路。"
                :Result.mission==Chapter3SchoolBattle.Mission.Pihu
                    ?"我從中央攻進學校；他掩護比胡穿過右側。三路在校後匯合，才一起撤進林路。"
                    :"我從中央攻進學校；比胡帶右側。三路在校後匯合，才一起撤進林路。",5.7f);
            yield return Say(school.bawan,Result.pihuSurvived
                ?"我們四個都到了。比胡在撤離時受傷，是他扶著比胡走回來的。"
                :"我們回來三個人。比胡受傷後留在林路，達多回去確認，他已經死了。",5.7f);
            yield return Say(Mona,Result.pihuSurvived
                ?"把同伴帶回來，就繼續照看好他。接下來，誰都不能落單。"
                :"我記住了。留下的人已經回不來，還在的人要彼此照應。",5.5f);
            yield return Say(Mona,"這場戰鬥還沒有結束。先退入山林，守住身邊的人。",5.2f);
        }
        yield return Fade(true,1.1f);
        // No earlier phase, chat answer, retry or shortcut commits a result.
        CommitResult();
        CurrentStage=Stage.Complete;player.canMove=player.canLook=false;
        ShowEnding();ui.hint.text="";ui.Say("",null);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    void BorrowMona()
    {
        Mona=school.flow.station.mona;if(!Mona)return;
        monaParent=Mona.transform.parent;monaLocalPosition=Mona.transform.localPosition;
        monaLocalRotation=Mona.transform.localRotation;monaLocalScale=Mona.transform.localScale;
        monaFacing=Mona.facing;monaActive=Mona.gameObject.activeSelf;monaBorrowed=true;
        Mona.transform.SetParent(transform,true);Mona.transform.position=P(19,100);
        Mona.gameObject.SetActive(true);Mona.speaking=false;Mona.Face(P(15.3f,104));
    }
    void ReturnMona()
    {
        if(!monaBorrowed)return;monaBorrowed=false;
        if(!Mona||!monaParent)return;
        Mona.speaking=false;Mona.transform.SetParent(monaParent,false);
        Mona.transform.localPosition=monaLocalPosition;Mona.transform.localRotation=monaLocalRotation;
        Mona.transform.localScale=monaLocalScale;Mona.facing=monaFacing;
        Mona.gameObject.SetActive(monaActive);Mona=null;
    }
    IEnumerator Walk(Chapter2Actor actor,Chapter3PathMotor motor,Vector3[] route,float speed)
    {
        foreach(var destination in route)
        {
            // Keep walking on the collision-tested route rather than time-out warp.
            while(Flat(actor.transform.position,destination)>.26f)
            {motor.Move(actor,destination,speed);yield return null;}
        }
    }
    IEnumerator Say(Chapter2Actor actor,string line,float duration,bool focus=true,bool facePlayer=true)
    {
        if(facePlayer)actor.Face(player.transform.position);actor.speaking=true;
        if(focus)player.FocusOn(actor.transform.position+Vector3.up*1.2f);
        ui.Say(actor.DisplayName,line);actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,duration,null);
        yield return Chapter3Flow.WaitForLine(duration);actor.speaking=false;ui.Say("",null);
    }
    IEnumerator Fade(bool dark,float duration)
    {
        float from=ui.fade.color.a,to=dark?1:0;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
        {ui.fade.color=new Color(0,0,0,Mathf.Lerp(from,to,t/duration));yield return null;}
        ui.fade.color=new Color(0,0,0,to);
    }
    void RestoreView()
    {
        if(!cutaway||!player)return;cutaway=false;
        player.Warp(receptionPlayer,receptionPlayer+receptionRotation*Vector3.forward*5);
        player.transform.rotation=receptionRotation;
        player.view.transform.localPosition=viewLocalPosition;player.view.transform.localRotation=viewLocalRotation;
        player.canLook=true;
    }
    void ShowEnding()
    {
        var labels=ui.ending.GetComponentsInChildren<Text>(true);
        foreach(var label in labels)
        {
            if(label.GetComponentInParent<Button>())continue;
            if(!endTitle)endTitle=label;else if(!endBody)endBody=label;
        }
        if(endTitle&&endBody)
        {
            if(!endingLabelsChanged)
            {previousTitle=endTitle.text;previousBody=endBody.text;previousReplay=ui.replay.GetComponentInChildren<Text>().text;}
            endingLabelsChanged=true;endTitle.text="第三章 · 霧社突襲與戰鬥";
            endBody.text=(Result.pihuSurvived?"比胡生還，四人抵達接應處。":"比胡死亡，三人抵達接應處。")+
                "\n"+(Result.mission==Chapter3SchoolBattle.Mission.Tado?"你與達多完成中央攻入。":
                    Result.mission==Chapter3SchoolBattle.Mission.Bawan?"你與巴萬完成左側突破。":"你掩護比胡完成右側穿越。")+
                "\n\n"+(Saved?"結果已保存。戰鬥還沒有結束……":PersistResult?"結果尚未保存，請按「重試保存」。":"戰鬥還沒有結束……");
            ui.replay.GetComponentInChildren<Text>().text="重玩第三章";
        }
        if(PersistResult&&!Saved)
        {
            if(!retrySave)
            {
                var button=Instantiate(ui.replay,ui.replay.transform.parent);
                button.name="重試保存第三章結果";button.onClick=new Button.ButtonClickedEvent();
                button.onClick.AddListener(()=>RetrySave());button.GetComponentInChildren<Text>().text="重試保存";
                retrySave=button;
            }
            ui.replay.gameObject.SetActive(false);retrySave.gameObject.SetActive(true);
        }
        else {ui.replay.gameObject.SetActive(true);if(retrySave)retrySave.gameObject.SetActive(false);}
        ui.ending.SetActive(true);
    }
    void CommitResult()
    {
        Saved=false;
        if(!PersistResult||!Chapter3ResultStore.SaveCompleted(Result))return;
        var stored=Chapter3ResultStore.Load();
        if(stored==null)return;
        Result=stored;Saved=true;
    }
    public bool RetrySave()
    {
        if(CurrentStage!=Stage.Complete||Saved||!PersistResult)return false;
        CommitResult();ShowEnding();return Saved;
    }
    void RestoreEndingLabels()
    {
        if(!endingLabelsChanged)return;endingLabelsChanged=false;
        if(endTitle)endTitle.text=previousTitle;if(endBody)endBody.text=previousBody;
        if(ui&&ui.replay)ui.replay.GetComponentInChildren<Text>().text=previousReplay;
        if(ui&&ui.replay)ui.replay.gameObject.SetActive(true);if(retrySave)retrySave.gameObject.SetActive(false);
    }
    public void Suspend()
    {
        StopAllCoroutines();if(Conversation)Conversation.Cancel();RestoreView();ReturnMona();RestoreEndingLabels();
        CurrentStage=Stage.Inactive;Saved=DeathConfirmed=RollCallCompleted=false;Result=null;
        if(school)school.bawan.speaking=school.pihu.speaking=school.tado.speaking=false;
        if(ui){ui.chat.SetActive(false);ui.ending.SetActive(false);ui.fade.color=Color.clear;}
    }
    void OnDisable()
    {
        // Flow calls Suspend before deactivating a region, while returning Mona
        // and restoring the camera is still safe. Replay/menu unloads the scene
        // directly: OnDisable then runs inside hierarchy teardown, where moving
        // Mona out of the deactivating School parent is forbidden. Cancel only;
        // the scene owns and destroys both regions and their actors together.
        // Keep the borrowed state if this was only a temporary disable, so the
        // next explicit Suspend/Begin can restore it outside the callback.
        StopAllCoroutines();if(Conversation)Conversation.Cancel();
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
