using UnityEngine;
using UnityEngine.SceneManagement;

public class tiao : MonoBehaviour
{
    // 原有功能：跳到 Build Profiles 中編號 1 的場景
    public void Jump()
    {
        SceneManager.LoadScene(1);
    }

    // 新增：跳到第一章場景
    public void JumpToScene5()
    {
        MenuPresentationController.EnterChapter(0);
    }

    public void JumpToChapter2()
    {
        MenuPresentationController.EnterChapter(1);
    }

    public void JumpToChapter3()
    {
        MenuPresentationController.EnterChapter(2);
    }

    public void JumpToChapter4()
    {
        MenuPresentationController.EnterChapter(3);
    }

    // 原有功能：跳到場景編號 4
    public void JumpToScene4()
    {
        SceneManager.LoadScene(4);
    }

    // 傳入場景編號進行切換
    public void JumpToAnyScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // 離開遊戲
    public void QuitGame()
    {
        Debug.Log("點擊了退出按鈕");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
