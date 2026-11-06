using UnityEngine;
using UnityEngine.UI;

// Matches Chapter 1's serialized delivery arrow: a shaft and two angled wings.
// World markers own their one material; screen markers reuse the same geometry.
public sealed class Chapter3YellowArrow : MonoBehaviour
{
    public static readonly Color Color=new Color(1f,.82f,.02f,1f);
    public const float BaseSize=1.05f,Height=.08f,Spacing=2.2f;
    public const float PulseAmount=.1f,PulseSpeed=4.5f,PhaseStep=.42f,Emission=2.4f;
    public const float StartDistance=1.2f,EndDistance=1.1f;
    public static float ForwardExtent => (.31f+.375f*Mathf.Sqrt(.5f))*BaseSize*(1+PulseAmount);
    public static float RearExtent => .54f*BaseSize*(1+PulseAmount);
    public int PhaseIndex {get;private set;}
    Material material;

    static readonly Vector3[] positions={new Vector3(0,0,-.16f),new Vector3(-.15f,0,.31f),new Vector3(.15f,0,.31f)};
    static readonly Vector3[] sizes={new Vector3(.22f,.045f,.76f),new Vector3(.2f,.05f,.55f),new Vector3(.2f,.05f,.55f)};
    static readonly float[] angles={0,45,-45};
    static readonly string[] partNames={"Shaft","HeadLeft","HeadRight"};

    public static float PulseScale(float unscaledTime,int phaseIndex=0)
        =>BaseSize*(1+Mathf.Sin(unscaledTime*PulseSpeed-phaseIndex*PhaseStep)*PulseAmount);

    // Forward is local +Z. Set position/rotation on the returned root; its
    // scale is animated here with Chapter 1's unscaled-time pulse.
    public static GameObject Create(Transform parent,string name,int phaseIndex=0)
    {
        var root=new GameObject(name);root.transform.SetParent(parent,false);
        var arrow=root.AddComponent<Chapter3YellowArrow>();arrow.PhaseIndex=phaseIndex;
        for(int i=0;i<positions.Length;i++)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=partNames[i];
            part.transform.SetParent(root.transform,false);part.transform.localPosition=positions[i];
            part.transform.localRotation=Quaternion.Euler(0,angles[i],0);part.transform.localScale=sizes[i];
            var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var renderer=part.GetComponent<Renderer>();
            if(!arrow.material)
            {
                // Use the same primitive default material as Chapter 1 rather
                // than introducing a build-only Shader.Find dependency.
                arrow.material=new Material(renderer.sharedMaterial);arrow.material.name="第一章樣式黃色指引";
                if(arrow.material.HasProperty("_BaseColor"))arrow.material.SetColor("_BaseColor",Color);
                if(arrow.material.HasProperty("_Color"))arrow.material.SetColor("_Color",Color);
                if(arrow.material.HasProperty("_EmissionColor"))
                {arrow.material.EnableKeyword("_EMISSION");arrow.material.SetColor("_EmissionColor",Color*Emission);}
            }
            renderer.sharedMaterial=arrow.material;
        }
        root.transform.localScale=Vector3.one*PulseScale(Time.unscaledTime,phaseIndex);return root;
    }
    void LateUpdate(){transform.localScale=Vector3.one*PulseScale(Time.unscaledTime,PhaseIndex);}
    void OnDestroy(){if(material)Destroy(material);}

    // Top-down projection of the same cubes onto UI, pointing local -Y.
    public static void PopulateUi(VertexHelper vertices,float pixelsPerUnit)
    {
        vertices.Clear();
        for(int i=0;i<positions.Length;i++)
        {
            int first=vertices.currentVertCount;
            var rotation=Quaternion.Euler(0,angles[i],0);
            for(int corner=0;corner<4;corner++)
            {
                float x=(corner==0||corner==3?-.5f:.5f)*sizes[i].x;
                float z=(corner<2?-.5f:.5f)*sizes[i].z;
                Vector3 point=positions[i]+rotation*new Vector3(x,0,z);
                vertices.AddVert(new Vector3(point.x,-point.z,0)*pixelsPerUnit,Color,Vector2.zero);
            }
            vertices.AddTriangle(first,first+1,first+2);vertices.AddTriangle(first,first+2,first+3);
        }
    }
}
