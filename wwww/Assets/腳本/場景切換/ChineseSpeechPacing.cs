using System;
using System.Collections.Generic;
using UnityEngine;

// Shorten only near-silent gaps in generated Mandarin. Keep the original
// voices, pitch, spoken samples, subtitles and authored assets intact.
public static class ChineseSpeechPacing
{
    const float QuietPeak=.0015f;
    struct Cut { public int start,end; public Cut(int a,int b){start=a;end=b;} }
    public static AudioClip Prepare(AudioClip source)
    {
        if(!source||source.samples<1||source.channels<1)return source;
        var samples=new float[source.samples*source.channels];
        if(!source.GetData(samples,0))return source;
        int block=Mathf.Max(1,source.frequency/100),start=-1;
        var cuts=new List<Cut>();
        bool audible=false;
        for(int frame=0;frame<source.samples;frame+=block)
        {
            int end=Mathf.Min(frame+block,source.samples);bool quiet=true;
            for(int i=frame*source.channels;i<end*source.channels;i++)
                if(Mathf.Abs(samples[i])>QuietPeak){quiet=false;break;}
            if(quiet){if(start<0)start=frame;}
            else
            {
                audible=true;
                if(start>=0)AddCut(cuts,start,frame,source.frequency,false);
                start=-1;
            }
        }
        // Leave invalid/all-silent recordings untouched for normal diagnostics.
        if(!audible)return source;
        if(start>=0)AddCut(cuts,start,source.samples,source.frequency,true);
        int removed=0;foreach(var cut in cuts)removed+=cut.end-cut.start;
        if(removed==0)return source;
        var result=new float[(source.samples-removed)*source.channels];
        int read=0,write=0;
        foreach(var cut in cuts)
        {
            int count=(cut.start-read)*source.channels;
            Array.Copy(samples,read*source.channels,result,write,count);
            write+=count;read=cut.end;
        }
        Array.Copy(samples,read*source.channels,result,write,(source.samples-read)*source.channels);
        var clip=AudioClip.Create(source.name+" (natural pauses)",result.Length/source.channels,source.channels,source.frequency,false);
        if(!clip.SetData(result,0)){UnityEngine.Object.Destroy(clip);return source;}
        return clip;
    }
    static void AddCut(List<Cut> cuts,int start,int end,int rate,bool trailing)
    {
        int length=end-start;
        if(start==0&&!trailing)
        {
            int keep=Mathf.RoundToInt(rate*.06f);
            if(length>keep)cuts.Add(new Cut(start,end-keep));
        }
        else if(trailing)
        {
            int keep=Mathf.RoundToInt(rate*.10f);
            if(length>keep)cuts.Add(new Cut(start+keep,end));
        }
        else if(length>rate*.60f)
        {
            // Preserve 160 ms at each edge, including quiet consonant tails.
            int edge=Mathf.RoundToInt(rate*.16f);
            cuts.Add(new Cut(start+edge,end-edge));
        }
    }
}
