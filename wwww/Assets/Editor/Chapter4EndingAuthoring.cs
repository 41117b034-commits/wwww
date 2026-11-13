using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter4EndingAuthoring
{
    [MenuItem("Tools/第四章/追加第四批聯絡與結局")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save the active scene first.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/第四章.unity");
        var c = UnityEngine.Object.FindFirstObjectByType<Chapter4SceneController>();
        if (!c || !c.gas || !c.evacuation) throw new InvalidOperationException("Chapter 4 batch 3 is required.");
        if (c.contact || c.ending || c.GetComponent<Chapter4Contact>() || c.GetComponent<Chapter4Ending>())
            throw new InvalidOperationException("Batch 4 already exists; refusing to overwrite it.");
        c.contact = c.gameObject.AddComponent<Chapter4Contact>(); c.contact.chapter = c;
        c.ending = c.gameObject.AddComponent<Chapter4Ending>(); c.ending.chapter = c;
        c.presentation.EnsureEndingUI();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("[Chapter4] Added two contact windows, real reunion and shared aftermath ending.");
    }
}
