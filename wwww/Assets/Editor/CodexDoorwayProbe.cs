using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[InitializeOnLoad]
public static class CodexDoorwayProbe
{
    const string Work = @"C:\Users\jimmy\Documents\Codex\2026-09-19\new-chat-2\work";
    static double next;
    static bool wasChoice, wasDown;
    static Chapter1PerformanceController C => UnityEngine.Object.FindFirstObjectByType<Chapter1PerformanceController>();
    static object Field(string n) => C?.GetType().GetField(n, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.GetValue(C);
    static CodexDoorwayProbe() { EditorApplication.update += Tick; EditorApplication.playModeStateChanged += s => File.AppendAllText(Path.Combine(Work,"events.txt"), DateTime.UtcNow.ToString("O")+" "+s+"\n"); Application.logMessageReceived += (message,trace,type)=> {if(message.StartsWith("[Doorway]"))File.AppendAllText(Path.Combine(Work,"events.txt"),DateTime.UtcNow.ToString("O")+" "+message+"\n");if(type==LogType.Error || type==LogType.Exception)File.AppendAllText(Path.Combine(Work,"runtime-errors.txt"),message+"\n"+trace+"\n");}; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 0.2;
        try
        {
            File.WriteAllText(Path.Combine(Work,"status.txt"), "play="+EditorApplication.isPlaying+" time="+Time.time+" choice="+Field("waitingForChoice")+" down="+Field("playerKnockedOut"));
            bool choice = Equals(Field("waitingForChoice"),true), down=Equals(Field("playerKnockedOut"),true);
            if(EditorApplication.isPlaying && choice && !wasChoice) { Shot("choice-auto"); Inspect("choice-auto"); }
            if(EditorApplication.isPlaying && down && !wasDown) { Shot("down-auto"); Inspect("down-auto"); File.AppendAllText(Path.Combine(Work,"events.txt"),DateTime.UtcNow.ToString("O")+" KNOCKOUT\n"); }
            wasChoice=choice;wasDown=down;
            string path=Path.Combine(Work,"command.txt"); if(!File.Exists(path))return;
            string cmd=File.ReadAllText(path).Trim();File.Delete(path);
            if(cmd=="play")EditorApplication.isPlaying=true;
            else if(cmd=="setup")Chapter1DoorwayAuthoring.Build();
            else if(cmd=="houses")
            {
                var b=new StringBuilder();
                foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if(Vector3.Distance(r.bounds.center,new Vector3(3672,-5816,-51))>110 || r.bounds.size.magnitude<18)continue;
                    b.AppendLine(r.name+" parent="+r.transform.parent?.name+" p="+r.transform.position.ToString("F3")+" r="+r.transform.eulerAngles.ToString("F2")+" scale="+r.transform.lossyScale.ToString("F3")+" bounds="+r.bounds.ToString("F3")+" mesh="+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh));
                }
                File.WriteAllText(Path.Combine(Work,"houses.txt"),b.ToString());
            }
            else if(cmd=="surfaces")
            {
                var b=new StringBuilder();
                for(float x=3670;x<=3750;x+=5)for(float z=-70;z<=0;z+=5)
                {
                    var hits=Physics.RaycastAll(new Vector3(x,-5800,z),Vector3.down,50).OrderBy(h=>h.distance);
                    foreach(var h in hits)if(h.normal.y>0.4f)b.AppendLine(x+","+z+" -> "+h.point.y+" "+h.collider.name);
                }
                File.WriteAllText(Path.Combine(Work,"surfaces.txt"),b.ToString());
            }
            else if(cmd=="house")
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/房子/長木屋/長木屋.fbx");
                var go=UnityEngine.Object.Instantiate(asset);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(-90,0,0));
                var cam=Camera.main;var p=cam.transform.position;var q=cam.transform.rotation;var rect=cam.rect;float f=cam.fieldOfView;
                var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();foreach(var c in canvases)c.enabled=false;
                try
                {
                    Bounds b=go.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in go.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
                    File.WriteAllText(Path.Combine(Work,"house-bounds.txt"),b.ToString("F4"));
                    cam.rect=new Rect(0,0,1,1);cam.fieldOfView=48;cam.nearClipPlane=0.01f;
                    for(int i=0;i<4;i++){var dir=Quaternion.Euler(0,i*90,0)*Vector3.back;cam.transform.position=b.center+dir*b.size.magnitude*1.05f+Vector3.up*b.size.y*0.25f;cam.transform.LookAt(b.center);Shot("house-"+i);}
                }
                finally{UnityEngine.Object.DestroyImmediate(go);cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=f;cam.rect=rect;foreach(var c in canvases)c.enabled=true;}
            }
            else if(cmd=="stop")EditorApplication.isPlaying=false;
            else if(cmd=="skip")C.SkipWeddingTasksToPoliceIncident();
            else if(cmd=="intervene")C.ChooseIntervene();
            else if(cmd=="watch")C.ChooseWatch();
            else if(cmd=="door")
            {
                C.StopAllCoroutines();
                C.GetType().GetField("choiceResolved",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(C,false);
                C.StartCoroutine((System.Collections.IEnumerator)C.GetType().GetMethod("DoorwayIncidentRoutine",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(C,null));
            }
            else if(cmd.StartsWith("set:"))
            {
                var pair=cmd.Substring(4).Split('=');var f=C.GetType().GetField(pair[0],BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
                if(f.FieldType==typeof(Vector3)){var v=pair[1].Split(',').Select(float.Parse).ToArray();f.SetValue(C,new Vector3(v[0],v[1],v[2]));}
                else if(f.FieldType==typeof(float))f.SetValue(C,float.Parse(pair[1]));
            }
            else if(cmd=="inspect")Inspect("inspect");
            else if(cmd.StartsWith("shot:"))Shot(cmd.Substring(5));
            else if(cmd.StartsWith("view:"))
            {
                var v=cmd.Substring(5).Split(',').Select(float.Parse).ToArray();var camera=Camera.main;
                var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;var rect=camera.rect;
                try {camera.rect=new Rect(0,0,1,1);camera.transform.position=new Vector3(v[0],v[1],v[2]);camera.transform.LookAt(new Vector3(v[3],v[4],v[5]));camera.fieldOfView=55;Shot("view");}
                finally {camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;camera.rect=rect;}
            }
            else if(cmd.StartsWith("invoke:")) C.GetType().GetMethod(cmd.Substring(7),BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(C,null);
        }
        catch(Exception e){File.AppendAllText(Path.Combine(Work,"probe-errors.txt"),e+"\n");}
    }
    static void Describe(Transform t,StringBuilder b)
    {
        if(t==null)return;
        b.AppendLine("OBJECT "+t.name+" p="+t.position.ToString("F3")+" r="+t.eulerAngles.ToString("F2")+" scale="+t.lossyScale.ToString("F3"));
        foreach(var r in t.GetComponentsInChildren<Renderer>(true).Take(5))b.AppendLine("RENDER "+r.name+" "+r.bounds.ToString("F3")+" asset="+AssetDatabase.GetAssetPath(r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh));
        var a=t.GetComponentInChildren<Animator>(true);if(a==null)return;
        b.AppendLine("ANIM "+a.name+" enabled="+a.enabled+" speed="+a.speed+" human="+a.isHuman+" avatar="+AssetDatabase.GetAssetPath(a.avatar)+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController));
        if(a.runtimeAnimatorController is AnimatorController ac) foreach(var l in ac.layers) foreach(var s in l.stateMachine.states)b.AppendLine("STATE "+s.state.name+" clip="+AssetDatabase.GetAssetPath(s.state.motion));
        if(a.isHuman)foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){var bt=a.GetBoneTransform(bone);if(bt!=null)b.AppendLine("BONE "+bone+" "+bt.name+" p="+bt.position.ToString("F3")+" lp="+bt.localPosition.ToString("F4")+" lr="+bt.localEulerAngles.ToString("F2"));}
    }
    static void Inspect(string name)
    {
        var b=new StringBuilder();b.AppendLine("time="+Time.time+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        if(C!=null) foreach(var f in C.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
        {if(f.FieldType==typeof(Transform)){b.AppendLine("FIELD "+f.Name);Describe(f.GetValue(C) as Transform,b);}}
        if(Camera.main!=null){Describe(Camera.main.transform,b);b.AppendLine("CAM fov="+Camera.main.fieldOfView);}
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.Contains("屋")||t.name.Contains("Hut")||t.name.Contains("house")||t.name.Contains("Police")))b.AppendLine("SCENE "+t.name+" "+t.position.ToString("F3")+" "+t.eulerAngles.ToString("F1"));
        File.WriteAllText(Path.Combine(Work,name+".txt"),b.ToString());
    }
    static void Shot(string name)
    {
        if(EditorApplication.isPlaying)ScreenCapture.CaptureScreenshot(Path.Combine(Work,name+"-game.png"));
        var camera=Camera.main;if(camera==null)return;var rt=RenderTexture.GetTemporary(1600,900,24);var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Work,name+".png"),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
}
