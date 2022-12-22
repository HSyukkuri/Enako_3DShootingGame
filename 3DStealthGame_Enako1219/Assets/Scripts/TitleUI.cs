using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleUI : MonoBehaviour
{
    public Button button_Start;
    public Slider slider_Mouse;
    public TMP_Text tmp_slider_Mouse_Value;

    public Slider slider_BGM;
    public TMP_Text tmp_slider_BGM_Value;

    public bool pb_Start { get; private set; }

    private void Start() {
        button_Start.onClick.AddListener(() => pb_Start = true);
    }

    void Update() {
        tmp_slider_Mouse_Value.text = slider_Mouse.value.ToString("0.0");
        tmp_slider_BGM_Value.text = slider_BGM.value.ToString("0.0");
    }

    private void LateUpdate() {
        pb_Start = false;
    }

}
