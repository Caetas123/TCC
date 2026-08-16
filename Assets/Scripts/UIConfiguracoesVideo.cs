using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIConfiguracoesVideo : MonoBehaviour
{
    [Header("FPS")]
    public Slider sliderFPS;
    public TextMeshProUGUI textoFPS;

    [Header("HZ")]
    public TMP_Dropdown dropdownHZ;
    public TextMeshProUGUI textoHZ;

    [Header("Botões")]
    public Button botaoAplicar;
    public Button botaoRestaurar;

    [Header("Referência ao VideoSettingsManager")]
    public VideoSettingsManager videoSettingsManager;

    private int fpsPendente = 60;

    private List<int> hzDisponiveis = new List<int>();
    private int hzPendente = 60;

    private bool eventosRegistrados = false;

    void Start()
    {
        if (sliderFPS == null || textoFPS == null)
        {
            Debug.LogWarning("UIConfiguracoesVideo: sliderFPS ou textoFPS não configurado.");
            return;
        }

        int fpsInicial = PlayerPrefs.HasKey("FPS")
            ? PlayerPrefs.GetInt("FPS", ObterFPSPadraoMonitor())
            : ObterFPSPadraoMonitor();

        fpsPendente = fpsInicial;

        sliderFPS.minValue = 30;
        sliderFPS.maxValue = 240;

        sliderFPS.SetValueWithoutNotify(fpsInicial);

        AtualizarTextoFPS(fpsInicial);

        CarregarHZ();

        // Busca o VideoSettingsManager automaticamente se não foi configurado
        if (videoSettingsManager == null)
            videoSettingsManager = FindFirstObjectByType<VideoSettingsManager>();

        RegistrarEventos();
    }

    void RegistrarEventos()
    {
        if (eventosRegistrados)
            return;

        sliderFPS.onValueChanged.AddListener(OnSliderFPSChange);

        if (dropdownHZ != null)
            dropdownHZ.onValueChanged.AddListener(OnHZChange);

        if (botaoAplicar != null)
            botaoAplicar.onClick.AddListener(AplicarFPSPendente);

        if (botaoRestaurar != null)
            botaoRestaurar.onClick.AddListener(RestaurarFPSPadrao);

        eventosRegistrados = true;

        // Sem isso, o rótulo "Taxa de Atualização (Hz)" ficava preso no idioma em que
        // o painel foi aberto pela primeira vez, mesmo depois de trocar o idioma.
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
    }

    void AtualizarTextosIdioma()
    {
        AtualizarTextoHZ(hzPendente);
    }

    void OnDestroy()
    {
        sliderFPS.onValueChanged.RemoveListener(OnSliderFPSChange);

        if (dropdownHZ != null)
            dropdownHZ.onValueChanged.RemoveListener(OnHZChange);

        if (botaoAplicar != null)
            botaoAplicar.onClick.RemoveListener(AplicarFPSPendente);

        if (botaoRestaurar != null)
            botaoRestaurar.onClick.RemoveListener(RestaurarFPSPadrao);

        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
    }

    void OnSliderFPSChange(float valor)
    {
        fpsPendente = Mathf.RoundToInt(valor);
        AtualizarTextoFPS(fpsPendente);
    }

    void AtualizarTextoFPS(int fps)
    {
        if (textoFPS != null)
            textoFPS.text = "FPS: " + fps;
    }

    void CarregarHZ()
    {
        if (dropdownHZ == null || textoHZ == null)
            return;

        dropdownHZ.ClearOptions();
        hzDisponiveis.Clear();

        Resolution[] resolucoes = Screen.resolutions;

        foreach (Resolution r in resolucoes)
        {
#if UNITY_2022_2_OR_NEWER
            int hz = Mathf.RoundToInt((float)r.refreshRateRatio.value);
#else
            int hz = r.refreshRate;
#endif

            if (!hzDisponiveis.Contains(hz))
                hzDisponiveis.Add(hz);
        }

        hzDisponiveis.Sort();

        List<string> opcoes = new List<string>();

        foreach (int hz in hzDisponiveis)
        {
            opcoes.Add(hz.ToString());
        }

        dropdownHZ.AddOptions(opcoes);

        int hzAtual = ObterFPSPadraoMonitor();

        int indexAtual = hzDisponiveis.IndexOf(hzAtual);

        if (indexAtual < 0)
            indexAtual = 0;

        dropdownHZ.SetValueWithoutNotify(indexAtual);

        hzPendente = hzDisponiveis[indexAtual];

        AtualizarTextoHZ(hzPendente);
    }

    void OnHZChange(int index)
    {
        if (index < 0 || index >= hzDisponiveis.Count)
            return;

        hzPendente = hzDisponiveis[index];

        AtualizarTextoHZ(hzPendente);
    }

    void AtualizarTextoHZ(int hz)
    {
        if (textoHZ == null)
            return;

        string rotulo = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("HZ")
            : "Hz:";

        textoHZ.text = rotulo + " " + hz;
    }

    public void AplicarFPSPendente()
    {
        // Aplica FPS
        if (FPSManager.instancia != null)
        {
            FPSManager.instancia.AplicarFPS(fpsPendente);
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fpsPendente;
            PlayerPrefs.SetInt("FPS", fpsPendente);
            PlayerPrefs.Save();
        }

        // Aplica HZ na resolução atual
        Resolution atual = Screen.currentResolution;
#if UNITY_2022_2_OR_NEWER
        RefreshRate refreshRate = new RefreshRate();
        refreshRate.numerator = (uint)hzPendente;
        refreshRate.denominator = 1;
        Screen.SetResolution(atual.width, atual.height, Screen.fullScreenMode, refreshRate);
#else
        Screen.SetResolution(atual.width, atual.height, Screen.fullScreen, hzPendente);
#endif

        // Aplica resolução e modo de tela via VideoSettingsManager
        if (videoSettingsManager != null)
            videoSettingsManager.AplicarConfiguracoesVideo();
    }

    public void RestaurarFPSPadrao()
    {
        int fpsPadrao = ObterFPSPadraoMonitor();

        fpsPendente = fpsPadrao;
        sliderFPS.SetValueWithoutNotify(fpsPadrao);
        AtualizarTextoFPS(fpsPadrao);

        hzPendente = fpsPadrao;
        if (dropdownHZ != null)
        {
            int index = hzDisponiveis.IndexOf(hzPendente);
            if (index >= 0)
                dropdownHZ.SetValueWithoutNotify(index);
        }
        AtualizarTextoHZ(hzPendente);

        if (FPSManager.instancia != null)
            FPSManager.instancia.AplicarFPS(fpsPadrao);

        // Restaura resolução e modo de tela via VideoSettingsManager
        if (videoSettingsManager != null)
            videoSettingsManager.RestaurarPadraoVideo();
    }

    int ObterFPSPadraoMonitor()
    {
#if UNITY_2022_2_OR_NEWER
        return Mathf.Clamp(
            Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value),
            30,
            240
        );
#else
        return Mathf.Clamp(Screen.currentResolution.refreshRate, 30, 240);
#endif
    }
}