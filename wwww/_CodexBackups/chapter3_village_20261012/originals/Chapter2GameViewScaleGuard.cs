using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repairs stretched Editor previews without changing scene cameras or assets.</summary>
[InitializeOnLoad]
public static class Chapter2GameViewScaleGuard
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type ViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    static readonly PropertyInfo Scale = ViewType?.GetProperty("zoomAreaScale", Instance);
    static readonly PropertyInfo Minimum = ViewType?.GetProperty("minScale", Instance);
    static readonly FieldInfo Area = ViewType?.GetField("m_ZoomArea", Instance);
    static readonly FieldInfo DefaultScale = ViewType?.GetField("m_defaultScale", Instance);
    static readonly MethodInfo Configure = ViewType?.GetMethod("ConfigureZoomArea", Instance);
    static readonly MethodInfo Constrain = ViewType?.GetMethod("EnforceZoomAreaConstraints", Instance);
    static double nextCheck;
    static bool unavailable;

    static Chapter2GameViewScaleGuard()
    {
        EditorApplication.update += Check;
    }

    static void Check()
    {
        if (unavailable || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < nextCheck)
            return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/第二章.unity")
            return;
        Repair(false);
    }

    [MenuItem("Tools/Chapter 2/Restore Game Preview Proportions")]
    public static void Restore()
    {
        Repair(true);
    }

    static void Repair(bool force)
    {
        if (ViewType == null || Scale == null || Minimum == null || Area == null ||
            DefaultScale == null || Configure == null || Constrain == null)
            return;
        try
        {
            foreach (EditorWindow view in Resources.FindObjectsOfTypeAll(ViewType))
            {
                Vector2 scale = (Vector2)Scale.GetValue(view);
                // Preserve normal user zoom; only unequal/invalid axes trigger automatic repair.
                bool valid = scale.x > 0 && scale.y > 0 &&
                    !float.IsInfinity(scale.x) && !float.IsInfinity(scale.y);
                if (!force && valid && Mathf.Abs(scale.x - scale.y) <= .001f)
                    continue;
                if (view.position.width <= 1 || view.position.height <= 21)
                    continue;
                object area = Area.GetValue(view);
                MethodInfo transform = area?.GetType().GetMethod("SetTransform", Instance);
                if (transform == null)
                    continue;
                Configure.Invoke(view, null);
                float fit = (float)Minimum.GetValue(view);
                if (!(fit > 0) || float.IsInfinity(fit))
                    continue;
                transform.Invoke(area, new object[] { Vector2.zero, Vector2.one * fit });
                Constrain.Invoke(view, null);
                DefaultScale.SetValue(view, fit);
                view.Repaint();
            }
        }
        catch (Exception e)
        {
            // Unity's preview APIs are internal: report once if a future Editor changes them.
            unavailable = true;
            Debug.LogWarning("Chapter 2 preview scale repair is unavailable: " + e.GetBaseException().Message);
        }
    }
}
