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
            if(drama.hasKnife&&!drama.knife)drama.knife=CreateKnife(actor.transform);
            if(drama.hasKnife)Chapter2KnifeGripAuthoring.Apply(actor,drama);
            if(drama.knife&&!drama.knife.Find("刀鞘"))
                Part(drama.knife,"刀鞘",new Vector3(0,.31f,0),new Vector3(.08f,.48f,.05f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter2/Props/CouncilHilt.mat"));
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
        const string folder="Assets/Chapter2/Props";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Chapter2","Props");
        var steel=Material(folder+"/CouncilBlade.mat",new Color(.58f,.64f,.69f),.8f,.55f);
        var wood=Material(folder+"/CouncilHilt.mat",new Color(.16f,.055f,.025f),0,.18f);
        string meshPath=folder+"/CouncilKnifeBlade.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!mesh)
        {
            mesh=new Mesh{name="Council hunting blade"};
            Vector3[] shape={new Vector3(-.032f,.08f,0),new Vector3(.036f,.08f,0),new Vector3(.045f,.45f,0),new Vector3(.008f,.58f,0),new Vector3(-.025f,.48f,0)};
            mesh.vertices=shape.Select(v=>v+Vector3.forward*.009f).Concat(shape.Select(v=>v-Vector3.forward*.009f)).ToArray();
            mesh.triangles=new[]{0,1,2,0,2,3,0,3,4,5,7,6,5,8,7,5,9,8,0,6,1,0,5,6,1,7,2,1,6,7,2,8,3,2,7,8,3,9,4,3,8,9,4,5,0,4,9,5};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
        }
        var root=new GameObject("獵刀").transform;root.SetParent(parent,true);
        root.position=parent.position+Vector3.up*.8f;
        var blade=new GameObject("刀刃",typeof(MeshFilter),typeof(MeshRenderer));blade.transform.SetParent(root,false);
        blade.GetComponent<MeshFilter>().sharedMesh=mesh;blade.GetComponent<MeshRenderer>().sharedMaterial=steel;
        Part(root,"木製刀柄",new Vector3(0,-.025f,0),new Vector3(.055f,.2f,.048f),wood);
        Part(root,"護手",new Vector3(0,.077f,0),new Vector3(.13f,.024f,.06f),steel);
        return root;
    }
    static Material Material(string path,Color color,float metallic,float smooth)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);return m;
    }
    static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
    }
}
