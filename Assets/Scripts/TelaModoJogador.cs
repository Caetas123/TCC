using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TelaModoJogador : MonoBehaviour
{
    [Header("Botões dos modos de jogo")]
    public Button btnPVP;
    public Button btnPVC;
    public Button btnCVC;

    [Header("Botão voltar")]
    public Button botaoVoltar;

    void Start()
    {
        // Modos de jogo
        if (btnPVP != null)
            btnPVP.onClick.AddListener(() => SelecionarModo("PVP"));

        if (btnPVC != null)
            btnPVC.onClick.AddListener(() => SelecionarModo("PVC"));

        if (btnCVC != null)
            btnCVC.onClick.AddListener(() => SelecionarModo("CVC"));

        // Botão voltar
        if (botaoVoltar != null)
            botaoVoltar.onClick.AddListener(Voltar);
    }

    void SelecionarModo(string modo)
    {
        PlayerPrefs.SetString("ModoJogo", modo);
        PlayerPrefs.Save();

        Debug.Log("Modo selecionado: " + modo);

        // Vai para seleção de personagem
        SceneManager.LoadScene("SelecaoPlayer");
    }

    void Voltar()
    {
        SceneManager.LoadScene("TelaInicial");
    }
}