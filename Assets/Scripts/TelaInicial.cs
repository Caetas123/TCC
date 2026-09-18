using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuInicial : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject painelCreditos;
    public GameObject painelConfirmacao;
    public GameObject painelConfiguracoes;

    [Header("Botões principais")]
    public Button botaoCreditos;
    public Button botaoStart;
    public Button botaoSair;
    public Button botaoConfiguracoes;

    [Header("Botões painel Créditos")]
    public Button botaoFecharCreditos;

    [Header("Botões painel Confirmação")]
    public Button botaoSim;
    public Button botaoNao;

    [Header("Botões painel Configurações")]
    public Button botaoFecharConfiguracoes;

    [Header("Primeiro botão do menu inicial")]
    public GameObject primeiroBotaoMenuInicial;

    private Button[] botoesMenuPrincipal;

    void Start()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(false);

        if (painelConfirmacao != null)
            painelConfirmacao.SetActive(false);

        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(false);

        botoesMenuPrincipal = new Button[]
        {
            botaoStart,
            botaoCreditos,
            botaoConfiguracoes,
            botaoSair
        };

        if (botaoCreditos != null)
        {
            botaoCreditos.onClick.RemoveAllListeners();
            botaoCreditos.onClick.AddListener(AbrirCreditos);
        }

        if (botaoFecharCreditos != null)
        {
            botaoFecharCreditos.onClick.RemoveAllListeners();
            botaoFecharCreditos.onClick.AddListener(FecharCreditos);
        }

        if (botaoStart != null)
        {
            botaoStart.onClick.RemoveAllListeners();
            botaoStart.onClick.AddListener(IniciarJogo);
        }

        if (botaoSair != null)
        {
            botaoSair.onClick.RemoveAllListeners();
            botaoSair.onClick.AddListener(AbrirConfirmacao);
        }

        if (botaoSim != null)
        {
            botaoSim.onClick.RemoveAllListeners();
            botaoSim.onClick.AddListener(SairDoJogo);
        }

        if (botaoNao != null)
        {
            botaoNao.onClick.RemoveAllListeners();
            botaoNao.onClick.AddListener(FecharConfirmacao);
        }

        if (botaoConfiguracoes != null)
        {
            botaoConfiguracoes.onClick.RemoveAllListeners();
            botaoConfiguracoes.onClick.AddListener(AbrirConfiguracoes);
        }

        if (botaoFecharConfiguracoes != null)
        {
            botaoFecharConfiguracoes.onClick.RemoveAllListeners();
            botaoFecharConfiguracoes.onClick.AddListener(FecharConfiguracoes);
        }

        SelecionarBotaoComAtraso(primeiroBotaoMenuInicial != null ? primeiroBotaoMenuInicial : (botaoStart != null ? botaoStart.gameObject : null));
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Se algum painel está aberto, Esc fecha ele
        if (painelCreditos != null && painelCreditos.activeSelf)      { FecharCreditos();       return; }
        if (painelConfiguracoes != null && painelConfiguracoes.activeSelf) { FecharConfiguracoes(); return; }
        if (painelConfirmacao != null && painelConfirmacao.activeSelf)  { FecharConfirmacao();    return; }

        // Nenhum painel aberto — abre confirmação de saída
        AbrirConfirmacao();
    }

    void AbrirCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(true);

        DefinirBotoesMenuPrincipalInterativos(false);
        SelecionarBotaoComAtraso(botaoFecharCreditos != null ? botaoFecharCreditos.gameObject : null);
    }

    void FecharCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(false);

        DefinirBotoesMenuPrincipalInterativos(true);
        SelecionarBotaoComAtraso(botaoCreditos != null ? botaoCreditos.gameObject : null);
    }

    void IniciarJogo()
    {
        SceneManager.LoadScene("ModoJogador");
    }

    void AbrirConfirmacao()
    {
        if (painelConfirmacao != null)
            painelConfirmacao.SetActive(true);

        DefinirBotoesMenuPrincipalInterativos(false);
        SelecionarBotaoComAtraso(botaoNao != null ? botaoNao.gameObject : (botaoSim != null ? botaoSim.gameObject : null));
    }

    void FecharConfirmacao()
    {
        if (painelConfirmacao != null)
            painelConfirmacao.SetActive(false);

        DefinirBotoesMenuPrincipalInterativos(true);
        SelecionarBotaoComAtraso(botaoSair != null ? botaoSair.gameObject : null);
    }

    void SairDoJogo()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }

    void AbrirConfiguracoes()
    {
        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(true);

        DefinirBotoesMenuPrincipalInterativos(false);

        // O prefab de configurações já deve selecionar o próprio primeiro botão,
        // mas deixamos um fallback aqui para o botão fechar.
        SelecionarBotaoComAtraso(botaoFecharConfiguracoes != null ? botaoFecharConfiguracoes.gameObject : null);
    }

    void FecharConfiguracoes()
    {
        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(false);

        DefinirBotoesMenuPrincipalInterativos(true);
        SelecionarBotaoComAtraso(botaoConfiguracoes != null ? botaoConfiguracoes.gameObject : null);
    }

    void DefinirBotoesMenuPrincipalInterativos(bool ativo)
    {
        if (botoesMenuPrincipal == null)
            return;

        for (int i = 0; i < botoesMenuPrincipal.Length; i++)
        {
            if (botoesMenuPrincipal[i] != null)
                botoesMenuPrincipal[i].interactable = ativo;
        }
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