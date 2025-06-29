using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using UnityEditor;


public class RedAndBlueManager : MonoBehaviour
{
  // 对话文本
  public TextMeshProUGUI dialog_text;

  // 背景直接放黑色的图片

  // // 所有背景图
  [SerializeField] public GIFPlayer[] backgrounds;

  // 背景图组件
  // [SerializeField] private Image backgroundImage; 

  // 进入json文件中的entry，加载信息，最初对话，开始计时
  
  // 后备箱音效
	[SerializeField] public AudioClip trunkAudioClip; 
	public AudioSource audioSource;

  void Start()
  {
    // gif播放
    foreach (var gif in backgrounds) gif.gameObject.SetActive(false);

    // 设置 audio source
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }

    if (trunkAudioClip)
    {
      audioSource.clip = trunkAudioClip;
      audioSource.loop = false;
      audioSource.Play();
    }

    new WaitForSeconds(1f);

    // StartCoroutine(check_for_click());
    // start_game();
    if (backgrounds.Length > 0) backgrounds[0].gameObject.SetActive(true);
  }


}