using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class Chapter3Combat : MonoBehaviour
{
    public enum Stage { Inactive, Intro, Fighting, Down, Cleared, Complete }
    public Chapter3Flow flow;
    public Chapter3Player player;
    public Chapter3Presentation ui;
    public Chapter3Weapons weapons;
    public Chapter2Actor bawan,pihu;
    public GameObject[] enemyTemplates;
    public Transform runtimeEnemies,playerTarget,ammoPoint,exitPoint;
    public GameObject hud,downPanel;
    public Text healthText,ammoText,combatText,crosshair;
    public Image hurtOverlay;
    public Button retry;
    public Vector3 spawn=new Vector3(0,.08f,-8);
    public float lineSeconds=4;
    public Stage CurrentStage {get;private set;}
    public Chapter3SchoolBattle SchoolEncounter {get;set;}
    public bool Fighting=>SchoolEncounter?SchoolEncounter.Fighting:CurrentStage==Stage.Fighting;
    public int Health {get;private set;}=100;
    public int Retries {get;private set;}
    public int DefeatedCount {get;private set;}
    public bool Complete=>CurrentStage==Stage.Complete;
    readonly List<Chapter3Enemy> localEnemies=new List<Chapter3Enemy>();
    Chapter3CompanionReaction[] localCompanions;
    public List<Chapter3Enemy> Enemies=>SchoolEncounter?SchoolEncounter.Enemies:localEnemies;
    public Chapter3CompanionReaction[] Companions=>SchoolEncounter?SchoolEncounter.Companions:localCompanions;
    public bool EscortWaiting {get;private set;}
    readonly Chapter3PathMotor leaderMotor=new Chapter3PathMotor(),rearMotor=new Chapter3PathMotor();
    float warningUntil;
    float hurtAt=-10;
    bool clearedRoutine;
    public void Begin()
    {
        SchoolEncounter=null;StopAllCoroutines();Retries=0;ResetCheckpoint();CurrentStage=Stage.Intro;
        player.canMove=player.canLook=false;weapons.gameObject.SetActive(false);
        StartCoroutine(Introduction());
    }
    IEnumerator Introduction()
    {
        ui.fade.color=Color.black;ui.objective.text="前往霧社 · 山路遭遇";
        for(float t=0;t<1.2f;t+=Time.deltaTime){ui.fade.color=new Color(0,0,0,1-t/1.2f);yield return null;}ui.fade.color=Color.clear;
        yield return Say(bawan,"前面有巡警。靠著岩石走，別站在路中央。",lineSeconds);
        yield return Say(pihu,"近了用刀。槍打完一發，就要再裝一發。",lineSeconds);
        StartFighting();
    }
    void StartFighting()
    {
        CurrentStage=Stage.Fighting;ui.Say("",null);ui.hint.text="";ui.objective.text="突破前方巡警，與同伴繼續前進";
        player.canMove=player.canLook=true;weapons.gameObject.SetActive(true);
        player.FocusOn(new Vector3(0,1.3f,10));
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
    }
    void ResetCheckpoint()
    {
        clearedRoutine=false;Health=100;DefeatedCount=0;hurtAt=-10;
        warningUntil=0;EscortWaiting=false;leaderMotor.Reset();rearMotor.Reset();
        bawan.transform.position=new Vector3(-5.1f,0,-5);pihu.transform.position=new Vector3(5.2f,0,-6);
        localCompanions=new[]{bawan.GetComponent<Chapter3CompanionReaction>(),pihu.GetComponent<Chapter3CompanionReaction>()};
        foreach(var companion in Companions)if(companion)companion.ResetReaction();
        foreach(var enemy in Enemies)if(enemy){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}Enemies.Clear();
        foreach(var template in enemyTemplates)
        {
            var go=Instantiate(template,runtimeEnemies);go.name=template.name.Replace("範本","");go.SetActive(true);
            var enemy=go.GetComponent<Chapter3Enemy>();enemy.combat=this;Enemies.Add(enemy);
        }
        player.chapter=null;player.station=null;player.combat=this;player.speed=2.8f;
        player.Warp(spawn,new Vector3(0,1.55f,8));player.view.fieldOfView=60;
        weapons.ResetEquipment();ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);ui.fade.color=Color.clear;
        hud.SetActive(true);downPanel.SetActive(false);hurtOverlay.color=Color.clear;
        retry.onClick.RemoveAllListeners();retry.onClick.AddListener(()=>Retry());
    }
    public bool Retry()
    {
        if(CurrentStage!=Stage.Down)return false;
        StopAllCoroutines();Retries++;ResetCheckpoint();StartFighting();return true;
    }
    public void Suspend()
    {
        SchoolEncounter=null;StopAllCoroutines();CurrentStage=Stage.Inactive;
        if(weapons)weapons.Suspend();if(hud)hud.SetActive(false);if(downPanel)downPanel.SetActive(false);if(hurtOverlay)hurtOverlay.color=Color.clear;
        if(bawan)bawan.speaking=false;if(pihu)pihu.speaking=false;
    }
    void Update()
    {
        if(CurrentStage==Stage.Inactive)return;
        if(warningUntil>0&&Time.time>=warningUntil){warningUntil=0;ui.Say("",null);}
        playerTarget.position=player.view.transform.position-Vector3.up*.38f;
        if(CurrentStage==Stage.Down)
        {if(Keyboard.current?.rKey.wasPressedThisFrame==true)Retry();return;}
        ui.ConfigureVR(player.IsVR);
        healthText.text="體力  "+Health+" / 100";
        ammoText.text=weapons.Selected==Chapter3Weapons.Weapon.Rifle ? "村田十八年式  "+(weapons.Loaded?"1":"0")+" / 1   備彈 "+weapons.Reserve : "獵刀";
        crosshair.text=Fighting?(weapons.Aiming?"·":"+"):"";
        string progress="前方巡警  "+DefeatedCount+" / "+enemyTemplates.Length;
        if(Fighting)
        {
            player.speed=weapons.Aiming?1.7f:2.8f;
            bool near=Vector3.Distance(player.transform.position,ammoPoint.position)<2.2f;
            if(near&&player.ActionPressed)weapons.Refill();
            string warning=Enemies.Exists(e=>e&&!e.Defeated&&e.Telegraphing)?"警察正在瞄準，移到岩石或木箱後！":"利用掩體接近敵人";
            combatText.text=weapons.Reloading?"單發裝填  "+Mathf.CeilToInt(weapons.ReloadProgress*100)+"%":!string.IsNullOrEmpty(weapons.Feedback)?weapons.Feedback:progress+"\n"+warning;
            ui.hint.text=near?"E 補充備彈   ｜   1 獵刀 · 2 步槍 · 左鍵攻擊 · 右鍵瞄準 · R 裝填":"WASD 移動 · 1 獵刀 · 2 步槍 · 左鍵攻擊 · 右鍵瞄準 · R 裝填 · Esc 放開滑鼠";
            if(DefeatedCount==enemyTemplates.Length&&!clearedRoutine){clearedRoutine=true;StartCoroutine(ClearEncounter());}
        }
        hurtOverlay.color=new Color(.6f,.025f,.01f,Mathf.Clamp01(1-(Time.time-hurtAt)/.7f)*.32f);
    }
    public void Damage(int amount,Vector3 source)
    {
        if(SchoolEncounter){SchoolEncounter.Damage(amount,source);return;}
        if(!Fighting)return;
        Health=Mathf.Max(0,Health-amount);hurtAt=Time.time;
        if(Health>0)return;
        CurrentStage=Stage.Down;player.canMove=player.canLook=false;weapons.Suspend();player.view.fieldOfView=60;
        downPanel.SetActive(true);ui.hint.text="R 從交戰前重試";ui.objective.text="你受傷倒下了";
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void EnemyDefeated(){if(!SchoolEncounter)DefeatedCount++;}
    public void CompanionWarning(string speaker,string line,float duration)
    {
        if(SchoolEncounter){SchoolEncounter.Notice(speaker,line,duration);return;}
        if(!Fighting)return;
        warningUntil=Time.time+duration;ui.Say(speaker,line);
    }
    IEnumerator ClearEncounter()
    {
        CurrentStage=Stage.Cleared;warningUntil=0;ui.Say("",null);weapons.Suspend();player.canMove=false;player.speed=2.8f;player.view.fieldOfView=60;
        yield return new WaitForSeconds(2);
        yield return Say(bawan,"前面的路通了。比胡，看看大家有沒有跟上。",lineSeconds);
        yield return Say(pihu,"我在。走吧，到學校外和達多會合。",lineSeconds);
        // Face up the same road as the police encounter. Bring the leader into
        // that view, then let him lead towards the school instead of following behind.
        Vector3 forwardView=exitPoint.position+Vector3.up*1.4f;
        forwardView.z=Mathf.Max(forwardView.z,player.transform.position.z+8);
        player.FocusOn(forwardView);
        ui.objective.text="與同伴會合，準備繼續向前";combatText.text="巴萬正在前方集合，比胡會跟上。";
        Vector3 rally=new Vector3(0,0,Mathf.Clamp(player.transform.position.z+3,-3,25));
        Vector3 rear=rally+new Vector3(1.2f,0,-1.7f);
        while(Flat(bawan.transform.position,rally)>.18f||Flat(pihu.transform.position,rear)>.18f)
        {leaderMotor.Move(bawan,rally,3.2f);rearMotor.Move(pihu,rear,3.4f);yield return null;}
        bawan.Face(exitPoint.position);pihu.Face(exitPoint.position);
        player.canMove=player.canLook=true;ui.objective.text="跟隨巴萬向前走，前往學校";combatText.text="巡警已退下，巴萬在前方帶路。";
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        while(Flat(player.transform.position,exitPoint.position)>2.5f||Flat(bawan.transform.position,exitPoint.position)>.35f||Flat(pihu.transform.position,exitPoint.position)>2.3f)
        {
            EscortWaiting=Flat(player.transform.position,bawan.transform.position)>5.5f;
            ui.hint.text=EscortWaiting?"巴萬在前方等你，走近後繼續。":"跟上巴萬 · WASD 移動 · 按住滑鼠右鍵環視";
            if(!EscortWaiting)leaderMotor.Move(bawan,exitPoint.position,1.85f);
            else bawan.Face(player.transform.position);
            rearMotor.Move(pihu,bawan.transform.position+new Vector3(1.2f,0,-1.7f),2.3f);
            yield return null;
        }
        EscortWaiting=false;
        player.canMove=player.canLook=false;
        for(float t=0;t<1.4f;t+=Time.deltaTime){ui.fade.color=new Color(0,0,0,t/1.4f);yield return null;}
        ui.fade.color=Color.black;hud.SetActive(false);ui.Say("",null);ui.hint.text="";CurrentStage=Stage.Complete;
        if(flow&&flow.school){flow.EnterSchool();yield break;}
        var texts=ui.ending.GetComponentsInChildren<Text>();texts[0].text="繼續前往霧社";texts[1].text="巴萬與比胡都在隊伍中。\n\n下一段：學校與運動會場";
        ui.replay.GetComponentInChildren<Text>().text="重玩第三章";ui.ending.SetActive(true);
        // Batch 3 continues here; never write chapter completion or Pihu's later outcome.
    }
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    IEnumerator Say(Chapter2Actor actor,string text,float duration)
    {
        ui.Say(actor.DisplayName,text);actor.speaking=true;actor.Face(player.transform.position);player.FocusOn(actor.transform.position+Vector3.up*1.2f);
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(text,duration,null);
        yield return Chapter3Flow.WaitForLine(duration);actor.speaking=false;ui.Say("",null);
    }
    public bool Trace(Vector3 origin,Vector3 direction,float distance,out RaycastHit result)
    {
        var hits=Physics.RaycastAll(origin,direction,distance,~0,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.collider.transform.IsChildOf(player.transform)||hit.collider.transform.IsChildOf(weapons.transform))continue;
            result=hit;return true;
        }
        result=default;return false;
    }
    public bool ClearLine(Vector3 from,Vector3 to,Chapter3Enemy source)
    {
        Vector3 d=to-from;
        foreach(var hit in Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.transform.IsChildOf(player.transform)||hit.collider.transform.IsChildOf(source.transform)||
                (source.escortTarget&&hit.collider.transform.IsChildOf(source.escortTarget)))continue;
            return false;
        }
        return true;
    }
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        desired.x=Mathf.Clamp(desired.x,-7,7);desired.z=Mathf.Clamp(desired.z,-12,30);return desired;
    }
}
