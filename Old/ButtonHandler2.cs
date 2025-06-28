using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler2 : MonoBehaviour
{
    public int button_index;

    void on_click()
    {
        DialogManager2 manager = FindObjectOfType<DialogManager2>();
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
