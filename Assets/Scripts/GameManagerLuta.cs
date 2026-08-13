using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    [Header("UI Nomes")]
    public TextMeshProUGUI nomeP1;
    public TextMeshProUGUI nomeP2;

    [Header("UI Controles")]
    public TextMeshProUGUI controlesP1;
    public TextMeshProUGUI controlesP2;
    public float tempoExibirControles = 4f;

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

            controlesP1.text = $"P1 {esq}/{dir} P:{pulo} D:{def} A:{atk} E:{esp} U:{ult}";
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
            string ult = FormatarTecla(PlayerPrefs.GetString("P2_Ultimate", "Semicolon"));

            controlesP2.text = $"P2 {esq}/{dir} P:{pulo} D:{def} A:{atk} E:{esp} U:{ult}";
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

        yield return new WaitForSeconds(tempoExibirControles);

        if (controlesP1 != null) controlesP1.gameObject.SetActive(false);
        if (controlesP2 != null) controlesP2.gameObject.SetActive(false);
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
            }
            else
            {
                imagemRostoVencedor.gameObject.SetActive(false);
            }
        }
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