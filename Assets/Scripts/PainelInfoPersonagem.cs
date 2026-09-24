using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Modal de informações da seleção de personagens.
/// A estrutura principal fica salva na Hierarchy para ser ajustada no Scene View;
/// a montagem em runtime permanece apenas como fallback para cenas antigas.
/// </summary>
public class PainelInfoPersonagem : MonoBehaviour
{
    private enum AcaoInfo
    {
        Ataque,
        Especial,
        Ultimate,
        Defesa
    }

    private TelaSelecaoPlayer telaSelecao;
    private DadosPersonagem[] dadosPersonagens;
    private RectTransform canvasRect;
    private RectTransform rectAreaPreview;
    private RectTransform rectEfeito;
    private RectTransform rectAlvo;
    private TMP_FontAsset fonteBase;

    private GameObject overlay;
    private RectTransform janela;
    private Image imagemPreview;
    private Image imagemEfeito;
    private Image imagemAlvo;
    private TextMeshProUGUI titulo;
    private TextMeshProUGUI subtitulo;
    private TextMeshProUGUI legendaPreview;
    private TextMeshProUGUI descricao;
    private TextMeshProUGUI atributos;
    private RectTransform graficoAtributos;
    private TextMeshProUGUI[] rotulosGraficoAtributos;
    private RawImage[,] segmentosGraficoAtributos;
    private Button botaoFechar;
    private Button[] botoesAcoes;
    private Button[] botoesInfo;
    private bool[] botoesInfoUsamPosicaoAutomatica;

    private DadosPersonagem dadosAtuais;
    private Sprite[] framesAtuais;
    private Sprite[] framesAlvo;
    private float fpsAtual;
    private float fpsAlvo;
    private float tempoFrame;
    private float tempoFrameAlvo;
    private int frameAtual;
    private int frameAlvoAtual;
    private Vector2 posicaoPersonagemBase;
    private Vector2 posicaoPersonagemHierarquia;
    private Vector3 escalaPersonagemHierarquia = Vector3.one;
    private bool posicaoPersonagemHierarquiaCapturada;
    private float tempoPreviewUnico;
    private float duracaoSaltoPreview;
    private float alturaSaltoPreview;
    private int etapaPreviewUnico;
    private bool previewEspecialSalto;
    private bool previewUltimateLava;
    private bool previewUltimateRaio;
    private bool efeitoImpactoAtivo;
    private float proximaNavegacao;
    private AcaoInfo acaoAtual;
    private bool inicializado;
    private bool aberto;

    private static readonly Color CorAlvoQueimandoPreview = new Color(1f, 0.55f, 0.25f, 1f);

    [Header("Visual do modal — editável no Inspector")]
    [SerializeField] private Vector2 tamanhoJanela = new Vector2(1680f, 930f);
    [SerializeField] private Vector2 tamanhoAreaPreview = new Vector2(760f, 500f);
    [SerializeField] private Vector2 tamanhoImagemPreview = new Vector2(360f, 400f);
    [SerializeField] private Vector2 posicaoPreview = new Vector2(-390f, 15f);
    [SerializeField] private Vector2 posicaoPersonagemPreview = new Vector2(-135f, 25f);
    [SerializeField] private Vector2 tamanhoAlvoPreview = new Vector2(280f, 340f);
    [SerializeField] private Vector2 posicaoAlvoPreview = new Vector2(330f, -55f);
    [SerializeField] private Vector2 tamanhoEfeito = new Vector2(430f, 120f);
    [SerializeField] private Vector2 posicaoEfeito = new Vector2(190f, -205f);
    // X avança o raio até a frente da mão; Y usa o eixo local do palco
    // (negativo desce) para alinhar o núcleo redondo da Jamanta.
    [SerializeField] private Vector2 posicaoRaio = new Vector2(65f, -60f);
    [SerializeField] private Vector2 tamanhoRaio = new Vector2(730f, 155f);
    [SerializeField] private float escalaDefesaJamantaPreview = 1.35f;
    [SerializeField] private Vector2 ajusteVerticalDefesaJamantaPreview = new Vector2(0f, 18f);
    [SerializeField] private Vector2 tamanhoBotaoAcao = new Vector2(190f, 60f);
    [SerializeField] private float espacamentoEntreAcoes = 205f;
    [SerializeField] private Vector2 posicaoLinhaAcoes = new Vector2(-55f, 265f);
    [SerializeField] private Vector2 tamanhoBotaoFechar = new Vector2(220f, 60f);
    [SerializeField] private Vector2 posicaoBotaoFechar = new Vector2(610f, -390f);
    [SerializeField] private Vector2 tamanhoBotaoInfo = new Vector2(140f, 58f);
    [SerializeField] private float deslocamentoVerticalBotaoInfo = -50f;
    [SerializeField] private int tamanhoFonteTitulo = 42;
    [SerializeField] private int tamanhoFonteSubtitulo = 24;
    [SerializeField] private int tamanhoFonteBotoes = 30;
    [SerializeField] private int tamanhoFonteDescricao = 52;
    [SerializeField] private int tamanhoFonteAtributos = 38;
    [SerializeField] private float tamanhoFonteGraficoAtributos = 24f;
    [SerializeField] private int quantidadeSegmentosGrafico = 10;
    [SerializeField] private Vector2 tamanhoSegmentoGrafico = new Vector2(17f, 14f);
    [SerializeField] private float espacamentoSegmentoGrafico = 3f;
    [SerializeField] private float multiplicadorVelocidadePreview = 1f;
    [SerializeField] private float fpsMinimoPreview = 0.01f;
    [SerializeField] private Color corBotaoInfoNormal = new Color(0.04f, 0.12f, 0.24f, 0.96f);
    [SerializeField] private Color corBotaoAcaoNormal = new Color(0.015f, 0.015f, 0.02f, 0.98f);
    [SerializeField] private Color corBotaoHoverSelecionado = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color corFundoModal = new Color(0.035f, 0.06f, 0.13f, 0.98f);
    [SerializeField] private Color corFundoOverlay = new Color(0.005f, 0.01f, 0.03f, 0.82f);
    [SerializeField] private Color corBordaModal = new Color(0.95f, 0.78f, 0.3f, 0.95f);
    [SerializeField] private Sprite spriteFundoModal;
    [SerializeField] private Sprite spriteBotaoAzul;
    [SerializeField] private Sprite spriteBotaoAmarelo;

    public bool EstaAberto
    {
        get { return aberto; }
    }

    void CapturarPosicaoPersonagemDaHierarquia()
    {
        if (posicaoPersonagemHierarquiaCapturada || imagemPreview == null)
            return;

        // Esta é a posição editada manualmente no Scene/Hierarchy. Ela fica
        // separada dos deslocamentos temporários usados pelo raio e pelo salto.
        posicaoPersonagemHierarquia = imagemPreview.rectTransform.anchoredPosition;
        escalaPersonagemHierarquia = imagemPreview.rectTransform.localScale;
        if (escalaPersonagemHierarquia.sqrMagnitude < 0.01f)
            escalaPersonagemHierarquia = Vector3.one;
        posicaoPersonagemBase = posicaoPersonagemHierarquia;
        posicaoPersonagemHierarquiaCapturada = true;
    }

    Vector2 ObterPosicaoPersonagemBase()
    {
        return posicaoPersonagemHierarquiaCapturada
            ? posicaoPersonagemHierarquia
            : posicaoPersonagemBase;
    }

    void AtualizarEscalaPreviewDaAcao()
    {
        if (imagemPreview == null)
            return;

        Vector3 escalaBase = posicaoPersonagemHierarquiaCapturada
            ? escalaPersonagemHierarquia
            : Vector3.one;
        bool eDefesaJamanta = dadosAtuais != null && acaoAtual == AcaoInfo.Defesa &&
            !string.IsNullOrEmpty(dadosAtuais.nomePersonagem) &&
            dadosAtuais.nomePersonagem.ToUpperInvariant().Contains("JAMANTA");
        float fator = eDefesaJamanta ? Mathf.Max(1f, escalaDefesaJamantaPreview) : 1f;
        imagemPreview.rectTransform.localScale = escalaBase * fator;
        imagemPreview.rectTransform.anchoredPosition = ObterPosicaoPersonagemBase() +
            (eDefesaJamanta ? ajusteVerticalDefesaJamantaPreview : Vector2.zero);
    }

    void RestaurarTransformacaoPersonagemDaHierarquia()
    {
        if (imagemPreview == null)
            return;

        imagemPreview.rectTransform.anchoredPosition = ObterPosicaoPersonagemBase();
        imagemPreview.rectTransform.localScale = posicaoPersonagemHierarquiaCapturada
            ? escalaPersonagemHierarquia
            : Vector3.one;
    }

    public void Inicializar(TelaSelecaoPlayer origem, DadosPersonagem[] dados, Image painelEstilo)
    {
        if (inicializado)
            return;

        telaSelecao = origem;
        dadosPersonagens = dados;
        canvasRect = GetComponent<RectTransform>();

        if (telaSelecao != null && telaSelecao.nomePlayer1 != null)
            fonteBase = telaSelecao.nomePlayer1.font;

        if (!LocalizarInterfaceExistente())
            CriarInterface(spriteFundoModal != null ? spriteFundoModal : painelEstilo != null ? painelEstilo.sprite : null);
        else
            ConfigurarInterfaceExistente();

        GarantirGraficoAtributos();
        CapturarPosicaoPersonagemDaHierarquia();

        CriarBotoesDeInfo();
        inicializado = true;
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
        AtualizarTextosIdioma();
        Fechar();
    }

    void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
    }

    void Update()
    {
        if (!inicializado)
            return;

        AtualizarPosicaoDosBotoesInfo();

        if (!aberto)
            return;

        if (UIInputUtility.WasCancelPressed())
        {
            Fechar();
            return;
        }

        // O botão FECHAR também precisa aceitar o ataque configurado do jogador
        // que está navegando no modal, não apenas o Submit padrão (Enter).
        if (botaoFechar != null
            && UIInputUtility.WasPlayerConfirmPressed()
            && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == botaoFechar.gameObject)
        {
            Fechar();
            return;
        }

        if (Time.unscaledTime >= proximaNavegacao)
        {
            if (UIInputUtility.WasNavigateDownPressed())
            {
                UIFocusUtility.Select(botaoFechar != null ? botaoFechar.gameObject : null);
                proximaNavegacao = Time.unscaledTime + 0.18f;
            }
            else if (UIInputUtility.WasNavigateUpPressed())
            {
                UIFocusUtility.Select(botoesAcoes[(int)acaoAtual].gameObject);
                proximaNavegacao = Time.unscaledTime + 0.18f;
            }
            else if (UIInputUtility.WasNavigateLeftPressed())
            {
                SelecionarAcao((AcaoInfo)(((int)acaoAtual + botoesAcoes.Length - 1) % botoesAcoes.Length));
                proximaNavegacao = Time.unscaledTime + 0.18f;
            }
            else if (UIInputUtility.WasNavigateRightPressed())
            {
                SelecionarAcao((AcaoInfo)(((int)acaoAtual + 1) % botoesAcoes.Length));
                proximaNavegacao = Time.unscaledTime + 0.18f;
            }
        }

        AtualizarAnimacaoPreview();

        if (imagemEfeito != null && imagemEfeito.gameObject.activeSelf)
        {
            float pulsacao = 0.88f + Mathf.Sin(Time.unscaledTime * 8f) * 0.12f;
            imagemEfeito.color = new Color(1f, 1f, 1f, pulsacao);
        }
    }

    public void AbrirParaLado(int lado)
    {
        if (telaSelecao == null)
            return;

        Abrir(telaSelecao.ObterIndiceParaInfo(lado));
    }

    public void Abrir(int indice)
    {
        if (!inicializado || dadosPersonagens == null || indice < 0 || indice >= dadosPersonagens.Length)
            return;

        DadosPersonagem dados = dadosPersonagens[indice];
        if (dados == null)
            return;

        dadosAtuais = dados;
        aberto = true;
        if (overlay != null)
            overlay.SetActive(true);
        if (botaoFechar != null)
        {
            botaoFechar.interactable = true;
            botaoFechar.gameObject.SetActive(true);
        }
        AtualizarBotoesInfo();
        AtualizarCabecalho();
        SelecionarAcao(AcaoInfo.Ataque);
    }

    public void Fechar()
    {
        aberto = false;
        if (overlay != null)
            overlay.SetActive(false);

        // O preview do raio desloca temporariamente o personagem para baixo.
        // Restaura a posição salva para o próximo ataque não herdar esse ajuste.
        if (dadosAtuais != null && imagemPreview != null)
            RestaurarTransformacaoPersonagemDaHierarquia();

        if (telaSelecao != null)
            telaSelecao.RestaurarFocoAposModal();

        if (inicializado)
            AtualizarBotoesInfo();

        UIFocusUtility.ClearSelection();
    }

    void AtualizarTextosIdioma()
    {
        if (!inicializado)
            return;

        AtualizarRotulosGraficoAtributos();

        string[] nomesAcoes =
        {
            Traduzir("CHAR_ATTACK", "ATAQUE"),
            Traduzir("CHAR_SPECIAL", "ESPECIAL"),
            Traduzir("CHAR_ULTIMATE", "ULTIMATE"),
            Traduzir("CHAR_DEFENSE", "DEFESA")
        };

        for (int i = 0; i < botoesAcoes.Length; i++)
        {
            TextMeshProUGUI texto = botoesAcoes[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null)
                texto.text = nomesAcoes[i];
        }

        for (int i = 0; i < botoesInfo.Length; i++)
        {
            if (botoesInfo[i] == null)
                continue;

            TextMeshProUGUI texto = botoesInfo[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null)
                texto.text = "I";
        }

        TextMeshProUGUI textoFechar = botaoFechar != null
            ? botaoFechar.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (textoFechar != null)
            textoFechar.text = Traduzir("CHAR_INFO_CLOSE", "FECHAR");

        if (legendaPreview != null)
            legendaPreview.text = Traduzir("CHAR_COMBAT_ANIMATION", "ANIMAÇÃO DE COMBATE");

        if (dadosAtuais != null)
        {
            AtualizarCabecalho();
            AtualizarTextoDaAcao();
        }
        else
        {
            if (titulo != null)
                titulo.text = Traduzir("CHAR_INFO", "INFORMAÇÕES DO PERSONAGEM");
            if (subtitulo != null)
                subtitulo.text = Traduzir("CHAR_INFO_HINT", "Passe pelos ataques para ver os frames em ação");
            if (descricao != null)
                descricao.text = Traduzir("CHAR_SELECT_ACTION", "Selecione uma ação para ver os frames da animação.");
            if (atributos != null)
                atributos.text = string.Empty;
        }
    }

    void CriarInterface(Sprite painelEstilo)
    {
        GameObject overlayObj = CriarObjetoUI("ModalInfoPersonagem", canvasRect);
        overlay = overlayObj;

        Image imagemOverlay = overlayObj.AddComponent<Image>();
        imagemOverlay.color = corFundoOverlay;
        imagemOverlay.raycastTarget = true;
        DefinirEsticado(overlayObj.GetComponent<RectTransform>());

        janela = CriarPainel("JanelaInfoPersonagem", overlayObj.transform, tamanhoJanela);
        Image fundoJanela = janela.GetComponent<Image>();
        if (painelEstilo != null)
        {
            fundoJanela.sprite = painelEstilo;
            fundoJanela.type = Image.Type.Simple;
            fundoJanela.color = Color.white;
        }
        else
        {
            fundoJanela.color = corFundoModal;
        }

        Outline contorno = janela.gameObject.AddComponent<Outline>();
        contorno.effectColor = corBordaModal;
        contorno.effectDistance = new Vector2(4f, -4f);

        CriarFaixaDecorativa(janela, new Vector2(0f, tamanhoJanela.y * 0.34f), new Vector2(tamanhoJanela.x - 280f, 3f));
        CriarFaixaDecorativa(janela, new Vector2(0f, -tamanhoJanela.y * 0.32f), new Vector2(tamanhoJanela.x - 280f, 2f));

        titulo = CriarTexto("TituloInfo", janela, tamanhoFonteTitulo, Color.white, TextAlignmentOptions.Center);
        DefinirPosicao(titulo.rectTransform, new Vector2(0f, tamanhoJanela.y * 0.44f), new Vector2(tamanhoJanela.x - 280f, 70f));
        titulo.fontStyle = FontStyles.Bold;

        subtitulo = CriarTexto("SubtituloInfo", janela, tamanhoFonteSubtitulo, new Color(0.8f, 0.86f, 0.95f), TextAlignmentOptions.Center);
        DefinirPosicao(subtitulo.rectTransform, new Vector2(0f, tamanhoJanela.y * 0.39f), new Vector2(tamanhoJanela.x - 280f, 42f));

        GameObject previewFundo = CriarObjetoUI("FundoPreview", janela);
        Image imagemPreviewFundo = previewFundo.AddComponent<Image>();
        imagemPreviewFundo.color = new Color(0.015f, 0.025f, 0.06f, 0.78f);
        rectAreaPreview = previewFundo.GetComponent<RectTransform>();
        DefinirPosicao(rectAreaPreview, posicaoPreview, tamanhoAreaPreview);
        previewFundo.AddComponent<RectMask2D>();

        GameObject efeitoObj = CriarObjetoUI("EfeitoAnimado", previewFundo.transform);
        imagemEfeito = efeitoObj.AddComponent<Image>();
        imagemEfeito.preserveAspect = true;
        imagemEfeito.raycastTarget = false;
        rectEfeito = efeitoObj.GetComponent<RectTransform>();
        DefinirPosicao(rectEfeito, posicaoEfeito, tamanhoEfeito);

        GameObject previewObj = CriarObjetoUI("PreviewAnimado", previewFundo.transform);
        imagemPreview = previewObj.AddComponent<Image>();
        imagemPreview.preserveAspect = true;
        imagemPreview.raycastTarget = false;
        DefinirPosicao(previewObj.GetComponent<RectTransform>(), posicaoPersonagemPreview, tamanhoImagemPreview);

        GameObject alvoObj = CriarObjetoUI("AlvoAnimado", previewFundo.transform);
        imagemAlvo = alvoObj.AddComponent<Image>();
        imagemAlvo.preserveAspect = true;
        imagemAlvo.raycastTarget = false;
        rectAlvo = alvoObj.GetComponent<RectTransform>();
        DefinirPosicao(rectAlvo, posicaoAlvoPreview, tamanhoAlvoPreview);
        alvoObj.SetActive(false);

        efeitoObj.transform.SetAsLastSibling();

        legendaPreview = CriarTexto("LegendaPreview", janela, 21, new Color(0.95f, 0.78f, 0.3f), TextAlignmentOptions.Center);
        legendaPreview.text = Traduzir("CHAR_COMBAT_ANIMATION", "ANIMAÇÃO DE COMBATE");
        DefinirPosicao(legendaPreview.rectTransform, new Vector2(posicaoPreview.x, -tamanhoJanela.y * 0.36f), new Vector2(tamanhoAreaPreview.x, 36f));

        botoesAcoes = new Button[4];
        string[] nomesAcoes =
        {
            Traduzir("CHAR_ATTACK", "ATAQUE"),
            Traduzir("CHAR_SPECIAL", "ESPECIAL"),
            Traduzir("CHAR_ULTIMATE", "ULTIMATE"),
            Traduzir("CHAR_DEFENSE", "DEFESA")
        };

        for (int i = 0; i < botoesAcoes.Length; i++)
        {
            int indiceBotao = i;
            botoesAcoes[i] = CriarBotao("BotaoAcao" + i, janela, nomesAcoes[i], tamanhoBotaoAcao);
            DefinirPosicao(botoesAcoes[i].GetComponent<RectTransform>(),
                new Vector2(posicaoLinhaAcoes.x + i * espacamentoEntreAcoes, posicaoLinhaAcoes.y), tamanhoBotaoAcao);
            AplicarVisualBotao(botoesAcoes[i], corBotaoAcaoNormal);
            botoesAcoes[i].onClick.AddListener(() => SelecionarAcao((AcaoInfo)indiceBotao));
        }

        descricao = CriarTexto("DescricaoAcao", janela, tamanhoFonteDescricao, Color.white, TextAlignmentOptions.TopLeft);
        descricao.enableWordWrapping = true;
        descricao.enableAutoSizing = true;
        descricao.fontSizeMin = 28f;
        descricao.fontSizeMax = tamanhoFonteDescricao;
        descricao.lineSpacing = -6f;
        descricao.margin = new Vector4(8f, 4f, 8f, 4f);

        atributos = CriarTexto("AtributosPersonagem", janela, tamanhoFonteAtributos, new Color(0.83f, 0.88f, 0.96f), TextAlignmentOptions.TopLeft);
        atributos.enableAutoSizing = true;
        atributos.fontSizeMin = 22f;
        atributos.fontSizeMax = Mathf.Min(tamanhoFonteAtributos, 34f);
        atributos.lineSpacing = -8f;
        atributos.margin = new Vector4(8f, 4f, 8f, 4f);
        DefinirPosicao(descricao.rectTransform, new Vector2(390f, 15f), new Vector2(760f, 410f));

        DefinirPosicao(atributos.rectTransform, new Vector2(390f, -285f), new Vector2(760f, 145f));

        botaoFechar = CriarBotao("BotaoFecharInfo", janela, Traduzir("CHAR_INFO_CLOSE", "FECHAR"), tamanhoBotaoFechar);
        DefinirPosicao(botaoFechar.GetComponent<RectTransform>(), posicaoBotaoFechar, tamanhoBotaoFechar);
        ConfigurarBotaoFechar();
        AplicarVisualBotao(botaoFechar, corBotaoAcaoNormal);
    }

    bool LocalizarInterfaceExistente()
    {
        // O modal editável pertence à composição nova da seleção. O script
        // PainelInfoPersonagem continua no Canvas por compatibilidade, então
        // não pode procurar apenas um filho direto do Canvas — isso fazia ele
        // criar/usar uma segunda cópia runtime.
        Transform overlayTransform = EncontrarModalDaHierarquia();
        if (overlayTransform == null)
            return false;

        Transform janelaTransform = overlayTransform.Find("JanelaInfoPersonagem");
        Transform previewTransform = janelaTransform != null
            ? janelaTransform.Find("FundoPreview/PreviewAnimado")
            : null;
        Transform alvoTransform = janelaTransform != null
            ? janelaTransform.Find("FundoPreview/AlvoAnimado")
            : null;
        Transform efeitoTransform = janelaTransform != null
            ? janelaTransform.Find("FundoPreview/EfeitoAnimado")
            : null;

        Transform[] acoes = new Transform[4];
        for (int i = 0; i < acoes.Length; i++)
            acoes[i] = janelaTransform != null ? janelaTransform.Find("BotaoAcao" + i) : null;

        Transform fecharTransform = janelaTransform != null
            ? janelaTransform.Find("BotaoFecharInfo")
            : null;

        if (janelaTransform == null || previewTransform == null || efeitoTransform == null ||
            fecharTransform == null || previewTransform.GetComponent<Image>() == null ||
            efeitoTransform.GetComponent<Image>() == null)
            return false;

        for (int i = 0; i < acoes.Length; i++)
        {
            if (acoes[i] == null || acoes[i].GetComponent<Button>() == null)
                return false;
        }

        overlay = overlayTransform.gameObject;
        janela = janelaTransform.GetComponent<RectTransform>();
        imagemPreview = previewTransform.GetComponent<Image>();
        imagemEfeito = efeitoTransform.GetComponent<Image>();
        rectAreaPreview = janelaTransform.Find("FundoPreview").GetComponent<RectTransform>();
        rectEfeito = efeitoTransform.GetComponent<RectTransform>();
        imagemAlvo = alvoTransform != null ? alvoTransform.GetComponent<Image>() : null;
        rectAlvo = alvoTransform != null ? alvoTransform.GetComponent<RectTransform>() : null;
        titulo = ObterTexto(janelaTransform, "TituloInfo");
        subtitulo = ObterTexto(janelaTransform, "SubtituloInfo");
        legendaPreview = ObterTexto(janelaTransform, "LegendaPreview");
        descricao = ObterTexto(janelaTransform, "DescricaoAcao");
        atributos = ObterTexto(janelaTransform, "AtributosPersonagem");
        botaoFechar = fecharTransform.GetComponent<Button>();
        botoesAcoes = new Button[acoes.Length];
        for (int i = 0; i < acoes.Length; i++)
            botoesAcoes[i] = acoes[i].GetComponent<Button>();

        return titulo != null && subtitulo != null && descricao != null && atributos != null;
    }

    Transform EncontrarModalDaHierarquia()
    {
        Transform direto = transform.Find("ModalInfoPersonagem");
        if (direto != null)
            return direto;

        // Compatibilidade para uma cena em que o modal tenha sido colocado
        // dentro da composição refatorada.
        Transform layout = transform.Find("SelecaoPlayerRefatorada");
        if (layout != null)
        {
            Transform modal = layout.Find("ModalInfoPersonagem");
            if (modal != null)
                return modal;
        }

        foreach (Transform filho in GetComponentsInChildren<Transform>(true))
        {
            if (filho != null && filho.name == "ModalInfoPersonagem")
                return filho;
        }

        return null;
    }

    void ConfigurarInterfaceExistente()
    {
        Image fundoJanela = janela.GetComponent<Image>();
        if (fundoJanela != null)
        {
            if (spriteFundoModal != null)
            {
                fundoJanela.sprite = spriteFundoModal;
                fundoJanela.type = Image.Type.Simple;
                fundoJanela.color = Color.white;
            }
            else if (fundoJanela.sprite == null)
            {
                fundoJanela.color = corFundoModal;
            }
        }

        imagemPreview.preserveAspect = true;
        imagemPreview.raycastTarget = false;
        imagemEfeito.preserveAspect = true;
        imagemEfeito.raycastTarget = false;
        if (imagemAlvo != null)
        {
            imagemAlvo.preserveAspect = true;
            imagemAlvo.raycastTarget = false;
        }
        else
        {
            Transform fundoPreview = janela.Find("FundoPreview");
            if (fundoPreview != null)
            {
                GameObject alvo = CriarObjetoUI("AlvoAnimado", fundoPreview);
                imagemAlvo = alvo.AddComponent<Image>();
                imagemAlvo.preserveAspect = true;
                imagemAlvo.raycastTarget = false;
                rectAlvo = alvo.GetComponent<RectTransform>();
                DefinirPosicao(rectAlvo, posicaoAlvoPreview, tamanhoAlvoPreview);
                alvo.SetActive(false);
            }
        }

        descricao.enableWordWrapping = true;
        descricao.enableAutoSizing = true;
        descricao.fontSizeMin = 28f;
        descricao.fontSizeMax = tamanhoFonteDescricao;
        descricao.lineSpacing = -6f;
        descricao.margin = new Vector4(8f, 4f, 8f, 4f);
        atributos.enableAutoSizing = true;
        atributos.fontSizeMin = 22f;
        atributos.fontSizeMax = Mathf.Min(tamanhoFonteAtributos, 34f);
        atributos.lineSpacing = -8f;
        atributos.margin = new Vector4(8f, 4f, 8f, 4f);

        for (int i = 0; i < botoesAcoes.Length; i++)
        {
            int indiceBotao = i;
            AplicarVisualBotao(botoesAcoes[i], corBotaoAcaoNormal);
            botoesAcoes[i].onClick.AddListener(() => SelecionarAcao((AcaoInfo)indiceBotao));
        }

        ConfigurarBotaoFechar();
        AplicarVisualBotao(botaoFechar, corBotaoAcaoNormal);
    }

    void GarantirGraficoAtributos()
    {
        if (atributos == null)
            return;

        // O texto antigo continua na Hierarchy para não quebrar referências,
        // mas deixa de renderizar. O gráfico ocupa exatamente a mesma área.
        atributos.enabled = false;
        atributos.text = string.Empty;

        Transform existente = atributos.transform.Find("GraficoAtributosPersonagem");
        if (existente != null)
        {
            graficoAtributos = existente.GetComponent<RectTransform>();
            rotulosGraficoAtributos = new TextMeshProUGUI[4];
            segmentosGraficoAtributos = new RawImage[4, quantidadeSegmentosGrafico];
            for (int linha = 0; linha < 4; linha++)
            {
                rotulosGraficoAtributos[linha] = existente.Find("RotuloGraficoAtributo" + linha)
                    ?.GetComponent<TextMeshProUGUI>();
                for (int segmento = 0; segmento < quantidadeSegmentosGrafico; segmento++)
                {
                    segmentosGraficoAtributos[linha, segmento] = existente.Find(
                        "BarraGraficoAtributo" + linha + "_" + segmento)?.GetComponent<RawImage>();
                }
            }
            AtualizarRotulosGraficoAtributos();
            return;
        }

        GameObject grafico = CriarObjetoUI("GraficoAtributosPersonagem", atributos.transform);
        graficoAtributos = grafico.GetComponent<RectTransform>();
        DefinirEsticado(graficoAtributos);

        int segmentos = Mathf.Max(1, quantidadeSegmentosGrafico);
        rotulosGraficoAtributos = new TextMeshProUGUI[4];
        segmentosGraficoAtributos = new RawImage[4, segmentos];

        float largura = Mathf.Max(300f, atributos.rectTransform.sizeDelta.x);
        float altura = Mathf.Max(150f, atributos.rectTransform.sizeDelta.y);
        float larguraRotulo = Mathf.Min(132f, largura * 0.38f);
        float larguraBarra = segmentos * tamanhoSegmentoGrafico.x +
            Mathf.Max(0, segmentos - 1) * espacamentoSegmentoGrafico;
        float esquerda = -largura * 0.5f + 4f;
        float centroRotulo = esquerda + larguraRotulo * 0.5f;
        float centroBarra = esquerda + larguraRotulo + 12f + larguraBarra * 0.5f;
        float espacamentoLinha = Mathf.Min(36f, (altura - 16f) / 4f);
        float primeiraLinha = (3f * espacamentoLinha) * 0.5f;

        for (int linha = 0; linha < 4; linha++)
        {
            float y = primeiraLinha - linha * espacamentoLinha;
            rotulosGraficoAtributos[linha] = CriarTexto(
                "RotuloGraficoAtributo" + linha,
                grafico.transform,
                tamanhoFonteGraficoAtributos,
                Color.white,
                TextAlignmentOptions.MidlineLeft);
            rotulosGraficoAtributos[linha].fontStyle = FontStyles.Bold;
            rotulosGraficoAtributos[linha].overflowMode = TextOverflowModes.Ellipsis;
            DefinirPosicao(rotulosGraficoAtributos[linha].rectTransform,
                new Vector2(centroRotulo, y), new Vector2(larguraRotulo, espacamentoLinha));

            for (int segmento = 0; segmento < segmentos; segmento++)
            {
                GameObject objetoSegmento = CriarObjetoUI(
                    "BarraGraficoAtributo" + linha + "_" + segmento,
                    grafico.transform);
                RawImage imagemSegmento = objetoSegmento.AddComponent<RawImage>();
                imagemSegmento.texture = Texture2D.whiteTexture;
                imagemSegmento.raycastTarget = false;
                float x = centroBarra - larguraBarra * 0.5f +
                    tamanhoSegmentoGrafico.x * 0.5f +
                    segmento * (tamanhoSegmentoGrafico.x + espacamentoSegmentoGrafico);
                DefinirPosicao(imagemSegmento.rectTransform, new Vector2(x, y), tamanhoSegmentoGrafico);
                segmentosGraficoAtributos[linha, segmento] = imagemSegmento;
            }
        }

        AtualizarRotulosGraficoAtributos();
    }

    void AtualizarRotulosGraficoAtributos()
    {
        if (rotulosGraficoAtributos == null || rotulosGraficoAtributos.Length < 4)
            return;

        string[] chaves =
        {
            "CHAR_STAT_FORCE",
            "CHAR_SPEED",
            "CHAR_RANGE",
            "CHAR_STAT_ENERGY"
        };
        string[] padroes = { "Força", "Velocidade", "Alcance", "Energia" };
        for (int i = 0; i < rotulosGraficoAtributos.Length; i++)
        {
            if (rotulosGraficoAtributos[i] != null)
                rotulosGraficoAtributos[i].text = Traduzir(chaves[i], padroes[i]).ToUpperInvariant();
        }
    }

    void AtualizarGraficoAtributos()
    {
        if (dadosAtuais == null || segmentosGraficoAtributos == null)
            return;

        float[] valores =
        {
            Mathf.Clamp01(dadosAtuais.danoAtaque / 20f),
            Mathf.Clamp01(dadosAtuais.velocidade / 12f),
            Mathf.Clamp01(dadosAtuais.alcanceAtaque / 4f),
            Mathf.Clamp01(dadosAtuais.velocidadeRecargaEnergia / 15f)
        };
        Color[] cores =
        {
            new Color(0.93f, 0.10f, 0.13f, 1f),
            new Color(0.93f, 0.10f, 0.13f, 1f),
            new Color(0.05f, 0.78f, 0.32f, 1f),
            new Color(1f, 0.78f, 0.03f, 1f)
        };
        Color corVazia = new Color(0.12f, 0.18f, 0.26f, 1f);
        int segmentos = segmentosGraficoAtributos.GetLength(1);

        for (int linha = 0; linha < 4; linha++)
        {
            int preenchidos = Mathf.Clamp(Mathf.RoundToInt(valores[linha] * segmentos), 0, segmentos);
            for (int segmento = 0; segmento < segmentos; segmento++)
            {
                RawImage imagem = segmentosGraficoAtributos[linha, segmento];
                if (imagem != null)
                    imagem.color = segmento < preenchidos ? cores[linha] : corVazia;
            }
        }
    }

    void ConfigurarBotaoFechar()
    {
        if (botaoFechar == null)
            return;

        // O botão é persistente na Hierarchy, então a ação precisa ser
        // reassociada ao iniciar para continuar funcionando mesmo depois de
        // alterações feitas no Inspector ou no Scene View.
        botaoFechar.onClick.RemoveAllListeners();
        botaoFechar.onClick.AddListener(Fechar);
        botaoFechar.interactable = true;

        Image imagem = botaoFechar.targetGraphic as Image;
        if (imagem == null)
        {
            imagem = botaoFechar.GetComponent<Image>();
            botaoFechar.targetGraphic = imagem;
        }

        if (imagem != null)
            imagem.raycastTarget = true;

        FecharAoClique clique = botaoFechar.GetComponent<FecharAoClique>();
        if (clique == null)
            clique = botaoFechar.gameObject.AddComponent<FecharAoClique>();
        clique.Configurar(this);
    }

    TextMeshProUGUI ObterTexto(Transform pai, string nome)
    {
        Transform filho = pai.Find(nome);
        return filho != null ? filho.GetComponent<TextMeshProUGUI>() : null;
    }

    void CriarBotoesDeInfo()
    {
        botoesInfo = new Button[2];
        botoesInfoUsamPosicaoAutomatica = new bool[2];
        Transform botaoP1Existente = EncontrarBotaoInfo("BotaoInfoP1", "InfoIP1");
        Transform botaoP2Existente = EncontrarBotaoInfo("BotaoInfoP2", "InfoIP2");
        // A tela nova já possui os botões I editáveis. Não criar BotaoInfoP1
        // ou BotaoInfoP2 antigos como fallback.
        botoesInfoUsamPosicaoAutomatica[0] = false;
        botoesInfoUsamPosicaoAutomatica[1] = false;
        botoesInfo[0] = botaoP1Existente != null
            ? botaoP1Existente.GetComponent<Button>()
            : null;
        botoesInfo[1] = botaoP2Existente != null
            ? botaoP2Existente.GetComponent<Button>()
            : null;
        if (botoesInfo[0] != null)
            botoesInfo[0].onClick.AddListener(() => AbrirParaLado(1));
        if (botoesInfo[1] != null)
            botoesInfo[1].onClick.AddListener(() => AbrirParaLado(2));

        // Os botões InfoI da tela refatorada possuem foco próprio: preservam
        // sua cor, mostram borda no hover/foco e usam amarelo ao confirmar.
        // Não instalar neles o visual antigo, que pintava todo o botão no hover.
        if (botoesInfo[0] != null && !botoesInfo[0].gameObject.name.StartsWith("InfoI"))
            AplicarVisualBotao(botoesInfo[0], corBotaoInfoNormal);
        if (botoesInfo[1] != null && !botoesInfo[1].gameObject.name.StartsWith("InfoI"))
            AplicarVisualBotao(botoesInfo[1], corBotaoInfoNormal);
    }

    Transform EncontrarBotaoInfo(string nomeOriginal, string nomeLayout)
    {
        // Primeiro procura dentro da tela refatorada. Os objetos diretos no
        // Canvas são restos da versão antiga e não podem receber o hover.
        foreach (Button botao in GetComponentsInChildren<Button>(true))
        {
            if (botao != null && botao.gameObject.name == nomeLayout)
                return botao.transform;
        }

        // Se a tela nova existe, não reutilize os botões antigos do Canvas.
        // Isso evita duplicação e mantém o botão I editável no card correto.
        if (transform.Find("SelecaoPlayerRefatorada") != null)
            return null;

        Transform direto = transform.Find(nomeOriginal);
        if (direto != null)
            return direto;

        return null;
    }

    void AtualizarPosicaoDosBotoesInfo()
    {
        if (botoesInfo == null || telaSelecao == null)
            return;

        if (botoesInfoUsamPosicaoAutomatica != null)
        {
            if (botoesInfoUsamPosicaoAutomatica[0])
                PosicionarBotaoInfo(botoesInfo[0], telaSelecao.nomePlayer1 != null ? telaSelecao.nomePlayer1.rectTransform : null, 1);
            if (botoesInfoUsamPosicaoAutomatica[1])
                PosicionarBotaoInfo(botoesInfo[1], telaSelecao.nomePlayer2 != null ? telaSelecao.nomePlayer2.rectTransform : null, 2);
        }
        AtualizarBotoesInfo();
    }

    void PosicionarBotaoInfo(Button botao, RectTransform alvo, int lado)
    {
        if (botao == null || alvo == null)
            return;

        RectTransform rect = botao.GetComponent<RectTransform>();
        rect.position = alvo.position + new Vector3(0f, deslocamentoVerticalBotaoInfo, 0f);
    }

    void AtualizarBotoesInfo()
    {
        if (botoesInfo == null)
            return;

        for (int i = 0; i < botoesInfo.Length; i++)
        {
            if (botoesInfo[i] == null)
                continue;

            int lado = i + 1;
            bool valido = telaSelecao != null && dadosPersonagens != null
                       && telaSelecao.ObterDadosPersonagem(telaSelecao.ObterIndiceParaInfo(lado)) != null;

            botoesInfo[i].gameObject.SetActive(!aberto && valido);
        }
    }

    void AtualizarCabecalho()
    {
        string nome = !string.IsNullOrEmpty(dadosAtuais.nomePersonagem) ? dadosAtuais.nomePersonagem : "PERSONAGEM";
        titulo.text = Traduzir("CHAR_INFO", "INFORMAÇÕES DO PERSONAGEM") + "  —  " + nome.ToUpper();
        subtitulo.text = Traduzir("CHAR_INFO_HINT", "Passe pelos ataques para ver os frames em ação");
    }

    void SelecionarAcao(AcaoInfo acao)
    {
        if (dadosAtuais == null)
            return;

        CapturarPosicaoPersonagemDaHierarquia();
        acaoAtual = acao;
        if (imagemPreview != null)
        {
            imagemPreview.rectTransform.anchoredPosition = ObterPosicaoPersonagemBase();
            AtualizarEscalaPreviewDaAcao();
        }

        tempoFrame = 0f;
        tempoFrameAlvo = 0f;
        tempoPreviewUnico = 0f;
        frameAtual = 0;
        frameAlvoAtual = 0;
        etapaPreviewUnico = 0;
        efeitoImpactoAtivo = false;
        previewEspecialSalto = acao == AcaoInfo.Especial && dadosAtuais.spriteRachadura != null;
        previewUltimateLava = acao == AcaoInfo.Ultimate && dadosAtuais.spriteLavaUltimate != null;
        previewUltimateRaio = acao == AcaoInfo.Ultimate && dadosAtuais.spriteRaioUltimate != null;
        posicaoPersonagemBase = ObterPosicaoPersonagemBase();
        duracaoSaltoPreview = 0f;
        alturaSaltoPreview = 0f;
        framesAtuais = ObterFrames(acao, out fpsAtual);
        if (framesAtuais == null || framesAtuais.Length == 0)
        {
            framesAtuais = new[] { dadosAtuais.spriteCorpo };
            if (dadosAtuais.framesIdle != null && dadosAtuais.framesIdle.Length > 0)
            {
                framesAtuais = dadosAtuais.framesIdle;
                fpsAtual = dadosAtuais.fpsIdle;
            }
        }

        if (framesAtuais.Length > 0)
            imagemPreview.sprite = framesAtuais[0];

        if (imagemPreview != null)
        {
            imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase;
            AtualizarEscalaPreviewDaAcao();
        }

        AtualizarAlvoDaAcao();

        for (int i = 0; i < botoesAcoes.Length; i++)
        {
            bool selecionado = i == (int)acao;
            DefinirBotaoSelecionado(botoesAcoes[i], selecionado);
        }

        if (aberto && botoesAcoes[(int)acao].gameObject != null)
            UIFocusUtility.Select(botoesAcoes[(int)acao].gameObject);

        AtualizarEfeitoDaAcao();
        AtualizarTextoDaAcao();
    }

    void AtualizarAlvoDaAcao()
    {
        if (imagemAlvo == null)
            return;

        framesAlvo = null;
        tempoFrameAlvo = 0f;
        frameAlvoAtual = 0;
        DadosPersonagem dadosAlvo = null;

        // O ultimate do Diego mostra o Jamanta como alvo para deixar claro onde a
        // lava aparece. O alvo fica em um objeto separado, então nunca cobre o
        // personagem que está executando a animação.
        if (acaoAtual == AcaoInfo.Ultimate && dadosAtuais.spriteLavaUltimate != null)
            dadosAlvo = ObterDadosJamanta();

        if (dadosAlvo != null)
        {
            framesAlvo = dadosAlvo.framesIdle != null && dadosAlvo.framesIdle.Length > 0
                ? dadosAlvo.framesIdle
                : new[] { dadosAlvo.spriteCorpo };
            fpsAlvo = dadosAlvo.fpsIdle;
        }

        bool ativo = framesAlvo != null && framesAlvo.Length > 0 && framesAlvo[0] != null;
        imagemAlvo.gameObject.SetActive(ativo);
        imagemAlvo.color = Color.white;
        if (ativo)
        {
            imagemAlvo.sprite = framesAlvo[0];
            if (rectAlvo != null && rectAlvo.sizeDelta.sqrMagnitude < 1f)
                DefinirPosicao(rectAlvo, posicaoAlvoPreview, tamanhoAlvoPreview);
        }
    }

    DadosPersonagem ObterDadosJamanta()
    {
        if (dadosPersonagens == null)
            return null;

        DadosPersonagem alternativa = null;
        foreach (DadosPersonagem dados in dadosPersonagens)
        {
            if (dados == null || dados == dadosAtuais)
                continue;

            if (!string.IsNullOrEmpty(dados.nomePersonagem) &&
                dados.nomePersonagem.ToUpperInvariant().Contains("JAMANTA"))
                return dados;

            if (alternativa == null)
                alternativa = dados;
        }

        return alternativa;
    }

    Sprite[] ObterFrames(AcaoInfo acao, out float fps)
    {
        fps = dadosAtuais.fpsIdle;
        switch (acao)
        {
            case AcaoInfo.Ataque:
                fps = dadosAtuais.fpsAttack;
                return dadosAtuais.framesAttack;
            case AcaoInfo.Especial:
                if (dadosAtuais.especialAlternavelComQueimadura &&
                    dadosAtuais.framesIdleFogo != null && dadosAtuais.framesIdleFogo.Length > 0)
                {
                    fps = dadosAtuais.fpsIdle;
                    return dadosAtuais.framesIdleFogo;
                }
                fps = dadosAtuais.fpsSpecial;
                return dadosAtuais.framesSpecial;
            case AcaoInfo.Ultimate:
                fps = dadosAtuais.fpsUltimate;
                return dadosAtuais.framesUltimate;
            case AcaoInfo.Defesa:
                fps = dadosAtuais.fpsDefend;
                return dadosAtuais.framesDefend;
            default:
                return dadosAtuais.framesIdle;
        }
    }

    void AtualizarEfeitoDaAcao()
    {
        if (imagemEfeito == null || dadosAtuais == null)
            return;

        Sprite spriteEfeito = null;
        Vector2 posicao = posicaoEfeito;
        Vector2 tamanhoMaximo = tamanhoEfeito;
        bool mostrarEfeito = false;
        Vector2 posicaoPersonagem = imagemPreview != null
            ? imagemPreview.rectTransform.anchoredPosition
            : posicaoPersonagemPreview;
        Vector2 tamanhoPersonagem = imagemPreview != null
            ? imagemPreview.rectTransform.sizeDelta
            : tamanhoImagemPreview;
        Vector2 posicaoAlvo = rectAlvo != null
            ? rectAlvo.anchoredPosition
            : posicaoAlvoPreview;
        Vector2 tamanhoAlvo = rectAlvo != null
            ? rectAlvo.sizeDelta
            : tamanhoAlvoPreview;
        Vector2 tamanhoPalco = rectAreaPreview != null
            ? rectAreaPreview.sizeDelta
            : tamanhoAreaPreview;

        if (acaoAtual == AcaoInfo.Especial && dadosAtuais.spriteRachadura != null)
        {
            spriteEfeito = dadosAtuais.spriteRachadura;
            // A rachadura fica no chão, abaixo do personagem, e entra só depois
            // dos frames de salto/queda do especial.
            posicao = new Vector2(posicaoPersonagem.x,
                posicaoPersonagem.y - tamanhoPersonagem.y * 0.46f);
            tamanhoMaximo = new Vector2(Mathf.Min(tamanhoPalco.x * 0.58f, tamanhoEfeito.x), tamanhoEfeito.y);
            mostrarEfeito = efeitoImpactoAtivo;
        }
        else if (acaoAtual == AcaoInfo.Ultimate && dadosAtuais.spriteLavaUltimate != null)
        {
            spriteEfeito = dadosAtuais.spriteLavaUltimate;
            posicao = new Vector2(posicaoAlvo.x,
                posicaoAlvo.y - tamanhoAlvo.y * 0.44f);
            tamanhoMaximo = tamanhoEfeito;
            mostrarEfeito = efeitoImpactoAtivo;
        }
        else if (acaoAtual == AcaoInfo.Ultimate && dadosAtuais.spriteRaioUltimate != null)
        {
            spriteEfeito = dadosAtuais.spriteRaioUltimate;
            // O combate nasce na altura real da mão/arma, usando a fração
            // configurada no DadosPersonagem. O ponto X é o centro do lutador,
            // igual ao OrigemDoRaioUltimate() da luta; posicaoRaio permite
            // ajustar frente/trás e altura sem mover o frame do personagem.
            Vector2 tamanhoDesenhadoPersonagem = ObterTamanhoDesenhado(imagemPreview);
            float fracaoAltura = Mathf.Clamp01(dadosAtuais.fracaoAlturaRaioUltimate);
            float origemX = posicaoPersonagem.x + posicaoRaio.x;
            float origemY = posicaoPersonagem.y - tamanhoDesenhadoPersonagem.y * 0.5f
                          + tamanhoDesenhadoPersonagem.y * fracaoAltura
                          + posicaoRaio.y;

            // O sprite do raio é quadrado (64x64), mas no combate ele é
            // esticado no eixo X. No modal fazemos a mesma coisa e deixamos a
            // máscara do palco limitar somente a ponta final.
            float larguraRaio = Mathf.Max(320f, tamanhoPalco.x - 30f);
            float alturaRaio = Mathf.Min(tamanhoRaio.y, tamanhoPalco.y);
            posicao = new Vector2(origemX + larguraRaio * 0.5f, origemY);
            tamanhoMaximo = new Vector2(larguraRaio, alturaRaio);
            mostrarEfeito = efeitoImpactoAtivo;
        }

        imagemEfeito.sprite = spriteEfeito;
        imagemEfeito.gameObject.SetActive(spriteEfeito != null && mostrarEfeito);
        if (imagemAlvo != null && imagemAlvo.gameObject.activeSelf)
            imagemAlvo.color = previewUltimateLava && efeitoImpactoAtivo
                ? CorAlvoQueimandoPreview
                : Color.white;

        if (spriteEfeito != null && mostrarEfeito)
        {
            bool esticarRaio = acaoAtual == AcaoInfo.Ultimate && dadosAtuais.spriteRaioUltimate != null;
            imagemEfeito.preserveAspect = !esticarRaio;
            Vector2 tamanhoVisual = esticarRaio
                ? tamanhoMaximo
                : AjustarTamanhoDoSprite(spriteEfeito, tamanhoMaximo);
            DefinirPosicao(rectEfeito, posicao, tamanhoVisual);
            imagemEfeito.color = Color.white;
        }
    }

    Vector2 ObterTamanhoDesenhado(Image imagem)
    {
        if (imagem == null || imagem.sprite == null)
            return tamanhoImagemPreview;

        RectTransform rect = imagem.rectTransform;
        float largura = Mathf.Abs(rect.rect.width);
        float altura = Mathf.Abs(rect.rect.height);
        if (!imagem.preserveAspect || largura <= 0.01f || altura <= 0.01f)
            return new Vector2(largura, altura);

        float proporcaoSprite = imagem.sprite.rect.width / imagem.sprite.rect.height;
        float proporcaoRect = largura / altura;
        if (proporcaoRect > proporcaoSprite)
            return new Vector2(altura * proporcaoSprite, altura);

        return new Vector2(largura, largura / proporcaoSprite);
    }

    Vector2 AjustarTamanhoDoSprite(Sprite sprite, Vector2 limite)
    {
        if (sprite == null || sprite.rect.height <= 0f || sprite.rect.width <= 0f)
            return limite;

        float proporcao = sprite.rect.width / sprite.rect.height;
        float largura = limite.x;
        float altura = largura / proporcao;
        if (altura > limite.y)
        {
            altura = limite.y;
            largura = altura * proporcao;
        }

        return new Vector2(largura, altura);
    }

    void AtualizarAnimacaoPreview()
    {
        if (imagemPreview == null || framesAtuais == null || framesAtuais.Length == 0)
            return;

        float delta = Time.unscaledDeltaTime;
        AtualizarFrameAlvo(delta);

        if (previewEspecialSalto)
        {
            AtualizarPreviewEspecialSalto(delta);
            return;
        }

        if (previewUltimateLava)
        {
            AtualizarPreviewUltimateLava(delta);
            return;
        }

        if (previewUltimateRaio)
        {
            AtualizarPreviewUltimateRaio(delta);
            return;
        }

        AtualizarPreviewPadrao(delta);
    }

    void AtualizarPreviewPadrao(float delta)
    {
        if (framesAtuais.Length <= 1)
            return;

        tempoFrame += delta;
        float fps = fpsAtual > 0f ? fpsAtual : 6f;
        float intervalo = 1f / Mathf.Max(0.01f, fps);
        if (tempoFrame < intervalo)
            return;

        tempoFrame -= intervalo;
        frameAtual = (frameAtual + 1) % framesAtuais.Length;
        imagemPreview.sprite = framesAtuais[frameAtual];
        AtualizarEfeitoDaAcao();
    }

    void AtualizarFrameAlvo(float delta)
    {
        if (imagemAlvo == null || !imagemAlvo.gameObject.activeSelf || framesAlvo == null || framesAlvo.Length == 0)
            return;

        if (framesAlvo.Length <= 1)
            return;

        tempoFrameAlvo += delta;
        float fps = fpsAlvo > 0f ? fpsAlvo : 6f;
        float intervalo = 1f / Mathf.Max(0.01f, fps);
        if (tempoFrameAlvo < intervalo)
            return;

        tempoFrameAlvo -= intervalo;
        frameAlvoAtual = (frameAlvoAtual + 1) % framesAlvo.Length;
        imagemAlvo.sprite = framesAlvo[frameAlvoAtual];
    }

    void AtualizarPreviewEspecialSalto(float delta)
    {
        float fps = fpsAtual > 0f ? fpsAtual : 8f;
        float intervaloFrame = 1f / Mathf.Max(0.01f, fps);

        if (etapaPreviewUnico == 0)
        {
            tempoPreviewUnico += delta;
            int frameWindup = Mathf.Min(
                Mathf.FloorToInt(tempoPreviewUnico / intervaloFrame),
                Mathf.Max(0, framesAtuais.Length - 1));
            AplicarSpritePreview(framesAtuais, frameWindup);
            imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase;

            float duracaoWindup = intervaloFrame * Mathf.Min(2, Mathf.Max(1, framesAtuais.Length));
            if (tempoPreviewUnico >= duracaoWindup)
            {
                etapaPreviewUnico = 1;
                tempoPreviewUnico = 0f;
                CalcularSaltoPreview();
            }

            efeitoImpactoAtivo = false;
            AtualizarEfeitoDaAcao();
            return;
        }

        if (etapaPreviewUnico == 1)
        {
            tempoPreviewUnico += delta;
            float progresso = Mathf.Clamp01(tempoPreviewUnico / Mathf.Max(0.01f, duracaoSaltoPreview));
            Sprite[] framesJump = dadosAtuais.framesJump;
            int frameJump = progresso < 0.5f ? 0 : 1;
            AplicarSpritePreview(framesJump != null && framesJump.Length > 0 ? framesJump : framesAtuais, frameJump);

            float arco = 4f * progresso * (1f - progresso);
            imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase + Vector2.up * (alturaSaltoPreview * arco);
            efeitoImpactoAtivo = false;
            AtualizarEfeitoDaAcao();

            if (progresso >= 1f)
            {
                etapaPreviewUnico = 2;
                tempoPreviewUnico = 0f;
                efeitoImpactoAtivo = true;
                imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase;
                AplicarSpritePreview(framesAtuais, Mathf.Min(1, framesAtuais.Length - 1));
                AtualizarEfeitoDaAcao();
            }
            return;
        }

        // O jogo mostra a pose de aterrissagem enquanto a rachadura nasce no
        // mesmo instante. Depois da pose, o preview reinicia como um vídeo.
        tempoPreviewUnico += delta;
        imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase;
        if (tempoPreviewUnico < intervaloFrame)
            AplicarSpritePreview(framesAtuais, Mathf.Min(1, framesAtuais.Length - 1));
        else if (tempoPreviewUnico < intervaloFrame * 2f)
            AplicarSpritePreview(framesAtuais, 0);
        else
        {
            etapaPreviewUnico = 0;
            tempoPreviewUnico = 0f;
            efeitoImpactoAtivo = false;
            AplicarSpritePreview(framesAtuais, 0);
        }

        AtualizarEfeitoDaAcao();
    }

    void CalcularSaltoPreview()
    {
        // Os lutadores da cena de combate usam Gravity Scale = 3. A prévia não
        // tem um Rigidbody2D próprio, então reproduzimos essa mesma gravidade
        // para o tempo de subida/queda não ficar artificialmente longo.
        float gravidade = Mathf.Abs(Physics2D.gravity.y) * 3f;
        float impulso = dadosAtuais.forcaPulo * Mathf.Max(1f, dadosAtuais.multiplicadorAlturaSaltoRachadura);
        duracaoSaltoPreview = gravidade > 0.01f ? (2f * impulso / gravidade) : 1f;
        duracaoSaltoPreview = Mathf.Max(0.5f, duracaoSaltoPreview);

        float alturaPalco = rectAreaPreview != null ? rectAreaPreview.rect.height : tamanhoAreaPreview.y;
        float alturaPersonagem = imagemPreview != null ? imagemPreview.rectTransform.rect.height : tamanhoImagemPreview.y;
        alturaSaltoPreview = Mathf.Min(alturaPalco * 0.28f, alturaPersonagem * 1.25f);
    }

    void AtualizarPreviewUltimateLava(float delta)
    {
        float fps = fpsAtual > 0f ? fpsAtual : 10f;
        float intervaloFrame = 1f / Mathf.Max(0.01f, fps);
        int frameImpacto = Mathf.Clamp(dadosAtuais.frameCravarLavaUltimate, 0, framesAtuais.Length - 1);

        if (etapaPreviewUnico == 0)
        {
            tempoPreviewUnico += delta;
            int frame = Mathf.Min(Mathf.FloorToInt(tempoPreviewUnico / intervaloFrame), framesAtuais.Length - 1);
            AplicarSpritePreview(framesAtuais, frame);
            efeitoImpactoAtivo = frame >= frameImpacto;
            AtualizarEfeitoDaAcao();

            if (frame >= framesAtuais.Length - 1)
            {
                etapaPreviewUnico = 1;
                tempoPreviewUnico = 0f;
                efeitoImpactoAtivo = true;
                AtualizarEfeitoDaAcao();
            }
            return;
        }

        // No combate o Diego fica preso na pose da espada cravada enquanto a
        // lava permanece ativa. O preview mantém exatamente esse intervalo.
        tempoPreviewUnico += delta;
        efeitoImpactoAtivo = true;
        AplicarSpritePreview(framesAtuais, framesAtuais.Length - 1);
        AtualizarEfeitoDaAcao();

        if (tempoPreviewUnico >= Mathf.Max(0f, dadosAtuais.duracaoVisualLavaUltimate))
        {
            etapaPreviewUnico = 0;
            tempoPreviewUnico = 0f;
            efeitoImpactoAtivo = false;
            AplicarSpritePreview(framesAtuais, 0);
            AtualizarEfeitoDaAcao();
        }
    }

    void AtualizarPreviewUltimateRaio(float delta)
    {
        // Apenas o efeito do raio recebe o ajuste de alinhamento. O frame do
        // personagem permanece exatamente na posição definida na Hierarchy.
        imagemPreview.rectTransform.anchoredPosition = posicaoPersonagemBase;

        float fps = fpsAtual > 0f ? fpsAtual : 10f;
        float intervaloFrame = 1f / Mathf.Max(0.01f, fps);
        int frameLancamento = Mathf.Clamp(dadosAtuais.frameLancamentoRaioUltimate, 0, framesAtuais.Length - 1);
        float tempoLancamento = frameLancamento * intervaloFrame;
        float duracaoRaio = Mathf.Max(0.01f,
            dadosAtuais.duracaoCrescimentoRaio +
            dadosAtuais.delayRaioUltimate +
            dadosAtuais.duracaoVisualRaioUltimate +
            dadosAtuais.duracaoEnfraquecimentoRaio);

        tempoPreviewUnico += delta;
        int frame = Mathf.Min(Mathf.FloorToInt(tempoPreviewUnico / intervaloFrame), framesAtuais.Length - 1);
        AplicarSpritePreview(framesAtuais, frame);
        efeitoImpactoAtivo = tempoPreviewUnico >= tempoLancamento;
        AtualizarEfeitoDaAcao();

        // O jogo mantém o ciclo do raio até terminar crescimento, impacto e
        // enfraquecimento; só então a prévia começa novamente.
        if (tempoPreviewUnico >= tempoLancamento + duracaoRaio)
        {
            tempoPreviewUnico = 0f;
            etapaPreviewUnico = 0;
            efeitoImpactoAtivo = false;
            AplicarSpritePreview(framesAtuais, 0);
            AtualizarEfeitoDaAcao();
        }
    }

    void AplicarSpritePreview(Sprite[] frames, int indice)
    {
        if (imagemPreview == null || frames == null || frames.Length == 0)
            return;

        int indiceSeguro = Mathf.Clamp(indice, 0, frames.Length - 1);
        frameAtual = indiceSeguro;
        if (frames[indiceSeguro] != null)
            imagemPreview.sprite = frames[indiceSeguro];
    }

    void AtualizarTextoDaAcao()
    {
        string nomeAcao;
        string textoAcao;
        string detalhesAcao;

        switch (acaoAtual)
        {
            case AcaoInfo.Especial:
                nomeAcao = Traduzir("CHAR_SPECIAL", "ESPECIAL");
                textoAcao = dadosAtuais.especialAlternavelComQueimadura
                    ? Traduzir("CHAR_BURNING_SWORD_DESC", "Espada em chamas: ativa queimação nos golpes.")
                    : dadosAtuais.spriteRachadura != null
                        ? Traduzir("CHAR_CRACK_DESC", "Controle de área: salto + rachadura no chão.")
                        : Traduzir("CHAR_SPECIAL_DESC", "Habilidade especial com efeito único.");
                detalhesAcao =
                    Traduzir("CHAR_DAMAGE", "Dano") + ": " + dadosAtuais.danoEspecial + "     " +
                    Traduzir("CHAR_RANGE", "Alcance") + ": " + Numero(dadosAtuais.alcanceEspecial) + "\n" +
                    Traduzir("CHAR_COST", "Custo") + ": " + Numero(dadosAtuais.custoEspecial) + "     " +
                    Traduzir("CHAR_SPEND", "Gasto") + ": " + TraduzirGastoEspecial(dadosAtuais.tipoGastoEspecial);
                break;

            case AcaoInfo.Ultimate:
                nomeAcao = Traduzir("CHAR_ULTIMATE", "ULTIMATE");
                textoAcao = dadosAtuais.spriteLavaUltimate != null
                    ? Traduzir("CHAR_LAVA_DESC", "Espada cravada: lava no ponto de impacto.")
                    : dadosAtuais.spriteRaioUltimate != null
                        ? Traduzir("CHAR_RAY_DESC", "Raio concentrado com impacto e recuo.")
                        : Traduzir("CHAR_ULTIMATE_DESC", "Golpe máximo para virar a luta.");
                detalhesAcao =
                    Traduzir("CHAR_DAMAGE", "Dano") + ": " + dadosAtuais.danoUltimate + "     " +
                    Traduzir("CHAR_RANGE", "Alcance") + ": " + Numero(dadosAtuais.alcanceUltimate) + "\n" +
                    Traduzir("CHAR_COST", "Custo") + ": " + Numero(dadosAtuais.custoUltimate) + "     " +
                    Traduzir("CHAR_USAGE", "Uso") + ": " + TraduzirUsoUltimate(dadosAtuais.tipoUsoUltimate);
                break;

            case AcaoInfo.Defesa:
                nomeAcao = Traduzir("CHAR_DEFENSE", "DEFESA");
                textoAcao = Traduzir("CHAR_DEFENSE_DESC", "Reduz o dano recebido enquanto estiver ativa.");
                detalhesAcao =
                    Traduzir("CHAR_BLOCK", "Bloqueio") + ": " + Mathf.RoundToInt(dadosAtuais.porcentagemBloqueioDefesa * 100f) + "%     " +
                    Traduzir("CHAR_COST", "Custo") + ": " + Numero(dadosAtuais.custoDefesa);
                break;

            default:
                nomeAcao = Traduzir("CHAR_ATTACK", "ATAQUE");
                textoAcao = Traduzir("CHAR_MELEE_DESC", "Golpe direto de curta distância.");
                detalhesAcao =
                    Traduzir("CHAR_DAMAGE", "Dano") + ": " + dadosAtuais.danoAtaque + "     " +
                    Traduzir("CHAR_RANGE", "Alcance") + ": " + Numero(dadosAtuais.alcanceAtaque) + "\n" +
                    Traduzir("CHAR_COST", "Custo") + ": " + Numero(dadosAtuais.custoAtaque);
                break;
        }

        descricao.text = nomeAcao + "\n\n" + textoAcao + "\n\n" + detalhesAcao;
        AtualizarGraficoAtributos();
    }

    string ObterEstilo()
    {
        if (dadosAtuais.spriteRachadura != null)
            return Traduzir("CHAR_AREA_CONTROL", "Controle de área");
        if (dadosAtuais.especialAlternavelComQueimadura || dadosAtuais.velocidade >= 7f)
            return Traduzir("CHAR_MOBILE", "Agressivo e móvel");
        return Traduzir("CHAR_BALANCED", "Equilibrado");
    }

    string Numero(float valor)
    {
        return valor.ToString("0.##");
    }

    string TraduzirGastoEspecial(TipoGastoEspecial tipo)
    {
        switch (tipo)
        {
            case TipoGastoEspecial.BarraCheia:
                return Traduzir("CHAR_SPEND_FULL_BAR", "Barra cheia");
            case TipoGastoEspecial.ZeraTudo:
                return Traduzir("CHAR_SPEND_EMPTY_ALL", "Zera tudo");
            case TipoGastoEspecial.GastoGradual:
                return Traduzir("CHAR_SPEND_GRADUAL", "Gasto gradual");
            default:
                return Traduzir("CHAR_SPEND_FIXED", "Gasto fixo");
        }
    }

    string TraduzirUsoUltimate(TipoUsoUltimate tipo)
    {
        return tipo == TipoUsoUltimate.BarraCheia
            ? Traduzir("CHAR_USAGE_FULL_BAR", "Barra cheia")
            : Traduzir("CHAR_USAGE_FIXED", "Gasto fixo");
    }

    string Traduzir(string chave, string fallback)
    {
        if (LanguageManager.Instance != null)
        {
            string traducao = LanguageManager.Instance.GetText(chave);
            if (!string.IsNullOrEmpty(traducao) && traducao != chave)
                return traducao;
        }

        return fallback;
    }

    GameObject CriarObjetoUI(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);
        return objeto;
    }

    RectTransform CriarPainel(string nome, Transform pai, Vector2 tamanho)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        Image imagem = objeto.AddComponent<Image>();
        imagem.color = new Color(0.035f, 0.06f, 0.13f, 0.98f);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        DefinirPosicao(rect, Vector2.zero, tamanho);
        return rect;
    }

    TextMeshProUGUI CriarTexto(string nome, Transform pai, float tamanhoFonte, Color cor, TextAlignmentOptions alinhamento)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.font = fonteBase;
        texto.fontSize = tamanhoFonte;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.raycastTarget = false;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        return texto;
    }

    Button CriarBotao(string nome, Transform pai, string texto, Vector2 tamanho)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        Image imagem = objeto.AddComponent<Image>();
        if (spriteBotaoAzul != null)
            imagem.sprite = spriteBotaoAzul;
        imagem.color = corBotaoInfoNormal;
        Button botao = objeto.AddComponent<Button>();
        botao.targetGraphic = imagem;
        botao.transition = Selectable.Transition.ColorTint;
        ColorBlock cores = botao.colors;
        cores.normalColor = imagem.color;
        cores.highlightedColor = corBotaoHoverSelecionado;
        cores.pressedColor = corBotaoHoverSelecionado;
        cores.selectedColor = cores.highlightedColor;
        cores.disabledColor = new Color(0.2f, 0.25f, 0.32f, 0.55f);
        botao.colors = cores;

        TextMeshProUGUI textoUI = CriarTexto("Texto" + nome, objeto.transform, tamanhoFonteBotoes, Color.white, TextAlignmentOptions.Center);
        textoUI.text = texto;
        DefinirEsticado(textoUI.rectTransform);
        DefinirPosicao(objeto.GetComponent<RectTransform>(), Vector2.zero, tamanho);
        return botao;
    }

    void AplicarVisualBotao(Button botao, Color corNormal)
    {
        if (botao == null)
            return;

        Image imagem = botao.targetGraphic as Image;

        if (imagem == null || spriteBotaoAzul == null || spriteBotaoAmarelo == null)
        {
            AplicarCoresBotaoFallback(botao, corNormal);
            return;
        }

        VisualBotaoPadrao visual = botao.GetComponent<VisualBotaoPadrao>();
        if (visual == null)
            visual = botao.gameObject.AddComponent<VisualBotaoPadrao>();

        visual.Configurar(imagem, spriteBotaoAzul, spriteBotaoAmarelo, corNormal);
    }

    void DefinirBotaoSelecionado(Button botao, bool selecionado)
    {
        if (botao == null)
            return;

        VisualBotaoPadrao visual = botao.GetComponent<VisualBotaoPadrao>();
        if (visual != null)
        {
            visual.DefinirSelecionado(selecionado);
            return;
        }

        AplicarCoresBotaoFallback(botao, selecionado ? corBotaoHoverSelecionado : corBotaoAcaoNormal);
    }

    void AplicarCoresBotaoFallback(Button botao, Color corNormal)
    {
        Image imagem = botao.targetGraphic as Image;
        if (imagem != null)
            imagem.color = corNormal;

        botao.transition = Selectable.Transition.ColorTint;
        ColorBlock cores = botao.colors;
        cores.normalColor = corNormal;
        cores.highlightedColor = corBotaoHoverSelecionado;
        cores.selectedColor = corBotaoHoverSelecionado;
        cores.pressedColor = corBotaoHoverSelecionado;
        botao.colors = cores;
    }

    private sealed class VisualBotaoPadrao : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Image imagem;
        private Sprite spriteNormal;
        private Sprite spriteDestaque;
        private Color corNormal;
        private bool apontado;
        private bool selecionado;
        private bool pressionado;

        public void Configurar(Image imagemBotao, Sprite normal, Sprite destaque, Color cor)
        {
            imagem = imagemBotao;
            spriteNormal = normal;
            spriteDestaque = destaque;
            corNormal = cor;
            GetComponent<Button>().transition = Selectable.Transition.None;
            AtualizarVisual();
        }

        public void DefinirSelecionado(bool valor)
        {
            selecionado = valor;
            AtualizarVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            apontado = true;

            // O mouse também assume o foco real. Assim, quando o teclado
            // passar para outro botão, este não fica destacado só porque o
            // cursor continua parado sobre ele.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);

            AtualizarVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            apontado = false;
            AtualizarVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selecionado = true;
            AtualizarVisual();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selecionado = false;

            // O EventSystem dispara o Deselect antes de atualizar o objeto
            // atualmente selecionado. Limpar diretamente aqui evita que o
            // hover antigo seja redesenhado e fique junto do novo foco.
            PintarComoNaoDestacado();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressionado = true;
            AtualizarVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressionado = false;
            AtualizarVisual();
        }

        void AtualizarVisual()
        {
            if (imagem == null)
                return;

            bool focoReal = EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject;
            bool destaque = selecionado || (apontado && focoReal) || pressionado;
            imagem.sprite = destaque && spriteDestaque != null ? spriteDestaque : spriteNormal;
            imagem.color = destaque ? Color.white : corNormal;
        }

        void PintarComoNaoDestacado()
        {
            if (imagem == null)
                return;

            imagem.sprite = spriteNormal;
            imagem.color = corNormal;
        }
    }

    private sealed class FecharAoClique : MonoBehaviour, IPointerClickHandler
    {
        private PainelInfoPersonagem painel;

        public void Configurar(PainelInfoPersonagem painelOrigem)
        {
            painel = painelOrigem;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && painel != null)
                painel.Fechar();
        }
    }

    void CriarFaixaDecorativa(Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        GameObject faixa = CriarObjetoUI("FaixaDecorativa", pai);
        Image imagem = faixa.AddComponent<Image>();
        imagem.color = new Color(0.95f, 0.78f, 0.3f, 0.9f);
        DefinirPosicao(faixa.GetComponent<RectTransform>(), posicao, tamanho);
    }

    void DefinirEsticado(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void DefinirPosicao(RectTransform rect, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        rect.localScale = Vector3.one;
    }
}
