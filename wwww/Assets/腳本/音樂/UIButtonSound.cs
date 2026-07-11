using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSound : MonoBehaviour
{
    [Header("按鈕點擊音效")]
    public AudioClip clickSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayClickSound()
    {
        if (clickSound == null)
        {
            Debug.LogWarning("尚未設定按鈕音效！");
            return;
        }

        audioSource.PlayOneShot(clickSound);
    }
}