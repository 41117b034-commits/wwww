using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ChapterMenuBlockingProbe
{
    const string Dir="_CodexBackups/chapter_menu_blocking_20261011/";
    static ChapterMenuBlockingProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Warning)File.AppendAllText(Dir+"unity-log.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+(type==LogType.Exception?trace:""));}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try
        {
            if(cmd=="inspect")Inspect();
            else if(cmd=="open-menu")Open("Assets/Scenes/選擇章節ˊ.unity");
            else if(cmd=="open-chapter")Open("Assets/Scenes/第二章.unity");
            else if(cmd=="apply-menu")ChapterSelectionAuthoring.Apply();
            else if(cmd=="play")EditorApplication.isPlaying=true;
            else if(cmd=="stop")EditorApplication.isPlaying=false;
            else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
            File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
        }catch(Exception e){File.WriteAllText(Dir+"last-command.txt",cmd+" FAILED "+e);Debug.LogException(e);}
    }
    static void Open(string path)
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Scene must be stopped and saved before switching.");
        EditorSceneManager.OpenScene(path);
    }
    static void Inspect()
    {
        var s=new System.Text.StringBuilder();var scene=SceneManager.GetActiveScene();
        s.AppendLine("scene="+scene.name+" playing="+Application.isPlaying+" dirty="+scene.isDirty+" compilationFailed="+EditorUtility.scriptCompilationFailed);
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(c){s.AppendLine("stage="+c.CurrentStage+" tree="+c.sacredTree.position+" approach="+c.treeApproach.position+" fire="+c.campfire.position+" mona="+c.mona.transform.position);
            foreach(var a in c.workers)s.AppendLine(a.name+"="+a.transform.position);
            foreach(var col in c.sacredTree.GetComponentsInChildren<Collider>())s.AppendLine("trunk collider "+col.GetType().Name+"="+col.bounds);}
        foreach(var b in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {s.AppendLine("button="+b.name+" text="+string.Join("/",b.GetComponentsInChildren<TMP_Text>(true).Select(t=>t.text))+" pos="+b.transform.position);for(int i=0;i<b.onClick.GetPersistentEventCount();i++)s.AppendLine("  "+b.onClick.GetPersistentMethodName(i));}
        File.WriteAllText(Dir+"state.txt",s.ToString());
    }
    public static void Shot(string name)
    {
        var cam=Camera.main;if(!cam)cam=Object.FindFirstObjectByType<Camera>();
        var old=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
}
