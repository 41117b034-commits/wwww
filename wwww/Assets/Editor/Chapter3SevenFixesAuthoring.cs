using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter3SevenFixesAuthoring
{
    [MenuItem("Tools/Chapter 3/修正世界路牌遮擋與置中")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before editing signs.");
        var flow=Object.FindFirstObjectByType<Chapter3Flow>();
        if(!flow||SceneManager.GetActiveScene().path!="Assets/Scenes/第三章.unity")
            throw new System.InvalidOperationException("Open the main Chapter 3 scene first.");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Chapter3/SchoolWorldText.shader");
        foreach(var region in new[]{flow.stationRegion,flow.combatRegion})
        {
            var lettering=region.GetComponent<Chapter3SchoolSigns>();
            if(!lettering)lettering=region.AddComponent<Chapter3SchoolSigns>();
            lettering.font=region.GetComponentsInChildren<TextMesh>(true).First().font;
            lettering.worldTextShader=shader;
        }
        var sign=flow.stationRegion.GetComponentsInChildren<TextMesh>(true).First(t=>t.text=="霧社 →");
        sign.anchor=TextAnchor.MiddleCenter;sign.alignment=TextAlignment.Center;
        sign.transform.localPosition=new Vector3(0,1.35f,-.11f);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
