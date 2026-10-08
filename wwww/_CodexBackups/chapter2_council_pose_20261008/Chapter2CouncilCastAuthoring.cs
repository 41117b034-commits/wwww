using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class Chapter2CouncilCastAuthoring
{
    const string SourceScene="Assets/Scenes/第一章新版警察.unity";
    public static void Audit()
    {
        var active=SceneManager.GetActiveScene();
        var source=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
        var report=new StringBuilder();
        try
        {
            foreach(var root in source.GetRootGameObjects())foreach(var a in root.GetComponentsInChildren<Animator>(true))
            {
                var skins=a.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length==0)continue;
                report.AppendLine(a.name+" | root="+a.transform.root.name+" | human="+a.isHuman+" | "+string.Join(";",skins.Select(s=>AssetDatabase.GetAssetPath(s.sharedMesh))));
            }
            var c=active.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Chapter2Controller>(true)).First();
            report.AppendLine("CURRENT COUNCIL:");
            foreach(var a in c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true))
                report.AppendLine(a.name+" | "+string.Join(";",a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>AssetDatabase.GetAssetPath(s.sharedMesh))));
            File.WriteAllText("_CodexBackups/chapter2_council_rally_20261007/cast-audit.txt",report.ToString());
        }
        finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(active);}
    }

    [MenuItem("Tools/Chapter 2/Use Distinct Council Characters")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var active=SceneManager.GetActiveScene();
        var source=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
        try
        {
            var roots=source.GetRootGameObjects();
            c.mona=Replace(c.mona,roots.First(r=>r.name=="莫那"));
            c.leaders[3]=Replace(c.leaders[3],roots.First(r=>r.name=="賽德克帥哥"));
            c.leaders[4]=Replace(c.leaders[4],AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/日治時期長輩女/tripo_convert_55823c37-f454-4808-bc43-e5329c672c81.fbx"));
            c.leaders[5]=Replace(c.leaders[5],roots.First(r=>r.name=="部落女性1"));
            c.conservatives[0]=Replace(c.conservatives[0],AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/畢專 賽德克青年/原住民青年2/tripo_convert_9c8dfc1a-8262-45ed-8cd9-4adc9d0040e8.fbx"));
            c.conservatives[1]=Replace(c.conservatives[1],AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/日本女性/賽德克新娘/tripo_convert_880135f6-31e4-4f15-a52b-10eacb2ded41.fbx"));
            c.mona.name="莫那魯道 · 既有長老角色";
            // This donor's long skirt needs a narrower stump behind the knees.
            var skirtActor=c.leaders[2];
            var seat=c.nightGroup.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name=="Council seat")
                .OrderBy(t=>Vector3.Distance(t.position,skirtActor.transform.position)).First();
            Vector3 inward=Vector3.ProjectOnPlane(c.campfire.position-skirtActor.transform.position,Vector3.up).normalized;
            seat.position=skirtActor.transform.position-inward*.25f+Vector3.up*.22f;
            seat.localScale=new Vector3(.45f,.22f,.45f);
            EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(active);EditorSceneManager.SaveScene(active);AssetDatabase.SaveAssets();
        }
        finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(active);}
    }

    static Chapter2Actor Replace(Chapter2Actor old,GameObject donor)
    {
        if(!donor)throw new InvalidOperationException("Missing council character donor.");
        var go=Object.Instantiate(donor);go.name=old.name;go.SetActive(true);
        SceneManager.MoveGameObjectToScene(go,old.gameObject.scene);
        foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        foreach(var col in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
        foreach(var t in go.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        var a=go.GetComponentsInChildren<Animator>(true).FirstOrDefault(x=>x.isHuman)??go.GetComponentInChildren<Animator>(true);
        if(!a)a=go.AddComponent<Animator>();
        var l=Bone(a,HumanBodyBones.LeftUpperArm,"L_Upperarm","LeftArm");
        var r=Bone(a,HumanBodyBones.RightUpperArm,"R_Upperarm","RightArm");
        var lh=Bone(a,HumanBodyBones.LeftHand,"L_Hand","LeftHand");
        var rh=Bone(a,HumanBodyBones.RightHand,"R_Hand","RightHand");
        if(!l||!r||!lh||!rh){Object.DestroyImmediate(go);throw new InvalidOperationException("Missing arm bones in "+donor.name);}
        go.transform.SetParent(old.transform.parent,false);go.transform.SetPositionAndRotation(old.transform.position,Quaternion.identity);
        var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Bounds bounds=skins[0].bounds;foreach(var skin in skins)bounds.Encapsulate(skin.bounds);
        go.transform.localScale*=1.72f/bounds.size.y;
        Vector3 forward=Vector3.Cross(r.position-l.position,Vector3.up).normalized;
        l.rotation=Quaternion.FromToRotation(lh.position-l.position,Vector3.down+forward*.13f)*l.rotation;
        r.rotation=Quaternion.FromToRotation(rh.position-r.position,Vector3.down+forward*.13f)*r.rotation;
        go.transform.rotation=Quaternion.FromToRotation(forward,old.facing.normalized)*go.transform.rotation;
        foreach(var animator in go.GetComponentsInChildren<Animator>(true)){animator.runtimeAnimatorController=null;animator.enabled=false;}
        foreach(var skin in skins){skin.updateWhenOffscreen=true;skin.sharedMaterials=skin.sharedMaterials.Select(Material).ToArray();}
        var actor=go.AddComponent<Chapter2Actor>();actor.seated=old.seated;actor.facing=old.facing;
        Object.DestroyImmediate(old.gameObject);return actor;
    }

    static Material Material(Material original)
    {
        if(!original)return original;
        if(original.shader.name.StartsWith("Universal Render Pipeline/"))return original;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long id);
        string path="Assets/Chapter2/Materials/Council_"+guid+"_"+id+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap",original.mainTexture);material.SetColor("_BaseColor",original.color);
            material.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(material,path);
        }
        return material;
    }

    static Transform Bone(Animator a,HumanBodyBones bone,params string[] names)
    {
        if(a.isHuman&&a.GetBoneTransform(bone))return a.GetBoneTransform(bone);
        return a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Any(n=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase)||t.name.EndsWith(":"+n,StringComparison.OrdinalIgnoreCase)));
    }
}
