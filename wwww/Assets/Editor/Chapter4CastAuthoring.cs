using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Adds batch 2 to the existing scene, preserving the grounded environment.
public static class Chapter4CastAuthoring
{
    [MenuItem("Tools/Chapter 4/Add Opening and Evacuation Cast")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene changes first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Chapter4SceneAuthoring.ScenePath) scene = EditorSceneManager.OpenScene(Chapter4SceneAuthoring.ScenePath);
        var chapter = Object.FindFirstObjectByType<Chapter4SceneController>();
        if (!chapter || chapter.evacuation || Object.FindFirstObjectByType<Chapter4Evacuation>())
            throw new InvalidOperationException("Chapter 4 missing or batch 2 already installed; existing cast was not replaced.");
        var donor = EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var sources = donor.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Chapter2Actor>(true)).ToArray();
            var flow = chapter.gameObject.AddComponent<Chapter4Evacuation>();
            chapter.evacuation = flow;
            flow.chapter = chapter;
            flow.terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).First(c => c.name == "Forest valley ground");
            var root = new GameObject("04 開場人物與撤離隊伍").transform;
            flow.cast = new[]
            {
                Create(sources, flow, root, "比胡", "pihu", "比胡·帖木", new Vector3(2.1f, 0, -2.8f), true),
                Create(sources, flow, root, "瓦旦", "wounded", "受傷族人", new Vector3(1.5f, 0, -.5f), true),
                Create(sources, flow, root, "達多", "tado", "達多·莫那", new Vector3(2.2f, 0, 1.6f)),
                Create(sources, flow, root, "古慕", "gumu", "古慕·阿基", new Vector3(-2.2f, 0, -3.7f)),
                Create(sources, flow, root, "露比", "lubi", "露比·巴干", new Vector3(-1.8f, 0, -5.2f)),
                Create(sources, flow, root, "莫那", "mona", "莫那·魯道", new Vector3(0, 0, 1.8f)),
                Create(sources, flow, root, "巴萬", "bawan", "巴萬·拿威", new Vector3(-1.5f, 0, -.4f)),
                Create(sources, flow, root, "歐冰", "obing", "歐冰·拿威", new Vector3(-.3f, 0, -6.8f))
            };
            var points = chapter.routeCheckpoints;
            int Closest(Vector3 point) => Enumerable.Range(1, points.Length - 1).OrderBy(i => Chapter4Evacuation.FlatDistance(points[i].position, point)).First();
            flow.legs = new[]
            {
                new Chapter4Evacuation.Leg { title = "穿過第一岔路，確認前方彎道", routeIndex = Closest(new Vector3(25, 0, 43)) },
                new Chapter4Evacuation.Leg { title = "越過中段岔路，確認上坡通路", routeIndex = Closest(new Vector3(1, 0, 78)) },
                new Chapter4Evacuation.Leg { title = "穿過岩肩，確認林路出口", routeIndex = Closest(new Vector3(17, 0, 108)) },
                new Chapter4Evacuation.Leg { title = "走到集合地，示意全隊靠攏", routeIndex = points.Length - 1 }
            };
            chapter.presentation.EnsureStoryUI();
            chapter.presentation.SetObjective("聽莫那與巴萬交代撤離");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Chapter4] Added eight authored characters and four player-led route segments to the existing scene.");
        }
        finally { EditorSceneManager.ClosePreviewScene(donor); }
    }

    static Chapter4Companion Create(Chapter2Actor[] sources, Chapter4Evacuation flow, Transform parent,
        string sourceName, string id, string name, Vector3 position, bool wounded = false)
    {
        var source = sources.FirstOrDefault(a => a.characterName.StartsWith(sourceName));
        if (!source) throw new InvalidOperationException("Existing character model not found: " + sourceName);
        var go = new GameObject(name, typeof(CharacterController));
        go.transform.SetParent(parent, false);
        if (!flow.terrain.Raycast(new Ray(position + Vector3.up * 30, Vector3.down), out RaycastHit hit, 60))
            throw new InvalidOperationException("No camp ground for " + name);
        go.transform.position = new Vector3(position.x, hit.point.y + .025f, position.z);
        var motor = go.GetComponent<CharacterController>();
        motor.height = 1.68f; motor.center = new Vector3(0, .86f, 0); motor.radius = .23f;
        motor.stepOffset = .25f; motor.skinWidth = .025f; motor.slopeLimit = 45;
        var actor = go.AddComponent<Chapter4Companion>();
        actor.actorId = id; actor.displayName = name; actor.wounded = wounded;
        actor.walkSpeed = wounded ? 1.55f : 1.75f;
        var model = Object.Instantiate(source.gameObject, go.transform);
        model.name = "人物外觀"; model.SetActive(true);
        // Remove RequireComponent dependants before their actor dependency.
        foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true).OrderBy(s => s is Chapter2Actor ? 1 : 0))
            Object.DestroyImmediate(script);
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (var audio in model.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(audio);
        foreach (var t in model.GetComponentsInChildren<Transform>(true).Where(t =>
            t.name.Contains("佩刀") || t.name.Contains("刀鞘") || t.name.Contains("步槍") || t.name.Contains("村田")).ToArray())
            if (t) Object.DestroyImmediate(t.gameObject);
        model.transform.localPosition = Vector3.zero;
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.position += go.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        var animator = model.GetComponentInChildren<Animator>();
        if (!animator) throw new InvalidOperationException("Character has no existing skeletal animation rig: " + name);
        animator.applyRootMotion = false;
        actor.model = model.transform;
        return actor;
    }
}
