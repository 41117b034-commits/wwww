using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Chapter2CouncilSeatRefinement
{
    [MenuItem("Tools/Chapter 2/Align Bihu With Council Circle")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var actor=c.conservatives[0];
        // This seat retains its original authoring name after the cast received names.
        var seat=c.nightGroup.transform.Find("會議座椅 · 會議戰士 · 捲髮青年");
        if(!seat)throw new InvalidOperationException("Bihu's council seat is missing.");
        actor.transform.position=c.campfire.position+new Vector3(-2.53f,0,-2.12f);
        actor.facing=c.campfire.position-actor.transform.position;
        actor.seatedHipHeight=.54f;
        seat.position=actor.transform.position+actor.facing.normalized*.04f+Vector3.up*.22f;
        EditorUtility.SetDirty(actor);EditorUtility.SetDirty(actor.transform);EditorUtility.SetDirty(seat);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
    }
}
