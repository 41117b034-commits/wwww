using System.Collections.Generic;
using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    readonly List<GameObject> populationObjects=new List<GameObject>();
    readonly List<Mesh> clothingMeshes=new List<Mesh>();
    readonly List<Material> clothingMaterials=new List<Material>();
    readonly Dictionary<Mesh,Mesh> clothingDetail=new Dictionary<Mesh,Mesh>();
    internal Mesh DistantClothing(Mesh source)=>source&&clothingDetail.TryGetValue(source,out var distant)?distant:null;
    Transform[] authoredSpectators;
    bool[] authoredActive;
    Transform nativeChildTemplate;
    readonly Chapter3PathMotor[] campusRunnerMotors={new Chapter3PathMotor(),new Chapter3PathMotor()};
    readonly int[] campusRunnerLeg={1,3};
    bool[] originalRunnerActive;

    Transform PreparePopulation()
    {
        authoredSpectators=spectators;authoredActive=new bool[spectators.Length];
        originalRunnerActive=new bool[runners.Length];
        for(int i=0;i<runners.Length;i++)originalRunnerActive[i]=runners[i]&&runners[i].gameObject.activeSelf;
        Transform woman=null,child=null,native=null;
        for(int i=0;i<spectators.Length;i++)if(spectators[i])
        {
            var t=spectators[i];authoredActive[i]=t.gameObject.activeSelf;
            if(!woman&&t.name.Contains("婦人"))woman=t;
            if(!child&&t.name.Contains("學童"))child=t;
            if(!native&&t.name.Contains("觀賽族人"))native=t;
        }
        if(!woman||!child||!native)return native;
        nativeChildTemplate=child; // Preserve the original Seediq tunic and texture.
        // Existing slots, new civilian clothes. The face/hand donor is only
        // geometry; its Indigenous clothes and accessories are not reused.
        var boy=CampusTemplate("CampusBoy",1.25f);
        var girl=CampusTemplate("CampusGirl",1.22f);
        var teacher=CampusTemplate("CampusTeacher",1.72f);
        var people=new List<Transform>();int adultNumber=0,childNumber=0;
        for(int i=0;i<20;i++)
        {
            bool pupil=i>=10,schoolgirl=pupil&&i%2==1,maleTeacher=!pupil&&i%3==0;
            int number=pupil?++childNumber:++adultNumber;
            Transform source=pupil?(schoolgirl?girl:boy):maleTeacher?teacher:woman;
            if(!source)throw new System.InvalidOperationException("第三章缺少已匯入的日籍師生模型");
            string role=pupil?(schoolgirl?"日籍女學童":"日籍男學童"):maleTeacher?"日籍男教師":"日籍女教師";
            var person=Chapter3SchoolCrowdActor.CreateFrom(source,transform,role+" "+number,bakedYaw:!pupil&&!maleTeacher?90:0);
            person.transform.position=GuestStart(i);
            person.transform.rotation=Quaternion.Euler(0,(i*137+25)%360,0);
            person.SetFallSide(i%2==0?1:-1);
            if(maleTeacher)AddKnife(person,GetComponentInParent<Chapter3SchoolController>(),"男教師手持刀");
            populationObjects.Add(person.gameObject);people.Add(person.transform);

        }
        foreach(var t in authoredSpectators)if(t)t.gameObject.SetActive(false);
        spectators=people.ToArray();return native;
    }
    Transform CampusTemplate(string modelName,float height)
    {
        var asset=Resources.Load<GameObject>("Chapter3/CampusCharacters/"+modelName);
        if(!asset)return null;
        var root=Instantiate(asset,transform).transform;root.name=modelName+" · template";
        root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;root.localScale=Vector3.one;
        populationObjects.Add(root.gameObject);
        var capsule=root.gameObject.AddComponent<CapsuleCollider>();
        capsule.height=height;capsule.center=Vector3.up*(height*.5f);capsule.radius=.23f;
        var skin=Resources.Load<Texture2D>("Chapter3/CampusCharacters/"+
            (modelName=="CampusTeacher"?"CampusTeacherSkin":"CampusSkin"));
        var cloth=Resources.Load<Texture2D>("Chapter3/CampusCharacters/CampusClothWeave");
        var materials=new Dictionary<Material,Material>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var slots=renderer.sharedMaterials;
            for(int i=0;i<slots.Length;i++)
            {
                var source=slots[i];if(!source)continue;
                if(!materials.TryGetValue(source,out var mat))
                {
                    mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=source.name+" · campus";
                    string n=source.name;Color color=Color.white;
                    if(n.Contains("Linen"))color=new Color(.73f,.73f,.66f);
                    else if(n.Contains("Navy"))color=new Color(.038f,.065f,.10f);
                    else if(n.Contains("Charcoal"))color=modelName=="CampusTeacher"?
                        (n.Contains("Button")?new Color(.045f,.049f,.055f):new Color(.14f,.15f,.16f)):new Color(.075f,.082f,.087f);
                    else if(n.Contains("NeckSkin"))color=new Color(.49f,.34f,.255f);
                    else if(n.Contains("DarkHair"))color=new Color(.018f,.015f,.012f);
                    else if(n.Contains("ClothShoe"))color=new Color(.035f,.031f,.027f);
                    else if(n.Contains("Seam"))color=modelName=="CampusTeacher"?new Color(.08f,.09f,.10f):new Color(.51f,.52f,.49f);
                    mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.16f);mat.SetFloat("_Metallic",0);
                    if(n.Contains("tripo_mat"))mat.SetTexture("_BaseMap",skin);
                    else if(cloth&&!n.Contains("Skin")&&!n.Contains("Hair")&&!n.Contains("Shoe")&&!n.Contains("Button"))
                    {mat.SetTexture("_BaseMap",cloth);mat.SetTextureScale("_BaseMap",new Vector2(12,12));}
                    clothingMaterials.Add(mat);materials.Add(source,mat);
                }
                slots[i]=mat;
            }
            renderer.sharedMaterials=slots;
        }
        root.gameObject.SetActive(false);return root;
    }
    void ResetCampusRunners()
    {
        for(int i=0;i<runners.Length;i++)if(runners[i])runners[i].gameObject.SetActive(false);
        for(int i=0;i<2;i++){campusRunnerLeg[i]=i==0?1:3;campusRunnerMotors[i].Reset();}
    }
    void UpdateCampusRunners()
    {
        if(Panicking||spectators.Length<20||runningTrack.Length<4)return;
        for(int i=0;i<2;i++)
        {
            var pupil=crowd[18+i];if(!pupil)continue;
            pupil.SetAnimated(true);
            Vector3 target=runningTrack[campusRunnerLeg[i]];
            if(Horizontal(pupil.transform.position,target)<.25f)
            {campusRunnerLeg[i]=(campusRunnerLeg[i]+1)%runningTrack.Length;campusRunnerMotors[i].Reset();target=runningTrack[campusRunnerLeg[i]];}
            campusRunnerMotors[i].Patrol(pupil,target,1.7f,.2f);
        }
    }
    void SwitchRunningPupils()
    {
        // These are now the same two Japanese pupil objects before and after
        // the signal. Their actual current track positions are retained.
        for(int i=0;i<runners.Length;i++)if(runners[i])runners[i].gameObject.SetActive(false);
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
            var mesh=ClotheMesh(original,label+" shared clothing",body,upperSlot);
            var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");
            if(catalog)foreach(var entry in catalog.entries)
                if(entry.source==original&&entry.distant&&entry.distant.isReadable)
                {
                    // The derivative uses the same clothing slots and colours.
                    // Keep an explicit source mapping; cloning changes identity.
                    clothingDetail.Add(mesh,ClotheMesh(entry.distant,label+" distant clothing",body,upperSlot));break;
                }
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
        }
        var capsule=source.GetComponent<CapsuleCollider>();if(capsule)
        {var copy=root.gameObject.AddComponent<CapsuleCollider>();copy.center=capsule.center;copy.height=capsule.height;copy.radius=capsule.radius;}
        root.gameObject.SetActive(false);return root;
    }
    Mesh ClotheMesh(Mesh source,string label,Bounds body,int upperSlot)
    {
        var mesh=Instantiate(source);mesh.name=label;clothingMeshes.Add(mesh);
        var triangles=new List<int>[upperSlot+2];for(int i=0;i<triangles.Length;i++)triangles[i]=new List<int>();
        var vertices=mesh.vertices;
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var indices=source.GetTriangles(sub);
            for(int n=0;n<indices.Length;n+=3)
            {
                Vector3 center=(vertices[indices[n]]+vertices[indices[n+1]]+vertices[indices[n+2]])/3;
                float y=(center.y-body.min.y)/Mathf.Max(.01f,body.size.y),x=Mathf.Abs(center.x-body.center.x)/Mathf.Max(.01f,body.size.y);
                int slot=y>.43f&&y<.77f&&x<.23f?upperSlot:y>.22f&&y<=.43f&&x<.17f?upperSlot+1:sub;
                triangles[slot].Add(indices[n]);triangles[slot].Add(indices[n+1]);triangles[slot].Add(indices[n+2]);
            }
        }
        mesh.subMeshCount=triangles.Length;for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);
        return mesh;
    }
    static void AddKnife(Chapter3SchoolCrowdActor actor,Chapter3SchoolController school,string label="族人手持獵刀")
    {
        var donor=school&&school.flow&&school.flow.combat?school.flow.combat.weapons.knife:null;
        if(!donor||!actor.RightHand)return;
        var root=new GameObject(label).transform;root.SetParent(actor.transform,false);
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
        clothingDetail.Clear();
        if(originalRunnerActive!=null)for(int i=0;i<runners.Length;i++)if(runners[i])runners[i].gameObject.SetActive(originalRunnerActive[i]);
        if(authoredSpectators!=null)for(int i=0;i<authoredSpectators.Length;i++)if(authoredSpectators[i])authoredSpectators[i].gameObject.SetActive(authoredActive[i]);
    }
}
