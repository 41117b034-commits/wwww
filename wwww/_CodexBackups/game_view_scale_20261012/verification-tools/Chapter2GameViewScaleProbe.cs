using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Chapter2GameViewScaleProbe
{
    const string Dir = "_CodexBackups/game_view_scale_20261012/";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type View = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    static int stage;
    static bool originalMaximized;
    static double next;

    static Chapter2GameViewScaleProbe() { EditorApplication.update += Tick; }

    static EditorWindow Current() { return Resources.FindObjectsOfTypeAll(View).OfType<EditorWindow>().FirstOrDefault(); }

    static void Record(string label)
    {
        var view = Current();
        var area = View.GetField("m_ZoomArea", Flags).GetValue(view);
        var scale = (Vector2)View.GetProperty("zoomAreaScale", Flags).GetValue(view);
        var translation = area.GetType().GetProperty("translation", Flags)?.GetValue(area);
        File.AppendAllText(Dir + "live-verification.txt", label + " scale=" + scale.ToString("F6") +
            " translation=" + translation + " maximized=" + view.maximized + " position=" + view.position +
            " playing=" + Application.isPlaying + " dirty=" + SceneManager.GetActiveScene().isDirty +
            " compilationFailed=" + EditorUtility.scriptCompilationFailed + "\n");
        if (Mathf.Abs(scale.x-scale.y)>.001f) throw new Exception("Game preview remains stretched.");
        typeof(EditorWindow).Assembly.GetType("UnityEditor.WindowLayout")
            .GetMethod("SaveWindowLayout", Flags, null, new[]{typeof(string)}, null)
            .Invoke(null, new object[]{Path.GetFullPath(Dir + label + ".dwlt")});
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<next) return;
        next = EditorApplication.timeSinceStartup+2;
        try
        {
            if (stage==0)
            {
                if (File.Exists(Dir+"completed.txt")) { EditorApplication.update-=Tick; return; }
                var view=Current(); if(!view)return;
                if (Application.isPlaying) { File.WriteAllText(Dir+"blocked.txt","Play Mode is active; preview layout was not toggled.");EditorApplication.update-=Tick;return; }
                originalMaximized=view.maximized;
                Chapter2GameViewScaleGuard.Restore();
                Record("initial-corrected");
                view.maximized=false;
            }
            else if(stage==1)
            {
                Chapter2GameViewScaleGuard.Restore(); Record("normal"); Current().maximized=true;
            }
            else if(stage==2)
            {
                Chapter2GameViewScaleGuard.Restore(); Record("maximized"); Current().maximized=originalMaximized;
            }
            else if(stage==3)
            {
                Chapter2GameViewScaleGuard.Restore(); Record("restored");
                typeof(EditorWindow).Assembly.GetType("UnityEditor.WindowLayout").GetMethod("SaveDefaultWindowPreferences",Flags).Invoke(null,null);
                File.WriteAllText(Dir+"completed.txt",DateTime.Now.ToString("O"));
                EditorApplication.update-=Tick;
            }
            stage++;
        }
        catch(Exception e)
        {
            File.WriteAllText(Dir+"error.txt",e.ToString());EditorApplication.update-=Tick;
        }
    }
}
