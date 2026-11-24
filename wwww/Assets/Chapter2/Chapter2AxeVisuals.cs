using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Visuals only: original axe root, tip, colliders and Swing remain authoritative.
[DisallowMultipleComponent]
public sealed class Chapter2AxeVisuals : MonoBehaviour
{
    [Header("斧頭外觀")]
    public bool replaceOriginalAppearance = true;
    public bool showRightHand = true;
    [Range(.6f,1.5f)] public float handSize = 1f;
    public Color woodColor = new Color(.29f,.13f,.055f);
    public Color skinColor = new Color(.60f,.40f,.27f);
    public Color sleeveColor = new Color(.16f,.12f,.085f);
    [Tooltip("選用：已有精細模型時可拖入 Prefab，取代程式產生的外觀。")]
    public GameObject customAxePrefab;
    public GameObject customRightHandPrefab;
    public Vector3 customAxePosition, customAxeEuler;
    public Vector3 customAxeScale = Vector3.one;
    public Vector3 customHandEuler;
    Transform generated;
    readonly List<Renderer> originals = new List<Renderer>();
    readonly List<bool> originalEnabled = new List<bool>();
    readonly List<Object> owned = new List<Object>();
    Material timber, steel, edge, skin, cloth, leather;
    bool built;

    void Start(){Rebuild();}
    [ContextMenu("重建斧頭與右手（播放時）")]
    public void Rebuild()
    {
        if(!Application.isPlaying)return;
        Clear();
        var axe=GetComponent<Chapter2Axe>();
        if(!axe||!axe.bladeTip)return;
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {originals.Add(renderer);originalEnabled.Add(renderer.enabled);if(replaceOriginalAppearance)renderer.enabled=false;}
        generated=new GameObject("Axe visuals and gripping hand").transform;
        generated.SetParent(transform,false);
        timber=MakeMaterial("Axe wood",woodColor,.03f,.25f);
        steel=MakeMaterial("Forged dark steel",new Color(.18f,.20f,.21f),.8f,.32f);
        edge=MakeMaterial("Worn cutting edge",new Color(.53f,.56f,.55f),.85f,.48f);
        skin=MakeMaterial("Right hand skin",skinColor,0,.22f);
        cloth=MakeMaterial("Forearm sleeve",sleeveColor,0,.12f);
        leather=MakeMaterial("Handle binding",new Color(.12f,.07f,.035f),0,.17f);
        MakeWoodGrain(timber);
        Vector3 tip=transform.InverseTransformPoint(axe.bladeTip.position);
        float hx=tip.x+.245f,hy=tip.y;
        if(replaceOriginalAppearance)
        {
            if(customAxePrefab)
            {
                var model=Instantiate(customAxePrefab,generated).transform;
                model.localPosition=customAxePosition;model.localRotation=Quaternion.Euler(customAxeEuler);model.localScale=customAxeScale;
                RemoveColliders(model.gameObject);
            }
            else
            {
                // The sharp edge passes through the original bladeTip exactly.
                Head(tip);
                TaperedHandle(hx,hy-.73f,hy+.095f);
                for(int i=0;i<5;i++)
                    Part(PrimitiveType.Cylinder,"Grip binding",new Vector3(hx,hy-.43f-i*.014f,tip.z),new Vector3(.048f,.005f,.043f),leather);
            }
        }
        if(showRightHand)
        {
            Vector3 grip=new Vector3(hx,hy-.47f,tip.z);
            if(customRightHandPrefab)
            {
                var hand=Instantiate(customRightHandPrefab,generated).transform;
                hand.localPosition=grip;hand.localRotation=Quaternion.Euler(customHandEuler);hand.localScale=Vector3.one*handSize;
                RemoveColliders(hand.gameObject);
            }
            else Hand(grip);
        }
        built=true;
    }
    void Head(Vector3 t)
    {
        // Broad forged cheek tapering to a thin cutting edge along local -X.
        Vector2[] outline={new Vector2(0,-.12f),new Vector2(.035f,-.145f),new Vector2(.24f,-.075f),new Vector2(.31f,-.065f),new Vector2(.31f,.075f),new Vector2(.24f,.085f),new Vector2(.035f,.145f),new Vector2(0,.12f)};
        var v=new List<Vector3>();
        for(int side=0;side<2;side++)foreach(var p in outline)
        {
            float half=Mathf.Lerp(.003f,.039f,Mathf.Clamp01(p.x/.10f));
            v.Add(t+new Vector3(p.x,p.y,side==0?-half:half));
        }
        var triangles=new List<int>();int n=outline.Length;
        for(int i=1;i<n-1;i++){triangles.Add(0);triangles.Add(i+1);triangles.Add(i);triangles.Add(n);triangles.Add(n+i);triangles.Add(n+i+1);}
        for(int i=0;i<n;i++){int j=(i+1)%n;triangles.AddRange(new[]{i,j,j+n,i,j+n,i+n});}
        Mesh mesh=new Mesh();mesh.name="Forged axe wedge";mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
        MeshObject("Forged axe head",mesh,steel);
        // A narrow polished bevel follows the same edge, without moving its tip.
        Vector3[] bevel={t+new Vector3(0,-.12f,-.0032f),t+new Vector3(.035f,-.145f,-.014f),t+new Vector3(.035f,.145f,-.014f),t+new Vector3(0,.12f,-.0032f),t+new Vector3(0,-.12f,.0032f),t+new Vector3(.035f,-.145f,.014f),t+new Vector3(.035f,.145f,.014f),t+new Vector3(0,.12f,.0032f)};
        var bm=new Mesh();bm.name="Polished bevel";bm.vertices=bevel;bm.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7};bm.RecalculateNormals();bm.RecalculateBounds();owned.Add(bm);MeshObject("Worn edge",bm,edge);
        Part(PrimitiveType.Cube,"Handle eye",t+new Vector3(.245f,.008f,0),new Vector3(.065f,.10f,.083f),steel);
    }
    void TaperedHandle(float x,float bottom,float top)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();const int sides=12,rings=9;
        var axe=GetComponent<Chapter2Axe>();float z=transform.InverseTransformPoint(axe.bladeTip.position).z;
        for(int j=0;j<rings;j++)
        {
            float t=(float)j/(rings-1),radius=Mathf.Lerp(.024f,.018f,t)+Mathf.Sin(t*Mathf.PI)*.003f;
            float bend=Mathf.Sin(t*Mathf.PI)*.018f;
            for(int i=0;i<=sides;i++)
            {float a=i*Mathf.PI*2/sides;v.Add(new Vector3(x+bend+Mathf.Cos(a)*radius,Mathf.Lerp(bottom,top,t),z+Mathf.Sin(a)*radius*.85f));uv.Add(new Vector2((float)i/sides,t));}
        }
        for(int j=0;j<rings-1;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        int cap=v.Count;v.Add(new Vector3(x,bottom,z));uv.Add(Vector2.zero);v.Add(new Vector3(x,top,z));uv.Add(Vector2.one);
        for(int i=0;i<sides;i++){tri.AddRange(new[]{cap,i+1,i});int a=(rings-1)*(sides+1)+i;tri.AddRange(new[]{cap+1,a,a+1});}
        var mesh=new Mesh();mesh.name="Curved wooden axe handle";mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);MeshObject("Wooden haft",mesh,timber);
    }
    void Hand(Vector3 grip)
    {
        var root=new GameObject("Procedural gripping right hand").transform;root.SetParent(generated,false);root.localPosition=grip;root.localScale=Vector3.one*handSize;
        // Closed fingers wrap a vertical handle; all points are in hand-local metres.
        HandPart(root,PrimitiveType.Sphere,"Palm",new Vector3(.035f,0,.008f),new Vector3(.057f,.098f,.042f),skin);
        for(int f=0;f<4;f++)
        {
            float y=.032f-f*.021f;
            Vector3 previous=new Vector3(.034f,y,.024f);
            float[] angles={42,94,150,205};
            for(int j=0;j<angles.Length;j++)
            {
                float a=angles[j]*Mathf.Deg2Rad;
                Vector3 next=new Vector3(Mathf.Cos(a)*.030f,y-Mathf.Max(0,j-1)*.002f,Mathf.Sin(a)*.030f);
                HandLink(root,"Finger "+f+" segment "+j,previous,next,f==3?.012f:.014f,skin);previous=next;
            }
        }
        HandLink(root,"Thumb base",new Vector3(.048f,.033f,.002f),new Vector3(.026f,.052f,-.024f),.020f,skin);
        HandLink(root,"Thumb tip",new Vector3(.026f,.052f,-.024f),new Vector3(-.005f,.040f,-.029f),.017f,skin);
        HandLink(root,"Wrist",new Vector3(.04f,-.035f,.003f),new Vector3(.075f,-.10f,-.075f),.046f,skin);
        HandLink(root,"Sleeved forearm",new Vector3(.075f,-.10f,-.075f),new Vector3(.20f,-.28f,-.32f),.072f,cloth);
        foreach(var renderer in root.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
    }
    void HandLink(Transform parent,string name,Vector3 from,Vector3 to,float diameter,Material mat)
    {
        var g=HandPart(parent,PrimitiveType.Capsule,name,(from+to)*.5f,new Vector3(diameter,Mathf.Max(diameter*.5f,(to-from).magnitude*.5f),diameter),mat);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
    }
    GameObject HandPart(Transform parent,PrimitiveType type,string name,Vector3 p,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;RemoveColliders(g);return g;
    }
    void Part(PrimitiveType type,string name,Vector3 p,Vector3 size,Material mat){HandPart(generated,type,name,p,size,mat);}
    void MeshObject(string name,Mesh mesh,Material mat)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(generated,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void RemoveColliders(GameObject root)
    {foreach(var c in root.GetComponentsInChildren<Collider>(true)){c.enabled=false;Destroy(c);}}
    Material MakeMaterial(string name,Color color,float metallic,float smooth)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
        var m=new Material(shader);m.name=name;m.color=color;if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);owned.Add(m);return m;
    }
    void MakeWoodGrain(Material mat)
    {
        var texture=new Texture2D(64,256,TextureFormat.RGB24,false);texture.name="Procedural axe wood grain";texture.wrapMode=TextureWrapMode.Repeat;
        for(int y=0;y<256;y++)for(int x=0;x<64;x++)
        {
            float n=Mathf.PerlinNoise(x*.19f+13,y*.018f+7);
            float grain=.7f+.48f*n+.10f*Mathf.Sin(x*.8f+n*9);
            texture.SetPixel(x,y,new Color(grain,grain,grain));
        }
        texture.Apply();owned.Add(texture);if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",texture);else mat.mainTexture=texture;
    }
    void OnEnable()
    {
        if(built&&generated)generated.gameObject.SetActive(true);
        if(built&&replaceOriginalAppearance)foreach(var renderer in originals)if(renderer)renderer.enabled=false;
    }
    void OnDisable()
    {
        if(generated)generated.gameObject.SetActive(false);
        for(int i=0;i<originals.Count;i++)if(originals[i])originals[i].enabled=originalEnabled[i];
    }
    void Clear()
    {
        for(int i=0;i<originals.Count;i++)if(originals[i])originals[i].enabled=originalEnabled[i];
        originals.Clear();originalEnabled.Clear();
        if(generated){generated.gameObject.SetActive(false);generated.SetParent(null,true);Destroy(generated.gameObject);generated=null;}
        foreach(var item in owned)if(item)Destroy(item);owned.Clear();built=false;
    }
    void OnDestroy(){Clear();}
}
