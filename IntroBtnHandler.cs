using UnityEngine;
using UnityEngine.UI;

public class IntroBtnHandler : MonoBehaviour
{
  public IntroDialogManager manager;
  private Button button;
  public int button_index;
  public AudioClip button_audio;
  private static AudioSource button_audio_source; // 按钮音效

  void Start()
  {
    manager = FindObjectOfType<IntroDialogManager>();
    button = GetComponent<Button>();
    button.onClick.AddListener(on_button_click);
    button.interactable = false; //初始时禁用按钮

    button_audio_source = GetComponent<AudioSource>();
    if (button_audio_source == null)
    {
      button_audio_source = gameObject.AddComponent<AudioSource>();
    }

    if (button_audio != null && GetComponent<AudioSource>() == null)
    {
      gameObject.AddComponent<AudioSource>().playOnAwake = false;
      if (button_audio_source != null)
      {
        button_audio_source.loop = false;
      }
    }
    if (manager != null)
      manager.on_sequence_changed += update_button_state;
    update_button_state();
  }

  void OnDestroy()
  {
    if (manager != null)
      manager.on_sequence_changed -= update_button_state;
  }

  public void on_button_click()
  {
    if (manager != null)
    {
      if (!manager.is_button_enabled(button_index))
      {
        Debug.Log($"{button_index} banned");
        return; // 被禁用的按钮不触发，也没有音效
      }
      play_button_sound();
      manager.on_button_clicked_with_intro(button_index);
    }
  }

  public void update_button_state()
  {
    if (button_index == 0)
    {
      bool is_active = !manager.is_intro_phase && manager.current_seq != null && manager.current_seq.Count > 0;
      button.interactable = is_active;
      button.gameObject.SetActive(is_active);
    }
  }

  private void play_button_sound()
  {
    if (button_audio == null) return;
    // 停止当前播放
    if (button_audio_source != null && button_audio_source.isPlaying)
    {
      button_audio_source.Stop();
    }
    // 获取AudioSource
    AudioSource audio_source = GetComponent<AudioSource>();
    if (audio_source == null)
      audio_source = gameObject.AddComponent<AudioSource>();
    // 播放
    audio_source.clip = button_audio;
    audio_source.Play();
    button_audio_source = audio_source;
  }
}