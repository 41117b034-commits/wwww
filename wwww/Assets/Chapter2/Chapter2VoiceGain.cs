using UnityEngine;

// Per-source gain, separate from AudioSource's 0..1 category volume.
// The audio thread only reads a volatile scalar; no Unity API calls here.
[DisallowMultipleComponent]
public sealed class Chapter2VoiceGain : MonoBehaviour
{
    public volatile float Gain = 1f;
    void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = Gain;
        for (int i = 0; i < data.Length; i++) data[i] *= gain;
    }
}
