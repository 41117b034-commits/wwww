using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Temporary validation harness. Archived outside Assets after verification.
[InitializeOnLoad]
static class Chapter1WatchProbe
{
    static string Folder => Path.GetFullPath("_CodexBackups/kick_grounding_20261003/" + SessionState.GetString("GroundedKickFolder", "baseline"));
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static Chapter1PerformanceController controller;
    static string lastBeat = "", pendingBeat = "";
    static float captureAt, choiceAt, nextSequenceCapture;
    static double poll;
    static bool triggered, chosen;
    static readonly List<string> errors = new List<string>();
    [Serializable] class Actor { public string name; public Vector3 position, forward, viewport, hips, leftHand, rightHand, leftFoot, rightFoot, leftKnee, rightKnee, leftElbow, leftShoulder, rightElbow, rightShoulder, leftThigh, rightThigh, toe, kickContact, leftFootForward,rightFootForward,leftFootUp,rightFootUp,leftHandForward,rightHandForward; public float height, kickProgress, phase; public int visibleRenderers; public bool walking, struggling, active, kicking; }
    static Vector3 MeshAxis(Chapter1IncidentRig rig,string boneName,Vector3 axis)
    {
        var bone=(Transform)typeof(Chapter1IncidentRig).GetField(boneName,Flags).GetValue(rig);
        Quaternion basis;
        if(boneName.Contains("Hand"))basis=(Quaternion)typeof(Chapter1IncidentRig).GetField(boneName=="leftHand"?"leftPalmBasis":"rightPalmBasis",Flags).GetValue(rig);
        else
        {
            var bases=(Dictionary<Transform,Quaternion>)typeof(Chapter1IncidentRig).GetField("meshBases",Flags).GetValue(rig);
            if(!bases.TryGetValue(bone,out basis))basis=Quaternion.identity;
        }
        return bone.TransformDirection(basis*axis);
    }
    [Serializable] class Wine { public string name; public Vector3 position, center, size, min; public Quaternion rotation; public bool kinematic; public int meshVertices; public float surfaceClearance, fireDistance; }
    static string sequenceBeat="";
    static int sequenceFrame;
    static float nextImage;
    static Vector3 Joint(Chapter1IncidentRig rig,string name) => ((Transform)typeof(Chapter1IncidentRig).GetField(name,Flags).GetValue(rig)).position;
    [Serializable] class State
    {
        public bool playing, paused, compiling, completed, waiting, audioPlaying, knockedOut;
        public string scene, beat, clip, subtitle;
        public float subtitleAlpha, endingAlpha;
        public float time, doorOpen, clipLength, audioVolume, audioPeak;
        public Vector3 cameraPosition, cameraForward, center;
        public Actor[] actors; public Wine[] wine; public string[] errors;
    }
    static Chapter1WatchProbe()
    {
        Directory.CreateDirectory(Folder);
        UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (path,messages) => {
            foreach(var message in messages)File.AppendAllText(Path.Combine(Folder,"compilation.txt"),message.type+" "+message.message+"\n");
        };
        EditorApplication.update += Update;
        Application.logMessageReceived += (message, stack, type) => {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message);
            if (message.Contains("[Doorway") || message.Contains("[Wedding Confrontation]") || message.Contains("[Wedding Wine Kick]"))
                File.AppendAllText(Path.Combine(Folder, "events.txt"), Time.time.ToString("F3") + " " + message + "\n");
            if(message.Contains("[Chapter1 Transition]") || message.Contains("[Chapter1 Ending]"))
                File.AppendAllText(Path.Combine(Folder,"ending-events.txt"),Time.realtimeSinceStartup.ToString("F3")+" "+message+"\n");
            if(message.Contains("Single impact:"))
            {
                WriteState("kick-contact-exact.json");
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"kick-contact-exact.png"));
                if(SessionState.GetBool("GroundedKickInspectContact",false))
                {
                    SessionState.SetBool("GroundedKickInspectContact",false);
                    EditorApplication.isPaused=true;
                }
            }
            if (message.Contains("Knockout tableau ready"))
            {
                pendingBeat = "intervene-knockout";
                captureAt = Time.time + 0.4f;
            }
        };
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingPlayMode)
            { WriteState("exit-result.json"); }
            if (state == PlayModeStateChange.EnteredEditMode)
                File.WriteAllText(Path.Combine(Folder,"returned-to-edit.txt"), DateTime.Now.ToString("O"));
        };
        EditorApplication.delayCall += () => WriteState("editor-state.json");
    }
    static string Beat() => controller == null ? "" : ((string)typeof(Chapter1PerformanceController).GetField("weddingDramaBeat", Flags).GetValue(controller) ?? "");
    static bool Waiting() => controller != null && (bool)typeof(Chapter1PerformanceController).GetField("waitingForChoice", Flags).GetValue(controller);
    static void Update()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (controller == null) controller = UnityEngine.Object.FindFirstObjectByType<Chapter1PerformanceController>();
        if (EditorApplication.isPlaying && !EditorApplication.isPaused && controller != null)
        {
            string mode = SessionState.GetString("GroundedKickRun", "");
            if (!string.IsNullOrEmpty(mode))
            {
                // Preserve the scene's real automatic ending setting.
                controller.saveResultToPlayerPrefs = false;
                if (mode != "manual" && !triggered && Time.time > 3f) { triggered = true; controller.SkipWeddingTasksToPoliceIncident(); }
                if (mode != "manual" && Waiting() && choiceAt == 0f) choiceAt = Time.time + 8f;
                if (mode != "manual" && !chosen && choiceAt > 0f && Time.time >= choiceAt)
                {
                    chosen = true;
                    if (mode.StartsWith("watch")) controller.ChooseWatch(); else controller.ChooseIntervene();
                }
            }
            if(EndingAlpha()>=0.999f && !File.Exists(Path.Combine(Folder,"black-screen.json"))) { WriteState("black-screen.json"); ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"black-screen.png")); }
            string beat = Beat();
            if (beat != lastBeat && !string.IsNullOrEmpty(beat))
            {
                lastBeat = beat;
                WriteState(beat + ".json");
                pendingBeat = beat;
                captureAt = Time.time + (beat == "watch-entering-hut" || beat == "watch-dragging-out" ? 2f : 0.35f);
            }
            if (!string.IsNullOrEmpty(pendingBeat) && Time.time >= captureAt)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder, pendingBeat + ".png"));
                WriteState(pendingBeat + "-capture.json");
                pendingBeat = "";
            }
            if ((beat.StartsWith("watch-") || beat.StartsWith("police-wine") || beat=="doorway-choice" || beat=="police-entrance-short") && Time.time >= nextSequenceCapture)
            {
                nextSequenceCapture = Time.time + 0.08f;
                File.AppendAllText(Path.Combine(Folder, "samples.jsonl"), JsonUtility.ToJson(GetState()) + "\n");
                if(sequenceBeat!=beat) {sequenceBeat=beat;sequenceFrame=0;nextImage=Time.time+0.25f;}
                if((beat=="doorway-choice" || beat.StartsWith("police-wine") || beat=="watch-entering-hut" || beat=="watch-dragging-out" || beat=="watch-police-departure" || beat=="watch-departure-tableau") && Time.time>=nextImage && sequenceFrame<10)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder,beat+"-frame-"+sequenceFrame+".png"));
                    nextImage=Time.time+0.28f;sequenceFrame++;
                }
            }
            if (controller.IsChapterCompleted() && Time.time > captureAt + 0.5f)
            {
                WriteState("verified-result.json");
                SessionState.SetString("GroundedKickRun", "");
                // Observe the real ending without pausing or stopping it.
            }
        }
        if (EditorApplication.timeSinceStartup < poll) return;
        poll = EditorApplication.timeSinceStartup + 0.25;
        string file = Path.Combine(Folder, "command.txt");
        if (!File.Exists(file)) return;
        string command = File.ReadAllText(file).Trim(); File.Delete(file);
        if(command.StartsWith("folder:")) { SessionState.SetString("GroundedKickFolder",command.Substring(7)); Directory.CreateDirectory(Folder); WriteState("editor-state.json"); return; }
        switch (command)
        {
            case "readable-wine":
                foreach(var t in controller.incidentGroundWineProps)foreach(var f in t.GetComponentsInChildren<MeshFilter>()) { var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(f.sharedMesh)) as ModelImporter; if(importer!=null&&!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();} } break;
            case "upgrade": Chapter1DoorwayAuthoring.UpgradeWatchDoor(); break;
            case "play-watch": case "play-watch-exit": case "play-intervene": case "play-manual":
                errors.Clear(); triggered = chosen = false; choiceAt = 0f; lastBeat = "";
                SessionState.SetString("GroundedKickRun", command.Substring(5));
                var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                game.maximized = true; game.Focus();
                EditorApplication.isPaused = false; EditorApplication.isPlaying = true; break;
            case "reload-scene":
                if (!EditorApplication.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                break;
            case "stop": SessionState.SetString("GroundedKickRun", ""); EditorApplication.isPaused = false; EditorApplication.isPlaying = false; break;
            case "pause": EditorApplication.isPaused = true; break;
            case "pause-next-kick": SessionState.SetBool("GroundedKickInspectContact",true); break;
            case "resume": EditorApplication.isPaused = false; break;
            case "watch": controller.ChooseWatch(); break;
            case "skip": controller.SkipWeddingTasksToPoliceIncident(); break;
            case "capture": ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "capture.png")); break;
            case "environment": DumpEnvironment(); break;
            case "inspect": DumpProps(); break;
            case "exact-wine":
                var clearanceLines=new List<string>();
                foreach(var prop in controller.incidentGroundWineProps)
                {
                    float minClearance=float.PositiveInfinity;
                    foreach(var terrain in Terrain.activeTerrains)
                    {
                        Vector3 local=prop.position-terrain.transform.position,size=terrain.terrainData.size;
                        if(local.x<0||local.z<0||local.x>size.x||local.z>size.z)continue;
                        foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
                            if(filter.sharedMesh!=null&&filter.sharedMesh.isReadable)
                                foreach(var v in filter.sharedMesh.vertices)
                                {
                                    Vector3 p=filter.transform.TransformPoint(v);
                                    minClearance=Mathf.Min(minClearance,p.y-terrain.SampleHeight(p)-terrain.transform.position.y);
                                }
                    }
                    clearanceLines.Add(prop.name+" full-mesh clearance="+minClearance.ToString("F6"));
                }
                File.WriteAllLines(Path.Combine(Folder,"exact-wine-clearance.txt"),clearanceLines);break;
            case "geometry": DumpRigGeometry(); break;
            case "author-wine": AuthorWine(); break;
            case "ground-wine-outside":
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
                Vector3 fire=(Vector3)typeof(Chapter1PerformanceController).GetMethod("GetFireCenterPosition",Flags).Invoke(controller,null);
                foreach(var prop in controller.incidentGroundWineProps)
                {
                    Undo.RecordObject(prop,"Move wine outside fire pit");
                    prop.position+=Vector3.ProjectOnPlane(prop.position-fire,Vector3.up).normalized*3f;
                    typeof(Chapter1PerformanceController).GetMethod("SetWineOnGround",Flags).Invoke(controller,new object[]{prop});
                    PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(controller.gameObject.scene);
                WriteState("authored-wine.json"); break;
            case "refresh": AssetDatabase.Refresh(); break;
            case "console":
                var le=typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                object[] counts={0,0,0};le.GetMethod("GetCountsByType",BindingFlags.Static|BindingFlags.Public).Invoke(null,counts);
                File.WriteAllText(Path.Combine(Folder,"console-counts.txt"),string.Join(",",counts)); break;
            case "door-geometry":
                var doorHits = new List<string>();
                for (float y = -5824f; y < -5808f; y += 4f)
                    foreach(var hit in Physics.RaycastAll(new Vector3(3686f,y,-75f),Vector3.forward,40f))
                        doorHits.Add(hit.collider.name + " point=" + hit.point);
                File.WriteAllLines(Path.Combine(Folder,"door-geometry.txt"),doorHits); break;
        }
        WriteState("state.json");
    }
    static State GetState()
    {
        Transform view = controller != null ? (Transform)typeof(Chapter1PerformanceController).GetField("incidentCameraView", Flags).GetValue(controller) : null;
        Camera camera = view != null ? view.GetComponent<Camera>() : Camera.main;
        var actors = new List<Actor>();
        foreach (var rig in UnityEngine.Object.FindObjectsByType<Chapter1IncidentRig>(FindObjectsSortMode.None))
        {
            if(!rig.police&&!rig.name.Contains("女性1"))continue;
            int visible = 0;
            foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if(renderer.enabled && renderer.gameObject.activeInHierarchy) visible++;
            actors.Add(new Actor { name=rig.name, position=rig.transform.position, forward=rig.Forward,
                height=rig.Height, walking=rig.walking, visibleRenderers=visible, active=rig.gameObject.activeInHierarchy,
                struggling=rig.strugglingInPlace, hips=rig.Hips.position, leftHand=rig.LeftHand.position, rightHand=rig.RightHand.position,
                leftFoot=Joint(rig,"leftFoot"), rightFoot=Joint(rig,"rightFoot"), leftKnee=Joint(rig,"leftCalf"), rightKnee=Joint(rig,"rightCalf"),
                leftElbow=Joint(rig,"leftElbow"),leftShoulder=Joint(rig,"leftArm"),
                rightElbow=Joint(rig,"rightElbow"),rightShoulder=Joint(rig,"rightArm"),leftThigh=Joint(rig,"leftThigh"),rightThigh=Joint(rig,"rightThigh"),
                toe=rig.KickToePosition,kickContact=rig.kickContact,kicking=rig.kicking,kickProgress=rig.kickProgress,phase=(float)typeof(Chapter1IncidentRig).GetField("phase",Flags).GetValue(rig),
                leftFootForward=MeshAxis(rig,"leftFoot",Vector3.forward),rightFootForward=MeshAxis(rig,"rightFoot",Vector3.forward),
                leftFootUp=MeshAxis(rig,"leftFoot",Vector3.up),rightFootUp=MeshAxis(rig,"rightFoot",Vector3.up),
                leftHandForward=MeshAxis(rig,"leftHand",Vector3.forward),rightHandForward=MeshAxis(rig,"rightHand",Vector3.forward),
                viewport=camera != null ? camera.WorldToViewportPoint(rig.Head != null ? rig.Head.position : rig.transform.position) : Vector3.zero });
        }
        foreach(var t in Resources.FindObjectsOfTypeAll<Transform>())
            if(t.gameObject.scene.IsValid() && (t.name=="部落女姓2" || t.name=="賽德克帥哥"))
            {
                int visible=0;
                foreach(var renderer in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(renderer.enabled&&renderer.gameObject.activeInHierarchy)visible++;
                actors.Add(new Actor{name=t.name,position=t.position,active=t.gameObject.activeInHierarchy,visibleRenderers=visible});
            }
        var source = controller != null ? (AudioSource)typeof(Chapter1PerformanceController).GetField("hutInteriorAudio", Flags).GetValue(controller) : null;
        float peak = 0f;
        if (source != null && source.isPlaying)
        {
            var samples = new float[256]; source.GetOutputData(samples, 0);
            foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
        }
        return new State { playing=EditorApplication.isPlaying, paused=EditorApplication.isPaused, compiling=EditorApplication.isCompiling,
            scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, beat=Beat(), time=Time.time,
            subtitle=controller!=null&&controller.dialogueUI!=null&&controller.dialogueUI.bodyText!=null?controller.dialogueUI.bodyText.text:"",
            endingAlpha=EndingAlpha(),
            subtitleAlpha=controller!=null&&controller.dialogueUI!=null&&controller.dialogueUI.canvasGroup!=null?controller.dialogueUI.canvasGroup.alpha:0f,
            completed=controller != null && controller.IsChapterCompleted(), waiting=Waiting(),
            knockedOut=controller != null && (bool)typeof(Chapter1PerformanceController).GetField("playerKnockedOut",Flags).GetValue(controller),
            doorOpen=controller != null && controller.incidentHutDoor != null ? controller.incidentHutDoor.OpenAmount : -1f,
            audioPlaying=source != null && source.isPlaying, audioVolume=source != null ? source.volume : 0f, audioPeak=peak,
            clip=source != null && source.clip != null ? source.clip.name : "", clipLength=source != null && source.clip != null ? source.clip.length : 0f,
            cameraPosition=camera != null ? camera.transform.position : Vector3.zero,
            cameraForward=camera != null ? camera.transform.forward : Vector3.zero,
            center=controller != null && controller.danceCenter != null ? controller.danceCenter.position : Vector3.zero,
            actors=actors.ToArray(), wine=GetWine(), errors=errors.ToArray() };
    }
    static float EndingAlpha() { foreach(var c in UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None)) if(c.name=="TransitionCanvas")return c.alpha; return 0f; }
    static Wine[] GetWine()
    {
        var result=new List<Wine>();
        if(controller!=null&&controller.incidentGroundWineProps!=null)
        foreach(var t in controller.incidentGroundWineProps)
        {
            if(t==null)continue;
            var rs=t.GetComponentsInChildren<Renderer>();
            Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            result.Add(new Wine{name=t.name,position=t.position,rotation=t.rotation,center=b.center,size=b.size,min=b.min,kinematic=t.GetComponent<Rigidbody>()==null||t.GetComponent<Rigidbody>().isKinematic, surfaceClearance=WineClearance(t), meshVertices=WineVertices(t), fireDistance=Vector3.ProjectOnPlane(b.center-controller.danceCenter.position,Vector3.up).magnitude});
        }
        return result.ToArray();
    }
    static int WineVertices(Transform t) { int count=0; foreach(var f in t.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null&&f.sharedMesh.isReadable)count+=f.sharedMesh.vertexCount; return count; }
    static readonly Dictionary<Transform,Vector3[]> supportPoints=new Dictionary<Transform,Vector3[]>();
    static float WineClearance(Transform t)
    {
        if(!supportPoints.TryGetValue(t,out var points))
        {
            var list=new List<Vector3>();
            foreach(var f in t.GetComponentsInChildren<MeshFilter>())
                if(f.sharedMesh!=null&&f.sharedMesh.isReadable)
                    foreach(var p in (IEnumerable<Vector3>)typeof(Chapter1PerformanceController).GetMethod("BuildWineSupportEnvelope",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{f.sharedMesh}))
                        list.Add(t.InverseTransformPoint(f.transform.TransformPoint(p)));
            points=list.ToArray();supportPoints[t]=points;
        }
        float min=float.PositiveInfinity;
        foreach(var terrain in Terrain.activeTerrains)
        {
            var local=t.position-terrain.transform.position; var size=terrain.terrainData.size;
            if(local.x<0||local.z<0||local.x>size.x||local.z>size.z)continue;
            foreach(var v in points) {var p=t.TransformPoint(v);min=Mathf.Min(min,p.y-terrain.SampleHeight(p)-terrain.transform.position.y);}
        }
        return float.IsInfinity(min)?-999:min;
    }
    static void AuthorWine()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author wine only in Edit Mode");
        string[] names={"酒杯","酒杯 (1)","無蓋酒甕 (1)","無蓋酒甕 (2)"};
        var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var props=new List<Transform>();
        Vector3 approach=new Vector3(1.5f,0f,1.65f).normalized;
        Vector3 side=Vector3.Cross(Vector3.up,approach);
        Vector3 anchor=new Vector3(3710.5f,-5818f,-143.5f);
        Vector2[] offsets={new Vector2(-0.065f,-0.16f),new Vector2(0.065f,-0.16f),new Vector2(-0.22f,0.16f),new Vector2(0.20f,0.16f)};
        for(int i=0;i<names.Length;i++)
        {
            Transform t=Array.Find(all,x=>x.name==names[i]);
            if(t==null)throw new InvalidOperationException("Missing "+names[i]);
            Undo.RecordObject(t,"Place ground wine beside fire");
            Vector3 position=anchor+(side*offsets[i].x+approach*offsets[i].y)*13.385f;
            t.position=position;
            if(i<2)t.rotation=Quaternion.identity;
            var rs=t.GetComponentsInChildren<Renderer>();
            Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            object[] args={position,0f};
            if((bool)typeof(Chapter1PerformanceController).GetMethod("TryGetIncidentSurfaceY",Flags).Invoke(controller,args))
                t.position+=Vector3.up*((float)args[1]+0.02f-b.min.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            var body=t.GetComponent<Rigidbody>();
            if(body!=null){Undo.RecordObject(body,"Keep wine upright until kick");body.isKinematic=true;body.useGravity=false;}
            props.Add(t);
        }
        Undo.RecordObject(controller,"Assign incident ground wine");
        controller.incidentGroundWineProps=props.ToArray();
        EditorUtility.SetDirty(controller);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(controller.gameObject.scene);
        WriteState("authored-wine.json");DumpProps();
    }
    static void WriteState(string filename) => File.WriteAllText(Path.Combine(Folder, filename), JsonUtility.ToJson(GetState(), true));
    static void DumpEnvironment()
    {
        var lines = new List<string>();
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            Bounds b = renderer.bounds;
            if (b.center.x < 3560f || b.center.x > 3800f || b.center.z < -220f || b.center.z > 0f || b.size.y < 8f) continue;
            if (renderer.GetComponentInParent<Animator>() != null) continue;
            lines.Add(renderer.name + " parent=" + renderer.transform.parent?.name + " center=" + b.center + " size=" + b.size);
        }
        File.WriteAllLines(Path.Combine(Folder,"environment.txt"), lines);
    }
    [Serializable] class Geometry { public string name,bone; public Vector3 forward,up,right,center; public Vector3[] points; }
    static void DumpRigGeometry()
    {
        foreach(var rig in UnityEngine.Object.FindObjectsByType<Chapter1IncidentRig>(FindObjectsSortMode.None))
        {
            if(!rig.police && !rig.name.Contains("女性1"))continue;
            var points=(Dictionary<Transform,List<Vector3>>)typeof(Chapter1IncidentRig).GetField("limbVertices",Flags).GetValue(rig);
            var bases=(Dictionary<Transform,Quaternion>)typeof(Chapter1IncidentRig).GetField("meshBases",Flags).GetValue(rig);
            foreach(var pair in points)
            {
                Quaternion basis=bases.TryGetValue(pair.Key,out var q)?q:Quaternion.identity;
                var g=new Geometry{name=rig.name,bone=pair.Key.name,forward=basis*Vector3.forward,up=basis*Vector3.up,right=basis*Vector3.right,points=pair.Value.ToArray()};
                foreach(var p in g.points)g.center+=p;
                if(g.points.Length>0)g.center/=g.points.Length;
                File.WriteAllText(Path.Combine(Folder,rig.name+"-"+pair.Key.name+"-geometry.json"),JsonUtility.ToJson(g));
            }
        }
    }
    static void DumpProps()
    {
        var lines = new List<string>();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid()) continue;
            if (!(t.name.Contains("酒") || t.name.Contains("甕") || t.name.Contains("Cup") || t.name.Contains("Jar") || t.name.Contains("酒瓶"))) continue;
            var rs=t.GetComponentsInChildren<Renderer>(true);
            Bounds bounds=new Bounds(t.position,Vector3.zero);
            if(rs.Length>0) { bounds=rs[0].bounds; foreach(var r in rs)bounds.Encapsulate(r.bounds); }
            lines.Add(t.name+" id="+t.GetInstanceID()+" parent="+t.parent?.name+" position="+t.position.ToString("F4")+" rotation="+t.eulerAngles+" size="+bounds.size+" bottom="+bounds.min+" colliders="+t.GetComponentsInChildren<Collider>(true).Length+" rb="+(t.GetComponent<Rigidbody>()!=null));
        }
        foreach(var a in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if(!(a.name.Contains("Police")||a.name.Contains("女性1")))continue;
            lines.Add("RIG "+a.name+" humanoid="+a.isHuman);
            foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>())lines.Add("MESH "+r.name+" readable="+r.sharedMesh.isReadable+" path="+AssetDatabase.GetAssetPath(r.sharedMesh));
            foreach(var t in a.GetComponentsInChildren<Transform>(true))
                lines.Add(t.name+" parent="+t.parent?.name+" local="+t.localPosition+" world="+t.position.ToString("F4"));
        }
        File.WriteAllLines(Path.Combine(Folder,"props.txt"), lines);
    }
}

