using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

// Temporary driver: moved outside Assets after verification. Does not save scenes.
[InitializeOnLoad]
public static class Chapter2MemoryBubbleProbe
{
    const string Dir="_CodexBackups/chapter2_bubble_palette_20261010/", Key="BubblePaletteProbe";
    static Chapter2Controller c; static Report report; static float start, next, until; static int phase,index,edge; static bool release;
    static Chapter2AmbientNPC[] npcs; static Vector3 origin,direction; static Quaternion recoveryLook;
    static Vector3[] path; static int corner; static float repath;
    [Serializable] class Report
    {
        public bool finished,passed,playing,dirty;public string error,provider,stage;public int errors,warnings;
        public List<string> checks=new List<string>();public List<string> answers=new List<string>();public float configuredSeconds;
    }
    static Chapter2MemoryBubbleProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string message,string stack,LogType type)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(type==LogType.Warning)SessionState.SetInt(Key+"w",SessionState.GetInt(Key+"w",0)+1);
        if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception)SessionState.SetInt(Key+"e",SessionState.GetInt(Key+"e",0)+1);
        if(type!=LogType.Log)File.AppendAllText(Dir+"play-log.txt",type+" "+message+"\n"+stack+"\n");
    }
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks.Add(label);}
    static void BackupPrefs()
    {
        foreach(string k in new[]{Chapter2StoryMemory.SaveKey,"Chapter1_ConflictChoice"})
        {SessionState.SetBool(Key+k+"had",PlayerPrefs.HasKey(k));SessionState.SetString(Key+k,PlayerPrefs.GetString(k,""));}
        foreach(string k in new[]{"Chapter1_PeopleInjured","Chapter1_Morale"})
        {SessionState.SetBool(Key+k+"had",PlayerPrefs.HasKey(k));SessionState.SetInt(Key+k,PlayerPrefs.GetInt(k,0));}
    }
    static void RestorePrefs()
    {
        foreach(string k in new[]{Chapter2StoryMemory.SaveKey,"Chapter1_ConflictChoice"})
            if(SessionState.GetBool(Key+k+"had",false))PlayerPrefs.SetString(k,SessionState.GetString(Key+k,""));else PlayerPrefs.DeleteKey(k);
        foreach(string k in new[]{"Chapter1_PeopleInjured","Chapter1_Morale"})
            if(SessionState.GetBool(Key+k+"had",false))PlayerPrefs.SetInt(k,SessionState.GetInt(Key+k,0));else PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){report=new Report();phase=0;index=edge=0;start=Time.realtimeSinceStartup;next=start+1;c=null;}
        if(state==PlayModeStateChange.ExitingPlayMode&&report!=null){report.stage=c?c.CurrentStage.ToString():"missing";SessionState.SetString(Key+"report",JsonUtility.ToJson(report));}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            RestorePrefs();report=JsonUtility.FromJson<Report>(SessionState.GetString(Key+"report","{}"));
            report.errors=SessionState.GetInt(Key+"e",0);report.warnings=SessionState.GetInt(Key+"w",0);
            report.playing=Application.isPlaying;report.dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
            File.WriteAllText(Dir+(SessionState.GetBool(Key+"preview",false)?"preview-result.json":"play-result.json"),JsonUtility.ToJson(report,true));SessionState.SetBool(Key,false);report=null;
        }
    }
    static void MemoryChecks()
    {
        BackupPrefs();report=new Report();
        try
        {
            var controller=Object.FindFirstObjectByType<Chapter2Controller>();
            var actors=controller.dayGroup.GetComponentsInChildren<Chapter2Actor>(true);
            var guide=controller.workers[0];var child=actors.First(a=>a.isChild);
            PlayerPrefs.DeleteKey(Chapter2StoryMemory.SaveKey);PlayerPrefs.DeleteKey("Chapter1_ConflictChoice");
            Check(Chapter2StoryMemory.ForActor(guide).Contains("沒有本輪已完成"),"direct chapter 2 does not invent chapter 1");
            Chapter2StoryMemory.BeginChapter();Chapter2StoryMemory.Complete("Intervene",true,3,2,true);
            string intervene=Chapter2StoryMemory.ForActor(guide);
            Check(intervene.Contains("警棍擊打玩家")&&!intervene.Contains("關門後傳出"),"intervene includes actual knockout, excludes watch events");
            Check(intervene.Contains("送過酒")&&intervene.Contains("分享過食物")&&intervene.Contains("完成了婚禮舞蹈"),"only completed wedding activities included");
            Check(!Chapter2StoryMemory.ForActor(child).Contains("警棍")&&!Chapter2StoryMemory.ForActor(child).Contains("實際選擇"),"child not given adult details or player choice");
            Check(Chapter2StoryMemory.ForActor(controller.officer).Contains("不是第一章"),"second chapter police not confused with perpetrators");
            foreach(var a in actors)Check(!Chapter2StoryMemory.ForActor(a).Contains("沒有確認你在場"),"authored knowledge scope: "+a.DisplayName);
            Chapter2StoryMemory.Complete("Watch",true,0,0,false);string watch=Chapter2StoryMemory.ForActor(guide);
            Check(watch.Contains("關門後傳出")&&!watch.Contains("警棍擊打玩家")&&!watch.Contains("完成了婚禮舞蹈"),"watch excludes intervene and skipped tasks");
            PlayerPrefs.SetString("Chapter1_ConflictChoice","Watch");Chapter2StoryMemory.BeginChapter();
            Check(Chapter2StoryMemory.ForActor(guide).Contains("沒有本輪已完成"),"new run masks stale legacy result");
            PlayerPrefs.DeleteKey(Chapter2StoryMemory.SaveKey);
            Check(Chapter2StoryMemory.ForActor(guide).Contains("舊版記錄只確認"),"old save safely imports choice without invented outcome");
            report.passed=true;
        }catch(Exception e){report.error=e.ToString();}
        finally{RestorePrefs();report.finished=true;File.WriteAllText(Dir+"memory-checks.json",JsonUtility.ToJson(report,true));report=null;}
    }
    static void Press(UnityEngine.InputSystem.Key k){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(k));release=true;}
    static void Chapter1SaveChecks()
    {
        BackupPrefs();report=new Report();var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var first=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/第一章新版警察.unity");bool opened=!first.isLoaded;
        try
        {
            if(opened)first=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/第一章新版警察.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(first);
            var controller=first.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Chapter1PerformanceController>(true)).First();
            var t=typeof(Chapter1PerformanceController);var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var fields=new[]{"lastChoice","doorwayStaged","deliveredWineCount","sharedFoodCount","danceFinished"}.Select(n=>t.GetField(n,flags)).ToArray();
            var before=fields.Select(f=>f.GetValue(controller)).ToArray();
            try
            {
                Check(controller.saveResultToPlayerPrefs,"first chapter scene enables saving");
                fields[1].SetValue(controller,true);fields[2].SetValue(controller,2);fields[3].SetValue(controller,1);fields[4].SetValue(controller,true);
                foreach(var choice in new[]{Chapter1PerformanceController.ConflictChoice.Intervene,Chapter1PerformanceController.ConflictChoice.Watch})
                {
                    Chapter2StoryMemory.BeginChapter();fields[0].SetValue(controller,choice);
                    t.GetMethod("SaveChapterResult",flags).Invoke(controller,null);var saved=Chapter2StoryMemory.Read();
                    Check(saved!=null&&saved.completed&&saved.doorway&&saved.choice==choice.ToString()&&saved.wineDeliveries==2&&saved.foodShares==1&&saved.danced,"real Chapter1 SaveChapterResult persists "+choice);
                }
            }finally{for(int i=0;i<fields.Length;i++)fields[i].SetValue(controller,before[i]);}
            report.passed=true;
        }catch(Exception e){report.error=e.ToString();}
        finally
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);if(opened&&first.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(first,true);
            RestorePrefs();report.finished=true;report.dirty=active.isDirty;File.WriteAllText(Dir+"chapter1-save-checks.json",JsonUtility.ToJson(report,true));report=null;
        }
    }
    static Chapter2GreetingBubble Bubble(Chapter2AmbientNPC npc)=>c.ui.canvas.GetComponentsInChildren<Chapter2GreetingBubble>(true).First(b=>b.Npc==npc);
    static void Near(Chapter2AmbientNPC npc,float distance)
    {
        for(int i=0;i<16;i++)
        {
            Vector3 pos=npc.transform.position+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*distance;
            c.player.worldBoundary.GroundHeight(pos,out float y);pos.y=y+.08f;
            if(Physics.OverlapCapsule(pos+Vector3.up*.45f,pos+Vector3.up*1.35f,.4f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.name!="Forest ground"&&!h.transform.IsChildOf(c.player.transform)))continue;
            c.player.Warp(pos,npc.transform.position);c.player.FocusOn(npc.GetComponent<Chapter2Actor>().Rig.Head.position);
            if(Bubble(npc).PlaceAndShow(true))return;
        }
        throw new Exception("No visible approach for "+npc.name);
    }
    static void Shot(string name)
    {
        var camera=c.player.view;var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Dir+name+".png",image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            if(cmd=="status")File.WriteAllText(Dir+"status.json","{\"playing\":"+(Application.isPlaying?"true":"false")+",\"failed\":"+(EditorUtility.scriptCompilationFailed?"true":"false")+",\"dirty\":"+(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty?"true":"false")+"}");
            if(cmd=="validate")MemoryChecks();
            if(cmd=="chapter1-save")Chapter1SaveChecks();
            if(cmd=="refresh")AssetDatabase.Refresh();
            if(cmd=="stop")EditorApplication.isPlaying=false;
            if((cmd=="play"||cmd=="preview")&&!Application.isPlaying)
            {BackupPrefs();SessionState.SetBool(Key+"preview",cmd=="preview");SessionState.SetBool(Key,true);SessionState.SetInt(Key+"e",0);SessionState.SetInt(Key+"w",0);EditorApplication.isPlaying=true;}
        }
        if(!Application.isPlaying||report==null||!SessionState.GetBool(Key,false))return;
        try
        {
            if(!c){c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;report.configuredSeconds=c.explorationSeconds;if(!SessionState.GetBool(Key+"preview",false))c.explorationSeconds=600;c.saveResult=false;}
            if(release){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
            if(Time.realtimeSinceStartup-start>450)throw new Exception("Playback timed out, phase "+phase);
            if(Time.realtimeSinceStartup<next)return;
            var ex=c.exploration;
            if(phase==0){Press(UnityEngine.InputSystem.Key.Space);phase=1;next=Time.realtimeSinceStartup+1;return;}
            if(phase==1)
            {
                if(!ex.Active)return;npcs=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>();
                Check(npcs.Length==9,"all nine NPCs retained");Check(!GameObject.Find("Roadside interaction"),"old green interaction rectangle absent");
                if(SessionState.GetBool(Key+"preview",false)){Near(c.workers[0].GetComponent<Chapter2AmbientNPC>(),2.7f);phase=20;next=Time.realtimeSinceStartup+.6f;return;}
                foreach(var n in npcs)n.patrolEnabled=false;
                report.provider=c.GetComponent<Chapter2LocalDialogue>().Provider;phase=2;
            }
            if(phase==2){Near(npcs[index],2.6f);phase=3;next=Time.realtimeSinceStartup+.35f;return;}
            if(phase==3)
            {
                var b=Bubble(npcs[index]);Check(b.Visible&&(b.Greeting=="早安！"||b.Greeting=="你好！"),"greeting at 2.6m: "+npcs[index].name);
                if(index==0){Shot("greeting-adult");Press(UnityEngine.InputSystem.Key.E);}else ExecuteEvents.Execute(b.ReplyButton.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                phase=4;next=Time.realtimeSinceStartup+.35f;return;
            }
            if(phase==4)
            {
                Check(ex.ChatOpen,"E/button opens conversation: "+npcs[index].name);
                Check(c.ui.canvas.GetComponentsInChildren<Chapter2GreetingBubble>(true).All(b=>!b.Visible),"greetings hidden in conversation");
                ex.CloseChat();Near(npcs[index],3.04f);phase=5;next=Time.realtimeSinceStartup+.35f;return;
            }
            if(phase==5)
            {
                Check(!Bubble(npcs[index]).Visible&&!ex.TryOpen(npcs[index]),"out-of-range hides and rejects: "+npcs[index].name);
                if(++index<npcs.Length){phase=2;return;}index=0;phase=6;
            }
            if(phase==6)
            {
                var bounds=c.player.worldBoundary.SafeBounds;
                direction=new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back,new Vector3(1,0,1).normalized,new Vector3(-1,0,1).normalized,new Vector3(1,0,-1).normalized,new Vector3(-1,0,-1).normalized}[edge];
                var p=new Vector3(direction.x==0?0:Mathf.Sign(direction.x)*(bounds.extents.x-.15f),0,direction.z==0?0:Mathf.Sign(direction.z)*(bounds.extents.z-.15f));
                c.player.worldBoundary.GroundHeight(p,out float y);p.y=y+.08f;c.player.Warp(p,p+direction*5);
                until=Time.realtimeSinceStartup+.7f;phase=7;return;
            }
            if(phase==7)
            {
                c.player.Move(direction*.12f);
                Check(c.player.worldBoundary.Inside(c.player.transform.position),"boundary contains edge "+edge);
                if(Time.realtimeSinceStartup<until)return;
                origin=c.player.transform.position;until=Time.realtimeSinceStartup+.6f;phase=8;if(edge==0)Shot("map-edge");return;
            }
            if(phase==8)
            {
                c.player.Move(-direction*.08f);if(Time.realtimeSinceStartup<until)return;
                Check(Vector3.Distance(origin,c.player.transform.position)>.25f,"can walk back from edge "+edge);
                if(++edge<8){phase=6;return;}
                c.player.Warp(new Vector3(110,-30,4),new Vector3(120,-30,4));recoveryLook=c.player.transform.rotation;phase=9;next=Time.realtimeSinceStartup+.4f;return;
            }
            if(phase==9)
            {
                Check(c.player.worldBoundary.Inside(c.player.transform.position)&&c.player.transform.position.y>-1,"outside/fallen position recovers to ground");
                Check(Quaternion.Angle(recoveryLook,c.player.transform.rotation)<.1f,"recovery preserves view direction");
                Check(c.player.worldBoundary.Recoveries==1,"normal boundary blocking does not teleport player");
                var child=npcs.First(n=>n.GetComponent<Chapter2Actor>().isChild);Near(child,2.5f);phase=10;next=Time.realtimeSinceStartup+.3f;return;
            }
            if(phase==10)
            {
                Shot("greeting-child");Chapter2StoryMemory.BeginChapter();Chapter2StoryMemory.Complete("Intervene",true,3,2,true);
                var npc=c.workers[0].GetComponent<Chapter2AmbientNPC>();Near(npc,2.5f);Check(ex.TryOpen(npc),"guide chat opens for memory test");
                ex.QuestionInput.text="婚禮時我做了什麼？後來我怎麼了？";
                var send=ex.QuestionInput.transform.parent.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Send question");
                ExecuteEvents.Execute(send.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                phase=11;return;
            }
            if(phase==11)
            {
                if(ex.WaitingForReply||ex.LastReply==null&&ex.LastError==null)return;
                Check(ex.LastError==null,"cloud/local intervene memory answer succeeds");report.answers.Add(ex.LastReply);Shot("memory-intervene");
                ex.CloseChat();Chapter2StoryMemory.Complete("Watch",true,0,0,false);
                var npc=npcs.First(n=>n.GetComponent<Chapter2Actor>().DisplayName=="帖木·阿班");Near(npc,2.5f);ex.TryOpen(npc);
                ex.SubmitQuestion("婚禮時我選擇怎麼做？後來發生什麼？");phase=12;return;
            }
            if(phase==12)
            {
                if(ex.WaitingForReply||ex.LastReply==null&&ex.LastError==null)return;
                Check(ex.LastError==null,"cloud/local watch memory answer succeeds");report.answers.Add(ex.LastReply);Shot("memory-watch");
                ex.CloseChat();var npc=npcs.First(n=>n.GetComponent<Chapter2Actor>().isChild);Near(npc,2.5f);ex.TryOpen(npc);
                Check(ex.SubmitQuestion("你親眼看見我在婚禮做了什麼嗎？"),"child final question sent");
                typeof(Chapter2Exploration).GetField("deadline",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ex,Time.unscaledTime+.15f);
                phase=13;return;
            }
            if(phase==13)
            {
                if(ex.WaitingForReply){if(ex.RemainingSeconds<=0)Check(ex.Active&&ex.ChatOpen,"expiry waits for in-flight answer");return;}
                if(ex.LastError!=null)throw new Exception(ex.LastError);
                if(ex.LastReply!=null&&report.answers.Count==2){report.answers.Add(ex.LastReply);Shot("memory-child");}
                if(ex.Active)return;Check(report.answers.Count==3,"three real memory replies recorded");
                Check(c.ui.canvas.GetComponentsInChildren<Chapter2GreetingBubble>(true).All(b=>!b.Visible),"expiry hides greetings");phase=14;return;
            }
            if(phase==14)
            {
                if(Time.frameCount%60==0)File.WriteAllText(Dir+"escort-progress.json",JsonUtility.ToJson(new EscortProgress{player=c.player.transform.position,guide=c.workers[0].transform.position,arrived=c.routeGuide.EscortArrived,leading=c.forestEscort.Leading,canMove=c.player.canMove,corner=corner}));
                if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice)
                {Check(true,"normal guide reaches tree choice after exploration");Shot("tree-arrival");report.finished=report.passed=true;EditorApplication.isPlaying=false;return;}
                if(c.player.canMove && !ex.Active)
                {
                    Vector3 target=c.routeGuide.EscortArrived?c.treeApproach.position:c.workers[0].transform.position;
                    if(path==null||Time.unscaledTime>repath){c.forestEscort.TryPath(c.player.transform.position,target,out path);corner=1;repath=Time.unscaledTime+.5f;}
                    if(path!=null&&corner<path.Length)
                    {var delta=Vector3.ProjectOnPlane(path[corner]-c.player.transform.position,Vector3.up);if(delta.magnitude<.2f&&corner<path.Length-1)corner++;else if(delta.magnitude>.08f)c.player.Move(Vector3.ClampMagnitude(delta,Time.deltaTime*2.6f));}
                }
            }
            if(phase==20){Check(c.explorationSeconds==180,"preview uses unchanged 180 seconds");Shot("greeting-final-preview");Press(UnityEngine.InputSystem.Key.P);phase=21;next=Time.realtimeSinceStartup+7;return;}
            if(phase==21)
            {
                if(c.ui.fade.color.a>.02f)return;
                Check(c.CurrentStage==Chapter2Controller.Stage.Meeting&&!ex.Active,"P shortcut reaches meeting");
                Check(c.ui.canvas.GetComponentsInChildren<Chapter2GreetingBubble>(true).All(b=>!b.Visible),"P shortcut hides every greeting");
                Shot("meeting-no-greetings");report.finished=report.passed=true;EditorApplication.isPlaying=false;return;
            }
        }
        catch(Exception e){if(c)Shot("diagnostic-failure");report.error=e.ToString()+" Player="+(c?c.player.transform.position.ToString():"missing")+" NPC="+(npcs!=null&&index<npcs.Length?npcs[index].transform.position.ToString():"missing");report.finished=true;EditorApplication.isPlaying=false;}
    }
    [Serializable] class EscortProgress{public Vector3 player,guide;public bool arrived,leading,canMove;public int corner;}
}
