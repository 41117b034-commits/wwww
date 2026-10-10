using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Private Windows trial: only the two currently free hosted Gemma models.
// Credentials are encrypted for this Windows user, outside the Unity project.
public static class Chapter2CloudGemma
{
    public const string DefaultModel = "gemma-4-26b-a4b-it";
    public static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WusheNPC", "cloud-gemma.json");
    [Serializable] public class Config
    {
        public bool enabled, freeTierConfirmed;
        public string model = DefaultModel;
        public string protectedKey;
    }
    [Serializable] public class Part { public string text; public bool thought; }
    [Serializable] public class Content { public string role; public Part[] parts; }
    [Serializable] class Instruction { public Part[] parts; }
    [Serializable] class Thinking { public string thinkingLevel = "minimal"; }
    [Serializable] class Generation
    {
        public float temperature = .65f;
        public int maxOutputTokens = 256;
        public Thinking thinkingConfig = new Thinking();
    }
    [Serializable] class Request
    {
        public Instruction systemInstruction;
        public Content[] contents;
        public Generation generationConfig = new Generation();
    }
    [Serializable] class Candidate { public Content content; public string finishReason; }
    [Serializable] class Feedback { public string blockReason; }
    [Serializable] class Response { public Candidate[] candidates; public Feedback promptFeedback; }

    public static bool AllowedModel(string model) => model == DefaultModel || model == "gemma-4-31b-it";

    public static Config Load()
    {
        if (!File.Exists(ConfigPath)) return new Config();
        var config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigPath));
        if (config == null) throw new InvalidDataException("Invalid NPC cloud settings.");
        return config;
    }

    public static bool TryGetActive(out Config config, out string key, out string error)
    {
        config = null; key = null; error = null;
        try
        {
            config = Load();
            if (!config.enabled) return false;
            if (!config.freeTierConfirmed || !AllowedModel(config.model))
            { error = "雲端設定未完成，請先在 Unity 的 NPC 對話設定確認免費方案。"; return false; }
            key = RevealKey(config);
            if (string.IsNullOrWhiteSpace(key))
            { error = "這台電腦還沒有雲端金鑰，請先完成 NPC 對話設定。"; return false; }
            return true;
        }
        catch (Exception)
        { error = "無法讀取這台電腦的雲端設定，請重新設定或切回本機模式。"; return false; }
    }

    public static string RevealKey(Config config)
    {
        if (string.IsNullOrEmpty(config.protectedKey)) return null;
        var plain = Protect(Convert.FromBase64String(config.protectedKey), false);
        try { return Encoding.UTF8.GetString(plain); }
        finally { Array.Clear(plain, 0, plain.Length); }
    }

    public static void SaveEnabled(string model, string key, bool confirmed)
    {
        if (!AllowedModel(model) || !confirmed || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("A free-tier confirmation, Gemma model and key are required.");
        var plain = Encoding.UTF8.GetBytes(key.Trim());
        try
        {
            var config = new Config { enabled = true, freeTierConfirmed = true, model = model,
                protectedKey = Convert.ToBase64String(Protect(plain, true)) };
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, true));
        }
        finally { Array.Clear(plain, 0, plain.Length); }
    }

    public static void UseLocal()
    {
        Config config;
        try { config = Load(); } catch (Exception) { config = new Config(); }
        config.enabled = false;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, true));
    }

    static Content TextContent(string role, string text) => new Content { role = role, parts = new[] { new Part { text = text } } };

    public static string BuildBody(string persona, List<Chapter2LocalDialogue.Message> history, string question)
    {
        var contents = new List<Content>();
        foreach (var message in history)
        {
            if (message.role == "user" || message.role == "assistant")
                contents.Add(TextContent(message.role == "assistant" ? "model" : "user", message.content));
        }
        contents.Add(TextContent("user", question));
        return JsonUtility.ToJson(new Request { systemInstruction = new Instruction { parts = new[] { new Part { text = persona } } }, contents = contents.ToArray() });
    }

    public static UnityWebRequest CreateRequest(string model, string key, string body)
    {
        if (!AllowedModel(model)) throw new InvalidOperationException("Only the free Gemma trial models are allowed.");
        if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            throw new InvalidOperationException("Cloud key is missing or invalid.");
        // No configurable host, query-string key, redirects, tools or paid-model fallback.
        var request = new UnityWebRequest("https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-goog-api-key", key);
        request.timeout = 30;
        request.redirectLimit = 0;
        return request;
    }

    public static string HttpError(long code)
    {
        if (code == 429) return "免費雲端目前達到使用上限，請稍後再試，或切回本機模式。";
        if (code == 401 || code == 403) return "雲端金鑰或使用權限有問題，請在 NPC 對話設定重新檢查。";
        if (code == 404) return "目前帳號無法使用這個雲端模型，請換另一個免費 Gemma 或切回本機模式。";
        if (code == 400) return "雲端不接受這次請求，請在 NPC 對話設定重新測試連線。";
        return "雲端暫時無法連線或回覆逾時，請稍後再試，或切回本機模式。";
    }

    public static bool TryReadAnswer(string json, out string answer, out string error)
    {
        answer = null; error = null;
        try
        {
            var response = JsonUtility.FromJson<Response>(json);
            var first = response?.candidates != null && response.candidates.Length > 0 ? response.candidates[0] : null;
            if (!string.IsNullOrEmpty(response?.promptFeedback?.blockReason) ||
                (first != null && first.finishReason != "STOP" && first.finishReason != "MAX_TOKENS"))
            { error = "這次問題沒有得到可顯示的回答，請換個問法。"; return false; }
            var result = new StringBuilder();
            if (first?.content?.parts != null)
                foreach (var part in first.content.parts)
                    if (part != null && !part.thought && !string.IsNullOrWhiteSpace(part.text)) result.Append(part.text);
            answer = result.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(answer)) return true;
            error = "這次沒有收到回答，請再試一次。";
        }
        catch (Exception) { error = "這次回答讀取失敗，請再試一次。"; }
        answer = null;
        return false;
    }

    // Windows DPAPI: copying this file to GitHub/another PC cannot decrypt the key.
    [StructLayout(LayoutKind.Sequential)] struct Blob { public int length; public IntPtr data; }
    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptProtectData(ref Blob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr pointer);
    internal static byte[] Protect(byte[] bytes, bool encrypt)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var input = new Blob { length = bytes.Length, data = Marshal.AllocHGlobal(bytes.Length) };
        var output = new Blob();
        try
        {
            Marshal.Copy(bytes, 0, input.data, bytes.Length);
            bool ok = encrypt ? CryptProtectData(ref input, "Wushe NPC Gemma", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output) :
                CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new InvalidOperationException("Windows credential encryption failed.");
            var result = new byte[output.length];
            Marshal.Copy(output.data, result, 0, result.Length);
            return result;
        }
        finally
        {
            for (int i = 0; i < input.length; i++) Marshal.WriteByte(input.data, i, 0);
            Marshal.FreeHGlobal(input.data);
            if (output.data != IntPtr.Zero)
            { for (int i = 0; i < output.length; i++) Marshal.WriteByte(output.data, i, 0); LocalFree(output.data); }
        }
#else
        throw new PlatformNotSupportedException("This private cloud trial uses Windows user encryption.");
#endif
    }
}
