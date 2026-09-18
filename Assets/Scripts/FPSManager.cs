using UnityEngine;

public class FPSManager : MonoBehaviour
{
    public static FPSManager instancia;

    public int fpsAtual = 60;

    private const string CHAVE_FPS = "FPS";

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);

            int fpsPadrao = ObterFPSPadraoDoMonitor();

            fpsAtual = PlayerPrefs.HasKey(CHAVE_FPS)
                ? PlayerPrefs.GetInt(CHAVE_FPS, fpsPadrao)
                : fpsPadrao;

            AplicarFPSInterno(fpsAtual, false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetFPSPadraoMonitor()
    {
        return ObterFPSPadraoDoMonitor();
    }

    private int ObterFPSPadraoDoMonitor()
    {
#if UNITY_2022_2_OR_NEWER
        int hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
#else
        int hz = Screen.currentResolution.refreshRate;
#endif

        return Mathf.Clamp(hz, 30, 240);
    }

    public void AplicarFPS(int fps)
    {
        AplicarFPSInterno(fps, true);
    }

    private void AplicarFPSInterno(int fps, bool salvar)
    {
        fpsAtual = Mathf.Clamp(fps, 30, 240);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fpsAtual;

        if (salvar)
        {
            PlayerPrefs.SetInt(CHAVE_FPS, fpsAtual);
            PlayerPrefs.Save();
        }
    }
}