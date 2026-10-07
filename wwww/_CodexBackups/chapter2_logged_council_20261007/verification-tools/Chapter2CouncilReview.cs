using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2CouncilReview
{
    const string Dir="_CodexBackups/chapter2_logged_council_20261007/";
    static Chapter2Controller c;
    static string label,lastLine;
    static double lineStarted;
    static int count;
    static bool wide;
    static Result result;
    [Serializable]class Result
    {
        public int trees,timberStacks,speakerShots;
        public bool daytimeUpright,allNightTreesHorizontal,complete;
        public float maxUprightDot;
        public List<string> speakers=new List<string>();
        public List<string> failures=new List<string>();
    }
    static Chapter2CouncilReview()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode)
            {
                label=SessionState.GetString("Chapter2PerformanceTest","");if(label=="")return;
                c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
                var forest=c.GetComponent<Chapter2LoggedForest>();
                result=new Result{trees=forest.trees.Length,timberStacks=forest.timberPiles.transform.childCount,
                    daytimeUpright=forest.trees.All(t=>Vector3.Dot(t.up,Vector3.up)>.99f)};
                lastLine="";wide=false;count=0;
                File.WriteAllText(Dir+label+"-speakers.csv","speaker,actor,cameraX,cameraY,cameraZ,headX,headY,headDepth,angle\n");
                c.StageChanged+=stage=>{if(stage==Chapter2Controller.Stage.Complete){result.complete=true;Save();}};
            }
        };
    }
    static void Save()=>File.WriteAllText(Dir+label+"-council-result.json",JsonUtility.ToJson(result,true));
    static void Tick()
    {
        if(!Application.isPlaying||!c||result==null||c.CurrentStage<Chapter2Controller.Stage.Meeting)return;
        var forest=c.GetComponent<Chapter2LoggedForest>();
        if(!wide&&c.ui.fade.color.a<.01f)
        {
            wide=true;result.maxUprightDot=forest.trees.Max(t=>Mathf.Abs(Vector3.Dot(t.up,Vector3.up)));
            result.allNightTreesHorizontal=forest.Applied&&result.maxUprightDot<.01f;
            if(!result.allNightTreesHorizontal)result.failures.Add("Standing tree remained at night");
            Chapter2WorkProbe.Shot(label+"-logged-wide");Save();
        }
        string line=c.ui.speaker.text+"|"+c.ui.subtitle.text;
        if(!c.ui.dialoguePanel.activeSelf||string.IsNullOrEmpty(c.ui.subtitle.text))return;
        if(line!=lastLine){lastLine=line;lineStarted=EditorApplication.timeSinceStartup;return;}
        if(EditorApplication.timeSinceStartup-lineStarted<.8||result.speakers.Contains(line))return;
        var actor=c.CouncilSpeaker;
        if(!actor||!actor.speaking){result.failures.Add("No matching active speaker for "+line);return;}
        var head=c.player.view.WorldToViewportPoint(actor.Rig.Head.position);
        float angle=Vector3.Angle(c.player.view.transform.forward,actor.Rig.Head.position-c.player.view.transform.position);
        float facing=Vector3.Dot(actor.Rig.Forward,Vector3.ProjectOnPlane(c.player.view.transform.position-actor.transform.position,Vector3.up).normalized);
        if(facing<.9f)result.failures.Add("Speaker is facing away: "+actor.name+" dot="+facing);
        if(head.z<=0||head.x<.18f||head.x>.82f||head.y<.3f||head.y>.85f)
            result.failures.Add("Speaker head outside readable frame: "+actor.name+" "+head);
        var position=c.player.view.transform.position;
        result.speakers.Add(line);result.speakerShots=++count;
        Chapter2WorkProbe.Shot(label+"-speaker-"+count.ToString("00"));
        File.AppendAllText(Dir+label+"-speakers.csv",c.ui.speaker.text+","+actor.name+","+position.x+","+position.y+","+position.z+","+head.x+","+head.y+","+head.z+","+angle+"\n");
        Save();
    }
}
