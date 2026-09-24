using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoSettingsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolucaoDropdown;
    [SerializeField] private TMP_Dropdown modoTelaDropdown;
    [SerializeField] private Button botaoAplicar;
    [SerializeField] private Button botaoRestaurar;

    private readonly List<Resolution> resolucoesDisponiveis = new List<Resolution>();

    private const string CHAVE_RESOLUCAO_LARGURA = "ResolucaoLargura";
    private const string CHAVE_RESOLUCAO_ALTURA  = "ResolucaoAltura";
    private const string CHAVE_MODO_TELA         = "ModoTela";

    private int larguraNativa;
    private int alturaNativa;

    private int indiceResolucaoSelecionada = 0;
    private int indiceModoTelaSelecionado  = 0;
    private bool inicializando = false;
    private Camera cameraPrincipal;
    private Camera cameraFundoBarrasPretas;
    private Coroutine reaplicarApresentacaoCoroutine;
    private int larguraTelaAnterior;
    private int alturaTelaAnterior;

    private const float ProporcaoDeJogo = 16f / 9f;

    private void Awake()
    {
        larguraNativa = ObterLarguraNativa();
        alturaNativa  = ObterAlturaNativa();

        inicializando = true;

        ConfigurarDropdownResolucoes();
        ConfigurarDropdownModoTela();
        CarregarConfiguracoesSalvas();
        RegistrarEventos();

        inicializando = false;

        // Reconstrói os textos localizados (sufixo "(Recomendado)" e as opções do
        // dropdown de Modo de Tela) sempre que o idioma mudar — sem isso eles ficavam
        // presos no idioma em que o painel foi aberto pela primeira vez.
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
    }

    private void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;

        if (reaplicarApresentacaoCoroutine != null)
            StopCoroutine(reaplicarApresentacaoCoroutine);
    }

    private void Update()
    {
        // Screen.SetResolution pode terminar a troca um frame depois do clique.
        // Reaplicar aqui garante que o viewport use o tamanho realmente aceito
        // pelo sistema operacional, inclusive em modo sem borda.
        if (Screen.width == larguraTelaAnterior && Screen.height == alturaTelaAnterior)
            return;

        larguraTelaAnterior = Screen.width;
        alturaTelaAnterior = Screen.height;

        if (resolucoesDisponiveis.Count == 0)
            return;

        Resolution resolucao = resolucoesDisponiveis[Mathf.Clamp(
            indiceResolucaoSelecionada, 0, resolucoesDisponiveis.Count - 1)];
        AplicarApresentacaoResolucao(resolucao,
            ConverterIndiceDropdownParaModoTela(indiceModoTelaSelecionado));
        AtualizarCanvasScalers();
    }

    private string ObterTexto(string chave, string fallback)
    {
        return LanguageManager.Instance != null ? LanguageManager.Instance.GetText(chave) : fallback;
    }

    private void AtualizarTextosIdioma()
    {
        bool estavaInicializando = inicializando;
        inicializando = true;

        ConfigurarDropdownResolucoes();
        ConfigurarDropdownModoTela();

        if (resolucaoDropdown != null)
        {
            resolucaoDropdown.SetValueWithoutNotify(indiceResolucaoSelecionada);
            resolucaoDropdown.RefreshShownValue();
        }

        if (modoTelaDropdown != null)
        {
            modoTelaDropdown.SetValueWithoutNotify(indiceModoTelaSelecionado);
            modoTelaDropdown.RefreshShownValue();
        }

        inicializando = estavaInicializando;
    }

    // ─────────────────────────────────────────────
    // RESOLUÇÕES COM FILTRO DE RESOLUÇÕES PARECIDAS
    // ─────────────────────────────────────────────
    private void ConfigurarDropdownResolucoes()
    {
        if (resolucaoDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: resolucaoDropdown não configurado.");
            return;
        }

        resolucaoDropdown.ClearOptions();
        resolucoesDisponiveis.Clear();

        Resolution[] resolucoesSistema = Screen.resolutions;
        HashSet<string> resolucoesUnicas = new HashSet<string>();
        List<Resolution> listaTemp = new List<Resolution>();

        // Remove duplicatas
        for (int i = 0; i < resolucoesSistema.Length; i++)
        {
            Resolution r = resolucoesSistema[i];
            // Alguns drivers expõem modos virtuais/ultrawide que não cabem no
            // monitor em uso. Oferecer, por exemplo, 3000x3000 num monitor
            // 1920x1080 causa troca de modo inválida e pode deixar a janela/UI
            // fora da área visível.
            if (!ResolucaoCabeNoMonitor(r))
                continue;

            string chave = r.width + "x" + r.height;
            if (resolucoesUnicas.Contains(chave)) continue;
            resolucoesUnicas.Add(chave);
            listaTemp.Add(r);
        }

        // Ordena da maior para a menor
        listaTemp.Sort((a, b) =>
        {
            int compW = b.width.CompareTo(a.width);
            return compW != 0 ? compW : b.height.CompareTo(a.height);
        });

        if (listaTemp.Count == 0)
        {
            Resolution fallback = Screen.currentResolution;
            fallback.width = Mathf.Min(fallback.width, larguraNativa);
            fallback.height = Mathf.Min(fallback.height, alturaNativa);
            listaTemp.Add(fallback);
        }

        // Não descarte resoluções apenas por serem próximas. Em telas pequenas
        // esse filtro eliminava opções úteis e fazia o item recomendado ficar
        // em uma ordem inesperada.
        List<Resolution> listaFiltrada = listaTemp;

        List<string> opcoes = new List<string>();

        foreach (Resolution r in listaFiltrada)
        {
            resolucoesDisponiveis.Add(r);

            bool ehNativa = r.width == larguraNativa && r.height == alturaNativa;
            string label = r.width + " x " + r.height
                + (ehNativa ? "  " + ObterTexto("VIDEO_RECOMENDADO", "(Recomendado)") : "");
            opcoes.Add(label);
        }

        resolucaoDropdown.AddOptions(opcoes);
        AjustarTextoDoDropdown(resolucaoDropdown);
        resolucaoDropdown.RefreshShownValue();
    }

    private void AjustarTextoDoDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        AjustarTexto(dropdown.captionText);
        AjustarTexto(dropdown.itemText);
    }

    private void AjustarTexto(TMP_Text texto)
    {
        if (texto == null)
            return;

        texto.enableWordWrapping = false;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = Mathf.Max(10f, texto.fontSize * 0.55f);
        texto.fontSizeMax = Mathf.Max(texto.fontSizeMin, texto.fontSize);
    }

    private void ConfigurarDropdownModoTela()
    {
        if (modoTelaDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: modoTelaDropdown não configurado.");
            return;
        }

        modoTelaDropdown.ClearOptions();
        modoTelaDropdown.AddOptions(new List<string>
        {
            ObterTexto("VIDEO_TELA_CHEIA", "Tela cheia"),
            ObterTexto("VIDEO_JANELA_SEM_BORDA", "Janela sem borda"),
            ObterTexto("VIDEO_JANELA", "Janela")
        });

        AjustarTextoDoDropdown(modoTelaDropdown);
        modoTelaDropdown.RefreshShownValue();
    }

    private void RegistrarEventos()
    {
        if (resolucaoDropdown != null)
            resolucaoDropdown.onValueChanged.AddListener(OnResolucaoAlterada);

        if (modoTelaDropdown != null)
            modoTelaDropdown.onValueChanged.AddListener(OnModoTelaAlterado);

        if (botaoAplicar != null)
            botaoAplicar.onClick.AddListener(AplicarConfiguracoesVideo);

        if (botaoRestaurar != null)
            botaoRestaurar.onClick.AddListener(RestaurarPadraoVideo);
    }

    private void OnResolucaoAlterada(int novoIndice)
    {
        if (inicializando) return;
        if (novoIndice < 0 || novoIndice >= resolucoesDisponiveis.Count) return;
        indiceResolucaoSelecionada = novoIndice;
    }

    private void OnModoTelaAlterado(int novoIndice)
    {
        if (inicializando) return;
        indiceModoTelaSelecionado = Mathf.Clamp(novoIndice, 0, 2);
    }

    private void CarregarConfiguracoesSalvas()
    {
        int larguraSalva = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_LARGURA, larguraNativa);
        int alturaSalva  = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_ALTURA,  alturaNativa);
        FullScreenMode modoSalvo = (FullScreenMode)PlayerPrefs.GetInt(
            CHAVE_MODO_TELA, (int)FullScreenMode.FullScreenWindow);

        int indice = EncontrarIndiceResolucao(larguraSalva, alturaSalva);
        if (indice < 0) indice = EncontrarIndiceResolucao(larguraNativa, alturaNativa);
        if (indice < 0) indice = 0;

        indiceResolucaoSelecionada = indice;
        indiceModoTelaSelecionado  = ConverterModoTelaParaIndiceDropdown(modoSalvo);

        if (resolucaoDropdown != null)
        {
            resolucaoDropdown.value = indiceResolucaoSelecionada;
            resolucaoDropdown.RefreshShownValue();
        }

        if (modoTelaDropdown != null)
        {
            modoTelaDropdown.value = indiceModoTelaSelecionado;
            modoTelaDropdown.RefreshShownValue();
        }

        AplicarResolucao(indiceResolucaoSelecionada,
            ConverterIndiceDropdownParaModoTela(indiceModoTelaSelecionado));
    }

    public void AplicarConfiguracoesVideo()
    {
        FullScreenMode modo = ConverterIndiceDropdownParaModoTela(indiceModoTelaSelecionado);
        AplicarResolucao(indiceResolucaoSelecionada, modo);
    }

    public void RestaurarPadraoVideo()
    {
        // O padrão é a resolução real deste monitor, nunca uma resolução fixa.
        int indice = EncontrarIndiceResolucao(larguraNativa, alturaNativa);
        if (indice < 0) indice = 0;

        indiceResolucaoSelecionada = indice;
        indiceModoTelaSelecionado  = ConverterModoTelaParaIndiceDropdown(FullScreenMode.FullScreenWindow);

        if (resolucaoDropdown != null)
        {
            resolucaoDropdown.value = indiceResolucaoSelecionada;
            resolucaoDropdown.RefreshShownValue();
        }

        if (modoTelaDropdown != null)
        {
            modoTelaDropdown.value = indiceModoTelaSelecionado;
            modoTelaDropdown.RefreshShownValue();
        }

        AplicarConfiguracoesVideo();
    }

    private void AplicarResolucao(int indiceResolucao, FullScreenMode modo)
    {
        if (resolucoesDisponiveis.Count == 0) return;
        if (indiceResolucao < 0 || indiceResolucao >= resolucoesDisponiveis.Count)
            indiceResolucao = 0;

        Resolution resolucao = resolucoesDisponiveis[indiceResolucao];
        // Defesa adicional para preferências antigas ou dados vindos de outro
        // monitor: nunca aplique um modo maior que a área nativa atual.
        resolucao.width = Mathf.Min(resolucao.width, larguraNativa);
        resolucao.height = Mathf.Min(resolucao.height, alturaNativa);
        Screen.SetResolution(resolucao.width, resolucao.height, modo);

        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_LARGURA, resolucao.width);
        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_ALTURA,  resolucao.height);
        PlayerPrefs.SetInt(CHAVE_MODO_TELA, (int)modo);
        PlayerPrefs.Save();

        indiceResolucaoSelecionada = indiceResolucao;
        indiceModoTelaSelecionado  = ConverterModoTelaParaIndiceDropdown(modo);

        AplicarApresentacaoResolucao(resolucao, modo);
        AtualizarCanvasScalers();

        if (reaplicarApresentacaoCoroutine != null)
            StopCoroutine(reaplicarApresentacaoCoroutine);
        reaplicarApresentacaoCoroutine = StartCoroutine(ReaplicarApresentacaoDepoisDaTroca(resolucao, modo));
    }

    private bool ResolucaoCabeNoMonitor(Resolution resolucao)
    {
        return resolucao.width > 0 && resolucao.height > 0 &&
            resolucao.width <= Mathf.Max(1, larguraNativa) &&
            resolucao.height <= Mathf.Max(1, alturaNativa);
    }

    private IEnumerator ReaplicarApresentacaoDepoisDaTroca(Resolution resolucao, FullScreenMode modo)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        AplicarApresentacaoResolucao(resolucao, modo);
        AtualizarCanvasScalers();
        reaplicarApresentacaoCoroutine = null;
    }

    private void AtualizarCanvasScalers()
    {
        foreach (CanvasScaler scaler in FindObjectsByType<CanvasScaler>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scaler == null) continue;

            UIResponsiveLayout responsivo = scaler.GetComponent<UIResponsiveLayout>();
            if (responsivo == null)
                responsivo = scaler.gameObject.AddComponent<UIResponsiveLayout>();
            responsivo.AplicarAgora();
        }

        Canvas.ForceUpdateCanvases();
    }

    int ObterLarguraNativa()
    {
        if (Display.main != null && Display.main.systemWidth > 0)
            return Display.main.systemWidth;
        return Screen.currentResolution.width;
    }

    int ObterAlturaNativa()
    {
        if (Display.main != null && Display.main.systemHeight > 0)
            return Display.main.systemHeight;
        return Screen.currentResolution.height;
    }

    void AplicarApresentacaoResolucao(Resolution resolucao, FullScreenMode modo)
    {
        cameraPrincipal = Camera.main;
        if (cameraPrincipal == null)
            cameraPrincipal = FindFirstObjectByType<Camera>();
        if (cameraPrincipal == null)
            return;

        // O jogo usa o renderizador padrão para manter compatibilidade com
        // GPUs antigas. HDR e MSAA não são necessários para a arte 2D.
        cameraPrincipal.allowHDR = false;
        cameraPrincipal.allowMSAA = false;

        float larguraAtual = Mathf.Max(1f, Screen.width > 0 ? Screen.width : resolucao.width);
        float alturaAtual = Mathf.Max(1f, Screen.height > 0 ? Screen.height : resolucao.height);
        float proporcaoTela = larguraAtual / alturaAtual;

        float viewportWidth = 1f;
        float viewportHeight = 1f;

        // Mantém a área jogável em 16:9. Em ultrawide, as laterais viram
        // barras pretas; em telas estreitas, as barras ficam em cima/baixo.
        if (proporcaoTela > ProporcaoDeJogo)
            viewportWidth = ProporcaoDeJogo / proporcaoTela;
        else if (proporcaoTela < ProporcaoDeJogo)
            viewportHeight = proporcaoTela / ProporcaoDeJogo;

        bool usarBarrasPretas = viewportWidth < 0.999f || viewportHeight < 0.999f;

        // Não redimensiona o buffer interno do URP. A GPU usada na build não
        // suporta alguns formatos de RenderGraph/dynamic resolution; o viewport
        // centralizado já preserva a proporção sem esse buffer.

        cameraPrincipal.rect = new Rect(
            (1f - viewportWidth) * 0.5f,
            (1f - viewportHeight) * 0.5f,
            viewportWidth,
            viewportHeight);

        // Limpa de preto a área fora do viewport centralizado.
        if (cameraFundoBarrasPretas == null)
        {
            GameObject objetoFundo = new GameObject("FundoBarrasPretas");
            objetoFundo.transform.SetParent(transform, false);
            cameraFundoBarrasPretas = objetoFundo.AddComponent<Camera>();
            cameraFundoBarrasPretas.clearFlags = CameraClearFlags.SolidColor;
            cameraFundoBarrasPretas.backgroundColor = Color.black;
            cameraFundoBarrasPretas.cullingMask = 0;
            cameraFundoBarrasPretas.depth = cameraPrincipal.depth - 1f;
            cameraFundoBarrasPretas.rect = new Rect(0f, 0f, 1f, 1f);
        }

        cameraFundoBarrasPretas.enabled = usarBarrasPretas;

        larguraTelaAnterior = Screen.width;
        alturaTelaAnterior = Screen.height;

        // A UI permanece em ScreenSpaceOverlay para ser renderizada diretamente
        // na resolução da janela. Forçar todos os Canvas para ScreenSpaceCamera
        // fazia os textos pixel art passarem pelo buffer escalável da câmera,
        // deixando a TelaInicial borrada durante a execução.
        // A resolução, o modo de janela e o buffer da câmera continuam sendo
        // aplicados normalmente acima.
    }

    private int EncontrarIndiceResolucao(int largura, int altura)
    {
        for (int i = 0; i < resolucoesDisponiveis.Count; i++)
        {
            if (resolucoesDisponiveis[i].width == largura &&
                resolucoesDisponiveis[i].height == altura)
                return i;
        }
        return -1;
    }

    private FullScreenMode ConverterIndiceDropdownParaModoTela(int indice)
    {
        switch (indice)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.FullScreenWindow;
            case 2: return FullScreenMode.Windowed;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    private int ConverterModoTelaParaIndiceDropdown(FullScreenMode modo)
    {
        switch (modo)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.FullScreenWindow: return 1;
            case FullScreenMode.Windowed: return 2;
            case FullScreenMode.MaximizedWindow: return 1;
            default: return 1;
        }
    }
}
