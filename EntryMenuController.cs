using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntryMenuController : MonoBehaviour
{
    // button references
    public Button start_button;
    public Button quit_button;

    private void on_start_clicked()
    {
        // 加载新场景
        SceneManager.LoadScene("Level");
    }

    private void on_quit_clicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void Start()
    {
        start_button.onClick.AddListener(on_start_clicked);
        quit_button.onClick.AddListener(on_quit_clicked);
    }


}
