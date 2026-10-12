using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Builds a NEW chapter from read-only donor scenes. Rebuilding an existing scene is deliberately disallowed.
public static class Chapter3VillageAuthoring
{
    public const string ScenePath="Assets/Scenes/第三章.unity";
    const string Folder="Assets/Chapter3/Environment";
    static Material ground,trailMat,wood,cloth,stone;
    static Vector3[] route;
    [MenuItem("Tools/Chapter 3/Create Village and Forest Scene")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("第三章 already exists; edit it without rebuilding.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current scene changes first.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var donor=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="第三章";
            var c=new GameObject("第三章 · 部落與山林劇情").AddComponent<Chapter3Controller>();
            c.dialogue=c.gameObject.AddComponent<Chapter2LocalDialogue>();
            route=new[]{new Vector3(0,0,3.5f),new Vector3(0,0,10),new Vector3(0,0,17),new Vector3(5,0,24),new Vector3(11,0,32),new Vector3(10,0,40),new Vector3(3,0,49),new Vector3(-1,0,60),new Vector3(5,0,70),new Vector3(10,0,78)};
            for(int i=0;i<route.Length;i++)route[i].y=Chapter3Controller.Height(route[i].x,route[i].z);
            c.trail=route;
            Materials();Landscape();
            Houses(donor);Forest(source);
            var cast=new GameObject("出發的同伴與留守族人").transform;
            c.bawan=Actor(donor.bawan,cast,new Vector3(0,0,3.5f));
            c.pihu=Actor(donor.conservatives[0],cast,new Vector3(2.1f,0,3.7f));
            Actor(donor.leaders[2],cast,new Vector3(-5.1f,0,1.4f));
            Actor(donor.leaders[5],cast,new Vector3(6.9f,0,-1));
            var child=donor.nightGroup.GetComponentsInChildren<Chapter2Actor>(true).FirstOrDefault(a=>a.isChild);
            if(child)Actor(child,cast,new Vector3(6.3f,0,-.4f));
            c.pihu.gameObject.AddComponent<Chapter3CompanionGesture>();
            var props=new GameObject("部落整裝 · 木桌獵刀與柴堆").transform;
            var table=Box("整裝木桌",props,new Vector3(-2.5f,.76f,3.9f),new Vector3(1.6f,.12f,.78f),wood);
            foreach(float x in new[]{-3.08f,-1.92f})foreach(float z in new[]{3.65f,4.15f})Box("桌腳",props,new Vector3(x,.36f,z),new Vector3(.11f,.72f,.11f),wood);
            var knifeDonor=donor.nightGroup.GetComponentsInChildren<Chapter2CouncilDrama>(true).First(d=>d.knife).knife;
            var knife=Object.Instantiate(knifeDonor.gameObject,props).transform;knife.name="可領取的獵刀";
            knife.gameObject.SetActive(true);knife.localScale=Vector3.one*1.35f;knife.SetPositionAndRotation(new Vector3(-2.55f,.84f,3.9f),Quaternion.Euler(90,30,0));
            var sheath=knife.Find("刀鞘");if(sheath)sheath.gameObject.SetActive(false);
            c.knifePickup=knife;
            for(int i=0;i<6;i++)Cylinder("堆疊木柴",props,new Vector3(-5.6f,.17f+(i/3)*.22f,4.8f+(i%3)*.25f),new Vector3(.22f,.85f,.22f),Quaternion.Euler(0,0,90),wood);
            for(int i=0;i<3;i++)Cylinder("倚牆竹槍",props,new Vector3(-6.2f+i*.19f,1.15f,3.7f),new Vector3(.035f,1.1f,.035f),Quaternion.Euler(0,0,-8+i*4),wood);
            Player(c);
            c.ui=Chapter3Presentation.Build(c.player.view,AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF"));
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            Lighting();
            var ambience=new GameObject("清晨林間環境聲").AddComponent<AudioSource>();
            ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/forest_morning.wav");ambience.loop=true;ambience.playOnAwake=true;ambience.volume=.32f;ambience.spatialBlend=0;
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            HookMenu();SceneManager.SetActiveScene(scene);
            Selection.activeGameObject=c.gameObject;
            var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(0,1,5),Quaternion.Euler(25,0,0),17);
            Chapter2GameViewScaleGuard.Restore();
            Debug.Log("[Chapter3] Created village, forest trail, three-turn conversation and companion story.");
        }
        finally{EditorSceneManager.ClosePreviewScene(source);}
    }
    static void Materials()
    {
        ground=Mat("Forest earth",new Color(.64f,.62f,.51f),"Assets/Chapter2/Environment/PolyHaven/forest_floor_diff_4k.jpg",new Vector2(1,1));
        trailMat=Mat("Walked earth",new Color(.72f,.64f,.51f),"Assets/Chapter2/Environment/RichForest/Textures/brown_mud_diff_2k.jpg",Vector2.one);
        wood=Mat("Weathered wood",new Color(.26f,.18f,.105f),"Assets/Chapter2/Props/CouncilKnifeWoodGrain.png",new Vector2(1,2));
        cloth=Mat("Woven pack",new Color(.33f,.25f,.15f),null,Vector2.one);
        stone=Mat("River stone",new Color(.26f,.29f,.25f),null,Vector2.one);
    }
    static Material Mat(string name,Color color,string texture,Vector2 tiling)
    {
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.1f);
        if(texture!=null){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture>(texture));m.SetTextureScale("_BaseMap",tiling);}
        EditorUtility.SetDirty(m);return m;
    }
    static void Landscape()
    {
        var root=new GameObject("山林地形與蜿蜒土路").transform;
        const int nx=111,nz=161;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tris=new List<int>();
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            int i=z*nx+x;float xx=x*1.5f-82.5f,zz=z*1.5f-35;v[i]=new Vector3(xx,Chapter3Controller.Height(xx,zz),zz);uv[i]=new Vector2(xx/5,zz/5);
            if(x<nx-1&&z<nz-1)tris.AddRange(new[]{i,i+nx,i+1,i+1,i+nx,i+nx+1});
        }
        MeshObject(root,"山林土地",v,uv,tris.ToArray(),ground,true);
        // Path ribbon follows the same height function as the physical ground.
        var points=new List<Vector3>();
        for(int i=1;i<route.Length;i++)for(int j=0;j<20;j++)points.Add(Vector3.Lerp(route[i-1],route[i],j/20f));points.Add(route.Last());
        var pv=new List<Vector3>();var pu=new List<Vector2>();var pt=new List<int>();float distance=0;
        for(int i=0;i<points.Count;i++)
        {
            Vector3 forward=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            if(i>0)distance+=Vector3.Distance(points[i],points[i-1]);
            float width=1.7f+.15f*Mathf.Sin(i*.45f);
            for(int j=0;j<2;j++){Vector3 p=points[i]+side*(j==0?-width:width);p.y=Chapter3Controller.Height(p.x,p.z)+.025f;pv.Add(p);pu.Add(new Vector2(j,distance/3));}
            if(i>0){int a=(i-1)*2;pt.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        }
        MeshObject(root,"往駐在所的土路",pv.ToArray(),pu.ToArray(),pt.ToArray(),trailMat,false);
        // The departure clearing is dirt, with an irregular edge hidden by plants and stones.
        var cv=new List<Vector3>{new Vector3(0,.018f,1)};var cu=new List<Vector2>{Vector2.zero};var ct=new List<int>();
        for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;Vector3 p=new Vector3(Mathf.Cos(a)*9,.018f,1+Mathf.Sin(a)*10);cv.Add(p);cu.Add(new Vector2(p.x/3,p.z/3));if(i>0)ct.AddRange(new[]{0,i+1,i});}
        MeshObject(root,"部落整裝空地",cv.ToArray(),cu.ToArray(),ct.ToArray(),trailMat,false);
    }
    static void MeshObject(Transform root,string name,Vector3[] v,Vector2[] uv,int[] triangles,Material m,bool collider)
    {
        string path=Folder+"/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Houses(Chapter2Controller donor)
    {
        var group=new GameObject("部落 · 既有傳統建築").transform;
        var houses=donor.nightGroup.transform.Find("會議外圍房屋 · 第一章模型").Cast<Transform>().ToArray();
        Vector3[] pos={new Vector3(-10,0,2),new Vector3(10,0,2),new Vector3(-9,0,-10),new Vector3(10,0,-10),new Vector3(-11,0,13)};
        float[] yaw={75,-75,30,-30,90};
        for(int i=0;i<pos.Length;i++)
        {
            var h=Object.Instantiate(houses[i%houses.Length].gameObject,group).transform;
            h.name=houses[i%houses.Length].name;
            h.gameObject.SetActive(true);h.rotation=Quaternion.AngleAxis(yaw[i],Vector3.up)*houses[i%houses.Length].rotation;
            Chapter2CouncilVillageAuthoring.LevelHouse(h,pos[i]);
        }
    }
    static void Forest(Scene source)
    {
        var transforms=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var donors=transforms.Where(t=>t.name.StartsWith("Grove tree ")).Take(14).ToArray();
        if(donors.Length==0)throw new InvalidOperationException("Missing dressed forest donors.");
        var root=new GameObject("樹冠與林下植物").transform;
        var random=new System.Random(303);
        for(int i=0;i<130;i++)
        {
            float x=(float)random.NextDouble()*82-41,z=(float)random.NextDouble()*119-24;
            if(Mathf.Abs(x)<17&&z<17)continue;
            if(PathDistance(new Vector3(x,0,z))<4.5f)continue;
            var sourceTree=donors[i%donors.Length];var t=Object.Instantiate(sourceTree.gameObject,root).transform;t.name="林木 "+i;t.gameObject.SetActive(true);
            foreach(var script in t.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var col in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            t.rotation=Quaternion.AngleAxis((float)random.NextDouble()*360,Vector3.up)*sourceTree.rotation;
            t.localScale*=.8f+(float)random.NextDouble()*.4f;
            Ground(t,new Vector3(x,Chapter3Controller.Height(x,z),z));
        }
        // Reuse converted understory from Chapter 2, preserving its material overrides.
        var under=transforms.FirstOrDefault(t=>t.name=="Undergrowth · fern grass shrub flower pockets");
        var shrubs=under?under.Cast<Transform>().Where(t=>t.GetComponentInChildren<Renderer>()).Take(20).ToArray():new Transform[0];
        for(int i=0;i<210&&shrubs.Length>0;i++)
        {
            Vector3 p=route[random.Next(1,route.Length)]+new Vector3((float)random.NextDouble()*24-12,0,(float)random.NextDouble()*16-8);
            if(PathDistance(p)<2.15f||p.z<15)continue;
            var t=Object.Instantiate(shrubs[i%shrubs.Length].gameObject,root).transform;t.name="林下植被 "+i;t.gameObject.SetActive(true);
            foreach(var script in t.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var col in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            t.rotation=Quaternion.AngleAxis((float)random.NextDouble()*360,Vector3.up)*t.rotation;
            Ground(t,new Vector3(p.x,Chapter3Controller.Height(p.x,p.z),p.z));
        }
        var rocks=new GameObject("路邊石塊").transform;
        for(int i=0;i<35;i++)
        {
            int j=random.Next(1,route.Length);Vector3 p=route[j]+new Vector3((i%2==0?-1:1)*(3.6f+(float)random.NextDouble()*3),0,(float)random.NextDouble()*6-3);
            p.y=Chapter3Controller.Height(p.x,p.z)+.16f;
            var r=GameObject.CreatePrimitive(PrimitiveType.Sphere);r.name="苔石";r.transform.SetParent(rocks);r.transform.position=p;r.transform.localScale=new Vector3(.6f,.45f,.9f)*( .6f+(float)random.NextDouble());r.GetComponent<Renderer>().sharedMaterial=stone;
        }
    }
    static float PathDistance(Vector3 p)
    {
        float result=float.MaxValue;p.y=0;
        for(int i=1;i<route.Length;i++){Vector3 a=route[i-1],b=route[i];a.y=b.y=0;float t=Vector3.Dot(p-a,b-a)/(b-a).sqrMagnitude;result=Mathf.Min(result,Vector3.Distance(p,Vector3.Lerp(a,b,Mathf.Clamp01(t))));}return result;
    }
    static void Ground(Transform t,Vector3 position)
    {
        var renderers=t.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        t.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
    static Chapter2Actor Actor(Chapter2Actor source,Transform parent,Vector3 pos)
    {
        var a=Object.Instantiate(source.gameObject,parent).GetComponent<Chapter2Actor>();a.gameObject.SetActive(true);
        foreach(var script in a.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(script is Chapter2Actor)&&!(script is Chapter2SpeechFace))Object.DestroyImmediate(script);
        foreach(var col in a.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
        foreach(var t in a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="賽德克佩刀").ToArray())Object.DestroyImmediate(t.gameObject);
        a.name=a.DisplayName;a.transform.position=pos;a.seated=a.fallen=a.speaking=false;a.facing=Vector3.back;
        return a;
    }
    static void Player(Chapter3Controller c)
    {
        var go=new GameObject("玩家 · 鍵鼠與VR",typeof(CharacterController));var motor=go.GetComponent<CharacterController>();
        motor.height=1.72f;motor.center=new Vector3(0,.9f,0);motor.radius=.24f;motor.stepOffset=.3f;
        go.transform.position=new Vector3(0,.05f,1.05f);c.player=go.AddComponent<Chapter3Player>();c.player.chapter=c;
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.SetParent(go.transform,false);camera.transform.localPosition=new Vector3(0,1.65f,0);
        c.player.view=camera.GetComponent<Camera>();c.player.view.fieldOfView=64;c.player.view.nearClipPlane=.06f;c.player.view.farClipPlane=190;
        c.player.view.allowHDR=true;c.player.view.backgroundColor=new Color(.48f,.54f,.55f);
    }
    static void Lighting()
    {
        var sun=new GameObject("清晨斜陽").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.80f,.56f);sun.intensity=1.4f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(23,-35,0);
        RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.50f,.59f,.63f);RenderSettings.ambientEquatorColor=new Color(.36f,.40f,.37f);RenderSettings.ambientGroundColor=new Color(.23f,.25f,.20f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.55f,.61f,.6f);RenderSettings.fogDensity=.013f;
        var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.45f,.48f,.48f));sky.SetFloat("_AtmosphereThickness",1.2f);sky.SetFloat("_Exposure",.9f);AssetDatabase.CreateAsset(sky,Folder+"/Dawn sky.mat");RenderSettings.skybox=sky;
    }
    static Transform Box(string name,Transform parent,Vector3 p,Vector3 size,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;return go.transform;
    }
    static void Cylinder(string name,Transform parent,Vector3 p,Vector3 size,Quaternion q,Material m)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,true);g.transform.SetPositionAndRotation(p,q);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;}
    static void HookMenu()
    {
        var menu=EditorSceneManager.OpenScene("Assets/Scenes/選擇章節ˊ.unity",OpenSceneMode.Additive);
        try
        {
            var roots=menu.GetRootGameObjects();var label=roots.SelectMany(r=>r.GetComponentsInChildren<TextMeshProUGUI>(true)).First(t=>t.text.Trim()=="第三章");
            var button=label.GetComponentInParent<Button>();var loader=roots.SelectMany(r=>r.GetComponentsInChildren<tiao>(true)).FirstOrDefault();if(!loader)loader=label.GetComponentInParent<Canvas>().gameObject.AddComponent<tiao>();
            while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);
            UnityEventTools.AddPersistentListener(button.onClick,loader.JumpToChapter3);EditorUtility.SetDirty(button);EditorSceneManager.SaveScene(menu);
        }
        finally{EditorSceneManager.CloseScene(menu,true);}
    }
    public static void PolishExisting()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter3Controller>();if(!c||c.gameObject.scene.path!=ScenePath)throw new InvalidOperationException("Open Chapter 3.");
        route=c.trail;Materials();
        var terrain=GameObject.Find("山林地形與蜿蜒土路");if(terrain)Object.DestroyImmediate(terrain);
        Landscape();c.player.transform.position=new Vector3(0,.05f,1.05f);
        c.player.FocusOn(c.bawan.transform.position+Vector3.up*1.08f);
        if(!c.pihu.GetComponent<Chapter3CompanionGesture>())c.pihu.gameObject.AddComponent<Chapter3CompanionGesture>();
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var all=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var plants=all.Where(t=>t.name.StartsWith("Fern ")||t.name.StartsWith("Low shrub ")||t.name.StartsWith("Grass clump ")).Take(40).ToArray();
            var trees=all.Where(t=>t.name.StartsWith("Grove tree ")).Take(14).ToArray();
            var rocks=all.Where(t=>t.name.StartsWith("Weathered stone ")).Take(10).ToArray();
            var old=GameObject.Find("第三章 · 林下細節");if(old)Object.DestroyImmediate(old);
            var root=new GameObject("第三章 · 林下細節").transform;
            var random=new System.Random(308);
            for(int i=0;i<650;i++)
            {
                Vector3 p=Vector3.Lerp(route[0],route.Last(),(float)random.NextDouble());p.x+=(float)random.NextDouble()*42-21;p.z+=(float)random.NextDouble()*14-7;
                if(p.z<15||PathDistance(p)<2.2f)continue;
                var t=Object.Instantiate(plants[i%plants.Length].gameObject,root).transform;t.name="林下草蕨 "+i;t.gameObject.SetActive(true);
                t.rotation=Quaternion.AngleAxis((float)random.NextDouble()*360,Vector3.up)*t.rotation;t.localScale*=.85f+(float)random.NextDouble()*.55f;
                Ground(t,new Vector3(p.x,Chapter3Controller.Height(p.x,p.z),p.z));
            }
            for(int i=0;i<160;i++)
            {
                float x=(float)random.NextDouble()*85-42.5f,z=17+(float)random.NextDouble()*125;
                if(PathDistance(new Vector3(x,0,z))<5)continue;
                var t=Object.Instantiate(trees[i%trees.Length].gameObject,root).transform;t.name="山腰林木 "+i;t.gameObject.SetActive(true);
                t.rotation=Quaternion.AngleAxis((float)random.NextDouble()*360,Vector3.up)*t.rotation;t.localScale*=.7f+(float)random.NextDouble()*.55f;
                Ground(t,new Vector3(x,Chapter3Controller.Height(x,z),z));
            }
            // Replace the initial rounded blockout stones with the existing textured meshes.
            var stoneRoot=GameObject.Find("路邊石塊").transform;
            var positions=stoneRoot.Cast<Transform>().Select(t=>t.position).ToArray();
            foreach(var t in stoneRoot.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);
            for(int i=0;i<positions.Length;i++)
            {
                var t=Object.Instantiate(rocks[i%rocks.Length].gameObject,stoneRoot).transform;t.name="路邊風化石 "+i;t.gameObject.SetActive(true);t.localScale*=2.2f;
                Vector3 p=positions[i];p.y=Chapter3Controller.Height(p.x,p.z)-.03f;Ground(t,p);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var col in root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            root.gameObject.AddComponent<Chapter2UnderstoryInstances>();
        }
        finally{EditorSceneManager.ClosePreviewScene(source);}
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
