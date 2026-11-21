#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only runtime diagnostic. Never saves audio changes into the scene.
public sealed class Chapter3SoundFinder : EditorWindow
{
    enum Mode { None, MuteAll, Solo, MuteOne }
    readonly Dictionary<AudioSource,bool> savedMute=new Dictionary<AudioSource,bool>();
    AudioSource[] sources=new AudioSource[0];
    AudioSource target;
    Mode mode;
    Vector2 scroll;
    double refreshAt;
    string filter="";
    [MenuItem("Tools/聲音/尋找逼聲來源")]
    public static void Open(){GetWindow<Chapter3SoundFinder>("尋找逼聲來源");}
    void OnEnable()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=PlayState;
    }
    void OnDisable()
    {
        Restore();EditorApplication.update-=Tick;
        EditorApplication.playModeStateChanged-=PlayState;
    }
    void PlayState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.ExitingPlayMode)Restore();
    }
    void Refresh()
    {
        var found=new List<AudioSource>();
        foreach(var source in Resources.FindObjectsOfTypeAll<AudioSource>())
            if(source&&source.gameObject.scene.IsValid())found.Add(source);
        found.Sort((a,b)=>
        {
            int active=b.isPlaying.CompareTo(a.isPlaying);
            return active!=0?active:string.Compare(a.name,b.name,StringComparison.Ordinal);
        });
        sources=found.ToArray();
    }
    void Tick()
    {
        if(EditorApplication.timeSinceStartup>=refreshAt)
        {refreshAt=EditorApplication.timeSinceStartup+.25;Refresh();Repaint();}
        if(!Application.isPlaying||mode==Mode.None)return;
        if(mode!=Mode.MuteAll&&!target){Restore();return;}
        // Enforce temporary muting even when game scripts unmute their sources.
        foreach(var source in sources)
        {
            if(!source)continue;
            if(!savedMute.ContainsKey(source))savedMute.Add(source,source.mute);
            switch(mode)
            {
                case Mode.MuteAll:source.mute=true;break;
                case Mode.Solo:source.mute=source!=target;break;
                case Mode.MuteOne:source.mute=source==target||savedMute[source];break;
            }
        }
    }
    void Choose(Mode value,AudioSource source=null)
    {
        Restore();Refresh();
        foreach(var item in sources)if(item)savedMute[item]=item.mute;
        mode=value;target=source;
    }
    void Restore()
    {
        foreach(var item in savedMute)if(item.Key)item.Key.mute=item.Value;
        savedMute.Clear();mode=Mode.None;target=null;
    }
    static string ObjectPath(Transform value)
    {
        string result=value.name;
        while(value.parent){value=value.parent;result=value.name+"/"+result;}
        return result;
    }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("先按 Play，等逼聲出現。按「全部暫時靜音」確認，再用各列「只聽這個／只關這個」找來源。關閉視窗或停止 Play 會恢復原本的 Mute。",MessageType.Info);
        if(!Application.isPlaying)EditorGUILayout.HelpBox("請先啟動遊戲。本工具只在 Play 模式測試聲音。",MessageType.Warning);
        using(new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("全部暫時靜音"))Choose(Mode.MuteAll);
            if(GUILayout.Button("恢復全部聲音"))Restore();
            EditorGUILayout.EndHorizontal();
            string state=mode==Mode.None?"原本狀態":mode==Mode.MuteAll?"全部靜音":(mode==Mode.Solo?"只聽：":"只關：")+(target?target.name:"");
            EditorGUILayout.LabelField("目前測試",state);
            filter=EditorGUILayout.TextField("搜尋物件或音檔",filter);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var source in sources)
            {
                if(!source)continue;
                string clip=source.clip?source.clip.name:"無固定 Clip（可能使用 PlayOneShot）";
                string path=ObjectPath(source.transform);
                if(filter.Length>0&&(path+" "+clip).IndexOf(filter,StringComparison.OrdinalIgnoreCase)<0)continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField((source.isPlaying?"▶ 播放中  ":"停止  ")+source.name,EditorStyles.boldLabel);
                EditorGUILayout.LabelField("音檔",clip);
                EditorGUILayout.LabelField("音量 / 循環",source.volume.ToString("0.00")+" / "+(source.loop?"是":"否"));
                EditorGUILayout.LabelField("路徑",path);
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("只聽這個"))Choose(Mode.Solo,source);
                if(GUILayout.Button("只關這個"))Choose(Mode.MuteOne,source);
                if(GUILayout.Button("選取物件")){Selection.activeGameObject=source.gameObject;EditorGUIUtility.PingObject(source.gameObject);}
                if(GUILayout.Button("複製資訊"))EditorGUIUtility.systemCopyBuffer="音源："+source.name+"\n音檔："+clip+"\n路徑："+path;
                EditorGUILayout.EndHorizontal();EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
