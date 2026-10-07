using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2CouncilReview
{
    const string Dir="_CodexBackups/chapter2_council_rally_20261007/";
    static Chapter2Controller c;
    static string label,lastLine;
    static double lineStarted;
    static int count;
    static bool wide;
    static float revealedAt,firstShotAt,standAt;
    static bool rallyShot,standingShot,standingMidShot;
    static Result result;
    [Serializable]class Result
    {
        public int trees,timberStacks,speakerShots;
        public bool daytimeUpright,allNightTreesHorizontal,complete;
        public float maxUprightDot;
        public int uniqueCouncilModels;
        public float openingHold,handAboveHead;
        public bool supportersRemainSeatedDuringSpeech,allLeadersStanding,refusalStayedSeated;
        public float minimumChairClearance;
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
                lastLine="";wide=false;count=0;revealedAt=firstShotAt=standAt=-1;rallyShot=standingShot=standingMidShot=false;
                result.uniqueCouncilModels=c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true).Select(a=>string.Join(";",a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>AssetDatabase.GetAssetPath(s.sharedMesh)))).Distinct().Count();
                if(result.uniqueCouncilModels!=9)result.failures.Add("Council actors reuse a model");
                File.WriteAllText(Dir+label+"-speakers.csv","speaker,actor,cameraX,cameraY,cameraZ,headX,headY,headDepth,angle\n");
                c.StageChanged+=stage=>{if(stage==Chapter2Controller.Stage.Complete){result.complete=true;result.refusalStayedSeated=c.MeetingDecision==1&&c.leaders.All(a=>a.seated);Save();}};
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
        if(c.ui.fade.color.a<.001f&&revealedAt<0)revealedAt=Time.time;
        if(firstShotAt<0&&c.CouncilSpeaker)
        {
            firstShotAt=Time.time;result.openingHold=firstShotAt-revealedAt-.7f;
            if(result.openingHold<2.9f)result.failures.Add("Opening overview shorter than 3 seconds: "+result.openingHold);
        }
        var gesture=c.mona.GetComponent<Chapter2RallyGesture>();
        if(gesture&&gesture.Weight>.95f&&!rallyShot)
        {
            rallyShot=true;result.handAboveHead=c.mona.Rig.RightHand.position.y-c.mona.Rig.Head.position.y;
            result.supportersRemainSeatedDuringSpeech=c.leaders.All(a=>a.seated);
            if(result.handAboveHead<.15f)result.failures.Add("Rally hand is not clearly above the head");
            if(!result.supportersRemainSeatedDuringSpeech)result.failures.Add("Supporters stood before the speech ended");
            Chapter2WorkProbe.Shot(label+"-rally-raised-hand");Save();
        }
        if(c.CouncilStandingTogether)
        {
            if(standAt<0)standAt=Time.time;
            if(Time.time-standAt>.7f&&!standingMidShot){standingMidShot=true;Chapter2WorkProbe.Shot(label+"-standing-midway");}
            if(Time.time-standAt>2.1f&&!standingShot)
            {
                standingShot=true;result.allLeadersStanding=c.leaders.All(a=>!a.seated&&a.SeatWeight<.01f);
                if(!result.allLeadersStanding)result.failures.Add("Some leaders did not finish standing");
                var seats=c.nightGroup.GetComponentsInChildren<Transform>().Where(t=>t.name=="Council seat"||t.name=="Mona seat").ToArray();
                result.minimumChairClearance=c.leaders.Append(c.mona).Min(a=>seats.Min(s=>Vector3.ProjectOnPlane(a.transform.position-s.position,Vector3.up).magnitude));
                if(result.minimumChairClearance<.4f)result.failures.Add("Standing actor remains inside the chair");
                Chapter2WorkProbe.Shot(label+"-all-standing");Save();
            }
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

