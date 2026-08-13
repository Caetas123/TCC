using UnityEngine;

/// <summary>
/// IA de luta com máquina de estados comportamental completa.
/// Lê perfil (Agressivo/Equilibrado/Defensivo) e dificuldade (Fácil/Médio/Difícil)
/// do PlayerPrefs salvos na tela de seleção — com padrão Equilibrado + Fácil.
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

    // ── Parâmetros derivados do perfil ────────────────────────────────────
    private float distanciaEngajamento;   // começa a pressionar
    private float distanciaAtaque;        // alcance de ataque normal
    private float distanciaSegura;        // mantém ao recuar

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
    private float multiplicadorPrecisao;  // 0..1, afeta chance de errar
    private float tempoReacaoMin;
    private float tempoReacaoMax;
    private float chanceErrarAtaque;      // 0 = nunca erra, 1 = sempre erra
    private float chanceIgnorarDecisao;   // simula lentidão de reação
    private bool  usaCombo;
    private bool  usaFinta;
    private bool  usaEvasao;              // esquiva de ataques visíveis
    private float capacidadeLeitura;      // 0..1 — o quanto a IA usa leitura tática (padrão do jogador,
                                           // punição de abertura, gestão de energia). Escala com a dificuldade.

    // ── Estado interno de tempo ───────────────────────────────────────────
    private float cronometroDecisao;
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

    // ── Memória de combate ─────────────────────────────────────────────────
    private int   danoRecebidoConsecutivo = 0;   // quantas vezes levou dano seguido
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
    // (aproveita a recuperação em vez de tentar acertar durante o golpe dele)
    private bool  oponenteEstavaAtacando = false;
    private float janelaPunicao = 0f;

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

        if (oponente != null)
            vidaAnteriorOponente = oponente.vidaAtual;
    }

    void Update()
    {
        if (!iaAtiva || lutador == null || oponente == null) return;
        if (Time.timeScale == 0f) return;
        if (lutador.EstaMorto())  { lutador.IA_LimparComandos(); return; }
        if (oponente.EstaMorto()) { lutador.IA_LimparComandos(); return; }

        // Desconta cooldowns
        cronometroDecisao          -= Time.deltaTime;
        cronometroDefesa           -= Time.deltaTime;
        cronometroAtaqueCooldown   -= Time.deltaTime;
        cronometroEspecialCooldown -= Time.deltaTime;
        cronometroUltimateCooldown -= Time.deltaTime;
        cronometroPuloCooldown     -= Time.deltaTime;
        cronometroFintaCooldown    -= Time.deltaTime;
        cronometroEstado           -= Time.deltaTime;
        cronometroCombo            -= Time.deltaTime;

        // Atualiza memória
        AtualizarMemoria();

        // Reação imediata a ataques do oponente (independente do ciclo de decisão)
        if (usaEvasao) TentarEsquivar();

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

        // Ciclo de decisão principal
        if (cronometroDecisao <= 0f)
        {
            if (Random.value < chanceIgnorarDecisao)
            {
                ReiniciarCronometroDecisao();
                return;
            }
            AtualizarEstadoComportamental();
            ExecutarEstado();
            ReiniciarCronometroDecisao();
        }
    }

    // ── Memória e contexto ────────────────────────────────────────────────
    void AtualizarMemoria()
    {
        if (oponente == null) return;

        // Detecta se o oponente tomou dano desde o último frame
        if (oponente.vidaAtual < vidaAnteriorOponente)
            tempoUltimoAtaqueBemSucedido = Time.time;

        vidaAnteriorOponente = oponente.vidaAtual;

        // Janela de punição: abre no instante em que o oponente SAI de uma animação
        // de ataque/especial/ultimate (recuperação — momento mais seguro pra revidar)
        bool oponenteAtacandoAgora = oponente.EstaAtacando();
        if (oponenteEstavaAtacando && !oponenteAtacandoAgora)
            janelaPunicao = 0.35f;
        oponenteEstavaAtacando = oponenteAtacandoAgora;
        if (janelaPunicao > 0f) janelaPunicao -= Time.deltaTime;

        // Leitura de padrão: registra a ação toda vez que o oponente entra numa
        // animação nova de ataque/especial/ultimate/pulo, junto com a distância
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

    // Fração das últimas ações do oponente que repetiram a ação anterior (0 = nunca repete, 1 = sempre repete)
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
        if (chanceRepeticao < 0.5f) return; // padrão ainda não é claro o suficiente

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
    }

    // ── Máquina de estados ────────────────────────────────────────────────
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

        // Leitura tática: roda em todo ciclo de decisão, independente do estado
        AvaliarLeituraDePadrao(distancia);

        // Força transição de estado a cada intervalo ou por condição crítica
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
            // Reage ao perfil: agressivo pressiona de volta, defensivo recua
            if (perfilIA == PerfilIA.Agressivo)
                MudarEstado(EstadoComportamento.Pressionar, Random.Range(1.0f, 2.0f));
            else
                MudarEstado(EstadoComportamento.Recuar, Random.Range(0.8f, 1.5f));
            return;
        }

        if (!mudarEstado) return;

        // Decisão baseada em distância + perfil + random
        float r = Random.value;

        if (distancia > distanciaEngajamento + 1f)
        {
            MudarEstado(EstadoComportamento.Aproximar, Random.Range(0.6f, 1.2f));
        }
        else if (distancia <= distanciaAtaque)
        {
            // No alcance — decide entre pressionar, defender, flanquear
            switch (perfilIA)
            {
                case PerfilIA.Agressivo:
                    if (r < 0.65f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.8f, 1.5f));
                    else if (r < 0.80f) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.5f, 1.0f));
                    else MudarEstado(EstadoComportamento.Recuar, Random.Range(0.4f, 0.8f));
                    break;

                case PerfilIA.Defensivo:
                    if (r < 0.40f) MudarEstado(EstadoComportamento.Defender, Random.Range(0.6f, 1.2f));
                    else if (r < 0.65f) MudarEstado(EstadoComportamento.Recuar, Random.Range(0.5f, 1.0f));
                    else if (r < 0.80f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.5f, 1.0f));
                    else MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.8f));
                    break;

                default: // Equilibrado
                    if (r < 0.40f) MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.7f, 1.3f));
                    else if (r < 0.60f) MudarEstado(EstadoComportamento.Defender, Random.Range(0.5f, 1.0f));
                    else if (r < 0.78f) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.8f));
                    else MudarEstado(EstadoComportamento.Recuar, Random.Range(0.4f, 0.8f));
                    break;
            }
        }
        else
        {
            // Na faixa intermediária
            if (r < 0.50f) MudarEstado(EstadoComportamento.Aproximar, Random.Range(0.5f, 1.0f));
            else if (r < 0.70f && usaFinta) MudarEstado(EstadoComportamento.Flanquear, Random.Range(0.4f, 0.7f));
            else MudarEstado(EstadoComportamento.Pressionar, Random.Range(0.6f, 1.2f));
        }
    }

    void MudarEstado(EstadoComportamento novo, float duracao)
    {
        estadoAtual      = novo;
        cronometroEstado = duracao;
    }

    void ExecutarEstado()
    {
        float distancia = lutador.DistanciaDoOponente();
        float dir       = DirecaoParaOponente();
        float recuo     = -dir;

        switch (estadoAtual)
        {
            case EstadoComportamento.Aproximar:
                movimentoAtual = dir;
                TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Pressionar:
                // Mantém pressão — avança ou fica parado conforme distância
                if (distancia > distanciaAtaque)
                    movimentoAtual = dir;
                else if (distancia < distanciaAtaque * 0.6f)
                    movimentoAtual = recuo; // não deixa ficar colado demais
                else
                    movimentoAtual = 0f;

                TentarAtacarSeNoAlcance(distancia);
                TentarEspecialSeVale(distancia);
                TentarUltimateSeVale(distancia);

                // Pulo ofensivo ocasional
                if (usaFinta && lutador.EstaNoChao() && cronometroPuloCooldown <= 0f
                    && Random.value < chancePuloBase * multiplicadorPrecisao)
                {
                    lutador.IA_SolicitarPulo();
                    cronometroPuloCooldown = cooldownPulo;
                }

                // Finta de aproximação
                if (usaFinta && cronometroFintaCooldown <= 0f && Random.value < chanceFintaBase)
                    IniciarFinta(dir, recuo);
                break;

            case EstadoComportamento.Recuar:
                // Só continua recuando até atingir a distância segura; depois disso, para
                // (sem isso a IA recuava sem limite, ignorando o valor de distanciaSegura)
                movimentoAtual = distancia < distanciaSegura ? recuo : 0f;
                // Mas ataca se o oponente entrar no alcance mesmo recuando
                if (distancia <= distanciaAtaque && Random.value < chanceAtaqueBase * 0.6f)
                    TentarAtacarSeNoAlcance(distancia);
                // Defende enquanto recua
                if (Random.value < chanceDefesaBase)
                    cronometroDefesa = Random.Range(0.2f, 0.5f);
                break;

            case EstadoComportamento.Defender:
                movimentoAtual = 0f;
                // Ativa defesa
                if (Random.value < chanceDefesaBase * multiplicadorPrecisao)
                    cronometroDefesa = Random.Range(0.3f, 0.8f);
                // Contra-ataca após defender
                if (distancia <= distanciaAtaque && oponente.EstaAtacando()
                    && Random.value < chanceAtaqueBase * multiplicadorPrecisao)
                    TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Flanquear:
                // Movimenta para lado, muda direção para confundir
                movimentoAtual = (Random.value > 0.5f) ? dir : recuo;
                if (usaFinta && cronometroFintaCooldown <= 0f && Random.value < chanceFintaBase * 1.5f)
                    IniciarFinta(dir, recuo);
                // Aproveita para atacar se flanquear criou oportunidade
                if (distancia <= distanciaAtaque)
                    TentarAtacarSeNoAlcance(distancia);
                break;

            case EstadoComportamento.Finalizar:
                // Modo agressivo total — vai tudo
                movimentoAtual = distancia > distanciaAtaque ? dir : 0f;
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
                // Foge, defende, tenta especial de longe se possível
                movimentoAtual = recuo;
                if (Random.value < chanceDefesaBase * 1.5f)
                    cronometroDefesa = Random.Range(0.4f, 0.9f);
                if (distancia <= distanciaAtaque * 1.2f)
                    TentarEspecialSeVale(distancia); // especial pode ter mais alcance
                break;
        }
    }

    // ── Ações de combate ──────────────────────────────────────────────────
    void TentarAtacarSeNoAlcance(float distancia)
    {
        if (lutador.dadosPersonagem == null) return;
        if (distancia > lutador.dadosPersonagem.alcanceAtaque) return;
        if (cronometroAtaqueCooldown > 0f) return;

        // Abertura: o oponente acabou de sair de um ataque (recuperação) — aproveita
        // sem depender da chance normal, na proporção da capacidade de leitura da dificuldade
        bool aproveitandoAbertura = janelaPunicao > 0f && Random.value < capacidadeLeitura;

        if (!aproveitandoAbertura)
        {
            if (Random.value > chanceAtaqueBase * multiplicadorPrecisao) return;
            if (Random.value < chanceErrarAtaque) return;
        }

        lutador.IA_SolicitarAtaque();
        cronometroAtaqueCooldown = cooldownAtaque;
        if (aproveitandoAbertura) janelaPunicao = 0f; // consome a janela

        // Agenda segundo hit do combo
        if (usaCombo && Random.value < chanceComboBase)
        {
            comboSegundoHitPendente = true;
            cronometroCombo = Random.Range(0.15f, 0.35f);
        }
    }

    void TentarSegundoHitCombo()
    {
        comboSegundoHitPendente = false;
        if (lutador.dadosPersonagem == null) return;

        float distancia = lutador.DistanciaDoOponente();

        // Segundo hit: especial se tiver energia, senão ataque normal
        if (distancia <= lutador.dadosPersonagem.alcanceEspecial
            && cronometroEspecialCooldown <= 0f
            && Random.value < chanceEspecialBase * multiplicadorPrecisao)
        {
            lutador.IA_SolicitarEspecial();
            cronometroEspecialCooldown = cooldownEspecial;
        }
        else if (distancia <= lutador.dadosPersonagem.alcanceAtaque
                 && cronometroAtaqueCooldown <= 0f)
        {
            lutador.IA_SolicitarAtaque();
            cronometroAtaqueCooldown = cooldownAtaque;
        }
    }

    void TentarEspecialSeVale(float distancia)
    {
        if (lutador.dadosPersonagem == null) return;
        if (distancia > lutador.dadosPersonagem.alcanceEspecial) return;
        if (cronometroEspecialCooldown > 0f) return;
        if (DevoGuardarEnergiaParaUltimate()) return;
        if (Random.value > chanceEspecialBase * multiplicadorPrecisao) return;
        if (Random.value < chanceErrarAtaque * 0.5f) return;

        lutador.IA_SolicitarEspecial();
        cronometroEspecialCooldown = cooldownEspecial;
    }

    // Gestão de energia: se o oponente já está perto de poder ser finalizado e a energia
    // própria está "a caminho" do ultimate, vale mais guardar do que gastar no especial agora.
    // Quanto maior a capacidadeLeitura (dificuldade), mais a IA prioriza isso.
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
        // Só usa ultimate com energia quase cheia E oportunidade real
        if (lutador.ObterEnergiaNormalizada() < 0.85f) return;
        if (Random.value > chanceUltimateBase * multiplicadorPrecisao) return;

        lutador.IA_SolicitarUltimate();
        cronometroUltimateCooldown = cooldownUltimate;
    }

    void TentarEsquivar()
    {
        if (oponente == null) return;
        if (!oponente.EstaAtacando()) return;

        float distancia = lutador.DistanciaDoOponente();
        if (distancia > distanciaAtaque * 1.3f) return; // longe demais, não precisa esquivar

        // Decide entre defender ou recuar
        float r = Random.value;
        if (r < chanceDefesaBase * multiplicadorPrecisao)
        {
            cronometroDefesa = Random.Range(0.25f, 0.55f);
        }
        else if (r < chanceRecuoBase * multiplicadorPrecisao)
        {
            movimentoAtual = -DirecaoParaOponente();
        }
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

        // Clamp defensivo — enum tem 3 valores (0, 1, 2)
        estilo      = Mathf.Clamp(estilo,      0, 2);
        dificuldade = Mathf.Clamp(dificuldade, 0, 2);

        perfilIA    = (PerfilIA)estilo;
        dificuldadeIA = (DificuldadeIA)dificuldade;

        Debug.Log($"[IA] {(ehP1 ? "P1" : "P2")} — Perfil: {perfilIA} | Dificuldade: {dificuldadeIA}");
    }

    // ── Aplicar parâmetros do perfil ──────────────────────────────────────
    void AplicarPerfil()
    {
        switch (perfilIA)
        {
            case PerfilIA.Agressivo:
                distanciaEngajamento = 4.0f;
                distanciaAtaque      = 2.2f;
                distanciaSegura      = 2.5f;
                chanceAtaqueBase     = 0.80f;
                chanceEspecialBase   = 0.40f;
                chanceUltimateBase   = 0.60f;
                chanceDefesaBase     = 0.15f;
                chancePuloBase       = 0.10f;
                chanceFintaBase      = 0.08f;
                chanceComboBase      = 0.55f;
                chanceRecuoBase      = 0.12f;
                cooldownAtaque       = 0.38f;
                cooldownEspecial     = 1.1f;
                cooldownUltimate     = 1.8f;
                cooldownPulo         = 1.2f;
                cooldownFinta        = 0.8f;
                break;

            case PerfilIA.Defensivo:
                distanciaEngajamento = 3.5f;
                distanciaAtaque      = 2.2f;
                distanciaSegura      = 3.0f;
                chanceAtaqueBase     = 0.40f;
                chanceEspecialBase   = 0.20f;
                chanceUltimateBase   = 0.50f;
                chanceDefesaBase     = 0.60f;
                chancePuloBase       = 0.04f;
                chanceFintaBase      = 0.25f;
                chanceComboBase      = 0.20f;
                chanceRecuoBase      = 0.60f;
                cooldownAtaque       = 0.70f;
                cooldownEspecial     = 1.6f;
                cooldownUltimate     = 2.2f;
                cooldownPulo         = 2.0f;
                cooldownFinta        = 1.2f;
                break;

            default: // Equilibrado
                distanciaEngajamento = 3.8f;
                distanciaAtaque      = 2.2f;
                distanciaSegura      = 2.8f;
                chanceAtaqueBase     = 0.60f;
                chanceEspecialBase   = 0.28f;
                chanceUltimateBase   = 0.55f;
                chanceDefesaBase     = 0.35f;
                chancePuloBase       = 0.07f;
                chanceFintaBase      = 0.15f;
                chanceComboBase      = 0.38f;
                chanceRecuoBase      = 0.30f;
                cooldownAtaque       = 0.52f;
                cooldownEspecial     = 1.4f;
                cooldownUltimate     = 2.0f;
                cooldownPulo         = 1.6f;
                cooldownFinta        = 1.0f;
                break;
        }
    }

    // ── Aplicar parâmetros de dificuldade ─────────────────────────────────
    void AplicarDificuldade()
    {
        switch (dificuldadeIA)
        {
            case DificuldadeIA.Facil:
                // Reage lento, erra bastante, não combina habilidades
                tempoReacaoMin       = 0.45f;
                tempoReacaoMax       = 0.85f;
                multiplicadorPrecisao = 0.50f;
                chanceErrarAtaque    = 0.45f;
                chanceIgnorarDecisao = 0.35f;
                usaCombo             = false;
                usaFinta             = false;
                usaEvasao            = false;
                capacidadeLeitura    = 0.15f; // quase não lê padrão nem gerencia energia
                // Penaliza cooldowns — ataca com menos frequência
                cooldownAtaque   *= 1.7f;
                cooldownEspecial *= 2.0f;
                cooldownUltimate *= 2.5f;
                // Penaliza chances ofensivas
                chanceAtaqueBase   *= 0.55f;
                chanceEspecialBase *= 0.35f;
                chanceUltimateBase *= 0.25f;
                break;

            case DificuldadeIA.Medio:
                tempoReacaoMin        = 0.20f;
                tempoReacaoMax        = 0.45f;
                multiplicadorPrecisao = 0.75f;
                chanceErrarAtaque     = 0.20f;
                chanceIgnorarDecisao  = 0.12f;
                usaCombo              = true;
                usaFinta              = true;
                usaEvasao             = false;
                capacidadeLeitura     = 0.55f;
                cooldownAtaque   *= 1.2f;
                cooldownEspecial *= 1.3f;
                cooldownUltimate *= 1.4f;
                chanceAtaqueBase   *= 0.82f;
                chanceEspecialBase *= 0.70f;
                chanceUltimateBase *= 0.55f;
                break;

            case DificuldadeIA.Dificil:
                // Reage rápido, quase nunca erra, usa combo + finta + evasão
                tempoReacaoMin        = 0.08f;
                tempoReacaoMax        = 0.20f;
                multiplicadorPrecisao = 1.00f;
                chanceErrarAtaque     = 0.04f;
                chanceIgnorarDecisao  = 0.02f;
                usaCombo              = true;
                usaFinta              = true;
                usaEvasao             = true;
                capacidadeLeitura     = 1.00f; // lê padrão, pune abertura e gerencia energia ao máximo
                // Cooldowns menores — mais agressivo
                cooldownAtaque   *= 0.80f;
                cooldownEspecial *= 0.85f;
                cooldownUltimate *= 0.85f;
                break;
        }
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