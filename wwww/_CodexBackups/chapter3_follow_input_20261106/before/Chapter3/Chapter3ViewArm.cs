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
    Vector3[] source,vertices,sourceNormals,normals;
    Vector3 end;
    Vector3 previousExtension;
    float[] weights;
    float[] gradients;
    int[] movingVertices;
    bool posed;
    float minimum;
    const float Wrist=-.115f;
    void Awake()
    {
        var filter=GetComponent<MeshFilter>();mesh=Instantiate(filter.sharedMesh);mesh.name="Chapter 3 continuous view arm";
        filter.sharedMesh=mesh;mesh.MarkDynamic();source=mesh.vertices;vertices=(Vector3[])source.Clone();
        minimum=mesh.bounds.min.z;int count=0;
        foreach(var p in source)if(p.z<minimum+.006f){end+=p;count++;}
        if(count>0)end/=count;
        sourceNormals=mesh.normals;normals=(Vector3[])sourceNormals.Clone();
        weights=new float[source.Length];gradients=new float[source.Length];
        var moving=new System.Collections.Generic.List<int>();
        for(int i=0;i<source.Length;i++)
        {
            float t=Mathf.Clamp01((Wrist-source[i].z)/(Wrist-minimum));
            weights[i]=Mathf.SmoothStep(0,1,t);gradients[i]=-6*t*(1-t)/(Wrist-minimum);
            if(weights[i]>0)moving.Add(i);
        }
        movingVertices=moving.ToArray();
    }
    void LateUpdate()
    {
        if(!view||mesh==null)return;
        Vector3 shoulder=transform.InverseTransformPoint(view.transform.TransformPoint(new Vector3(left?-.95f:.95f,-.65f,-.35f)));
        Vector3 extension=shoulder-end;
        // Camera and grip move together while walking/looking. Rebuild only
        // when recoil, aiming, reload or a slash actually changes their pose.
        if(posed&&(extension-previousExtension).sqrMagnitude<.000001f)return;
        posed=true;previousExtension=extension;
        foreach(int i in movingVertices)
        {
            vertices[i]=source[i]+extension*weights[i];
            // Inverse-transpose of p'=p+extension*w(z). This avoids traversing
            // every triangle to rebuild the same smooth forearm normals.
            if(i<normals.Length)
            {
                Vector3 normal=sourceNormals[i];float denominator=1+extension.z*gradients[i];
                if(Mathf.Abs(denominator)>.0001f)normal.z-=gradients[i]*Vector3.Dot(extension,normal)/denominator;
                normals[i]=normal.normalized;
            }
        }
        mesh.vertices=vertices;if(normals.Length==vertices.Length)mesh.normals=normals;mesh.RecalculateBounds();
    }
    void OnDestroy(){if(mesh)Destroy(mesh);}
}
