using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class DialogueFireVerify
{
    static IEnumerator GeometryChecks()
    {
        var ambient=f.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        var natives=ambient.GetComponentsInChildren<Chapter3SchoolCrowdActor>().Where(c=>c.UsingOriginalSkin&&c.name.StartsWith("校內埋伏")).ToArray();
        Check("all ten original ambushers remain active",natives.Length==10&&ambient.VisibleAssailants==10);
        var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");
        var eye=f.player.view.transform;Vector3 saved=eye.position;Quaternion rotation=eye.rotation;
        foreach(var crowd in natives.Take(3))
        {
            var skin=crowd.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
            var entry=catalog.entries.First(e=>e.source==skin.sharedMesh||e.distant==skin.sharedMesh);
            var bones=skin.bones;var materials=skin.sharedMaterials;var capsule=crowd.GetComponent<CapsuleCollider>();float radius=capsule.radius;
            eye.position=crowd.transform.position+Vector3.up*1.5f+Vector3.back*2;eye.LookAt(crowd.transform.position+Vector3.up);yield return Frames(3);
            Check(crowd.name+" restores original near model",skin.sharedMesh==entry.source);yield return Capture("native-near-"+crowd.GetInstanceID());
            eye.position=crowd.transform.position+Vector3.up*1.5f+Vector3.back*16;eye.LookAt(crowd.transform.position+Vector3.up);yield return Frames(3);
            Check(crowd.name+" uses distant model",skin.sharedMesh==entry.distant&&skin.sharedMesh.vertexCount<100000);
            Check(crowd.name+" retains bones materials collision",skin.bones.SequenceEqual(bones)&&skin.sharedMaterials.SequenceEqual(materials)&&Mathf.Approximately(capsule.radius,radius));
        }
        eye.SetPositionAndRotation(saved,rotation);yield return Frames(3);yield return Capture("school-final-crowd");
        var children=ambient.GetComponentsInChildren<Chapter3SchoolCrowdActor>().Where(c=>c.name.StartsWith("日籍學童")).ToArray();
        Check("all ten original pupils still present",children.Length==10);
        foreach(var crowd in children)
        {
            var skin=crowd.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.sharedMesh&&s.sharedMesh.vertexCount>1000);
            Check(crowd.name+" retains clothing and valid pose",skin.sharedMesh.subMeshCount==skin.sharedMaterials.Length&&skin.bones.Length==15&&skin.sharedMesh.bindposes.Length==15);
        }
        Check("reduced meshes are shared across repeated actors",natives.Select(c=>c.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First().sharedMesh).Distinct().Count()<=3);
    }
}
