using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Áudio de seleção e confirmação para botões de todas as telas.</summary>
public class SonsBotoesUI : MonoBehaviour
{
    private static SonsBotoesUI instancia;

    private ConfiguracaoSonsBotoes configuracao;
    private AudioSource fonte;
    private float proximaBusca;
    private GameObject ultimoSelecionado;
    private GameObject ultimoClicado;
    private float tempoUltimaSelecao = -1f;
    private float tempoUltimoClique = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Inicializar()
    {
        if (instancia == null)
            new GameObject("SonsBotoesUI").AddComponent<SonsBotoesUI>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        configuracao = Resources.Load<ConfiguracaoSonsBotoes>("ConfiguracaoSonsBotoes");
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        SceneManager.sceneLoaded += AoCarregarCena;
        RegistrarBotoes();
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            SceneManager.sceneLoaded -= AoCarregarCena;
            instancia = null;
        }
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        RegistrarBotoes();
    }

    private void Update()
    {
        // Alguns painéis são criados depois de sceneLoaded.
        if (Time.unscaledTime < proximaBusca)
            return;

        proximaBusca = Time.unscaledTime + 0.5f;
        RegistrarBotoes();
    }

    private static void RegistrarBotoes()
    {
        Button[] botoes = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button botao in botoes)
        {
            if (botao == null)
                continue;

            SomBotaoUI som = botao.GetComponent<SomBotaoUI>();
            if (som == null)
                som = botao.gameObject.AddComponent<SomBotaoUI>();
            som.ReconectarClique();
        }
    }

    public static void TocarSelecao(GameObject botao)
    {
        if (instancia == null || instancia.configuracao == null || botao == null)
            return;

        float agora = Time.unscaledTime;
        if (instancia.ultimoSelecionado == botao && agora - instancia.tempoUltimaSelecao < 0.12f)
            return;

        instancia.ultimoSelecionado = botao;
        instancia.tempoUltimaSelecao = agora;
        instancia.Tocar(instancia.configuracao.somSelecionar, instancia.configuracao.volumeSelecionar);
    }

    public static void TocarClique(GameObject botao)
    {
        if (instancia == null || instancia.configuracao == null || botao == null)
            return;

        float agora = Time.unscaledTime;
        if (instancia.ultimoClicado == botao && agora - instancia.tempoUltimoClique < 0.15f)
            return;

        instancia.ultimoClicado = botao;
        instancia.tempoUltimoClique = agora;
        instancia.Tocar(instancia.configuracao.somClicar, instancia.configuracao.volumeClicar);
    }

    private void Tocar(AudioClip clip, float volume)
    {
        if (clip == null || fonte == null)
            return;

        float volumeFinal = AudioManager.Instance != null
            ? AudioManager.Instance.ObterVolumeFinalEfeitos(volume)
            : PlayerPrefs.GetInt("SomAtivado", 1) == 1
                ? PlayerPrefs.GetFloat("VolumeEfeitos", 1f) * volume
                : 0f;
        fonte.PlayOneShot(clip, volumeFinal);
    }
}
