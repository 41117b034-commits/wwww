using System;
using UnityEngine;

// Runs after the dramatic hands/mesh selection. Facial shapes coexist with each
// grip/fist/refusal variant; no camera or exploration-greeting behavior is changed.
[DefaultExecutionOrder(4490)]
[DisallowMultipleComponent]
public sealed class Chapter2SpeechFace:MonoBehaviour
{
    public const string Jaw="Story speech jaw",Angry="Story anger",Worried="Story worry",Closed="Story eyes closed";
    [Serializable] public class Surface {
        public SkinnedMeshRenderer body,renderer;
        public Chapter2FaceCut cut;
        public Transform head;
        public Vector3 origin,right,up,forward;
    }
    public Surface[] surfaces;
    public float MouthWeight {get;private set;}
    public float AngerWeight {get;private set;}
    public float WorryWeight {get;private set;}
    public string Expression {get;private set;}
    Chapter2Actor actor;
    Chapter2CouncilDrama drama;
    AudioSource voice;
    bool recorded;
    float began,ends,voicePeak;
    string words="";
    readonly float[] samples=new float[256];
    MaterialPropertyBlock block;
    void Start(){
        actor=GetComponent<Chapter2Actor>();drama=GetComponent<Chapter2CouncilDrama>();
        block=new MaterialPropertyBlock();
    }
    public void BeginLine(string text,float seconds,Chapter2AudioManager audio)
    {
        words=text??"";began=Time.unscaledTime;ends=began+Mathf.Max(.1f,seconds);voicePeak=0;
        voice=null;
        if(audio)foreach(var source in audio.GetComponentsInChildren<AudioSource>())
            if(source.name=="Chapter2 Character Voice"){voice=source;break;}
        recorded=voice&&voice.isPlaying;
    }
    void LateUpdate()
    {
        if(!actor)return;
        if(block==null)block=new MaterialPropertyBlock();
        bool talking=actor.speaking&&!actor.fallen&&Time.unscaledTime<ends;
        float open=0,elapsed=Time.unscaledTime-began;
        if(talking)
        {
            if(recorded)
            {
                if(voice&&voice.isPlaying){
                    voice.GetOutputData(samples,0);float sum=0;foreach(float v in samples)sum+=v*v;
                    float rms=Mathf.Sqrt(sum/samples.Length);
                    voicePeak=Mathf.Max(rms,voicePeak*Mathf.Exp(-Time.unscaledDeltaTime*.5f));
                    // Recorded lines vary in loudness; normalize articulation
                    // per line while keeping genuine silence closed.
                    open=rms>.0003f?Mathf.Clamp01(rms/Mathf.Max(.002f,voicePeak*.85f))*.9f:0;
                }
            }
            else
            {
                // Text-timed articulation includes closed intervals and punctuation rests.
                float pace=Mathf.Max(.16f,(ends-began)/Mathf.Max(1,words.Length));
                int index=Mathf.Clamp((int)(elapsed/pace),0,Mathf.Max(0,words.Length-1));
                bool pause=words.Length==0||char.IsPunctuation(words[index])||char.IsWhiteSpace(words[index]);
                open=pause?0:Mathf.Pow(Mathf.Max(0,Mathf.Sin(elapsed*Mathf.PI*7.2f)),.7f)*(.65f+.25f*Mathf.Sin(elapsed*3.7f));
            }
        }
        MouthWeight=Mathf.MoveTowards(MouthWeight,open,Time.unscaledDeltaTime*(open>MouthWeight?12:16));
        float anger=0,worry=0,closed=0;
        if(drama)
        {
            switch(drama.CurrentBeat)
            {
                case Chapter2CouncilDrama.Beat.Hilt:anger=talking?.82f:.28f;break;
                case Chapter2CouncilDrama.Beat.Fire:anger=.65f;break;
                case Chapter2CouncilDrama.Beat.Resolve:case Chapter2CouncilDrama.Beat.Declare:case Chapter2CouncilDrama.Beat.Pledge:case Chapter2CouncilDrama.Beat.Planted:anger=.42f;break;
                case Chapter2CouncilDrama.Beat.Concern:worry=.8f;break;
                case Chapter2CouncilDrama.Beat.EyesClosed:worry=.9f;closed=.75f;break;
                case Chapter2CouncilDrama.Beat.Cold:case Chapter2CouncilDrama.Beat.Expel:anger=1;break;
                case Chapter2CouncilDrama.Beat.Nod:worry=.45f;break;
            }
        }
        else if(talking&&(words.Contains("祖靈")||words.Contains("守護者")))worry=.5f;
        AngerWeight=Mathf.MoveTowards(AngerWeight,anger,Time.unscaledDeltaTime*3);
        WorryWeight=Mathf.MoveTowards(WorryWeight,worry,Time.unscaledDeltaTime*3);
        Expression=anger>.5f?"憤怒":worry>.5f?"擔憂":anger>0?"堅定":"平靜";
        foreach(var surface in surfaces)
        {
            if(!surface.renderer||!surface.renderer.sharedMesh)continue;
            if(surface.head&&surface.body){
                surface.body.GetPropertyBlock(block);
                block.SetVector("_StoryCutOrigin",surface.head.TransformPoint(surface.origin));
                block.SetVector("_StoryCutRight",InverseAxis(surface.head.TransformVector(surface.right)));
                block.SetVector("_StoryCutUp",InverseAxis(surface.head.TransformVector(surface.up)));
                block.SetVector("_StoryCutForward",InverseAxis(surface.head.TransformVector(surface.forward)));
                block.SetFloat("_StoryCutEnabled",1);
                surface.body.SetPropertyBlock(block);
            }
            Set(surface.renderer,Jaw,MouthWeight);Set(surface.renderer,Angry,AngerWeight);
            Set(surface.renderer,Worried,WorryWeight);Set(surface.renderer,Closed,closed);
        }
    }
    static void Set(SkinnedMeshRenderer skin,string shape,float weight){int index=skin.sharedMesh.GetBlendShapeIndex(shape);if(index>=0)skin.SetBlendShapeWeight(index,weight*100);}
    static Vector3 InverseAxis(Vector3 axis)=>axis/Mathf.Max(.00001f,axis.sqrMagnitude);
    void OnDisable(){MouthWeight=0;}
}
