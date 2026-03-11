using UnityEngine;
using UnityEngine.UI;

public class ConfiguracoesManager : MonoBehaviour
{
    [Header("BotÃµes Principais")]
    public Button botaoSair;
    public Button botaoVoltar;

    [Header("BotÃµes de Subpainel")]
    public Button botaoVideo;
    public Button botaoAudio;
    public Button botaoControle;

    [Header("SubpainÃ©is")]
    public GameObject painelVideo;
    public GameObject painelAudio;
    public GameObject painelControle;

    [Header("Menu Anterior (Opcional)")]
    public GameObject painelMenuAnterior;

    void Start()
    {
        // BotÃµes principais
        botaoSair.onClick.AddListener(SairAplicacao);
        botaoVoltar.onClick.AddListener(VoltarAoMenu);

        // BotÃµes que abrem subpainÃ©is
        botaoVideo.onClick.AddListener(() => AbrirSubPainel(painelVideo));
        botaoAudio.onClick.AddListener(() => AbrirSubPainel(painelAudio));
        botaoControle.onClick.AddListener(() => AbrirSubPainel(painelControle));

        // ComeÃ§a com todos os subpainÃ©is fechados
        FecharTodosSubPainel();
    }

    // Abre apenas o subpainel desejado
    void AbrirSubPainel(GameObject painel)
    {
        FecharTodosSubPainel();
        painel.SetActive(true);
        // Esconde o painel principal de configuraÃ§Ãµes enquanto o subpainel estÃ¡ aberto
        gameObject.SetActive(false);
    }

    // Fecha todos os subpainÃ©is
    void FecharTodosSubPainel()
    {
        painelVideo.SetActive(false);
        painelAudio.SetActive(false);
        painelControle.SetActive(false);
    }

    // FunÃ§Ã£o do botÃ£o sair
    void SairAplicacao()
    {
        Debug.Log("Saindo da aplicaÃ§Ã£o...");
        Application.Quit();
    }

    // FunÃ§Ã£o do botÃ£o voltar
    void VoltarAoMenu()
    {
        Debug.Log("Voltando ao menu principal...");
        FecharTodosSubPainel();
        gameObject.SetActive(false);
        if (painelMenuAnterior != null)
        {
            painelMenuAnterior.SetActive(true);
        }
    }
}
