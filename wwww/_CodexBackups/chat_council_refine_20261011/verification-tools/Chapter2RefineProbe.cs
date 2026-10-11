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
public static class Chapter2RefineProbe
{
    const string Dir="_CodexBackups/chat_council_refine_20261011/";
    static Chapter2RefineProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Warning)File.AppendAllText(Dir+"unity-log.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+(type==LogType.Exception?trace:""));}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try
        {
            if(cmd=="inspect")Inspect(); else if(cmd=="actors")Actors(); else if(cmd=="apply") { Chapter2CouncilKnifeAuthoring.Apply(); Chapter2CouncilSeatRefinement.Apply(); }
            else if(cmd=="open-menu")Open("Assets/Scenes/選擇章節ˊ.unity");
            else if(cmd=="open-chapter")Open("Assets/Scenes/第二章.unity");
            else if(cmd=="apply-menu")ChapterSelectionAuthoring.Apply();
            else if(cmd=="play")EditorApplication.isPlaying=true;
            else if(cmd=="stop")EditorApplication.isPlaying=false;
            else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
            File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
        }catch(Exception e){File.WriteAllText(Dir+"last-command.txt",cmd+" FAILED "+e);Debug.LogException(e);}
    }
    static void Actors()
    {
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        var s=new System.Text.StringBuilder();
        s.AppendLine("fire="+c.campfire.position);
        foreach(var a in c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true)) {
            var d=a.GetComponent<Chapter2CouncilDrama>(); if(d&&d.gripMesh)s.AppendLine("grip="+AssetDatabase.GetAssetPath(d.gripMesh)+" bounds="+d.gripMesh.bounds); if(a.Rig)s.AppendLine("hips="+a.Rig.Hips.name+" pos="+a.Rig.Hips.position+" head="+a.Rig.Head.position+" height="+a.Rig.Height); s.AppendLine(a.name+" display="+a.DisplayName+" pos="+a.transform.position+" scale="+a.transform.localScale+" hipHeight="+a.seatedHipHeight);
            foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))s.AppendLine("mesh="+AssetDatabase.GetAssetPath(r.sharedMesh)+" bounds="+r.bounds);
            foreach(var t in a.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Hip")||t.name.Contains("Pelvis")||t.name.Contains("Waist")||t.name.Contains("Spine")||t.name.Contains("Head")))s.AppendLine("bone="+t.name+" pos="+t.position+" parent="+t.parent.name+" local="+t.localPosition+" scale="+t.localScale);
        }
        foreach(var t in c.nightGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("座椅")||t.name=="Council seat"))s.AppendLine("seat="+t.name+" pos="+t.position+" scale="+t.localScale);
        File.WriteAllText(Dir+"actors.txt",s.ToString());
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





