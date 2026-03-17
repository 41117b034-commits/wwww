using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public void GoToSettingScene()
    {
        SceneManager.LoadScene("設定內容");
    }
}