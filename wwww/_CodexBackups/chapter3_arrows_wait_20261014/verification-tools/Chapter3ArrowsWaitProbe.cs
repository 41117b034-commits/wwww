using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Temporary focused evidence harness. Keep the archived source outside Assets.
[InitializeOnLoad]
public static class Chapter3ArrowsWaitProbe
{
    const string Dir="_CodexBackups/chapter3_arrows_wait_20261014/",KeyName="Chapter3ArrowsWaitProbe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>(),observations=new List<string>();}
    static Report report;static Chapter3ArrowsWaitProbeDriver driver;static float began,progressAt;
    static Chapter3ArrowsWaitProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
    static void Log(string m,string stack,LogType t){if(report==null)return;if(t==LogType.Warning)report.warnings.Add(m);if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)report.errors.Add(m+"\n"+stack);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="audit")Audit();
                else if(cmd=="stop"){SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){SessionState.SetString(KeyName,cmd);EditorApplication.isPlaying=true;}
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary arrow and waiting verification").AddComponent<Chapter3ArrowsWaitProbeDriver>();
    }
    public static IEnumerator Run()
    {
        Application.runInBackground=true;yield return null;report=new Report{mode=SessionState.GetString(KeyName,"")};began=Time.realtimeSinceStartup;
        yield return Guard(Scenario(Object.FindFirstObjectByType<Chapter3Flow>(),report.mode));
        if(report!=null){report.completed=true;Finish();}
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool moved=false;object value=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());Shot("failure-"+report.mode);Finish();yield break;}
            if(!moved){stack.Pop();continue;}if(value is IEnumerator sub){stack.Push(sub);continue;}yield return value;
        }
    }
    static IEnumerator Scenario(Chapter3Flow f,string mode)
    {
        Check("main chapter scene loaded",f&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity");
        if(mode.StartsWith("test-station-")){yield return StationArrows(f,mode.EndsWith("knife"));yield break;}
        if(mode=="test-school"){yield return Press(Key.Space);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"Space sports entrance");}
        else
        {
            yield return CombatWait(f);
            if(mode=="test-wait")yield break;
        }
        yield return SchoolGuide(f);
    }
    static IEnumerator CombatWait(Chapter3Flow f)
    {
        var c=f.combat;var p=f.player;f.Carry(Chapter3Weapons.Weapon.Rifle);c.lineSeconds=.3f;
        yield return Press(Key.F7);yield return Wait(()=>c.Fighting,12,"short combat");
        Check("three patrol officers active",c.Enemies.Count==3&&c.Enemies.All(e=>!e.Defeated));
        foreach(var enemy in c.Enemies.ToArray())yield return Shoot(f,enemy);
        yield return Wait(()=>c.CurrentStage==Chapter3Combat.Stage.Cleared&&!c.hud.activeSelf,8,"patrol cleared");
        Check("postcombat transition from real rifle attacks",c.DefeatedCount==3&&c.weapons.Shots==3);
        yield return Wait(()=>c.EscortWaiting&&p.canMove,25,"player lags behind escort");
        yield return new WaitForSeconds(2.5f);
        float stoppedGap=Flat(p.transform.position,c.bawan.transform.position);
        Check("escort stops once gap exceeds 5.5 metres",stoppedGap>5.5f&&stoppedGap<5.9f);
        CheckFacing(c.bawan,p,"first waiting Bawan");CheckFacing(c.pihu,p,"first waiting Pihu");
        var bawanAtWait=c.bawan.transform.position;var pihuAtWait=c.pihu.transform.position;
        p.FocusOn(Vector3.Lerp(c.bawan.transform.position,c.pihu.transform.position,.5f)+Vector3.up*1.1f);Shot("postcombat-both-wait-facing");
        yield return Walk(p,p.transform.position+Vector3.right*2.25f,"sidestep while escorts wait");
        yield return new WaitForSeconds(2.5f);
        Check("side movement leaves escort waiting",c.EscortWaiting);
        CheckFacing(c.bawan,p,"side waiting Bawan");CheckFacing(c.pihu,p,"side waiting Pihu");
        Check("waiting companions settle without walking away",Flat(bawanAtWait,c.bawan.transform.position)<.3f&&Flat(pihuAtWait,c.pihu.transform.position)<.5f);
        p.FocusOn(Vector3.Lerp(c.bawan.transform.position,c.pihu.transform.position,.5f)+Vector3.up*1.1f);Shot("postcombat-side-wait-facing");
        Vector3 waitingDirection=Vector3.ProjectOnPlane(p.transform.position-c.bawan.transform.position,Vector3.up).normalized;
        yield return Walk(p,c.bawan.transform.position+waitingDirection*5,"approach within hysteresis band");yield return new WaitForSeconds(.8f);
        float bandGap=Flat(p.transform.position,c.bawan.transform.position);
        Check("5 metre band remains waiting until below 4.6 metres",bandGap>4.6f&&bandGap<5.5f&&c.EscortWaiting&&Flat(bawanAtWait,c.bawan.transform.position)<.1f&&Flat(pihuAtWait,c.pihu.transform.position)<.1f);
        yield return Walk(p,c.bawan.transform.position+waitingDirection*4.2f,"approach below restart distance");
        Check("crossing 4.6 metre restart distance resumes escort",!c.EscortWaiting);
        float start=Time.realtimeSinceStartup;
        while(Flat(c.bawan.transform.position,bawanAtWait)<1)
        {
            Follow(p,c.bawan,2.1f);
            if(Time.realtimeSinceStartup-start>15)throw new Exception("escort did not resume when approached");
            Progress("approach and resume escort");yield return null;
        }
        Check("walking near resumes guide",!c.EscortWaiting&&Flat(c.bawan.transform.position,bawanAtWait)>=1);
        start=Time.realtimeSinceStartup;
        while(f.Region=="Combat")
        {
            Follow(p,c.bawan,1.65f);
            if(Time.realtimeSinceStartup-start>75)throw new Exception("postcombat route failed to reach school p="+p.transform.position+" B="+c.bawan.transform.position+" P="+c.pihu.transform.position);
            Progress("follow companions naturally into school");yield return null;
        }
        yield return Wait(()=>f.Region=="School"&&f.school.Exploring,12,"natural school arrival");
        Check("natural transition keeps same scene and weapon",SceneManager.sceneCount==1&&f.CarriedWeapon==Chapter3Weapons.Weapon.Rifle&&f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado);
    }
    static IEnumerator SchoolGuide(Chapter3Flow f)
    {
        var s=f.school;var p=f.player;
        yield return Wait(()=>s.ArrivalGuide&&s.ArrivalGuide.VisibleMarks>0,5,"school entry guide visible");
        yield return new WaitForSeconds(.6f);var guide=s.ArrivalGuide;int initial=guide.VisibleMarks;
        Check("school guide spans entry to Tado",Mathf.Abs(guide.Length-Flat(s.spawn,s.tado.transform.position))<.05f&&Flat(guide.Sample(guide.Length,out _),s.tado.transform.position)<.05f);
        CheckGuide(guide,p,s.tado.transform,"school entrance");
        p.FocusOn(new Vector3(0,2,28));yield return Press(Key.Space);yield return new WaitForSeconds(.3f);Shot("school-entry-yellow-arrows");
        yield return new WaitForSeconds(.4f);yield return Press(Key.Space);
        yield return Walk(p,Vector3.Lerp(s.spawn,s.tado.transform.position,.52f),"follow school arrow route");yield return null;
        Check("passing arrows shortens visible guide",guide.VisibleMarks>0&&guide.VisibleMarks<initial);
        CheckGuide(guide,p,s.tado.transform,"school halfway");
        p.FocusOn(s.tado.transform.position+Vector3.up*.6f);Shot("school-halfway-yellow-arrows");
        yield return Walk(p,s.tado.transform.position+Vector3.back*1.25f,"approach Tado");yield return new WaitForSeconds(.4f);
        Check("guide hides when close to Tado",guide.VisibleMarks==0);
        yield return Press(Key.E);yield return Wait(()=>s.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Observing,5,"E speaks to Tado");yield return null;
        Check("guide hidden during sport observation",guide.VisibleMarks==0&&guide.GetComponentsInChildren<Chapter3YellowArrow>().Length==0);
        Shot("school-Tado-guide-cleared");
        yield return Press(Key.F8);yield return Wait(()=>s.Exploring&&s.ArrivalGuide.VisibleMarks>0,12,"school guide replay");yield return null;
        Check("F8 restores calm entry and full guide",s.Battle.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&s.ArrivalGuide.VisibleMarks==initial);
        Check("school replay reuses one guide component",s.GetComponentsInChildren<Chapter3RouteGuide>(true).Length==1);
        CheckGuide(s.ArrivalGuide,p,s.tado.transform,"school replay");
        yield return Press(Key.F6);yield return null;
        Check("leaving school removes its guide",s.ArrivalGuide.VisibleMarks==0&&s.ArrivalGuide.GetComponentsInChildren<Chapter3YellowArrow>(true).Length==0);
    }
    static IEnumerator StationArrows(Chapter3Flow f,bool knife)
    {
        var s=f.station;var p=f.player;s.lineSeconds=.3f;
        yield return Wait(()=>s.Opening.Active,12,"station opening");s.Opening.replyTimeout=.01f;
        Check("first opening reply accepted",s.Opening.SubmitReply("我們接下來要去哪裡？"));yield return Wait(()=>!s.Opening.WaitingForReply,8,"first fallback answer");
        Check("second opening reply accepted",s.Opening.SubmitReply("我會跟上你，也照應比胡。"));yield return Wait(()=>!s.Opening.WaitingForReply,8,"second fallback answer");
        yield return Wait(()=>s.CurrentStage==Chapter3StationController.Stage.Approach&&s.RouteGuide.VisibleMarks>0,15,"station escort arrows");yield return new WaitForSeconds(.3f);
        CheckGuide(s.RouteGuide,p,s.bawan.transform,"station escort");p.FocusOn(s.bawan.transform.position+Vector3.up*.25f);Shot((knife?"knife":"rifle")+"-station-escort-arrows");
        float start=Time.realtimeSinceStartup;
        while(s.CurrentStage!=Chapter3StationController.Stage.WeaponChoice)
        {Follow(p,s.bawan,1.7f);if(Time.realtimeSinceStartup-start>60)throw new Exception("station briefing did not reach weapon choice");Progress("follow station escort to weapon choice");yield return null;}
        Check("station escort arrows clear for weapon choice",s.RouteGuide.VisibleMarks==0);
        if(knife)s.Choice.knife.onClick.Invoke();else s.Choice.rifle.onClick.Invoke();yield return null;
        var target=knife?s.knifeFocus:s.rifleFocus;var arrow=f.ui.GetComponentInChildren<Chapter3PickupArrow>(true);
        Check("selected weapon receives pickup arrow",arrow&&arrow.Target==target&&arrow.gameObject.activeInHierarchy);
        yield return Walk(p,new Vector3(target.position.x,0,-1.8f),"approach selected weapon");p.FocusOn(target.position+Vector3.up*.25f);yield return null;
        Canvas.ForceUpdateCanvases();var mesh=arrow.canvasRenderer.GetMesh();
        Check("pickup arrow has shaft and two wings without dark border",mesh.vertexCount==12&&mesh.triangles.Length==18&&mesh.colors.All(c=>ColorNear(c,new Color(1,.82f,.02f,1))));Object.Destroy(mesh);
        Shot((knife?"knife":"rifle")+"-pickup-arrow");yield return Press(Key.E);
        Check("collecting chosen weapon hides arrow",!arrow.gameObject.activeSelf&&!arrow.Target&&s.HasWeapon);
        yield return Press(Key.F8);yield return Wait(()=>f.school.Exploring,12,"weapon arrow jump cleanup");
        Check("no station arrows leak into school",s.RouteGuide.VisibleMarks==0&&!arrow.gameObject.activeSelf&&!arrow.Target);
    }
    static void CheckGuide(Chapter3RouteGuide guide,Chapter3Player player,Transform target,string label)
    {
        var arrows=guide.GetComponentsInChildren<Chapter3YellowArrow>();
        Check(label+" visible count matches arrow objects",arrows.Length==guide.VisibleMarks&&arrows.Length>0);
        bool shape=true,tint=true,solid=false,direction=true,inside=true;float playerAlong=guide.Project(player.transform.position),targetAlong=guide.Project(target.position);
        foreach(var arrow in arrows)
        {
            var parts=arrow.GetComponentsInChildren<MeshFilter>();
            shape&=parts.Length==3&&parts.Any(p=>p.name=="Shaft")&&parts.Any(p=>p.name=="HeadLeft")&&parts.Any(p=>p.name=="HeadRight");
            solid|=arrow.GetComponentsInChildren<Collider>().Any(c=>c.enabled);
            guide.Sample(guide.Project(arrow.transform.position),out var expectedDirection);direction&=Vector3.Angle(arrow.transform.forward,expectedDirection)<1;
            foreach(var part in parts)
            {
                var material=part.GetComponent<Renderer>().sharedMaterial;var color=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.color;tint&=ColorNear(color,new Color(1,.82f,.02f,1));
                foreach(var vertex in part.sharedMesh.vertices)
                {float along=guide.Project(part.transform.TransformPoint(vertex));inside&=along>=playerAlong+.3f-.02f&&along<=targetAlong-.7f+.02f;}
            }
        }
        Check(label+" uses three-piece yellow arrows without collision",shape&&tint&&!solid);
        Check(label+" arrows face along route and entirely stop before target",direction&&inside);
    }
    static bool ColorNear(Color a,Color b)=>Mathf.Abs(a.r-b.r)<.01f&&Mathf.Abs(a.g-b.g)<.01f&&Mathf.Abs(a.b-b.b)<.01f&&Mathf.Abs(a.a-b.a)<.01f;
    static void CheckFacing(Chapter2Actor actor,Chapter3Player p,string label)
    {
        Check(label+" rig exists",actor.Rig!=null);
        float angle=Vector3.Angle(Vector3.ProjectOnPlane(actor.Rig.Forward,Vector3.up),Vector3.ProjectOnPlane(p.transform.position-actor.transform.position,Vector3.up));
        report.observations.Add(label+" Rig.Forward angle="+angle);
        Check(label+" rendered rig faces player",angle<10);
    }
    static IEnumerator Shoot(Chapter3Flow f,Chapter3Enemy enemy)
    {
        var w=f.combat.weapons;yield return Wait(()=>w.Ready,3,"rifle ready");
        if(!w.Loaded){Check("single-round reload accepted",w.Reload());yield return Wait(()=>!w.Reloading,4,"reload finished");}
        f.player.FocusOn(enemy.AimPoint);yield return new WaitForSeconds(.15f);
        if(!f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var hit)||hit.collider.GetComponentInParent<Chapter3Enemy>()!=enemy)
            throw new Exception("shot blocked by "+(hit.collider?hit.collider.name:"nothing")+" from "+f.player.transform.position+" toward "+enemy.transform.position);
        Check("actual rifle attack accepted",w.Attack());yield return new WaitForSeconds(.55f);Check("actual shot defeats patrol officer",enemy.Defeated);
    }
    static void Follow(Chapter3Player p,Chapter2Actor actor,float gap)
    {var d=Vector3.ProjectOnPlane(actor.transform.position-p.transform.position,Vector3.up);if(p.canMove&&d.magnitude>gap){p.Move(d.normalized*Mathf.Min(d.magnitude-gap,2.8f*Mathf.Min(Time.deltaTime,.04f)));p.FocusOn(actor.transform.position+Vector3.up*1.15f);}}
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label)
    {
        float start=Time.realtimeSinceStartup;p.FocusOn(target+Vector3.up*1.15f);
        while(Flat(p.transform.position,target)>.22f)
        {
            if(Time.realtimeSinceStartup-start>20)throw new Exception("stuck "+label+" at "+p.transform.position+" target="+target);
            var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);p.Move(d.normalized*Mathf.Min(d.magnitude,2.8f*Mathf.Min(Time.deltaTime,.04f)));
            Progress(label);yield return null;
        }
    }
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(.12f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.15f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string s){if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;File.WriteAllText(Dir+"progress.txt",report.mode+" "+s);}
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish(){if(report==null)return;report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(KeyName);report=null;EditorApplication.isPlaying=false;}
    static void Shot(string name)
    {
        var cam=Camera.main;if(!cam)return;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var scene=SceneManager.GetActiveScene();var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh),"missingSkinnedMeshes="+t.SelectMany(x=>x.GetComponents<SkinnedMeshRenderer>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3ArrowsWaitProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3ArrowsWaitProbe.Run();}}


