using UnityEngine;

// Keep the existing grip and wrist mesh, extending its forearm to a shoulder
// outside the first-person frame. The cut edge cannot swing into view.
[DefaultExecutionOrder(4600)]
[RequireComponent(typeof(MeshFilter))]
public sealed class Chapter3ViewArm : MonoBehaviour
{
    public Camera view;
    public bool left;
    Mesh mesh;
    Vector3[] source,vertices;
    Vector3 end;
    float minimum;
    const float Wrist=-.115f;
    void Awake()
    {
        var filter=GetComponent<MeshFilter>();mesh=Instantiate(filter.sharedMesh);mesh.name="Chapter 3 continuous view arm";
        filter.sharedMesh=mesh;mesh.MarkDynamic();source=mesh.vertices;vertices=(Vector3[])source.Clone();
        minimum=mesh.bounds.min.z;int count=0;
        foreach(var p in source)if(p.z<minimum+.006f){end+=p;count++;}
        if(count>0)end/=count;
    }
    void LateUpdate()
    {
        if(!view||mesh==null)return;
        Vector3 shoulder=transform.InverseTransformPoint(view.transform.TransformPoint(new Vector3(left?-.95f:.95f,-.65f,-.35f)));
        Vector3 extension=shoulder-end;
        for(int i=0;i<vertices.Length;i++)
        {
            float t=Mathf.Clamp01((Wrist-source[i].z)/(Wrist-minimum));
            vertices[i]=source[i]+extension*Mathf.SmoothStep(0,1,t);
        }
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
    }
    void OnDestroy(){if(mesh)Destroy(mesh);}
}
