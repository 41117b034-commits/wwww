using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Runtime-only connecting terrain. No source mesh, scene asset or import
// setting is edited. Local rises and bends are a fictional level layout.
public sealed class Chapter3ApproachEnvironment : MonoBehaviour
{
    public const float CombatOriginZ=-75, TrailStartZ=-47;
    public Vector3[] Waypoints {get;private set;}
    public Transform Root {get;private set;}
    readonly List<Mesh> owned=new List<Mesh>();
    readonly List<GameObject> clearedPlants=new List<GameObject>();
    readonly Dictionary<Collider,bool> entranceColliders=new Dictionary<Collider,bool>();
    readonly List<(MeshFilter filter,Mesh original,Mesh open)> entranceMeshes=new List<(MeshFilter,Mesh,Mesh)>();
    MeshFilter valley;
    Mesh originalValley,cutValley;
    MeshCollider trailSurface,ridgeSurface;
    Material earth,road;
    static readonly Vector3[] roadFootprint={Vector3.zero,Vector3.left*.32f,Vector3.right*.32f,Vector3.forward*.32f,Vector3.back*.32f};
    Chapter3Flow flow;
    static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
    public void Build(Chapter3Flow owner)
    {
        flow=owner;
        Waypoints=new[]{P(0,0,TrailStartZ),P(-6,.45f,-40),P(-8,.9f,-32),P(-4,.35f,-25),P(0,0,-18)};
        Root=new GameObject("林路連接 · 關卡安排").transform;
        Root.SetParent(transform,false);
        foreach(var m in flow.combatRegion.GetComponentsInChildren<MeshFilter>(true))
        {
            if(m.name=="交戰山谷地形")valley=m;
            if(m.name=="山路地面")road=m.GetComponent<Renderer>().sharedMaterial;
        }
        earth=valley?valley.GetComponent<Renderer>().sharedMaterial:road;
        if(!road)road=earth;
        TrimValley();
        BuildFloor();BuildTrail();BuildRidge();OpenEntrance();
        Physics.SyncTransforms();PlantForest();ClearEntrancePlants();
        SetVisible(false);
    }
    void TrimValley()
    {
        if(!valley||!valley.sharedMesh||!valley.sharedMesh.isReadable)return;
        originalValley=valley.sharedMesh;
        cutValley=Instantiate(originalValley);cutValley.name="山路地形 · 連接出口裁切副本";owned.Add(cutValley);
        var vertices=originalValley.vertices;var keep=new List<int>();var triangles=originalValley.triangles;
        for(int i=0;i<vertices.Length;i++)
            vertices[i].y=Mathf.Lerp(vertices[i].y,-.07f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,52,vertices[i].z)));
        cutValley.vertices=vertices;
        for(int i=0;i<triangles.Length;i+=3)
            if(vertices[triangles[i]].z<=52&&vertices[triangles[i+1]].z<=52&&vertices[triangles[i+2]].z<=52)
            {keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}
        cutValley.triangles=keep.ToArray();cutValley.RecalculateNormals();cutValley.RecalculateBounds();
    }
    void BuildFloor()
    {
        MeshObject("林間基底",new[]{P(-160,-.08f,-190),P(160,-.08f,-190),P(-160,-.08f,-20),P(160,-.08f,-20)},
            new[]{0,2,1,1,2,3},earth,true);
    }
    void BuildTrail()
    {
        // Mitered cross sections give the bend one continuous surface, rather
        // than overlapping tilted boxes with lips at each joint.
        var v=new List<Vector3>();var tr=new List<int>();
        float[] offsets={-8,-3.4f,-2.7f,2.7f,3.4f,8};
        for(int i=0;i<Waypoints.Length;i++)
        {
            Vector3 before=Horizontal(Waypoints[i]-Waypoints[Mathf.Max(0,i-1)]).normalized;
            Vector3 after=Horizontal(Waypoints[Mathf.Min(Waypoints.Length-1,i+1)]-Waypoints[i]).normalized;
            if(i==0)before=after;if(i==Waypoints.Length-1)after=before;
            Vector3 side=Vector3.Cross(Vector3.up,(before+after).normalized);
            float miter=1/Mathf.Max(.7f,Vector3.Dot(side,Vector3.Cross(Vector3.up,after)));
            foreach(float offset in offsets)
            {
                var p=Waypoints[i]+side*(offset*miter);
                float shoulder=Mathf.InverseLerp(3.4f,8,Mathf.Abs(offset));
                p.y=Mathf.Lerp(Waypoints[i].y,-.065f,shoulder)+.015f;
                v.Add(p);
            }
            if(i==0)continue;
            for(int j=0;j<offsets.Length-1;j++)
            {
                int a=(i-1)*offsets.Length+j,b=i*offsets.Length+j;
                tr.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
        }
        MeshObject("連續緩坡與林路",v.ToArray(),tr.ToArray(),road,true);
    }
    void BuildRidge()
    {
        // An irregular vegetated shoulder hides the straight sightline from
        // the encounter. The walking path bends around its western foot.
        const int nx=19,nz=14;var v=new Vector3[nx*nz];var tr=new List<int>();
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            float xx=-12+x*2,zz=-46+z*2;
            float ellipse=Mathf.Pow((xx-8)/17,2)+Mathf.Pow((zz+34)/13,2);
            float h=Mathf.Pow(Mathf.Max(0,1-ellipse),1.3f)*6.5f;
            Vector3 p=P(xx,0,zz);
            Closest(p,out var near,out _);
            // The ridge cannot intrude into the real traversable corridor.
            h*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,9,Horizontal(p-near).magnitude));
            v[z*nx+x]=P(xx,h-.07f,zz);
            if(x<nx-1&&z<nz-1){int a=z*nx+x;tr.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
        }
        MeshObject("彎道內側林坡",v,tr.ToArray(),earth,true);
    }
    void PlantForest()
    {
        var trees=new List<Transform>();var shrubs=new List<Transform>();
        foreach(var t in flow.combatRegion.GetComponentsInChildren<Transform>(true))
        {
            if(t.name.StartsWith("交戰道路林木 ")&&trees.Count<6)trees.Add(t);
            if(t.name.StartsWith("交戰道路蕨草 ")&&shrubs.Count<6)shrubs.Add(t);
        }
        int number=0;
        for(int i=0;i<Waypoints.Length-1;i++)
        {
            Vector3 direction=Horizontal(Waypoints[i+1]-Waypoints[i]).normalized,side=Vector3.Cross(Vector3.up,direction);
            for(int j=0;j<2;j++)foreach(int sign in new[]{-1,1})
            {
                Vector3 p=Vector3.Lerp(Waypoints[i],Waypoints[i+1],.18f+j*.50f)+side*(sign*(7.5f+(i%2)*.8f));
                p.y=PlantGround(p);
                if(trees.Count>0)Plant(trees[number%trees.Count],p,"林路樹木 "+number,true);
                if(shrubs.Count>0)Plant(shrubs[number%shrubs.Count],p-side*(sign*1.25f),"林路蕨草 "+number,false);
                number++;
            }
        }
    }
    void Plant(Transform source,Vector3 position,string name,bool solid)
    {
        position.y=PlantGround(position);
        var wrapper=new GameObject(name).transform;wrapper.SetParent(Root,false);wrapper.position=position;
        var t=Instantiate(source.gameObject,wrapper).transform;t.name=source.name;t.gameObject.SetActive(true);
        t.localScale*=.8f;
        foreach(var collider in t.GetComponentsInChildren<Collider>(true)){collider.enabled=false;Destroy(collider);}
        var renderers=t.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length>0)
        {
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            t.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        if(solid)
        {
            var c=wrapper.gameObject.AddComponent<CapsuleCollider>();c.radius=.30f;c.height=5;c.center=Vector3.up*2.5f;
        }
    }
    void ClearEntrancePlants()
    {
        foreach(var region in new[]{flow.schoolRegion,flow.combatRegion})
        foreach(var t in region.GetComponentsInChildren<Transform>(true))
        {
            if(!(t.name.StartsWith("林木 ")||t.name.StartsWith("路旁蕨草 ")||t.name.StartsWith("可碰撞岩石 ")||
                t.name.StartsWith("交戰道路林木 ")||t.name.StartsWith("交戰道路蕨草 ")))continue;
            var renderers=t.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Vector3 center=bounds.center;
            if(center.z>-18||center.z<TrailStartZ-2)continue;
            Closest(center,out var nearest,out _);
            float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
            if(Horizontal(center-nearest).magnitude<3+radius&&t.gameObject.activeSelf)clearedPlants.Add(t.gameObject);
        }
    }
    void OpenEntrance()
    {
        var boxes=new List<Bounds>();
        foreach(var t in flow.schoolRegion.GetComponentsInChildren<Transform>(true))
        {
            if(t.name!="來路邊界木柵")continue;
            foreach(var c in t.GetComponentsInChildren<Collider>(true))
            {
                // Remove whole central spans while retaining the two end
                // posts. Physics and combined visible geometry open together.
                if(Mathf.Abs(c.transform.position.x)>=4.5f||!(c is BoxCollider box))continue;
                entranceColliders[c]=c.enabled;
                // Inactive collider.bounds is empty; authoring uses boxes.
                var bounds=new Bounds(box.transform.TransformPoint(box.center),Vector3.zero);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    bounds.Encapsulate(box.transform.TransformPoint(box.center+Vector3.Scale(box.size,new Vector3(x,y,z))*.5f));
                bounds.Expand(.025f);boxes.Add(bounds);
            }
        }
        if(boxes.Count==0)return;
        var opened=new bool[boxes.Count];
        foreach(var filter in flow.schoolRegion.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;
            // Saved/serialized school geometry uses Combined_* names. The
            // original authoring name only exists before that saved polish.
            if(!mesh||!mesh.isReadable||(!filter.name.StartsWith("School geometry ")&&!filter.name.StartsWith("Combined_")))continue;
            var vertices=mesh.vertices;var triangles=mesh.triangles;var kept=new List<int>();bool changed=false;
            for(int i=0;i<triangles.Length;i+=3)
            {
                bool remove=false;
                var a=filter.transform.TransformPoint(vertices[triangles[i]]);
                var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);
                var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                for(int j=0;j<boxes.Count;j++)
                    if(boxes[j].Contains(a)&&boxes[j].Contains(b)&&boxes[j].Contains(c))
                    {remove=true;opened[j]=true;}
                if(remove)changed=true;else kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
            }
            if(!changed)continue;
            var copy=Instantiate(mesh);copy.name=mesh.name+" · 林路入口";copy.triangles=kept.ToArray();copy.RecalculateBounds();
            owned.Add(copy);entranceMeshes.Add((filter,mesh,copy));
        }
        // Never switch off a visible fence's collision if any central piece
        // could not be removed from the readable runtime geometry.
        if(System.Array.Exists(opened,removed=>!removed))
        {
            foreach(var entry in entranceMeshes){owned.Remove(entry.open);Destroy(entry.open);}
            entranceMeshes.Clear();entranceColliders.Clear();
            Debug.LogWarning("[Chapter3] School entrance geometry could not be opened completely; fence collisions retained.");
        }
    }
    void MeshObject(string name,Vector3[] vertices,int[] triangles,Material material,bool solid)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};
        var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x/3,vertices[i].z/3);
        mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(Root,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        if(solid)
        {
            var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
            if(name=="連續緩坡與林路")trailSurface=collider;
            if(name=="彎道內側林坡")ridgeSurface=collider;
        }
    }
    public void SetVisible(bool visible)
    {
        if(Root)Root.gameObject.SetActive(visible);
        if(valley&&originalValley)valley.sharedMesh=visible?cutValley:originalValley;
        foreach(var plant in clearedPlants)if(plant)plant.SetActive(!visible);
        foreach(var pair in entranceColliders)if(pair.Key)pair.Key.enabled=visible?false:pair.Value;
        foreach(var entry in entranceMeshes)if(entry.filter)entry.filter.sharedMesh=visible?entry.open:entry.original;
    }
    public float GroundHeight(Vector3 position)
    {
        if(trailSurface&&trailSurface.Raycast(new Ray(position+Vector3.up*8,Vector3.down),out var hit,16))
            return hit.point.y;
        Closest(position,out var point,out _);return point.y+.015f;
    }
    float PlantGround(Vector3 position)
    {
        float height=-.08f;
        foreach(var c in Root.GetComponentsInChildren<MeshCollider>())
            if(c.Raycast(new Ray(new Vector3(position.x,24,position.z),Vector3.down),out var hit,30))
                height=Mathf.Max(height,hit.point.y);
        return height;
    }
    public Vector3 Constrain(Vector3 current,Vector3 desired)
    {
        if(Walkable(desired))return desired;
        // Reject an outward step. Never project a distant body to a waypoint,
        // which would be a visible teleport at the encounter/path junction.
        return new Vector3(current.x,desired.y,current.z);
    }
    public bool Walkable(Vector3 position)
    {
        // Use the rendered earth strip, including its broad shoulders and
        // mitered corners. The old 2.35 m centreline tube rejected visible road.
        // Retain body clearance at the mesh edge and the vegetated ridge.
        foreach(var offset in roadFootprint)
        {
            Vector3 p=position+offset;
            if(p.z>=-20&&p.z<=-14&&Mathf.Abs(p.x)<=9)continue; // Existing school dirt apron; fences still collide.
            var ray=new Ray(new Vector3(p.x,12,p.z),Vector3.down);
            if(!trailSurface||!trailSurface.Raycast(ray,out var floor,20)||floor.normal.y<.7f)return false;
            if(ridgeSurface&&ridgeSurface.Raycast(ray,out var ridge,20)&&ridge.point.y>floor.point.y+.06f)return false;
        }
        return true;
    }
    void Closest(Vector3 position,out Vector3 point,out int segment)
    {
        float best=float.MaxValue;point=Waypoints[0];segment=0;
        for(int i=0;i<Waypoints.Length-1;i++)
        {
            Vector3 d=Horizontal(Waypoints[i+1]-Waypoints[i]);
            float t=Mathf.Clamp01(Vector3.Dot(Horizontal(position-Waypoints[i]),d)/d.sqrMagnitude);
            Vector3 q=Vector3.Lerp(Waypoints[i],Waypoints[i+1],t);
            float error=Horizontal(position-q).sqrMagnitude;
            if(error<best){best=error;point=q;segment=i;}
        }
    }
    static Vector3 Horizontal(Vector3 v)=>Vector3.ProjectOnPlane(v,Vector3.up);
    void OnDestroy()
    {
        if(valley&&originalValley)valley.sharedMesh=originalValley;
        foreach(var entry in entranceMeshes)if(entry.filter)entry.filter.sharedMesh=entry.original;
        foreach(var m in owned)if(m)Destroy(m);
    }
}
