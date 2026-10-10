using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Three-minute free roam. Only the roadside ambient cast participates.
public sealed class Chapter2Exploration : MonoBehaviour
{
    public Chapter2Controller chapter;
    public float interactionRadius=3f;
    public bool Active { get; private set; }
    public bool ChatOpen => current;
    public bool WaitingForReply => client && client.Busy;
    public float RemainingSeconds => Active?Mathf.Max(0,deadline-Time.unscaledTime):0;
    public Chapter2AmbientNPC Nearest { get; private set; }
    public Chapter2AmbientNPC Current => current;
    public string LastReply { get; private set; }
    public string LastError { get; private set; }
    public InputField QuestionInput => input;
    public int ParticipantCount => participants.Length;
    Chapter2AmbientNPC[] participants=new Chapter2AmbientNPC[0];
    readonly Dictionary<Chapter2AmbientNPC,List<Chapter2LocalDialogue.Message>> histories=new Dictionary<Chapter2AmbientNPC,List<Chapter2LocalDialogue.Message>>();
    Chapter2AmbientNPC current;
    Chapter2LocalDialogue client;
    GameObject panel;
    Button prompt,send,close;
    Text promptText,title,transcript,status,timer;
    ScrollRect scroll;
    InputField input;
    float deadline,answerVisibleUntil;
    int conversationVersion;
    string pendingQuestion;

    void EnsureUI()
    {
        if(panel)return;
        client=GetComponent<Chapter2LocalDialogue>();
        if(!client)client=gameObject.AddComponent<Chapter2LocalDialogue>();
        var parent=chapter.ui.canvas.transform;
        var timerBox=Box(parent,"Exploration timer",new Vector2(.77f,.89f),new Vector2(.98f,.965f),new Color(.025f,.04f,.035f,.86f));
        timerBox.GetComponent<Image>().raycastTarget=false;
        timer=Label(timerBox,"探索時間",24,new Vector2(.04f,0),new Vector2(.96f,1),TextAnchor.MiddleCenter);
        timer.color=new Color(.93f,.83f,.6f);
        prompt=Button(parent,"Roadside interaction",new Vector2(.34f,.31f),new Vector2(.66f,.39f),out promptText);
        prompt.onClick.AddListener(()=>TryOpen(Nearest));
        panel=Box(parent,"Roadside conversation",new Vector2(.10f,.025f),new Vector2(.90f,.46f),new Color(.025f,.04f,.035f,.96f)).gameObject;
        title=Label(panel.transform,"",30,new Vector2(.035f,.87f),new Vector2(.79f,.98f),TextAnchor.MiddleLeft);
        title.color=new Color(.91f,.79f,.51f);
        close=Button(panel.transform,"Leave conversation",new Vector2(.81f,.875f),new Vector2(.97f,.98f),out var closeText);
        closeText.text="離開  Esc";closeText.fontSize=23;close.onClick.AddListener(CloseChat);
        var viewport=Box(panel.transform,"Conversation history",new Vector2(.035f,.30f),new Vector2(.965f,.85f),new Color(.01f,.02f,.016f,.55f));
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.scrollSensitivity=28;
        transcript=Label(viewport,"",27,Vector2.zero,Vector2.one,TextAnchor.UpperLeft);
        var content=transcript.rectTransform;
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
        content.offsetMin=new Vector2(14,0);content.offsetMax=new Vector2(-14,0);
        transcript.verticalOverflow=VerticalWrapMode.Overflow;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content;
        var field=Box(panel.transform,"Question input",new Vector2(.035f,.135f),new Vector2(.78f,.265f),new Color(.12f,.16f,.14f,1));
        input=field.gameObject.AddComponent<InputField>();input.targetGraphic=field.GetComponent<Image>();
        var value=Label(field,"",26,new Vector2(.025f,.04f),new Vector2(.975f,.96f),TextAnchor.MiddleLeft);
        var placeholder=Label(field,"想問什麼？輸入後按「送出」",25,new Vector2(.025f,.04f),new Vector2(.975f,.96f),TextAnchor.MiddleLeft);
        placeholder.color=new Color(.72f,.76f,.7f,.8f);
        input.textComponent=value;input.placeholder=placeholder;input.characterLimit=240;input.lineType=InputField.LineType.MultiLineNewline;
        send=Button(panel.transform,"Send question",new Vector2(.80f,.135f),new Vector2(.965f,.265f),out var sendText);
        sendText.text="送出";send.onClick.AddListener(()=>SubmitQuestion(input.text));
        status=Label(panel.transform,"",21,new Vector2(.035f,.01f),new Vector2(.965f,.12f),TextAnchor.MiddleLeft);
        panel.SetActive(false);prompt.gameObject.SetActive(false);timer.transform.parent.gameObject.SetActive(false);
        // Fade and authored story cards must stay above the temporary dialogue.
        chapter.ui.fade.transform.SetAsLastSibling();chapter.ui.endingPanel.transform.SetAsLastSibling();
    }

    public IEnumerator Explore(float seconds)
    {
        EnsureUI();
        var group=chapter.dayGroup.transform.Find("Background people · local patrols");
        participants=group?group.GetComponentsInChildren<Chapter2AmbientNPC>(false):new Chapter2AmbientNPC[0];
        var cast=new List<Chapter2AmbientNPC>(participants);
        for(int i=1;i<chapter.workers.Length;i++)
        {
            var witness=chapter.workers[i].GetComponent<Chapter2AmbientNPC>();
            if(witness && !cast.Contains(witness))cast.Add(witness);
        }
        participants=cast.ToArray();
        Active=true;deadline=Time.unscaledTime+Mathf.Max(1,seconds);answerVisibleUntil=0;
        chapter.routeGuide.GuidanceEnabled=false;
        chapter.ui.objective.text="自由探索，認識林間的人們";
        chapter.ui.hint.text="";
        chapter.player.canMove=true;chapter.player.canLook=true;
        timer.transform.parent.gameObject.SetActive(true);
        client.StartCoroutine(client.Prepare());
        while(Active && RemainingSeconds>0)yield return null;
        if(!Active)yield break;
        prompt.gameObject.SetActive(false);
        // A request already sent may finish. No new question can extend the timer.
        while(Active && (WaitingForReply || Time.unscaledTime<answerVisibleUntil))yield return null;
        if(!Active)yield break;
        CloseChat();Active=false;Nearest=null;timer.transform.parent.gameObject.SetActive(false);
        chapter.ui.hint.text="探索結束，準備跟隨族人前往巨木。";
    }
    void Update()
    {
        if(!Active || !panel)return;
        float remaining=RemainingSeconds;
        timer.text=remaining>0?$"自由探索  {Mathf.CeilToInt(remaining)/60:00}:{Mathf.CeilToInt(remaining)%60:00}":"探索結束，準備出發";
        if(current)
        {
            prompt.gameObject.SetActive(false);
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){CloseChat();return;}
            if(!current.isActiveAndEnabled || Distance(current)>interactionRadius+.05f){CloseChat();return;}
            current.GetComponent<Chapter2Actor>().speaking=!WaitingForReply && LastReply!=null && Time.unscaledTime<answerVisibleUntil;
            input.interactable=send.interactable=remaining>0 && !WaitingForReply;
            status.text=remaining<=0?"探索時間已結束，等這次回答結束後出發。":
                WaitingForReply?"對方正在回應……  你可以按 Esc 離開。":"可以繼續追問；滾動滑鼠查看先前對話。";
            return;
        }
        Nearest=null;float best=interactionRadius;
        if(remaining>0)
        {
            foreach(var npc in participants)
            {
                if(!npc || !npc.isActiveAndEnabled)continue;
                float distance=Distance(npc);
                if(distance<=best){best=distance;Nearest=npc;}
            }
        }
        prompt.gameObject.SetActive(Nearest);
        if(Nearest)
        {
            promptText.text="E  與"+DisplayName(Nearest)+"交談";
            if(Chapter2Player.Key(Chapter2Player.KeyControlName.E) || (chapter.player.IsVR && chapter.player.ActionPressed))TryOpen(Nearest);
        }
    }
    float Distance(Chapter2AmbientNPC npc)=>Vector3.Distance(new Vector3(chapter.player.transform.position.x,0,chapter.player.transform.position.z),new Vector3(npc.transform.position.x,0,npc.transform.position.z));
    public static string DisplayName(Chapter2AmbientNPC npc)
    {
        if(npc.name.Contains("小孩"))return "林間小孩";
        if(npc.name.Contains("巨木"))return npc.name;
        if(npc.GetComponent<Chapter2Actor>().police)return npc.name.EndsWith("1")?"樹旁警察":"路邊警察";
        return npc.name.EndsWith("2")?"休息的族人":"林間族人";
    }
    public bool TryOpen(Chapter2AmbientNPC npc)
    {
        if(!Active || RemainingSeconds<=0 || !npc || Distance(npc)>interactionRadius || System.Array.IndexOf(participants,npc)<0)return false;
        CloseChat();conversationVersion++;current=npc;
        if(!histories.ContainsKey(npc))histories[npc]=new List<Chapter2LocalDialogue.Message>();
        current.ConversationPartner=chapter.player.transform;
        chapter.player.canMove=false;chapter.player.canLook=false;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        chapter.player.FocusOn(npc.transform.position+Vector3.up*(npc.name.Contains("小孩")?.95f:1.45f));
        title.text=DisplayName(npc);input.text="";LastError=null;LastReply=null;pendingQuestion=null;
        chapter.ui.hint.text="";
        panel.SetActive(true);prompt.gameObject.SetActive(false);RefreshTranscript();
        input.interactable=true;input.ActivateInputField();return true;
    }
    public bool SubmitQuestion(string question)
    {
        question=question?.Trim();
        if(!Active || !current || RemainingSeconds<=0 || WaitingForReply || string.IsNullOrWhiteSpace(question))return false;
        if(question.Length>240)question=question.Substring(0,240);
        LastError=null;pendingQuestion=question;input.text="";RefreshTranscript();
        var speaker=current;int version=conversationVersion;
        client.StartCoroutine(client.Ask(speaker,histories[speaker],question,(answer,error)=>
        {
            if(!Active || current!=speaker || version!=conversationVersion)return;
            pendingQuestion=null;LastReply=answer;LastError=error;
            answerVisibleUntil=Time.unscaledTime+Mathf.Clamp((answer?.Length??30)*.1f+2,5,13);
            RefreshTranscript();
        }));
        return true;
    }
    void RefreshTranscript()
    {
        if(!current)return;
        var text=new StringBuilder();
        foreach(var message in histories[current])
        {text.Append(message.role=="user"?"你：":DisplayName(current)+"：");text.AppendLine(message.content);text.AppendLine();}
        if(pendingQuestion!=null){text.AppendLine("你："+pendingQuestion);text.AppendLine("……");}
        if(LastError!=null)text.AppendLine("（"+LastError+"）");
        if(text.Length==0)text.Append("你可以詢問眼前的環境、生活，或任何想聊的事情。");
        transcript.text=text.ToString();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;
    }
    public void CloseChat()
    {
        conversationVersion++;
        if(current){current.ConversationPartner=null;current.GetComponent<Chapter2Actor>().speaking=false;}
        current=null;pendingQuestion=null;answerVisibleUntil=0;
        if(client && client.Busy)client.CancelRequest();
        if(panel)panel.SetActive(false);
        if(Active && chapter)
        {chapter.player.canMove=true;chapter.player.canLook=true;chapter.ui.hint.text="";}
    }
    public void CancelExploration()
    {
        CloseChat();Active=false;Nearest=null;
        if(client)client.CancelRequest();
        if(prompt)prompt.gameObject.SetActive(false);if(timer)timer.transform.parent.gameObject.SetActive(false);
    }
    void OnDisable(){CancelExploration();}
    Text Label(Transform parent,string words,int size,Vector2 min,Vector2 max,TextAnchor align)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=go.GetComponent<Text>();t.font=chapter.ui.font;t.fontSize=size;t.color=Color.white;t.text=words;t.alignment=align;
        t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
    }
    RectTransform Box(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return r;
    }
    Button Button(Transform parent,string name,Vector2 min,Vector2 max,out Text label)
    {
        var r=Box(parent,name,min,max,new Color(.18f,.25f,.2f,1));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        label=Label(r,"",27,new Vector2(.03f,.04f),new Vector2(.97f,.96f),TextAnchor.MiddleCenter);return b;
    }
}
