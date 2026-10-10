using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Temporary interaction driver; archived after verification, never saves the scene.
[InitializeOnLoad]
public static class Chapter2EnterProbe
{
    const string Dir="_CodexBackups/chapter2_enter_greetings_20261010/", Key="EnterGreetingProbe";
    static Chapter2Controller c;
    static Chapter2Exploration ex;
    static Report report;
    static int phase, requests;
    static float next, started;
    static bool release;
    static HttpListener listener;
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    [Serializable] class Report
    {
        public bool passed,playing,dirty;
        public int errors,warnings,requests;
        public string error;
        public List<string> checks=new List<string>();
    }
    static Chapter2EnterProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string msg,string stack,LogType type)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(type==LogType.Warning)SessionState.SetInt(Key+"w",SessionState.GetInt(Key+"w",0)+1);
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Key+"e",SessionState.GetInt(Key+"e",0)+1);
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){report=new Report();phase=requests=0;started=Time.realtimeSinceStartup;next=started+1;}
        if(state==PlayModeStateChange.ExitingPlayMode)
        {
            listener?.Close();listener=null;
            if(report!=null){report.requests=requests;SessionState.SetString(Key+"report",JsonUtility.ToJson(report));}
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.playing=Application.isPlaying;report.dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
            report.errors=SessionState.GetInt(Key+"e",0);report.warnings=SessionState.GetInt(Key+"w",0);
            File.WriteAllText(Dir+"result.json",JsonUtility.ToJson(report,true));SessionState.SetBool(Key,false);report=null;
        }
    }
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks.Add(label);}
    static void After(int step,float seconds=.3f){phase=step;next=Time.realtimeSinceStartup+seconds;}
    static void Return(KeyCode key=KeyCode.Return)
    {
        var game=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        game.Focus();game.SendEvent(new Event{type=EventType.KeyDown,keyCode=key,character='\n'});
        game.SendEvent(new Event{type=EventType.KeyUp,keyCode=key});
    }
    static void StartLocalReply()
    {
        listener=new HttpListener();listener.Prefixes.Add("http://127.0.0.1:18773/");listener.Start();var server=listener;
        Task.Run(async()=>
        {
            try
            {
                while(server.IsListening)
                {
                    var context=await server.GetContextAsync();
                    using(var reader=new StreamReader(context.Request.InputStream))await reader.ReadToEndAsync();
                    System.Threading.Interlocked.Increment(ref requests);
                    await Task.Delay(1500);
                    byte[] data=Encoding.UTF8.GetBytes("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"測試收到問題。\"}}]}");
                    context.Response.ContentType="application/json; charset=utf-8";context.Response.ContentLength64=data.Length;
                    await context.Response.OutputStream.WriteAsync(data,0,data.Length);context.Response.Close();
                }
            }catch(Exception){if(server.IsListening)throw;}
        });
        var client=c.GetComponent<Chapter2LocalDialogue>();client.StopAllCoroutines();
        typeof(Chapter2LocalDialogue).GetField("cloudConfig",Private).SetValue(client,null);
        typeof(Chapter2LocalDialogue).GetField("<Ready>k__BackingField",Private).SetValue(client,true);
        client.settings.endpoint="http://127.0.0.1:18773/v1/chat/completions";client.settings.autoStart=false;
    }
    static void NearGuide()
    {
        var npc=c.workers[0].GetComponent<Chapter2AmbientNPC>();npc.patrolEnabled=false;
        for(int i=0;i<16;i++)
        {
            Vector3 pos=npc.transform.position+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*2.6f;
            c.player.worldBoundary.GroundHeight(pos,out float y);pos.y=y+.08f;
            if(Physics.OverlapCapsule(pos+Vector3.up*.45f,pos+Vector3.up*1.35f,.4f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.name!="Forest ground"&&!h.transform.IsChildOf(c.player.transform)))continue;
            c.player.Warp(pos,npc.transform.position);Check(ex.TryOpen(npc),"guide chat opens");return;
        }
        throw new Exception("No clear guide position");
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            if(cmd=="refresh")AssetDatabase.Refresh();
            if(cmd=="status")File.WriteAllText(Dir+"status.json","{\"playing\":"+(Application.isPlaying?"true":"false")+",\"failed\":"+(EditorUtility.scriptCompilationFailed?"true":"false")+"}");
            if(cmd=="run"&&!Application.isPlaying){SessionState.SetBool(Key,true);SessionState.SetInt(Key+"e",0);SessionState.SetInt(Key+"w",0);EditorApplication.isPlaying=true;}
            if(cmd=="stop")EditorApplication.isPlaying=false;
        }
        if(!Application.isPlaying||report==null)return;
        try
        {
            if(!c){c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;c.saveResult=false;}
            if(release){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
            if(Time.realtimeSinceStartup-started>90)throw new Exception("Timeout at "+phase);
            if(Time.realtimeSinceStartup<next)return;
            ex=c.exploration;
            if(phase==0){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Space));release=true;After(1,1);return;}
            if(phase==1)
            {
                if(!ex.Active)return;
                var bubbles=c.ui.canvas.GetComponentsInChildren<Chapter2GreetingBubble>(true);
                Check(bubbles.Length==9,"all 9 greeting pools present");
                foreach(var b in bubbles)
                {
                    var a=b.Npc.GetComponent<Chapter2Actor>();var lines=(string[])typeof(Chapter2GreetingBubble).GetField("greetingOptions",Private).GetValue(b);
                    bool ordinary=!a.police&&!a.isChild&&a!=c.workers[0];
                    Check(lines.Contains("早安！")==ordinary&&lines.Contains("你好！")==ordinary,"greeting restriction: "+a.DisplayName);
                }
                StartLocalReply();NearGuide();After(2);return;
            }
            if(phase==2){Check(ex.QuestionInput.isFocused,"field initially focused");ex.QuestionInput.text="   ";Return();After(3);return;}
            if(phase==3){Check(requests==0&&ex.QuestionInput.isFocused,"blank Enter ignored, focus retained");ex.QuestionInput.text="點別處不能送出";EventSystem.current.SetSelectedGameObject(null);After(4);return;}
            if(phase==4){Check(requests==0,"blur does not submit");ex.QuestionInput.ActivateInputField();After(5);return;}
            if(phase==5){ex.QuestionInput.text="中文選字";Keyboard.current.OnIMECompositionChanged(new IMECompositionString("ㄓ"));Return();After(6);return;}
            if(phase==6){Check(requests==0&&ex.QuestionInput.text=="中文選字"&&ex.QuestionInput.isFocused,"IME Enter does not submit or clear text");Keyboard.current.OnIMECompositionChanged(new IMECompositionString(""));Return();After(7);return;}
            if(phase==7){Check(requests==0,"composition commit Enter does not submit");ex.QuestionInput.text="你在這裡做什麼？";Return();After(8,.4f);return;}
            if(phase==8){Check(requests==1&&ex.WaitingForReply&&ex.QuestionInput.text=="","Return sends exactly one question");Return();After(9,2);return;}
            if(phase==9){Check(requests==1&&!ex.WaitingForReply&&ex.LastReply=="測試收到問題。"&&ex.QuestionInput.isFocused,"no duplicate while waiting; answer restores focus");ex.QuestionInput.text="可以繼續問嗎？";Return(KeyCode.KeypadEnter);After(10,2);return;}
            if(phase==10)
            {
                Check(requests==2&&ex.LastReply=="測試收到問題。"&&ex.QuestionInput.isFocused,"keypad Enter follow-up succeeds");
                ex.QuestionInput.text="保留滑鼠送出";
                var send=(UnityEngine.UI.Button)typeof(Chapter2Exploration).GetField("send",Private).GetValue(ex);
                ExecuteEvents.Execute(send.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                After(11,2);return;
            }
            if(phase==11)
            {
                Check(requests==3&&!ex.WaitingForReply,"mouse Send still works");
                ex.QuestionInput.text="時間到不能送出";typeof(Chapter2Exploration).GetField("deadline",Private).SetValue(ex,Time.unscaledTime-1);
                Return();After(12);return;
            }
            if(phase==12){Check(requests==3,"expired exploration blocks new question");Check(c.explorationSeconds==180,"formal exploration remains 180 seconds");report.passed=true;EditorApplication.isPlaying=false;}
        }
        catch(Exception e){report.error=e.ToString();EditorApplication.isPlaying=false;}
    }
}
