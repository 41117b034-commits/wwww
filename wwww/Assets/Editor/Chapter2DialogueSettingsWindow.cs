using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// Private developer trial; no credentials are serialized into a scene or asset.
public sealed class Chapter2DialogueSettingsWindow : EditorWindow
{
    [NonSerialized] string apiKey = "";
    [NonSerialized] string status = "", answer = "";
    [NonSerialized] bool confirmed;
    [NonSerialized] int selected;
    UnityWebRequest request;
    double began;
    string testedModel, testedKey;
    static readonly string[] Models = { Chapter2CloudGemma.DefaultModel, "gemma-4-31b-it" };

    [MenuItem("Tools/Chapter 2/NPC 對話設定（免費雲端／本機）")]
    public static void Open()
    {
        var window = GetWindow<Chapter2DialogueSettingsWindow>("NPC 對話設定");
        window.minSize = new Vector2(510, 480);
        window.Show();
    }
    void OnEnable()
    {
        try
        {
            var config = Chapter2CloudGemma.Load();
            selected = config.model == Models[1] ? 1 : 0;
            confirmed = config.freeTierConfirmed;
            apiKey = Chapter2CloudGemma.RevealKey(config) ?? "";
            status = config.enabled ? "目前：免費雲端 Gemma。" : "目前：本機模型。";
        }
        catch (Exception) { status = "無法讀取舊設定，可以重新輸入金鑰或切回本機。"; }
        EditorApplication.update += Poll;
    }
    void OnDisable()
    {
        EditorApplication.update -= Poll;
        request?.Abort(); request?.Dispose(); request = null;
        apiKey = testedKey = null;
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("第二章 NPC：免費雲端試用", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("先使用 Google AI Studio 的 Free Tier 專案。此工具只呼叫 Gemma，不會開啟計費或換成付費模型。免費額度及可用性以 Google 帳號顯示為準。", MessageType.Info);
        if (GUILayout.Button("1. 開啟 Google AI Studio，取得免費專案的 API key"))
            Application.OpenURL("https://aistudio.google.com/api-keys");
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(request != null || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            selected = EditorGUILayout.Popup("2. 試用模型", selected, Models);
            apiKey = EditorGUILayout.PasswordField("3. API key（不會上傳 Git）", apiKey);
            confirmed = EditorGUILayout.ToggleLeft("我已確認這把金鑰所屬專案是 Free Tier，未啟用計費。", confirmed);
            EditorGUILayout.HelpBox("問題、角色背景和最近三輪對話會傳到 Google；免費服務的內容可能用於改善產品。金鑰使用 Windows 帳號加密，只存這台電腦。", MessageType.None);
            using (new EditorGUI.DisabledScope(!confirmed || string.IsNullOrWhiteSpace(apiKey)))
                if (GUILayout.Button("4. 測試並啟用免費雲端")) StartTest();
            if (GUILayout.Button("切回原本本機 Gemma（離線）"))
            {
                try { Chapter2CloudGemma.UseLocal(); status = "已切回本機。下一次 Play 生效。"; answer = ""; }
                catch (Exception) { status = "設定無法儲存，請檢查 Windows 使用者資料夾權限。"; }
            }
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("請先停止 Play Mode 再切換。", MessageType.Info);
        if (request != null && GUILayout.Button("取消連線測試"))
        { request.Abort(); request.Dispose(); request = null; testedKey = null; status = "已取消，原本模式未變更。"; }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        if (!string.IsNullOrEmpty(answer))
        {
            EditorGUILayout.LabelField("測試問題：你猜我叫甚麼名字", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(answer, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("通過連線後重新 Play，影片結束後走近 NPC 即可試問。\n組員可用自己的免費金鑰，無須下載本機模型。\n此設定適用 Windows 私下測試；公開發行需另接伺服器保管金鑰。", EditorStyles.wordWrappedLabel);
    }
    void StartTest()
    {
        testedModel = Models[selected]; testedKey = apiKey.Trim(); answer = "";
        string persona = "用繁體中文扮演1930年西仔希克森林的賽德克青年阿威·比胡，回答最多兩句。玩家是另一個人。未介紹過就不知道玩家姓名，不能把自己的名字或地名當成玩家姓名。";
        try
        {
            request = Chapter2CloudGemma.CreateRequest(testedModel, testedKey,
                Chapter2CloudGemma.BuildBody(persona, new List<Chapter2LocalDialogue.Message>(), "你猜我叫甚麼名字"));
            began = EditorApplication.timeSinceStartup;
            request.SendWebRequest(); status = "正在連線測試（最多30秒）……";
        }
        catch (Exception)
        { request?.Dispose(); request = null; testedKey = null; status = "連線測試無法啟動，請檢查金鑰格式。"; }
    }
    void Poll()
    {
        if (request == null || !request.isDone) return;
        try
        {
            if (request.result != UnityWebRequest.Result.Success)
                status = Chapter2CloudGemma.HttpError(request.responseCode) + " 原本模式未變更。";
            else if (Chapter2CloudGemma.TryReadAnswer(request.downloadHandler.text, out answer, out var error))
            {
                Chapter2CloudGemma.SaveEnabled(testedModel, testedKey, confirmed);
                status = "連線成功，已啟用免費雲端。這次 " + (EditorApplication.timeSinceStartup - began).ToString("F1") + " 秒；下一次 Play 生效。";
            }
            else status = error + " 原本模式未變更。";
        }
        catch (Exception) { status = "測試或設定儲存失敗，請重新操作；未切換付費服務。"; }
        finally { request.Dispose(); request = null; testedKey = null; Repaint(); }
    }
}
