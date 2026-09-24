using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Temporary local validation harness; removed after the transition is verified.
[InitializeOnLoad]
static class Chapter1TransitionProbe
{
    static readonly string Folder = Path.GetFullPath("_CodexBackups/police_guest_scale_20260924");
    static double nextPoll;
    static readonly Dictionary<int, Vector3> previous = new Dictionary<int, Vector3>();
    static float maxSpeed;
    static string lastBeat;
    static Chapter1PerformanceController controller;
    static int lastFrame = -1;
    static float lastSampleTime;
    [Serializable] class Actor { public string name; public Vector3 position; public bool human; public float rigHeight, measuredHeight; public Vector3 localScale; public bool child; public string[] bones; }
    [Serializable] class State { public bool playing, paused, compiling, letterbox; public string scene, beat; public float time, maxCrowdSpeed; public Vector3 center; public Actor[] actors; public string[] errors; }
    static readonly List<string> errors = new List<string>();
    static Chapter1TransitionProbe()
    {
        EditorApplication.update += Update;
        Application.logMessageReceived += (message, stack, type) => {
            if (type == LogType.Exception || type == LogType.Error) errors.Add(message);
        };
    }
    static void Update()
    {
        if (controller == null) controller = UnityEngine.Object.FindFirstObjectByType<Chapter1PerformanceController>();
        if (EditorApplication.isPlaying && !EditorApplication.isPaused && controller != null && lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;
            float sampleSeconds = Time.time - lastSampleTime;
            lastSampleTime = Time.time;
            foreach (var dancer in Actors())
            {
                int id = dancer.GetInstanceID();
                if (controller.IsPoliceSequenceStarted && previous.TryGetValue(id, out var point) && sampleSeconds > 0.001f)
                    maxSpeed = Mathf.Max(maxSpeed, Vector3.ProjectOnPlane(dancer.transform.position - point, Vector3.up).magnitude / sampleSeconds);
                previous[id] = dancer.transform.position;
            }
            string beat = Beat();
            if (beat != lastBeat && !string.IsNullOrEmpty(beat))
            {
                lastBeat = beat;
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder, beat + ".png"));
                WriteState(beat + ".json");
            }
        }
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 0.3;
        string path = Path.Combine(Folder, "command.txt");
        if (!File.Exists(path)) return;
        string command = File.ReadAllText(path).Trim(); File.Delete(path);
        switch (command)
        {
            case "play": previous.Clear(); maxSpeed = 0f; lastBeat = null; errors.Clear(); EditorApplication.isPlaying = true; break;
            case "stop": EditorApplication.isPlaying = false; break;
            case "pause": EditorApplication.isPaused = true; break;
            case "resume": EditorApplication.isPaused = false; break;
            case "full-game":
                var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                game.maximized = true; game.Focus(); break;
            case "complete-tasks":
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(Chapter1PerformanceController);
                type.GetField("deliveredWineCount", flags).SetValue(controller, controller.wineTargetCount);
                type.GetField("sharedFoodCount", flags).SetValue(controller, controller.foodTargetCount);
                type.GetField("danceFinished", flags).SetValue(controller, true);
                type.GetMethod("TryStartPoliceAfterWeddingTasks", flags).Invoke(controller, null);
                break;
            case "capture": ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "capture.png")); break;
            case "save-full-frame":
                if (!EditorApplication.isPlaying && controller != null) {
                    controller.showCinematicLetterbox = false;
                    EditorUtility.SetDirty(controller);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(controller.gameObject.scene);
                }
                break;
        }
        WriteState("state.json");
    }
    static List<Transform> Actors()
    {
        var result = new List<Transform>();
        foreach(var dancer in UnityEngine.Object.FindObjectsByType<Chapter1CircleDancer>(FindObjectsSortMode.None))
            if(!result.Contains(dancer.transform))result.Add(dancer.transform);
        if(controller != null)
        {
            var resolve = typeof(Chapter1PerformanceController).GetMethod("GetDeliveryActorRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach(var targets in new[]{controller.wineDeliveryTargets,controller.foodDeliveryTargets})
                if(targets != null)foreach(var target in targets)
                {
                    var actor = (Transform)resolve.Invoke(controller,new object[]{target});
                    if(actor != null && !result.Contains(actor))result.Add(actor);
                }
        }
        return result;
    }
    static string Beat() => controller == null ? "" : (string)typeof(Chapter1PerformanceController).GetField("weddingDramaBeat", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
    static void WriteState(string filename)
    {
        var actors = new List<Actor>();
        foreach (var dancer in Actors())
        {
            var animator = dancer.GetComponentInChildren<Animator>();
            var bones = new List<string>();
            if (animator != null && !animator.isHuman)
                foreach (var bone in animator.GetComponentsInChildren<Transform>()) bones.Add(bone.name);
            var rig = dancer.GetComponent<Chapter1IncidentRig>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object[] heightArgs = { animator, 0f };
            if (controller != null && animator != null)
                typeof(Chapter1PerformanceController).GetMethod("TryGetAnimatorRigHeight", flags).Invoke(controller, heightArgs);
            bool child = controller != null && animator != null && (bool)typeof(Chapter1PerformanceController).GetMethod("IsChildCharacter", flags).Invoke(controller, new object[] { animator });
            actors.Add(new Actor { name = dancer.name, localScale = dancer.localScale, child = child, measuredHeight = (float)heightArgs[1], position = dancer.transform.position, human = animator != null && animator.isHuman, rigHeight = rig != null ? rig.Height : 0f, bones = bones.ToArray() });
        }
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, filename), JsonUtility.ToJson(new State {
            playing = EditorApplication.isPlaying, paused = EditorApplication.isPaused, compiling = EditorApplication.isCompiling,
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, beat = Beat(), time = Time.time,
            maxCrowdSpeed = maxSpeed, actors = actors.ToArray(), errors = errors.ToArray(),
            center = controller != null && controller.danceCenter != null ? controller.danceCenter.position : Vector3.zero,
            letterbox = controller != null && controller.showCinematicLetterbox
        }, true));
    }
}

