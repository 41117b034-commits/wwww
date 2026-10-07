using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter2StreakReview
{
    const string Dir="_CodexBackups/chapter2_progress_streak_20261007/";
    static readonly int[] ExpectedValid={0,0,1,1,1,2,2,2,2,0,0,1,1,1,2,3,4,5};
    static readonly int[] ExpectedStreak={1,2,0,1,2,0,1,2,3,1,2,0,1,2,0,0,0,0};
    static Chapter2Controller c;
    static string label;
    static int impacts;
    static bool initial,warningVisible,retrySeen;
    [Serializable] class Result
    {
        public int warnings,impacts;
        public bool initialZero,resetAfterWarning,completed;
        public List<string> failures=new List<string>();
    }
    static Result result;
    static Chapter2StreakReview()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode)
            {
                label=SessionState.GetString("Chapter2PerformanceTest","");
                if(string.IsNullOrEmpty(label))return;
                c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
                impacts=0;initial=warningVisible=retrySeen=false;result=new Result();
                File.WriteAllText(Dir+label+"-streak.csv","impacts,valid,failed,consecutive,integrity,label,fill,barAnchorMaxX\n");
                c.StageChanged+=stage=>{
                    if(stage==Chapter2Controller.Stage.Complete)
                    {
                        result.completed=true;result.impacts=impacts;
                        Check(impacts==18,"Expected 18 real axe contacts");
                        Check(result.warnings==1,"Expected exactly one warning after the third consecutive mistake");
                        Check(result.resetAfterWarning,"Retry must reset valid hits, streak and progress");
                        Save();
                    }
                };
            }
        };
    }
    static void Check(bool passed,string message){if(!passed)result.failures.Add(message);}
    static void Save()=>File.WriteAllText(Dir+label+"-streak-result.json",JsonUtility.ToJson(result,true));
    static void Tick()
    {
        if(!Application.isPlaying||!c||result==null)return;
        var axe=c.axe.GetComponent<Chapter2Axe>();
        if(c.CurrentStage==Chapter2Controller.Stage.Chopping)
        {
            if(!initial&&c.ui.meterPanel.activeSelf)
            {
                initial=true;result.initialZero=c.ValidCuts==0&&c.ui.meter.fillAmount==0;
                Check(result.initialZero,"Initial progress must be zero");
                Capture("initial-zero");
            }
            if(axe.Impacts>impacts)
            {
                impacts=axe.Impacts;int index=impacts-1;
                Check(index<ExpectedValid.Length,"Unexpected extra impact");
                if(index<ExpectedValid.Length)
                {
                    Check(c.ValidCuts==ExpectedValid[index],"Valid count mismatch at contact "+impacts);
                    Check(c.ConsecutiveFailedCuts==ExpectedStreak[index],"Consecutive count mismatch at contact "+impacts);
                    int percent=ExpectedValid[index]*20;
                    Check(c.ui.meterLabel.text.Contains("砍伐完成度 "+percent+"%"),"Label mismatch at contact "+impacts);
                    Check(Mathf.Abs(c.ui.meter.fillAmount-percent/100f)<.001f,"Fill mismatch at contact "+impacts);
                }
                File.AppendAllText(Dir+label+"-streak.csv",impacts+","+c.ValidCuts+","+c.FailedCuts+","+c.ConsecutiveFailedCuts+","+c.Integrity+","+c.ui.meterLabel.text+","+c.ui.meter.fillAmount+","+c.ui.meter.rectTransform.anchorMax.x+"\n");
                if(c.ui.meterPanel.activeSelf)Capture("contact-"+impacts);
            }
            if(!retrySeen&&impacts==9&&c.ui.meterPanel.activeSelf&&c.ConsecutiveFailedCuts==0)
            {
                retrySeen=true;result.resetAfterWarning=c.ValidCuts==0&&c.ui.meter.fillAmount==0;
                Capture("retry-zero");Save();
            }
        }
        bool warning=c.ui.dialoguePanel.activeSelf&&c.ui.subtitle.text=="木材不能再受損！放慢動作，重新找準落點。";
        if(warning&&!warningVisible)
        {
            result.warnings++;
            Check(axe.Impacts==9&&c.ConsecutiveFailedCuts==3,"Warning happened without three consecutive incorrect contacts");
            Capture("third-consecutive-warning");Save();
        }
        warningVisible=warning;
    }
    static void Capture(string name){Canvas.ForceUpdateCanvases();Chapter2WorkProbe.Shot(label+"-"+name);}
}
