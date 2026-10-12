using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class Chapter2FinalPerformanceProbe
{
    public const string Dir="_CodexBackups/chapter2_final_performance_20261012/";
    static Chapter2FinalPerformanceProbe(){EditorApplication.update+=Tick;File.WriteAllText(Dir+"loaded.txt",DateTime.Now.ToString("s"));}
    static float previewAt=-1; static bool previewSent;
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(Application.isPlaying&&SessionState.GetBool("c2-preview",false)){
            var c=Object.FindFirstObjectByType<Chapter2Controller>();
            if(c&&!previewSent&&Time.time>1){UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.P));previewSent=true;}
            else if(previewSent)UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            if(c&&c.CurrentStage==Chapter2Controller.Stage.Meeting){if(previewAt<0)previewAt=Time.time;if(Time.time-previewAt>2.8f){
                foreach(var a in c.nightGroup.GetComponentsInChildren<Chapter2Actor>())Portrait(a,"before-"+a.name,false);
                SessionState.SetBool("c2-preview",false);EditorApplication.isPaused=true;
            }}
        }
        if(!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try{
            if(cmd=="audit")Audit();
            else if(cmd=="play")EditorApplication.isPlaying=true;
            else if(cmd=="preview"){SessionState.SetBool("c2-preview",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
            else if(cmd=="stop")EditorApplication.isPlaying=false;
            else if(cmd.StartsWith("call:")){var t=typeof(Chapter2FinalPerformanceProbe).Assembly.GetType(cmd.Substring(5));t.GetMethod("Apply").Invoke(null,null);}
            else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
            File.WriteAllText(Dir+"last.txt",cmd+" OK");
        }catch(Exception e){File.WriteAllText(Dir+"last.txt",cmd+" "+e);Debug.LogException(e);}
    }
    public static void Portrait(Chapter2Actor actor,string name,bool face){
        var c=Object.FindFirstObjectByType<Chapter2Controller>();var cam=c.player.view;var p=cam.transform.position;var q=cam.transform.rotation;float fov=cam.fieldOfView;
        var target=face?actor.Rig.Head.position+Vector3.up*.03f:actor.Rig.Hips.position+Vector3.up*.18f;
        cam.transform.position=target+actor.Rig.Forward*(face?.62f:2.25f);cam.transform.LookAt(target);cam.fieldOfView=face?40:43;
        var canvas=c.ui.canvas;bool active=canvas.enabled;canvas.enabled=false;
        try{Shot(name);}finally{canvas.enabled=active;cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=fov;}
    }
    public static void Shot(string name){
        var c=Object.FindFirstObjectByType<Chapter2Controller>();var cam=c.player.view;
        var old=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit(){
        var c=Object.FindFirstObjectByType<Chapter2Controller>();var s=new StringBuilder();
        s.AppendLine("playing="+Application.isPlaying+" dirty="+c.gameObject.scene.isDirty+" failed="+EditorUtility.scriptCompilationFailed);
        foreach(var a in c.dayGroup.GetComponentsInChildren<Chapter2Actor>(true).Concat(c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true))){
            s.AppendLine("ACTOR "+a.name+" pos="+a.transform.position+" scale="+a.transform.localScale+" facing="+a.facing+" child="+a.isChild+" police="+a.police);
            foreach(var skin in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))s.AppendLine("  "+skin.name+" mesh="+AssetDatabase.GetAssetPath(skin.sharedMesh)+" vertices="+skin.sharedMesh.vertexCount+" bounds="+skin.bounds+" shapes="+skin.sharedMesh.blendShapeCount+" bones="+string.Join(",",skin.bones.Select(b=>b?b.name:"null")));
        }
        File.WriteAllText(Dir+"audit.txt",s.ToString());
    }
}
