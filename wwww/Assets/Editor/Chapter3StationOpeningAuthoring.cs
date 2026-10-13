using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class Chapter3StationOpeningAuthoring
{
    public const string MainPath="Assets/Scenes/第三章.unity",ArchivePath="Assets/Scenes/ch3部落與山林.unity";
    [MenuItem("Tools/Chapter 3/駐在所開場與部落保留場景")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=MainPath||scene.isDirty)throw new InvalidOperationException("Open the saved third chapter first.");
        var f=Object.FindFirstObjectByType<Chapter3Flow>();
        if(!f)throw new InvalidOperationException("Chapter 3 flow missing.");
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ArchivePath))
        {
            if(!f.villageRegion||!f.village)throw new InvalidOperationException("Village must be preserved before removal.");
            if(!EditorSceneManager.SaveScene(scene,ArchivePath,true))throw new Exception("Archive copy failed.");
            var archive=EditorSceneManager.OpenScene(ArchivePath,OpenSceneMode.Additive);
            var old=archive.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter3Flow>(true)).Single();
            var v=old.village;v.flow=null;old.player.chapter=v;old.player.station=null;old.player.combat=null;old.player.school=null;
            if(old.combat.weapons)Object.DestroyImmediate(old.combat.weapons.gameObject);
            Object.DestroyImmediate(old.combat.hud);Object.DestroyImmediate(old.combat.downPanel);
            if(old.combat.hurtOverlay)Object.DestroyImmediate(old.combat.hurtOverlay.gameObject);
            Object.DestroyImmediate(old.stationRegion);Object.DestroyImmediate(old.combatRegion);Object.DestroyImmediate(old.schoolRegion);
            old.villageRegion.SetActive(true);Object.DestroyImmediate(old.gameObject);
            EditorSceneManager.MarkSceneDirty(archive);EditorSceneManager.SaveScene(archive);EditorSceneManager.CloseScene(archive,true);
            SceneManager.SetActiveScene(scene);
        }
        var s=f.station;
        if(f.village)
        {
            var donor=f.village.knifePickup;
            var group=new GameObject("右側領刀木桌").transform;group.SetParent(f.stationRegion.transform,false);
            // Move the original knife, tabletop and four legs; leave firewood in the archive.
            var items=donor.parent.Cast<Transform>().Where(t=>t==donor||t.name=="整裝木桌"||t.name=="桌腳").ToArray();
            foreach(var item in items){item.SetParent(group,true);item.position+=new Vector3(7.3f,0,-4.4f);if(item!=donor&&!item.GetComponent<Collider>())item.gameObject.AddComponent<BoxCollider>();}
            s.knifePickup=donor;
            var focus=new GameObject("領刀互動點").transform;focus.SetParent(group,false);focus.position=donor.position;s.knifeFocus=focus;
            Object.DestroyImmediate(f.villageRegion);f.village=null;f.villageRegion=null;
        }
        if(!s.knifePickup||!s.knifeFocus)throw new InvalidOperationException("Knife table transfer failed.");
        f.startAtStation=true;f.stationRegion.SetActive(true);f.combatRegion.SetActive(false);f.schoolRegion.SetActive(false);
        f.player.chapter=null;f.player.station=s;f.player.combat=null;f.player.school=null;
        s.bawan.transform.position=new Vector3(0,0,-15);s.pihu.transform.position=new Vector3(1.6f,0,-17);
        f.player.Warp(new Vector3(0,.06f,-18),s.bawan.transform.position+Vector3.up*1.2f);
        s.bawan.Face(f.player.transform.position);s.pihu.Face(Vector3.zero);
        f.ui.objective.text="馬赫坡駐在所 · 清晨";f.ui.Say("",null);f.ui.chat.SetActive(false);f.ui.ending.SetActive(false);f.ui.fade.color=Color.clear;
        RenderSettings.sun=f.stationRegion.GetComponentInChildren<Light>();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Chapter2GameViewScaleGuard.Restore();
    }
}
