using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2RoamProbe
{
    const string Dir="_CodexBackups/chapter2_named_roam_20261010/";
    static Chapter2RoamProbe(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"command.txt"))return;
        string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
        try
        {
            var c=Object.FindFirstObjectByType<Chapter2Controller>();
            if(cmd=="inspect")
            {
                var actors=c.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Actor>(true));
                File.WriteAllLines(Dir+"cast.txt",new[]{"playing="+Application.isPlaying+" dirty="+c.gameObject.scene.isDirty}.Concat(actors.Select(a=>a.GetInstanceID()+" | "+a.name+" | "+a.transform.parent.name+" | "+a.transform.position+" | police="+a.police+" | patrol="+(bool)a.GetComponent<Chapter2AmbientNPC>()+" | model="+a.GetComponentInChildren<Animator>(true)?.name)));
            }
            else if(cmd=="stop")EditorApplication.isPlaying=false;
            else if(cmd=="apply")
            {
                if(Application.isPlaying)throw new Exception("Exit Play before authoring");
                c.forestEscort.Initialize(c);Chapter2CastNames.Apply(c);
                foreach(var actor in c.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Actor>(true)))
                {
                    EditorUtility.SetDirty(actor);EditorUtility.SetDirty(actor.gameObject);
                    var npc=actor.GetComponent<Chapter2AmbientNPC>();if(npc)EditorUtility.SetDirty(npc);
                }
                EditorUtility.SetDirty(c.forestEscort);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
                EditorSceneManager.SaveScene(c.gameObject.scene);
            }
            else if(cmd=="refresh")AssetDatabase.Refresh();
            File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
        }catch(Exception e){File.WriteAllText(Dir+"last-command.txt",e.ToString());}
    }
}
