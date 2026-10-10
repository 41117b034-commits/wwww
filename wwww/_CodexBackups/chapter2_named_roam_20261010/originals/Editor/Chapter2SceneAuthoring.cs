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
using UnityEngine.Video;

public static class Chapter2SceneAuthoring
{
    const string Root="Assets/Chapter2/";
    static Dictionary<Material,Material> materials;
    static Scene target;
    [MenuItem("Tools/Chapter 2/Build Forest Chapter")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building.");
        if(SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before building Chapter 2.");
        foreach(string folder in new[]{"Materials","Geometry","Audio","Media"})Directory.CreateDirectory(Root+folder);
        AssetDatabase.Refresh();materials=new Dictionary<Material,Material>();
        string scenePath="Assets/Scenes/第二章.unity";
        Directory.CreateDirectory("_CodexBackups/chapter2_20261004");
        string backup="_CodexBackups/chapter2_20261004/第二章.before.unity";
        if(!File.Exists(backup))File.Copy(scenePath,backup);
        var source=EditorSceneManager.OpenScene("Assets/Scenes/第一章新版警察.unity",OpenSceneMode.Additive);
        GameObject[] originals=source.GetRootGameObjects();
        target=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(target);
        var controller=new GameObject("Chapter2_NewRulesAndSecretCouncil").AddComponent<Chapter2Controller>();
        Material floor=Mat("Forest floor",Color.white,AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Environment/PolyHaven/forest_floor_diff_1k.jpg"));
        floor.mainTextureScale=Vector2.one*45;
        Normal(floor,Root+"Environment/PolyHaven/forest_floor_nor_gl_1k.jpg");
        Ground(floor);
        var forest=new GameObject("Forest · sacred grove").transform;
        UnityEngine.Random.InitState(19301007);
        for(int i=0;i<110;i++)
        {
            float angle=i*2.39996f, radius=18+Mathf.Sqrt(i/110f)*45;
            Vector3 p=new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius+5);
            float height=UnityEngine.Random.Range(16f,27f);
            var tree=Model("Assets/Hipernt/Pine Pack/Prefabs/Pine"+(1+i%6)+".prefab","Forest pine "+i,forest,p,height);
            tree.transform.Rotate(0,UnityEngine.Random.Range(0,360),0);
            var collider=tree.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.5f;collider.height=1;collider.radius=.025f;
        }
        for(int i=0;i<20;i++)
        {
            float angle=i*2.39996f;Vector3 p=new Vector3(Mathf.Sin(angle)*UnityEngine.Random.Range(15,31),0,Mathf.Cos(angle)*UnityEngine.Random.Range(16,31));
            Model("Assets/樹/綠樹.prefab","Broadleaf "+i,forest,p,UnityEngine.Random.Range(12f,19f));
        }
        var rocks=new GameObject("Mossy rocks and forest floor").transform;
        for(int i=0;i<35;i++)
        {
            float a=i*2.39996f,r=UnityEngine.Random.Range(9f,30f);
            var rock=Model("Assets/Rock_pack/prefab/Rock_0"+(1+i%4)+".prefab","Rock "+i,rocks,new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r),UnityEngine.Random.Range(.25f,1.1f));
            rock.transform.Rotate(0,UnityEngine.Random.Range(0,360),0);
        }
        Material bark=ConvertMaterial(AssetDatabase.LoadAssetAtPath<Material>("Assets/GreenForest/Nature/Modular Trees/Materials/TreeBark.mat"));
        var hero=new GameObject("Sacred giant tree · 西仔希克").transform;hero.position=new Vector3(0,0,12);controller.sacredTree=hero;
        Trunk(hero,bark);
        for(int i=0;i<7;i++)
        {
            float a=i*Mathf.PI*2/7;
            var root=Primitive("Buttress root",PrimitiveType.Cylinder,hero,new Vector3(Mathf.Sin(a)*1.2f,.23f,Mathf.Cos(a)*1.2f),new Vector3(.5f,1.3f,.5f),bark);
            root.transform.rotation=Quaternion.Euler(Mathf.Cos(a)*73,0,Mathf.Sin(a)*73);
        }
        var crown=Model("Assets/Hipernt/Pine Pack/Prefabs/Pine2.prefab","Giant tree canopy",hero,hero.position+new Vector3(0,9,0),23);
        crown.transform.localScale=Vector3.Scale(crown.transform.localScale,new Vector3(1.5f,1,1.5f));
        // Reuse foliage only: the source tree trunk would otherwise float above our authored trunk.
        var crownFilter=crown.GetComponentInChildren<MeshFilter>();var crownRenderer=crownFilter.GetComponent<Renderer>();
        int foliage=Array.FindIndex(crownRenderer.sharedMaterials,m=>m.name.StartsWith("Branch",StringComparison.Ordinal));
        if(foliage>=0){var mesh=UnityEngine.Object.Instantiate(crownFilter.sharedMesh);int[] triangles=mesh.GetTriangles(foliage);mesh.subMeshCount=1;mesh.SetTriangles(triangles,0);mesh.name="GiantCanopy";crownFilter.sharedMesh=SaveMesh(mesh,Root+"Geometry/GiantCanopy.asset");crownRenderer.sharedMaterials=new[]{crownRenderer.sharedMaterials[foliage]};}
        var treeCollision=hero.gameObject.AddComponent<CapsuleCollider>();treeCollision.center=new Vector3(0,10,0);treeCollision.height=20;treeCollision.radius=1.5f;
        Material stumpMat=Mat("Poly Haven stump",Color.white,AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Environment/PolyHaven/tree_stump_01_diff_1k.jpg"));Normal(stumpMat,Root+"Environment/PolyHaven/tree_stump_01_nor_gl_1k.jpg");
        for(int i=0;i<7;i++)
        {
            var stump=Model(Root+"Environment/PolyHaven/tree_stump_01_1k.fbx","Weathered stump "+i,rocks,new Vector3(i%2==0?-8-i:8+i,0,9-i*3),.5f);
            foreach(var renderer in stump.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=stumpMat;
        }
        var felled=Model(Root+"Environment/PolyHaven/tree_stump_01_1k.fbx","Felled sacred stump",null,hero.position,.9f);
        foreach(var renderer in felled.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=stumpMat;
        felled.SetActive(false);controller.fallenStump=felled;
        var marker=Model("Assets/Rock_pack/prefab/Rock_01.prefab","Sacred ground marker",null,new Vector3(-3.8f,0,9),1.35f);
        controller.treeApproach=new GameObject("Giant tree interaction point").transform;controller.treeApproach.position=new Vector3(0,0,7.4f);
        // Small logging props explain the imposed work without importing modern buildings.
        for(int i=0;i<4;i++) {var log=Primitive("Carried timber "+i,PrimitiveType.Cube,rocks,new Vector3(5.5f+i*.28f,.22f,6),new Vector3(.16f,.16f,3.6f),bark);log.transform.Rotate(0,8,0);}
        var player=new GameObject("Chapter2_Player");player.transform.position=new Vector3(0,.08f,-17);
        var motor=player.AddComponent<CharacterController>();motor.height=1.72f;motor.radius=.25f;motor.center=new Vector3(0,.9f,0);motor.stepOffset=.3f;
        var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";cameraObject.transform.SetParent(player.transform,false);cameraObject.transform.localPosition=Vector3.up*1.65f;
        var camera=cameraObject.GetComponent<Camera>();camera.nearClipPlane=.06f;camera.farClipPlane=180;camera.fieldOfView=64;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.52f,.59f,.54f);
        controller.player=player.AddComponent<Chapter2Player>();controller.player.view=camera;
        controller.ui=Chapter2Presentation.Build(camera,AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF"));
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var daylight=new GameObject("Early morning sun").AddComponent<Light>();daylight.type=LightType.Directional;daylight.color=new Color(1,.89f,.68f);daylight.intensity=1.35f;daylight.shadows=LightShadows.Soft;daylight.transform.rotation=Quaternion.Euler(29,-32,0);controller.sun=daylight;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.49f,.56f,.51f);RenderSettings.ambientIntensity=1;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogDensity=.012f;RenderSettings.skybox=null;
        controller.dayGroup=new GameObject("Morning · forced logging");
        string[] donors={"賽德克中年(2)","部落男性","部落女姓2"};
        controller.workers=new Chapter2Actor[3];
        for(int i=0;i<3;i++)controller.workers[i]=Actor(originals,donors[i],"族人 "+(i+1),controller.dayGroup.transform,new Vector3(-1.5f-i*1.3f,0,-12+i*.8f),false);
        controller.officer=Actor(originals,"警察","督工警察",controller.dayGroup.transform,new Vector3(3,0,7),true);controller.officer.facing=Vector3.left;
        // A visible rifle conveys the scripted threat; no first-chapter wedding systems are copied.
        var rifle=Model("Assets/物品/38式步槍/38式步槍.fbx","督工步槍",controller.officer.transform,controller.officer.transform.position+new Vector3(.28f,1.0f,.12f),.95f);
        rifle.transform.rotation=Quaternion.Euler(80,0,0);
        controller.nightGroup=new GameObject("Night · secret council");
        controller.campfire=new GameObject("Council campfire").transform;controller.campfire.SetParent(controller.nightGroup.transform,false);
        var emberMaterial=Mat("Embers",new Color(.35f,.045f,.005f));emberMaterial.SetColor("_EmissionColor",new Color(2.5f,.16f,.005f));emberMaterial.EnableKeyword("_EMISSION");
        Primitive("Ember bed",PrimitiveType.Sphere,controller.campfire,new Vector3(0,.055f,0),new Vector3(.85f,.10f,.75f),emberMaterial);
        var charredWood=Mat("Charred firewood",new Color(.09f,.035f,.012f));
        for(int i=0;i<5;i++){float a=i*36;var log=Primitive("Firewood",PrimitiveType.Cylinder,controller.campfire,new Vector3(0,.16f+i*.013f,0),new Vector3(.14f,.55f,.14f),charredWood);log.transform.localRotation=Quaternion.Euler(90,a,0);}
        for(int i=0;i<11;i++){float a=i*Mathf.PI*2/11;var stone=Model("Assets/Rock_pack/prefab/Rock_0"+(1+i%4)+".prefab","Fire ring stone",controller.campfire,new Vector3(Mathf.Sin(a)*.65f,0,Mathf.Cos(a)*.65f),.16f+(i%3)*.025f);stone.transform.Rotate(0,i*47,0);}
        var fire=new GameObject("Firelight").AddComponent<Light>();fire.transform.SetParent(controller.nightGroup.transform);fire.transform.position=new Vector3(0,1,0);fire.type=LightType.Point;fire.range=15;fire.intensity=4;fire.color=new Color(1,.5f,.19f);fire.shadows=LightShadows.Soft;controller.fireLight=fire;
        Fire(controller.nightGroup.transform);
        controller.mona=Actor(originals,"賽德克中年(2)","莫那魯道 · 暫用既有人物",controller.nightGroup.transform,new Vector3(0,0,3.1f),false);controller.mona.seated=true;
        controller.leaders=new Chapter2Actor[6];
        for(int i=0;i<6;i++)
        {
            float a=new float[]{35,70,105,255,290,325}[i]*Mathf.Deg2Rad;
            controller.leaders[i]=Actor(originals,donors[i%3],"六社領袖 "+(i+1),controller.nightGroup.transform,new Vector3(Mathf.Sin(a)*3.3f,0,Mathf.Cos(a)*3.3f),false);controller.leaders[i].seated=true;controller.leaders[i].facing=-controller.leaders[i].transform.position;
            Primitive("Council seat",PrimitiveType.Cylinder,controller.nightGroup.transform,controller.leaders[i].transform.position+Vector3.up*.22f,new Vector3(.6f,.22f,.6f),bark);
        }
        Primitive("Mona seat",PrimitiveType.Cylinder,controller.nightGroup.transform,new Vector3(0,.22f,3.1f),new Vector3(.65f,.22f,.65f),bark);
        controller.conservatives=new Chapter2Actor[2];
        for(int i=0;i<2;i++) {controller.conservatives[i]=Actor(originals,donors[i],"保守派族人 "+i,controller.nightGroup.transform,new Vector3(-4.6f-i*.85f,0,-1.4f),false);controller.conservatives[i].facing=Vector3.right;}
        controller.meetingSpawn=new GameObject("Council player position").transform;controller.meetingSpawn.position=new Vector3(0,.05f,-4.8f);
        controller.nightGroup.SetActive(false);
        controller.axe=new GameObject("Player logging axe");controller.axe.transform.SetParent(camera.transform,false);controller.axe.transform.localPosition=new Vector3(.48f,-.46f,.75f);
        Primitive("Axe handle",PrimitiveType.Cylinder,controller.axe.transform,Vector3.zero,new Vector3(.035f,.36f,.035f),bark);
        Primitive("Axe blade",PrimitiveType.Cube,controller.axe.transform,new Vector3(-.1f,.26f,0),new Vector3(.24f,.16f,.035f),Mat("Worn iron",new Color(.25f,.28f,.28f)));
        controller.axe.SetActive(false);
        controller.ambience=Audio(controller.transform,"Forest ambience",.35f);controller.effects=Audio(controller.transform,"Story effects",.7f);
        controller.fireAudio=Audio(controller.campfire,"Fire crackle",.25f);controller.fireAudio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/音樂/營火.mp3");controller.fireAudio.loop=true;controller.fireAudio.spatialBlend=.7f;
        controller.forestAudio=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/forest_morning.wav");controller.nightAudio=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/forest_night.wav");controller.chopAudio=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/axe_impact.wav");controller.threatAudio=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/rifle_report.wav");
        controller.openingFilm=AssetDatabase.LoadAssetAtPath<VideoClip>(Root+"Media/Chapter2_Opening.mp4");
        Chapter2InteractionAuthoring.Apply(controller);
        foreach(var lod in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))if(lod.gameObject.scene==target)lod.RecalculateBounds();
        foreach(var root in target.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        EditorSceneManager.CloseScene(source,true);
        for(int i=SceneManager.sceneCount-1;i>=0;i--){var s=SceneManager.GetSceneAt(i);if(s!=target)EditorSceneManager.CloseScene(s,true);}
        EditorSceneManager.SaveScene(target,scenePath);
        Selection.activeGameObject=controller.gameObject;SceneView.lastActiveSceneView?.LookAt(new Vector3(0,4,5),Quaternion.Euler(16,0,0),24);
        AssetDatabase.SaveAssets();File.WriteAllText("_CodexBackups/chapter2_20261004/build.txt","Built "+DateTime.Now.ToString("s"));
    }
    static Chapter2Actor Actor(GameObject[] originals,string donor,string name,Transform parent,Vector3 position,bool police)
    {
        GameObject original=originals.FirstOrDefault(x=>x.name==donor);
        if(!original)throw new InvalidOperationException("Missing original actor: "+donor);
        var actor=UnityEngine.Object.Instantiate(original);actor.name=name;actor.SetActive(true);SceneManager.MoveGameObjectToScene(actor,target);
        foreach(var behavior in actor.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behavior);
        foreach(var c in actor.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        actor.transform.SetParent(parent,false);actor.transform.rotation=Quaternion.identity;actor.transform.position=position;
        Bounds b=BoundsOf(actor);actor.transform.localScale*=1.72f/b.size.y;b=BoundsOf(actor);actor.transform.position+=Vector3.up*(position.y-b.min.y);
        var a=actor.AddComponent<Chapter2Actor>();a.police=police;a.facing=police?Vector3.left:Vector3.forward;
        // Persist a relaxed authoring pose so offline film renders never contain T-poses.
        var animator=actor.GetComponentInChildren<Animator>();
        var leftArm=Bone(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        var rightArm=Bone(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        var leftHand=Bone(animator,HumanBodyBones.LeftHand,"L_Hand","LeftHand");
        var rightHand=Bone(animator,HumanBodyBones.RightHand,"R_Hand","RightHand");
        if(leftArm&&rightArm&&leftHand&&rightHand)
        {
            Vector3 forward=police?-actor.transform.right:Vector3.Cross(rightArm.position-leftArm.position,Vector3.up).normalized;
            leftArm.rotation=Quaternion.FromToRotation(leftHand.position-leftArm.position,Vector3.down+forward*.13f)*leftArm.rotation;
            rightArm.rotation=Quaternion.FromToRotation(rightHand.position-rightArm.position,Vector3.down+forward*.13f)*rightArm.rotation;
            actor.transform.rotation=Quaternion.FromToRotation(forward,a.facing)*actor.transform.rotation;
        }
        // Chapter 2 drives bones itself. Do not retain the donor's unused animation graph
        // or its old state-machine behaviours when copying the character.
        if(animator){animator.runtimeAnimatorController=null;animator.enabled=false;}
        foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
        return a;
    }
    static GameObject Model(string path,string name,Transform parent,Vector3 position,float height)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new InvalidOperationException("Missing model "+path);
        var g=UnityEngine.Object.Instantiate(asset);g.name=name;SceneManager.MoveGameObjectToScene(g,target);g.transform.SetParent(parent,true);g.transform.position=position;g.transform.rotation=Quaternion.identity;
        foreach(var behaviour in g.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
        foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var b=BoundsOf(g);if(b.size.y>.001f)g.transform.localScale*=height/b.size.y;b=BoundsOf(g);g.transform.position+=Vector3.up*(position.y-b.min.y);
        foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(ConvertMaterial).ToArray();return g;
    }
    static Transform Bone(Animator animator,HumanBodyBones bone,params string[] aliases)
    {
        if(!animator)return null;
        if(animator.isHuman)return animator.GetBoneTransform(bone);
        return animator.GetComponentsInChildren<Transform>().FirstOrDefault(t=>aliases.Any(a=>t.name.EndsWith(a,StringComparison.OrdinalIgnoreCase)));
    }
    static Bounds BoundsOf(GameObject g)
    {var renderers=g.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
    static Material Mat(string name,Color color,Texture texture=null)
    {
        string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.mainTexture=texture;m.SetFloat("_Smoothness",.12f);return m;
    }
    static Material ConvertMaterial(Material source)
    {
        if(!source)return Mat("Fallback bark",new Color(.26f,.24f,.18f));
        if(materials.TryGetValue(source,out Material m))return m;
        string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
        string name=source.name.Replace('/','_')+"_"+(id.Length>=8?id.Substring(0,8):materials.Count.ToString());
        Texture texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture;
        m=Mat(name,Color.white,texture);m.SetFloat("_Cull",0);m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.35f);materials[source]=m;return m;
    }
    static void Normal(Material m,string path)
    {var importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.55f);}
    static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 local,Vector3 scale,Material mat)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static AudioSource Audio(Transform parent,string name,float volume)
    {var g=new GameObject(name);g.transform.SetParent(parent,false);var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.volume=volume;return a;}
    static void Ground(Material mat)
    {
        const int n=121;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var triangles=new int[(n-1)*(n-1)*6];int ti=0;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            float px=(x/(float)(n-1)-.5f)*160,pz=(z/(float)(n-1)-.5f)*160;
            float edge=Mathf.Clamp01((new Vector2(px,pz).magnitude-23)/28);float h=edge*edge*(3+Mathf.PerlinNoise(px*.03f+4,pz*.03f+4)*12);
            vertices[z*n+x]=new Vector3(px,h,pz);uv[z*n+x]=new Vector2(x/(float)n,z/(float)n);
            if(x<n-1&&z<n-1){int a=z*n+x;triangles[ti++]=a;triangles[ti++]=a+n;triangles[ti++]=a+1;triangles[ti++]=a+1;triangles[ti++]=a+n;triangles[ti++]=a+n+1;}
        }
        var mesh=new Mesh{name="ForestGround"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,Root+"Geometry/ForestGround.asset");
        var go=new GameObject("Forest ground",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.GetComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Trunk(Transform parent,Material bark)
    {
        const int sides=32,rings=17;var v=new Vector3[(sides+1)*rings];var uv=new Vector2[v.Length];var t=new List<int>();
        for(int y=0;y<rings;y++)for(int x=0;x<=sides;x++)
        {float h=y*22f/(rings-1),a=x*2*Mathf.PI/sides;float radius=Mathf.Lerp(1.5f,.35f,y/(float)(rings-1))*(1+.09f*Mathf.Sin(a*7+y*.7f));if(y==0)radius*=1.45f;int i=y*(sides+1)+x;v[i]=new Vector3(Mathf.Cos(a)*radius,h,Mathf.Sin(a)*radius);uv[i]=new Vector2(x*3f/sides,h/4);if(y<rings-1&&x<sides){int next=i+sides+1;t.AddRange(new[]{i,next,i+1,i+1,next,next+1});}}
        var mesh=new Mesh{name="GiantTrunk"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,Root+"Geometry/GiantTrunk.asset");
        var g=new GameObject("Ancient trunk",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=bark;
    }
    static Mesh SaveMesh(Mesh mesh,string path)
    {var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
    static void Fire(Transform parent)
    {
        var g=new GameObject("Council flame");g.transform.SetParent(parent,false);g.transform.localPosition=Vector3.up*.4f;
        var p=g.AddComponent<ParticleSystem>();var main=p.main;main.startLifetime=1.1f;main.startSpeed=1f;main.startSize=.35f;main.startColor=new Color(1,.55f,.12f,.9f);main.maxParticles=90;
        var emission=p.emission;emission.rateOverTime=65;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.24f;shape.angle=12;shape.rotation=new Vector3(-90,0,0);
        var col=p.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.6f,.12f),0),new GradientColorKey(new Color(.9f,.15f,.02f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});col.color=gradient;
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="FlameSoft"};
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}texture.Apply();
        string path=Root+"Materials/FlameSoft.asset";var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing){EditorUtility.CopySerialized(texture,existing);UnityEngine.Object.DestroyImmediate(texture);texture=existing;}else AssetDatabase.CreateAsset(texture,path);
        var material=Mat("Fire particles",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");material.SetTexture("_BaseMap",texture);material.SetFloat("_Surface",1);material.SetFloat("_Blend",2);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",1);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;g.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
    }
}
