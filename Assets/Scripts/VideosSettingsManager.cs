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

    private void Awake()
    {
        larguraNativa = Screen.currentResolution.width;
        alturaNativa  = Screen.currentResolution.height;

        inicializando = true;

        ConfigurarDropdownResolucoes();
        ConfigurarDropdownModoTela();
        CarregarConfiguracoesSalvas();
        RegistrarEventos();

        inicializando = false;
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
            listaTemp.Add(Screen.currentResolution);

        // FILTRO DE RESOLUÇÕES MUITO PARECIDAS
        List<Resolution> listaFiltrada = new List<Resolution>();

        for (int i = 0; i < listaTemp.Count; i++)
        {
            Resolution atual = listaTemp[i];

            if (listaFiltrada.Count == 0)
            {
                listaFiltrada.Add(atual);
                continue;
            }

            Resolution ultima = listaFiltrada[listaFiltrada.Count - 1];

            int diferencaLargura = Mathf.Abs(atual.width - ultima.width);
            int diferencaAltura  = Mathf.Abs(atual.height - ultima.height);

            bool muitoParecida = diferencaLargura < 120 && diferencaAltura < 80;

            if (!muitoParecida)
                listaFiltrada.Add(atual);
        }

        List<string> opcoes = new List<string>();

        foreach (Resolution r in listaFiltrada)
        {
            resolucoesDisponiveis.Add(r);

            bool ehNativa = r.width == larguraNativa && r.height == alturaNativa;
            string label = r.width + " x " + r.height + (ehNativa ? "  (Recomendado)" : "");
            opcoes.Add(label);
        }

        resolucaoDropdown.AddOptions(opcoes);
        resolucaoDropdown.RefreshShownValue();
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
            "Tela cheia",
            "Janela sem borda",
            "Janela"
        });

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
        int indice = EncontrarIndiceResolucao(1920, 1080);
        if (indice < 0) indice = EncontrarIndiceResolucao(larguraNativa, alturaNativa);
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
        Screen.SetResolution(resolucao.width, resolucao.height, modo);

        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_LARGURA, resolucao.width);
        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_ALTURA,  resolucao.height);
        PlayerPrefs.SetInt(CHAVE_MODO_TELA, (int)modo);
        PlayerPrefs.Save();

        indiceResolucaoSelecionada = indiceResolucao;
        indiceModoTelaSelecionado  = ConverterModoTelaParaIndiceDropdown(modo);

        AtualizarCanvasScalers();
    }

    private void AtualizarCanvasScalers()
    {
        foreach (CanvasScaler scaler in FindObjectsOfType<CanvasScaler>())
        {
            if (scaler == null) continue;
            scaler.enabled = false;
            scaler.enabled = true;
        }
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