using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Single-school owner. Batch 4 takes over following and objectives after Tado.
public sealed class Chapter3SchoolController : MonoBehaviour
{
    public Chapter3Flow flow;
    public Chapter3Player player;
    public Chapter3Presentation ui;
    public Chapter2Actor bawan, pihu, tado;
    public Light sun;
    public Vector3 spawn = new Vector3(0,.06f,-18);
    public Transform[] landmarks;
    public Vector3[] breakthroughRoute, coverRoute, rescueRoute;
    public string Area { get; private set; }
    public bool Exploring { get; private set; }
    public bool MetTado { get; private set; }
    public bool[] Visited { get; private set; }
    public Chapter3SchoolBattle Battle {get;private set;}
    readonly Chapter3PathMotor[] motors = {new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly List<Vector3> trail = new List<Vector3>();
    readonly int[] trailIndex = new int[2];
    float lineUntil, lineBegan, nextTrail;
    float previousFogDensity;
    Light previousSun;
    bool lightingSet;

    public void Begin()
    {
        StopAllCoroutines();Exploring=false;MetTado=false;Area="校門前";lineUntil=0;
        Visited=new bool[landmarks.Length];trail.Clear();trail.Add(spawn);
        for(int i=0;i<2;i++){motors[i].Reset();trailIndex[i]=0;}
        bawan.transform.position=spawn+new Vector3(-1.4f,-spawn.y,-1.7f);
        pihu.transform.position=spawn+new Vector3(1.4f,-spawn.y,-2.8f);
        bawan.facing=pihu.facing=Vector3.forward;bawan.speaking=pihu.speaking=tado.speaking=false;
        tado.transform.position=new Vector3(-4.7f,0,-5);
        player.chapter=null;player.station=null;player.combat=null;player.school=this;
        // Solid companions keep the camera out of their heads. Mission walking
        // also gives the player room when a companion crosses their position.
        var body=player.GetComponent<CharacterController>();
        foreach(var actor in new[]{bawan,pihu,tado})
            foreach(var collider in actor.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(body,collider,false);
        player.Warp(spawn,new Vector3(0,2,28));player.speed=2.8f;player.canMove=player.canLook=false;
        ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);ui.hint.text="";
        ui.objective.text="霧社公學校 · 運動會";
        previousFogDensity=RenderSettings.fogDensity;previousSun=RenderSettings.sun;lightingSet=true;
        RenderSettings.fogDensity=.0065f;if(sun)RenderSettings.sun=sun;
        if(!Battle)Battle=GetComponent<Chapter3SchoolBattle>();
        if(!Battle)Battle=gameObject.AddComponent<Chapter3SchoolBattle>();
        Battle.Begin(this);
        StartCoroutine(Arrival());
    }
    IEnumerator Arrival()
    {
        for(float t=0;t<1.3f;t+=Time.deltaTime){ui.fade.color=new Color(0,0,0,1-t/1.3f);yield return null;}
        ui.fade.color=Color.clear;Exploring=true;player.canMove=player.canLook=true;
        Speak(bawan,"學校就在前面，達多在校門旁等我們。",4.2f);
    }
    public void Suspend()
    {
        StopAllCoroutines();Exploring=false;
        if(Battle)Battle.Suspend();
        if(lightingSet){RenderSettings.fogDensity=previousFogDensity;RenderSettings.sun=previousSun;lightingSet=false;}
        if(bawan)bawan.speaking=false;if(pihu)pihu.speaking=false;if(tado)tado.speaking=false;
    }
    void Update()
    {
        if(!Exploring)return;
        ui.ConfigureVR(player.IsVR);
        if(Battle&&Battle.OwnsFlow)return;
        if(lineUntil>0&&(Time.time>lineUntil||(Time.time>lineBegan+.25f&&Keyboard.current?.spaceKey.wasPressedThisFrame==true)))
        {lineUntil=0;ui.Say("",null);bawan.speaking=pihu.speaking=tado.speaking=false;}
        var position=player.transform.position;
        if(Time.time>=nextTrail&&Flat(position,trail[trail.Count-1])>.9f)
        {trail.Add(position);nextTrail=Time.time+.2f;}
        Follow(bawan,0,2.8f);Follow(pihu,1,4.7f);
        float best=1000;int nearest=-1;
        for(int i=0;i<landmarks.Length;i++)
        {
            float distance=Flat(position,landmarks[i].position);
            if(distance<best){best=distance;nearest=i;}
            if(distance<4)Visited[i]=true;
        }
        Area=nearest<0?"校門前":landmarks[nearest].name;
        ui.objective.text="到校門旁與達多會合";
        bool nearTado=Flat(position,tado.transform.position)<3;
        ui.hint.text=nearTado?"E 與達多交談  ·  WASD 移動  ·  按住右鍵環視":"走向校門左側的達多 · WASD 移動 · 按住右鍵環視";
        if(nearTado&&player.ActionPressed&&lineUntil<=0)
        {
            MetTado=true;lineUntil=0;bawan.speaking=pihu.speaking=tado.speaking=false;Battle.MeetTado();
        }
    }
    void Follow(Chapter2Actor actor,int index,float gap)
    {
        Vector3 current=actor.transform.position,position=player.transform.position;
        if(Flat(current,position)<gap){if(!actor.speaking)actor.Face(position);return;}
        // Follow the player's walked path around corners instead of cutting
        // through the building when the player changes sides of the school.
        int end=trail.Count-1;
        while(end>0&&Flat(trail[end],position)<gap)end--;
        trailIndex[index]=Mathf.Min(trailIndex[index],end);
        for(int i=Mathf.Min(end,trailIndex[index]+12);i>=trailIndex[index];i--)
            if(Chapter3PathMotor.Clear(current,trail[i])){trailIndex[index]=i;break;}
        Vector3 target=trail[trailIndex[index]];
        if(Flat(current,target)<.5f&&trailIndex[index]<end)target=trail[++trailIndex[index]];
        if(Flat(current,target)<.2f&&Chapter3PathMotor.Clear(current,position))
            target=Vector3.MoveTowards(position,current,gap-.1f);
        motors[index].Move(actor,target,3.35f);
    }
    void Speak(Chapter2Actor actor,string line,float seconds)
    {
        bawan.speaking=pihu.speaking=tado.speaking=false;actor.speaking=true;
        ui.Say(actor.DisplayName,line);lineBegan=Time.time;lineUntil=Time.time+seconds;
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,seconds,null);
    }
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {desired.x=Mathf.Clamp(desired.x,-43,43);desired.z=Mathf.Clamp(desired.z,-23,113);return Battle?Battle.Constrain(current,desired):desired;}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
}
