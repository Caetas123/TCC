using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        text.text = LanguageManager.Instance.GetText(key);
    }
}