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
        ConectarNavegacaoAudio();
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
        ConectarNavegacaoControles();
        SelecionarControleDepoisDeAtualizar(painelControles, "P1_MoverDireita");
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

    private void ConectarNavegacaoAudio()
    {
        if (painelAudio == null)
            return;

        Selectable menuAudio = EncontrarSelectable(painelPrincipal, "BotaoAudio");
        Selectable voltar = EncontrarSelectable(painelPrincipal, "BotaoVoltar");
        Selectable voltarAudio = EncontrarSelectable(painelAudio, "BotaoVoltarAudio");
        Selectable volumeGeral = EncontrarSelectable(painelAudio, "SliderVolumeGeral");
        Selectable volumeMusica = EncontrarSelectable(painelAudio, "SliderVolumeMusica");
        Selectable volumeEfeitos = EncontrarSelectable(painelAudio, "SliderVolumeEfeitos");
        Selectable som = EncontrarSelectable(painelAudio, "ToggleSom");

        // A aba de audio segue a ordem visual dos controles:
        //
        //   Volume geral
        //        |
        //   Volume musica
        //        |
        //   Volume SFX
        //        |
        //   Som ativado
        //
        // Os sliders tambem podem voltar para a aba AUDIO pela esquerda e
        // chegar ao botao de retorno superior pela direita.
        DefinirNavegacao(volumeGeral, null, volumeMusica, menuAudio, voltarAudio);
        DefinirNavegacao(volumeMusica, volumeGeral, volumeEfeitos, menuAudio, voltarAudio);
        DefinirNavegacao(volumeEfeitos, volumeMusica, som, menuAudio, voltarAudio);
        DefinirNavegacao(som, volumeEfeitos, null, voltar, null);

        // O botao superior pode ser alcancado por qualquer slider e retorna ao
        // primeiro slider quando o usuario pressiona esquerda ou baixo.
        DefinirNavegacao(voltarAudio, null, volumeGeral, volumeGeral, null);

        if (voltar != null)
        {
            Navigation navegacaoVoltar = voltar.navigation;
            navegacaoVoltar.selectOnRight = som;
            voltar.navigation = navegacaoVoltar;
        }

        if (menuAudio != null)
        {
            Navigation navegacaoMenu = menuAudio.navigation;
            navegacaoMenu.selectOnRight = volumeGeral;
            menuAudio.navigation = navegacaoMenu;
        }
    }

    private void ConectarNavegacaoControles()
    {
        if (painelControles == null)
            return;

        Selectable menuControles = EncontrarSelectable(painelPrincipal, "BotaoControles");
        Selectable voltar = EncontrarSelectable(painelPrincipal, "BotaoVoltar");
        Selectable voltarControles = EncontrarSelectable(painelControles, "BotaoVoltarControles");
        Selectable restaurar = EncontrarSelectable(painelControles, "RestaurarControle");

        Selectable[] p1 =
        {
            EncontrarSelectable(painelControles, "P1_MoverDireita"),
            EncontrarSelectable(painelControles, "P1_MoverEsquerda"),
            EncontrarSelectable(painelControles, "P1_Ataque"),
            EncontrarSelectable(painelControles, "P1_Especial"),
            EncontrarSelectable(painelControles, "P1_Ultimate"),
            EncontrarSelectable(painelControles, "P1_Defesa")
        };

        Selectable[] p2 =
        {
            EncontrarSelectable(painelControles, "P2_MoverDireita"),
            EncontrarSelectable(painelControles, "P2_MoverEsquerda"),
            EncontrarSelectable(painelControles, "P2_Ataque"),
            EncontrarSelectable(painelControles, "P2_Especial"),
            EncontrarSelectable(painelControles, "P2_Ultimate"),
            EncontrarSelectable(painelControles, "P2_Defesa")
        };

        // Cada coluna segue a ordem visual da tela. A esquerda de qualquer
        // tecla do P1 retorna para o menu CONTROLES; a direita de qualquer
        // tecla do P2 leva ao botao de voltar do canto superior direito.
        for (int i = 0; i < p1.Length; i++)
        {
            Selectable acima = i > 0 ? p1[i - 1] : null;
            Selectable abaixo = i < p1.Length - 1 ? p1[i + 1] : restaurar;
            DefinirNavegacao(p1[i], acima, abaixo, menuControles, p2[i]);

            acima = i > 0 ? p2[i - 1] : null;
            abaixo = i < p2.Length - 1 ? p2[i + 1] : restaurar;
            DefinirNavegacao(p2[i], acima, abaixo, p1[i], voltarControles);
        }

        // O botao superior direito retorna para a primeira tecla do P2.
        // Usar a mesma rota em cima e na esquerda evita que o foco fique preso
        // quando o usuario chega nele por qualquer uma das linhas.
        DefinirNavegacao(voltarControles, null, p2[0], p2[0], null);

        // As duas defesas chegam ao botao RESTAURAR. A partir dele, cima volta
        // para a defesa do P1 e direita alcança a defesa do P2, como no layout.
        DefinirNavegacao(restaurar, p1[p1.Length - 1], null, voltar, p2[p2.Length - 1]);

        if (voltar != null)
        {
            Navigation navegacaoVoltar = voltar.navigation;
            navegacaoVoltar.selectOnRight = restaurar;
            voltar.navigation = navegacaoVoltar;
        }

        if (menuControles != null)
        {
            Navigation navegacaoMenu = menuControles.navigation;
            navegacaoMenu.selectOnRight = p1[0];
            menuControles.navigation = navegacaoMenu;
        }
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
