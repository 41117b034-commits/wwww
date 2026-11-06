using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static partial class DialogueFireVerify
{
    const string ReadableLine="槍打完一發，就要再裝一發。裝填時躲好。";
    static IEnumerator UIAudit()
    {
        Object.DontDestroyOnLoad(driver.gameObject);
        foreach(string chapter in new[]{"第三章","第二章","第一章新版警察"})
        {
            if(SceneManager.GetActiveScene().name!=chapter)yield return SceneManager.LoadSceneAsync("Assets/Scenes/"+chapter+".unity");
            yield return Frames(12);
            Camera camera;Canvas canvas;RectTransform textRect;
            if(chapter=="第三章")
            {
                f=Object.FindFirstObjectByType<Chapter3Flow>();f.station.Suspend();f.ui.chat.SetActive(false);f.ui.fade.color=Color.clear;
                f.ui.Say("比胡 · 瓦歷斯",ReadableLine);camera=f.player.view;canvas=f.ui.canvas;textRect=f.ui.subtitle.rectTransform;
                f.player.canMove=f.player.canLook=false;
            }
            else if(chapter=="第二章")
            {
                var ui=Object.FindFirstObjectByType<Chapter2Presentation>();camera=ui.view;canvas=ui.canvas;textRect=ui.subtitle.rectTransform;
                foreach(var c in Object.FindObjectsByType<Chapter2Controller>(FindObjectsSortMode.None)){c.StopAllCoroutines();c.enabled=false;}
                ui.fade.color=Color.clear;ui.videoImage.gameObject.SetActive(false);ui.endingPanel.SetActive(false);ui.Line("族人",ReadableLine);
            }
            else
            {
                var bridge=Object.FindFirstObjectByType<Chapter1StorySubtitleBridge>();bridge.onlyFivePoliceLines=false;
                foreach(var c in Object.FindObjectsByType<Chapter1PerformanceController>(FindObjectsSortMode.None)){c.StopAllCoroutines();c.enabled=false;}
                bridge.ShowFromStory("",ReadableLine,90);camera=bridge.vrCamera;
                canvas=camera.transform.Find("StorySubtitleCanvas_Auto").GetComponent<Canvas>();textRect=canvas.GetComponentInChildren<TMP_Text>().rectTransform;
            }
            yield return Frames(4);
            report.notes.Add(chapter+" canvases="+string.Join(";",Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.name+" mode="+c.renderMode+" guard="+(bool)c.GetComponent<GameDialogueOverlay>())));
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Temporary opaque subtitle occluder";blocker.transform.SetParent(camera.transform,false);
            blocker.transform.localPosition=new Vector3(0,0,.2f);blocker.transform.localScale=new Vector3(2,2,.03f);
            var mat=new Material(Shader.Find("Unlit/Color"));mat.color=new Color(.25f,.01f,.01f,1);blocker.GetComponent<Renderer>().sharedMaterial=mat;
            camera.nearClipPlane=.03f;
            var overlays=canvas.GetComponentsInChildren<GameDialogueOverlayGraphic>(true);
            foreach(var item in overlays)item.enabled=false;yield return Frames(3);
            int hidden=ReadPixels(camera,textRect,chapter+"-occluded-before");
            foreach(var item in overlays)item.enabled=true;yield return Frames(3);
            int visible=ReadPixels(camera,textRect,chapter+"-readable-after");
            report.notes.Add(chapter+" white text pixels through foreground geometry="+hidden+" -> "+visible);
            Check(chapter+" text remains visible through nearer opaque geometry",visible>100&&visible<10000&&visible>hidden+100);
            Check(chapter+" every shown graphic has overlay material",canvas.GetComponentsInChildren<Graphic>().All(g=>!g.enabled||g.materialForRendering.shader.name.StartsWith("Wushe/Readable Dialogue")));
            Object.Destroy(blocker);Object.Destroy(mat);yield return Frames(2);
            if(chapter=="第三章")
            {
                f.player.enabled=false;f.ui.ConfigureVR(true);Canvas.ForceUpdateCanvases();Check("third chapter world UI retains readable material",f.ui.canvas.renderMode==RenderMode.WorldSpace&&f.ui.subtitle.materialForRendering.shader.name.StartsWith("Wushe/"));
                f.ui.ConfigureVR(false);yield return Frames(3);Check("third chapter desktop layout restores",f.ui.canvas.renderMode==RenderMode.ScreenSpaceCamera);
                f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterCombat();f.combat.StopAllCoroutines();Call(f.combat,"StartFighting",true);yield return Frames(4);
                f.combat.enabled=false;f.combat.weapons.gameObject.SetActive(false);f.combat.hud.SetActive(false);f.ui.fade.color=Color.clear;
                var actor=f.combat.bawan;var head=actor.Rig.Head;var forward=actor.Rig.Forward;
                f.ui.Say("比胡 · 瓦歷斯",ReadableLine);camera.transform.position=head.position-forward*.32f+Vector3.right*.22f;
                camera.transform.rotation=Quaternion.LookRotation(forward);f.player.canLook=f.player.canMove=false;
                yield return Frames(3);int npcText=ReadPixels(camera,f.ui.subtitle.rectTransform,"third-chapter-near-npc-subtitle");
                Check("near NPC dialogue retains complete visible subtitle",npcText>100&&npcText<10000&&f.ui.subtitle.text==ReadableLine&&f.ui.subtitle.materialForRendering.shader.name.StartsWith("Wushe/"));
            }
        }
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/第三章.unity");yield return Frames(5);
    }
    static int ReadPixels(Camera camera,RectTransform text,string name)
    {
        Canvas.ForceUpdateCanvases();var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());
            var corners=new Vector3[4];text.GetWorldCorners(corners);var screen=corners.Select(camera.WorldToViewportPoint).ToArray();
            int x0=Mathf.Clamp(Mathf.FloorToInt(screen.Min(p=>p.x)*1280),0,1279),x1=Mathf.Clamp(Mathf.CeilToInt(screen.Max(p=>p.x)*1280),0,1279);
            int y0=Mathf.Clamp(Mathf.FloorToInt(screen.Min(p=>p.y)*720),0,719),y1=Mathf.Clamp(Mathf.CeilToInt(screen.Max(p=>p.y)*720),0,719),count=0;
            var pixels=tex.GetPixels32();for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){var c=pixels[y*1280+x];if(c.r>190&&c.g>190&&c.b>175)count++;}
            return count;
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(tex);}
    }
}
