using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Builds only the fourth region of the existing Chapter 3 scene. The saved
// school is an editable game layout, not an archaeological site reconstruction.
public static class Chapter3SchoolAuthoring
{
    const string Folder="Assets/Chapter3/School";
    static Material wood,dark,wall,roof,stone,soil,earth,grass,glass,linen,chalk,red,metal;
    static Font font;
    static int meshId;
    static Chapter3Flow flow;
    static Chapter3SchoolController school;
    static Transform root,details;
    static readonly List<Mesh> temporaryMeshes=new List<Mesh>();
    [MenuItem("Tools/Chapter 3/Build School and Sports Ground")]
    public static void Build()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play and save first.");
        flow=Object.FindFirstObjectByType<Chapter3Flow>();
        if(!flow||flow.gameObject.scene.path!="Assets/Scenes/第三章.unity")throw new InvalidOperationException("Open the existing Chapter 3 scene.");
        if(flow.schoolRegion)throw new InvalidOperationException("School already exists. Edit the saved region.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();meshId=0;Materials();
        root=new GameObject("04 霧社學校與運動會").transform;
        school=root.gameObject.AddComponent<Chapter3SchoolController>();
        school.flow=flow;school.player=flow.player;school.ui=flow.ui;
        flow.school=school;flow.schoolRegion=root.gameObject;
        details=Group("建築與器物 · 靜態幾何",root);
        Routes();Ground();Buildings();SportsGround();FencesAndProps();Forest();Cast();Lighting();
        CombineDetails();
        foreach(var text in flow.ui.GetComponentsInChildren<Text>(true))
            if(text.text.Contains("F6")&&text.text.Contains("F7"))text.text="快速跳段  F6 駐在所 · F7 短交戰 · F8 學校\n空白鍵略過預寫台詞";
        root.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);AssetDatabase.SaveAssets();
        PolishSaved();
        flow=Object.FindFirstObjectByType<Chapter3Flow>();school=flow.school;root=school.transform;
        Chapter2GameViewScaleGuard.Restore();Selection.activeGameObject=root.gameObject;
        File.WriteAllText("_CodexBackups/chapter3_batch3_20261013/build.txt","school built\nobjects="+root.GetComponentsInChildren<Transform>(true).Length+"\ncolliders="+root.GetComponentsInChildren<Collider>(true).Length+"\nrenderers="+root.GetComponentsInChildren<Renderer>(true).Length);
    }
    [MenuItem("Tools/Chapter 3/Polish Saved School Assets")]
    public static void PolishSaved()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var saved=Object.FindFirstObjectByType<Chapter3Flow>().school;
        var signs=saved.GetComponent<Chapter3SchoolSigns>();if(!signs)signs=saved.gameObject.AddComponent<Chapter3SchoolSigns>();
        signs.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF");signs.worldTextShader=Shader.Find("Chapter3/School World Text");
        if(!signs.worldTextShader)throw new Exception("Import school text shader first.");
        foreach(var text in saved.GetComponentsInChildren<TextMesh>(true))
            if(text.text=="1"||text.text=="2"||text.text=="3"||text.text=="4")text.transform.localRotation=Quaternion.Euler(90,0,0);
        // A shared vertex used for opposite-facing triangles has a zero normal.
        // Cloth is double sided, with an explicit front normal for stable light.
        foreach(string material in new[]{"Unbleached cotton","Faded vermilion cloth"})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+material+".mat");
            m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);
        }
        string[] paths=AssetDatabase.FindAssets("t:Mesh",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach(string path in paths)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(path.Contains("Combined_"))
            {
                if(path.Contains("Unbleached cotton")||path.Contains("Faded vermilion cloth"))
                {
                    var triangles=mesh.triangles;var unique=new HashSet<(int,int,int)>();var clean=new List<int>();
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        int a=triangles[i],b=triangles[i+1],c=triangles[i+2];int lo=Mathf.Min(a,Mathf.Min(b,c)),hi=Mathf.Max(a,Mathf.Max(b,c));
                        if(unique.Add((lo,a+b+c-lo-hi,hi))){clean.Add(a);clean.Add(b);clean.Add(c);}
                    }
                    mesh.triangles=clean.ToArray();mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
                }
                var normals=mesh.normals;bool changed=false;
                for(int i=0;i<normals.Length;i++)if(normals[i].sqrMagnitude<.01f){normals[i]=Vector3.back;changed=true;}
                if(changed){mesh.normals=normals;EditorUtility.SetDirty(mesh);}
            }
        }
        saved.sun.shadowBias=.25f;saved.sun.shadowNormalBias=1.1f;
        EditorSceneManager.MarkSceneDirty(saved.gameObject.scene);EditorSceneManager.SaveScene(saved.gameObject.scene);AssetDatabase.SaveAssets();
        // Binary mesh payloads retain the existing GUID/local IDs and avoid
        // inflating high-poly baked people into enormous YAML hex strings.
        foreach(string path in paths)
        {
            var copy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>(path));
            copy.name=Path.GetFileNameWithoutExtension(path);
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{copy},path,false);
            Object.DestroyImmediate(copy);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        RepairSavedMeshReferences();
        File.WriteAllText("_CodexBackups/chapter3_batch3_20261013/polish.txt","cloth normals repaired; mesh assets serialized as binary");
    }
    public static void RepairSavedMeshReferences()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        const string scenePath="Assets/Scenes/第三章.unity";
        string yaml=File.ReadAllText(scenePath);var log=new List<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string actualGuid,out long id))throw new Exception("Missing mesh "+path);
            yaml=System.Text.RegularExpressions.Regex.Replace(yaml,@"fileID: -?\d+, guid: "+guid+@", type: \d+","fileID: "+id+", guid: "+actualGuid+", type: 2");
            log.Add(path+" => "+id);
        }
        File.WriteAllText(scenePath,yaml);AssetDatabase.ImportAsset(scenePath,ImportAssetOptions.ForceSynchronousImport);
        EditorSceneManager.OpenScene(scenePath);
        File.WriteAllLines("_CodexBackups/chapter3_batch3_20261013/mesh-bindings.txt",log);
    }
    static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    static Transform Group(string name,Transform parent)
    {var g=new GameObject(name).transform;g.SetParent(parent,false);return g;}
    static void Materials()
    {
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/中文字體/STKAITI.TTF");
        string grain="Assets/Chapter2/Props/CouncilKnifeWoodGrain.png";
        wood=Mat("Weathered school cedar",new Color(.56f,.43f,.28f),grain,new Vector2(1,3));
        dark=Mat("School structural timber",new Color(.23f,.17f,.12f),grain,new Vector2(1,4));
        wall=Mat("Lime plaster",new Color(.79f,.76f,.65f));
        roof=Mat("Layered dark shingles",new Color(.22f,.25f,.24f),grain,new Vector2(4,1));
        stone=Mat("School masonry",new Color(.53f,.55f,.49f),"Assets/Chapter2/Environment/RichForest/Textures/6d259800_rock_01_diffuse.jpg",Vector2.one);
        soil=Mat("Sports ground earth",new Color(.86f,.79f,.62f),"Assets/Chapter2/Environment/RichForest/Textures/brown_mud_diff_2k.jpg",Vector2.one);
        earth=Mat("School forest floor",new Color(.68f,.68f,.57f),"Assets/Chapter2/Environment/PolyHaven/forest_floor_diff_4k.jpg",Vector2.one);
        grass=Mat("School grassy verges",new Color(.4f,.46f,.25f),"Assets/Chapter2/Environment/PolyHaven/forest_floor_diff_4k.jpg",Vector2.one);
        glass=Mat("Deep window glass",new Color(.14f,.24f,.25f));glass.SetFloat("_Smoothness",.38f);
        linen=Mat("Unbleached cotton",new Color(.85f,.81f,.68f));chalk=Mat("Lime field markings",new Color(.89f,.86f,.71f));
        red=Mat("Faded vermilion cloth",new Color(.63f,.15f,.11f));metal=Mat("Weathered iron",new Color(.24f,.26f,.25f));
    }
    static Material Mat(string name,Color color,string texture=null,Vector2 tiling=default)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);
        if(texture!=null){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture>(texture));m.SetTextureScale("_BaseMap",tiling);}
        EditorUtility.SetDirty(m);return m;
    }
    static Transform Box(string name,Transform parent,Vector3 p,Vector3 size,Material material,bool solid=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static Transform Cylinder(string name,Transform parent,Vector3 p,float radius,float height,Material material,bool solid=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=new Vector3(radius*2,height/2,radius*2);g.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static void Beam(string name,Transform parent,Vector3 a,Vector3 b,float width,Material mat,bool solid=true)
    {var t=Box(name,parent,(a+b)/2,new Vector3(width,(b-a).magnitude,width),mat,solid);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static Transform MeshObject(string name,Transform parent,Vector3[] vertices,int[] tris,Vector2[] uv,Material mat,bool solid=false)
    {
        var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.vertices=vertices;m.triangles=tris;m.uv=uv;m.RecalculateNormals();m.RecalculateBounds();temporaryMeshes.Add(m);
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=m;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(solid){AssetDatabase.CreateAsset(m,Folder+"/"+(meshId++)+"_"+name+".asset");temporaryMeshes.Remove(m);g.AddComponent<MeshCollider>().sharedMesh=m;}
        return g.transform;
    }
    static void Patch(string name,Transform parent,float x,float z,float width,float length,float y,Material mat,bool solid=false)
    {
        Vector3[] v={new Vector3(x-width/2,y,z-length/2),new Vector3(x+width/2,y,z-length/2),new Vector3(x-width/2,y,z+length/2),new Vector3(x+width/2,y,z+length/2)};
        MeshObject(name,parent,v,new[]{0,2,1,1,2,3},v.Select(p=>new Vector2(p.x/3,p.z/3)).ToArray(),mat,solid);
    }
    static void Ribbon(string name,Vector3[] points,float width)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();float distance=0;
        for(int i=0;i<points.Length;i++)
        {
            var direction=i==points.Length-1?points[i]-points[i-1]:points[i+1]-points[i];direction.y=0;
            var side=Vector3.Cross(Vector3.up,direction.normalized)*width/2;
            if(i>0)distance+=Vector3.Distance(points[i],points[i-1]);
            v.Add(points[i]-side+Vector3.up*.018f);v.Add(points[i]+side+Vector3.up*.018f);
            uv.Add(new Vector2(0,distance/3));uv.Add(new Vector2(width/3,distance/3));
            if(i>0){int n=i*2;tr.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
        }
        MeshObject(name,details,v.ToArray(),tr.ToArray(),uv.ToArray(),soil);
    }
    static void Routes()
    {
        school.breakthroughRoute=new[]{P(0,-18),P(-7,-8),P(-23,-4),P(-27,8),P(-27,30),P(-27,54),P(-25,66),P(-12,74),P(0,78)};
        school.coverRoute=new[]{P(0,-18),P(8,-8),P(25,-4),P(29,9),P(29,23),P(32,38),P(32,58),P(25,68),P(12,74),P(0,78)};
        school.rescueRoute=new[]{P(0,78),P(0,85),P(-3,92),P(0,98),P(8,103),P(13,107)};
        var markers=Group("劇情空間錨點 · 第四至六批接續",root);
        var labels=new[]{"校門與達多集合點","運動場","西側外廊通道","東側石牆掩護路","校後匯合空地","林間轉折處","林間接應空地"};
        var positions=new[]{P(0,-5),P(0,22),P(-27,38),P(29,30),P(0,78),P(-3,92),P(13,107)};
        school.landmarks=new Transform[labels.Length];
        for(int i=0;i<labels.Length;i++){var t=Group(labels[i],markers);t.localPosition=positions[i];school.landmarks[i]=t;}
    }
    static void Ground()
    {
        var terrain=Group("山地校園地面",details);const int n=85;var v=new Vector3[n*n];var uv=new Vector2[v.Length];var tris=new List<int>();
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            float xx=-126+x*3,zz=-70+z*3;
            float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(46,105,Mathf.Abs(xx)))*24+Mathf.SmoothStep(0,1,Mathf.InverseLerp(119,175,zz))*22;
            int i=z*n+x;v[i]=new Vector3(xx,-.025f+rise*(.8f+Mathf.PerlinNoise(xx*.021f,zz*.023f)*.8f),zz);uv[i]=new Vector2(xx/3,zz/3);
            if(x<n-1&&z<n-1)tris.AddRange(new[]{i,i+n,i+1,i+1,i+n,i+n+1});
        }
        MeshObject("School mountain terrain",terrain,v,tris.ToArray(),uv,earth,true);
        Patch("夯土運動場",terrain,0,23,43,44,.008f,soil);
        Ribbon("校門前道路",new[]{P(0,-24),P(0,-12),P(0,0),P(0,8)},6);
        Ribbon("西側可行走路線",school.breakthroughRoute,4.4f);
        Ribbon("東側可行走路線",school.coverRoute,4.2f);
        Ribbon("林間接應道路",school.rescueRoute,4.3f);
        Patch("校後匯合踏地",terrain,0,77,22,12,.022f,soil);
        Patch("林間接應踏地",terrain,13,107,13,10,.022f,soil);
        foreach(int side in new[]{-1,1})Patch("校舍草地",terrain,side*20,65,8,7,.008f,grass);
    }
    static void Buildings()
    {
        SchoolBlock("正面木造校舍",new Vector3(0,0,53),42,11,0);
        SchoolBlock("西側教室",new Vector3(-36,0,36),23,9,-90);
        SchoolBlock("東側倉房",new Vector3(39,0,45),12,7,90);
    }
    static void SchoolBlock(string name,Vector3 pos,float width,float depth,float angle)
    {
        var b=Group(name,details);b.localPosition=pos;b.localRotation=Quaternion.Euler(0,angle,0);
        float front=-depth/2;
        Box("石砌校舍基座",b,new Vector3(0,.13f,0),new Vector3(width+.5f,.26f,depth+.5f),stone);
        Box("封閉校舍牆體",b,new Vector3(0,1.93f,0),new Vector3(width,3.35f,depth),wall);
        Box("外廊木地板",b,new Vector3(0,.22f,front-1.15f),new Vector3(width+.3f,.16f,2.35f),wood);
        Box("外廊矮石階",b,new Vector3(0,.065f,front-2.45f),new Vector3(width+.6f,.13f,.45f),stone);
        // Dark board seams and a lower timber wall give scale at walking height.
        for(int i=0;i<Mathf.RoundToInt(width/.42f);i++)
        {
            float x=-width/2+.21f+i*.42f;
            Box("腰牆板",b,new Vector3(x,.86f,front-.045f),new Vector3(.402f,1.08f,.10f),wood,false);
            Box("外廊板縫",b,new Vector3(x,.308f,front-1.16f),new Vector3(.012f,.005f,2.28f),dark,false);
        }
        int bays=Mathf.FloorToInt(width/3.5f);
        for(int i=0;i<=bays;i++)
        {
            float x=-width/2+i*width/bays;
            Box("外廊承重柱",b,new Vector3(x,1.96f,front-2.13f),new Vector3(.19f,3.43f,.19f),dark);
            Beam("柱斜撐",b,new Vector3(x,3.1f,front-2.13f),new Vector3(x+.64f,3.62f,front-2.13f),.12f,dark,false);
            if(i<bays)
            {
                float center=x+width/bays/2;
                if(i==bays/2||i==1)Door(b,center,front);else Window(b,center,front);
            }
        }
        Box("外廊橫梁",b,new Vector3(0,3.68f,front-2.13f),new Vector3(width+.7f,.20f,.24f),dark);
        float half=depth/2+1.9f,peak=5.42f,eave=3.62f;
        foreach(int side in new[]{-1,1})
        {
            float run=half;float length=Mathf.Sqrt(run*run+(peak-eave)*(peak-eave));
            var panel=Box("疊木瓦屋面",b,new Vector3(0,(peak+eave)/2,side*half/2),new Vector3(width+1.6f,.22f,length),roof);
            panel.localRotation=Quaternion.Euler(side*Mathf.Atan2(peak-eave,half)*Mathf.Rad2Deg,0,0);
            for(int row=0;row<20;row++)
            {
                float z=side*half*row/19f,y=peak-(peak-eave)*row/19f+.14f;
                Box("瓦面搭接",b,new Vector3(0,y,z),new Vector3(width+1.64f,.045f,.04f),dark,false);
            }
        }
        Box("屋脊覆板",b,new Vector3(0,peak+.15f,0),new Vector3(width+1.8f,.20f,.22f),dark,false);
        foreach(int side in new[]{-1,1})
        {
            float x=side*(width/2+.04f);
            var v=new[]{new Vector3(x,3.58f,-depth/2),new Vector3(x,3.58f,depth/2),new Vector3(x,5.2f,0)};
            MeshObject("山牆木封板",b,v,side<0?new[]{0,2,1}:new[]{0,1,2},new[]{Vector2.zero,Vector2.right,Vector2.one},wood);
            Beam("山牆簷板",b,new Vector3(x,3.62f,-half),new Vector3(x,peak,0),.17f,dark,false);
            Beam("山牆簷板",b,new Vector3(x,peak,0),new Vector3(x,3.62f,half),.17f,dark,false);
        }
        // Alternating stone joints read as a low foundation without projecting
        // invisible collision beyond the visible building.
        for(int i=0;i<width;i++)Box("基座石縫",b,new Vector3(-width/2+i+.5f,.13f,front-.265f),new Vector3(.018f,.22f,.006f),dark,false);
        if(width>30)
        {
            Box("校名木匾",b,new Vector3(0,3.13f,front-2.30f),new Vector3(3.6f,.65f,.11f),wood);
            Sign("霧 社 公 學 校",b,new Vector3(0,3.13f,front-2.366f),.22f,new Color(.11f,.12f,.10f));
        }
    }
    static void Window(Transform b,float x,float z)
    {
        Box("玻璃暗窗",b,new Vector3(x,2.20f,z-.06f),new Vector3(2.35f,1.53f,.08f),glass,false);
        foreach(float dx in new[]{-1.22f,-.41f,.41f,1.22f})Box("窗直框",b,new Vector3(x+dx,2.20f,z-.13f),new Vector3(.065f,1.69f,.10f),dark,false);
        foreach(float y in new[]{1.35f,2.2f,3.05f})Box("窗橫框",b,new Vector3(x,y,z-.13f),new Vector3(2.51f,.07f,.10f),dark,false);
        Box("木窗台",b,new Vector3(x,1.30f,z-.16f),new Vector3(2.64f,.10f,.24f),wood,false);
    }
    static void Door(Transform b,float x,float z)
    {
        Box("關閉的雙扇木門",b,new Vector3(x,1.59f,z-.07f),new Vector3(1.86f,2.60f,.10f),wood,false);
        foreach(float dx in new[]{-.98f,0,.98f})Box("門框",b,new Vector3(x+dx,1.62f,z-.15f),new Vector3(.07f,2.69f,.12f),dark,false);
        Box("門楣",b,new Vector3(x,2.99f,z-.15f),new Vector3(2.09f,.12f,.13f),dark,false);
        foreach(float dx in new[]{-.43f,.43f})
        {
            Box("門上方玻璃",b,new Vector3(x+dx,2.37f,z-.138f),new Vector3(.66f,.74f,.012f),glass,false);
            Cylinder("門把",b,new Vector3(x+dx*.30f,1.46f,z-.18f),.025f,.16f,metal,false);
        }
    }
    static void Sign(string value,Transform parent,Vector3 pos,float size,Color color,float angle=0)
    {
        var t=new GameObject("牌字 · "+value,typeof(TextMesh)).GetComponent<TextMesh>();t.transform.SetParent(parent,false);t.transform.localPosition=pos;t.transform.localRotation=Quaternion.Euler(0,angle,0);
        t.text=value;t.font=font;t.fontSize=72;t.characterSize=size*.33f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;t.GetComponent<Renderer>().sharedMaterial=font.material;
    }
    static void SportsGround()
    {
        var g=Group("運動會場 · 跑道與集合區",details);
        for(int i=0;i<5;i++)Box("石灰跑道",g,new Vector3(-7.2f+i*1.2f,.033f,23),new Vector3(.075f,.012f,29),chalk,false);
        foreach(float z in new[]{8.5f,37.5f})Box("起終點線",g,new Vector3(-4.8f,.035f,z),new Vector3(5,.014f,.11f),chalk,false);
        for(int i=0;i<4;i++)Sign((i+1).ToString(),g,new Vector3(-6.6f+i*1.2f,.046f,8),.22f,Color.white);
        // Hurdles stand on the east practice area, clear of the running lanes.
        for(int i=0;i<3;i++)
        {
            var h=Group("木製練習欄架 "+i,g);h.localPosition=new Vector3(7,0,12+i*5);
            foreach(float x in new[]{-.8f,.8f}){Box("欄架腳",h,new Vector3(x,.32f,0),new Vector3(.08f,.64f,.09f),wood);Box("欄架底座",h,new Vector3(x,.04f,0),new Vector3(.12f,.08f,.7f),wood);}
            Box("欄架橫桿",h,new Vector3(0,.65f,0),new Vector3(1.8f,.10f,.08f),linen);
        }
        // Officials' shade at the north-east, spectators on both sidelines.
        var pavilion=Group("來賓席遮棚",g);pavilion.localPosition=P(13,39);
        foreach(float x in new[]{-4f,4})foreach(float z in new[]{-2f,2})Cylinder("遮棚木柱",pavilion,new Vector3(x,1.75f,z),.08f,3.5f,dark);
        foreach(int side in new[]{-1,1})
        {var canopy=Box("帆布遮棚",pavilion,new Vector3(0,3.65f,side*1.12f),new Vector3(9,.045f,2.8f),linen);canopy.localRotation=Quaternion.Euler(side*12,0,0);}
        Table(pavilion,new Vector3(0,0,-.5f),5);
        for(int i=0;i<4;i++)Chair(pavilion,new Vector3(-2.1f+i*1.4f,0,1.1f));
        for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)Bench(g,new Vector3(side*17,0,10+i*6),side*90);
        Cylinder("旗杆",g,new Vector3(18,4.2f,4),.075f,8.4f,wood);
        Box("旗杆石座",g,new Vector3(18,.17f,4),new Vector3(.9f,.34f,.9f),stone);
        Flag(g,new Vector3(18,7.1f,4),1.4f,.85f,true);
        for(int row=0;row<2;row++)
        {
            float z=4+row*37;
            foreach(float x in new[]{-20f,20f})Cylinder("裝飾繩木杆",g,new Vector3(x,2.65f,z),.055f,5.3f,wood);
            for(int i=0;i<30;i++)
            {
                float x=-20+i*40/29f,y=5.25f-Mathf.Sin(i*Mathf.PI/29)*.65f;
                if(i<29){float nx=-20+(i+1)*40/29f,ny=5.25f-Mathf.Sin((i+1)*Mathf.PI/29)*.65f;Beam("懸掛棉繩",g,new Vector3(x,y,z),new Vector3(nx,ny,z),.017f,dark,false);}
                if(i%2==0)Flag(g,new Vector3(x,y-.48f,z),.52f,.42f,i%4==0);
            }
        }
        var board=Group("運動會告示牌",g);board.localPosition=P(8,-1);
        foreach(float x in new[]{-.9f,.9f})Box("告示木腳",board,new Vector3(x,1,0),new Vector3(.10f,2,.10f),dark);
        Box("告示板",board,new Vector3(0,1.56f,0),new Vector3(2.2f,1.08f,.12f),wood);
        Box("告示紙",board,new Vector3(0,1.58f,-.071f),new Vector3(1.88f,.83f,.014f),linen,false);
        Sign("運 動 會\n徒競走　體操　接力",board,new Vector3(0,1.6f,-.082f),.135f,new Color(.17f,.15f,.11f));
    }
    static void Flag(Transform parent,Vector3 p,float w,float h,bool disc)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int i=0;i<6;i++){float x=w*i/5f,z=Mathf.Sin(i*.85f)*w*.085f;v.Add(p+new Vector3(x,0,z));v.Add(p+new Vector3(x,h,z));uv.Add(new Vector2(i/5f,0));uv.Add(new Vector2(i/5f,1));if(i>0){int n=i*2;tr.AddRange(new[]{n-2,n-1,n,n-1,n+1,n,n,n-1,n-2,n,n+1,n-1});}}
        MeshObject("懸掛布旗",parent,v.ToArray(),tr.ToArray(),uv.ToArray(),linen);
        if(disc)
        {
            var center=p+new Vector3(w*.5f,h*.52f,-.022f);var points=new List<Vector3>{center};
            for(int i=0;i<=24;i++){float a=i*Mathf.PI/12;points.Add(center+new Vector3(Mathf.Cos(a)*h*.24f,Mathf.Sin(a)*h*.24f,0));}
            var triangles=new List<int>();for(int i=1;i<=24;i++)triangles.AddRange(new[]{0,i+1,i,0,i,i+1});
            MeshObject("布旗圓紋",parent,points.ToArray(),triangles.ToArray(),points.Select(q=>Vector2.zero).ToArray(),red);
        }
    }
    static void Table(Transform parent,Vector3 pos,float width=1.8f)
    {
        var g=Group("木桌",parent);g.localPosition=pos;Box("桌面",g,new Vector3(0,.82f,0),new Vector3(width,.12f,.85f),wood);
        foreach(float x in new[]{-width/2+.13f,width/2-.13f})foreach(float z in new[]{-.31f,.31f})Box("桌腳",g,new Vector3(x,.40f,z),new Vector3(.10f,.8f,.10f),dark);
        Box("桌上棉布",g,new Vector3(0,.887f,0),new Vector3(width*.65f,.012f,.78f),linen,false);
    }
    static void Bench(Transform parent,Vector3 pos,float angle)
    {
        var g=Group("木長凳",parent);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,angle,0);
        Box("長凳坐板",g,new Vector3(0,.47f,0),new Vector3(2.5f,.11f,.42f),wood);
        foreach(float x in new[]{-.94f,.94f})Box("長凳腳",g,new Vector3(x,.22f,0),new Vector3(.13f,.44f,.35f),dark);
    }
    static void Chair(Transform parent,Vector3 pos)
    {
        var g=Group("來賓木椅",parent);g.localPosition=pos;Box("椅面",g,new Vector3(0,.44f,0),new Vector3(.52f,.08f,.52f),wood);
        foreach(float x in new[]{-.21f,.21f})foreach(float z in new[]{-.21f,.21f})Box("椅腳",g,new Vector3(x,.22f,z),new Vector3(.055f,.44f,.055f),dark);
        Box("椅背",g,new Vector3(0,.81f,.23f),new Vector3(.52f,.62f,.06f),wood);
    }
    static void Fence(Transform parent,Vector3 a,Vector3 b,string name)
    {
        var g=Group(name,parent);int spans=Mathf.CeilToInt(Vector3.Distance(a,b)/2.4f);
        for(int i=0;i<=spans;i++)
        {
            Vector3 p=Vector3.Lerp(a,b,i/(float)spans);
            Box("實體柵欄柱",g,p+Vector3.up*.65f,new Vector3(.17f,1.3f,.17f),wood);
            if(i<spans)
            {
                var next=Vector3.Lerp(a,b,(i+1f)/spans);
                foreach(float y in new[]{.46f,.94f})Beam("實體柵欄橫木",g,p+Vector3.up*y,next+Vector3.up*y,.12f,dark);
            }
        }
    }
    static void FencesAndProps()
    {
        var g=Group("圍籬與校園生活器物",details);
        Fence(g,P(-22,0),P(-3.6f,0),"校門左木柵");Fence(g,P(3.6f,0),P(22,0),"校門右木柵");
        foreach(int side in new[]{-1,1})
        {
            Fence(g,P(side*22,0),P(side*22,21),"運動場側柵前段");Fence(g,P(side*22,28),P(side*22,44),"運動場側柵後段");
            Fence(g,P(side*43,-23),P(side*43,113),"校園外圍實體木柵");
        }
        Fence(g,P(-43,-23),P(43,-23),"來路邊界木柵");Fence(g,P(-43,113),P(43,113),"林間邊界木柵");
        foreach(float x in new[]{-3.6f,3.6f})
        {
            Box("校門石柱",g,new Vector3(x,1.12f,0),new Vector3(.65f,2.24f,.65f),stone);
            Box("石柱壓頂",g,new Vector3(x,2.29f,0),new Vector3(.85f,.15f,.85f),stone);
        }
        Box("校門木匾",g,new Vector3(0,2.54f,0),new Vector3(7.75f,.4f,.17f),dark);
        Sign("霧 社 公 學 校",g,new Vector3(0,2.55f,-.105f),.22f,new Color(.86f,.82f,.67f));
        // East side low masonry provides cover, with generous gaps to change sides.
        foreach(var span in new[]{new Vector2(5,18),new Vector2(30,40)})
        {
            Box("東側石牆",g,new Vector3(25,.60f,(span.x+span.y)/2),new Vector3(.72f,1.20f,span.y-span.x),stone);
            for(float z=span.x+.65f;z<span.y;z+=1.3f)Box("石牆灰縫",g,new Vector3(25.366f,.6f,z),new Vector3(.006f,1.15f,.018f),dark,false);
            Box("石牆壓頂",g,new Vector3(25,1.24f,(span.x+span.y)/2),new Vector3(.86f,.1f,span.y-span.x+.08f),stone);
        }
        foreach(Vector3 p in new[]{P(34,10),P(35,12),P(27,47),P(36,60),P(-31,57),P(-34,59)})Crate(g,p);
        Crate(g,new Vector3(34,.9f,10));
        foreach(Vector3 p in new[]{P(-15,-3),P(-14,-3),P(34,53),P(36,54)})Barrel(g,p);
        Bench(g,P(-9,72),0);Bench(g,P(9,72),0);
        var basin=Group("校舍洗手石槽",g);basin.localPosition=P(-17,44);
        Box("水槽基座",basin,new Vector3(0,.35f,0),new Vector3(3.5f,.7f,.95f),stone);
        Box("水槽水面",basin,new Vector3(0,.715f,0),new Vector3(3.12f,.01f,.65f),glass,false);
        foreach(float x in new[]{-1.7f,1.7f})Box("水槽邊",basin,new Vector3(x,.79f,0),new Vector3(.15f,.19f,.95f),stone);
        foreach(float z in new[]{-.43f,.43f})Box("水槽沿",basin,new Vector3(0,.79f,z),new Vector3(3.5f,.19f,.12f),stone);
        var well=Group("校後取水井",g);well.localPosition=P(14,80);
        for(int i=0;i<16;i++)
        {float a=i*Mathf.PI/8;var block=Box("井口環石",well,new Vector3(Mathf.Sin(a)*.76f,.38f,Mathf.Cos(a)*.76f),new Vector3(.31f,.76f,.36f),stone);block.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}
        Cylinder("井口木蓋",well,new Vector3(0,.77f,0),.83f,.08f,wood);
        foreach(float x in new[]{-1.05f,1.05f})Box("水井木架",well,new Vector3(x,1.15f,0),new Vector3(.13f,2.3f,.13f),dark);
        Beam("水井橫木",well,new Vector3(-1.2f,2.25f,0),new Vector3(1.2f,2.25f,0),.15f,dark);
        var shelter=Group("林間接應棚與擔架",g);shelter.localPosition=P(13,108);
        foreach(float x in new[]{-3.6f,3.6f})foreach(float z in new[]{-1.5f,2.8f})Cylinder("接應棚木柱",shelter,new Vector3(x,1.4f,z),.08f,2.8f,dark);
        var cover=Box("接應棚斜布",shelter,new Vector3(0,2.8f,.65f),new Vector3(8,.05f,4.8f),linen);cover.localRotation=Quaternion.Euler(-8,0,0);
        Crate(shelter,new Vector3(-2.8f,0,1.8f));Barrel(shelter,new Vector3(2.6f,0,1.9f));
        var stretcher=Group("待用擔架",shelter);stretcher.localPosition=new Vector3(0,.33f,1.3f);
        foreach(float x in new[]{-.38f,.38f})Beam("擔架木杆",stretcher,new Vector3(x,0,-1.4f),new Vector3(x,0,1.4f),.06f,wood);
        Box("擔架棉布",stretcher,Vector3.zero,new Vector3(.7f,.05f,1.95f),linen);
        foreach(float x in new[]{-.37f,.37f})foreach(float z in new[]{-.82f,.82f})Box("擔架矮腳",stretcher,new Vector3(x,-.17f,z),new Vector3(.06f,.33f,.06f),dark);
        RoadSign(g,P(-19,-6),"校舍外廊 ←",25);RoadSign(g,P(20,-6),"倉房 →",-20);
        RoadSign(g,P(4,80),"山徑 →",0);RoadSign(g,P(7,101),"接應處 →",0);
    }
    static void Crate(Transform parent,Vector3 p)
    {
        var g=Group("物資木箱",parent);g.localPosition=p;
        Box("木箱實體",g,new Vector3(0,.44f,0),new Vector3(1.1f,.88f,.78f),wood);
        foreach(float x in new[]{-.39f,.39f})Box("木箱束條",g,new Vector3(x,.44f,0),new Vector3(.085f,.90f,.80f),dark,false);
        for(int i=0;i<5;i++)Box("木箱板縫",g,new Vector3(0,.1f+i*.17f,-.397f),new Vector3(1.08f,.015f,.008f),dark,false);
    }
    static void Barrel(Transform parent,Vector3 p)
    {
        var g=Group("木桶",parent);g.localPosition=p;
        Cylinder("木桶實體",g,new Vector3(0,.48f,0),.36f,.96f,wood);
        foreach(float y in new[]{.12f,.76f})Cylinder("木桶鐵箍",g,new Vector3(0,y,0),.368f,.045f,metal,false);
    }
    static void RoadSign(Transform parent,Vector3 p,string label,float angle)
    {
        var g=Group("方向木牌 "+label,parent);g.localPosition=p;g.localRotation=Quaternion.Euler(0,angle,0);
        Box("路牌柱",g,new Vector3(0,.87f,0),new Vector3(.12f,1.74f,.12f),dark);
        Box("路牌木板",g,new Vector3(0,1.52f,0),new Vector3(1.85f,.43f,.1f),wood);
        Sign(label,g,new Vector3(0,1.52f,-.062f),.15f,new Color(.16f,.13f,.10f));
    }
    static float DistanceToRoutes(Vector3 p)
    {
        float best=1000;
        foreach(var path in new[]{school.breakthroughRoute,school.coverRoute,school.rescueRoute})
            for(int i=1;i<path.Length;i++){var d=path[i]-path[i-1];float t=Mathf.Clamp01(Vector3.Dot(p-path[i-1],d)/d.sqrMagnitude);best=Mathf.Min(best,Vector3.Distance(p,path[i-1]+d*t));}
        return best;
    }
    static void Forest()
    {
        var forest=Group("校園樹林 · 樹幹有碰撞",root);var rocks=Group("校園岩石 · 網格碰撞",root);var under=Group("蕨草與地表植被",root);
        var trees=flow.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("駐在所林木 ")).Take(12).ToArray();
        var shrubs=flow.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("路側蕨草 ")).Take(10).ToArray();
        var rock=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rock_pack/prefab/Rock_03.prefab");
        var random=new System.Random(331013);
        for(int i=0;i<210;i++)
        {
            float x=(float)random.NextDouble()*150-75,z=(float)random.NextDouble()*174-34;Vector3 p=P(x,z);
            bool outside=Mathf.Abs(x)>46||z>119||z< -26;
            bool north=z>83&&DistanceToRoutes(p)>5&&Vector3.Distance(p,P(13,107))>10;
            if(!outside&&!north)continue;
            float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(46,105,Mathf.Abs(x)))*24+Mathf.SmoothStep(0,1,Mathf.InverseLerp(119,175,z))*22;
            p.y=rise*(.8f+Mathf.PerlinNoise(x*.021f,z*.023f)*.8f);
            var wrapper=Group("林木 "+i,forest);wrapper.position=p;
            var t=Object.Instantiate(trees[i%trees.Length].gameObject,wrapper).transform;t.gameObject.SetActive(true);t.Rotate(0,(float)random.NextDouble()*360,0,Space.World);
            foreach(var c in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            GroundObject(t,p);var capsule=wrapper.gameObject.AddComponent<CapsuleCollider>();capsule.height=5;capsule.radius=.31f;capsule.center=Vector3.up*2.5f;
        }
        var fixedTrees=new[]{P(-17,-12),P(17,-13),P(-13,69),P(22,83),P(-15,86),P(10,91)};
        for(int i=0;i<fixedTrees.Length;i++)
        {
            var wrapper=Group("場邊遮蔭樹 "+i,forest);wrapper.position=fixedTrees[i];
            var t=Object.Instantiate(trees[i%trees.Length].gameObject,wrapper).transform;t.gameObject.SetActive(true);
            foreach(var c in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);GroundObject(t,fixedTrees[i]);
            var c2=wrapper.gameObject.AddComponent<CapsuleCollider>();c2.height=5;c2.radius=.36f;c2.center=Vector3.up*2.5f;
        }
        for(int i=0;i<52;i++)
        {
            Vector3 p=i<4?new[]{P(-9,-12),P(13,-5),P(-8,92),P(5,95)}[i]:P((i%2==0?-1:1)*(35+(float)random.NextDouble()*6),-15+(float)random.NextDouble()*122);
            if(DistanceToRoutes(p)<3.1f||p.z>24&&p.z<61)continue;
            var t=Object.Instantiate(rock,rocks).transform;t.name="可碰撞岩石 "+i;t.gameObject.SetActive(true);t.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
            var b=BoundsOf(t);t.localScale*= (i<4?2.6f:1.2f+(float)random.NextDouble()*1.9f)/Mathf.Max(b.size.x,b.size.z);
            GroundObject(t,p);foreach(var c in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var m in t.GetComponentsInChildren<MeshFilter>(true))if(m.sharedMesh)m.gameObject.AddComponent<MeshCollider>().sharedMesh=m.sharedMesh;
            foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=stone;
        }
        for(int i=0;i<440&&shrubs.Length>0;i++)
        {
            Vector3 p=P((float)random.NextDouble()*84-42,-21+(float)random.NextDouble()*132);
            bool allowed=(Mathf.Abs(p.x)>23&&p.z<23)||(p.z>82)||(p.z< -5)||(Mathf.Abs(p.x)>40);
            if(!allowed||DistanceToRoutes(p)<2.8f||Vector3.Distance(p,P(13,107))<8)continue;
            var t=Object.Instantiate(shrubs[i%shrubs.Length].gameObject,under).transform;t.name="路旁蕨草 "+i;t.gameObject.SetActive(true);t.Rotate(0,(float)random.NextDouble()*360,0,Space.World);GroundObject(t,p);
            // Soft grass is non-solid; trunks, rocks and all placed furniture are solid.
            foreach(var c in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        }
    }
    static Bounds BoundsOf(Transform t)
    {var renderers=t.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
    static void GroundObject(Transform t,Vector3 p)
    {var b=BoundsOf(t);t.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);}
    static Chapter2Actor Actor(Chapter2Actor source,Transform parent,Vector3 position)
    {
        var a=Object.Instantiate(source.gameObject,parent).GetComponent<Chapter2Actor>();a.gameObject.SetActive(true);
        foreach(var rifle in a.GetComponentsInChildren<Chapter2Rifle>(true))if(rifle.weapon)Object.DestroyImmediate(rifle.weapon.gameObject);
        foreach(var c in a.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var script in a.GetComponentsInChildren<MonoBehaviour>(true))if(!(script is Chapter2Actor)&&!(script is Chapter2SpeechFace))Object.DestroyImmediate(script);
        foreach(var t in a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="賽德克佩刀").ToArray())Object.DestroyImmediate(t.gameObject);
        a.name=a.DisplayName;a.transform.position=position;a.seated=a.fallen=a.speaking=false;a.facing=Vector3.back;
        AddPersonCollider(a.gameObject,a.isChild?1.2f:1.75f);return a;
    }
    static void AddPersonCollider(GameObject go,float height)
    {
        var s=go.transform.lossyScale;var c=go.AddComponent<CapsuleCollider>();c.height=height/Mathf.Abs(s.y);c.radius=.27f/Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.z));c.center=new Vector3(0,height*.5f/s.y,0);
    }
    static void Cast()
    {
        var cast=Group("達多與兩位同伴",root);
        school.bawan=Actor(flow.station.bawan,cast,P(-1.4f,-19.7f));school.pihu=Actor(flow.station.pihu,cast,P(1.4f,-20.8f));
        var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/第二章.unity");
        var templates=new List<GameObject>();
        try
        {
            var chapter=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Controller>(true)).First();
            school.tado=Actor(chapter.tado,cast,P(-4.7f,-5));school.tado.name="達多·莫那 · 校門集合";
            var child=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter2Actor>(true)).First(a=>a.isChild);
            var crowdRoot=Group("運動會人群 · 非戰鬥角色",root);var ambient=crowdRoot.gameObject.AddComponent<Chapter3SchoolAmbient>();
            var sourceWoman=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/日本女性/日本女1/日本女1.fbx"));
            try
            {
                templates.Add(BakeSpectator(chapter.officer.gameObject,"警察來賓",true,1.72f));
                templates.Add(BakeSpectator(sourceWoman,"觀賽婦人",false,1.56f));
                templates.Add(BakeSpectator(child.gameObject,"參加運動會的學童",false,1.16f));
                templates.Add(BakeSpectator(chapter.watan.gameObject,"觀賽族人",false,1.68f));
            }
            finally{Object.DestroyImmediate(sourceWoman);}
            var spectators=new List<Transform>();
            Vector3[] seats={P(12,36),P(14,36),P(16,36),P(12,42),P(14,42),P(-12,10),P(-14,12),P(-12,15),P(-14,18),P(-12,21),P(-14,24),P(-12,28),P(-13,33),P(13,11),P(15,14),P(13,17),P(14,20),P(13,25),P(16,28),P(10,29),P(-10,39),P(-7,40),P(-4,40),P(3,39)};
            for(int i=0;i<seats.Length;i++)
            {
                int kind=i<3?0:i%4;var person=Object.Instantiate(templates[kind],crowdRoot).transform;person.name=templates[kind].name+" "+(i+1);person.gameObject.SetActive(true);person.localPosition=seats[i];
                person.rotation=Quaternion.LookRotation((P(-3,23)-seats[i]).normalized);
                person.localScale*=.96f+(i%5)*.02f;AddPersonCollider(person.gameObject,kind==2?1.16f:1.70f);spectators.Add(person);
            }
            ambient.spectators=spectators.ToArray();
            ambient.runners=new[]{Actor(child,crowdRoot,P(-6,10)),Actor(child,crowdRoot,P(-4,34))};
            for(int i=0;i<ambient.runners.Length;i++)ambient.runners[i].name="跑道上的學童 "+(i+1);
            ambient.runningTrack=new[]{P(-6,10),P(-6,35),P(-3.6f,35),P(-3.6f,10)};
        }
        finally
        {foreach(var t in templates)if(t)Object.DestroyImmediate(t);EditorSceneManager.ClosePreviewScene(preview);}
    }
    static GameObject BakeSpectator(GameObject source,string name,bool police,float height)
    {
        var temp=Object.Instantiate(source);temp.name="Temporary crowd source";temp.SetActive(true);temp.transform.position=Vector3.zero;
        try
        {
            foreach(var rifle in temp.GetComponentsInChildren<Chapter2Rifle>(true))if(rifle.weapon)Object.DestroyImmediate(rifle.weapon.gameObject);
            foreach(var t in temp.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="賽德克佩刀").ToArray())Object.DestroyImmediate(t.gameObject);
            foreach(var script in temp.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            var rig=temp.AddComponent<Chapter1IncidentRig>();rig.Initialize(temp.GetComponentInChildren<Animator>(),police);rig.Face(Vector3.forward);rig.Ground(0);rig.speakingWeight=police?0:.20f;rig.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
            var result=new GameObject(name);result.transform.SetParent(root,false);
            foreach(var renderer in temp.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
            {
                var mesh=new Mesh{name=name+" pose"};renderer.BakeMesh(mesh,false);
                var matrix=temp.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;
                var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=matrix.MultiplyVector(normals[i]).normalized;mesh.normals=normals;mesh.RecalculateBounds();
                string path=Folder+"/Crowd_"+name+"_"+(meshId++)+".asset";
                AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
                var part=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(result.transform,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;
                part.GetComponent<Renderer>().sharedMaterials=renderer.sharedMaterials.Select(ConvertCrowdMaterial).ToArray();
            }
            if(result.GetComponentsInChildren<Renderer>().Length==0)throw new Exception("Cannot bake crowd model: "+name);
            var bounds=BoundsOf(result.transform);result.transform.localScale=Vector3.one*(height/bounds.size.y);
            // Bake local floor offset into child transforms, leaving a clean root
            // pivot for standing spectators and their world-sized capsules.
            float floor=bounds.min.y;foreach(Transform part in result.transform)part.localPosition-=Vector3.up*floor;
            result.SetActive(false);return result;
        }
        finally{Object.DestroyImmediate(temp);}
    }
    static Material ConvertCrowdMaterial(Material source)
    {
        if(source&&source.shader.name.StartsWith("Universal Render Pipeline/"))return source;
        string id=source?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)):"plain";
        string path=Folder+"/Crowd material "+id+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=Color.white;
        if(source&&source.mainTexture)material.SetTexture("_BaseMap",source.mainTexture);
        material.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Lighting()
    {
        var light=new GameObject("學校晨光").AddComponent<Light>();light.transform.SetParent(root,false);light.type=LightType.Directional;light.intensity=1.45f;light.color=new Color(1,.91f,.76f);light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(37,-32,0);school.sun=light;
        var audio=root.gameObject.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Chapter2/Audio/forest_morning.wav");audio.loop=audio.playOnAwake=true;audio.volume=.17f;
    }
    static void CombineDetails()
    {
        var combinedRoot=Group("合併靜態可見網格",details);
        var filters=details.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()).ToArray();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var instances=group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=details.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{name="School geometry "+group.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances,true,true);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Folder+"/Combined_"+(meshId++)+"_"+group.Key.name+".asset");
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(combinedRoot,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var f in filters)
        {
            var go=f.gameObject;Object.DestroyImmediate(go.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);
            if(go.transform.childCount==0&&go.GetComponents<Component>().Length==1)Object.DestroyImmediate(go);
        }
        foreach(var mesh in temporaryMeshes)if(mesh)Object.DestroyImmediate(mesh);temporaryMeshes.Clear();
    }
}
