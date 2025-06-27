using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public int button_index;

    void on_click()
    {
        DialogueManager manager = FindObjectOfType<DialogueManager>();
        if (manager != null)
        {
            manager.on_button_clicked(button_index);
        }
    }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(on_click);
    }

}
