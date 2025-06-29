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


public class IntroDialogManager : MonoBehaviour
{
  // 关卡元信息，config在start时初始化读取json
  private JObject config;
  private JToken current_branch; // 目前所在的分支，一般包含dialog等各种信息，分种类包含time/goto

  // 对话当前状态
  private int dialog_index; //第几句话
  private JArray dialog; // 对话内容
  private bool is_dialog_active = true;
  private float typing_speed = 0.05f;
  private float interval = 0.2f;

  // 交互按钮和文本
  public TextMeshProUGUI upper_text;
  public TextMeshProUGUI lower_text;
  public TextMeshProUGUI sequence_text; // 显示的按钮次序，是current_text后6个元素
  public Button[] buttons;
  public List<string> button_text;
  public event System.Action on_sequence_changed; // 检测sequence变化，判断是否显示ok键

  // 键盘打断
  private bool skip_now = false; // 迫使当前打字协程快速完成

  // 计时装置
  private float idletime = 0f;
  private float gametime = 0f;
  private float chartime = 0f; // 用于按钮一个个字显示
  public float maxidle = 30f;
  public float maxgame = 300f;
  private bool istimeactive = false;
  public TextMeshProUGUI time_display; // 时钟显示
  private float combo_interval = 1f; // 两个按键之间的时间间隔

  // 序列
  public List<string> current_text; // 当前输入的字，要显示在屏幕上
  public List<int> current_seq;

  // game over
  public Image dark_mask;
  public TextMeshProUGUI gameover_text;
  public TextMeshProUGUI restart_text;
  public float fadeduration = 0.8f;

  public string config_file_name = "intro"; // 配置文件文件名

  public bool is_intro_phase = true; // 是否处于引导阶段

  private int click_count = 0; // 按了几次按钮，= 1 和 = 2 的时候需要特判

  private bool during_intro_phase_handling = false; // 自动有多余的填充，所有按钮禁用

  public int[] button_idx_to_be_added_in_phase_2;
  // phase 2 中需要按的其他按钮, 比如
  // buttons: ["OK", "help", "爸爸", "衣服", "妈妈"]
  // 要多按 "衣服"、"爸爸"
  // 就填写 [3,2,0]
  // 要填 OK

  // 进入json文件中的entry，加载信息，最初对话，开始计时
  void Start()
  {
    // 游戏结束设置禁用
    gameover_text.gameObject.SetActive(false);
    restart_text.gameObject.SetActive(false);
    // 读取json
    TextAsset json_file = Resources.Load<TextAsset>("intro");
    config = JObject.Parse(json_file.text);
    foreach (var word in config["buttons"])
    {
      button_text.Add(word.ToString());
    }
    // 加载开始对话
    StartCoroutine(check_for_click());
    start_game();
  }
  void start_game()
  {
    if (!find_key_value_pair(config, "entry"))
    {
      Debug.Log("fail to start_game X_X");
      return;
    }
    find_key_value_pair(config, current_branch.ToString()); //第一次找到的是"hello"，第二次才找到内容
    StartCoroutine(show_dialog());
    // 计时
    gametime = 0f;
    idletime = 0f;
    chartime = 0f;
    istimeactive = true;
    clear_sequence();
  }

  public bool is_button_enabled(int button_index)
  {
    if (is_intro_phase)
    {
      return !during_intro_phase_handling && button_text[button_index] == "help";
    }
    else
    {
      return true;
    }
  }

  public void on_button_clicked_with_intro(int button_index)
  {
    if (!is_button_enabled(button_index)) return; // 被禁用的按钮没有反应
    click_count += 1;
    if (is_intro_phase)
    {
      on_button_clicked(button_index);
      during_intro_phase_handling = true;
    }
    else
    {
      on_button_clicked(button_index);
    }   
  }


  // 处理按钮
  public void on_button_clicked(int button_index, bool automatic = false)
  {
    // automatic 表示自动调用按下按钮
    if (!automatic && !is_button_enabled(button_index)) return; // 被禁用的按钮没有反应
    add_to_sequence(button_index);
    if (button_index > 0) // 正常输入
    {
      string add_string = button_text[button_index];
      // if (add_string[0] >= '\u4e00' && add_string[0] <= '\u9fff') // 中文字符，只添加第一个字
      current_text.Add(add_string[0].ToString());
      // else current_text.Add(add_string);
    }
    else // ok键，输出对话，跳转逻辑
    {
      JArray judges = current_branch["judges"] as JArray;
      for (int i = 0; i < judges.Count; i++)
      { // 每个pattern
        JArray judge = ((JObject)judges[i])["pattern"] as JArray;
        if (judge.Count != current_text.Count) continue;
        bool match_pattern = true;
        for (int j = 0; j < judge.Count; j++)
        { // 每个pattern的元素
          JArray options = judge[j] as JArray;
          bool match = false;
          for (int k = 0; k < options.Count; k++)
          {
            if (options[k].ToString() == current_text[j]) match = true;
          }
          if (match == false)
          {
            match_pattern = false;
            break;
          }
        }
        if (match_pattern == false) continue;
        // 成功匹配，更新current_branch,播放对话，goto新剧情
        current_branch = judges[i];
        StartCoroutine(handle_switch());
        idletime = 0f;
        chartime = 0f;
        return;
      }
      // 全部匹配失败,default
      current_branch = current_branch["default"];
      StartCoroutine(handle_default());
    }
    idletime = 0f;
    chartime = 0f;
  }
  IEnumerator handle_default()
  {
    during_intro_phase_handling = false;
    Debug.Log(current_branch.ToString());
    clear_sequence();
    current_text.Clear();
    yield return StartCoroutine(show_dialog());
    if (!find_key_value_pair(config, current_branch["goto"].ToString()))
      Debug.Log($"fail to find key_value_pair of {current_branch["goto"]} (default)");
    yield return StartCoroutine(show_dialog());
  }
  IEnumerator handle_switch()
  {
    during_intro_phase_handling = false;
    clear_sequence();
    current_text.Clear();
    Debug.Log(current_branch.ToString());
    yield return StartCoroutine(show_dialog());
    if (!find_key_value_pair(config, current_branch["goto"].ToString()))
      Debug.Log($"fail to find key_value_pair of {current_branch["goto"]}");
    if (((JObject)current_branch).Property("next") != null)
    {
      string next_scene = current_branch["next"].ToString();
      SceneManager.LoadScene(next_scene);
    }
    yield return StartCoroutine(show_dialog());
  }

  // 更新空闲时间、剩余时间、sequence_text、按下的按钮
  void Update()
  {
    if (!istimeactive) return;

    // 下部分显示：button还是dialog。为了省事，我选择直接把lower_text清空
    if (!is_dialog_active && current_seq.Count > 0) lower_text.text = "";

    // 游戏时长
    gametime += Time.deltaTime;
    float remain = Mathf.Max(0, maxgame - gametime);
    time_display.text = $"{(int)remain / 60}:{(int)remain % 60:D2}";
    if (gametime >= maxgame)
    {
      game_fail();
      return;
    }
    // 无操作时长
    idletime += Time.deltaTime;
    chartime += Time.deltaTime;
    if (chartime >= combo_interval && current_seq.Count > 0)
    { // 最后一个按键改变字符
      if (is_intro_phase && click_count == 1 && current_text.Count > 0 && current_text[current_text.Count - 1] == "he") // 实际上要多打一个
      {
        // 第一次按按钮，在出现 "he" 以后直接切换到 default
        during_intro_phase_handling = false;
        on_button_clicked(0, true);
        // current_branch = current_branch["default"];
        // StartCoroutine(handle_default());
      }
      else if (is_intro_phase && click_count == 2 && current_text.Count > 0
        && current_text[current_text.Count - 1].Length == button_text[current_seq[current_seq.Count - 1]].Length // 最后一个按钮的文字已经输入
      )
      {
        // 第二次开始按按钮，"help" 出现之后会加上其他按钮
        if (button_idx_to_be_added_in_phase_2[current_text.Count - 1] == 0)
        {
          is_intro_phase = false;
        } 
        on_button_clicked(button_idx_to_be_added_in_phase_2[current_text.Count - 1], true);
      } else 
      if (// current_text[current_text.Count - 1][0] <= '\u9fff' &&
          // current_text[current_text.Count - 1][0] >= '\u4e00' &&
          button_text[current_seq[current_seq.Count - 1]].Length > current_text[current_text.Count - 1].Length)
      {
        current_text[current_text.Count - 1] += button_text[current_seq[current_seq.Count - 1]][current_text[current_text.Count - 1].Length];
      }
      chartime = 0f;
    }
    
    if (idletime >= maxidle)
    {
      current_branch = current_branch["default"];
      StartCoroutine(handle_default());
      idletime = 0f;
      chartime = 0f;
    }
    // 更新显示的按下按钮
    string manifest = "";
    if (current_text.Count <= 6)
    {
      foreach (string item in current_text)
      {
        manifest = manifest + item + ' ';
      }
    }
    else
    { // 只显示最后6项
      for (int i = current_text.Count - 6; i < current_text.Count; i++)
      {
        string item = current_text[i];
        manifest = manifest + item + ' ';
      }
    }
    sequence_text.text = manifest;
  }

  // 游戏失败
  void game_fail()
  {
    istimeactive = false;
    StopAllCoroutines();
    clear_sequence();
    current_text.Clear();
    StartCoroutine(wait_for_restart());
  }
  private IEnumerator wait_for_restart()
  {
    float elapsed = 0f;
    while (elapsed < fadeduration)
    {
      dark_mask.color = Color.Lerp(
        Color.clear,
        new Color(0, 0, 0, 0.9f),
        elapsed / fadeduration
      );
      elapsed += Time.deltaTime;
      yield return null;
    }

    gameover_text.gameObject.SetActive(true);
    restart_text.gameObject.SetActive(true);

    yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
    SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 重新加载场景
  }

  // json处理:提取键值对
  bool find_key_value_pair(JObject config, string target_key)
  {
    foreach (var pair in config)
    {
      if (pair.Key == target_key)
      {
        current_branch = pair.Value;
        return true;
      }
      if (pair.Value is JObject child_obj && find_key_value_pair(child_obj, target_key))
      {
        return true;
      }
    }
    return false;
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
    foreach (var button in buttons)
    {
      button.interactable = false;
    }
    is_dialog_active = true;
    dialog = current_branch["dialog"] as JArray;
    // Debug.Log(dialog);
    Debug.Log("show");
    if (dialog != null)
    {
      int dialog_len = dialog.Count;
      for (dialog_index = 0; dialog_index < dialog_len; dialog_index++)
      {
        JToken elem = dialog[dialog_index];
        if (((JObject)elem).Properties().First().Name == "r")
          yield return StartCoroutine(type_text(((JObject)elem).Properties().First().Value.ToString(), upper_text));
        else
          yield return StartCoroutine(type_text(((JObject)elem).Properties().First().Value.ToString(), lower_text));
        // yield return new WaitForSeconds(interval);
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        skip_now = false;
      }
    }
    is_dialog_active = false;
    Debug.Log("unlock buttons");
    foreach (var button in buttons)
    {
      button.interactable = true;
    }
  }
  void skip_dialog()
  {
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

  // 封装seq变化函数，激活Ok键
  public void add_to_sequence(int index)
  {
    current_seq.Add(index);
    on_sequence_changed?.Invoke();
  }
  public void clear_sequence()
  {
    current_seq.Clear();
    on_sequence_changed?.Invoke();
  }
}