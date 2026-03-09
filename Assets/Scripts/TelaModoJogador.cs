using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TelaModoJogador : MonoBehaviour
{
    [Header("Botões dos modos de jogo")]
    public Button btnPVP;
    public Button btnPVC;
    public Button btnCVC;

    [Header("Botões das arenas")]
    public Button btnArenaAleatoria;
    public Button btnArena1;
    public Button btnArena2;

    [Header("Botão voltar")]
    public Button botaoVoltar;

    private string modoSelecionado = "";
    private string arenaSelecionada = "";

    void Start()
    {
        // Modos de jogo
        if (btnPVP != null) btnPVP.onClick.AddListener(() => SelecionarModo("PVP"));
        if (btnPVC != null) btnPVC.onClick.AddListener(() => SelecionarModo("PVC"));
        if (btnCVC != null) btnCVC.onClick.AddListener(() => SelecionarModo("CVC"));

        // Arenas
        if (btnArenaAleatoria != null) btnArenaAleatoria.onClick.AddListener(() => SelecionarArena("aleatoria"));
        if (btnArena1 != null) btnArena1.onClick.AddListener(() => SelecionarArena("cena1"));
        if (btnArena2 != null) btnArena2.onClick.AddListener(() => SelecionarArena("cena2"));

        botaoVoltar.onClick.AddListener(Voltar);
    }

    void SelecionarModo(string modo)
    {
        modoSelecionado = modo;

        PlayerPrefs.SetString("ModoJogo", modoSelecionado);
        PlayerPrefs.Save();

        Debug.Log("Modo selecionado: " + modoSelecionado);

        VerificarSePodeAvancar();
    }

    void SelecionarArena(string arena)
    {
        arenaSelecionada = arena;

        PlayerPrefs.SetString("ArenaEscolhida", arenaSelecionada);
        PlayerPrefs.Save();

        Debug.Log("Arena selecionada: " + arenaSelecionada);

        VerificarSePodeAvancar();
    }

    void VerificarSePodeAvancar()
    {
        if (modoSelecionado != "" && arenaSelecionada != "")
        {
            Debug.Log("Modo e Arena escolhidos, indo para seleção de personagem");
            SceneManager.LoadScene("SelecaoPlayer");
        }
    }

    void Voltar()
    {
        SceneManager.LoadScene("TelaInicial");
    }
}