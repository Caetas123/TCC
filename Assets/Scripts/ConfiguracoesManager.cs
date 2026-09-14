using UnityEngine;

public class ConfiguracoesManager : MonoBehaviour
{
    public GameObject painelConfiguracoes;
    public GameObject painelPrincipal;
    public GameObject painelAudio;
    public GameObject painelVideo;
    public GameObject painelControles;

    private PauseManager pauseManager;

    void Awake()
    {
        pauseManager = FindFirstObjectByType<PauseManager>();
    }

    void AbrirPainelBase()
    {
        // No menu principal não existe PauseManager. Na luta, qualquer caminho
        // para as configurações precisa garantir que o combate esteja pausado.
        if (pauseManager == null)
            pauseManager = FindFirstObjectByType<PauseManager>();

        if (pauseManager != null && !pauseManager.EstaPausado())
            pauseManager.Pausar();

        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(true);
    }

    public void AbrirPainelPrincipal()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
    }

    public void AbrirPainelAudio()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(true);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
    }

    public void AbrirPainelVideo()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(true);
        painelControles.SetActive(false);
    }

    public void AbrirPainelControles()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(true);
    }

    public void FecharConfiguracoes()
    {
        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(false);

        // Fechar pelo botão ou pelo ESC, vindo da luta, retorna ao pause sem
        // liberar o tempo do jogo.
        if (pauseManager != null && pauseManager.EstaPausado())
            pauseManager.ReabrirPainelPause();
    }

    public bool EstaAberta()
    {
        return painelConfiguracoes != null && painelConfiguracoes.activeInHierarchy;
    }
}
