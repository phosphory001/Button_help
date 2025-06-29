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


public class PoliceDialogManager : MonoBehaviour
{
  // 对话当前状态
  private int dialog_index; //第几句话
  private JArray dialog; // 对话内容
  private bool is_dialog_active = true;
  private float typing_speed = 0.05f;

  // 对话文本
  public TextMeshProUGUI dialog_text;

  // 键盘打断
  private bool skip_now = false; // 迫使当前打字协程快速完成

  // 广告配置文件
  public string police_config_file = "police";

  // 放映结束后跳转到的 Scene
  public string next_scene = "RedAndBlue";

  // 背景直接放黑色的图片

  // // 所有背景图
  // [SerializeField] private GIFPlayer[] backgrounds;
  // private int current_gif = 0;

  // 背景图组件
  // [SerializeField] private Image backgroundImage; 

  // 进入json文件中的entry，加载信息，最初对话，开始计时
  
  // 后备箱音效
	[SerializeField] public AudioClip trunkAudioClip; 
	public AudioSource audioSource;

  // 从哪一句开始播放后备箱音效
  public int audioStartIndex = 0;

  void Start()
  {
    // gif播放
    // foreach (var gif in backgrounds) gif.gameObject.SetActive(false);
    // if (backgrounds.Length > 0) backgrounds[0].gameObject.SetActive(true);

    // 读取json
    TextAsset json_file = Resources.Load<TextAsset>(police_config_file);
    dialog = JArray.Parse(json_file.text);
    Debug.Log(dialog.ToString());

    // 设置 audio source
		audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }

    StartCoroutine(check_for_click());
    start_game();
  }
  void start_game()
  {
    StartCoroutine(show_dialog());
  }

  // // gif切换
  // private void switch_gif()
  // {
  //   backgrounds[current_gif].Stop();
  //   backgrounds[current_gif].gameObject.SetActive(false);

  //   current_gif = (current_gif + 1) % backgrounds.Length;

  //   if (current_gif < backgrounds.Length)
  //   {
  //     backgrounds[current_gif].gameObject.SetActive(true);
  //     backgrounds[current_gif].Play();
  //   }
  //   else //跳转
  //   {
  //     SceneManager.LoadScene(next_scene);
  //   }
  // }

  // 文字显示：打印机效果，在上/下文本框中打印出固定文字。进一步，实现对话效果和鼠标点击时马上切换下一段对话
  // 依据current_branch进行对话选择
  IEnumerator type_text(string line, TextMeshProUGUI target, bool playAudio = false)
  {
    target.text = "";
    System.Text.StringBuilder builder = new System.Text.StringBuilder();

    if (playAudio && trunkAudioClip)
    {
      audioSource.clip = trunkAudioClip;
      audioSource.loop = false;
      audioSource.Play();
    }
    
    List<string> splitted = StringSplitter.SplitStringWithTags(line);

		foreach (string c in splitted)
    {
      if (target == null) yield break;
      builder.Append(c);
      target.text = builder.ToString();
      if (!skip_now) yield return new WaitForSeconds(typing_speed);
    }
    skip_now = false;
  }
  IEnumerator show_dialog()
  {
    is_dialog_active = true;
    Debug.Log(dialog);
    int dialog_len = dialog.Count;
    for (dialog_index = 0; dialog_index < dialog_len; dialog_index++)
    {
      JToken elem = dialog[dialog_index];
      Debug.Log(elem.ToString());
      yield return StartCoroutine(type_text(elem.ToString(), dialog_text, dialog_index == audioStartIndex));
      yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
      skip_now = false;
    }
    is_dialog_active = false;
  }
  void skip_dialog()
  {
    // 设置下一张背景图
    if (dialog_index + 1 == dialog.Count)
    {
      SceneManager.LoadScene(next_scene); // 进入下一个场景
    }
    skip_now = true;
  }
  IEnumerator check_for_click()
  {
    while (true)
    {
      if (Input.GetMouseButtonDown(0) && is_dialog_active) skip_dialog();
      yield return null;
    }
  }

}