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
    const string Dir = "_CodexBackups/chapter2_grief_20261005/";
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
            if(command=="settings-text") {var objects=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget("ProjectSettings/EditorSettings.asset"); UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(objects,"ProjectSettings/EditorSettings.asset",true); File.WriteAllText(Dir+"editor-serialization-mode.txt",EditorSettings.serializationMode.ToString());} else if (command == "pack-grief") PackGrief(); else if (command == "bake-grief") BakeGrief(); else if (command == "upgrade") Chapter2InteractionAuthoring.UpdateCurrent(); else if (command == "inspect") Inspect();
            else if (command == "build") typeof(Chapter2WorkProbe).Assembly.GetType("Chapter2SceneAuthoring").GetMethod("Build").Invoke(null, null);
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
    static void PackGrief()
    {
        foreach(var path in Directory.GetFiles("Assets/Chapter2/Resources/GriefHands","*-mesh.asset"))
        {
            var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().First(); var mesh=UnityEngine.Object.Instantiate(source); MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Medium);
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{mesh},path,false);UnityEngine.Object.DestroyImmediate(mesh);
            AssetDatabase.ImportAsset(path);
            var loaded=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().First();
            var pose=AssetDatabase.LoadAssetAtPath<Chapter2GriefHandPose>(path.Replace("-mesh.asset",".asset"));
            pose.mesh=loaded;pose.fistShape=loaded.blendShapeCount-1;EditorUtility.SetDirty(pose);AssetDatabase.SaveAssetIfDirty(pose);
            File.AppendAllText(Dir+"packed-meshes.txt",path+" shapes="+loaded.blendShapeCount+" readable="+loaded.isReadable+" size="+new FileInfo(path).Length+"\n");
        }
    }
    static void BakeGrief()
    {
        if(Application.isPlaying)throw new Exception("Bake in Edit Mode.");
        const string output="Assets/Chapter2/Resources/GriefHands";
        Directory.CreateDirectory(output);AssetDatabase.Refresh();
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        
        try
        {
            
            for(int i=1;i<c.workers.Length;i++)
            {
                var clone=UnityEngine.Object.Instantiate(c.workers[i].gameObject);
                try
                {
                    var actor=clone.GetComponent<Chapter2Actor>();
                    typeof(Chapter2Actor).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(actor,null);
                    var reaction=clone.AddComponent<Chapter2GriefReaction>();reaction.Prepare();
                    var palms=(Quaternion[])typeof(Chapter2GriefReaction).GetField("palms",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(reaction);
                    var skin=clone.GetComponentInChildren<SkinnedMeshRenderer>();
                    var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);
                    string key=c.workers[i].GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name;
                    mesh.name=key+" Chapter2 grief";
                    mesh.UploadMeshData(true);
                    UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{mesh},output+"/"+key+"-mesh.asset",false); AssetDatabase.ImportAsset(output+"/"+key+"-mesh.asset"); mesh=AssetDatabase.LoadAssetAtPath<Mesh>(output+"/"+key+"-mesh.asset");
                    var pose=ScriptableObject.CreateInstance<Chapter2GriefHandPose>();
                    pose.mesh=mesh;pose.leftPalm=palms[0];pose.rightPalm=palms[1];pose.fistShape=mesh.blendShapeCount-1;
                    AssetDatabase.CreateAsset(pose,output+"/"+key+".asset");
                }
                finally{UnityEngine.Object.DestroyImmediate(clone);}
            }
            AssetDatabase.SaveAssets();
        }
        finally{}
    }
    static void Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        string report = scene.path + " dirty=" + scene.isDirty + " playing=" + Application.isPlaying + "\n";
        foreach (var root in scene.GetRootGameObjects())
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            report += root.name + " | pos=" + root.transform.position + " | size=" + b.size + " | " + string.Join(",", root.GetComponents<MonoBehaviour>().Where(x=>x).Select(x=>x.GetType().Name)) + "\n";
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)report+="MISSING SCRIPT "+t.name+"\n";
        }
        foreach(var actor in UnityEngine.Object.FindObjectsByType<Chapter2Actor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var animator=actor.GetComponentInChildren<Animator>();report+="\nACTOR "+actor.name+" human="+animator.isHuman+"\n";
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                report+="MESH "+skin.name+" path="+AssetDatabase.GetAssetPath(skin.sharedMesh)+" readable="+skin.sharedMesh.isReadable+" vertices="+skin.sharedMesh.vertexCount+"\n";
                report+=string.Join(",",skin.bones.Select(b=>b?b.name:"NULL"))+"\n";
            }
        }
        File.WriteAllText(Dir + "scene-inspection.txt", report);
    }
}









