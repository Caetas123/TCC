using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class ControleManager : MonoBehaviour
{
    [Header("Botões P1")]
    [SerializeField] private Button p1Esquerda;
    [SerializeField] private Button p1Direita;
    [SerializeField] private Button p1Pular;
    [SerializeField] private Button p1Defender;
    [SerializeField] private Button p1Ataque;
    [SerializeField] private Button p1Especial;
    [SerializeField] private Button p1Ultimate;

    [Header("Botões P2")]
    [SerializeField] private Button p2Esquerda;
    [SerializeField] private Button p2Direita;
    [SerializeField] private Button p2Pular;
    [SerializeField] private Button p2Defender;
    [SerializeField] private Button p2Ataque;
    [SerializeField] private Button p2Especial;
    [SerializeField] private Button p2Ultimate;

    [Header("Aviso")]
    [SerializeField] private GameObject painelAviso;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private float tempoAviso = 2f;

    private static readonly string[] TodasChaves =
    {
        "P1_Esquerda", "P1_Direita", "P1_Pular", "P1_Defender", "P1_Ataque", "P1_Especial", "P1_Ultimate",
        "P2_Esquerda", "P2_Direita", "P2_Pular", "P2_Defender", "P2_Ataque", "P2_Especial", "P2_Ultimate"
    };

    private static readonly Dictionary<string, string> Padroes = new Dictionary<string, string>
    {
        { "P1_Esquerda", "A" },
        { "P1_Direita", "D" },
        { "P1_Pular", "W" },
        { "P1_Defender", "S" },
        { "P1_Ataque", "F" },
        { "P1_Especial", "G" },
        { "P1_Ultimate", "H" },

        { "P2_Esquerda", "LeftArrow" },
        { "P2_Direita", "RightArrow" },
        { "P2_Pular", "UpArrow" },
        { "P2_Defender", "DownArrow" },
        { "P2_Ataque", "K" },
        { "P2_Especial", "L" },
        { "P2_Ultimate", "M" },
    };

    private string teclaAtual = string.Empty;
    private Button botaoAtual;
    private bool esperandoTecla = false;
    private Coroutine rotinaAviso;

    string Traduzir(string chave, string fallback)
    {
        return LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(chave)
            : fallback;
    }

    private void Start()
    {
        GarantirPadroes();
        ResolverConflitosIniciais();
        RegistrarBotoes();
        AtualizarTexto();

        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    private void Update()
    {
        if (!esperandoTecla || !TentarCapturarTecla(out KeyCode tecla))
            return;

        if (tecla == KeyCode.Escape)
        {
            if (botaoAtual != null)
                AtualizarTextoBotao(botaoAtual, teclaAtual);

            CancelarRemapeamento();
            MostrarAviso(Traduzir("CTRL_CANCELADO", "Cancelado!"));
            return;
        }

        if (TeclaProibida(tecla))
        {
            MostrarAviso(Traduzir("CTRL_PROIBIDA", "Tecla não permitida!"));
            return;
        }

        string teclaSalva = tecla.ToString();

        if (TeclaJaEmUso(teclaSalva, teclaAtual))
        {
            CancelarRemapeamento();
            AtualizarTexto();
            MostrarAviso(Traduzir("CTRL_EM_USO", "Tecla já está em uso!"));
            return;
        }

        PlayerPrefs.SetString(teclaAtual, teclaSalva);
        PlayerPrefs.Save();
        AplicarTeclasNosLutadoresEmCena();

        if (botaoAtual != null)
        {
            TextMeshProUGUI textoBotao = botaoAtual.GetComponentInChildren<TextMeshProUGUI>();
            if (textoBotao != null)
                textoBotao.text = FormatarTecla(teclaSalva);
        }

        esperandoTecla = false;
        teclaAtual = string.Empty;
        botaoAtual = null;

        MostrarAviso(Traduzir("CTRL_SALVA", "Tecla salva!"));
    }

    private bool TentarCapturarTecla(out KeyCode tecla)
    {
        tecla = KeyCode.None;

        // O menu funciona mesmo quando o projeto está configurado para usar
        // somente o Input System novo. O Input legado continua como fallback
        // para builds/configurações antigas.
        if (Keyboard.current != null)
        {
            foreach (KeyControl controle in Keyboard.current.allKeys)
            {
                if (!controle.wasPressedThisFrame)
                    continue;

                if (TryConverterTecla(controle.keyCode, out tecla))
                    return true;
            }

        }

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.anyKeyDown)
        {
            foreach (KeyCode codigo in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(codigo))
                {
                    tecla = codigo;
                    return true;
                }
            }
        }
        #endif

        return false;
    }

    private bool TryConverterTecla(Key tecla, out KeyCode codigo)
    {
        switch (tecla)
        {
            case Key.Enter: codigo = KeyCode.Return; return true;
            case Key.PrintScreen: codigo = KeyCode.Print; return true;
            case Key.ContextMenu: codigo = KeyCode.Menu; return true;
            case Key.Digit0: codigo = KeyCode.Alpha0; return true;
            case Key.Digit1: codigo = KeyCode.Alpha1; return true;
            case Key.Digit2: codigo = KeyCode.Alpha2; return true;
            case Key.Digit3: codigo = KeyCode.Alpha3; return true;
            case Key.Digit4: codigo = KeyCode.Alpha4; return true;
            case Key.Digit5: codigo = KeyCode.Alpha5; return true;
            case Key.Digit6: codigo = KeyCode.Alpha6; return true;
            case Key.Digit7: codigo = KeyCode.Alpha7; return true;
            case Key.Digit8: codigo = KeyCode.Alpha8; return true;
            case Key.Digit9: codigo = KeyCode.Alpha9; return true;
            case Key.Numpad0: codigo = KeyCode.Keypad0; return true;
            case Key.Numpad1: codigo = KeyCode.Keypad1; return true;
            case Key.Numpad2: codigo = KeyCode.Keypad2; return true;
            case Key.Numpad3: codigo = KeyCode.Keypad3; return true;
            case Key.Numpad4: codigo = KeyCode.Keypad4; return true;
            case Key.Numpad5: codigo = KeyCode.Keypad5; return true;
            case Key.Numpad6: codigo = KeyCode.Keypad6; return true;
            case Key.Numpad7: codigo = KeyCode.Keypad7; return true;
            case Key.Numpad8: codigo = KeyCode.Keypad8; return true;
            case Key.Numpad9: codigo = KeyCode.Keypad9; return true;
            case Key.NumpadPeriod: codigo = KeyCode.KeypadPeriod; return true;
            case Key.NumpadDivide: codigo = KeyCode.KeypadDivide; return true;
            case Key.NumpadMultiply: codigo = KeyCode.KeypadMultiply; return true;
            case Key.NumpadMinus: codigo = KeyCode.KeypadMinus; return true;
            case Key.NumpadPlus: codigo = KeyCode.KeypadPlus; return true;
            case Key.NumpadEnter: codigo = KeyCode.KeypadEnter; return true;
            case Key.NumpadEquals: codigo = KeyCode.KeypadEquals; return true;
            default:
                return System.Enum.TryParse(tecla.ToString(), true, out codigo)
                    && codigo != KeyCode.None;
        }
    }

    private void GarantirPadroes()
    {
        foreach (var par in Padroes)
        {
            if (!PlayerPrefs.HasKey(par.Key))
                PlayerPrefs.SetString(par.Key, par.Value);
        }

        PlayerPrefs.Save();
    }

    private void ResolverConflitosIniciais()
    {
        var usadas = new Dictionary<string, string>();

        foreach (string chave in TodasChaves)
        {
            string tecla = PlayerPrefs.GetString(chave, Padroes[chave]);

            if (usadas.ContainsKey(tecla))
            {
                PlayerPrefs.SetString(chave, Padroes[chave]);
                Debug.LogWarning($"[ControleManager] Conflito de tecla '{tecla}' em '{chave}'. Resetado para '{Padroes[chave]}'.");
            }
            else
            {
                usadas[tecla] = chave;
            }
        }

        PlayerPrefs.Save();
    }

    private void RegistrarBotoes()
    {
        RegistrarBotao(p1Esquerda, "P1_Esquerda");
        RegistrarBotao(p1Direita, "P1_Direita");
        RegistrarBotao(p1Pular, "P1_Pular");
        RegistrarBotao(p1Defender, "P1_Defender");
        RegistrarBotao(p1Ataque, "P1_Ataque");
        RegistrarBotao(p1Especial, "P1_Especial");
        RegistrarBotao(p1Ultimate, "P1_Ultimate");

        RegistrarBotao(p2Esquerda, "P2_Esquerda");
        RegistrarBotao(p2Direita, "P2_Direita");
        RegistrarBotao(p2Pular, "P2_Pular");
        RegistrarBotao(p2Defender, "P2_Defender");
        RegistrarBotao(p2Ataque, "P2_Ataque");
        RegistrarBotao(p2Especial, "P2_Especial");
        RegistrarBotao(p2Ultimate, "P2_Ultimate");
    }

    private void RegistrarBotao(Button botao, string chave)
    {
        if (botao == null) return;

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => AlterarTecla(chave, botao));
    }

    private void AlterarTecla(string nomeChave, Button botao)
    {
        if (botaoAtual != null)
            AtualizarTextoBotao(botaoAtual, ObterChaveDoBotao(botaoAtual));

        teclaAtual = nomeChave;
        botaoAtual = botao;
        esperandoTecla = true;

        TextMeshProUGUI textoBotao = botaoAtual.GetComponentInChildren<TextMeshProUGUI>();
        if (textoBotao != null)
            textoBotao.text = "...";

        MostrarAviso(Traduzir("CTRL_PRESSIONE", "Pressione uma tecla"));
    }

    private void CancelarRemapeamento()
    {
        esperandoTecla = false;
        teclaAtual = string.Empty;
        botaoAtual = null;
    }

    private bool TeclaJaEmUso(string tecla, string chaveAtual)
    {
        foreach (string chave in TodasChaves)
        {
            if (chave == chaveAtual) continue;

            string valor = PlayerPrefs.GetString(chave, Padroes[chave]);
            if (valor == tecla)
                return true;
        }
        return false;
    }

    private bool TeclaProibida(KeyCode tecla)
    {
        return tecla == KeyCode.Escape
            || tecla == KeyCode.Return
            || tecla == KeyCode.KeypadEnter
            || tecla == KeyCode.Mouse0
            || tecla == KeyCode.Mouse1
            || tecla == KeyCode.Mouse2;
    }

    private void AtualizarTexto()
    {
        AtualizarTextoBotao(p1Esquerda, "P1_Esquerda");
        AtualizarTextoBotao(p1Direita, "P1_Direita");
        AtualizarTextoBotao(p1Pular, "P1_Pular");
        AtualizarTextoBotao(p1Defender, "P1_Defender");
        AtualizarTextoBotao(p1Ataque, "P1_Ataque");
        AtualizarTextoBotao(p1Especial, "P1_Especial");
        AtualizarTextoBotao(p1Ultimate, "P1_Ultimate");

        AtualizarTextoBotao(p2Esquerda, "P2_Esquerda");
        AtualizarTextoBotao(p2Direita, "P2_Direita");
        AtualizarTextoBotao(p2Pular, "P2_Pular");
        AtualizarTextoBotao(p2Defender, "P2_Defender");
        AtualizarTextoBotao(p2Ataque, "P2_Ataque");
        AtualizarTextoBotao(p2Especial, "P2_Especial");
        AtualizarTextoBotao(p2Ultimate, "P2_Ultimate");
    }

    private void AtualizarTextoBotao(Button botao, string chave)
    {
        if (botao == null || string.IsNullOrEmpty(chave)) return;

        TextMeshProUGUI texto = botao.GetComponentInChildren<TextMeshProUGUI>();
        if (texto == null) return;

        string tecla = PlayerPrefs.GetString(
            chave,
            Padroes.ContainsKey(chave) ? Padroes[chave] : "?"
        );

        texto.text = FormatarTecla(tecla);
    }
    private string ObterChaveDoBotao(Button botao)
    {
        if (botao == p1Esquerda) return "P1_Esquerda";
        if (botao == p1Direita) return "P1_Direita";
        if (botao == p1Pular) return "P1_Pular";
        if (botao == p1Defender) return "P1_Defender";
        if (botao == p1Ataque) return "P1_Ataque";
        if (botao == p1Especial) return "P1_Especial";
        if (botao == p1Ultimate) return "P1_Ultimate";

        if (botao == p2Esquerda) return "P2_Esquerda";
        if (botao == p2Direita) return "P2_Direita";
        if (botao == p2Pular) return "P2_Pular";
        if (botao == p2Defender) return "P2_Defender";
        if (botao == p2Ataque) return "P2_Ataque";
        if (botao == p2Especial) return "P2_Especial";
        if (botao == p2Ultimate) return "P2_Ultimate";

        return string.Empty;
    }

    private void MostrarAviso(string mensagem)
    {
        if (painelAviso == null || textoAviso == null) return;

        if (rotinaAviso != null)
            StopCoroutine(rotinaAviso);

        painelAviso.SetActive(true);
        textoAviso.text = mensagem;
        rotinaAviso = StartCoroutine(EsconderAviso());
    }

    private IEnumerator EsconderAviso()
    {
        yield return new WaitForSeconds(tempoAviso);

        if (painelAviso != null)
            painelAviso.SetActive(false);

        rotinaAviso = null;
    }

    private string FormatarTecla(string tecla)
    {
        switch (tecla)
        {
            case "LeftArrow": return "←";
            case "RightArrow": return "→";
            case "UpArrow": return "↑";
            case "DownArrow": return "↓";
            case "Semicolon": return ";";
            default: return tecla;
        }
    }

    public void RestaurarPadraoControles()
    {
        foreach (var par in Padroes)
            PlayerPrefs.SetString(par.Key, par.Value);

        PlayerPrefs.Save();
        AplicarTeclasNosLutadoresEmCena();
        AtualizarTexto();
        MostrarAviso(Traduzir("CTRL_RESTAURADO", "Controles restaurados!"));
    }

    // Empurra as teclas recém-salvas pra qualquer LutadorController2D já vivo na
    // cena (cena1/cena2, remapeado pelo pause no meio da luta). Em telas sem
    // lutador (TelaInicial) simplesmente não encontra nada e não faz nada — só o
    // PlayerPrefs importa nesse caso, e ele já foi salvo acima.
    private void AplicarTeclasNosLutadoresEmCena()
    {
        foreach (var lutador in FindObjectsByType<LutadorController2D>(FindObjectsSortMode.None))
            lutador.RecarregarTeclas();
    }
}
