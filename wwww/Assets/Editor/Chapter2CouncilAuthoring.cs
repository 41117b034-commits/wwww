using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Chapter2CouncilAuthoring
{
    [MenuItem("Tools/Chapter 2/Prepare Logged Night Council")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var forest=GameObject.Find("Forest · sacred grove").transform;
        var near=GameObject.Find("Forest enrichment · layered woodland").transform.Find("Trees · intermediate canopy");
        var trees=forest.Cast<Transform>().Concat(near.Cast<Transform>().Where(t=>t.name.StartsWith("Grove tree "))).Append(c.sacredTree).ToArray();
        var ground=GameObject.Find("Forest ground").GetComponent<Collider>();
        var setting=c.GetComponent<Chapter2LoggedForest>();if(!setting)setting=c.gameObject.AddComponent<Chapter2LoggedForest>();
        setting.chapter=c;setting.trees=trees;setting.ground=ground;
        const string groupName="Stacked cut timber · night clearing";
        var previous=c.nightGroup.transform.Find(groupName);if(previous)Object.DestroyImmediate(previous.gameObject);
        var group=new GameObject(groupName);group.transform.SetParent(c.nightGroup.transform,false);
        setting.timberPiles=group;
        var source=GameObject.Find("Carried timber 0");
        if(!source)throw new InvalidOperationException("Existing cut timber reference was not found.");
        var material=source.GetComponent<Renderer>().sharedMaterial;
        Vector3[] centers={new Vector3(-5.5f,0,4.9f),new Vector3(5.5f,0,4.7f),new Vector3(.8f,0,7.2f),new Vector3(6.4f,0,-2.7f),new Vector3(-7.2f,0,-4.5f),new Vector3(1.8f,0,-7.3f)};
        float[] angles={65,-55,83,20,-25,75};
        for(int i=0;i<centers.Length;i++)
        {
            var pile=new GameObject("Sawn timber stack "+(i+1)).transform;pile.SetParent(group.transform,false);
            var p=centers[i];
            if(ground.Raycast(new Ray(p+Vector3.up*50,Vector3.down),out var hit,100))p.y=hit.point.y;
            pile.SetPositionAndRotation(p,Quaternion.Euler(0,angles[i],0));
            for(int level=0;level<2;level++)for(int column=0;column<4-level;column++)
            {
                var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.name="Squared logging timber";
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                beam.transform.SetParent(pile,false);
                beam.transform.localScale=new Vector3(.22f,.19f,2.8f+(column%2)*.24f);
                beam.transform.localPosition=new Vector3((column-(3-level)*.5f)*.25f,.095f+level*.195f,(column%2==0?-.08f:.08f));
                beam.GetComponent<Renderer>().sharedMaterial=material;
            }
        }
        EditorUtility.SetDirty(setting);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        Debug.Log("[Chapter2 council] Configured "+trees.Length+" fallen night trees and "+centers.Length+" timber stacks.");
    }
}
