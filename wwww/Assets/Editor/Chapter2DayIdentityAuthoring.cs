using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class Chapter2DayIdentityAuthoring
{
    [MenuItem("Tools/Chapter 2/Link Day Characters To Council")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        if(!c||c.gameObject.scene.path!="Assets/Scenes/第二章.unity")throw new InvalidOperationException("Open Chapter 2 first.");
        var group=c.dayGroup.transform.Find("Background people · local patrols");
        var people=group.GetComponentsInChildren<Chapter2Actor>(true);
        var mona=people.First(a=>a.name=="帖木·阿班"||a.name=="莫那·魯道");
        var lubi=people.First(a=>a.name=="伊婉·諾幹"||a.name=="露比·巴干");
        // These two background slots previously duplicated the workers' models.
        // Use the actual council donors so a shared name also has a shared face.
        mona=ReplaceAppearance(mona,c.mona);
        lubi=ReplaceAppearance(lubi,c.leaders[5]);
        Link(mona,c.mona,"你是莫那·魯道，賽德克社頭目，達多·莫那的父親。你在林道附近關心族人的勞作和安危，說話沉穩有威嚴，不是帶路青年阿威。");
        Link(c.workers[1],c.tado,"你是達多·莫那，莫那·魯道的兒子。你在巨木旁與同伴勞作，關心父親、族人與家人，對警察強迫勞動感到不滿；不是頭目本人，也不負責帶路。");
        Link(c.workers[2],c.leaders[2],"你是古慕·阿基，熟悉山林生活的賽德克女子。你在巨木附近勞作，關心同伴、家人與糧食，說話溫和。");
        Link(lubi,c.leaders[5],"你是露比·巴干，熟悉林間植物與部落生活的賽德克女子。你在林道旁活動，關心家人，願意與玩家交談。");
        var dayChild=people.First(a=>a.isChild);
        var nightChild=c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true).First(a=>a.isChild);
        Link(dayChild,nightChild,"你是都比·阿威，跟著家人在林邊活動的賽德克小孩。好奇、說話簡單，關心家人和動植物，不懂軍事政治。");
        Chapter2CastNames.Apply(c);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void Link(Chapter2Actor day,Chapter2Actor night,string role)
    {
        var link=day.GetComponent<Chapter2DayCouncilIdentity>()??day.gameObject.AddComponent<Chapter2DayCouncilIdentity>();
        link.councilActor=night;link.daytimeRole=role;link.Apply();EditorUtility.SetDirty(link);EditorUtility.SetDirty(day);
    }
    static Chapter2Actor ReplaceAppearance(Chapter2Actor old,Chapter2Actor donor)
    {
        if(old.GetComponent<Chapter2DayCouncilIdentity>()?.councilActor==donor)return old;
        var go=Object.Instantiate(donor.gameObject,old.transform.parent);go.name=old.name;go.SetActive(true);
        var knife=go.GetComponent<Chapter2CouncilDrama>()?.knife;
        if(knife)Object.DestroyImmediate(knife.gameObject);
        foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        foreach(var col in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
        foreach(var t in go.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        var actor=go.AddComponent<Chapter2Actor>();EditorUtility.CopySerialized(old,actor);
        actor.seated=false;actor.fallen=false;actor.speaking=false;
        var patrol=go.AddComponent<Chapter2AmbientNPC>();EditorUtility.CopySerialized(old.GetComponent<Chapter2AmbientNPC>(),patrol);
        go.transform.position=old.transform.position;
        go.transform.rotation=Quaternion.FromToRotation(Vector3.ProjectOnPlane(donor.facing,Vector3.up).normalized,Vector3.ProjectOnPlane(old.facing,Vector3.up).normalized)*donor.transform.rotation;
        Object.DestroyImmediate(old.gameObject);
        return actor;
    }
}
