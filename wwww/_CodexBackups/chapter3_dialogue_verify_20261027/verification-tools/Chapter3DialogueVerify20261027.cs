using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Temporary verification of the user's reported missing dialogue. Archive after use.
[InitializeOnLoad]
public static class Chapter3DialogueVerify20261027
{
    const string Dir="_CodexBackups/chapter3_dialogue_verify_20261027/",Key="Chapter3DialogueVerify20261027";
    static Chapter3DialogueVerifyDriver20261027 driver;
    static Chapter3Flow flow;
    static Report report;
    static bool oldBackground;
    [Serializable] sealed class Report
    {
        public bool completed;
        public string scene;
        public List<string> checks=new List<string>(),observations=new List<string>(),errors=new List<string>();
    }
    static Chapter3DialogueVerify20261027()
    {
        Directory.CreateDirectory(Dir);
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string command=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            if(command=="run"&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity"&&!SceneManager.GetActiveScene().isDirty)
            {SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
            else if(command=="stop"){SessionState.EraseBool(Key);EditorApplication.isPlaying=false;}
            else File.WriteAllText(Dir+"command-error.txt","Not a clean, compiled third chapter in Edit Mode");
        }
        if(Application.isPlaying&&SessionState.GetBool(Key,false)&&!driver)
            driver=new GameObject("Temporary dialogue verification").AddComponent<Chapter3DialogueVerifyDriver20261027>();
    }
    public static IEnumerator Run()
    {
        report=new Report{scene=SceneManager.GetActiveScene().path};oldBackground=Application.runInBackground;Application.runInBackground=true;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){report.errors.Add(failure.ToString());break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        report.completed=report.errors.Count==0;File.WriteAllText(Dir+"report.json",JsonUtility.ToJson(report,true));
        Application.runInBackground=oldBackground;SessionState.EraseBool(Key);EditorApplication.isPlaying=false;
    }
    static IEnumerator Scenario()
    {
        yield return null;yield return null;flow=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal Chapter3Flow",flow&&flow.startAtStation);
        var station=flow.station;var ui=flow.ui;
        yield return Wait(()=>station.Opening&&station.Opening.Active,30,"opening");
        var service=(Chapter2LocalDialogue)typeof(Chapter3OpeningConversation).GetField("dialogue",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(station.Opening);
        report.observations.Add("Editor driver: runtime-only Busy=true forces the authored error fallback without sending a question to the LLM; no service settings changed.");
        service.CancelRequest();Set(service,"Busy",true);
        Check("affirm input accepted",station.Opening.SubmitReply("好"));
        yield return Wait(()=>!station.Opening.WaitingForReply,8,"affirm fallback");Set(service,"Busy",false);
        Check("affirm fallback actually displayed",station.Opening.FallbackReplies==1&&ui.response.text.Contains("好，保持間距，別烙單了!"));
        yield return Shot("01-opening-affirm");
        Set(service,"Busy",true);Check("unknown input accepted",station.Opening.SubmitReply("請回答未知代碼XYZ"));
        yield return Wait(()=>!station.Opening.WaitingForReply,8,"unknown fallback");Set(service,"Busy",false);
        Check("unknown fallback actually displayed",station.Opening.FallbackReplies==2&&ui.response.text.Contains("這個問題我現在答不上來。先去見莫那，聽聽他的安排。"));
        yield return Shot("02-opening-unknown");
        float started=Time.realtimeSinceStartup;
        while(station.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
        {
            if(Time.realtimeSinceStartup-started>120)throw new Exception("Timed out following Bawan to weapon choice");
            if(flow.player.canMove)
            {
                Vector3 target=station.bawan.transform.position-Vector3.forward*2.3f;
                var delta=Vector3.ProjectOnPlane(target-flow.player.transform.position,Vector3.up);
                flow.player.Move(Vector3.ClampMagnitude(delta,flow.player.speed*Time.deltaTime)+Vector3.down*.05f);
            }
            yield return null;
        }
        Check("knife choice accepted",station.ChooseWeapon(Chapter3Weapons.Weapon.Knife));
        report.observations.Add("At each selected weapon table, player positioning uses Warp, then the real TakeKnife/TakeRifle -> ConfirmDeparture -> Readiness.Begin path.");
        flow.player.Warp(station.knifeFocus.position+Vector3.back*.8f,station.knifeFocus.position);
        Check("knife pickup accepted",station.TakeKnife());
        yield return Wait(()=>station.Readiness.Active&&!station.Readiness.WaitingForOpeningRelease,8,"knife readiness");
        Check("knife greeting actually displayed",station.Readiness.LastReply=="手握獵刀，接下來就要與日本決戰了，你準備好了嗎？"&&ui.response.text.Contains(station.Readiness.LastReply));
        yield return Shot("03-knife-readiness");
        flow.EnterStation();yield return Wait(()=>station.Opening.Active,20,"station reset");
        station.Opening.Cancel();station.StopAllCoroutines();ui.chat.SetActive(false);
        Set(station,"CurrentStage",Chapter3StationController.Stage.WeaponChoice);
        report.observations.Add("Second weapon branch uses a runtime-only WeaponChoice stage fixture to avoid replaying the already observed escort.");
        Check("rifle choice accepted",station.ChooseWeapon(Chapter3Weapons.Weapon.Rifle));
        flow.player.Warp(station.rifleFocus.position+Vector3.back*.8f,station.rifleFocus.position);
        Check("rifle pickup accepted",station.TakeRifle());
        yield return Wait(()=>station.Readiness.Active&&!station.Readiness.WaitingForOpeningRelease,8,"rifle readiness");
        Check("rifle greeting actually displayed",station.Readiness.LastReply=="拿穩步槍，接下來就要與日本決戰了，你準備好了嗎？"&&ui.response.text.Contains(station.Readiness.LastReply));
        yield return Shot("04-rifle-readiness");
        var panel=(RectTransform)ui.subtitlePanel.transform;Vector2 originalSize=panel.sizeDelta;Vector3 originalPosition=panel.anchoredPosition3D;
        flow.EnterSchool();yield return Wait(()=>flow.school.Exploring,30,"school arrival");
        report.observations.Add("School entry uses EnterSchool; player positioned within 3 metres of Tado; SchoolController.Update triggers the normal proximity briefing.");
        flow.player.Warp(flow.school.tado.transform.position+Vector3.back*2,flow.school.tado.transform.position+Vector3.up*1.2f);
        yield return Wait(()=>ui.subtitle.text.StartsWith("前面就是學校了"),10,"Tado briefing");
        string expected="前面就是學校了，裡面正在舉行運動會，大家都埋伏好了。\n等等聽到信號，我會進去運動場。巴萬打通左側通道，比胡負責右側掩護。你選一隊跟上。";
        Check("Tado full string and newline",ui.subtitle.text==expected&&ui.speaker.text==flow.school.tado.DisplayName);
        yield return Shot("05-tado-briefing");
        Canvas.ForceUpdateCanvases();
        report.observations.Add("Tado subtitle rect="+ui.subtitle.rectTransform.rect+" preferredHeight="+ui.subtitle.preferredHeight+" font="+ui.subtitle.fontSize+" generatedLines="+ui.subtitle.cachedTextGenerator.lineCount+" visibleCharacters="+ui.subtitle.cachedTextGenerator.characterCountVisible+" textLength="+expected.Length);
        Check("Tado text fits subtitle height",ui.subtitle.preferredHeight<=ui.subtitle.rectTransform.rect.height+.5f);
        Check("Tado font unchanged",ui.subtitle.fontSize==30);
        yield return Wait(()=>ui.subtitle.text.StartsWith("跟緊各自的領隊"),18,"following Bawan subtitle");
        Check("next line restores subtitle panel",Vector2.Distance(panel.sizeDelta,originalSize)<.01f&&Vector3.Distance(panel.anchoredPosition3D,originalPosition)<.01f);
        yield return Wait(()=>flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,10,"mission choice");
        Check("clear keeps original subtitle panel",!ui.subtitlePanel.activeSelf&&Vector2.Distance(panel.sizeDelta,originalSize)<.01f);
        Check("runtime scene remains clean",!SceneManager.GetActiveScene().isDirty);
    }
    static void Set(object target,string property,object value)
    {target.GetType().GetField("<"+property+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
    static void Check(string name,bool passed){if(!passed)throw new Exception("FAILED: "+name);report.checks.Add(name);File.WriteAllText(Dir+"progress.txt",name);}
    static IEnumerator Wait(Func<bool> condition,float timeout,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var camera=flow.player.view;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Dir+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(texture);}
        report.observations.Add(name+": "+(flow.ui.chat.activeInHierarchy?flow.ui.response.text:flow.ui.subtitle.text));
    }
}
public sealed class Chapter3DialogueVerifyDriver20261027:MonoBehaviour
{IEnumerator Start(){yield return Chapter3DialogueVerify20261027.Run();}}
