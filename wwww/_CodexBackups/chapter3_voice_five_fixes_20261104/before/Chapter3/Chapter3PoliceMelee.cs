using System.Collections.Generic;
using UnityEngine;

// Period police sidearm, based on the documented Western-style police sabre.
// These four duels are fictional encounter staging, not named historical acts.
[DefaultExecutionOrder(4800)]
public sealed class Chapter3PoliceMelee : MonoBehaviour
{
    Chapter3Enemy enemy;
    Transform upper,elbow,leftUpper,leftElbow,weapon;
    Mesh blade;
    readonly List<Material> materials=new List<Material>();
    float nextStrike,started=-1,flinchUntil;
    bool landed;
    Transform strikeTarget;
    public int Swings {get;private set;}
    public int Contacts {get;private set;}
    public bool Swinging=>started>=0;
    public float StrikeProgress=>started<0?0:Mathf.Clamp01((Time.time-started)/.9f);
    public bool Blocking=>Time.time<blockUntil;
    public bool Recovering=>Swinging&&StrikeProgress>.70f;
    public int Parries {get;private set;}
    public int AttackVariant=>Swings%3;
    float blockUntil;
    public bool ParryAlly()
    {
        if(Swinging||Time.time<flinchUntil)return false;
        blockUntil=Time.time+.42f;nextStrike=Mathf.Min(nextStrike,Time.time+.48f);Parries++;return true;
    }
    public Transform Weapon=>weapon;
    public void Configure(Chapter3Enemy owner)
    {
        enemy=owner;
        if(enemy.rifle)
        {
            enemy.rifle.enabled=false;
            if(enemy.rifle.weapon)enemy.rifle.weapon.gameObject.SetActive(false);
            if(enemy.rifle.muzzleFlash)enemy.rifle.muzzleFlash.SetActive(false);
        }
        if(!weapon)BuildSabre();
        nextStrike=Time.time+1.3f;
    }
    public void ReactToHit(){flinchUntil=Time.time+.55f;started=-1;nextStrike=Time.time+1;}
    public void Tick()
    {
        if(!enemy||!enemy.actor||!enemy.actor.Rig)return;
        var rig=enemy.actor.Rig;rig.strike=false;
        if(enemy.Defeated||!enemy.AttackEnabled||enemy.HoldingFire||!enemy.combat.Fighting||(enemy.AlliesOnly&&!enemy.escortTarget))
        {started=-1;return;}
        Vector3 target=enemy.escortTarget?enemy.escortTarget.position:enemy.combat.player.transform.position;
        enemy.actor.Face(target);
        if(Time.time<flinchUntil||Blocking)return;
        float range=Vector3.ProjectOnPlane(target-transform.position,Vector3.up).magnitude;
        if(started<0&&range<=1.9f&&Time.time>=nextStrike)
        {started=Time.time;strikeTarget=enemy.escortTarget;landed=false;Swings++;}
        if(started<0)return;
        float progress=StrikeProgress;
        if(progress>=.62f&&!landed)
        {
            landed=true;
            Vector3 destination=strikeTarget?strikeTarget.position+Vector3.up*1.05f:enemy.combat.playerTarget.position;
            if(Vector3.ProjectOnPlane(destination-transform.position,Vector3.up).magnitude<=1.9f&&
                enemy.combat.ClearLine(enemy.AimPoint,destination,enemy))
            {
                Contacts++;
                if(strikeTarget)enemy.combat.SchoolEncounter.CampusAllyUnderFire(strikeTarget);
                else if(!enemy.AlliesOnly)enemy.combat.Damage(20,enemy.AimPoint);
            }
        }
        if(progress>=1){started=-1;nextStrike=Time.time+(Swings%3==0?.85f:.38f);}
    }
    void LateUpdate()
    {
        if(!enemy||!enemy.actor||!enemy.actor.Rig||!weapon)return;
        var rig=enemy.actor.Rig;
        if(enemy.Defeated)
        {
            // Fall with the skeleton, then settle the same visible weapon.
            float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,enemy.actor.FallProgress));
            Vector3 ground=transform.position+rig.Forward*.55f+Vector3.Cross(Vector3.up,rig.Forward)*.38f+Vector3.up*.06f;
            weapon.SetPositionAndRotation(Vector3.Lerp(rig.RightHand.position,ground,t),Quaternion.Slerp(rig.RightHand.rotation,Quaternion.LookRotation(Vector3.up,rig.Forward),t));
            return;
        }
        if(!upper)
        {
            var animator=rig.animator;
            upper=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
            elbow=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.RightLowerArm,"R_Forearm","RightForeArm");
            leftUpper=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
            leftElbow=Chapter1WeddingRigBones.Resolve(animator,HumanBodyBones.LeftLowerArm,"L_Forearm","LeftForeArm");
        }
        if(!upper||!elbow)return;
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward);
        float p=StrikeProgress;
        float wind=started<0?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.38f,p));
        float cut=started<0?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.72f,p));
        float recover=started<0?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1,p));
        Vector3 guard=upper.position+forward*.32f+right*.07f-Vector3.up*.22f;
        Vector3 raised=upper.position+Vector3.up*.28f-forward*.08f;
        Vector3 follow=upper.position+forward*.48f-right*.16f-Vector3.up*.24f;
        if(AttackVariant==1){raised=upper.position+right*.33f+forward*.10f+Vector3.up*.12f;follow=upper.position-right*.30f+forward*.40f-Vector3.up*.08f;}
        if(AttackVariant==2){raised=upper.position-right*.22f+forward*.12f;follow=upper.position+right*.27f+forward*.43f-Vector3.up*.14f;}
        Vector3 hand=Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(guard,raised,wind),follow,cut),guard,recover);
        bool block=Time.time<flinchUntil||Blocking;
        if(block)hand=upper.position+forward*.26f+Vector3.up*.04f;
        Quaternion wrist=rig.RightHand.rotation;
        Chapter1IncidentRig.Solve(upper,elbow,rig.RightHand,hand,right+Vector3.down*.3f);
        rig.RightHand.rotation=wrist;
        if(leftUpper&&leftElbow)
            Chapter1IncidentRig.Solve(leftUpper,leftElbow,rig.LeftHand,leftUpper.position+forward*.24f-Vector3.up*.22f,-right);
        Vector3 direction=block?(Vector3.up+right*.85f).normalized:
            Vector3.Slerp(Vector3.Slerp((Vector3.up+forward*.55f).normalized,Vector3.up-forward*.3f,wind),forward-Vector3.up*.65f,cut*(1-recover));
        if(!block&&AttackVariant>0)direction=Vector3.Slerp(direction,(forward+(AttackVariant==1?-right:right)*.65f-Vector3.up*.15f).normalized,cut*(1-recover));
        weapon.SetPositionAndRotation(rig.RightHand.position+forward*.025f,Quaternion.LookRotation(right,direction));
    }
    Material Material(string name,Color color,float metal)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
        var material=new Material(shader){name=name,color=color};material.SetFloat("_Metallic",metal);material.SetFloat("_Smoothness",.65f);materials.Add(material);return material;
    }
    void BuildSabre()
    {
        weapon=new GameObject("日警洋式佩刀 · 遊戲重建").transform;weapon.SetParent(transform,false);
        var scale=transform.lossyScale;weapon.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var steel=Material("佩刀鋼",new Color(.68f,.72f,.76f),.85f);
        var brass=Material("佩刀護手",new Color(.42f,.31f,.13f),.65f);
        var grip=Material("佩刀黑握柄",new Color(.065f,.055f,.045f),.05f);
        var vertices=new List<Vector3>();var triangles=new List<int>();const int sections=10;
        for(int i=0;i<=sections;i++)
        {
            float t=i/(float)sections,y=.10f+t*.74f,x=t*t*.045f,width=.022f*Mathf.Lerp(1,.05f,t*t*t*t);
            vertices.Add(new Vector3(x-width,y,0));vertices.Add(new Vector3(x,y,.006f));
            vertices.Add(new Vector3(x+width,y,0));vertices.Add(new Vector3(x,y,-.006f));
            if(i==0)continue;
            for(int j=0;j<4;j++){int a=(i-1)*4+j,b=(i-1)*4+(j+1)%4,c=i*4+j,d=i*4+(j+1)%4;triangles.AddRange(new[]{a,c,b,b,c,d});}
        }
        blade=new Mesh{name="洋式佩刀刀身"};blade.SetVertices(vertices);blade.SetTriangles(triangles,0);blade.RecalculateNormals();blade.RecalculateBounds();
        var body=new GameObject("鋼刀身",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(weapon,false);
        body.GetComponent<MeshFilter>().sharedMesh=blade;body.GetComponent<Renderer>().sharedMaterial=steel;
        Bar("握柄",new Vector3(0,-.075f,0),new Vector3(0,.09f,0),.028f,grip);
        Bar("護手",new Vector3(-.055f,.09f,0),new Vector3(.11f,.09f,0),.012f,brass);
        Vector3 previous=new Vector3(.11f,.09f,0);
        for(int i=1;i<=8;i++)
        {
            float t=i/8f;Vector3 next=new Vector3(.11f+Mathf.Sin(t*Mathf.PI)*.025f-.11f*t,-.075f*t+.09f*(1-t),0);
            Bar("護弓",previous,next,.009f,brass);previous=next;
        }
        Bar("柄頭",new Vector3(-.03f,-.075f,0),new Vector3(.03f,-.075f,0),.015f,brass);
    }
    void Bar(string name,Vector3 a,Vector3 b,float radius,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(weapon,false);
        go.transform.localPosition=(a+b)*.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        go.transform.localScale=new Vector3(radius*2,(b-a).magnitude*.5f,radius*2);
        var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=material;
    }
    void OnDestroy(){if(blade)Destroy(blade);foreach(var material in materials)if(material)Destroy(material);}
}
