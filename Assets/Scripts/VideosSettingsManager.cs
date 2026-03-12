using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoSettingsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Dropdown resolucaoDropdown;
    [SerializeField] private Dropdown modoTelaDropdown;

    private readonly List<Resolution> resolucoesFiltradas = new List<Resolution>();

    private const string CHAVE_RESOLUCAO_LARGURA = "ResolucaoLargura";
    private const string CHAVE_RESOLUCAO_ALTURA = "ResolucaoAltura";
    private const string CHAVE_MODO_TELA = "ModoTela";

    private void Start()
    {
        ConfigurarDropdownResolucoes();
        ConfigurarDropdownModoTela();
        CarregarConfiguracoesSalvas();
        RegistrarEventos();
    }

    private void ConfigurarDropdownResolucoes()
    {
        if (resolucaoDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: resolucaoDropdown não foi ligado no Inspector.");
            return;
        }

        resolucaoDropdown.ClearOptions();
        resolucoesFiltradas.Clear();

        List<string> opcoes = new List<string>();
        Resolution[] resolucoes = Screen.resolutions;

        HashSet<string> resolucoesUnicas = new HashSet<string>();
        int indiceAtual = 0;

        for (int i = 0; i < resolucoes.Length; i++)
        {
            string chave = resolucoes[i].width + "x" + resolucoes[i].height;

            if (resolucoesUnicas.Contains(chave))
                continue;

            resolucoesUnicas.Add(chave);
            resolucoesFiltradas.Add(resolucoes[i]);
            opcoes.Add(resolucoes[i].width + " x " + resolucoes[i].height);

            if (resolucoes[i].width == Screen.currentResolution.width &&
                resolucoes[i].height == Screen.currentResolution.height)
            {
                indiceAtual = resolucoesFiltradas.Count - 1;
            }
        }

        resolucaoDropdown.AddOptions(opcoes);
        resolucaoDropdown.value = indiceAtual;
        resolucaoDropdown.RefreshShownValue();
    }

    private void ConfigurarDropdownModoTela()
    {
        if (modoTelaDropdown == null)
        {
            Debug.LogWarning("VideoSettingsManager: modoTelaDropdown não foi ligado no Inspector.");
            return;
        }

        if (modoTelaDropdown.options == null || modoTelaDropdown.options.Count == 0)
        {
            modoTelaDropdown.ClearOptions();
            modoTelaDropdown.AddOptions(new List<string>
            {
                "Tela Cheia Exclusiva",
                "Tela Cheia em Janela",
                "Janela"
            });
        }
    }

    private void RegistrarEventos()
    {
        if (resolucaoDropdown != null)
            resolucaoDropdown.onValueChanged.AddListener(MudarResolucao);

        if (modoTelaDropdown != null)
            modoTelaDropdown.onValueChanged.AddListener(MudarModoTela);
    }

    private void CarregarConfiguracoesSalvas()
    {
        int larguraSalva = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_LARGURA, Screen.currentResolution.width);
        int alturaSalva = PlayerPrefs.GetInt(CHAVE_RESOLUCAO_ALTURA, Screen.currentResolution.height);
        int modoTelaSalvo = PlayerPrefs.GetInt(CHAVE_MODO_TELA, (int)Screen.fullScreenMode);

        int indiceResolucao = EncontrarIndiceResolucao(larguraSalva, alturaSalva);
        if (indiceResolucao >= 0 && indiceResolucao < resolucoesFiltradas.Count)
        {
            if (resolucaoDropdown != null)
            {
                resolucaoDropdown.value = indiceResolucao;
                resolucaoDropdown.RefreshShownValue();
            }

            Resolution resolucao = resolucoesFiltradas[indiceResolucao];
            Screen.SetResolution(resolucao.width, resolucao.height, (FullScreenMode)modoTelaSalvo);
        }

        int indiceModoDropdown = ConverterModoTelaParaDropdown((FullScreenMode)modoTelaSalvo);
        if (modoTelaDropdown != null)
        {
            modoTelaDropdown.value = indiceModoDropdown;
            modoTelaDropdown.RefreshShownValue();
        }

        Screen.fullScreenMode = (FullScreenMode)modoTelaSalvo;
    }

    public void MudarResolucao(int indice)
    {
        if (indice < 0 || indice >= resolucoesFiltradas.Count)
            return;

        Resolution resolucao = resolucoesFiltradas[indice];
        Screen.SetResolution(resolucao.width, resolucao.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_LARGURA, resolucao.width);
        PlayerPrefs.SetInt(CHAVE_RESOLUCAO_ALTURA, resolucao.height);
        PlayerPrefs.Save();
    }

    public void MudarModoTela(int indiceModo)
    {
        FullScreenMode modo = ConverterDropdownParaModoTela(indiceModo);
        Screen.fullScreenMode = modo;

        PlayerPrefs.SetInt(CHAVE_MODO_TELA, (int)modo);
        PlayerPrefs.Save();
    }

    private int EncontrarIndiceResolucao(int largura, int altura)
    {
        for (int i = 0; i < resolucoesFiltradas.Count; i++)
        {
            if (resolucoesFiltradas[i].width == largura &&
                resolucoesFiltradas[i].height == altura)
            {
                return i;
            }
        }

        return 0;
    }

    private FullScreenMode ConverterDropdownParaModoTela(int indice)
    {
        switch (indice)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.FullScreenWindow;
            case 2: return FullScreenMode.Windowed;
            default: return FullScreenMode.Windowed;
        }
    }

    private int ConverterModoTelaParaDropdown(FullScreenMode modo)
    {
        switch (modo)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.FullScreenWindow: return 1;
            case FullScreenMode.Windowed: return 2;
            default: return 2;
        }
    }
}