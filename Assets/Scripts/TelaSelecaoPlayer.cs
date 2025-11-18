using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;


public class TelaSelecaoPlayer : MonoBehaviour
{
    [Header("Player 1")]
    public Image imagemPlayer1;              
    public Sprite[] rostosPlayer1;           
    public Sprite[] corposPlayer1;           
    public Button[] botoesPlayer1;           

    [Header("Player 2")]
    public Image imagemPlayer2;              
    public Sprite[] rostosPlayer2;           
    public Sprite[] corposPlayer2;           
    public Button[] botoesPlayer2;           

    [Header("Botões principais")]
    public Button botaoIniciar;
    public Button botaoVoltar;

    [Header("Avisos na tela")]
    public GameObject painelAviso;        // Fundo branco (objeto de UI com Image)
    public TextMeshProUGUI textoAviso;    // Texto vermelho dentro do painel

    private int indiceSelecionadoP1 = -1;
    private int indiceSelecionadoP2 = -1;

    void Start()
    {
        // Configura botões do Player 1
        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            int index = i;
            botoesPlayer1[i].GetComponent<Image>().sprite = rostosPlayer1[i];
            botoesPlayer1[i].onClick.AddListener(() => SelecionarPersonagemP1(index));
        }

        // Configura botões do Player 2
        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            int index = i;
            botoesPlayer2[i].GetComponent<Image>().sprite = rostosPlayer2[i];
            botoesPlayer2[i].onClick.AddListener(() => SelecionarPersonagemP2(index));
        }

        // Botões principais
        botaoIniciar.onClick.AddListener(IniciarJogo);
        botaoVoltar.onClick.AddListener(Voltar);

        // Garante que o painel de aviso comece oculto
        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    void SelecionarPersonagemP1(int indice)
    {
        indiceSelecionadoP1 = indice;
        imagemPlayer1.sprite = corposPlayer1[indice];
        PlayerPrefs.SetInt("PersonagemP1", indice);
        PlayerPrefs.Save();
    }

    void SelecionarPersonagemP2(int indice)
    {
        indiceSelecionadoP2 = indice;
        imagemPlayer2.sprite = corposPlayer2[indice];
        PlayerPrefs.SetInt("PersonagemP2", indice);
        PlayerPrefs.Save();
    }

    void IniciarJogo()
    {
        if (indiceSelecionadoP1 == -1 || indiceSelecionadoP2 == -1)
        {
            MostrarAviso("Selecione um personagem para cada jogador antes de iniciar!");
            return;
        }

        SceneManager.LoadScene("Cenario1");
    }

    void Voltar()
    {
        SceneManager.LoadScene("ModoJogador");
    }

    void MostrarAviso(string mensagem)
    {
        if (painelAviso != null && textoAviso != null)
        {
            painelAviso.SetActive(true);
            textoAviso.text = mensagem;
            StopAllCoroutines();
            StartCoroutine(EsconderAvisoDepois(3f)); // some após 3 segundos
        }
    }

    IEnumerator EsconderAvisoDepois(float tempo)
    {
        yield return new WaitForSeconds(tempo);
        painelAviso.SetActive(false);
    }
}
