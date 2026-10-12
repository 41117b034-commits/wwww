using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3WorkProbe
{
    public const string Dir="_CodexBackups/chapter3_village_20261012/";
    static double next;
    static Chapter3WorkProbe(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.5;
        string path=Dir+"command.txt";if(!File.Exists(path))return;
        string command=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(command=="build")Chapter3VillageAuthoring.Build();
            else if(command=="recover-build")
            {
                var scene=SceneManager.GetActiveScene();
                var c=Object.FindFirstObjectByType<Chapter3Controller>();
                if(scene.path!=""||!c||c.player||c.ui)throw new Exception("Not the incomplete Chapter 3 build; refusing to reset scene.");
                EditorSceneManager.SaveScene(scene,Dir+"partial-build.unity",true);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                Chapter3VillageAuthoring.Build();
            }
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="refresh")AssetDatabase.Refresh();
            else if(command=="shot")Shot("current");
            else if(command=="polish")Chapter3VillageAuthoring.PolishExisting();
            else if(command=="final-prep")
            {
                if(Application.isPlaying)throw new Exception("Stop first");
                var c=Object.FindFirstObjectByType<Chapter3Controller>();
                foreach(var t in c.pihu.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="比胡的布包").ToArray())Object.DestroyImmediate(t.gameObject);
                foreach(var a in Object.FindObjectsByType<Chapter2Actor>(FindObjectsSortMode.None))
                {
                    var bones=a.GetComponentsInChildren<Transform>(true);
                    var l=bones.FirstOrDefault(t=>t.name.EndsWith("L_Upperarm")||t.name.EndsWith("LeftArm"));
                    var r=bones.FirstOrDefault(t=>t.name.EndsWith("R_Upperarm")||t.name.EndsWith("RightArm"));
                    if(l&&r){Vector3 f=Vector3.Cross(r.position-l.position,Vector3.up).normalized;a.transform.rotation=Quaternion.FromToRotation(f,Vector3.back)*a.transform.rotation;}
                }
                EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
            }
            else if(command=="menu-play")
            {
                if(Application.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Editor not clean");
                EditorSceneManager.OpenScene("Assets/Scenes/選擇章節ˊ.unity");EditorApplication.isPlaying=true;
            }
            else if(command=="inspect")Inspect();
            else if(command=="test-real"||command=="test-fallback")Chapter3PlayProbe.Begin(command);
            File.WriteAllText(Dir+"done-"+command+".txt",DateTime.Now.ToString("O"));
        }
        catch(Exception e){File.WriteAllText(Dir+"error-"+command+".txt",e.ToString());}
    }
    static void Inspect()
    {
        var c=Object.FindFirstObjectByType<Chapter3Controller>();
        File.WriteAllText(Dir+"inspect.txt","Scene="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty+" playing="+Application.isPlaying+" compilationFailed="+EditorUtility.scriptCompilationFailed+"\n"+
            (c?"stage="+c.CurrentStage+" turns="+c.CompletedTurns+" generated="+c.GeneratedReplies+" fallback="+c.FallbackReplies+" reply="+c.LastReply+"\n"+string.Join("\n",Object.FindObjectsByType<Chapter2Actor>(FindObjectsSortMode.None).Select(a=>a.DisplayName+" "+a.transform.position+" scale="+a.transform.lossyScale)):"no chapter"));
    }
    public static void Shot(string name)
    {
        var c=Object.FindFirstObjectByType<Chapter3Controller>();if(!c)throw new Exception("No chapter");
        Canvas.ForceUpdateCanvases();var cam=c.player.view;var old=cam.targetTexture;var active=RenderTexture.active;float oldAspect=cam.aspect;
        var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=oldAspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
}
