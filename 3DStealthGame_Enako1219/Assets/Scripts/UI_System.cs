using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_System : MonoBehaviour
{
    public int index { get; private set; } = -1;
    public Button button_retry;
    public Text text_Achievement;

    void Start()
    {
        button_retry.onClick.AddListener(() => index = 0);
    }

    public void UpdateAchievementText(string text) {
        text_Achievement.text = "ñ⁄ïWÅF" + text;
    }

    public void HideRestartButton() {
        button_retry.gameObject.SetActive(false);
    }

    public void ShowRestartButton() {
        button_retry.gameObject.SetActive(true);
    }

    private void LateUpdate() {
        index = -1;
    }

    
}
