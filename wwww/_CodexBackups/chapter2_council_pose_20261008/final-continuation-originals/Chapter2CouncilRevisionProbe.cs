using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2CouncilRevisionProbe
{
    public const string Dir="_CodexBackups/chapter2_council_pose_20261008/";
    static Chapter2Controller c;
    static string label,line;
    static float lineAt,started;
    static bool sent,shot,release,overview,finished;
    static readonly System.Collections.Generic.HashSet<string> shots=new System.Collections.Generic.HashSet<string>();
    static Chapter2CouncilRevisionProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string trace,LogType t){File.AppendAllText(Dir+"unity-log.txt",DateTime.Now.ToString("s")+" "+t+" "+m+"\n"+(t==LogType.Exception?trace:""));}
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode){label=SessionState.GetString("CouncilRevision","");c=null;sent=shot=release=overview=finished=false;line="";shots.Clear();started=Time.time;}
        if(s==PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(label))
        {File.WriteAllText(Dir+label+"-completed.txt","completed="+finished+" playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty);SessionState.EraseString("CouncilRevision");label=null;}
    }
    static void Press(Key key){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));release=true;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(release){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());release=false;}
        if(File.Exists(Dir+"command.txt"))
        {
            string cmd;
            try{cmd=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");}catch(IOException){return;}
            try
            {
                if(cmd=="refresh")AssetDatabase.Refresh();
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd=="inspect")File.WriteAllText(Dir+"state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" scene="+SceneManager.GetActiveScene().path);
                else if(cmd.StartsWith("test:")){SessionState.SetString("CouncilRevision",cmd.Substring(5));EditorApplication.isPlaying=true;}
                else if(cmd=="apply")typeof(Chapter2CouncilRevisionProbe).Assembly.GetType("Chapter2CouncilPoseAuthoring").GetMethod("Apply").Invoke(null,null);
                else if(cmd=="dump-hand")DumpHand();
                else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
                File.WriteAllText(Dir+"last-command.txt",cmd+" OK");
            }catch(Exception e){Debug.LogException(e);}
        }
        if(!Application.isPlaying||string.IsNullOrEmpty(label))return;
        if(!c)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            if(label.Contains("quick"))c.lineSeconds=.3f;
            c.StageChanged+=s=>{File.AppendAllText(Dir+label+"-stages.txt",Time.time+" "+s+"\n");if(s==Chapter2Controller.Stage.Complete)finished=true;};
        }
        if(!sent&&Time.time>started+1){Press(Key.P);sent=true;}
        if(c.CurrentStage==Chapter2Controller.Stage.Meeting && c.ui.fade.color.a==0 && !overview)
        {overview=true;Audit();Shot(label+"-overview");}
        if(c.ui.subtitle.text!=line){line=c.ui.subtitle.text;lineAt=Time.time;shot=false;}
        if(!shot && line.Length>0 && Time.time-lineAt>(label.Contains("quick")?.12f:1.2f) && c.CouncilSpeaker)
        {
            shot=true;string name=label+"-"+c.CouncilSpeaker.name+"-"+(c.CurrentStage==Chapter2Controller.Stage.Ending?"ending":"speech");
            if(shots.Add(name)){Shot(name);if(c.CouncilRally)HandShot(label+"-fist-close");}
        }
        if(c.CurrentStage==Chapter2Controller.Stage.Vote){Press(label.Contains("refuse")?Key.Digit2:Key.Digit1);}
        if(c.CouncilRally)
        {
            var gesture=c.mona.GetComponent<Chapter2RallyGesture>();
            if(gesture && gesture.Weight>.12f && shots.Add("rising"))Shot(label+"-rising");
            if(gesture && gesture.Weight>.98f && shots.Add("fist-held")){Shot(label+"-fist-held");HandShot(label+"-fist-held-close");}
            if(gesture && c.mona.SeatWeight==0 && Time.time-lineAt>2 && shots.Add("rally-settled"))
            {
                Shot(label+"-rally-settled");HandShot(label+"-rally-settled-close");
                var rig=c.mona.Rig;var bones=c.mona.GetComponentsInChildren<Transform>();
                var arm=bones.First(b=>b.name=="R_Upperarm");var elbow=bones.First(b=>b.name=="R_Forearm");
                var h=c.mona.GetComponent<Chapter2CouncilHands>();
                File.WriteAllText(Dir+label+"-rally-metrics.txt","elbowAngle="+Vector3.Angle(arm.position-elbow.position,rig.RightHand.position-elbow.position)+" fist="+h.fistRenderer.GetBlendShapeWeight(h.fistShape)+" seated="+c.mona.seated+" handY="+rig.RightHand.position.y+" headY="+rig.Head.position.y);
            }
        }
        if(c.CouncilStandingTogether && shots.Add("standing"))Shot(label+"-standing-start");
        if(c.CouncilStandingTogether && c.leaders.All(a=>!a.seated) && shots.Add("stood"))Shot(label+"-standing-complete");
        if(Time.time-started>180){Debug.LogError("Council preview timeout");EditorApplication.isPlaying=false;}
    }
    static void Audit()
    {
        var report=new StringBuilder();
        foreach(var actor in new[]{c.mona}.Concat(c.leaders).Concat(c.conservatives))
        {
            var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
            var a=actor.Rig.animator;
            report.AppendLine(actor.name+" scale="+actor.transform.localScale+" height="+actor.Rig.Height+" head="+actor.Rig.Head.position+" seated="+actor.seated);
            foreach(var skin in skins)report.AppendLine("mesh="+AssetDatabase.GetAssetPath(skin.sharedMesh)+" vertices="+skin.sharedMesh.vertexCount+" bones="+string.Join(",",skin.bones.Select(b=>b?b.name:"null")));
            foreach(var field in new[]{"rightPalmBasis","leftPalmBasis","rightPalmLength","leftPalmLength"})report.AppendLine(field+"="+typeof(Chapter1IncidentRig).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(actor.Rig));
        }
        File.WriteAllText(Dir+label+"-actors.txt",report.ToString());
    }
    static void DumpHand()
    {
        var controller=Object.FindFirstObjectByType<Chapter2Controller>();
        var h=controller.mona.GetComponent<Chapter2CouncilHands>();var skin=h.fistRenderer;
        int slot=Array.FindIndex(skin.bones,b=>b.name=="R_Hand");
        var mesh=skin.sharedMesh;var v=mesh.vertices;var w=mesh.boneWeights;var n=mesh.normals;
        var bind=mesh.bindposes[slot];var basis=Quaternion.Inverse(h.rightPalmBasis);
        var s=new StringBuilder("i,x,y,z,w,nx,ny,nz\n");
        for(int i=0;i<v.Length;i++)
        {
            float weight=(w[i].boneIndex0==slot?w[i].weight0:0)+(w[i].boneIndex1==slot?w[i].weight1:0)+(w[i].boneIndex2==slot?w[i].weight2:0)+(w[i].boneIndex3==slot?w[i].weight3:0);
            if(weight<.05f)continue;
            Vector3 q=basis*bind.MultiplyPoint3x4(v[i]),normal=basis*bind.inverse.transpose.MultiplyVector(n[i]);
            s.AppendLine($"{i},{q.x},{q.y},{q.z},{weight},{normal.x},{normal.y},{normal.z}");
        }
        File.WriteAllText(Dir+"hand-points.csv",s.ToString());
    }
    static void HandShot(string name)
    {
        var view=c.player.view;var t=view.transform;var pos=t.position;var rot=t.rotation;var fov=view.fieldOfView;
        var rig=c.mona.Rig;Vector3 at=rig.RightHand.position;Vector3 right=Vector3.Cross(Vector3.up,rig.Forward);
        t.position=at+rig.Forward*.7f+right*.35f+Vector3.up*.1f;t.LookAt(at);view.fieldOfView=38;
        Shot(name);t.SetPositionAndRotation(pos,rot);view.fieldOfView=fov;
    }
    public static void Shot(string name)
    {
        var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(tex);Object.DestroyImmediate(target);}
    }
}
