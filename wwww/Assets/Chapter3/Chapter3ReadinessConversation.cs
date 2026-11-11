using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// This decision is authored and immediate. No free-text interpretation or LLM request.
public sealed class Chapter3ReadinessConversation : MonoBehaviour
{
    public enum Decision { Unclear, Ready, NotReady }
    public bool Active {get;private set;}
    public bool WaitingForOpeningRelease {get;private set;}
    public Decision LastDecision {get;private set;}
    public Button ReadyButton {get;private set;}
    public Button NotReadyButton {get;private set;}
    Chapter3StationController station;
    Chapter3Presentation ui;
    Action<Decision> complete;
    GameObject panel;
    RectTransform promptRect;
    Vector2 promptMin,promptMax;
    bool promptRaised;

    public void Begin(Chapter3StationController owner,Action<Decision> done)
    {
        Cancel();station=owner;ui=owner.ui;complete=done;
        if(!panel)BuildPanel();
        Active=true;LastDecision=Decision.Unclear;WaitingForOpeningRelease=true;
        ui.input.DeactivateInputField();ui.chat.SetActive(false);
        string prompt=station.HasKnife?"手握獵刀，接下來就要與日本決戰了，你準備好了嗎？":"拿穩步槍，接下來就要與日本決戰了，你準備好了嗎？";
        ui.Say(station.bawan.DisplayName,prompt);
        promptRect=(RectTransform)ui.subtitlePanel.transform;
        promptMin=promptRect.anchorMin;promptMax=promptRect.anchorMax;promptRaised=true;
        promptRect.anchorMin+=Vector2.up*.12f;promptRect.anchorMax+=Vector2.up*.12f;
        station.bawan.Face(station.player.transform.position);station.bawan.speaking=true;
        var face=station.bawan.GetComponent<Chapter2SpeechFace>();if(face)face.BeginLine(prompt,5,null);
        ui.hint.text="點選「好了」或「還沒」 · 也可按 1／2";
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        panel.SetActive(true);ReadyButton.interactable=NotReadyButton.interactable=false;
        StartCoroutine(ReleaseOpeningAction());
    }
    IEnumerator ReleaseOpeningAction()
    {
        yield return null;
        while(Active&&OpeningActionHeld())yield return null;
        yield return null;
        if(!Active)yield break;
        WaitingForOpeningRelease=false;ReadyButton.interactable=NotReadyButton.interactable=true;
    }
    static bool OpeningActionHeld()
    {
        var keys=Keyboard.current;
        if(keys!=null&&(keys.eKey.isPressed||keys.digit1Key.isPressed||keys.digit2Key.isPressed))return true;
        var hand=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        return hand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out bool held)&&held;
    }
    void Update()
    {
        if(!Active)return;
        if(station)station.bawan.speaking=ui.VoiceBusy;
        if(WaitingForOpeningRelease)return;
        var keys=Keyboard.current;
        if(keys?.digit1Key.wasPressedThisFrame==true)Choose(Decision.Ready);
        else if(keys?.digit2Key.wasPressedThisFrame==true)Choose(Decision.NotReady);
    }
    public bool Choose(Decision decision)
    {
        if(!Active||WaitingForOpeningRelease||(decision!=Decision.Ready&&decision!=Decision.NotReady))return false;
        LastDecision=decision;Active=false;StopAllCoroutines();panel.SetActive(false);RestorePrompt();
        station.bawan.speaking=false;ui.Say("",null);ui.hint.text="";
        var callback=complete;complete=null;callback?.Invoke(decision);return true;
    }
    void BuildPanel()
    {
        var row=Rect(ui.canvas.transform,"出發準備選擇",new Vector2(.14f,.085f),new Vector2(.86f,.195f));
        panel=row.gameObject;
        ReadyButton=Choice(row,"好了",new Vector2(0,0),new Vector2(.47f,1),Decision.Ready);
        NotReadyButton=Choice(row,"還沒",new Vector2(.53f,0),Vector2.one,Decision.NotReady);
    }
    Button Choice(Transform parent,string title,Vector2 min,Vector2 max,Decision decision)
    {
        var r=Rect(parent,title,min,max);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.15f,.22f,.18f,.98f);
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=colors.selectedColor=new Color(1,.88f,.62f);button.colors=colors;
        var label=Rect(r,"文字",Vector2.zero,Vector2.one).gameObject.AddComponent<Text>();
        label.font=ui.subtitle.font;label.fontSize=31;label.text=title;label.color=Color.white;
        label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        button.onClick.AddListener(()=>Choose(decision));return button;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    public void Cancel()
    {
        Active=WaitingForOpeningRelease=false;StopAllCoroutines();complete=null;
        if(panel)panel.SetActive(false);if(station)station.bawan.speaking=false;RestorePrompt();
    }
    void RestorePrompt()
    {if(promptRaised&&promptRect){promptRect.anchorMin=promptMin;promptRect.anchorMax=promptMax;}promptRaised=false;}
    void OnDisable(){Cancel();}
    void OnDestroy(){Cancel();if(panel)Destroy(panel);}
}
