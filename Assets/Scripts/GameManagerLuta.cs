using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class GameManagerLuta : MonoBehaviour
{
    [Header("Players da cena")]
    public LutadorController2D player1;
    public LutadorController2D player2;

    [Header("Dados dos personagens")]
    public DadosPersonagem[] personagensDisponiveis;

    [Header("UI Vida")]
    public Slider barraVidaP1;
    public Slider barraVidaP2;

    [Header("UI Energia")]
    public Slider barraEnergiaP1;
    public Slider barraEnergiaP2;

    [Header("Retratos na HUD")]
    [Tooltip("Rosto exibido no círculo da moldura da barra do Player 1")]
    public Image rostoP1;
    [Tooltip("Rosto exibido no círculo da moldura da barra do Player 2")]
    public Image rostoP2;

    // Faixa temporária que mostra o custo que faltou quando uma habilidade é
    // recusada por falta de energia. Ela é criada dentro do Fill Area da
    // própria Slider, então acompanha exatamente o desenho da barra.
    private Image indicadorEnergiaP1;
    private Image indicadorEnergiaP2;
    private Coroutine rotinaIndicadorEnergiaP1;
    private Coroutine rotinaIndicadorEnergiaP2;

    [Header("UI Nomes")]
    public TextMeshProUGUI nomeP1;
    public TextMeshProUGUI nomeP2;

    [Header("UI Controles")]
    public TextMeshProUGUI controlesP1;
    public TextMeshProUGUI controlesP2;
    public float tempoExibirControles = 4f;
    [Tooltip("Mantém as teclas de ataque, especial e ultimate visíveis durante toda a luta.")]
    public bool manterControlesVisiveis = true;

    [Header("UI Round e Tempo")]
    public TextMeshProUGUI textoRound;
    public TextMeshProUGUI textoTempo;
    public TextMeshProUGUI placarRoundsP1;
    public TextMeshProUGUI placarRoundsP2;

    [Header("Bolinhas de rounds")]
    public Image[] bolinhasP1;
    public Image[] bolinhasP2;
    public Sprite spriteBolinhaVaziaP1;
    public Sprite spriteBolinhaGanhaP1;
    public Sprite spriteBolinhaVaziaP2;
    public Sprite spriteBolinhaGanhaP2;

    [Header("Fim de luta")]
    public GameObject painelFim;
    [Tooltip("Texto grande — palavra 'VITÓRIA' / 'VICTORY'")]
    public TextMeshProUGUI textoTituloVencedor;
    [Tooltip("Texto menor — 'Nome - P1' ou 'Nome - P2'")]
    public TextMeshProUGUI textoNomeVencedor;
    [Tooltip("Campo legado — mantido para compatibilidade, pode deixar vazio")]
    public TextMeshProUGUI textoVencedor;
    [Tooltip("Imagem que recebe o rosto do personagem vencedor")]
    public Image imagemRostoVencedor;
    [Tooltip("Sprite do balão usado ao lado do personagem vencedor")]
    public Sprite spriteBalaoVencedor;
    public Button botaoReiniciar;
    public Button botaoVoltar;

    [Header("Round Win")]
    [Tooltip("Painel que aparece entre rounds mostrando quem ganhou")]
    public GameObject painelRoundWin;
    [Tooltip("Texto grande — palavra 'VENCEDOR' / 'WINNER'")]
    public TextMeshProUGUI textoRoundWinTitulo;
    [Tooltip("Texto menor — 'Nome - P1' ou 'Nome - P2'")]
    public TextMeshProUGUI textoRoundWinNome;
    [Tooltip("Campo legado — mantido para compatibilidade, pode deixar vazio")]
    public TextMeshProUGUI textoRoundWin;

    private string modoJogo;
    private bool lutaTerminou = false;
    private bool roundEmAndamento = false;

    private int roundsSelecionados = 3;
    private int vitoriasNecessarias = 2;
    private int vitoriasP1 = 0;
    private int vitoriasP2 = 0;
    private int roundAtual = 1;

    private int tempoSelecionado = 60;
    private float tempoAtual = 0f;

    [Header("Posições de spawn (arraste os Transforms aqui)")]
    [Tooltip("Se não configurado, usa a posição inicial do próprio player")]
    public Transform spawnP1;
    public Transform spawnP2;

    private Vector3 posInicialP1;
    private Vector3 posInicialP2;

    private Coroutine rotinaControles;
    [SerializeField, Min(0.5f)]
    [Tooltip("Intervalo entre os retratos dos dois personagens quando a partida termina empatada.")]
    private float intervaloRetratosEmpate = 1.25f;
    private Coroutine rotinaRetratosEmpate;
    private Image imagemBalaoVencedor;
    private TextMeshProUGUI textoBalaoVencedor;

    void Awake()
    {
        // Time.timeScale é global e continua com o mesmo valor ao trocar de
        // cena. Se a resolução foi alterada a partir do menu de pause, a arena
        // podia carregar com o tempo em zero: o HUD aparecia, mas a gravidade,
        // o cronômetro e os controles ficavam congelados. Toda cena de luta
        // nova deve começar em tempo normal; o PauseManager poderá pausar
        // novamente depois que a rodada estiver ativa.
        Time.timeScale = 1f;
    }

    void Start()
    {
        modoJogo = PlayerPrefs.GetString("ModoJogo", "PVP");

        roundsSelecionados = PlayerPrefs.GetInt("RoundsSelecionados", 3);
        tempoSelecionado = PlayerPrefs.GetInt("TempoSelecionado", 60);

        if (roundsSelecionados == 1) vitoriasNecessarias = 1;
        else if (roundsSelecionados == 3) vitoriasNecessarias = 2;
        else if (roundsSelecionados == 5) vitoriasNecessarias = 3;
        else
        {
            roundsSelecionados = 3;
            vitoriasNecessarias = 2;
        }

        int personagemP1 = PlayerPrefs.GetInt("PersonagemP1", 0);
        int personagemP2 = PlayerPrefs.GetInt("PersonagemP2", 0);

        // A cena pode ter vindo de um teste com referências vazias ou trocadas.
        // A luta sempre deve tratar cada lutador como oponente do outro; isso
        // evita ataques acertando a própria instância e também libera a IA para
        // calcular distância/alcance corretamente.
        if (player1 != null && player2 != null)
        {
            player1.oponente = player2;
            player2.oponente = player1;
        }

        if (player1 != null && personagemP1 >= 0 && personagemP1 < personagensDisponiveis.Length)
        {
            player1.dadosPersonagem = personagensDisponiveis[personagemP1];
            player1.AplicarDadosPersonagem();
        }

        if (player2 != null && personagemP2 >= 0 && personagemP2 < personagensDisponiveis.Length)
        {
            player2.dadosPersonagem = personagensDisponiveis[personagemP2];
            player2.AplicarDadosPersonagem();
        }

        AtualizarRetratosHUD();

        // BUG 23 — usa SpawnPoint fixo se configurado, senão usa posição atual
        posInicialP1 = spawnP1 != null ? spawnP1.position : (player1 != null ? player1.transform.position : Vector3.zero);
        posInicialP2 = spawnP2 != null ? spawnP2.position : (player2 != null ? player2.transform.position : Vector3.zero);

        if (painelFim != null)
            painelFim.SetActive(false);

        if (botaoReiniciar != null)
        {
            botaoReiniciar.onClick.RemoveAllListeners();
            botaoReiniciar.onClick.AddListener(ReiniciarLuta);
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveAllListeners();
            botaoVoltar.onClick.AddListener(VoltarParaSelecao);
        }

        AtualizarTextoControles();
        AtualizarPlacar();
        IniciarRound();

        // Subscreve ao evento de mudança de idioma
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
    }

    void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
    }

    /// <summary>
    /// Chamado automaticamente quando o idioma muda.
    /// Reatualiza todos os textos compostos da cena de luta.
    /// </summary>
    void AtualizarTextosIdioma()
    {
        AtualizarUI();
        AtualizarTextoRound();
        AtualizarTextoTempo();
        AtualizarTextoControles();
    }

    void Update()
    {
        if (lutaTerminou)
            return;

        AtualizarUI();

        if (roundEmAndamento && tempoSelecionado != -1)
        {
            tempoAtual -= Time.deltaTime;

            if (tempoAtual < 0f)
                tempoAtual = 0f;

            AtualizarTextoTempo();

            if (tempoAtual <= 0f)
            {
                roundEmAndamento = false;
                FinalizarRoundPorTempo();
            }
        }
        else if (!lutaTerminou && tempoSelecionado == -1)
        {
            AtualizarTextoTempo();
        }
    }

    void IniciarRound()
    {
        if (lutaTerminou)
            return;

        if (player1 != null)
        {
            player1.transform.position = posInicialP1;

            if (player1.rb != null)
            {
                player1.rb.linearVelocity = Vector2.zero;
                player1.rb.angularVelocity = 0f;
                player1.rb.position = posInicialP1;
            }

            player1.ResetarParaNovoRound();
        }

        if (player2 != null)
        {
            player2.transform.position = posInicialP2;

            if (player2.rb != null)
            {
                player2.rb.linearVelocity = Vector2.zero;
                player2.rb.angularVelocity = 0f;
                player2.rb.position = posInicialP2;
            }

            player2.ResetarParaNovoRound();
        }

        // Objetos arremessáveis são consumidos por round, nas duas arenas.
        // O reinício completo já recria a cena; aqui garantimos o reset na
        // transição normal entre rounds.
        ObjetoInterativoLuta.ResetarObjetosDaRodada();

        tempoAtual = tempoSelecionado;
        roundEmAndamento = true;

        AtualizarUI();
        AtualizarTextoRound();
        AtualizarTextoTempo();
        AtualizarPlacar();
        MostrarControlesTemporariamente();
    }

    void AtualizarUI()
    {
        string labelP1 = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("PLAYER1")
            : "Player 1";
        string labelP2 = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("PLAYER2")
            : "Player 2";

        if (player1 != null && player1.dadosPersonagem != null)
        {
            if (barraVidaP1 != null)
            {
                barraVidaP1.minValue = 0f;
                barraVidaP1.maxValue = player1.dadosPersonagem.vidaMax;
                barraVidaP1.value = player1.vidaAtual;
            }

            if (barraEnergiaP1 != null)
            {
                barraEnergiaP1.maxValue = player1.dadosPersonagem.energiaMax;
                barraEnergiaP1.value = player1.energiaAtual;
            }

            if (nomeP1 != null)
            {
                nomeP1.text = labelP1 + " - " + player1.dadosPersonagem.nomePersonagem;
                nomeP1.enableAutoSizing = true;
                if (nomeP1.fontSizeMin <= 0f) nomeP1.fontSizeMin = 8f;
            }
        }

        if (player2 != null && player2.dadosPersonagem != null)
        {
            if (barraVidaP2 != null)
            {
                barraVidaP2.minValue = 0f;
                barraVidaP2.maxValue = player2.dadosPersonagem.vidaMax;
                barraVidaP2.value = player2.vidaAtual;
            }

            if (barraEnergiaP2 != null)
            {
                barraEnergiaP2.maxValue = player2.dadosPersonagem.energiaMax;
                barraEnergiaP2.value = player2.energiaAtual;
            }

            if (nomeP2 != null)
            {
                nomeP2.text = labelP2 + " - " + player2.dadosPersonagem.nomePersonagem;
                nomeP2.enableAutoSizing = true;
                if (nomeP2.fontSizeMin <= 0f) nomeP2.fontSizeMin = 8f;
            }
        }
    }

    private void AtualizarRetratosHUD()
    {
        ConfigurarRetratoHUD(rostoP1, player1);
        ConfigurarRetratoHUD(rostoP2, player2);
    }

    private void ConfigurarRetratoHUD(Image imagem, LutadorController2D jogador)
    {
        if (imagem == null)
            return;

        Sprite rosto = jogador != null && jogador.dadosPersonagem != null
            ? jogador.dadosPersonagem.spriteRosto
            : null;

        imagem.sprite = rosto;
        imagem.enabled = rosto != null;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        imagem.color = Color.white;
    }

    public void MostrarIndicadorEnergiaInsuficiente(LutadorController2D jogador, float custo)
    {
        if (jogador == null || jogador.dadosPersonagem == null || custo <= jogador.energiaAtual)
            return;

        bool eJogador1 = jogador == player1 || jogador.numeroJogador == 1;
        Slider barra = eJogador1 ? barraEnergiaP1 : barraEnergiaP2;
        if (barra == null)
            return;

        float energiaMax = jogador.dadosPersonagem.energiaMax;
        if (energiaMax <= 0f)
            return;

        // Sincroniza o preenchimento antes de medir o trecho faltante. Assim,
        // se a energia regenerou no mesmo frame do comando, o indicador usa a
        // posição exata da barra no instante em que a habilidade foi tentada.
        barra.SetValueWithoutNotify(Mathf.Clamp(jogador.energiaAtual, barra.minValue, barra.maxValue));

        Image indicador = eJogador1 ? indicadorEnergiaP1 : indicadorEnergiaP2;
        if (indicador == null)
        {
            indicador = CriarIndicadorEnergia(barra, eJogador1 ? "EnergiaP1Insuficiente" : "EnergiaP2Insuficiente");
            if (eJogador1) indicadorEnergiaP1 = indicador;
            else indicadorEnergiaP2 = indicador;
        }

        if (indicador == null)
            return;

        float energiaAtualNormalizada = Mathf.Clamp01(jogador.energiaAtual / energiaMax);
        float custoNormalizado = Mathf.Clamp01(custo / energiaMax);
        if (custoNormalizado <= energiaAtualNormalizada)
            return;

        RectTransform faixa = indicador.rectTransform;
        RectTransform areaIndicador = barra.fillRect != null
            ? barra.fillRect.parent as RectTransform
            : barra.transform as RectTransform;
        if (areaIndicador == null)
            return;

        // A barra azul continua sendo o primeiro layer e o indicador fica como
        // segundo layer dentro do Fill Area. A imagem não recebe raycast, então
        // o Slider continua atualizando a energia normalmente por baixo dela.
        bool preenchimentoInvertido = barra.direction == Slider.Direction.RightToLeft;
        float inicio = preenchimentoInvertido
            ? 1f - custoNormalizado
            : energiaAtualNormalizada;
        float fim = preenchimentoInvertido
            ? 1f - energiaAtualNormalizada
            : custoNormalizado;

        faixa.SetParent(areaIndicador, false);
        faixa.SetAsLastSibling();
        faixa.anchorMin = new Vector2(inicio, 0f);
        faixa.anchorMax = new Vector2(fim, 1f);
        faixa.offsetMin = Vector2.zero;
        faixa.offsetMax = Vector2.zero;

        indicador.color = Color.red;
        indicador.enabled = true;

        Coroutine rotinaAnterior = eJogador1 ? rotinaIndicadorEnergiaP1 : rotinaIndicadorEnergiaP2;
        if (rotinaAnterior != null)
            StopCoroutine(rotinaAnterior);

        Coroutine rotinaNova = StartCoroutine(PiscarIndicadorEnergia(indicador, eJogador1));
        if (eJogador1) rotinaIndicadorEnergiaP1 = rotinaNova;
        else rotinaIndicadorEnergiaP2 = rotinaNova;
    }

    Image CriarIndicadorEnergia(Slider barra, string nome)
    {
        RectTransform area = barra.fillRect != null
            ? barra.fillRect.parent as RectTransform
            : barra.transform as RectTransform;

        if (area == null)
            return null;

        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(area, false);
        objeto.transform.SetAsLastSibling();

        RectTransform faixa = objeto.GetComponent<RectTransform>();
        faixa.anchorMin = new Vector2(0f, 0f);
        faixa.anchorMax = new Vector2(0f, 1f);
        faixa.offsetMin = Vector2.zero;
        faixa.offsetMax = Vector2.zero;

        Image indicador = objeto.GetComponent<Image>();
        Image imagemFill = barra.fillRect != null
            ? barra.fillRect.GetComponent<Image>()
            : null;

        if (imagemFill != null)
        {
            indicador.sprite = imagemFill.sprite;
            indicador.type = imagemFill.type;
            indicador.preserveAspect = imagemFill.preserveAspect;
            indicador.material = imagemFill.material;
        }

        indicador.raycastTarget = false;
        indicador.enabled = false;
        return indicador;
    }

    IEnumerator PiscarIndicadorEnergia(Image indicador, bool eJogador1)
    {
        const float duracao = 0.32f;
        float tempo = 0f;

        while (tempo < duracao && indicador != null)
        {
            float progresso = Mathf.Clamp01(tempo / duracao);
            float intensidade = progresso < 0.5f
                ? Mathf.Lerp(0.2f, 1f, progresso * 2f)
                : Mathf.Lerp(1f, 0f, (progresso - 0.5f) * 2f);
            indicador.color = new Color(1f, 0f, 0f, intensidade);
            tempo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (indicador != null)
            indicador.enabled = false;

        if (eJogador1) rotinaIndicadorEnergiaP1 = null;
        else rotinaIndicadorEnergiaP2 = null;
    }

    void AtualizarTextoControles()
    {
        if (controlesP1 != null)
        {
            string esq = FormatarTecla(PlayerPrefs.GetString("P1_Esquerda", "A"));
            string dir = FormatarTecla(PlayerPrefs.GetString("P1_Direita", "D"));
            string def = FormatarTecla(PlayerPrefs.GetString("P1_Defender", "S"));
            string atk = FormatarTecla(PlayerPrefs.GetString("P1_Ataque", "F"));
            string esp = FormatarTecla(PlayerPrefs.GetString("P1_Especial", "G"));
            string ult = FormatarTecla(PlayerPrefs.GetString("P1_Ultimate", "H"));

            controlesP1.text = $"P1 {esq}/{dir} DEF:{def} ATAQUE:{atk} ESPECIAL:{esp} ULTIMATE:{ult}";
            controlesP1.enableAutoSizing = true;
            if (controlesP1.fontSizeMin <= 0f) controlesP1.fontSizeMin = 8f;
        }

        if (controlesP2 != null)
        {
            string esq = FormatarTecla(PlayerPrefs.GetString("P2_Esquerda", "LeftArrow"));
            string dir = FormatarTecla(PlayerPrefs.GetString("P2_Direita", "RightArrow"));
            string def = FormatarTecla(PlayerPrefs.GetString("P2_Defender", "DownArrow"));
            string atk = FormatarTecla(PlayerPrefs.GetString("P2_Ataque", "K"));
            string esp = FormatarTecla(PlayerPrefs.GetString("P2_Especial", "L"));
            string ult = FormatarTecla(PlayerPrefs.GetString("P2_Ultimate", "M"));

            controlesP2.text = $"P2 {esq}/{dir} DEF:{def} ATAQUE:{atk} ESPECIAL:{esp} ULTIMATE:{ult}";
            controlesP2.enableAutoSizing = true;
            if (controlesP2.fontSizeMin <= 0f) controlesP2.fontSizeMin = 8f;
        }
    }

    void MostrarControlesTemporariamente()
    {
        if (rotinaControles != null)
            StopCoroutine(rotinaControles);

        rotinaControles = StartCoroutine(RotinaMostrarControles());
    }

    IEnumerator RotinaMostrarControles()
    {
        if (controlesP1 != null) controlesP1.gameObject.SetActive(true);
        if (controlesP2 != null) controlesP2.gameObject.SetActive(true);

        if (!manterControlesVisiveis)
        {
            yield return new WaitForSeconds(tempoExibirControles);

            if (controlesP1 != null) controlesP1.gameObject.SetActive(false);
            if (controlesP2 != null) controlesP2.gameObject.SetActive(false);
        }
    }

    void AtualizarTextoRound()
    {
        if (textoRound == null) return;

        string labelRound = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("ROUND_LABEL")
            : "Round";

        textoRound.text = labelRound + " " + roundAtual;
    }

    void AtualizarTextoTempo()
    {
        if (textoTempo == null)
            return;

        if (tempoSelecionado == -1)
            textoTempo.text = "∞";
        else
            textoTempo.text = Mathf.CeilToInt(tempoAtual).ToString();
    }

    void AtualizarPlacar()
    {
        AtualizarBolinhasRound();

        string labelRounds = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("ROUNDS_LABEL")
            : "Rounds";

        if (placarRoundsP1 != null)
        {
            placarRoundsP1.text = labelRounds + ": " + vitoriasP1;
            placarRoundsP1.enableAutoSizing = true;
            if (placarRoundsP1.fontSizeMin <= 0f) placarRoundsP1.fontSizeMin = 8f;
        }

        if (placarRoundsP2 != null)
        {
            placarRoundsP2.text = labelRounds + ": " + vitoriasP2;
            placarRoundsP2.enableAutoSizing = true;
            if (placarRoundsP2.fontSizeMin <= 0f) placarRoundsP2.fontSizeMin = 8f;
        }
    }

    void AtualizarBolinhasRound()
    {
        AtualizarBolinhasDeUmLado(bolinhasP1, vitoriasP1, spriteBolinhaVaziaP1, spriteBolinhaGanhaP1);
        AtualizarBolinhasDeUmLado(bolinhasP2, vitoriasP2, spriteBolinhaVaziaP2, spriteBolinhaGanhaP2);
    }

    void AtualizarBolinhasDeUmLado(Image[] bolinhas, int vitorias, Sprite spriteVazio, Sprite spriteGanho)
    {
        if (bolinhas == null || bolinhas.Length == 0) return;

        for (int i = 0; i < bolinhas.Length; i++)
        {
            if (bolinhas[i] == null) continue;

            bool deveMostrar = i < vitoriasNecessarias;
            bolinhas[i].gameObject.SetActive(deveMostrar);
            if (!deveMostrar) continue;

            if (spriteVazio != null && spriteGanho != null)
                bolinhas[i].sprite = i < vitorias ? spriteGanho : spriteVazio;
            else
                bolinhas[i].color = i < vitorias ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    string FormatarTecla(string tecla)
    {
        switch (tecla)
        {
            case "LeftArrow": return "<";
            case "RightArrow": return ">";
            case "UpArrow": return "^";
            case "DownArrow": return "v";
            case "Semicolon": return ";";
            default: return tecla;
        }
    }

    public void FinalizarLuta(LutadorController2D derrotado)
    {
        if (lutaTerminou || !roundEmAndamento)
            return;

        roundEmAndamento = false;

        // BUG 30 — zera vida do derrotado e força atualização da UI antes de tudo
        if (derrotado != null) derrotado.vidaAtual = 0;
        AtualizarUI();

        LutadorController2D vencedorRound = (derrotado == player1) ? player2 : player1;
        string nomeVencedor = vencedorRound.dadosPersonagem != null
            ? vencedorRound.dadosPersonagem.nomePersonagem
            : (vencedorRound == player1 ? "P1" : "P2");

        if (vencedorRound == player1) vitoriasP1++;
        else vitoriasP2++;

        AtualizarPlacar();

        if (player1 != null) player1.TravarLutador();
        if (player2 != null) player2.TravarLutador();

        if (VerificarFimPartida())
            return;

        MostrarRoundWin(nomeVencedor, roundAtual, vencedorRound);
        StartCoroutine(ProximoRoundDepois(2.5f));
    }

    void FinalizarRoundPorTempo()
    {
        if (lutaTerminou) return;

        if (player1 != null) player1.TravarLutador();
        if (player2 != null) player2.TravarLutador();

        string nomeVencedorRound = "";
        LutadorController2D vencedorRoundObj = null;

        if (player1.vidaAtual > player2.vidaAtual)
        {
            vitoriasP1++;
            nomeVencedorRound = player1.dadosPersonagem != null ? player1.dadosPersonagem.nomePersonagem : "P1";
            vencedorRoundObj = player1;
        }
        else if (player2.vidaAtual > player1.vidaAtual)
        {
            vitoriasP2++;
            nomeVencedorRound = player2.dadosPersonagem != null ? player2.dadosPersonagem.nomePersonagem : "P2";
            vencedorRoundObj = player2;
        }
        else
        {
            vitoriasP1++;
            vitoriasP2++;
            nomeVencedorRound = ""; // empate no round
        }

        AtualizarPlacar();

        if (VerificarFimPartida()) return;

        MostrarRoundWin(nomeVencedorRound, roundAtual, vencedorRoundObj);
        StartCoroutine(ProximoRoundDepois(2.5f));
    }

    bool VerificarFimPartida()
    {
        if (vitoriasP1 >= vitoriasNecessarias && vitoriasP2 >= vitoriasNecessarias)
        {
            lutaTerminou = true;
            MostrarPainelFim(null);
            return true;
        }

        if (vitoriasP1 >= vitoriasNecessarias)
        {
            lutaTerminou = true;
            MostrarPainelFim(player1);
            return true;
        }

        if (vitoriasP2 >= vitoriasNecessarias)
        {
            lutaTerminou = true;
            MostrarPainelFim(player2);
            return true;
        }

        return false;
    }

    void MostrarPainelFim(LutadorController2D vencedor)
    {
        if (rotinaRetratosEmpate != null)
        {
            StopCoroutine(rotinaRetratosEmpate);
            rotinaRetratosEmpate = null;
        }

        // Esconde os personagens para não aparecerem atrás do painel
        if (player1 != null)
        {
            var sr = player1.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
        if (player2 != null)
        {
            var sr = player2.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        if (painelFim != null)
            painelFim.SetActive(true);

        ConfigurarMolduraPainelVencedor();

        var lm = LanguageManager.Instance;

        // Texto 1 — palavra "VITÓRIA" / "VICTORY" (ou "EMPATE" / "DRAW")
        bool ehEmpate = vencedor == null;
        DefinirImagemEmpate(painelFim, ehEmpate);
        string tituloPainel = ehEmpate
            ? (lm != null ? lm.GetText("FIM_EMPATE") : "Empate!")
            : (lm != null ? lm.GetText("FIM_VITORIA") : "VITÓRIA");

        if (textoTituloVencedor != null)
        {
            textoTituloVencedor.text = tituloPainel;
        }

        // Texto 2 — "Nome - P1" ou "Nome - P2"
        if (textoNomeVencedor != null)
        {
            if (ehEmpate)
            {
                AtualizarNomePersonagemEmpate(player1);
            }
            else
            {
                string lado = vencedor == player1 ? "P1" : "P2";
                string nome = vencedor.dadosPersonagem != null
                    ? vencedor.dadosPersonagem.nomePersonagem
                    : lado;
                textoNomeVencedor.text = nome + " - " + lado;
                textoNomeVencedor.gameObject.SetActive(true);
            }
        }

        // No empate, começa mostrando o P1. A rotina alterna para o P2 logo em
        // seguida, sem deixar o painel parado por muito tempo.
        DefinirRetratoFim(ehEmpate ? player1 : vencedor);

        ConfigurarBalaoVencedor(ehEmpate ? player1 : vencedor, ehEmpate);

        if (ehEmpate && player1 != null && player2 != null)
            rotinaRetratosEmpate = StartCoroutine(AlternarRetratosEmpate());

        // Seleciona de verdade o primeiro botão real do painel pro teclado/controle
        // funcionar assim que a tela aparece — sem isso, nada está selecionado no
        // EventSystem até o mouse clicar em algo, e as setas não têm de onde partir.
        Button primeiroBotao = botaoReiniciar != null ? botaoReiniciar : botaoVoltar;
        if (primeiroBotao != null)
            StartCoroutine(SelecionarBotaoNoProximoFrame(primeiroBotao.gameObject));
    }

    private Vector2 ObterTamanhoCorpoNoPainel(LutadorController2D vencedor, Sprite corpo)
    {
        if (vencedor == null || corpo == null || corpo.pixelsPerUnit <= 0f)
            return new Vector2(184f, 335f);

        Canvas canvas = painelFim != null ? painelFim.GetComponentInParent<Canvas>() : null;
        CanvasScaler escalaCanvas = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        Camera cameraLuta = canvas != null && canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;

        if (cameraLuta == null || !cameraLuta.orthographic)
            return new Vector2(
                corpo.rect.width / corpo.pixelsPerUnit * 90f * Mathf.Abs(vencedor.transform.lossyScale.x),
                corpo.rect.height / corpo.pixelsPerUnit * 90f * Mathf.Abs(vencedor.transform.lossyScale.y));

        float alturaReferencia = escalaCanvas != null
            ? escalaCanvas.referenceResolution.y
            : 1080f;
        float pixelsPorUnidadeDeMundo = alturaReferencia / (cameraLuta.orthographicSize * 2f);
        Vector3 escala = vencedor.transform.lossyScale;

        return new Vector2(
            corpo.rect.width / corpo.pixelsPerUnit * Mathf.Abs(escala.x) * pixelsPorUnidadeDeMundo,
            corpo.rect.height / corpo.pixelsPerUnit * Mathf.Abs(escala.y) * pixelsPorUnidadeDeMundo);
    }

    private void ConfigurarMolduraPainelVencedor()
    {
        if (painelFim == null)
            return;

        Transform jogador = painelFim.transform.Find("jogador");
        if (jogador == null)
            return;

        // O Image no próprio objeto "jogador" é o fundo legado. A moldura nova
        // fica como filha desse container e deve continuar visível.
        Image fundoLegado = jogador.GetComponent<Image>();
        if (fundoLegado != null)
            fundoLegado.enabled = false;

        Image[] imagensDaMoldura = jogador.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imagensDaMoldura.Length; i++)
        {
            Image imagem = imagensDaMoldura[i];
            if (imagem == null || imagem == fundoLegado || imagem == imagemRostoVencedor)
                continue;

            imagem.enabled = true;
            imagem.raycastTarget = false;
            imagem.transform.SetAsFirstSibling();
        }
    }

    private void ConfigurarBalaoVencedor(LutadorController2D personagem, bool empate)
    {
        if (painelFim == null || spriteBalaoVencedor == null)
            return;

        Transform existente = painelFim.transform.Find("BalaoVencedor");
        if (existente == null)
        {
            GameObject objeto = new GameObject("BalaoVencedor", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(painelFim.transform, false);
            existente = objeto.transform;
        }

        imagemBalaoVencedor = existente.GetComponent<Image>();
        if (imagemBalaoVencedor == null)
            imagemBalaoVencedor = existente.gameObject.AddComponent<Image>();

        RectTransform rect = existente as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // Mantém o balão ao lado do personagem, sem cobrir o rosto nem o
        // centro da moldura. A posição é compartilhada pelas duas cenas.
        rect.anchoredPosition = new Vector2(250f, 155f);
        rect.sizeDelta = new Vector2(350f, 180f);

        imagemBalaoVencedor.sprite = spriteBalaoVencedor;
        imagemBalaoVencedor.type = Image.Type.Simple;
        imagemBalaoVencedor.preserveAspect = false;
        imagemBalaoVencedor.color = Color.white;
        imagemBalaoVencedor.raycastTarget = false;

        Transform textoExistente = existente.Find("Texto");
        if (textoExistente == null)
        {
            GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            textoObjeto.transform.SetParent(existente, false);
            textoExistente = textoObjeto.transform;
        }

        textoBalaoVencedor = textoExistente.GetComponent<TextMeshProUGUI>();
        if (textoBalaoVencedor == null)
            textoBalaoVencedor = textoExistente.gameObject.AddComponent<TextMeshProUGUI>();

        RectTransform rectTexto = textoExistente as RectTransform;
        rectTexto.anchorMin = new Vector2(0.13f, 0.18f);
        rectTexto.anchorMax = new Vector2(0.92f, 0.84f);
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;

        if (textoNomeVencedor != null)
            textoBalaoVencedor.font = textoNomeVencedor.font;
        textoBalaoVencedor.fontSize = 24f;
        textoBalaoVencedor.alignment = TextAlignmentOptions.Center;
        textoBalaoVencedor.enableWordWrapping = true;
        textoBalaoVencedor.overflowMode = TextOverflowModes.Overflow;
        textoBalaoVencedor.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        textoBalaoVencedor.raycastTarget = false;
        textoBalaoVencedor.text = empate
            ? ObterFraseEmpate(personagem)
            : ObterFraseVencedor(personagem);
        existente.SetAsLastSibling();
    }

    private void DefinirImagemEmpate(GameObject painel, bool empate)
    {
        if (painel == null)
            return;

        LocalizedImage[] imagens = painel.GetComponentsInChildren<LocalizedImage>(true);
        for (int i = 0; i < imagens.Length; i++)
            imagens[i].DefinirEmpate(empate);
    }

    private void DefinirRetratoFim(LutadorController2D personagem)
    {
        if (imagemRostoVencedor == null)
            return;

        Sprite corpo = personagem != null && personagem.dadosPersonagem != null
            ? personagem.dadosPersonagem.spriteCorpo
            : null;

        if (corpo == null && personagem != null && personagem.dadosPersonagem != null &&
            personagem.dadosPersonagem.framesIdle != null && personagem.dadosPersonagem.framesIdle.Length > 0)
            corpo = personagem.dadosPersonagem.framesIdle[0];

        if (corpo == null)
        {
            imagemRostoVencedor.gameObject.SetActive(false);
            return;
        }

        imagemRostoVencedor.sprite = corpo;
        imagemRostoVencedor.preserveAspect = false;
        imagemRostoVencedor.rectTransform.anchoredPosition = new Vector2(0f, 10f);
        imagemRostoVencedor.rectTransform.sizeDelta = ObterTamanhoCorpoNoPainel(personagem, corpo);
        imagemRostoVencedor.gameObject.SetActive(true);
        imagemRostoVencedor.transform.SetAsLastSibling();

        Selectable selecionavelNaImagem = imagemRostoVencedor.GetComponent<Selectable>();
        if (selecionavelNaImagem != null)
            selecionavelNaImagem.interactable = false;

        imagemRostoVencedor.raycastTarget = false;
    }

    private void AtualizarNomePersonagemEmpate(LutadorController2D personagem)
    {
        if (textoNomeVencedor == null)
            return;

        if (personagem == null)
        {
            textoNomeVencedor.text = "";
            textoNomeVencedor.gameObject.SetActive(false);
            return;
        }

        string lado = personagem == player1 ? "P1" : "P2";
        string nome = personagem.dadosPersonagem != null
            ? personagem.dadosPersonagem.nomePersonagem
            : lado;
        textoNomeVencedor.text = nome + " - " + lado;
        textoNomeVencedor.gameObject.SetActive(true);
    }

    private IEnumerator AlternarRetratosEmpate()
    {
        bool mostrarP1 = true;
        float intervalo = Mathf.Max(0.5f, intervaloRetratosEmpate);

        while (lutaTerminou && painelFim != null && painelFim.activeInHierarchy)
        {
            yield return new WaitForSecondsRealtime(intervalo);
            mostrarP1 = !mostrarP1;

            LutadorController2D personagem = mostrarP1 ? player1 : player2;
            DefinirRetratoFim(personagem);
            AtualizarNomePersonagemEmpate(personagem);
            if (textoBalaoVencedor != null)
                textoBalaoVencedor.text = ObterFraseEmpate(personagem);
        }

        rotinaRetratosEmpate = null;
    }

    private string ObterFraseVencedor(LutadorController2D vencedor)
    {
        bool portugues = LanguageManager.Instance == null ||
            LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Portuguese;
        string nome = vencedor != null && vencedor.dadosPersonagem != null
            ? vencedor.dadosPersonagem.nomePersonagem
            : string.Empty;

        if (nome.IndexOf("Jamanta", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return portugues
                ? "Errar também faz parte de aprender."
                : "Making mistakes is part of learning.";

        if (nome.IndexOf("Diego", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return portugues
                ? "Compartilhar conhecimento faz todos crescerem."
                : "Sharing knowledge helps everyone grow.";

        return portugues
            ? "Aprender abre novas possibilidades."
            : "Learning opens new possibilities.";
    }

    private string ObterFraseEmpate(LutadorController2D personagem)
    {
        bool portugues = LanguageManager.Instance == null ||
            LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Portuguese;
        string nome = personagem != null && personagem.dadosPersonagem != null
            ? personagem.dadosPersonagem.nomePersonagem
            : string.Empty;

        if (nome.IndexOf("Jamanta", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return portugues
                ? "Ninguém venceu. Ainda dá para aprender com essa luta."
                : "No one won. There is still more to learn from this fight.";

        if (nome.IndexOf("Diego", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return portugues
                ? "Foi igual para os dois. Vamos decidir na próxima!"
                : "We were evenly matched. Let's settle it next time!";

        if (nome.IndexOf("Renata", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return portugues
                ? "Empatamos! Na próxima, eu vou com tudo."
                : "It's a draw! Next time, I'll give it my all.";

        return portugues
            ? "Ficou tudo igual. Quem sabe na próxima?"
            : "It's a tie. Maybe next time?";
    }

    IEnumerator SelecionarBotaoNoProximoFrame(GameObject botao)
    {
        yield return null;

        if (EventSystem.current == null || botao == null)
            yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(botao);
    }

    void MostrarRoundWin(string nomeVencedor, int round, LutadorController2D vencedorObj)
    {
        if (painelRoundWin == null) return;

        var lm = LanguageManager.Instance;

        // Texto 1 — "VENCEDOR" / "WINNER" (ou "EMPATE")
        string titulo;
        if (string.IsNullOrEmpty(nomeVencedor))
            titulo = lm != null ? lm.GetText("ROUND_DRAW") : "Empate";
        else
            titulo = lm != null ? lm.GetText("ROUND_VENCEDOR") : "VENCEDOR";

        if (textoRoundWinTitulo != null)
        {
            textoRoundWinTitulo.text = titulo;
        }

        // Texto 2 — "Nome - P1" ou "Nome - P2"
        if (textoRoundWinNome != null)
        {
            textoRoundWinNome.fontSize = 64f;
            if (string.IsNullOrEmpty(nomeVencedor) || vencedorObj == null)
            {
                textoRoundWinNome.text = "";
                textoRoundWinNome.gameObject.SetActive(false);
            }
            else
            {
                string lado = vencedorObj == player1 ? "P1" : "P2";
                textoRoundWinNome.text = nomeVencedor + " - " + lado;
                textoRoundWinNome.gameObject.SetActive(true);
            }
        }

        // Campo legado
        if (textoRoundWin != null)
            textoRoundWin.text = titulo;

        painelRoundWin.SetActive(true);
        DefinirImagemEmpate(painelRoundWin, string.IsNullOrEmpty(nomeVencedor));
    }

    IEnumerator ProximoRoundDepois(float tempoEspera)
    {
        yield return new WaitForSeconds(tempoEspera);

        // Esconde painel de round win antes de iniciar próximo round
        if (painelRoundWin != null) painelRoundWin.SetActive(false);

        roundAtual++;
        IniciarRound();
    }

    /// <summary>
    /// Consultado pelo PauseManager antes de abrir o menu de pause. Retorna false
    /// durante a tela de Round Win, a transição pro próximo round e a tela de
    /// vencedor final — evitando o menu de pause abrir por cima delas.
    /// </summary>
    public bool PodePausar()
    {
        return !lutaTerminou && roundEmAndamento;
    }

    public void ReiniciarLuta()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarParaSelecao()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelecaoPlayer");
    }
}
