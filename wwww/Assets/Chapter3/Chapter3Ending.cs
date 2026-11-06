using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Authored outcomes lead directly to Mona's single roll call and closing.
public sealed partial class Chapter3Ending : MonoBehaviour
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
    bool monaActive,monaBorrowed;
    Text endTitle,endBody;
    Button retrySave;
    string previousTitle,previousBody,previousReplay;
    bool endingLabelsChanged;
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);

    public void Begin(Chapter3SchoolController owner,Chapter3Rescue branch)
    {
        StopAllCoroutines();if(Conversation)Conversation.Cancel();RestoreEndingLabels();
        school=owner;rescue=branch;player=owner.player;ui=owner.ui;
        PrepareEntrance(owner);
        Result=Chapter3ResultStore.Build(owner.Battle.Selected,branch.Selected,
            owner.flow.CarriedWeapon??Chapter3Weapons.Weapon.Rifle,CentralNpcCount(owner));
        if(Result==null)return;
        Saved=DeathConfirmed=RollCallCompleted=false;
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
            yield return Say(school.bawan,"人帶回來了。先讓比胡休息，等莫那過來。",3.8f);
        }
        else
        {
            ui.objective.text="達多返回林路確認比胡的情況";
            yield return Say(school.tado,"我回去找他。你們在這裡等。",3.8f);
            // The player stays at the reception, free to look along the path.
            // Tado makes the entire return journey in this same visible world.
            yield return Walk(school.tado,tadoMotor,new[]{P(8,103),P(0,98),P(-1,94),P(-1.75f,92)},3.2f);
            school.tado.Face(school.pihu.transform.position);
            yield return Say(school.tado,"比胡……我回來了。",3.2f,false,false);
            for(float t=0;t<2.8f;t+=Time.deltaTime)
            {rescue.Pose.SetDying(t/2.8f);yield return null;}
            rescue.Pose.SetDying(1);DeathConfirmed=true;
            yield return new WaitForSeconds(1.4f);
            yield return Say(school.tado,"比胡……",2.8f,false,false);
            ui.objective.text="林間接應 · 等待達多沿原路回報";
            yield return Walk(school.tado,tadoMotor,new[]{P(-1,94),P(0,98),P(8,103),P(14.2f,105)},3.2f);
            yield return Say(school.tado,"我找到比胡了。他已經死了。",4.8f);
            yield return Say(school.bawan,"……記住他。我們一起出發，卻沒能一起回來。",4.8f);
        }
        yield return CloseWithMona();
    }
    IEnumerator CloseWithMona()
    {
        CurrentStage=Stage.MonaClosing;ui.objective.text="莫那抵達接應處";ui.hint.text="空白鍵略過當句台詞";
        if(!Mona)
        {
            ui.objective.text="接應尚未完成";ui.hint.text="莫那尚未到場，結果未保存。按 F8 重進運動會。";
            yield break;
        }
        ui.objective.text="莫那正從接應棚後方走來";
        if(Mona)
        {
            // Leave the player's view of the entrance clear. Both leaders
            // move on foot, with the shelter and landscape always visible.
            yield return Walk(school.tado,tadoMotor,new[]{P(16.5f,108)},Chapter3Player.WalkSpeed);
            player.FocusOn(P(18.7f,108)+Vector3.up*1.2f);
            yield return Walk(Mona,monaMotor,new[]{P(18.7f,112),P(18.7f,108),P(17,104.5f),P(16,104)},Chapter3Player.WalkSpeed);
            CurrentStage=Stage.RollCall;ui.objective.text="莫那清點三路人數";
            yield return Say(Mona,"左側、右側和進入會場的人，各有幾個？把人數和情況告訴我。",5.4f);
            yield return Say(school.bawan,LeftReport,4.8f);
            yield return Say(school.tado,CentralReport,4.8f);
            yield return Say(school.tado,RightReport,5.4f);
            string weapon=Result.weapon==Chapter3Weapons.Weapon.Knife?"獵刀":"村田十八年式步槍";
            yield return Say(school.bawan,"他的"+weapon+"也帶回來了。",3.6f);
            RollCallCompleted=true;CurrentStage=Stage.MonaClosing;
            yield return Say(Mona,Result.pihuSurvived
                ?"把同伴帶回來，就繼續照看好他。接下來，誰都不能落單。"
                :"我記住了。留下的人已經回不來，還在的人要彼此照應。",5.5f);
            yield return Say(Mona,"這場戰鬥還沒有結束。先退入山林，守住身邊的人。",5.2f);
        }
        // Only the completed outcome and Mona's report commit a result.
        CommitResult();
        CurrentStage=Stage.Complete;player.canMove=player.canLook=false;
        ShowEnding();ui.hint.text="";ui.Say("",null);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    static int CentralNpcCount(Chapter3SchoolController owner)
    {
        var ambient=owner.GetComponentInChildren<Chapter3SchoolAmbient>();
        return (owner.tado&&owner.tado.gameObject.activeInHierarchy?1:0)+(ambient?ambient.VisibleAssailants:0);
    }
    // These are deployment counts. The right-side report separately records
    // Pihu's outcome, so a death cannot silently change the original assignment.
    public string LeftReport=>"左側通路"+Result.LeftCount+"人，"+
        (Result.mission==Chapter3SchoolBattle.Mission.Bawan?"他和我一起打通側門，都回來了。":"由我打通側門，我已回來。");
    public string CentralReport=>"進入會場的有"+Result.CentralCount+"人，"+
        (Result.mission==Chapter3SchoolBattle.Mission.Tado?"包括他、我和一起進入會場的同伴。":"是我和一起進入會場的同伴。");
    public string RightReport=>"右側通路原有"+Result.RightCount+"人，"+
        (Result.pihuSurvived
            ?(Result.mission==Chapter3SchoolBattle.Mission.Pihu?"他和比胡都回來了，比胡受傷，由他扶回接應處。":"比胡受傷，由他扶回接應處，已經回來了。")
            :(Result.mission==Chapter3SchoolBattle.Mission.Pihu?"他回來了，比胡留在林路。我回去確認，比胡已經死了。":"比胡留在林路。我回去確認，他已經死了，右側沒有人回來。"));

    void BorrowMona()
    {
        Mona=school.flow.station.mona;if(!Mona)return;
        monaParent=Mona.transform.parent;monaLocalPosition=Mona.transform.localPosition;
        monaLocalRotation=Mona.transform.localRotation;monaLocalScale=Mona.transform.localScale;
        monaFacing=Mona.facing;monaActive=Mona.gameObject.activeSelf;monaBorrowed=true;
        Mona.transform.SetParent(transform,true);Mona.transform.position=P(13,112);
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
            string missionSummary=Result.mission==Chapter3SchoolBattle.Mission.Tado?"你與達多完成中央攻入。":
                Result.mission==Chapter3SchoolBattle.Mission.Bawan?"你與巴萬完成左側突破。":"你掩護比胡完成右側穿越。";
            if(Result.version>=2)
                missionSummary=Result.mission==Chapter3SchoolBattle.Mission.Tado?"你跟達多進入會場，帶同伴一起撤出。":
                    Result.mission==Chapter3SchoolBattle.Mission.Bawan?"你和巴萬打通側門，照應同伴通過。":"你照應比胡穿過右側，一起撤出。";
            endBody.text=(Result.pihuSurvived?"比胡獲救生還。":"達多確認比胡已死亡。")+
                "\n分隊人數：左側 "+Result.LeftCount+" 人、右側 "+Result.RightCount+" 人、會場 "+Result.CentralCount+" 人。"+
                "\n"+missionSummary+
                "\n"+(Saved?"結果已保存。戰鬥還沒有結束……":PersistResult?"結果尚未保存，請按「重試保存」。":"戰鬥還沒有結束……");
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
        StopAllCoroutines();if(Conversation)Conversation.Cancel();ReturnMona();RestoreEndingLabels();
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
