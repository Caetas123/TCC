using System.Collections;
using UnityEngine;

public class LutadorController2D : MonoBehaviour
{
    // ── Estados de animação — ordem = prioridade (maior = mais prioritário) ──
    public enum EstadoAnim
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Defend = 3,
        Attack = 4,
        Special = 5,
        Ultimate = 6,
        Hit = 7,
        Death = 8
    }

    [Header("Identificação")]
    [Range(1, 2)] public int numeroJogador = 1;
    public bool jogador1 => numeroJogador == 1;

    [Header("Dados do personagem")]
    public DadosPersonagem dadosPersonagem;

    [Header("Estado atual")]
    public int vidaAtual;
    public float energiaAtual;

    [Header("Referências")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public LutadorController2D oponente;

    [Header("Empurrão entre lutadores")]
    [Tooltip("Distância mínima que os dois lutadores podem chegar um do outro — abaixo disso, o empurrão entra em ação")]
    public float distanciaMinimaEntreLutadores = 2.0f;
    [Tooltip("Intensidade do empurrão ao ficarem mais próximos que a distância mínima")]
    public float forcaEmpurraoLutadores = 12f;
    public GameManagerLuta gameManager;

    [Header("Teclas atuais")]
    [SerializeField] private KeyCode teclaEsquerda;
    [SerializeField] private KeyCode teclaDireita;
    [SerializeField] private KeyCode teclaPular;
    [SerializeField] private KeyCode teclaAtaque;
    [SerializeField] private KeyCode teclaEspecial;
    [SerializeField] private KeyCode teclaUltimate;
    [SerializeField] private KeyCode teclaDefender;

    [Header("Controle de videogame")]
    [Tooltip("Deixe vazio para evitar conflito com o teclado.")]
    [SerializeField] private string eixoHorizontalControle = "";
    [SerializeField] private KeyCode botaoPularControle = KeyCode.None;
    [SerializeField] private KeyCode botaoAtaqueControle = KeyCode.None;
    [SerializeField] private KeyCode botaoEspecialControle = KeyCode.None;
    [SerializeField] private KeyCode botaoUltimateControle = KeyCode.None;
    [SerializeField] private KeyCode botaoDefenderControle = KeyCode.None;
    [SerializeField] private float deadZoneAnalogico = 0.35f;

    [Header("Controle por IA")]
    public bool controladoPorIA = false;

    [Header("Áudio")]
    [Tooltip("AudioSource deste lutador (ataque, hit, pulo, etc.) — se não atribuído, busca/cria automaticamente")]
    public AudioSource audioSource;
    [Tooltip("AudioSource dedicado ao som de passos (loop) — separado do audioSource principal para não ter conflito de pitch/volume. Se não atribuído, cria automaticamente")]
    public AudioSource audioSourcePassos;

    // ── Estado de jogo ─────────────────────────────────────────────────────
    private bool estaNoChao;
    private bool defendendo;
    private float movimento;
    private bool morreu;

    // ── Inputs IA ─────────────────────────────────────────────────────────
    private float movimentoIA;
    private bool defesaIA;
    private bool puloIASolicitado;
    private bool ataqueIASolicitado;
    private bool especialIASolicitado;
    private bool ultimateIASolicitado;

    // ── Sistema de animação ────────────────────────────────────────────────
    private EstadoAnim estadoAtual = EstadoAnim.Idle;
    private EstadoAnim estadoAnterior = EstadoAnim.Idle;
    private int frameAtual = 0;
    private float cronometroFrame = 0f;
    private bool animacaoUmaVezAtiva = false;

    // ── Controle de dano do ataque por frame ──────────────────────────────
    private enum FaseAtaque { Windup, Hit, FollowThrough }

    // ── Controle de sons ──────────────────────────────────────────────────
    private bool estavaMovendoAntes = false;   // detecta início/fim do movimento
    private bool estavaNoChaoBefore = false;    // detecta pouso
    private FaseAtaque faseAtaqueAtual = FaseAtaque.Windup;
    private bool danoJaAplicado = false;

    // ── Habilidade especial única (ex: salto+rachadura do Jamanta) ─────────
    // Enquanto true: ignora todo input (humano e IA) e a Update() de animação
    // não mexe em nada — a própria coroutine da habilidade controla tudo
    // (estado, frame, física) do início ao fim, sem o resto do script interferir.
    private bool emHabilidadeUnica = false;

    // Pedaços de rachadura ainda na tela — destruídos todos juntos e rápido
    // quando a habilidade termina, em vez de cada um sumir sozinho depois de
    // um tempo longo e escalonado (ver DestruirRachadurasAtivas).
    private readonly System.Collections.Generic.List<GameObject> pedacosRachaduraAtivos = new System.Collections.Generic.List<GameObject>();

    // Detecta a transição pra Time.timeScale == 0 (pausa/fim de round), pra
    // parar os sons só uma vez nesse instante, não em todo frame pausado.
    private bool estavaPausadoAntes = false;

    // ── Especial alternável — Espada em Chamas (ex: Diego) ──────────────────
    // true enquanto o especial estiver ligado: troca os sprites de
    // Idle/Move/Jump/Defend/Attack pela variante em chamas (se configurada) e faz
    // o ataque básico aplicar queimação, além de drenar energia por segundo.
    private bool espadaEmChamasAtiva = false;

    // Queimação ativa NESTE lutador (aplicada pelo ataque em chamas do oponente).
    // Guardada aqui pra poder reiniciar a contagem se levar outro golpe em chamas
    // antes da anterior acabar, em vez de empilhar múltiplas queimações somando dano.
    private Coroutine rotinaQueimacao;

    // ── Ultimate único — Raio com Recuo (ex: Jamanta) ────────────────────────
    // Somado à velocidade normal em FixedUpdate; RotinaRecuoUltimate decai isso
    // suavemente até 0 sozinha — não é um único impulso instantâneo (rb.AddForce
    // seria sobrescrito no próximo FixedUpdate de qualquer forma, já que
    // velocidadeX é recalculada do zero a cada frame).
    private float velocidadeRecuoX = 0f;

    // true entre o instante do disparo e o raio sumir da tela — AtualizarAnimacao()
    // usa isso pra congelar no frame de lançamento em vez de deixar a animação de
    // Ultimate terminar sozinha (só 7 frames, bem mais rápido que o raio fica
    // visível) e voltar o personagem a andar enquanto o próprio golpe ainda está
    // acontecendo na tela.
    private bool travandoUltimateAteRaioSumir = false;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManagerLuta>();

        CarregarTeclas();
        ConfigurarControlePadraoSeNecessario();
        AplicarDadosPersonagem();

        // Busca AudioSource automaticamente se não configurado
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // som 2D

        // AudioSource dedicado aos passos — separado do principal para o pitch/volume
        // dos passos não ser alterado por outros efeitos tocados enquanto anda (ataque, etc.)
        if (audioSourcePassos == null)
            audioSourcePassos = gameObject.AddComponent<AudioSource>();

        audioSourcePassos.playOnAwake = false;
        audioSourcePassos.spatialBlend = 0f;
        audioSourcePassos.loop = true;

        // Inscreve para receber aviso quando o jogador mexer nos sliders do AudioManager,
        // assim o som de passos (que fica em loop) é atualizado em tempo real
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnVolumeChanged += AtualizarVolumeSomEmLoop;

        estaNoChao = true;
        estavaNoChaoBefore = true;
        estadoAtual = EstadoAnim.Idle;
        estadoAnterior = EstadoAnim.Idle;
        animacaoUmaVezAtiva = false;
    }

    void OnDestroy()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnVolumeChanged -= AtualizarVolumeSomEmLoop;
    }

    void Update()
    {
        // BUG 24 — bloqueia inputs quando pausado
        if (Time.timeScale == 0f)
        {
            // O jogo pausou (fim de rodada/partida) — para todo som ainda tocando
            // (passos em loop, ataque, etc.) só UMA vez, na transição pra pausado.
            // Sem isso o Update inteiro parava de rodar aqui embaixo, mas o som
            // que já estava tocando continuava pra sempre.
            if (!estavaPausadoAntes)
            {
                PararTodosOsSons();
                estavaPausadoAntes = true;
            }
            return;
        }
        estavaPausadoAntes = false;

        if (morreu) return;

        if (controladoPorIA)
            LerInputsIA();
        else
            LerInputsHumano();

        RecarregarEnergiaParado();
        DrenarEspadaEmChamasSeAtiva();
        VirarParaOponente();
        AtualizarAnimacao();
        AtualizarSons();
    }

    // Para o som de passos (loop) e qualquer efeito ainda tocando neste lutador —
    // usado quando o round/partida acaba (pausa) e ao morrer.
    void PararTodosOsSons()
    {
        if (audioSourcePassos != null) audioSourcePassos.Stop();
        if (audioSource != null) audioSource.Stop();
    }

    // ── Movimento por animação ──────────────────────────────────────────────
    // Centraliza a decisão de "quanto o personagem pode andar" em cada animação,
    // toda configurável por personagem via DadosPersonagem (podeSeMoverDuranteX +
    // multiplicadorMovimentoX). "Pode Se Mover" desligado = 0 (trava completa),
    // ligado = usa o multiplicador correspondente.
    //
    // Defesa usa o campo "defendendo" direto (não o estadoAtual) porque é a fonte
    // de verdade mais confiável de "está defendendo agora" — não depende de qual
    // frame de animação está tocando no instante exato.
    float ObterMultiplicadorMovimentoAtual()
    {
        if (defendendo)
            return dadosPersonagem.podeSeMoverDuranteDefesa ? dadosPersonagem.multiplicadorMovimentoDefesa : 0f;

        if (animacaoUmaVezAtiva)
        {
            switch (estadoAtual)
            {
                case EstadoAnim.Attack:
                    return dadosPersonagem.podeSeMoverDuranteAtaque ? dadosPersonagem.multiplicadorMovimentoAtaque : 0f;
                case EstadoAnim.Special:
                    return dadosPersonagem.podeSeMoverDuranteEspecial ? dadosPersonagem.multiplicadorMovimentoEspecial : 0f;
                case EstadoAnim.Ultimate:
                    return dadosPersonagem.podeSeMoverDuranteUltimate ? dadosPersonagem.multiplicadorMovimentoUltimate : 0f;
                case EstadoAnim.Hit:
                    return dadosPersonagem.podeSeMoverDuranteHit ? dadosPersonagem.multiplicadorMovimentoHit : 0f;
            }
        }

        return 1f; // Idle/Move/Jump — velocidade normal
    }

    void FixedUpdate()
    {
        if (morreu) return;
        if (rb == null || dadosPersonagem == null) return;

        float velocidadeX = movimento * dadosPersonagem.velocidade;
        velocidadeX *= ObterMultiplicadorMovimentoAtual();

        // Recuo do ultimate único (ex: Jamanta) — soma-se por cima do movimento
        // normal, decaindo sozinho até 0 (ver RotinaRecuoUltimate). Fica ANTES do
        // empurrão entre lutadores de propósito: se o recuo jogar alguém pra dentro
        // do oponente, o empurrão abaixo ainda impede a sobreposição.
        velocidadeX += velocidadeRecuoX;

        // ── Empurrão entre lutadores ────────────────────────────────────────
        // Antes isso só ZERAVA a velocidade de quem tentava se aproximar demais —
        // um bloqueio duro, não um empurrão: o personagem simplesmente travava no
        // lugar e o oponente não reagia nada, mesmo em PVP. Agora os dois são
        // afastados de verdade quando ficam colados — cada instância deste script
        // roda essa mesma conta independentemente, então o resultado é simétrico:
        // os dois deslizam pra trás, como se estivessem se empurrando.
        if (oponente != null)
        {
            float dx = transform.position.x - oponente.transform.position.x;
            float distanciaX = Mathf.Abs(dx);

            if (distanciaX < distanciaMinimaEntreLutadores && distanciaX > 0.0001f)
            {
                float direcaoAfastamento = Mathf.Sign(dx);
                float penetracao = distanciaMinimaEntreLutadores - distanciaX;

                // Não deixa o movimento do jogador empurrar AINDA MAIS pra dentro do
                // oponente (continua impossível atravessar), mas se afastar continua livre
                bool indoParaDentro = velocidadeX != 0f && Mathf.Sign(velocidadeX) == -direcaoAfastamento;
                if (indoParaDentro) velocidadeX = 0f;

                // O empurrão em si: proporcional à sobreposição, soma-se ao movimento —
                // é isso que efetivamente separa os dois, inclusive quando o overlap vem
                // de knockback/spawn, não só de andar um em direção ao outro
                velocidadeX += direcaoAfastamento * penetracao * forcaEmpurraoLutadores;
            }
        }

        rb.linearVelocity = new Vector2(velocidadeX, rb.linearVelocity.y);
    }
    // ── Setup ──────────────────────────────────────────────────────────────
    public void AplicarDadosPersonagem()
    {
        if (dadosPersonagem == null) return;

        vidaAtual = dadosPersonagem.vidaMax;
        // Energia começa ZERADA no início da luta — combinado com o time pra ninguém
        // conseguir usar Especial/Ultimate de cara, sem precisar lutar pra carregar energia.
        energiaAtual = 0f;
        morreu = false;
        espadaEmChamasAtiva = false;
        velocidadeRecuoX = 0f;
        travandoUltimateAteRaioSumir = false;
        if (rotinaQueimacao != null) { StopCoroutine(rotinaQueimacao); rotinaQueimacao = null; }

        // Aplica o primeiro frame do Idle imediatamente
        AplicarPrimeiroFrame(dadosPersonagem.framesIdle, dadosPersonagem.spriteCorpo);
    }

    void AplicarPrimeiroFrame(Sprite[] frames, Sprite fallback)
    {
        if (spriteRenderer == null) return;
        if (frames != null && frames.Length > 0 && frames[0] != null)
            spriteRenderer.sprite = frames[0];
        else if (fallback != null)
            spriteRenderer.sprite = fallback;
    }

    public void ResetarParaNovoRound()
    {
        if (dadosPersonagem == null) return;

        vidaAtual = dadosPersonagem.vidaMax;
        // Mesma regra do início da luta: cada round novo também começa com energia
        // ZERADA, pra ninguém entrar no round já podendo soltar Especial/Ultimate.
        energiaAtual = 0f;
        movimento = 0f;
        defendendo = false;
        morreu = false;
        estaNoChao = false;
        espadaEmChamasAtiva = false;
        velocidadeRecuoX = 0f;
        travandoUltimateAteRaioSumir = false;
        if (rotinaQueimacao != null) { StopCoroutine(rotinaQueimacao); rotinaQueimacao = null; }

        movimentoIA = 0f;
        defesaIA = false;
        puloIASolicitado = false;
        ataqueIASolicitado = false;
        especialIASolicitado = false;
        ultimateIASolicitado = false;

        // Reseta animação para Idle
        estadoAtual = EstadoAnim.Idle;
        estadoAnterior = EstadoAnim.Idle;
        frameAtual = 0;
        cronometroFrame = 0f;
        animacaoUmaVezAtiva = false;
        faseAtaqueAtual = FaseAtaque.Windup;
        danoJaAplicado = false;

        AplicarPrimeiroFrame(dadosPersonagem.framesIdle, dadosPersonagem.spriteCorpo);

        if (rb != null) rb.linearVelocity = Vector2.zero;
        enabled = true;
    }

    public void TravarLutador()
    {
        movimento = 0f;
        defendendo = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    public string ObterNomeExibicao()
    {
        string prefixo = numeroJogador == 1 ? "Player 1 - " : "Player 2 - ";
        string nome = dadosPersonagem != null ? dadosPersonagem.nomePersonagem : "Unknown";
        return prefixo + nome;
    }

    // ── Teclas ────────────────────────────────────────────────────────────
    // Chamado publicamente pelo ControleManager assim que uma tecla é remapeada
    // e salva no PlayerPrefs — sem isso, remapear controle NO MEIO DE UMA LUTA
    // (o ControleManager mora dentro de cena1/cena2, acessível pelo pause) salvava
    // certinho, mas o lutador já em cena continuava com a tecla antiga: CarregarTeclas
    // só rodava uma vez, no Start(), então nada recarregava o novo valor em memória.
    public void RecarregarTeclas() => CarregarTeclas();

    void CarregarTeclas()
    {
        if (numeroJogador == 1)
        {
            teclaEsquerda = StringParaKeyCodeSeguro("P1_Esquerda", KeyCode.A);
            teclaDireita = StringParaKeyCodeSeguro("P1_Direita", KeyCode.D);
            teclaPular = StringParaKeyCodeSeguro("P1_Pular", KeyCode.W);
            teclaAtaque = StringParaKeyCodeSeguro("P1_Ataque", KeyCode.F);
            teclaEspecial = StringParaKeyCodeSeguro("P1_Especial", KeyCode.G);
            teclaUltimate = StringParaKeyCodeSeguro("P1_Ultimate", KeyCode.H);
            teclaDefender = StringParaKeyCodeSeguro("P1_Defender", KeyCode.S);
        }
        else
        {
            teclaEsquerda = StringParaKeyCodeSeguro("P2_Esquerda", KeyCode.LeftArrow);
            teclaDireita = StringParaKeyCodeSeguro("P2_Direita", KeyCode.RightArrow);
            teclaPular = StringParaKeyCodeSeguro("P2_Pular", KeyCode.UpArrow);
            teclaAtaque = StringParaKeyCodeSeguro("P2_Ataque", KeyCode.K);
            teclaEspecial = StringParaKeyCodeSeguro("P2_Especial", KeyCode.L);
            teclaUltimate = StringParaKeyCodeSeguro("P2_Ultimate", KeyCode.Semicolon);
            teclaDefender = StringParaKeyCodeSeguro("P2_Defender", KeyCode.DownArrow);
        }
    }

    void ConfigurarControlePadraoSeNecessario()
    {
        if (botaoPularControle == KeyCode.None) botaoPularControle = numeroJogador == 1 ? KeyCode.Joystick1Button0 : KeyCode.Joystick2Button0;
        if (botaoDefenderControle == KeyCode.None) botaoDefenderControle = numeroJogador == 1 ? KeyCode.Joystick1Button1 : KeyCode.Joystick2Button1;
        if (botaoAtaqueControle == KeyCode.None) botaoAtaqueControle = numeroJogador == 1 ? KeyCode.Joystick1Button2 : KeyCode.Joystick2Button2;
        if (botaoEspecialControle == KeyCode.None) botaoEspecialControle = numeroJogador == 1 ? KeyCode.Joystick1Button3 : KeyCode.Joystick2Button3;
        if (botaoUltimateControle == KeyCode.None) botaoUltimateControle = numeroJogador == 1 ? KeyCode.Joystick1Button4 : KeyCode.Joystick2Button4;
    }

    KeyCode StringParaKeyCodeSeguro(string chave, KeyCode padrao)
    {
        string valor = PlayerPrefs.GetString(chave, padrao.ToString());
        if (string.IsNullOrWhiteSpace(valor)) return padrao;
        try { return (KeyCode)System.Enum.Parse(typeof(KeyCode), valor); }
        catch { Debug.LogWarning("Tecla inválida em " + chave + ": " + valor); return padrao; }
    }

    // ── Inputs ────────────────────────────────────────────────────────────
    void LerInputsHumano()
    {
        if (emHabilidadeUnica)
        {
            movimento = 0f;
            defendendo = false;
            return;
        }

        // Bloqueia a LEITURA de movimento (trava e ignora novas teclas) durante
        // Special/Ultimate SÓ SE o personagem estiver configurado pra não se mover
        // nessas animações (dadosPersonagem.podeSeMoverDuranteEspecial/Ultimate).
        // O Attack (ataque básico) NUNCA entra aqui: ele sempre lê teclas novas
        // normalmente durante a animação (senão, se o ataque começasse parado, uma
        // tecla de andar pressionada DEPOIS nunca seria lida até o ataque acabar —
        // só funcionava se já estivesse andando ANTES do ataque começar). A redução
        // de velocidade durante o Attack é aplicada depois, no FixedUpdate.
        bool travaEspecial  = dadosPersonagem == null || !dadosPersonagem.podeSeMoverDuranteEspecial;
        bool travaUltimate  = dadosPersonagem == null || !dadosPersonagem.podeSeMoverDuranteUltimate;
        bool bloqueado = animacaoUmaVezAtiva &&
                         ((estadoAtual == EstadoAnim.Special  && travaEspecial) ||
                          (estadoAtual == EstadoAnim.Ultimate && travaUltimate));

        if (!bloqueado)
        {
            movimento = 0f;
            float movTeclado = 0f;
            float movControle = 0f;

            if (Input.GetKey(teclaEsquerda)) movTeclado = -1f;
            if (Input.GetKey(teclaDireita)) movTeclado = 1f;

            float valorAxis = LerAxisComSeguranca(eixoHorizontalControle);
            if (!string.IsNullOrWhiteSpace(eixoHorizontalControle) && Mathf.Abs(valorAxis) >= deadZoneAnalogico)
                movControle = Mathf.Sign(valorAxis);

            movimento = Mathf.Abs(movControle) > 0.01f ? movControle : movTeclado;
        }

        bool pressionouPulo = Input.GetKeyDown(teclaPular) ||
                              (botaoPularControle != KeyCode.None && Input.GetKeyDown(botaoPularControle));
        if (pressionouPulo && estaNoChao) Pular();

        bool defTeclado = Input.GetKey(teclaDefender);
        bool defControle = botaoDefenderControle != KeyCode.None && Input.GetKey(botaoDefenderControle);
        defendendo = defTeclado || defControle;

        bool pressionouAtaque = Input.GetKeyDown(teclaAtaque) ||
                                (botaoAtaqueControle != KeyCode.None && Input.GetKeyDown(botaoAtaqueControle));
        if (pressionouAtaque) AtaqueNormal();

        bool pressionouEspecial = Input.GetKeyDown(teclaEspecial) ||
                                  (botaoEspecialControle != KeyCode.None && Input.GetKeyDown(botaoEspecialControle));
        if (pressionouEspecial) AtaqueEspecial();

        bool pressionouUltimate = Input.GetKeyDown(teclaUltimate) ||
                                  (botaoUltimateControle != KeyCode.None && Input.GetKeyDown(botaoUltimateControle));
        if (pressionouUltimate) AtaqueUltimate();
    }

    float LerAxisComSeguranca(string nomeAxis)
    {
        if (string.IsNullOrWhiteSpace(nomeAxis)) return 0f;
        try { return Input.GetAxisRaw(nomeAxis); }
        catch { return 0f; }
    }

    void LerInputsIA()
    {
        if (emHabilidadeUnica)
        {
            movimento = 0f;
            defendendo = false;
            IA_LimparComandos();
            return;
        }

        // Mesma trava do jogador humano — sem isso, a IA nunca era impedida de andar
        // durante Special/Ultimate genéricos (só travava no Especial Única tipo Jamanta).
        bool travaEspecialIA = dadosPersonagem == null || !dadosPersonagem.podeSeMoverDuranteEspecial;
        bool travaUltimateIA = dadosPersonagem == null || !dadosPersonagem.podeSeMoverDuranteUltimate;
        bool bloqueadoIA = animacaoUmaVezAtiva &&
                           ((estadoAtual == EstadoAnim.Special  && travaEspecialIA) ||
                            (estadoAtual == EstadoAnim.Ultimate && travaUltimateIA));

        movimento = bloqueadoIA ? 0f : Mathf.Clamp(movimentoIA, -1f, 1f);
        defendendo = defesaIA;

        if (puloIASolicitado && estaNoChao) Pular();
        if (ataqueIASolicitado) AtaqueNormal();
        if (especialIASolicitado) AtaqueEspecial();
        if (ultimateIASolicitado) AtaqueUltimate();

        puloIASolicitado = false;
        ataqueIASolicitado = false;
        especialIASolicitado = false;
        ultimateIASolicitado = false;
    }

    // ── Ações de combate ──────────────────────────────────────────────────
    // Impede cancelar Ataque/Especial/Ultimate um no outro no meio da animação —
    // sem isso, dava pra encadear Ataque -> cancela pra Especial -> assim que
    // possível cancela de volta pra Ataque, batendo repetidamente sem nunca esperar
    // a recuperação normal de nenhum dos dois (dano sem delay, infinito). Levar um
    // Hit continua interrompendo normalmente — só não dá pra cancelar POR VONTADE
    // PRÓPRIA num golpe pra outro.
    //
    // Também bloqueia iniciar golpe durante o próprio Hit (EstaLevandoHit()): antes
    // só checava Attack/Special/Ultimate, então apertar ataque enquanto tomava dano
    // gastava energia à toa — a animação nem saía do lugar (Hit tem prioridade maior
    // e barra a troca de estado em IniciarAnimacaoUmaVez), mas o custo em energia já
    // tinha sido debitado antes dessa checagem.
    bool EmAcaoOfensiva()
    {
        return animacaoUmaVezAtiva &&
               (estadoAtual == EstadoAnim.Attack ||
                estadoAtual == EstadoAnim.Special ||
                estadoAtual == EstadoAnim.Ultimate ||
                estadoAtual == EstadoAnim.Hit);
    }

    void RecarregarEnergiaParado()
    {
        if (dadosPersonagem == null) return;
        bool paradoNoChao = Mathf.Abs(movimento) < 0.01f && estaNoChao && !defendendo;
        if (paradoNoChao)
        {
            energiaAtual += dadosPersonagem.velocidadeRecargaEnergia * Time.deltaTime;
            energiaAtual = Mathf.Clamp(energiaAtual, 0f, dadosPersonagem.energiaMax);
        }
    }

    void Pular()
    {
        if (rb == null || dadosPersonagem == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * dadosPersonagem.forcaPulo, ForceMode2D.Impulse);
        TocarSom(
            dadosPersonagem.somPulo,
            dadosPersonagem.volumePulo
        );
        IniciarAnimacaoUmaVez(EstadoAnim.Jump);
    }

    void AtaqueNormal()
    {
        if (oponente == null || dadosPersonagem == null || morreu) return;
        if (EmAcaoOfensiva()) return;
        if (!TemEnergiaSuficiente(dadosPersonagem.custoAtaque)) return;

        GastarEnergia(dadosPersonagem.custoAtaque);
        faseAtaqueAtual = FaseAtaque.Windup;
        danoJaAplicado = false;
        TocarSom(
            dadosPersonagem.somAtaqueInicio,
            dadosPersonagem.volumeAtaqueInicio
        );
        IniciarAnimacaoUmaVez(EstadoAnim.Attack);
    }

    void AplicarDanoAtaquePorFrame()
    {
        if (oponente == null || dadosPersonagem == null) return;
        if (danoJaAplicado) return;

        switch (faseAtaqueAtual)
        {
            case FaseAtaque.Windup:
                // Sem hitbox — apenas animação de preparação
                break;

            case FaseAtaque.Hit:
                float distHit = Vector2.Distance(transform.position, oponente.transform.position);
                if (distHit <= dadosPersonagem.alcanceAtaque)
                {
                    oponente.ReceberDano(dadosPersonagem.danoAtaque);
                    AplicarQueimacaoSeEspadaEmChamas();
                    TocarSom(
                        dadosPersonagem.somAtaqueImpacto,
                        dadosPersonagem.volumeAtaqueImpacto
                    );
                }
                danoJaAplicado = true;
                break;

            case FaseAtaque.FollowThrough:
                float distFollow = Vector2.Distance(transform.position, oponente.transform.position);
                if (distFollow <= dadosPersonagem.alcanceAtaque)
                {
                    oponente.ReceberDano(Mathf.RoundToInt(dadosPersonagem.danoAtaque * 0.5f));
                    AplicarQueimacaoSeEspadaEmChamas();
                    TocarSom(
                        dadosPersonagem.somAtaqueImpacto,
                        dadosPersonagem.volumeAtaqueImpacto
                    );
                }
                danoJaAplicado = true;
                break;
        }
    }

    // Espada em chamas ativa (ver AlternarEspadaEmChamas) faz o ataque básico
    // também queimar quem levou o golpe. A queimação nunca é bloqueada — só a
    // duração encolhe se o alvo estava defendendo no instante do impacto.
    void AplicarQueimacaoSeEspadaEmChamas()
    {
        if (!espadaEmChamasAtiva || oponente == null || oponente.EstaMorto()) return;

        bool oponenteDefendendo = oponente.EstaDefendendo();
        float duracao = oponenteDefendendo
            ? dadosPersonagem.duracaoQueimacaoDefendendo
            : dadosPersonagem.duracaoQueimacao;

        oponente.AplicarQueimadura(dadosPersonagem.danoQueimacaoPorSegundo, duracao);
    }

    void AtaqueEspecial()
    {
        if (oponente == null || dadosPersonagem == null || morreu) return;

        // Especial alternável (ex: espada em chamas do Diego): não é "toca uma vez e
        // acaba" como os outros — é liga/desliga. Sai ANTES do gate EmAcaoOfensiva()
        // de propósito: ligar/desligar não deve depender de estar no meio de outra
        // animação de golpe, é só uma troca de estado + sprite, não uma ação ofensiva
        // nova em si.
        if (dadosPersonagem.especialAlternavelComQueimadura)
        {
            AlternarEspadaEmChamas();
            return;
        }

        if (EmAcaoOfensiva()) return;
        if (!PodeUsarEspecial()) return;

        // Personagem com spriteRachadura configurado usa o golpe único (salto alto +
        // rachaduras no chão) em vez do especial padrão de dano à distância.
        if (dadosPersonagem.spriteRachadura != null)
        {
            if (emHabilidadeUnica) return; // já está no meio do golpe, ignora reaperto
            GastarEnergiaEspecial();
            StartCoroutine(EspecialSaltoRachadura());
            return;
        }

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);
        if (distancia <= dadosPersonagem.alcanceEspecial)
        {
            GastarEnergiaEspecial();
            oponente.ReceberDano(dadosPersonagem.danoEspecial);
        }

        TocarSom(
            dadosPersonagem.somEspecial,
            dadosPersonagem.volumeEspecial
        );
        IniciarAnimacaoUmaVez(EstadoAnim.Special);
    }

    // ── Especial alternável — Espada em Chamas (ex: Diego) ──────────────────
    void AlternarEspadaEmChamas()
    {
        if (espadaEmChamasAtiva)
        {
            DesativarEspadaEmChamas();
            return;
        }

        float custoAtivacao = dadosPersonagem.custoAtivarEspecialAlternavel / 100f * dadosPersonagem.energiaMax;
        if (energiaAtual < custoAtivacao) return; // sem energia nem pra ativar

        energiaAtual -= custoAtivacao;
        energiaAtual = Mathf.Clamp(energiaAtual, 0f, dadosPersonagem.energiaMax);
        espadaEmChamasAtiva = true;

        TocarSom(
            dadosPersonagem.somEspecial,
            dadosPersonagem.volumeEspecial
        );
    }

    void DesativarEspadaEmChamas()
    {
        espadaEmChamasAtiva = false;
    }

    // Drena energia por segundo enquanto o especial estiver ativo, chamado todo
    // Update() (independe de estado/animação — roda em paralelo a qualquer coisa
    // que o personagem esteja fazendo). Desativa sozinho ao zerar a energia.
    void DrenarEspadaEmChamasSeAtiva()
    {
        if (!espadaEmChamasAtiva || dadosPersonagem == null) return;

        float dreno = dadosPersonagem.drenoPorSegundoEspecialAlternavel / 100f * dadosPersonagem.energiaMax * Time.deltaTime;
        energiaAtual -= dreno;

        if (energiaAtual <= 0f)
        {
            energiaAtual = 0f;
            espadaEmChamasAtiva = false;
        }
    }

    // Chamada pelo ATACANTE no oponente que acabou de ser acertado por um golpe com
    // a espada em chamas. A queimação em si nunca é reduzida pela defesa — só a
    // DURAÇÃO é mais curta se o alvo estava defendendo no instante do impacto (ver
    // AplicarDanoAtaquePorFrame). Reinicia a contagem em vez de empilhar se já
    // estiver queimando — assim golpes seguidos não somam dano por segundo.
    public void AplicarQueimadura(int danoPorSegundo, float duracao)
    {
        if (morreu) return;
        if (rotinaQueimacao != null) StopCoroutine(rotinaQueimacao);
        rotinaQueimacao = StartCoroutine(RotinaQueimacao(danoPorSegundo, duracao));
    }

    IEnumerator RotinaQueimacao(int danoPorSegundo, float duracao)
    {
        int ticks = Mathf.Max(1, Mathf.RoundToInt(duracao)); // 1 tick por segundo
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (morreu) break;
            AplicarDanoQueimacaoTick(danoPorSegundo);
        }
        rotinaQueimacao = null;
    }

    // Dano de queimação é aplicado direto na vida, sem passar pela redução de
    // defesa de ReceberDano (a queimação "não pode ser defendida") e sem disparar
    // a animação de Hit a cada tick — senão o personagem ficaria travado em
    // stagger constante enquanto queimasse, em vez de continuar lutando normalmente.
    void AplicarDanoQueimacaoTick(int dano)
    {
        if (morreu) return;

        vidaAtual -= dano;
        if (vidaAtual < 0) vidaAtual = 0;

        if (vidaAtual <= 0)
            Morrer();
    }

    // ── Golpe único: salto normal + rachaduras no chão (ex: Jamanta) ────────
    // 1) Windup (2 frames do Special) parado no chão
    // 2) Salto — MESMA força/altura do pulo normal (não mexe nisso)
    // 3) No ápice, acelera a queda ("cai muito rápido")
    // 4) Ao tocar o chão: Special de volta (frame 2 -> 1), landing
    // 5) 0.5s depois do fim da animação: 1ª rachadura aparece E o movimento libera
    // 6) Rachaduras seguintes a cada 0.5s (até completar quantidadeRachaduras),
    //    cada uma crescendo a partir do pé dele, com partículas e hitbox de dano
    IEnumerator EspecialSaltoRachadura()
    {
        emHabilidadeUnica = true;

        TocarSom(dadosPersonagem.somEspecial, dadosPersonagem.volumeEspecial);

        Sprite[] framesSpecial = dadosPersonagem.framesSpecial;
        float fpsSpecial = dadosPersonagem.fpsSpecial > 0f ? dadosPersonagem.fpsSpecial : 8f;
        float tempoPorFrameSpecial = 1f / fpsSpecial;

        // ── 1) Windup: frame 1 -> frame 2 ──
        estadoAtual = EstadoAnim.Special;
        frameAtual = 0;
        AplicarFrameAtual();
        yield return new WaitForSeconds(tempoPorFrameSpecial);

        if (framesSpecial != null && framesSpecial.Length > 1)
        {
            frameAtual = 1;
            AplicarFrameAtual();
        }
        yield return new WaitForSeconds(tempoPorFrameSpecial);

        // ── 2) Salto — mais alto que o pulo normal, usando o multiplicador ──
        estaNoChao = false;
        if (rb != null)
        {
            float forcaSaltoEspecial = dadosPersonagem.forcaPulo * Mathf.Max(1f, dadosPersonagem.multiplicadorAlturaSaltoRachadura);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * forcaSaltoEspecial, ForceMode2D.Impulse);
        }

        estadoAtual = EstadoAnim.Jump;
        AtualizarFrameJump();

        // Espera 2 passos físicos antes de checar o ápice — dá tempo do personagem
        // descolar de verdade do chão antes de confiar em qualquer flag de colisão
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Espera alcançar o ápice do salto (velocidade Y deixa de ser positiva) —
        // só pela física, sem depender de estaNoChao aqui (é exatamente isso que
        // cortava o pulo curto antes)
        yield return new WaitUntil(() => rb == null || rb.linearVelocity.y <= 0f);

        // ── 3) Queda rápida ──
        const float velocidadeQuedaRapida = -22f;
        while (rb != null && !estaNoChao)
        {
            Vector2 v = rb.linearVelocity;
            if (v.y > velocidadeQuedaRapida)
                v.y = velocidadeQuedaRapida;
            rb.linearVelocity = v;
            AtualizarFrameJump();
            yield return null;
        }

        Vector3 posicaoImpacto = transform.position;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // ── 4) Rachadura inicial + animação de pouso, em SIMULTÂNEO ──
        // A 1ª rachadura dispara no exato instante em que ele toca o chão, sem
        // esperar a animação de pouso terminar. O delay (intervaloRachaduras)
        // continua existindo só ENTRE uma rachadura e a próxima (dentro de
        // SequenciaRachaduras), não mais antes da primeira.
        // Direção pra onde a rachadura cresce = pra onde ele está virado no pouso
        // (transform.localScale.x > 0 = olhando pra direita, < 0 = olhando pra esquerda)
        float direcaoCrescimento = transform.localScale.x < 0f ? -1f : 1f;
        StartCoroutine(SequenciaRachaduras(posicaoImpacto, direcaoCrescimento));

        // ── 5) Landing: Special frame 2 -> 1 ──
        if (framesSpecial != null && framesSpecial.Length > 1)
        {
            estadoAtual = EstadoAnim.Special;
            frameAtual = 1;
            AplicarFrameAtual();
            yield return new WaitForSeconds(tempoPorFrameSpecial);

            frameAtual = 0;
            AplicarFrameAtual();
            yield return new WaitForSeconds(tempoPorFrameSpecial);
        }

        // Volta pro idle antes de liberar (evita 1 frame com pose errada)
        estadoAtual = EstadoAnim.Idle;
        frameAtual = 0;
        cronometroFrame = 0f;
        AplicarFrameAtual();

        // ── 6) 0.5s de espera — só pra liberar o movimento do jogador; as
        // rachaduras seguintes já estão rodando em paralelo desde o passo 4 ──
        yield return new WaitForSeconds(0.5f);

        emHabilidadeUnica = false; // libera input — a partir daqui volta ao normal
    }

    IEnumerator SequenciaRachaduras(Vector3 posicaoBase, float direcao)
    {
        int quantidade = 3; // a imagem é cortada em 3 pedaços fixos
        float intervalo = Mathf.Max(0.05f, dadosPersonagem.intervaloRachaduras);

        for (int i = 0; i < quantidade; i++)
        {
            StartCoroutine(RachaduraComHitbox(posicaoBase, i, direcao, dadosPersonagem.raioRachadura));

            if (i < quantidade - 1)
                yield return new WaitForSeconds(intervalo);
        }

        // Espera só o tempo da janela de dano do último pedaço (não mais 4s fixos
        // por pedaço) e some com TODOS de uma vez, rápido — evita ficar arrastando
        // rachadura na tela depois do golpe já ter acabado.
        yield return new WaitForSeconds(Mathf.Max(0.05f, dadosPersonagem.duracaoHitboxRachadura));
        DestruirRachadurasAtivas();
    }

    void DestruirRachadurasAtivas()
    {
        for (int i = 0; i < pedacosRachaduraAtivos.Count; i++)
        {
            if (pedacosRachaduraAtivos[i] != null)
                Destroy(pedacosRachaduraAtivos[i]);
        }
        pedacosRachaduraAtivos.Clear();
    }

    // Um pedaço da rachadura: sprite cortado da imagem original + partículas + hitbox
    // de dano ativa por dadosPersonagem.duracaoHitboxRachadura (acerta se o oponente
    // entrar na área do pedaço durante essa janela, não só no instante em que aparece)
    IEnumerator RachaduraComHitbox(Vector3 posicaoBase, int indicePedaco, float direcao, float raio)
    {
        SpriteRenderer pedacoCriado = CriarPedacoRachadura(posicaoBase, indicePedaco, direcao);
        if (pedacoCriado == null) yield break;

        CriarParticulasRachadura(pedacoCriado.bounds.center);

        Vector3 centroPedaco = pedacoCriado.bounds.center;
        float duracao = Mathf.Max(0.05f, dadosPersonagem.duracaoHitboxRachadura);
        bool acertou = false;
        float t = 0f;

        while (t < duracao)
        {
            if (!acertou && oponente != null && !oponente.EstaMorto())
            {
                float dist = Vector2.Distance(centroPedaco, oponente.transform.position);
                if (dist <= raio)
                {
                    int dano = Mathf.RoundToInt(dadosPersonagem.danoEspecial * dadosPersonagem.multiplicadorDanoRachadura);
                    oponente.ReceberDano(dano);
                    acertou = true;
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Corta a imagem original da rachadura em 3 pedaços de verdade (recorte de
    // pixels, não é efeito de revelar/máscara) e posiciona o pedaço "indicePedaco"
    // encostado no anterior, formando a imagem completa aos poucos:
    //   - Pedaço 0 = a ponta DIREITA da imagem original — fica com essa ponta
    //     ancorada exatamente no pé do Jamanta (posicaoBase)
    //   - Pedaço 1 = o meio da imagem original — encosta no pedaço 0
    //   - Pedaço 2 = a ponta ESQUERDA da imagem original — encosta no pedaço 1
    // "direcao" (+1 ou -1) espelha pra que lado a sequência se estende no mundo,
    // conforme o Jamanta está virado — mas a ordem de qual pedaço da imagem
    // aparece primeiro (a ponta direita ORIGINAL) nunca muda.
    SpriteRenderer CriarPedacoRachadura(Vector3 posicaoBase, int indicePedaco, float direcao)
    {
        Sprite spriteOriginal = dadosPersonagem.spriteRachadura;
        if (spriteOriginal == null) return null;

        Rect rectOriginal = spriteOriginal.rect; // região usada na textura, em pixels
        float larguraFatiaPx = rectOriginal.width / 3f;

        // indicePedaco 0,1,2 (ordem de exibição) -> fatia 2,1,0 da imagem original
        // (fatia 2 = a mais à direita na textura = "ponta direita")
        int fatiaOrigem = 2 - indicePedaco;
        Rect rectFatia = new Rect(
            rectOriginal.x + fatiaOrigem * larguraFatiaPx,
            rectOriginal.y,
            larguraFatiaPx,
            rectOriginal.height
        );

        float pixelsPerUnit = spriteOriginal.pixelsPerUnit;
        // Mantém o mesmo alinhamento vertical (pivô Y) da imagem original — geralmente
        // 0 (embaixo, no chão), mas assim funciona com qualquer configuração.
        float pivotYNormalizado = rectOriginal.height > 0f ? spriteOriginal.pivot.y / rectOriginal.height : 0f;

        // A borda que fica "presa" no pedaço anterior (ou no pé, no caso do pedaço 0)
        // é sempre a de trás, na direção contrária ao crescimento: se cresce pra
        // direita (direcao > 0), a borda de encaixe é a ESQUERDA do pedaço (pivô x=0);
        // se cresce pra esquerda, é a DIREITA (pivô x=1).
        float pivotXNormalizado = direcao > 0f ? 0f : 1f;

        Sprite spriteFatia = Sprite.Create(
            spriteOriginal.texture,
            rectFatia,
            new Vector2(pivotXNormalizado, pivotYNormalizado),
            pixelsPerUnit
        );

        float larguraFatiaUnidades = larguraFatiaPx / pixelsPerUnit;
        Vector3 posicao = posicaoBase + Vector3.right * (direcao * indicePedaco * larguraFatiaUnidades);

        GameObject obj = new GameObject("Rachadura_Pedaco" + indicePedaco);
        obj.transform.position = posicao;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spriteFatia;
        sr.sortingOrder = -1; // no chão, atrás dos personagens

        Destroy(obj, 4f); // rede de segurança — se por algum motivo DestruirRachadurasAtivas não rodar
        pedacosRachaduraAtivos.Add(obj);

        return sr;
    }

    // ── Sprites gerados na hora (sem depender de nenhum asset) ──────────────
    private static Sprite spriteParticulaBranca;

    Sprite ObterSpriteParticulaBranca()
    {
        if (spriteParticulaBranca == null)
        {
            // 16x16 em vez de 1x1 — um sprite de 1 pixel só, mesmo escalado no
            // transform, acaba renderizando microscópico (era por isso que as
            // partículas "sumiam"). Com pixelsPerUnit=64 (igual aos personagens),
            // 16px = 0.25 unidades de base, aí sim dá pra ver.
            Texture2D tex = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            spriteParticulaBranca = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 64f);
        }
        return spriteParticulaBranca;
    }

    void CriarParticulasRachadura(Vector3 posicao)
    {
        const int quantidade = 8;
        Sprite spriteParticula = ObterSpriteParticulaBranca();
        string layerPersonagem = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        int ordemPersonagem = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

        for (int i = 0; i < quantidade; i++)
        {
            GameObject p = new GameObject("ParticulaRachadura");
            p.transform.position = posicao + new Vector3(Random.Range(-0.3f, 0.3f), 0.1f, 0f);

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = spriteParticula;
            sr.color = Color.white;
            // Mesma sorting layer do personagem, mas sempre por cima dele — garante
            // que aparecem visíveis independente de como as layers do projeto estão
            // configuradas.
            sr.sortingLayerName = layerPersonagem;
            sr.sortingOrder = ordemPersonagem + 10;
            p.transform.localScale = Vector3.one * Random.Range(0.4f, 0.7f);

            Vector2 velocidadeInicial = new Vector2(Random.Range(-2f, 2f), Random.Range(4.5f, 6.5f));
            StartCoroutine(AnimarParticula(p, velocidadeInicial));
        }
    }

    IEnumerator AnimarParticula(GameObject obj, Vector2 velocidade)
    {
        const float tempoVida = 0.6f;
        const float gravidade = 9f;
        float t = 0f;

        while (t < tempoVida && obj != null)
        {
            velocidade.y -= gravidade * Time.deltaTime;
            obj.transform.position += (Vector3)(velocidade * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    void AtaqueUltimate()
    {
        if (oponente == null || dadosPersonagem == null || morreu) return;
        if (EmAcaoOfensiva()) return;
        if (!PodeUsarUltimate()) return;

        // Personagem com spriteRaioUltimate configurado usa o ultimate único (raio
        // com recuo) em vez do dano instantâneo padrão. Diferente do ultimate
        // genérico abaixo, esse NÃO checa alcanceUltimate — é um projétil que
        // atravessa a tela até o oponente, não um golpe corpo a corpo, então
        // sempre dispara independente da distância entre os dois.
        if (dadosPersonagem.spriteRaioUltimate != null)
        {
            GastarEnergiaUltimate();
            StartCoroutine(UltimateRaioComRecuo(oponente));

            TocarSom(dadosPersonagem.somUltimate, dadosPersonagem.volumeUltimate);
            IniciarAnimacaoUmaVez(EstadoAnim.Ultimate);
            return;
        }

        float distancia = Vector2.Distance(transform.position, oponente.transform.position);
        if (distancia <= dadosPersonagem.alcanceUltimate)
        {
            GastarEnergiaUltimate();
            oponente.ReceberDano(dadosPersonagem.danoUltimate);
        }

        TocarSom(
            dadosPersonagem.somUltimate,
            dadosPersonagem.volumeUltimate
        );
        IniciarAnimacaoUmaVez(EstadoAnim.Ultimate);
    }

    // ── Ultimate único: raio com recuo (ex: Jamanta) ─────────────────────────
    // 1) A animação de Ultimate já está tocando por fora (IniciarAnimacaoUmaVez,
    //    frames de "se posicionar" configurados em framesUltimate).
    // 2) Espera até o frame configurado como lançamento — sincroniza o recuo e o
    //    raio com o momento exato em que ele solta o golpe, não com o instante em
    //    que o botão foi apertado.
    // 3) Nesse instante: CONGELA na pose de lançamento (travandoUltimateAteRaioSumir
    //    — sem isso a animação de Ultimate, com só 7 frames, terminava e voltava
    //    o personagem a andar bem antes do raio sumir da tela), aplica o recuo
    //    (empurrão suave pra trás, decai sozinho) e mostra o raio (com lampejo
    //    inicial opcional).
    // 4) Espera um delay bem curto (delayRaioUltimate) — só pra dar a sensação de
    //    trajeto, nem instantâneo nem lento feito a rachadura.
    // 5) Aplica o dano, então segura a pose pelo tempo que falta até o raio sumir
    //    de vez (duracaoVisualRaioUltimate) e só então libera o personagem — igual
    //    ao que a animação faria sozinha ao chegar no último frame.
    IEnumerator UltimateRaioComRecuo(LutadorController2D alvo)
    {
        float fps = dadosPersonagem.fpsUltimate > 0f ? dadosPersonagem.fpsUltimate : 10f;
        float tempoAteLancamento = Mathf.Max(0f, dadosPersonagem.frameLancamentoRaioUltimate) / fps;
        yield return new WaitForSeconds(tempoAteLancamento);

        frameAtual = Mathf.Max(0, dadosPersonagem.frameLancamentoRaioUltimate);
        AplicarFrameAtual();
        travandoUltimateAteRaioSumir = true;

        // Recuo: empurra pra trás, na direção oposta a quem ele está olhando
        float direcaoRecuo = transform.localScale.x > 0f ? -1f : 1f;
        StartCoroutine(RotinaRecuoUltimate(direcaoRecuo, dadosPersonagem.forcaRecuoUltimate, dadosPersonagem.duracaoRecuoUltimate));

        // Lampejo primeiro, sozinho na tela por duracaoLampejoRaioUltimate, e SÓ
        // DEPOIS o raio cheio nasce — antes os dois eram criados no mesmo instante
        // (sem yield entre eles), então visualmente sempre apareciam sobrepostos
        // desde o primeiro frame renderizado: o retângulo grande do raio "engolia"
        // o lampejo pequeno, que nunca dava tempo de ser percebido sozinho.
        if (dadosPersonagem.spriteRaioUltimateInicio != null)
        {
            GameObject lampejo = CriarLampejoRaioUltimate();
            yield return new WaitForSeconds(Mathf.Max(0.03f, dadosPersonagem.duracaoLampejoRaioUltimate));
            if (lampejo != null) Destroy(lampejo);
        }

        GameObject raio = CriarRaioUltimate(alvo);
        if (raio != null) Destroy(raio, dadosPersonagem.duracaoVisualRaioUltimate);

        yield return new WaitForSeconds(Mathf.Max(0f, dadosPersonagem.delayRaioUltimate));

        if (alvo != null && !alvo.EstaMorto())
            alvo.ReceberDano(dadosPersonagem.danoUltimate);

        // Já esperamos delayRaioUltimate acima — só falta o restante até bater
        // com duracaoVisualRaioUltimate (quando o raio de fato some da tela).
        float tempoRestante = dadosPersonagem.duracaoVisualRaioUltimate - dadosPersonagem.delayRaioUltimate;
        if (tempoRestante > 0f)
            yield return new WaitForSeconds(tempoRestante);

        travandoUltimateAteRaioSumir = false;

        // Só libera se ninguém mais mexeu no estado nesse meio tempo (ex: o
        // personagem levou um Hit, que tem prioridade maior e já assumiu sozinho
        // — nesse caso não é a gente que deve decidir pra onde a animação volta).
        if (estadoAtual == EstadoAnim.Ultimate)
        {
            animacaoUmaVezAtiva = false;
            estadoAtual = estadoAnterior;
            frameAtual = 0;
            cronometroFrame = 0f;
            AplicarFrameAtual();
        }
    }

    // Decaimento suave (ease-out): forte no início, esvaindo até 0 — sensação de
    // onda de pressão empurrando e se dissipando, não um solavanco constante nem
    // um teleporte instantâneo.
    IEnumerator RotinaRecuoUltimate(float direcao, float forca, float duracao)
    {
        if (duracao <= 0f) yield break;

        float t = 0f;
        while (t < duracao)
        {
            float progresso = t / duracao;
            velocidadeRecuoX = direcao * forca * (1f - progresso * progresso);
            t += Time.deltaTime;
            yield return null;
        }
        velocidadeRecuoX = 0f;
    }

    // A cena usa personagens com localScale bem maior que 1 (o sprite "cru" é
    // pequeno, e o GameObject escala ele pra cima) — um deslocamento fixo tipo
    // "0.6 unidades" é praticamente zero perto do tamanho real do lutador na tela,
    // por isso o raio aparecia baixo demais (quase no chão) e fino demais. Essas
    // duas funções dão a altura e a escala REAIS (já em unidades de mundo,
    // considerando o localScale), pra qualquer efeito gerado na hora acompanhar o
    // tamanho de verdade do personagem em vez de um número fixo que só funcionaria
    // numa cena sem escala nenhuma.
    float AlturaRealDoLutador() => spriteRenderer != null ? spriteRenderer.bounds.size.y : 1f;
    float EscalaVisualDoLutador() => Mathf.Abs(transform.localScale.y);

    GameObject CriarLampejoRaioUltimate()
    {
        if (dadosPersonagem.spriteRaioUltimateInicio == null) return null;

        float escala = EscalaVisualDoLutador();
        float direcao = transform.localScale.x > 0f ? 1f : -1f;
        float altura = AlturaRealDoLutador() * dadosPersonagem.fracaoAlturaRaioUltimate;
        Vector3 posicao = transform.position + new Vector3(direcao * altura * 0.3f, altura, 0f);

        GameObject obj = new GameObject("LampejoRaioUltimate");
        obj.transform.position = posicao;
        obj.transform.localScale = Vector3.one * escala;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = dadosPersonagem.spriteRaioUltimateInicio;
        sr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 5;

        return obj;
    }

    // Estica o sprite do raio (pivô central — import padrão) no eixo X pra cobrir
    // exatamente a distância até o oponente, sem precisar conhecer o pivô/recorte
    // exato do sprite. A altura de origem (mão/peito) é uma FRAÇÃO da altura real
    // do personagem (fracaoAlturaRaioUltimate, 0 a 1), não um valor fixo — assim
    // funciona igual em qualquer escala de cena. A espessura (eixo Y) escala junto
    // com o personagem, senão o raio fica fininho perto de um personagem bem maior
    // que 1 unidade.
    GameObject CriarRaioUltimate(LutadorController2D alvo)
    {
        if (dadosPersonagem.spriteRaioUltimate == null || alvo == null) return null;

        float alturaOrigem = AlturaRealDoLutador() * dadosPersonagem.fracaoAlturaRaioUltimate;
        float alturaAlvo = alvo.AlturaRealDoLutador() * dadosPersonagem.fracaoAlturaRaioUltimate;
        Vector3 origemJamanta = transform.position + new Vector3(0f, alturaOrigem, 0f);
        Vector3 origemAlvo = alvo.transform.position + new Vector3(0f, alturaAlvo, 0f);
        Vector3 meio = (origemJamanta + origemAlvo) * 0.5f;
        float distancia = Mathf.Abs(origemAlvo.x - origemJamanta.x);

        GameObject obj = new GameObject("RaioUltimate");
        obj.transform.position = meio;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = dadosPersonagem.spriteRaioUltimate;
        sr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 5;

        float larguraOriginal = sr.sprite.bounds.size.x;
        float escalaEspessura = EscalaVisualDoLutador();
        if (larguraOriginal > 0.001f)
            obj.transform.localScale = new Vector3(distancia / larguraOriginal, escalaEspessura, 1f);

        return obj;
    }

    public void ReceberDano(int dano)
    {
        if (morreu) return;

        int danoFinal = dano;

        if (defendendo && dadosPersonagem != null)
        {
            float custoDefesa = Mathf.Max(0f, dadosPersonagem.custoDefesa);
            float bloqueioMaximo = Mathf.Clamp01(dadosPersonagem.porcentagemBloqueioDefesa);

            if (custoDefesa <= 0f)
            {
                danoFinal = Mathf.RoundToInt(dano * (1f - bloqueioMaximo));
            }
            else if (energiaAtual >= custoDefesa)
            {
                energiaAtual -= custoDefesa;
                danoFinal = Mathf.RoundToInt(dano * (1f - bloqueioMaximo));
            }
            else if (energiaAtual > 0f)
            {
                float proporcao = energiaAtual / custoDefesa;
                float bloqueioReal = bloqueioMaximo * proporcao;
                danoFinal = Mathf.RoundToInt(dano * (1f - bloqueioReal));
                energiaAtual = 0f;
            }
        }

        energiaAtual = Mathf.Clamp(energiaAtual, 0f,
            dadosPersonagem != null ? dadosPersonagem.energiaMax : 100f);

        vidaAtual -= danoFinal;
        if (vidaAtual < 0) vidaAtual = 0;

        if (vidaAtual <= 0)
            Morrer();
        else
        {
            TocarSom(
                dadosPersonagem.somHit,
                dadosPersonagem.volumeHit
            );
            IniciarAnimacaoUmaVez(EstadoAnim.Hit);
        }
    }

    void Morrer()
    {
        morreu = true;
        if (audioSourcePassos != null) audioSourcePassos.Stop();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        TocarSom(
            dadosPersonagem.somMorte,
            dadosPersonagem.volumeMorte
        );
        IniciarAnimacaoUmaVez(EstadoAnim.Death);
        if (gameManager != null) gameManager.FinalizarLuta(this);
    }

    // ── Energia ───────────────────────────────────────────────────────────
    bool TemEnergiaSuficiente(float custo)
    {
        if (dadosPersonagem == null) return false;
        return custo <= 0f || energiaAtual >= custo;
    }

    void GastarEnergia(float custo)
    {
        energiaAtual -= custo;
        energiaAtual = Mathf.Clamp(energiaAtual, 0f, dadosPersonagem.energiaMax);
    }

    bool PodeUsarEspecial()
    {
        if (dadosPersonagem == null) return false;
        switch (dadosPersonagem.tipoGastoEspecial)
        {
            case TipoGastoEspecial.BarraCheia:
            case TipoGastoEspecial.ZeraTudo: return energiaAtual >= dadosPersonagem.energiaMax;
            case TipoGastoEspecial.GastoFixo:
            case TipoGastoEspecial.GastoGradual: return energiaAtual >= dadosPersonagem.custoEspecial;
            default: return false;
        }
    }

    void GastarEnergiaEspecial()
    {
        if (dadosPersonagem == null) return;
        switch (dadosPersonagem.tipoGastoEspecial)
        {
            case TipoGastoEspecial.BarraCheia:
            case TipoGastoEspecial.ZeraTudo: energiaAtual = 0f; break;
            case TipoGastoEspecial.GastoFixo:
            case TipoGastoEspecial.GastoGradual: energiaAtual -= dadosPersonagem.custoEspecial; break;
        }
        energiaAtual = Mathf.Clamp(energiaAtual, 0f, dadosPersonagem.energiaMax);
    }

    bool PodeUsarUltimate()
    {
        if (dadosPersonagem == null) return false;
        switch (dadosPersonagem.tipoUsoUltimate)
        {
            case TipoUsoUltimate.BarraCheia: return energiaAtual >= dadosPersonagem.energiaMax;
            case TipoUsoUltimate.GastoFixo: return energiaAtual >= dadosPersonagem.custoUltimate;
            default: return false;
        }
    }

    void GastarEnergiaUltimate()
    {
        if (dadosPersonagem == null) return;
        switch (dadosPersonagem.tipoUsoUltimate)
        {
            case TipoUsoUltimate.BarraCheia: energiaAtual = 0f; break;
            case TipoUsoUltimate.GastoFixo: energiaAtual -= dadosPersonagem.custoUltimate; break;
        }
        energiaAtual = Mathf.Clamp(energiaAtual, 0f, dadosPersonagem.energiaMax);
    }

    // ── Utilidades ────────────────────────────────────────────────────────
    void VirarParaOponente()
    {
        if (oponente == null) return;
        Vector3 escala = transform.localScale;
        escala.x = transform.position.x < oponente.transform.position.x
            ? Mathf.Abs(escala.x)
            : -Mathf.Abs(escala.x);
        transform.localScale = escala;
    }

    public float ObterEnergiaNormalizada()
    {
        if (dadosPersonagem == null || dadosPersonagem.energiaMax <= 0f) return 0f;
        return energiaAtual / dadosPersonagem.energiaMax;
    }

    public bool EstaMorto() => morreu;
    public bool EstaDefendendo() => defendendo;
    public bool EstaNoChao() => estaNoChao;
    public bool EstaAtacando() => animacaoUmaVezAtiva &&
        (estadoAtual == EstadoAnim.Attack ||
         estadoAtual == EstadoAnim.Special ||
         estadoAtual == EstadoAnim.Ultimate);
    public bool EstaLevandoHit() => animacaoUmaVezAtiva && estadoAtual == EstadoAnim.Hit;
    public EstadoAnim ObterEstadoAnim() => estadoAtual;

    public float DistanciaDoOponente()
    {
        if (oponente == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, oponente.transform.position);
    }

    // ── IA ────────────────────────────────────────────────────────────────
    public void IA_DefinirMovimento(float valor) { if (!controladoPorIA) return; movimentoIA = Mathf.Clamp(valor, -1f, 1f); }
    public void IA_DefinirDefesa(bool valor) { if (!controladoPorIA) return; defesaIA = valor; }
    public void IA_SolicitarPulo() { if (!controladoPorIA) return; puloIASolicitado = true; }
    public void IA_SolicitarAtaque() { if (!controladoPorIA) return; ataqueIASolicitado = true; }
    public void IA_SolicitarEspecial() { if (!controladoPorIA) return; especialIASolicitado = true; }
    public void IA_SolicitarUltimate() { if (!controladoPorIA) return; ultimateIASolicitado = true; }
    public void IA_LimparComandos()
    {
        if (!controladoPorIA) return;
        movimentoIA = 0f; defesaIA = false;
        puloIASolicitado = false; ataqueIASolicitado = false;
        especialIASolicitado = false; ultimateIASolicitado = false;
    }

    // ── Sistema de animação ────────────────────────────────────────────────
    // Inicia uma animação que toca uma vez (attack, hit, death, etc.)
    // Se a nova animação tem prioridade maior que a atual, interrompe
    void IniciarAnimacaoUmaVez(EstadoAnim novoEstado)
    {
        // Death nunca é interrompido
        if (estadoAtual == EstadoAnim.Death)
            return;

        // Só substitui se prioridade maior ou igual
        if (animacaoUmaVezAtiva && novoEstado <= estadoAtual)
            return;

        estadoAnterior = animacaoUmaVezAtiva ? estadoAnterior : estadoAtual;

        estadoAtual = novoEstado;
        cronometroFrame = 0f;
        animacaoUmaVezAtiva = true;
        frameAtual = 0;

        if (novoEstado == EstadoAnim.Attack)
        {
            faseAtaqueAtual = FaseAtaque.Windup;
            danoJaAplicado = false;
        }

        // Aplica primeiro frame imediatamente
        AplicarFrameAtual();

        // ✅ CORREÇÃO:
        // Evita travamento caso personagem não tenha sprites suficientes
        Sprite[] frames = ObterFramesDoEstado(novoEstado);

        if (frames == null || frames.Length <= 1)
        {
            animacaoUmaVezAtiva = false;

            // volta para estado normal
            if (morreu)
                estadoAtual = EstadoAnim.Death;
            else if (defendendo)
                estadoAtual = EstadoAnim.Defend;
            else if (!estaNoChao)
                estadoAtual = EstadoAnim.Jump;
            else if (Mathf.Abs(movimento) > 0.01f)
                estadoAtual = EstadoAnim.Move;
            else
                estadoAtual = EstadoAnim.Idle;

            frameAtual = 0;
            cronometroFrame = 0f;

            AplicarFrameAtual();
        }
    }

    void AtualizarAnimacao()
    {
        if (dadosPersonagem == null || spriteRenderer == null) return;
        if (emHabilidadeUnica) return; // a coroutine da habilidade única controla tudo sozinha

        // ── Decide estado contínuo (só quando não há animação de uma vez ativa) ──
        if (!animacaoUmaVezAtiva)
        {
            EstadoAnim novoEstado;

            if (morreu)
                novoEstado = EstadoAnim.Death;
            else if (defendendo)
                novoEstado = EstadoAnim.Defend;
            else if (!estaNoChao)
                novoEstado = EstadoAnim.Jump;
            else if (Mathf.Abs(movimento) > 0.01f)
                novoEstado = EstadoAnim.Move;
            else
                novoEstado = EstadoAnim.Idle;

            if (novoEstado != estadoAtual)
            {
                estadoAtual = novoEstado;
                frameAtual = 0;
                cronometroFrame = 0f;
                AplicarFrameAtual();
            }
        }

        // ── Pulo: 2 poses fixas (subindo / caindo) controladas pela física, ──
        // ── não por um timer de fps como as outras animações ────────────────
        if (estadoAtual == EstadoAnim.Jump)
        {
            AtualizarFrameJump();
            return;
        }

        // ── Ultimate único: segura a pose de disparo enquanto o raio estiver na
        // tela ────────────────────────────────────────────────────────────────
        // Sem isso, a animação de Ultimate (só 7 frames, ~0.7s) terminava e
        // voltava pro Idle/Move MUITO antes do raio sumir (que pode durar vários
        // segundos, configurável) — o personagem já andando de novo enquanto o
        // próprio golpe dele ainda estava na tela. UltimateRaioComRecuo() controla
        // quando essa trava liga/desliga.
        if (travandoUltimateAteRaioSumir && estadoAtual == EstadoAnim.Ultimate)
            return;

        // ── Avança o frame ────────────────────────────────────────────────
        Sprite[] frames = ObterFramesDoEstado(estadoAtual);
        float fps = ObterFpsDoEstado(estadoAtual);

        if (frames == null || frames.Length == 0 || fps <= 0f)
        {
            // Personagem ainda sem os sprites desse estado prontos (produção em
            // andamento). Enquanto isso não existia, faltar frames numa animação
            // "toca uma vez" (Hit/Special/Ultimate) travava o lutador PRA SEMPRE
            // nesse estado — o único lugar que volta animacaoUmaVezAtiva pra false
            // fica adiante, dentro do bloco de cronômetro, que a gente nunca
            // alcançava sem frame nenhum pra avançar. Sem sprite pra tocar, trata
            // como se a animação já tivesse terminado na hora — volta pro estado
            // anterior igual quando ela chega no último frame normalmente. Death é
            // exceção: não tem "estado anterior" pra voltar, e a partida já é
            // encerrada por FinalizarLuta() independente da animação terminar.
            if (animacaoUmaVezAtiva && estadoAtual != EstadoAnim.Death)
            {
                animacaoUmaVezAtiva = false;
                estadoAtual = estadoAnterior;
                frameAtual = 0;
                cronometroFrame = 0f;
                AplicarFrameAtual();
            }
            return;
        }

        cronometroFrame += Time.deltaTime;
        float intervalo = 1f / fps;

        if (cronometroFrame >= intervalo)
        {
            cronometroFrame -= intervalo;

            if (animacaoUmaVezAtiva && estadoAtual == EstadoAnim.Attack)
            {
                bool ultimoFrameAtk = frameAtual >= frames.Length - 1;

                if (ultimoFrameAtk)
                {
                    // Animação terminou — volta ao estado anterior
                    animacaoUmaVezAtiva = false;
                    estadoAtual = estadoAnterior;
                    frameAtual = 0;
                    cronometroFrame = 0f;
                    AplicarFrameAtual();
                    return;
                }

                frameAtual++;

                // Atualiza fase com base no frame atual
                int inicioHit     = dadosPersonagem != null ? dadosPersonagem.frameInicioHitbox : 1;
                int followThrough = dadosPersonagem != null ? dadosPersonagem.frameFollowThrough : -1;

                FaseAtaque faseAnterior = faseAtaqueAtual;

                if (frameAtual < inicioHit)
                    faseAtaqueAtual = FaseAtaque.Windup;
                else if (followThrough >= 0 && frameAtual == followThrough)
                    faseAtaqueAtual = FaseAtaque.FollowThrough;
                else if (frameAtual >= inicioHit)
                    faseAtaqueAtual = FaseAtaque.Hit;

                // Só reseta o "já aplicou dano" quando a FASE muda de verdade (entrou
                // agora no Hit, ou entrou agora no FollowThrough) — nunca a cada frame.
                // Antes resetava sempre aqui, então se a fase Hit durasse mais de um
                // frame de animação, cada frame aplicava o dano (e o gasto de energia
                // de quem defendia) de novo — 2 ou 3 vezes no mesmo golpe.
                if (faseAtaqueAtual != faseAnterior)
                    danoJaAplicado = false;

                AplicarFrameAtual();
                AplicarDanoAtaquePorFrame();
                return;
            }

            bool ultimoFrame = frameAtual >= frames.Length - 1;

            if (ultimoFrame && animacaoUmaVezAtiva)
            {
                if (estadoAtual == EstadoAnim.Death)
                {
                    AplicarFrameAtual();
                    return;
                }

                animacaoUmaVezAtiva = false;
                estadoAtual = estadoAnterior;
                frameAtual = 0;
                cronometroFrame = 0f;
                AplicarFrameAtual();
                return;
            }

            frameAtual = ultimoFrame ? 0 : frameAtual + 1;
            AplicarFrameAtual();
        }
    }

    // Pulo: frame 0 = subindo (velocidade Y positiva), frame 1 = caindo (Y negativa ou
    // zero no ápice). Funciona pra qualquer personagem, desde que framesJump tenha 2
    // sprites — se tiver só 1, fica só nele; se tiver mais de 2, os extras não são usados.
    void AtualizarFrameJump()
    {
        Sprite[] frames = ObterFramesDoEstado(EstadoAnim.Jump);
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;

        int novoFrame;
        if (frames.Length == 1)
        {
            novoFrame = 0;
        }
        else
        {
            bool subindo = rb != null && rb.linearVelocity.y > 0.01f;
            novoFrame = subindo ? 0 : 1;
            novoFrame = Mathf.Min(novoFrame, frames.Length - 1);
        }

        if (novoFrame != frameAtual)
        {
            frameAtual = novoFrame;
            AplicarFrameAtual();
        }
    }

    void AplicarFrameAtual()
    {
        Sprite[] frames = ObterFramesDoEstado(estadoAtual);
        if (frames == null || frames.Length == 0) return;
        int idx = Mathf.Clamp(frameAtual, 0, frames.Length - 1);
        if (frames[idx] != null)
            spriteRenderer.sprite = frames[idx];
    }

    Sprite[] ObterFramesDoEstado(EstadoAnim estado)
    {
        if (dadosPersonagem == null) return null;
        switch (estado)
        {
            case EstadoAnim.Idle: return ComVarianteFogo(dadosPersonagem.framesIdle, dadosPersonagem.framesIdleFogo);
            case EstadoAnim.Move: return ComVarianteFogo(dadosPersonagem.framesMove, dadosPersonagem.framesMoveFogo);
            case EstadoAnim.Jump: return ComVarianteFogo(dadosPersonagem.framesJump, dadosPersonagem.framesJumpFogo);
            case EstadoAnim.Defend: return ComVarianteFogo(dadosPersonagem.framesDefend, dadosPersonagem.framesDefendFogo);
            case EstadoAnim.Attack: return ComVarianteFogo(dadosPersonagem.framesAttack, dadosPersonagem.framesAttackFogo);
            case EstadoAnim.Special: return dadosPersonagem.framesSpecial;
            case EstadoAnim.Ultimate: return dadosPersonagem.framesUltimate;
            case EstadoAnim.Hit: return dadosPersonagem.framesHit;
            case EstadoAnim.Death: return dadosPersonagem.framesDeath;
            default: return dadosPersonagem.framesIdle;
        }
    }

    // Enquanto a espada em chamas estiver ativa, troca pela variante em chamas do
    // estado — mas só se ela estiver preenchida. Personagem sem a variante (ou sem
    // o especial alternável ligado) continua usando os frames normais de sempre.
    Sprite[] ComVarianteFogo(Sprite[] normal, Sprite[] fogo)
    {
        if (espadaEmChamasAtiva && fogo != null && fogo.Length > 0) return fogo;
        return normal;
    }

    float ObterFpsDoEstado(EstadoAnim estado)
    {
        if (dadosPersonagem == null) return 6f;
        switch (estado)
        {
            case EstadoAnim.Idle: return dadosPersonagem.fpsIdle;
            case EstadoAnim.Move: return dadosPersonagem.fpsMove;
            case EstadoAnim.Jump: return dadosPersonagem.fpsJump;
            case EstadoAnim.Defend: return dadosPersonagem.fpsDefend;
            case EstadoAnim.Attack: return dadosPersonagem.fpsAttack;
            case EstadoAnim.Special: return dadosPersonagem.fpsSpecial;
            case EstadoAnim.Ultimate: return dadosPersonagem.fpsUltimate;
            case EstadoAnim.Hit: return dadosPersonagem.fpsHit;
            case EstadoAnim.Death: return dadosPersonagem.fpsDeath;
            default: return 6f;
        }
    }

    // ── Sistema de sons ───────────────────────────────────────────────────

    // Toca um AudioClip uma vez (PlayOneShot — não interrompe outros sons)
   void TocarSom(AudioClip clip, float volumeIndividual = 1f)
    {
        if (clip == null || audioSource == null)
            return;

        float volumeFinal = volumeIndividual;

        if (AudioManager.Instance != null)
            volumeFinal = AudioManager.Instance.ObterVolumeFinalEfeitos(
                dadosPersonagem.volumeEfeitos * volumeIndividual);

        audioSource.pitch = dadosPersonagem.pitchEfeitos;

        audioSource.PlayOneShot(clip, volumeFinal);
    }

    // Chamado pelo AudioManager (evento OnVolumeChanged) sempre que os sliders de som
    // são alterados. Necessário porque o som de passos fica em loop — os demais sons
    // (PlayOneShot) já pegam o volume correto no momento em que são tocados.
    void AtualizarVolumeSomEmLoop()
    {
        if (dadosPersonagem == null || audioSourcePassos == null) return;
        if (!audioSourcePassos.isPlaying) return;

        audioSourcePassos.volume = AudioManager.Instance != null
            ? AudioManager.Instance.ObterVolumeFinalEfeitos(
                dadosPersonagem.volumeEfeitos * dadosPersonagem.volumePassos)
            : dadosPersonagem.volumePassos;
    }

    // Detecta início de movimento (passos) e pouso no chão
    void AtualizarSons()
    {
        if (dadosPersonagem == null || audioSource == null || audioSourcePassos == null) return;

        bool estaMovendoAgora = Mathf.Abs(movimento) > 0.05f && estaNoChao
                                && !animacaoUmaVezAtiva; // não toca passos durante ataque/hit

        // Passos: inicia loop ao começar a andar, para ao parar.
        // Usa audioSourcePassos (dedicado) — assim o pitch/volume dos passos não é
        // afetado por outros efeitos (ataque, hit, etc.) tocados no meio do loop.
        if (dadosPersonagem.somPassos != null)
        {
            if (estaMovendoAgora && !estavaMovendoAntes)
            {
                audioSourcePassos.clip = dadosPersonagem.somPassos;
                audioSourcePassos.volume =
                    AudioManager.Instance != null
                        ? AudioManager.Instance.ObterVolumeFinalEfeitos(
                            dadosPersonagem.volumeEfeitos *
                            dadosPersonagem.volumePassos)
                        : dadosPersonagem.volumePassos;
                audioSourcePassos.pitch = dadosPersonagem.pitchEfeitos;
                audioSourcePassos.Play();
            }
            else if (!estaMovendoAgora && estavaMovendoAntes)
            {
                audioSourcePassos.Stop();
            }
        }

        // Pouso: toca som ao tocar o chão vindo do ar
        if (!estavaNoChaoBefore && estaNoChao && dadosPersonagem.somPouso != null)
            TocarSom(
                dadosPersonagem.somPouso,
                dadosPersonagem.volumePouso
            );

        estavaMovendoAntes = estaMovendoAgora;
        estavaNoChaoBefore = estaNoChao;
    }

    // ── Colisão com chão ──────────────────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Chao")) return;
        estaNoChao = true;

        // Ao pousar, se estava em Jump, finaliza a animação de uma vez
        if (animacaoUmaVezAtiva && estadoAtual == EstadoAnim.Jump)
        {
            animacaoUmaVezAtiva = false;
            estadoAtual = estadoAnterior;
            frameAtual = 0;
            cronometroFrame = 0f;
            AplicarFrameAtual();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
            estaNoChao = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Chao"))
            estaNoChao = false;
    }
}