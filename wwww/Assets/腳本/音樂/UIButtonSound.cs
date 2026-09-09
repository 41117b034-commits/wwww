using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            if (instance.clickSound == null && clickSound != null)
            {
                instance.clickSound = clickSound;
            }

            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(BindButtonsAfterSceneLoad());
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BindButtonsAfterSceneLoad());
    }

    private IEnumerator BindButtonsAfterSceneLoad()
    {
        // Let duplicate scene-local UIAudio objects destroy themselves first.
        yield return null;

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(PlayClickSound);
            if (!HasPersistentClickListener(button))
            {
                button.onClick.AddListener(PlayClickSound);
            }
        }
    }

    private bool HasPersistentClickListener(Button button)
    {
        int listenerCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this
                && button.onClick.GetPersistentMethodName(i) == nameof(PlayClickSound))
            {
                return true;
            }
        }

        return false;
    }

    public void PlayClickSound()
    {
        if (clickSound == null)
        {
            Debug.LogWarning("UIAudio has no click sound assigned.", this);
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.PlayOneShot(clickSound);
    }
}
