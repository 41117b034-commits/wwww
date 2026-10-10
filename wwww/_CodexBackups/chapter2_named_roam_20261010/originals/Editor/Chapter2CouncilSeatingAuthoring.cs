using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Chapter2CouncilSeatingAuthoring
{
    public const string ChildName="會議原住民小孩";
    [MenuItem("Tools/Chapter 2/Level Council Houses and Seat Listeners")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var houses=c.nightGroup.transform.Find("會議外圍房屋 · 第一章模型");
        var ground=GameObject.Find("Forest ground").GetComponent<Collider>();Physics.SyncTransforms();
        foreach(Transform house in houses)
        {
            Vector3 center=house.position;
            center.y=ground.Raycast(new Ray(new Vector3(center.x,80,center.z),Vector3.down),out var hit,160)?hit.point.y:0;
            Chapter2CouncilVillageAuthoring.LevelHouse(house,center);
        }
        foreach(var actor in new[]{c.mona}.Concat(c.leaders).Concat(c.conservatives))
        {actor.seated=true;EditorUtility.SetDirty(actor);}
        Vector3[] positions={new Vector3(-4.15f,0,-2.15f),new Vector3(-3.15f,0,-3.75f)};
        for(int i=0;i<c.conservatives.Length;i++)
        {
            var actor=c.conservatives[i];actor.transform.position=positions[i];actor.facing=c.campfire.position-positions[i];
            Seat(c,actor,.44f,i==0?.55f:.45f);
            EditorUtility.SetDirty(actor);EditorUtility.SetDirty(actor.transform);
        }
        var child=c.nightGroup.transform.Find(ChildName);
        if(!child)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Chapter2ChildNPCAuthoring.PrefabPath);
            if(!prefab)throw new InvalidOperationException("Missing existing Chapter 2 child prefab.");
            child=Object.Instantiate(prefab,c.nightGroup.transform).transform;child.name=ChildName;
        }
        child.position=new Vector3(3.5f,0,-2.85f);child.gameObject.SetActive(true);
        var listener=child.GetComponent<Chapter2Actor>();listener.seated=true;listener.seatedHipHeight=.38f;
        listener.facing=c.campfire.position-child.position;
        foreach(var skin in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
        Seat(c,listener,.29f,.38f);
        // The child is a seated listener and is not added to the warrior cast.
        EditorUtility.SetDirty(listener);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
    }
    static void Seat(Chapter2Controller c,Chapter2Actor actor,float height,float width)
    {
        string name="會議座椅 · "+actor.name;var existing=c.nightGroup.transform.Find(name);
        var seat=existing?existing.gameObject:GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seat.name=name;seat.transform.SetParent(c.nightGroup.transform,true);
        Vector3 inward=Vector3.ProjectOnPlane(c.campfire.position-actor.transform.position,Vector3.up).normalized;
        seat.transform.SetPositionAndRotation(actor.transform.position+inward*.04f+Vector3.up*(height*.5f),Quaternion.identity);
        seat.transform.localScale=new Vector3(width,height*.5f,width);
        var donor=c.nightGroup.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Council seat").GetComponent<Renderer>();
        seat.GetComponent<Renderer>().sharedMaterial=donor.sharedMaterial;
    }
}
