using UnityEngine;
using UnityEngine.SceneManagement; // pra trocar de cena
using UnityEngine.UI; // pra usar botões

public class MenuInicial : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject painelCreditos;
    public GameObject painelConfirmacao;

    [Header("Botões")]
    public Button botaoCreditos;
    public Button botaoFecharCreditos;
    public Button botaoStart;
    public Button botaoSair;
    public Button botaoSim;
    public Button botaoNao;

    void Start()
    {
        // Garante que os painéis começam invisíveis
        painelCreditos.SetActive(false);
        painelConfirmacao.SetActive(false);

        // Liga os botões às funções
        botaoCreditos.onClick.AddListener(AbrirCreditos);
        botaoFecharCreditos.onClick.AddListener(FecharCreditos);
        botaoStart.onClick.AddListener(IniciarJogo);
        botaoSair.onClick.AddListener(AbrirConfirmacao);
        botaoSim.onClick.AddListener(SairDoJogo);
        botaoNao.onClick.AddListener(FecharConfirmacao);
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
        // Troca pra cena principal (coloca o nome da cena que quiser abrir)
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
}
