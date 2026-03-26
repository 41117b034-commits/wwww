using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public void GoToSettingScene()
    {
        SceneManager.LoadScene("設定內容");
    }

    public void GoToMenuScene()
    {
        SceneManager.LoadScene("點選介面");
    }

    public void GoToSubtitleScene()
    {
        SceneManager.LoadScene("字幕選單");
    }
}