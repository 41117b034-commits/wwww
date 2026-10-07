using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public sealed partial class Chapter2Controller : MonoBehaviour
{
    public enum Stage { Intro, Follow, TreeChoice, Chopping, Consequence, Meeting, Vote, Ending, Complete }
    [Header("Scene references")]
    public Chapter2Player player;
    public Chapter2Presentation ui;
    public Transform sacredTree, treeApproach, meetingSpawn, campfire;
    public GameObject fallenStump, axe, dayGroup, nightGroup;
    public Chapter2Actor[] workers, leaders, conservatives;
    public Chapter2Actor officer, mona;
    public Light sun, fireLight;
    public AudioSource ambience, effects, fireAudio;
    public AudioClip forestAudio, nightAudio, chopAudio, threatAudio;
    public Chapter2RouteGuide routeGuide;
    public Collider trunkSurface;
    [Header("Replace this clip with the final opening film")]
    public VideoClip openingFilm;
    [Min(1)] public float videoPrepareTimeout = 12;
    public bool stopEditorAfterEnding = true;
    public bool saveResult = true;
    [Header("Interaction")]
    [Min(.1f)] public float treeArrivalDistance = 1.5f;
    public float interactionDistance = .7f;
    public float rhythmPeriod = 2.6f;
    public float lineSeconds = 4.5f;
    public Stage CurrentStage { get; private set; }
    public int TreeDecision { get; private set; } = -1;
    public int MeetingDecision { get; private set; } = -1;
    public int ValidCuts { get; private set; }
    public int FailedCuts { get; private set; }
    public int ConsecutiveFailedCuts { get; private set; }
    public float Integrity { get; private set; } = 100;
    public bool VideoPlayed { get; private set; }
    public bool VideoFinished { get; private set; }
    public bool Completed => CurrentStage == Stage.Complete;
    public float TreeDistance => Vector3.ProjectOnPlane(player.transform.position-treeApproach.position,Vector3.up).magnitude;
    public bool ArrivedAtTree => TreeDistance < treeArrivalDistance;
    public bool AtTree => TreeDistance <= interactionDistance;
    public Vector3 WorkerDestination(int index)
    {
        Vector3[] offsets={new Vector3(-1.8f,0,-2.1f),new Vector3(2.3f,0,-2.4f),new Vector3(-2.9f,0,-1.3f)};
        var p=sacredTree.position+offsets[index%offsets.Length];p.y=workers[index].transform.position.y;return p;
    }
    public float RhythmPhase => Mathf.PingPong((Time.time-chopStarted)/rhythmPeriod,1);
    public event Action<Stage> StageChanged;
    VideoPlayer video;
    RenderTexture videoTexture;
    bool videoError, videoEnded, skip, readyToChoose, canChop;
    float chopStarted, nextCut, raisedFor;
    Coroutine flow;
    readonly string[] introLines = {
        "警察要求族人集合，宣讀新的伐木與搬運規定。",
        "木材不准拖曳，必須肩扛下山。陡坡與長途搬運，使族人的生活更加艱困。",
        "警察進一步命令族人前往聖地「西仔希克」，砍伐被視為守護者的巨木。",
        "在槍口與鞭子的威逼下，族人走入森林。壓抑的憤怒，逐漸化為反抗的決心。"
    };
    void Start()
    {
        if (!player || !ui || !sacredTree || !treeApproach || !meetingSpawn || !officer || !mona || !sun)
        { Debug.LogError("[Chapter2] Required scene references are missing.",this); enabled=false; return; }
        ui.buttonA.onClick.AddListener(SelectA);ui.buttonB.onClick.AddListener(SelectB);
        ui.continueButton.onClick.AddListener(ReturnToMenu);
        player.canMove=false;player.canLook=false;
        dayGroup.SetActive(true);nightGroup.SetActive(false);fallenStump.SetActive(false);axe.SetActive(false);
        ambience.clip=forestAudio;ambience.loop=true;ambience.Play();
        if(routeGuide){routeGuide.chapter=this;player.routeGuide=routeGuide;}
        flow=StartCoroutine(Run());
    }
    void SetStage(Stage value)
    {
        CurrentStage=value; StageChanged?.Invoke(value);Debug.Log("[Chapter2] Stage="+value);
    }
    void Update()
    {
        if (!player || !ui) return;
        ui.ConfigureVR(player.IsVR);
        if(CurrentStage==Stage.Intro && player.ActionPressed) skip=true;
        if(CurrentStage==Stage.TreeChoice || CurrentStage==Stage.Vote)
        {
            if(player.PrimaryPressed) SelectA();
            else if(player.SecondaryPressed) SelectB();
        }
        if(CurrentStage==Stage.Vote)
        {
            raisedFor=player.HandRaised?raisedFor+Time.deltaTime:0;
            if(raisedFor>0.85f) SelectA();
        }
        if(CurrentStage==Stage.Chopping)
        {
            ui.SetMeter(RhythmPhase,ValidCuts);
            if(player.ActionPressed) TryChop();
        }
        if(CurrentStage==Stage.Complete && player.ActionPressed) ReturnToMenu();
    }
    public void SelectA() { Choose(0); }
    public void SelectB() { Choose(1); }
    public bool TryGetChopContact(out RaycastHit contact)
    {
        contact=default;
        Vector3 origin=player.view.transform.position;origin.y=sacredTree.position.y+1.15f;
        Vector3 inward=sacredTree.position+Vector3.up*1.15f-origin;
        return trunkSurface&&trunkSurface.Raycast(new Ray(origin,inward.normalized),out contact,2.9f);
    }
    public void Choose(int choice)
    {
        if(choice<0 || choice>1 || !readyToChoose) return;
        if(CurrentStage==Stage.TreeChoice && routeGuide.EscortArrived)
        { TreeDecision=choice;readyToChoose=false;ui.HideChoices(); }
        else if(CurrentStage==Stage.Vote)
        { MeetingDecision=choice;readyToChoose=false;ui.HideChoices(); }
    }
    public bool TryChop()
    {
        if(CurrentStage!=Stage.Chopping || !canChop || Time.time<nextCut) return false;
        var tool=axe.GetComponent<Chapter2Axe>();
        if(!tool||tool.IsSwinging)return false;
        if(!AtTree)
        { ui.hint.text="靠近巨木前的斧痕，再進行砍伐。";return false; }
        Vector3 direction=sacredTree.position+Vector3.up*1.2f-player.view.transform.position;
        if(Vector3.Angle(player.view.transform.forward,direction)>40)
        { ui.hint.text="面向巨木的樹幹，再按 E／右手扳機。";return false; }
        ui.hint.text="游標進入綠色區域時，按 E／空白鍵／右手扳機。避免傷及木材。";
        if(!TryGetChopContact(out RaycastHit contact))
        {ui.hint.text="再靠近樹幹正面的黃色位置，讓斧刃能碰到木頭。";return false;}
        nextCut=Time.time+.85f;
        float phase=RhythmPhase;
        StartCoroutine(SwingAxe(tool,contact,phase>=.32f&&phase<=.68f));
        return true;
    }
    IEnumerator Run()
    {
        SetStage(Stage.Intro);ui.objective.text="新規定";ui.hint.text="E／空白鍵／右手扳機  略過片頭";
        yield return Intro();
        ui.videoImage.gameObject.SetActive(false); ui.fade.color=Color.black;
        player.Warp(new Vector3(0,.08f,-17),sacredTree.position);
        player.canMove=false;player.canLook=false;
        SetStage(Stage.Follow);ui.objective.text="跟隨族人，前往巨木";
        ui.hint.text="WASD 移動・按住滑鼠右鍵環顧  |  VR 左搖桿移動、右搖桿轉向";
        yield return Fade(0,1.2f);
        yield return IntroduceForest();
        Vector3[] starts=new Vector3[workers.Length];
        for(int i=0;i<workers.Length;i++) starts[i]=workers[i].transform.position;
        float progress=0;
        while(progress<1)
        {
            // The escort waits when the player falls behind.
            Vector3 relative=player.transform.position-workers[0].transform.position;
            if(relative.magnitude<6.5f)progress+=Time.deltaTime/15;
            ui.objective.text=relative.magnitude>=6.5f?"族人正在等你，沿黃色箭頭跟上":"跟著黃色箭頭，前往巨木";
            for(int i=0;i<workers.Length;i++)workers[i].transform.position=Vector3.Lerp(starts[i],WorkerDestination(i),Mathf.Clamp01(progress));
            yield return null;
        }
        for(int i=0;i<workers.Length;i++)workers[i].Face(sacredTree.position);
        if(routeGuide)routeGuide.EscortArrived=true;
        while(!ArrivedAtTree)
        { ui.objective.text="沿黃色箭頭走近巨木";ui.hint.text=$"距離黃色標記小於 {treeArrivalDistance:0.0} 公尺，就會自動進入劇情。";yield return null; }
        player.canMove=false;
        routeGuide.GuidanceEnabled=false;
        ui.hint.text="";ui.objective.text="聆聽警察與族人的對話";
        yield return FrameSpeaker(officer);
        yield return Say(officer,"日本警察","把這些樹都砍了。");
        yield return FrameSpeaker(workers[0]);
        yield return Say(workers[0],"族人","這棵巨木是我們的守護者……真的要砍下去嗎？");
        yield return FrameConfrontation();
        SetStage(Stage.TreeChoice);ui.objective.text="面對聖地的抉擇";
        ui.hint.text="按 1／右手主按鈕，或按 2／左手主按鈕";
        ui.Choices("你要如何回應伐木命令？","1  保護巨樹","2  砍伐巨樹");readyToChoose=true;
        while(TreeDecision<0) yield return null;
        if(TreeDecision==0) yield return Protect(); else yield return Fell();
        yield return Fade(1,1.6f);
        ui.Line("","");ui.hint.text="";yield return new WaitForSeconds(.6f);
        SetNight();
        player.Warp(meetingSpawn.position,campfire.position);
        player.canMove=false;player.canLook=true;
        SetStage(Stage.Meeting);ui.chapter.text="第二章  /  夜晚的秘密會議";ui.objective.text="聆聽莫那魯道與六社領袖";
        yield return Fade(0,1.5f);
        yield return Say(mona,"莫那魯道","若再忍下去，族人的靈魂將被踐踏殆盡！");
        string[] speeches={"我們肩上的木材越來越重，能帶回家的工錢卻越來越少。","他們的命令已經踏進聖地，連祖靈的守護也要奪走。","我們的孩子，不該只學會在槍口前低頭。","部落之間要彼此照應，不能再讓任何一社獨自受辱。","反抗會付出代價。但沉默，也正在奪走我們的一切。","我們要奪回尊嚴。今晚，把各社的決心連在一起。"};
        for(int i=0;i<leaders.Length;i++) yield return Say(leaders[i],"六社領袖 · "+(i+1),speeches[i%speeches.Length]);
        SetStage(Stage.Vote);ui.objective.text="表達你的立場";ui.hint.text="1 支持／2 拒絕  |  VR 舉起右手支持，或按左手主按鈕拒絕";
        ui.Choices("你支持起義嗎？","1  舉手，支持起義","2  搖頭，拒絕起義");readyToChoose=true;
        while(MeetingDecision<0) yield return null;
        SetStage(Stage.Ending);ui.hint.text="";
        if(MeetingDecision==0)
        {
            yield return Say(leaders[0],"族人","願有一天，孩子能不再受命令與槍口擺布。願我們自由地生活。",6);
            yield return Say(mona,"莫那魯道","我們記住彼此今晚的決心。",4);
        }
        else
        {
            yield return Say(conservatives[0],"保守派族人","起義會把家人也捲進去！我不能答應。",5);
            yield return Say(mona,"莫那魯道","我們已經忍受太多。這一次，我們要守住尊嚴。",5);
            yield return Say(conservatives[1],"保守派族人","既然你們已經決定，我們就先離開。",4);
            yield return LeaveConservatives();
        }
        mona.seated=false;
        yield return new WaitForSeconds(.8f);
        // Keep tracked head control in XR. Desktop gets a restrained close-up and pullback.
        if(!player.IsVR) player.Warp(new Vector3(0,.05f,1.1f),mona.transform.position);
        yield return Say(mona,"莫那魯道","我們的血，不該再白白流淌。霧社，該覺醒了！",6);
        ui.Line("","");
        if(!player.IsVR)
        {
            player.canLook=false;
            Vector3 from=player.transform.position;
            for(float t=0;t<1;t+=Time.deltaTime/5)
            { player.transform.position=Vector3.Lerp(from,new Vector3(0,2,-8),Mathf.SmoothStep(0,1,t));player.view.transform.LookAt(campfire.position+Vector3.up);yield return null; }
        }
        yield return Fade(1,2);
        ui.endingPanel.SetActive(true);ui.continueButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(3);
        SetStage(Stage.Complete);
        if(saveResult)
        {
            var result=new Chapter2Result {treeChoice=TreeDecision==0?"protect":"fell",meetingChoice=MeetingDecision==0?"support":"refuse",woodIntegrity=Integrity,validCuts=ValidCuts,failedCuts=FailedCuts,casualties=TreeDecision==0?1:0,completed=true};
            PlayerPrefs.SetString("WusheEvent.Chapter2.Result",JsonUtility.ToJson(result));PlayerPrefs.Save();
        }
        ui.continueButton.gameObject.SetActive(true);player.canLook=false;
#if UNITY_EDITOR
        if(stopEditorAfterEnding) UnityEditor.EditorApplication.isPlaying=false;
#endif
    }
    IEnumerator Intro()
    {
        // Retain the selected clip while the decoder prepares it. Inspector changes
        // to the source field must not invalidate an already running introduction.
        var clip=openingFilm;
        if(clip)
        {
            video=gameObject.AddComponent<VideoPlayer>();video.playOnAwake=false;video.isLooping=false;video.clip=clip;
            if(clip.audioTrackCount>0){video.audioOutputMode=VideoAudioOutputMode.AudioSource;video.controlledAudioTrackCount=1;video.SetTargetAudioSource(0,effects);}
            else video.audioOutputMode=VideoAudioOutputMode.None;
            video.renderMode=VideoRenderMode.RenderTexture;videoTexture=new RenderTexture(1280,720,0);videoTexture.Create();video.targetTexture=videoTexture;
            ui.videoImage.texture=videoTexture;ui.videoImage.gameObject.SetActive(true);
            video.errorReceived+=VideoError;video.loopPointReached+=VideoEnd;
            video.Prepare();float began=Time.realtimeSinceStartup;
            while(!video.isPrepared && !videoError && Time.realtimeSinceStartup-began<videoPrepareTimeout && !skip) yield return null;
            if(video.isPrepared && !skip && !videoError)
            {
                video.Play();VideoPlayed=true;
                // Start and finish watchdogs cover decoder failures without trapping the player.
                float deadline=Time.realtimeSinceStartup+(float)clip.length+8;
                while(!videoEnded && !videoError && !skip && Time.realtimeSinceStartup<deadline) yield return null;
                VideoFinished=videoEnded;
            }
            video.Stop();ui.videoImage.gameObject.SetActive(false);
        }
        if((!VideoPlayed || videoError || !videoEnded) && !skip)
        {
            for(int i=0;i<introLines.Length && !skip;i++)
            {
                ui.Line("新規定",introLines[i]);
                for(float t=0;t<6 && !skip;t+=Time.unscaledDeltaTime) yield return null;
            }
        }
        ui.Line("","");
    }
    void VideoError(VideoPlayer source,string message) { videoError=true;Debug.LogWarning("[Chapter2] Opening film unavailable; using story captions. "+message); }
    void VideoEnd(VideoPlayer source) { videoEnded=true; }
    public void SkipIntro() { if(CurrentStage==Stage.Intro) skip=true; }
    IEnumerator Protect()
    {
        SetStage(Stage.Consequence);ui.objective.text="保護巨樹";ui.hint.text="";
        // Prepare the hand meshes before the threat, keeping the gunshot frame smooth.
        for(int i=1;i<workers.Length;i++)
            workers[i].gameObject.AddComponent<Chapter2GriefReaction>().Prepare();
        var blocking=workers[0].gameObject.AddComponent<Chapter2BlockingPose>();
        blocking.Prepare();
        // Speak while approaching, then hold a close confrontation at the rifle's reach.
        var start=workers[0].transform.position;
        var block=officer.transform.position+new Vector3(-1.7f,0,.15f);block.y=start.y;
        yield return CameraShot(new Vector3(1.1f,1.65f,2.5f),new Vector3(1.1f,.85f,7.4f),.65f);
        CameraBeat="blocking";
        ui.Line("族人","別碰它！這是我們的聖地。");
        workers[0].speaking=true;workers[0].Rig.conversationTarget=officer.Rig.Head;
        for(float t=0;t<1;t+=Time.deltaTime/2.1f)
        {
            workers[0].transform.position=Vector3.Lerp(start,block,Mathf.SmoothStep(0,1,t));
            officer.Face(workers[0].transform.position);yield return null;
        }
        workers[0].transform.position=block;
        blocking.Raise();
        CameraBeat="shooting";
        workers[0].Face(officer.transform.position);officer.Face(workers[0].transform.position);
        yield return new WaitForSeconds(2.2f);workers[0].speaking=false;workers[0].Rig.conversationTarget=null;
        var rifle=officer.GetComponent<Chapter2Rifle>();
        rifle.target=workers[0];
        for(float t=0;t<1;t+=Time.deltaTime/1.3f){rifle.aim=Mathf.SmoothStep(0,1,t);yield return null;}
        rifle.aim=1;
        yield return Say(officer,"日本警察","退開！誰敢違抗命令？",4);
        ui.Line("族人","這是祖靈守護的地方……我們不能退。 ");
        yield return new WaitForSeconds(1.2f);
        rifle.Fire();
        if(threatAudio) effects.PlayOneShot(threatAudio,.45f);
        workers[0].BeginFall(officer.transform.position);
        CameraBeat="shot-lowering";
        // Lower immediately while the casualty collapses; witnesses wait for the fall.
        for(float t=0;t<1;t+=Time.deltaTime/.42f)
        {
            rifle.aim=1-Mathf.SmoothStep(0,1,t);rifle.lowered=Mathf.SmoothStep(0,1,t);
            yield return null;
        }
        rifle.aim=0;rifle.lowered=1;
        ui.Line("族人","槍聲過後，一名阻擋警察的族人倒下。同伴急忙上前查看。");
        while(workers[0].FallProgress<1)yield return null;
        CameraBeat="rescue";
        for(int i=1;i<workers.Length;i++)
        {
            Vector3 beside=workers[0].transform.position+new Vector3(i==1?.65f:-.95f,0,i==1?.95f:1.1f);
            workers[i].GetComponent<Chapter2GriefReaction>().Begin(workers[0],.12f+(i-1)*.2f,beside);
        }
        foreach(var worker in workers)
        {
            var reaction=worker.GetComponent<Chapter2GriefReaction>();
            if(reaction)while(!reaction.Examining)yield return null;
        }
        CameraBeat="checking-casualty";
        yield return new WaitForSeconds(2.2f);
        yield return OrderSurvivorsToTrees(rifle);
    }
    IEnumerator Fell()
    {
        yield return RestoreChoppingView();
        SetStage(Stage.Chopping);canChop=true;player.canMove=true;axe.SetActive(true);chopStarted=Time.time;
        if(TryGetChopContact(out RaycastHit focus))player.FocusOn(focus.point);
        ui.Line("伐木","握穩木柄斧。等游標進入綠色區域，再朝樹幹落斧。");
        ui.SetMeter(RhythmPhase,ValidCuts);
        ui.objective.text="對準樹幹，小心落斧";ui.meterPanel.SetActive(true);
        ui.hint.text="游標進入綠色區域時，按 E／空白鍵／右手扳機。避免傷及木材。";
        while(ValidCuts<5)
        {
            if(ConsecutiveFailedCuts>=3)
            {
                canChop=false;player.canMove=false;ui.meterPanel.SetActive(false);
                axe.SetActive(false);yield return FrameSpeaker(officer);
                yield return Say(officer,"日本警察","木材不能再受損！放慢動作，重新找準落點。",4);
                yield return RestoreChoppingView();axe.SetActive(true);
                Integrity=100;ValidCuts=0;ConsecutiveFailedCuts=0;chopStarted=Time.time;canChop=true;player.canMove=true;
                ui.SetMeter(RhythmPhase,ValidCuts);ui.meterPanel.SetActive(true);
            }
            yield return null;
        }
        SetStage(Stage.Consequence);canChop=false;player.canMove=false;ui.meterPanel.SetActive(false);ui.hint.text="";
        axe.SetActive(false);ui.objective.text="巨木倒下";
        yield return FallingTreeWarning();
        foreach(var c in sacredTree.GetComponentsInChildren<Collider>()) c.enabled=false;
        Quaternion start=sacredTree.rotation;
        Vector3 axis=Vector3.Cross(Vector3.up,TreeFallDirection).normalized;
        for(float t=0;t<1;t+=Time.deltaTime/4)
        { TreeFallProgress=t;sacredTree.rotation=Quaternion.AngleAxis(82*t*t,axis)*start;yield return null; }
        sacredTree.rotation=Quaternion.AngleAxis(82,axis)*start;TreeFallProgress=1;
        fallenStump.SetActive(true);
        yield return new WaitForSeconds(.6f);
        foreach(var worker in workers)worker.GetComponent<Chapter2StartleReaction>().Release();
        yield return new WaitForSeconds(.65f);
        yield return FrameSpeaker(workers[0]);
        yield return Say(workers[0],"族人","命令完成了。可是，我們該怎麼面對祖靈？",5);
    }
    IEnumerator SwingAxe(Chapter2Axe tool,RaycastHit contact,bool accurate)
    {
        player.canMove=false;
        yield return tool.Swing(contact,()=>{
            if(accurate){ValidCuts++;ConsecutiveFailedCuts=0;ui.Line("伐木","斧刃切進樹皮，木屑飛散。放穩斧頭，等待下一次時機。");}
            else{FailedCuts++;ConsecutiveFailedCuts++;Integrity=Mathf.Max(0,Integrity-20);ui.Line("伐木","落斧偏了，木材受到損傷。等游標進入綠色區域，再落斧。");}
            ui.SetMeter(RhythmPhase,ValidCuts);
            if(chopAudio)effects.PlayOneShot(chopAudio,.7f);
        });
        if(CurrentStage==Stage.Chopping&&canChop)player.canMove=true;
    }
    IEnumerator Say(Chapter2Actor actor,string name,string words,float seconds=0)
    {
        ui.Line(name,words);if(actor) {actor.speaking=true;if(actor.Rig) actor.Rig.conversationTarget=player.view.transform;}
        yield return new WaitForSeconds(seconds>0?seconds:lineSeconds);
        if(actor) actor.speaking=false;
    }
    IEnumerator LeaveConservatives()
    {
        Vector3[] start=new Vector3[conservatives.Length];for(int i=0;i<start.Length;i++){start[i]=conservatives[i].transform.position;conservatives[i].seated=false;}
        for(float t=0;t<1;t+=Time.deltaTime/7)
        {for(int i=0;i<start.Length;i++) conservatives[i].transform.position=Vector3.Lerp(start[i],new Vector3(-12-i*1.2f,start[i].y,-12),t);yield return null;}
        foreach(var actor in conservatives) actor.gameObject.SetActive(false);
    }
    public void SetNight()
    {
        // The forward-fallen tree overlaps the separate night meeting set.
        // Remove it under the transition's black screen before revealing the council.
        if(TreeDecision==1){sacredTree.gameObject.SetActive(false);fallenStump.SetActive(false);}
        dayGroup.SetActive(false);nightGroup.SetActive(true);sun.color=new Color(.38f,.5f,.78f);sun.intensity=.25f;sun.transform.rotation=Quaternion.Euler(35,-40,0);
        RenderSettings.ambientLight=new Color(.12f,.17f,.24f);RenderSettings.ambientIntensity=.45f;RenderSettings.fogColor=new Color(.025f,.04f,.065f);RenderSettings.fogDensity=.018f;
        player.view.backgroundColor=RenderSettings.fogColor;fireLight.gameObject.SetActive(true);
        ambience.clip=nightAudio;ambience.Play();if(fireAudio)fireAudio.Play();
    }
    IEnumerator Fade(float target,float seconds)
    {
        float from=ui.fade.color.a;
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/Mathf.Max(.01f,seconds)) { ui.fade.color=new Color(0,0,0,Mathf.Lerp(from,target,t));yield return null; }
        ui.fade.color=new Color(0,0,0,target);
    }
    void ReturnToMenu()
    {
        const string menu="選擇章節ˊ";
        if(Application.CanStreamedLevelBeLoaded(menu)) SceneManager.LoadScene(menu);
    }
    void OnDisable()
    {
        if(flow!=null)StopCoroutine(flow);
        if(video){video.errorReceived-=VideoError;video.loopPointReached-=VideoEnd;video.Stop();}
        if(videoTexture){videoTexture.Release();Destroy(videoTexture);}
        if(ui){ui.buttonA.onClick.RemoveListener(SelectA);ui.buttonB.onClick.RemoveListener(SelectB);ui.continueButton.onClick.RemoveListener(ReturnToMenu);}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    [Serializable] public class Chapter2Result
    {
        public string treeChoice,meetingChoice;
        public float woodIntegrity;
        public int validCuts,failedCuts,casualties;
        public bool completed;
    }
}
