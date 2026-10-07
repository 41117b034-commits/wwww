using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2MeterChildReview
{
    const string Dir="_CodexBackups/chapter2_meter_child_20261007/";
    static int cuts=-1,failed=-1;
    static bool followShot,nightRecorded;
    static string label;
    static Chapter2Controller c;
    static Chapter2AmbientNPC child;
    static Chapter2MeterChildReview()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode)
            {
                label=SessionState.GetString("Chapter2PerformanceTest","");cuts=failed=-1;followShot=nightRecorded=false;
                c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
                child=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().First(n=>n.name==Chapter2ChildNPCAuthoring.ChildName);
                File.WriteAllText(Dir+label+"-meter.csv","stage,valid,failed,damageIntegrity,label,fill,barAnchorMaxX\n");
            }
        };
    }
    static void Tick()
    {
        if(!Application.isPlaying||!c||string.IsNullOrEmpty(label))return;
        if((c.CurrentStage==Chapter2Controller.Stage.Chopping||c.ValidCuts>0)&&(cuts!=c.ValidCuts||failed!=c.FailedCuts))
        {
            cuts=c.ValidCuts;failed=c.FailedCuts;Canvas.ForceUpdateCanvases();
            File.AppendAllText(Dir+label+"-meter.csv",c.CurrentStage+","+cuts+","+failed+","+c.Integrity+","+c.ui.meterLabel.text+","+c.ui.meter.fillAmount+","+c.ui.meter.rectTransform.anchorMax.x+"\n");
            if(c.ui.meterPanel.activeSelf)Chapter2WorkProbe.Shot(label+"-meter-"+cuts+"-miss-"+failed);
        }
        if(!followShot&&c.CurrentStage==Chapter2Controller.Stage.Follow&&!c.Introducing&&c.player.transform.position.z>-10)
        {
            followShot=true;
            Chapter2WorkProbe.Shot(label+"-left-child-follow");
            var a=child.GetComponent<Chapter2Actor>();
            File.WriteAllText(Dir+label+"-child.txt","name="+child.name+" position="+child.transform.position+" rigValid="+(a.Rig&&a.Rig.Head)+" rigHeight="+a.Rig.Height+" trips="+child.TripsCompleted+" skin="+child.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name+"\n");
        }
        if(!nightRecorded&&c.CurrentStage==Chapter2Controller.Stage.Meeting)
        {
            nightRecorded=true;
            File.AppendAllText(Dir+label+"-child.txt","hiddenAtNight="+!child.gameObject.activeInHierarchy+" trips="+child.TripsCompleted+" distance="+child.MaxDistanceFromHome+"\n");
        }
    }
}
