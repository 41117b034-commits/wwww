using System;
using UnityEditor;
using UnityEngine;

public static class Chapter2ForestReview
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BeforeStart()
    {
        if(SessionState.GetBool("ForestReview",false))UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>().saveResult=false;
    }
    public static void Run(string shot)
    {
        if(shot=="start"){SessionState.SetBool("ForestReview",true);EditorApplication.isPlaying=true;return;}
        if(shot=="clear")
        {
            SessionState.EraseBool("ForestReview");
            typeof(Editor).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public).Invoke(null,null);
            return;
        }
        if(!Application.isPlaying)throw new InvalidOperationException("Review requires Play Mode.");
        var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        var camera=c.player.view;var position=camera.transform.position;var rotation=camera.transform.rotation;
        bool ui=c.ui.gameObject.activeSelf;
        try
        {
            c.ui.gameObject.SetActive(false);
            if(shot=="ground")
            {camera.transform.position=new Vector3(-.5f,1.1f,-12.5f);camera.transform.LookAt(new Vector3(-.3f,0,-10.6f));}
            else if(shot=="flowers")
            {camera.transform.position=new Vector3(2.4f,1.05f,-11.5f);camera.transform.LookAt(new Vector3(3.9f,.18f,-9));}
            else if(shot=="npc")
            {
                var actor=GameObject.Find("林間族人 1").transform;
                camera.transform.position=actor.position+new Vector3(2.7f,1.5f,-3.4f);camera.transform.LookAt(actor.position+Vector3.up*.8f);
            }
            Chapter2WorkProbe.Shot("detail-"+shot);
        }
        finally{camera.transform.SetPositionAndRotation(position,rotation);c.ui.gameObject.SetActive(ui);}
    }
}
