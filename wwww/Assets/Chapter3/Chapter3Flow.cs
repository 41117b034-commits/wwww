using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Regions are serialized together in 第三章.unity. Switching never loads another map.
public sealed class Chapter3Flow : MonoBehaviour
{
    public GameObject villageRegion, stationRegion, combatRegion, schoolRegion;
    public Chapter3Controller village;
    public Chapter3StationController station;
    public Chapter3Combat combat;
    public Chapter3SchoolController school;
    public Chapter3Player player;
    public Chapter3Presentation ui;
    public bool startAtStation;
    public Chapter3Weapons.Weapon? CarriedWeapon { get; private set; }
    public void Carry(Chapter3Weapons.Weapon weapon) { CarriedWeapon=weapon; }
    public void ClearEquipment() { CarriedWeapon=null; }
    public string Region { get; private set; } = "Village";
    public int Transitions { get; private set; }
    public Chapter3SchoolApproach Approach { get; private set; }
    static Chapter3Flow activeFlow;
    bool stationStarted;
    public Chapter3StationLink StationLink {get;private set;}
    public Chapter3DialogueView DialogueView {get;private set;}
    void Awake()
    {
        activeFlow=this;
        DialogueView=gameObject.AddComponent<Chapter3DialogueView>();DialogueView.Configure(this);ui.dialogueView=DialogueView;
        ui.voice=gameObject.AddComponent<Chapter3SeediqVoice>();ui.voice.Flow=this;
        if(village)village.flow=this;station.flow=this;combat.flow=this;
        if(villageRegion)villageRegion.SetActive(!startAtStation);
        stationRegion.SetActive(startAtStation);combatRegion.SetActive(false);
        if(startAtStation){Region="Station";stationStarted=true;}
        if(schoolRegion)schoolRegion.SetActive(false);
        if(school)school.flow=this;
        if(school&&schoolRegion)
        {
            Approach=gameObject.AddComponent<Chapter3SchoolApproach>();
            combatRegion.transform.position=new Vector3(0,0,Chapter3ApproachEnvironment.CombatOriginZ);
            StationLink=gameObject.AddComponent<Chapter3StationLink>();StationLink.Build(this);
            Approach.Initialize(this);
        }
    }
    void Start()
    {
        // Retire the old authoring overlay even in an older saved scene.
        foreach(var label in ui.canvas.GetComponentsInChildren<Text>(true))
            if(label.text.Contains("F6")&&label.text.Contains("F7"))
            {label.text="";label.gameObject.SetActive(false);}
        if(startAtStation&&StationLink)StationLink.Preview();
        // One persistent camera, presentation, listener and EventSystem for every region.
        ui.replay.onClick.RemoveAllListeners();ui.replay.onClick.AddListener(()=>SceneManager.LoadScene("第三章"));
        ui.menu.onClick.RemoveAllListeners();ui.menu.onClick.AddListener(ReturnToChapterSelection);
    }
    public static void ReturnToChapterSelection()
    {
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        SceneManager.LoadScene("選擇章節ˊ",LoadSceneMode.Single);
    }
    void LeaveCurrent()
    {
        if(Approach)Approach.Suspend();
        Chapter3PickupArrow.Hide(ui);
        if(village)village.Suspend();station.Suspend();combat.Suspend();
        if(school)school.Suspend();
        ui.input.DeactivateInputField();ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);ui.hint.text="";
        if(villageRegion)villageRegion.SetActive(false);stationRegion.SetActive(false);combatRegion.SetActive(false);
        if(schoolRegion)schoolRegion.SetActive(false);
        player.chapter=null;player.station=null;player.combat=null;player.school=null;
        player.view.fieldOfView=60;player.speed=Chapter3Player.WalkSpeed;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void EnterStation()
    {
        if(station.RefusalCommitted)return;
        LeaveCurrent();Region="Station";Transitions++;stationRegion.SetActive(true);
        if(stationStarted)station.Begin();stationStarted=true;
        if(StationLink)StationLink.Preview();
        // On its first activation Start() performs the initialization.
    }
    public void EnterCombat()
    {
        if(station.RefusalCommitted)return;
        LeaveCurrent();Region="Combat";Transitions++;combatRegion.SetActive(true);
        combat.bawan.gameObject.SetActive(true);combat.pihu.gameObject.SetActive(true);
        foreach(var light in combatRegion.GetComponentsInChildren<Light>(true))light.enabled=true;
        if(Approach)Approach.Prepare();
        combat.Begin();
    }
    public void ContinueFromStation()
    {
        Vector3 leader=station.bawan.transform.position,rear=station.pihu.transform.position;
        Vector3 leaderFacing=station.bawan.facing,rearFacing=station.pihu.facing;
        station.Suspend();station.bawan.gameObject.SetActive(false);station.pihu.gameObject.SetActive(false);
        Region="Combat";Transitions++;
        combat.bawan.gameObject.SetActive(true);combat.pihu.gameObject.SetActive(true);
        combat.BeginFromStation(leader,rear,leaderFacing,rearFacing);
    }
    public void CompleteSchoolApproach()
    {
        if(!Approach||!Approach.Active||Region!="Combat")return;
        // The camera, player and both walking actors stay at their current
        // positions. The existing school cast takes over in the same poses.
        Vector3 leader=combat.bawan.transform.position,rear=combat.pihu.transform.position;
        Vector3 leaderFacing=combat.bawan.facing,rearFacing=combat.pihu.facing;
        Region="School";Transitions++;
        combat.Suspend();combatRegion.SetActive(false);stationRegion.SetActive(false);
        school.BeginFromApproach(leader,rear,leaderFacing,rearFacing);
        Approach.Finish();
    }
    public void EnterSchool()
    {
        if(station.RefusalCommitted)return;
        if(!school||!schoolRegion)return;
        LeaveCurrent();Region="School";Transitions++;schoolRegion.SetActive(true);school.Begin();
    }
    void OnDestroy(){if(activeFlow==this)activeFlow=null;}
    public static IEnumerator WaitForLine(float seconds)
    {
        float elapsed=0,voiceTail=0;
        var voice=activeFlow&&activeFlow.ui ? activeFlow.ui.voice : null;
        // A recorded line ends with its audio. The old subtitle estimate is
        // only a fallback for lines without a recording, never extra silence.
        bool voiced=voice&&(voice.Busy||voice.HasAudio);
        while(true)
        {
            bool busy=voice&&voice.Busy;
            if(voiced&&voice&&voice.HasAudio)
            {
                voiceTail=busy?0:voiceTail+Time.deltaTime;
                if(!busy&&voiceTail>=.12f)break;
            }
            else if(!busy&&elapsed>=seconds)break;
            elapsed+=Time.deltaTime;yield return null;
        }
    }
}
