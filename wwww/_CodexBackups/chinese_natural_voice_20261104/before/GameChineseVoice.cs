using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// All authored lines ship as WAV Resources. Newly generated dialogue uses the
// same installed Taiwan Chinese voices locally on Windows, without a network.
public sealed class GameChineseVoice:MonoBehaviour
{
    [Serializable] public sealed class Entry {public string chinese,resource,femaleResource;}
    [Serializable] public sealed class Bank {public Entry[] entries;}
    [Serializable] sealed class Job {public string text,path;public bool female;}
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
        Stop();LastError=null;CurrentLine=line;
        if(!GameLanguageSettings.ChineseVoice||string.IsNullOrWhiteSpace(line))return;
        if(!output){output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.spatialBlend=0;}
        output.pitch=1;output.volume=.85f;
        bool female=(speaker??"").Contains("女")||(speaker??"").Contains("新娘");
        string key=(female?"f|":"m|")+line;LoadBank();
        if(cache.TryGetValue(key,out var clip)){Play(clip);return;}
        if(bank.TryGetValue(line,out var entry))
        {
            string path=female?entry.femaleResource:entry.resource;
            if(!string.IsNullOrEmpty(path))clip=Resources.Load<AudioClip>(path);
            if(clip){cache[key]=clip;Play(clip);return;}
        }
        pending=StartCoroutine(Generate(line,female,key));
    }
    void Play(AudioClip clip){ownsPlayback=true;output.clip=clip;output.Play();}
    IEnumerator Generate(string text,bool female,string key)
    {
        // Yield once so the coroutine handle is assigned before any early exit.
        yield return null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string script=Path.Combine(Application.streamingAssetsPath,"ChineseVoice/synthesize.ps1");
        string folder=Path.Combine(Application.temporaryCachePath,"WusheChineseVoice");Directory.CreateDirectory(folder);
        string id=Guid.NewGuid().ToString("N");requestPath=Path.Combine(folder,id+".json");wavePath=Path.Combine(folder,id+".wav");
        Exception failure=null;
        try
        {
            File.WriteAllText(requestPath,JsonUtility.ToJson(new Request{jobs=new[]{new Job{text=text,path=wavePath,female=female}}}),Encoding.UTF8);
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
        using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(wavePath).AbsoluteUri,AudioType.WAV))
        {
            download=request;yield return request.SendWebRequest();download=null;
            if(request.result==UnityWebRequest.Result.Success){var clip=DownloadHandlerAudioClip.GetContent(request);owned.Add(clip);cache[key]=clip;Play(clip);}
            else LastError=request.error;
        }
        Cleanup();
#else
        LastError="此平台尚未提供動態中文合成；固定劇情配音可離線播放";
#endif
        pending=null;
    }
    public void Stop()
    {
        if(pending!=null)StopCoroutine(pending);pending=null;
        if(download!=null){download.Abort();download.Dispose();download=null;}
        if(output)output.Stop();ownsPlayback=false;CurrentLine=null;Cleanup();
    }
    void Cleanup()
    {
        if(process!=null){try{if(!process.HasExited)process.Kill();}catch{}process.Dispose();process=null;}
        foreach(var path in new[]{requestPath,wavePath})if(!string.IsNullOrEmpty(path))try{File.Delete(path);}catch{}
        requestPath=wavePath=null;
    }
    void OnDisable(){Stop();}
    void OnDestroy(){Stop();GameLanguageSettings.Changed-=Stop;foreach(var clip in owned)if(clip)Destroy(clip);}
}
