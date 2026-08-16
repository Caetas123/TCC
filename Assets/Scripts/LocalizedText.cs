using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    private TextMeshProUGUI text;

    private void Awake()
    {
        GarantirReferenciaDeTexto();
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void Start()
    {
        // Segunda tentativa: Start roda depois de TODOS os Awake da cena, então aqui o
        // LanguageManager.Instance já existe com certeza. Sem isso, um objeto cujo
        // OnEnable rodou antes do Awake do LanguageManager ficaria com o texto vazio
        // pra sempre (o evento OnLanguageChanged só dispara quando o idioma MUDA).
        UpdateText();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= UpdateText;
    }

    private void GarantirReferenciaDeTexto()
    {
        if (text != null) return;

        text = GetComponent<TextMeshProUGUI>();

        // Fallback: alguns prefabs têm o texto num filho em vez do próprio objeto
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void UpdateText()
    {
        // Este método é chamado por OnEnable, por Start e pelo evento de troca de idioma.
        // O OnEnable dispara sempre que o objeto é reativado — inclusive via SetActive()
        // no meio do jogo (ex: preview de personagem na tela de seleção). Nesse momento
        // o LanguageManager pode ainda não ter sido inicializado, ou o objeto pode não ter
        // o componente de texto. Era exatamente isso que causava o NullReferenceException.
        GarantirReferenciaDeTexto();
        if (text == null) return;

        if (LanguageManager.Instance == null) return;
        if (string.IsNullOrEmpty(key)) return;

        text.text = LanguageManager.Instance.GetText(key);
    }
}
