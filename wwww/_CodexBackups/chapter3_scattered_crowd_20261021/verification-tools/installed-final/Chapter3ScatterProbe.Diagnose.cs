using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;
public static partial class Chapter3ScatterProbe
{
    static IEnumerator Diagnose(Chapter3Flow f)
    {
        f.Carry(Chapter3Weapons.Weapon.Rifle);yield return Press(Key.F7);
        yield return Wait(()=>f.combat.Fighting,20,"mountain active");
        foreach(var e in f.combat.Enemies)e.HoldFire(300);
        yield return new WaitForEndOfFrame();
        foreach(var e in f.combat.Enemies.ToArray())ScanBody(f,e,"mountain");
        yield return Press(Key.F8);yield return Wait(()=>f.school.Exploring,15,"school calm");
        var a=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        foreach(var t in a.spectators)if(t&&t.name.Contains("警察"))Check("duplicate immune police hidden "+t.name,!t.gameObject.activeInHierarchy);
        CaptureCalm(f);yield return Choose(f,Chapter3SchoolBattle.Mission.Tado);
        foreach(var e in f.school.Battle.Enemies)e.HoldFire(300);
        yield return new WaitForEndOfFrame();foreach(var e in f.school.Battle.Enemies.ToArray())ScanBody(f,e,"school");
        yield return new WaitForSeconds(18);yield return new WaitForEndOfFrame();
        var people=a.GetComponentsInChildren<Chapter3SchoolCrowdActor>().Where(x=>!x.IsFallen).Select(x=>x.transform).Concat(a.runners.Select(x=>x.transform)).ToArray();
        float minGap=999;string pair="";
        for(int i=0;i<people.Length;i++)for(int j=i+1;j<people.Length;j++){float gap=Flat(people[i].position,people[j].position);if(gap<minGap){minGap=gap;pair=people[i].name+" & "+people[j].name;}}
        Check("fled crowd has no overlapping standing pair",minGap>.65f);report.observations.Add("Settled crowd minimum spacing="+minGap+" pair="+pair);
        ShotAt("crowd-corner",new Vector3(-25,2,12),new Vector3(-19,1,16));
        yield return ResetSchool(f);
    }
    static void ScanBody(Chapter3Flow f,Chapter3Enemy e,string label)
    {
        var rig=e.actor.Rig;var torso=e.GetComponentsInChildren<Chapter3EnemyHitZone>().First(z=>z.name.EndsWith("Torso"));
        report.observations.Add(label+" "+e.name+" height="+rig.Height+" hips="+rig.Hips.position+" neckZone="+torso.GetComponent<Collider>().bounds+" activeZones="+e.GetComponentsInChildren<Chapter3EnemyHitZone>().Length);
        // Sample actual visible geometry, independently of the hit-zone centres.
        var baked=new System.Collections.Generic.List<GameObject>();
        foreach(var skin in e.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.enabled&&skin.sharedMesh){var mesh=new Mesh();skin.BakeMesh(mesh);var go=new GameObject("Diagnostic visible skin");go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.transform.localScale=skin.transform.lossyScale;var meshCollider=go.AddComponent<MeshCollider>();meshCollider.cookingOptions &= ~MeshColliderCookingOptions.UseFastMidphase;meshCollider.sharedMesh=mesh;baked.Add(go);}
        Physics.SyncTransforms();int visible=0,miss=0;var details=new System.Collections.Generic.List<string>();
        Vector3 forward=rig.Forward,right=Vector3.Cross(Vector3.up,forward),basePoint=rig.Hips.position;
        for(int side=0;side<4;side++)for(int y=1;y<6;y++)for(int x=-3;x<=3;x++)
        {var dir=Quaternion.Euler(0,side*90,0)*forward;var across=Vector3.Cross(Vector3.up,dir);var point=basePoint+Vector3.up*(rig.Height*(-.07f+y*.058f))+across*(rig.Height*x*.045f);var ray=new Ray(point+dir*4,-dir);bool skinHit=baked.Any(go=>go.GetComponent<Collider>().Raycast(ray,out var hit,4.5f));if(!skinHit)continue;visible++;bool zoneHit=e.GetComponentsInChildren<Chapter3EnemyHitZone>().Any(z=>z.Region==Chapter3EnemyHitZone.BodyRegion.Body&&z.GetComponent<Collider>().Raycast(ray,out var h,4.5f));if(!zoneHit){miss++;if(details.Count<15)details.Add("side="+side+" y="+y+" x="+x);}}
        report.observations.Add(label+" skin-confirmed torso samples="+visible+" missing body region="+miss+" "+string.Join(";",details));
        Check(label+" independently sampled visible waist and torso are covered",visible>50&&miss==0);
        foreach(var go in baked){var mesh=go.GetComponent<MeshCollider>().sharedMesh;go.SetActive(false);Object.Destroy(go);Object.Destroy(mesh);}
    }
}
