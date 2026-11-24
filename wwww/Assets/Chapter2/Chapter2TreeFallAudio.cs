using UnityEngine;
using UnityEngine.Audio;

// Separate component: observes the existing tree motion without changing the story.
[DisallowMultipleComponent]
public sealed class Chapter2TreeFallAudio : MonoBehaviour
{
    public Chapter2Controller chapter;
    [Header("倒樹音效")]
    [Tooltip("樹開始傾倒時播放一次。完整倒樹音效也可放這裡。")]
    public AudioClip fallClip;
    [Range(0,1)] public float fallVolume=.8f;
    [Tooltip("選用：到達最終倒下角度時播放。若 Fall Clip 已含撞地聲，可留空。")]
    public AudioClip impactClip;
    [Range(0,1)] public float impactVolume=.85f;
    public AudioMixerGroup output;
    [Header("配合原本82度的倒樹動畫")]
    [Min(.01f)] public float startAngle=.05f;
    [Range(1,180)] public float finalAngle=82;
    Transform watchedTree;
    Quaternion upright;
    AudioSource fallSource,impactSource;
    bool started,landed;
    void Awake()
    {
        if(!chapter)chapter=GetComponent<Chapter2Controller>();
        fallSource=MakeSource("Tree fall sound");impactSource=MakeSource("Tree landing sound");
        CaptureTree();
    }
    AudioSource MakeSource(string label)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);
        var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
        source.spatialBlend=0;return source;
    }
    void CaptureTree()
    {
        if(!chapter||!chapter.sacredTree)return;
        watchedTree=chapter.sacredTree;upright=watchedTree.rotation;started=landed=false;
    }
    void LateUpdate()
    {
        if(!chapter)return;
        if(chapter.sacredTree!=watchedTree)CaptureTree();
        if(!watchedTree)return;
        if(chapter.CurrentStage!=Chapter2Controller.Stage.Consequence||chapter.TreeDecision!=1)
        {
            upright=watchedTree.rotation;started=landed=false;return;
        }
        float angle=Quaternion.Angle(upright,watchedTree.rotation);
        if(!started&&angle>=Mathf.Max(.01f,startAngle))
        {started=true;Play(fallSource,fallClip,fallVolume);}
        if(started&&!landed&&angle>=Mathf.Max(startAngle,finalAngle-.05f))
        {landed=true;Play(impactSource,impactClip,impactVolume);}
        if(fallSource){fallSource.volume=Mathf.Clamp01(fallVolume);fallSource.outputAudioMixerGroup=output;}
        if(impactSource){impactSource.volume=Mathf.Clamp01(impactVolume);impactSource.outputAudioMixerGroup=output;}
    }
    void Play(AudioSource source,AudioClip clip,float volume)
    {
        if(!source||!clip)return;
        source.clip=clip;source.volume=Mathf.Clamp01(volume);source.outputAudioMixerGroup=output;source.Play();
    }
    void OnDisable()
    {if(fallSource)fallSource.Stop();if(impactSource)impactSource.Stop();}
    void OnDestroy()
    {if(fallSource)Destroy(fallSource.gameObject);if(impactSource)Destroy(impactSource.gameObject);}
}
