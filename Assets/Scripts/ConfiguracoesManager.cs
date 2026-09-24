using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfiguracoesManager : MonoBehaviour
{
    public GameObject painelConfiguracoes;
    public GameObject painelPrincipal;
    public GameObject painelAudio;
    public GameObject painelVideo;
    public GameObject painelControles;

    private PauseManager pauseManager;
    private Coroutine selecionarFocoCoroutine;

    void Update()
    {
        if (painelConfiguracoes == null || !painelConfiguracoes.activeInHierarchy)
            return;

        // F/K (ou as teclas remapeadas) funcionam como confirmação nos menus,
        // inclusive para abrir Dropdowns e acionar o botão FECHAR.
        if (UIInputUtility.WasPlayerConfirmPressed())
            UIInputUtility.SubmitSelected();
    }

    void Awake()
    {
        pauseManager = FindFirstObjectByType<PauseManager>();
    }

    void AbrirPainelBase()
    {
        // No menu principal não existe PauseManager. Na luta, qualquer caminho
        // para as configurações precisa garantir que o combate esteja pausado.
        if (pauseManager == null)
            pauseManager = FindFirstObjectByType<PauseManager>();

        if (pauseManager != null && !pauseManager.EstaPausado())
            pauseManager.Pausar();

        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(true);

        // O painel da luta inicia desativado e pode ser aberto depois que o
        // LanguageManager ja foi criado. Atualiza o rotulo PT/EN no proprio
        // momento da abertura para nunca deixar o botao visualmente vazio.
        var botoesIdioma = FindObjectsByType<LanguageButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var botaoIdioma in botoesIdioma)
        {
            if (botaoIdioma != null)
                botaoIdioma.AtualizarTexto();
        }
    }

    public void AbrirPainelPrincipal()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
        ConfigurarNavegacaoPainel(painelPrincipal);
        SelecionarControleDepoisDeAtualizar(painelPrincipal, "BotaoAudio");
    }

    public void AbrirPainelAudio()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(true);
        painelVideo.SetActive(false);
        painelControles.SetActive(false);
        ConfigurarNavegacaoPainel(painelAudio);
        ConectarPainelAoMenu(painelAudio, "SliderVolumeGeral", "BotaoAudio");
        SelecionarControleDepoisDeAtualizar(painelAudio, "SliderVolumeGeral");
    }

    public void AbrirPainelVideo()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(true);
        painelControles.SetActive(false);
        PrepararDropdownsVideo();
        ConfigurarNavegacaoPainel(painelVideo);
        ConectarNavegacaoVideo();
        SelecionarControleDepoisDeAtualizar(painelVideo, "DropdownResolucao");
    }

    public void AbrirPainelControles()
    {
        AbrirPainelBase();
        painelPrincipal.SetActive(true);
        painelAudio.SetActive(false);
        painelVideo.SetActive(false);
        painelControles.SetActive(true);
        ConfigurarNavegacaoPainel(painelControles);
        ConectarPainelAoMenu(painelControles, "RestaurarControle", "BotaoControles");
        SelecionarControleDepoisDeAtualizar(painelControles, "RestaurarControle");
    }

    public void FecharConfiguracoes()
    {
        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(false);

        // Fechar pelo botão ou pelo ESC, vindo da luta, retorna ao pause sem
        // liberar o tempo do jogo.
        if (pauseManager != null && pauseManager.EstaPausado())
            pauseManager.ReabrirPainelPause();
    }

    public bool EstaAberta()
    {
        return painelConfiguracoes != null && painelConfiguracoes.activeInHierarchy;
    }

    private void SelecionarControleDepoisDeAtualizar(GameObject painel, string nomePreferido)
    {
        if (selecionarFocoCoroutine != null)
            StopCoroutine(selecionarFocoCoroutine);

        selecionarFocoCoroutine = StartCoroutine(SelecionarControleNoProximoFrame(painel, nomePreferido));
    }

    private IEnumerator SelecionarControleNoProximoFrame(GameObject painel, string nomePreferido)
    {
        // Os paineis sao ativados dentro do callback de um Button. Esperar um
        // frame evita que o EventSystem reprocesse o clique antigo e devolva o
        // foco ao botao que abriu a aba.
        yield return null;
        Canvas.ForceUpdateCanvases();

        Selectable controle = EncontrarSelectable(painel, nomePreferido);
        if (controle == null)
            controle = ObterPrimeiroSelectableDireto(painel);

        if (controle == null || !controle.interactable || !controle.gameObject.activeInHierarchy)
            yield break;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            yield break;

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(controle.gameObject);
        selecionarFocoCoroutine = null;
    }

    private void ConfigurarNavegacaoPainel(GameObject painel)
    {
        if (painel == null)
            return;

        List<Selectable> controles = ObterSelectablesDiretos(painel);
        for (int i = 0; i < controles.Count; i++)
        {
            Selectable controle = controles[i];
            Navigation navegacao = controle.navigation;
            navegacao.mode = Navigation.Mode.Explicit;
            navegacao.wrapAround = false;
            navegacao.selectOnUp = EncontrarVizinho(controle, controles, Vector2.up);
            navegacao.selectOnDown = EncontrarVizinho(controle, controles, Vector2.down);
            navegacao.selectOnLeft = EncontrarVizinho(controle, controles, Vector2.left);
            navegacao.selectOnRight = EncontrarVizinho(controle, controles, Vector2.right);
            controle.navigation = navegacao;
        }
    }

    private void ConectarNavegacaoVideo()
    {
        if (painelVideo == null)
            return;

        Selectable resolucao = EncontrarSelectable(painelVideo, "DropdownResolucao");
        Selectable modoTela = EncontrarSelectable(painelVideo, "DropdownModoTela");
        Selectable sliderFPS = EncontrarSelectable(painelVideo, "SliderFPS");
        Selectable dropdownHZ = EncontrarSelectable(painelVideo, "DropdownHZ");
        Selectable restaurar = EncontrarSelectable(painelVideo, "BotaoRestaurar");
        Selectable aplicar = EncontrarSelectable(painelVideo, "BotaoAplicar");
        Selectable voltar = EncontrarSelectable(painelVideo, "BotaoVoltarVideo");
        Selectable menuVideo = EncontrarSelectable(painelPrincipal, "BotaoVideo");

        // A aba de video funciona como uma grade de duas colunas:
        //
        //   Resolucao  ->  Hz
        //       |          |
        //   Modo tela  ->  FPS
        //       |          |
        //   Restaurar -> Aplicar
        //
        // As ligacoes sao fixas para que a escala da tela nao altere a rota.
        DefinirNavegacao(resolucao, menuVideo, modoTela, menuVideo, dropdownHZ);
        DefinirNavegacao(modoTela, resolucao, restaurar, menuVideo, sliderFPS);
        DefinirNavegacao(restaurar, modoTela, null, voltar, aplicar);
        DefinirNavegacao(dropdownHZ, menuVideo, sliderFPS, resolucao, null);
        DefinirNavegacao(sliderFPS, dropdownHZ, aplicar, modoTela, null);
        DefinirNavegacao(aplicar, sliderFPS, null, restaurar, null);
        DefinirNavegacao(voltar, null, null, null, restaurar);

        // Ao sair para a coluna de abas, o retorno para Video continua
        // deterministico; ao entrar novamente, a resolucao recebe o foco.
        DefinirNavegacao(menuVideo, EncontrarSelectable(painelPrincipal, "BotaoAudio"),
            EncontrarSelectable(painelPrincipal, "BotaoControles"), null, resolucao);
    }

    private void PrepararDropdownsVideo()
    {
        if (painelVideo == null)
            return;

        TMP_Dropdown[] dropdowns = painelVideo.GetComponentsInChildren<TMP_Dropdown>(true);
        for (int i = 0; i < dropdowns.Length; i++)
            DropdownAcessivel.Preparar(dropdowns[i]);
    }

    private void ConectarPainelAoMenu(GameObject painel, string primeiroControle, string nomeBotaoMenu)
    {
        Selectable primeiro = EncontrarSelectable(painel, primeiroControle);
        Selectable botaoMenu = EncontrarSelectable(painelPrincipal, nomeBotaoMenu);
        if (primeiro == null || botaoMenu == null)
            return;

        Navigation navegacaoPrimeiro = primeiro.navigation;
        navegacaoPrimeiro.selectOnLeft = botaoMenu;
        primeiro.navigation = navegacaoPrimeiro;

        Navigation navegacaoMenu = botaoMenu.navigation;
        navegacaoMenu.selectOnRight = primeiro;
        botaoMenu.navigation = navegacaoMenu;
    }

    private static void DefinirNavegacao(Selectable controle, Selectable cima,
        Selectable baixo, Selectable esquerda, Selectable direita)
    {
        if (controle == null)
            return;

        Navigation navegacao = controle.navigation;
        navegacao.mode = Navigation.Mode.Explicit;
        navegacao.wrapAround = false;
        navegacao.selectOnUp = cima;
        navegacao.selectOnDown = baixo;
        navegacao.selectOnLeft = esquerda;
        navegacao.selectOnRight = direita;
        controle.navigation = navegacao;
    }

    private static List<Selectable> ObterSelectablesDiretos(GameObject painel)
    {
        List<Selectable> resultado = new List<Selectable>();
        if (painel == null)
            return resultado;

        Selectable[] encontrados = painel.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < encontrados.Length; i++)
        {
            Selectable encontrado = encontrados[i];
            if (encontrado == null || encontrado.transform.parent != painel.transform)
                continue;

            if (!resultado.Contains(encontrado))
                resultado.Add(encontrado);
        }

        return resultado;
    }

    private static Selectable ObterPrimeiroSelectableDireto(GameObject painel)
    {
        List<Selectable> controles = ObterSelectablesDiretos(painel);
        for (int i = 0; i < controles.Count; i++)
        {
            if (controles[i].interactable && controles[i].gameObject.activeInHierarchy)
                return controles[i];
        }

        return null;
    }

    private static Selectable EncontrarSelectable(GameObject painel, string nome)
    {
        if (painel == null || string.IsNullOrEmpty(nome))
            return null;

        Transform[] transforms = painel.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transformacao = transforms[i];
            if (transformacao != null && transformacao.name == nome)
                return transformacao.GetComponent<Selectable>();
        }

        return null;
    }

    private static Selectable EncontrarVizinho(Selectable origem, List<Selectable> controles, Vector2 direcao)
    {
        if (origem == null)
            return null;

        Vector2 pontoOrigem = ObterCentro(origem);
        Selectable melhor = null;
        float melhorPontuacao = float.MaxValue;

        for (int i = 0; i < controles.Count; i++)
        {
            Selectable candidato = controles[i];
            if (candidato == null || candidato == origem || !candidato.interactable ||
                !candidato.gameObject.activeInHierarchy)
                continue;

            Vector2 delta = ObterCentro(candidato) - pontoOrigem;
            float distanciaPrincipal = Vector2.Dot(delta, direcao);
            if (distanciaPrincipal <= 0.1f)
                continue;

            float distanciaLateral = Mathf.Abs(Vector2.Dot(delta, new Vector2(-direcao.y, direcao.x)));
            float pontuacao = distanciaPrincipal + distanciaLateral * 0.15f;
            if (pontuacao < melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhor = candidato;
            }
        }

        return melhor;
    }

    private static Vector2 ObterCentro(Selectable controle)
    {
        RectTransform rect = controle != null ? controle.transform as RectTransform : null;
        if (rect == null)
            return controle != null ? (Vector2)controle.transform.position : Vector2.zero;

        return rect.TransformPoint(rect.rect.center);
    }
}
