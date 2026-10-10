using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// Temporary verification driver, archived outside Assets after validation.
[InitializeOnLoad]
public static class Chapter2CloudTrialProbe
{
    const string Dir = "_CodexBackups/chapter2_cloud_gemma_20261010/";
    [Serializable] class Report { public bool finished, passed, playing, dirty, cloudConfigured; public List<string> checks = new List<string>(); public string failure; }
    static Chapter2CloudTrialProbe() { EditorApplication.update += Tick; }
    static void Check(Report report, bool ok, string name)
    { if (!ok) throw new Exception(name); report.checks.Add(name); }
    static void Validate()
    {
        var report = new Report();
        string beforeConfig = File.Exists(Chapter2CloudGemma.ConfigPath) ? File.ReadAllText(Chapter2CloudGemma.ConfigPath) : null;
        try
        {
            var history = new List<Chapter2LocalDialogue.Message> { new Chapter2LocalDialogue.Message("user", "我叫阿山\n你好"), new Chapter2LocalDialogue.Message("assistant", "阿山你好。") };
            string body = Chapter2CloudGemma.BuildBody("你是阿威。", history, "你猜我叫甚麼名字");
            Check(report, body.Contains("\"role\":\"model\"") && body.Contains("阿山") && body.Contains("你猜我叫甚麼名字"), "conversation history and current question serialized");
            Check(report, body.Contains("systemInstruction") && body.Contains("minimal") && !body.Contains("tools"), "separate persona, thinking off, no paid tools");
            using (var request = Chapter2CloudGemma.CreateRequest(Chapter2CloudGemma.DefaultModel, "test-only-not-a-key", body))
                Check(report, request.url == "https://generativelanguage.googleapis.com/v1beta/models/gemma-4-26b-a4b-it:generateContent" && !request.url.Contains("test-only") && request.redirectLimit == 0 && request.timeout == 30, "fixed HTTPS destination, no key in URL, bounded timeout, no redirects");
            bool refused = false;
            try { using (Chapter2CloudGemma.CreateRequest("gemini-paid-or-arbitrary", "test", body)) {} } catch (InvalidOperationException) { refused = true; }
            Check(report, refused, "unlisted model rejected before network");
            string answer, error;
            bool read = Chapter2CloudGemma.TryReadAnswer("{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"text\":\"private reasoning\",\"thought\":true},{\"text\":\"我不知道\"},{\"text\":\"你的名字。\"}]}}]}", out answer, out error);
            Check(report, read && answer == "我不知道你的名字。" && error == null, "reply parts combined and thinking excluded");
            read = Chapter2CloudGemma.TryReadAnswer("{\"candidates\":[{\"finishReason\":\"SAFETY\",\"content\":{\"parts\":[{\"text\":\"partial unsafe text\"}]}}]}", out answer, out error);
            Check(report, !read && answer == null && error != null, "blocked candidate not shown as answer");
            read = Chapter2CloudGemma.TryReadAnswer("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}", out answer, out error);
            Check(report, !read && answer == null && error != null, "blocked prompt handled without candidate");
            read = Chapter2CloudGemma.TryReadAnswer("{\"candidates\":[]}", out answer, out error);
            Check(report, !read && answer == null && error != null, "empty response handled");
            read = Chapter2CloudGemma.TryReadAnswer("not-json", out answer, out error);
            Check(report, !read && answer == null && error != null, "malformed response handled");
            Check(report, Chapter2CloudGemma.HttpError(429).Contains("上限") && Chapter2CloudGemma.HttpError(403).Contains("權限") && Chapter2CloudGemma.HttpError(0).Contains("連線"), "quota, authentication and network errors distinguished");
            var protect = typeof(Chapter2CloudGemma).GetMethod("Protect", BindingFlags.NonPublic | BindingFlags.Static);
            var original = Encoding.UTF8.GetBytes("test-only-windows-user-encryption");
            var encrypted = (byte[])protect.Invoke(null, new object[] { original, true });
            var restored = (byte[])protect.Invoke(null, new object[] { encrypted, false });
            Check(report, !encrypted.SequenceEqual(original) && restored.SequenceEqual(original), "Windows DPAPI roundtrip without saving a credential");
            Chapter2CloudGemma.Config config; string key, configError;
            report.cloudConfigured = Chapter2CloudGemma.TryGetActive(out config, out key, out configError);
            key = null;
            Check(report, beforeConfig == (File.Exists(Chapter2CloudGemma.ConfigPath) ? File.ReadAllText(Chapter2CloudGemma.ConfigPath) : null), "validation preserved user cloud configuration");
            report.passed = true;
        }
        catch (Exception e) { report.failure = e.GetType().Name + ": " + e.Message; }
        report.finished = true; report.playing = EditorApplication.isPlaying;
        report.dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
        File.WriteAllText(Dir + "adapter-checks.json", JsonUtility.ToJson(report, true));
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var path = Dir + "command.txt";
        if (!File.Exists(path)) return;
        string command = File.ReadAllText(path).Trim(); File.Delete(path);
        if (command == "validate") Validate();
        if (command == "open") Chapter2DialogueSettingsWindow.Open();
        if (command == "refresh") AssetDatabase.Refresh();
        if (command == "status") File.WriteAllText(Dir + "status.json", "{\"playing\":" + (EditorApplication.isPlaying ? "true" : "false") + ",\"compilationFailed\":" + (EditorUtility.scriptCompilationFailed ? "true" : "false") + "}");
    }
}
