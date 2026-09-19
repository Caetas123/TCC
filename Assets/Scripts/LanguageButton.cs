using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageButton : MonoBehaviour
{
    public TextMeshProUGUI text;

    private bool inscritoNoEvento;
    private Button botao;

    void Awake()
    {
        // O painel de configuracoes da luta pode ficar desativado no inicio da
        // cena. Se a referencia do prefab nao vier preenchida, recupera o TMP
        // filho automaticamente quando o painel for aberto.
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        botao = GetComponent<Button>();
    }

    void OnEnable()
    {
        GarantirAcaoDoBotao();

        if (!inscritoNoEvento)
        {
            LanguageManager.OnLanguageChanged += UpdateText;
            inscritoNoEvento = true;
        }

        UpdateText();
    }

    void OnDisable()
    {
        if (botao != null)
            botao.onClick.RemoveListener(AlternarIdioma);

        if (inscritoNoEvento)
        {
            LanguageManager.OnLanguageChanged -= UpdateText;
            inscritoNoEvento = false;
        }
    }

    void OnDestroy()
    {
        OnDisable();
    }

    public void AtualizarTexto()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
            return;

        bool portugues = LanguageManager.Instance == null
            || LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Portuguese;

        text.text = portugues ? "PT" : "EN";
    }

    private void GarantirAcaoDoBotao()
    {
        if (botao == null)
            botao = GetComponent<Button>();

        if (botao == null)
            return;

        for (int i = 0; i < botao.onClick.GetPersistentEventCount(); i++)
        {
            if (botao.onClick.GetPersistentMethodName(i) == nameof(LanguageManager.ToggleLanguage))
            {
                // A referencia serializada pode apontar para o LanguageManager
                // duplicado da cena. O jogo usa o singleton persistente, entao
                // o listener antigo precisa ser desligado.
                botao.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }
        }

        // Liga sempre a acao ao singleton atual e evita duplicidade ao reabrir
        // o painel de configuracoes.
        botao.onClick.RemoveListener(AlternarIdioma);
        botao.onClick.AddListener(AlternarIdioma);
    }

    private void AlternarIdioma()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.ToggleLanguage();
    }

    // Mantem compatibilidade com o evento estatico existente.
    private void UpdateText()
    {
        AtualizarTexto();
    }
}
