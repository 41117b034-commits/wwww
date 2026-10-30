using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    int schoolEnteredFrame=-1;
    Text shortcutLegend;
    bool stationStarted;
    public Chapter3StationLink StationLink {get;private set;}
    void Awake()
    {
        activeFlow=this;
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
        foreach(var label in ui.canvas.GetComponentsInChildren<Text>(true))
            if(label.text.Contains("F6")&&label.text.Contains("F7")){shortcutLegend=label;break;}
        UpdateShortcutLegend();
        if(startAtStation&&StationLink)StationLink.Preview();
        // One persistent camera, presentation, listener and EventSystem for every region.
        ui.replay.onClick.RemoveAllListeners();ui.replay.onClick.AddListener(()=>SceneManager.LoadScene("第三章"));
        ui.menu.onClick.RemoveAllListeners();ui.menu.onClick.AddListener(()=>SceneManager.LoadScene("選擇章節ˊ"));
    }
    void UpdateShortcutLegend()
    {
        if(shortcutLegend)shortcutLegend.text=Region=="School"
            ?"空白鍵 略過當句台詞 · F8 重進運動會\nF6 駐在所 · F7 短交戰"
            :"空白鍵 前往運動會（交談輸入時除外）\nF6 駐在所 · F7 短交戰 · F8 運動會";
    }
    void Update()
    {
        var k=Keyboard.current;if(k==null)return;
        // Function keys never insert letters into the opening conversation / IME.
        if(k.f6Key.wasPressedThisFrame)EnterStation();
        else if(k.f7Key.wasPressedThisFrame)EnterCombat();
        else if(k.f8Key.wasPressedThisFrame)EnterSchool();
        else if(k.spaceKey.wasPressedThisFrame&&CanUseSchoolShortcut)EnterSchool();
    }
    public bool CanUseSchoolShortcut => Region!="School"&&school&&schoolRegion&&
        !station.RefusalCommitted&&!TextInputOwnsSpace();
    bool TextInputOwnsSpace()
    {
        // Keep spaces / IME candidates in the conversation, including while
        // its request is pending and the text field temporarily loses focus.
        if(ui.chat&&ui.chat.activeInHierarchy)return true;
        if(ui.input&&ui.input.isActiveAndEnabled&&ui.input.isFocused)return true;
        var selected=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
        if(!selected||!selected.activeInHierarchy)return false;
        var legacy=selected.GetComponentInParent<InputField>();
        if(legacy&&legacy.isActiveAndEnabled)return true;
        var tmp=selected.GetComponentInParent<TMP_InputField>();
        return tmp&&tmp.isActiveAndEnabled;
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
        player.view.fieldOfView=60;player.speed=2.8f;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void EnterStation()
    {
        if(station.RefusalCommitted)return;
        LeaveCurrent();Region="Station";Transitions++;stationRegion.SetActive(true);
        UpdateShortcutLegend();
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
        UpdateShortcutLegend();
    }
    public void ContinueFromStation()
    {
        Vector3 leader=station.bawan.transform.position,rear=station.pihu.transform.position;
        Vector3 leaderFacing=station.bawan.facing,rearFacing=station.pihu.facing;
        station.Suspend();station.bawan.gameObject.SetActive(false);station.pihu.gameObject.SetActive(false);
        Region="Combat";Transitions++;
        combat.bawan.gameObject.SetActive(true);combat.pihu.gameObject.SetActive(true);
        combat.BeginFromStation(leader,rear,leaderFacing,rearFacing);UpdateShortcutLegend();
    }
    public void CompleteSchoolApproach()
    {
        if(!Approach||!Approach.Active||Region!="Combat")return;
        // The camera, player and both walking actors stay at their current
        // positions. The existing school cast takes over in the same poses.
        Vector3 leader=combat.bawan.transform.position,rear=combat.pihu.transform.position;
        Vector3 leaderFacing=combat.bawan.facing,rearFacing=combat.pihu.facing;
        schoolEnteredFrame=Time.frameCount;Region="School";Transitions++;
        combat.Suspend();combatRegion.SetActive(false);stationRegion.SetActive(false);
        school.BeginFromApproach(leader,rear,leaderFacing,rearFacing);
        Approach.Finish();UpdateShortcutLegend();
    }
    public void EnterSchool()
    {
        if(station.RefusalCommitted)return;
        if(!school||!schoolRegion)return;
        schoolEnteredFrame=Time.frameCount;
        LeaveCurrent();Region="School";Transitions++;schoolRegion.SetActive(true);school.Begin();
        UpdateShortcutLegend();
    }
    void OnDestroy(){if(activeFlow==this)activeFlow=null;}
    public static IEnumerator WaitForLine(float seconds)
    {
        float elapsed=0;
        while(elapsed<seconds)
        {
            // Before school Space belongs to the region shortcut, never to an
            // outgoing dialogue coroutine. At school it only advances a line,
            // and the same keypress that entered school cannot advance one.
            bool canSkip=!activeFlow||(activeFlow.Region=="School"&&
                activeFlow.schoolEnteredFrame!=Time.frameCount&&!activeFlow.TextInputOwnsSpace());
            if(elapsed>.25f&&canSkip&&Keyboard.current?.spaceKey.wasPressedThisFrame==true)break;
            elapsed+=Time.deltaTime;yield return null;
        }
    }
}
