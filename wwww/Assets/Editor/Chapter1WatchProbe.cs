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
    static readonly string Folder = Path.GetFullPath("_CodexBackups/struggle_subtitle_20260926/verification");
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static Chapter1PerformanceController controller;
    static string lastBeat = "", pendingBeat = "";
    static float captureAt, choiceAt, nextSequenceCapture;
    static double poll;
    static bool triggered, chosen;
    static readonly List<string> errors = new List<string>();
    [Serializable] class Actor { public string name; public Vector3 position, forward, viewport, hips, leftHand, rightHand, leftFoot, rightFoot, leftKnee, rightKnee, leftElbow, leftShoulder; public float height; public int visibleRenderers; public bool walking, struggling, active; }
    static string sequenceBeat="";
    static int sequenceFrame;
    static float nextImage;
    static Vector3 Joint(Chapter1IncidentRig rig,string name) => ((Transform)typeof(Chapter1IncidentRig).GetField(name,Flags).GetValue(rig)).position;
    [Serializable] class State
    {
        public bool playing, paused, compiling, completed, waiting, audioPlaying, knockedOut;
        public string scene, beat, clip, subtitle;
        public float subtitleAlpha;
        public float time, doorOpen, clipLength, audioVolume, audioPeak;
        public Vector3 cameraPosition, cameraForward, center;
        public Actor[] actors; public string[] errors;
    }
    static Chapter1WatchProbe()
    {
        Directory.CreateDirectory(Folder);
        EditorApplication.update += Update;
        Application.logMessageReceived += (message, stack, type) => {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message);
            if (message.Contains("[Doorway") || message.Contains("[Wedding Confrontation]"))
                File.AppendAllText(Path.Combine(Folder, "events.txt"), Time.time.ToString("F3") + " " + message + "\n");
            if (message.Contains("Knockout tableau ready"))
            {
                pendingBeat = "intervene-knockout";
                captureAt = Time.time + 0.4f;
            }
        };
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingPlayMode)
                WriteState("exit-result.json");
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
            string mode = SessionState.GetString("WatchProbeRun", "");
            if (!string.IsNullOrEmpty(mode))
            {
                controller.stopPlayModeAfterChapterEnding = mode == "watch-exit";
                controller.saveResultToPlayerPrefs = false;
                if (mode != "manual" && !triggered && Time.time > 3f) { triggered = true; controller.SkipWeddingTasksToPoliceIncident(); }
                if (mode != "manual" && Waiting() && choiceAt == 0f) choiceAt = Time.time + 8f;
                if (mode != "manual" && !chosen && choiceAt > 0f && Time.time >= choiceAt)
                {
                    chosen = true;
                    if (mode.StartsWith("watch")) controller.ChooseWatch(); else controller.ChooseIntervene();
                }
            }
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
            if ((beat.StartsWith("watch-") || beat=="doorway-choice" || beat=="police-entrance-short") && Time.time >= nextSequenceCapture)
            {
                nextSequenceCapture = Time.time + 0.15f;
                File.AppendAllText(Path.Combine(Folder, "samples.jsonl"), JsonUtility.ToJson(GetState()) + "\n");
                if(sequenceBeat!=beat) {sequenceBeat=beat;sequenceFrame=0;nextImage=Time.time+0.25f;}
                if((beat=="doorway-choice" || beat=="watch-police-departure" || beat=="watch-departure-tableau") && Time.time>=nextImage && sequenceFrame<8)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder,beat+"-frame-"+sequenceFrame+".png"));
                    nextImage=Time.time+0.65f;sequenceFrame++;
                }
            }
            if (controller.IsChapterCompleted() && Time.time > captureAt + 0.5f)
            {
                WriteState("verified-result.json");
                SessionState.SetString("WatchProbeRun", "");
                EditorApplication.isPaused = true;
            }
        }
        if (EditorApplication.timeSinceStartup < poll) return;
        poll = EditorApplication.timeSinceStartup + 0.25;
        string file = Path.Combine(Folder, "command.txt");
        if (!File.Exists(file)) return;
        string command = File.ReadAllText(file).Trim(); File.Delete(file);
        switch (command)
        {
            case "upgrade": Chapter1DoorwayAuthoring.UpgradeWatchDoor(); break;
            case "play-watch": case "play-watch-exit": case "play-intervene": case "play-manual":
                errors.Clear(); triggered = chosen = false; choiceAt = 0f; lastBeat = "";
                SessionState.SetString("WatchProbeRun", command.Substring(5));
                var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                game.maximized = true; game.Focus();
                EditorApplication.isPaused = false; EditorApplication.isPlaying = true; break;
            case "reload-scene":
                if (!EditorApplication.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                break;
            case "stop": SessionState.SetString("WatchProbeRun", ""); EditorApplication.isPaused = false; EditorApplication.isPlaying = false; break;
            case "pause": EditorApplication.isPaused = true; break;
            case "resume": EditorApplication.isPaused = false; break;
            case "watch": controller.ChooseWatch(); break;
            case "skip": controller.SkipWeddingTasksToPoliceIncident(); break;
            case "capture": ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "capture.png")); break;
            case "environment": DumpEnvironment(); break;
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
            int visible = 0;
            foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if(renderer.enabled && renderer.gameObject.activeInHierarchy) visible++;
            actors.Add(new Actor { name=rig.name, position=rig.transform.position, forward=rig.Forward,
                height=rig.Height, walking=rig.walking, visibleRenderers=visible, active=rig.gameObject.activeInHierarchy,
                struggling=rig.strugglingInPlace, hips=rig.Hips.position, leftHand=rig.LeftHand.position, rightHand=rig.RightHand.position,
                leftFoot=Joint(rig,"leftFoot"), rightFoot=Joint(rig,"rightFoot"), leftKnee=Joint(rig,"leftCalf"), rightKnee=Joint(rig,"rightCalf"),
                leftElbow=Joint(rig,"leftElbow"),leftShoulder=Joint(rig,"leftArm"),
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
            subtitleAlpha=controller!=null&&controller.dialogueUI!=null&&controller.dialogueUI.canvasGroup!=null?controller.dialogueUI.canvasGroup.alpha:0f,
            completed=controller != null && controller.IsChapterCompleted(), waiting=Waiting(),
            knockedOut=controller != null && (bool)typeof(Chapter1PerformanceController).GetField("playerKnockedOut",Flags).GetValue(controller),
            doorOpen=controller != null && controller.incidentHutDoor != null ? controller.incidentHutDoor.OpenAmount : -1f,
            audioPlaying=source != null && source.isPlaying, audioVolume=source != null ? source.volume : 0f, audioPeak=peak,
            clip=source != null && source.clip != null ? source.clip.name : "", clipLength=source != null && source.clip != null ? source.clip.length : 0f,
            cameraPosition=camera != null ? camera.transform.position : Vector3.zero,
            cameraForward=camera != null ? camera.transform.forward : Vector3.zero,
            center=controller != null && controller.danceCenter != null ? controller.danceCenter.position : Vector3.zero,
            actors=actors.ToArray(), errors=errors.ToArray() };
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
}

