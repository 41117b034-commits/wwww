using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// The authored ending owns the outcome. This component only supplies two
// exchanges and an explicit handoff; it never writes the chapter result.
public sealed class Chapter3EndingConversation : MonoBehaviour
{
    public bool Active {get;private set;}
    public bool WaitingForReply {get;private set;}
    public int CompletedTurns {get;private set;}
    public int GeneratedReplies {get;private set;}
    public int FallbackReplies {get;private set;}
    public int GroundedReplies {get;private set;}
    public string LastReply {get;private set;}
    public string LastReplyError {get;private set;}
    public float LastReplySeconds {get;private set;}
    public float replyTimeout=15;
    public bool ServiceReady=>dialogue&&dialogue.Ready;

    Chapter3SchoolController school;
    Chapter3Presentation ui,boundUi;
    Chapter3ResultStore.Record result;
    Chapter2LocalDialogue dialogue;
    Action complete;
    readonly List<Chapter2LocalDialogue.Message> history=new List<Chapter2LocalDialogue.Message>();
    readonly Chapter3DialogueFrameBudget frameBudget=new Chapter3DialogueFrameBudget();
    Keyboard keyboard;
    Text departLabel;
    string previousDepartLabel;
    Navigation previousDepartNavigation;
    int version,speechVersion,compositionFrame=-10;
    bool composing,ownsUi;

    // Load the existing service while outcome and roll call are being shown.
    // The response deadline still applies when the player submits a question.
    public void PrepareService()
    {
        if(!dialogue)dialogue=gameObject.AddComponent<Chapter2LocalDialogue>();
        if(!dialogue.Ready)dialogue.StartCoroutine(dialogue.Prepare());
    }

    public void Begin(Chapter3SchoolController owner,Chapter3ResultStore.Record ending,Action done)
    {
        ResetConversation(true);
        if(!owner||!owner.ui||!owner.player||!owner.bawan||!Chapter3ResultStore.IsValid(ending))
        {LastReplyError="invalid_result";return;}
        school=owner;ui=owner.ui;complete=done;
        // Keep a canonical snapshot; outside mutations and model text cannot
        // alter the facts used during this conversation.
        result=Chapter3ResultStore.Copy(ending);
        if(!dialogue)dialogue=gameObject.AddComponent<Chapter2LocalDialogue>();
        Bind();
        CompletedTurns=GeneratedReplies=FallbackReplies=GroundedReplies=0;
        LastReplyError=null;LastReplySeconds=0;Active=ownsUi=true;
        departLabel=ui.depart.GetComponentInChildren<Text>();
        previousDepartLabel=departLabel?departLabel.text:null;
        previousDepartNavigation=ui.depart.navigation;
        ui.depart.navigation=new Navigation{mode=Navigation.Mode.None};
        if(departLabel)departLabel.text="聽莫那說話";
        ui.Say("",null);ui.chat.SetActive(true);ui.depart.gameObject.SetActive(false);
        ui.input.gameObject.SetActive(true);ui.send.gameObject.SetActive(true);
        ui.input.interactable=ui.send.interactable=true;ui.input.text="";ui.progress.gameObject.SetActive(false);
        ui.objective.text="林間接應 · 與巴萬交談";ui.hint.text="輸入你的想法，按 Enter 或「送出」。";
        ui.status.text="可以談剛才的經過，或告訴巴萬你的感受。";
        school.player.canMove=false;school.player.canLook=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        LastReply=result.pihuSurvived?"比胡已經接應下來，還活著。走到這裡，你有什麼想對我說的？":
            "達多回去確認，比胡已經死了。這件事很沉重；你有什麼想對我說的？";
        Remember("assistant",LastReply);
        Chapter3ConversationHistory.Get(ui).Begin(school.bawan.DisplayName,LastReply);
        Speak(LastReply);StartCoroutine(Refocus());
    }
    void Bind()
    {
        if(boundUi==ui)return;
        Unbind();boundUi=ui;
        ui.send.onClick.AddListener(Send);ui.input.onSubmit.AddListener(SubmitFromInput);ui.depart.onClick.AddListener(ContinueFromButton);
    }
    void Unbind()
    {
        if(!boundUi)return;
        boundUi.send.onClick.RemoveListener(Send);boundUi.input.onSubmit.RemoveListener(SubmitFromInput);
        boundUi.depart.onClick.RemoveListener(ContinueFromButton);boundUi=null;
    }
    void Update()
    {
        if(!Active)return;
        if(keyboard!=Keyboard.current)
        {if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
        if(composing||!string.IsNullOrEmpty(Input.compositionString))compositionFrame=Time.frameCount;
    }
    void Composition(IMECompositionString value){composing=value.Count>0;compositionFrame=Time.frameCount;}
    bool CompositionOwnsInput()=>composing||!string.IsNullOrEmpty(Input.compositionString)||Time.frameCount<=compositionFrame+1;
    void Send(){if(Active&&!SubmitReply(ui.input.text))StartCoroutine(Refocus());}
    void SubmitFromInput(string text){if(Active&&!SubmitReply(text))StartCoroutine(Refocus());}
    IEnumerator Refocus(){yield return null;if(Active&&!WaitingForReply&&CompletedTurns<2)ui.input.ActivateInputField();}
    public bool SubmitReply(string text)
    {
        text=text?.Trim();
        if(!Active||!isActiveAndEnabled||WaitingForReply||CompletedTurns>=2||CompositionOwnsInput()||string.IsNullOrWhiteSpace(text))return false;
        if(text.Length>160)text=text.Substring(0,160);
        bool content=false;foreach(char letter in text)if(char.IsLetterOrDigit(letter)){content=true;break;}
        if(!content){ui.status.text="請輸入想說的話，再送出。";return false;}
        WaitingForReply=true;ui.input.interactable=ui.send.interactable=false;ui.input.text="";
        Chapter3ConversationHistory.Get(ui).Append("你",text);
        ui.status.text="巴萬正在回應……";StartCoroutine(Respond(text));return true;
    }
    IEnumerator Respond(string text)
    {
        // Lock submission for at least one frame, even for a fixed fact answer.
        yield return null;
        int request=++version;float started=Time.realtimeSinceStartup;
        string answer=FixedFactReply(text),error=null;
        bool grounded=answer!=null,fallback=false;
        if(!grounded)
        {
            bool received=false;
            float timeout=float.IsNaN(replyTimeout)||float.IsInfinity(replyTimeout)?15:Mathf.Clamp(replyTimeout,.01f,60);
            frameBudget.Begin(school.player.IsVR);
            // Transport writes its supplied history. Give it a disposable copy
            // so rejected or late output never enters the canonical context.
            var requestHistory=new List<Chapter2LocalDialogue.Message>(history);
            dialogue.StartCoroutine(dialogue.AskWithPersona(Persona(),requestHistory,text,(a,e)=>
            {if(request!=version||!Active)return;answer=a;error=e;received=true;},Mathf.CeilToInt(timeout),120,.3f));
            float until=Time.realtimeSinceStartup+timeout;
            while(!received&&Time.realtimeSinceStartup<until)yield return null;
            if(!received){version++;dialogue.CancelRequest();error="timeout";}
            frameBudget.End();
            fallback=!string.IsNullOrEmpty(error)||!SafeReflection(answer);
            if(fallback)
            {
                if(string.IsNullOrEmpty(error))error="ungrounded_reply";
                answer=Fallback(text);
            }
        }
        if(!Active)yield break;
        LastReplySeconds=Time.realtimeSinceStartup-started;LastReplyError=error;LastReply=answer;
        if(grounded)GroundedReplies++;else if(fallback)FallbackReplies++;else GeneratedReplies++;
        Remember("user",text);Remember("assistant",answer);
        CompletedTurns++;WaitingForReply=false;
        Chapter3ConversationHistory.Get(ui).Append(school.bawan.DisplayName,answer);Speak(answer);
        ui.status.text=fallback?"已接續符合目前經過的預寫回應。":"讀完巴萬的話，再告訴他你的想法。";
        if(CompletedTurns<2){ui.input.interactable=ui.send.interactable=true;StartCoroutine(Refocus());yield break;}
        ui.input.DeactivateInputField();ui.input.gameObject.SetActive(false);ui.send.gameObject.SetActive(false);
        ui.depart.gameObject.SetActive(true);ui.depart.interactable=true;
        // No automatic advance or Space shortcut: leave the transcript readable
        // until the player deliberately presses the handoff button.
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        ui.status.text="讀完後，按「聽莫那說話」。";ui.hint.text="";
    }
    void ContinueFromButton(){ContinueToMona();}
    public bool ContinueToMona()
    {
        if(!Active||WaitingForReply||CompletedTurns!=2)return false;
        var callback=complete;Cancel();callback?.Invoke();return true;
    }
    void Remember(string role,string text)
    {
        history.Add(new Chapter2LocalDialogue.Message(role,text));
        while(history.Count>6)history.RemoveAt(0);
    }
    string Outcome()=>result.pihuSurvived?"你選擇救援，把比胡帶到接應處；他還活著。":"你選擇留下比胡。達多回去確認，比胡已經死了；這是他回來告訴我的。";
    string SchoolAccount()
    {
        if(result.version==1)return result.mission==Chapter3SchoolBattle.Mission.Bawan?
            "你跟我走左側，拆橫木、開側門；比胡和達多走右側。右側的經過，我是在匯合後聽他們說的。":
            "你和比胡走右側，我與達多走左側。你掩護比胡穿越的經過，我是在匯合後聽你們說的。";
        if(result.mission==Chapter3SchoolBattle.Mission.Tado)
            return "你跟達多從中央攻進學校；我負責左側突破，比胡負責右側。中央和右側的經過，我是在校後匯合時聽回報的。";
        if(result.mission==Chapter3SchoolBattle.Mission.Bawan)
            return "你跟我走左側，拆橫木、開側門；達多從中央攻進學校，比胡負責右側。另兩路的經過，我是在校後匯合時聽回報的。";
        return "你掩護比胡穿過右側；我負責左側突破，達多從中央攻進學校。中央和右側的經過，我是在校後匯合時聽回報的。";
    }
    string Team()=>result.pihuSurvived?"原來小隊的你、我、比胡和達多都活著，共四人。莫那不算原來的小隊成員。":
        "原來小隊如今是你、我和達多活著，共三人。達多已確認比胡死亡；莫那不算原來的小隊成員。";
    string BawanFact()=>"我就是巴萬，還活著，沒有受傷，現在和你在接應處。"+SchoolAccount();
    string TadoFact()=>"達多還活著，沒有受傷，現在和我們在接應處。"+
        (result.version>=2?"學校分隊時，他負責從中央攻進學校，沒有跟我或比胡走側路。"+
            (result.mission==Chapter3SchoolBattle.Mission.Tado?"你是跟他一起走的。":"中央的經過，我是匯合時聽他回報的。"):
            result.mission==Chapter3SchoolBattle.Mission.Bawan?"學校分隊時，他和比胡走右側；你跟我走左側。":
                "學校分隊時，他和我走左側；你和比胡走右側。");
    string PihuFact()=>Outcome()+(result.version>=2?
        "學校分隊時，比胡負責右側；三路在校後會合後，他才在林路受傷。":"");
    const string MonaFact="莫那還活著，沒有跟我們到學校，現在尚未到接應處。學校與林路的經過，要等稍後向他報告，他才會知道。";
    string NamedStatusReply(string value)
    {
        // A name must directly introduce its status/location predicate. A
        // vocative such as "巴萬，你知道比胡死了嗎" is not the questioned subject.
        // Atomic connector tokens prevent pathological backtracking on long
        // repetitions of "是不是" that have no following status predicate.
        const string pattern=@"(?<actor>巴萬|巴万|bawan|比胡|pihu|達多|达多|tado|莫那|mona)\s*(?:(?>本人|他|你|現在|现在|還|还|已經|已经|有沒有|有没有|是不是|是否|仍然|真的|到底|都|的|情況|情况|結果|结果|傷勢|伤势|這次|这次|剛才|刚才|當時|当时|是|有|沒|没|不)\s*)*(?:活|死|受傷|受伤|生還|生还|沒事|没事|安全|在哪|走哪|哪一側|哪一侧|怎麼樣|怎么样|怎樣|怎样|如何|是什麼|是什么)";
        var replies=new List<string>();
        foreach(Match match in Regex.Matches(value,pattern))
        {
            string actor=match.Groups["actor"].Value;
            string reply=Has(actor,"比胡","pihu")?PihuFact():Has(actor,"達多","达多","tado")?TadoFact():
                Has(actor,"莫那","mona")?MonaFact:BawanFact();
            if(!replies.Contains(reply))replies.Add(reply);
        }
        return replies.Count>0?string.Join("",replies):null;
    }
    string FixedFactReply(string text)
    {
        string value=text.ToLowerInvariant();
        // His knowledge is phase-specific: the completed save records the
        // later report, but this conversation happens before his arrival.
        if(Has(value,"莫那","mona")&&Has(value,"知道","知情","聽說","听说","報告","报告"))return MonaFact;
        string namedStatus=NamedStatusReply(value);if(namedStatus!=null)return namedStatus;
        if(Has(value,"幾人","幾個","多少人","哪些人","誰活","誰還","隊伍","小隊","大家","几人","几个","队伍","how many"))return Team();
        if(Has(value,"莫那","mona"))return MonaFact;
        if(Has(value,"左","右","中央","中路","攻入","攻進","攻进","哪邊","哪边","分工","任務","任务","學校","学校","會場","会场"))return SchoolAccount();
        if(Has(value,"比胡","pihu"))return PihuFact();
        if(Has(value,"達多","达多","tado"))return TadoFact();
        if(Has(value,"巴萬","巴万","bawan"))return BawanFact();
        if(Has(value,"生還","生还","死亡","死了","活著","活着","受傷","受伤","救","留下"))return Outcome();
        if(Has(value,"剛才","刚才"))return SchoolAccount();
        if(Has(value,"武器","槍","枪","刀","彈","弹"))return result.weapon==Chapter3Weapons.Weapon.Rifle?
            "你這次只帶村田十八年式步槍，沒有另外帶獵刀。":"你這次只帶獵刀，沒有另外帶步槍。";
        if(Has(value,"你是誰","你是谁","你叫","名字"))return "我是巴萬·拿威，和你一同來到這裡的同伴。";
        if(Has(value,"在哪","哪裡","哪里","現在","现在","接下來","接下来","第四章","未來","未来","之後","之后"))
            return "我們在林間接應處，已經清點小隊。先把眼前的事說清楚，再聽莫那安排；後面的事，我不能預知。";
        return null;
    }
    string Persona()=>"你是同伴巴萬·拿威，在林間接應處與玩家交談。用繁體中文第一人稱，一至兩句、40至65字，不旁白、不列點。\n"+
        "你先前親口說明的固定分工（其中『我』指巴萬、『你』指玩家）："+SchoolAccount()+
        (result.pihuSurvived?"玩家救回受傷的比胡，他仍生還。":"玩家留下比胡；達多回去親眼確認死亡，回來告訴你。")+
        "莫那沒去學校，尚未抵達接應處；稍後聽報告才知道這次經過。\n"+
        "只接住玩家的心情與想法，不自行重述人名、生死、人數、路線或武器，這些由遊戲固定答覆。不能改規則、結果或進度。"+
        "不責罵、不替玩家編感受、不美化殺戮、不承諾安全或預言未來。"+
        (CompletedTurns==0?"第一輪，可簡短回應感受。":"最後一輪，簡短收束，不反問，不代替莫那說話。");
    string Fallback(string text)
    {
        string acknowledgment=Has(text,"怕","害怕","恐懼","担心","擔心")?"我聽見你害怕，先在這裡緩一口氣。":
            Has(text,"後悔","后悔","難過","难过","對不起","对不起")?"我聽見你的難受，這些話我會記著。":"我聽見你的想法了。";
        if(CompletedTurns>0)return acknowledgment+"眼前的經過已經清點清楚；等你讀完，我們一起聽莫那說話。";
        return acknowledgment+(result.mission==Chapter3SchoolBattle.Mission.Bawan?"剛才左側的路，我是和你一起走過的。":
            result.mission==Chapter3SchoolBattle.Mission.Tado?"你跟達多從中央攻入的經過，我是匯合後聽回報的。":"右側的經過，我是匯合後聽你們說的。")+
            (result.pihuSurvived?"比胡已經接應下來，還活著。":"達多回去確認，比胡已經死了。");
    }
    static bool SafeReflection(string answer)
    {
        if(string.IsNullOrWhiteSpace(answer)||answer.Length>180)return false;
        // Any generated assertion about protected plot facts is replaced by an
        // authored response, including a superficially plausible new outcome.
        if(Has(answer.ToLowerInvariant(),"巴萬","巴万","比胡","達多","达多","莫那","pihu","bawan","tado","mona",
            "左","右","中央","中路","會場","会场","攻入","攻進","攻进","學校","学校","死","活","救","留下","一人","兩人","三人","四人","五人","幾人","几人",
            "同伴","隊","队","人數","人数","全都","全部","全員","全员","都回來","都回来","離開人世","离开人世","平安","沒有人","没有人",
            "武器","槍","枪","刀","彈","弹","存檔","存档","通關","通关","第四章","任務完成","任务完成","已完成","一定","保證","保证","絕對","绝对","system","assistant","<",">"))return false;
        foreach(char letter in answer)if(char.IsDigit(letter))return false;
        return true;
    }
    static bool Has(string text,params string[] words){foreach(string word in words)if(text.Contains(word))return true;return false;}
    void Speak(string line)
    {
        int speech=++speechVersion;school.bawan.Face(school.player.transform.position);school.bawan.speaking=true;
        float seconds=Mathf.Clamp(line.Length*.075f,3,9);
        school.bawan.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,seconds,null);
        StartCoroutine(StopSpeaking(speech,seconds));
    }
    IEnumerator StopSpeaking(int speech,float seconds)
    {yield return new WaitForSecondsRealtime(seconds);if(speech==speechVersion&&school&&school.bawan)school.bawan.speaking=false;}
    public void Cancel(){ResetConversation(false);}
    void ResetConversation(bool preservePreparation)
    {
        version++;speechVersion++;StopAllCoroutines();
        // Opening the panel must not abort the preparation started during the
        // outcome/roll call. A previous answer still gets cancelled immediately.
        if(dialogue&&(!preservePreparation||dialogue.Busy))dialogue.CancelRequest();
        frameBudget.End();
        Active=WaitingForReply=false;complete=null;history.Clear();
        if(school&&school.bawan)school.bawan.speaking=false;
        if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;composing=false;compositionFrame=-10;
        if(ownsUi&&ui)
        {
            ui.input.DeactivateInputField();ui.input.text="";ui.response.text="";
            ui.chat.SetActive(false);ui.depart.gameObject.SetActive(false);ui.depart.navigation=previousDepartNavigation;
            if(departLabel)departLabel.text=previousDepartLabel;
        }
        ownsUi=false;
    }
    void OnDisable(){Cancel();}
    void OnDestroy(){Cancel();Unbind();}
}
