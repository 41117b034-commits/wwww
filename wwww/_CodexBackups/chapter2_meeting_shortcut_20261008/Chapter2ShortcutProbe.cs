using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class Chapter2ShortcutProbe
{
    const string Dir="_CodexBackups/chapter2_meeting_shortcut_20261008/";
    static Chapter2Controller observed;
    static bool captured;
    static bool releaseKey;
    static Chapter2ShortcutProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string message,string trace,LogType type)
    {File.AppendAllText(Dir+"unity-log.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+(type==LogType.Exception?trace:""));}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(releaseKey && Keyboard.current!=null){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());releaseKey=false;}
        if(Application.isPlaying && c && c!=observed)
        {
            observed=c;captured=false;
            c.StageChanged+=stage=>File.AppendAllText(Dir+"stages.txt",DateTime.Now.ToString("s")+" "+stage+"\n");
        }
        if(Application.isPlaying && c && !captured && c.CurrentStage==Chapter2Controller.Stage.Meeting && c.ui.fade.color.a==0 && c.CameraBeat=="council-overview")
        {captured=true;Shot("meeting-overview-"+DateTime.Now.ToString("HHmmss"));Snapshot(c);}
        if(!File.Exists(Dir+"command.txt"))return;
        var command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try
        {
            if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="inspect")Snapshot(c);
            else if(command.StartsWith("key:")){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(command=="key:p"?Key.P:command=="key:space"?Key.Space:command=="key:1"?Key.Digit1:Key.Digit2));releaseKey=true;}
            else if(command=="refresh")AssetDatabase.Refresh();
            else if(command.StartsWith("shot:"))Shot(command.Substring(5));
            File.WriteAllText(Dir+"last-command.txt",DateTime.Now.ToString("s")+" "+command+" OK");
        }catch(Exception e){Debug.LogException(e);}
    }
    static void Snapshot(Chapter2Controller c)
    {
        File.WriteAllText(Dir+"state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+
            " stage="+(c?c.CurrentStage.ToString():"none")+" beat="+(c?c.CameraBeat:"")+"\n"+
            (c?"video="+c.ui.videoImage.gameObject.activeSelf+" day="+c.dayGroup.activeSelf+" night="+c.nightGroup.activeSelf+
            " axe="+c.axe.activeSelf+" meter="+c.ui.meterPanel.activeSelf+" choices="+c.ui.choicePanel.activeSelf+
            " subtitle="+c.ui.subtitle.text+" fade="+c.ui.fade.color.a+" cuts="+c.ValidCuts+"\n":"")+
            "keyboard="+Keyboard.current+" focused="+Application.isFocused+" editorFocus="+EditorWindow.focusedWindow+"\n"+
            "shortcut="+(c?typeof(Chapter2Controller).GetField("meetingShortcutUsed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(c):null)+" flow="+(c?typeof(Chapter2Controller).GetField("flow",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(c):null)+"\n"+
            "result="+PlayerPrefs.GetString("WusheEvent.Chapter2.Result","<missing>"));
    }
    static void Shot(string name)
    {
        var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(target);}
    }
}
