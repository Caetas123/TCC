using UnityEngine;

public enum TipoGastoEspecial
{
    BarraCheia,
    ZeraTudo,
    GastoFixo,
    GastoGradual
}

public enum TipoUsoUltimate
{
    BarraCheia,
    GastoFixo
}

[CreateAssetMenu(fileName = "NovoPersonagem", menuName = "Jogo/Dados do Personagem")]
public class DadosPersonagem : ScriptableObject
{
    [Header("Identificação")]
    public string nomePersonagem;
    public Sprite spriteCorpo;
    public Sprite spriteRosto;

    [Header("Status")]
    public int vidaMax = 100;
    public float energiaMax = 100f;
    public float velocidade = 6f;
    public float forcaPulo = 12f;

    [Header("Ataque normal")]
    public int danoAtaque = 10;
    public float alcanceAtaque = 2f;
    public float custoAtaque = 5f;
    [Tooltip("Alcance do frame de follow-through (frame 3) — recomendado ~60% do alcance normal")]
    public float alcanceAtaqueFollowThrough = 1.2f;

    [Header("Especial")]
    public int danoEspecial = 25;
    public float alcanceEspecial = 2.5f;
    public TipoGastoEspecial tipoGastoEspecial = TipoGastoEspecial.GastoFixo;
    public float custoEspecial = 25f;

    [Header("Especial Única — Salto + Rachadura (ex: Jamanta)")]
    [Tooltip("Sprite da rachadura que aparece no chão. Deixe VAZIO pra esse personagem usar o especial padrão (dano simples à distância).")]
    public Sprite spriteRachadura;
    [Tooltip("Multiplicador da força de pulo normal, pra altura do salto do especial (ex: 3 = triplo da altura do pulo comum)")]
    public float multiplicadorAlturaSaltoRachadura = 3f;
    [Tooltip("Raio de alcance de cada rachadura no chão")]
    public float raioRachadura = 1.5f;
    [Tooltip("Quantas rachaduras aparecem ao todo")]
    public int quantidadeRachaduras = 3;
    [Tooltip("Intervalo entre o aparecimento de cada rachadura")]
    public float intervaloRachaduras = 0.5f;
    [Tooltip("Multiplicador de dano de cada rachadura em relação ao danoEspecial")]
    public float multiplicadorDanoRachadura = 1.5f;
    [Tooltip("Duração (em segundos) da hitbox de dano de cada rachadura")]
    public float duracaoHitboxRachadura = 0.2f;

    [Header("Ultimate")]
    public int danoUltimate = 45;
    public float alcanceUltimate = 3f;
    public TipoUsoUltimate tipoUsoUltimate = TipoUsoUltimate.BarraCheia;

    // ── Ultimate Único — Raio com Recuo (ex: Jamanta) ────────────────────────
    // Diferente do ultimate padrão (dano instantâneo no aperto do botão), esse
    // sincroniza o dano com um frame específico da animação de Ultimate (o
    // personagem se posiciona antes) e o raio tem ciclo de vida em 3 fases:
    // CRESCE (rápido, saindo da origem até o alvo) → fica CHEIO (dano aplicado
    // aqui) → ENFRAQUECE (esmaece até sumir, e nessa fase dá um dreno de ENERGIA
    // no alvo em vez de dano de vida — o raio "morrendo" ainda entrega um resíduo,
    // só que mais fraco).
    [Header("Ultimate Único — Raio com Recuo (ex: Jamanta)")]
    [Tooltip("Sprite ÚNICO do raio, já com a ponta de origem (brilho) e o corpo do raio no mesmo desenho — não precisa mais de um sprite de lampejo separado.")]
    public Sprite spriteRaioUltimate;
    [Tooltip("Frame (0-based) da animação de Ultimate em que o raio de fato dispara — os frames antes disso são só o personagem se posicionando")]
    public int frameLancamentoRaioUltimate = 4;
    [Range(0f, 1f)]
    [Tooltip("A que altura do corpo o raio sai, como FRAÇÃO da altura real do personagem na cena (0 = pé, 1 = topo da cabeça) — não é um valor fixo, porque a escala dos personagens muda de cena pra cena")]
    public float fracaoAlturaRaioUltimate = 0.7f;
    [Tooltip("Duração (segundos) do crescimento do raio, saindo da origem até alcançar o alvo — rápido e natural, não instantâneo (tipo o corte em pedaços da rachadura, só que revelado por escala em vez de pedaços fixos)")]
    public float duracaoCrescimentoRaio = 0.08f;
    [Tooltip("Atraso (segundos) entre o raio terminar de crescer e o dano realmente acertar — só pra dar uma sensação extra de impacto, além do próprio crescimento")]
    public float delayRaioUltimate = 0.05f;
    [Tooltip("Quanto tempo (segundos) o raio fica CHEIO (força total, depois de crescer e antes de começar a enfraquecer)")]
    public float duracaoVisualRaioUltimate = 0.15f;
    [Tooltip("Duração (segundos) do enfraquecimento do raio — esmaece (fade) até sumir de vez, em vez de cortar de uma vez")]
    public float duracaoEnfraquecimentoRaio = 0.25f;
    [Tooltip("Quanto de ENERGIA (não vida) o oponente perde quando o raio começa a enfraquecer — o resíduo do golpe morrendo ainda drena algo, só que energia em vez de dano")]
    public float energiaDrenoRaioEnfraquecendo = 15f;
    [Tooltip("Força do recuo (empurrão pra trás) no instante em que o raio dispara")]
    public float forcaRecuoUltimate = 6f;
    [Tooltip("Duração do recuo (segundos) — decai suavemente até parar, não é um teleporte instantâneo")]
    public float duracaoRecuoUltimate = 0.25f;

    // ── Movimento durante cada animação ─────────────────────────────────────
    // Pra cada animação: "Pode Se Mover" desligado = personagem trava completamente
    // no lugar (equivale a multiplicador 0, mas mais claro de configurar). Ligado =
    // o multiplicador ao lado controla o quanto (0 = parado, 1 = velocidade normal).
    [Header("Movimento — Ataque normal")]
    public bool podeSeMoverDuranteAtaque = true;
    [Tooltip("Só usado se 'Pode Se Mover' estiver ligado (0=parado, 0.1=90% reduzido)")]
    [Range(0f, 1f)] public float multiplicadorMovimentoAtaque = 0.1f;

    [Header("Movimento — Especial")]
    [Tooltip("Não afeta o Especial Única (Salto + Rachadura) — esse sempre trava o personagem durante o salto, faz parte do próprio golpe")]
    public bool podeSeMoverDuranteEspecial = false;
    [Range(0f, 1f)] public float multiplicadorMovimentoEspecial = 0f;

    [Header("Movimento — Ultimate")]
    public bool podeSeMoverDuranteUltimate = false;
    [Range(0f, 1f)] public float multiplicadorMovimentoUltimate = 0f;

    [Header("Movimento — Defesa")]
    public bool podeSeMoverDuranteDefesa = false;
    [Range(0f, 1f)] public float multiplicadorMovimentoDefesa = 0f;

    [Header("Movimento — Hit (recebendo dano)")]
    public bool podeSeMoverDuranteHit = false;
    [Range(0f, 1f)] public float multiplicadorMovimentoHit = 0f;
    public float custoUltimate = 100f;

    [Header("Defesa")]
    [Range(0f, 1f)] public float porcentagemBloqueioDefesa = 1f;
    public float custoDefesa = 20f;

    [Header("Energia")]
    public float velocidadeRecargaEnergia = 10f;

    // ── Especial alternável — Espada em Chamas (ex: Diego) ──────────────────
    // Diferente do especial padrão (toca uma vez e acaba), esse é LIGA/DESLIGA:
    // primeiro toque ativa (custa uma % da energia máxima na hora), enquanto fica
    // ativo drena energia por segundo até acabar sozinho, e um segundo toque
    // desativa manualmente antes disso. Ativo, o ataque básico passa a aplicar
    // queimação (dano contínuo que ignora bloqueio) além do dano normal do golpe.
    [Header("Especial Alternável — Espada em Chamas (ex: Diego)")]
    [Tooltip("Ligue só no personagem que usa esse tipo de especial (liga/desliga, ex: Diego) — substitui o especial padrão por completo nele.")]
    public bool especialAlternavelComQueimadura = false;
    [Tooltip("Quanto da energia MÁXIMA (%) é gasto no instante em que o especial é ATIVADO")]
    public float custoAtivarEspecialAlternavel = 30f;
    [Tooltip("Quanto da energia MÁXIMA (%) é drenado POR SEGUNDO enquanto o especial está ativo — ele desativa sozinho quando a energia chega a zero")]
    public float drenoPorSegundoEspecialAlternavel = 3f;
    [Tooltip("Dano de queimação por segundo, aplicado pelo ataque básico enquanto o especial estiver ativo")]
    public int danoQueimacaoPorSegundo = 3;
    [Tooltip("Duração da queimação (segundos) quando o golpe acerta um oponente que NÃO está defendendo")]
    public float duracaoQueimacao = 5f;
    [Tooltip("Duração da queimação (segundos) quando o golpe acerta um oponente que ESTÁ defendendo no momento do impacto — a queimação em si nunca é bloqueada, só a duração fica mais curta")]
    public float duracaoQueimacaoDefendendo = 3f;

    // ── Animações ──────────────────────────────────────────────────────────
    [Header("Animação — Idle (parado)")]
    public Sprite[] framesIdle;
    public float fpsIdle = 6f;
    [Tooltip("Variante com a espada em chamas (especial alternável ativo). Vazio = usa os frames normais acima.")]
    public Sprite[] framesIdleFogo;

    [Header("Animação — Move (andando)")]
    public Sprite[] framesMove;
    public float fpsMove = 10f;
    [Tooltip("Variante com a espada em chamas (especial alternável ativo). Vazio = usa os frames normais acima.")]
    public Sprite[] framesMoveFogo;

    [Header("Animação — Jump (no ar)")]
    public Sprite[] framesJump;
    public float fpsJump = 8f;
    [Tooltip("Variante com a espada em chamas (especial alternável ativo). Vazio = usa os frames normais acima.")]
    public Sprite[] framesJumpFogo;

    [Header("Animação — Defend (defendendo)")]
    public Sprite[] framesDefend;
    public float fpsDefend = 8f;
    [Tooltip("Variante com a espada em chamas (especial alternável ativo). Vazio = usa os frames normais acima.")]
    public Sprite[] framesDefendFogo;

    [Header("Animação — Attack (ataque normal) — toca uma vez")]
    public Sprite[] framesAttack;
    public float fpsAttack = 12f;
    [Tooltip("Frame (0-based) a partir do qual o dano começa a ser aplicado")]
    public int frameInicioHitbox = 1;
    [Tooltip("Frame (0-based) do follow-through com dano reduzido. -1 para desativar")]
    public int frameFollowThrough = -1;
    [Tooltip("Variante com a espada em chamas (especial alternável ativo). Vazio = usa os frames normais acima.")]
    public Sprite[] framesAttackFogo;

    [Header("Animação — Special (especial) — toca uma vez")]
    public Sprite[] framesSpecial;
    public float fpsSpecial = 12f;

    [Header("Animação — Ultimate — toca uma vez")]
    public Sprite[] framesUltimate;
    public float fpsUltimate = 10f;

    [Header("Animação — Hit (recebendo dano) — toca uma vez")]
    public Sprite[] framesHit;
    public float fpsHit = 12f;

    [Header("Animação — Death (morte) — toca uma vez e para no último frame")]
    public Sprite[] framesDeath;
    public float fpsDeath = 8f;

    // ── Sons específicos do personagem ────────────────────────────────────
    [Header("Sons — Ataque")]
    [Tooltip("Som ao iniciar o ataque (windup/grito de esforço)")]
    public AudioClip somAtaqueInicio;
    [Tooltip("Som do impacto do ataque ao acertar o oponente")]
    public AudioClip somAtaqueImpacto;
    [Tooltip("Som do ataque especial")]
    public AudioClip somEspecial;
    [Tooltip("Som do ultimate")]
    public AudioClip somUltimate;

    [Header("Sons — Dano e morte")]
    [Tooltip("Som ao receber dano")]
    public AudioClip somHit;
    [Tooltip("Som de morte do personagem")]
    public AudioClip somMorte;

    [Header("Sons — Movimento")]
    [Tooltip("Som de passos ao andar (loop enquanto se move)")]
    public AudioClip somPassos;
    [Tooltip("Som ao pular")]
    public AudioClip somPulo;
    [Tooltip("Som ao pousar no chão")]
    public AudioClip somPouso;

    // ── Configuração técnica dos sons ───────────────────────────────────────

    [Header("Configuração de Áudio")]

    [Tooltip("Multiplicador de volume para todos os efeitos deste personagem.")]
    [Range(0f, 2f)]
    public float volumeEfeitos = 1f;

    [Tooltip("Altura (Pitch) de todos os efeitos sonoros deste personagem.")]
    [Range(0.5f, 2f)]
    public float pitchEfeitos = 1f;

    [Header("Volumes Individuais")]

    [Range(0f, 2f)]
    public float volumePassos = 1f;

    [Range(0f, 2f)]
    public float volumePulo = 1f;

    [Range(0f, 2f)]
    public float volumePouso = 1f;

    [Range(0f, 2f)]
    public float volumeAtaqueInicio = 1f;

    [Range(0f, 2f)]
    public float volumeAtaqueImpacto = 1f;

    [Range(0f, 2f)]
    public float volumeEspecial = 1f;

    [Range(0f, 2f)]
    public float volumeUltimate = 1f;

    [Range(0f, 2f)]
    public float volumeHit = 1f;

    [Range(0f, 2f)]
    public float volumeMorte = 1f;
    }