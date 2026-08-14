using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TelaSelecaoPlayer : MonoBehaviour
{
    private enum EstadoTela
    {
        SelecionandoPersonagens,
        ConfiguracaoIA,
        Sorteando,
        Transicao
    }

    [Header("Player 1")]
    public Image imagemPlayer1;
    public TextMeshProUGUI nomePlayer1;
    public Sprite[] rostosPlayer1;
    public Sprite[] corposPlayer1;
    public string[] nomesPlayer1;
    public Button[] botoesPlayer1;
    public Image[] rostosSlotsP1;
    public Image[] destaquesP1;
    public Image[] engrenagensP1;
    public Button botaoDeselecionarP1;

    [Header("Player 2")]
    public Image imagemPlayer2;
    public TextMeshProUGUI nomePlayer2;
    public Sprite[] rostosPlayer2;
    public Sprite[] corposPlayer2;
    public string[] nomesPlayer2;
    public Button[] botoesPlayer2;
    public Image[] rostosSlotsP2;
    public Image[] destaquesP2;
    public Image[] engrenagensP2;
    public Button botaoDeselecionarP2;

    [Header("Botões principais")]
    public Button botaoIniciar;
    public Button botaoVoltar;

    [Header("Sprites de Foco/Hover — Iniciar e Voltar")]
    [Tooltip("PNG mostrado quando o TECLADO/CONTROLE está com o foco neste botão. Persiste enquanto o foco ficar aqui (não é o mesmo que 'clicou').")]
    public Sprite spriteFocoIniciar;
    [Tooltip("PNG mostrado só enquanto o MOUSE está fisicamente em cima do botão — some assim que o mouse sai. Se deixar vazio, usa o mesmo PNG do Foco.")]
    public Sprite spriteHoverIniciar;
    [Tooltip("PNG mostrado quando o TECLADO/CONTROLE está com o foco neste botão. Persiste enquanto o foco ficar aqui (não é o mesmo que 'clicou').")]
    public Sprite spriteFocoVoltar;
    [Tooltip("PNG mostrado só enquanto o MOUSE está fisicamente em cima do botão — some assim que o mouse sai. Se deixar vazio, usa o mesmo PNG do Foco.")]
    public Sprite spriteHoverVoltar;

    [Header("Avisos na tela")]
    public GameObject painelAviso;
    public TextMeshProUGUI textoAviso;

    [Header("Texto Random (LocalizedText com chave RANDOM_CHARACTER)")]
    [Tooltip("Objeto filho do nomePlayer1 com LocalizedText — chave RANDOM_CHARACTER")]
    public GameObject nomeRandomP1;
    [Tooltip("Objeto filho do nomePlayer2 com LocalizedText — chave RANDOM_CHARACTER")]
    public GameObject nomeRandomP2;

    [Header("Configuração da IA (UI)")]
    public GameObject painelConfigIAP1;
    public TMP_Dropdown dropdownEstiloIAP1;
    public TMP_Dropdown dropdownDificuldadeIAP1;
    public Button botaoSalvarConfigIAP1;
    public Button botaoFecharConfigIAP1;

    [FormerlySerializedAs("painelConfigIA")]
    public GameObject painelConfigIAP2;
    [FormerlySerializedAs("dropdownEstiloIA")]
    public TMP_Dropdown dropdownEstiloIAP2;
    [FormerlySerializedAs("dropdownDificuldadeIA")]
    public TMP_Dropdown dropdownDificuldadeIAP2;
    [FormerlySerializedAs("botaoSalvarConfigIA")]
    public Button botaoSalvarConfigIAP2;
    [FormerlySerializedAs("botaoFecharConfigIA")]
    public Button botaoFecharConfigIAP2;

    [Header("Sprites de Foco/Hover — Menu de Configuração de IA (Salvar/Fechar)")]
    [Tooltip("Vale tanto pro painel do P1 quanto do P2 (só um painel fica aberto por vez).")]
    public Sprite spriteFocoSalvarIA;
    public Sprite spriteHoverSalvarIA;
    public Sprite spriteFocoFecharIA;
    public Sprite spriteHoverFecharIA;

    [Header("Seleção automática CPU")]
    public bool cpuPodeRepetirMesmoPersonagem = false;
    [Header("Aviso Deselecionar")]
    public TextMeshProUGUI avisoP1;
    public TextMeshProUGUI avisoP2;

    [Header("Random")]
    public Sprite spriteRandom;

    [Header("Personagem Aleatório")]
    public Button botaoRandomP1;
    public Button botaoRandomP2;
    public Image destaqueRandomP1;
    public Image destaqueRandomP2;
    public float tempoInicialRoleta = 0.05f;
    public int ciclosRoleta = 18;

    private bool sorteandoP1 = false;
    private bool sorteandoP2 = false;

    // A borda usa a cor normal dela (branco) — só a OPACIDADE muda entre foco e
    // selecionado, sem tingir a cor. O amarelo (corHoverBotao) continua só nos botões
    // Iniciar/Voltar/Deselecionar, que é outro sistema separado.
    private readonly Color corDestaqueVisivel  = new Color(1f, 1f, 1f, 1f);    // opacidade cheia - selecionado
    private readonly Color corDestaqueInvisivel = new Color(1f, 1f, 1f, 0f);   // invisível
    private readonly Color corHoverSuave        = new Color(1f, 1f, 1f, 0.5f); // opacidade reduzida - foco/hover
    private readonly Color corHoverBotao        = new Color(1f, 0.95f, 0.65f, 1f);

    private int indiceSelecionadoP1 = -1;
    private int indiceSelecionadoP2 = -1;
    private int focoP1 = 0;
    private int focoP2 = 0;
    private int grupoAtual = 1;
    private int focoInferior = 1;
    private bool mouseSobreBotaoInferior = false; // só cosmético — nunca usado pra lógica de teclado

    // Rastreiam se o eixo "Vertical" já estava acima/abaixo do limiar no frame anterior —
    // usados só pra detectar borda (subida) em TratarNavegacaoGrupos, evitando que segurar
    // a tecla processe várias transições de grupo em frames consecutivos.
    private bool eixoVerticalCimaSegurando = false;
    private bool eixoVerticalBaixoSegurando = false;

    private Color corOriginalIniciar;
    private Color corOriginalVoltar;
    private Sprite spriteOriginalIniciar;
    private Sprite spriteOriginalVoltar;
    private Sprite spriteOriginalSalvarIAP1;
    private Sprite spriteOriginalFecharIAP1;
    private Sprite spriteOriginalSalvarIAP2;
    private Sprite spriteOriginalFecharIAP2;
    private Color corOriginalDeselecionarP1;
    private Color corOriginalDeselecionarP2;

    private KeyCode teclaEsquerdaP1;
    private KeyCode teclaDireitaP1;
    private KeyCode teclaConfirmarP1;
    private KeyCode teclaCimaP1;
    private KeyCode teclaBaixoP1;
    private KeyCode teclaDefenderP1;
    private KeyCode teclaEsquerdaP2;
    private KeyCode teclaDireitaP2;
    private KeyCode teclaConfirmarP2;
    private KeyCode teclaCimaP2;
    private KeyCode teclaBaixoP2;
    private KeyCode teclaDefenderP2;

    private string modoJogo = "PVP";

    enum EstiloIA { Agressivo = 0, Equilibrado = 1, Defensivo = 2 }
    enum DificuldadeIA { Facil = 0, Medio = 1, Dificil = 2 }

    private EstiloIA[] estilosP1;
    private DificuldadeIA[] dificuldadesP1;
    private EstiloIA[] estilosP2;
    private DificuldadeIA[] dificuldadesP2;

    private float[] lastClickTimeP1;
    private float[] lastClickTimeP2;
    private const float doubleClickThreshold = 0.35f;

    private int ultimoLadoAtivo = 1;
    private int ladoIAConfigAberta = 0;
    private int indiceIAConfigAberta = -1;
    private GameObject focoRetornoIA;
    private EstadoTela estadoAtual = EstadoTela.SelecionandoPersonagens;

    // Desliga o "Transition" nativo (ColorTint/SpriteSwap) de um Button, sem mexer em
    // mais nada configurado no Inspector (sprites, cores originais, onClick, etc.) —
    // só impede a Unity de pintar por cima sozinha quando o EventSystem seleciona ele.
    void DesligarHighlightNativoBotao(Button botao)
    {
        if (botao == null) return;
        botao.transition = Selectable.Transition.None;
    }

    void DesligarHighlightNativo()
    {
        if (botoesPlayer1 != null)
            foreach (Button b in botoesPlayer1) DesligarHighlightNativoBotao(b);

        if (botoesPlayer2 != null)
            foreach (Button b in botoesPlayer2) DesligarHighlightNativoBotao(b);

        // Iniciar/Voltar/Deselecionar e os botões do painel de IA ficam de fora —
        // o hover deles agora é decidido pelo próprio Transition/cores configurados
        // no Inspector de cada Button, não mais pintado na mão pelo script.
    }

    void Start()
    {
        // Se o EventSystem tiver um "First Selected" configurado no Inspector (mesmo sem
        // querer, de algum teste no Editor), a Unity auto-seleciona ele sozinha sempre que
        // uma tecla de navegação/confirmação é pressionada com nada selecionado de verdade
        // — e dispara o onClick dele junto. Era isso que selecionava um personagem sozinho
        // ao dar Enter em Iniciar sem nada selecionado. Zera aqui pra nunca depender de
        // ninguém lembrar de checar esse campo no Inspector.
        if (EventSystem.current != null)
            EventSystem.current.firstSelectedGameObject = null;

        modoJogo = PlayerPrefs.GetString("ModoJogo", "PVP");

        indiceSelecionadoP1 = -1;
        indiceSelecionadoP2 = -1;

        PlayerPrefs.DeleteKey("PersonagemP1");
        PlayerPrefs.DeleteKey("PersonagemP2");

        if (imagemPlayer1 != null) imagemPlayer1.gameObject.SetActive(false);
        if (imagemPlayer2 != null) imagemPlayer2.gameObject.SetActive(false);
        if (nomePlayer1 != null) nomePlayer1.text = "";
        if (nomePlayer2 != null) nomePlayer2.text = "";

        CarregarTeclas();
        GuardarCoresOriginais();
        GarantirBotoesRandomNosArrays();
        InicializarConfiguracoesIA();

        ConfigurarBotoesP1();
        ConfigurarBotoesP2();
        ConfigurarInteratividadePorModo();
        ConfigurarBotoesAuxiliares();

        // A Unity tem seu próprio sistema de "Selected/Highlighted" (cor configurada no
        // Inspector de cada Button), que reage sozinho quando o EventSystem marca um
        // botão como selecionado — seja pelo UIFocusUtility.Select() abaixo, seja pela
        // navegação nativa por seta que o Input Manager já entende. Isso brigava com o
        // nosso destaque customizado (o amarelo) e causava vários botões "acesos" ao
        // mesmo tempo com cores que a gente nem pintou. Desligando o Transition nativo,
        // só sobra o nosso sistema de destaque no controle visual.
        DesligarHighlightNativo();


        if (botaoIniciar != null)
        {
            botaoIniciar.onClick.RemoveAllListeners();
            botaoIniciar.onClick.AddListener(IniciarJogo);

            EventoHoverUI h = botaoIniciar.GetComponent<EventoHoverUI>();
            if (h == null) h = botaoIniciar.gameObject.AddComponent<EventoHoverUI>();
            h.aoEntrar = () =>
            {
                if (PainelConfiguracaoIAAberto()) return;

                // O mouse assume o foco de verdade (grupoAtual/focoInferior), não só
                // pinta por cima — assim o destaque da grade de personagens realmente
                // some, em vez de ficar aceso junto com o Iniciar.
                mouseSobreBotaoInferior = true;
                grupoAtual = 2;
                focoInferior = 1;
                AtualizarFocoInferior();
                AtualizarBotaoVoltar();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
            h.aoSair = () =>
            {
                mouseSobreBotaoInferior = false;
                AtualizarFocoInferior();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveAllListeners();
            botaoVoltar.onClick.AddListener(Voltar);

            EventoHoverUI h = botaoVoltar.GetComponent<EventoHoverUI>();
            if (h == null) h = botaoVoltar.gameObject.AddComponent<EventoHoverUI>();
            h.aoEntrar = () =>
            {
                if (PainelConfiguracaoIAAberto()) return;

                mouseSobreBotaoInferior = true;
                grupoAtual = 0;
                AtualizarFocoInferior();
                AtualizarBotaoVoltar();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
            h.aoSair = () =>
            {
                mouseSobreBotaoInferior = false;
                AtualizarBotaoVoltar();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
        }
        if (avisoP1 != null) avisoP1.gameObject.SetActive(true);
        if (avisoP2 != null) avisoP2.gameObject.SetActive(true);

        StartCoroutine(EsconderAvisoDeselecao());

        if (painelAviso != null)
            painelAviso.SetActive(false);

        // Garante que ambos os painéis de IA começam fechados
        if (painelConfigIAP1 != null) painelConfigIAP1.SetActive(false);
        if (painelConfigIAP2 != null) painelConfigIAP2.SetActive(false);
        ladoIAConfigAberta = 0;
        indiceIAConfigAberta = -1;

        ResetarDestaquesP1();
        ResetarDestaquesP2();
        AplicarEstadoInicialPorModo();

        grupoAtual = 1;
        focoInferior = 1;
        estadoAtual = EstadoTela.SelecionandoPersonagens;
        AtualizarFocoGrupo();

        // Importante: NÃO usamos UIFocusUtility.Select() num botão de personagem aqui.
        // Essa tela pinta o destaque (foco/hover) na mão via focoP1/focoP2 — não depende
        // da seleção "de verdade" do EventSystem pra nada. Selecionar de verdade um botão
        // de personagem é perigoso: a Unity dispara o onClick dele sozinha sempre que
        // Enter é pressionado (Button implementa ISubmitHandler), por fora da nossa lógica
        // que restringe o Enter só a Iniciar/Voltar. Era isso que fazia o Enter "clicar"
        // num personagem errado. Deixamos limpo — a grade já nasce com foco visual em
        // focoP1 = 0 / focoP2 = 0 sem precisar de nenhum GameObject realmente selecionado.
        UIFocusUtility.ClearSelection();

        // Atualiza textos traduzidos quando idioma mudar
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
    }

    void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
    }

    // Chave: helper local que nunca falha independente do LanguageManager
    string Traduzir(string chave, string fallbackPT)
    {
        if (LanguageManager.Instance != null)
            return LanguageManager.Instance.GetText(chave);
        return fallbackPT;
    }

    // Chamado quando idioma muda — re-renderiza o preview ativo
    void AtualizarTextosIdioma()
    {
        // Re-exibe preview P1 se estiver mostrando algo
        if (nomePlayer1 != null && !string.IsNullOrEmpty(nomePlayer1.text))
        {
            int indice = indiceSelecionadoP1 >= 0 ? indiceSelecionadoP1 : focoP1;
            if (indice >= 0) PreviewPersonagemP1(indice);
        }

        // Re-exibe preview P2 se estiver mostrando algo
        if (nomePlayer2 != null && !string.IsNullOrEmpty(nomePlayer2.text))
        {
            int indice = indiceSelecionadoP2 >= 0 ? indiceSelecionadoP2 : focoP2;
            if (indice >= 0) PreviewPersonagemP2(indice);
        }
    }

    void Update()
    {
        if (estadoAtual == EstadoTela.Sorteando || estadoAtual == EstadoTela.Transicao)
            return;

        if (PainelConfiguracaoIAAberto())
        {
            estadoAtual = EstadoTela.ConfiguracaoIA;
            TratarAtalhosPainelIA();
            return;
        }

        estadoAtual = EstadoTela.SelecionandoPersonagens;

        // Navegação global (Esc, Enter, cima/baixo entre grupos).
        // Se ela já consumiu a tecla desse frame, PARA aqui — sem esse corte, o mesmo
        // aperto de tecla era processado de novo pelos handlers de P1/P2/Inferior logo
        // abaixo (porque GetKeyDown continua "true" no resto do frame), fazendo o foco
        // pular direto de Iniciar pro Voltar (ou vice-versa), pulando a grade inteira.
        if (TratarNavegacaoGrupos())
            return;

        if (grupoAtual == 1)
        {
            // Teclas de personagem — totalmente separadas por lado
            if (PermiteSelecaoP1())
                TratarTecladoP1();

            if (PermiteSelecaoP2())
                TratarTecladoP2();
        }
        else if (grupoAtual == 2)
        {
            TratarTecladoInferior();
        }
    }


    void GarantirBotoesRandomNosArrays()
    {
        if (botaoRandomP1 != null && !ArrayContemBotao(botoesPlayer1, botaoRandomP1))
            botoesPlayer1 = AdicionarBotaoNoArray(botoesPlayer1, botaoRandomP1);

        if (botaoRandomP2 != null && !ArrayContemBotao(botoesPlayer2, botaoRandomP2))
            botoesPlayer2 = AdicionarBotaoNoArray(botoesPlayer2, botaoRandomP2);
    }

    bool ArrayContemBotao(Button[] array, Button botao)
    {
        if (array == null || botao == null) return false;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == botao)
                return true;
        }

        return false;
    }

    Button[] AdicionarBotaoNoArray(Button[] array, Button botao)
    {
        if (botao == null) return array;

        if (array == null)
            array = new Button[0];

        Button[] novoArray = new Button[array.Length + 1];

        for (int i = 0; i < array.Length; i++)
            novoArray[i] = array[i];

        novoArray[novoArray.Length - 1] = botao;
        return novoArray;
    }

    void InicializarConfiguracoesIA()
    {
        if (botoesPlayer1 != null)
        {
            int n = botoesPlayer1.Length;
            estilosP1 = new EstiloIA[n];
            dificuldadesP1 = new DificuldadeIA[n];
            lastClickTimeP1 = new float[n];
            for (int i = 0; i < n; i++)
            {
                estilosP1[i] = EstiloIA.Equilibrado;
                dificuldadesP1[i] = DificuldadeIA.Medio;
                lastClickTimeP1[i] = -10f;
            }
        }

        if (botoesPlayer2 != null)
        {
            int n = botoesPlayer2.Length;
            estilosP2 = new EstiloIA[n];
            dificuldadesP2 = new DificuldadeIA[n];
            lastClickTimeP2 = new float[n];
            for (int i = 0; i < n; i++)
            {
                estilosP2[i] = EstiloIA.Equilibrado;
                dificuldadesP2[i] = DificuldadeIA.Medio;
                lastClickTimeP2[i] = -10f;
            }
        }
    }

    void ConfigurarBotoesAuxiliares()
    {
        if (botaoDeselecionarP1 != null)
        {
            botaoDeselecionarP1.onClick.RemoveAllListeners();
            botaoDeselecionarP1.onClick.AddListener(DeselecionarPersonagemP1);

            EventoHoverUI h = botaoDeselecionarP1.GetComponent<EventoHoverUI>();
            if (h == null) h = botaoDeselecionarP1.gameObject.AddComponent<EventoHoverUI>();
            h.aoEntrar = () =>
            {
                if (PainelConfiguracaoIAAberto()) return;

                grupoAtual = 2;
                focoInferior = 0;
                AtualizarBotaoVoltar();
                AtualizarFocoInferior();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
            h.aoSair = () => AtualizarFocoInferior();
        }

        if (botaoDeselecionarP2 != null)
        {
            botaoDeselecionarP2.onClick.RemoveAllListeners();
            botaoDeselecionarP2.onClick.AddListener(DeselecionarPersonagemP2);

            EventoHoverUI h = botaoDeselecionarP2.GetComponent<EventoHoverUI>();
            if (h == null) h = botaoDeselecionarP2.gameObject.AddComponent<EventoHoverUI>();
            h.aoEntrar = () =>
            {
                if (PainelConfiguracaoIAAberto()) return;

                grupoAtual = 2;
                focoInferior = 2;
                AtualizarBotaoVoltar();
                AtualizarFocoInferior();
                AtualizarFocoVisualP1();
                AtualizarFocoVisualP2();
            };
            h.aoSair = () => AtualizarFocoInferior();
        }

        if (botaoSalvarConfigIAP1 != null)
        {
            botaoSalvarConfigIAP1.onClick.RemoveAllListeners();
            botaoSalvarConfigIAP1.onClick.AddListener(SalvarConfiguracaoIA);
        }

        if (botaoFecharConfigIAP1 != null)
        {
            botaoFecharConfigIAP1.onClick.RemoveAllListeners();
            botaoFecharConfigIAP1.onClick.AddListener(FecharConfiguracaoIAAtiva);
        }

        if (botaoSalvarConfigIAP2 != null)
        {
            botaoSalvarConfigIAP2.onClick.RemoveAllListeners();
            botaoSalvarConfigIAP2.onClick.AddListener(SalvarConfiguracaoIA);
        }

        if (botaoFecharConfigIAP2 != null)
        {
            botaoFecharConfigIAP2.onClick.RemoveAllListeners();
            botaoFecharConfigIAP2.onClick.AddListener(FecharConfiguracaoIAAtiva);
        }

        // Hover de mouse dos botões Salvar/Fechar do painel de IA — mesma lógica
        // de Foco/Hover do Iniciar/Voltar, sincronizada com focoIAPanel (2=Salvar,
        // 3=Fechar) pra teclado e mouse nunca ficarem em desacordo.
        ConfigurarHoverBotaoIA(botaoSalvarConfigIAP1, 1, 2);
        ConfigurarHoverBotaoIA(botaoFecharConfigIAP1, 1, 3);
        ConfigurarHoverBotaoIA(botaoSalvarConfigIAP2, 2, 2);
        ConfigurarHoverBotaoIA(botaoFecharConfigIAP2, 2, 3);
    }

    void ConfigurarHoverBotaoIA(Button botao, int lado, int focoAlvo)
    {
        if (botao == null) return;

        EventoHoverUI h = botao.GetComponent<EventoHoverUI>();
        if (h == null) h = botao.gameObject.AddComponent<EventoHoverUI>();

        h.aoEntrar = () =>
        {
            if (ladoIAConfigAberta != lado) return; // painel deste lado não é o que está aberto agora

            mouseSobreBotaoInferior = true;
            focoIAPanel = focoAlvo;
            UIFocusUtility.Select(botao.gameObject);
            AtualizarFocoVisualPainelIA();
        };
        h.aoSair = () =>
        {
            mouseSobreBotaoInferior = false;
            AtualizarFocoVisualPainelIA();
        };
    }

    // Repinta Salvar/Fechar do painel de IA aberto de acordo com focoIAPanel —
    // chamada tanto pela navegação por teclado (NavegarPainelIA) quanto pelo
    // hover de mouse (ConfigurarHoverBotaoIA), então os dois nunca se contradizem.
    void AtualizarFocoVisualPainelIA()
    {
        if (ladoIAConfigAberta == 0) return;

        Button btnSalvar = ladoIAConfigAberta == 1 ? botaoSalvarConfigIAP1 : botaoSalvarConfigIAP2;
        Button btnFechar = ladoIAConfigAberta == 1 ? botaoFecharConfigIAP1 : botaoFecharConfigIAP2;
        Sprite normalSalvar = ladoIAConfigAberta == 1 ? spriteOriginalSalvarIAP1 : spriteOriginalSalvarIAP2;
        Sprite normalFechar = ladoIAConfigAberta == 1 ? spriteOriginalFecharIAP1 : spriteOriginalFecharIAP2;

        if (focoIAPanel == 2)
            PintarFocoOuHover(btnSalvar, spriteFocoSalvarIA, spriteHoverSalvarIA, normalSalvar);
        else
            SetSpriteBotao(btnSalvar, normalSalvar);

        if (focoIAPanel == 3)
            PintarFocoOuHover(btnFechar, spriteFocoFecharIA, spriteHoverFecharIA, normalFechar);
        else
            SetSpriteBotao(btnFechar, normalFechar);
    }

    void CarregarTeclas()
    {
        teclaEsquerdaP1 = StringParaKeyCode(PlayerPrefs.GetString("P1_Esquerda", "A"), KeyCode.A);
        teclaDireitaP1 = StringParaKeyCode(PlayerPrefs.GetString("P1_Direita", "D"), KeyCode.D);
        teclaConfirmarP1 = StringParaKeyCode(PlayerPrefs.GetString("P1_Ataque", "F"), KeyCode.F);
        teclaCimaP1 = KeyCode.W;
        teclaBaixoP1 = KeyCode.S;
        teclaDefenderP1 = KeyCode.X;

        teclaEsquerdaP2 = StringParaKeyCode(PlayerPrefs.GetString("P2_Esquerda", "LeftArrow"), KeyCode.LeftArrow);
        teclaDireitaP2 = StringParaKeyCode(PlayerPrefs.GetString("P2_Direita", "RightArrow"), KeyCode.RightArrow);
        teclaConfirmarP2 = StringParaKeyCode(PlayerPrefs.GetString("P2_Ataque", "K"), KeyCode.K);
        teclaCimaP2 = KeyCode.UpArrow;
        teclaBaixoP2 = KeyCode.DownArrow;
        teclaDefenderP2 = KeyCode.M;
    }

    KeyCode StringParaKeyCode(string valor, KeyCode padrao)
    {
        try { return (KeyCode)System.Enum.Parse(typeof(KeyCode), valor); }
        catch { return padrao; }
    }

    void GuardarCoresOriginais()
    {
        if (botaoIniciar != null)
        {
            Image img = botaoIniciar.GetComponent<Image>();
            corOriginalIniciar = img != null ? img.color : Color.white;
            spriteOriginalIniciar = img != null ? img.sprite : null;
        }

        if (botaoVoltar != null)
        {
            Image img = botaoVoltar.GetComponent<Image>();
            corOriginalVoltar = img != null ? img.color : Color.white;
            spriteOriginalVoltar = img != null ? img.sprite : null;
        }

        if (botaoDeselecionarP1 != null)
        {
            Image img = botaoDeselecionarP1.GetComponent<Image>();
            corOriginalDeselecionarP1 = img != null ? img.color : Color.white;
        }

        if (botaoDeselecionarP2 != null)
        {
            Image img = botaoDeselecionarP2.GetComponent<Image>();
            corOriginalDeselecionarP2 = img != null ? img.color : Color.white;
        }

        spriteOriginalSalvarIAP1 = CapturarSpriteEDesligarTransition(botaoSalvarConfigIAP1);
        spriteOriginalFecharIAP1 = CapturarSpriteEDesligarTransition(botaoFecharConfigIAP1);
        spriteOriginalSalvarIAP2 = CapturarSpriteEDesligarTransition(botaoSalvarConfigIAP2);
        spriteOriginalFecharIAP2 = CapturarSpriteEDesligarTransition(botaoFecharConfigIAP2);

        // Iniciar/Voltar também passam a ser 100% pintados pelo script — desliga o
        // Transition nativo deles pra Unity não brigar com o nosso sprite de foco.
        DesligarHighlightNativoBotao(botaoIniciar);
        DesligarHighlightNativoBotao(botaoVoltar);
    }

    // Guarda o sprite atual (normal) do botão e desliga o Transition nativo dele —
    // usado pros botões que agora são pintados manualmente (Salvar/Fechar da IA).
    Sprite CapturarSpriteEDesligarTransition(Button botao)
    {
        if (botao == null) return null;
        Image img = botao.GetComponent<Image>();
        Sprite normal = img != null ? img.sprite : null;
        DesligarHighlightNativoBotao(botao);
        return normal;
    }

    void AplicarEstadoInicialPorModo()
    {
        AtualizarVisuaisIA();
    }

    void ConfigurarInteratividadePorModo()
    {
        bool permitirP1 = PermiteSelecaoP1();
        bool permitirP2 = PermiteSelecaoP2();

        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            if (botoesPlayer1[i] != null)
                botoesPlayer1[i].interactable = botoesPlayer1[i].interactable && permitirP1;
        }

        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            if (botoesPlayer2[i] != null)
                botoesPlayer2[i].interactable = botoesPlayer2[i].interactable && permitirP2;
        }
    }

    bool PermiteControleHumanoP1()
    {
        return modoJogo == "PVP" || modoJogo == "PVC";
    }

    bool PermiteControleHumanoP2()
    {
        return modoJogo == "PVP";
    }

    bool PermiteSelecaoP1()
    {
        if (PainelConfiguracaoIAAberto()) return false;
        return modoJogo == "PVP" || modoJogo == "PVC" || modoJogo == "CVC";
    }

    bool PermiteSelecaoP2()
    {
        if (PainelConfiguracaoIAAberto()) return false;
        return modoJogo == "PVP" || modoJogo == "PVC" || modoJogo == "CVC";
    }

    bool UsaIAP1()
    {
        return modoJogo == "CVC";
    }

    bool UsaIAP2()
    {
        return modoJogo == "PVC" || modoJogo == "CVC";
    }

    bool TratarNavegacaoGrupos()
    {
        // Esc volta para tela anterior em qualquer situação
        if (UIInputUtility.WasCancelPressed())
        {
            Voltar();
            return true;
        }

        // Detecção "por borda" de cima/baixo — NÃO usa UIInputUtility.WasNavigateUpPressed()
        // direto aqui porque ele também checa o eixo "Vertical" de forma contínua
        // (Input.GetAxisRaw > 0.5), e W/S/setas alimentam esse eixo por padrão. Isso fazia
        // segurar a tecla (mesmo que por uma fração de segundo) manter a condição "true"
        // por vários frames seguidos, processando várias transições de grupo em sequência
        // (ex: 2->1->0 quase no mesmo instante) — dava a impressão de "pular" direto de
        // Iniciar pro Voltar, sem passar visivelmente pelos personagens.
        float eixoVertical = Input.GetAxisRaw("Vertical");
        bool eixoAcimaAgora = eixoVertical > 0.5f;
        bool eixoAbaixoAgora = eixoVertical < -0.5f;

        bool cimaGlobal = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(teclaCimaP1)
            || Input.GetKeyDown(teclaCimaP2)
            || (eixoAcimaAgora && !eixoVerticalCimaSegurando);
        bool baixoGlobal = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)
            || Input.GetKeyDown(teclaBaixoP1)
            || Input.GetKeyDown(teclaBaixoP2)
            || (eixoAbaixoAgora && !eixoVerticalBaixoSegurando);

        eixoVerticalCimaSegurando = eixoAcimaAgora;
        eixoVerticalBaixoSegurando = eixoAbaixoAgora;

        // Enter global — funciona só no botão Voltar e no botão Start.
        // NÃO seleciona personagem e NÃO confirma os botões de deselecionar.
        if (UIInputUtility.WasSubmitPressed())
        {
            if (grupoAtual == 0)
                Voltar();
            else if (grupoAtual == 2 && focoInferior == 1)
                IniciarJogo();

            SincronizarHoverTeclado();
            return true;
        }

        // ── Hierarquia fixa de 3 níveis ─────────────────────────────────────
        // grupoAtual 0 = Voltar (topo, sozinho)
        // grupoAtual 1 = Personagens (P1 à esquerda / P2 à direita, cada um só
        //                mexe com as próprias teclas — isso é tratado em
        //                TratarTecladoP1/TratarTecladoP2, não aqui)
        // grupoAtual 2 = Iniciar (base, com Deselecionar P1/P2 ao lado)
        //
        // Cima/baixo só andam UM nível por vez, nunca pulam — qualquer tecla de
        // cima ou baixo (de qualquer um dos dois jogadores) serve pra subir/descer
        // de nível; o que muda por jogador é só o movimento esquerda/direita
        // DENTRO do nível 1, que continua totalmente separado.

        if (cimaGlobal)
        {
            if (grupoAtual == 2)
            {
                grupoAtual = 1;
                ultimoLadoAtivo = Input.GetKeyDown(teclaCimaP2) ? 2 : 1;
                AtualizarFocoGrupo();
                AtualizarFocoVisualP1();
                SincronizarHoverTeclado();
                return true;
            }

            if (grupoAtual == 1)
            {
                grupoAtual = 0;
                ultimoLadoAtivo = Input.GetKeyDown(teclaCimaP2) ? 2 : 1;
                AtualizarFocoGrupoVoltar();
                SincronizarHoverTeclado();
                return true;
            }

            // grupoAtual == 0: já está no topo, cima não faz nada.
            return true;
        }

        if (baixoGlobal)
        {
            if (grupoAtual == 0)
            {
                grupoAtual = 1;
                ultimoLadoAtivo = Input.GetKeyDown(teclaBaixoP2) ? 2 : 1;
                AtualizarFocoGrupo();
                AtualizarFocoVisualP1();
                SincronizarHoverTeclado();
                return true;
            }

            if (grupoAtual == 1)
            {
                grupoAtual = 2;
                ultimoLadoAtivo = Input.GetKeyDown(teclaBaixoP2) ? 2 : 1;
                focoInferior = 1;
                AtualizarFocoGrupo();
                AtualizarFocoInferior();
                SincronizarHoverTeclado();
                return true;
            }

            // grupoAtual == 2: já está na base, baixo não faz nada.
            return true;
        }

        // Nada foi consumido aqui — os handlers de P1/P2/Inferior tratam o resto
        // (esquerda/direita dentro do nível 1 ou 2, confirmar, deselecionar, etc.)
        return false;
    }

    void AtualizarFocoGrupoVoltar()
    {
        // Destaca o botão Voltar e limpa destaques inferiores
        PintarFocoOuHover(botaoVoltar, spriteFocoVoltar, spriteHoverVoltar, spriteOriginalVoltar);
        RestaurarCorBotaoInferior(botaoDeselecionarP1, 0);
        RestaurarCorBotaoInferior(botaoIniciar, 1);
        RestaurarCorBotaoInferior(botaoDeselecionarP2, 2);
        AtualizarFocoVisualP1();
        AtualizarFocoVisualP2();
    }

    void AtualizarFocoGrupo()
    {
        // Limpa destaque do botão Voltar ao sair do grupo 0
        SetCorBotao(botaoVoltar, corOriginalVoltar);
        // Recalcula as duas grades — com o fix do "mostrarFoco", isso apaga qualquer
        // destaque de foco (hover/teclado) que tenha ficado preso ao trocar de grupo
        AtualizarFocoVisualP1();
        AtualizarFocoVisualP2();
        AtualizarFocoInferior();
    }

    void SincronizarHoverTeclado()
    {
        mouseSobreBotaoInferior = false;
        AtualizarBotaoVoltar();
        AtualizarFocoVisualP1();
        AtualizarFocoVisualP2();
        AtualizarFocoInferior();
    }

    void AtualizarBotaoVoltar()
    {
        if (botaoVoltar == null)
            return;

        if (grupoAtual == 0)
            PintarFocoOuHover(botaoVoltar, spriteFocoVoltar, spriteHoverVoltar, spriteOriginalVoltar);
        else
            SetSpriteBotao(botaoVoltar, spriteOriginalVoltar);
    }

    bool PainelConfiguracaoIAAberto()
    {
        bool p1Aberto = painelConfigIAP1 != null && painelConfigIAP1.activeSelf;
        bool p2Aberto = painelConfigIAP2 != null && painelConfigIAP2.activeSelf;
        return p1Aberto || p2Aberto;
    }

    void TratarAtalhosPainelIA()
    {
        if (UIInputUtility.WasCancelPressed())
        {
            FecharConfiguracaoIAAtiva();
            return;
        }

        // Enter (ou a tecla de confirmar/ataque do próprio player) respeita o campo
        // focado: no dropdown, abre a lista (1º toque) e confirma/fecha ela (2º toque) —
        // igual um dropdown de verdade. Em Salvar, salva. Em Fechar, fecha. Nunca faz
        // Salvar/Fechar estando com foco no dropdown (era isso que fechava o painel sem
        // lógica nenhuma antes).
        bool confirmarPressionado = UIInputUtility.WasSubmitPressed()
            || (ladoIAConfigAberta == 1 && Input.GetKeyDown(teclaConfirmarP1))
            || (ladoIAConfigAberta == 2 && Input.GetKeyDown(teclaConfirmarP2));

        if (confirmarPressionado)
        {
            if (focoIAPanel == 0 || focoIAPanel == 1)
            {
                TMP_Dropdown dropFocado = ladoIAConfigAberta == 1
                    ? (focoIAPanel == 0 ? dropdownEstiloIAP1 : dropdownDificuldadeIAP1)
                    : (focoIAPanel == 0 ? dropdownEstiloIAP2 : dropdownDificuldadeIAP2);

                if (dropFocado != null)
                {
                    if (dropFocado.IsExpanded)
                        dropFocado.Hide(); // 2º toque: confirma o valor atual e fecha a lista
                    else
                        dropFocado.Show(); // 1º toque: abre a lista
                }
            }
            else if (focoIAPanel == 2)
                SalvarConfiguracaoIA();
            else if (focoIAPanel == 3)
                FecharConfiguracaoIAAtiva();

            return;
        }

        // Painel do P1: teclas do P1 navegam e confirmam
        if (ladoIAConfigAberta == 1)
        {
            TMP_Dropdown dropAtivoP1 = focoIAPanel == 0 ? dropdownEstiloIAP1 : (focoIAPanel == 1 ? dropdownDificuldadeIAP1 : null);
            bool listaAbertaP1 = dropAtivoP1 != null && dropAtivoP1.IsExpanded;

            if (listaAbertaP1 && Input.GetKeyDown(teclaCimaP1))
                MudarValorEMostrarLista(dropAtivoP1, -1);
            else if (listaAbertaP1 && Input.GetKeyDown(teclaBaixoP1))
                MudarValorEMostrarLista(dropAtivoP1, 1);
            else if (Input.GetKeyDown(teclaCimaP1))
                NavegarPainelIA(-1);
            else if (Input.GetKeyDown(teclaBaixoP1))
                NavegarPainelIA(1);
            else if (Input.GetKeyDown(teclaEsquerdaP1))
                AlterarValorDropdownFocado(-1);
            else if (Input.GetKeyDown(teclaDireitaP1))
                AlterarValorDropdownFocado(1);
        }
        // Painel do P2: teclas do P2 navegam e confirmam
        else if (ladoIAConfigAberta == 2)
        {
            TMP_Dropdown dropAtivoP2 = focoIAPanel == 0 ? dropdownEstiloIAP2 : (focoIAPanel == 1 ? dropdownDificuldadeIAP2 : null);
            bool listaAbertaP2 = dropAtivoP2 != null && dropAtivoP2.IsExpanded;

            if (listaAbertaP2 && Input.GetKeyDown(teclaCimaP2))
                MudarValorEMostrarLista(dropAtivoP2, -1);
            else if (listaAbertaP2 && Input.GetKeyDown(teclaBaixoP2))
                MudarValorEMostrarLista(dropAtivoP2, 1);
            else if (Input.GetKeyDown(teclaCimaP2))
                NavegarPainelIA(-1);
            else if (Input.GetKeyDown(teclaBaixoP2))
                NavegarPainelIA(1);
            else if (Input.GetKeyDown(teclaEsquerdaP2))
                AlterarValorDropdownFocado(-1);
            else if (Input.GetKeyDown(teclaDireitaP2))
                AlterarValorDropdownFocado(1);
        }
    }

    // Esquerda/direita alteram o valor do dropdown que está em foco (focoIAPanel 0 ou 1)
    void AlterarValorDropdownFocado(int direcao)
    {
        if (focoIAPanel == 0)
        {
            TMP_Dropdown drop = ladoIAConfigAberta == 1 ? dropdownEstiloIAP1 : dropdownEstiloIAP2;
            MudarValorEMostrarLista(drop, direcao);
        }
        else if (focoIAPanel == 1)
        {
            TMP_Dropdown drop = ladoIAConfigAberta == 1 ? dropdownDificuldadeIAP1 : dropdownDificuldadeIAP2;
            MudarValorEMostrarLista(drop, direcao);
        }
        else
        {
            // focoIAPanel 2 (Salvar) ou 3 (Fechar): esquerda/direita alterna entre eles
            focoIAPanel = focoIAPanel == 2 ? 3 : 2;
            ultimoBotaoLinhaIA = focoIAPanel;
            AplicarFocoIAPanel();
        }
    }

    // Muda o valor do dropdown e reabre a lista suspensa em seguida, pra ela aparecer
    // com o item certo marcado — se só mudássemos o value com a lista já aberta, o
    // checkmark do item na lista ficaria desatualizado (a Unity só marca o item ativo
    // no momento em que a lista é criada, não fica "ouvindo" mudanças de value depois).
    void MudarValorEMostrarLista(TMP_Dropdown drop, int direcao)
    {
        if (drop == null) return;

        drop.value = Mathf.Clamp(drop.value + direcao, 0, drop.options.Count - 1);

        drop.Hide();
        drop.Show();
    }

    // Navegação Tab dentro do painel de IA com destaque visual igual aos outros botões
    private int focoIAPanel = 0; // 0=EstiloDropdown, 1=DificuldadeDropdown, 2=Salvar, 3=Fechar
    private int ultimoBotaoLinhaIA = 2; // lembra se foi Salvar(2) ou Fechar(3) da última vez que Cima/Baixo passou pela linha

    void NavegarPainelIA(int direcao)
    {
        // Salvar e Fechar ficam lado a lado na mesma linha — Cima/Baixo não deve
        // parar em cada um separadamente, só entra/sai da linha (Esquerda/Direita
        // que alternam entre os dois, ali embaixo em AlterarValorDropdownFocado).
        if (direcao > 0) // baixo
            focoIAPanel = focoIAPanel == 1 ? ultimoBotaoLinhaIA : Mathf.Min(focoIAPanel + 1, 1);
        else // cima
            focoIAPanel = (focoIAPanel == 2 || focoIAPanel == 3) ? 1 : Mathf.Max(focoIAPanel - 1, 0);

        AplicarFocoIAPanel();
    }

    // Seleciona (EventSystem) e repinta o elemento correspondente ao focoIAPanel
    // atual, sem alterar focoIAPanel — usado tanto pelo Cima/Baixo (NavegarPainelIA)
    // quanto pelo Esquerda/Direita quando o foco está na linha Salvar/Fechar.
    void AplicarFocoIAPanel()
    {
        TMP_Dropdown dropEstilo = ladoIAConfigAberta == 1 ? dropdownEstiloIAP1 : dropdownEstiloIAP2;
        TMP_Dropdown dropDific = ladoIAConfigAberta == 1 ? dropdownDificuldadeIAP1 : dropdownDificuldadeIAP2;
        Button btnSalvar = ladoIAConfigAberta == 1 ? botaoSalvarConfigIAP1 : botaoSalvarConfigIAP2;
        Button btnFechar = ladoIAConfigAberta == 1 ? botaoFecharConfigIAP1 : botaoFecharConfigIAP2;

        // Teclado/controle sempre "solta" o mouse — se o destaque veio de uma
        // tecla, não é mais o hover do mouse que está ativo.
        mouseSobreBotaoInferior = false;

        // Fecha a lista suspensa de qualquer um dos dois dropdowns antes de aplicar o
        // novo foco — evita ficar uma lista "fantasma" aberta quando o jogador sai do
        // dropdown pra outro campo (Cima/Baixo pro outro dropdown, ou pra Salvar/Fechar).
        if (dropEstilo != null) dropEstilo.Hide();
        if (dropDific != null) dropDific.Hide();

        // A navegação usa UIFocusUtility.Select pra seleção real do EventSystem
        // (dropdowns dependem disso); Salvar/Fechar agora são pintados pelo nosso
        // próprio sistema de sprites (AtualizarFocoVisualPainelIA), já que o
        // Transition nativo deles foi desligado.
        switch (focoIAPanel)
        {
            case 0:
                if (dropEstilo != null)
                    UIFocusUtility.Select(dropEstilo.gameObject);
                break;
            case 1:
                if (dropDific != null)
                    UIFocusUtility.Select(dropDific.gameObject);
                break;
            case 2:
                if (btnSalvar != null)
                    UIFocusUtility.Select(btnSalvar.gameObject);
                break;
            case 3:
                if (btnFechar != null)
                    UIFocusUtility.Select(btnFechar.gameObject);
                break;
        }

        AtualizarFocoVisualPainelIA();
    }

    /// <summary>
    /// Aplica cor no background do dropdown (componente Image do próprio dropdown).
    /// </summary>
    void SetCorDropdown(TMP_Dropdown dropdown, Color cor)
    {
        if (dropdown == null) return;
        Image img = dropdown.GetComponent<Image>();
        if (img != null) img.color = cor;
    }

    void RestaurarCorOriginalBotaoIA(Button botao)
    {
        if (botao == null) return;
        Image img = botao.GetComponent<Image>();
        // Restaura a cor original guardada — usa branco como fallback seguro
        if (img != null) img.color = Color.white;
    }

    // Enter global no grupo 1 — funciona IGUAL às teclas de ataque (F/K) de cada player,
    // só que global: age SOMENTE sobre o último lado ativo. Nunca toca no lado oposto.
    // Nunca desseleciona — se já está selecionado e é IA, abre config; senão seleciona normalmente.
    void ConfirmarSelecaoGlobal()
    {
        if (ultimoLadoAtivo == 2)
        {
            if (PermiteSelecaoP2()) ConfirmarLadoP2();
        }
        else
        {
            if (PermiteSelecaoP1()) ConfirmarLadoP1();
        }
    }

    // Mesma lógica da tecla de ataque do P1 (ex: F)
    void ConfirmarLadoP1()
    {
        if (focoP1 == indiceSelecionadoP1 && focoP1 >= 0)
        {
            // Já selecionado: abre config de IA (se modo IA), senão não faz nada (Enter não desseleciona)
            if (UsaIAP1())
                AbrirConfiguracaoIA(1, focoP1);
            // Enter nunca desseleciona — apenas teclas de ataque do próprio player fazem isso
        }
        else
        {
            SelecionarPersonagemP1(focoP1);
        }
    }

    // Mesma lógica da tecla de ataque do P2 (ex: K)
    void ConfirmarLadoP2()
    {
        if (focoP2 == indiceSelecionadoP2 && focoP2 >= 0)
        {
            // Já selecionado: abre config de IA (se modo IA), senão não faz nada (Enter não desseleciona)
            if (UsaIAP2())
                AbrirConfiguracaoIA(2, focoP2);
            // Enter nunca desseleciona — apenas teclas de ataque do próprio player fazem isso
        }
        else
        {
            SelecionarPersonagemP2(focoP2);
        }
    }

    void TratarTecladoInferior()
    {
        bool esquerdaP1 = Input.GetKeyDown(teclaEsquerdaP1);
        bool esquerdaP2 = Input.GetKeyDown(teclaEsquerdaP2);
        bool direitaP1 = Input.GetKeyDown(teclaDireitaP1);
        bool direitaP2 = Input.GetKeyDown(teclaDireitaP2);
        bool confirmarP1 = Input.GetKeyDown(teclaConfirmarP1);
        bool confirmarP2 = Input.GetKeyDown(teclaConfirmarP2);

        // Esquerda/Direita no grupo inferior — só Start existe, então apenas muda ultimoLadoAtivo
        if (esquerdaP1 || direitaP1) { ultimoLadoAtivo = 1; return; }
        if (esquerdaP2 || direitaP2) { ultimoLadoAtivo = 2; return; }

        // Confirmar — qualquer player inicia o jogo
        if (confirmarP1 || confirmarP2)
        {
            IniciarJogo();
            return;
        }

        // No grupo inferior, S/Seta para baixo não sobem mais.
        // Para voltar aos personagens use W/Seta para cima.
    }

    // Mantido por compatibilidade, mas o Enter global só pode confirmar o Start.
    void ConfirmarBotaoInferior()
    {
        if (focoInferior == 1)
            IniciarJogo();
    }

    // Confirma o botão focado, respeitando qual lado está ativo
    void ConfirmarBotaoInferiorParaLado(int lado)
    {
        if (focoInferior == 0)
        {
            // Deselecionar P1 — só P1 pode fazer isso
            if (lado == 1) DeselecionarPersonagemP1();
        }
        else if (focoInferior == 1)
        {
            // Start — qualquer lado pode confirmar
            IniciarJogo();
        }
        else if (focoInferior == 2)
        {
            // Deselecionar P2 — só P2 pode fazer isso
            if (lado == 2) DeselecionarPersonagemP2();
        }
    }

    void AtualizarFocoInferior()
    {
        // Só o Start existe no grupo inferior agora
        // Deselecionar foi movido para a tecla Defender de cada player
        if (grupoAtual == 2)
            PintarFocoOuHover(botaoIniciar, spriteFocoIniciar, spriteHoverIniciar, spriteOriginalIniciar);
        else
            SetSpriteBotao(botaoIniciar, spriteOriginalIniciar);
    }

    void RestaurarCorBotaoInferior(Button botao, int indice)
    {
        if (botao == null) return;

        if (indice == 1)
            SetCorBotao(botao, corOriginalIniciar);
        else if (indice == 0)
            SetCorBotao(botao, corOriginalDeselecionarP1);
        else if (indice == 2)
            SetCorBotao(botao, corOriginalDeselecionarP2);
        else
            SetCorBotao(botao, Color.white);
    }

    void TratarTecladoP1()
    {
        int total = ContarBotoesValidosP1();
        if (total == 0) return;

        if (Input.GetKeyDown(teclaEsquerdaP1))
        {
            ultimoLadoAtivo = 1;
            focoP1 = ProximoIndiceValidoP1(focoP1, -1);
            AtualizarFocoVisualP1();
            if (indiceSelecionadoP1 < 0) PreviewPersonagemP1(focoP1);
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaDireitaP1))
        {
            ultimoLadoAtivo = 1;
            focoP1 = ProximoIndiceValidoP1(focoP1, 1);
            AtualizarFocoVisualP1();
            if (indiceSelecionadoP1 < 0) PreviewPersonagemP1(focoP1);
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaDefenderP1))
        {
            ultimoLadoAtivo = 1;
            if (indiceSelecionadoP1 >= 0)
                DeselecionarPersonagemP1();
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaConfirmarP1))
        {
            ultimoLadoAtivo = 1;

            if (EhSlotRandomP1(focoP1))
            {
                StartCoroutine(RoletaPersonagemP1());
                return;
            }

            if (focoP1 == indiceSelecionadoP1 && focoP1 >= 0)
            {
                if (UsaIAP1())
                    AbrirConfiguracaoIA(1, focoP1);
            }
            else
            {
                SelecionarPersonagemP1(focoP1);
            }
            SincronizarHoverTeclado();
        }
    }

    void TratarTecladoP2()
    {
        int total = ContarBotoesValidosP2();
        if (total == 0) return;

        if (Input.GetKeyDown(teclaEsquerdaP2))
        {
            ultimoLadoAtivo = 2;
            focoP2 = ProximoIndiceValidoP2(focoP2, -1);
            AtualizarFocoVisualP2();
            if (indiceSelecionadoP2 < 0) PreviewPersonagemP2(focoP2);
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaDireitaP2))
        {
            ultimoLadoAtivo = 2;
            focoP2 = ProximoIndiceValidoP2(focoP2, 1);
            AtualizarFocoVisualP2();
            if (indiceSelecionadoP2 < 0) PreviewPersonagemP2(focoP2);
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaDefenderP2))
        {
            ultimoLadoAtivo = 2;
            if (indiceSelecionadoP2 >= 0)
                DeselecionarPersonagemP2();
            SincronizarHoverTeclado();
        }
        else if (Input.GetKeyDown(teclaConfirmarP2))
        {
            ultimoLadoAtivo = 2;

            if (EhSlotRandomP2(focoP2))
            {
                StartCoroutine(RoletaPersonagemP2());
                return;
            }

            if (focoP2 == indiceSelecionadoP2 && focoP2 >= 0)
            {
                if (UsaIAP2())
                    AbrirConfiguracaoIA(2, focoP2);
            }
            else
            {
                SelecionarPersonagemP2(focoP2);
            }
            SincronizarHoverTeclado();
        }
    }

    void ConfigurarBotoesP1()
    {
        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            int index = i;
            bool slotRandom = EhSlotRandomP1(index);
            bool personagemValido = index < corposPlayer1.Length && corposPlayer1[index] != null
                                  && index < nomesPlayer1.Length && !string.IsNullOrEmpty(nomesPlayer1[index]);
            bool slotValido = slotRandom || personagemValido;

            if (botoesPlayer1[i] != null)
                botoesPlayer1[i].interactable = slotValido;

            if (!slotValido)
            {
                SetDestaqueP1(index, corDestaqueInvisivel);

                if (rostosSlotsP1 != null && index < rostosSlotsP1.Length && rostosSlotsP1[index] != null)
                    rostosSlotsP1[index].enabled = false;

                continue;
            }

            if (!slotRandom && rostosSlotsP1 != null && index < rostosSlotsP1.Length && rostosSlotsP1[index] != null)
            {
                Sprite rosto = index < rostosPlayer1.Length ? rostosPlayer1[index] : null;
                if (rosto != null)
                {
                    rostosSlotsP1[index].sprite = rosto;
                    rostosSlotsP1[index].enabled = true;
                }
            }

            botoesPlayer1[i].onClick.RemoveAllListeners();
            botoesPlayer1[i].onClick.AddListener(() =>
            {
                if (!PermiteSelecaoP1())
                    return;

                if (EhSlotRandomP1(index))
                {
                    StartCoroutine(RoletaPersonagemP1());
                    return;
                }

                if (indiceSelecionadoP1 == index)
                {
                    if (UsaIAP1())
                    {
                        float now = Time.time;
                        if (now - lastClickTimeP1[index] < doubleClickThreshold)
                            AbrirConfiguracaoIA(1, index);
                        lastClickTimeP1[index] = now;
                    }
                    else
                    {
                        DeselecionarPersonagemP1();
                    }
                }
                else
                {
                    SelecionarPersonagemP1(index);
                }
            });

            EventoHoverUI hover = botoesPlayer1[i].GetComponent<EventoHoverUI>();
            if (hover == null) hover = botoesPlayer1[i].gameObject.AddComponent<EventoHoverUI>();
            hover.aoEntrar = () => AoEntrarBotaoP1(index);
            hover.aoSair = () => AoSairBotaoP1(index);
        }
    }

    void ConfigurarBotoesP2()
    {
        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            int index = i;
            bool slotRandom = EhSlotRandomP2(index);
            bool personagemValido = index < corposPlayer2.Length && corposPlayer2[index] != null
                                  && index < nomesPlayer2.Length && !string.IsNullOrEmpty(nomesPlayer2[index]);
            bool slotValido = slotRandom || personagemValido;

            if (botoesPlayer2[i] != null)
                botoesPlayer2[i].interactable = slotValido;

            if (!slotValido)
            {
                SetDestaqueP2(index, corDestaqueInvisivel);

                if (rostosSlotsP2 != null && index < rostosSlotsP2.Length && rostosSlotsP2[index] != null)
                    rostosSlotsP2[index].enabled = false;

                continue;
            }

            if (!slotRandom && rostosSlotsP2 != null && index < rostosSlotsP2.Length && rostosSlotsP2[index] != null)
            {
                Sprite rosto = index < rostosPlayer2.Length ? rostosPlayer2[index] : null;
                if (rosto != null)
                {
                    rostosSlotsP2[index].sprite = rosto;
                    rostosSlotsP2[index].enabled = true;
                }
            }

            botoesPlayer2[i].onClick.RemoveAllListeners();
            botoesPlayer2[i].onClick.AddListener(() =>
            {
                if (!PermiteSelecaoP2())
                    return;

                if (EhSlotRandomP2(index))
                {
                    StartCoroutine(RoletaPersonagemP2());
                    return;
                }

                if (indiceSelecionadoP2 == index)
                {
                    if (UsaIAP2())
                    {
                        float now = Time.time;
                        if (now - lastClickTimeP2[index] < doubleClickThreshold)
                            AbrirConfiguracaoIA(2, index);
                        lastClickTimeP2[index] = now;
                    }
                    else
                    {
                        DeselecionarPersonagemP2();
                    }
                }
                else
                {
                    SelecionarPersonagemP2(index);
                }
            });

            EventoHoverUI hover = botoesPlayer2[i].GetComponent<EventoHoverUI>();
            if (hover == null) hover = botoesPlayer2[i].gameObject.AddComponent<EventoHoverUI>();
            hover.aoEntrar = () => AoEntrarBotaoP2(index);
            hover.aoSair = () => AoSairBotaoP2(index);
        }
    }

    void AoEntrarBotaoP1(int index)
    {
        if (!PermiteSelecaoP1())
            return;

        ultimoLadoAtivo = 1;
        focoP1 = index;

        // O mouse passando por cima de um personagem sempre traz a navegação de volta
        // pra grade (grupo 1) e limpa qualquer destaque que estivesse preso no Iniciar/
        // Voltar/Deselecionar — sem isso, dava pra ter o personagem E o Iniciar amarelos
        // ao mesmo tempo (um pelo mouse, outro pelo teclado).
        if (grupoAtual != 1)
        {
            grupoAtual = 1;
            AtualizarBotaoVoltar();
            AtualizarFocoInferior();
        }

        // Sempre chama a varredura completa (AtualizarFocoVisualP1), não pinta só o
        // botão novo — ela apaga automaticamente o destaque de foco de qualquer outro
        // botão que tenha ficado aceso antes (pelo teclado, controle ou mouse). Assim,
        // só o botão SELECIONADO de verdade (Enter/clique) mantém o destaque quando o
        // foco muda de lugar; o resto some.
        AtualizarFocoVisualP1();

        if (indiceSelecionadoP1 < 0)
            PreviewPersonagemP1(index);
    }

    void AoSairBotaoP1(int index)
    {
        if (!PermiteSelecaoP1())
            return;

        SetDestaqueP1(index, index == indiceSelecionadoP1 ? corDestaqueVisivel : corDestaqueInvisivel);
        RestaurarPreviewP1();
    }

    void AoEntrarBotaoP2(int index)
    {
        if (!PermiteSelecaoP2())
            return;

        ultimoLadoAtivo = 2;
        focoP2 = index;

        if (grupoAtual != 1)
        {
            grupoAtual = 1;
            AtualizarBotaoVoltar();
            AtualizarFocoInferior();
        }

        AtualizarFocoVisualP2();

        if (indiceSelecionadoP2 < 0)
            PreviewPersonagemP2(index);
    }

    void AoSairBotaoP2(int index)
    {
        if (!PermiteSelecaoP2())
            return;

        SetDestaqueP2(index, index == indiceSelecionadoP2 ? corDestaqueVisivel : corDestaqueInvisivel);
        RestaurarPreviewP2();
    }

    void AtualizarFocoVisualP1()
    {
        // O destaque "de foco" (hover/teclado, ainda não confirmado) só faz sentido
        // enquanto realmente estamos navegando a grade de personagens (grupo 1).
        // Fora disso, mostra só o destaque do personagem já SELECIONADO (se houver) —
        // é isso que evita a grade ficar "acesa" junto com o botão Iniciar/Voltar.
        bool mostrarFoco = grupoAtual == 1;

        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            if (botoesPlayer1[i] == null || !botoesPlayer1[i].interactable) continue;

            if (i == indiceSelecionadoP1) SetDestaqueP1(i, corDestaqueVisivel);
            else if (mostrarFoco && i == focoP1) SetDestaqueP1(i, corHoverSuave);
            else SetDestaqueP1(i, corDestaqueInvisivel);
        }
    }

    void AtualizarFocoVisualP2()
    {
        bool mostrarFoco = grupoAtual == 1;

        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            if (botoesPlayer2[i] == null || !botoesPlayer2[i].interactable) continue;

            if (i == indiceSelecionadoP2) SetDestaqueP2(i, corDestaqueVisivel);
            else if (mostrarFoco && i == focoP2) SetDestaqueP2(i, corHoverSuave);
            else SetDestaqueP2(i, corDestaqueInvisivel);
        }
    }

    void PreviewPersonagemP1(int indice)
    {
        bool ehRandom = EhSlotRandomP1(indice);

        if (!ehRandom)
        {
            if (indice < 0 || indice >= corposPlayer1.Length)
                return;

            if (corposPlayer1[indice] == null)
                return;
        }

        if (imagemPlayer1 != null)
        {
            imagemPlayer1.gameObject.SetActive(true);

            imagemPlayer1.sprite = ehRandom
                ? spriteRandom
                : corposPlayer1[indice];

            imagemPlayer1.preserveAspect = true;
        }

        if (nomePlayer1 != null)
        {
            if (ehRandom)
            {
                // Esconde o texto normal, mostra o LocalizedText de random
                nomePlayer1.gameObject.SetActive(false);
                if (nomeRandomP1 != null) nomeRandomP1.SetActive(true);
            }
            else
            {
                // Mostra o texto normal com o nome do personagem
                if (nomeRandomP1 != null) nomeRandomP1.SetActive(false);
                nomePlayer1.gameObject.SetActive(true);
                nomePlayer1.text = nomesPlayer1[indice];
            }
        }
    }
    void PreviewPersonagemP2(int indice)
    {
        bool ehRandom = EhSlotRandomP2(indice);

        if (!ehRandom)
        {
            if (indice < 0 || indice >= corposPlayer2.Length)
                return;

            if (corposPlayer2[indice] == null)
                return;
        }

        if (imagemPlayer2 != null)
        {
            imagemPlayer2.gameObject.SetActive(true);

            imagemPlayer2.sprite = ehRandom
                ? spriteRandom
                : corposPlayer2[indice];

            imagemPlayer2.preserveAspect = true;
        }

        if (nomePlayer2 != null)
        {
            if (ehRandom)
            {
                nomePlayer2.gameObject.SetActive(false);
                if (nomeRandomP2 != null) nomeRandomP2.SetActive(true);
            }
            else
            {
                if (nomeRandomP2 != null) nomeRandomP2.SetActive(false);
                nomePlayer2.gameObject.SetActive(true);
                nomePlayer2.text = nomesPlayer2[indice];
            }
        }
    }

    void RestaurarPreviewP1()
    {
        if (indiceSelecionadoP1 >= 0)
            PreviewPersonagemP1(indiceSelecionadoP1);
        else
        {
            if (imagemPlayer1 != null) imagemPlayer1.gameObject.SetActive(false);
            if (nomePlayer1 != null) { nomePlayer1.gameObject.SetActive(true); nomePlayer1.text = ""; }
            if (nomeRandomP1 != null) nomeRandomP1.SetActive(false);
        }
    }

    void RestaurarPreviewP2()
    {
        if (indiceSelecionadoP2 >= 0)
            PreviewPersonagemP2(indiceSelecionadoP2);
        else
        {
            if (imagemPlayer2 != null) imagemPlayer2.gameObject.SetActive(false);
            if (nomePlayer2 != null) { nomePlayer2.gameObject.SetActive(true); nomePlayer2.text = ""; }
            if (nomeRandomP2 != null) nomeRandomP2.SetActive(false);
        }
    }

    void SelecionarPersonagemP1(int indice)
    {
        if (PainelConfiguracaoIAAberto()) return;
        if (indice < 0 || indice >= botoesPlayer1.Length) return;
        if (indice >= corposPlayer1.Length || corposPlayer1[indice] == null) return;
        if (indice >= nomesPlayer1.Length || string.IsNullOrEmpty(nomesPlayer1[indice])) return;

        if (indiceSelecionadoP1 >= 0)
            SetDestaqueP1(indiceSelecionadoP1, corDestaqueInvisivel);

        if (indiceSelecionadoP1 >= 0)
            SetEngrenagemP1(indiceSelecionadoP1, false);

        indiceSelecionadoP1 = indice;
        focoP1 = indice;
        ultimoLadoAtivo = 1;
        SetDestaqueP1(indice, corDestaqueVisivel);
        PreviewPersonagemP1(indice);
        SetEngrenagemP1(indice, UsaIAP1());
        AtualizarVisuaisIA();

        PlayerPrefs.SetInt("PersonagemP1", indice);
        PlayerPrefs.Save();

        // Se esse clique veio do mouse, a Unity automaticamente marcou esse botão como
        // o "selecionado de verdade" do EventSystem — o que faria um Enter futuro em
        // QUALQUER lugar da tela (Iniciar, Voltar, etc.) disparar o onClick dele de novo
        // sozinho. Limpa aqui pra garantir que isso nunca fica "preso".
        UIFocusUtility.ClearSelection();
    }

    void SelecionarPersonagemP2(int indice)
    {
        if (PainelConfiguracaoIAAberto()) return;
        if (indice < 0 || indice >= botoesPlayer2.Length) return;
        if (indice >= corposPlayer2.Length || corposPlayer2[indice] == null) return;
        if (indice >= nomesPlayer2.Length || string.IsNullOrEmpty(nomesPlayer2[indice])) return;

        if (indiceSelecionadoP2 >= 0)
            SetDestaqueP2(indiceSelecionadoP2, corDestaqueInvisivel);

        if (indiceSelecionadoP2 >= 0)
            SetEngrenagemP2(indiceSelecionadoP2, false);

        indiceSelecionadoP2 = indice;
        focoP2 = indice;
        ultimoLadoAtivo = 2;
        SetDestaqueP2(indice, corDestaqueVisivel);
        PreviewPersonagemP2(indice);
        SetEngrenagemP2(indice, UsaIAP2());
        AtualizarVisuaisIA();

        PlayerPrefs.SetInt("PersonagemP2", indice);
        PlayerPrefs.Save();

        // Mesmo motivo do SelecionarPersonagemP1 — limpa a seleção real do EventSystem
        // pra um Enter futuro em qualquer lugar da tela não disparar esse botão de novo.
        UIFocusUtility.ClearSelection();
    }

    void DeselecionarPersonagemP1()
    {
        if (PainelConfiguracaoIAAberto()) return;
        if (indiceSelecionadoP1 < 0) return;
        SetDestaqueP1(indiceSelecionadoP1, corDestaqueInvisivel);
        SetEngrenagemP1(indiceSelecionadoP1, false);
        indiceSelecionadoP1 = -1;
        if (imagemPlayer1 != null) imagemPlayer1.gameObject.SetActive(false);
        if (nomePlayer1 != null) nomePlayer1.text = "";
        PlayerPrefs.DeleteKey("PersonagemP1");
        AtualizarVisuaisIA();
        AtualizarFocoVisualP1();
        UIFocusUtility.ClearSelection();
    }

    void DeselecionarPersonagemP2()
    {
        if (PainelConfiguracaoIAAberto()) return;
        if (indiceSelecionadoP2 < 0) return;
        SetDestaqueP2(indiceSelecionadoP2, corDestaqueInvisivel);
        SetEngrenagemP2(indiceSelecionadoP2, false);
        indiceSelecionadoP2 = -1;
        if (imagemPlayer2 != null) imagemPlayer2.gameObject.SetActive(false);
        if (nomePlayer2 != null) nomePlayer2.text = "";
        PlayerPrefs.DeleteKey("PersonagemP2");
        AtualizarVisuaisIA();
        AtualizarFocoVisualP2();
        UIFocusUtility.ClearSelection();
    }


    bool EhSlotRandomP1(int index)
    {
        return botaoRandomP1 != null
            && botoesPlayer1 != null
            && index >= 0
            && index < botoesPlayer1.Length
            && botoesPlayer1[index] == botaoRandomP1;
    }

    bool EhSlotRandomP2(int index)
    {
        return botaoRandomP2 != null
            && botoesPlayer2 != null
            && index >= 0
            && index < botoesPlayer2.Length
            && botoesPlayer2[index] == botaoRandomP2;
    }

    IEnumerator RoletaPersonagemP1()
    {
        if (PainelConfiguracaoIAAberto()) yield break;
        if (sorteandoP1) yield break;

        List<int> validos = ObterIndicesValidosP1();
        if (validos.Count == 0) yield break;

        sorteandoP1 = true;
        estadoAtual = EstadoTela.Sorteando;
        ultimoLadoAtivo = 1;

        // Limpa o destaque do próprio botão Random — sem isso ele fica "preso"
        // com a cor de hover depois que a roleta seleciona um personagem real
        if (destaqueRandomP1 != null) destaqueRandomP1.color = corDestaqueInvisivel;

        if (indiceSelecionadoP1 >= 0)
        {
            SetDestaqueP1(indiceSelecionadoP1, corDestaqueInvisivel);
            SetEngrenagemP1(indiceSelecionadoP1, false);
            indiceSelecionadoP1 = -1;
            PlayerPrefs.DeleteKey("PersonagemP1");
        }

        float tempo = tempoInicialRoleta;
        int indiceAtual = validos[0];

        for (int i = 0; i < ciclosRoleta; i++)
        {
            if (i > 0)
                SetDestaqueP1(indiceAtual, corDestaqueInvisivel);

            indiceAtual = validos[Random.Range(0, validos.Count)];
            focoP1 = indiceAtual;

            PreviewPersonagemP1(indiceAtual);
            SetDestaqueP1(indiceAtual, corHoverSuave);

            yield return new WaitForSeconds(tempo);
            tempo += 0.015f;
        }

        SetDestaqueP1(indiceAtual, corDestaqueInvisivel);

        int final = validos[Random.Range(0, validos.Count)];
        SelecionarPersonagemP1(final);
        AtualizarFocoVisualP1(); // garante que nenhum destaque antigo (inclusive do Random) fique preso

        sorteandoP1 = false;
        estadoAtual = EstadoTela.SelecionandoPersonagens;
    }

    IEnumerator RoletaPersonagemP2()
    {
        if (PainelConfiguracaoIAAberto()) yield break;
        if (sorteandoP2) yield break;

        List<int> validos = ObterIndicesValidosP2();
        if (validos.Count == 0) yield break;

        if (!cpuPodeRepetirMesmoPersonagem && indiceSelecionadoP1 >= 0)
        {
            validos.Remove(indiceSelecionadoP1);

            if (validos.Count == 0)
                validos = ObterIndicesValidosP2();
        }

        sorteandoP2 = true;
        estadoAtual = EstadoTela.Sorteando;
        ultimoLadoAtivo = 2;

        // Limpa o destaque do próprio botão Random — sem isso ele fica "preso"
        // com a cor de hover depois que a roleta seleciona um personagem real
        if (destaqueRandomP2 != null) destaqueRandomP2.color = corDestaqueInvisivel;

        if (indiceSelecionadoP2 >= 0)
        {
            SetDestaqueP2(indiceSelecionadoP2, corDestaqueInvisivel);
            SetEngrenagemP2(indiceSelecionadoP2, false);
            indiceSelecionadoP2 = -1;
            PlayerPrefs.DeleteKey("PersonagemP2");
        }

        float tempo = tempoInicialRoleta;
        int indiceAtual = validos[0];

        for (int i = 0; i < ciclosRoleta; i++)
        {
            if (i > 0)
                SetDestaqueP2(indiceAtual, corDestaqueInvisivel);

            indiceAtual = validos[Random.Range(0, validos.Count)];
            focoP2 = indiceAtual;

            PreviewPersonagemP2(indiceAtual);
            SetDestaqueP2(indiceAtual, corHoverSuave);

            yield return new WaitForSeconds(tempo);
            tempo += 0.015f;
        }

        SetDestaqueP2(indiceAtual, corDestaqueInvisivel);

        int final = validos[Random.Range(0, validos.Count)];
        SelecionarPersonagemP2(final);
        AtualizarFocoVisualP2(); // garante que nenhum destaque antigo (inclusive do Random) fique preso

        sorteandoP2 = false;
        estadoAtual = EstadoTela.SelecionandoPersonagens;
    }

    void SortearPersonagemParaP1()
    {
        List<int> validos = ObterIndicesValidosP1();

        if (validos.Count == 0)
            return;

        int sorteado = validos[Random.Range(0, validos.Count)];
        SelecionarPersonagemP1(sorteado);
    }

    void SortearPersonagemParaP2(int evitarIndiceP1 = -1)
    {
        List<int> validos = ObterIndicesValidosP2();

        if (validos.Count == 0)
            return;

        if (!cpuPodeRepetirMesmoPersonagem && evitarIndiceP1 >= 0)
        {
            validos.Remove(evitarIndiceP1);

            if (validos.Count == 0)
                validos = ObterIndicesValidosP2();
        }

        int sorteado = validos[Random.Range(0, validos.Count)];
        SelecionarPersonagemP2(sorteado);
    }

    List<int> ObterIndicesValidosP1()
    {
        List<int> lista = new List<int>();

        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            bool valido = i < corposPlayer1.Length && corposPlayer1[i] != null
                       && i < nomesPlayer1.Length && !string.IsNullOrEmpty(nomesPlayer1[i]);

            if (valido)
                lista.Add(i);
        }

        return lista;
    }

    List<int> ObterIndicesValidosP2()
    {
        List<int> lista = new List<int>();

        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            bool valido = i < corposPlayer2.Length && corposPlayer2[i] != null
                       && i < nomesPlayer2.Length && !string.IsNullOrEmpty(nomesPlayer2[i]);

            if (valido)
                lista.Add(i);
        }

        return lista;
    }

    Button PrimeiroBotaoValidoP1()
    {
        foreach (Button b in botoesPlayer1)
            if (b != null && b.interactable) return b;
        return null;
    }

    int ProximoIndiceValidoP1(int atual, int direcao)
    {
        int tentativa = atual;
        for (int i = 0; i < botoesPlayer1.Length; i++)
        {
            tentativa = (tentativa + direcao + botoesPlayer1.Length) % botoesPlayer1.Length;
            if (botoesPlayer1[tentativa] != null && botoesPlayer1[tentativa].interactable)
                return tentativa;
        }
        return atual;
    }

    int ProximoIndiceValidoP2(int atual, int direcao)
    {
        int tentativa = atual;
        for (int i = 0; i < botoesPlayer2.Length; i++)
        {
            tentativa = (tentativa + direcao + botoesPlayer2.Length) % botoesPlayer2.Length;
            if (botoesPlayer2[tentativa] != null && botoesPlayer2[tentativa].interactable)
                return tentativa;
        }
        return atual;
    }

    int ContarBotoesValidosP1()
    {
        int count = 0;
        foreach (Button b in botoesPlayer1)
            if (b != null && b.interactable) count++;
        return count;
    }

    int ContarBotoesValidosP2()
    {
        int count = 0;
        foreach (Button b in botoesPlayer2)
            if (b != null && b.interactable) count++;
        return count;
    }

    void SetDestaqueP1(int index, Color cor)
    {
        if (EhSlotRandomP1(index))
        {
            if (destaqueRandomP1 != null)
                destaqueRandomP1.color = cor;
            return;
        }

        if (destaquesP1 == null || index < 0 || index >= destaquesP1.Length) return;
        if (destaquesP1[index] != null) destaquesP1[index].color = cor;
    }

    void SetDestaqueP2(int index, Color cor)
    {
        if (EhSlotRandomP2(index))
        {
            if (destaqueRandomP2 != null)
                destaqueRandomP2.color = cor;
            return;
        }

        if (destaquesP2 == null || index < 0 || index >= destaquesP2.Length) return;
        if (destaquesP2[index] != null) destaquesP2[index].color = cor;
    }

    void SetEngrenagemP1(int index, bool visivel)
    {
        if (engrenagensP1 == null || index < 0 || index >= engrenagensP1.Length) return;
        if (engrenagensP1[index] != null) engrenagensP1[index].gameObject.SetActive(visivel);
    }

    void SetEngrenagemP2(int index, bool visivel)
    {
        if (engrenagensP2 == null || index < 0 || index >= engrenagensP2.Length) return;
        if (engrenagensP2[index] != null) engrenagensP2[index].gameObject.SetActive(visivel);
    }

    void AtualizarVisuaisIA()
    {
        // Engrenagem P1 — só aparece no slot selecionado do P1, se P1 usa IA
        for (int i = 0; i < (engrenagensP1 != null ? engrenagensP1.Length : 0); i++)
            SetEngrenagemP1(i, UsaIAP1() && i == indiceSelecionadoP1);

        // Engrenagem P2 — só aparece no slot selecionado do P2, se P2 usa IA
        for (int i = 0; i < (engrenagensP2 != null ? engrenagensP2.Length : 0); i++)
            SetEngrenagemP2(i, UsaIAP2() && i == indiceSelecionadoP2);
    }

    Color CorHoverDoInspector(Selectable s)
    {
        return s != null ? s.colors.highlightedColor : corHoverBotao;
    }

    // ── Sistema próprio de sprites de Foco/Hover ───────────────────────────
    // Não depende mais do Sprite Swap nativo do Button (spriteState.highlighted/
    // selected), que dependia de o EventSystem ter selecionado de verdade o
    // objeto — e Iniciar/Voltar/Salvar/Fechar nunca recebem seleção real do
    // EventSystem quando o foco é movido pelo nosso teclado customizado, então
    // a Unity nunca sabia qual sprite mostrar sozinha. Agora os PNGs vêm direto
    // dos campos do Inspector (spriteFoco.../spriteHover...) e quem decide qual
    // mostrar é sempre este script, sem ambiguidade:
    //   • Foco  = o teclado/controle está com o destaque aqui (persiste).
    //   • Hover = o MOUSE está fisicamente em cima agora (temporário).
    // Se um dos dois não for preenchido no Inspector, cai no outro como
    // fallback pra nunca sumir o destaque.

    Sprite EscolherSpriteFoco(Sprite foco, Sprite hover, Sprite normal)
    {
        if (foco != null) return foco;
        if (hover != null) return hover;
        return normal;
    }

    Sprite EscolherSpriteHover(Sprite foco, Sprite hover, Sprite normal)
    {
        if (hover != null) return hover;
        if (foco != null) return foco;
        return normal;
    }

    // Pinta o botão com Hover só se o mouse estiver realmente em cima agora
    // (mouseSobreBotaoInferior == true); em qualquer outro caso — ou seja,
    // quando quem trouxe o destaque foi o teclado/controle — pinta com Foco.
    void PintarFocoOuHover(Button botao, Sprite foco, Sprite hover, Sprite normal)
    {
        Sprite escolhido = mouseSobreBotaoInferior
            ? EscolherSpriteHover(foco, hover, normal)
            : EscolherSpriteFoco(foco, hover, normal);
        SetSpriteBotao(botao, escolhido);
    }

    void SetSpriteBotao(Button botao, Sprite sprite)
    {
        if (botao == null || sprite == null) return;
        Image img = botao.GetComponent<Image>();
        if (img != null) img.sprite = sprite;
    }

    void SetCorBotao(Button botao, Color cor)
    {
        if (botao == null) return;
        Image img = botao.GetComponent<Image>();
        if (img != null) img.color = cor;
    }

    void ResetarDestaquesP1()
    {
        if (destaquesP1 != null)
            for (int i = 0; i < destaquesP1.Length; i++)
                SetDestaqueP1(i, corDestaqueInvisivel);

        if (destaqueRandomP1 != null)
            destaqueRandomP1.color = corDestaqueInvisivel;

        if (engrenagensP1 != null)
            for (int i = 0; i < engrenagensP1.Length; i++)
                if (engrenagensP1[i] != null) engrenagensP1[i].gameObject.SetActive(false);
    }

    void ResetarDestaquesP2()
    {
        if (destaquesP2 != null)
            for (int i = 0; i < destaquesP2.Length; i++)
                SetDestaqueP2(i, corDestaqueInvisivel);

        if (destaqueRandomP2 != null)
            destaqueRandomP2.color = corDestaqueInvisivel;

        if (engrenagensP2 != null)
            for (int i = 0; i < engrenagensP2.Length; i++)
                if (engrenagensP2[i] != null) engrenagensP2[i].gameObject.SetActive(false);
    }

    void IniciarJogo()
    {
        if (PainelConfiguracaoIAAberto()) return;

        // Só marca Transicao DEPOIS de confirmar que os personagens necessários estão
        // selecionados — se essa linha rodasse antes das checagens abaixo, um Enter em
        // Iniciar sem personagem selecionado deixava estadoAtual preso em Transicao pra
        // sempre (Update() para de rodar nesse estado), travando teclado/controle até
        // reiniciar a cena.
        if (modoJogo == "PVP")
        {
            if (indiceSelecionadoP1 == -1 || indiceSelecionadoP2 == -1)
            {
                MostrarAviso(Traduzir("AVISO_SELECIONE_AMBOS", "Selecione um personagem para cada jogador antes de iniciar!"));
                return;
            }
        }
        else if (modoJogo == "PVC")
        {
            if (indiceSelecionadoP1 == -1)
            {
                MostrarAviso(Traduzir("AVISO_SELECIONE_P1", "Selecione um personagem para o Player 1 antes de iniciar!"));
                return;
            }

            if (indiceSelecionadoP2 == -1)
            {
                MostrarAviso(Traduzir("AVISO_SELECIONE_CPU", "Selecione o personagem da CPU antes de iniciar!"));
                return;
            }
        }
        else if (modoJogo == "CVC")
        {
            if (indiceSelecionadoP1 == -1 || indiceSelecionadoP2 == -1)
            {
                MostrarAviso(Traduzir("AVISO_SELECIONE_BOTS", "Selecione personagens para os bots antes de iniciar!"));
                return;
            }
        }

        estadoAtual = EstadoTela.Transicao;
        SceneManager.LoadScene("SelecaoArena");
    }

    void Voltar()
    {
        if (PainelConfiguracaoIAAberto()) return;

        estadoAtual = EstadoTela.Transicao;
        SceneManager.LoadScene("ModoJogador");
    }

    private Coroutine rotinaAviso;

    void MostrarAviso(string mensagem)
    {
        if (painelAviso == null || textoAviso == null) return;

        painelAviso.SetActive(true);
        textoAviso.text = mensagem;

        // Para apenas a coroutine do aviso, não a roleta
        if (rotinaAviso != null) StopCoroutine(rotinaAviso);
        rotinaAviso = StartCoroutine(EsconderAvisoDepois(3f));
    }

    IEnumerator EsconderAvisoDepois(float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    void AbrirConfiguracaoIA(int lado, int index)
    {
        if (lado == 1 && !UsaIAP1()) return;
        if (lado == 2 && !UsaIAP2()) return;

        FecharConfiguracaoIAAtiva();

        GameObject painel = lado == 1 ? painelConfigIAP1 : painelConfigIAP2;
        TMP_Dropdown dropdownEstilo = lado == 1 ? dropdownEstiloIAP1 : dropdownEstiloIAP2;
        TMP_Dropdown dropdownDificuldade = lado == 1 ? dropdownDificuldadeIAP1 : dropdownDificuldadeIAP2;

        if (painel == null || dropdownEstilo == null || dropdownDificuldade == null)
        {
            Debug.Log("AbrirConfiguracaoIA: painel ou dropdowns nao atribuidos.");
            return;
        }

        ladoIAConfigAberta = lado;
        indiceIAConfigAberta = index;
        focoIAPanel = 0; // sempre começa no dropdown de estilo
        focoRetornoIA = ObterBotaoPersonagem(lado, index);
        estadoAtual = EstadoTela.ConfiguracaoIA;
        painel.SetActive(true);

        dropdownEstilo.ClearOptions();
        dropdownEstilo.AddOptions(new List<string>
        {
            Traduzir("IA_ESTILO_AGRESSIVO", "Agressivo"),
            Traduzir("IA_ESTILO_EQUILIBRADO", "Equilibrado"),
            Traduzir("IA_ESTILO_DEFENSIVO", "Defensivo")
        });
        dropdownDificuldade.ClearOptions();
        dropdownDificuldade.AddOptions(new List<string>
        {
            Traduzir("IA_DIFIC_FACIL", "Fácil"),
            Traduzir("IA_DIFIC_MEDIO", "Médio"),
            Traduzir("IA_DIFIC_DIFICIL", "Difícil")
        });

        if (lado == 1)
        {
            dropdownEstilo.value = (int)estilosP1[index];
            dropdownDificuldade.value = (int)dificuldadesP1[index];
        }
        else
        {
            dropdownEstilo.value = (int)estilosP2[index];
            dropdownDificuldade.value = (int)dificuldadesP2[index];
        }

        UIFocusUtility.Select(dropdownEstilo.gameObject);
        AtualizarFocoVisualPainelIA();
    }

    void SalvarConfiguracaoIA()
    {
        if (indiceIAConfigAberta < 0 || ladoIAConfigAberta == 0) return;

        int idx = indiceIAConfigAberta;
        TMP_Dropdown dropdownEstilo = ladoIAConfigAberta == 1 ? dropdownEstiloIAP1 : dropdownEstiloIAP2;
        TMP_Dropdown dropdownDificuldade = ladoIAConfigAberta == 1 ? dropdownDificuldadeIAP1 : dropdownDificuldadeIAP2;

        if (dropdownEstilo == null || dropdownDificuldade == null)
            return;

        if (ladoIAConfigAberta == 1)
        {
            estilosP1[idx] = (EstiloIA)dropdownEstilo.value;
            dificuldadesP1[idx] = (DificuldadeIA)dropdownDificuldade.value;
            PlayerPrefs.SetInt($"IA_P1_Estilo_{idx}", (int)estilosP1[idx]);
            PlayerPrefs.SetInt($"IA_P1_Dificuldade_{idx}", (int)dificuldadesP1[idx]);
        }
        else
        {
            estilosP2[idx] = (EstiloIA)dropdownEstilo.value;
            dificuldadesP2[idx] = (DificuldadeIA)dropdownDificuldade.value;
            PlayerPrefs.SetInt($"IA_P2_Estilo_{idx}", (int)estilosP2[idx]);
            PlayerPrefs.SetInt($"IA_P2_Dificuldade_{idx}", (int)dificuldadesP2[idx]);
        }

        PlayerPrefs.Save();
        FecharConfiguracaoIA();
    }

    void FecharConfiguracaoIA()
    {
        // Fecha apenas o painel ativo — não fecha os dois ao mesmo tempo
        FecharConfiguracaoIAAtiva();
    }

    void FecharConfiguracaoIAAtiva()
    {
        // Fecha a lista suspensa de qualquer um dos dois dropdowns antes de desativar
        // o painel — evita lista aberta "fantasma" reaparecer na próxima vez que o
        // painel for aberto de novo.
        TMP_Dropdown dropEstiloAtivo = ladoIAConfigAberta == 1 ? dropdownEstiloIAP1 : dropdownEstiloIAP2;
        TMP_Dropdown dropDificAtivo = ladoIAConfigAberta == 1 ? dropdownDificuldadeIAP1 : dropdownDificuldadeIAP2;
        if (dropEstiloAtivo != null) dropEstiloAtivo.Hide();
        if (dropDificAtivo != null) dropDificAtivo.Hide();

        // Desativar o painel já é suficiente — a seleção do EventSystem que estava
        // em cima do dropdown/botão dentro dele deixa de existir junto (o objeto some),
        // e ClearSelection() abaixo garante que nada fica "preso" selecionado.
        if (ladoIAConfigAberta == 1)
        {
            if (painelConfigIAP1 != null) painelConfigIAP1.SetActive(false);
        }
        else if (ladoIAConfigAberta == 2)
        {
            if (painelConfigIAP2 != null) painelConfigIAP2.SetActive(false);
        }

        UIFocusUtility.ClearSelection();

        ladoIAConfigAberta = 0;
        indiceIAConfigAberta = -1;
        estadoAtual = EstadoTela.SelecionandoPersonagens;

        // Mesmo motivo do Start(): NÃO selecionamos de verdade o botão de personagem
        // (focoRetornoIA) no EventSystem — ele voltaria a ficar vulnerável ao onClick
        // nativo da Unity disparado pelo Enter. A grade recalcula o destaque visual
        // sozinha por focoP1/focoP2, que já continuam apontando pro personagem certo.
        focoRetornoIA = null;
        AtualizarFocoGrupo();
    }

    GameObject ObterBotaoPersonagem(int lado, int indice)
    {
        if (indice < 0)
            return null;

        if (lado == 1 && botoesPlayer1 != null && indice < botoesPlayer1.Length && botoesPlayer1[indice] != null)
            return botoesPlayer1[indice].gameObject;

        if (lado == 2 && botoesPlayer2 != null && indice < botoesPlayer2.Length && botoesPlayer2[indice] != null)
            return botoesPlayer2[indice].gameObject;

        return null;
    }
    IEnumerator EsconderAvisoDeselecao()
    {
        yield return new WaitForSeconds(3.5f);

        if (avisoP1 != null) avisoP1.gameObject.SetActive(false);
        if (avisoP2 != null) avisoP2.gameObject.SetActive(false);
    }
}