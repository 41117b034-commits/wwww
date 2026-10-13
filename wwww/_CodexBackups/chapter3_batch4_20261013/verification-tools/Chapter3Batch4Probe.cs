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

[InitializeOnLoad]
public static class Chapter3Batch4Probe
{
    const string Dir="_CodexBackups/chapter3_batch4_20261013/",KeyName="Chapter3Batch4Probe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>();}
    static Report report;static Chapter3Batch4ProbeDriver driver;static float began,progressAt;
    static Chapter3Batch4Probe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;File.WriteAllText(Dir+"probe-ready.txt",DateTime.Now.ToString("O"));}
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
        if(Application.isPlaying&&SessionState.GetString(KeyName,"")!=""&&!driver)driver=new GameObject("Temporary fourth batch verification").AddComponent<Chapter3Batch4ProbeDriver>();
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
        var p=f.player;
        if(mode=="test-village")
        {
            var v=f.village;v.lineSeconds=.35f;v.replyTimeout=.01f;
            yield return Wait(()=>v.CurrentStage==Chapter3Controller.Stage.Conversation,15,"opening");
            v.SubmitReply("好");yield return Wait(()=>!v.WaitingForReply,6,"reply one");v.SubmitReply("我會跟上");yield return Wait(()=>!v.WaitingForReply,6,"reply two");
            yield return new WaitForSeconds(2.2f);Check("two replies unlock pickup",v.Depart());
            yield return Walk(p,new Vector3(-2.55f,0,2.6f),"knife");yield return Press(Key.E);
            Check("knife collected",v.HasKnife);yield return Wait(()=>v.CurrentStage==Chapter3Controller.Stage.Trail,8,"trail starts");
            float start=Time.time;var lines=new HashSet<string>();int furthest=0;
            while(f.Region=="Village")
            {
                if(!string.IsNullOrEmpty(v.CurrentLine))lines.Add(v.CurrentLine);furthest=Mathf.Max(furthest,v.TrailPoint);
                if(p.canMove)MoveToward(p,v.bawan.transform.position,2.7f,1.8f);
                Progress("short forest route "+v.TrailPoint);if(Time.time-start>45)throw new Exception("village too long");yield return null;
            }
            Check("only first four waypoints used",furthest==4);Check("no repeated trail dialogue",lines.All(l=>l=="前面就是駐在所，壓低聲音。"));
            Check("short route reaches outpost under 30 seconds",f.Region=="Station"&&Time.time-start<30);report.checks.Add("walk and exit seconds="+(Time.time-start));yield break;
        }
        yield return Press(Key.F8);yield return Wait(()=>f.school.Exploring,12,"enter school");
        var s=f.school;var b=s.Battle;var ambient=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        Check("single scene camera listener",SceneManager.sceneCount==1&&Camera.allCameras.Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1&&Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
        Check("calm sports day before mission",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&!ambient.Panicking&&!f.combat.weapons.gameObject.activeSelf);
        Check("mission cannot be chosen early",!b.Choose(Chapter3SchoolBattle.Mission.Bawan));
        yield return new WaitForSeconds(.4f);yield return Press(Key.Space);
        yield return Walk(p,new Vector3(-4.7f,0,-7.2f),"meet Tado");yield return Press(Key.E);
        Check("E begins watching sport",b.CurrentStage==Chapter3SchoolBattle.Stage.Observing);yield return new WaitForSeconds(1);Shot("sports-before-outbreak");
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,20,"choice");Shot("mission-choice");
        bool pihu=mode.Contains("pihu");
        if(pihu)b.Menu.pihu.onClick.Invoke();else yield return Press(Key.Digit1);
        Check("outbreak follows choice",b.CurrentStage==Chapter3SchoolBattle.Stage.Outbreak&&ambient.Panicking);
        Check("choice is committed",!b.Choose(pihu?Chapter3SchoolBattle.Mission.Bawan:Chapter3SchoolBattle.Mission.Pihu));
        yield return new WaitForSeconds(1);Shot("sports-outbreak");
        yield return Wait(()=>b.Fighting,15,"combat starts");
        Check("shared weapons point to school encounter",f.combat.SchoolEncounter==b&&f.combat.Fighting&&f.combat.Enemies==b.Enemies&&p.school==s&&p.combat==null);
        Check("two guards and three allies",b.Enemies.Count==2&&b.Companions.Length==3);
        Check("new solid scenery collides",b.Barricades.All(t=>t.GetComponentsInChildren<Collider>(true).Length>=5)&&b.Ammo.GetComponentInChildren<Collider>());
        if(mode=="test-physics")
        {
            p.Warp(new Vector3(-27,.08f,13.2f),new Vector3(-27,1,20));yield return null;
            for(float t=0;t<1.5f;t+=Time.deltaTime){p.Move(Vector3.forward*2.8f*Time.deltaTime);yield return null;}
            Check("new barricade physically blocks player",p.transform.position.z<15.75f&&p.transform.position.z>14.5f);
            p.Warp(new Vector3(30.5f,.08f,7),new Vector3(30.5f,1,15));yield return null;
            for(float t=0;t<1.4f;t+=Time.deltaTime){p.Move(Vector3.forward*2.8f*Time.deltaTime);yield return null;}
            Check("new crate physically blocks player",p.transform.position.z<9.35f&&p.transform.position.z>8);
            Check("crate blocks incoming ray",Physics.Raycast(new Vector3(30.5f,1.05f,14),Vector3.back,out var coverHit,8)&&coverHit.collider.name=="東側厚木掩體");
            var protectedActor=ambient.spectators[5];var protectedBody=protectedActor.GetComponentInChildren<Collider>();
            p.Warp(protectedActor.position+new Vector3(0,.08f,-2.5f),protectedBody.bounds.center);yield return new WaitForSeconds(.2f);
            Check("civilian lies on real shot ray",f.combat.Trace(p.view.transform.position,p.view.transform.forward,10,out var hit)&&hit.collider.GetComponentInParent<Chapter3Bystander>());
            int shots=f.combat.weapons.Shots;Check("civilian shot blocked without ammo use",!f.combat.weapons.Attack()&&f.combat.weapons.Shots==shots&&f.combat.weapons.Loaded);
            p.Warp(new Vector3(-27,.08f,13),b.Enemies[0].AimPoint);yield return new WaitForSeconds(.5f);yield return Shoot(f,b.Enemies[0]);
            yield return Hold(Key.E,2.1f);Check("first gate opened before retry",b.Step==1);b.Damage(100,p.transform.position);b.Menu.retry.onClick.Invoke();yield return null;
            Check("Bawan retry restores closed solid gates",b.Fighting&&b.Selected==Chapter3SchoolBattle.Mission.Bawan&&b.Step==0&&b.Retries==1&&b.Barricades.All(t=>t.gameObject.activeSelf&&t.GetComponentsInChildren<Collider>().All(c=>c.enabled)));
            Check("retry rejects while alive",!b.Retry());yield break;
        }
        if(mode=="test-shortcuts")
        {
            yield return Press(Key.F7);f.combat.lineSeconds=.2f;yield return Wait(()=>f.combat.Fighting,20,"old combat");
            Check("old combat owns weapon service",!f.combat.SchoolEncounter&&f.combat.Enemies.Count==3&&!b.Menu.choice.activeSelf&&!b.Menu.result.activeSelf);
            var enemy=f.combat.Enemies[0];p.Warp(enemy.transform.position+Vector3.back*3+Vector3.up*.08f,enemy.AimPoint);yield return new WaitForSeconds(.3f);
            Check("old combat rifle ready",f.combat.weapons.Attack());yield return new WaitForSeconds(.6f);Check("old combat rifle defeats guard",enemy.Defeated&&f.combat.DefeatedCount==1);
            f.combat.Damage(100,p.transform.position);Check("old combat down state",f.combat.CurrentStage==Chapter3Combat.Stage.Down);yield return Press(Key.R);
            Check("old combat retry resets",f.combat.Health==100&&f.combat.DefeatedCount==0&&f.combat.Retries==1);
            foreach(var e in f.combat.Enemies)e.Hit(100,p.transform.position);
            p.Warp(new Vector3(0,.06f,29.5f),new Vector3(0,1,40));
            yield return Wait(()=>f.Region=="School"&&s.Exploring,40,"old combat handoff");
            Check("handoff resets school story",b.CurrentStage==Chapter3SchoolBattle.Stage.AwaitingTado&&b.Selected==Chapter3SchoolBattle.Mission.None&&!ambient.Panicking&&!f.combat.SchoolEncounter);
            int count=s.GetComponentsInChildren<Transform>(true).Length;
            yield return Press(Key.F8);yield return Wait(()=>s.Exploring,10,"school replay");
            Check("no duplicated props or menus on replay",count==s.GetComponentsInChildren<Transform>(true).Length&&Object.FindObjectsByType<Chapter3MissionUI>(FindObjectsSortMode.None).Length==1);yield break;
        }
        if(mode=="test-pihu-retry")
        {
            yield return WalkRoute(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(30.5f,0,7)},"cover entry");
            yield return Wait(()=>b.AtCrossing,35,"companions at cover");
            Check("Bawan interaction rejected on Pihu branch",!b.WorkBarricade(true,10));
            yield return Press(Key.E);Check("E orders advance",b.Advancing);
            yield return Wait(()=>b.EscortPressure>0,12,"real enemy shot reaches moving escort");
            Check("enemy fire causes visible escort pressure",b.EscortPressure>0&&b.Enemies.Any(e=>e.Shots>0));p.FocusOn(s.pihu.transform.position+Vector3.up*1.1f);yield return new WaitForSeconds(.2f);Shot("pihu-under-fire");
            yield return Press(Key.E);Check("E orders return to cover",b.Returning&&!b.Advancing);
            yield return Wait(()=>!b.Returning,20,"return to cover");Check("return retains crossing",b.Step==0);yield return Press(Key.E);
            // Simulate sustained incoming fire to exercise the failure checkpoint.
            b.EscortHit();b.EscortHit();b.EscortHit();Check("pressure fails without Pihu death",b.CurrentStage==Chapter3SchoolBattle.Stage.Down&&s.pihu.FallProgress==0);Shot("pihu-checkpoint");
            yield return Press(Key.R);yield return null;
            Check("retry preserves Pihu task and resets pressure",b.Fighting&&b.Selected==Chapter3SchoolBattle.Mission.Pihu&&b.Retries==1&&b.Step==0&&b.Health==100&&b.EscortPressure==0&&!b.Advancing);
            Check("retry recreates two living guards",b.Enemies.Count==2&&b.Enemies.All(e=>!e.Defeated));
            b.Damage(100,p.transform.position);yield return Press(Key.R);Check("health retry also retains task",b.Fighting&&b.Retries==2&&b.Selected==Chapter3SchoolBattle.Mission.Pihu);yield break;
        }
        if(pihu)
        {
            yield return WalkRoute(p,new[]{new Vector3(8,0,-8),new Vector3(25,0,-4),new Vector3(27.5f,0,7)},"east approach");
            yield return Wait(()=>b.AtCrossing,35,"cover readiness");
            Check("escort waits for command",!b.Advancing&&b.Step==0);Shot("pihu-hold");
            yield return Walk(p,new Vector3(27.5f,0,12),"peek first guard");yield return Shoot(f,b.Enemies[0]);
            yield return Press(Key.E);Check("Pihu first crossing command",b.Advancing);
            yield return new WaitForSeconds(2);Shot("pihu-crossing");yield return Wait(()=>b.Step==1,30,"first crossing");
            Check("first crossing completed with both allies",Flat(s.pihu.transform.position,new Vector3(30,0,23))<.4f&&Flat(s.tado.transform.position,new Vector3(29.15f,0,22.1f))<.4f&&!b.Advancing);
            yield return WalkRoute(p,new[]{new Vector3(27.5f,0,24),new Vector3(27.5f,0,30),new Vector3(29,0,37),new Vector3(32,0,43),new Vector3(27,0,53)},"second cover position");
            yield return Shoot(f,b.Enemies[1]);
            yield return WalkRoute(p,new[]{new Vector3(32,0,43),new Vector3(29,0,37)},"return to command range");
            yield return Press(Key.E);Check("Pihu second crossing command",b.Advancing);yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup,35,"second crossing");
            Check("cover branch completes by escort arrival",b.Step==2&&b.EscortPressure==0);
            yield return WalkRoute(p,new[]{new Vector3(32,0,43),new Vector3(32,0,53)},"join escort");
        }
        else
        {
            Check("escort command rejected on Bawan branch",!b.CommandEscort());
            yield return WalkRoute(p,new[]{new Vector3(-7,0,-8),new Vector3(-23,0,-4),new Vector3(-27,0,8),new Vector3(-27,0,12.9f)},"west first barricade");
            Check("cannot dismantle while guard lives",!b.WorkBarricade(true,1)&&b.WorkProgress==0);
            Shot("bawan-first-barricade");yield return Shoot(f,b.Enemies[0]);
            yield return Hold(Key.E,.6f);Check("released hold cancels progress",b.Step==0&&b.WorkProgress==0);
            yield return Hold(Key.E,2.1f);Check("held E opens first barricade",b.Step==1);yield return new WaitForSeconds(.7f);
            Check("first gate removed",!b.Barricades[0].gameObject.activeSelf);
            yield return WalkRoute(p,new[]{new Vector3(-27,0,24),new Vector3(-27,0,35),new Vector3(-27,0,43)},"west second barricade");
            yield return Shoot(f,b.Enemies[1]);yield return Hold(Key.E,2.1f);
            Check("both gates trigger regroup",b.Step==2&&b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup);Shot("bawan-route-open");
        }
        var exit=pihu?new[]{new Vector3(27.5f,0,54),new Vector3(27.5f,0,60),new Vector3(25,0,68),new Vector3(12,0,74),new Vector3(0,0,78)}:new[]{new Vector3(-27,0,54),new Vector3(-25,0,66),new Vector3(-12,0,74),new Vector3(0,0,78)};
        yield return WalkRoute(p,exit,"walk to rendezvous");
        yield return Wait(()=>b.Complete,65,"both squads reunite");Shot(pihu?"pihu-reunion":"bawan-reunion");
        Check("three companions reunited and no injuries",new[]{s.bawan,s.pihu,s.tado}.All(a=>Flat(a.transform.position,new Vector3(0,0,78))<4&&a.FallProgress==0));
        Check("weapons hidden and batch four completion card",!f.combat.weapons.gameObject.activeSelf&&b.Menu.result.activeSelf&&!b.Menu.retry.gameObject.activeSelf&&SceneManager.sceneCount==1);
        Check("mission retained at reunion",b.Selected==(pihu?Chapter3SchoolBattle.Mission.Pihu:Chapter3SchoolBattle.Mission.Bawan));
    }
    static IEnumerator Shoot(Chapter3Flow f,Chapter3Enemy enemy)
    {
        var w=f.combat.weapons;if(!w.Loaded){yield return Wait(()=>w.Ready,3,"reload ready");Check("reload starts",w.Reload());yield return Wait(()=>!w.Reloading,4,"single reload");}
        f.player.FocusOn(enemy.AimPoint);yield return new WaitForSeconds(.15f);
        if(!f.combat.Trace(f.player.view.transform.position,f.player.view.transform.forward,65,out var hit)||hit.collider.GetComponentInParent<Chapter3Enemy>()!=enemy)
            throw new Exception("shot blocked by "+(hit.collider?hit.collider.name:"nothing")+" from "+f.player.transform.position+" to "+enemy.transform.position);
        Check("rifle attack accepted",w.Attack());yield return new WaitForSeconds(.55f);Check("real rifle shot defeats guard",enemy.Defeated);
    }
    static IEnumerator WalkRoute(Chapter3Player p,Vector3[] points,string label){foreach(var point in points)yield return Walk(p,point,label);}
    static IEnumerator Walk(Chapter3Player p,Vector3 target,string label)
    {
        float start=Time.realtimeSinceStartup;p.FocusOn(target+Vector3.up*1.25f);
        while(Flat(p.transform.position,target)>.3f)
        {
            if(label=="walk to rendezvous"&&!p.canMove&&Flat(p.transform.position,target)<4&&p.school&&p.school.Battle.CurrentStage>=Chapter3SchoolBattle.Stage.Regroup)yield break;
            if(Time.realtimeSinceStartup-start>35)throw new Exception("stuck "+label+" at "+p.transform.position+" target "+target);
            MoveToward(p,target,2.8f,0);Progress(label+" "+p.transform.position);yield return null;
        }
    }
    static void MoveToward(Chapter3Player p,Vector3 target,float speed,float gap)
    {var d=Vector3.ProjectOnPlane(target-p.transform.position,Vector3.up);if(d.magnitude>gap)p.Move(d.normalized*Mathf.Min(d.magnitude-gap,speed*Mathf.Min(Time.deltaTime,.04f)));}
    static IEnumerator Wait(Func<bool> condition,float seconds,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>seconds)throw new Exception("timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key){yield return Hold(key,.12f);}
    static IEnumerator Hold(Key key,float seconds){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(seconds);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.15f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string s){if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;var f=Object.FindFirstObjectByType<Chapter3Flow>();File.WriteAllText(Dir+"progress.txt",report.mode+" "+s+(f&&f.school.Battle?"\nstate "+f.school.Battle.CurrentStage+" step "+f.school.Battle.Step+" hp "+f.school.Battle.Health+" B "+f.school.bawan.transform.position+" P "+f.school.pihu.transform.position+" T "+f.school.tado.transform.position:""));}
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
        File.WriteAllLines(Dir+"audit.txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"missingScripts="+t.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),"missingMeshes="+t.SelectMany(x=>x.GetComponents<MeshFilter>()).Count(x=>!x.sharedMesh)});
    }
}
public sealed class Chapter3Batch4ProbeDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3Batch4Probe.Run();}}
