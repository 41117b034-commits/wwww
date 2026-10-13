using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// The two opening exchanges at the outpost, before the escort and weapon choice.
public sealed class Chapter3OpeningConversation : MonoBehaviour
{
    public bool Active {get;private set;}
    public bool WaitingForReply {get;private set;}
    public int CompletedTurns {get;private set;}
    public int GeneratedReplies {get;private set;}
    public int FallbackReplies {get;private set;}
    public float replyTimeout=15;
    public float LastReplySeconds {get;private set;}
    public string LastReplyError {get;private set;}
    Chapter3StationController station;
    Chapter3Presentation ui;
    Chapter2LocalDialogue dialogue;
    Action complete;
    readonly List<Chapter2LocalDialogue.Message> history=new List<Chapter2LocalDialogue.Message>();
    Keyboard keyboard;
    int version,compositionFrame=-10,speechVersion;
    bool composing,bound;
    readonly Chapter3DialogueFrameBudget frameBudget=new Chapter3DialogueFrameBudget();
    public void PrepareService()
    {
        if(!dialogue)dialogue=gameObject.AddComponent<Chapter2LocalDialogue>();
        dialogue.StartCoroutine(dialogue.Prepare());
    }
    public void Begin(Chapter3StationController owner,Action done)
    {
        Cancel();station=owner;ui=owner.ui;complete=done;
        if(!dialogue)dialogue=gameObject.AddComponent<Chapter2LocalDialogue>();
        if(!bound){ui.send.onClick.AddListener(Send);ui.input.onSubmit.AddListener(SubmitFromInput);bound=true;}
        CompletedTurns=GeneratedReplies=FallbackReplies=0;history.Clear();Active=true;
        ui.Say("",null);ui.chat.SetActive(true);ui.depart.gameObject.SetActive(false);
        ui.input.gameObject.SetActive(true);ui.send.gameObject.SetActive(true);
        ui.input.interactable=ui.send.interactable=true;ui.input.text="";ui.progress.gameObject.SetActive(false);
        const string greeting="我們到駐在所了。莫那在前面；見他之前，你有什麼想說的？";
        history.Add(new Chapter2LocalDialogue.Message("assistant",greeting));
        Chapter3ConversationHistory.Get(ui).Begin(station.bawan.DisplayName,greeting);ui.status.text="輸入你的回答，按 Enter 或「送出」。";
        Speak(greeting);StartCoroutine(Refocus());
    }
    void Send(){if(Active)SubmitReply(ui.input.text);}
    void Update()
    {
        if(!Active)return;
        if(keyboard!=Keyboard.current)
        {if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
        if(composing||!string.IsNullOrEmpty(Input.compositionString))compositionFrame=Time.frameCount;
    }
    void Composition(IMECompositionString text){composing=text.Count>0;compositionFrame=Time.frameCount;}
    void SubmitFromInput(string text)
    {
        if(!Active)return;
        if(composing||!string.IsNullOrEmpty(Input.compositionString)||Time.frameCount<=compositionFrame+1||!SubmitReply(text))StartCoroutine(Refocus());
    }
    IEnumerator Refocus(){yield return null;if(Active&&!WaitingForReply&&CompletedTurns<2)ui.input.ActivateInputField();}
    public bool SubmitReply(string text)
    {
        text=text?.Trim();
        if(!Active||!isActiveAndEnabled||WaitingForReply||CompletedTurns>=2||string.IsNullOrWhiteSpace(text))return false;
        if(text.Length>160)text=text.Substring(0,160);
        WaitingForReply=true;ui.input.interactable=ui.send.interactable=false;ui.input.text="";
        Chapter3ConversationHistory.Get(ui).Append("你",text);
        frameBudget.Begin(station.player.IsVR);
        ui.status.text="巴萬正在回應……";StartCoroutine(Respond(text));return true;
    }
    IEnumerator Respond(string text)
    {
        yield return null;
        float started=Time.realtimeSinceStartup;
        int request=++version;bool received=false;string answer=null,error=null;
        dialogue.StartCoroutine(dialogue.AskWithPersona(Persona(),history,text,(a,e)=>{if(request!=version)return;answer=a;error=e;received=true;},Mathf.CeilToInt(replyTimeout),160,.35f));
        float until=Time.realtimeSinceStartup+replyTimeout;
        while(!received&&Time.realtimeSinceStartup<until)yield return null;
        if(!received){version++;dialogue.CancelRequest();error="timeout";}
        bool fallback=!string.IsNullOrEmpty(error)||string.IsNullOrWhiteSpace(answer);
        LastReplySeconds=Time.realtimeSinceStartup-started;LastReplyError=error;
        frameBudget.End();
        if(fallback)
        {
            answer=Chapter3DialogueGrounding.OpeningReply(text);
            history.Add(new Chapter2LocalDialogue.Message("user",text));history.Add(new Chapter2LocalDialogue.Message("assistant",answer));FallbackReplies++;
        }
        else GeneratedReplies++;
        CompletedTurns++;WaitingForReply=false;
        Chapter3ConversationHistory.Get(ui).Append(station.bawan.DisplayName,answer);ui.status.text=fallback?"連線暫時無法回應，已接續預寫對話。":"聽完巴萬的話，再告訴他你的想法。";
        Speak(answer);
        if(CompletedTurns<2){ui.input.interactable=ui.send.interactable=true;StartCoroutine(Refocus());yield break;}
        ui.input.DeactivateInputField();ui.input.gameObject.SetActive(false);ui.send.gameObject.SetActive(false);
        ui.status.text=(fallback?"連線暫時無法回應，已接續預寫對話。  ":"")+"讀完後，巴萬會帶你去見莫那。";
        yield return new WaitForSecondsRealtime(Mathf.Clamp(answer.Length*.09f,4,9));
        Active=false;ui.chat.SetActive(false);station.bawan.speaking=false;
        var callback=complete;complete=null;callback?.Invoke();
    }
    string Persona()=>"你是同伴巴萬·拿威。1930年10月27日清晨，在馬赫坡駐在所，比胡·帖木在旁。先見莫那·魯道，選獵刀或村田十八年式步槍一把，再沿山路去霧社學校外找達多。隊伍已襲擊這裡的日警，玩家尚無武器。繁體中文直接回答問題，最多45字，不反問或旁白，不改角色、杜撰史實或預知傷亡。你不知道玩家姓名。";
    void Speak(string line)
    {
        int current=++speechVersion;station.bawan.Face(station.player.transform.position);station.bawan.speaking=true;
        float seconds=Mathf.Clamp(line.Length*.075f,3,9);
        var face=station.bawan.GetComponent<Chapter2SpeechFace>();if(face)face.BeginLine(line,seconds,null);
        StartCoroutine(StopSpeaking(current,seconds));
    }
    IEnumerator StopSpeaking(int current,float seconds){yield return new WaitForSeconds(seconds);if(current==speechVersion&&station)station.bawan.speaking=false;}
    public void Cancel()
    {
        version++;speechVersion++;StopAllCoroutines();if(dialogue)dialogue.CancelRequest();
        frameBudget.End();
        Active=WaitingForReply=false;complete=null;composing=false;compositionFrame=-10;
        if(station&&station.bawan)station.bawan.speaking=false;
        if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;
    }
    void OnDisable(){Cancel();}
    void OnDestroy(){if(bound&&ui){ui.send.onClick.RemoveListener(Send);ui.input.onSubmit.RemoveListener(SubmitFromInput);}}
}
