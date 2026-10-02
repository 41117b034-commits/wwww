using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 第一章 → 第二章轉場器。
/// 不使用 Coroutine、不使用 MonoBehaviour.Update、不受 Time.timeScale 影響。
/// </summary>
public class Chapter1ToChapter2Transition : MonoBehaviour
{
    private static bool transitionStarted;
    private static GameObject transitionRoot;
    private static CanvasGroup fadeGroup;

    private const int Chapter2BuildIndex = 7;
    private const string Chapter2SceneName = "第二章";
    private const string Chapter2EditorPath = "Assets/Scenes/第二章.unity";

    private const int FadeMilliseconds = 1500;
    private const int HoldMilliseconds = 350;

    public static void BeginTransition()
    {
        if (transitionStarted)
        {
            UnityEngine.Debug.Log(
                "[Chapter1 Transition] 已在轉場中，忽略重複要求。");
            return;
        }

        transitionStarted = true;

        BuildFadeCanvas();

        UnityEngine.Debug.Log(
            "[Chapter1 Transition] Async 轉場啟動：開始淡黑。");

        RunTransitionAsync();
    }

    private static void BuildFadeCanvas()
    {
        transitionRoot =
            new GameObject(
                "Chapter1_To_Chapter2_Transition");

        UnityEngine.Object.DontDestroyOnLoad(
            transitionRoot);

        GameObject canvasObject =
            new GameObject(
                "TransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));

        canvasObject.transform.SetParent(
            transitionRoot.transform,
            false);

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            32767;

        fadeGroup =
            canvasObject.GetComponent<CanvasGroup>();

        fadeGroup.alpha = 0f;
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = true;

        GameObject blackObject =
            new GameObject(
                "Black",
                typeof(RectTransform),
                typeof(Image));

        blackObject.transform.SetParent(
            canvasObject.transform,
            false);

        RectTransform rect =
            blackObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image =
            blackObject.GetComponent<Image>();

        image.color = Color.black;
        image.raycastTarget = false;
    }

    private static async void RunTransitionAsync()
    {
        try
        {
            Stopwatch timer = Stopwatch.StartNew();
            bool halfwayLogged = false;

            while (timer.ElapsedMilliseconds
                   < FadeMilliseconds)
            {
                float progress =
                    Mathf.Clamp01(
                        timer.ElapsedMilliseconds
                        / (float)FadeMilliseconds);

                if (fadeGroup != null)
                {
                    fadeGroup.alpha = progress;
                }

                if (!halfwayLogged
                    && progress >= 0.5f)
                {
                    halfwayLogged = true;

                    UnityEngine.Debug.Log(
                        "[Chapter1 Transition] 淡黑進度 50%。");
                }

                // 不靠 Unity Coroutine / Update。
                await Task.Delay(16);
            }

            if (fadeGroup != null)
            {
                fadeGroup.alpha = 1f;
            }

            UnityEngine.Debug.Log(
                "[Chapter1 Transition] 淡黑完成。");

            await Task.Delay(
                HoldMilliseconds);

            UnityEngine.Debug.Log(
                "[Chapter1 Transition] 準備載入第二章。");

            LoadChapter2();
        }
        catch (Exception ex)
        {
            transitionStarted = false;

            UnityEngine.Debug.LogException(ex);
        }
    }

    private static void LoadChapter2()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

#if UNITY_EDITOR
        UnityEngine.Debug.Log(
            "[Chapter1 Transition] Editor 載入："
            + Chapter2EditorPath);

        EditorSceneManager.LoadSceneInPlayMode(
            Chapter2EditorPath,
            new LoadSceneParameters(
                LoadSceneMode.Single));
#else
        if (Application.CanStreamedLevelBeLoaded(
                Chapter2SceneName))
        {
            UnityEngine.Debug.Log(
                "[Chapter1 Transition] Build 載入 Scene："
                + Chapter2SceneName);

            SceneManager.LoadScene(
                Chapter2SceneName,
                LoadSceneMode.Single);

            return;
        }

        UnityEngine.Debug.Log(
            "[Chapter1 Transition] Scene 名稱載入不可用，改用 Build Index 7。");

        SceneManager.LoadScene(
            Chapter2BuildIndex,
            LoadSceneMode.Single);
#endif
    }

    private static void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnityEngine.Debug.Log(
            "[Chapter1 Transition] 已成功進入："
            + scene.name);

        transitionStarted = false;

        if (transitionRoot != null)
        {
            UnityEngine.Object.Destroy(
                transitionRoot);

            transitionRoot = null;
            fadeGroup = null;
        }
    }
}
