using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Chapter2InteractionAuthoring
{
    const string Root="Assets/Chapter2/";
    [MenuItem("Tools/Chapter 2/Update Following and Axe")]
    public static void UpdateCurrent()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open the saved Chapter 2 scene first.");
        if(c.gameObject.scene.isDirty)throw new InvalidOperationException("Save scene changes first.");
        Apply(c);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
    }
    public static void Apply(Chapter2Controller c)
    {
        c.interactionDistance=.5f;c.treeApproach.position=c.sacredTree.position+new Vector3(0,0,-3.4f);
        var stone=c.gameObject.scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Sacred ground marker");
        if(stone)stone.transform.position=new Vector3(-6.3f,stone.transform.position.y,7.8f);
        var trunk=c.sacredTree.Find("Ancient trunk");
        // Solid bark must not inherit the foliage material's cutout rendering settings.
        var bark=trunk.GetComponent<Renderer>().sharedMaterial;
        bark.SetFloat("_AlphaClip",0);bark.SetFloat("_AlphaToMask",0);bark.DisableKeyword("_ALPHATEST_ON");
        bark.SetOverrideTag("RenderType","Opaque");bark.renderQueue=2000;
        var scatter=c.gameObject.scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Mossy rocks and forest floor");
        if(scatter)foreach(Transform prop in scatter.transform)
        {var p=prop.position;if(prop.name.StartsWith("Rock ")&&Mathf.Abs(p.x)<4.3f&&p.z>-17&&p.z<11){p.x=(p.x<0?-1:1)*5.5f;prop.position=p;}}
        var surface=trunk.GetComponent<MeshCollider>();if(!surface)surface=trunk.gameObject.AddComponent<MeshCollider>();
        surface.sharedMesh=trunk.GetComponent<MeshFilter>().sharedMesh;c.trunkSurface=surface;
        var guide=c.GetComponent<Chapter2RouteGuide>();if(!guide)guide=c.gameObject.AddComponent<Chapter2RouteGuide>();
        guide.chapter=c;guide.yellow=Material("Guidance yellow",new Color(1,.82f,.02f),0,.2f);
        guide.yellow.shader=Shader.Find("Universal Render Pipeline/Unlit");guide.yellow.color=new Color(1,.82f,.02f);
        c.routeGuide=guide;c.player.routeGuide=guide;
        guide.leftBoundary=-3.2f;guide.rightBoundary=.65f;
        if(!guide.distanceLabel)
        {
            var label=new GameObject("Follow distance in metres",typeof(RectTransform),typeof(UnityEngine.UI.Text),typeof(UnityEngine.UI.Outline));
            label.transform.SetParent(c.ui.canvas.transform,false);
            label.transform.SetSiblingIndex(c.ui.fade.transform.GetSiblingIndex());
            var text=label.GetComponent<UnityEngine.UI.Text>();text.font=c.ui.font;text.fontSize=28;
            text.color=new Color(1,.86f,.12f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            text.rectTransform.sizeDelta=new Vector2(340,48);text.rectTransform.anchoredPosition=Vector2.zero;
            var outline=label.GetComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.035f,.04f,.01f,1);outline.effectDistance=new Vector2(2,-2);
            guide.distanceLabel=text;label.SetActive(false);
        }
        Chapter2RifleAuthoring.Apply(c.officer);
        if(c.axe)UnityEngine.Object.DestroyImmediate(c.axe);
        var axe=new GameObject("Player logging axe · wooden haft and forged edge");axe.transform.SetParent(c.player.view.transform,false);
        axe.transform.localPosition=new Vector3(.40f,-.56f,1.05f);axe.transform.localRotation=Quaternion.Euler(0,15,-20);
        var wood=Material("Axe wood",new Color(.34f,.16f,.055f),0,.26f);
        var steel=Material("Axe forged steel",new Color(.28f,.31f,.32f),.65f,.32f);
        var edge=Material("Axe sharpened edge",new Color(.63f,.67f,.66f),.8f,.58f);
        Part(axe.transform,"Long wooden haft",PrimitiveType.Cylinder,new Vector3(0,.015f,0),new Vector3(.052f,.475f,.043f),wood);
        Part(axe.transform,"Forged eye",PrimitiveType.Cube,new Vector3(0,.47f,0),new Vector3(.09f,.18f,.09f),steel);
        var blade=new GameObject("Single cutting edge",typeof(MeshFilter),typeof(MeshRenderer));blade.transform.SetParent(axe.transform,false);
        blade.GetComponent<MeshFilter>().sharedMesh=BladeMesh();blade.GetComponent<Renderer>().sharedMaterial=steel;
        Part(axe.transform,"Sharpened bevel",PrimitiveType.Cube,new Vector3(-.292f,.46f,0),new Vector3(.022f,.365f,.008f),edge);
        var tip=new GameObject("Blade contact tip").transform;tip.SetParent(axe.transform,false);tip.localPosition=new Vector3(-.303f,.46f,0);
        var tool=axe.AddComponent<Chapter2Axe>();tool.bladeTip=tip;tool.cutWood=Material("Fresh cut wood",new Color(.69f,.43f,.19f),0,.2f);
        c.axe=axe;axe.SetActive(false);
        var meter=c.ui.meterPanel.GetComponent<RectTransform>();meter.anchorMin=new Vector2(.55f,.81f);meter.anchorMax=new Vector2(.97f,.975f);meter.offsetMin=meter.offsetMax=Vector2.zero;
        c.ui.meterLabel.fontSize=23;
    }
    static Material Material(string name,Color color,float metal,float smooth)
    {
        string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);return m;
    }
    static void Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;
    }
    static Mesh BladeMesh()
    {
        var outline=new[]{new Vector2(.06f,.4f),new Vector2(.06f,.56f),new Vector2(-.13f,.58f),new Vector2(-.29f,.65f),new Vector2(-.303f,.27f),new Vector2(-.13f,.36f)};
        var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
        Action<Vector3,Vector3,Vector3> face=(a,b,c)=>{int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.AddRange(new[]{n,n+1,n+2});};
        var front=new Vector3[6];var back=new Vector3[6];
        for(int i=0;i<6;i++){float half=outline[i].x<-.25f?.004f:.036f;front[i]=new Vector3(outline[i].x,outline[i].y,half);back[i]=new Vector3(outline[i].x,outline[i].y,-half);}
        for(int i=1;i<5;i++){face(front[0],front[i],front[i+1]);face(back[0],back[i+1],back[i]);}
        for(int i=0;i<6;i++){int j=(i+1)%6;face(front[i],back[i],back[j]);face(front[i],back[j],front[j]);}
        var mesh=new Mesh{name="ForgedAxeHead"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"Geometry/ForgedAxeHead.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
