using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Reuses Chapter 1 building instances, including their material overrides.
// Only the Chapter 2 night group is saved; the donor scene is opened as a preview.
public static class Chapter2CouncilVillageAuthoring
{
    [MenuItem("Tools/Chapter 2/Add Chapter 1 Council Houses")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var previous=c.nightGroup.transform.Find("會議外圍房屋 · 第一章模型");
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第一章新版警察.unity");
        try
        {
            var donors=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                .Where(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).StartsWith("Assets/房子/",StringComparison.Ordinal))
                .GroupBy(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject))
                .Select(g=>g.First().gameObject).ToArray();
            if(donors.Length<3)throw new InvalidOperationException("Expected Chapter 1 house models.");
            if(previous)Object.DestroyImmediate(previous.gameObject);
            var group=new GameObject("會議外圍房屋 · 第一章模型");group.transform.SetParent(c.nightGroup.transform,false);
            string[] names={"傳統建築","茅屋1","茅屋2","茅屋1"};
            Vector3[] positions={new Vector3(0,0,11.6f),new Vector3(10.5f,0,4),new Vector3(-10.5f,0,4),new Vector3(9.5f,0,-8)};
            float[] yaw={0,-70,70,-125};
            for(int i=0;i<names.Length;i++)
            {
                var donor=donors.First(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("/"+names[i]+"/"));
                var house=Object.Instantiate(donor);house.name="會議房屋 "+(i+1)+" · "+names[i];
                SceneManager.MoveGameObjectToScene(house,c.gameObject.scene);house.transform.SetParent(group.transform,false);
                // Donor scripts belong to Chapter 1; retain only static building geometry.
                foreach(var t in house.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach(var component in house.GetComponentsInChildren<Component>(true))
                    if(component&&!(component is Transform)&&!(component is MeshFilter)&&!(component is MeshRenderer)&&!(component is Collider)&&!(component is LODGroup))Object.DestroyImmediate(component);
                // FBX roots carry an axis correction; preserve it when adding a world yaw.
                house.SetActive(true);house.transform.SetPositionAndRotation(Vector3.zero,Quaternion.AngleAxis(yaw[i],Vector3.up)*donor.transform.rotation);
                var renderers=house.GetComponentsInChildren<Renderer>();Bounds b=renderers[0].bounds;
                foreach(var r in renderers)b.Encapsulate(r.bounds);
                house.transform.localScale*=3.6f/b.size.y;
                b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                house.transform.position=positions[i]-new Vector3(b.center.x,b.min.y,b.center.z);
            }
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        }
        finally{EditorSceneManager.ClosePreviewScene(source);}
    }
}
