using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static partial class Chapter3Batch7Probe
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string SecondKey="WusheEvent.Chapter2.Result";
    static void PrepareScenarioScene(string mode)
    {
        string path=mode.StartsWith("test-entry-")?"Assets/Scenes/第二章.unity":"Assets/Scenes/第三章.unity";
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty)throw new Exception("Save or resolve the existing scene edits before verification");
        if(scene.path!=path)EditorSceneManager.OpenScene(path);
    }
    static IEnumerator EntryScenario(string mode)
    {
        var chapter=Object.FindFirstObjectByType<Chapter2Controller>();
        Check("second chapter is the real loaded scene",chapter&&SceneManager.GetActiveScene().path=="Assets/Scenes/第二章.unity");
        if(mode=="test-entry-save")
        {yield return EntrySaveFailure(chapter);yield break;}
        chapter.SkipToMeeting();
        bool preview=mode.Contains("preview");
        if(!preview)
        {
            // Isolate the chapter boundary with a completed-daytime precondition;
            // the original council, choice, fade, save and load run unmodified.
            typeof(Chapter2Controller).GetField("<TreeDecision>k__BackingField",Fields).SetValue(chapter,mode.Contains("refuse")?1:0);
            typeof(Chapter2Controller).GetField("meetingShortcutUsed",Fields).SetValue(chapter,false);
        }
        report.observations.Add("Council driven through its actual coroutine and UI choices; daytime completion precondition injected for formal saves, P path retained for preview. Council clock accelerated 3x; no whole Chapter 2 or headset claim.");
        Time.timeScale=3;
        yield return Chapter2EntryVerification.CouncilToThirdChapter(chapter,mode.Contains("refuse")?1:0,preview,Check);
        yield return Wait(()=>Object.FindFirstObjectByType<Chapter3Flow>()?.station.Opening?.Active==true,15,"third chapter opening after previous chapter");
        CheckSceneSystems("chapter entry");Shot(mode+"-third-chapter");
    }
    static IEnumerator EntrySaveFailure(Chapter2Controller chapter)
    {
        chapter.SkipToMeeting();chapter.StopAllCoroutines();
        typeof(Chapter2Controller).GetField("<TreeDecision>k__BackingField",Fields).SetValue(chapter,0);
        typeof(Chapter2Controller).GetField("<MeetingDecision>k__BackingField",Fields).SetValue(chapter,0);
        typeof(Chapter2Controller).GetField("<CurrentStage>k__BackingField",Fields).SetValue(chapter,Chapter2Controller.Stage.Complete);
        typeof(Chapter2Controller).GetField("meetingShortcutUsed",Fields).SetValue(chapter,false);
        var method=typeof(Chapter2Controller).GetMethod("TrySaveChapter2Result",Fields,null,new[]{typeof(Action)},null);
        Check("second chapter save failure injection exists",method!=null);
        foreach(bool existing in new[]{false,true})
        {
            if(existing)PlayerPrefs.SetString(SecondKey,"prior second chapter");else PlayerPrefs.DeleteKey(SecondKey);
            bool result=(bool)method.Invoke(chapter,new object[]{(Action)(()=>{throw new PlayerPrefsException("injected flush failure");})});
            Check("second chapter failed flush restores "+(existing?"existing key":"absent key"),!result&&!chapter.ResultSaved&&PlayerPrefs.HasKey(SecondKey)==existing&&(!existing||PlayerPrefs.GetString(SecondKey)=="prior second chapter"));
            Check("failed second chapter save stays with visible retry",!chapter.TransitionInProgress&&chapter.TransitionError!=null&&chapter.ui.endingPanel.activeSelf&&chapter.ui.continueButton.gameObject.activeInHierarchy&&chapter.ui.continueButton.interactable);
        }
        chapter.ui.continueButton.onClick.Invoke();
        yield return Wait(()=>SceneManager.GetActiveScene().name=="第三章",20,"second chapter successful retry loads next chapter");
        var resultRecord=JsonUtility.FromJson<Chapter2Controller.Chapter2Result>(PlayerPrefs.GetString(SecondKey));
        Check("retry commits valid second chapter result",resultRecord.completed&&resultRecord.treeChoice=="protect"&&resultRecord.meetingChoice=="support");
        Check("second chapter retry retains prior third chapter result",OriginalSaveMatches());
        report.observations.Add("Save failure and completed-stage prerequisites injected. Public retry button, normal persistence and actual scene loading executed; not an operating-system disk failure.");
    }
    static IEnumerator DialogueScenario(Chapter3Flow f)
    {
        yield return Chapter3Batch7DialogueChecks.Run(f,Check,m=>report.observations.Add(m),Shot);
        Check("dialogue faults never overwrite completed save",OriginalSaveMatches());
    }
    static IEnumerator ReplayScenario(Chapter3Flow f,string mode,string completedSave)
    {
        Check("complete ending presents replay and menu",f.ui.ending.activeSelf&&f.ui.replay.gameObject.activeInHierarchy&&f.ui.menu.gameObject.activeInHierarchy);
        int original=f.GetInstanceID();
        if(mode.Contains("pihu"))
        {
            f.ui.menu.onClick.Invoke();
            yield return Wait(()=>SceneManager.GetActiveScene().name=="選擇章節ˊ",20,"ending returns to chapter menu");
            yield return null;
            Check("menu preserves completed result",PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)==completedSave);
            var buttons=Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            var chapterButton=buttons.FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="JumpToChapter3"));
            Check("one visible third-chapter menu entry",buttons.Count(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="JumpToChapter3"))==1);
            Check("third chapter menu button is usable",chapterButton&&chapterButton.isActiveAndEnabled&&chapterButton.interactable);
            Shot(mode+"-chapter-menu");chapterButton.onClick.Invoke();
        }
        else f.ui.replay.onClick.Invoke();
        yield return Wait(()=>{var fresh=Object.FindFirstObjectByType<Chapter3Flow>();return fresh&&fresh.GetInstanceID()!=original&&fresh.station.Opening&&fresh.station.Opening.Active;},25,"actual third-chapter reload");
        var next=Object.FindFirstObjectByType<Chapter3Flow>();
        Check("replay starts at original station without stale equipment",next.Region=="Station"&&!next.CarriedWeapon.HasValue&&!next.station.HasWeapon&&next.station.Opening.CompletedTurns==0);
        Check("replay preserves completed result",PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)==completedSave&&Chapter3ResultStore.Load()!=null);
        CheckSceneSystems("replay");Shot(mode+"-replayed-opening");
    }
    static void CheckSceneSystems(string label)
    {
        Check(label+" has one camera",Camera.allCameras.Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1);
        Check(label+" has one audio listener",Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled&&a.gameObject.activeInHierarchy)==1);
        Check(label+" has one EventSystem",Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(e=>e.enabled&&e.gameObject.activeInHierarchy)==1);
    }
    static IEnumerator LifecycleScenario(Chapter3Flow f)
    {
        yield return Wait(()=>f.station.Opening&&f.station.Opening.Active,15,"lifecycle opening");
        f.station.Suspend();
        var ui=f.ui;var rect=(RectTransform)ui.canvas.transform;
        var parent=rect.parent;var pos=rect.localPosition;var rotation=rect.localRotation;var scale=rect.localScale;
        var size=rect.sizeDelta;var min=rect.anchorMin;var max=rect.anchorMax;var pivot=rect.pivot;var mode=ui.canvas.renderMode;
        ui.ConfigureVR(true);
        Check("VR presentation uses existing head-relative canvas",ui.canvas.renderMode==RenderMode.WorldSpace&&rect.parent==ui.canvas.worldCamera.transform&&Vector3.Distance(rect.localScale,Vector3.one*.0023f)<.000001f);
        ui.ConfigureVR(false);
        Check("leaving VR restores desktop canvas parent and mode",rect.parent==parent&&ui.canvas.renderMode==mode);
        Check("leaving VR restores desktop canvas geometry",Vector3.Distance(rect.localPosition,pos)<.001f&&Quaternion.Angle(rect.localRotation,rotation)<.01f&&Vector3.Distance(rect.localScale,scale)<.00001f&&Vector2.Distance(rect.sizeDelta,size)<.01f&&rect.anchorMin==min&&rect.anchorMax==max&&rect.pivot==pivot);
        yield return null;Canvas.ForceUpdateCanvases();
        Check("desktop canvas retains equal scale after layout",Mathf.Abs(rect.localScale.x-rect.localScale.y)<.001f);
        CheckSceneSystems("presentation mode roundtrip");Shot("lifecycle-desktop-restored");
        var storeMethod=typeof(Chapter3ResultStore).GetMethod("SaveCompleted",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Chapter3ResultStore.Record),typeof(Action)},null);
        var pending=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Bawan,Chapter3Rescue.Decision.Rescue,Chapter3Weapons.Weapon.Knife);
        Check("save failure injection path exists",storeMethod!=null);
        foreach(bool existing in new[]{false,true})
        {
            if(existing)PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,"prior completed data");else PlayerPrefs.DeleteKey(Chapter3ResultStore.SaveKey);
            bool result=(bool)storeMethod.Invoke(null,new object[]{pending,(Action)(()=>{throw new PlayerPrefsException("injected flush failure");})});
            Check("failed flush restores "+(existing?"existing result":"absent key"),!result&&PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey)==existing&&(!existing||PlayerPrefs.GetString(Chapter3ResultStore.SaveKey)=="prior completed data"));
        }
        RestoreSave();Check("save failure checks restore user result",OriginalSaveMatches());BackupSave(report.mode);
        foreach(string region in new[]{"Combat","School","Station"})
        {
            if(region=="Combat")f.EnterCombat();else if(region=="School")f.EnterSchool();else f.EnterStation();
            yield return new WaitForSecondsRealtime(.3f);
            Check("region switch "+region+" activates only its region",f.Region==region&&new[]{f.stationRegion,f.combatRegion,f.schoolRegion}.Count(g=>g.activeSelf)==1);
            CheckSceneSystems("region "+region);
        }
        report.observations.Add("ConfigureVR toggled directly without an XR headset. Save failure injected at the persistence delegate; no OS disk failure induced. Desktop and XR input parity is not established.");
    }
    [Serializable] class BuildInfo
    {public string result,outputPath,backend;public string[] scenes;public long dependencyBytes,freeBytes;public double seconds;public ulong size;public int errors,warnings;public string[] messages;}
    static string[] BuildScenes()=>EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
    static void BuildPreflight()
    {
        var scenes=BuildScenes();var dependencies=AssetDatabase.GetDependencies(scenes,true).Where(File.Exists).Distinct().ToArray();
        var info=new BuildInfo{result="preflight",scenes=scenes,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),dependencyBytes=dependencies.Sum(p=>new FileInfo(p).Length),freeBytes=new DriveInfo(Path.GetPathRoot(Path.GetFullPath(Dir))).AvailableFreeSpace};
        File.WriteAllText(Dir+"build-preflight.json",JsonUtility.ToJson(info,true));
    }
    static void BuildPlayer()
    {
        if(EditorApplication.isPlaying||EditorUtility.scriptCompilationFailed)throw new Exception("Build requires non-Play, compiled Editor");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Build must not implicitly save an edited scene");
        BuildPreflight();
        var preflight=JsonUtility.FromJson<BuildInfo>(File.ReadAllText(Dir+"build-preflight.json"));
        if(preflight.freeBytes<Math.Max(8L*1024*1024*1024,preflight.dependencyBytes*2+4L*1024*1024*1024))throw new Exception("Insufficient build headroom: inspect preflight before choosing another output");
        string path=Path.GetFullPath(Dir+"build/Wushe.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(Dir+"build-started.txt",DateTime.UtcNow.ToString("O"));
        var built=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=BuildScenes(),locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.DetailedBuildReport});
        var summary=built.summary;
        var info=new BuildInfo{result=summary.result.ToString(),outputPath=path,scenes=BuildScenes(),backend=preflight.backend,seconds=summary.totalTime.TotalSeconds,size=summary.totalSize,errors=(int)summary.totalErrors,warnings=(int)summary.totalWarnings,messages=built.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()};
        File.WriteAllText(Dir+"build-result.json",JsonUtility.ToJson(info,true));
    }
}
