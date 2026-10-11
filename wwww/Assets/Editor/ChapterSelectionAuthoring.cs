using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ChapterSelectionAuthoring
{
    [MenuItem("Tools/Chapter Selection/Apply Four Chapters")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.name.Contains("選擇章節"))throw new InvalidOperationException("Open the chapter selection scene first.");
        var labels=Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        string[] oldNames={"第一章","第二章","第三章","第四章","第五章"};
        var headings=oldNames.Select(n=>labels.FirstOrDefault(t=>t.text.Trim().StartsWith(n))).ToArray();
        if(!headings[0]||!headings[1])throw new InvalidOperationException("Chapter labels missing.");
        var canvas=headings[0].GetComponentInParent<Canvas>();
        if(headings[4])
        {
            // Legacy scene includes nested buttons and a detached fifth heading.
            // Resolve all five, then detach them before removing the old third card.
            var all=canvas.GetComponentsInChildren<Button>(true);
            var buttons=new Button[5];
            for(int i=0;i<5;i++)if(headings[i])buttons[i]=headings[i].GetComponentInParent<Button>();
            for(int i=0;i<5;i++)if(headings[i]&&!buttons[i])
                buttons[i]=all.Where(b=>!buttons.Contains(b)).OrderBy(b=>(b.transform.position-headings[i].transform.position).sqrMagnitude).First();
            for(int i=0;i<5;i++)
            {
                if(!buttons[i]||!headings[i])throw new InvalidOperationException("Legacy chapter card missing: "+i);
                buttons[i].transform.SetParent(canvas.transform,true);
                headings[i].transform.SetParent(buttons[i].transform,true);
            }
            Object.DestroyImmediate(buttons[2].gameObject);
            headings[3].text="第三章";headings[4].text="第四章";
        }
        MenuPresentationController.ArrangeChapterSelection();
        var chapter2=canvas.GetComponentsInChildren<TextMeshProUGUI>(true).First(t=>t.text.Trim()=="第二章").GetComponentInParent<Button>();
        var loader=Object.FindFirstObjectByType<tiao>();
        if(!loader)loader=canvas.gameObject.AddComponent<tiao>();
        bool linked=false;
        for(int i=0;i<chapter2.onClick.GetPersistentEventCount();i++)
            linked|=chapter2.onClick.GetPersistentTarget(i)==loader&&chapter2.onClick.GetPersistentMethodName(i)==nameof(tiao.JumpToChapter2);
        if(!linked)UnityEventTools.AddPersistentListener(chapter2.onClick,loader.JumpToChapter2);
        EditorUtility.SetDirty(chapter2);EditorUtility.SetDirty(loader);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
