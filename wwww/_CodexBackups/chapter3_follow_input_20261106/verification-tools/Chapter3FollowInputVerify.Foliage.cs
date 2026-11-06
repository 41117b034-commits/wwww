using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public static partial class Chapter3FollowInputVerify
{
    static IEnumerator ContinuousFoliage()
    {
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.ContinueFromStation();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",false);
        foreach(var e in f.combat.Enemies){e.AttackEnabled=false;e.HoldFire(1000);}
        f.ui.fade.color=Color.clear;f.ui.Say("",null);yield return Frames(3);
        Check("continuous station entry retains station scenery",f.stationRegion.activeInHierarchy);
        var fern=f.stationRegion.GetComponentsInChildren<Transform>(true).First(t=>t.name=="路側蕨草 6");
        var bounds=fern.GetComponent<Renderer>().bounds;
        report.notes.Add("Original fern 6 position="+fern.position+" minY="+bounds.min.y+" active="+fern.gameObject.activeInHierarchy);
        if(mode.EndsWith("before"))Check("old station fern remains 1.7 metres above combat road",fern.gameObject.activeInHierarchy&&bounds.min.y>1.7f);
        else
        {
            int removed=0,retained=0;
            foreach(var t in f.stationRegion.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("路側蕨草 ")))
            {
                if(t.position.z> -93){Check("trimmed terrain fern hidden: "+t.name,!t.gameObject.activeSelf);removed++;}
                else if(t.gameObject.activeSelf)retained++;
            }
            Check("station courtyard vegetation retained",retained>0);report.notes.Add("removed unsupported old ferns="+removed+" retained="+retained);
            var env=f.GetComponent<Chapter3ApproachEnvironment>();
            foreach(Transform t in env.Root)
            {
                if(!t.name.StartsWith("林路蕨草 ")&&!t.name.StartsWith("林路樹木 "))continue;
                var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                var hits=Physics.RaycastAll(new Vector3(b.center.x,24,b.center.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore);
                var groundHits=hits.Where(h=>h.collider.name=="連續緩坡與林路"||h.collider.name=="彎道內側林坡"||h.collider.name=="林間基底"||h.collider.name=="School mountain terrain"||h.collider.name=="山路地面").ToArray();
                float gap=groundHits.Length>0?b.min.y-groundHits.Max(h=>h.point.y):999;
                // A root slightly buried at the join is valid. Include the
                // adjacent school floor and steep ridge faces independently.
                Check("independent terrain hit supports "+t.name,gap<.025f&&gap>-.1f);
            }
        }
        f.player.Warp(new Vector3(-1,.06f,-82),new Vector3(-2.45f,1.73f,-71.71f));yield return Frames(3);yield return Capture(mode);
        if(!mode.EndsWith("before"))
        {
            yield return Press(Keyboard.current,UnityEngine.InputSystem.Key.F7);yield return Frames(3);
            Check("F7 enters short combat after continuous route",f.Region=="Combat"&&!f.stationRegion.activeSelf);
            yield return Press(Keyboard.current,UnityEngine.InputSystem.Key.F6);yield return Frames(3);
            Check("F6 restores station without old floating ferns",f.Region=="Station"&&!fern.gameObject.activeSelf);
        }
    }
    static IEnumerator FoliageAudit()
    {
        var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>());
        foreach(var r in all)
        {
            string path=r.name;for(var p=r.transform.parent;p;p=p.parent)path=p.name+"/"+path;
            if(!(path.Contains("草")||path.IndexOf("grass",StringComparison.OrdinalIgnoreCase)>=0||path.Contains("蕨")))continue;
            var b=r.bounds;if(b.center.z>0||b.center.z< -110)continue;
            var hits=Physics.RaycastAll(new Vector3(b.center.x,24,b.center.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore).OrderByDescending(h=>h.point.y);
            report.notes.Add(path+" bounds="+b+" minY="+b.min.y+" hits="+string.Join("; ",hits.Select(h=>h.collider.name+" y="+h.point.y+" normal="+h.normal)));
        }
        f.player.Warp(f.combat.WorldPoint(new Vector3(0,.06f,5)),f.combat.WorldPoint(new Vector3(0,1.5f,35)));
        yield return Frames(3);yield return Capture("foliage-crop-view");
        Check("recorded actual visible foliage and unfiltered surface hits",true);
    }
}
