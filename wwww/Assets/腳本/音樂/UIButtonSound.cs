using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSound : MonoBehaviour
{
    public AudioClip clickSound;

    private static UIButtonSound instance;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 切換場景時不要刪除 UIAudio
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayClickSound()
    {
        if (clickSound == null)
        {
            Debug.LogError("UIAudio 的 Click Sound 沒有放音效！");
            return;
        }

        audioSource.PlayOneShot(clickSound);
        Debug.Log("播放按鈕音效");
    }
}