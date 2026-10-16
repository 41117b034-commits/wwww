using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Temporary isolated audio-output check, never a production component.
// Measures the Unity AudioListener output buffer; this is not native speaker
// capture, human listening, headset evidence, or a review of the complete mix.
// Other sources are temporarily muted to attribute energy to one runtime cue.
// School mission/route prerequisites are injected; its ordinary Update triggers
// the covering shot. Rifle/knife actions use the existing public Attack method.
public static class Chapter3Batch7AudioChecks
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    sealed class Window
    {
        public string name;public float peak;public double squares,dspSeconds;public int samples,frames;
        public double Rms=>samples>0?Math.Sqrt(squares/samples):0;
        public bool HasSignal=>peak>0.000001f;
        public override string ToString()=>name+": peak="+peak.ToString("G6")+", rms="+Rms.ToString("G6")+
            ", frames="+frames+", samples="+samples+", DSP seconds="+dspSeconds.ToString("F3");
    }
    public static IEnumerator Run(Chapter3Flow flow,Action<string,bool> check,Action<string> observation)
    {
        if(AudioListener.pause||AudioListener.volume<=0||EditorAudioMuted())
        {observation("INCONCLUSIVE audio capture: listener/editor audio is paused or muted; user audio settings were left unchanged.");yield break;}
        var listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
            .Where(l=>l.isActiveAndEnabled).ToArray();
        if(listeners.Length!=1)
        {observation("INCONCLUSIVE audio capture: expected one active listener, found "+listeners.Length+".");yield break;}
        float scale=Time.timeScale,lines=flow.combat.lineSeconds;
        var carried=flow.CarriedWeapon;
        var mutes=new Dictionary<AudioSource,bool>();
        var ambientWindow=new Window{name="school ambience isolated listener output"};
        var rifleWindow=new Window{name="player rifle isolated listener output"};
        var knifeWindow=new Window{name="knife action isolated listener output"};
        var coverWindow=new Window{name="Bawan covering shot isolated listener output"};
        bool originalTargetMuted=false;
        try
        {
            Time.timeScale=1;
            flow.EnterSchool();yield return null;
            var ambience=flow.schoolRegion.GetComponentsInChildren<AudioSource>(true)
                .FirstOrDefault(source=>source.isActiveAndEnabled&&source.loop&&source.clip);
            check("school ambience has an active loop source",ambience);
            RememberMutes(mutes);
            if(ambience.mute)originalTargetMuted=true;
            Isolate(mutes,ambience);yield return new WaitForSecondsRealtime(.15f);
            yield return Sample(ambientWindow,1);
            observation(ambientWindow.ToString());

            flow.Carry(Chapter3Weapons.Weapon.Rifle);flow.combat.lineSeconds=.1f;flow.EnterCombat();
            var combat=flow.combat;
            check("audio fixture enters the real combat region and initializes its checkpoint",
                flow.Region=="Combat"&&combat.isActiveAndEnabled&&combat.CurrentStage==Chapter3Combat.Stage.Intro&&combat.Enemies.Count>0);
            // Audio sampling does not validate the introduction. EnterCombat has
            // already run the real checkpoint/equipment setup; skip only its
            // scaled-time fade/dialogue coroutine and use its normal fight entry.
            // A five-second realtime deadline does not reliably bound a
            // scaled-time cutscene on a busy Editor.
            combat.StopAllCoroutines();
            typeof(Chapter3Combat).GetMethod("StartFighting",Hidden).Invoke(combat,null);
            yield return null; // Run enemy Start before isolating their sources.
            yield return Wait(()=>combat.Fighting&&combat.weapons.Ready,3,"initialized combat weapon ready");
            observation("Audio-only combat setup: normal EnterCombat checkpoint initialization, followed by its existing StartFighting method; introductory fade/dialogue bypassed. This mode does not validate the combat intro timing.");
            var weapon=combat.weapons;
            foreach(var guard in combat.Enemies)if(guard)guard.HoldFire(20);
            flow.player.FocusOn(flow.player.view.transform.position+Vector3.up*15);
            RememberMutes(mutes);
            check("rifle action has its actual runtime audio source",weapon.audioSource&&weapon.shotClip);
            if(mutes[weapon.audioSource])originalTargetMuted=true;
            Isolate(mutes,weapon.audioSource);weapon.audioSource.Stop();
            yield return new WaitForSecondsRealtime(.15f);
            int shots=weapon.Shots;
            check("real player rifle Attack emits one shot",weapon.Attack()&&weapon.Shots==shots+1);
            yield return Sample(rifleWindow,1.2f);
            observation(rifleWindow.ToString());

            // Current Slash has no dedicated sound trigger. Do not mistake
            // background ambience for proof that a knife sound exists.
            flow.Carry(Chapter3Weapons.Weapon.Knife);weapon.ResetEquipment();
            weapon.audioSource.Stop();Isolate(mutes,weapon.audioSource);
            yield return new WaitForSecondsRealtime(.15f);
            int slashes=weapon.Slashes;
            check("real knife Attack executes during output sampling",weapon.Attack()&&weapon.Slashes==slashes+1);
            yield return Sample(knifeWindow,.85f);
            observation(knifeWindow+". Current Slash/knife impact and reload have no dedicated audio trigger; this is not a knife-sound pass.");

            flow.Carry(Chapter3Weapons.Weapon.Rifle);flow.EnterSchool();yield return null;
            var school=flow.school;var battle=school.Battle;
            school.StopAllCoroutines();
            // Short audio test only: bypass the long briefing/walk while keeping
            // runtime mission setup, line-of-sight, shot timing and audio intact.
            Set(battle,"<Selected>k__BackingField",Chapter3SchoolBattle.Mission.Bawan);
            typeof(Chapter3SchoolBattle).GetMethod("ResetMission",Hidden).Invoke(battle,null);
            yield return null; // Enemy Start and cooperation AudioSource creation.
            var cover=(AudioSource)Get(battle,"bawanCoverAudio");
            check("cooperation owns its runtime covering audio source",cover);
            foreach(var guard in battle.Enemies)if(guard)guard.HoldFire(12);
            RememberMutes(mutes);
            if(mutes[cover])originalTargetMuted=true;
            Isolate(mutes,cover);yield return new WaitForSecondsRealtime(.15f);
            var route=(Vector3[])Get(battle,"activeRoute");Set(battle,"guideIndex",route.Length);
            school.bawan.transform.position=new Vector3(-28.15f,0,13);
            flow.player.Warp(new Vector3(-27,.08f,12.9f),battle.Enemies[0].AimPoint);
            int bursts=battle.BawanCoverBursts;
            // Sample before the normal Update emits its shot, so the transient
            // cannot finish during a separate wait for the event counter.
            yield return Sample(coverWindow,2.5f);
            check("ordinary cooperation Update emitted a covering shot",battle.BawanCoverBursts>bursts);
            observation(coverWindow.ToString());

            bool dspAdvanced=ambientWindow.dspSeconds>.1&&rifleWindow.dspSeconds>.1&&coverWindow.dspSeconds>.1;
            bool anySignal=ambientWindow.HasSignal||rifleWindow.HasSignal||coverWindow.HasSignal;
            if(originalTargetMuted||!dspAdvanced||!anySignal)
                observation("INCONCLUSIVE audible-output checks: a target was originally muted, DSP did not advance, or every output window was zero. Editor focus/output-device state can suppress capture; no settings were forced and zero buffers are not declared a runtime defect.");
            else
            {
                check("school ambience reaches isolated listener output",ambientWindow.HasSignal);
                check("player rifle cue reaches isolated listener output",rifleWindow.HasSignal);
                check("Bawan cooperation cue reaches isolated listener output",coverWindow.HasSignal);
            }
            observation("Audio boundary: isolated AudioListener buffers from Editor Play Mode, public weapon triggers, injected mission/route prerequisites and enemy HoldFire. No native audio-device recording, human listening, spatial-mix balance, headset, full soundtrack or dedicated knife/reload sound claim.");
        }
        finally
        {
            foreach(var entry in mutes)if(entry.Key)entry.Key.mute=entry.Value;
            if(flow){flow.combat.lineSeconds=lines;if(carried.HasValue)flow.Carry(carried.Value);else flow.ClearEquipment();}
            Time.timeScale=scale;
        }
    }
    static void RememberMutes(Dictionary<AudioSource,bool> saved)
    {
        foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(!saved.ContainsKey(source))saved[source]=source.mute;
    }
    static void Isolate(Dictionary<AudioSource,bool> saved,AudioSource target)
    {
        foreach(var source in saved.Keys.ToArray())if(source)source.mute=source==target?saved[source]:true;
    }
    static IEnumerator Sample(Window window,float seconds)
    {
        var buffer=new float[2048];double dsp=AudioSettings.dspTime;
        float until=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<until)
        {
            AudioListener.GetOutputData(buffer,0);window.frames++;
            foreach(float value in buffer){window.peak=Mathf.Max(window.peak,Mathf.Abs(value));window.squares+=(double)value*value;window.samples++;}
            yield return null;
        }
        window.dspSeconds=AudioSettings.dspTime-dsp;
    }
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {
        float until=Time.realtimeSinceStartup+seconds;
        while(!condition()&&Time.realtimeSinceStartup<until)yield return null;
        if(!condition())throw new Exception("Timed out: "+label);
    }
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Hidden).SetValue(owner,value);
    static bool EditorAudioMuted()
    {
#if UNITY_EDITOR
        var property=typeof(UnityEditor.EditorUtility).GetProperty("audioMasterMute",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        return property!=null&&property.PropertyType==typeof(bool)&&(bool)property.GetValue(null);
#else
        return false;
#endif
    }
}
