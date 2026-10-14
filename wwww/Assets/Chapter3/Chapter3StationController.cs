using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Station story within the single Chapter 3 scene.
public sealed class Chapter3StationController : MonoBehaviour
{
    public enum Stage { Arrival, Approach, Briefing, Equipment, Regroup, Departure, SchoolHandoff, Readiness, Refused, Ended, Conversation, WeaponChoice }
    public Chapter3Player player;
    public Chapter3Flow flow;
    public Chapter3Presentation ui;
    public Chapter2Actor bawan, pihu, mona;
    public Transform riflePickup, rifleFocus, knifePickup, knifeFocus;
    public Vector3[] approachPath, departurePath;
    [Min(1)] public float lineSeconds = 4;
    public Stage CurrentStage { get; private set; }
    public bool HasRifle { get; private set; }
    public bool HasKnife { get; private set; }
    public bool HasWeapon => HasKnife||HasRifle;
    public Chapter3Weapons.Weapon? ChosenWeapon { get; private set; }
    public string WeaponName => HasKnife?"獵刀":"村田十八年式步槍";
    public Chapter3OpeningConversation Opening {get;private set;}
    public Chapter3WeaponChoiceUI Choice {get;private set;}
    public bool CompanionsWaiting { get; private set; }
    public bool PartComplete => CurrentStage == Stage.SchoolHandoff;
    public string CurrentLine { get; private set; }
    public int Waypoint { get; private set; }
    bool equipmentBusy;
    public Chapter3ReadinessConversation Readiness {get;private set;}
    public bool RefusalCommitted => CurrentStage==Stage.Refused||CurrentStage==Stage.Ended;
    Chapter3MonaRebuke rebuke;
    readonly Chapter3PathMotor monaMotor=new Chapter3PathMotor();
    Vector3 monaHome;
    Chapter3StationRoadBoundary roadBoundary;
    public Chapter3RouteGuide RouteGuide {get;private set;}
    bool attendingPlayer;
    readonly Chapter3PathMotor bawanMotor=new Chapter3PathMotor(),pihuMotor=new Chapter3PathMotor();

    void Start()
    {
        if(!player || !ui || !bawan || !pihu || !mona || !riflePickup || !rifleFocus || !knifePickup || !knifeFocus || approachPath.Length<2 || departurePath.Length<2)
        { Debug.LogError("[Chapter3Station] Missing scene references.");enabled=false;return; }
        player.station=this;player.chapter=null;
        monaHome=mona.transform.position;
        roadBoundary=new Chapter3StationRoadBoundary(flow?flow.stationRegion.transform:transform.parent);
        RouteGuide=gameObject.AddComponent<Chapter3RouteGuide>();
        Readiness=gameObject.AddComponent<Chapter3ReadinessConversation>();
        Opening=gameObject.AddComponent<Chapter3OpeningConversation>();
        Choice=Chapter3WeaponChoiceUI.Build(ui);
        Choice.knife.onClick.AddListener(()=>ChooseWeapon(Chapter3Weapons.Weapon.Knife));
        Choice.rifle.onClick.AddListener(()=>ChooseWeapon(Chapter3Weapons.Weapon.Rifle));
        rebuke=mona.gameObject.AddComponent<Chapter3MonaRebuke>();rebuke.listener=player.transform;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);
        Begin();
    }
    public void Begin()
    {
        StopAllCoroutines();attendingPlayer=false;if(RouteGuide)RouteGuide.Hide();HasRifle=HasKnife=false;ChosenWeapon=null;equipmentBusy=false;CompanionsWaiting=false;Waypoint=0;CurrentLine=null;
        if(flow)flow.ClearEquipment();if(Opening)Opening.Cancel();if(Choice)Choice.gameObject.SetActive(false);
        if(Readiness)Readiness.Cancel();if(rebuke)rebuke.Rebuking=false;
        monaMotor.Reset();mona.transform.position=monaHome;
        Chapter3PickupArrow.Hide(ui);
        bawanMotor.Reset();pihuMotor.Reset();
        bawan.speaking=pihu.speaking=mona.speaking=false;
        bawan.transform.position=new Vector3(0,0,-15);pihu.transform.position=new Vector3(1.6f,0,-17);
        bawan.Face(Vector3.zero);pihu.Face(Vector3.zero);mona.Face(new Vector3(0,0,-3));
        riflePickup.gameObject.SetActive(true);knifePickup.gameObject.SetActive(true);player.station=this;player.chapter=null;player.combat=null;player.school=null;
        player.Warp(new Vector3(0,.06f,-18),bawan.transform.position+Vector3.up*1.2f);
        bawan.Face(player.transform.position);
        ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);
        Opening.PrepareService();
        StartCoroutine(Arrival());
    }
    void Update()
    {
        ui.ConfigureVR(player.IsVR);
        if(CurrentStage==Stage.WeaponChoice)
        {
            if(Keyboard.current?.digit1Key.wasPressedThisFrame==true)ChooseWeapon(Chapter3Weapons.Weapon.Knife);
            else if(Keyboard.current?.digit2Key.wasPressedThisFrame==true)ChooseWeapon(Chapter3Weapons.Weapon.Rifle);
        }
        else if(CurrentStage==Stage.Equipment && !equipmentBusy)
        {
            var target=ChosenWeapon==Chapter3Weapons.Weapon.Knife?knifeFocus:rifleFocus;
            var other=ChosenWeapon==Chapter3Weapons.Weapon.Knife?rifleFocus:knifeFocus;
            string name=ChosenWeapon==Chapter3Weapons.Weapon.Knife?"獵刀":"村田十八年式步槍";
            bool near=Flat(player.transform.position,target.position)<=2;
            ui.hint.text=near?"按 E 拿起"+name:Flat(player.transform.position,other.position)<=2?
                "你選了"+name+"，請依黃色箭頭領取；只能持有一種武器。":"依黃色箭頭，到"+(ChosenWeapon==Chapter3Weapons.Weapon.Knife?"右":"左")+"側木桌領取"+name+"。";
            if(player.ActionPressed){if(ChosenWeapon==Chapter3Weapons.Weapon.Knife)TakeKnife();else TakeRifle();}
        }
    }
    void LateUpdate()
    {
        if(attendingPlayer||CompanionsWaiting){FacePlayer(bawan);FacePlayer(pihu);}
        // Escort coroutines also write the hint; apply this warning after them.
        if(HasWeapon&&CurrentStage!=Stage.Refused&&CurrentStage!=Stage.Ended)
        {
            var other=HasKnife?rifleFocus:knifeFocus;
            if(Flat(player.transform.position,other.position)<=2)ui.hint.text="只能持有一種武器，你已經拿了"+WeaponName+"。";
        }
    }
    IEnumerator Arrival()
    {
        CurrentStage=Stage.Arrival;player.canMove=player.canLook=false;
        ui.objective.text="馬赫坡駐在所 · 清晨";
        ui.Say("","馬赫坡駐在所 · 清晨");
        ui.fade.color=Color.black;
        yield return Fade(false,1.8f);
        // Keep the establishing shot and place/time legible before opening chat.
        yield return new WaitForSecondsRealtime(3);
        CurrentStage=Stage.Conversation;
        ui.objective.text="馬赫坡駐在所 · 和巴萬交談";
        Opening.Begin(this,()=>{if(CurrentStage==Stage.Conversation)StartCoroutine(ApproachMona());});
    }
    IEnumerator ApproachMona()
    {
        player.canLook=true;
        yield return Say(bawan,"先去見莫那，聽他安排。跟我來。",lineSeconds);
        CurrentStage=Stage.Approach;player.canMove=true;
        ui.objective.text="跟隨巴萬，與莫那會合";
        yield return Escort(approachPath);
        yield return Gather();
        CurrentStage=Stage.Briefing;player.canMove=false;ui.hint.text="";
        attendingPlayer=false;
        // Step aside so the player can see Mona during the handoff.
        Vector3 left=new Vector3(-1.9f,0,-.4f),right=new Vector3(1.9f,0,-1);
        while(Flat(bawan.transform.position,left)>.1f||Flat(pihu.transform.position,right)>.1f)
        {MoveActor(bawan,left,1.75f);MoveActor(pihu,right,2.05f);yield return null;}
        yield return Say(bawan,"莫那，我們到了。",lineSeconds);
        yield return Say(mona,"我們已經攻下這裡，打倒了駐在所的日本警察。",lineSeconds+1);
        yield return Say(mona,"接下來到霧社，跟達多會合。先選一把襯手的武器，刀或槍，只帶一種。",lineSeconds+2);
        CurrentStage=Stage.WeaponChoice;player.canMove=player.canLook=false;ui.objective.text="選一把襯手的武器";
        ui.hint.text="點選武器，或按數字 1／2";Choice.gameObject.SetActive(true);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public bool ChooseWeapon(Chapter3Weapons.Weapon weapon)
    {
        if(CurrentStage!=Stage.WeaponChoice||ChosenWeapon.HasValue||HasWeapon)return false;
        if(weapon!=Chapter3Weapons.Weapon.Knife&&weapon!=Chapter3Weapons.Weapon.Rifle)return false;
        ChosenWeapon=weapon;Choice.gameObject.SetActive(false);CurrentStage=Stage.Equipment;
        player.canMove=player.canLook=true;ui.objective.text=weapon==Chapter3Weapons.Weapon.Knife?"到右側木桌領取獵刀":"到左側木桌領取步槍";
        var target=weapon==Chapter3Weapons.Weapon.Knife?knifeFocus:rifleFocus;
        Chapter3PickupArrow.Show(ui,target);player.FocusOn(target.position+Vector3.up*.2f);return true;
    }
    public bool TakeRifle()=>TakeWeapon(Chapter3Weapons.Weapon.Rifle);
    public bool TakeKnife()=>TakeWeapon(Chapter3Weapons.Weapon.Knife);
    bool TakeWeapon(Chapter3Weapons.Weapon weapon)
    {
        if(HasWeapon){ui.hint.text="只能持有一種武器，你已經拿了"+WeaponName+"。";return false;}
        var target=weapon==Chapter3Weapons.Weapon.Knife?knifeFocus:rifleFocus;
        if(CurrentStage!=Stage.Equipment||ChosenWeapon!=weapon||equipmentBusy||Flat(player.transform.position,target.position)>2)return false;
        HasKnife=weapon==Chapter3Weapons.Weapon.Knife;HasRifle=weapon==Chapter3Weapons.Weapon.Rifle;equipmentBusy=true;
        (HasKnife?knifePickup:riflePickup).gameObject.SetActive(false);
        if(flow)flow.Carry(weapon);Chapter3PickupArrow.Hide(ui);
        CurrentStage=Stage.Regroup;equipmentBusy=false;
        return ConfirmDeparture();
    }
    public bool ConfirmDeparture()
    {
        if(CurrentStage!=Stage.Regroup || !HasWeapon)return false;
        CurrentStage=Stage.Readiness;player.canMove=player.canLook=false;ui.objective.text="告訴巴萬，你是否準備好出發";
        player.FocusOn(bawan.transform.position+Vector3.up*1.2f);
        Readiness.Begin(this,decision=>
        {
            if(CurrentStage!=Stage.Readiness)return;
            if(decision==Chapter3ReadinessConversation.Decision.Ready){CurrentStage=Stage.Departure;player.canLook=true;StartCoroutine(Depart());}
            else if(decision==Chapter3ReadinessConversation.Decision.NotReady){CurrentStage=Stage.Refused;StartCoroutine(RefuseDeparture());}
        });attendingPlayer=true;return true;
    }
    IEnumerator RefuseDeparture()
    {
        ui.hint.text="";ui.objective.text="莫那走到你面前";player.canMove=player.canLook=false;
        Vector3 direction=Vector3.ProjectOnPlane(mona.transform.position-player.transform.position,Vector3.up).normalized;
        if(direction.sqrMagnitude<.01f)direction=Vector3.forward;
        Vector3 target=mona.transform.position;float best=float.MaxValue;
        // A player can answer beside the steps or the table: choose an open standing spot.
        for(int i=0;i<12;i++)
        {
            Vector3 candidate=player.transform.position+Quaternion.Euler(0,i*30,0)*direction*1.6f;candidate.y=0;
            if(!Chapter3PathMotor.Clear(candidate,candidate))continue;
            float distance=(candidate-mona.transform.position).sqrMagnitude;
            if(distance<best){best=distance;target=candidate;}
        }
        // Step around the companion and courtyard props; never warp through them.
        float approachDeadline=Time.time+8;
        while(Flat(mona.transform.position,target)>.15f&&Time.time<approachDeadline)
        {monaMotor.Move(mona,target,1.65f);player.FocusOn(mona.transform.position+Vector3.up*1.25f);yield return null;}
        rebuke.Rebuking=true;
        yield return Say(mona,(HasKnife?"刀":"槍")+"都拿了，現在才說沒準備好？抬起頭，看著我！",lineSeconds+1);
        yield return Say(mona,"同行的人要把性命交在彼此手上。你連自己的心都沒定下來，憑什麼要他們放心把背後交給你！",lineSeconds+4);
        yield return Say(mona,"今天你不准跟來。把"+(HasKnife?"刀":"槍")+"留下，回家休息！想清楚自己能承擔什麼，再來見我。",lineSeconds+3);
        rebuke.Rebuking=false;HasRifle=HasKnife=false;if(flow)flow.ClearEquipment();ui.hint.text="你沒有隨隊出發，莫那讓你回家休息。";
        yield return new WaitForSeconds(1.5f);yield return Fade(true,1.5f);
        CurrentStage=Stage.Ended;ui.objective.text="此次行動結束";
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        // A standalone player has no Play Mode; retain a legible, non-progressing ending.
        var labels=ui.ending.GetComponentsInChildren<Text>(true);int i=0;
        foreach(var label in labels)if(!label.GetComponentInParent<Button>())label.text=i++==0?"此次行動結束":"你尚未準備好出發。\n莫那讓你留下，回家休息。";
        ui.ending.SetActive(true);
#endif
    }
    IEnumerator Depart()
    {
        player.canMove=false;ui.hint.text="";
        yield return Say(mona,"去吧。",lineSeconds);
        yield return Say(bawan,"比胡，跟上。我們到霧社去。",lineSeconds);
        player.canMove=true;ui.objective.text="跟隨同伴，沿山路前往霧社";
        yield return Escort(departurePath);yield return Gather();
        player.canMove=false;
        yield return Say(bawan,"往前就是霧社。到學校外面，再和達多會合。",lineSeconds+1);
        yield return Say(pihu,"前面山路有日本警察擋著。先突破他們，再去學校外和達多會合。",lineSeconds+2);
        ui.Say("",null);ui.hint.text="";yield return Fade(true,1.5f);
        CurrentStage=Stage.SchoolHandoff;player.canLook=false;
        ui.objective.text="前往霧社的山路";
        if(flow) flow.EnterCombat();
    }
    IEnumerator Escort(Vector3[] route)
    {
        attendingPlayer=false;bawanMotor.Reset();pihuMotor.Reset();
        RouteGuide.Show(bawan.transform.position,route,player.transform,bawan.transform);
        // Pihu waits for the leader to pass his existing position instead of
        // reversing to a newly rotated formation slot at every corner.
        float pihuStart=RouteGuide.Project(pihu.transform.position,6),leaderProgress=0;
        for(int i=0;i<route.Length;i++)
        {
            Waypoint=i;
            while(Flat(bawan.transform.position,route[i])>.12f)
            {
                CompanionsWaiting=Flat(player.transform.position,bawan.transform.position)>6.5f;
                ui.hint.text=CompanionsWaiting?"巴萬與比胡正在等你，走近他們。":HasWeapon?WeaponName+"已攜帶 · 跟上同伴":"跟上巴萬，先去見莫那";
                if(!CompanionsWaiting)
                {
                    MoveActor(bawan,route[i],1.75f);
                    leaderProgress=Mathf.Max(leaderProgress,RouteGuide.Project(bawan.transform.position,leaderProgress+2));
                    float rearProgress=Mathf.Max(0,leaderProgress-1.6f);
                    if(rearProgress>=pihuStart)
                    {
                        Vector3 target=RouteGuide.SampleBeside(rearProgress,.9f);
                        if(Flat(pihu.transform.position,target)>.12f)MoveActor(pihu,target,2.05f);
                        else FacePlayer(pihu);
                    }
                    else FacePlayer(pihu);
                }
                else {FacePlayer(bawan);FacePlayer(pihu);}
                yield return null;
            }
        }
        CompanionsWaiting=false;
    }
    IEnumerator Gather()
    {
        attendingPlayer=true;
        while(Flat(player.transform.position,bawan.transform.position)>3.8f)
        {CompanionsWaiting=true;ui.hint.text="同伴在前方等你，走近後繼續。";yield return null;}
        attendingPlayer=false;CompanionsWaiting=false;
        // Let the rear companion finish arriving before starting the dialogue.
        Vector3 target=bawan.transform.position+new Vector3(1.5f,0,-1.3f);
        while(Flat(pihu.transform.position,target)>.15f){MoveActor(pihu,target,2.05f);yield return null;}
        CompanionsWaiting=false;attendingPlayer=true;RouteGuide.Hide();
    }
    void FacePlayer(Chapter2Actor actor)
    {
        Vector3 target=Vector3.ProjectOnPlane(player.transform.position-actor.transform.position,Vector3.up);
        Vector3 facing=Vector3.RotateTowards(actor.facing.normalized,target.normalized,140*Mathf.Deg2Rad*Time.deltaTime,0);
        actor.Face(actor.transform.position+facing);
    }
    void MoveActor(Chapter2Actor actor,Vector3 target,float speed)
    {(actor==bawan?bawanMotor:pihuMotor).Move(actor,target,speed);}
    IEnumerator Say(Chapter2Actor actor,string line,float seconds)
    {
        CurrentLine=line;ui.Say(actor.DisplayName,line);actor.Face(player.transform.position);actor.speaking=true;
        attendingPlayer=true;
        if(actor!=mona)mona.Face(actor.transform.position);
        player.FocusOn(actor.transform.position+Vector3.up*1.2f);
        var face=actor.GetComponent<Chapter2SpeechFace>();if(face)face.BeginLine(line,seconds,null);
        yield return Chapter3Flow.WaitForLine(seconds);
        actor.speaking=false;CurrentLine=null;ui.Say("",null);
    }
    IEnumerator Fade(bool black,float duration)
    {for(float t=0;t<duration;t+=Time.deltaTime){ui.fade.color=new Color(0,0,0,black?t/duration:1-t/duration);yield return null;}ui.fade.color=black?Color.black:Color.clear;}
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        Vector3 p=current;p.y=desired.y;
        int steps=Mathf.Max(1,Mathf.CeilToInt(Flat(current,desired)/.12f));
        Vector3 step=(desired-current)/steps;step.y=0;
        for(int i=0;i<steps;i++)
        {
            if(Walkable(p+step)){p+=step;continue;}
            Vector3 alongX=p+new Vector3(step.x,0,0);if(Walkable(alongX))p=alongX;
            Vector3 alongZ=p+new Vector3(0,0,step.z);if(Walkable(alongZ))p=alongZ;
        }
        return p;
    }
    bool Walkable(Vector3 p)
    {
        bool departing=CurrentStage==Stage.Departure||CurrentStage==Stage.SchoolHandoff;
        return roadBoundary.Contains(p,player.GetComponent<CharacterController>().radius+.04f,departing);
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    public void Suspend(){StopAllCoroutines();attendingPlayer=false;CompanionsWaiting=false;if(RouteGuide)RouteGuide.Hide();if(Opening)Opening.Cancel();if(Choice)Choice.gameObject.SetActive(false);if(Readiness)Readiness.Cancel();if(rebuke)rebuke.Rebuking=false;bawan.speaking=pihu.speaking=mona.speaking=false;CurrentLine=null;}
}
