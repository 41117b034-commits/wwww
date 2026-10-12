using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3FinalProbe
{
    const string Dir="_CodexBackups/chapter3_village_20261012/";
    static double captureAt;
    static Chapter3FinalProbe(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(SessionState.GetBool("chapter3.gesture",false)&&Application.isPlaying)
            {
                var c=Object.FindFirstObjectByType<Chapter3Controller>();if(!c||!c.pihu.Rig||!c.pihu.Rig.RightHand)return;
                if(captureAt==0)
                {
                    c.StopAllCoroutines();c.dialogue.CancelRequest();c.bawan.speaking=false;c.pihu.speaking=true;
                    c.ui.fade.color=Color.clear;c.ui.chat.SetActive(false);
                    c.player.Warp(new Vector3(2.1f,.05f,1.3f),c.pihu.transform.position+Vector3.up*1.22f);
                    c.player.canMove=c.player.canLook=false;c.pihu.Face(c.player.transform.position);
                    c.pihu.GetComponent<Chapter3CompanionGesture>().AdjustStrap(8);
                    c.ui.Say(c.pihu.DisplayName,"等等我，這條帶子又鬆了。");
                    captureAt=EditorApplication.timeSinceStartup+2;
                }
                else if(EditorApplication.timeSinceStartup>=captureAt)
                {
                    Chapter3WorkProbe.Shot("pihu-gesture-final");
                    float distance=Vector3.Distance(c.pihu.Rig.RightHand.position,c.pihu.Rig.gripTarget.position);
                    File.WriteAllText(Dir+"gesture-final.txt","handToStrapTarget="+distance+" raised="+(c.pihu.Rig.RightHand.position.y-c.pihu.transform.position.y)+"\n");
                    SessionState.SetBool("chapter3.gesture",false);captureAt=0;EditorApplication.isPlaying=false;
                }
            }
            if(!File.Exists(Dir+"final-command.txt"))return;
            string command=File.ReadAllText(Dir+"final-command.txt").Trim();File.Delete(Dir+"final-command.txt");
            if(command=="open"||command=="gesture-play")
            {
                if(Application.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Editor not clean");
                EditorSceneManager.OpenScene(Chapter3VillageAuthoring.ScenePath);
                if(command=="gesture-play"){SessionState.SetBool("chapter3.gesture",true);EditorApplication.isPlaying=true;}
            }
            if(command=="audit")
            {
                var scene=SceneManager.GetActiveScene();var c=Object.FindFirstObjectByType<Chapter3Controller>();
                var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                int missing=transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                int badMesh=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Count(m=>!m.sharedMesh);
                int badSkin=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Count(m=>!m.sharedMesh);
                File.WriteAllText(Dir+"final-audit.txt","scene="+scene.path+"\nplaying="+Application.isPlaying+"\ndirty="+scene.isDirty+"\ncompileFailed="+EditorUtility.scriptCompilationFailed+"\nmissingScripts="+missing+"\nmissingMeshes="+(badMesh+badSkin)+"\nactors="+Object.FindObjectsByType<Chapter2Actor>(FindObjectsSortMode.None).Length+"\nreferences="+(c&&c.player&&c.ui&&c.bawan&&c.pihu&&c.dialogue&&c.knifePickup&&c.trail.Length==10)+"\n");
            }
        }
        catch(Exception e){File.WriteAllText(Dir+"final-error.txt",e.ToString());SessionState.SetBool("chapter3.gesture",false);}
    }
}
