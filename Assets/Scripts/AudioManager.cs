using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider volumeGeral;
    [SerializeField] private Slider volumeMusica;
    [SerializeField] private Slider volumeEfeitos;

    [Header("Toggle Opcional")]
    [SerializeField] private Toggle toggleSom;

    private const string CHAVE_VOLUME_GERAL = "VolumeGeral";
    private const string CHAVE_VOLUME_MUSICA = "VolumeMusica";
    private const string CHAVE_VOLUME_EFEITOS = "VolumeEfeitos";
    private const string CHAVE_SOM_ATIVADO = "SomAtivado";

    private void Start()
    {
        CarregarConfiguracoes();
        RegistrarEventos();
        AplicarAudio();
    }

    private void CarregarConfiguracoes()
    {
        if (volumeGeral != null)
            volumeGeral.value = PlayerPrefs.GetFloat(CHAVE_VOLUME_GERAL, 1f);

        if (volumeMusica != null)
            volumeMusica.value = PlayerPrefs.GetFloat(CHAVE_VOLUME_MUSICA, 1f);

        if (volumeEfeitos != null)
            volumeEfeitos.value = PlayerPrefs.GetFloat(CHAVE_VOLUME_EFEITOS, 1f);

        if (toggleSom != null)
            toggleSom.isOn = PlayerPrefs.GetInt(CHAVE_SOM_ATIVADO, 1) == 1;
    }

    private void RegistrarEventos()
    {
        if (volumeGeral != null)
            volumeGeral.onValueChanged.AddListener(SetVolumeGeral);

        if (volumeMusica != null)
            volumeMusica.onValueChanged.AddListener(SetVolumeMusica);

        if (volumeEfeitos != null)
            volumeEfeitos.onValueChanged.AddListener(SetVolumeEfeitos);

        if (toggleSom != null)
            toggleSom.onValueChanged.AddListener(SetSomAtivado);
    }

    public void SetVolumeGeral(float volume)
    {
        PlayerPrefs.SetFloat(CHAVE_VOLUME_GERAL, volume);
        PlayerPrefs.Save();
        AplicarAudio();
    }

    public void SetVolumeMusica(float volume)
    {
        PlayerPrefs.SetFloat(CHAVE_VOLUME_MUSICA, volume);
        PlayerPrefs.Save();
    }

    public void SetVolumeEfeitos(float volume)
    {
        PlayerPrefs.SetFloat(CHAVE_VOLUME_EFEITOS, volume);
        PlayerPrefs.Save();
    }

    public void SetSomAtivado(bool ativado)
    {
        PlayerPrefs.SetInt(CHAVE_SOM_ATIVADO, ativado ? 1 : 0);
        PlayerPrefs.Save();
        AplicarAudio();
    }

    private void AplicarAudio()
    {
        bool somAtivado = PlayerPrefs.GetInt(CHAVE_SOM_ATIVADO, 1) == 1;
        float volumeGeralSalvo = PlayerPrefs.GetFloat(CHAVE_VOLUME_GERAL, 1f);

        AudioListener.volume = somAtivado ? volumeGeralSalvo : 0f;
    }
}