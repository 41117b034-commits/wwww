using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Chapter2CouncilStoryAuthoring
{
    [MenuItem("Tools/Chapter 2/Apply Named Council Story")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring.");
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2.");
        c.mona.name="莫那·魯道";c.tado=c.leaders[1];c.bawan=c.leaders[3];c.watan=c.leaders[0];
        c.tado.name="達多·莫那";c.bawan.name="巴萬·拿威";c.watan.name="瓦旦";
        c.leaders[2].name="會議戰士 · 長裙女族人";c.leaders[4].name="會議族人 · 女長輩";c.leaders[5].name="會議戰士 · 短髮女族人";
        c.conservatives[0].name="會議戰士 · 捲髮青年";c.conservatives[1].name="會議戰士 · 頭巾女族人";
        var actors=new[]{c.mona}.Concat(c.leaders).Concat(c.conservatives).Distinct();
        foreach(var actor in actors)
        {
            var drama=actor.GetComponent<Chapter2CouncilDrama>();if(!drama)drama=actor.gameObject.AddComponent<Chapter2CouncilDrama>();
            drama.hasKnife=actor!=c.mona&&actor!=c.watan;
            if(drama.hasKnife)
            {
                if(!drama.knife)drama.knife=CreateKnife(actor.transform);
                else Chapter2CouncilKnifeAuthoring.RefreshKnife(drama.knife);
                drama.knifeTip=drama.knife.Find("刀尖");
            }
            if(drama.hasKnife)Chapter2KnifeGripAuthoring.Apply(actor,drama);
            EditorUtility.SetDirty(actor);EditorUtility.SetDirty(drama);
        }
        EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        AssetDatabase.SaveAssets();
        // Write only mesh files directly; never change the project's serialization mode.
        foreach(var actor in actors)
        {
            var mesh=actor.GetComponent<Chapter2CouncilDrama>().gripMesh;
            if(mesh)SaveBinary(mesh);
        }
    }

    public static void SaveBinary(Mesh mesh)
    {
        string path=AssetDatabase.GetAssetPath(mesh);
        var copy=UnityEngine.Object.Instantiate(mesh);
        copy.name=System.IO.Path.GetFileNameWithoutExtension(path);
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[]{copy},path,false);
        UnityEngine.Object.DestroyImmediate(copy);
        EditorUtility.ClearDirty(mesh);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    }

    static Transform CreateKnife(Transform parent)
    {
        return Chapter2CouncilKnifeAuthoring.CreateKnife(parent);
    }
}
