using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class tiao : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // 跳轉場景的功能
    public void Jump()
    {
        SceneManager.LoadScene(1);
    }

    // 退出遊戲的功能
    public void QuitGame()
    {
        Debug.Log("點擊了退出按鈕");

#if UNITY_EDITOR
        // 如果在編輯器內執行，就停止播放模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 如果是編譯後的程式，就關閉遊戲
            Application.Quit();
#endif
    }
}