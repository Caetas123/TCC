using UnityEngine;

public class ConfiguracoesManager : MonoBehaviour
{
    public GameObject painelConfiguracoes;
    public GameObject painelPrincipal;
    public GameObject painelAudio;
    public GameObject painelVideo;
    public GameObject painelControles;

    public void AbrirPainelPrincipal()
    {
        painelConfiguracoes.SetActive(true);
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
    }

    public void AbrirPainelAudio()
    {
        painelConfiguracoes.SetActive(true);
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(true);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
    }

    public void AbrirPainelVideo()
    {
        painelConfiguracoes.SetActive(true);
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(true);
        painelControles.SetActive(false);
    }

    public void AbrirPainelControles()
    {
        painelConfiguracoes.SetActive(true);
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(true);
    }

    public void FecharConfiguracoes()
    {
        painelConfiguracoes.SetActive(false);
    }
}