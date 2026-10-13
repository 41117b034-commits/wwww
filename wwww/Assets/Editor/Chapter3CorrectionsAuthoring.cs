using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class Chapter3CorrectionsAuthoring
{
    [MenuItem("Tools/Chapter 3/Apply Fence and Companion Corrections")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var flow=Object.FindFirstObjectByType<Chapter3Flow>();
        if(!flow||flow.gameObject.scene.path!="Assets/Scenes/第三章.unity")throw new InvalidOperationException("Open Chapter 3 first.");
        flow.ui.progress.gameObject.SetActive(false);
        var station=flow.station;
        foreach(var t in flow.stationRegion.GetComponentsInChildren<Transform>(true))
            if(t.name=="木圍籬立柱"||t.name=="圍籬橫桿")
            {var col=t.GetComponent<BoxCollider>();if(!col)col=t.gameObject.AddComponent<BoxCollider>();col.isTrigger=false;col.enabled=true;}
        station.departurePath=new[]{new Vector3(0,0,-1.5f),new Vector3(5.5f,0,-3),new Vector3(8.5f,0,-1),
            new Vector3(8.5f,0,3),new Vector3(13,0,3),new Vector3(14,0,0),new Vector3(15,0,-8),
            new Vector3(22,0,-4),new Vector3(30,0,4),new Vector3(32,0,12)};
        var ground=flow.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="駐在所山坳與道路");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Trampled courtyard.mat");
        foreach(var old in ground.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("前往霧社的土路 ")).ToArray())Object.DestroyImmediate(old.gameObject);
        for(int i=1;i<station.departurePath.Length;i++)
        {
            Vector3 a=station.departurePath[i-1],b=station.departurePath[i];
            var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="前往霧社的土路 "+i;road.transform.SetParent(ground,false);
            road.transform.position=(a+b)*.5f+Vector3.down*.08f;road.transform.rotation=Quaternion.LookRotation(b-a);
            road.transform.localScale=new Vector3(3.3f,.15f,(b-a).magnitude+1);
            road.GetComponent<Renderer>().sharedMaterial=material;
        }
        var sign=flow.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="往霧社的路標");
        sign.position=new Vector3(12.2f,0,4.1f);sign.rotation=Quaternion.Euler(0,35,0);
        foreach(var actor in new[]{flow.combat.bawan,flow.combat.pihu})
        {
            var response=actor.GetComponent<Chapter3CompanionReaction>();if(!response)response=actor.gameObject.AddComponent<Chapter3CompanionReaction>();
            response.actor=actor;response.combat=flow.combat;
            var capsule=actor.GetComponent<CapsuleCollider>();if(!capsule)capsule=actor.gameObject.AddComponent<CapsuleCollider>();
            var s=actor.transform.lossyScale;capsule.height=1.72f/Mathf.Abs(s.y);capsule.radius=.32f/Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.z));
            capsule.center=new Vector3(0,.88f/s.y,0);capsule.isTrigger=false;
        }
        var weapons=flow.combat.weapons;
        foreach(var hand in new[]{weapons.rightHand,weapons.leftHand})
        {
            var filter=hand.GetComponentInChildren<MeshFilter>(true);var arm=filter.GetComponent<Chapter3ViewArm>();
            if(!arm)arm=filter.gameObject.AddComponent<Chapter3ViewArm>();arm.view=flow.player.view;arm.left=hand==weapons.leftHand;
        }
        EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
