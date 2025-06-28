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


public class AdvertisementManager : MonoBehaviour
{
  // 对话当前状态
  private int dialog_index; //第几句话
  private JArray dialog; // 对话内容
  private bool is_dialog_active = true;
  private float typing_speed = 0.05f;
  private float interval = 0.2f;

  // 广告文本
  public TextMeshProUGUI advertisement_text;

  // 键盘打断
  private bool skip_now = false; // 迫使当前打字协程快速完成

  // 广告配置文件
  public string advertisement_config_file_name = "advertisement";

  // 放映结束后跳转到的 Scene
  public string next_scene = "intro";

  // 所有背景图
  [SerializeField] private Sprite[] backgrounds; 

  // 背景图组件
  [SerializeField] private Image backgroundImage; 

  // 进入json文件中的entry，加载信息，最初对话，开始计时
  void Start()
  {
    // 读取json
    TextAsset json_file = Resources.Load<TextAsset>(advertisement_config_file_name);
    dialog = JArray.Parse(json_file.text);
    backgroundImage.sprite = backgrounds[0]; // 初始化第一张背景
    StartCoroutine(check_for_click());
    start_game();
  }
  void start_game()
  {
    StartCoroutine(show_dialog());
  }

  // 文字显示：打印机效果，在上/下文本框中打印出固定文字。进一步，实现对话效果和鼠标点击时马上切换下一段对话
  // 依据current_branch进行对话选择
  IEnumerator type_text(string line, TextMeshProUGUI target)
  {
    target.text = "";
    System.Text.StringBuilder builder = new System.Text.StringBuilder();
    foreach (char c in line)
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
    // Debug.Log(dialog);
    int dialog_len = dialog.Count;
    for (dialog_index = 0; dialog_index < dialog_len; dialog_index++)
    {
      JToken elem = dialog[dialog_index];
      yield return StartCoroutine(type_text(((JObject)elem).Properties().First().Value.ToString(), advertisement_text));
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
    else
    {
      backgroundImage.sprite = backgrounds[dialog_index + 1];
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