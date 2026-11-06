using UnityEngine;

// One continuous, non-verbal sports cue shared by the approach and calm school.
// Forest audio remains owned by Chapter3SchoolApproach.
public sealed class Chapter3ApproachAudio : MonoBehaviour
{
    const int SampleRate=22050, Seconds=12;
    const float FullVolume=.24f;
    Chapter3SchoolController school;
    Transform listener;
    AudioSource source;
    AudioClip generatedClip;
    bool running,quieting;
    float progress;

    public AudioSource Source=>source;
    // Latched only after a positive-volume playback; Suspend resets it.
    public bool Started {get;private set;}

    public void Configure(Chapter3SchoolController owner)
    {
        Suspend();school=owner;
        if(source&&school)
        {
            source.transform.SetParent(school.transform,true);
            source.transform.position=new Vector3(0,1.8f,22);
        }
    }
    public void Begin(Transform listenerTransform)
    {
        Suspend();
        if(!school||!listenerTransform)return;
        listener=listenerTransform;
        EnsureSource();
        running=true;quieting=false;
        SetProgress(0);
    }
    public void SetProgress(float hearingProgress)
    {
        if(!running||quieting||!source)return;
        progress=float.IsNaN(hearingProgress)?0:Mathf.Clamp01(hearingProgress);
        source.volume=FullVolume*Mathf.SmoothStep(0,1,progress);
        if(source.volume>.0001f&&listener&&listener.gameObject.activeInHierarchy&&source.isActiveAndEnabled)
        {
            source.mute=false;
            if(!source.isPlaying)source.Play();
            if(source.isPlaying)Started=true;
        }
    }
    public void Complete()
    {
        // Retain the same source, sample cursor and clip across the handoff.
        SetProgress(1);
    }
    public void Suspend()
    {
        running=quieting=Started=false;progress=0;listener=null;
        if(source){source.Stop();source.volume=0;source.mute=true;}
    }
    void Update()
    {
        if(!running)return;
        if(!school||!school.gameObject.activeInHierarchy||!listener||!listener.gameObject.activeInHierarchy)
        {Suspend();return;}
        var battle=school.Battle;
        if(battle&&battle.CurrentStage!=Chapter3SchoolBattle.Stage.Inactive
            &&battle.CurrentStage!=Chapter3SchoolBattle.Stage.AwaitingTado)quieting=true;
        if(quieting&&source)
        {
            source.volume=Mathf.MoveTowards(source.volume,0,.6f*Time.unscaledDeltaTime);
            if(source.volume<=0){source.Stop();source.mute=true;running=false;}
        }
    }
    void EnsureSource()
    {
        if(!generatedClip)generatedClip=BuildClip();
        if(!source)
        {
            var go=new GameObject("School sports ambience · continuous approach");
            go.transform.SetParent(school.transform,false);
            go.transform.position=new Vector3(0,1.8f,22);
            source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Linear;
            source.minDistance=14;source.maxDistance=180;
            source.dopplerLevel=0;source.spread=80;source.priority=160;
            source.reverbZoneMix=.15f;
        }
        source.clip=generatedClip;source.volume=0;source.mute=true;
    }
    static AudioClip BuildClip()
    {
        // About 1 MiB, generated once per component. No speech samples, external
        // assets, Unity Random state, streaming callbacks or per-frame synthesis.
        var samples=new float[SampleRate*Seconds];
        uint seed=0x35A17B4Du;
        float low=0,mid=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)SampleRate;
            seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;
            float noise=(seed&65535)/32767.5f-1;
            low+=.025f*(noise-low);mid+=.24f*(noise-mid);
            // A soft, slowly changing diffuse bed without intelligible voices.
            float swell=.72f+.18f*Mathf.Sin(2*Mathf.PI*t/3f)+.10f*Mathf.Sin(2*Mathf.PI*t/4f);
            float value=(low*.14f+(mid-low)*.035f)*swell;
            // Loose applause clusters: short filtered noise, not weapon reports.
            float cluster=t%4;
            for(int clap=0;clap<6;clap++)
            {
                float age=cluster-(.42f+clap*.23f+(clap%2)*.035f);
                if(age>=0&&age<.13f)
                {
                    float envelope=Mathf.Min(age/.008f,1)*Mathf.Exp(-age*35);
                    value+=(noise-mid)*envelope*(.13f-clap*.008f);
                }
            }
            // Two modest referee-whistle bursts per loop, with soft edges.
            float whistle=t%6-1.65f;
            if(whistle>=0&&whistle<.38f)
            {
                float envelope=Mathf.Sin(Mathf.PI*whistle/.38f);
                float phase=2*Mathf.PI*(2050*whistle+.6f*Mathf.Sin(2*Mathf.PI*17*whistle));
                value+=(Mathf.Sin(phase)+.22f*Mathf.Sin(phase*2.01f))*envelope*.065f;
            }
            // Quiet loop seam; no discontinuity or sharp start on first hearing.
            float edge=Mathf.Clamp01(Mathf.Min(t,Seconds-t)/.08f);
            samples[i]=Mathf.Clamp(value*edge,-.5f,.5f);
        }
        var clip=AudioClip.Create("School sports · whistle, soft applause and diffuse crowd",samples.Length,1,SampleRate,false);
        clip.SetData(samples,0);
        return clip;
    }
    void OnDisable(){Suspend();}
    void OnDestroy()
    {
        Suspend();
        if(source)Destroy(source.gameObject);
        if(generatedClip)Destroy(generatedClip);
    }
}
