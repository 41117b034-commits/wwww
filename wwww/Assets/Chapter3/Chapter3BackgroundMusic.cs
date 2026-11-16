using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Standalone BGM only. Reads story state without changing dialogue or progression.
[DisallowMultipleComponent, DefaultExecutionOrder(9400)]
public sealed class Chapter3BackgroundMusic : MonoBehaviour
{
    public enum Cue { Village, Station, Refusal, TrailCombat, SchoolApproach, SchoolCalm, Briefing, MissionChoice, SchoolBattle, RescueChoice, RescueEscort, LeaveCompanion, SurvivorEnding, LossEnding, RollCall, Complete }
    [Header("掛有 Chapter3Flow 的物件；同物件可留空自動尋找")]
    public Chapter3Flow flow;
    [Header("背景音樂（留空則該段淡出，可共用同一首）")]
    public AudioClip villageMusic;
    public AudioClip stationMusic, refusalMusic, trailCombatMusic, schoolApproachMusic, schoolCalmMusic;
    public AudioClip briefingMusic, missionChoiceMusic, schoolBattleMusic, rescueChoiceMusic;
    public AudioClip rescueEscortMusic, leaveCompanionMusic, survivorEndingMusic, lossEndingMusic, rollCallMusic;
    [Header("音量與轉場")]
    [Range(0,1)] public float musicVolume=.3f;
    [Tooltip("有人說話時的音樂倍率；1 表示不降低音量。")]
    [Range(0,1)] public float duckFactor=.3f;
    [Min(0)] public float musicFadeSeconds=1f;
    public AudioMixerGroup musicOutput;
    [SerializeField] Cue currentMusicCue;
    public Cue CurrentMusicCue=>currentMusicCue;
    AudioSource music;
    AudioClip requestedMusic;
    Coroutine transition;
    Chapter3Ending ending;
    float envelope,duck=1;
    void Awake()
    {
        if(!flow)flow=GetComponent<Chapter3Flow>();
        if(!flow)flow=GetComponentInParent<Chapter3Flow>();
        var child=new GameObject("Chapter3 Background Music");child.transform.SetParent(transform,false);
        music=child.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;
        music.volume=0;
    }
    void Start()
    {
        if(!flow){Debug.LogError("[Chapter3BGM] 請指定 Chapter3Flow，或掛在相同物件。",this);enabled=false;}
    }
    void LateUpdate()
    {
        if(!flow||!music)return;
        currentMusicCue=SelectCue();AudioClip clip=ClipFor(currentMusicCue);
        if(requestedMusic!=clip||(music.clip!=clip&&transition==null))
        {
            requestedMusic=clip;if(transition!=null)StopCoroutine(transition);
            transition=StartCoroutine(ChangeMusic(clip));
        }
        bool speaking=flow.ui&&flow.ui.voice&&(flow.ui.voice.Speaking||flow.ui.voice.Busy);
        duck=Mathf.MoveTowards(duck,speaking?duckFactor:1,Time.unscaledDeltaTime*4);
        music.volume=musicVolume*envelope*duck;music.outputAudioMixerGroup=musicOutput;
    }
    Cue SelectCue()
    {
        if(flow.Region=="Station")
        {
            var stage=flow.station.CurrentStage;
            return stage==Chapter3StationController.Stage.Refused||stage==Chapter3StationController.Stage.Ended?Cue.Refusal:Cue.Station;
        }
        if(flow.Region=="Combat")
        {
            if(flow.Approach&&flow.Approach.Active)return Cue.SchoolApproach;
            return Cue.TrailCombat;
        }
        if(flow.Region!="School"||!flow.school)return Cue.Village;
        if(!ending)ending=flow.school.GetComponent<Chapter3Ending>();
        if(ending&&ending.Active)
        {
            if(ending.CurrentStage==Chapter3Ending.Stage.Complete)return Cue.Complete;
            if(ending.CurrentStage==Chapter3Ending.Stage.RollCall||ending.CurrentStage==Chapter3Ending.Stage.MonaClosing)return Cue.RollCall;
            return ending.Result!=null&&ending.Result.pihuSurvived?Cue.SurvivorEnding:Cue.LossEnding;
        }
        var battle=flow.school.Battle;
        if(!battle)return Cue.SchoolCalm;
        var rescue=battle.Rescue;
        if(rescue&&rescue.Active)
        {
            if(rescue.CurrentStage==Chapter3Rescue.Stage.Injury||rescue.CurrentStage==Chapter3Rescue.Stage.Choice)return Cue.RescueChoice;
            if(rescue.Selected==Chapter3Rescue.Decision.Leave)return Cue.LeaveCompanion;
            return Cue.RescueEscort;
        }
        switch(battle.CurrentStage)
        {
            case Chapter3SchoolBattle.Stage.Observing:return Cue.Briefing;
            case Chapter3SchoolBattle.Stage.Choice:return Cue.MissionChoice;
            case Chapter3SchoolBattle.Stage.Outbreak:
            case Chapter3SchoolBattle.Stage.Fighting:
            case Chapter3SchoolBattle.Stage.Down:
            case Chapter3SchoolBattle.Stage.Regroup:return Cue.SchoolBattle;
            default:return Cue.SchoolCalm;
        }
    }
    AudioClip ClipFor(Cue cue)
    {
        switch(cue)
        {
            case Cue.Village:return villageMusic;case Cue.Station:return stationMusic;
            case Cue.Refusal:return refusalMusic;case Cue.TrailCombat:return trailCombatMusic;
            case Cue.SchoolApproach:return schoolApproachMusic;case Cue.SchoolCalm:return schoolCalmMusic;
            case Cue.Briefing:return briefingMusic;case Cue.MissionChoice:return missionChoiceMusic;
            case Cue.SchoolBattle:return schoolBattleMusic;case Cue.RescueChoice:return rescueChoiceMusic;
            case Cue.RescueEscort:return rescueEscortMusic;case Cue.LeaveCompanion:return leaveCompanionMusic;
            case Cue.SurvivorEnding:return survivorEndingMusic;case Cue.LossEnding:return lossEndingMusic;
            case Cue.RollCall:return rollCallMusic;default:return null;
        }
    }
    IEnumerator ChangeMusic(AudioClip clip)
    {
        float duration=Mathf.Max(.01f,musicFadeSeconds*.5f),start=envelope;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime){envelope=Mathf.Lerp(start,0,t/duration);yield return null;}
        envelope=0;music.Stop();music.clip=clip;
        if(clip)
        {
            music.Play();
            for(float t=0;t<duration;t+=Time.unscaledDeltaTime){envelope=t/duration;yield return null;}
            envelope=1;
        }
        transition=null;
    }
    public void SetMusicVolume(float value){musicVolume=Mathf.Clamp01(value);}
    void OnDisable()
    {
        StopAllCoroutines();transition=null;requestedMusic=null;envelope=0;duck=1;
        if(music){music.Stop();music.clip=null;music.volume=0;}
    }
    void OnDestroy(){if(music)Destroy(music.gameObject);}
}
