using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ChineseNaturalVoiceVerify
{
    const string Dir="_CodexBackups/chinese_natural_voice_20261104/",Key="ChineseNaturalVoiceVerify.Run";
    static bool launched;
    static ChineseNaturalVoiceVerify(){EditorApplication.update+=Tick;File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&SessionState.GetBool(Key,false)&&!launched){launched=true;var go=new GameObject("Temporary voice language verification");Object.DontDestroyOnLoad(go);go.AddComponent<ChineseNaturalVoiceDriver>();}
        if(!File.Exists(Dir+"command.txt"))return;
        string command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        if(command=="refresh"){AssetDatabase.Refresh();return;}
        if(command=="run"&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&!SceneManager.GetActiveScene().isDirty){SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        else if(command=="stop"){SessionState.EraseBool(Key);EditorApplication.isPlaying=false;}
        else File.WriteAllText(Dir+"command-error.txt","Expected clean compiled scene in Edit Mode");
    }
    [Serializable] sealed class Pref {public string key;public bool exists;public int value;}
    [Serializable] sealed class Report{public bool completed;public List<string> checks=new List<string>(),errors=new List<string>(),observations=new List<string>();public Pref[] originalPreferences;}
    static Report report;
    static readonly string[] PrefKeys={"WusheEvent.MasterVolume","WusheEvent.MasterVolumeChosen","WusheEvent.VoiceLanguage","WusheEvent.SubtitleLanguage"};
    static void Check(string label,bool valid){if(!valid)throw new Exception(label);report.checks.Add(label);Save(false);}
    static void Save(bool completed){report.completed=completed;File.WriteAllText(Dir+"report-progress.json",JsonUtility.ToJson(report,true));}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(text);}
    static object Get(object target,string field)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);
    static IEnumerator Invoke(object target,string method,params object[] args)=>(IEnumerator)target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    static IEnumerator Wait(Func<bool> condition,float timeout,string label){float deadline=Time.realtimeSinceStartup+timeout;while(!condition()){if(Time.realtimeSinceStartup>deadline)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Scene(string name){yield return SceneManager.LoadSceneAsync(name);yield return Frames(8);}
    public static IEnumerator Run()
    {
        report=new Report{originalPreferences=PrefKeys.Select(k=>new Pref{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray()};
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
        foreach(var p in report.originalPreferences){if(p.exists)PlayerPrefs.SetInt(p.key,p.value);else PlayerPrefs.DeleteKey(p.key);}PlayerPrefs.Save();GameAudioSettings.Apply();
        report.checks.Add("original preference keys restored");report.completed=report.errors.Count==0;
        File.WriteAllText(Dir+"report-languages.json",JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=Log;Application.runInBackground=background;SessionState.EraseBool(Key);EditorApplication.isPlaying=false;
    }
    static IEnumerator Scenario()
    {
        foreach(string key in PrefKeys)PlayerPrefs.DeleteKey(key);
        Check("fresh default is Chinese voice",GameLanguageSettings.ChineseVoice);
        Check("fresh default volume 10",GameAudioSettings.Level==10);GameAudioSettings.Apply();Check("default master gain 1",AudioListener.volume==1);
        Check("reading defaults does not create a saved selection",!PlayerPrefs.HasKey("WusheEvent.MasterVolume")&&!PlayerPrefs.HasKey(GameLanguageSettings.VoicePreferenceKey));
        PlayerPrefs.SetInt("WusheEvent.MasterVolume",5);Check("old automatic level 5 migrates to 10",GameAudioSettings.Level==10);
        GameAudioSettings.SetLevel(5);Check("explicit level 5 remains 5",GameAudioSettings.Level==5);
        GameAudioSettings.SetLevel(0);Check("zero really mutes",AudioListener.volume==0);GameAudioSettings.SetLevel(10);
        foreach(var language in new[]{GameSubtitleLanguage.Chinese,GameSubtitleLanguage.Seediq,GameSubtitleLanguage.Japanese,GameSubtitleLanguage.None})
        {GameLanguageSettings.SetLanguage(language);Check("Chinese subtitles for "+language,GameLanguageSettings.SubtitlesEnabled&&GameLanguageSettings.LocalizeSubtitle("願祖靈庇佑新人。") == "願祖靈庇佑新人。"&&GameLanguageSettings.LocalizeSpeaker("族人")=="族人");}
        yield return Scene("字幕選單");
        var buttons=Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var chinese=buttons.First(b=>b.GetComponentInChildren<TextMeshProUGUI>(true)?.text=="中文");var seediq=buttons.First(b=>b.GetComponentInChildren<TextMeshProUGUI>(true)?.text=="賽德克語");
        Check("only supported voice options visible",chinese.gameObject.activeInHierarchy&&seediq.gameObject.activeInHierarchy&&buttons.Where(b=>new[]{"日文","無字幕"}.Contains(b.GetComponentInChildren<TextMeshProUGUI>(true)?.text)).All(b=>!b.gameObject.activeInHierarchy));
        Check("language menu explains fixed Chinese subtitles",Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text=="字幕固定中文"));
        seediq.onClick.Invoke();yield return Wait(()=>SceneManager.GetActiveScene().name=="設定內容",10,"return to settings");yield return Frames(5);
        Check("Seediq button saves voice selection",GameLanguageSettings.CurrentLanguage==GameSubtitleLanguage.Seediq);
        Check("settings shows volume 10",Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text=="音量 10 / 10"));
        yield return Scene("字幕選單");chinese=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<TextMeshProUGUI>()?.text=="中文");chinese.onClick.Invoke();yield return Wait(()=>SceneManager.GetActiveScene().name=="設定內容",10,"Chinese return");Check("Chinese button saves voice selection",GameLanguageSettings.ChineseVoice);
        yield return Scene("第一章新版警察");var c1=Object.FindFirstObjectByType<Chapter1PerformanceController>();Check("first chapter loaded",c1!=null);c1.StopAllCoroutines();c1.enabled=false;
        const string line1="我們只是辦婚禮，沒有冒犯。";c1.Talk("族人",line1,1);yield return Frames(2);
        var chinese1=c1.GetComponent<GameChineseVoice>();Check("first chapter Chinese recording plays",chinese1&&chinese1.Output.isPlaying&&AssetDatabase.GetAssetPath(chinese1.Output.clip).Contains("ChineseVoice/Audio"));
        var dialogue=(Chapter1DialogueUI)Get(c1,"dialogueUI");Check("first chapter subtitle remains exact",dialogue?dialogue.bodyText.text==line1:(string)Get(c1,"fallbackLine")==line1);
        chinese1.Stop();var legacy=(AudioClip)Get(c1,"groomReplyVoice");float start=Time.realtimeSinceStartup;
        yield return Invoke(c1,"PlayPoliceVoicedLine","族人",line1,legacy,1f,1f);
        Check("first chapter voiced scene waits for complete Chinese audio",Time.realtimeSinceStartup-start>=chinese1.Output.clip.length-.1f&&!chinese1.Busy);
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Seediq);var routine=c1.StartCoroutine(Invoke(c1,"PlayPoliceVoicedLine","族人",line1,legacy,1f,1f));yield return Frames(2);
        Check("first chapter original recording path retained",((AudioSource)Get(c1,"policeDialogueAudio")).isPlaying&&!chinese1.Busy);c1.StopCoroutine(routine);((AudioSource)Get(c1,"policeDialogueAudio")).Stop();
        Check("first chapter original mode subtitle remains Chinese",dialogue?dialogue.bodyText.text==line1:(string)Get(c1,"fallbackLine")==line1);
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);c1.Talk("女性族人","放開我！",1);yield return Frames(2);Check("female Chinese character voice exists",AssetDatabase.GetAssetPath(chinese1.Output.clip).EndsWith("_f.wav"));chinese1.Stop();
        yield return Scene("第二章");var c2=Object.FindFirstObjectByType<Chapter2Controller>();c2.StopAllCoroutines();c2.enabled=false;var a=Object.FindFirstObjectByType<Chapter2AudioManager>();Check("second chapter loaded",a!=null);a.StopVoice();
        int chineseLines=0,legacyLines=0;var chinese2=a.GetComponent<GameChineseVoice>();
        foreach(var line in a.lines){if(string.IsNullOrEmpty(line.chinese))continue;a.BeginLine(line.role,line.chinese,.1f);if(!chinese2.Output||!chinese2.Output.isPlaying||!AssetDatabase.GetAssetPath(chinese2.Output.clip).Contains("ChineseVoice/Audio"))throw new Exception("Missing chapter 2 Chinese line "+line.id+":"+line.chinese);chineseLines++;}
        Check("all serialized chapter 2 lines have offline Chinese audio",chineseLines>=59);a.StopVoice();GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Seediq);
        foreach(var line in a.lines){if(!line.clip)continue;a.BeginLine(line.role,line.chinese,.1f);var source=(AudioSource)Get(a,line.role==Chapter2AudioManager.Role.Narrator?"narration":"character");if(source.clip!=line.clip||!source.isPlaying||chinese2.Busy)throw new Exception("Original chapter 2 audio changed "+line.id);legacyLines++;}
        Check("all assigned chapter 2 originals retained",legacyLines>0);report.observations.Add("chapter 2 Chinese="+chineseLines+" original="+legacyLines);a.StopVoice();
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);yield return Scene("第三章");var f=Object.FindFirstObjectByType<Chapter3Flow>();yield return Wait(()=>f.station.CurrentLine!=null,20,"third chapter start");f.station.StopAllCoroutines();f.station.enabled=false;f.ui.Say("",null);
        var voice=f.ui.voice;voice.AllowOnline=false;var chinese3=f.GetComponent<GameChineseVoice>();var source3=(AudioSource)Get(voice,"output");const string line3="前面有巡警。靠著岩石走，別站在路中央。";
        f.ui.Say(f.station.bawan.DisplayName,line3);yield return Frames(2);Check("third chapter Chinese voice selected",source3.isPlaying&&AssetDatabase.GetAssetPath(source3.clip).Contains("ChineseVoice/Audio")&&source3.pitch==1);Check("third chapter Chinese subtitle exact",f.ui.subtitle.text==line3);
        float peak=0;var face=f.station.bawan.GetComponent<Chapter2SpeechFace>();for(int i=0;i<40;i++){peak=Mathf.Max(peak,face.MouthWeight);yield return null;}Check("Chinese audio drives third chapter mouth",peak>.05f);
        yield return Chapter3Flow.WaitForLine(.1f);Check("third chapter waits for Chinese recording",!voice.Busy&&!voice.Speaking);
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Seediq);f.ui.Say(f.station.bawan.DisplayName,line3);yield return Frames(2);Check("third chapter original Seediq tone retained",voice.Speaking&&source3.clip.name.Contains("Chapter 1 male tone")&&Mathf.Abs(source3.pitch-.95f)<.001f);Check("Seediq voice still shows Chinese only",f.ui.subtitle.text==line3&&!string.IsNullOrEmpty(voice.LastTranslation));
        GameLanguageSettings.SetLanguage(GameSubtitleLanguage.Chinese);Check("language switch stops old voice",!voice.Busy&&!voice.Speaking);
        const string dynamicLine="我們先在路旁等一下，大家確認安全再出發。";f.ui.Say(f.station.bawan.DisplayName,dynamicLine);yield return Wait(()=>voice.Speaking||!voice.Busy,40,"neural dynamic Chinese");
        report.observations.Add("dynamic provider="+chinese3.LastProvider+" error="+chinese3.LastError+" neuralFailure="+chinese3.NeuralFailure);
        Check("uncached Chinese dialogue uses neural speech",voice.Speaking&&chinese3.LastError==null&&chinese3.LastProvider=="online-neural");Check("dynamic Chinese keeps subtitle",f.ui.subtitle.text==dynamicLine);report.observations.Add("dynamic Chinese duration="+source3.clip.length);f.ui.Say("",null);
        f.ui.Say(f.station.bawan.DisplayName,"請你在這裡稍等一下，我們馬上出發。");yield return Frames(2);f.ui.Say(f.station.bawan.DisplayName,line3);yield return Frames(2);Check("cancel synthesis and replace with cached Chinese",voice.Speaking&&f.ui.subtitle.text==line3&&AssetDatabase.GetAssetPath(source3.clip).Contains("ChineseVoice/Audio"));f.ui.Say("",null);
        Check("voice clear stops all Chinese playback",!voice.Busy&&!voice.Speaking);
        var bank=JsonUtility.FromJson<GameChineseVoice.Bank>(Resources.Load<TextAsset>("ChineseVoice/catalog").text);
        var matched=bank.entries.First(e=>e.chinese==line3);
        Check("four distinct neural voice identities",matched.variants.Length==4&&matched.variants.Select(v=>v.voice).Distinct().Count()==4);
        Check("Tado full name resolves before Mona",GameChineseVoice.ResolveRole("達多．莫那")=="tado");
        var names=new[]{"巴萬．拿威","莫那．魯道","達多．莫那","比胡．薩波"};
        var roles=new[]{"bawan","mona","tado","pihu"};var resources=new HashSet<string>();
        chinese3.AllowOnline=false;
        for(int i=0;i<names.Length;i++)
        {
            f.ui.Say(names[i],line3);yield return Frames(3);
            string resource=AssetDatabase.GetAssetPath(source3.clip);resources.Add(resource);
            Check("offline correct character recording "+roles[i],chinese3.CurrentRole==roles[i]&&source3.isPlaying&&resource=="Assets/Resources/"+matched.variants.First(v=>v.role==roles[i]).resource+".wav");
            Check("subtitle unchanged for "+roles[i],f.ui.subtitle.text==line3);
            report.observations.Add(roles[i]+"="+resource+" seconds="+source3.clip.length);
        }
        Check("same sentence uses four different clips",resources.Count==4);
        f.ui.Say(names[0],line3);yield return Frames(2);Check("role cache reuses the correct Bawan clip",chinese3.LastProvider=="cache"&&chinese3.CurrentRole=="bawan"&&AssetDatabase.GetAssetPath(source3.clip).EndsWith(matched.resource+".wav"));
        f.ui.Say("",null);chinese3.AllowOnline=true;
        for(int i=1;i<names.Length;i++)
        {
            string sentence="我們會在這條山路旁等你，請先確認大家都平安。";
            f.ui.Say(names[i],sentence);yield return Wait(()=>voice.Speaking||!voice.Busy,60,"dynamic role "+roles[i]);
            Check("dynamic role uses same neural profile "+roles[i],voice.Speaking&&chinese3.CurrentRole==roles[i]&&chinese3.LastProvider=="online-neural"&&chinese3.LastError==null);
            report.observations.Add("dynamic "+roles[i]+" seconds="+source3.clip.length);f.ui.Say("",null);
        }
        chinese3.AllowOnline=false;const string offlineLine="確認離線測試時仍能聽見這句話。";
        f.ui.Say(names[0],offlineLine);yield return Wait(()=>voice.Speaking||!voice.Busy,40,"offline fallback");
        Check("offline uncached dialogue has local fallback",voice.Speaking&&chinese3.LastProvider=="offline-fallback"&&chinese3.LastError==null);f.ui.Say("",null);chinese3.AllowOnline=true;
        f.ui.Say(names[0],"這是一句用來測試立即取消的新對話。");
        yield return Wait(()=>Get(chinese3,"process")!=null||!chinese3.Busy,10,"worker launch before cancellation");
        Check("dynamic worker launched",Get(chinese3,"process")!=null);f.ui.Say(names[2],line3);yield return Frames(2);
        Check("cancel releases owned worker and request",Get(chinese3,"process")==null&&Get(chinese3,"requestPath")==null&&chinese3.CurrentRole=="tado"&&source3.isPlaying);
        f.ui.Say("",null);

    }
}
public sealed class ChineseNaturalVoiceDriver:MonoBehaviour{IEnumerator Start(){yield return ChineseNaturalVoiceVerify.Run();}}
