using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Appends batch 3 to the saved, grounded scene. It never rebuilds the terrain,
// cast, route, or any previous chapter.
public static class Chapter4GasAuthoring
{
    [MenuItem("Tools/第四章/追加第三批毒氣與固定撤離演出")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save the active scene before authoring chapter 4.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/第四章.unity");
        var chapter = UnityEngine.Object.FindFirstObjectByType<Chapter4SceneController>();
        if (!chapter || !chapter.evacuation) throw new InvalidOperationException("Batch 2 must be present first.");
        if (chapter.gas || chapter.GetComponent<Chapter4GasSequence>())
            throw new InvalidOperationException("Batch 3 already exists. Refusing to overwrite it.");
        const string materialPath = "Assets/Chapter4/Materials/RetreatSmoke.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Chapter4/Materials")) AssetDatabase.CreateFolder("Assets/Chapter4", "Materials");
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) throw new InvalidOperationException("URP particle shader is missing.");
            material = new Material(shader) { name = "RetreatSmoke", renderQueue = (int)RenderQueue.Transparent };
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var gas = chapter.gameObject.AddComponent<Chapter4GasSequence>();
        var atmosphere = chapter.gameObject.AddComponent<Chapter4Atmosphere>();
        gas.chapter = chapter; gas.atmosphere = atmosphere; chapter.gas = gas;
        atmosphere.smokeMaterial = material;
        atmosphere.rifleReport = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/rifle_report.wav");
        if (!atmosphere.rifleReport) throw new InvalidOperationException("Existing rifle audio is missing.");
        chapter.presentation.EnsureGasUI();
        chapter.presentation.SetGasStatus("", 0, 0);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("[Chapter4] Added gas, fixed assistance, rear separation and local retry checkpoints.");
    }
}
