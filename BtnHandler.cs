using UnityEngine;
using UnityEngine.UI;

public class BtnHandler : MonoBehaviour
{
  public DialogManager manager;
  private Button button;
  public int button_index;

  void Start()
  {
    manager = FindObjectOfType<DialogManager>();
    button = GetComponent<Button>();
    button.onClick.AddListener(on_button_click);
    button.interactable = false; //初始时禁用按钮
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
      manager.on_button_clicked(button_index);
    }
  }

  public void update_button_state()
  {
    if (button_index == 0)
    {
      bool is_active = manager.current_seq != null && manager.current_seq.Count > 0;
      button.interactable = is_active;
      button.gameObject.SetActive(is_active);
    }
  }
}