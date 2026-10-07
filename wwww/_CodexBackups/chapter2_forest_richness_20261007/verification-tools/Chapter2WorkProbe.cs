using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Chapter2WorkProbe
{
    const string Dir = "_CodexBackups/chapter2_forest_richness_20261007/";
    static Chapter2WorkProbe() { EditorApplication.update += Tick; Application.logMessageReceived+=Log; }
    static void Log(string message,string trace,LogType type) { File.AppendAllText(Dir+"unity-log.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+(type==LogType.Exception?trace:"")); }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Dir + "command.txt")) return;
        string command;
        try {command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}
        catch(IOException){return;}
        try
        {
            if(command=="refresh") AssetDatabase.Refresh();
            else if(command=="save-arrival") { if(Application.isPlaying)throw new Exception("Edit mode required"); var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>(); c.treeArrivalDistance=1.5f; EditorUtility.SetDirty(c); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); } else if(command=="fearfaces") Chapter2FearFaceAuthoring.Bake();
            else if(command=="details") Chapter2ForestDetailAuthoring.Apply(); else if(command=="enrich") typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2ForestRichnessAuthoring").GetMethod("Apply").Invoke(null,null);
            else if(command=="reload") EditorSceneManager.OpenScene("Assets/Scenes/第二章.unity"); else if(command=="audit") typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2ForestMetrics").GetMethod("Audit").Invoke(null,null); else if(command.StartsWith("review:")) typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2ForestReview").GetMethod("Run").Invoke(null,new object[]{command.Substring(7)}); else if(command=="inspect") File.WriteAllText(Dir+"scene-status.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" pipeline="+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name);
            
            else if (command == "play") EditorApplication.isPlaying = true;
            else if (command == "film") typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2FilmAuthoring").GetMethod("Record").Invoke(null, null);
            else if (command == "stop") EditorApplication.isPlaying = false;
            else if (command.StartsWith("shot:")) Shot(command.Substring(5));
            else if (command.StartsWith("test:")) typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2Verification").GetMethod("Run").Invoke(null, new object[] { command.Substring(5) });
            File.WriteAllText(Dir + "last-command.txt", DateTime.Now.ToString("s") + " " + command + " OK");
        }
        catch (Exception e) { File.WriteAllText(Dir + "last-command.txt", command + " FAILED " + e); Debug.LogException(e); }
    }
    public static void Shot(string name)
    {
        var camera = Camera.main;
        var old = camera.targetTexture;
        var target = new RenderTexture(1600, 900, 24);
        camera.targetTexture = target; camera.Render();
        var active = RenderTexture.active; RenderTexture.active = target;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        File.WriteAllBytes(Dir + name + ".png", tex.EncodeToPNG());
        camera.targetTexture = old; RenderTexture.active = active;
        UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(target);
    }
}
