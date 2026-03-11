using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VideoSettingsManager : MonoBehaviour
{
    public Dropdown resolucaoDropdown;
    public Dropdown modoTelaDropdown;

    public Button voltar; // Botão voltar
    public GameObject painelPrincipal; // Painel principal

    Resolution[] resolucoes;

    void Start()
    {
        resolucoes = Screen.resolutions;

        resolucaoDropdown.ClearOptions();
        List<string> opcoes = new List<string>();
        int resolucaoAtual = 0;

        for (int i = 0; i < resolucoes.Length; i++)
        {
            string opcao = resolucoes[i].width + " x " + resolucoes[i].height;
            opcoes.Add(opcao);

            if (resolucoes[i].width == Screen.currentResolution.width &&
                resolucoes[i].height == Screen.currentResolution.height)
            {
                resolucaoAtual = i;
            }
        }

        resolucaoDropdown.AddOptions(opcoes);
        resolucaoDropdown.value = resolucaoAtual;
        resolucaoDropdown.RefreshShownValue();

        // Botão voltar
        voltar.onClick.AddListener(VoltarParaPrincipal);
    }

    public void MudarResolucao(int indice)
    {
        Resolution resolucao = resolucoes[indice];
        Screen.SetResolution(resolucao.width, resolucao.height, Screen.fullScreen);
    }

    public void MudarModoTela(int modo)
    {
        if (modo == 0)
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        else if (modo == 1)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else if (modo == 2)
            Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    void VoltarParaPrincipal()
    {
        this.gameObject.SetActive(false);
        painelPrincipal.SetActive(true);
    }
}