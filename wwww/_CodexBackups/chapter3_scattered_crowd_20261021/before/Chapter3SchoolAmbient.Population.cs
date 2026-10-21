using System.Collections.Generic;
using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    readonly List<GameObject> populationObjects=new List<GameObject>();
    readonly List<Mesh> clothingMeshes=new List<Mesh>();
    readonly List<Material> clothingMaterials=new List<Material>();
    Transform[] authoredSpectators;
    bool[] authoredActive;

    Transform PreparePopulation()
    {
        authoredSpectators=spectators;authoredActive=new bool[spectators.Length];
        Transform woman=null,child=null,native=null;
        for(int i=0;i<spectators.Length;i++)if(spectators[i])
        {
            var t=spectators[i];authoredActive[i]=t.gameObject.activeSelf;
            if(!woman&&t.name.Contains("婦人"))woman=t;
            if(!child&&t.name.Contains("學童"))child=t;
            if(!native&&t.name.Contains("觀賽族人"))native=t;
        }
        if(!woman||!child||!native)return native;
        // Reuse existing face/body geometry with plain school clothing. These
        // are anonymous dramatic roles, not identified historical people.
        var schoolChild=ClothedTemplate(child,"日籍學童服裝",new Color(.78f,.81f,.78f),new Color(.12f,.17f,.23f));
        var teacher=ClothedTemplate(native,"日籍男教師服裝",new Color(.32f,.34f,.31f),new Color(.14f,.17f,.19f));
        var people=new List<Transform>();int adultNumber=0,childNumber=0;
        // Two of these pupils replace the running actors at the alarm, at
        // their exact current positions. Never render both copies together.
        for(int i=0;i<20;i++)
        {
            bool pupil=i<10?i%2==1:i>=15;
            int number=pupil?++childNumber:++adultNumber;
            Transform source=pupil?schoolChild:number%2==1?teacher:woman;
            string role=pupil?"日籍學童":number%2==1?"日籍男教師":"日籍女教師與家屬";
            var person=Chapter3SchoolCrowdActor.CreateFrom(source,transform,role+" "+number);
            person.transform.position=AttackSpot(i)+Vector3.forward*1.35f;
            person.transform.rotation=Quaternion.LookRotation(new Vector3(-person.transform.position.x,0,0));
            populationObjects.Add(person.gameObject);people.Add(person.transform);
            if(i>=18)person.gameObject.SetActive(false);
        }
        foreach(var t in authoredSpectators)if(t)t.gameObject.SetActive(false);
        spectators=people.ToArray();return native;
    }
    void SwitchRunningPupils()
    {
        for(int i=0;i<runners.Length&&18+i<spectators.Length;i++)if(runners[i])
        {
            var target=spectators[18+i];target.SetPositionAndRotation(runners[i].transform.position,runners[i].transform.rotation);
            target.gameObject.SetActive(true);runners[i].gameObject.SetActive(false);
        }
    }
    Transform ClothedTemplate(Transform source,string label,Color shirt,Color trousers)
    {
        var root=new GameObject(label).transform;root.SetParent(transform,false);root.localScale=source.localScale;
        populationObjects.Add(root.gameObject);
        Bounds body=default;bool first=true;
        foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh)
        {if(first){body=filter.sharedMesh.bounds;first=false;}else body.Encapsulate(filter.sharedMesh.bounds);}
        foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh)continue;
            var part=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;
            part.SetParent(root,false);part.localPosition=source.InverseTransformPoint(filter.transform.position);
            part.localRotation=Quaternion.Inverse(source.rotation)*filter.transform.rotation;part.localScale=filter.transform.localScale;
            var original=filter.sharedMesh;
            if(!original.isReadable){part.GetComponent<MeshFilter>().sharedMesh=original;part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;continue;}
            var mesh=Instantiate(original);mesh.name=label+" shared clothing";clothingMeshes.Add(mesh);
            var materials=new List<Material>(renderer.sharedMaterials);
            var upper=new Material(materials[0]);upper.name=label+"上衣";
            var lower=new Material(materials[0]);lower.name=label+"褲裝";
            foreach(var mat in new[]{upper,lower})
            {
                if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",null);
                if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",null);
                if(mat.HasProperty("_BumpMap"))mat.SetTexture("_BumpMap",null);
                mat.DisableKeyword("_NORMALMAP");clothingMaterials.Add(mat);
            }
            upper.color=shirt;lower.color=trousers;
            int upperSlot=materials.Count;materials.Add(upper);materials.Add(lower);
            var triangles=new List<int>[materials.Count];for(int i=0;i<triangles.Length;i++)triangles[i]=new List<int>();
            var vertices=mesh.vertices;
            for(int sub=0;sub<original.subMeshCount;sub++)
            {
                var indices=original.GetTriangles(sub);
                for(int n=0;n<indices.Length;n+=3)
                {
                    Vector3 center=(vertices[indices[n]]+vertices[indices[n+1]]+vertices[indices[n+2]])/3;
                    float y=(center.y-body.min.y)/Mathf.Max(.01f,body.size.y),x=Mathf.Abs(center.x-body.center.x)/Mathf.Max(.01f,body.size.y);
                    int slot=y>.43f&&y<.77f&&x<.23f?upperSlot:y>.22f&&y<=.43f&&x<.17f?upperSlot+1:sub;
                    triangles[slot].Add(indices[n]);triangles[slot].Add(indices[n+1]);triangles[slot].Add(indices[n+2]);
                }
            }
            mesh.subMeshCount=triangles.Length;for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
        }
        var capsule=source.GetComponent<CapsuleCollider>();if(capsule)
        {var copy=root.gameObject.AddComponent<CapsuleCollider>();copy.center=capsule.center;copy.height=capsule.height;copy.radius=capsule.radius;}
        root.gameObject.SetActive(false);return root;
    }
    static void AddKnife(Chapter3SchoolCrowdActor actor,Chapter3SchoolController school)
    {
        var donor=school&&school.flow&&school.flow.combat?school.flow.combat.weapons.knife:null;
        if(!donor||!actor.RightHand)return;
        var root=new GameObject("族人手持獵刀").transform;root.SetParent(actor.transform,false);
        // Geometry only; no first-person controller, collider or copied actor.
        foreach(var filter in donor.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!VisibleKnifePart(filter.transform,donor))continue;
            var part=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;part.SetParent(root,false);
            part.localPosition=donor.InverseTransformPoint(filter.transform.position);
            part.localRotation=Quaternion.Inverse(donor.rotation)*filter.transform.rotation;
            part.localScale=filter.transform.lossyScale/donor.lossyScale.x;
            part.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
        actor.AttachPresentationKnife(root);
    }
    static bool VisibleKnifePart(Transform part,Transform root)
    {for(var t=part;t&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
    void ReleasePopulation()
    {
        foreach(var obj in populationObjects)if(obj)Destroy(obj);
        foreach(var mesh in clothingMeshes)if(mesh)Destroy(mesh);
        foreach(var mat in clothingMaterials)if(mat)Destroy(mat);
        if(authoredSpectators!=null)for(int i=0;i<authoredSpectators.Length;i++)if(authoredSpectators[i])authoredSpectators[i].gameObject.SetActive(authoredActive[i]);
    }
}
