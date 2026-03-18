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
    private const string CHAVE_RESOLUCAO_ALTURA = "ResolucaoAltura";
    private const string CHAVE_MODO_TELA = "ModoTela";

    private int indiceResolucaoSelecionada = 0;
    private int indiceModoTelaSelecionado = 0;
    private bool inicializando = false;

    private void Awake()
    {
        inicializando = true;

        ConfigurarDropdownResolucoes();
        ConfigurarDropdownModoTela();
        CarregarConfiguracoesSalvas();
        RegistrarEventos();

        inicializando = false;
    }

    private void ConfigurarDropdownResolucoes()
    {
        if (resolucaoDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: resolucaoDropdown não foi configurado no Inspector.");
            return;
        }

        resolucaoDropdown.ClearOptions();
        resolucoesDisponiveis.Clear();

        Resolution[] resolucoesSistema = Screen.resolutions;
        HashSet<string> resolucoesUnicas = new HashSet<string>();
        List<string> opcoes = new List<string>();

        for (int i = 0; i < resolucoesSistema.Length; i++)
        {
            Resolution resolucao = resolucoesSistema[i];
            string chave = resolucao.width + "x" + resolucao.height;

            if (resolucoesUnicas.Contains(chave))
                continue;

            resolucoesUnicas.Add(chave);
            resolucoesDisponiveis.Add(resolucao);
            opcoes.Add(resolucao.width + " x " + resolucao.height);
        }

        if (resolucoesDisponiveis.Count == 0)
        {
            Resolution fallback = Screen.currentResolution;
            resolucoesDisponiveis.Add(fallback);
            opcoes.Add(fallback.width + " x " + fallback.height);
        }

        resolucaoDropdown.AddOptions(opcoes);
        resolucaoDropdown.RefreshShownValue();
    }

    private void ConfigurarDropdownModoTela()
    {
        if (modoTelaDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: modoTelaDropdown não foi configurado no Inspector.");
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

    private void CarregarConfiguracoesSalvas()
    {
        int larguraSalva = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_LARGURA, Screen.currentResolution.width);
        int alturaSalva = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_ALTURA, Screen.currentResolution.height);
        FullScreenMode modoSalvo = (FullScreenMode)PlayerPrefs.GetInt(
            CHAVE_MODO_TELA,
            (int)FullScreenMode.FullScreenWindow
        );

        int indiceResolucaoSalva = EncontrarIndiceResolucao(larguraSalva, alturaSalva);
        if (indiceResolucaoSalva < 0)
        {
            indiceResolucaoSalva = EncontrarIndiceResolucao(
                Screen.currentResolution.width,
                Screen.currentResolution.height
            );
        }

        if (indiceResolucaoSalva < 0)
            indiceResolucaoSalva = 0;

        indiceResolucaoSelecionada = indiceResolucaoSalva;
        indiceModoTelaSelecionado = ConverterModoTelaParaIndiceDropdown(modoSalvo);

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

        AplicarResolucao(
            indiceResolucaoSelecionada,
            ConverterIndiceDropdownParaModoTela(indiceModoTelaSelecionado)
        );
    }

    private void OnResolucaoAlterada(int novoIndice)
    {
        if (inicializando)
            return;

        if (novoIndice < 0 || novoIndice >= resolucoesDisponiveis.Count)
            return;

        indiceResolucaoSelecionada = novoIndice;
    }

    private void OnModoTelaAlterado(int novoIndice)
    {
        if (inicializando)
            return;

        indiceModoTelaSelecionado = Mathf.Clamp(novoIndice, 0, 2);
    }

    public void AplicarConfiguracoesVideo()
    {
        FullScreenMode modo = ConverterIndiceDropdownParaModoTela(indiceModoTelaSelecionado);
        AplicarResolucao(indiceResolucaoSelecionada, modo);
    }

    public void RestaurarPadraoVideo()
    {
        int indiceResolucaoMonitor = EncontrarIndiceResolucao(
            Screen.currentResolution.width,
            Screen.currentResolution.height
        );

        if (indiceResolucaoMonitor < 0)
            indiceResolucaoMonitor = 0;

        indiceResolucaoSelecionada = indiceResolucaoMonitor;
        indiceModoTelaSelecionado = ConverterModoTelaParaIndiceDropdown(FullScreenMode.FullScreenWindow);

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
        if (resolucoesDisponiveis.Count == 0)
            return;

        if (indiceResolucao < 0 || indiceResolucao >= resolucoesDisponiveis.Count)
            indiceResolucao = 0;

        Resolution resolucao = resolucoesDisponiveis[indiceResolucao];
        Screen.SetResolution(resolucao.width, resolucao.height, modo);

        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_LARGURA, resolucao.width);
        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_ALTURA, resolucao.height);
        PlayerPrefs.SetInt(CHAVE_MODO_TELA, (int)modo);
        PlayerPrefs.Save();

        indiceResolucaoSelecionada = indiceResolucao;
        indiceModoTelaSelecionado = ConverterModoTelaParaIndiceDropdown(modo);
    }

    private int EncontrarIndiceResolucao(int largura, int altura)
    {
        for (int i = 0; i < resolucoesDisponiveis.Count; i++)
        {
            if (resolucoesDisponiveis[i].width == largura &&
                resolucoesDisponiveis[i].height == altura)
            {
                return i;
            }
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