using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class Chapter2ForestDetailAuthoring
{
    const string Root="Assets/Chapter2/";
    const string Maps=Root+"Environment/PolyHaven/";
    [MenuItem("Tools/Chapter 2/Improve Morning Forest Detail")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var chapter=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(!chapter||chapter.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        AssetDatabase.Refresh();
        Material oldFloor=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Forest floor.mat");
        var floor=CopyMaterial(oldFloor,"Morning forest floor detail");
        SetTexture(floor,"_BaseMap",Maps+"forest_floor_diff_4k.jpg",false,4096);
        SetTexture(floor,"_BumpMap",Maps+"forest_floor_nor_gl_4k.jpg",true,4096);
        SetTexture(floor,"_OcclusionMap",Maps+"forest_floor_ao_4k.jpg",false,4096);
        floor.SetTexture("_MainTex",floor.GetTexture("_BaseMap"));
        floor.SetFloat("_BumpScale",.82f);floor.SetFloat("_OcclusionStrength",.55f);floor.SetFloat("_Smoothness",.08f);
        floor.EnableKeyword("_NORMALMAP");floor.EnableKeyword("_OCCLUSIONMAP");floor.enableInstancing=true;EditorUtility.SetDirty(floor);
        var detail=chapter.dayGroup.GetComponent<Chapter2ForestDetail>();
        if(!detail)detail=chapter.dayGroup.AddComponent<Chapter2ForestDetail>();
        detail.view=chapter.player.view;detail.ground=GameObject.Find("Forest ground").GetComponent<Renderer>();detail.detailedFloor=floor;
        foreach(var path in Directory.GetFiles(Root+"Materials","*.mat"))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m.name.StartsWith("Bark_")||m.name.StartsWith("Bark1_")||m.name.StartsWith("TreeBark_"))
            {
                SetTexture(m,"_BaseMap",Maps+"pine_bark_diff_2k.jpg",false,2048);
                SetTexture(m,"_BumpMap",Maps+"pine_bark_nor_gl_2k.jpg",true,2048);
                SetTexture(m,"_OcclusionMap",Maps+"pine_bark_ao_2k.jpg",false,2048);
                m.SetTexture("_MainTex",m.GetTexture("_BaseMap"));m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_OCCLUSIONMAP");
                m.SetFloat("_BumpScale",.7f);m.SetFloat("_OcclusionStrength",.6f);m.SetFloat("_Smoothness",.1f);
                m.DisableKeyword("_ALPHATEST_ON");m.SetFloat("_AlphaClip",0);
                // The giant trunk has generated UVs; tile its bark at a believable scale.
                if(m.name.StartsWith("TreeBark_"))m.mainTextureScale=new Vector2(2,2);
                m.enableInstancing=true;EditorUtility.SetDirty(m);
            }
            if(m.name.StartsWith("Branch_"))
            {
                SetTexture(m,"_BumpMap",ScopedTexture("Assets/Hipernt/Pine Pack/Textures/pine branch_Normal.png"),true,2048);
                SetTexture(m,"_BaseMap",ScopedTexture("Assets/Hipernt/Pine Pack/Textures/pine branch.png"),false,2048);
                m.SetTexture("_MainTex",m.GetTexture("_BaseMap"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.5f);
                m.SetFloat("_Smoothness",.08f);m.enableInstancing=true;EditorUtility.SetDirty(m);
            }
        }
        // Restore normal maps dropped by the original material conversion, without editing source assets.
        foreach(string name in new[]{"BlackwalnutBark_","BlackwalnutSpring_"})
        {
            var path=Directory.GetFiles(Root+"Materials",name+"*.mat").FirstOrDefault();if(path==null)continue;
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            var sourceGuid=Path.GetFileNameWithoutExtension(path).Split('_').Last();
            var sourcePath=AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>!p.StartsWith(Root)&&AssetDatabase.AssetPathToGUID(p).StartsWith(sourceGuid));
            var source=sourcePath==null?null:AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if(source&&source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap"))
            {SetTexture(m,"_BumpMap",ScopedTexture(AssetDatabase.GetAssetPath(source.GetTexture("_BumpMap"))),true,2048);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.65f);}
            if(m.mainTexture)SetTexture(m,"_BaseMap",ScopedTexture(AssetDatabase.GetAssetPath(m.mainTexture)),false,2048);
            m.SetFloat("_Smoothness",.1f);m.enableInstancing=true;EditorUtility.SetDirty(m);
        }
        AddUndergrowth(chapter);
        // Tangents are required for tangent-space normal maps on the generated meshes.
        foreach(var name in new[]{"ForestGround","GiantTrunk"})
        {var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Geometry/"+name+".asset");if(mesh&&mesh.tangents.Length!=mesh.vertexCount){mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);}}
        EditorSceneManager.MarkSceneDirty(chapter.gameObject.scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(chapter.gameObject.scene);
        Debug.Log("[Chapter2] Morning forest detail applied.");
    }

    static string ScopedTexture(string source)
    {
        string folder=Root+"Environment/Detail/";Directory.CreateDirectory(folder);
        string target=folder+Path.GetFileName(source);
        if(!File.Exists(target)){File.Copy(source,target);AssetDatabase.ImportAsset(target);}
        return target;
    }
    static void SetTexture(Material m,string slot,string path,bool normal,int size)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Missing texture "+path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=!normal&&!path.Contains("_ao_");importer.maxTextureSize=size;
        importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=16;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
        importer.SaveAndReimport();m.SetTexture(slot,AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    }
    static Material CopyMaterial(Material source,string name)
    {
        string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(source){name=name};AssetDatabase.CreateAsset(m,path);}return m;
    }
    static void AddUndergrowth(Chapter2Controller chapter)
    {
        if(chapter.dayGroup.transform.Find("Pathside undergrowth"))return;
        var group=new GameObject("Pathside undergrowth").transform;group.SetParent(chapter.dayGroup.transform,false);
        var random=new System.Random(19301005);
        var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hipernt/Pine Pack/Prefabs/Plant2.prefab");
        var material=CopyMaterial(AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Branch_d9d9eecd.mat"),"Pathside plants");
        SetTexture(material,"_BaseMap",ScopedTexture("Assets/Hipernt/Pine Pack/Textures/plant05.png"),false,1024);
        SetTexture(material,"_BumpMap",ScopedTexture("Assets/Hipernt/Pine Pack/Textures/plant05_normal.png"),true,1024);
        material.SetTexture("_MainTex",material.GetTexture("_BaseMap"));material.SetColor("_BaseColor",new Color(.72f,.8f,.65f));
        material.SetFloat("_BumpScale",.5f);EditorUtility.SetDirty(material);
        for(int i=0;i<100;i++)
        {
            float side=i%2==0?-1:1;
            float x=side*(4.5f+(float)random.NextDouble()*9),z=-19+(float)random.NextDouble()*25;
            var plant=UnityEngine.Object.Instantiate(template,group);plant.name="Forest floor plant "+i;
            foreach(var b in plant.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(b);
            foreach(var collider in plant.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            var renderers=plant.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            plant.transform.localScale*=Mathf.Lerp(.18f,.48f,(float)random.NextDouble())/bounds.size.y;
            plant.transform.position=new Vector3(x,0,z);plant.transform.Rotate(0,(float)random.NextDouble()*360,0);
            bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
            plant.transform.position-=Vector3.up*bounds.min.y;
        }
    }
}
