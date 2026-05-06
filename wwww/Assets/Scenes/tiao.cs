using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class tiao : MonoBehaviour
{
    // 原有的跳轉功能（固定跳到編號 1）
    public void Jump()
    {
        SceneManager.LoadScene(1);
    }

    // 新增：跳轉到場景 4 的功能
    public void JumpToScene4()
    {
        SceneManager.LoadScene(4);
    }

    // 更進階：傳入編號來跳轉（一個腳本搞定所有場景）
    public void JumpToAnyScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

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