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

    private bool pausado = false;

    void Start()
    {
        if (painelPause != null)
            painelPause.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("Configuracoes");
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