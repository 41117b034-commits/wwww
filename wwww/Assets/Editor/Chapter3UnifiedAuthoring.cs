using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class Chapter3UnifiedAuthoring
{
    const string ScenePath="Assets/Scenes/第三章.unity";
    const string Folder="Assets/Chapter3/Combat";
    static Material wood,rock,soil,skin;
    static Font font;
    [MenuItem("Tools/Chapter 3/Merge Regions and Build Combat")]
    public static void Build()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play and save first.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        if(Object.FindFirstObjectByType<Chapter3Flow>())throw new InvalidOperationException("Already merged. Edit the saved Chapter 3 scene.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF");
        wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Aged cedar boards.mat");
        rock=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Foundation stone.mat");
        soil=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Trampled courtyard.mat");
        skin=AssetDatabase.LoadAssetAtPath<Material>("Assets/人/手/HandSkin.mat");
        var village=Object.FindFirstObjectByType<Chapter3Controller>();
        var player=village.player;var ui=village.ui;
        var flow=new GameObject("第三章 · 單一場景流程與快速跳段").AddComponent<Chapter3Flow>();
        var villageRoots=scene.GetRootGameObjects();
        flow.villageRegion=new GameObject("01 部落與山林");
        foreach(var root in villageRoots)
        {
            if(root==flow.gameObject||root==player.transform.root.gameObject||root==ui.transform.root.gameObject||root.GetComponent<EventSystem>())continue;
            root.transform.SetParent(flow.villageRegion.transform,true);
        }
        var stationScene=EditorSceneManager.OpenScene(Chapter3StationAuthoring.ScenePath,OpenSceneMode.Additive);
        var station=stationScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter3StationController>(true)).Single();
        var redundantPlayer=station.player.gameObject;var redundantUI=station.ui.gameObject;
        station.player=player;station.ui=ui;
        Object.DestroyImmediate(redundantPlayer);Object.DestroyImmediate(redundantUI);
        SceneManager.SetActiveScene(scene);
        flow.stationRegion=new GameObject("02 駐在所");
        foreach(var root in stationScene.GetRootGameObjects())
        {
            if(root.GetComponent<EventSystem>()){Object.DestroyImmediate(root);continue;}
            SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetParent(flow.stationRegion.transform,true);
        }
        EditorSceneManager.CloseScene(stationScene,true);
        flow.village=village;flow.station=station;flow.player=player;flow.ui=ui;
        village.flow=flow;station.flow=flow;
        flow.combatRegion=new GameObject("03 山路短交戰");
        var combat=flow.combatRegion.AddComponent<Chapter3Combat>();flow.combat=combat;combat.flow=flow;combat.player=player;combat.ui=ui;
        var council=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        try
        {
            var donor=council.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();
            BuildArena(flow,combat,station,donor.officer);
            BuildWeapons(combat,village.knifePickup,station.riflePickup);
            BuildHud(flow,combat);
        }
        finally{EditorSceneManager.ClosePreviewScene(council);}
        flow.stationRegion.SetActive(false);flow.combatRegion.SetActive(false);
        // Region lights are toggled with their environment; one remains active at a time.
        RenderSettings.sun=flow.villageRegion.GetComponentInChildren<Light>();
        player.chapter=village;player.station=null;player.combat=null;
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=Chapter3StationAuthoring.ScenePath).ToArray();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Chapter2GameViewScaleGuard.Restore();Selection.activeGameObject=flow.gameObject;
    }
    static void BuildArena(Chapter3Flow flow,Chapter3Combat c,Chapter3StationController station,Chapter2Actor officer)
    {
        var root=flow.combatRegion.transform;
        Box("山路地面",root,new Vector3(0,-.12f,9),new Vector3(22,.24f,49),soil,true);
        var sun=Object.Instantiate(flow.stationRegion.GetComponentInChildren<Light>().gameObject,root);sun.name="山路清晨光線";
        var ambience=root.gameObject.AddComponent<AudioSource>();ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/forest_morning.wav");ambience.loop=ambience.playOnAwake=true;ambience.volume=.2f;
        // Use the project's forest library; keep the playable lane clear.
        var trees=flow.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("駐在所林木 ")).Take(10).ToArray();
        var shrubs=flow.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("路側蕨草 ")).Take(10).ToArray();
        var random=new System.Random(3212);
        for(int i=0;i<90;i++)
        {
            float side=i%2==0?-1:1;
            var p=new Vector3(side*(8+(float)random.NextDouble()*15),0,-18+(float)random.NextDouble()*65);
            var t=Object.Instantiate(trees[i%trees.Length].gameObject,root).transform;t.name="交戰道路林木 "+i;t.gameObject.SetActive(true);Ground(t,p);
        }
        for(int i=0;i<90;i++)
        {
            var p=new Vector3((i%2==0?-1:1)*(6.7f+(float)random.NextDouble()*3.8f),0,-12+(float)random.NextDouble()*43);
            var t=Object.Instantiate(shrubs[i%shrubs.Length].gameObject,root).transform;t.name="交戰道路蕨草 "+i;t.gameObject.SetActive(true);Ground(t,p);
        }
        Rock(root,new Vector3(-3.2f,.77f,-2),new Vector3(3.1f,1.65f,2.6f));
        Rock(root,new Vector3(3.2f,.9f,7),new Vector3(3.1f,1.85f,2.1f));
        Rock(root,new Vector3(-4.3f,.88f,17.5f),new Vector3(2.5f,1.9f,2.2f));
        for(int i=0;i<2;i++)Box("路邊木箱掩體",root,new Vector3(-4.8f+i*1.05f,.72f,4.5f),new Vector3(1,1.44f,1.1f),wood,true);
        Box("後方彈藥木箱",root,new Vector3(-3.6f,.46f,-8),new Vector3(1.15f,.92f,.7f),wood,true);
        c.ammoPoint=Point(root,"備彈補給位置",new Vector3(-3.6f,0,-8));
        c.exitPoint=Point(root,"往學校的銜接路口",new Vector3(0,0,28));
        c.playerTarget=Point(root,"敵人瞄準的玩家胸前",c.spawn+Vector3.up*1.25f);
        c.runtimeEnemies=Point(root,"交戰中的巡警",Vector3.zero);
        c.bawan=CloneCompanion(station.bawan,root,new Vector3(-5.1f,0,-5));
        c.pihu=CloneCompanion(station.pihu,root,new Vector3(5.2f,0,-6));
        c.enemyTemplates=new GameObject[3];
        for(int i=0;i<3;i++)
        {
            var go=Object.Instantiate(officer.gameObject,root);go.name="巡警 "+(i+1)+" 範本";
            foreach(var m in go.GetComponentsInChildren<MonoBehaviour>(true))
                if(!(m is Chapter2Actor)&&!(m is Chapter2Rifle))Object.DestroyImmediate(m);
            foreach(var col in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            var actor=go.GetComponent<Chapter2Actor>();actor.characterName="巡警";actor.romanizedName="";actor.seated=actor.fallen=actor.speaking=false;actor.facing=Vector3.back;
            go.transform.position=new Vector3(i==0?0:i==1?-2:2,0,7+i*7);
            var collider=go.AddComponent<CapsuleCollider>();collider.height=1.65f;collider.radius=.32f;collider.center=new Vector3(0,.86f,0);
            var enemy=go.AddComponent<Chapter3Enemy>();enemy.combat=c;enemy.actor=actor;enemy.rifle=go.GetComponent<Chapter2Rifle>();enemy.advances=i==0;
            if(enemy.rifle){enemy.rifle.target=null;enemy.rifle.aimTarget=c.playerTarget;enemy.rifle.aim=0;enemy.rifle.lowered=.8f;}
            c.enemyTemplates[i]=go;go.SetActive(false);
        }
        var sign=Point(root,"前往霧社的木路標",new Vector3(3.8f,0,26));
        Box("木柱",sign,new Vector3(0,.9f,0),new Vector3(.1f,1.8f,.1f),wood);
        Box("路牌",sign,new Vector3(0,1.5f,0),new Vector3(1.5f,.42f,.1f),wood);
        var text=new GameObject("霧社方向",typeof(TextMesh)).GetComponent<TextMesh>();text.transform.SetParent(sign,false);text.transform.localPosition=new Vector3(0,1.52f,-.065f);text.text="霧社 ↑";text.font=font;text.fontSize=60;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.1f,.1f,.08f);text.GetComponent<Renderer>().sharedMaterial=font.material;
    }
    static Chapter2Actor CloneCompanion(Chapter2Actor source,Transform parent,Vector3 position)
    {
        var a=Object.Instantiate(source.gameObject,parent).GetComponent<Chapter2Actor>();a.transform.position=position;a.gameObject.SetActive(true);return a;
    }
    static void BuildWeapons(Chapter3Combat c,Transform knifeSource,Transform rifleSource)
    {
        var root=new GameObject("第一人稱獵刀與步槍").transform;root.SetParent(c.player.view.transform,false);
        var w=root.gameObject.AddComponent<Chapter3Weapons>();c.weapons=w;w.combat=c;
        w.knife=Object.Instantiate(knifeSource.gameObject,root).transform;w.knife.name="手持獵刀";w.knife.localScale=Vector3.one;
        w.rifle=Object.Instantiate(rifleSource.gameObject,root).transform;w.rifle.name="手持村田十八年式";w.rifle.localScale=Vector3.one;
        w.knife.gameObject.SetActive(false);w.rifle.gameObject.SetActive(true);w.bolt=w.rifle.Find("槍機");
        foreach(var collider in root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        w.muzzle=Point(w.rifle,"槍口",new Vector3(0,.055f,.67f));
        var flash=GameObject.CreatePrimitive(PrimitiveType.Sphere);flash.name="短暫槍口火光";flash.transform.SetParent(w.muzzle,false);flash.transform.localScale=new Vector3(.065f,.065f,.16f);Object.DestroyImmediate(flash.GetComponent<Collider>());
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(1,.69f,.18f);AssetDatabase.CreateAsset(material,Folder+"/Muzzle flash.mat");flash.GetComponent<Renderer>().sharedMaterial=material;w.flash=flash;flash.SetActive(false);
        w.audioSource=root.gameObject.AddComponent<AudioSource>();w.audioSource.playOnAwake=false;w.audioSource.spatialBlend=0;w.shotClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/rifle_report.wav");
        // Closed grips are local game meshes, with distinct fingers and forearm silhouette.
        w.rightHand=Hand(root,false);w.leftHand=Hand(root,true);
        root.gameObject.SetActive(false);
    }
    static Transform Hand(Transform parent,bool left)
    {
        var root=Point(parent,left?"左手護木持握":"右手持握",Vector3.zero);
        // Grip axis is Y, with four fingers wrapping around a 3 cm handle.
        var palm=Sphere("掌背",root,new Vector3(.035f,-.004f,-.004f),new Vector3(.040f,.091f,.074f),skin);
        for(int i=0;i<4;i++)
        {
            float y=-.035f+i*.021f;
            Segment(root,"指節",new Vector3(.040f,y,-.022f),new Vector3(.015f,y,-.046f),.019f,skin);
            Segment(root,"彎指",new Vector3(.015f,y,-.046f),new Vector3(-.024f,y,-.025f),.018f,skin);
            Segment(root,"指尖",new Vector3(-.024f,y,-.025f),new Vector3(-.026f,y,.001f),.017f,skin);
        }
        Segment(root,"拇指",new Vector3(.043f,.040f,.019f),new Vector3(.004f,.018f,.027f),.027f,skin);
        Segment(root,"手腕",new Vector3(.054f,-.050f,.01f),new Vector3(.090f,-.18f,-.018f),.069f,skin);
        if(left)root.localScale=new Vector3(-1,1,1);
        return root;
    }
    static void BuildHud(Chapter3Flow f,Chapter3Combat c)
    {
        var parent=f.ui.canvas.transform;
        var hotkeys=Text(parent,"快速跳段  F6 駐在所  ·  F7 短交戰\n空白鍵略過預寫台詞",19,new Vector2(.60f,.89f),new Vector2(.98f,.975f));hotkeys.alignment=TextAnchor.UpperRight;hotkeys.color=new Color(.9f,.87f,.75f);
        c.hud=Panel(parent,"交戰介面",Vector2.zero,Vector2.one,Color.clear);c.hud.GetComponent<Image>().raycastTarget=false;
        c.healthText=Text(c.hud.transform,"",25,new Vector2(.025f,.72f),new Vector2(.25f,.78f));
        c.ammoText=Text(c.hud.transform,"",24,new Vector2(.025f,.66f),new Vector2(.38f,.72f));
        c.combatText=Text(c.hud.transform,"",23,new Vector2(.025f,.51f),new Vector2(.40f,.65f));
        c.crosshair=Text(c.hud.transform,"",26,new Vector2(.475f,.47f),new Vector2(.525f,.53f));c.crosshair.alignment=TextAnchor.MiddleCenter;
        c.hurtOverlay=Panel(c.hud.transform,"受傷泛紅",Vector2.zero,Vector2.one,Color.clear).GetComponent<Image>();c.hurtOverlay.raycastTarget=false;
        c.downPanel=Panel(parent,"檢查點重試",new Vector2(.26f,.31f),new Vector2(.74f,.67f),new Color(.04f,.04f,.035f,.97f));
        Text(c.downPanel.transform,"你受傷倒下了",34,new Vector2(.08f,.70f),new Vector2(.92f,.91f));
        Text(c.downPanel.transform,"從交戰前重試，巴萬與比胡仍在身旁。",23,new Vector2(.08f,.41f),new Vector2(.92f,.64f));
        var button=Panel(c.downPanel.transform,"重試按鈕",new Vector2(.22f,.12f),new Vector2(.78f,.31f),new Color(.3f,.36f,.25f));c.retry=button.AddComponent<Button>();c.retry.targetGraphic=button.GetComponent<Image>();
        var t=Text(button.transform,"R  從檢查點重試",24,Vector2.zero,Vector2.one);t.alignment=TextAnchor.MiddleCenter;
        c.hud.SetActive(false);c.downPanel.SetActive(false);
    }
    static GameObject Panel(Transform p,string name,Vector2 min,Vector2 max,Color color)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(p,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;g.GetComponent<Image>().color=color;return g;
    }
    static Text Text(Transform p,string value,int size,Vector2 min,Vector2 max)
    {
        var g=new GameObject("文字",typeof(RectTransform),typeof(Text));var r=(RectTransform)g.transform;r.SetParent(p,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=g.GetComponent<Text>();t.text=value;t.font=font;t.fontSize=size;t.color=new Color(.96f,.94f,.86f);t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;return t;
    }
    static Transform Point(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static Transform Box(string n,Transform root,Vector3 p,Vector3 size,Material mat,bool collision=false)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
    static Transform Sphere(string n,Transform root,Vector3 p,Vector3 size,Material mat)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
    static void Segment(Transform root,string n,Vector3 a,Vector3 b,float radius,Material mat)
    {var t=Sphere(n,root,(a+b)*.5f,new Vector3(radius,(b-a).magnitude+radius,radius),mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Rock(Transform root,Vector3 p,Vector3 scale)
    {var t=Sphere("山路岩石掩體",root,p,scale,rock);t.gameObject.AddComponent<BoxCollider>();}
    static void Ground(Transform t,Vector3 p)
    {var rs=t.GetComponentsInChildren<Renderer>();Bounds bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);t.position+=p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);}
    public static void Polish()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var f=Object.FindFirstObjectByType<Chapter3Flow>();if(!f)throw new InvalidOperationException("Open merged Chapter 3.");
        var c=f.combat;var root=f.combatRegion.transform;
        foreach(var e in c.enemyTemplates)
        {
            var collider=e.GetComponent<CapsuleCollider>();var scale=e.transform.lossyScale;
            collider.height=1.7f/scale.y;collider.radius=.31f/Mathf.Max(scale.x,scale.z);collider.center=new Vector3(0,.88f/scale.y,0);
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="山路岩石掩體").ToArray())
        {
            var pos=t.localPosition;var size=t.localScale;
            var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rock_pack/prefab/Rock_01.prefab"),t);
            visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
            foreach(var col in visual.GetComponentsInChildren<Collider>())Object.DestroyImmediate(col);
            Bounds b=visual.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in visual.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
            visual.transform.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
            b=visual.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in visual.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
            visual.transform.position+=t.position-b.center;
            Object.DestroyImmediate(t.GetComponent<MeshRenderer>());Object.DestroyImmediate(t.GetComponent<MeshFilter>());
        }
        const int nx=65,nz=85;
        var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var indices=new System.Collections.Generic.List<int>();
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            float xx=x*2-64,zz=z*2-58;
            float height=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,46,Mathf.Abs(xx)))*15+Mathf.SmoothStep(0,1,Mathf.InverseLerp(35,89,zz))*26;
            int i=z*nx+x;vertices[i]=new Vector3(xx,height*(.7f+Mathf.PerlinNoise(x*.10f,z*.09f)*.6f)-.07f,zz);uv[i]=new Vector2(xx/4,zz/4);
            if(x<nx-1&&z<nz-1)indices.AddRange(new[]{i,i+nx,i+1,i+1,i+nx,i+nx+1});
        }
        var mesh=new Mesh{name="交戰山谷地形"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Encounter valley.asset");
        var ground=new GameObject("交戰山谷地形",typeof(MeshFilter),typeof(MeshRenderer));ground.transform.SetParent(root,false);ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Station forest soil.mat");
        c.hud.GetComponent<Image>().raycastTarget=false;
        var panel=Panel(c.hud.transform,"體力彈藥資訊底板",new Vector2(.018f,.505f),new Vector2(.40f,.79f),new Color(.025f,.035f,.03f,.75f));panel.GetComponent<Image>().raycastTarget=false;panel.transform.SetAsFirstSibling();
        AddReloadRound(c.weapons);
        EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void AddReloadRound(Chapter3Weapons w)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.52f,.34f,.10f);material.SetFloat("_Metallic",.7f);AssetDatabase.CreateAsset(material,Folder+"/Cartridge brass.mat");
        w.cartridge=Sphere("單發裝填的彈藥",w.rifle,new Vector3(.04f,.10f,-.25f),new Vector3(.016f,.016f,.065f),material);w.cartridge.gameObject.SetActive(false);
        w.spentCase=Sphere("退出的空彈殼",w.rifle,new Vector3(.04f,.10f,-.25f),new Vector3(.016f,.016f,.055f),material);w.spentCase.gameObject.SetActive(false);
    }
    public static void RefineHands()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var f=Object.FindFirstObjectByType<Chapter3Flow>();var w=f.combat.weapons;
        foreach(var old in f.gameObject.scene.GetRootGameObjects().Where(g=>g.name=="Hands_Rigged(Clone)"))Object.DestroyImmediate(old);
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/手/Hands_Rigged.fbx"));
        try
        {
            var renderer=go.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Hand.R");
            Transform Bone(string name)=>renderer.bones.First(b=>b.name==name);
            Vector3 forward=(Bone("Bone.003").position-Bone("Bone.016").position).normalized;
            Vector3 across=(Bone("Bone").position-Bone("Bone.009").position).normalized;
            across=Vector3.ProjectOnPlane(across,forward).normalized;
            Vector3 normal=Vector3.Cross(forward,across).normalized;
            Vector3 origin=Bone("Bone.003").position+forward*.075f+normal*.075f;
            foreach(string[] finger in new[]{new[]{"Bone","Bone.001","Bone.002"},new[]{"Bone.003","Bone.004","Bone.005"},new[]{"Bone.006","Bone.007","Bone.008"},new[]{"Bone.009","Bone.010","Bone.011"}})
                for(int i=0;i<3;i++){var b=Bone(finger[i]);b.rotation=Quaternion.AngleAxis(i==0?-62:i==1?-72:-42,across)*b.rotation;}
            // Bring the thumb across the grip, using its own metacarpal direction.
            var thumb=Bone("Bone.017");var dest=origin+normal*.045f+across*.02f;
            thumb.rotation=Quaternion.FromToRotation(Bone("Bone.018").position-thumb.position,dest-thumb.position)*thumb.rotation;
            var mesh=new Mesh{name="Chapter3 closed right hand"};renderer.BakeMesh(mesh);
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                // BakeMesh includes the imported 100x renderer scale. Apply only its pose here.
                var p=renderer.transform.position+renderer.transform.rotation*vertices[i]-origin;
                vertices[i]=new Vector3(Vector3.Dot(p,-normal),Vector3.Dot(p,across),Vector3.Dot(p,forward))*.32f;
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/Closed grip hand final.asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous)
            {
                previous.Clear();previous.indexFormat=mesh.indexFormat;previous.vertices=mesh.vertices;previous.triangles=mesh.triangles;previous.uv=mesh.uv;
                previous.RecalculateNormals();previous.RecalculateBounds();previous.UploadMeshData(false);EditorUtility.SetDirty(previous);Object.DestroyImmediate(mesh);mesh=previous;
            }
            else AssetDatabase.CreateAsset(mesh,path);
            foreach(var hand in new[]{w.rightHand,w.leftHand})
            {
                for(int i=hand.childCount-1;i>=0;i--)Object.DestroyImmediate(hand.GetChild(i).gameObject);
                var g=new GameObject("原玩家手模型 · 握持姿勢",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(hand,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/人/手/HandSkin.mat");
            }
            // The bolt and its handle travel together during the loading action.
            var boltGroup=w.rifle.Find("操作槍機");if(!boltGroup)boltGroup=Point(w.rifle,"操作槍機",Vector3.zero);
            foreach(string n in new[]{"槍機","槍機柄","槍機柄球"}){var t=w.rifle.Find(n);if(t)t.SetParent(boltGroup,true);}w.bolt=boltGroup;
            var sight=w.rifle.Find("照門");
            if(sight)
            {
                var mat=sight.GetComponent<Renderer>().sharedMaterial;Object.DestroyImmediate(sight.gameObject);
                Box("照門左緣",w.rifle,new Vector3(-.013f,.089f,-.05f),new Vector3(.007f,.026f,.026f),mat);
                Box("照門右緣",w.rifle,new Vector3(.013f,.089f,-.05f),new Vector3(.007f,.026f,.026f),mat);
                Box("照門底部",w.rifle,new Vector3(0,.078f,-.05f),new Vector3(.033f,.006f,.026f),mat);
            }
            foreach(var r in f.combatRegion.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Rock_01(Clone)"))r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/Station/Foundation stone.mat");
            Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);AssetDatabase.SaveAssets();
        }
        finally{if(go)Object.DestroyImmediate(go);}
    }
}
