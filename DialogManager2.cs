using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogManager2 : MonoBehaviour
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
  private float last_button_time = -1f; //最后一次按下ear的时间
  private float combo_interval = 1f; // 两个按键之间的时间间隔不超过1s

  public List<int> current_seq; // 当前输入序列

  private List<string> upperlines = new List<string>();
  private List<string> lowerlines = new List<string>();
  private bool is_dialog_active = true;

  // game over
  public Image dark_mask;
  public TextMeshProUGUI gameover_text;
  public TextMeshProUGUI restart_text;
  public float fadeduration = 0.8f;

  // 状态机设置
  private int state = 0; // 0：初始/失败，1：其他按键，2：耳朵，3：help 

  void Start()
  { 
    gameover_text.gameObject.SetActive(false);
    restart_text.gameObject.SetActive(false);

    state = 0;
    TextAsset upperfile = Resources.Load<TextAsset>("upper");
    TextAsset lowerfile = Resources.Load<TextAsset>("lower");
    upperlines = new List<string>(upperfile.text.Split('\n'));
    lowerlines = new List<string>(lowerfile.text.Split('\n'));

    StartCoroutine(run_dialog());

    start_timer();
  }

  // 开始时对话
  IEnumerator run_dialog()
  {
    yield return StartCoroutine(type_text(upperlines[14], upper_text));
    yield return new WaitForSeconds(interval);

    yield return StartCoroutine(type_text(lowerlines[4], lower_text));

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
    yield return StartCoroutine(type_text(lowerlines[6], lower_text));
    yield return new WaitForSeconds(interval);
    yield return StartCoroutine(type_text(lowerlines[4], lower_text));
  }
  IEnumerator sequential_upper_fail()
  {
    yield return StartCoroutine(type_text(upperlines[17], upper_text));
    yield return new WaitForSeconds(interval);
    yield return StartCoroutine(type_text(upperlines[14], upper_text));
    current_seq.Clear();
  }
  IEnumerator success()
  {
    yield return StartCoroutine(type_text(lowerlines[3], lower_text));
    yield return StartCoroutine(type_text(upperlines[15], upper_text));
    yield return new WaitForSeconds(interval);
    yield return StartCoroutine(type_text(upperlines[19], upper_text));
    current_seq.Clear();
    // SceneManager.LoadScene("Level2");
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
    current_seq.Clear();
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

  private IEnumerator time_interval_too_long()
  {
    current_seq.Clear();
    if (is_lower_busy) StopCoroutine(current_typing_lower);
    if (is_upper_busy) StopCoroutine(current_typing_upper);
    is_lower_busy = true;
    is_upper_busy = true;
    yield return current_typing_upper = StartCoroutine(type_text(upperlines[17], upper_text));
    yield return new WaitForSeconds(interval);
    yield return current_typing_lower = StartCoroutine(type_text(lowerlines[5], lower_text));
    yield return new WaitForSeconds(interval);
    upper_text.text = upperlines[14];
    lower_text.text = lowerlines[4];
    is_lower_busy = false;
    is_upper_busy = false;
    current_typing_lower = null;
    current_typing_upper = null;
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

    ////// 状态转移
    int last = current_seq.Count - 1;
    switch (state) {
      case 0:
        if (current_seq[0] == 5)
        {
          state = 2;
          last_button_time = Time.time;
        }
        else if (current_seq[0] == 4)
          state = 3;
        else
          state = 1;
        break;
      case 1:
        if (current_seq[last] == 5)
        {
          state = 2;
          last_button_time = Time.time;
        }
        else if (current_seq[last] == 4)
          state = 3;
        else
        {
          if (current_seq.Count >= 10)
          {
            state = 0;
            fail_upper();
          }
        }
        break;
      case 2:
        if (current_seq[last] == 2 && last_button_time > 0 && Time.time - last_button_time < combo_interval)
          pass_level();
        else if (current_seq[last] == 2)
        {
          state = 0;
          StartCoroutine(time_interval_too_long());
        }
        else if (current_seq[last] == 4)
          state = 3;
        else
        {
          if (current_seq.Count >= 10)
          {
            state = 0;
            fail_upper();
          }
          else state = 1;
        }
        break;
      default: // case 3
        if (current_seq[last] == 4) state = 3;
        else
        { // 输入其他，首先判断是否是20个help
          int count = 0;
          for (int i = last - 1; i >= 0; i--)
          {
            if (current_seq[i] == 4) count++;
          }
          if (count == 20) pass_level();
          else //不是20个，判断
          {
            if (count > 20)
            {
              fail_lower();
              state = 0;
            }
            else
            {
              if (current_seq[last] == 5)
              {
                state = 2;
                last_button_time = Time.time;
              }
              else
                  {
                    fail_upper();
                    state = 0;
                  }
            }
          }
        }
        break;
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
      int last = current_seq.Count - 1;
      if (last < 0)
      {
        fail_upper();
        state = 0;
      }
      else if (current_seq[last] == 4)
      {
        // 检测help情况
        int count = 0;
        for (int i = last; i >= 0; i--)
        {
          if (current_seq[i] == 4) count++;
        }
        if (count == 20) pass_level();
        else //不是20个，判断
        {
          fail_lower();
          state = 0;
        }
      }
      else
      {
        fail_upper();
        state = 0;
      }
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