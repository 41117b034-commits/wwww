using System;
using UnityEngine;

// Reference: Chapter 1's male wedding guest (Assets/音樂/喝.mp3).
// Its measured voiced median is about 148 Hz; the official Tgdaya sample is
// about 156 Hz. A small playback shift retains the original pronunciation and
// intonation. This is a mix/style adjustment, not a cloned speaker identity.
public static class Chapter3VoiceTone
{
    public const float PlaybackPitch=.95f;
    public const float PlaybackVolume=.85f;

    // Keep the official source assets untouched. Prepare once when a line is
    // first loaded; both offline and freshly synthesized lines take this path.
    public static AudioClip Prepare(AudioClip source)
    {
        if(!source)return null;
        var data=new float[source.samples*source.channels];
        if(!source.GetData(data,0))return source;
        Process(data,source.channels,source.frequency);
        var clip=AudioClip.Create(source.name+" (Chapter 1 male tone)",source.samples,source.channels,source.frequency,false);
        clip.SetData(data,0);return clip;
    }

    public static void Process(float[] data,int channels,int rate)
    {
        if(data==null||data.Length==0||channels<1||rate<8000)return;
        // Modest body and a softer high register, without indoor reverb or
        // distortion on the outdoor dialogue. Each channel has its own state.
        for(int channel=0;channel<channels;channel++)
        {
            var body=Shelf(rate,220,1.5f,false);
            var presence=Shelf(rate,3200,-2.5f,true);
            float previous=0,highpass=0;
            float decay=Mathf.Exp(-2*Mathf.PI*65/rate);
            for(int i=channel;i<data.Length;i+=channels)
            {
                float input=data[i];highpass=decay*(highpass+input-previous);previous=input;
                data[i]=presence.Next(body.Next(highpass));
            }
        }
        // Match voiced-level consistency, excluding pauses from the estimate.
        // Limit gain so a quiet/short recording cannot be amplified excessively.
        double sum=0;int count=0;int window=Math.Max(channels,rate/50*channels);
        for(int start=0;start<data.Length;start+=window)
        {
            int end=Math.Min(start+window,data.Length);double power=0;
            for(int i=start;i<end;i++)power+=data[i]*data[i];
            if(power/(end-start)>.000324){sum+=power;count+=end-start;}
        }
        float gain=count>0?Mathf.Clamp(.115f/(float)Math.Sqrt(sum/count),.7f,1.2f):1;
        for(int i=0;i<data.Length;i++)
        {
            float sample=data[i]*gain,level=Mathf.Abs(sample);
            // Smoothly contain the original TTS's near-full-scale transients.
            if(level>.72f)sample=Mathf.Sign(sample)*(.72f+.18f*(1-Mathf.Exp(-(level-.72f)/.18f)));
            data[i]=sample;
        }
    }

    struct Biquad
    {
        public float b0,b1,b2,a1,a2;
        float x1,x2,y1,y2;
        public float Next(float x){float y=b0*x+b1*x1+b2*x2-a1*y1-a2*y2;x2=x1;x1=x;y2=y1;y1=y;return y;}
    }
    static Biquad Shelf(int rate,float frequency,float db,bool high)
    {
        float a=Mathf.Pow(10,db/40),c=Mathf.Cos(2*Mathf.PI*frequency/rate);
        float s=Mathf.Sin(2*Mathf.PI*frequency/rate),beta=2*Mathf.Sqrt(a)*s/Mathf.Sqrt(2);
        float a0=high?(a+1)-(a-1)*c+beta:(a+1)+(a-1)*c+beta;
        return new Biquad{
            b0=a*((a+1)+(high?1:-1)*(a-1)*c+beta)/a0,
            b1=(high?-2:2)*a*((a-1)+(high?1:-1)*(a+1)*c)/a0,
            b2=a*((a+1)+(high?1:-1)*(a-1)*c-beta)/a0,
            a1=(high?2:-2)*((a-1)+(high?-1:1)*(a+1)*c)/a0,
            a2=((a+1)+(high?-1:1)*(a-1)*c-beta)/a0
        };
    }
}
