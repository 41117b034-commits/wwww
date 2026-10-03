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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTransition()
    {
        transitionStarted = false;
        transitionRoot = null;
        fadeGroup = null;
    }

    public static void BeginTransition()
    {
        if (transitionStarted)
        {
            UnityEngine.Debug.Log(
                "[Chapter1 Transition] 已在轉場中，忽略重複要求。");
            return;
        }

        if (!Application.isPlaying) return;
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
            GameObject activeTransition = transitionRoot;
            Stopwatch timer = Stopwatch.StartNew();
            bool halfwayLogged = false;

            while (timer.ElapsedMilliseconds
                   < FadeMilliseconds)
            {
                if (!Application.isPlaying || activeTransition == null) return;
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

            await Task.Delay(HoldMilliseconds);

            if (!Application.isPlaying || activeTransition == null) return;
            FinishTransition();
        }
        catch (Exception ex)
        {
            transitionStarted = false;

            UnityEngine.Debug.LogException(ex);
        }
    }

    private static void FinishTransition()
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log("[Chapter1 Ending] Black screen complete; stopping Play Mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        FixChapter2CameraAfterLoad(scene);

        transitionStarted = false;

        if (transitionRoot != null)
        {
            UnityEngine.Object.Destroy(
                transitionRoot);

            transitionRoot = null;
            fadeGroup = null;
        }
    }

    private static void FixChapter2CameraAfterLoad(Scene loadedScene)
    {
        Camera[] cameras =
            UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        Camera chapter2Camera = null;

        // 優先找「第二章場景本身」裡的 Main Camera。
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];

            if (cam == null)
                continue;

            if (cam.gameObject.scene != loadedScene)
                continue;

            if (cam.gameObject.name == "Main Camera"
                || cam.CompareTag("MainCamera"))
            {
                chapter2Camera = cam;
                break;
            }
        }

        // 如果沒有符合名稱，再退而求其次使用第二章裡第一台 Camera。
        if (chapter2Camera == null)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];

                if (cam != null
                    && cam.gameObject.scene == loadedScene)
                {
                    chapter2Camera = cam;
                    break;
                }
            }
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];

            if (cam == null)
                continue;

            bool isChapter2Camera =
                cam == chapter2Camera;

            // 只要不是第二章要使用的 Camera，
            // 尤其是 DontDestroyOnLoad 留下來的第一章 Camera，
            // 全部關掉，避免它蓋掉第二章畫面。
            if (!isChapter2Camera)
            {
                if (cam.enabled)
                {
                    UnityEngine.Debug.Log(
                        "[Chapter1 Transition] 關閉舊 Camera："
                        + cam.gameObject.name
                        + " / Scene="
                        + cam.gameObject.scene.name);

                    cam.enabled = false;
                }

                AudioListener oldListener =
                    cam.GetComponent<AudioListener>();

                if (oldListener != null)
                    oldListener.enabled = false;
            }
        }

        if (chapter2Camera != null)
        {
            chapter2Camera.enabled = true;

            AudioListener listener =
                chapter2Camera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = true;

            UnityEngine.Debug.Log(
                "[Chapter1 Transition] 第二章使用 Camera："
                + chapter2Camera.gameObject.name
                + " / Scene="
                + chapter2Camera.gameObject.scene.name);
        }
        else
        {
            UnityEngine.Debug.LogError(
                "[Chapter1 Transition] 第二章找不到可用 Camera。");
        }
    }
}
