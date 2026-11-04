using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
public static partial class Chapter3ForestVerify
{
    static IEnumerator BeforeSchool()
    {
        var s=f.station;var p=f.player;var c=f.combat;
        report.observations.Add("Formal start through station, weapon pickup, continuous mountain and school approach. Controller-driven walking; mountain weapon tests use explicitly positioned aim fixtures with enemy HoldFire. School clearing fixture remains direct damage. No scene skip.");
        yield return Wait(()=>s.CurrentLine!=null,15,"station opening");
        float until=Time.realtimeSinceStartup+210;
        while(s.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("Station walk/briefing stuck "+s.CurrentStage+" "+p.transform.position);
            if(Flat(p.transform.position,s.bawan.transform.position)>2.25f)StoryMove(s.bawan.transform.position);
            yield return null;
        }
        Check("natural station briefing reaches weapon choice",f.Region=="Station");
        Check("choose rifle",s.ChooseWeapon(Chapter3Weapons.Weapon.Rifle));
        yield return StoryWalk(s.rifleFocus.position+Vector3.back*1.7f,.25f,35);
        yield return Keys(Key.E,.15f);yield return Wait(()=>s.Readiness.Active&&!s.Readiness.WaitingForOpeningRelease,8,"E pickup readiness");
        Check("single rifle acquired",s.HasRifle&&!s.HasKnife);
        s.Readiness.replyTimeout=.01f;report.observations.Add("Readiness timeout fixture uses deterministic fallback to avoid local LLM dependency.");
        Check("ready answer submitted",s.Readiness.SubmitReply("我準備好了"));
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Departure,25,"ready departure");
        until=Time.realtimeSinceStartup+200;
        while(f.Region=="Station")
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("Station departure stuck "+s.Waypoint+" player="+p.transform.position+" B="+s.bawan.transform.position);
            if(Flat(p.transform.position,s.bawan.transform.position)>2.1f)StoryMove(s.bawan.transform.position);
            yield return null;
        }
        Check("station walks into continuous mountain encounter",f.Region=="Combat");
        yield return Wait(()=>c.Fighting,30,"mountain begins");
        foreach(var enemy in c.Enemies)enemy.HoldFire(180);
        foreach(var enemy in c.Enemies.ToArray())
        {
            p.Warp(enemy.transform.position+Vector3.back*3,enemy.AimPoint);yield return null;
            yield return Wait(()=>c.weapons.Ready,5,"rifle ready");
            if(!c.weapons.Loaded){Check("reload accepted",c.weapons.Reload());yield return Wait(()=>!c.weapons.Reloading,5,"reload done");}
            p.FocusOn(enemy.AimPoint);Check("rifle fired",c.weapons.Attack());yield return null;
            Check("actual shot defeats officer",enemy.Defeated);
        }
        yield return Wait(()=>f.Approach.Active,35,"forest school approach");
        until=Time.realtimeSinceStartup+210;
        while(f.Region=="Combat")
        {
            if(Time.realtimeSinceStartup>until)throw new Exception("School approach stuck player="+p.transform.position+" B="+c.bawan.transform.position);
            var path=f.Approach.WorldWaypoints;
            if(f.Approach.CurrentWaypoint==path.Length-1&&Flat(c.bawan.transform.position,path[path.Length-1])<.5f)
                StoryMove(path[path.Length-1]+Vector3.right*1.4f);
            else if(Flat(p.transform.position,c.bawan.transform.position)>2.1f)StoryMove(c.bawan.transform.position);
            yield return null;
        }
        Check("physical forest walk reaches school without shortcut",f.Region=="School");
        Check("Pihu finishes slope before flat school handoff",Chapter3PathMotor.Clear(f.school.pihu.transform.position,f.school.pihu.transform.position));
        yield return Wait(()=>f.school.Exploring,15,"school exploration");
    }
    static IEnumerator Voice()
    {
        yield return Wait(()=>f.station.CurrentLine!=null,15,"opening");f.station.StopAllCoroutines();f.ui.Say("",null);
        var voice=f.ui.voice;
        string line="前面有巡警。靠著岩石走，別站在路中央。";
        f.ui.Say(f.station.bawan.DisplayName,line);yield return null;yield return new WaitForEndOfFrame();
        Check("official cached voice plays with exact Chinese subtitle",voice.Speaking&&f.ui.subtitle.text==line);
        var source=(AudioSource)Get(voice,"output");
        var samples=new float[source.clip.samples*source.clip.channels];source.clip.GetData(samples,0);
        Check("official clip has audible samples",samples.Any(v=>Mathf.Abs(v)>.01f));
        Check("audio follows speaker mouth",f.station.bawan.speaking);
        float start=Time.time,peak=0;var face=f.station.bawan.GetComponent<Chapter2SpeechFace>();
        for(float t=0;t<2;t+=Time.deltaTime){if(face)peak=Mathf.Max(peak,face.MouthWeight);yield return null;}
        Check("recorded audio drives actual mouth shape",face&&peak>.02f&&(bool)Get(face,"recorded"));
        report.observations.Add("Mouth peak="+peak);
        yield return Chapter3Flow.WaitForLine(.1f);
        Check("line duration extends to complete audio",Time.time-start>=source.clip.length-.3f&&!voice.Speaking);
        f.ui.Say(f.station.bawan.DisplayName,line);yield return null;
        f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"F8");
        Check("shortcut stops old station voice",voice.CurrentLine!=line);
        // Uncached, harmless runtime line verifies MT + TTS + downloaded WAV,
        // the same path used for generated conversation responses.
        f.school.StopAllCoroutines();f.school.enabled=false;f.school.Battle.enabled=false;f.ui.Say("",null);
        const string dynamicLine="我們在這裡休息一下，然後再一起出發。";
        f.ui.Say(f.school.bawan.DisplayName,dynamicLine);
        yield return Wait(()=>voice.Speaking||!voice.Busy,70,"live official translation and TTS");
        Check("uncached dialogue produces official speech",voice.Speaking);
        report.observations.Add("Live translated text="+voice.LastTranslation+" clip="+source.clip.length+" seconds; no linguistic certification.");
        Check("Chinese subtitle remains unchanged",f.ui.subtitle.text==dynamicLine);
        f.ui.Say("",null);Check("clearing subtitle stops audio and request",!voice.Busy&&!voice.Speaking);
        f.ui.Say(f.school.bawan.DisplayName,"我們先在路邊等一下，再一起走。");yield return new WaitForSeconds(.15f);
        const string replacement="大家一起走，別停在這裡。";f.ui.Say(f.school.bawan.DisplayName,replacement);
        yield return Wait(()=>voice.Speaking||!voice.Busy,70,"replace in-flight request");
        report.observations.Add("Replacement line="+voice.CurrentLine+" translation="+voice.LastTranslation+" error="+voice.LastError);
        Check("replacing in-flight speech keeps latest line",voice.Speaking&&voice.CurrentLine==replacement&&f.ui.subtitle.text==replacement);
        f.ui.Say("",null);
        Check("queue response parser",Chapter3SeediqVoice.CompletedQueue("data: {\"msg\":\"process_completed\",\"success\":true,\"output\":{\"data\":[\"ok\"]}}\n")=="[\"ok\"]");
    }
}
