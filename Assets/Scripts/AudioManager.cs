using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    // Disparado sempre que qualquer volume/toggle muda. Scripts que tocam sons em
    // loop (ex.: passos) devem se inscrever aqui para recalcular o próprio volume,
    // já que sons em loop não "escutam" o slider sozinhos.
    public event Action OnVolumeChanged;

    public float VolumeMaster
    {
        get
        {
            if (!SomAtivado)
                return 0f;

            return PlayerPrefs.GetFloat(CHAVE_VOLUME_GERAL, 1f);
        }
    }

    public float VolumeMusica
    {
        get
        {
            return PlayerPrefs.GetFloat(CHAVE_VOLUME_MUSICA, 1f);
        }
    }

    public float VolumeEfeitos
    {
        get
        {
            return PlayerPrefs.GetFloat(CHAVE_VOLUME_EFEITOS, 1f);
        }
    }

    public bool SomAtivado
    {
        get
        {
            return PlayerPrefs.GetInt(CHAVE_SOM_ATIVADO, 1) == 1;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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
            toggleSom.isOn = SomAtivado;
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
        AplicarAudio();
    }

    public void SetVolumeEfeitos(float volume)
    {
        PlayerPrefs.SetFloat(CHAVE_VOLUME_EFEITOS, volume);
        PlayerPrefs.Save();
        AplicarAudio();
    }

    public void SetSomAtivado(bool ativado)
    {
        PlayerPrefs.SetInt(CHAVE_SOM_ATIVADO, ativado ? 1 : 0);
        PlayerPrefs.Save();
        AplicarAudio();
    }

    // O volume "Geral" já é aplicado uma única vez via AudioListener.volume (em
    // AplicarAudio). Por isso essas funções NÃO multiplicam por VolumeMaster de novo
    // — isso era o bug: o volume geral estava sendo aplicado duas vezes (uma no
    // AudioListener e outra aqui dentro), fazendo o efeito ficar bem mais baixo do
    // que o slider indicava (ou até "sumir") sempre que o volume geral não estava em 100%.
    public float ObterVolumeFinalMusica()
    {
        if (!SomAtivado)
            return 0f;

        return VolumeMusica;
    }

    public float ObterVolumeFinalEfeitos(float multiplicador = 1f)
    {
        if (!SomAtivado)
            return 0f;

        return VolumeEfeitos * multiplicador;
    }

    private void AplicarAudio()
    {
        // Volume geral: único ponto onde ele é aplicado. Afeta automaticamente
        // TUDO que é tocado (PlayOneShot, loops, música), sem precisar de tag.
        AudioListener.volume = SomAtivado ? VolumeMaster : 0f;

        // Ainda útil para AudioSources persistentes marcados manualmente com as tags
        // "Musica" ou "Efeito" (ex.: uma música de fundo tocando em loop na cena).
        AudioSource[] fontes = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioSource src in fontes)
        {
            if (src == null)
                continue;

            if (src.CompareTag("Musica"))
            {
                src.volume = ObterVolumeFinalMusica();
            }
            else if (src.CompareTag("Efeito"))
            {
                src.volume = ObterVolumeFinalEfeitos();
            }
        }

        // Avisa quem estiver tocando som em loop (ex.: passos do lutador) para
        // recalcular o próprio volume agora — PlayOneShot não precisa disso porque
        // já busca o volume certo no instante em que é chamado.
        OnVolumeChanged?.Invoke();
    }
}