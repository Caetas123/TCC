using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Painel de Pause")]
    public GameObject painelPause;

    [Header("Primeiro botão do pause")]
    public GameObject primeiroBotaoPause;

    [Header("Bloqueio automático")]
    [Tooltip("Arraste aqui o GameManagerLuta da cena (opcional — só é usado na cena de luta). Se preenchido, o Esc é ignorado enquanto a luta já tiver terminado ou estiver mostrando a tela de round/vencedor.")]
    public GameManagerLuta gameManagerLuta;

    private bool pausado = false;
    private ConfiguracoesManager configuracoesManager;

    void Start()
    {
        if (painelPause != null)
            painelPause.SetActive(false);

        if (gameManagerLuta == null)
            gameManagerLuta = FindFirstObjectByType<GameManagerLuta>();

        configuracoesManager = FindFirstObjectByType<ConfiguracoesManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Dentro das configurações abertas a partir do pause, o ESC deve
            // voltar ao pause e manter a luta congelada.
            if (pausado && configuracoesManager != null &&
                configuracoesManager.EstaAberta())
            {
                configuracoesManager.FecharConfiguracoes();
                return;
            }

            // Não deixa abrir o pause em cima da tela de Round Win / Vencedor, nem depois
            // que a luta já terminou — era isso que abria o menu por cima do "WINNER".
            if (!pausado && gameManagerLuta != null && !gameManagerLuta.PodePausar())
                return;

            if (pausado)
                Continuar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        if (painelPause != null)
            painelPause.SetActive(true);

        Time.timeScale = 0f;
        pausado = true;

        SelecionarBotaoComAtraso(primeiroBotaoPause);
    }

    public bool EstaPausado() => pausado;

    public void ReabrirPainelPause()
    {
        if (!pausado) return;

        if (painelPause != null)
            painelPause.SetActive(true);

        Time.timeScale = 0f;
        SelecionarBotaoComAtraso(primeiroBotaoPause);
    }

    public void Continuar()
    {
        if (painelPause != null)
            painelPause.SetActive(false);

        Time.timeScale = 1f;
        pausado = false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TelaInicial");
    }

    public void VoltarSelecaoPersonagem()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelecaoPlayer");
    }

    public void AbrirConfiguracoes()
    {
        // A cena Configuracoes não existe; o fluxo atual usa o painel embutido.
        if (configuracoesManager == null)
            configuracoesManager = FindFirstObjectByType<ConfiguracoesManager>();

        if (configuracoesManager != null)
            configuracoesManager.AbrirPainelPrincipal();
    }

    void SelecionarBotaoComAtraso(GameObject botao)
    {
        if (botao == null)
            return;

        StartCoroutine(SelecionarNoProximoFrame(botao));
    }

    IEnumerator SelecionarNoProximoFrame(GameObject botao)
    {
        yield return null;

        if (EventSystem.current == null || botao == null)
            yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(botao);
    }
}
