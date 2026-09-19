using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Chapter1DoorwayAuthoring
{
    [MenuItem("Tools/Chapter 1/Build Incident Doorway")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Build the doorway in Edit Mode.");
        var controller=Object.FindFirstObjectByType<Chapter1PerformanceController>();
        if(controller==null || !controller.gameObject.scene.path.EndsWith("第一章新版警察.unity"))
            throw new System.InvalidOperationException("Open 第一章新版警察 first.");
        const string folder="Assets/Materials/Chapter1Doorway";
        Directory.CreateDirectory(folder);
        var wood=AssetDatabase.LoadAssetAtPath<Material>(folder+"/DoorTimber.mat");
        if(wood==null)
        {
            var tex=new Texture2D(128,512,TextureFormat.RGB24,false);
            for(int y=0;y<512;y++)for(int x=0;x<128;x++)
            {
                float grain=Mathf.PerlinNoise(x*0.17f,y*0.012f)*0.50f+Mathf.PerlinNoise(x*0.7f,y*0.04f)*0.18f;
                tex.SetPixel(x,y,Color.Lerp(new Color(0.13f,0.072f,0.031f),new Color(0.40f,0.245f,0.12f),grain));
            }
            tex.Apply();
            File.WriteAllBytes(folder+"/DoorTimber.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(folder+"/DoorTimber.png");
            wood=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/DoorTimber.png"));
            wood.SetFloat("_Smoothness",0.12f);AssetDatabase.CreateAsset(wood,folder+"/DoorTimber.mat");
        }
        var dark=AssetDatabase.LoadAssetAtPath<Material>(folder+"/DoorInterior.mat");
        if(dark==null){dark=new Material(Shader.Find("Universal Render Pipeline/Lit"));dark.color=new Color(0.028f,0.021f,0.014f);dark.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(dark,folder+"/DoorInterior.mat");}
        var old=GameObject.Find("Chapter1_IncidentDoorway");
        if(old!=null)Object.DestroyImmediate(old);
        var root=new GameObject("Chapter1_IncidentDoorway");
        root.transform.position=new Vector3(3686f,-5826.4f,-58.6f);
        // A deep, framed entry extends in front of the original closed facade.
        Box(root,"Shadowed doorway",new Vector3(0,9.4f,0.75f),new Vector3(10.3f,18.8f,1.4f),dark);
        Box(root,"Left door jamb",new Vector3(-5.65f,10,0),new Vector3(1.15f,20,3.3f),wood);
        Box(root,"Right door jamb",new Vector3(5.65f,10,0),new Vector3(1.15f,20,3.3f),wood);
        Box(root,"Door lintel",new Vector3(0,20,0),new Vector3(12.4f,1.6f,3.3f),wood);
        Box(root,"Door threshold",new Vector3(0,0.35f,-0.6f),new Vector3(11.1f,0.7f,4.5f),wood);
        // Join the entry to the existing stone porch; no detached frame remains.
        Box(root,"Left entry return",new Vector3(-5.65f,10,5.2f),new Vector3(1.15f,20,10.4f),wood);
        Box(root,"Right entry return",new Vector3(5.65f,10,5.2f),new Vector3(1.15f,20,10.4f),wood);
        Box(root,"Entry roof",new Vector3(0,20,5.2f),new Vector3(12.4f,1.6f,10.4f),wood);
        for(int i=0;i<6;i++)Box(root,"Interior plank "+i,new Vector3(-4.3f+i*1.7f,9.4f,0.01f),new Vector3(0.025f,18.2f,0.04f),wood);
        Undo.RecordObject(controller,"Set incident doorway blocking");
        controller.incidentDoorPosition=root.transform.position+Vector3.back*4.4f;
        controller.incidentDoorOutward=Vector3.back;
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log("[Doorway] Authored entrance and saved scene.");
    }
    static void Box(GameObject parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent.transform,false);
        go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
    }
}
