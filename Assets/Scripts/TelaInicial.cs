using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;

public class MenuInicial : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject painelCreditos;
    public GameObject painelConfirmacao;
    public GameObject painelConfiguracoes;

    [Header("Botões")]
    public Button botaoCreditos;
    public Button botaoFecharCreditos;
    public Button botaoStart;
    public Button botaoSair;
    public Button botaoSim;
    public Button botaoNao;
    public Button botaoConfiguracoes;
    public Button botaoFecharConfiguracoes;

    void Start()
    {
        // Garante que os painéis começam invisíveis
        painelCreditos.SetActive(false);
        painelConfirmacao.SetActive(false);
        painelConfiguracoes.SetActive(false);

        // Liga os botões às funções
        botaoCreditos.onClick.AddListener(AbrirCreditos);
        botaoFecharCreditos.onClick.AddListener(FecharCreditos);
        botaoStart.onClick.AddListener(IniciarJogo);
        botaoSair.onClick.AddListener(AbrirConfirmacao);
        botaoSim.onClick.AddListener(SairDoJogo);
        botaoNao.onClick.AddListener(FecharConfirmacao);

        // Configurações
        botaoConfiguracoes.onClick.AddListener(AbrirConfiguracoes);
        botaoFecharConfiguracoes.onClick.AddListener(FecharConfiguracoes);
    }

    void AbrirCreditos()
    {
        painelCreditos.SetActive(true);
    }

    void FecharCreditos()
    {
        painelCreditos.SetActive(false);
    }

    void IniciarJogo()
    {
        SceneManager.LoadScene("ModoJogador");
    }

    void AbrirConfirmacao()
    {
        painelConfirmacao.SetActive(true);
    }

    void FecharConfirmacao()
    {
        painelConfirmacao.SetActive(false);
    }

    void SairDoJogo()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }

    void AbrirConfiguracoes()
    {
        painelConfiguracoes.SetActive(true);
    }

    void FecharConfiguracoes()
    {
        painelConfiguracoes.SetActive(false);
    }
}