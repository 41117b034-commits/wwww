using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Authored neural recordings play offline. New dialogue uses the bundled neural
// worker on Windows, with the installed voice as an offline fallback.
public sealed class GameChineseVoice:MonoBehaviour
{
    [Serializable] public sealed class Variant {public string role,resource,voice;}
    [Serializable] public sealed class Entry {public string chinese,resource,femaleResource;public Variant[] variants;}
    [Serializable] public sealed class Bank {public Entry[] entries;}
    [Serializable] sealed class Job {public string text,path,role;public bool female;}
    [Serializable] sealed class Request {public Job[] jobs;}
    static Dictionary<string,Entry> bank;
    readonly Dictionary<string,AudioClip> cache=new Dictionary<string,AudioClip>();
    readonly List<AudioClip> owned=new List<AudioClip>();
    AudioSource output;
    Coroutine pending;
    UnityWebRequest download;
    System.Diagnostics.Process process;
    string requestPath,wavePath;
    bool ownsPlayback;
    public bool Busy=>pending!=null||(ownsPlayback&&output&&output.isPlaying);
    public string LastError {get;private set;}
    public string CurrentLine {get;private set;}
    public string CurrentRole {get;private set;}
    public string LastProvider {get;private set;}
    public string NeuralFailure {get;private set;}
    public bool AllowOnline=true;
    public AudioSource Output=>output;
    public float Remaining=>ownsPlayback&&output&&output.clip?Mathf.Max(0,(output.clip.length-output.time)/Mathf.Max(.1f,output.pitch)):0;
    public void Configure(AudioSource source){output=source;}
    void Awake(){GameLanguageSettings.Changed+=Stop;}
    static void LoadBank()
    {
        if(bank!=null)return;bank=new Dictionary<string,Entry>();var text=Resources.Load<TextAsset>("ChineseVoice/catalog");
        if(!text)return;var data=JsonUtility.FromJson<Bank>(text.text);if(data?.entries!=null)foreach(var entry in data.entries)bank[entry.chinese]=entry;
    }
    public void Speak(string speaker,string line)
    {
        Stop();LastError=null;NeuralFailure=null;LastProvider=null;CurrentLine=line;
        if(!GameLanguageSettings.ChineseVoice||string.IsNullOrWhiteSpace(line))return;
        if(!output){output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.spatialBlend=0;}
        output.pitch=1;output.volume=.85f;
        bool female=(speaker??"").Contains("女")||(speaker??"").Contains("新娘");
        string role=ResolveRole(speaker,female);CurrentRole=role;
        string key=role+"|"+line;LoadBank();
        if(cache.TryGetValue(key,out var clip)){LastProvider="cache";Play(clip);return;}
        if(bank.TryGetValue(line,out var entry))
        {
            string path=female?entry.femaleResource:entry.resource;
            if(role!="default"&&role!="female"&&role!="bawan")path=null;
            if(entry.variants!=null)foreach(var variant in entry.variants)if(variant.role==role){path=variant.resource;break;}
            if(!string.IsNullOrEmpty(path))clip=Resources.Load<AudioClip>(path);
            if(clip){cache[key]=clip;LastProvider="recorded-neural";Play(clip);return;}
        }
        pending=StartCoroutine(Generate(line,female,role,key));
    }
    public static string ResolveRole(string speaker,bool female=false)
    {
        if(female)return "female";speaker=speaker??"";
        // Tado's full name contains Mona: resolve the son first.
        if(speaker.Contains("達多")||speaker=="Tado")return "tado";
        if(speaker.Contains("比胡")||speaker=="Pihu")return "pihu";
        if(speaker.Contains("莫那")||speaker=="Mona")return "mona";
        if(speaker.Contains("巴萬")||speaker=="Bawan")return "bawan";
        return "default";
    }
    void Play(AudioClip clip){ownsPlayback=true;output.clip=clip;output.Play();}
    IEnumerator Generate(string text,bool female,string role,string key)
    {
        // Yield once so the coroutine handle is assigned before any early exit.
        yield return null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string assets=Path.Combine(Application.streamingAssetsPath,"ChineseVoice");
        string script=Path.Combine(assets,"synthesize.ps1");
        Exception requestError=null;
        try
        {
            // The worker lives under LocalLow on Windows. Keep its output in
            // the same application data area, where it can create files even
            // when the OS restricts writes to higher-integrity Temp folders.
            string folder=Path.Combine(Application.persistentDataPath,"ChineseVoiceRequests");Directory.CreateDirectory(folder);
            string id=Guid.NewGuid().ToString("N");requestPath=Path.Combine(folder,id+".json");wavePath=Path.Combine(folder,id+".wav");
            File.WriteAllText(requestPath,JsonUtility.ToJson(new Request{jobs=new[]{new Job{text=text,path=wavePath,female=female,role=role}}}),Encoding.UTF8);
        }catch(Exception e){requestError=e;}
        if(requestError!=null){LastError=requestError.Message;Cleanup();pending=null;yield break;}
        if(AllowOnline)
        {
            var setup=ChineseNeuralRuntime.Prepare(assets,Path.Combine(Application.persistentDataPath,"ChineseNeuralRuntime"));
            while(!setup.IsCompleted)yield return null;
            if(setup.IsFaulted)NeuralFailure=setup.Exception.GetBaseException().Message;
            else
            {
                Exception launchError=null;
                try
                {
                    var start=new System.Diagnostics.ProcessStartInfo{FileName=setup.Result,Arguments="\""+Path.Combine(assets,"neural-worker.py")+"\"",UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden};
                    start.EnvironmentVariables["WUSHE_TTS_REQUEST"]=requestPath;
                    process=System.Diagnostics.Process.Start(start);
                }catch(Exception e){launchError=e;}
                if(launchError!=null)NeuralFailure=launchError.Message;
                else
                {
                    float until=Time.realtimeSinceStartup+26;
                    while(process!=null&&!process.HasExited&&Time.realtimeSinceStartup<until)yield return null;
                    if(process!=null&&process.HasExited&&process.ExitCode==0&&File.Exists(wavePath+".mp3"))
                    {
                        yield return LoadAudio(wavePath+".mp3",AudioType.MPEG,key);
                        if(ownsPlayback){LastProvider="online-neural";Cleanup();pending=null;yield break;}
                    }
                    NeuralFailure=NeuralFailure??"Neural voice unavailable or timed out";
                }
                ReleaseProcess();
            }
        }
        Exception failure=null;
        try
        {
            var start=new System.Diagnostics.ProcessStartInfo{
                FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell/v1.0/powershell.exe"),
                Arguments="-NoLogo -NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(File.ReadAllText(script,Encoding.UTF8))),
                UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden
            };
            start.EnvironmentVariables["WUSHE_TTS_REQUEST"]=requestPath;
            process=System.Diagnostics.Process.Start(start);
        }catch(Exception e){failure=e;}
        if(failure!=null){LastError="中文語音產生失敗："+failure.Message;Cleanup();pending=null;yield break;}
        float deadline=Time.realtimeSinceStartup+30;
        while(process!=null&&!process.HasExited&&Time.realtimeSinceStartup<deadline)yield return null;
        if(process==null||!process.HasExited||process.ExitCode!=0||!File.Exists(wavePath)){LastError="無法使用本機台灣中文語音";Cleanup();pending=null;yield break;}
        yield return LoadAudio(wavePath,AudioType.WAV,key);
        if(ownsPlayback){LastProvider="offline-fallback";LastError=null;}
        Cleanup();
#else
        LastError="此平台尚未提供動態中文合成；固定劇情配音可離線播放";
#endif
        pending=null;
    }
    IEnumerator LoadAudio(string path,AudioType type,string key)
    {
        using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,type))
        {
            download=request;yield return request.SendWebRequest();download=null;
            if(request.result==UnityWebRequest.Result.Success)
            {
                var clip=DownloadHandlerAudioClip.GetContent(request);
                if(clip&&clip.samples>0){owned.Add(clip);cache[key]=clip;Play(clip);LastError=null;}
                else LastError="中文語音回傳空白音訊";
            }
            else LastError=request.error;
        }
    }
    public void Stop()
    {
        if(pending!=null)StopCoroutine(pending);pending=null;
        if(download!=null){download.Abort();download.Dispose();download=null;}
        if(output)output.Stop();ownsPlayback=false;CurrentLine=null;Cleanup();
    }
    void ReleaseProcess()
    {
        if(process!=null){try{if(!process.HasExited)process.Kill();}catch{}process.Dispose();process=null;}
    }
    void Cleanup()
    {
        ReleaseProcess();
        foreach(var path in new[]{requestPath,wavePath,string.IsNullOrEmpty(wavePath)?null:wavePath+".mp3"})if(!string.IsNullOrEmpty(path))try{File.Delete(path);}catch{}
        requestPath=wavePath=null;
    }
    void OnDisable(){Stop();}
    void OnDestroy(){Stop();GameLanguageSettings.Changed-=Stop;foreach(var clip in owned)if(clip)Destroy(clip);}
}
