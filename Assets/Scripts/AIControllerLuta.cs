using UnityEngine;

/// <summary>
/// IA de luta com máquina de estados comportamental.
/// Lê perfil (Agressivo/Equilibrado/Defensivo) e dificuldade (Fácil/Médio/Difícil)
/// do PlayerPrefs salvos na tela de seleção — com padrão Equilibrado + Fácil.
///
/// ── Arquitetura (importante) ──────────────────────────────────────────────
/// O loop é dividido em camadas com frequências diferentes, que é o padrão usado
/// em jogos de luta comerciais:
///
///   PENSAR (baixa frequência, ~tempo de reação): escolhe QUAL estado
///     comportamental adotar (pressionar, recuar, defender...). Roda no ciclo
///     de decisão porque trocar de plano toda hora gera indecisão/tremelique.
///
///   MOVER (todo frame): traduz o estado atual em direção de movimento, usando a
///     distância REAL do momento. Isso é o que faz a IA parecer atenta — se o
///     movimento só fosse recalculado no ciclo de decisão, ela andaria numa
///     direção congelada por até meio segundo, ultrapassando o oponente e só
///     percebendo depois. Era a principal causa da sensação de "IA burra".
///
///   BOTÕES (limitado por relógio de reação próprio): atacar, especial, defender,
///     pular. Assim o movimento responde na hora sem que os botões sejam
///     apertados 60x por segundo.
///
/// ── Filosofia de dificuldade ──────────────────────────────────────────────
/// Dificuldade NÃO reduz a frequência de ataque nem enfraquece o personagem.
/// Todos os níveis mantêm pressão constante — o que muda é:
///   • tempo de reação (o quanto ela demora pra responder)
///   • imprecisão (ataca fora de alcance, atrasa o bloqueio) — ou seja, ela ERRA
///     AGINDO MAL, não ficando parada. Ausência de ação parece bug; ação ruim
///     parece humano e é punível pelo jogador.
///   • leitura tática (punir aberturas, ler padrão, guardar energia)
///   • uso de recursos avançados (combo, finta, esquiva)
/// </summary>
public class AIControllerLuta : MonoBehaviour
{
    // ── Enums públicos ────────────────────────────────────────────────────
    public enum PerfilIA    { Equilibrado, Agressivo, Defensivo }
    public enum DificuldadeIA { Facil, Medio, Dificil }

    // ── Estados comportamentais internos ──────────────────────────────────
    private enum EstadoComportamento
    {
        Aproximar,      // avança para entrar no alcance
        Pressionar,     // está no alcance, ataca e mantém pressão
        Recuar,         // abre espaço deliberadamente
        Defender,       // bloqueia e espera abertura
        Flanquear,      // movimenta lateralmente para confundir
        Espacamento,    // joga o NEUTRO: fica logo fora do alcance do oponente,
                        // provocando o erro dele pra punir a recuperação
        Finalizar,      // oponente com pouca vida — vai tudo
        Recuperar       // própria vida baixa — mais cauteloso
    }

    // ── Referências ───────────────────────────────────────────────────────
    [Header("Referências")]
    public LutadorController2D lutador;
    public LutadorController2D oponente;

    [Header("Ativação")]
    public bool ativarAutomaticamentePeloModo = true;

    [Header("Perfil — sobrescrito pelo PlayerPrefs")]
    public PerfilIA    perfilIA    = PerfilIA.Equilibrado;
    public DificuldadeIA dificuldadeIA = DificuldadeIA.Facil;

    // ── Distâncias (derivadas do alcance REAL do personagem) ──────────────
    // Multiplicadores vêm do perfil; as distâncias finais são calculadas em
    // CalcularDistancias() a partir de dadosPersonagem.alcanceAtaque. Antes eram
    // valores fixos (2.2f) que não batiam com o alcance real (2.0f), criando uma
    // zona morta: entre 2.0 e 2.2 a IA parava de andar mas o ataque era bloqueado
    // pelo gate de alcance — ela ficava parada olhando pro oponente.
    private float multEngajamento;
    private float multSegura;
    private float pesoEspacamento;      // o quanto o perfil gosta de jogar o neutro
                                        // (Defensivo zoneia muito, Agressivo quase nada)

    private float distanciaEngajamento;   // começa a pressionar
    private float distanciaAtaque;        // alcance efetivo de ataque
    private float distanciaSegura;        // mantém ao recuar
    private bool  distanciasCalculadas = false;

    // ── Parâmetros derivados do perfil ────────────────────────────────────
    private float chanceAtaqueBase;
    private float chanceEspecialBase;
    private float chanceUltimateBase;
    private float chanceDefesaBase;
    private float chancePuloBase;
    private float chanceFintaBase;
    private float chanceComboBase;        // encadear ataque + especial
    private float chanceRecuoBase;

    private float cooldownAtaque;
    private float cooldownEspecial;
    private float cooldownUltimate;
    private float cooldownPulo;
    private float cooldownFinta;

    // ── Parâmetros de dificuldade ─────────────────────────────────────────
    private float tempoReacaoMin;
    private float tempoReacaoMax;
    private float chanceAcaoImprecisa;    // 0..1 — chance de executar a ação MAL
                                          // (ataca cedo demais e passa raspando, atrasa
                                          // o bloqueio). Substituiu a antiga
                                          // chanceErrarAtaque, que só cancelava a ação e
                                          // fazia a IA congelar em vez de errar.
    private float multCooldownAcao;       // ritmo geral das ações
    private float multDefesa;             // o quanto ela bloqueia, por dificuldade
    private bool  usaCombo;
    private bool  usaFinta;
    private bool  usaEvasao;              // esquiva/bloqueia ataques visíveis
    private float capacidadeLeitura;      // 0..1 — leitura tática (padrão do jogador,
                                          // punição de abertura, gestão de energia)

    // ── Estado interno de tempo ───────────────────────────────────────────
    private float cronometroDecisao;      // camada PENSAR
    private float cronometroAcao;         // camada BOTÕES
    private float cronometroDefesa;
    private float cronometroAtaqueCooldown;
    private float cronometroEspecialCooldown;
    private float cronometroUltimateCooldown;
    private float cronometroPuloCooldown;
    private float cronometroFintaCooldown;
    private float cronometroEstado;       // quanto tempo fica nesse estado
    private float cronometroCombo;        // janela para segundo hit do combo

    // ── Estado comportamental ─────────────────────────────────────────────
    private EstadoComportamento estadoAtual = EstadoComportamento.Aproximar;
    private float movimentoAtual = 0f;
    private bool emFinta = false;
    private float direcaoFinta = 0f;
    private float cronometroFintaAcao;
    // Direção do flanque é sorteada UMA vez, ao entrar no estado. Se fosse sorteada
    // a cada frame (agora que o movimento roda todo frame), ela tremeria no lugar.
    private float direcaoFlanque = 1f;

    // ── Memória de combate ─────────────────────────────────────────────────
    private int   danoRecebidoConsecutivo = 0;
    private float tempoUltimoDanoRecebido = -99f;
    private float tempoUltimoAtaqueBemSucedido = -99f;
    private bool  comboSegundoHitPendente = false;

    // Leitura de padrão: guarda as últimas ações do oponente pra detectar repetição
    private struct AcaoOponente
    {
        public LutadorController2D.EstadoAnim tipo;
        public float distancia;
    }
    private System.Collections.Generic.List<AcaoOponente> historicoOponente = new System.Collections.Generic.List<AcaoOponente>();
    private LutadorController2D.EstadoAnim ultimoEstadoOponenteObservado = LutadorController2D.EstadoAnim.Idle;

    // Janela de punição: fica "aberta" logo que o oponente termina uma animação de ataque
    private bool  oponenteEstavaAtacando = false;
    private float janelaPunicao = 0f;

    // ── Anti-aéreo ────────────────────────────────────────────────────────
    // Pulo é a ação mais punível de um jogo de luta: quem está no ar não pode
    // mudar de ideia. Uma IA que não castiga pulo parece burra na hora. Isso abre
    // uma janela no instante em que o oponente POUSA, que é quando ele está
    // vulnerável na recuperação.
    private bool  oponenteEstavaNoAr = false;
    private float janelaAntiAereo = 0f;

    // Combo: no Difícil a IA encadeia até 3 golpes; nos demais, no máximo 2
    private int   hitsDoComboAtual = 0;
    private int   maxHitsCombo = 2;

    // ── Anti-empate (essencial no modo CVC: IA contra IA) ─────────────────
    // Quando dois AIControllerLuta se enfrentam, eles podem entrar em espelho: ambos
    // recuam ao mesmo tempo, ambos avançam ao mesmo tempo, e a luta oscila sem
    // ninguém trocar golpe — especialmente com dois perfis Defensivos. Isso registra
    // quanto tempo passou sem NENHUM dano trocado; passando do limite, a IA força
    // aproximação e ignora o filtro de chance pra quebrar o impasse.
    private float tempoUltimaTrocaDeDano = 0f;
    private const float SEGUNDOS_ATE_FORCAR_ACAO = 3.5f;

    private bool EmImpasse()
    {
        return Time.time - tempoUltimaTrocaDeDano > SEGUNDOS_ATE_FORCAR_ACAO;
    }

    // ── Ativação ──────────────────────────────────────────────────────────
    private bool iaAtiva = false;
    private int  vidaAnteriorOponente;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (lutador == null) lutador = GetComponent<LutadorController2D>();
        if (lutador != null && oponente == null) oponente = lutador.oponente;

        DefinirAtivacaoInicial();
        CarregarPerfilDoPlayerPrefs();
        AplicarPerfil();
        AplicarDificuldade();

        estadoAtual = EstadoComportamento.Aproximar;
        ReiniciarCronometroDecisao();

        // Desfasagem inicial aleatória — no modo CVC as duas IAs começam no mesmo frame
        // com os mesmos parâmetros; sem isso elas decidiriam em lockstep, sempre no mesmo
        // instante, o que amplifica o efeito espelho (as duas avançam/recuam juntas).
        cronometroDecisao += Random.Range(0f, 0.25f);

        tempoUltimaTrocaDeDano = Time.time;

        if (oponente != null)
            vidaAnteriorOponente = oponente.vidaAtual;
    }

    void Update()
    {
        if (!iaAtiva || lutador == null || oponente == null) return;
        if (Time.timeScale == 0f) return;
        if (lutador.EstaMorto())  { lutador.IA_LimparComandos(); return; }
        if (oponente.EstaMorto()) { lutador.IA_LimparComandos(); return; }

        CalcularDistancias();
        if (!distanciasCalculadas) return; // dadosPersonagem ainda não disponível

        // Desconta cooldowns
        cronometroDecisao          -= Time.deltaTime;
        cronometroAcao             -= Time.deltaTime;
        cronometroDefesa           -= Time.deltaTime;
        cronometroAtaqueCooldown   -= Time.deltaTime;
        cronometroEspecialCooldown -= Time.deltaTime;
        cronometroUltimateCooldown -= Time.deltaTime;
        cronometroPuloCooldown     -= Time.deltaTime;
        cronometroFintaCooldown    -= Time.deltaTime;
        cronometroEstado           -= Time.deltaTime;
        cronometroCombo            -= Time.deltaTime;

        AtualizarMemoria();

        // Reação imediata a ataques do oponente (independente do ciclo de decisão)
        if (usaEvasao) TentarEsquivar();

        // ── CAMADA 1 — PENSAR: escolhe o estado (baixa frequência) ────────
        if (cronometroDecisao <= 0f)
        {
            AtualizarEstadoComportamental();
            ReiniciarCronometroDecisao();
        }

        // ── CAMADA 2 — MOVER: recalculado TODO frame ──────────────────────
        AtualizarMovimentoDoEstado();

        // ── CAMADA 3 — BOTÕES: limitada pelo relógio de reação ────────────
        if (cronometroAcao <= 0f)
        {
            ExecutarAcoesDoEstado();
            cronometroAcao = Random.Range(tempoReacaoMin, tempoReacaoMax) * 0.6f;
        }

        // Aplica movimento (finta tem prioridade)
        if (emFinta && cronometroFintaAcao > 0f)
        {
            cronometroFintaAcao -= Time.deltaTime;
            lutador.IA_DefinirMovimento(direcaoFinta);
        }
        else
        {
            emFinta = false;
            lutador.IA_DefinirMovimento(movimentoAtual);
        }

        // Defesa contínua
        lutador.IA_DefinirDefesa(cronometroDefesa > 0f);

        // Combo: segundo hit pendente dentro da janela
        if (comboSegundoHitPendente && cronometroCombo > 0f)
            TentarSegundoHitCombo();
    }

    // ── Distâncias baseadas no alcance real do personagem ─────────────────
    void CalcularDistancias()
    {
        if (distanciasCalculadas) return;
        if (lutador == null || lutador.dadosPersonagem == null) return;

        float alcance = lutador.dadosPersonagem.alcanceAtaque;

        // A distância de ataque é levemente MENOR que o alcance real, pra que a IA
        // sempre esteja de fato dentro do alcance quando decidir bater — nunca no
        // limiar exato, onde o golpe passaria raspando.
        distanciaAtaque      = alcance * 0.92f;
        distanciaEngajamento = alcance * multEngajamento;
        distanciaSegura      = alcance * multSegura;

        distanciasCalculadas = true;
    }

    // ── Memória e contexto ────────────────────────────────────────────────
    void AtualizarMemoria()
    {
        if (oponente == null) return;

        if (oponente.vidaAtual < vidaAnteriorOponente)
        {
            tempoUltimoAtaqueBemSucedido = Time.time;
            tempoUltimaTrocaDeDano       = Time.time; // houve troca — sem impasse
        }

        vidaAnteriorOponente = oponente.vidaAtual;

        // Janela de punição: abre no instante em que o oponente SAI de uma animação
        // de ataque/especial/ultimate (recuperação — momento mais seguro pra revidar)
        bool oponenteAtacandoAgora = oponente.EstaAtacando();
        if (oponenteEstavaAtacando && !oponenteAtacandoAgora)
            janelaPunicao = 0.35f;
        oponenteEstavaAtacando = oponenteAtacandoAgora;

        if (janelaPunicao > 0f) janelaPunicao -= Time.deltaTime;

        // Anti-aéreo: abre a janela no instante em que o oponente POUSA
        bool oponenteNoArAgora = !oponente.EstaNoChao();
        if (oponenteEstavaNoAr && !oponenteNoArAgora)
            janelaAntiAereo = 0.30f;
        oponenteEstavaNoAr = oponenteNoArAgora;

        if (janelaAntiAereo > 0f) janelaAntiAereo -= Time.deltaTime;

        var estadoOponente = oponente.ObterEstadoAnim();
        if (estadoOponente != ultimoEstadoOponenteObservado)
        {
            if (estadoOponente == LutadorController2D.EstadoAnim.Attack   ||
                estadoOponente == LutadorController2D.EstadoAnim.Special  ||
                estadoOponente == LutadorController2D.EstadoAnim.Ultimate ||
                estadoOponente == LutadorController2D.EstadoAnim.Jump)
            {
                historicoOponente.Add(new AcaoOponente { tipo = estadoOponente, distancia = lutador.DistanciaDoOponente() });
                if (historicoOponente.Count > 6) historicoOponente.RemoveAt(0);
            }
            ultimoEstadoOponenteObservado = estadoOponente;
        }
    }

    // Fração das últimas ações do oponente que repetiram a ação anterior
    float EstimarChanceRepeticao()
    {
        if (historicoOponente.Count < 3) return 0f;

        int total = 0, repetiu = 0;
        for (int i = 1; i < historicoOponente.Count; i++)
        {
            total++;
            if (historicoOponente[i].tipo == historicoOponente[i - 1].tipo) repetiu++;
        }
        return total > 0 ? (float)repetiu / total : 0f;
    }

    // Distância média em que o oponente costuma atacar/especializar
    float DistanciaTipicaDeAtaque()
    {
        float soma = 0f; int n = 0;
        foreach (var acao in historicoOponente)
        {
            if (acao.tipo == LutadorController2D.EstadoAnim.Attack || acao.tipo == LutadorController2D.EstadoAnim.Special)
            {
                soma += acao.distancia;
                n++;
            }
        }
        return n > 0 ? soma / n : -1f;
    }

    // Antecipa defesa quando o padrão do oponente é claro e a distância bate com o
    // "range favorito" dele — quanto maior a capacidadeLeitura, mais a IA confia nisso
    void AvaliarLeituraDePadrao(float distancia)
    {
        if (capacidadeLeitura <= 0f || cronometroDefesa > 0f) return;

        float chanceRepeticao = EstimarChanceRepeticao();
        if (chanceRepeticao < 0.5f) return;

        float distanciaTipica = DistanciaTipicaDeAtaque();
        bool distanciaCompativel = distanciaTipica > 0f && Mathf.Abs(distancia - distanciaTipica) < 0.5f;
        if (!distanciaCompativel) return;

        if (Random.value < chanceRepeticao * capacidadeLeitura)
            cronometroDefesa = Random.Range(0.25f, 0.45f);
    }

    // Chamado por ReceberDano via evento — registra dano recebido
    public void NotificarDanoRecebido()
    {
        if (Time.time - tempoUltimoDanoRecebido < 2f)
            danoRecebidoConsecutivo++;
        else
            danoRecebidoConsecutivo = 1;

        tempoUltimoDanoRecebido = Time.time;
        tempoUltimaTrocaDeDano  = Time.time; // houve troca — sem impasse
    }

    // ── CAMADA 1: PENSAR — máquina de estados ─────────────────────────────
    void AtualizarEstadoComportamental()
    {
        if (lutador.dadosPersonagem == null) return;

        float distancia       = lutador.DistanciaDoOponente();
        float vidaPropria     = lutador.vidaAtual;
        float vidaOponente    = oponente.vidaAtual;
        float vidaMaxPropria  = lutador.dadosPersonagem.vidaMax;
        float vidaMaxOponente = oponente.dadosPersonagem != null ? oponente.dadosPersonagem.vidaMax : 100f;
        float percVidaPropria = vidaPropria  / vidaMaxPropria;
        float percVidaOponente= vidaOponente / vidaMaxOponente;

        AvaliarLeituraDePadrao(distancia);

        bool mudarEstado = cronometroEstado <= 0f;

        // Prioridade 1: oponente está quase morto → finalizar
        if (percVidaOponente < 0.20f)
        {
            MudarEstado(EstadoComportamento.Finalizar, Random.Range(1.5f, 2.5f));
            return;
        }

        // Prioridade 2: própria vida muito baixa → recuperar/recuar
        if (percVidaPropria < 0.20f)
        {
            MudarEstado(EstadoComportamento.Recuperar, Random.Range(1.0f, 2.0f));
            return;
        }

        // Prioridade 3: levou dano consecutivo → muda tática
        if (danoRecebidoConsecutivo >= 3 && Time.time - tempoUltimoDanoRecebido < 3f)
        {
            danoRecebidoConsecutivo = 0;
            if (perfilIA == PerfilIA.Agressivo)
                MudarEstado(EstadoComportamento.Pressionar, Random.Range(1.0f, 2.0f));
            else
                MudarEstado(EstadoComportamento.Recuar, Random.Range(0.8f, 1.5f));
            return;
        }

        // Prioridade 4: impasse (ninguém trocou dano há muito tempo) → força aproximação.
        // Crítico no CVC, onde duas IAs podem se espelhar indefinidamente; no PVC também
        // evita a IA ficar dançando de longe contra um jogador que só espera.
        if (EmImpasse())
        {
            MudarEstado(distancia > distanciaAtaque
                ? EstadoComportamento.Aproximar
                : EstadoComportamento.Pressionar, Random.Range(0.8f, 1.4f));
            return;
        }

        if (!mudarEstado) return;

        float r = Random.value;

        // Espaçamento (jogar o neutro) é um comportamento de COMPETÊNCIA: só faz sentido
        // se a IA sabe ler o oponente pra punir o erro que ela mesma provocou. Por isso a
        // chance é o peso do perfil × a capacidade de leitura da dificuldade — no Fácil
        // praticamente não acontece, no Difícil vira a base do jogo neutro.
        if (distancia > distanciaAtaque * 0.8f && distancia <= distanciaEngajamento)
        {
            if (Random.value < pesoEspacamento * capacidadeLeitura)
            {
                MudarEstado(EstadoComportamento.Espacamento, Random.Range(0.9f, 1.8f));
                return;
            }
        }

        if (distancia > distanciaEngajamento)
        {
            MudarEstado(EstadoComportamento.Aproximar, Random.Range(0.6f, 1.2f));
        }
        else if (distancia <= distanciaAtaque)
        {
            switch (perfilIA)
            {
                case PerfilIA.Agressivo:
                    if (r < 0.70f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.8f, 1.5f));
                    else if (r < 0.85f) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.5f, 1.0f));
                    else MudarEstado(EstadoComportamento.Recuar, Random.Range(0.4f, 0.8f));
                    break;

                case PerfilIA.Defensivo:
                    if (r < 0.38f) MudarEstado(EstadoComportamento.Defender, Random.Range(0.6f, 1.2f));
                    else if (r < 0.60f) MudarEstado(EstadoComportamento.Recuar, Random.Range(0.5f, 1.0f));
                    else if (r < 0.85f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.5f, 1.0f));
                    else MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.8f));
                    break;

                default: // Equilibrado
                    if (r < 0.50f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.7f, 1.3f));
                    else if (r < 0.68f) MudarEstado(EstadoComportamento.Defender, Random.Range(0.5f, 1.0f));
                    else if (r < 0.84f) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.8f));
                    else MudarEstado(EstadoComportamento.Recuar, Random.Range(0.4f, 0.8f));
                    break;
            }
        }
        else
        {
            // Faixa intermediária — entre o alcance e a distância de engajamento
            if (r < 0.55f) MudarEstado(EstadoComportamento.Aproximar, Random.Range(0.5f, 1.0f));
            else if (r < 0.72f && usaFinta) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.7f));
            else MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.6f, 1.2f));
        }
    }

    void MudarEstado(EstadoComportamento novo, float duracao)
    {
        // Sorteia a direção do flanque UMA vez, ao entrar no estado
        if (novo == EstadoComportamento.Flanquear && estadoAtual != EstadoComportamento.Flanquear)
            direcaoFlanque = Random.value > 0.5f ? 1f : -1f;

        estadoAtual      = novo;
        cronometroEstado = duracao;
    }

    // ── CAMADA 2: MOVER — recalculado todo frame ──────────────────────────
    // Só decide DIREÇÃO a partir do estado + distância real. Nada de aleatório
    // aqui: sorteio por frame a 60fps geraria tremelique. A aleatoriedade fica na
    // camada de botões, que é limitada pelo relógio de reação.
    void AtualizarMovimentoDoEstado()
    {
        if (lutador.dadosPersonagem == null) return;

        float distancia = lutador.DistanciaDoOponente();
        float dir       = DirecaoParaOponente();
        float recuo     = -dir;

        switch (estadoAtual)
        {
            case EstadoComportamento.Aproximar:
                movimentoAtual = distancia > distanciaAtaque ? dir : 0f;
                break;

            case EstadoComportamento.Pressionar:
                if (distancia > distanciaAtaque)
                    movimentoAtual = dir;
                else if (distancia < distanciaAtaque * 0.55f)
                    movimentoAtual = recuo; // não deixa ficar colado demais
                else
                    movimentoAtual = 0f;
                break;

            case EstadoComportamento.Recuar:
                movimentoAtual = distancia < distanciaSegura ? recuo : 0f;
                break;

            case EstadoComportamento.Defender:
                movimentoAtual = 0f;
                break;

            case EstadoComportamento.Flanquear:
                // Direção fixa durante o estado; recua se estiver colado demais
                movimentoAtual = distancia < distanciaAtaque * 0.6f ? recuo : direcaoFlanque;
                break;

            case EstadoComportamento.Espacamento:
            {
                // Se o oponente acabou de errar (ou de pousar), a espera acabou:
                // fecha a distância pra punir a recuperação dele.
                if (janelaPunicao > 0f || janelaAntiAereo > 0f)
                {
                    movimentoAtual = distancia > distanciaAtaque ? dir : 0f;
                    break;
                }

                // Caso contrário, fica pairando LOGO FORA do alcance dele — perto o
                // suficiente pra ameaçar e provocar o golpe, longe o suficiente pra
                // que o golpe passe no vazio. Usa o alcance observado do oponente
                // quando já há histórico; senão, uma margem em cima do próprio.
                float alcanceDoOponente = DistanciaTipicaDeAtaque();
                if (alcanceDoOponente <= 0f) alcanceDoOponente = distanciaAtaque;
                float alvo = alcanceDoOponente * 1.18f;

                if (distancia < alvo - 0.15f)      movimentoAtual = recuo;
                else if (distancia > alvo + 0.35f) movimentoAtual = dir;
                else                                movimentoAtual = 0f;
                break;
            }

            case EstadoComportamento.Finalizar:
                movimentoAtual = distancia > distanciaAtaque ? dir : 0f;
                break;

            case EstadoComportamento.Recuperar:
                movimentoAtual = distancia < distanciaSegura ? recuo : 0f;
                break;
        }
    }

    // ── CAMADA 3: BOTÕES — limitados pelo relógio de reação ───────────────
    void ExecutarAcoesDoEstado()
    {
        if (lutador.dadosPersonagem == null) return;

        float distancia = lutador.DistanciaDoOponente();
        float dir       = DirecaoParaOponente();
        float recuo     = -dir;

        switch (estadoAtual)
        {
            case EstadoComportamento.Aproximar:
                TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Pressionar:
                TentarAtacarSeNoAlcance(distancia);
                TentarEspecialSeVale(distancia);
                TentarUltimateSeVale(distancia);

                if (usaFinta && lutador.EstaNoChao() && cronometroPuloCooldown <= 0f
                    && Random.value < chancePuloBase)
                {
                    lutador.IA_SolicitarPulo();
                    cronometroPuloCooldown = cooldownPulo;
                }

                if (usaFinta && cronometroFintaCooldown <= 0f && Random.value < chanceFintaBase)
                    IniciarFinta(dir, recuo);
                break;

            case EstadoComportamento.Recuar:
                // Ataca se o oponente entrar no alcance mesmo recuando
                if (distancia <= distanciaAtaque && Random.value < chanceAtaqueBase * 0.7f)
                    TentarAtacarSeNoAlcance(distancia);
                if (Random.value < chanceDefesaBase * multDefesa)
                    cronometroDefesa = Random.Range(0.2f, 0.5f);
                break;

            case EstadoComportamento.Defender:
                if (Random.value < chanceDefesaBase * multDefesa)
                    cronometroDefesa = Random.Range(0.3f, 0.8f);
                // Contra-ataca na abertura do oponente
                if (distancia <= distanciaAtaque && Random.value < chanceAtaqueBase)
                    TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Flanquear:
                if (usaFinta && cronometroFintaCooldown <= 0f && Random.value < chanceFintaBase * 1.5f)
                    IniciarFinta(dir, recuo);
                TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Espacamento:
                // A punição em si é feita dentro de TentarAtacarSeNoAlcance, que já
                // trata janelaPunicao/janelaAntiAereo ignorando o filtro de chance.
                TentarAtacarSeNoAlcance(distancia);

                // Se o oponente está atacando e estamos no alcance dele, bloqueia —
                // o zoneador não come golpe de graça enquanto espera.
                if (oponente.EstaAtacando() && distancia <= distanciaAtaque * 1.2f
                    && Random.value < Mathf.Max(chanceDefesaBase * multDefesa, capacidadeLeitura * 0.7f))
                    cronometroDefesa = Random.Range(0.25f, 0.5f);

                // Poke ocasional pra não virar estátua e forçar reação do oponente
                if (usaFinta && cronometroFintaCooldown <= 0f && Random.value < chanceFintaBase)
                    IniciarFinta(dir, recuo);
                break;

            case EstadoComportamento.Finalizar:
                TentarAtacarSeNoAlcance(distancia);
                TentarEspecialSeVale(distancia);
                TentarUltimateSeVale(distancia);
                if (lutador.EstaNoChao() && cronometroPuloCooldown <= 0f && Random.value < 0.12f)
                {
                    lutador.IA_SolicitarPulo();
                    cronometroPuloCooldown = cooldownPulo;
                }
                break;

            case EstadoComportamento.Recuperar:
                if (Random.value < chanceDefesaBase * multDefesa * 1.4f)
                    cronometroDefesa = Random.Range(0.4f, 0.9f);
                TentarEspecialSeVale(distancia);
                break;
        }
    }

    // ── Ações de combate ──────────────────────────────────────────────────
    void TentarAtacarSeNoAlcance(float distancia)
    {
        if (lutador.dadosPersonagem == null) return;
        if (cronometroAtaqueCooldown > 0f) return;

        float alcance = lutador.dadosPersonagem.alcanceAtaque;

        // Erro humano: às vezes ela puxa o golpe cedo demais e passa raspando.
        // Isso é um ERRO VISÍVEL e punível — muito melhor que a versão antiga, que
        // simplesmente cancelava o ataque e deixava a IA parada feito estátua.
        bool impreciso = Random.value < chanceAcaoImprecisa;
        float alcanceEfetivo = impreciso ? alcance * 1.40f : alcance;

        if (distancia > alcanceEfetivo) return;

        // Oportunidades de punição: o oponente acabou de sair de um ataque (recuperação)
        // ou acabou de POUSAR de um pulo. Nos dois casos ele está vulnerável — a IA
        // aproveita direto, na proporção da capacidade de leitura da dificuldade.
        bool aproveitandoAbertura = (janelaPunicao > 0f || janelaAntiAereo > 0f)
                                    && Random.value < capacidadeLeitura;

        // Em impasse o filtro de chance é ignorado — se está no alcance, bate. É o que
        // destrava lutas CVC em que os dois lados ficam se olhando.
        bool forcandoPorImpasse = EmImpasse();

        if (!aproveitandoAbertura && !forcandoPorImpasse && Random.value > chanceAtaqueBase) return;

        lutador.IA_SolicitarAtaque();
        cronometroAtaqueCooldown = cooldownAtaque;
        if (aproveitandoAbertura) { janelaPunicao = 0f; janelaAntiAereo = 0f; }

        if (usaCombo && !impreciso && Random.value < chanceComboBase)
        {
            comboSegundoHitPendente = true;
            hitsDoComboAtual = 1;
            cronometroCombo = Random.Range(0.15f, 0.35f);
        }
    }

    void TentarSegundoHitCombo()
    {
        comboSegundoHitPendente = false;
        if (lutador.dadosPersonagem == null) return;

        float distancia = lutador.DistanciaDoOponente();
        bool encadeou = false;

        if (distancia <= lutador.dadosPersonagem.alcanceEspecial
            && cronometroEspecialCooldown <= 0f
            && !DevoGuardarEnergiaParaUltimate()
            && Random.value < chanceEspecialBase)
        {
            lutador.IA_SolicitarEspecial();
            cronometroEspecialCooldown = cooldownEspecial;
            encadeou = true;
        }
        else if (distancia <= lutador.dadosPersonagem.alcanceAtaque
                 && cronometroAtaqueCooldown <= 0f)
        {
            lutador.IA_SolicitarAtaque();
            cronometroAtaqueCooldown = cooldownAtaque;
            encadeou = true;
        }

        if (!encadeou) { hitsDoComboAtual = 0; return; }

        hitsDoComboAtual++;

        // Combo mais longo é privilégio do Difícil (maxHitsCombo = 3). Nos outros
        // níveis para no segundo hit, o que mantém a diferença de competência visível.
        if (hitsDoComboAtual < maxHitsCombo && Random.value < chanceComboBase * 0.7f)
        {
            comboSegundoHitPendente = true;
            cronometroCombo = Random.Range(0.15f, 0.30f);
        }
        else
        {
            hitsDoComboAtual = 0;
        }
    }

    void TentarEspecialSeVale(float distancia)
    {
        if (lutador.dadosPersonagem == null) return;
        if (distancia > lutador.dadosPersonagem.alcanceEspecial) return;
        if (cronometroEspecialCooldown > 0f) return;
        if (DevoGuardarEnergiaParaUltimate()) return;
        if (Random.value > chanceEspecialBase) return;

        lutador.IA_SolicitarEspecial();
        cronometroEspecialCooldown = cooldownEspecial;
    }

    // Gestão de energia: se o oponente já está perto de poder ser finalizado e a energia
    // própria está "a caminho" do ultimate, vale mais guardar do que gastar no especial.
    bool DevoGuardarEnergiaParaUltimate()
    {
        if (capacidadeLeitura <= 0f) return false;
        if (oponente == null || oponente.dadosPersonagem == null) return false;

        float percVidaOponente = (float)oponente.vidaAtual / oponente.dadosPersonagem.vidaMax;
        float energiaPropria   = lutador.ObterEnergiaNormalizada();

        bool oponentePertoDeFinalizar   = percVidaOponente < 0.30f;
        bool energiaEmRotaParaUltimate  = energiaPropria >= 0.55f && energiaPropria < 0.95f;

        if (oponentePertoDeFinalizar && energiaEmRotaParaUltimate)
            return Random.value < capacidadeLeitura;

        return false;
    }

    void TentarUltimateSeVale(float distancia)
    {
        if (lutador.dadosPersonagem == null) return;
        if (distancia > lutador.dadosPersonagem.alcanceUltimate) return;
        if (cronometroUltimateCooldown > 0f) return;
        if (lutador.ObterEnergiaNormalizada() < 0.85f) return;
        // Com energia cheia e no alcance, o ultimate DEVE sair — é o momento de
        // espetáculo do personagem. Antes era filtrado por chance * precisão e
        // praticamente nunca aparecia nas dificuldades baixas.
        if (Random.value > chanceUltimateBase) return;

        lutador.IA_SolicitarUltimate();
        cronometroUltimateCooldown = cooldownUltimate;
    }

    void TentarEsquivar()
    {
        if (oponente == null) return;
        if (!oponente.EstaAtacando()) return;
        if (cronometroDefesa > 0f) return;

        float distancia = lutador.DistanciaDoOponente();
        if (distancia > distanciaAtaque * 1.3f) return;

        // A imprecisão atrasa/perde a reação defensiva em vez de anulá-la sempre —
        // nas dificuldades baixas ela tenta bloquear, só que tarde demais às vezes.
        if (Random.value < chanceAcaoImprecisa) return;

        float r = Random.value;

        // A defesa reativa escala com a LEITURA, não só com o perfil. Sem isso um
        // Agressivo Difícil (chanceDefesaBase 0.18) quase nunca bloquearia — competência
        // e personalidade são eixos separados: ele é agressivo, não cego.
        float chanceBloqueio = Mathf.Max(chanceDefesaBase * multDefesa, capacidadeLeitura * 0.7f);

        if (r < chanceBloqueio)
            cronometroDefesa = Random.Range(0.25f, 0.55f);
        else if (r < chanceBloqueio + chanceRecuoBase)
            movimentoAtual = -DirecaoParaOponente();
    }

    void IniciarFinta(float dir, float recuo)
    {
        emFinta           = true;
        movimentoAtual    = dir;
        direcaoFinta      = recuo;
        cronometroFintaAcao = Random.Range(0.08f, 0.20f);
        cronometroFintaCooldown = cooldownFinta;
    }

    float DirecaoParaOponente()
    {
        if (oponente == null) return 1f;
        return oponente.transform.position.x > lutador.transform.position.x ? 1f : -1f;
    }

    void ReiniciarCronometroDecisao()
    {
        cronometroDecisao = Random.Range(tempoReacaoMin, tempoReacaoMax);
    }

    // ── Carregar perfil do PlayerPrefs ────────────────────────────────────
    void CarregarPerfilDoPlayerPrefs()
    {
        bool ehP1 = lutador != null && lutador.jogador1;
        int indice, estilo, dificuldade;

        if (ehP1)
        {
            indice      = PlayerPrefs.GetInt("PersonagemP1", 0);
            estilo      = PlayerPrefs.GetInt("IA_P1_Estilo_" + indice, 1);      // 1 = Equilibrado
            dificuldade = PlayerPrefs.GetInt("IA_P1_Dificuldade_" + indice, 0); // 0 = Fácil
        }
        else
        {
            indice      = PlayerPrefs.GetInt("PersonagemP2", 0);
            estilo      = PlayerPrefs.GetInt("IA_P2_Estilo_" + indice, 1);
            dificuldade = PlayerPrefs.GetInt("IA_P2_Dificuldade_" + indice, 0);
        }

        estilo      = Mathf.Clamp(estilo,      0, 2);
        dificuldade = Mathf.Clamp(dificuldade, 0, 2);

        perfilIA    = (PerfilIA)estilo;
        dificuldadeIA = (DificuldadeIA)dificuldade;

        Debug.Log($"[IA] {(ehP1 ? "P1" : "P2")} — Perfil: {perfilIA} | Dificuldade: {dificuldadeIA}");
    }

    // ── Aplicar parâmetros do perfil ──────────────────────────────────────
    // O perfil define a PERSONALIDADE (como ela luta). A dificuldade define a
    // COMPETÊNCIA (o quão bem ela executa). São eixos independentes — um Defensivo
    // Difícil e um Agressivo Fácil devem sentir-se completamente diferentes, e é
    // isso que dá identidade a cada configuração da tela de seleção.
    void AplicarPerfil()
    {
        switch (perfilIA)
        {
            case PerfilIA.Agressivo:
                multEngajamento      = 2.4f;
                multSegura           = 1.1f;
                pesoEspacamento      = 0.15f; // rushdown: quase não joga o neutro
                chanceAtaqueBase     = 0.85f;
                chanceEspecialBase   = 0.45f;
                chanceUltimateBase   = 0.85f;
                chanceDefesaBase     = 0.18f;
                chancePuloBase       = 0.10f;
                chanceFintaBase      = 0.10f;
                chanceComboBase      = 0.60f;
                chanceRecuoBase      = 0.12f;
                cooldownAtaque       = 0.38f;
                cooldownEspecial     = 1.1f;
                cooldownUltimate     = 1.8f;
                cooldownPulo         = 1.2f;
                cooldownFinta        = 0.8f;
                break;

            case PerfilIA.Defensivo:
                multEngajamento      = 1.9f;
                multSegura           = 1.5f;
                pesoEspacamento      = 0.75f; // zoneador: o neutro é o jogo dele
                chanceAtaqueBase     = 0.55f;
                chanceEspecialBase   = 0.28f;
                chanceUltimateBase   = 0.80f;
                chanceDefesaBase     = 0.65f;
                chancePuloBase       = 0.04f;
                chanceFintaBase      = 0.25f;
                chanceComboBase      = 0.25f;
                chanceRecuoBase      = 0.55f;
                cooldownAtaque       = 0.62f;
                cooldownEspecial     = 1.5f;
                cooldownUltimate     = 2.0f;
                cooldownPulo         = 2.0f;
                cooldownFinta        = 1.2f;
                break;

            default: // Equilibrado
                multEngajamento      = 2.1f;
                multSegura           = 1.3f;
                pesoEspacamento      = 0.40f; // mistura pressão e neutro
                chanceAtaqueBase     = 0.72f;
                chanceEspecialBase   = 0.35f;
                chanceUltimateBase   = 0.82f;
                chanceDefesaBase     = 0.38f;
                chancePuloBase       = 0.07f;
                chanceFintaBase      = 0.15f;
                chanceComboBase      = 0.42f;
                chanceRecuoBase      = 0.28f;
                cooldownAtaque       = 0.48f;
                cooldownEspecial     = 1.3f;
                cooldownUltimate     = 1.9f;
                cooldownPulo         = 1.6f;
                cooldownFinta        = 1.0f;
                break;
        }
    }

    // ── Aplicar parâmetros de dificuldade ─────────────────────────────────
    // REGRA: dificuldade não corta a frequência de ataque nem enfraquece o
    // personagem. Todos os níveis mantêm pressão — muda o tempo de reação, a
    // imprecisão (erro visível e punível), a leitura tática e os recursos
    // avançados. Era o empilhamento de nerfs multiplicativos (chance × precisão
    // × erro × ignorar decisão) que fazia o Fácil atacar ~1x a cada 10 segundos.
    void AplicarDificuldade()
    {
        switch (dificuldadeIA)
        {
            case DificuldadeIA.Facil:
                // Reage devagar e erra bastante — mas ERRA ATACANDO, mantendo o
                // combate vivo e dando aberturas claras pro jogador punir.
                tempoReacaoMin      = 0.32f;
                tempoReacaoMax      = 0.55f;
                chanceAcaoImprecisa = 0.38f;
                multCooldownAcao    = 1.30f;
                multDefesa          = 0.55f;
                usaCombo            = false;
                usaFinta            = false;
                usaEvasao           = true;  // tenta bloquear, mas com atraso e imprecisão
                capacidadeLeitura   = 0.12f;
                maxHitsCombo        = 1;     // não encadeia
                break;

            case DificuldadeIA.Medio:
                tempoReacaoMin      = 0.18f;
                tempoReacaoMax      = 0.32f;
                chanceAcaoImprecisa = 0.16f;
                multCooldownAcao    = 1.05f;
                multDefesa          = 0.85f;
                usaCombo            = true;
                usaFinta            = true;
                usaEvasao           = true;
                capacidadeLeitura   = 0.55f;
                maxHitsCombo        = 2;
                break;

            case DificuldadeIA.Dificil:
                // Reação quase imediata, erro raro, usa tudo: combo, finta, esquiva,
                // leitura de padrão, punição de abertura e gestão de energia.
                tempoReacaoMin      = 0.09f;
                tempoReacaoMax      = 0.17f;
                chanceAcaoImprecisa = 0.03f;
                multCooldownAcao    = 0.85f;
                multDefesa          = 1.15f;
                usaCombo            = true;
                usaFinta            = true;
                usaEvasao           = true;
                capacidadeLeitura   = 1.00f;
                maxHitsCombo        = 3;     // combo mais longo é privilégio do Difícil
                break;
        }

        // Ritmo das ações — único ajuste multiplicativo que sobrou, e é suave
        cooldownAtaque   *= multCooldownAcao;
        cooldownEspecial *= multCooldownAcao;
        cooldownUltimate *= multCooldownAcao;
    }

    // ── Ativação ──────────────────────────────────────────────────────────
    void DefinirAtivacaoInicial()
    {
        if (lutador == null) { iaAtiva = false; return; }

        if (!ativarAutomaticamentePeloModo)
        {
            iaAtiva = true;
            lutador.controladoPorIA = true;
            return;
        }

        string modoJogo = PlayerPrefs.GetString("ModoJogo", "PVP");

        if      (modoJogo == "PVC") iaAtiva = !lutador.jogador1;
        else if (modoJogo == "CVC") iaAtiva = true;
        else                         iaAtiva = false;

        lutador.controladoPorIA = iaAtiva;
    }
}
