using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioDebugTest : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        Debug.Log("音樂物件是否啟用：" + gameObject.activeInHierarchy);
        Debug.Log("Audio Source 是否啟用：" + audioSource.enabled);
        Debug.Log("音檔：" +
                  (audioSource.clip != null ? audioSource.clip.name : "沒有音檔"));

        AudioListener listener = FindFirstObjectByType<AudioListener>();

        Debug.Log("Audio Listener：" +
                  (listener != null ? listener.gameObject.name : "找不到"));

        AudioListener.pause = false;
        AudioListener.volume = 1f;

        audioSource.mute = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f;

        audioSource.Play();

        Invoke(nameof(CheckAudio), 1f);
    }

    private void CheckAudio()
    {
        Debug.Log("音樂是否正在播放：" + audioSource.isPlaying);
        Debug.Log("播放時間：" + audioSource.time);
    }
}