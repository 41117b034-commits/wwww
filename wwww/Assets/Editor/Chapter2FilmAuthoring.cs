using System;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

// Offline Unity render to a replaceable H.264 clip. It is an authored story introduction,
// not historical footage. No dependency on a third-party encoder or network at runtime.
public static class Chapter2FilmAuthoring
{
    static readonly string[] Captions={
        "新規定與秘密會議\n霧社，一九三〇年。",
        "警察要求族人集合，宣讀新的命令。\n搬運木材不准拖曳，必須肩扛下山。",
        "十二尺長、四寸見方的檜木柱，壓在族人的肩上。\n陡坡難行，途中甚至必須過夜。",
        "原本一天的工資，分作兩天發放。\n一天只有二十錢，族人懷疑工資遭到剋扣。",
        "警察進一步命令族人，前往聖地「西仔希克」伐木。\n被視為守護者的巨木，也成了砍伐的目標。",
        "槍口與鞭子，逼著族人走向聖地。\n這個早晨，你也在隊伍之中。"
    };
    static MediaEncoder encoder;
    static RenderTexture target;
    static Texture2D pixels;
    static Chapter2Controller controller;
    static Vector3 originalPosition;
    static Quaternion originalRotation;
    static int frame;
    const int FPS=15, Seconds=48;
    const string Path="Assets/Chapter2/Media/Chapter2_Opening.mp4";
    [MenuItem("Tools/Chapter 2/Render Opening Film")]
    public static void Record()
    {
        if(EditorApplication.isPlaying||encoder!=null)return;
        controller=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
        if(!controller)throw new InvalidOperationException("Open the Chapter 2 scene first.");
        originalPosition=controller.player.view.transform.position;originalRotation=controller.player.view.transform.rotation;
        target=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGBA32,false);
        encoder=new MediaEncoder(Path,new VideoTrackAttributes{frameRate=new MediaRational(FPS),width=1280,height=720,includeAlpha=false});
        frame=0;controller.ui.chapter.text="第二章  /  新規定與秘密會議";controller.ui.objective.text="";controller.ui.hint.text="E／空白鍵／右手扳機  略過影片";
        controller.player.view.targetTexture=target;
        EditorApplication.update+=Encode;
    }
    static void Encode()
    {
        try
        {
            int shot=frame/(FPS*8);float u=(frame%(FPS*8))/(FPS*8f);
            Vector3[] starts={new Vector3(-11,5,-12),new Vector3(7,2.8f,-6),new Vector3(8,1.3f,2),new Vector3(-7,2.4f,-8),new Vector3(-6,5,3),new Vector3(0,1.65f,-17)};
            Vector3[] ends={new Vector3(-8,4,-8),new Vector3(5,2,-3),new Vector3(6.6f,1.6f,4),new Vector3(-5,2,-5),new Vector3(-4,7,5),new Vector3(0,1.65f,-14)};
            Vector3[] looks={new Vector3(0,7,12),new Vector3(1,1.3f,7),new Vector3(5.8f,.4f,6),new Vector3(-3,1,-9),new Vector3(0,15,12),new Vector3(0,2,12)};
            var camera=controller.player.view;camera.transform.position=Vector3.Lerp(starts[shot],ends[shot],u);camera.transform.LookAt(looks[shot]);
            controller.ui.Line(shot==0?"第二章":"新規定",Captions[shot]);
            float alpha=Mathf.Max(1-Mathf.Clamp01(u*10),Mathf.Clamp01((u-.9f)*10));controller.ui.fade.color=new Color(0,0,0,alpha);
            Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=old;encoder.AddFrame(pixels);
            frame++;
            if(frame%120==0)File.WriteAllText("_CodexBackups/chapter2_20261004/film-progress.txt",frame+" / "+FPS*Seconds);
            if(frame>=FPS*Seconds) Finish();
        }
        catch(Exception e){Debug.LogException(e);Finish();}
    }
    static void Finish()
    {
        EditorApplication.update-=Encode;encoder?.Dispose();encoder=null;
        controller.player.view.targetTexture=null;controller.player.view.transform.SetPositionAndRotation(originalPosition,originalRotation);
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        controller.ui.Line("","");controller.ui.hint.text="";controller.ui.fade.color=Color.clear;
        AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        controller.openingFilm=AssetDatabase.LoadAssetAtPath<VideoClip>(Path);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);EditorSceneManager.SaveScene(controller.gameObject.scene);
        File.WriteAllText("_CodexBackups/chapter2_20261004/film-result.txt","frames="+frame+" clip="+(controller.openingFilm!=null));
    }
}
