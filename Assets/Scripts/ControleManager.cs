using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControleManager : MonoBehaviour
{
    // Botões do jogador 1 e 2
    public Button p1Esquerda, p1Direita, p1Pular, p1Ataque, p1Especial;
    public Button p2Esquerda, p2Direita, p2Pular, p2Ataque, p2Especial;

    public Button voltar; // Botão voltar
    public GameObject painelPrincipal; // Painel principal

    private string teclaAtual = "";
    private bool esperandoTecla = false;

    void Start()
    {
        // Configuração dos botões de alterar tecla
        p1Esquerda.onClick.AddListener(() => AlterarTecla("P1_Esquerda"));
        p1Direita.onClick.AddListener(() => AlterarTecla("P1_Direita"));
        p1Pular.onClick.AddListener(() => AlterarTecla("P1_Pular"));
        p1Ataque.onClick.AddListener(() => AlterarTecla("P1_Ataque"));
        p1Especial.onClick.AddListener(() => AlterarTecla("P1_Especial"));

        p2Esquerda.onClick.AddListener(() => AlterarTecla("P2_Esquerda"));
        p2Direita.onClick.AddListener(() => AlterarTecla("P2_Direita"));
        p2Pular.onClick.AddListener(() => AlterarTecla("P2_Pular"));
        p2Ataque.onClick.AddListener(() => AlterarTecla("P2_Ataque"));
        p2Especial.onClick.AddListener(() => AlterarTecla("P2_Especial"));

        AtualizarTexto();

        // Botão voltar
        voltar.onClick.AddListener(VoltarParaPrincipal);
    }

    void Update()
    {
        if (!esperandoTecla) return;

        if (Input.anyKeyDown)
        {
            foreach (KeyCode tecla in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(tecla))
                {
                    PlayerPrefs.SetString(teclaAtual, tecla.ToString());
                    PlayerPrefs.Save();

                    esperandoTecla = false;
                    AtualizarTexto();
                    break;
                }
            }
        }
    }

    void AlterarTecla(string nome)
    {
        teclaAtual = nome;
        esperandoTecla = true;
    }

    void AtualizarTexto()
    {
        AtualizarBotao(p1Esquerda, "Mover Esquerda: ", "P1_Esquerda", "A");
        AtualizarBotao(p1Direita, "Mover Direita: ", "P1_Direita", "D");
        AtualizarBotao(p1Pular, "Pular: ", "P1_Pular", "W");
        AtualizarBotao(p1Ataque, "Ataque: ", "P1_Ataque", "F");
        AtualizarBotao(p1Especial, "Especial: ", "P1_Especial", "G");

        AtualizarBotao(p2Esquerda, "Mover Esquerda: ", "P2_Esquerda", "LeftArrow");
        AtualizarBotao(p2Direita, "Mover Direita: ", "P2_Direita", "RightArrow");
        AtualizarBotao(p2Pular, "Pular: ", "P2_Pular", "UpArrow");
        AtualizarBotao(p2Ataque, "Ataque: ", "P2_Ataque", "K");
        AtualizarBotao(p2Especial, "Especial: ", "P2_Especial", "L");
    }

    void AtualizarBotao(Button botao, string texto, string chave, string padrao)
    {
        string tecla = PlayerPrefs.GetString(chave, padrao);
        botao.GetComponentInChildren<TextMeshProUGUI>().text = texto + tecla;
    }

    void VoltarParaPrincipal()
    {
        this.gameObject.SetActive(false);
        painelPrincipal.SetActive(true);
    }
}
