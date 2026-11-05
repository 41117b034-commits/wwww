using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DialoguePacingVerify
{
    const string Dir="_CodexBackups/dialogue_pacing_20261104/",Key="DialoguePacingVerify.Run";
    static bool launched;
    static DialoguePacingVerify()
    {
        EditorApplication.update+=Tick;
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&SessionState.GetBool(Key,false)&&!launched){launched=true;var go=new GameObject("Temporary pacing verification");Object.DontDestroyOnLoad(go);go.AddComponent<DialoguePacingDriver>();}
        if(!File.Exists(Dir+"command.txt"))return;
        string command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        if(command=="refresh"){AssetDatabase.Refresh();return;}
        if((command=="run"||command=="performance")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&!SceneManager.GetActiveScene().isDirty){SessionState.SetString(Key+".mode",command);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        else if(command=="stop"){SessionState.EraseBool(Key);EditorApplication.isPlaying=false;}
        else File.WriteAllText(Dir+"command-error.txt","Expected clean compiled scene in Edit Mode");
    }
    [Serializable] sealed class Pref {public string key;public bool exists;public int value;}
    [Serializable] sealed class Report {public bool completed;public List<string> checks=new List<string>(),errors=new List<string>(),observations=new List<string>();public Pref[] originalPreferences;}
    static Report report;
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string name)=>o.GetType().GetField(name,Fields).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Fields).Invoke(o,args);
    static void Check(string label,bool valid){if(!valid)throw new Exception(label);report.checks.Add(label);Save();}
    static void Save(){File.WriteAllText(Dir+"report-progress.json",JsonUtility.ToJson(report,true));}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(text);}
    static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
    static IEnumerator Wait(Func<bool> condition,float timeout,string label){float end=Time.realtimeSinceStartup+timeout;while(!condition()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout "+label);yield return null;}}
    public static IEnumerator Run()
    {
        string[] keys={"WusheEvent.MasterVolume","WusheEvent.MasterVolumeChosen","WusheEvent.VoiceLanguage","WusheEvent.SubtitleLanguage"};
        report=new Report{originalPreferences=keys.Select(k=>new Pref{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray()};
        bool background=Application.runInBackground;Application.runInBackground=true;Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        foreach(var p in report.originalPreferences){if(p.exists)PlayerPrefs.SetInt(p.key,p.value);else PlayerPrefs.DeleteKey(p.key);}PlayerPrefs.Save();
        report.checks.Add("original preferences restored");report.completed=report.errors.Count==0;
        File.WriteAllText(Dir+(SessionState.GetString(Key+".mode","")=="performance"?"report-performance.json":"report-final.json"),JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseBool(Key);EditorApplication.isPlaying=false;
    }
    static IEnumerator Scenario()
    {
        if(SessionState.GetString(Key+".mode","")=="performance"){yield return Performance();yield break;}
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);
        // Verify voiced samples remain byte-for-byte in order across channels.
        var original=AudioClip.Create("stereo speech and silence fixture",4000,2,1000,false);
        var values=new float[8000];for(int i=400;i<900;i++){values[i*2]=.2f;values[i*2+1]=-.3f;}for(int i=2100;i<2800;i++){values[i*2]=-.4f;values[i*2+1]=.5f;}
        original.SetData(values,0);var paced=ChineseSpeechPacing.Prepare(original);var after=new float[paced.samples*2];paced.GetData(after,0);
        Check("long silences shortened with stereo alignment",paced.channels==2&&paced.frequency==1000&&paced.length<2f);
        Check("all audible samples preserved in exact order",values.Where(x=>Mathf.Abs(x)>.0015f).SequenceEqual(after.Where(x=>Mathf.Abs(x)>.0015f)));
        var silence=AudioClip.Create("silence fixture",1000,1,1000,false);Check("all-silent input returned unchanged",ChineseSpeechPacing.Prepare(silence)==silence);
        Object.Destroy(original);Object.Destroy(paced);Object.Destroy(silence);
        yield return SceneManager.LoadSceneAsync("第一章新版警察");yield return Frames(8);
        var c1=Object.FindFirstObjectByType<Chapter1PerformanceController>();c1.StopAllCoroutines();c1.enabled=false;
        const string first="我們只是辦婚禮，沒有冒犯。";
        c1.Talk("族人",first,8);var v1=c1.GetComponent<GameChineseVoice>();float length=v1.Output.clip.length;
        Check("chapter 1 full voiced line uses trimmed audio at original pitch",v1.Busy&&v1.Output.pitch==1&&length>2&&length<3);
        var d1=(Chapter1DialogueUI)Get(c1,"dialogueUI");Check("chapter 1 subtitle unchanged",d1?d1.bodyText.text==first:(string)Get(c1,"fallbackLine")==first);
        float begin=Time.realtimeSinceStartup;
        yield return (IEnumerator)Call(c1,"PlayPoliceVoicedLine","族人",first,Get(c1,"groomReplyVoice"),8f,1f);
        float elapsed=Time.realtimeSinceStartup-begin;
        Check("chapter 1 waits through recording and short tail",!v1.Busy&&elapsed>=length-.1f&&elapsed<length+.55f);
        report.observations.Add("chapter1 line="+length+" wait="+elapsed);
        c1.Talk("女性族人","放開我！",8);Check("female voice retained",v1.CurrentRole=="female"&&v1.Output.pitch==1&&v1.Busy);v1.Stop();
        yield return SceneManager.LoadSceneAsync("第二章");yield return Frames(8);
        var c2=Object.FindFirstObjectByType<Chapter2Controller>();c2.StopAllCoroutines();c2.enabled=false;
        var a=Object.FindFirstObjectByType<Chapter2AudioManager>();a.StopVoice();
        var shortLine=a.lines.First(x=>x.chinese=="父親！");
        float duration=a.BeginLine(shortLine.role,shortLine.chinese,8);var v2=a.GetComponent<GameChineseVoice>();length=v2.Output.clip.length;
        Check("chapter 2 short line no longer waits old eight seconds",duration<3&&duration>=length&&v2.CurrentRole=="tado");
        begin=Time.realtimeSinceStartup;yield return (IEnumerator)Call(c2,"WaitChapterVoice",duration);elapsed=Time.realtimeSinceStartup-begin;
        Check("chapter 2 actual wait follows recording",!a.VoicePlaying&&elapsed>=length-.1f&&elapsed<length+.55f);
        report.observations.Add("chapter2 line="+length+" wait="+elapsed);
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Seediq);
        var legacy=a.lines.First(x=>x.clip);duration=a.BeginLine(legacy.role,legacy.chinese,30);
        var legacySource=(AudioSource)Get(a,legacy.role==Chapter2AudioManager.Role.Narrator?"narration":"character");
        Check("original chapter 2 voice clip retained and old timer removed",legacySource.clip==legacy.clip&&Mathf.Abs(duration-legacy.clip.length-.12f)<.02f);a.StopVoice();
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);
        yield return SceneManager.LoadSceneAsync("第三章");yield return Frames(12);
        var f=Object.FindFirstObjectByType<Chapter3Flow>();f.station.StopAllCoroutines();f.station.enabled=false;f.ui.Say("",null);
        var v3=f.GetComponent<GameChineseVoice>();var voice=f.ui.voice;
        string[] names={"巴萬．拿威","莫那．魯道","達多．莫那","比胡．薩波"},roles={"bawan","mona","tado","pihu"};
        const string shortThird="去吧。",longThird="前面有巡警。靠著岩石走，別站在路中央。";
        var clips=new HashSet<AudioClip>();
        for(int i=0;i<names.Length;i++)
        {
            f.ui.Say(names[i],shortThird);length=v3.Output.clip.length;clips.Add(v3.Output.clip);
            Check("correct offline role and unshifted pitch "+roles[i],v3.CurrentRole==roles[i]&&v3.Output.pitch==1&&v3.Busy&&v3.LastProvider=="recorded-neural");
            begin=Time.realtimeSinceStartup;yield return Chapter3Flow.WaitForLine(6);elapsed=Time.realtimeSinceStartup-begin;
            Check("short subtitle follows complete voice instead of six seconds "+roles[i],!v3.Busy&&elapsed>=length-.1f&&elapsed<length+.6f&&elapsed<2);
            Check("Chinese text unchanged "+roles[i],f.ui.subtitle.text==shortThird);
            report.observations.Add(roles[i]+" short clip="+length+" wait="+elapsed);
        }
        Check("four voices remain distinct clips",clips.Count==4);
        f.ui.Say(f.station.bawan.DisplayName,longThird);length=v3.Output.clip.length;var cached=v3.Output.clip;
        Check("internal long silence shortened",length>4&&length<4.8f);
        var face=f.station.bawan.GetComponent<Chapter2SpeechFace>();float peak=0;
        while(voice.Busy){peak=Mathf.Max(peak,face.MouthWeight);yield return null;}
        Check("recorded speech still drives mouth",peak>.05f);
        yield return Chapter3Flow.WaitForLine(.1f);Check("long speech is never cut to short subtitle estimate",!voice.Busy);
        f.ui.Say(names[0],longThird);Check("replay reuses paced clip",v3.Output.clip==cached&&v3.LastProvider=="cache");
        f.ui.Say("",null);Check("cancel clears recording ownership",!voice.Busy&&!voice.HasAudio&&!v3.Busy);
        begin=Time.realtimeSinceStartup;yield return Chapter3Flow.WaitForLine(.4f);
        Check("unvoiced line retains subtitle fallback",Time.realtimeSinceStartup-begin>=.39f);
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Seediq);f.ui.Say(names[0],longThird);
        Check("original Seediq voice and tone retained",voice.Speaking&&Mathf.Abs(v3.Output.pitch-.95f)<.001f&&v3.Output.clip.name.Contains("Chapter 1 male tone"));
        f.ui.Say("",null);GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);
        // Run the real intro/handoff, then diagnose the existing weapon gate.
        f.Carry(Chapter3Weapons.Weapon.Knife);f.EnterCombat();
        yield return Wait(()=>f.combat.Fighting,25,"mountain intro voice handoff");
        foreach(var enemy in f.combat.Enemies)enemy.enabled=false;
        var w=f.combat.weapons;
        Check("dialogue handoff enables selected knife",w.Selected==Chapter3Weapons.Weapon.Knife&&w.Ready&&f.player.canMove);
        begin=Time.realtimeSinceStartup;Check("ready knife starts in same call",w.Attack()&&w.Slashes==1);
        Check("immediate second click is rejected by existing cooldown",!w.Attack()&&w.Slashes==1);
        yield return new WaitForSeconds(.8f);Check("rejected click is not buffered",w.Slashes==1&&w.Ready);
        Check("next ready knife attack succeeds",w.Attack()&&w.Slashes==2);yield return new WaitForSeconds(.8f);
        f.Carry(Chapter3Weapons.Weapon.Rifle);w.ResetEquipment();
        Check("ready rifle fires immediately",w.Attack()&&w.Shots==1&&!w.Loaded);
        Check("reload during recoil is also rejected",!w.Reload());yield return new WaitForSeconds(.55f);
        Check("empty chamber cannot fire",!w.Attack()&&w.Shots==1);yield return new WaitForSeconds(.3f);
        begin=Time.realtimeSinceStartup;Check("ready reload starts",w.Reload());
        yield return new WaitForSeconds(2.35f);Check("click during reload is rejected",w.Reloading&&!w.Attack());
        yield return Wait(()=>!w.Reloading,2,"reload completion");elapsed=Time.realtimeSinceStartup-begin;
        Check("single shot reload is about 2.6 seconds",elapsed>=2.55f&&elapsed<3.2f&&w.Loaded&&w.Reloads==1);
        report.observations.Add("reload seconds="+elapsed);
        int count=w.Shots;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));yield return Frames(2);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return Frames(2);
        Check("ready synthetic left click is received by weapon Update",w.Shots==count+1);
        var frames=new List<float>();float until=Time.realtimeSinceStartup+4;
        while(Time.realtimeSinceStartup<until){frames.Add(Time.unscaledDeltaTime);yield return null;}
        frames.Sort();report.observations.Add("mountain Editor frame milliseconds median="+frames[frames.Count/2]*1000+" p95="+frames[(int)(frames.Count*.95)]*1000+" max="+frames.Last()*1000+" target="+Application.targetFrameRate+" vsync="+QualitySettings.vSyncCount);
        f.EnterSchool();yield return Frames(20);Check("F8 equivalent transition stops weapon and old speech",f.Region=="School"&&!w.Reloading&&!f.combat.Fighting);
        Save();
    }
    static IEnumerator Performance()
    {
        var f=Object.FindFirstObjectByType<Chapter3Flow>();
        Check("performance fixture starts in formal third chapter",f&&SceneManager.GetActiveScene().name=="第三章");
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();
        Call(f.combat,"StartFighting",true);foreach(var e in f.combat.Enemies)e.enabled=false;
        yield return new WaitForSeconds(2);
        var names=new[]{"PlayerLoop","Update.ScriptRunBehaviourUpdate","BehaviourUpdate","WaitForTargetFPS","Chapter3.PathMotor.Move","Chapter3.PathMotor.Plan","Camera.Render"};
        var recorders=names.Select(n=>UnityEngine.Profiling.Recorder.Get(n)).ToArray();foreach(var r in recorders)r.enabled=true;
        var cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);var enabled=cameras.Select(c=>c.enabled).ToArray();
        for(int phase=0;phase<2;phase++)
        {
            if(phase==1)foreach(var c in cameras)c.enabled=false;
            yield return new WaitForSeconds(.5f);
            var samples=new List<float>();var totals=new double[names.Length];int focused=0;
            float end=Time.realtimeSinceStartup+4;
            while(Time.realtimeSinceStartup<end)
            {
                samples.Add(Time.unscaledDeltaTime);if(Application.isFocused)focused++;
                for(int i=0;i<names.Length;i++)totals[i]+=recorders[i].elapsedNanoseconds/1000000.0;
                yield return null;
            }
            samples.Sort();report.observations.Add((phase==0?"foreground rendering":"cameras disabled")+" frames="+samples.Count+" focused="+focused+" medianMs="+samples[samples.Count/2]*1000+" p95Ms="+samples[(int)(samples.Count*.95)]*1000);
            for(int i=0;i<names.Length;i++)report.observations.Add(names[i]+" valid="+recorders[i].isValid+" meanMs="+totals[i]/samples.Count);
        }
        for(int i=0;i<cameras.Length;i++)cameras[i].enabled=enabled[i];foreach(var r in recorders)r.enabled=false;
        Check("all camera states restored after diagnostic",cameras.Select(c=>c.enabled).SequenceEqual(enabled));
    }
}
public sealed class DialoguePacingDriver:MonoBehaviour {IEnumerator Start(){yield return DialoguePacingVerify.Run();}}
