using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Primeiro layout visual da seleção refatorada.
///
/// O bloco é criado com nomes claros dentro do Canvas, como o modal de
/// informações. Isso deixa a composição fácil de localizar na Hierarchy e os
/// principais tamanhos/posições ficam expostos no Inspector para a próxima
/// rodada de ajuste visual.
/// </summary>
[ExecuteAlways]
public sealed class TelaSelecaoPlayerLayout : MonoBehaviour
{
    [Header("Composição")]
    [SerializeField] private Vector2 tamanhoReferencia = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 margemHorizontal = new Vector2(55f, 55f);
    [SerializeField] private float alturaCartoes = 560f;
    [SerializeField] private float larguraCartao = 740f;
    [SerializeField] private float alturaRoster = 205f;
    [SerializeField] private float larguraSlotRoster = 225f;
    [SerializeField] private float espacamentoRoster = 18f;
    [SerializeField] private int quantidadeSlotsRoster = 4;
    [SerializeField] private float alturaRodape = 105f;
    [Tooltip("Elevação da moldura vertical quando o lado não usa BOT.")]
    [SerializeField] private float elevacaoFundoAlternativo = 70f;

    [Header("Fundos pixel-art — arraste no Inspector")]
    [Tooltip("Arte do cartão vermelho, sem personagem nem texto.")]
    [SerializeField] private Sprite fundoCartaoP1;
    [Tooltip("Arte do cartão azul, sem personagem nem texto.")]
    [SerializeField] private Sprite fundoCartaoP2;
    [Tooltip("Arte alternativa do cartão P1 quando o lado é jogador humano, sem BOT.")]
    [SerializeField] private Sprite fundoCartaoP1Alternativo;
    [Tooltip("Arte alternativa do cartão P2 quando o lado é jogador humano, sem BOT.")]
    [SerializeField] private Sprite fundoCartaoP2Alternativo;
    [Header("Botões de informação")]
    [Tooltip("PNG único do botão de informação do Player 1.")]
    [SerializeField] private Sprite spriteInfoP1;
    [Tooltip("PNG único do botão de informação do Player 2.")]
    [SerializeField] private Sprite spriteInfoP2;
    [Tooltip("Faixa vermelha do título, sem texto embutido.")]
    [SerializeField] private Sprite fundoTitulo;
    [Tooltip("Fundo horizontal do rodapé. Os badges e textos ficam separados.")]
    [SerializeField] private Sprite fundoRodape;
    [Tooltip("Moldura reutilizável de cada slot de personagem desbloqueado.")]
    [SerializeField] private Sprite fundoSlotPersonagem;
    [Tooltip("Borda vermelha usada pelo foco/seleção do jogador 1.")]
    [SerializeField] private Sprite spriteBordaP1;
    [Tooltip("Borda azul usada pelo foco/seleção do jogador 2.")]
    [SerializeField] private Sprite spriteBordaP2;
    [Tooltip("Moldura reutilizável de cada slot bloqueado.")]
    [SerializeField] private Sprite fundoSlotBloqueado;
    [Tooltip("Ícone separado que aparece dentro dos slots bloqueados.")]
    [SerializeField] private Sprite iconeCadeado;
    [Tooltip("Estado normal do voltar, normalizado no mesmo canvas do estado selecionado.")]
    [SerializeField] private Sprite spriteVoltarNormalizado;
    [Tooltip("Estado focado do voltar, com a borda azul.")]
    [SerializeField] private Sprite spriteVoltarSelecionado;
    [Tooltip("Recorte normalizado do PNG do rodapé. Mantém a moldura responsiva sem carregar a margem transparente do asset.")]
    [SerializeField] private Rect recorteRodapeNormalizado = new Rect(0f, 0.075f, 1f, 0.4f);

    [Header("Cores")]
    [SerializeField] private Color corFundo = new Color(0.025f, 0.055f, 0.12f, 0.96f);
    [SerializeField] private Color corP1 = new Color(0.72f, 0.05f, 0.07f, 0.92f);
    [SerializeField] private Color corP2 = new Color(0.03f, 0.28f, 0.78f, 0.92f);
    [SerializeField] private Color corPainel = new Color(0.015f, 0.04f, 0.09f, 0.9f);
    [SerializeField] private Color corTexto = Color.white;
    [SerializeField] private Color corDestaque = new Color(1f, 0.82f, 0.18f, 1f);

    [Header("Referências criadas")]
    [SerializeField] private RectTransform layoutRoot;
    [SerializeField] private RectTransform painelP1;
    [SerializeField] private RectTransform painelP2;
    [SerializeField] private RectTransform roster;
    [SerializeField] private TextMeshProUGUI textoInstrucaoEnter;

    private TelaSelecaoPlayer tela;
    private Image imagemP1;
    private Image imagemP2;
    private TextMeshProUGUI nomeP1;
    private TextMeshProUGUI nomeP2;
    private TMP_Dropdown estiloP1;
    private TMP_Dropdown dificuldadeP1;
    private TMP_Dropdown estiloP2;
    private TMP_Dropdown dificuldadeP2;
    private Sprite spriteDropdownP1Original;
    private Sprite spriteDropdownP2Original;
    private SpriteState estadoDropdownP1Original;
    private SpriteState estadoDropdownP2Original;
    private Button botaoInfoOriginalP1;
    private Button botaoInfoOriginalP2;
    private BotaoInfoFocoVisual visualInfoP1;
    private BotaoInfoFocoVisual visualInfoP2;
    private Image fundoAlternativoP1;
    private Image fundoAlternativoP2;

    private struct ComposicaoCartaoOriginal
    {
        public bool capturada;
        public Vector2 posicaoRetrato;
        public Vector2 tamanhoRetrato;
        public Vector2 posicaoNome;
        public Vector2 tamanhoNome;
        public TextAlignmentOptions alinhamentoNome;
        public Vector2 posicaoRotulo;
        public Vector2 tamanhoRotulo;
        public TextAlignmentOptions alinhamentoRotulo;
        public Vector2 posicaoInfo;
        public Vector2 tamanhoInfo;
    }

    private ComposicaoCartaoOriginal composicaoOriginalP1;
    private ComposicaoCartaoOriginal composicaoOriginalP2;
    private int ladoAtivo = 1;
    private bool inicializado;
    private readonly List<Button> botoesRoster = new List<Button>();
    private readonly List<Outline> destaquesRoster = new List<Outline>();
    private readonly List<int> indicesRoster = new List<int>();
    private readonly List<Image> bordasRosterP1 = new List<Image>();
    private readonly List<Image> bordasRosterP2 = new List<Image>();
    private readonly List<int> indicesBordasRoster = new List<int>();
    private Outline focoCardP1;
    private Outline focoCardP2;
    private readonly Dictionary<TMP_Dropdown, Sprite> spritesNormaisDropdown =
        new Dictionary<TMP_Dropdown, Sprite>();
    private int categoriaFocoP1 = 4;
    private int categoriaFocoP2 = 4;
    private int indiceFocoP1;
    private int indiceFocoP2 = 1;
    private bool idiomaInscrito;
    private readonly Dictionary<string, TextMeshProUGUI> textosTeclasRodape =
        new Dictionary<string, TextMeshProUGUI>();
    private readonly Dictionary<string, TextMeshProUGUI> legendasRodape =
        new Dictionary<string, TextMeshProUGUI>();
    private bool rodapeJaDetectouEntrada;
    private bool rodapeUsandoControle = true;
    private bool eixoControleEstavaAtivo;
    private static readonly KeyCode[] TeclasConhecidasRodape =
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
        KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.Return, KeyCode.KeypadEnter,
        KeyCode.Escape, KeyCode.F, KeyCode.G, KeyCode.H,
        KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.X
    };

    private void InscreverIdioma()
    {
        if (idiomaInscrito)
            return;

        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
        idiomaInscrito = true;
    }

    private void DesinscreverIdioma()
    {
        if (!idiomaInscrito)
            return;

        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
        idiomaInscrito = false;
    }

    private void Awake()
    {
        InscreverIdioma();
    }

#if UNITY_EDITOR
    private bool aguardandoInicializacaoNoEditor;

    private void OnEnable()
    {
        InscreverIdioma();
        if (Application.isPlaying || aguardandoInicializacaoNoEditor)
            return;

        aguardandoInicializacaoNoEditor = true;
        EditorApplication.delayCall += InicializarNoEditor;
    }

    private void OnDisable()
    {
        DesinscreverIdioma();
        EditorApplication.delayCall -= InicializarNoEditor;
        aguardandoInicializacaoNoEditor = false;
    }

    private void InicializarNoEditor()
    {
        aguardandoInicializacaoNoEditor = false;
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
            return;

        TelaSelecaoPlayer origem = GetComponent<TelaSelecaoPlayer>();
        if (origem == null)
            return;

        Inicializar(origem);
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void OnDestroy()
    {
        DesinscreverIdioma();
    }

    public void Inicializar(TelaSelecaoPlayer origem)
    {
        if (inicializado && tela == origem)
        {
            // Com o Domain Reload desligado, o Unity pode reutilizar esta
            // instância entre execuções. Revalida os textos antes do Play.
            tela.PrepararDropdownIAPublicamente(1);
            tela.PrepararDropdownIAPublicamente(2);
            PrepararAparenciaDosDropdowns();
            AplicarFundosDosCartoesPorModo();
            ReconectarIndicadoresRodape();
            AplicarTextosRodape(rodapeUsandoControle);
            return;
        }

        tela = origem;
        inicializado = true;
        GarantirSpritesDeBorda();
        CapturarVisuaisOriginaisDosDropdowns();
        CapturarBotoesInfoOriginais();
        ConstruirHierarquia();
        OcultarRestosDaVersaoAntiga();
        ReconectarInteracoesDaHierarquia();
        // A hierarquia pode já existir na cena; nesse caso CriarCartao não é
        // executado e os dropdowns precisam receber as opções novamente.
        tela.PrepararDropdownIAPublicamente(1);
        tela.PrepararDropdownIAPublicamente(2);
        PrepararAparenciaDosDropdowns();
        CapturarComposicaoOriginalDosCartoes();
        AplicarFundosDosCartoesPorModo();
        AplicarTextosRodape(rodapeUsandoControle);
        AtualizarVisual();
    }

    private void ReconectarInteracoesDaHierarquia()
    {
        if (layoutRoot == null || tela == null)
            return;

        painelP1 = layoutRoot.Find("PainelP1") as RectTransform;
        painelP2 = layoutRoot.Find("PainelP2") as RectTransform;
        roster = layoutRoot.Find("RosterPersonagens") as RectTransform;
        textoInstrucaoEnter = layoutRoot.Find("InstrucaoEnter")?.GetComponent<TextMeshProUGUI>();
        ReconectarIndicadoresRodape();

        if (painelP1 != null)
        {
            imagemP1 = painelP1.Find("RetratoP1")?.GetComponent<Image>();
            nomeP1 = painelP1.Find("NomeP1")?.GetComponent<TextMeshProUGUI>();
            estiloP1 = painelP1.Find("EstiloBotP1")?.GetComponent<TMP_Dropdown>();
            dificuldadeP1 = painelP1.Find("DificuldadeBotP1")?.GetComponent<TMP_Dropdown>();
            VincularBotaoDoLado(painelP1, 1);
            VincularBotaoInfo(painelP1, 1);
        }

        if (painelP2 != null)
        {
            imagemP2 = painelP2.Find("RetratoP2")?.GetComponent<Image>();
            nomeP2 = painelP2.Find("NomeP2")?.GetComponent<TextMeshProUGUI>();
            estiloP2 = painelP2.Find("EstiloBotP2")?.GetComponent<TMP_Dropdown>();
            dificuldadeP2 = painelP2.Find("DificuldadeBotP2")?.GetComponent<TMP_Dropdown>();
            VincularBotaoDoLado(painelP2, 2);
            VincularBotaoInfo(painelP2, 2);
        }

        focoCardP1 = painelP1 != null ? painelP1.GetComponent<Outline>() : null;
        focoCardP2 = painelP2 != null ? painelP2.GetComponent<Outline>() : null;

        AdicionarListenerConfiguracao(estiloP1, dificuldadeP1, 1);
        AdicionarListenerConfiguracao(estiloP2, dificuldadeP2, 2);

        Button voltar = layoutRoot.Find("BotaoVoltarSelecao")?.GetComponent<Button>();
        if (voltar != null)
        {
            voltar.onClick.RemoveAllListeners();
            voltar.onClick.AddListener(() => tela.SendMessage("Voltar", SendMessageOptions.DontRequireReceiver));
            ConfigurarVisualVoltar(voltar);
        }

        botoesRoster.Clear();
        destaquesRoster.Clear();
        indicesRoster.Clear();
        bordasRosterP1.Clear();
        bordasRosterP2.Clear();
        indicesBordasRoster.Clear();
        if (roster != null)
        {
            foreach (Button slot in roster.GetComponentsInChildren<Button>(true))
            {
                if (slot == null || !slot.gameObject.name.StartsWith("SlotPersonagem"))
                    continue;
                if (slot.gameObject.name.StartsWith("SlotPersonagemBloqueado"))
                    continue;

                string sufixo = slot.gameObject.name.Substring("SlotPersonagem".Length);
                int indice;
                if (!int.TryParse(sufixo, out indice))
                    continue;

                slot.onClick.RemoveAllListeners();
                int indiceCapturado = indice;
                slot.onClick.AddListener(() =>
                {
                    tela.SelecionarPersonagemPublicamente(ladoAtivo, indiceCapturado);
                    if (tela.ObterIndiceSelecionadoPublicamente(1) >= 0 &&
                        tela.ObterIndiceSelecionadoPublicamente(2) < 0)
                        ladoAtivo = 2;
                    AtualizarVisual();
                });
                botoesRoster.Add(slot);

                Image moldura = slot.transform.Find("FundoAzulSlot")?.GetComponent<Image>();
                Outline destaque = moldura != null ? moldura.GetComponent<Outline>() : null;
                if (destaque != null)
                {
                    destaquesRoster.Add(destaque);
                    indicesRoster.Add(indice);
                }

                ConfigurarBordasDoSlot(slot.transform, indice);
            }
        }
    }

    private void VincularBotaoDoLado(Transform painel, int lado)
    {
        Button botao = painel.Find("SelecionarLadoP" + lado)?.GetComponent<Button>();
        if (botao == null)
            return;

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => ladoAtivo = lado);
    }

    private void VincularBotaoInfo(Transform painel, int lado)
    {
        Button botao = null;
        foreach (Button candidato in painel.GetComponentsInChildren<Button>(true))
        {
            if (candidato == null || !candidato.gameObject.name.StartsWith("InfoI"))
                continue;

            if (botao == null)
            {
                botao = candidato;
                continue;
            }

            // O cartão deve ter somente um botão de informação. Qualquer
            // cópia antiga que tenha ficado na Hierarchy não pode continuar
            // visível por cima do PNG novo.
            candidato.gameObject.SetActive(false);
        }

        if (botao == null)
            return;

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => tela.AbrirInfoPublicamente(lado));
        AplicarSpriteBotaoInfo(botao, lado);
    }

    private void CapturarBotoesInfoOriginais()
    {
        Transform raizInterface = ObterRaizInterface();
        Transform infoP1 = raizInterface.Find("BotaoInfoP1");
        Transform infoP2 = raizInterface.Find("BotaoInfoP2");
        if (infoP1 == null || infoP2 == null)
        {
            foreach (Button botao in GetComponentsInChildren<Button>(true))
            {
                if (botao == null) continue;
                if (botao.gameObject.name == "BotaoInfoP1") infoP1 = botao.transform;
                if (botao.gameObject.name == "BotaoInfoP2") infoP2 = botao.transform;
            }
        }
        botaoInfoOriginalP1 = infoP1 != null ? infoP1.GetComponent<Button>() : null;
        botaoInfoOriginalP2 = infoP2 != null ? infoP2.GetComponent<Button>() : null;
    }

    private void CapturarVisuaisOriginaisDosDropdowns()
    {
        TMP_Dropdown dropdownP1 = tela.ObterDropdownEstiloPublicamente(1);
        TMP_Dropdown dropdownP2 = tela.ObterDropdownEstiloPublicamente(2);

        if (dropdownP1 != null)
        {
            Image imagem = dropdownP1.GetComponent<Image>();
            spriteDropdownP1Original = imagem != null ? imagem.sprite : null;
            estadoDropdownP1Original = dropdownP1.spriteState;
        }

        if (dropdownP2 != null)
        {
            Image imagem = dropdownP2.GetComponent<Image>();
            spriteDropdownP2Original = imagem != null ? imagem.sprite : null;
            estadoDropdownP2Original = dropdownP2.spriteState;
        }
    }

    private void PrepararAparenciaDosDropdowns()
    {
        PrepararAparenciaDropdown(estiloP1);
        PrepararAparenciaDropdown(dificuldadeP1);
        PrepararAparenciaDropdown(estiloP2);
        PrepararAparenciaDropdown(dificuldadeP2);
    }

    private void PrepararAparenciaDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        // O Label do estilo do P1 ficou desativado na cena antiga. Reativa a
        // legenda e o texto dos itens para que o valor escolhido nunca fique
        // invisível.
        if (dropdown.captionText != null)
        {
            dropdown.captionText.gameObject.SetActive(true);
            dropdown.captionText.color = Color.white;
        }

        if (dropdown.itemText != null)
        {
            dropdown.itemText.gameObject.SetActive(true);
            dropdown.itemText.color = Color.black;
        }

        RectTransform template = dropdown.template;
        if (template == null)
            return;

        Image fundoTemplate = template.GetComponent<Image>();
        if (fundoTemplate != null)
            fundoTemplate.color = Color.white;

        // O template padrão tem uma imagem cinza no Viewport e outra em cada
        // linha. Deixa o popup branco, mantendo a cor vermelha/azul apenas
        // no botão fechado do respectivo jogador.
        Image[] imagens = template.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imagens.Length; i++)
        {
            Image imagem = imagens[i];
            if (imagem == null)
                continue;

            string nome = imagem.gameObject.name;
            if (nome == "Viewport" || nome == "Item Background")
                imagem.color = Color.white;
        }

        dropdown.RefreshShownValue();
    }

    private void ConstruirHierarquia()
    {
        if (layoutRoot != null)
            return;

        Sprite spriteFundo = EncontrarSpriteDeFundo();
        OcultarInterfaceAntiga();

        GameObject root = new GameObject("SelecaoPlayerRefatorada", typeof(RectTransform));
        layoutRoot = root.GetComponent<RectTransform>();
        layoutRoot.SetParent(ObterRaizInterface(), false);
        layoutRoot.anchorMin = Vector2.zero;
        layoutRoot.anchorMax = Vector2.one;
        layoutRoot.offsetMin = Vector2.zero;
        layoutRoot.offsetMax = Vector2.zero;
        layoutRoot.SetAsLastSibling();

        RectTransform fundo = CriarPainel("FundoSelecao", layoutRoot, corFundo);
        if (spriteFundo != null)
        {
            Image imagemFundo = fundo.GetComponent<Image>();
            imagemFundo.sprite = spriteFundo;
            imagemFundo.color = Color.white;
            imagemFundo.preserveAspect = false;
        }

        RectTransform faixaTitulo = CriarPainel("FaixaTituloSelecao", layoutRoot, Color.white);
        Image imagemTitulo = faixaTitulo.GetComponent<Image>();
        imagemTitulo.sprite = fundoTitulo;
        imagemTitulo.color = imagemTitulo.sprite != null ? Color.white : corP1;
        imagemTitulo.type = Image.Type.Simple;
        imagemTitulo.raycastTarget = false;
        Posicionar(faixaTitulo, new Vector2(0f, 480f), new Vector2(960f, 86f));

        TextMeshProUGUI titulo = CriarTexto("TituloSelecao", faixaTitulo,
            Traduzir("SELECT_PLAYER", "SELEÇÃO DE PERSONAGENS"), 54f, corTexto);
        Posicionar(titulo.rectTransform, Vector2.zero, new Vector2(900f, 70f));

        painelP1 = CriarCartao("PainelP1", new Vector2(-495f, 125f), corP1,
            "P1", 1, out imagemP1, out nomeP1, out estiloP1, out dificuldadeP1);
        painelP2 = CriarCartao("PainelP2", new Vector2(495f, 125f), corP2,
            "P2", 2, out imagemP2, out nomeP2, out estiloP2, out dificuldadeP2);

        CriarRoster();
        CriarRodape();

        textoInstrucaoEnter = CriarTexto("InstrucaoEnter", layoutRoot,
            Traduzir("SELECT_PRESS_ENTER", "SELECIONE OS DOIS JOGADORES E PRESSIONE ENTER PARA CONTINUAR"),
            23f, corTexto);
        Posicionar(textoInstrucaoEnter.rectTransform, new Vector2(0f, -390f), new Vector2(1500f, 54f));

        Button voltar = CriarBotaoVoltar("BotaoVoltarSelecao", layoutRoot);
        Posicionar(voltar.GetComponent<RectTransform>(), new Vector2(-875f, 470f), new Vector2(130f, 82f));
        voltar.onClick.AddListener(() =>
        {
            tela.SendMessage("Voltar", SendMessageOptions.DontRequireReceiver);
        });

        ReparentarDropdowns();

        // A faixa fica acima do fundo e dos cartões, mas o texto continua
        // separado e editável como filho dela.
        faixaTitulo.SetAsLastSibling();
        titulo.transform.SetAsLastSibling();
    }

    private RectTransform CriarCartao(string nome, Vector2 posicao, Color cor, string rotulo,
        int lado, out Image imagem, out TextMeshProUGUI nomePersonagem,
        out TMP_Dropdown dropdownEstilo, out TMP_Dropdown dropdownDificuldade)
    {
        RectTransform cartao = CriarPainel(nome, layoutRoot, cor);
        Posicionar(cartao, posicao, new Vector2(larguraCartao, alturaCartoes));
        Image fundo = cartao.GetComponent<Image>();
        fundo.sprite = lado == 1 ? fundoCartaoP1 : fundoCartaoP2;
        fundo.color = fundo.sprite != null ? Color.white : cor;
        fundo.preserveAspect = false;

        Outline foco = cartao.gameObject.GetComponent<Outline>();
        if (foco == null)
            foco = cartao.gameObject.AddComponent<Outline>();
        foco.effectColor = lado == 1
            ? new Color(1f, 0.08f, 0.12f, 0f)
            : new Color(0.08f, 0.48f, 1f, 0f);
        foco.effectDistance = new Vector2(9f, 9f);
        if (lado == 1) focoCardP1 = foco;
        else focoCardP2 = foco;

        TextMeshProUGUI player = CriarTexto("Rotulo" + rotulo, cartao,
            rotulo, 42f, corTexto);
        float espelho = lado == 1 ? -1f : 1f;
        Posicionar(player.rectTransform, new Vector2(espelho * 270f, 205f), new Vector2(120f, 48f));

        Button selecionarLado = CriarAreaClique("SelecionarLado" + rotulo, cartao);
        Posicionar(selecionarLado.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(larguraCartao - 18f, alturaCartoes - 18f));
        selecionarLado.onClick.AddListener(() => ladoAtivo = lado);

        imagem = CriarImagem("Retrato" + rotulo, cartao);
        imagem.preserveAspect = true;
        Posicionar(imagem.rectTransform, new Vector2(espelho * 170f, -10f), new Vector2(330f, 390f));

        nomePersonagem = CriarTexto("Nome" + rotulo, cartao, "—", 32f, corTexto);
        Posicionar(nomePersonagem.rectTransform, new Vector2(espelho * 170f, -225f), new Vector2(330f, 52f));

        float coluna = -espelho * 185f;

        TextMeshProUGUI tituloBot = CriarTexto("TituloBot" + rotulo,
            cartao, Traduzir("TXT_DIFICUL", "DIFICULDADE (BOT)"), 25f, corTexto);
        Posicionar(tituloBot.rectTransform, new Vector2(coluna, 142f), new Vector2(330f, 44f));

        dropdownEstilo = lado == 1 ? tela.ObterDropdownEstiloPublicamente(1) : tela.ObterDropdownEstiloPublicamente(2);
        dropdownDificuldade = lado == 1 ? tela.ObterDropdownDificuldadePublicamente(1) : tela.ObterDropdownDificuldadePublicamente(2);
        tela.PrepararDropdownIAPublicamente(lado);

        ConfigurarDropdownVisual(dropdownDificuldade, cartao, "DificuldadeBot" + rotulo, new Vector2(coluna, 82f), lado);

        TextMeshProUGUI estilo = CriarTexto("LegendaEstilo" + rotulo,
            cartao, Traduzir("TXT_ESTILO", "ESTILO DO BOT"), 16f, corTexto);
        Posicionar(estilo.rectTransform, new Vector2(coluna, 38f), new Vector2(330f, 38f));
        ConfigurarDropdownVisual(dropdownEstilo, cartao, "EstiloBot" + rotulo, new Vector2(coluna, -2f), lado);

        TextMeshProUGUI explicacao = CriarTexto("ExplicacaoBot" + rotulo, cartao,
            Traduzir("BOT_OPTIONS_HINT", "ESCOLHA O ESTILO E A DIFICULDADE DO SEU BOT."),
            18f, corTexto);
        explicacao.enableWordWrapping = true;
        explicacao.overflowMode = TextOverflowModes.Overflow;
        Posicionar(explicacao.rectTransform, new Vector2(coluna, -112f), new Vector2(300f, 86f));

        Button info = PrepararBotaoInfo(lado, rotulo, cartao);
        if (info != null)
        {
            // O indicador I fica no canto superior oposto ao rótulo P1/P2,
            // como nos fundos dos cartões da seleção.
            Posicionar(info.GetComponent<RectTransform>(), new Vector2(-espelho * 270f, 205f), new Vector2(150f, 56f));
            info.onClick.AddListener(() => tela.AbrirInfoPublicamente(lado));
        }

        AdicionarListenerConfiguracao(dropdownEstilo, dropdownDificuldade, lado);
        return cartao;
    }

    private Button PrepararBotaoInfo(int lado, string rotulo, Transform cartao)
    {
        Button original = lado == 1 ? botaoInfoOriginalP1 : botaoInfoOriginalP2;
        if (original == null)
        {
            // A tela nova só usa os botões I que já existem na Hierarchy.
            // Não recriar os botões antigos como fallback.
            return null;
        }

        original.gameObject.name = "InfoI" + rotulo;
        original.transform.SetParent(cartao, false);
        original.onClick.RemoveAllListeners();
        original.interactable = true;

        CanvasGroup grupo = original.GetComponent<CanvasGroup>();
        if (grupo != null)
        {
            grupo.alpha = 1f;
            grupo.interactable = true;
            grupo.blocksRaycasts = true;
        }

        AplicarSpriteBotaoInfo(original, lado);

        return original;
    }

    private void AplicarSpriteBotaoInfo(Button botao, int lado)
    {
        if (botao == null)
            return;

        Image imagem = botao.GetComponent<Image>();
        if (imagem == null)
            return;

        Sprite sprite = lado == 1 ? spriteInfoP1 : spriteInfoP2;
        if (sprite != null)
        {
            imagem.sprite = sprite;
            imagem.type = Image.Type.Simple;
            imagem.preserveAspect = true;
        }

        imagem.color = Color.white;
        imagem.raycastTarget = true;

        // O PNG já contém todo o desenho do botão. Desativa apenas os gráficos
        // antigos que ficavam como subimagem/texto dentro do botão.
        Graphic[] filhos = botao.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] != null && filhos[i].gameObject != imagem.gameObject)
                filhos[i].gameObject.SetActive(false);
        }

        BotaoInfoFocoVisual visual = botao.GetComponent<BotaoInfoFocoVisual>();
        if (visual == null)
            visual = botao.gameObject.AddComponent<BotaoInfoFocoVisual>();
        visual.Configurar(imagem, imagem.sprite, imagem.sprite, lado == 1 ? corP1 : corP2);

        if (lado == 1)
            visualInfoP1 = visual;
        else
            visualInfoP2 = visual;
    }

    private void CriarRoster()
    {
        // Este objeto é apenas um agrupador. Não existe mais uma barra única com
        // quatro slots desenhados dentro dela: cada quadrado é independente para
        // que novos personagens possam ser acrescentados sem refazer uma imagem.
        roster = CriarPainel("RosterPersonagens", layoutRoot, new Color(1f, 1f, 1f, 0f));
        Posicionar(roster, new Vector2(0f, -245f), new Vector2(1350f, alturaRoster));

        int totalSlots = Mathf.Max(4, quantidadeSlotsRoster);
        float larguraTotal = totalSlots * larguraSlotRoster + (totalSlots - 1) * espacamentoRoster;
        for (int slotIndex = 0; slotIndex < totalSlots; slotIndex++)
        {
            bool desbloqueado = tela.PersonagemValidoPublicamente(slotIndex);
            float x = -larguraTotal * 0.5f + larguraSlotRoster * 0.5f +
                      slotIndex * (larguraSlotRoster + espacamentoRoster);

            if (desbloqueado)
                CriarSlotPersonagem(roster, slotIndex, x);
            else
                CriarSlotBloqueado(roster, slotIndex, x);
        }
    }

    private void CriarSlotPersonagem(Transform pai, int indice, float x)
    {
        GameObject objeto = new GameObject("SlotPersonagem" + indice,
            typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        Button slot = objeto.GetComponent<Button>();
        Image areaClique = objeto.GetComponent<Image>();
        areaClique.color = new Color(1f, 1f, 1f, 0f);
        areaClique.raycastTarget = true;
        slot.targetGraphic = areaClique;
        slot.transition = Selectable.Transition.None;
        // A moldura azul fica em uma camada independente do botão e do rosto.
        // Assim o personagem pode crescer ou ser trocado sem depender de uma
        // arte única com o roster inteiro desenhado dentro dela.
        Image moldura = CriarImagem("FundoAzulSlot", objeto.transform);
        moldura.sprite = fundoSlotPersonagem;
        moldura.color = moldura.sprite != null ? Color.white : new Color(0.08f, 0.35f, 0.9f, 1f);
        moldura.preserveAspect = false;
        moldura.raycastTarget = false;
        moldura.transform.SetAsFirstSibling();
        Posicionar(moldura.rectTransform, Vector2.zero,
            new Vector2(larguraSlotRoster, alturaRoster));
        Outline destaque = moldura.gameObject.AddComponent<Outline>();
        destaque.effectColor = new Color(0.15f, 0.75f, 1f, 0f);
        destaque.effectDistance = new Vector2(7f, 7f);
        destaquesRoster.Add(destaque);
        indicesRoster.Add(indice);
        Posicionar(objeto.GetComponent<RectTransform>(), new Vector2(x, 0f),
            new Vector2(larguraSlotRoster, alturaRoster));

        Image retrato = CriarImagem("RostoSlot" + indice, objeto.transform);
        retrato.sprite = tela.ObterRostoPersonagemPublicamente(1, indice) ??
                         tela.ObterRostoPersonagemPublicamente(2, indice);
        retrato.preserveAspect = true;
        Posicionar(retrato.rectTransform, new Vector2(0f, 15f), new Vector2(150f, 135f));

        TextMeshProUGUI legenda = CriarTexto("NomeSlot" + indice, objeto.transform,
            tela.ObterNomePersonagemPublicamente(indice), 17f, corTexto);
        Posicionar(legenda.rectTransform, new Vector2(0f, -78f), new Vector2(190f, 28f));

        int indiceCapturado = indice;
        slot.onClick.AddListener(() =>
        {
            tela.SelecionarPersonagemPublicamente(ladoAtivo, indiceCapturado);
            // Depois de escolher o primeiro lado, o próximo clique no roster
            // fica naturalmente pronto para o segundo. O cartão ainda pode
            // trocar o lado ativo manualmente.
            if (tela.ObterIndiceSelecionadoPublicamente(1) >= 0 &&
                tela.ObterIndiceSelecionadoPublicamente(2) < 0)
                ladoAtivo = 2;
            AtualizarVisual();
        });
        botoesRoster.Add(slot);
    }

    private void CriarSlotBloqueado(Transform pai, int indice, float x)
    {
        GameObject objeto = new GameObject("SlotPersonagemBloqueado" + indice,
            typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(pai, false);
        Image moldura = objeto.GetComponent<Image>();
        moldura.sprite = fundoSlotBloqueado;
        moldura.color = moldura.sprite != null ? Color.white : new Color(0.3f, 0.32f, 0.38f, 1f);
        moldura.preserveAspect = false;
        moldura.raycastTarget = false;
        Posicionar(objeto.GetComponent<RectTransform>(), new Vector2(x, 0f),
            new Vector2(larguraSlotRoster, alturaRoster));

        Image cadeado = CriarImagem("Cadeado", objeto.transform);
        cadeado.sprite = iconeCadeado;
        cadeado.color = cadeado.sprite != null ? Color.white : Color.gray;
        cadeado.preserveAspect = true;
        Posicionar(cadeado.rectTransform, new Vector2(0f, 8f), new Vector2(64f, 72f));

        // Slots bloqueados não participam da rota e não exibem borda de foco.
        foreach (Image borda in objeto.GetComponentsInChildren<Image>(true))
        {
            if (borda != null && borda.gameObject.name.Equals("border", System.StringComparison.OrdinalIgnoreCase))
            {
                Color cor = borda.color;
                cor.a = 0f;
                borda.color = cor;
                borda.raycastTarget = false;
            }
        }
    }

    private void CriarRodape()
    {
        RectTransform rodape = CriarPainel("RodapeControles", layoutRoot, new Color(0.01f, 0.03f, 0.07f, 0.96f));
        Image imagem = rodape.GetComponent<Image>();
        imagem.sprite = CriarSpriteRecortado(fundoRodape, recorteRodapeNormalizado);
        imagem.color = imagem.sprite != null ? Color.white : new Color(0.01f, 0.03f, 0.07f, 0.96f);
        imagem.preserveAspect = false;
        Posicionar(rodape, new Vector2(0f, -445f), new Vector2(1920f, alturaRodape));

        CriarIndicadorControle(rodape, "A", Traduzir("SELECT_CONFIRM", "CONFIRMAR"),
            new Color(0.05f, 0.82f, 0.43f, 1f), new Vector2(-655f, -49f));
        CriarIndicadorControle(rodape, "B", Traduzir("SELECT_BACK", "VOLTAR"),
            new Color(0.93f, 0.08f, 0.12f, 1f), new Vector2(-206.18312f, -49f));
        CriarIndicadorControle(rodape, "Y", Traduzir("SELECT_DETAILS", "DETALHES"),
            new Color(1f, 0.78f, 0.02f, 1f), new Vector2(242.63289f, -49f));
        CriarIndicadorControle(rodape, "X", Traduzir("SELECT_NAVIGATE", "NAVEGAR"),
            new Color(0.04f, 0.43f, 1f, 1f), new Vector2(691.44995f, -49f));
    }

    private Sprite CriarSpriteRecortado(Sprite origem, Rect recorteNormalizado)
    {
        if (origem == null || origem.texture == null)
            return null;

        Texture2D textura = origem.texture;
        Rect recorte = new Rect(
            Mathf.Clamp01(recorteNormalizado.x) * textura.width,
            Mathf.Clamp01(recorteNormalizado.y) * textura.height,
            Mathf.Clamp01(recorteNormalizado.width) * textura.width,
            Mathf.Clamp01(recorteNormalizado.height) * textura.height);

        recorte.xMax = Mathf.Min(recorte.xMax, textura.width);
        recorte.yMax = Mathf.Min(recorte.yMax, textura.height);
        return Sprite.Create(textura, recorte, new Vector2(0.5f, 0.5f), origem.pixelsPerUnit);
    }

    private void CriarIndicadorControle(Transform pai, string tecla, string legenda,
        Color cor, Vector2 posicao)
    {
        RectTransform grupo = CriarPainel("Controle" + tecla, pai, new Color(1f, 1f, 1f, 0f));
        Posicionar(grupo, posicao, new Vector2(390f, 86f));

        RectTransform badge = CriarPainel("Tecla" + tecla, grupo, cor);
        Posicionar(badge, new Vector2(-112f, 0f), new Vector2(76f, 68f));
        TextMeshProUGUI textoTecla = CriarTexto("TextoTecla", badge, tecla, 32f,
            tecla == "Y" ? Color.black : Color.white);
        Posicionar(textoTecla.rectTransform, Vector2.zero, new Vector2(76f, 68f));

        TextMeshProUGUI textoLegenda = CriarTexto("Legenda", grupo, legenda, 28f, corTexto);
        textoLegenda.alignment = TextAlignmentOptions.Left;
        textoLegenda.enableAutoSizing = false;
        textoLegenda.overflowMode = TextOverflowModes.Overflow;
        Posicionar(textoLegenda.rectTransform, new Vector2(45f, 0f), new Vector2(300f, 60f));

        textosTeclasRodape[tecla] = textoTecla;
        legendasRodape[tecla] = textoLegenda;
    }

    private void ReconectarIndicadoresRodape()
    {
        textosTeclasRodape.Clear();
        legendasRodape.Clear();

        if (layoutRoot == null)
            return;

        Transform rodape = layoutRoot.Find("RodapeControles");
        if (rodape == null)
            return;

        string[] indicadores = { "A", "B", "Y", "X" };
        for (int i = 0; i < indicadores.Length; i++)
        {
            Transform controle = rodape.Find("Controle" + indicadores[i]);
            if (controle == null)
                continue;

            TextMeshProUGUI textoTecla = controle.Find("Tecla" + indicadores[i] + "/TextoTecla")
                ?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI textoLegenda = controle.Find("Legenda")?.GetComponent<TextMeshProUGUI>();

            if (textoTecla != null)
            {
                textosTeclasRodape[indicadores[i]] = textoTecla;
            }
            if (textoLegenda != null)
            {
                legendasRodape[indicadores[i]] = textoLegenda;
            }
        }
    }

    private void ReparentarDropdowns()
    {
        // Reparentar mantém o template e a navegação configurados pelo Inspector,
        // mas coloca os dois campos diretamente no card editável da nova tela.
        if (estiloP1 != null) PosicionarDropdown(estiloP1, painelP1, new Vector2(185f, -2f));
        if (dificuldadeP1 != null) PosicionarDropdown(dificuldadeP1, painelP1, new Vector2(185f, 82f));
        if (estiloP2 != null) PosicionarDropdown(estiloP2, painelP2, new Vector2(-185f, -2f));
        if (dificuldadeP2 != null) PosicionarDropdown(dificuldadeP2, painelP2, new Vector2(-185f, 82f));
    }

    private void ConfigurarDropdownVisual(TMP_Dropdown dropdown, Transform pai, string nome, Vector2 posicao, int lado)
    {
        if (dropdown == null)
            return;

        dropdown.gameObject.name = nome;
        Image imagem = dropdown.GetComponent<Image>();
        if (imagem != null)
        {
            // Reutiliza a referência do próprio lado: P1 mantém o vermelho e
            // P2 mantém o azul, como nos demais menus do jogo.
            imagem.sprite = lado == 1 ? spriteDropdownP1Original : spriteDropdownP2Original;
        }
        dropdown.spriteState = lado == 1 ? estadoDropdownP1Original : estadoDropdownP2Original;
        PosicionarDropdown(dropdown, pai, posicao);
        ConfigurarFocoDropdown(dropdown);
    }

    private void ConfigurarFocoDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        Image imagem = dropdown.GetComponent<Image>();
        if (imagem != null && !spritesNormaisDropdown.ContainsKey(dropdown))
            spritesNormaisDropdown.Add(dropdown, imagem.sprite);

        // A seleção de Player usa o mesmo SpriteSwap das outras telas:
        // vermelho/azul selecionado como destaque persistente e amarelo só no
        // flash de confirmação. Não criar um Outline diferente aqui evita que
        // o dropdown pareça pertencer a outro sistema visual.
        dropdown.transition = Selectable.Transition.SpriteSwap;

        Outline outlineAntigo = dropdown.GetComponent<Outline>();
        if (outlineAntigo != null)
            outlineAntigo.enabled = false;
    }

    public void DefinirFocoDropdown(TMP_Dropdown dropdown, bool focado)
    {
        if (dropdown == null)
            return;

        ConfigurarFocoDropdown(dropdown);
        Image imagem = dropdown.GetComponent<Image>();
        if (imagem == null)
            return;

        Sprite normal = spritesNormaisDropdown.ContainsKey(dropdown)
            ? spritesNormaisDropdown[dropdown]
            : imagem.sprite;
        Sprite foco = dropdown.spriteState.highlightedSprite != null
            ? dropdown.spriteState.highlightedSprite
            : dropdown.spriteState.selectedSprite;

        imagem.sprite = focado && dropdown.interactable && foco != null ? foco : normal;
    }

    private void AtualizarFocosDropdowns()
    {
        DefinirFocoDropdown(dificuldadeP1,
            categoriaFocoP1 == 2 && dificuldadeP1 != null && dificuldadeP1.interactable);
        DefinirFocoDropdown(estiloP1,
            categoriaFocoP1 == 3 && estiloP1 != null && estiloP1.interactable);
        DefinirFocoDropdown(dificuldadeP2,
            categoriaFocoP2 == 2 && dificuldadeP2 != null && dificuldadeP2.interactable);
        DefinirFocoDropdown(estiloP2,
            categoriaFocoP2 == 3 && estiloP2 != null && estiloP2.interactable);
    }

    private void PosicionarDropdown(TMP_Dropdown dropdown, Transform pai, Vector2 posicao)
    {
        dropdown.transform.SetParent(pai, false);
        RectTransform rect = dropdown.GetComponent<RectTransform>();
        Posicionar(rect, posicao, new Vector2(320f, 68f));
        if (dropdown.captionText != null)
        {
            dropdown.captionText.enableWordWrapping = false;
            dropdown.captionText.overflowMode = TextOverflowModes.Ellipsis;
            dropdown.captionText.enableAutoSizing = true;
            dropdown.captionText.fontSizeMin = 14f;
            dropdown.captionText.fontSizeMax = 27f;
        }
    }

    private void AdicionarListenerConfiguracao(TMP_Dropdown estilo, TMP_Dropdown dificuldade, int lado)
    {
        if (estilo != null)
        {
            estilo.onValueChanged.RemoveAllListeners();
            estilo.onValueChanged.AddListener(valor =>
                tela.SalvarConfiguracaoIAPublicamente(lado, valor,
                    dificuldade != null ? dificuldade.value : 1));
        }
        if (dificuldade != null)
        {
            dificuldade.onValueChanged.RemoveAllListeners();
            dificuldade.onValueChanged.AddListener(valor =>
                tela.SalvarConfiguracaoIAPublicamente(lado,
                    estilo != null ? estilo.value : 1, valor));
        }
    }

    private void AtualizarVisual()
    {
        if (tela == null)
            return;

        AplicarFundosDosCartoesPorModo();
        AtualizarCard(1, imagemP1, nomeP1, estiloP1, dificuldadeP1);
        AtualizarCard(2, imagemP2, nomeP2, estiloP2, dificuldadeP2);
        AtualizarDestaquesRoster();

        bool pronto = tela.PodeIniciarPublicamente();
        if (textoInstrucaoEnter != null)
        {
            textoInstrucaoEnter.text = pronto
                ? Traduzir("SELECT_PRESS_ENTER", "PRESSIONE ENTER PARA CONTINUAR")
                : Traduzir("SELECT_CHOOSE_BOTH", "SELECIONE UM PERSONAGEM PARA CADA JOGADOR");
            textoInstrucaoEnter.color = pronto ? corDestaque : corTexto;
        }
    }

    private void AplicarFundosDosCartoesPorModo()
    {
        AplicarFundoCartao(painelP1, 1);
        AplicarFundoCartao(painelP2, 2);
    }

    private void AplicarFundoCartao(RectTransform painel, int lado)
    {
        if (painel == null)
            return;

        Image fundo = painel.GetComponent<Image>();
        if (fundo == null)
            return;

        Sprite spriteNormal = lado == 1 ? fundoCartaoP1 : fundoCartaoP2;
        Sprite spriteAlternativo = lado == 1
            ? fundoCartaoP1Alternativo
            : fundoCartaoP2Alternativo;
        bool usarAlternativo = tela != null
            && !tela.UsaIAPublicamente(lado)
            && spriteAlternativo != null;

        if (!usarAlternativo)
        {
            fundo.enabled = true;
            fundo.sprite = spriteNormal;
            fundo.color = spriteNormal != null ? Color.white : Color.clear;
            fundo.preserveAspect = false;
            DefinirFundoAlternativoVisivel(painel, lado, false);
            return;
        }

        // A arte alternativa é vertical. Ela não pode ocupar o RectTransform
        // largo do cartão antigo, senão perde a proporção e a placa de nome
        // deixa de coincidir com o desenho do PNG.
        fundo.sprite = spriteNormal;
        fundo.color = new Color(1f, 1f, 1f, 0f);
        fundo.preserveAspect = false;

        Image imagemAlternativa = ObterOuCriarFundoAlternativo(painel, lado);
        imagemAlternativa.sprite = spriteAlternativo;
        imagemAlternativa.type = Image.Type.Simple;
        imagemAlternativa.preserveAspect = true;
        imagemAlternativa.color = Color.white;
        imagemAlternativa.raycastTarget = false;
        imagemAlternativa.enabled = true;

        RectTransform rect = imagemAlternativa.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        // Os painéis não têm a mesma altura no canvas: o P2 está abaixo do P1.
        // Usa a diferença real entre eles, em vez de um valor fixo, para que
        // as bordas superiores e inferiores continuem alinhadas se o layout
        // for ajustado novamente no Inspector.
        float compensacaoP2 = 0f;
        if (lado == 2 && painelP1 != null && painelP2 != null)
            compensacaoP2 = painelP1.anchoredPosition.y - painelP2.anchoredPosition.y;

        float elevacao = elevacaoFundoAlternativo + compensacaoP2;
        rect.anchoredPosition = new Vector2(0f, elevacao);

        // Mantém o tamanho original do arquivo sempre que houver espaço. Em
        // layouts menores, reduz somente o necessário para caber no cartão,
        // preservando a relação vertical 365 x 684.
        Vector2 tamanho = spriteAlternativo.rect.size;
        float alturaDisponivel = Mathf.Max(1f, painel.rect.height);
        float larguraDisponivel = Mathf.Max(1f, painel.rect.width);
        float escala = Mathf.Min(1f, alturaDisponivel / tamanho.y,
            larguraDisponivel / tamanho.x);
        rect.sizeDelta = tamanho * escala;

        // Os dois modais devem ter exatamente a mesma escala visual. O P2
        // usa o tamanho já calculado do P1 para evitar qualquer diferença de
        // importação ou arredondamento entre os PNGs.
        if (lado == 2 && fundoAlternativoP1 != null && fundoAlternativoP1.enabled)
            rect.sizeDelta = fundoAlternativoP1.rectTransform.sizeDelta;

        rect.SetAsFirstSibling();

        if (lado == 1)
            fundoAlternativoP1 = imagemAlternativa;
        else
            fundoAlternativoP2 = imagemAlternativa;
    }

    private Image ObterOuCriarFundoAlternativo(RectTransform painel, int lado)
    {
        string nome = "FundoAlternativoP" + lado;
        Transform existente = painel.Find(nome);
        Image imagem = existente != null ? existente.GetComponent<Image>() : null;
        if (imagem != null)
            return imagem;

        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(painel, false);
        imagem = objeto.GetComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }

    private void DefinirFundoAlternativoVisivel(RectTransform painel, int lado, bool visivel)
    {
        Image imagem = lado == 1 ? fundoAlternativoP1 : fundoAlternativoP2;
        if (imagem == null && painel != null)
            imagem = painel.Find("FundoAlternativoP" + lado)?.GetComponent<Image>();

        if (lado == 1)
            fundoAlternativoP1 = imagem;
        else
            fundoAlternativoP2 = imagem;

        if (imagem != null)
            imagem.enabled = visivel;
    }

    private void AtualizarTextosIdioma()
    {
        if (layoutRoot == null)
            return;

        DefinirTextoIdioma("FaixaTituloSelecao/TituloSelecao", "SELECT_PLAYER", "SELEÇÃO DE PERSONAGENS");
        DefinirTextoIdioma("PainelP1/TituloBotP1", "SELECT_DIFFICULTY_BOT", "DIFICULDADE (BOT)");
        DefinirTextoIdioma("PainelP2/TituloBotP2", "SELECT_DIFFICULTY_BOT", "DIFICULDADE (BOT)");
        DefinirTextoIdioma("PainelP1/LegendaEstiloP1", "SELECT_STYLE_BOT", "ESTILO DO BOT");
        DefinirTextoIdioma("PainelP2/LegendaEstiloP2", "SELECT_STYLE_BOT", "ESTILO DO BOT");
        DefinirTextoIdioma("PainelP1/ExplicacaoBotP1", "BOT_OPTIONS_HINT", "ESCOLHA O ESTILO E A DIFICULDADE DO SEU BOT.");
        DefinirTextoIdioma("PainelP2/ExplicacaoBotP2", "BOT_OPTIONS_HINT", "ESCOLHA O ESTILO E A DIFICULDADE DO SEU BOT.");

        DefinirTextoIdioma("RodapeControles/ControleA/Legenda", "SELECT_CONFIRM", "CONFIRMAR");
        DefinirTextoIdioma("RodapeControles/ControleB/Legenda", "SELECT_BACK", "VOLTAR");
        DefinirTextoIdioma("RodapeControles/ControleY/Legenda", "SELECT_DETAILS", "DETALHES");
        DefinirTextoIdioma("RodapeControles/ControleX/Legenda", "SELECT_NAVIGATE", "NAVEGAR");
        AplicarTextosRodape(rodapeUsandoControle);

        AtualizarVisual();
    }

    private void DefinirTextoIdioma(string caminho, string chave, string fallback)
    {
        Transform alvo = layoutRoot.Find(caminho);
        TextMeshProUGUI texto = alvo != null ? alvo.GetComponent<TextMeshProUGUI>() : null;
        if (texto != null)
            texto.text = Traduzir(chave, fallback);
    }

    private void AtualizarDestaquesRoster()
    {
        for (int i = 0; i < destaquesRoster.Count; i++)
        {
            Outline destaque = destaquesRoster[i];
            if (destaque == null)
                continue;

            int indice = indicesRoster[i];
            bool selecionadoP1 = tela.ObterIndiceSelecionadoPublicamente(1) == indice;
            bool selecionadoP2 = tela.ObterIndiceSelecionadoPublicamente(2) == indice;
            bool focadoP1 = categoriaFocoP1 == 4 && indiceFocoP1 == indice;
            bool focadoP2 = categoriaFocoP2 == 4 && indiceFocoP2 == indice;

            // A borda PNG da própria Hierarchy é o visual oficial. O Outline
            // antigo fica invisível para não duplicar a moldura.
            Color cor = destaque.effectColor;
            cor.a = 0f;
            destaque.effectColor = cor;
        }

        AtualizarBordasRoster();

        AtualizarFocoCard(focoCardP1, categoriaFocoP1 >= 1);
        AtualizarFocoCard(focoCardP2, categoriaFocoP2 >= 1);
        if (visualInfoP1 != null)
            visualInfoP1.DefinirFocoDaNavegacao(categoriaFocoP1 == 1);
        if (visualInfoP2 != null)
            visualInfoP2.DefinirFocoDaNavegacao(categoriaFocoP2 == 1);
        AtualizarFocosDropdowns();
    }

    private void GarantirSpritesDeBorda()
    {
#if UNITY_EDITOR
        if (spriteBordaP1 == null)
            spriteBordaP1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/bordas_QVer.png");
        if (spriteBordaP2 == null)
            spriteBordaP2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/bordas_QAz.png");
#endif
    }

    private void ConfigurarBordasDoSlot(Transform slot, int indice)
    {
        if (slot == null)
            return;

        Transform transformBordaP1 = slot.Find("BordaP1");
        if (transformBordaP1 == null)
            transformBordaP1 = slot.Find("border");

        // A borda que já foi colocada manualmente no slot é reaproveitada
        // como P1. O segundo layer tem nome explícito para continuar editável
        // na Hierarchy e permitir P1 e P2 no mesmo personagem.
        if (transformBordaP1 == null)
        {
            GameObject objeto = new GameObject("BordaP1", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(slot, false);
            transformBordaP1 = objeto.transform;
        }

        Image bordaP1 = transformBordaP1.GetComponent<Image>();
        if (bordaP1 == null)
            bordaP1 = transformBordaP1.gameObject.AddComponent<Image>();
        transformBordaP1.name = "BordaP1";

        Transform transformBordaP2 = slot.Find("BordaP2");
        if (transformBordaP2 == null)
        {
            GameObject objeto = new GameObject("BordaP2", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(slot, false);
            transformBordaP2 = objeto.transform;
            CopiarRectTransform(transformBordaP1 as RectTransform, transformBordaP2 as RectTransform);
        }

        Image bordaP2 = transformBordaP2.GetComponent<Image>();
        if (bordaP2 == null)
            bordaP2 = transformBordaP2.gameObject.AddComponent<Image>();

        ConfigurarBorda(bordaP1, spriteBordaP1);
        ConfigurarBorda(bordaP2, spriteBordaP2);
        transformBordaP1.SetAsLastSibling();
        transformBordaP2.SetAsLastSibling();

        bordasRosterP1.Add(bordaP1);
        bordasRosterP2.Add(bordaP2);
        indicesBordasRoster.Add(indice);
    }

    private void ConfigurarBorda(Image borda, Sprite sprite)
    {
        if (borda == null)
            return;

        if (sprite != null)
            borda.sprite = sprite;
        borda.preserveAspect = false;
        borda.raycastTarget = false;
        Color cor = Color.white;
        cor.a = 0f;
        borda.color = cor;
    }

    private void CopiarRectTransform(RectTransform origem, RectTransform destino)
    {
        if (origem == null || destino == null)
            return;

        destino.anchorMin = origem.anchorMin;
        destino.anchorMax = origem.anchorMax;
        destino.pivot = origem.pivot;
        destino.anchoredPosition = origem.anchoredPosition;
        destino.sizeDelta = origem.sizeDelta;
        destino.localRotation = origem.localRotation;
        destino.localScale = origem.localScale;
    }

    private void AtualizarBordasRoster()
    {
        for (int i = 0; i < indicesBordasRoster.Count; i++)
        {
            int indice = indicesBordasRoster[i];
            // A moldura é foco/hover da rota de personagens. Ao ir para I,
            // dificuldade, estilo ou voltar, ela some; a seleção continua
            // guardada internamente sem deixar uma borda presa no slot.
            bool p1 = categoriaFocoP1 == 4 &&
                (tela.ObterIndiceSelecionadoPublicamente(1) == indice || indiceFocoP1 == indice);
            bool p2 = categoriaFocoP2 == 4 &&
                (tela.ObterIndiceSelecionadoPublicamente(2) == indice || indiceFocoP2 == indice);

            AplicarVisibilidadeBorda(bordasRosterP1[i], p1);
            AplicarVisibilidadeBorda(bordasRosterP2[i], p2);
        }
    }

    private void AplicarVisibilidadeBorda(Image borda, bool visivel)
    {
        if (borda == null)
            return;

        Color cor = borda.color;
        cor.a = visivel ? 1f : 0f;
        borda.color = cor;
        borda.gameObject.SetActive(true);
    }

    private void AtualizarFocoCard(Outline foco, bool visivel)
    {
        if (foco == null)
            return;

        Color cor = foco.effectColor;
        cor.a = visivel ? 1f : 0f;
        foco.effectColor = cor;
    }

    /// <summary>
    /// Atualiza somente o destaque visual da navegação. Não altera seleção,
    /// dificuldade, estilo ou qualquer regra de partida.
    /// </summary>
    public void DefinirFocoNavegacao(int lado, int categoria, int indiceRoster)
    {
        if (lado == 1)
        {
            categoriaFocoP1 = categoria;
            indiceFocoP1 = indiceRoster;
        }
        else if (lado == 2)
        {
            categoriaFocoP2 = categoria;
            indiceFocoP2 = indiceRoster;
        }

        // O roster é compartilhado visualmente, mas os previews não são:
        // cada cartão lê somente indiceFocoP1 ou indiceFocoP2. Atualizamos os
        // dois cartões aqui para que mover um jogador atualize apenas o cartão
        // correspondente, inclusive quando ambos apontam para o mesmo slot.
        AtualizarCard(1, imagemP1, nomeP1, estiloP1, dificuldadeP1);
        AtualizarCard(2, imagemP2, nomeP2, estiloP2, dificuldadeP2);
        AtualizarDestaquesRoster();
    }

    public int ObterLadoAtivo()
    {
        return ladoAtivo;
    }

    public void SelecionarPersonagemPorNavegacao(int lado, int indice)
    {
        if (tela == null)
            return;

        tela.SelecionarPersonagemPublicamente(lado, indice);
        ladoAtivo = lado;
        AtualizarVisual();
    }

    private void CapturarComposicaoOriginalDosCartoes()
    {
        if (!composicaoOriginalP1.capturada)
            composicaoOriginalP1 = CapturarComposicaoOriginal(1, imagemP1, nomeP1);
        if (!composicaoOriginalP2.capturada)
            composicaoOriginalP2 = CapturarComposicaoOriginal(2, imagemP2, nomeP2);

        // A cena já foi salva depois dos ajustes do modo sem BOT. Portanto,
        // não usamos esses valores salvos como referência do modo BOT: a
        // composição abaixo é a posição original dos cartões largos.
        AplicarPosicoesOriginaisDoModoBot(1, ref composicaoOriginalP1);
        AplicarPosicoesOriginaisDoModoBot(2, ref composicaoOriginalP2);
    }

    private void AplicarPosicoesOriginaisDoModoBot(int lado,
        ref ComposicaoCartaoOriginal original)
    {
        if (!original.capturada)
            return;

        if (lado == 1)
        {
            original.posicaoRetrato = new Vector2(-170f, 35f);
            original.posicaoNome = new Vector2(-170f, -198.55188f);
            original.tamanhoNome = new Vector2(330f, 60.688f);
            original.posicaoRotulo = new Vector2(-302f, 265f);
            original.tamanhoRotulo = new Vector2(120f, 48f);
            original.posicaoInfo = new Vector2(299.55f, 288f);
            original.tamanhoInfo = new Vector2(70.893f, 79.018f);
        }
        else
        {
            original.posicaoRetrato = new Vector2(170f, 35f);
            original.posicaoNome = new Vector2(167.49994f, -175.79207f);
            original.tamanhoNome = new Vector2(325f, 63.584f);
            original.posicaoRotulo = new Vector2(298.34f, 283f);
            original.tamanhoRotulo = new Vector2(120f, 48f);
            original.posicaoInfo = new Vector2(-309.42f, 314.85f);
            original.tamanhoInfo = new Vector2(71.161f, 70.304f);
        }
    }

    private ComposicaoCartaoOriginal CapturarComposicaoOriginal(int lado,
        Image imagem, TextMeshProUGUI nome)
    {
        ComposicaoCartaoOriginal original = new ComposicaoCartaoOriginal();
        RectTransform painel = lado == 1 ? painelP1 : painelP2;
        if (painel == null)
            return original;

        if (imagem != null)
        {
            original.posicaoRetrato = imagem.rectTransform.anchoredPosition;
            original.tamanhoRetrato = imagem.rectTransform.sizeDelta;
        }

        if (nome != null)
        {
            original.posicaoNome = nome.rectTransform.anchoredPosition;
            original.tamanhoNome = nome.rectTransform.sizeDelta;
            original.alinhamentoNome = nome.alignment;
        }

        TextMeshProUGUI rotulo = painel.Find("RotuloP" + lado)?.GetComponent<TextMeshProUGUI>();
        if (rotulo != null)
        {
            original.posicaoRotulo = rotulo.rectTransform.anchoredPosition;
            original.tamanhoRotulo = rotulo.rectTransform.sizeDelta;
            original.alinhamentoRotulo = rotulo.alignment;
        }

        Button botaoInfo = lado == 1 ? botaoInfoOriginalP1 : botaoInfoOriginalP2;
        if (botaoInfo == null)
            botaoInfo = painel.Find("InfoIP" + lado)?.GetComponent<Button>();
        if (botaoInfo != null)
        {
            RectTransform rectInfo = botaoInfo.GetComponent<RectTransform>();
            original.posicaoInfo = rectInfo.anchoredPosition;
            original.tamanhoInfo = rectInfo.sizeDelta;
        }

        original.capturada = true;
        return original;
    }

    private void RestaurarComposicaoOriginal(int lado, Image imagem,
        TextMeshProUGUI nome)
    {
        ComposicaoCartaoOriginal original = lado == 1
            ? composicaoOriginalP1
            : composicaoOriginalP2;
        if (!original.capturada)
            return;

        // Reaplica a referência fixa do layout BOT também quando o Unity
        // manteve a instância viva entre execuções (Domain Reload desligado).
        AplicarPosicoesOriginaisDoModoBot(lado, ref original);
        if (lado == 1)
            composicaoOriginalP1 = original;
        else
            composicaoOriginalP2 = original;

        if (imagem != null)
        {
            imagem.rectTransform.anchoredPosition = original.posicaoRetrato;
            imagem.rectTransform.sizeDelta = original.tamanhoRetrato;
        }

        if (nome != null)
        {
            nome.rectTransform.anchoredPosition = original.posicaoNome;
            nome.rectTransform.sizeDelta = original.tamanhoNome;
            nome.alignment = original.alinhamentoNome;
        }

        RectTransform painel = lado == 1 ? painelP1 : painelP2;
        TextMeshProUGUI rotulo = painel != null
            ? painel.Find("RotuloP" + lado)?.GetComponent<TextMeshProUGUI>()
            : null;
        if (rotulo != null)
        {
            rotulo.rectTransform.anchoredPosition = original.posicaoRotulo;
            rotulo.rectTransform.sizeDelta = original.tamanhoRotulo;
            rotulo.alignment = original.alinhamentoRotulo;
        }

        Button botaoInfo = lado == 1 ? botaoInfoOriginalP1 : botaoInfoOriginalP2;
        if (botaoInfo == null && painel != null)
            botaoInfo = painel.Find("InfoIP" + lado)?.GetComponent<Button>();
        if (botaoInfo != null)
        {
            RectTransform rectInfo = botaoInfo.GetComponent<RectTransform>();
            rectInfo.anchoredPosition = original.posicaoInfo;
            rectInfo.sizeDelta = original.tamanhoInfo;
        }
    }

    private void AtualizarCard(int lado, Image imagem, TextMeshProUGUI nome,
        TMP_Dropdown estilo, TMP_Dropdown dificuldade)
    {
        bool estaNoRoster = lado == 1 ? categoriaFocoP1 == 4 : categoriaFocoP2 == 4;
        int indiceFoco = lado == 1 ? indiceFocoP1 : indiceFocoP2;
        int indicePreview = estaNoRoster ? indiceFoco : tela.ObterIndiceSelecionadoPublicamente(lado);
        Sprite corpo = indicePreview >= 0
            ? tela.ObterCorpoPersonagemPublicamente(indicePreview)
            : tela.ObterCorpoSelecionadoPublicamente(lado);
        if (imagem != null)
        {
            imagem.sprite = corpo;
            imagem.color = Color.white;
            imagem.gameObject.SetActive(corpo != null);
        }
        if (nome != null)
            nome.text = indicePreview >= 0
                ? tela.ObterNomePersonagemPublicamente(indicePreview)
                : tela.ObterNomeSelecionadoPublicamente(lado);

        bool usaIA = tela.UsaIAPublicamente(lado);
        if (!usaIA)
            AjustarComposicaoDoCartaoAlternativo(lado, imagem, nome);
        else
            RestaurarComposicaoOriginal(lado, imagem, nome);

        AplicarVisibilidadeOpcoesBot(lado == 1 ? painelP1 : painelP2, lado, usaIA);
        if (estilo != null)
        {
            estilo.interactable = usaIA;
            AplicarEstadoDropdown(estilo, usaIA);
        }
        if (dificuldade != null)
        {
            dificuldade.interactable = usaIA;
            AplicarEstadoDropdown(dificuldade, usaIA);
        }
    }

    private void AjustarComposicaoDoCartaoAlternativo(int lado, Image imagem,
        TextMeshProUGUI nome)
    {
        RectTransform painel = lado == 1 ? painelP1 : painelP2;
        if (painel == null)
            return;

        Image fundoAlternativo = lado == 1 ? fundoAlternativoP1 : fundoAlternativoP2;
        if (fundoAlternativo == null)
            fundoAlternativo = painel.Find("FundoAlternativoP" + lado)?.GetComponent<Image>();
        if (fundoAlternativo == null || !fundoAlternativo.enabled)
            return;

        // O retrato acompanha o centro da nova moldura vertical, em vez de
        // permanecer deslocado para a coluna dos controles de BOT.
        if (imagem != null)
        {
            Vector2 posicaoRetrato = imagem.rectTransform.anchoredPosition;
            posicaoRetrato.x = 0f;
            posicaoRetrato.y = fundoAlternativo.rectTransform.anchoredPosition.y - 10f;
            imagem.rectTransform.anchoredPosition = posicaoRetrato;
        }

        if (nome == null)
        {
            AjustarRotuloDoJogadorParaFundoAlternativo(lado, painel, fundoAlternativo);
            AjustarBotaoInfoParaFundoAlternativo(lado, painel, fundoAlternativo);
            return;
        }

        RectTransform rectNome = nome.rectTransform;
        rectNome.anchoredPosition = new Vector2(
            0f,
            fundoAlternativo.rectTransform.anchoredPosition.y
                - fundoAlternativo.rectTransform.rect.height * 0.38f);
        rectNome.sizeDelta = new Vector2(
            Mathf.Min(330f, fundoAlternativo.rectTransform.rect.width * 0.86f),
            rectNome.sizeDelta.y);
        nome.alignment = TextAlignmentOptions.Center;

        AjustarRotuloDoJogadorParaFundoAlternativo(lado, painel, fundoAlternativo);
        AjustarBotaoInfoParaFundoAlternativo(lado, painel, fundoAlternativo);
    }

    private void AjustarRotuloDoJogadorParaFundoAlternativo(int lado,
        RectTransform painel, Image fundoAlternativo)
    {
        if (painel == null)
            return;

        TextMeshProUGUI rotulo = painel.Find("RotuloP" + lado)?.GetComponent<TextMeshProUGUI>();
        if (rotulo == null)
            return;

        RectTransform rectRotulo = rotulo.rectTransform;
        rectRotulo.anchoredPosition = fundoAlternativo.rectTransform.anchoredPosition
            + new Vector2(
                fundoAlternativo.rectTransform.rect.width * 0.33f,
                fundoAlternativo.rectTransform.rect.height * 0.43f);
        rectRotulo.sizeDelta = new Vector2(120f, 56f);
        rotulo.alignment = TextAlignmentOptions.Center;
    }

    private void AjustarBotaoInfoParaFundoAlternativo(int lado,
        RectTransform painel, Image fundoAlternativo)
    {
        Button botao = lado == 1 ? botaoInfoOriginalP1 : botaoInfoOriginalP2;
        if (botao == null && painel != null)
            botao = painel.Find("InfoIP" + lado)?.GetComponent<Button>();
        if (botao == null)
            return;

        RectTransform rectBotao = botao.GetComponent<RectTransform>();
        if (rectBotao == null)
            return;

        // O botão fica no canto superior esquerdo da moldura vertical. A placa
        // superior do PNG fica reservada ao rótulo P1/P2.
        rectBotao.anchoredPosition = fundoAlternativo.rectTransform.anchoredPosition
            + new Vector2(
                -fundoAlternativo.rectTransform.rect.width * 0.34f,
                fundoAlternativo.rectTransform.rect.height * 0.43f);

        float tamanho = Mathf.Min(rectBotao.sizeDelta.x, rectBotao.sizeDelta.y);
        if (tamanho <= 0f)
            tamanho = 76f;
        rectBotao.sizeDelta = new Vector2(tamanho, tamanho);
    }

    private void AplicarVisibilidadeOpcoesBot(RectTransform painel, int lado, bool visivel)
    {
        if (painel == null)
            return;

        string rotulo = lado == 1 ? "P1" : "P2";
        string[] nomes =
        {
            "TituloBot" + rotulo,
            "DificuldadeBot" + rotulo,
            "LegendaEstilo" + rotulo,
            "EstiloBot" + rotulo,
            "ExplicacaoBot" + rotulo
        };

        for (int i = 0; i < nomes.Length; i++)
        {
            Transform objeto = painel.Find(nomes[i]);
            if (objeto != null)
                objeto.gameObject.SetActive(visivel);
        }
    }

    private void AplicarEstadoDropdown(TMP_Dropdown dropdown, bool habilitado)
    {
        if (dropdown == null)
            return;

        Color cor = habilitado ? Color.white : new Color(0.42f, 0.45f, 0.52f, 0.72f);
        Image imagem = dropdown.GetComponent<Image>();
        if (imagem != null)
            imagem.color = cor;

        if (dropdown.captionText != null)
            dropdown.captionText.color = habilitado ? corTexto : new Color(0.62f, 0.64f, 0.7f, 0.8f);

        Graphic[] graficos = dropdown.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graficos.Length; i++)
        {
            if (graficos[i] == null || graficos[i] == imagem || graficos[i] == dropdown.captionText)
                continue;

            Color filho = graficos[i].color;
            filho.a = habilitado ? Mathf.Max(filho.a, 1f) : Mathf.Min(filho.a, 0.55f);
            if (!habilitado)
                filho = Color.Lerp(filho, new Color(0.35f, 0.38f, 0.45f, filho.a), 0.55f);
            graficos[i].color = filho;
        }
    }

    private void Update()
    {
        if (!inicializado)
            return;

        AtualizarVisual();
        AtualizarRodapePeloDispositivo();
    }

    private void AtualizarRodapePeloDispositivo()
    {
        if (!Application.isPlaying)
            return;

        bool tecladoFoiUsado = TecladoFoiUsadoNesteFrame();
        bool controleFoiUsado = ControleFoiUsadoNesteFrame();

        if (!tecladoFoiUsado && !controleFoiUsado)
            return;

        bool usandoControle = controleFoiUsado && !tecladoFoiUsado;
        if (rodapeJaDetectouEntrada && rodapeUsandoControle == usandoControle)
            return;

        rodapeJaDetectouEntrada = true;
        rodapeUsandoControle = usandoControle;
        AplicarTextosRodape(usandoControle);
    }

    private bool TecladoFoiUsadoNesteFrame()
    {
        // A própria navegação da seleção lê essas teclas por UIInputUtility.
        // Usar a mesma leitura evita que as setas do P2 sejam confundidas com
        // ausência de entrada no detector visual do rodapé.
        for (int i = 0; i < TeclasConhecidasRodape.Length; i++)
        {
            KeyCode tecla = TeclasConhecidasRodape[i];
            if (TeclaLegadaEstaSendoUsada(tecla) || UIInputUtility.WasKeyPressed(tecla))
                return true;
        }

        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return false;

        // anyKey não identifica as setas de forma consistente nesta versão do
        // Input System. A lista allKeys inclui W/A/S/D e também Up/Down/Left/Right.
        foreach (var tecla in teclado.allKeys)
        {
            if (tecla != null && tecla.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool TeclaLegadaEstaSendoUsada(KeyCode tecla)
    {
        try
        {
            // A navegação de SelecaoPlayerNavigation usa esta mesma API. O
            // GetKey cobre também o caso em que o outro Update já consumiu o
            // frame de GetKeyDown antes de o layout ser atualizado.
            return Input.GetKeyDown(tecla) || Input.GetKey(tecla);
        }
        catch (System.InvalidOperationException)
        {
            // O projeto pode alternar para o Input System novo no Inspector.
            return false;
        }
    }

    private bool ControleFoiUsadoNesteFrame()
    {
        bool eixoAtivo = false;

        foreach (Gamepad controle in Gamepad.all)
        {
            if (controle == null)
                continue;

            if (AlgumBotaoDoControleFoiPressionado(controle))
                return true;

            Vector2 eixo = controle.dpad.ReadValue();
            if (eixo.sqrMagnitude < 0.01f)
                eixo = controle.leftStick.ReadValue();
            if (eixo.sqrMagnitude < 0.01f)
                eixo = controle.rightStick.ReadValue();

            if (eixo.sqrMagnitude > 0.25f)
                eixoAtivo = true;
        }

        bool novoMovimentoDoEixo = eixoAtivo && !eixoControleEstavaAtivo;
        eixoControleEstavaAtivo = eixoAtivo;
        return novoMovimentoDoEixo;
    }

    private bool AlgumBotaoDoControleFoiPressionado(Gamepad controle)
    {
        return controle.buttonSouth.wasPressedThisFrame
            || controle.buttonEast.wasPressedThisFrame
            || controle.buttonWest.wasPressedThisFrame
            || controle.buttonNorth.wasPressedThisFrame
            || controle.startButton.wasPressedThisFrame
            || controle.selectButton.wasPressedThisFrame
            || controle.leftShoulder.wasPressedThisFrame
            || controle.rightShoulder.wasPressedThisFrame
            || controle.leftStickButton.wasPressedThisFrame
            || controle.rightStickButton.wasPressedThisFrame
            || controle.dpad.up.wasPressedThisFrame
            || controle.dpad.down.wasPressedThisFrame
            || controle.dpad.left.wasPressedThisFrame
            || controle.dpad.right.wasPressedThisFrame;
    }

    private void AplicarTextosRodape(bool usandoControle)
    {
        // O layout fica salvo na cena, mas os dicionários são referências de
        // runtime. Rebusca sempre os quatro objetos visíveis antes de alterar
        // qualquer valor, evitando atualizar um objeto antigo/inativo.
        ReconectarIndicadoresRodape();

        if (usandoControle)
        {
            DefinirTeclaRodape("A", "A", 60f);
            DefinirTeclaRodape("B", "B", 60f);
            DefinirTeclaRodape("Y", "Y", 60f);
            DefinirTeclaRodape("X", "X", 60f);

            DefinirLegendaRodape("A", Traduzir("SELECT_CONFIRM", "CONFIRMAR"), 50f);
            DefinirLegendaRodape("B", Traduzir("SELECT_BACK", "VOLTAR"), 50f);
            DefinirLegendaRodape("Y", Traduzir("SELECT_DETAILS", "DETALHES"), 50f);
            DefinirLegendaRodape("X", Traduzir("SELECT_NAVIGATE", "NAVEGAR"), 50f);
            return;
        }

        string p1Confirmar = LerTeclaRodape("P1_Ataque", "F");
        string p2Confirmar = LerTeclaRodape("P2_Ataque", "K");
        string p1Cima = LerTeclaRodape("P1_Pular", "W");
        string p1Esquerda = LerTeclaRodape("P1_Esquerda", "A");
        string p1Baixo = LerTeclaRodape("P1_Defender", "S");
        string p1Direita = LerTeclaRodape("P1_Direita", "D");
        string p2Cima = LerTeclaRodape("P2_Pular", "UpArrow");
        string p2Esquerda = LerTeclaRodape("P2_Esquerda", "LeftArrow");
        string p2Baixo = LerTeclaRodape("P2_Defender", "DownArrow");
        string p2Direita = LerTeclaRodape("P2_Direita", "RightArrow");

        DefinirTeclaRodape("A", p1Confirmar + "/" + p2Confirmar, 60f);
        DefinirTeclaRodape("B", "ESC", 60f);
        DefinirTeclaRodape("Y", "INFO", 60f);
        DefinirTeclaRodape("X", "NAV", 60f);

        DefinirLegendaRodape("A", "P1 " + p1Confirmar + " / P2 " + p2Confirmar + "  " +
            Traduzir("SELECT_CONFIRM", "CONFIRMAR"), 50f);
        DefinirLegendaRodape("B", "ESC  " + Traduzir("SELECT_BACK", "VOLTAR"), 50f);
        DefinirLegendaRodape("Y", "INFO + " + p1Confirmar + "/" + p2Confirmar + "  " +
            Traduzir("SELECT_DETAILS", "DETALHES"), 50f);
        DefinirLegendaRodape("X", "P1 " + p1Cima + " " + p1Esquerda + " " + p1Baixo + " " + p1Direita +
            "  P2 " + p2Cima + " " + p2Esquerda + " " + p2Baixo + " " + p2Direita, 50f);

        // O teclado exibe muito mais informação que o controle. Mantém o
        // tamanho máximo desejado, mas reduz somente o texto que não couber
        // no espaço que foi ajustado manualmente na cena.
        AjustarTeclaRodapeAoEspaco("A", 60f, 22f);
        AjustarTeclaRodapeAoEspaco("B", 60f, 22f);
        AjustarTeclaRodapeAoEspaco("Y", 60f, 22f);
        AjustarTeclaRodapeAoEspaco("X", 60f, 22f);

        AjustarLegendaRodapeAoEspaco("A", 50f, 18f);
        AjustarLegendaRodapeAoEspaco("B", 50f, 18f);
        AjustarLegendaRodapeAoEspaco("Y", 50f, 16f);
        AjustarLegendaRodapeAoEspaco("X", 50f, 12f);
    }

    private void AjustarTeclaRodapeAoEspaco(string id, float tamanhoMaximo, float tamanhoMinimo)
    {
        TextMeshProUGUI alvo;
        if (!textosTeclasRodape.TryGetValue(id, out alvo) || alvo == null)
            return;

        alvo.fontSizeMax = tamanhoMaximo;
        alvo.fontSizeMin = tamanhoMinimo;
        alvo.enableAutoSizing = true;
        alvo.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void AjustarLegendaRodapeAoEspaco(string id, float tamanhoMaximo, float tamanhoMinimo)
    {
        TextMeshProUGUI alvo;
        if (!legendasRodape.TryGetValue(id, out alvo) || alvo == null)
            return;

        alvo.fontSizeMax = tamanhoMaximo;
        alvo.fontSizeMin = tamanhoMinimo;
        alvo.enableAutoSizing = true;
        alvo.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void DefinirTeclaRodape(string id, string texto, float tamanho)
    {
        TextMeshProUGUI alvo;
        if (!textosTeclasRodape.TryGetValue(id, out alvo) || alvo == null)
            return;

        alvo.text = texto;
        alvo.fontSize = tamanho;
        alvo.enableAutoSizing = false;
    }

    private void DefinirLegendaRodape(string id, string texto, float tamanho)
    {
        TextMeshProUGUI alvo;
        if (!legendasRodape.TryGetValue(id, out alvo) || alvo == null)
            return;

        alvo.text = texto;
        alvo.fontSize = tamanho;
        alvo.enableAutoSizing = false;
        alvo.overflowMode = TextOverflowModes.Overflow;
    }

    private string LerTeclaRodape(string chave, string padrao)
    {
        string tecla = PlayerPrefs.GetString(chave, padrao);
        switch (tecla)
        {
            case "LeftArrow": return "←";
            case "RightArrow": return "→";
            case "UpArrow": return "↑";
            case "DownArrow": return "↓";
            case "Escape": return "ESC";
            case "Return": return "ENTER";
            case "KeypadEnter": return "ENTER";
            case "Space": return "SPACE";
            default: return tecla;
        }
    }

    private string Traduzir(string chave, string fallback)
    {
        return LanguageManager.Instance != null ? LanguageManager.Instance.GetText(chave) : fallback;
    }

    private Sprite EncontrarSpriteDeFundo()
    {
        Image[] imagens = GetComponentsInChildren<Image>(true);
        foreach (Image imagem in imagens)
        {
            if (imagem != null && imagem.sprite != null &&
                imagem.sprite.name.IndexOf("background", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return imagem.sprite;
        }
        return null;
    }

    private void OcultarInterfaceAntiga()
    {
        Transform raizInterface = ObterRaizInterface();
        for (int i = 0; i < raizInterface.childCount; i++)
        {
            Transform filho = raizInterface.GetChild(i);
            string nome = filho.name.ToLowerInvariant();
            if (nome.Contains("modalinfopersonagem"))
            {
                // O modal permanece na Hierarchy para ser editável, mas nunca
                // começa aberto sobre a nova tela de seleção.
                filho.gameObject.SetActive(false);
                continue;
            }

            if (nome.Contains("background") || nome.Contains("fundo") ||
                nome.Contains("painelaviso") || nome.Contains("aviso"))
                continue;

            CanvasGroup grupo = filho.GetComponent<CanvasGroup>();
            if (grupo == null)
                grupo = filho.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
        }
    }

    private void OcultarRestosDaVersaoAntiga()
    {
        Transform raizInterface = ObterRaizInterface();
        Transform infoP1Antigo = raizInterface.Find("BotaoInfoP1");
        if (infoP1Antigo != null)
            infoP1Antigo.gameObject.SetActive(false);

        Transform infoP2Antigo = raizInterface.Find("BotaoInfoP2");
        if (infoP2Antigo != null)
            infoP2Antigo.gameObject.SetActive(false);

        // O VS é uma arte real da tela e fica sob o Canvas, atrás da composição.
        // Não o remover nem ocultar durante a inicialização.
    }

    private Transform ObterRaizInterface()
    {
        Transform areaSegura = transform.Find("UI_16x9_SafeArea");
        return areaSegura != null ? areaSegura : transform;
    }

    private RectTransform CriarPainel(string nome, Transform pai, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(pai, false);
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return objeto.GetComponent<RectTransform>();
    }

    private Image CriarImagem(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(pai, false);
        Image imagem = objeto.GetComponent<Image>();
        imagem.raycastTarget = false;
        return imagem;
    }

    private TextMeshProUGUI CriarTexto(string nome, Transform pai, string texto, float tamanho, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        TextMeshProUGUI componente = objeto.GetComponent<TextMeshProUGUI>();
        componente.text = texto;
        componente.color = cor;
        componente.fontSize = tamanho;
        componente.alignment = TextAlignmentOptions.Center;
        componente.enableWordWrapping = false;
        componente.overflowMode = TextOverflowModes.Ellipsis;
        if (tela != null && tela.nomePlayer1 != null)
            componente.font = tela.nomePlayer1.font;
        return componente;
    }

    private Button CriarBotaoTexto(string nome, Transform pai, string texto, Vector2 tamanho, Color fundo, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = fundo;
        Button botao = objeto.GetComponent<Button>();
        botao.targetGraphic = imagem;

        TextMeshProUGUI label = CriarTexto("Texto", objeto.transform, texto, 20f, cor);
        Posicionar(label.rectTransform, Vector2.zero, tamanho);
        return botao;
    }

    private void AplicarSpriteBotao(Button botao, Sprite sprite)
    {
        if (botao == null || sprite == null)
            return;

        Image imagem = botao.GetComponent<Image>();
        if (imagem == null)
            return;

        imagem.sprite = sprite;
        imagem.color = Color.white;
        imagem.preserveAspect = false;
    }

    private Button CriarBotaoVoltar(string nome, Transform pai)
    {
        Button botao = CriarBotaoTexto(nome, pai, string.Empty,
            new Vector2(110f, 110f), Color.white, corTexto);
        Image imagem = botao.GetComponent<Image>();
        if (imagem != null)
        {
            imagem.sprite = spriteVoltarNormalizado != null
                ? spriteVoltarNormalizado
                : tela.botaoVoltar != null
                    ? tela.botaoVoltar.GetComponent<Image>()?.sprite
                    : null;
            imagem.type = Image.Type.Simple;
            imagem.preserveAspect = true;
            imagem.color = Color.white;
        }

        TextMeshProUGUI texto = botao.GetComponentInChildren<TextMeshProUGUI>(true);
        if (texto != null)
            texto.gameObject.SetActive(false);

        SpriteState estado = botao.spriteState;
        Sprite foco = spriteVoltarSelecionado != null ? spriteVoltarSelecionado : tela.spriteFocoVoltar;
        estado.highlightedSprite = foco;
        estado.pressedSprite = foco;
        estado.selectedSprite = foco;
        botao.spriteState = estado;
        botao.transition = Selectable.Transition.SpriteSwap;
        return botao;
    }

    private void ConfigurarVisualVoltar(Button botao)
    {
        if (botao == null)
            return;

        Image imagem = botao.GetComponent<Image>();
        if (imagem != null)
        {
            if (spriteVoltarNormalizado != null)
                imagem.sprite = spriteVoltarNormalizado;
            imagem.type = Image.Type.Simple;
            imagem.preserveAspect = true;
            imagem.color = Color.white;
        }

        SpriteState estado = botao.spriteState;
        Sprite foco = spriteVoltarSelecionado != null ? spriteVoltarSelecionado : tela.spriteFocoVoltar;
        estado.highlightedSprite = foco;
        estado.pressedSprite = foco;
        estado.selectedSprite = foco;
        botao.spriteState = estado;
        botao.transition = Selectable.Transition.SpriteSwap;
        // Não reposicionar aqui: esta é a etapa de rebind dos eventos. A posição
        // editada manualmente no Scene/Inspector deve permanecer intacta.
    }

    private Button CriarAreaClique(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = new Color(1f, 1f, 1f, 0f);
        Button botao = objeto.GetComponent<Button>();
        botao.targetGraphic = imagem;
        return botao;
    }

    private void Posicionar(RectTransform rect, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
    }
}
