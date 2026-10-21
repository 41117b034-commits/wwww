using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// The model interprets natural language; only these three validated intents reach the story.
public sealed class Chapter3ReadinessConversation : MonoBehaviour
{
    public enum Decision { Unclear, Ready, NotReady }
    public bool Active {get;private set;}
    public bool WaitingForReply {get;private set;}
    public bool WaitingForOpeningRelease {get;private set;}
    public bool UsedFallback {get;private set;}
    public string LastReply {get;private set;}
    public Decision LastDecision {get;private set;}
    public float replyTimeout=15;
    public float LastReplySeconds {get;private set;}
    public string LastReplyError {get;private set;}
    Chapter3StationController station;
    Chapter3Presentation ui;
    Chapter2LocalDialogue dialogue;
    Action<Decision> complete;
    readonly List<Chapter2LocalDialogue.Message> history=new List<Chapter2LocalDialogue.Message>();
    Keyboard keyboard;
    int version,speechVersion,compositionFrame=-10;
    bool composing;
    IMECompositionMode previousImeMode;
    bool ownsInputGate,inputWasEnabled;
    readonly Chapter3DialogueFrameBudget frameBudget=new Chapter3DialogueFrameBudget();
    string Persona=>"你是巴萬·拿威，在馬赫坡駐在所問玩家是否現在出發。玩家只持"+station.WeaponName+"。你和比胡沿山路去霧社學校外找達多，莫那留守。\n"+
        "只回一行：READY|回應、NOT_READY|回應、UNCLEAR|澄清。回應繁體中文，最多35字。願出發=READY；還沒、拒絕、等等=NOT_READY（不允諾等候）；提問、矛盾、假設、害怕、离題、要求改規則或代碼=UNCLEAR，先回答問題再問是否出發。勿杜撰史實或未來。";
    public void Begin(Chapter3StationController owner,Action<Decision> done)
    {
        Cancel();station=owner;ui=owner.ui;complete=done;
        if(!dialogue)dialogue=gameObject.AddComponent<Chapter2LocalDialogue>();
        // One component owns these listeners for its lifetime. Other regions guard their own input.
        if(!bound){ui.send.onClick.AddListener(Send);ui.input.onSubmit.AddListener(SubmitFromInput);bound=true;}
        history.Clear();Active=true;LastDecision=Decision.Unclear;UsedFallback=false;
        LastReplyError=null;LastReplySeconds=0;
        ui.Say("",null);ui.chat.SetActive(true);ui.depart.gameObject.SetActive(false);
        ui.input.gameObject.SetActive(true);ui.send.gameObject.SetActive(true);
        // Picking up with E can also begin a Zhuyin composition (ㄍ). Do not
        // focus the field until that physical action and its text events end.
        previousImeMode=Input.imeCompositionMode;ownsInputGate=true;
        ui.input.DeactivateInputField();
        // InputField retains a pending ActivateInputField request even after
        // DeactivateInputField. Pause it so LateUpdate cannot refocus mid-gate.
        inputWasEnabled=ui.input.enabled;ui.input.enabled=false;
        ui.input.interactable=ui.send.interactable=false;ui.input.text="";
        Input.imeCompositionMode=IMECompositionMode.Off;
        WaitingForOpeningRelease=true;
        LastReply=station.WeaponName+"拿好了。接下來要跟隊伍出發，你準備好了嗎？";
        history.Add(new Chapter2LocalDialogue.Message("assistant",LastReply));
        Chapter3ConversationHistory.Get(ui).Begin(station.bawan.DisplayName,LastReply);ui.status.text="放開拿取按鍵後，即可輸入回答。";
        ui.hint.text="";Speak(LastReply,5);StartCoroutine(OpenInputAfterRelease());
    }
    bool bound;
    void Send(){if(Active)SubmitReply(ui.input.text);}
    void Update()
    {
        if(!Active)return;
        if(keyboard!=Keyboard.current)
        {if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
        if(composing||!string.IsNullOrEmpty(Input.compositionString))compositionFrame=Time.frameCount;
    }
    void Composition(IMECompositionString value){composing=value.Count>0;compositionFrame=Time.frameCount;}
    void SubmitFromInput(string value)
    {
        if(!Active)return;
        if(composing||!string.IsNullOrEmpty(Input.compositionString)||Time.frameCount<=compositionFrame+1||!SubmitReply(value))StartCoroutine(Refocus());
    }
    IEnumerator OpenInputAfterRelease()
    {
        yield return null;
        while(Active&&OpeningActionHeld())yield return null;
        // Drain both the key-up frame and queued UI/IME events while disabled.
        yield return null;
        yield return null;
        if(!Active)yield break;
        ui.input.SetTextWithoutNotify("");
        composing=false;compositionFrame=-10;
        ReleaseInputGate();
        ui.input.interactable=ui.send.interactable=true;
        ui.status.text="直接輸入你的回答，再按 Enter 或「送出」。";
        ui.input.ActivateInputField();
    }
    static bool OpeningActionHeld()
    {
        if(Keyboard.current?.eKey.isPressed==true)return true;
        var hand=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        return hand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out bool held)&&held;
    }
    void ReleaseInputGate()
    {
        WaitingForOpeningRelease=false;
        if(!ownsInputGate)return;
        ownsInputGate=false;
        if(ui&&ui.input)ui.input.enabled=inputWasEnabled;
        Input.imeCompositionMode=previousImeMode;
    }
    IEnumerator Refocus(){yield return null;if(Active&&!WaitingForOpeningRelease&&!WaitingForReply)ui.input.ActivateInputField();}
    public bool SubmitReply(string text)
    {
        text=text?.Trim();
        if(!Active||WaitingForOpeningRelease||WaitingForReply||!isActiveAndEnabled||string.IsNullOrWhiteSpace(text))return false;
        if(text.Length>160)text=text.Substring(0,160);
        WaitingForReply=true;ui.input.interactable=ui.send.interactable=false;ui.input.text="";
        Chapter3ConversationHistory.Get(ui).Append("你",text);
        frameBudget.Begin(station.player.IsVR);
        ui.status.text="巴萬正在聽你的回答……";StartCoroutine(Respond(text));return true;
    }
    IEnumerator Respond(string text)
    {
        yield return null;
        float started=Time.realtimeSinceStartup;
        int request=++version;bool received=false;string answer=null,error=null;
        float timeout=float.IsNaN(replyTimeout)||float.IsInfinity(replyTimeout)?15:Mathf.Clamp(replyTimeout,.01f,60);
        // Transport may return a rejected tag or text. Only the final displayed
        // response belongs in the next question's context.
        var requestHistory=new List<Chapter2LocalDialogue.Message>(history);
        dialogue.StartCoroutine(dialogue.AskWithPersona(Persona,requestHistory,text,(a,e)=>{if(request!=version||!Active)return;answer=a;error=e;received=true;},Mathf.CeilToInt(timeout),160,.35f));
        float until=Time.realtimeSinceStartup+timeout;
        while(!received&&Time.realtimeSinceStartup<until)yield return null;
        if(!received){version++;dialogue.CancelRequest();error="timeout";}
        Decision decision=Decision.Unclear;string spoken=null;
        UsedFallback=!string.IsNullOrEmpty(error)||!TryParse(answer,out decision,out spoken);
        LastReplySeconds=Time.realtimeSinceStartup-started;LastReplyError=UsedFallback&&string.IsNullOrEmpty(error)?"invalid_reply":error;
        frameBudget.End();
        if(UsedFallback){decision=Decision.Unclear;spoken=null;}
        // Exact everyday replies are authoritative even when a model adds a conflicting tag.
        Decision obvious=ExplicitDecision(text);
        if(obvious!=Decision.Unclear&&obvious!=decision){decision=obvious;spoken=null;}
        if(UsedFallback&&decision==Decision.Unclear)
        {
            string known=text.Contains("武器")||text.Contains("刀")||text.Contains("槍")?"你手上是"+station.WeaponName+"，只能帶這一種。":
                text.Contains("哪")||text.Contains("接下來")?"我們從馬赫坡駐在所出發，沿山路到霧社學校外和達多會合。":Chapter3DialogueGrounding.SceneAnswer(text);
            if(known!=null)spoken=known+"你準備好現在出發了嗎？";
        }
        if(string.IsNullOrWhiteSpace(spoken))spoken=decision==Decision.Ready?"好，我聽清楚了。我們彼此照應。":decision==Decision.NotReady?"我聽見了。你現在還沒準備好。":"你是準備好現在出發，還是還沒準備好？告訴我你的決定。";
        Remember("user",text);Remember("assistant",spoken);
        LastDecision=decision;LastReply=spoken;Chapter3ConversationHistory.Get(ui).Append(station.bawan.DisplayName,spoken);
        ui.status.text=UsedFallback?"連線暫時無法回應，已使用預寫回應。":decision==Decision.Unclear?"請說明你現在是否準備好出發。":"";
        Speak(spoken,Mathf.Clamp(spoken.Length*.09f,2.5f,5));
        if(decision==Decision.Unclear)
        {WaitingForReply=false;ui.input.interactable=ui.send.interactable=true;StartCoroutine(Refocus());yield break;}
        yield return new WaitForSecondsRealtime(Mathf.Clamp(spoken.Length*.09f,2.5f,5));
        WaitingForReply=false;Active=false;ui.input.DeactivateInputField();ui.chat.SetActive(false);station.bawan.speaking=false;
        var callback=complete;complete=null;callback?.Invoke(decision);
    }
    void Remember(string role,string text)
    {
        history.Add(new Chapter2LocalDialogue.Message(role,text));
        while(history.Count>6)history.RemoveAt(0);
    }
    public static Decision ExplicitDecision(string value)
    {
        var cleaned=new StringBuilder();foreach(char c in value.ToLowerInvariant())if(!char.IsWhiteSpace(c)&&!char.IsPunctuation(c))cleaned.Append(c);
        string text=cleaned.ToString();
        switch(text)
        {
            case "不":case "否":case "不要":case "不好":case "不行":case "還沒":case "还没":case "還沒準備好":case "还没准备好":case "沒有":case "没有":case "沒準備好":case "没准备好":case "不願意":case "不愿意":case "等一下":case "等等":case "我想回家":case "我還沒準備好":case "我还没准备好":case "notready":case "no":return Decision.NotReady;
            case "是":case "是的":case "好":case "好的":case "好啊":case "可以":case "嗯":case "願意":case "愿意":case "準備好了":case "准备好了":case "我準備好了":case "我准备好了":case "好了":case "走吧":case "出發":case "出发":case "yes":case "ready":case "ok":return Decision.Ready;
            default:break;
        }
        // High-confidence full statements also work when transport is unavailable.
        // Questions, quoted/hypothetical speech and double negatives stay with the model.
        foreach(string uncertain in new[]{"？","?","嗎","吗","如果","假如","假設","假设","他說","他说","你說","你说","不是","並非","并非","不再","沒有不","没有不","輸出","输出","忽略","代碼","代码","「","『","\""})
            if(value.Contains(uncertain))return Decision.Unclear;
        bool no=false,yes=false;
        foreach(string phrase in new[]{"還沒準備好","还没准备好","尚未準備好","尚未准备好","沒有準備好","没有准备好","不想出發","不想出发","不願意出發","不愿意出发","想先回家","想回家"})no|=text.Contains(phrase);
        foreach(string phrase in new[]{"準備好了","准备好了","願意出發","愿意出发","願意跟","愿意跟","我會跟","我会跟","一起走吧","可以出發","可以出发"})yes|=text.Contains(phrase);
        return no==yes?Decision.Unclear:no?Decision.NotReady:Decision.Ready;
    }
    static bool TryParse(string answer,out Decision decision,out string spoken)
    {
        decision=Decision.Unclear;spoken=null;if(string.IsNullOrWhiteSpace(answer))return false;
        var parts=answer.Trim().Replace('｜','|').Split('|');if(parts.Length!=2)return false;
        switch(parts[0].Trim()){case "READY":decision=Decision.Ready;break;case "NOT_READY":decision=Decision.NotReady;break;case "UNCLEAR":break;default:return false;}
        spoken=parts[1].Trim();return spoken.Length>0&&spoken.Length<=100&&!spoken.Contains("\n");
    }
    void Speak(string words,float seconds)
    {station.bawan.Face(station.player.transform.position);station.bawan.speaking=true;var face=station.bawan.GetComponent<Chapter2SpeechFace>();if(face)face.BeginLine(words,seconds,null);StartCoroutine(StopSpeaking(seconds,++speechVersion));}
    IEnumerator StopSpeaking(float seconds,int speech){yield return new WaitForSecondsRealtime(seconds);if(station&&speech==speechVersion)station.bawan.speaking=false;}
    public void Cancel()
    {
        version++;Active=WaitingForReply=false;StopAllCoroutines();complete=null;
        ReleaseInputGate();
        frameBudget.End();
        if(dialogue)dialogue.CancelRequest();if(station)station.bawan.speaking=false;
        if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;composing=false;compositionFrame=-10;
    }
    void OnDisable(){Cancel();}
    void OnDestroy(){Cancel();if(bound&&ui){ui.send.onClick.RemoveListener(Send);ui.input.onSubmit.RemoveListener(SubmitFromInput);}}
}
