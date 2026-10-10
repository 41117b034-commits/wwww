using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class Chapter2ChildNPCAuthoring
{
    public const string PrefabPath = "Assets/Chapter2/Characters/原住民小孩.prefab";
    const string SourceScene = "Assets/Scenes/第一章新版警察.unity";
    const string GroupName = "Background people · local patrols";
    public const string ChildName = "林間原住民小孩";

    public static GameObject LoadChild()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab) return prefab;
        var active = SceneManager.GetActiveScene();
        var source = SceneManager.GetSceneByPath(SourceScene);
        bool opened = !source.IsValid() || !source.isLoaded;
        GameObject child = null;
        try
        {
            if (opened) source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);
            var donor = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "原住民小孩(2)");
            if (!donor) throw new InvalidOperationException("Original indigenous child was not found.");
            child = Object.Instantiate(donor.gameObject);
            child.name = ChildName;
            SceneManager.MoveGameObjectToScene(child, active);
            foreach (var b in child.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
            foreach (var collider in child.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var t in child.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var animator in child.GetComponentsInChildren<Animator>(true))
            { animator.runtimeAnimatorController = null; animator.enabled = false; }
            child.SetActive(true);
            child.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var skins = child.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skins.Length == 0 || !child.GetComponentInChildren<Animator>())
                throw new InvalidOperationException("Child model must contain a skin and animator.");
            Bounds bounds = skins[0].bounds;
            foreach (var skin in skins) bounds.Encapsulate(skin.bounds);
            child.transform.localScale *= 1.22f / bounds.size.y;
            child.AddComponent<Chapter2Actor>();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            prefab = PrefabUtility.SaveAsPrefabAsset(child, PrefabPath);
        }
        finally
        {
            if (child) Object.DestroyImmediate(child);
            if (opened && source.IsValid()) EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(active);
        }
        return prefab;
    }

    [MenuItem("Tools/Chapter 2/Replace Left Background NPC with Child")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var c = Object.FindFirstObjectByType<Chapter2Controller>();
        if (!c || c.gameObject.scene.path != "Assets/Scenes/第二章.unity")
            throw new InvalidOperationException("Open Chapter 2 first.");
        var parent = c.dayGroup.transform.Find(GroupName);
        var old = parent ? parent.Find("林間族人 1") : null;
        if (!old && parent && parent.Find(ChildName)) return;
        if (!old) throw new InvalidOperationException("Left background NPC was not found.");
        var source = LoadChild();
        var child = Object.Instantiate(source, parent);
        child.name = ChildName;
        child.transform.position = old.position;
        child.GetComponent<Chapter2Actor>().facing = old.GetComponent<Chapter2Actor>().facing;
        var npc = child.AddComponent<Chapter2AmbientNPC>();
        EditorUtility.CopySerialized(old.GetComponent<Chapter2AmbientNPC>(), npc);
        foreach (var skin in child.GetComponentsInChildren<SkinnedMeshRenderer>())
        { skin.updateWhenOffscreen = false; skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        Object.DestroyImmediate(old.gameObject);
        c.ui.meterPanel.name = "Chopping progress";
        c.ui.meter.name = "Remaining cuts";
        c.ui.SetMeter(0, 0);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        EditorSceneManager.SaveScene(c.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
}
