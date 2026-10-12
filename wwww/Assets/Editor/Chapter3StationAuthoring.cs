using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class Chapter3StationAuthoring
{
    public const string ScenePath="Assets/Scenes/第三章_駐在所.unity";
    const string Folder="Assets/Chapter3/Station";
    static Material timber, boards, plaster, stone, roof, metal, glass, dirt, path, bamboo, paper;
    static Font font;
    [MenuItem("Tools/Chapter 3/Create Station Story Scene")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("Station already exists; edit the saved scene.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current scene first.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var village=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第三章.unity");
        var council=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var vc=village.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter3Controller>(true)).First();
            var cc=council.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="第三章_駐在所";Materials();
            var c=new GameObject("第三章 · 駐在所劇情").AddComponent<Chapter3StationController>();
            c.approachPath=new[]{new Vector3(0,0,-9),new Vector3(0,0,-4),new Vector3(0,0,-1.5f)};
            c.departurePath=new[]{new Vector3(0,0,-1.5f),new Vector3(6,0,-3),new Vector3(10,0,-5),new Vector3(15,0,-8),new Vector3(22,0,-4),new Vector3(30,0,4),new Vector3(32,0,12)};
            Terrain(c.departurePath);Station();Props(c);Forest(village,c.departurePath);
            var cast=new GameObject("莫那與集結族人").transform;
            c.bawan=Actor(vc.bawan,cast,new Vector3(0,0,-15));
            c.pihu=Actor(vc.pihu,cast,new Vector3(1.6f,0,-17));
            c.mona=Actor(cc.mona,cast,new Vector3(0,0,1.4f));
            c.mona.name="莫那·魯道 · 駐在所交代任務";
            var support=Actor(cc.watan,cast,new Vector3(5.8f,0,1));
            support.facing=Vector3.left;
            Spear(support.transform.position+new Vector3(.35f,0,.1f));
            var player=Object.Instantiate(vc.player.gameObject);player.name="玩家 · 駐在所";
            c.player=player.GetComponent<Chapter3Player>();c.player.chapter=null;c.player.station=c;
            player.transform.SetPositionAndRotation(new Vector3(0,.06f,-18),Quaternion.identity);
            c.player.view.transform.localPosition=new Vector3(0,1.65f,0);c.player.view.transform.localRotation=Quaternion.identity;
            c.player.view.farClipPlane=220;
            c.ui=Chapter3Presentation.Build(c.player.view,font);
            var texts=c.ui.ending.GetComponentsInChildren<Text>();
            texts[0].text="前往霧社";texts[1].text="巴萬與比胡帶著隊伍繼續前進。\n\n下一段：學校與運動會場";
            c.ui.replay.GetComponentInChildren<Text>().text="重玩駐在所";
            c.ui.objective.text="馬赫坡駐在所 · 清晨";
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            Lighting();
            var ambience=new GameObject("山林清晨環境聲").AddComponent<AudioSource>();
            ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/forest_morning.wav");ambience.loop=true;ambience.playOnAwake=true;ambience.volume=.24f;
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            Selection.activeGameObject=c.gameObject;
            var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(0,1,3),Quaternion.Euler(23,0,0),24);
            Chapter2GameViewScaleGuard.Restore();
        }
        finally{EditorSceneManager.ClosePreviewScene(village);EditorSceneManager.ClosePreviewScene(council);}
    }
    static void Materials()
    {
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF");
        string grain="Assets/Chapter2/Props/CouncilKnifeWoodGrain.png";
        timber=Mat("Dark structural timber",new Color(.23f,.18f,.13f),grain,new Vector2(1,3));
        boards=Mat("Aged cedar boards",new Color(.50f,.40f,.27f),grain,new Vector2(1,3));
        plaster=Mat("Weathered pale wall",new Color(.69f,.68f,.57f));
        stone=Mat("Foundation stone",new Color(.45f,.47f,.43f),"Assets/Chapter2/Environment/RichForest/Textures/6d259800_rock_01_diffuse.jpg",Vector2.one);
        roof=Mat("Charcoal roof shingles",new Color(.16f,.19f,.18f),grain,new Vector2(3,1));
        metal=Mat("Rifle dark steel",new Color(.15f,.17f,.17f));metal.SetFloat("_Metallic",.7f);metal.SetFloat("_Smoothness",.36f);
        glass=Mat("Shadowed windows",new Color(.12f,.19f,.19f));glass.SetFloat("_Smoothness",.35f);
        dirt=Mat("Station forest soil",new Color(.62f,.61f,.49f),"Assets/Chapter2/Environment/PolyHaven/forest_floor_diff_4k.jpg",Vector2.one);
        path=Mat("Trampled courtyard",new Color(.72f,.65f,.53f),"Assets/Chapter2/Environment/RichForest/Textures/brown_mud_diff_2k.jpg",Vector2.one);
        bamboo=Mat("Bamboo shafts",new Color(.44f,.40f,.20f),grain,new Vector2(1,5));
        paper=Mat("Faded notice paper",new Color(.79f,.76f,.64f));
    }
    static Material Mat(string name,Color color,string texture=null,Vector2 tiling=default)
    {
        string file=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(file);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,file);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);
        if(texture!=null){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture>(texture));m.SetTextureScale("_BaseMap",tiling);}
        EditorUtility.SetDirty(m);return m;
    }
    static Transform Box(string name,Transform root,Vector3 p,Vector3 size,Material mat,bool collision=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static Transform Cylinder(string name,Transform root,Vector3 p,Vector3 size,Quaternion rot,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=rot;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static void Beam(string name,Transform root,Vector3 a,Vector3 b,float width,Material mat)
    {var t=Box(name,root,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Mesh(string name,Transform parent,Vector3[] vertices,int[] triangles,Material mat,bool collider=false,Vector2[] uv=null)
    {
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv??vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(collider)g.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Terrain(Vector3[] route)
    {
        var root=new GameObject("駐在所山坳與道路").transform;
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        const int n=101;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            float xx=x*2-80,zz=z*2-80;
            float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,72,Mathf.Abs(xx-8)))*16+Mathf.SmoothStep(0,1,Mathf.InverseLerp(21,65,zz))*23;
            int i=z*n+x;v.Add(new Vector3(xx,rise*(.65f+Mathf.PerlinNoise(x*.08f,z*.08f)*.5f)-.03f,zz));uv.Add(new Vector2(xx/5,zz/5));
            if(x<n-1&&z<n-1)tri.AddRange(new[]{i,i+n,i+1,i+1,i+n,i+n+1});
        }
        Mesh("Station mountain ground",root,v.ToArray(),tri.ToArray(),dirt,true,uv.ToArray());
        Box("庭院可行走地面",root,new Vector3(0,-.08f,-8),new Vector3(23,.15f,30),path,true);
        // Dedicated ribbons keep the walking route level, while hills stay outside it.
        for(int i=1;i<route.Length;i++)
        {Vector3 a=route[i-1],b=route[i];var t=Box("前往霧社的土路 "+i,root,(a+b)*.5f+Vector3.down*.08f,new Vector3(5.5f,.15f,(b-a).magnitude+1),path,true);t.rotation=Quaternion.LookRotation(b-a);}
    }
    static void Station()
    {
        var root=new GameObject("馬赫坡駐在所 · 遊戲改編建築").transform;
        Box("石砌基座",root,new Vector3(0,.26f,9.1f),new Vector3(14.4f,.52f,9),stone,true);
        Box("室內牆體",root,new Vector3(0,1.9f,9.8f),new Vector3(13.6f,2.8f,6.5f),plaster,true);
        Box("木造外廊",root,new Vector3(0,.58f,5.3f),new Vector3(14,.17f,2.3f),boards,true);
        for(int i=0;i<55;i++)Box("外廊板縫",root,new Vector3(-6.9f+i*.255f,.673f,5.3f),new Vector3(.013f,.008f,2.28f),timber);
        for(int i=0;i<3;i++)Box("入口石階",root,new Vector3(0,.09f+i*.15f,3.55f+i*.32f),new Vector3(2.8f,.18f+i*.30f,1.2f-i*.17f),stone,true);
        for(int i=0;i<33;i++)Box("腰牆木板",root,new Vector3(-6.6f+i*.413f,1.04f,6.49f),new Vector3(.396f,.90f,.075f),boards);
        foreach(float x in new[]{-6.85f,-4.55f,-2.3f,2.3f,4.55f,6.85f})
        {
            Box("外廊柱",root,new Vector3(x,1.99f,4.44f),new Vector3(.15f,2.85f,.15f),timber);
            Beam("柱斜撐",root,new Vector3(x,2.7f,4.44f),new Vector3(x+(x<0?.65f:-.65f),3.36f,4.44f),.085f,timber);
        }
        Box("外廊橫梁",root,new Vector3(0,3.4f,4.44f),new Vector3(14.6f,.19f,.22f),timber);
        foreach(float x in new[]{-4.65f,-2.75f,2.75f,4.65f})Window(root,x,6.44f);
        Box("入口暗部",root,new Vector3(0,1.76f,6.48f),new Vector3(1.68f,2.22f,.10f),glass);
        foreach(float x in new[]{-.93f,.93f})Box("門柱",root,new Vector3(x,1.87f,6.36f),new Vector3(.16f,2.52f,.16f),timber);
        Box("門楣",root,new Vector3(0,3.14f,6.35f),new Vector3(2.1f,.18f,.18f),timber);
        var door=Box("半開的木門",root,new Vector3(-1.0f,1.75f,5.98f),new Vector3(.78f,2.15f,.10f),boards);door.localRotation=Quaternion.Euler(0,-48,0);
        for(int i=0;i<4;i++)Box("立面木柱",root,new Vector3(-6.8f+i*4.53f,2.0f,6.41f),new Vector3(.12f,2.75f,.12f),timber);
        // Gabled timber roof, with visible lapped courses and deep eaves.
        foreach(int side in new[]{-1,1})
        {
            float z=side<0?6.2f:11.9f;
            var slope=Box("深簷屋頂",root,new Vector3(0,4.05f,z),new Vector3(15.7f,.16f,6.2f),roof);
            slope.localRotation=Quaternion.Euler(side<0?-16:16,0,0);
            for(int i=0;i<17;i++)
            {
                float zz=side<0?3.2f+i*.35f:9.15f+i*.35f;
                float yy=4.93f-Mathf.Abs(zz-9.05f)*.285f;
                Box("屋面搭接",root,new Vector3(0,yy,zz),new Vector3(15.72f,.045f,.043f),timber);
            }
        }
        Box("屋脊",root,new Vector3(0,4.99f,9.05f),new Vector3(15.85f,.16f,.18f),roof);
        foreach(float x in new[]{-7.75f,7.75f})
        {Beam("山牆封簷",root,new Vector3(x,3.30f,3.1f),new Vector3(x,4.98f,9.05f),.16f,timber);Beam("山牆封簷",root,new Vector3(x,4.98f,9.05f),new Vector3(x,3.30f,15),.16f,timber);}
        Box("駐在所木牌",root,new Vector3(.05f,2.33f,4.27f),new Vector3(.52f,1.22f,.10f),boards);
        Sign("駐\n在\n所",root,new Vector3(.05f,2.79f,4.20f),.12f,new Color(.11f,.09f,.07f));
        // Interior props glimpsed through the entrance; the chapter route stays outside.
        Box("事務桌",root,new Vector3(.3f,1.03f,7.4f),new Vector3(1.5f,.12f,.7f),boards);
        for(int i=0;i<3;i++)Box("散落文書",root,new Vector3(1.6f+i*.25f,.69f,5.3f+i*.17f),new Vector3(.23f,.008f,.31f),paper);
    }
    static void Window(Transform root,float x,float z)
    {
        Box("窗內暗部",root,new Vector3(x,2.18f,z),new Vector3(1.52f,1.22f,.08f),glass);
        foreach(float dx in new[]{-.80f,0,.80f})Box("木窗框",root,new Vector3(x+dx,2.18f,z-.055f),new Vector3(.06f,1.37f,.09f),timber);
        foreach(float y in new[]{1.52f,2.16f,2.85f})Box("木窗框",root,new Vector3(x,y,z-.055f),new Vector3(1.67f,.055f,.10f),timber);
        Box("窗台",root,new Vector3(x,1.48f,z-.1f),new Vector3(1.79f,.09f,.25f),boards);
    }
    static void Props(Chapter3StationController c)
    {
        var root=new GameObject("集結空地 · 武器木桌與物資").transform;
        Box("領槍木桌",root,new Vector3(-4.8f,.82f,-.5f),new Vector3(2,.13f,.95f),boards,true);
        foreach(float x in new[]{-5.62f,-3.98f})foreach(float z in new[]{-.86f,-.14f})Box("桌腳",root,new Vector3(x,.4f,z),new Vector3(.10f,.8f,.10f),timber,true);
        c.riflePickup=Rifle(root,new Vector3(-4.8f,.97f,-.5f),Quaternion.Euler(0,90,0));
        var focus=new GameObject("領槍互動點");focus.transform.SetParent(root);focus.transform.position=new Vector3(-4.8f,.94f,-.5f);c.rifleFocus=focus.transform;
        Box("領槍桌邊木牌",root,new Vector3(-4.8f,.55f,-1.01f),new Vector3(.9f,.25f,.04f),timber);
        Sign("領  槍",root,new Vector3(-4.8f,.59f,-1.04f),.085f,new Color(.87f,.80f,.63f));
        foreach(Vector3 pos in new[]{new Vector3(-7.8f,.3f,3),new Vector3(-6.8f,.3f,3.4f),new Vector3(6.3f,.3f,3),new Vector3(6.8f,.3f,2.2f)})Crate(root,pos);
        Crate(root,new Vector3(-7.4f,.9f,3.15f));
        for(int i=0;i<4;i++)
        {Cylinder("倚放竹槍",root,new Vector3(-8.0f+i*.2f,1.05f,4.35f),new Vector3(.032f,1.03f,.032f),Quaternion.Euler(-8,0,-5),bamboo);}
        foreach(int side in new[]{-1,1})
        {
            for(int i=0;i<7;i++)
            {
                float x=side*10.8f,z=-17+i*3;
                Box("木圍籬立柱",root,new Vector3(x,.65f,z),new Vector3(.15f,1.3f,.15f),boards);
                if(i<6)foreach(float y in new[]{.46f,.91f})Box("圍籬橫桿",root,new Vector3(x,y,z+1.5f),new Vector3(.08f,.11f,3),timber);
            }
        }
        Cylinder("木製通訊杆",root,new Vector3(9.3f,3.5f,2),new Vector3(.21f,3.5f,.21f),Quaternion.identity,timber);
        Box("通訊橫木",root,new Vector3(9.3f,6.7f,2),new Vector3(1.4f,.12f,.12f),timber);
        foreach(float x in new[]{8.8f,9.8f})Cylinder("絕緣子",root,new Vector3(x,6.8f,2),new Vector3(.12f,.12f,.12f),Quaternion.identity,plaster);
        for(int i=0;i<8;i++)Beam("垂落通訊線",root,new Vector3(8.8f+i*.04f,6.7f-i*.43f,2-i*.09f),new Vector3(8.8f+(i+1)*.04f,6.7f-(i+1)*.43f,2-(i+1)*.09f),.018f,metal);
        var road=new GameObject("往霧社的路標").transform;road.SetParent(root);road.position=new Vector3(11.7f,0,-10.4f);road.rotation=Quaternion.Euler(0,-30,0);
        Box("路標柱",road,new Vector3(0,.8f,0),new Vector3(.11f,1.6f,.11f),timber);
        Box("路標木板",road,new Vector3(0,1.35f,-.05f),new Vector3(1.4f,.39f,.09f),boards);
        Sign("霧社 →",road,new Vector3(0,1.43f,-.11f),.12f,new Color(.12f,.1f,.07f));
    }
    static void Crate(Transform root,Vector3 p)
    {
        var crate=new GameObject("木箱").transform;crate.SetParent(root,false);crate.localPosition=p;
        Box("箱體",crate,Vector3.zero,new Vector3(.85f,.6f,.65f),boards,true);
        foreach(float x in new[]{-.30f,.30f})Box("束帶",crate,new Vector3(x,0,0),new Vector3(.045f,.615f,.665f),metal);
        for(int i=0;i<4;i++)Box("箱板縫",crate,new Vector3(0,-.20f+i*.14f,-.331f),new Vector3(.85f,.011f,.008f),timber);
    }
    static void Spear(Vector3 p)
    {var root=new GameObject("族人身旁竹槍").transform;root.position=p;Cylinder("竹柄",root,new Vector3(0,1.05f,0),new Vector3(.031f,1.05f,.031f),Quaternion.identity,bamboo);}
    static void Sign(string text,Transform parent,Vector3 p,float height,Color color)
    {
        var go=new GameObject(text,typeof(TextMesh));go.transform.SetParent(parent,false);go.transform.localPosition=p;
        var t=go.GetComponent<TextMesh>();t.text=text;t.font=font;t.fontSize=70;t.characterSize=height*.30f;t.anchor=TextAnchor.UpperCenter;t.alignment=TextAlignment.Center;t.color=color;
        go.GetComponent<MeshRenderer>().sharedMaterial=font.material;
    }
    static Transform Rifle(Transform parent,Vector3 p,Quaternion rotation)
    {
        var root=new GameObject("村田十八年式步槍 · 可領取").transform;root.SetParent(parent,false);root.localPosition=p;root.localRotation=rotation;
        // Visual interpretation of the museum silhouette; no modern magazine or scope.
        Vector2[] outline={new Vector2(-.67f,.035f),new Vector2(-.67f,-.135f),new Vector2(-.47f,-.10f),new Vector2(-.28f,-.02f),new Vector2(.38f,-.012f),new Vector2(.38f,.035f),new Vector2(-.27f,.045f),new Vector2(-.39f,.015f)};
        var v=new List<Vector3>();foreach(float side in new[]{-.030f,.030f})foreach(var q in outline)v.Add(new Vector3(side,q.y,q.x));
        var tr=new List<int>();int n=outline.Length;
        for(int i=1;i<n-1;i++){tr.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});}
        for(int i=0;i<n;i++){int j=(i+1)%n;tr.AddRange(new[]{i,j,n+i,j,n+j,n+i});}
        Mesh("Murata18 interpreted stock",root,v.ToArray(),tr.ToArray(),boards);
        Cylinder("長槍管",root,new Vector3(0,.055f,.17f),new Vector3(.024f,.49f,.024f),Quaternion.Euler(90,0,0),metal);
        Cylinder("槍機",root,new Vector3(0,.061f,-.27f),new Vector3(.047f,.095f,.047f),Quaternion.Euler(90,0,0),metal);
        Cylinder("槍機柄",root,new Vector3(.043f,.059f,-.285f),new Vector3(.014f,.045f,.014f),Quaternion.Euler(0,0,90),metal);
        var knob=GameObject.CreatePrimitive(PrimitiveType.Sphere);knob.name="槍機柄球";knob.transform.SetParent(root,false);knob.transform.localPosition=new Vector3(.09f,.059f,-.285f);knob.transform.localScale=Vector3.one*.028f;knob.GetComponent<Renderer>().sharedMaterial=metal;Object.DestroyImmediate(knob.GetComponent<Collider>());
        foreach(float z in new[]{.13f,.34f})Box("護木箍",root,new Vector3(0,.013f,z),new Vector3(.067f,.086f,.020f),metal);
        Box("準星",root,new Vector3(0,.086f,.60f),new Vector3(.014f,.034f,.024f),metal);
        Box("照門",root,new Vector3(0,.087f,-.05f),new Vector3(.033f,.029f,.040f),metal);
        Box("槍托底板",root,new Vector3(0,-.047f,-.674f),new Vector3(.062f,.17f,.011f),metal);
        for(int i=0;i<12;i++)
        {float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;Beam("扳機護圈",root,new Vector3(0,-.046f+Mathf.Cos(a)*.041f,-.29f+Mathf.Sin(a)*.057f),new Vector3(0,-.046f+Mathf.Cos(b)*.041f,-.29f+Mathf.Sin(b)*.057f),.007f,metal);}
        Box("扳機",root,new Vector3(0,-.037f,-.28f),new Vector3(.007f,.030f,.009f),metal);
        return root;
    }
    static Chapter2Actor Actor(Chapter2Actor source,Transform parent,Vector3 p)
    {
        var a=Object.Instantiate(source.gameObject,parent).GetComponent<Chapter2Actor>();a.gameObject.SetActive(true);
        foreach(var script in a.GetComponentsInChildren<MonoBehaviour>(true))if(!(script is Chapter2Actor)&&!(script is Chapter2SpeechFace))Object.DestroyImmediate(script);
        foreach(var col in a.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
        foreach(var t in a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="賽德克佩刀").ToArray())Object.DestroyImmediate(t.gameObject);
        a.name=a.DisplayName;a.transform.position=p;a.seated=a.fallen=a.speaking=false;a.facing=Vector3.back;
        var bones=a.GetComponentsInChildren<Transform>(true);var l=bones.FirstOrDefault(t=>t.name.EndsWith("L_Upperarm")||t.name.EndsWith("LeftArm"));var r=bones.FirstOrDefault(t=>t.name.EndsWith("R_Upperarm")||t.name.EndsWith("RightArm"));
        if(l&&r){var f=Vector3.Cross(r.position-l.position,Vector3.up).normalized;a.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(f,Vector3.back,Vector3.up),Vector3.up)*a.transform.rotation;}
        return a;
    }
    public static void PolishExisting()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var c=Object.FindFirstObjectByType<Chapter3StationController>();
        if(!c||c.gameObject.scene.path!=ScenePath)throw new InvalidOperationException("Open the station scene first.");
        var village=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第三章.unity");
        var council=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var vc=village.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter3Controller>(true)).First();
            var cc=council.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();
            var cast=c.bawan.transform.parent;
            foreach(var a in cast.GetComponentsInChildren<Chapter2Actor>(true))Object.DestroyImmediate(a.gameObject);
            c.bawan=Actor(vc.bawan,cast,new Vector3(0,0,-15));
            c.pihu=Actor(vc.pihu,cast,new Vector3(1.6f,0,-17));
            c.mona=Actor(cc.mona,cast,new Vector3(0,0,1.4f));
            Actor(cc.watan,cast,new Vector3(5.8f,0,1)).facing=Vector3.left;
            foreach(var text in c.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)))
                text.characterSize=text.text.Contains("駐")?.036f:text.text.Contains("霧社")?.048f:.033f;
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
        }
        finally{EditorSceneManager.ClosePreviewScene(village);EditorSceneManager.ClosePreviewScene(council);}
    }
    static void Forest(Scene village,Vector3[] route)
    {
        var all=village.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var trees=all.Where(t=>t.name.StartsWith("林木 ")).Take(12).ToArray();
        var shrubs=all.Where(t=>t.name.StartsWith("林下植被 ")).Take(14).ToArray();
        var rocks=all.Where(t=>t.name.StartsWith("路邊岩石")||t.name.StartsWith("自然苔石")).Take(6).ToArray();
        var root=new GameObject("山林包圍的駐在所").transform;var rand=new System.Random(3101);
        for(int i=0;i<230;i++)
        {
            Vector3 p=new Vector3((float)rand.NextDouble()*100-40,0,(float)rand.NextDouble()*88-35);
            if(p.x>-12&&p.x<12&&p.z<18)continue;
            if(Distance(route,p)<4.7f)continue;
            p.y=GroundHeight(p);
            var t=Object.Instantiate(trees[i%trees.Length].gameObject,root).transform;t.name="駐在所林木 "+i;t.gameObject.SetActive(true);t.Rotate(0,(float)rand.NextDouble()*360,0,Space.World);Ground(t,p);
        }
        for(int i=0;i<390&&shrubs.Length>0;i++)
        {
            Vector3 p=new Vector3((float)rand.NextDouble()*69-22,0,(float)rand.NextDouble()*55-25);
            if(p.x>-10.4f&&p.x<10.4f&&p.z<16)continue;
            if(Distance(route,p)<3.1f)continue;
            p.y=GroundHeight(p);var t=Object.Instantiate(shrubs[i%shrubs.Length].gameObject,root).transform;t.name="路側蕨草 "+i;t.gameObject.SetActive(true);t.Rotate(0,(float)rand.NextDouble()*360,0,Space.World);Ground(t,p);
        }
        for(int i=0;i<23;i++)
        {
            float x=(i%2==0?-1:1)*(11.8f+(float)rand.NextDouble()*3),z=-19+(i/2)*3.7f;
            var t=Box("庭院石緣",root,new Vector3(x,.17f,z),new Vector3(.75f,.34f,1.3f),stone);t.rotation=Quaternion.Euler(0,(float)rand.NextDouble()*100,0);
        }
    }
    static float GroundHeight(Vector3 p)
    {
        float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,72,Mathf.Abs(p.x-8)))*16+Mathf.SmoothStep(0,1,Mathf.InverseLerp(21,65,p.z))*23;
        return rise*(.65f+Mathf.PerlinNoise((p.x+80)/2*.08f,(p.z+80)/2*.08f)*.5f)-.03f;
    }
    static float Distance(Vector3[] route,Vector3 p)
    {float best=999;for(int i=1;i<route.Length;i++){Vector3 a=route[i-1],b=route[i];float t=Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude;best=Mathf.Min(best,Vector3.Distance(p,Vector3.Lerp(a,b,Mathf.Clamp01(t))));}return best;}
    static void Ground(Transform t,Vector3 p)
    {var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)return;Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);t.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);}
    static void Lighting()
    {
        var sun=new GameObject("駐在所清晨光線").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.86f,.68f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(29,-30,0);
        RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.55f,.62f,.64f);RenderSettings.ambientEquatorColor=new Color(.39f,.43f,.37f);RenderSettings.ambientGroundColor=new Color(.24f,.26f,.22f);
        RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Environment/Dawn sky.mat");
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.57f,.63f,.60f);RenderSettings.fogDensity=.011f;
    }
}
