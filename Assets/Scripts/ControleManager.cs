using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControleManager : MonoBehaviour
{
    [Header("Player 1")]
    [SerializeField] private Button p1Esquerda;
    [SerializeField] private Button p1Direita;
    [SerializeField] private Button p1Pular;
    [SerializeField] private Button p1Ataque;
    [SerializeField] private Button p1Especial;

    [Header("Player 2")]
    [SerializeField] private Button p2Esquerda;
    [SerializeField] private Button p2Direita;
    [SerializeField] private Button p2Pular;
    [SerializeField] private Button p2Ataque;
    [SerializeField] private Button p2Especial;

    [Header("Aviso")]
    [SerializeField] private GameObject painelAviso;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private float tempoAviso = 2f;

    private string teclaAtual = string.Empty;
    private Button botaoAtual;
    private bool esperandoTecla = false;
    private Coroutine rotinaAviso;

    private void Start()
    {
        GarantirPadroes();
        RegistrarBotoes();
        AtualizarTexto();

        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    private void Update()
    {
        if (!esperandoTecla)
            return;

        if (!Input.anyKeyDown)
            return;

        foreach (KeyCode tecla in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(tecla))
                continue;

            if (tecla == KeyCode.Escape)
            {
                CancelarRemapeamento();
                AtualizarTexto();
                MostrarAviso("Cancelado");
                return;
            }

            string teclaSalva = tecla.ToString();

            if (TeclaJaEmUso(teclaSalva, teclaAtual))
            {
                CancelarRemapeamento();
                AtualizarTexto();
                MostrarAviso("Tecla em uso");
                return;
            }

            PlayerPrefs.SetString(teclaAtual, teclaSalva);
            PlayerPrefs.Save();

            esperandoTecla = false;
            teclaAtual = string.Empty;
            botaoAtual = null;

            AtualizarTexto();
            MostrarAviso("Tecla salva");
            return;
        }
    }

    private void MostrarAviso(string mensagem)
    {
        if (painelAviso == null || textoAviso == null)
            return;

        if (rotinaAviso != null)
            StopCoroutine(rotinaAviso);

        painelAviso.SetActive(true);
        textoAviso.text = mensagem;
        rotinaAviso = StartCoroutine(EsconderAviso());
    }

    private IEnumerator EsconderAviso()
    {
        yield return new WaitForSeconds(tempoAviso);
        painelAviso.SetActive(false);
        rotinaAviso = null;
    }

    private void GarantirPadroes()
    {
        DefinirPadraoSeNaoExistir("P1_Esquerda", "A");
        DefinirPadraoSeNaoExistir("P1_Direita", "D");
        DefinirPadraoSeNaoExistir("P1_Pular", "W");
        DefinirPadraoSeNaoExistir("P1_Ataque", "F");
        DefinirPadraoSeNaoExistir("P1_Especial", "G");

        DefinirPadraoSeNaoExistir("P2_Esquerda", "LeftArrow");
        DefinirPadraoSeNaoExistir("P2_Direita", "RightArrow");
        DefinirPadraoSeNaoExistir("P2_Pular", "UpArrow");
        DefinirPadraoSeNaoExistir("P2_Ataque", "K");
        DefinirPadraoSeNaoExistir("P2_Especial", "L");

        PlayerPrefs.Save();
    }

    private void DefinirPadraoSeNaoExistir(string chave, string valor)
    {
        if (!PlayerPrefs.HasKey(chave))
            PlayerPrefs.SetString(chave, valor);
    }

    private void RegistrarBotoes()
    {
        RegistrarBotao(p1Esquerda, "P1_Esquerda");
        RegistrarBotao(p1Direita, "P1_Direita");
        RegistrarBotao(p1Pular, "P1_Pular");
        RegistrarBotao(p1Ataque, "P1_Ataque");
        RegistrarBotao(p1Especial, "P1_Especial");

        RegistrarBotao(p2Esquerda, "P2_Esquerda");
        RegistrarBotao(p2Direita, "P2_Direita");
        RegistrarBotao(p2Pular, "P2_Pular");
        RegistrarBotao(p2Ataque, "P2_Ataque");
        RegistrarBotao(p2Especial, "P2_Especial");
    }

    private void RegistrarBotao(Button botao, string chave)
    {
        if (botao == null)
            return;

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => AlterarTecla(chave, botao));
    }

    private void AlterarTecla(string nomeChave, Button botao)
    {
        teclaAtual = nomeChave;
        botaoAtual = botao;
        esperandoTecla = true;

        TextMeshProUGUI textoBotao = botaoAtual.GetComponentInChildren<TextMeshProUGUI>();
        if (textoBotao != null)
            textoBotao.text = "Pressione uma tecla";

        MostrarAviso("Aguardando tecla");
    }

    private void CancelarRemapeamento()
    {
        esperandoTecla = false;
        teclaAtual = string.Empty;
        botaoAtual = null;
    }

    private bool TeclaJaEmUso(string tecla, string chaveAtual)
    {
        string[] chaves =
        {
            "P1_Esquerda", "P1_Direita", "P1_Pular", "P1_Ataque", "P1_Especial",
            "P2_Esquerda", "P2_Direita", "P2_Pular", "P2_Ataque", "P2_Especial"
        };

        foreach (string chave in chaves)
        {
            if (chave == chaveAtual)
                continue;

            string valor = PlayerPrefs.GetString(chave, ObterPadrao(chave));
            if (valor == tecla)
                return true;
        }

        return false;
    }

    private void AtualizarTexto()
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

    private void AtualizarBotao(Button botao, string prefixo, string chave, string padrao)
    {
        if (botao == null)
            return;

        TextMeshProUGUI texto = botao.GetComponentInChildren<TextMeshProUGUI>();
        if (texto == null)
            return;

        string tecla = PlayerPrefs.GetString(chave, padrao);
        texto.text = prefixo + FormatarTecla(tecla);
    }

    private string FormatarTecla(string tecla)
    {
        switch (tecla)
        {
            case "LeftArrow": return "←";
            case "RightArrow": return "→";
            case "UpArrow": return "↑";
            case "DownArrow": return "↓";
            default: return tecla;
        }
    }

    private string ObterPadrao(string chave)
    {
        switch (chave)
        {
            case "P1_Esquerda": return "A";
            case "P1_Direita": return "D";
            case "P1_Pular": return "W";
            case "P1_Ataque": return "F";
            case "P1_Especial": return "G";
            case "P2_Esquerda": return "LeftArrow";
            case "P2_Direita": return "RightArrow";
            case "P2_Pular": return "UpArrow";
            case "P2_Ataque": return "K";
            case "P2_Especial": return "L";
            default: return string.Empty;
        }
    }
}