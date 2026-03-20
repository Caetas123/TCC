using TMPro;
using UnityEngine;

public class LanguageButton : MonoBehaviour
{
    public TextMeshProUGUI text;

    void Start()
    {
        UpdateText();
        LanguageManager.OnLanguageChanged += UpdateText;
    }

    void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        if (LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Portuguese)
            text.text = "PT";
        else
            text.text = "EN";
    }
}