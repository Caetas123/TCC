using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Painel de Pause")]
    public GameObject painelPause;

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
    }

    public void Continuar()
    {
        if (painelPause != null)
            painelPause.SetActive(false);

        Time.timeScale = 1f;
        pausado = false;
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

    // Volta para seleção de personagens
    public void VoltarSelecaoPersonagem()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelecaoPlayer");
    }

    // NOVO BOTÃO - abrir configurações
    public void AbrirConfiguracoes()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Configuracoes");
    }
}