using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public Button[] buttons;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Button i in buttons)
        {
            i.onClick.AddListener(() => SelectedButton(i));
        }
    }

    // Update is called once per frame
    public void SelectedButton(Button target)
    {
        foreach (Button i in buttons)
        {
            i.image.color = Color.white;
        }
        target.image.color = Color.gray;
    }
}
