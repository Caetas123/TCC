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
        // A faixa precisa usar a mesma direção da Slider. Isso é importante
        // para a barra do P2, que é RightToLeft: usar sempre [atual, custo]
        // coloca o vermelho em uma posição que pode ficar sobre o azul.
        bool preenchimentoInvertido = barra.direction == Slider.Direction.RightToLeft;
        float inicio = preenchimentoInvertido
            ? 1f - custoNormalizado
            : energiaAtualNormalizada;
        float fim = preenchimentoInvertido
            ? 1f - energiaAtualNormalizada
            : custoNormalizado;

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
        const float duracao = 1f;
        float tempo = 0f;

        while (tempo < duracao && indicador != null)
        {
            float intensidade = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(tempo * 5f, 1f));
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
            string pulo = FormatarTecla(PlayerPrefs.GetString("P1_Pular", "W"));
            string def = FormatarTecla(PlayerPrefs.GetString("P1_Defender", "S"));
            string atk = FormatarTecla(PlayerPrefs.GetString("P1_Ataque", "F"));
            string esp = FormatarTecla(PlayerPrefs.GetString("P1_Especial", "G"));
            string ult = FormatarTecla(PlayerPrefs.GetString("P1_Ultimate", "H"));

            controlesP1.text = $"P1 {esq}/{dir} PULO:{pulo} DEF:{def} ATAQUE:{atk} ESPECIAL:{esp} ULTIMATE:{ult}";
            controlesP1.enableAutoSizing = true;
            if (controlesP1.fontSizeMin <= 0f) controlesP1.fontSizeMin = 8f;
        }

        if (controlesP2 != null)
        {
            string esq = FormatarTecla(PlayerPrefs.GetString("P2_Esquerda", "LeftArrow"));
            string dir = FormatarTecla(PlayerPrefs.GetString("P2_Direita", "RightArrow"));
            string pulo = FormatarTecla(PlayerPrefs.GetString("P2_Pular", "UpArrow"));
            string def = FormatarTecla(PlayerPrefs.GetString("P2_Defender", "DownArrow"));
            string atk = FormatarTecla(PlayerPrefs.GetString("P2_Ataque", "K"));
            string esp = FormatarTecla(PlayerPrefs.GetString("P2_Especial", "L"));
            string ult = FormatarTecla(PlayerPrefs.GetString("P2_Ultimate", "M"));

            controlesP2.text = $"P2 {esq}/{dir} PULO:{pulo} DEF:{def} ATAQUE:{atk} ESPECIAL:{esp} ULTIMATE:{ult}";
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
            case "LeftArrow": return "←";
            case "RightArrow": return "→";
            case "UpArrow": return "↑";
            case "DownArrow": return "↓";
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

        var lm = LanguageManager.Instance;

        // Texto 1 — palavra "VITÓRIA" / "VICTORY" (ou "EMPATE" / "DRAW")
        bool ehEmpate = vencedor == null;
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
                textoNomeVencedor.text = "";
                textoNomeVencedor.gameObject.SetActive(false);
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

        // Rosto do vencedor
        if (imagemRostoVencedor != null)
        {
            Sprite rosto = vencedor != null && vencedor.dadosPersonagem != null
                ? vencedor.dadosPersonagem.spriteRosto
                : null;

            if (rosto != null)
            {
                imagemRostoVencedor.sprite = rosto;
                imagemRostoVencedor.gameObject.SetActive(true);

                // Blindagem: essa imagem é só visual, nunca deveria interceptar
                // clique/navegação. Se em algum momento um componente Selectable
                // (Button, Toggle etc.) tiver sido adicionado nela sem querer no
                // Inspector, desliga aqui — é exatamente esse tipo de coisa que
                // rouba a seleção do teclado sem o mouse nunca ter tocado em nada.
                Selectable selecionavelNaImagem = imagemRostoVencedor.GetComponent<Selectable>();
                if (selecionavelNaImagem != null)
                    selecionavelNaImagem.interactable = false;

                imagemRostoVencedor.raycastTarget = false;
            }
            else
            {
                imagemRostoVencedor.gameObject.SetActive(false);
            }
        }

        // Seleciona de verdade o primeiro botão real do painel pro teclado/controle
        // funcionar assim que a tela aparece — sem isso, nada está selecionado no
        // EventSystem até o mouse clicar em algo, e as setas não têm de onde partir.
        Button primeiroBotao = botaoReiniciar != null ? botaoReiniciar : botaoVoltar;
        if (primeiroBotao != null)
            StartCoroutine(SelecionarBotaoNoProximoFrame(primeiroBotao.gameObject));
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
