using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class TelaSelecaoPlayer : MonoBehaviour
{
    [Header("Player 1")]
    public Image imagemPlayer1;
    public TextMeshProUGUI nomePlayer1;
    public Sprite[] rostosPlayer1;
    public Sprite[] corposPlayer1;
    public string[] nomesPlayer1;
    public Button[] botoesPlayer1;

    [Header("Player 2")]
    public Image imagemPlayer2;
    public TextMeshProUGUI nomePlayer2;
    public Sprite[] rostosPlayer2;
    public Sprite[] corposPlayer2;
    public string[] nomesPlayer2;
    public Button[] botoesPlayer2;

    [Header("Botões principais")]
    public Button botaoIniciar;
    public Button botaoVoltar;

    [Header("Avisos na tela")]
    public GameObject painelAviso;
    public TextMeshProUGUI textoAviso;

    private int indiceSelecionadoP1 = -1;
    private int indiceSelecionadoP2 = -1;

    void Start()
    {
        // Garante que os corpos começam escondidos
        if (imagemPlayer1 != null)
            imagemPlayer1.gameObject.SetActive(false);

        if (imagemPlayer2 != null)
            imagemPlayer2.gameObject.SetActive(false);

        // Limpa nomes no começo
        if (nomePlayer1 != null)
            nomePlayer1.text = "";

        if (nomePlayer2 != null)
            nomePlayer2.text = "";

        // Configura botões do Player 1
        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            int index = i;
            botoesPlayer1[i].GetComponent<Image>().sprite = rostosPlayer1[i];
            botoesPlayer1[i].onClick.AddListener(() => SelecionarPersonagemP1(index));

            EventoHoverUI hover = botoesPlayer1[i].GetComponent<EventoHoverUI>();
            if (hover == null)
                hover = botoesPlayer1[i].gameObject.AddComponent<EventoHoverUI>();

            hover.aoEntrar = () => PreviewPersonagemP1(index);
            hover.aoSair = RestaurarPreviewP1;
        }

        // Configura botões do Player 2
        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            int index = i;
            botoesPlayer2[i].GetComponent<Image>().sprite = rostosPlayer2[i];
            botoesPlayer2[i].onClick.AddListener(() => SelecionarPersonagemP2(index));

            EventoHoverUI hover = botoesPlayer2[i].GetComponent<EventoHoverUI>();
            if (hover == null)
                hover = botoesPlayer2[i].gameObject.AddComponent<EventoHoverUI>();

            hover.aoEntrar = () => PreviewPersonagemP2(index);
            hover.aoSair = RestaurarPreviewP2;
        }

        botaoIniciar.onClick.AddListener(IniciarJogo);
        botaoVoltar.onClick.AddListener(Voltar);

        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    void PreviewPersonagemP1(int indice)
    {
        if (imagemPlayer1 != null)
        {
            imagemPlayer1.gameObject.SetActive(true);
            imagemPlayer1.sprite = corposPlayer1[indice];
            imagemPlayer1.preserveAspect = true;
        }

        if (nomePlayer1 != null && indice >= 0 && indice < nomesPlayer1.Length)
            nomePlayer1.text = nomesPlayer1[indice];
    }

    void PreviewPersonagemP2(int indice)
    {
        if (imagemPlayer2 != null)
        {
            imagemPlayer2.gameObject.SetActive(true);
            imagemPlayer2.sprite = corposPlayer2[indice];
            imagemPlayer2.preserveAspect = true;
        }

        if (nomePlayer2 != null && indice >= 0 && indice < nomesPlayer2.Length)
            nomePlayer2.text = nomesPlayer2[indice];
    }

    void RestaurarPreviewP1()
    {
        if (indiceSelecionadoP1 >= 0)
        {
            PreviewPersonagemP1(indiceSelecionadoP1);
        }
        else
        {
            if (imagemPlayer1 != null)
                imagemPlayer1.gameObject.SetActive(false);

            if (nomePlayer1 != null)
                nomePlayer1.text = "";
        }
    }

    void RestaurarPreviewP2()
    {
        if (indiceSelecionadoP2 >= 0)
        {
            PreviewPersonagemP2(indiceSelecionadoP2);
        }
        else
        {
            if (imagemPlayer2 != null)
                imagemPlayer2.gameObject.SetActive(false);

            if (nomePlayer2 != null)
                nomePlayer2.text = "";
        }
    }

    void SelecionarPersonagemP1(int indice)
    {
        indiceSelecionadoP1 = indice;
        PreviewPersonagemP1(indice);

        PlayerPrefs.SetInt("PersonagemP1", indice);
        PlayerPrefs.Save();
    }

    void SelecionarPersonagemP2(int indice)
    {
        indiceSelecionadoP2 = indice;
        PreviewPersonagemP2(indice);

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

        Debug.Log("Personagens escolhidos. Indo para seleção de arena.");
        SceneManager.LoadScene("SelecaoArena");
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
            StartCoroutine(EsconderAvisoDepois(3f));
        }
    }

    IEnumerator EsconderAvisoDepois(float tempo)
    {
        yield return new WaitForSeconds(tempo);
        painelAviso.SetActive(false);
    }
}