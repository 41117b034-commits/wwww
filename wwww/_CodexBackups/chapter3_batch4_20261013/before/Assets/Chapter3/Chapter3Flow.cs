using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public string Region { get; private set; } = "Village";
    public int Transitions { get; private set; }
    bool stationStarted;
    void Awake()
    {
        village.flow=this;station.flow=this;combat.flow=this;
        villageRegion.SetActive(true);stationRegion.SetActive(false);combatRegion.SetActive(false);
        if(schoolRegion)schoolRegion.SetActive(false);
        if(school)school.flow=this;
    }
    void Start()
    {
        // One persistent camera, presentation, listener and EventSystem for every region.
        ui.replay.onClick.RemoveAllListeners();ui.replay.onClick.AddListener(()=>SceneManager.LoadScene("第三章"));
        ui.menu.onClick.RemoveAllListeners();ui.menu.onClick.AddListener(()=>SceneManager.LoadScene("選擇章節ˊ"));
    }
    void Update()
    {
        var k=Keyboard.current;if(k==null)return;
        // Function keys never insert letters into the opening conversation / IME.
        if(k.f6Key.wasPressedThisFrame)EnterStation();
        else if(k.f7Key.wasPressedThisFrame)EnterCombat();
        else if(k.f8Key.wasPressedThisFrame)EnterSchool();
    }
    void LeaveCurrent()
    {
        Chapter3PickupArrow.Hide(ui);
        village.Suspend();station.Suspend();combat.Suspend();
        if(school)school.Suspend();
        ui.input.DeactivateInputField();ui.chat.SetActive(false);ui.ending.SetActive(false);ui.Say("",null);ui.hint.text="";
        villageRegion.SetActive(false);stationRegion.SetActive(false);combatRegion.SetActive(false);
        if(schoolRegion)schoolRegion.SetActive(false);
        player.chapter=null;player.station=null;player.combat=null;player.school=null;
        player.view.fieldOfView=60;player.speed=2.8f;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void EnterStation()
    {
        if(station.RefusalCommitted)return;
        LeaveCurrent();Region="Station";Transitions++;stationRegion.SetActive(true);
        if(stationStarted)station.Begin();stationStarted=true;
        // On its first activation Start() performs the initialization.
    }
    public void EnterCombat()
    {
        if(station.RefusalCommitted)return;
        LeaveCurrent();Region="Combat";Transitions++;combatRegion.SetActive(true);combat.Begin();
    }
    public void EnterSchool()
    {
        if(station.RefusalCommitted)return;
        if(!school||!schoolRegion)return;
        LeaveCurrent();Region="School";Transitions++;schoolRegion.SetActive(true);school.Begin();
    }
    public static IEnumerator WaitForLine(float seconds)
    {
        float elapsed=0;
        while(elapsed<seconds)
        {
            // Keep a short minimum so held/queued input cannot consume the next line.
            if(elapsed>.25f && Keyboard.current?.spaceKey.wasPressedThisFrame==true)break;
            elapsed+=Time.deltaTime;yield return null;
        }
    }
}
