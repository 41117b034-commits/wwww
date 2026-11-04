using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

// Official ILRDF Tgdaya MT/TTS. Generated text is an AI draft, not a certified
// translation. A reviewed Resources bank can replace the network at any time.
[DefaultExecutionOrder(4480)]
public sealed class Chapter3SeediqVoice : MonoBehaviour
{
    const string Translation="https://ai-labs.ilrdf.org.tw/kari-seejiq-tnpusu-ai-hmjil";
    const string Speech="https://ai-labs.ilrdf.org.tw/hnang-kari-ai-asi-sluhay";
    [Serializable] public sealed class Entry {public string chinese,seediq,resource,source;public bool reviewed;}
    [Serializable] public sealed class Bank {public Entry[] entries;}
    [Serializable] sealed class Request {public string[] data;public string session_hash;public int fn_index;}
    readonly Dictionary<string,AudioClip> cache=new Dictionary<string,AudioClip>();
    readonly Dictionary<string,string> translations=new Dictionary<string,string>();
    readonly Dictionary<string,Entry> bank=new Dictionary<string,Entry>();
    readonly List<AudioClip> downloaded=new List<AudioClip>();
    readonly Dictionary<string,int> endpoints=new Dictionary<string,int>();
    readonly Dictionary<string,string> spokenNumbers=new Dictionary<string,string>();
    string session;
    bool translationReady,speechReady;
    AudioSource output;
    Chapter2Actor voiceActor;
    AudioClip faceClip;
    string speakerName;
    Coroutine pending;
    UnityWebRequest activeRequest;
    float unavailableUntil;
    public bool Busy {get;private set;}
    public bool Speaking=>output&&output.isPlaying;
    public string LastError {get;private set;}
    public string LastTranslation {get;private set;}
    public string CurrentLine {get;private set;}
    public bool AllowOnline=true;
    public Chapter3Flow Flow {get;set;}
    void Awake()
    {
        output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.spatialBlend=0;output.volume=.85f;
        var resource=Resources.Load<TextAsset>("Chapter3Voice/catalog");
        if(resource){var data=JsonUtility.FromJson<Bank>(resource.text);if(data?.entries!=null)foreach(var entry in data.entries)if(!string.IsNullOrEmpty(entry.chinese))bank[entry.chinese]=entry;}
    }
    public void Speak(string speaker,string line)
    {
        Stop();CurrentLine=line;speakerName=speaker;LastError=null;LastTranslation=null;
        if(string.IsNullOrEmpty(line)||string.IsNullOrEmpty(speaker)||speaker=="你"||speaker=="玩家")return;
        if(cache.TryGetValue(line,out var existing)){translations.TryGetValue(line,out string translated);LastTranslation=translated;Play(existing);return;}
        if(bank.TryGetValue(line,out var entry)&&!string.IsNullOrEmpty(entry.resource))
        {
            var clip=Resources.Load<AudioClip>(entry.resource);
            if(clip){LastTranslation=entry.seediq;translations[line]=entry.seediq;cache[line]=clip;Play(clip);return;}
        }
        if(!AllowOnline||Time.realtimeSinceStartup<unavailableUntil)return;
        Busy=true;pending=StartCoroutine(Generate(line));
    }
    void Play(AudioClip clip){Busy=true;output.clip=clip;output.Play();}
    void Update(){if(Busy&&pending==null&&!Speaking){Busy=false;if(voiceActor)voiceActor.speaking=false;}}
    void LateUpdate()
    {
        if(!Speaking)return;
        if(!voiceActor)voiceActor=Chapter3DialogueView.FindSpeaker(Flow,speakerName);
        if(!voiceActor)return;voiceActor.speaking=true;
        if(faceClip!=output.clip){faceClip=output.clip;voiceActor.GetComponent<Chapter2SpeechFace>()?.BeginRecordedLine(CurrentLine,output);}
    }
    public void Stop(){if(pending!=null)StopCoroutine(pending);pending=null;if(activeRequest!=null){activeRequest.Abort();activeRequest.Dispose();activeRequest=null;}if(output)output.Stop();if(voiceActor)voiceActor.speaking=false;voiceActor=null;faceClip=null;Busy=false;CurrentLine=null;}
    IEnumerator Generate(string line)
    {
        // Gradio closes a queue stream when its client cancels. A fresh line
        // gets its own selected-language session so it cannot inherit that
        // closed stream or an abandoned line's completion event.
        session=Guid.NewGuid().ToString("N");translationReady=speechReady=false;
        if(!translationReady){string ready=null;yield return Call(Translation,"to_formosan_languages",new[]{"賽德克"},value=>ready=value);translationReady=ready!=null;if(!translationReady){pending=null;Busy=false;yield break;}}
        if(!speechReady){string ready=null;yield return Call(Speech,"lambda",new[]{"賽德克"},value=>ready=value);speechReady=ready!=null;if(!speechReady){pending=null;Busy=false;yield break;}}
        // Short Chinese sentences keep the resulting Latin text under TTS's
        // 300-character limit. Never truncate a translation to fit that limit.
        string text=null;yield return Call(Translation,"translate_1",new[]{line,"zho_Hant","trv_Tegu"},value=>text=value);
        if(text==null){pending=null;Busy=false;yield break;}
        LastTranslation=FirstString(text);
        if(string.IsNullOrWhiteSpace(LastTranslation)){Fail("官方翻譯未回傳文字");yield break;}
        string spoken=System.Text.RegularExpressions.Regex.Replace(LastTranslation,@"\.{2,}|…+",".").TrimStart('.',' ',',');
        spoken=System.Text.RegularExpressions.Regex.Replace(spoken,"[:;：；]",",");
        spoken=System.Text.RegularExpressions.Regex.Replace(spoken,"[\"“”「」『』()（）]","");
        foreach(System.Text.RegularExpressions.Match number in System.Text.RegularExpressions.Regex.Matches(spoken,@"\d+"))
        {
            if(!spokenNumbers.TryGetValue(number.Value,out string word))
            {
                if(!int.TryParse(number.Value,out int value)||value<0||value>9999){Fail("數字超出語音轉寫範圍");yield break;}
                string converted=null;yield return Call(Translation,"translate_1",new[]{ChineseNumber(value),"zho_Hant","trv_Tegu"},result=>converted=result);
                word=FirstString(converted);
                if(string.IsNullOrEmpty(word)||System.Text.RegularExpressions.Regex.IsMatch(word,@"[\d\u4e00-\u9fff]")){Fail("官方服務未提供數字族語讀法");yield break;}
                word=word.TrimEnd('.','!','?',',');spokenNumbers[number.Value]=word;
            }
            spoken=System.Text.RegularExpressions.Regex.Replace(spoken,@"\b"+number.Value+@"\b",word);
        }
        if(System.Text.RegularExpressions.Regex.IsMatch(spoken,@"[\d\u4e00-\u9fff]")){Fail("官方翻譯仍有未轉換文字，保留中文字幕");yield break;}
        List<string> chunks=null;try{chunks=SplitSpeech(spoken);}catch(ArgumentException){Fail("官方文字含無法朗讀的過長詞彙");}
        if(chunks==null)yield break;
        foreach(var chunk in chunks)
        {
            string result=null;yield return Call(Speech,"default_speaker_tts",new[]{"賽德克_德固達雅_男聲",chunk},value=>result=value);
            if(result==null){pending=null;Busy=false;yield break;}
            var files=Array(result);var file=files!=null&&files.Count>0?files[0] as JObject:null;
            string url=(string)file?["url"],path=(string)file?["path"];
            if(string.IsNullOrEmpty(url)&&!string.IsNullOrEmpty(path))url=Speech+"/gradio_api/file="+UnityWebRequest.EscapeURL(path);
            if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="ai-labs.ilrdf.org.tw") {Fail("官方語音未回傳有效音檔");yield break;}
            AudioClip clip;
            using(var download=UnityWebRequestMultimedia.GetAudioClip(url,AudioType.WAV))
            {
                activeRequest=download;download.timeout=12;yield return download.SendWebRequest();activeRequest=null;
                if(download.result!=UnityWebRequest.Result.Success){Fail("官方音檔下載失敗："+download.error);yield break;}
                clip=DownloadHandlerAudioClip.GetContent(download);
            }
            downloaded.Add(clip);if(chunks.Count==1){cache[line]=clip;translations[line]=LastTranslation;}
            output.clip=clip;output.Play();while(output.isPlaying)yield return null;
        }
        LastError=null;pending=null;Busy=false;
    }
    public static List<string> SplitSpeech(string text)
    {
        var chunks=new List<string>();string current="";
        foreach(var word in text.Split(new[]{' ','\n','\r'},StringSplitOptions.RemoveEmptyEntries))
        {
            if(word.Length>300)throw new ArgumentException("TTS token exceeds 300 characters");
            if(current.Length+word.Length+1>280){chunks.Add(current);current="";}
            current+=(current.Length>0?" ":"")+word;
        }
        if(current.Length>0)chunks.Add(current);return chunks;
    }
    public static string ChineseNumber(int value)
    {
        if(value==0)return "零";string result="",digits="零一二三四五六七八九",units="千百十";bool zero=false;
        for(int place=3;place>=0;place--)
        {
            int digit=value/(int)Mathf.Pow(10,place)%10;
            if(digit>0){if(zero)result+="零";if(!(digit==1&&place==1&&result.Length==0))result+=digits[digit];if(place>0)result+=units[3-place];zero=false;}
            else if(result.Length>0)zero=true;
        }
        return result;
    }
    IEnumerator Call(string root,string endpoint,string[] data,Action<string> done)
    {
        string key=root+"/"+endpoint;
        if(!endpoints.ContainsKey(key))
        {
            using(var config=UnityWebRequest.Get(root+"/config"))
            {
                activeRequest=config;config.timeout=8;yield return config.SendWebRequest();activeRequest=null;
                if(config.result!=UnityWebRequest.Result.Success){Fail("無法讀取官方服務設定");yield break;}
                var settings=Parse(config.downloadHandler.text);
                if(settings?["dependencies"] is JArray dependencies)
                    foreach(var dependency in dependencies)
                        if(dependency["api_name"]?.Type==JTokenType.String&&dependency["id"]!=null)
                            endpoints[root+"/"+(string)dependency["api_name"]]=(int)dependency["id"];
            }
        }
        if(!endpoints.TryGetValue(key,out int function)){Fail("官方服務缺少指定功能");yield break;}
        string id=null;
        using(var request=new UnityWebRequest(root+"/gradio_api/queue/join","POST"))
        {
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Request{data=data,session_hash=session,fn_index=function})));
            request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=8;
            activeRequest=request;yield return request.SendWebRequest();activeRequest=null;
            if(request.result!=UnityWebRequest.Result.Success){Fail("官方服務連線失敗："+request.error);yield break;}
            id=(string)Parse(request.downloadHandler.text)?["event_id"];
        }
        if(string.IsNullOrEmpty(id)){Fail("官方服務未提供工作編號");yield break;}
        using(var result=UnityWebRequest.Get(root+"/gradio_api/queue/data?session_hash="+session))
        {
            activeRequest=result;result.timeout=40;yield return result.SendWebRequest();activeRequest=null;
            if(result.result!=UnityWebRequest.Result.Success){Fail("官方服務等待逾時或失敗："+result.error);yield break;}
            string value=CompletedQueue(result.downloadHandler.text,id);
            if(value==null){Fail("官方"+(endpoint=="translate_1"?"翻譯":"語音合成")+"服務回傳錯誤");yield break;}
            done(value);
        }
    }
    static JObject Parse(string text){try{return JObject.Parse(text);}catch{return null;}}
    static JArray Array(string text){try{return JArray.Parse(text);}catch{return null;}}
    static string FirstString(string text){var values=Array(text);return values!=null&&values.Count>0&&values[0].Type==JTokenType.String?(string)values[0]:null;}
    public static string CompletedQueue(string text,string expectedEvent=null)
    {
        foreach(var line in text.Split('\n'))
        {
            if(!line.StartsWith("data:"))continue;
            var message=Parse(line.Substring(5).Trim());
            if((string)message?["msg"]!="process_completed")continue;
            if(expectedEvent!=null&&(string)message["event_id"]!=expectedEvent)continue;
            if((bool?)message["success"]!=true)return null;
            return message["output"]?["data"]?.ToString(Newtonsoft.Json.Formatting.None);
        }
        return null;
    }
    void Fail(string error){LastError=error;unavailableUntil=Time.realtimeSinceStartup+120;pending=null;Busy=false;}
    void OnDisable(){Stop();}
    void OnDestroy(){foreach(var clip in downloaded)if(clip)Destroy(clip);}
}
