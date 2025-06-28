using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogueManager : MonoBehaviour
{
  public TextMeshProUGUI upper_text;
  public TextMeshProUGUI lower_text;
  public Button[] buttons;
  private float typing_speed = 0.05f;
  private float interval = 0.2f;

  // 键盘打断，互斥锁
  private Coroutine current_typing_upper; // 当前正在打字的协程
  private bool is_upper_busy = false;
  private Coroutine current_typing_lower;
  private bool is_lower_busy = false;

  // 计时装置
  private float idletime = 0f;
  private float gametime = 0f;
  public float maxidle = 30f;
  public float maxgame = 300f;
  private bool istimeactive = false;
  public TextMeshProUGUI time_display; // 时钟显示

  public List<int> current_seq; // 当前输入序列

  private List<string> upperlines = new List<string>();
  private List<string> lowerlines = new List<string>();
  private bool is_dialog_active = true;

  // game over
  public Image dark_mask;
  public TextMeshProUGUI gameover_text;
  public TextMeshProUGUI restart_text;
  public float fadeduration = 0.8f;

  void Start()
  { 
    gameover_text.gameObject.SetActive(false);
    restart_text.gameObject.SetActive(false);

    TextAsset upperfile = Resources.Load<TextAsset>("upper");
    TextAsset lowerfile = Resources.Load<TextAsset>("lower");
    upperlines = new List<string>(upperfile.text.Split('\n'));
    lowerlines = new List<string>(lowerfile.text.Split('\n'));

    // 输出最开始对话内容
    upperlines = upperlines.GetRange(0, 1);
    lowerlines = lowerlines.GetRange(0, 2);

    StartCoroutine(run_dialog());

    // 恢复到所有对话内容
    upperlines = new List<string>(upperfile.text.Split('\n'));
    lowerlines = new List<string>(lowerfile.text.Split('\n'));

    start_timer();
  }

  // 开始时对话
  IEnumerator run_dialog()
  {
    yield return StartCoroutine(type_text(upperlines[0], upper_text));
    yield return new WaitForSeconds(interval);

    yield return StartCoroutine(type_text(lowerlines[0], lower_text));
    yield return new WaitForSeconds(interval);

    yield return StartCoroutine(type_text(lowerlines[1], lower_text));

    is_dialog_active = false;
  }

  void start_timer()
  {
    gametime = 0f;
    idletime = 0f;
    istimeactive = true;
    current_seq.Clear();
  }

  // 打字机效果
  IEnumerator type_text(string line, TextMeshProUGUI target)
  {
    target.text = "";
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    
    foreach (char c in line)
    {
        if (target == null) yield break; // 防止目标被销毁
        
        sb.Append(c);
        target.text = sb.ToString(); 
        yield return new WaitForSeconds(typing_speed);
    }
  }

  IEnumerator sequential_lower_fail()
  {
    yield return StartCoroutine(type_text(lowerlines[2], lower_text));
    yield return new WaitForSeconds(interval);
    yield return StartCoroutine(type_text(lowerlines[1], lower_text));
  }
  IEnumerator sequential_upper_fail()
  {
    yield return StartCoroutine(type_text(upperlines[10], upper_text));
    yield return new WaitForSeconds(interval);
    current_seq.Clear();
    yield return StartCoroutine(type_text(upperlines[0], upper_text));
  }
  IEnumerator success()
  {
    yield return StartCoroutine(type_text(upperlines[11], upper_text));
    yield return new WaitForSeconds(interval);
    current_seq.Clear();
    SceneManager.LoadScene("Level2");
  }

  private void fail_upper()
  {
    current_seq.Clear();
    if (is_upper_busy) StopCoroutine(current_typing_upper);
    is_upper_busy = true;
    current_typing_upper = StartCoroutine(sequential_fail_upper());
  }
  private IEnumerator sequential_fail_upper()
  {
    yield return sequential_upper_fail();
    current_typing_upper = null;
    is_upper_busy = false;
  }

  private void fail_lower()
  {
    current_seq.RemoveAt(current_seq.Count - 1);
    if (is_lower_busy) StopCoroutine(current_typing_lower);
    is_lower_busy = true;
    current_typing_lower = StartCoroutine(sequential_fail_lower());
  }
  private IEnumerator sequential_fail_lower()
  {
    yield return sequential_lower_fail();
    current_typing_lower = null;
    is_lower_busy = false;
  }

  private void say_upper(int pos)
  {
    if (is_upper_busy) StopCoroutine(current_typing_upper);
    is_upper_busy = true;
    current_typing_upper = StartCoroutine(sequential_say_upper(pos));
  }
  private IEnumerator sequential_say_upper(int pos)
  {
    yield return type_text(upperlines[pos], upper_text);
    current_typing_upper = null;
    is_upper_busy = false;
  }

  private void pass_level()
  {
    is_dialog_active = true;
    StartCoroutine(success());
  }

  public void on_button_clicked(int button_index)
  {
    if (is_dialog_active) return;
    is_dialog_active = true; // 上锁
    current_seq.Add(button_index);

    // 条件判断
    if (current_seq[0] == 0 || current_seq[0] == 1) //头/下巴，唯一答案
    {
      if (current_seq.Count == 1) {  }
      else
      {
        if (current_seq[1] == 2)
        { // 零食
          if (current_seq.Count == 2) { }
          else
          {
            if (current_seq[2] == 3 || current_seq[2] == 4) // help/痛
              pass_level();
            else
              fail_upper();
          }
        }
        else fail_upper();
      }
    }
    else if (current_seq[0] == 3) // 痛
    {
      if (current_seq.Count == 1) say_upper(2);
      else
      {
        if (current_seq[1] == 1) // 下巴
        {
          if (current_seq.Count == 2) say_upper(3);
          else
          {
            if (current_seq[2] == 4) fail_lower();
            else
            {
              if (current_seq.Count == 3) say_upper(4);
              else
              {
                if (current_seq[3] == 4) fail_lower();
                else
                {
                  if (current_seq.Count == 4) say_upper(5);
                  else
                  {
                    if (current_seq[4] == 4) pass_level();
                    else fail_lower();
                  }
                }
              }
            }
          }
        }
        else if (current_seq[1] == 0)
          say_upper(12);
        else fail_upper();
      }
    }
    else if (current_seq[0] == 2) // 零食
    {
      if (current_seq.Count == 1) say_upper(6);
      else
      {
        if (current_seq[1] == 4) fail_lower();
        else
        {
          if (current_seq.Count == 2) say_upper(7);
          else
          {
            if (current_seq[2] == 4) pass_level();
            else fail_lower();
          }
        }
      }
    }
    else if (current_seq[0] == 6)
    {
      if (current_seq.Count == 1) say_upper(8);
      else
      {
        if (current_seq[1] == 0 || current_seq[1] == 1 || current_seq[1] == 5)
        {
          say_upper(10);
          fail_upper();
        }
        else
        {
          fail_upper();
        }
      }
    }
    else
    {
      fail_upper();
    }
    is_dialog_active = false;
    idletime = 0f;
  }

  // 主要是更新时间
  void Update()
  {
    if (!istimeactive) return;

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
    if (idletime >= maxidle)
    {
      fail_upper();
      idletime = 0f;
    }

  }

  void game_fail()
  {
    istimeactive = false;
    StopAllCoroutines();
    current_seq.Clear();
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

}