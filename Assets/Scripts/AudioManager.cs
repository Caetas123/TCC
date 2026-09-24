using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            // As cenas de luta também possuem uma cópia do objeto. Reaproveita
            // os controles daquela cena antes de destruir a cópia, mantendo o
            // singleton responsável pelo áudio.
            Instance.AssumirControlesDaCena(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void Start()
    {
        CarregarConfiguracoes();
        RegistrarEventos();
        AplicarAudio();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= AoCarregarCena;
            Instance = null;
        }
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        // Os painéis de configuração são criados por cena. Sem esta busca o
        // singleton ficava apontando para sliders destruídos da TelaInicial.
        VincularControlesEncontrados();
        CarregarConfiguracoes();
        AplicarAudio();
    }

    private void AssumirControlesDaCena(AudioManager outro)
    {
        if (outro == null)
            return;

        DesregistrarEventos();
        volumeGeral = outro.volumeGeral;
        volumeMusica = outro.volumeMusica;
        volumeEfeitos = outro.volumeEfeitos;
        toggleSom = outro.toggleSom;
        CarregarConfiguracoes();
        RegistrarEventos();
        AplicarAudio();
    }

    private void VincularControlesEncontrados()
    {
        Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Toggle[] toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Slider geral = EncontrarSlider(sliders, "SliderVolumeGeral");
        Slider musica = EncontrarSlider(sliders, "SliderVolumeMusica");
        Slider efeitos = EncontrarSlider(sliders, "SliderVolumeEfeitos");
        Toggle som = EncontrarToggle(toggles, "Som");

        if (geral == null && musica == null && efeitos == null && som == null)
            return;

        DesregistrarEventos();
        if (geral != null) volumeGeral = geral;
        if (musica != null) volumeMusica = musica;
        if (efeitos != null) volumeEfeitos = efeitos;
        if (som != null) toggleSom = som;
        RegistrarEventos();
    }

    private static Slider EncontrarSlider(Slider[] sliders, string parteNome)
    {
        foreach (Slider slider in sliders)
        {
            if (slider != null && slider.name.IndexOf(parteNome, StringComparison.OrdinalIgnoreCase) >= 0)
                return slider;
        }

        return null;
    }

    private static Toggle EncontrarToggle(Toggle[] toggles, string parteNome)
    {
        foreach (Toggle toggle in toggles)
        {
            if (toggle != null && toggle.name.IndexOf(parteNome, StringComparison.OrdinalIgnoreCase) >= 0)
                return toggle;
        }

        return null;
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

    private void DesregistrarEventos()
    {
        if (volumeGeral != null)
            volumeGeral.onValueChanged.RemoveListener(SetVolumeGeral);
        if (volumeMusica != null)
            volumeMusica.onValueChanged.RemoveListener(SetVolumeMusica);
        if (volumeEfeitos != null)
            volumeEfeitos.onValueChanged.RemoveListener(SetVolumeEfeitos);
        if (toggleSom != null)
            toggleSom.onValueChanged.RemoveListener(SetSomAtivado);
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
