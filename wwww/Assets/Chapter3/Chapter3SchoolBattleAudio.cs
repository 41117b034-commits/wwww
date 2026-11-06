using UnityEngine;

// Licensed human crowd recording; one preloaded source, no per-frame allocations.
public sealed class Chapter3SchoolBattleAudio : MonoBehaviour
{
    Chapter3SchoolAmbient crowd;
    Chapter3SchoolController school;
    AudioSource source;
    public AudioSource Source=>source;
    public void Configure(Chapter3SchoolAmbient population,Chapter3SchoolController owner)
    {
        crowd=population;school=owner;
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
        source.clip=Resources.Load<AudioClip>("Chapter3/Audio/CrowdPanic");
        if(source.clip)source.clip.LoadAudioData();
        source.loop=true;source.spatialBlend=0;source.volume=0;source.priority=180;
    }
    void Update()
    {
        if(!source||!crowd||!school)return;
        bool battle=crowd.Panicking&&!crowd.Withdrawing;
        if(battle&&!source.isPlaying&&source.clip)source.Play();
        float distance=Vector3.ProjectOnPlane(school.player.transform.position-new Vector3(0,0,28),Vector3.up).magnitude;
        float volume=battle?.38f*Mathf.Clamp01(1-Mathf.Max(0,distance-20)/60):0;
        if(school.ui.VoiceBusy)volume*=.24f;
        if(source.isPlaying)volume*=Mathf.Min(Mathf.Clamp01(source.time/.4f),Mathf.Clamp01((source.clip.length-source.time)/.7f));
        source.volume=Mathf.MoveTowards(source.volume,volume,Time.unscaledDeltaTime*.6f);
        if(!battle&&source.volume<=0)source.Stop();
    }
    void OnDisable(){if(source){source.Stop();source.volume=0;}}
}
