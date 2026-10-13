using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3SchoolProbe
{
    const string Dir="_CodexBackups/chapter3_batch3_20261013/",SessionKey="Chapter3SchoolProbe.mode";
    [Serializable] class Report {public string mode;public bool completed;public float elapsed;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>();}
    static Report report;static Chapter3SchoolProbeDriver driver;static float began,progressAt;
    static Chapter3SchoolProbe(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;File.WriteAllText(Dir+"probe-ready-v2.txt",DateTime.Now.ToString("O"));}
    static void Log(string m,string stack,LogType type){if(report==null)return;if(type==LogType.Warning)report.warnings.Add(m);if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(m+"\n"+stack);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="build")Chapter3SchoolAuthoring.Build();
                else if(cmd=="audit")Audit();
                else if(cmd=="polish")Chapter3SchoolAuthoring.PolishSaved();
                else if(cmd=="repair-meshes")Chapter3SchoolAuthoring.RepairSavedMeshReferences();
                else if(cmd=="reload")EditorSceneManager.OpenScene("Assets/Scenes/第三章.unity");
                else if(cmd=="stop"){SessionState.EraseString(SessionKey);report=null;EditorApplication.isPlaying=false;}
                else if(cmd.StartsWith("test-")){SessionState.SetString(SessionKey,cmd);EditorApplication.isPlaying=true;}
                else throw new Exception("Unknown command "+cmd);
                File.WriteAllText(Dir+"done-"+cmd+".txt",DateTime.Now.ToString("O"));
            }
            catch(Exception e){File.WriteAllText(Dir+"error-"+cmd+".txt",e.ToString());}
        }
        if(Application.isPlaying&&SessionState.GetString(SessionKey,"")!=""&&!driver)
            driver=new GameObject("Temporary school verification").AddComponent<Chapter3SchoolProbeDriver>();
    }
    public static IEnumerator Run()
    {
        Application.runInBackground=true;
        yield return null;
        var f=Object.FindFirstObjectByType<Chapter3Flow>();string mode=SessionState.GetString(SessionKey,"");
        report=new Report{mode=mode};began=Time.realtimeSinceStartup;
        yield return Guard(Scenario(f,mode));
        if(report!=null){report.completed=true;Finish();}
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool moved=false;object current=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
            catch(Exception e){error=e;}
            if(error!=null){report.errors.Add(error.ToString());Finish();yield break;}
            if(!moved){stack.Pop();continue;}
            if(current is IEnumerator nested){stack.Push(nested);continue;}
            yield return current;
        }
    }
    static IEnumerator Scenario(Chapter3Flow f,string mode)
    {
        Check("school meshes and collision meshes resolve",f.school.GetComponentsInChildren<MeshFilter>(true).All(m=>m.sharedMesh)&&f.school.GetComponentsInChildren<MeshCollider>(true).All(m=>m.sharedMesh));
        if(mode=="test-handoff")
        {
            yield return Press(Key.F7);yield return Wait(()=>f.combat.Fighting,25,"combat intro");
            f.combat.lineSeconds=.4f;foreach(var e in f.combat.Enemies)e.Hit(100,f.player.transform.position);
            f.player.Warp(new Vector3(0,.06f,29.5f),Vector3.forward*40);
            yield return Wait(()=>f.Region=="School"&&f.school.Exploring,40,"combat to school");
            Check("combat exits into school in same scene",SceneManager.sceneCount==1&&f.schoolRegion.activeSelf&&!f.combatRegion.activeSelf);
            Check("school has player control",f.player.canMove&&f.player.school==f.school&&f.player.combat==null);
            Shot("handoff-arrival");yield break;
        }
        yield return Press(Key.F8);yield return Wait(()=>f.Region=="School"&&f.school.Exploring,15,"F8 school");
        var s=f.school;var p=f.player;
        Check("F8 enters school",f.Region=="School");Check("single scene camera listener",SceneManager.sceneCount==1&&Camera.allCameras.Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1&&Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
        if(mode=="test-dialogue")
        {
            yield return new WaitForSeconds(.5f);yield return Press(Key.Space);
            Check("Space skips school arrival line",!s.bawan.speaking);
            yield return Walk(p,new[]{new Vector3(-4.7f,0,-7.4f)},"meet Tado");yield return Press(Key.E);
            Check("E starts Tado explanation",s.MetTado&&s.tado.speaking);Shot("tado-final");
            yield return new WaitForSeconds(.4f);yield return Press(Key.Space);
            Check("Space skips Tado explanation",!s.tado.speaking);
            yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"dialogue replay");
            Check("repeated F8 resets greeting",!s.MetTado);yield break;
        }
        if(mode=="test-layout")
        {
            yield return new WaitForSeconds(5);Audit();Shot("school-entrance");
            Capture("school-overview",new Vector3(62,51,-15),new Vector3(0,0,44),65);
            Capture("school-facade",new Vector3(2,2.4f,35),new Vector3(0,2.5f,53),67);
            Capture("sports-day",new Vector3(-15,2,8),new Vector3(10,1.5f,28),65);
            Capture("west-passage",new Vector3(-27,1.65f,21),new Vector3(-27,1.5f,53),65);
            Capture("east-cover",new Vector3(30,1.65f,15),new Vector3(31,1.3f,46),65);
            Capture("reunion",new Vector3(0,2,83),new Vector3(0,2,56),65);
            Capture("rescue-clearing",new Vector3(7,1.8f,99),new Vector3(13,1.5f,108),65);
            Check("school map has seven landmarks",s.landmarks.Length==7);
            Check("all fence rails are solid",s.GetComponentsInChildren<Transform>().Where(t=>t.name=="實體柵欄橫木").All(t=>t.GetComponent<Collider>()));
            Check("all rock objects have colliders",s.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("可碰撞岩石 ")).All(t=>t.GetComponentInChildren<MeshCollider>()));
            Check("twenty four spectators and two runners",s.GetComponentInChildren<Chapter3SchoolAmbient>().spectators.Length==24&&s.GetComponentInChildren<Chapter3SchoolAmbient>().runners.Length==2);
            yield break;
        }
        if(mode=="test-shortcuts")
        {
            int count=s.GetComponentsInChildren<Transform>(true).Length;
            yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"repeat F8");
            Check("repeat F8 does not duplicate content",count==s.GetComponentsInChildren<Transform>(true).Length);
            yield return Press(Key.F6);yield return Wait(()=>f.Region=="Station",8,"F6");
            Check("station disables school and restores movement owner",!s.gameObject.activeInHierarchy&&p.school==null);
            yield return Press(Key.F7);yield return Wait(()=>f.combat.Fighting,25,"F7");
            Check("combat has exactly three enemies",f.combat.Enemies.Count==3);
            yield return Press(Key.F8);yield return Wait(()=>s.Exploring,12,"back to school");
            Check("school disables weapons and resets camera",!f.combat.weapons.gameObject.activeSelf&&Mathf.Abs(p.view.fieldOfView-60)<.01f);
            yield break;
        }
        if(mode=="test-collisions")
        {
            yield return new WaitForSeconds(4.5f);
            yield return Push(p,"school fence",new Vector3(12,.06f,-2),Vector3.forward,()=>p.transform.position.z<-.22f);
            yield return Push(p,"east masonry",new Vector3(28,.06f,10),Vector3.left,()=>p.transform.position.x>25.35f);
            yield return Push(p,"school wall",new Vector3(4,.06f,43),Vector3.forward,()=>p.transform.position.z<47.5f);
            yield return Push(p,"tree trunk",new Vector3(-17,.06f,-14),Vector3.forward,()=>p.transform.position.z< -12.3f);
            var rock=s.GetComponentsInChildren<Transform>().First(t=>t.name=="可碰撞岩石 0");var rs=rock.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            yield return Push(p,"rock surface",new Vector3(b.center.x,.06f,b.min.z-1),Vector3.forward,()=>p.transform.position.z<b.center.z);
            yield return Push(p,"crate",new Vector3(35,.06f,9),Vector3.forward,()=>p.transform.position.z<11.8f);
            yield return Push(p,"north boundary",new Vector3(0,.06f,111),Vector3.forward,()=>p.transform.position.z<112.9f);
            p.Warp(new Vector3(0,.06f,-2),new Vector3(0,1.5f,5));yield return Walk(p,new[]{new Vector3(0,0,4)},"open school gate");
            Check("gate remains passable",p.transform.position.z>3.5f);Shot("gate-clearance");yield break;
        }
        // Both route tests walk with the real CharacterController and keep the
        // companions active; no teleports or terrain bypass on these routes.
        yield return new WaitForSeconds(4.5f);
        yield return Walk(p,new[]{new Vector3(-4.7f,0,-7.4f)},"meet Tado");yield return Press(Key.E);yield return null;
        Check("Tado can be greeted",s.MetTado);Shot(mode+"-tado");
        yield return Walk(p,mode=="test-west"?s.breakthroughRoute:s.coverRoute,mode);
        yield return Wait(()=>Flat(s.bawan.transform.position,p.transform.position)<6&&Flat(s.pihu.transform.position,p.transform.position)<8,15,"companions at reunion");
        Check("both companions reach reunion",Flat(s.bawan.transform.position,s.landmarks[4].position)<7&&Flat(s.pihu.transform.position,s.landmarks[4].position)<9);
        Shot(mode+"-reunion");yield return Walk(p,s.rescueRoute,"rescue path");
        yield return Wait(()=>Flat(s.bawan.transform.position,p.transform.position)<6&&Flat(s.pihu.transform.position,p.transform.position)<8,15,"companions at shelter");
        Check("shelter reached with both companions",p.transform.position.z>105&&s.bawan.transform.position.z>99&&s.pihu.transform.position.z>97);
        Check("no invented combat or injury state",!s.bawan.fallen&&!s.pihu.fallen&&f.Region=="School"&&!f.combat.Fighting);
        Shot(mode+"-shelter");
    }
    static IEnumerator Push(Chapter3Player p,string label,Vector3 start,Vector3 direction,Func<bool> blocked)
    {
        p.Warp(start,start+direction*8+Vector3.up);yield return null;
        for(float t=0;t<2;t+=Time.deltaTime){p.Move(direction*2.8f*Mathf.Min(Time.deltaTime,.045f));yield return null;}
        File.AppendAllText(Dir+"collision-positions.txt",label+" start="+start+" end="+p.transform.position+" motor="+p.GetComponent<CharacterController>().bounds+"\n");
        Shot("collision-"+label.Replace(" ","-"));Check(label+" blocks CharacterController",blocked()&&p.transform.position.y>-.1f&&Flat(p.transform.position,start)<5.5f&&Vector3.Dot(p.transform.position-start,direction)>.1f);
    }
    static IEnumerator Walk(Chapter3Player p,Vector3[] route,string label)
    {
        for(int i=0;i<route.Length;i++)
        {
            float start=Time.realtimeSinceStartup;
            while(Flat(p.transform.position,route[i])>.28f)
            {
                if(Time.realtimeSinceStartup-start>32)throw new Exception("Walk stuck "+label+" node="+i+" player="+p.transform.position+" goal="+route[i]);
                Vector3 d=Vector3.ProjectOnPlane(route[i]-p.transform.position,Vector3.up);p.Move(d.normalized*2.8f*Mathf.Min(Time.deltaTime,.045f));p.FocusOn(route[i]+Vector3.up*1.65f);
                Progress(label+" node="+i+" player="+p.transform.position);yield return null;
            }
            if(i>0&&i%3==0)Shot(label.Replace(" ","-")+"-node-"+i);
        }
        Check(label+" walkable",true);
    }
    static IEnumerator Wait(Func<bool> condition,float timeout,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Timeout "+label);Progress(label);yield return null;}}
    static IEnumerator Press(Key key)
    {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return new WaitForSeconds(.12f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSeconds(.12f);}
    static float Flat(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    static void Progress(string status){if(Time.realtimeSinceStartup<progressAt)return;progressAt=Time.realtimeSinceStartup+.5f;File.WriteAllText(Dir+"progress.txt",report.mode+" "+status);}
    static void Check(string name,bool passed){report.checks.Add(name+"="+passed);if(!passed)throw new Exception(name);}
    static void Finish(){report.elapsed=Time.realtimeSinceStartup-began;File.WriteAllText(Dir+report.mode+".json",JsonUtility.ToJson(report,true));SessionState.EraseString(SessionKey);report=null;EditorApplication.isPlaying=false;}
    static void Capture(string name,Vector3 p,Vector3 at,float fov)
    {var c=Camera.main;Vector3 old=c.transform.position;Quaternion rotation=c.transform.rotation;float previous=c.fieldOfView;try{c.transform.SetPositionAndRotation(p,Quaternion.LookRotation(at-p));c.fieldOfView=fov;Shot(name);}finally{c.transform.SetPositionAndRotation(old,rotation);c.fieldOfView=previous;}}
    static void Shot(string name)
    {
        var cam=Camera.main;var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;var rt=RenderTexture.GetTemporary(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();cam.targetTexture=rt;cam.aspect=1600f/900;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    static void Audit()
    {
        var f=Object.FindFirstObjectByType<Chapter3Flow>();var scene=SceneManager.GetActiveScene();var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var school=f.school;var filters=school.GetComponentsInChildren<MeshFilter>(true);
        File.WriteAllLines(Dir+(Application.isPlaying?"play-audit":"audit")+".txt",new[]{"scene="+scene.path,"dirty="+scene.isDirty,"playing="+Application.isPlaying,"compileFailed="+EditorUtility.scriptCompilationFailed,"sceneCount="+SceneManager.sceneCount,"missingScripts="+transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),"missingMeshes="+transforms.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(m=>!m.sharedMesh),"missingSkinnedMeshes="+transforms.SelectMany(t=>t.GetComponents<SkinnedMeshRenderer>()).Count(m=>!m.sharedMesh),"schoolObjects="+school.GetComponentsInChildren<Transform>(true).Length,"schoolColliders="+school.GetComponentsInChildren<Collider>(true).Length,"schoolRenderers="+school.GetComponentsInChildren<Renderer>(true).Length,"schoolVertices="+filters.Where(x=>x.sharedMesh).Sum(x=>(long)x.sharedMesh.vertexCount)});
    }
}
public sealed class Chapter3SchoolProbeDriver:MonoBehaviour
{IEnumerator Start(){yield return Chapter3SchoolProbe.Run();}}
