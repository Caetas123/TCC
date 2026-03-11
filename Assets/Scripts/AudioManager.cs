using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public Slider volumeGeral;
    public Slider volumeMusica;
    public Slider volumeEfeitos;

    public Button voltar; // Botão voltar
    public GameObject painelPrincipal; // Painel principal

    void Start()
    {
        // Configuração dos sliders
        volumeGeral.value = PlayerPrefs.GetFloat("VolumeGeral", 1f);
        volumeMusica.value = PlayerPrefs.GetFloat("VolumeMusica", 1f);
        volumeEfeitos.value = PlayerPrefs.GetFloat("VolumeEfeitos", 1f);

        volumeGeral.onValueChanged.AddListener(SetVolumeGeral);
        volumeMusica.onValueChanged.AddListener(SetVolumeMusica);
        volumeEfeitos.onValueChanged.AddListener(SetVolumeEfeitos);

        // Botão voltar
        voltar.onClick.AddListener(VoltarParaPrincipal);
    }

    void VoltarParaPrincipal()
    {
        this.gameObject.SetActive(false);
        painelPrincipal.SetActive(true);
    }

    public void SetVolumeGeral(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("VolumeGeral", volume);
    }

    public void SetVolumeMusica(float volume)
    {
        PlayerPrefs.SetFloat("VolumeMusica", volume);
    }

    public void SetVolumeEfeitos(float volume)
    {
        PlayerPrefs.SetFloat("VolumeEfeitos", volume);
    }
}