using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Chapter3VoiceStyleVerify
{
    public const string Dir="_CodexBackups/chapter3_voice_style_20261104/";
    static bool launched;
    static Chapter3VoiceStyleVerify(){EditorApplication.update+=Tick;File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&SessionState.GetBool("Chapter3VoiceStyleRun",false)&&!launched){launched=true;new GameObject("Temporary voice style verification").AddComponent<Chapter3VoiceStyleDriver>();}
        if(!File.Exists(Dir+"command.txt"))return;
        string command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try
        {
            if(command=="refresh"){AssetDatabase.Refresh();return;}
            if(command=="run")
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed||SceneManager.GetActiveScene().isDirty||SceneManager.GetActiveScene().path!="Assets/Scenes/第三章.unity")throw new Exception("Expected clean compiled formal third chapter");
                SessionState.SetBool("Chapter3VoiceStyleRun",true);EditorApplication.isPlaying=true;return;
            }
            if(command=="analyze")
            {
                Export("Assets/音樂/我們只是辦婚禮，沒有冒犯。.mp3","chapter1-groom");
                Export("Assets/音樂/喝.mp3","chapter1-male-thanks");
                Export("Assets/音樂/夠了！不要再羞辱我們！.mp3","chapter1-intervene");
                File.WriteAllText(Dir+"analyze-complete.txt","playing="+EditorApplication.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);
            }
        }
        catch(Exception e){File.WriteAllText(Dir+"error.txt",e.ToString());}
    }
    [Serializable] sealed class Report{public bool completed;public List<string> checks=new List<string>(),errors=new List<string>(),observations=new List<string>();}
    static Report report;
    static void Check(string label,bool valid){if(!valid)throw new Exception(label);report.checks.Add(label);}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(text);}
    public static IEnumerator Run()
    {
        report=new Report();bool background=Application.runInBackground;Application.runInBackground=true;Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception error=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        report.completed=report.errors.Count==0;File.WriteAllText(Dir+"report-style.json",JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseBool("Chapter3VoiceStyleRun");EditorApplication.isPlaying=false;
    }
    static object Get(object target,string field)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    static IEnumerator Wait(Func<bool> ready,float seconds,string label){float deadline=Time.realtimeSinceStartup+seconds;while(!ready()){if(Time.realtimeSinceStartup>deadline)throw new Exception("Timeout "+label);yield return null;}}
    static IEnumerator Scenario()
    {
        yield return null;yield return null;
        var f=UnityEngine.Object.FindFirstObjectByType<Chapter3Flow>();Check("formal third chapter",f&&f.startAtStation);
        yield return Wait(()=>f.station.CurrentLine!=null,20,"station start");f.station.StopAllCoroutines();f.station.enabled=false;f.ui.Say("",null);
        var voice=f.ui.voice;voice.AllowOnline=false;var source=(AudioSource)Get(voice,"output");
        const string line="前面有巡警。靠著岩石走，別站在路中央。";f.ui.Say(f.station.bawan.DisplayName,line);
        yield return null;Check("Chinese subtitle unchanged",f.ui.subtitle.text==line);Check("offline processed clip plays",voice.Speaking&&source.clip.name.Contains("Chapter 1 male tone"));
        Check("reference pitch and 2D mix",Mathf.Abs(source.pitch-.95f)<.001f&&source.spatialBlend==0&&Mathf.Abs(source.volume-.85f)<.001f);
        var original=Resources.Load<AudioClip>("Chapter3Voice/Audio/b2a4a9a44732f43d0664");Check("processing preserves full source duration",source.clip.samples==original.samples&&source.clip.frequency==original.frequency);
        var data=new float[source.clip.samples*source.clip.channels];source.clip.GetData(data,0);Check("processed clip finite and below ceiling",data.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)&&Mathf.Abs(x)<.901f));
        Check("processed clip audible",data.Any(x=>Mathf.Abs(x)>.05f));
        Save(data.Select(x=>x*source.volume).ToArray(),source.clip.channels,Mathf.RoundToInt(source.clip.frequency*source.pitch),Dir+"third-chapter-style-preview.wav");
        var dry=new float[original.samples*original.channels];original.GetData(dry,0);Save(dry.Select(x=>x*.85f).ToArray(),original.channels,original.frequency,Dir+"third-chapter-before.wav");
        var face=f.station.bawan.GetComponent<Chapter2SpeechFace>();float start=Time.realtimeSinceStartup,mouthPeak=0;
        while(voice.Busy){mouthPeak=Mathf.Max(mouthPeak,face.MouthWeight);CheckHeartbeat();yield return null;}
        float duration=Time.realtimeSinceStartup-start;
        Check("slower speech completes before advancing",duration>=original.length/.95f-.35f);Check("mouth follows processed audio",mouthPeak>.1f);report.observations.Add("full sample duration="+duration+" expected="+original.length/.95f+" mouthPeak="+mouthPeak);
        f.ui.Say(f.station.bawan.DisplayName,line);yield return null;var cached=source.clip;f.ui.Say("",null);Check("clear stops processed voice",!voice.Speaking&&!voice.Busy);
        f.ui.Say(f.station.bawan.DisplayName,line);yield return null;Check("replay reuses processed cache",source.clip==cached);f.ui.Say("",null);
        // A sustained tone at the final 5% catches the old raw-length mouth cutoff.
        var tone=AudioClip.Create("Late mouth verification",72000,1,24000,false);var wave=new float[72000];for(int i=0;i<wave.Length;i++)wave[i]=.08f*Mathf.Sin(i*2*Mathf.PI*148/24000);tone.SetData(wave,0);
        source.clip=tone;source.Play();f.station.bawan.speaking=true;face.BeginRecordedLine("嘴形同步驗證",source);
        yield return new WaitForSecondsRealtime(3.02f);Check("mouth remains active beyond unpitched duration",source.isPlaying&&face.MouthWeight>.05f);source.Stop();f.station.bawan.speaking=false;UnityEngine.Object.Destroy(tone);
        // Check all 198 source assets without retaining the processed copies.
        var bank=JsonUtility.FromJson<Chapter3SeediqVoice.Bank>(Resources.Load<TextAsset>("Chapter3Voice/catalog").text);int count=0;
        foreach(var entry in bank.entries)
        {
            var clip=Resources.Load<AudioClip>(entry.resource);var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);Chapter3VoiceTone.Process(samples,clip.channels,clip.frequency);
            if(samples.Any(x=>float.IsNaN(x)||float.IsInfinity(x)||Mathf.Abs(x)>.901f)||!samples.Any(x=>Mathf.Abs(x)>.005f))throw new Exception("Invalid processed line "+entry.chinese);
            count++;if(count%12==0)yield return null;
        }
        Check("all prepared lines retain finite non-silent PCM",count==198);report.observations.Add("Processed bank lines="+count);
        voice.AllowOnline=true;const string dynamicLine="我們在這裡休息一下，然後再一起出發。";f.ui.Say(f.station.bawan.DisplayName,dynamicLine);
        yield return Wait(()=>voice.Speaking||!voice.Busy,100,"online voice");Check("live official voice uses same tone",voice.Speaking&&source.clip.name.Contains("Chapter 1 male tone"));
        Check("dynamic Chinese subtitle preserved",f.ui.subtitle.text==dynamicLine);report.observations.Add("Live official translation="+voice.LastTranslation);f.ui.Say("",null);
        Check("online playback stops cleanly",!voice.Busy&&!voice.Speaking);
    }
    static float heartbeat;
    static void CheckHeartbeat(){if(Time.realtimeSinceStartup<heartbeat)return;heartbeat=Time.realtimeSinceStartup+.5f;File.WriteAllText(Dir+"heartbeat.txt",DateTime.Now.ToString("O"));}
    public static void Export(string path,string name)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if(!clip||!clip.LoadAudioData())throw new Exception("Cannot load "+path);
        var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Cannot read "+path);
        Save(data,clip.channels,clip.frequency,Dir+name+".wav");
    }
    public static void Save(float[] data,int channels,int rate,string path)
    {
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);foreach(var x in data)writer.Write((short)(Mathf.Clamp(x,-1,1)*32767));
        }
    }
}
public sealed class Chapter3VoiceStyleDriver:MonoBehaviour{IEnumerator Start(){yield return Chapter3VoiceStyleVerify.Run();}}
