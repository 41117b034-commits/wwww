using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Station story within the single Chapter 3 scene.
public sealed class Chapter3StationController : MonoBehaviour
{
    public enum Stage { Arrival, Approach, Briefing, Equipment, Regroup, Departure, SchoolHandoff }
    public Chapter3Player player;
    public Chapter3Flow flow;
    public Chapter3Presentation ui;
    public Chapter2Actor bawan, pihu, mona;
    public Transform riflePickup, rifleFocus;
    public Vector3[] approachPath, departurePath;
    [Min(1)] public float lineSeconds = 4;
    public Stage CurrentStage { get; private set; }
    public bool HasRifle { get; private set; }
    public bool CompanionsWaiting { get; private set; }
    public bool PartComplete => CurrentStage == Stage.SchoolHandoff;
    public string CurrentLine { get; private set; }
    public int Waypoint { get; private set; }
    bool equipmentBusy;

    void Start()
    {
        if(!player || !ui || !bawan || !pihu || !mona || !riflePickup || !rifleFocus || approachPath.Length<2 || departurePath.Length<2)
        { Debug.LogError("[Chapter3Station] Missing scene references.");enabled=false;return; }
        player.station=this;player.chapter=null;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);
        Begin();
    }
    public void Begin()
    {
        StopAllCoroutines();HasRifle=false;equipmentBusy=false;CompanionsWaiting=false;Waypoint=0;CurrentLine=null;
        bawan.speaking=pihu.speaking=mona.speaking=false;
        bawan.transform.position=new Vector3(0,0,-15);pihu.transform.position=new Vector3(1.6f,0,-17);
        bawan.Face(Vector3.zero);pihu.Face(Vector3.zero);mona.Face(new Vector3(0,0,-3));
        riflePickup.gameObject.SetActive(true);player.station=this;player.chapter=null;player.combat=null;
        player.Warp(new Vector3(0,.06f,-18),new Vector3(0,1.65f,0));
        ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);
        StartCoroutine(Arrival());
    }
    void Update()
    {
        ui.ConfigureVR(player.IsVR);
        if(CurrentStage==Stage.Equipment && !equipmentBusy)
        {
            bool near=Flat(player.transform.position,rifleFocus.position)<=2;
            ui.hint.text=near?"按 E 領取村田十八年式步槍":"到左前方木桌領槍。  WASD 移動 · 按住滑鼠右鍵環視";
            if(player.ActionPressed)TakeRifle();
        }
        else if(CurrentStage==Stage.Regroup)
        {
            bool near=Flat(player.transform.position,bawan.transform.position)<=3;
            ui.hint.text=near?"按 E 告訴巴萬：準備好了，出發。":"步槍已攜帶。走到巴萬身旁，準備前往霧社。";
            if(player.ActionPressed)ConfirmDeparture();
        }
    }
    IEnumerator Arrival()
    {
        CurrentStage=Stage.Arrival;player.canMove=player.canLook=false;
        ui.objective.text="馬赫坡駐在所 · 清晨";
        ui.fade.color=Color.black;
        yield return Fade(false,1.8f);
        player.canLook=true;
        yield return new WaitForSeconds(2.5f);
        yield return Say(bawan,"到了，莫那在前面。跟我來。",lineSeconds);
        CurrentStage=Stage.Approach;player.canMove=true;
        ui.objective.text="跟隨巴萬，與莫那會合";
        yield return Escort(approachPath);
        yield return Gather();
        CurrentStage=Stage.Briefing;player.canMove=false;ui.hint.text="";
        // Step aside so the player can see Mona during the handoff.
        Vector3 left=new Vector3(-1.9f,0,-.4f),right=new Vector3(1.9f,0,-1);
        while(Flat(bawan.transform.position,left)>.1f||Flat(pihu.transform.position,right)>.1f)
        {MoveActor(bawan,left,1.75f);MoveActor(pihu,right,2.05f);yield return null;}
        yield return Say(bawan,"莫那，我們到了。",lineSeconds);
        yield return Say(mona,"帶上槍。到霧社去，跟達多會合。",lineSeconds+1);
        yield return Say(mona,"巴萬，把你的人帶好。",lineSeconds);
        yield return Say(bawan,"知道了。",lineSeconds);
        yield return Say(pihu,"槍在那邊。我在這裡等你。",lineSeconds);
        CurrentStage=Stage.Equipment;player.canMove=true;ui.objective.text="到木桌領取步槍";
        player.FocusOn(rifleFocus.position+Vector3.up*.2f);
    }
    public bool TakeRifle()
    {
        if(CurrentStage!=Stage.Equipment || HasRifle || equipmentBusy || Flat(player.transform.position,rifleFocus.position)>2)return false;
        HasRifle=true;equipmentBusy=true;riflePickup.gameObject.SetActive(false);
        StartCoroutine(Equipment());return true;
    }
    IEnumerator Equipment()
    {
        player.canMove=false;ui.hint.text="已取得村田十八年式步槍";
        yield return Say(bawan,"把槍帶好。這是單發的，打完一發要重新裝。",lineSeconds+1);
        yield return Say(pihu,"好了嗎？我跟你一起走。",lineSeconds);
        equipmentBusy=false;CurrentStage=Stage.Regroup;player.canMove=true;
        ui.objective.text="回到巴萬身旁，確認出發";
        player.FocusOn(bawan.transform.position+Vector3.up*1.15f);
    }
    public bool ConfirmDeparture()
    {
        if(CurrentStage!=Stage.Regroup || !HasRifle || Flat(player.transform.position,bawan.transform.position)>3)return false;
        CurrentStage=Stage.Departure;StartCoroutine(Depart());return true;
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
        yield return Say(pihu,"我們一起走。",lineSeconds);
        ui.Say("",null);ui.hint.text="";yield return Fade(true,1.5f);
        CurrentStage=Stage.SchoolHandoff;player.canLook=false;
        ui.objective.text="前往霧社的山路";
        if(flow) flow.EnterCombat();
    }
    IEnumerator Escort(Vector3[] route)
    {
        for(int i=0;i<route.Length;i++)
        {
            Waypoint=i;
            while(Flat(bawan.transform.position,route[i])>.12f)
            {
                CompanionsWaiting=Flat(player.transform.position,bawan.transform.position)>6.5f;
                ui.hint.text=CompanionsWaiting?"巴萬與比胡正在等你，走近他們。":HasRifle?"獵刀、步槍已攜帶 · 跟上同伴":"獵刀已攜帶 · 跟上巴萬";
                if(!CompanionsWaiting)
                {
                    Vector3 direction=Vector3.ProjectOnPlane(route[i]-bawan.transform.position,Vector3.up).normalized;
                    MoveActor(bawan,route[i],1.75f);
                    MoveActor(pihu,bawan.transform.position-direction*1.7f+Vector3.Cross(Vector3.up,direction)*1.25f,2.05f);
                }
                else {bawan.Face(player.transform.position);pihu.Face(player.transform.position);}
                yield return null;
            }
        }
        CompanionsWaiting=false;
    }
    IEnumerator Gather()
    {
        while(Flat(player.transform.position,bawan.transform.position)>3.8f)
        {CompanionsWaiting=true;ui.hint.text="同伴在前方等你，走近後繼續。";bawan.Face(player.transform.position);yield return null;}
        // Let the rear companion finish arriving before starting the dialogue.
        Vector3 target=bawan.transform.position+new Vector3(1.5f,0,-1.3f);
        while(Flat(pihu.transform.position,target)>.15f){MoveActor(pihu,target,2.05f);yield return null;}
        CompanionsWaiting=false;
    }
    void MoveActor(Chapter2Actor actor,Vector3 target,float speed)
    {target.y=0;actor.transform.position=Vector3.MoveTowards(actor.transform.position,target,speed*Time.deltaTime);actor.GroundHeight=0;}
    IEnumerator Say(Chapter2Actor actor,string line,float seconds)
    {
        CurrentLine=line;ui.Say(actor.DisplayName,line);actor.Face(player.transform.position);actor.speaking=true;
        foreach(var other in new[]{bawan,pihu,mona})if(other!=actor)other.Face(actor.transform.position);
        player.FocusOn(actor.transform.position+Vector3.up*1.2f);
        var face=actor.GetComponent<Chapter2SpeechFace>();if(face)face.BeginLine(line,seconds,null);
        yield return Chapter3Flow.WaitForLine(seconds);
        actor.speaking=false;CurrentLine=null;ui.Say("",null);
    }
    IEnumerator Fade(bool black,float duration)
    {for(float t=0;t<duration;t+=Time.deltaTime){ui.fade.color=new Color(0,0,0,black?t/duration:1-t/duration);yield return null;}ui.fade.color=black?Color.black:Color.clear;}
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        if(Walkable(desired))return desired;
        Vector3 p=current;p.y=desired.y;
        if(Walkable(new Vector3(desired.x,desired.y,current.z)))p.x=desired.x;
        if(Walkable(new Vector3(p.x,desired.y,desired.z)))p.z=desired.z;
        return p;
    }
    bool Walkable(Vector3 p)
    {
        if(p.x>=-10 && p.x<=10 && p.z>=-24 && p.z<=2.7f)return true;
        // Keep the final trail gated until the player has the rifle and confirms departure.
        if(CurrentStage!=Stage.Departure && CurrentStage!=Stage.SchoolHandoff)return false;
        for(int i=1;i<departurePath.Length;i++)
        {
            Vector3 a=departurePath[i-1],b=departurePath[i];a.y=b.y=p.y;
            float t=Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude;
            if((p-Vector3.Lerp(a,b,Mathf.Clamp01(t))).sqrMagnitude<=8.4f)return true;
        }
        return false;
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    public void Suspend(){StopAllCoroutines();bawan.speaking=pihu.speaking=mona.speaking=false;CurrentLine=null;}
}
