using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class TelaSelecaoArena : MonoBehaviour
{
    [Header("Botões das arenas")]
    public Button btnArena1;
    public Button btnArena2;
    public Button btnArenaAleatoria;

    [Header("Destaques das arenas")]
    public Image destaqueArena1;
    public Image destaqueArena2;
    public Image destaqueArenaAleatoria;

    [Header("Textos opcionais")]
    [Tooltip("Label fixo ROUNDS — tem LocalizedText, script só controla SetActive")]
    public TextMeshProUGUI textoRounds;
    [Tooltip("Valor dinâmico '1 Round' — tem LocalizedText chave ROUNDS_1")]
    public TextMeshProUGUI textoRounds1Valor;
    [Tooltip("Valor dinâmico 'Melhor de 3' — tem LocalizedText chave ROUNDS_3")]
    public TextMeshProUGUI textoRounds3Valor;
    [Tooltip("Valor dinâmico 'Melhor de 5' — tem LocalizedText chave ROUNDS_5")]
    public TextMeshProUGUI textoRounds5Valor;
    [Tooltip("Label fixo TIME — tem LocalizedText, script só controla SetActive")]
    public TextMeshProUGUI textoTempo;
    [Tooltip("Valor dinâmico tempo numérico (30s/60s/90s) — script escreve o número")]
    public TextMeshProUGUI textoTempoNumero;
    [Tooltip("Valor dinâmico INFINITO — tem LocalizedText (TIME_INFINITE), script só controla SetActive")]
    public TextMeshProUGUI textoTempoInfinito;
    public TextMeshProUGUI textoAviso;

    [Header("Botões de rounds")]
    public Button btnRound1;
    public Button btnRound3;
    public Button btnRound5;

    [Header("Botões de tempo")]
    public Button btnTempo30;
    public Button btnTempo60;
    public Button btnTempo90;
    public Button btnTempoInfinito;

    [Header("Botões principais")]
    public Button botaoIniciar;
    public Button botaoVoltar;

    [Header("Padrões")]
    public int roundPadrao = 3;
    public int tempoPadrao = 60;

    private int arenaSelecionada = -1;
    private int roundsSelecionados = -1;
    private int tempoSelecionado = -2; // -2 = não escolhido | -1 = infinito
    private bool sorteando = false;

    // Grupo 0 = Voltar (sozinho, topo)
    // Grupo 1 = arenas — Arena1 / Aleatório / Arena2, na mesma linha
    // Grupo 2 = rounds + tempo linha 1 (Round1..Round5 ←→ 30s..60s)
    // Grupo 3 = tempo linha 2 (90s ←→ ∞)
    // Grupo 4 = START
    private int grupoAtual = 1;
    private int indiceArena = 0;            // 0 arena1 | 1 aleatório | 2 arena2
    private int indiceLinhaRoundsTime = 1;  // 0=1r | 1=3r | 2=5r | 3=30s | 4=60s
    private int indiceLinha2 = 0;           // 0=90s | 1=∞
    private int indiceRounds = 1;           // mantido para compatibilidade
    private int indiceTempo = 1;            // mantido para compatibilidade
    private int indiceLinhaInferior = 1;    // mantido para compatibilidade

    // P1
    private KeyCode p1Esquerda;
    private KeyCode p1Direita;
    private KeyCode p1Cima;
    private KeyCode p1Baixo;
    private KeyCode p1Confirmar;
    private KeyCode p1Voltar;

    // P2
    private KeyCode p2Esquerda;
    private KeyCode p2Direita;
    private KeyCode p2Cima;
    private KeyCode p2Baixo;
    private KeyCode p2Confirmar;
    private KeyCode p2Voltar;

    private readonly Color corDestaqueVisivel = new Color(1f, 1f, 1f, 1f);
    private readonly Color corDestaqueHover = new Color(1f, 1f, 1f, 0.5f);
    private readonly Color corDestaqueInvisivel = new Color(1f, 1f, 1f, 0f);

    private readonly Color corHover = new Color(1f, 0.95f, 0.65f, 1f);
    private readonly Color corSelecionado = new Color(1f, 0.85f, 0.2f, 1f);

    private Color corOriginalRound1;
    private Color corOriginalRound3;
    private Color corOriginalRound5;

    private Color corOriginalTempo30;
    private Color corOriginalTempo60;
    private Color corOriginalTempo90;
    private Color corOriginalTempoInfinito;

    private Color corNormalVoltar;
    private Color corNormalIniciar;

    void Start()
    {
        CarregarTeclas();
        GuardarCoresOriginais();

        ResetarDestaquesArena();

        // Textos dinâmicos começam ocultos — labels fixos (LocalizedText) ficam visíveis
        if (textoRounds1Valor != null)  textoRounds1Valor.gameObject.SetActive(false);
        if (textoRounds3Valor != null)  textoRounds3Valor.gameObject.SetActive(false);
        if (textoRounds5Valor != null)  textoRounds5Valor.gameObject.SetActive(false);
        if (textoTempoNumero != null)   textoTempoNumero.gameObject.SetActive(false);
        if (textoTempoInfinito != null) textoTempoInfinito.gameObject.SetActive(false);

        // #21 — textos via LanguageManager, com fallback
        AtualizarTextosIdioma();

        if (textoAviso != null)
            textoAviso.text = "";

        ConfigurarBotoes();
        ConfigurarHover();
        AtualizarFocoVisualTeclado();

        // #21 — atualiza textos quando idioma mudar
        LanguageManager.OnLanguageChanged += AtualizarTextosIdioma;
    }

    void OnDestroy()
    {
        LanguageManager.OnLanguageChanged -= AtualizarTextosIdioma;
    }

    void AtualizarTextosIdioma()
    {
        // Força todos os LocalizedText da cena a re-renderizar com o idioma atual
        foreach (var lt in FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))
            lt.SendMessage("UpdateText", SendMessageOptions.DontRequireReceiver);

        // Atualiza os textos dinâmicos controlados pelo script
        AtualizarTextoRounds();
        AtualizarTextoTempo();
    }

    void Update()
    {
        TratarTeclado();
    }

    void CarregarTeclas()
    {
        p1Esquerda  = StringParaKeyCode(PlayerPrefs.GetString("P1_Esquerda", "A"), KeyCode.A);
        p1Direita   = StringParaKeyCode(PlayerPrefs.GetString("P1_Direita", "D"), KeyCode.D);
        p1Cima      = StringParaKeyCode(PlayerPrefs.GetString("P1_Pular", "W"), KeyCode.W);
        p1Baixo     = StringParaKeyCode(PlayerPrefs.GetString("P1_Defender", "S"), KeyCode.S);
        p1Confirmar = StringParaKeyCode(PlayerPrefs.GetString("P1_Ataque", "F"), KeyCode.F);
        p1Voltar    = StringParaKeyCode(PlayerPrefs.GetString("P1_Especial", "G"), KeyCode.G);

        p2Esquerda  = StringParaKeyCode(PlayerPrefs.GetString("P2_Esquerda", "LeftArrow"), KeyCode.LeftArrow);
        p2Direita   = StringParaKeyCode(PlayerPrefs.GetString("P2_Direita", "RightArrow"), KeyCode.RightArrow);
        p2Cima      = StringParaKeyCode(PlayerPrefs.GetString("P2_Pular", "UpArrow"), KeyCode.UpArrow);
        p2Baixo     = StringParaKeyCode(PlayerPrefs.GetString("P2_Defender", "DownArrow"), KeyCode.DownArrow);
        p2Confirmar = StringParaKeyCode(PlayerPrefs.GetString("P2_Ataque", "K"), KeyCode.K);
        p2Voltar    = StringParaKeyCode(PlayerPrefs.GetString("P2_Especial", "L"), KeyCode.L);
    }

    KeyCode StringParaKeyCode(string valor, KeyCode padrao)
    {
        try { return (KeyCode)System.Enum.Parse(typeof(KeyCode), valor); }
        catch { return padrao; }
    }

    void GuardarCoresOriginais()
    {
        corOriginalRound1 = PegarCorBotao(btnRound1);
        corOriginalRound3 = PegarCorBotao(btnRound3);
        corOriginalRound5 = PegarCorBotao(btnRound5);

        corOriginalTempo30 = PegarCorBotao(btnTempo30);
        corOriginalTempo60 = PegarCorBotao(btnTempo60);
        corOriginalTempo90 = PegarCorBotao(btnTempo90);
        corOriginalTempoInfinito = PegarCorBotao(btnTempoInfinito);

        corNormalVoltar = PegarCorBotao(botaoVoltar);
        corNormalIniciar = PegarCorBotao(botaoIniciar);

        // Salva sprites normais de todos os botões para restaurar após hover/seleção
        GuardarSpritesNormais();
    }

    void GuardarSpritesNormais()
    {
        GuardarSpriteNormal(btnRound1);
        GuardarSpriteNormal(btnRound3);
        GuardarSpriteNormal(btnRound5);
        GuardarSpriteNormal(btnTempo30);
        GuardarSpriteNormal(btnTempo60);
        GuardarSpriteNormal(btnTempo90);
        GuardarSpriteNormal(btnTempoInfinito);
        GuardarSpriteNormal(botaoVoltar);
        GuardarSpriteNormal(botaoIniciar);
        GuardarSpriteNormal(btnArenaAleatoria);
    }

    void GuardarSpriteNormal(Button botao)
    {
        if (botao == null) return;
        Image img = botao.GetComponent<Image>();
        if (img != null && !_spritesNormais.ContainsKey(botao))
            _spritesNormais[botao] = img.sprite;
    }

    Color PegarCorBotao(Button botao)
    {
        if (botao == null) return Color.white;
        Image img = botao.GetComponent<Image>();
        if (img == null) return Color.white;
        return img.color;
    }

    void ConfigurarBotoes()
    {
        if (btnArena1 != null)
        {
            btnArena1.onClick.RemoveAllListeners();
            btnArena1.onClick.AddListener(() =>
            {
                grupoAtual = 1;
                indiceArena = 0;
                EscolherArena1();
            });
        }

        if (btnArena2 != null)
        {
            btnArena2.onClick.RemoveAllListeners();
            btnArena2.onClick.AddListener(() =>
            {
                grupoAtual = 1;
                indiceArena = 2;
                EscolherArena2();
            });
        }

        if (btnArenaAleatoria != null)
        {
            btnArenaAleatoria.onClick.RemoveAllListeners();
            btnArenaAleatoria.onClick.AddListener(() =>
            {
                grupoAtual = 1;
                indiceArena = 1;
                ArenaAleatoria();
            });
        }

        if (btnRound1 != null)
        {
            btnRound1.onClick.RemoveAllListeners();
            btnRound1.onClick.AddListener(() =>
            {
                indiceRounds = 0;
                indiceLinhaInferior = 0;
                EscolherRound1();
            });
        }

        if (btnRound3 != null)
        {
            btnRound3.onClick.RemoveAllListeners();
            btnRound3.onClick.AddListener(() =>
            {
                indiceRounds = 1;
                indiceLinhaInferior = 1;
                EscolherRound3();
            });
        }

        if (btnRound5 != null)
        {
            btnRound5.onClick.RemoveAllListeners();
            btnRound5.onClick.AddListener(() =>
            {
                indiceRounds = 2;
                indiceLinhaInferior = 2;
                EscolherRound5();
            });
        }

        if (btnTempo30 != null)
        {
            btnTempo30.onClick.RemoveAllListeners();
            btnTempo30.onClick.AddListener(() =>
            {
                indiceTempo = 0;
                indiceLinhaInferior = 3;
                EscolherTempo30();
            });
        }

        if (btnTempo60 != null)
        {
            btnTempo60.onClick.RemoveAllListeners();
            btnTempo60.onClick.AddListener(() =>
            {
                indiceTempo = 1;
                indiceLinhaInferior = 4;
                EscolherTempo60();
            });
        }

        if (btnTempo90 != null)
        {
            btnTempo90.onClick.RemoveAllListeners();
            btnTempo90.onClick.AddListener(() =>
            {
                indiceTempo = 2;
                indiceLinhaInferior = 5;
                EscolherTempo90();
            });
        }

        if (btnTempoInfinito != null)
        {
            btnTempoInfinito.onClick.RemoveAllListeners();
            btnTempoInfinito.onClick.AddListener(() =>
            {
                indiceTempo = 3;
                indiceLinhaInferior = 6;
                EscolherTempoInfinito();
            });
        }

        if (botaoIniciar != null)
        {
            botaoIniciar.onClick.RemoveAllListeners();
            botaoIniciar.onClick.AddListener(TentarIniciarLuta);
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveAllListeners();
            botaoVoltar.onClick.AddListener(Voltar);
        }
    }

    void ConfigurarHover()
    {
        ConfigurarHoverArena(btnArena1, 1);
        ConfigurarHoverArena(btnArena2, 2);
        ConfigurarHoverArenaAleatoria();
        ConfigurarHoverVoltar();

        ConfigurarHoverBotao(btnRound1, 1, 0, 2, 0);
        ConfigurarHoverBotao(btnRound3, 1, 1, 2, 1);
        ConfigurarHoverBotao(btnRound5, 1, 2, 2, 2);

        ConfigurarHoverBotao(btnTempo30, 2, 0, 2, 3);
        ConfigurarHoverBotao(btnTempo60, 2, 1, 2, 4);
        ConfigurarHoverBotao(btnTempo90, 2, 2, 3, 0);
        ConfigurarHoverBotao(btnTempoInfinito, 2, 3, 3, 1);

        ConfigurarHoverComecar();
    }

    void ConfigurarHoverArenaAleatoria()
    {
        if (btnArenaAleatoria == null) return;

        EventoHoverUI hover = btnArenaAleatoria.GetComponent<EventoHoverUI>();
        if (hover == null) hover = btnArenaAleatoria.gameObject.AddComponent<EventoHoverUI>();

        hover.aoEntrar = () =>
        {
            if (sorteando) return;
            grupoAtual = 1;
            indiceArena = 1;
            AtualizarFocoVisualTeclado();
        };

        hover.aoSair = () => { };
    }

    void ConfigurarHoverVoltar()
    {
        if (botaoVoltar == null) return;

        EventoHoverUI hover = botaoVoltar.GetComponent<EventoHoverUI>();
        if (hover == null) hover = botaoVoltar.gameObject.AddComponent<EventoHoverUI>();

        hover.aoEntrar = () =>
        {
            grupoAtual = 0;
            AtualizarFocoVisualTeclado();
        };

        hover.aoSair = () => { };
    }

    void ConfigurarHoverComecar()
    {
        if (botaoIniciar == null) return;

        EventoHoverUI hover = botaoIniciar.GetComponent<EventoHoverUI>();
        if (hover == null) hover = botaoIniciar.gameObject.AddComponent<EventoHoverUI>();

        hover.aoEntrar = () =>
        {
            grupoAtual = 4;
            AtualizarFocoVisualTeclado();
        };

        hover.aoSair = () => { };
    }

    void ConfigurarHoverArena(Button botao, int arena)
    {
        if (botao == null) return;

        EventoHoverUI hover = botao.GetComponent<EventoHoverUI>();
        if (hover == null) hover = botao.gameObject.AddComponent<EventoHoverUI>();

        hover.aoEntrar = () =>
        {
            if (sorteando) return;
            grupoAtual = 1;
            indiceArena = arena == 1 ? 0 : 2;
            AtualizarFocoVisualTeclado();
        };

        hover.aoSair = () => { };
    }

    // navGrupo/navIndice = pra onde a navegação por teclado deve apontar quando o
    // mouse passa por cima deste botão.
    void ConfigurarHoverBotao(Button botao, int grupoSel, int indiceSel, int navGrupo, int navIndice)
    {
        if (botao == null) return;

        EventoHoverUI hover = botao.GetComponent<EventoHoverUI>();
        if (hover == null) hover = botao.gameObject.AddComponent<EventoHoverUI>();

        hover.aoEntrar = () =>
        {
            grupoAtual = navGrupo;
            if (navGrupo == 2)
            {
                indiceLinhaRoundsTime = navIndice;
                SincronizarRoundsTime();
            }
            else if (navGrupo == 3)
            {
                indiceLinha2 = navIndice;
            }
            AtualizarFocoVisualTeclado();
        };

        hover.aoSair = () => { };
    }

    void TratarTeclado()
    {
        if (sorteando) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { Voltar(); return; }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmarSelecaoTeclado();
            return;
        }

        bool cima      = Input.GetKeyDown(p1Cima)      || Input.GetKeyDown(p2Cima);
        bool baixo     = Input.GetKeyDown(p1Baixo)     || Input.GetKeyDown(p2Baixo);
        bool esquerda  = Input.GetKeyDown(p1Esquerda)  || Input.GetKeyDown(p2Esquerda);
        bool direita   = Input.GetKeyDown(p1Direita)   || Input.GetKeyDown(p2Direita);
        bool confirmar = Input.GetKeyDown(p1Confirmar) || Input.GetKeyDown(p2Confirmar);
        bool voltarTecla = Input.GetKeyDown(p1Voltar)  || Input.GetKeyDown(p2Voltar);

        if (voltarTecla) { Voltar(); return; }

        if (cima)
        {
            switch (grupoAtual)
            {
                case 4:
                    grupoAtual = 3;
                    break;

                case 3:
                    grupoAtual = 2;

                    // Mantém a coluna do tempo ao subir da linha 90/∞
                    if (indiceLinha2 == 0) indiceLinhaRoundsTime = 4; // volta em 60s
                    else indiceLinhaRoundsTime = 4; // ∞ também sobe para 60s
                    SincronizarRoundsTime();
                    break;

                case 2:
                    grupoAtual = 1;
                    break;

                case 1:
                    grupoAtual = 0;
                    break;

                case 0:
                    break;
            }

            AtualizarFocoVisualTeclado();
            return;
        }

        if (baixo)
        {
            switch (grupoAtual)
            {
                case 0:
                    grupoAtual = 1;
                    break;

                case 1:
                    grupoAtual = 2;

                    // Ao descer das arenas, cai primeiro nos rounds
                    if (indiceLinhaRoundsTime < 0 || indiceLinhaRoundsTime > 2)
                        indiceLinhaRoundsTime = 1; // Melhor de 3
                    SincronizarRoundsTime();
                    break;

                case 2:
                    // Só desce para a linha de baixo se estiver no lado do tempo
                    if (indiceLinhaRoundsTime >= 3)
                    {
                        grupoAtual = 3;
                        indiceLinha2 = 0; // 90s
                    }
                    else
                    {
                        grupoAtual = 4; // dos rounds vai direto para o Start
                    }
                    break;

                case 3:
                    grupoAtual = 4;
                    break;

                case 4:
                    break;
            }

            AtualizarFocoVisualTeclado();
            return;
        }

        if (esquerda)
        {
            switch (grupoAtual)
            {
                case 1:
                    indiceArena--;
                    if (indiceArena < 0) indiceArena = 2;
                    break;

                case 2:
                    indiceLinhaRoundsTime--;
                    if (indiceLinhaRoundsTime < 0) indiceLinhaRoundsTime = 4;
                    SincronizarRoundsTime();
                    break;

                case 3:
                    indiceLinha2--;
                    if (indiceLinha2 < 0) indiceLinha2 = 1;
                    break;
            }

            AtualizarFocoVisualTeclado();
            return;
        }

        if (direita)
        {
            switch (grupoAtual)
            {
                case 1:
                    indiceArena++;
                    if (indiceArena > 2) indiceArena = 0;
                    break;

                case 2:
                    indiceLinhaRoundsTime++;
                    if (indiceLinhaRoundsTime > 4) indiceLinhaRoundsTime = 0;
                    SincronizarRoundsTime();
                    break;

                case 3:
                    indiceLinha2++;
                    if (indiceLinha2 > 1) indiceLinha2 = 0;
                    break;
            }

            AtualizarFocoVisualTeclado();
            return;
        }

        if (confirmar)
            ConfirmarSelecaoTeclado();
    }

    // Sincroniza indiceRounds e indiceTempo a partir de indiceLinhaRoundsTime
    // Grupo 2: 0=1r | 1=3r | 2=5r | 3=30s | 4=60s
    void SincronizarRoundsTime()
    {
        if (indiceLinhaRoundsTime <= 2)
            indiceRounds = indiceLinhaRoundsTime;
        else
            indiceTempo = indiceLinhaRoundsTime - 3; // 3→0=30s | 4→1=60s
    }

    void ConfirmarSelecaoTeclado()
    {
        switch (grupoAtual)
        {
            case 0:
                Voltar();
                break;
            case 1:
                if (indiceArena == 0) EscolherArena1();
                else if (indiceArena == 1) ArenaAleatoria();
                else EscolherArena2();
                break;
            case 2: // Round1..Round5 | 30s | 60s
                switch (indiceLinhaRoundsTime)
                {
                    case 0: EscolherRound1();  break;
                    case 1: EscolherRound3();  break;
                    case 2: EscolherRound5();  break;
                    case 3: EscolherTempo30(); break;
                    case 4: EscolherTempo60(); break;
                }
                break;
            case 3: // 90s | ∞
                if (indiceLinha2 == 0) EscolherTempo90();
                else EscolherTempoInfinito();
                break;
            case 4:
                TentarIniciarLuta();
                break;
        }
    }

    void AtualizarFocoVisualTeclado()
    {
        // Grupo 0 — Voltar (sozinho)
        if (botaoVoltar != null)
        {
            bool focado = grupoAtual == 0;
            if (botaoVoltar.transition == Selectable.Transition.SpriteSwap)
            {
                Image img = botaoVoltar.GetComponent<Image>();
                if (img != null)
                {
                    if (focado && botaoVoltar.spriteState.highlightedSprite != null)
                        img.sprite = botaoVoltar.spriteState.highlightedSprite;
                    else
                    {
                        Sprite normal = ObterSpriteNormal(botaoVoltar);
                        if (normal != null) img.sprite = normal;
                    }
                }
            }
            else
            {
                Image img = botaoVoltar.GetComponent<Image>();
                if (img != null) img.color = focado ? corHover : corNormalVoltar;
            }
        }

        // Grupo 1 — Arena1 / Aleatório / Arena2, na mesma linha
        SetDestaqueAleatorio(grupoAtual == 1 && indiceArena == 1 ? corDestaqueHover : corDestaqueInvisivel);
        if (grupoAtual == 1 && indiceArena == 0 && arenaSelecionada != 1) SetDestaqueArena1(corDestaqueHover);
        else RestaurarArena1();
        if (grupoAtual == 1 && indiceArena == 2 && arenaSelecionada != 2) SetDestaqueArena2(corDestaqueHover);
        else RestaurarArena2();

        // Grupo 2 — Round1..Round5 | 30s | 60s
        DefinirCorFocoOuSelecionado(btnRound1,  grupoAtual == 2 && indiceLinhaRoundsTime == 0, roundsSelecionados == 1,  corOriginalRound1);
        DefinirCorFocoOuSelecionado(btnRound3,  grupoAtual == 2 && indiceLinhaRoundsTime == 1, roundsSelecionados == 3,  corOriginalRound3);
        DefinirCorFocoOuSelecionado(btnRound5,  grupoAtual == 2 && indiceLinhaRoundsTime == 2, roundsSelecionados == 5,  corOriginalRound5);
        DefinirCorFocoOuSelecionado(btnTempo30, grupoAtual == 2 && indiceLinhaRoundsTime == 3, tempoSelecionado == 30,   corOriginalTempo30);
        DefinirCorFocoOuSelecionado(btnTempo60, grupoAtual == 2 && indiceLinhaRoundsTime == 4, tempoSelecionado == 60,   corOriginalTempo60);

        // Grupo 3 — 90s | ∞
        DefinirCorFocoOuSelecionado(btnTempo90,       grupoAtual == 3 && indiceLinha2 == 0, tempoSelecionado == 90, corOriginalTempo90);
        DefinirCorFocoOuSelecionado(btnTempoInfinito, grupoAtual == 3 && indiceLinha2 == 1, tempoSelecionado == -1, corOriginalTempoInfinito);

        // Grupo 4 — START
        if (botaoIniciar != null)
        {
            bool focado = grupoAtual == 4;
            if (botaoIniciar.transition == Selectable.Transition.SpriteSwap)
            {
                Image img = botaoIniciar.GetComponent<Image>();
                if (img != null)
                {
                    if (focado && botaoIniciar.spriteState.highlightedSprite != null)
                        img.sprite = botaoIniciar.spriteState.highlightedSprite;
                    else
                    {
                        Sprite normal = ObterSpriteNormal(botaoIniciar);
                        if (normal != null) img.sprite = normal;
                    }
                }
            }
            else
            {
                Image img = botaoIniciar.GetComponent<Image>();
                if (img != null) img.color = focado ? corHover : corNormalIniciar;
            }
        }
    }

    void DefinirCorFocoOuSelecionado(Button botao, bool temFoco, bool estaSelecionado, Color corOriginal)
    {
        if (estaSelecionado)
            DefinirCorBotao(botao, corSelecionado);
        else if (temFoco)
            DefinirCorBotao(botao, corHover);
        else
            DefinirCorBotao(botao, corOriginal);
    }

    public void EscolherArena1()
    {
        arenaSelecionada = 1;
        LimparAviso();
        AtualizarDestaquesArena();
        AtualizarFocoVisualTeclado();
    }

    public void EscolherArena2()
    {
        arenaSelecionada = 2;
        LimparAviso();
        AtualizarDestaquesArena();
        AtualizarFocoVisualTeclado();
    }

    public void ArenaAleatoria()
    {
        if (!sorteando)
            StartCoroutine(SorteioAnimado());
    }

    IEnumerator SorteioAnimado()
    {
        sorteando = true;
        float tempo = 0.1f;

        for (int i = 0; i < 10; i++)
        {
            int arenaTemp = Random.Range(1, 3);
            MostrarDestaqueArena(arenaTemp);
            yield return new WaitForSeconds(tempo);
            tempo += 0.05f;
        }

        arenaSelecionada = Random.Range(1, 3);
        indiceArena = arenaSelecionada == 1 ? 0 : 2;
        grupoAtual = 1;

        AtualizarDestaquesArena();
        sorteando = false;
        LimparAviso();
        AtualizarFocoVisualTeclado();
    }

    void AtualizarDestaquesArena()
    {
        MostrarDestaqueArena(arenaSelecionada);
    }

    void MostrarDestaqueArena(int arena)
    {
        SetDestaqueArena1(arena == 1 ? corDestaqueVisivel : corDestaqueInvisivel);
        SetDestaqueArena2(arena == 2 ? corDestaqueVisivel : corDestaqueInvisivel);
    }

    void SetDestaqueArena1(Color cor)
    {
        if (destaqueArena1 != null)
            destaqueArena1.color = cor;
    }

    void SetDestaqueArena2(Color cor)
    {
        if (destaqueArena2 != null)
            destaqueArena2.color = cor;
    }

    void SetDestaqueAleatorio(Color cor)
    {
        if (destaqueArenaAleatoria != null)
            destaqueArenaAleatoria.color = cor;
    }

    void RestaurarArena1()
    {
        if (arenaSelecionada == 1) SetDestaqueArena1(corDestaqueVisivel);
        else SetDestaqueArena1(corDestaqueInvisivel);
    }

    void RestaurarArena2()
    {
        if (arenaSelecionada == 2) SetDestaqueArena2(corDestaqueVisivel);
        else SetDestaqueArena2(corDestaqueInvisivel);
    }

    void RestaurarAleatorio()
    {
        SetDestaqueAleatorio(corDestaqueInvisivel);
    }

    void ResetarDestaquesArena()
    {
        SetDestaqueArena1(corDestaqueInvisivel);
        SetDestaqueArena2(corDestaqueInvisivel);
        SetDestaqueAleatorio(corDestaqueInvisivel);
    }

    public void EscolherRound1()
    {
        roundsSelecionados = 1;
        AtualizarTextoRounds();
        AtualizarVisualRoundsComFoco();
        LimparAviso();
    }

    public void EscolherRound3()
    {
        roundsSelecionados = 3;
        AtualizarTextoRounds();
        AtualizarVisualRoundsComFoco();
        LimparAviso();
    }

    public void EscolherRound5()
    {
        roundsSelecionados = 5;
        AtualizarTextoRounds();
        AtualizarVisualRoundsComFoco();
        LimparAviso();
    }

    void AtualizarTextoRounds()
    {
        // Esconde todos os dinâmicos
        if (textoRounds1Valor != null) textoRounds1Valor.gameObject.SetActive(false);
        if (textoRounds3Valor != null) textoRounds3Valor.gameObject.SetActive(false);
        if (textoRounds5Valor != null) textoRounds5Valor.gameObject.SetActive(false);

        if (roundsSelecionados == -1)
        {
            // Sem seleção: mostra label fixo
            if (textoRounds != null) textoRounds.gameObject.SetActive(true);
            return;
        }

        // Com seleção: esconde label fixo, ativa o texto certo
        if (textoRounds != null) textoRounds.gameObject.SetActive(false);

        if (roundsSelecionados == 1 && textoRounds1Valor != null)
            textoRounds1Valor.gameObject.SetActive(true);
        else if (roundsSelecionados == 3 && textoRounds3Valor != null)
            textoRounds3Valor.gameObject.SetActive(true);
        else if (roundsSelecionados == 5 && textoRounds5Valor != null)
            textoRounds5Valor.gameObject.SetActive(true);
    }

    void AtualizarVisualRounds()
    {
        DefinirCorBotao(btnRound1, corOriginalRound1);
        DefinirCorBotao(btnRound3, corOriginalRound3);
        DefinirCorBotao(btnRound5, corOriginalRound5);

        if (roundsSelecionados == 1) DefinirCorBotao(btnRound1, corSelecionado);
        if (roundsSelecionados == 3) DefinirCorBotao(btnRound3, corSelecionado);
        if (roundsSelecionados == 5) DefinirCorBotao(btnRound5, corSelecionado);
    }

    void AtualizarVisualRoundsComFoco()
    {
        AtualizarVisualRounds();

        if (grupoAtual != 2) return;

        if (indiceLinhaInferior == 0 && roundsSelecionados != 1) DefinirCorBotao(btnRound1, corHover);
        if (indiceLinhaInferior == 1 && roundsSelecionados != 3) DefinirCorBotao(btnRound3, corHover);
        if (indiceLinhaInferior == 2 && roundsSelecionados != 5) DefinirCorBotao(btnRound5, corHover);
    }

    public void EscolherTempo30()
    {
        tempoSelecionado = 30;
        AtualizarTextoTempo();
        AtualizarVisualTempoComFoco();
        LimparAviso();
    }

    public void EscolherTempo60()
    {
        tempoSelecionado = 60;
        AtualizarTextoTempo();
        AtualizarVisualTempoComFoco();
        LimparAviso();
    }

    public void EscolherTempo90()
    {
        tempoSelecionado = 90;
        AtualizarTextoTempo();
        AtualizarVisualTempoComFoco();
        LimparAviso();
    }

    public void EscolherTempoInfinito()
    {
        tempoSelecionado = -1;
        AtualizarTextoTempo();
        AtualizarVisualTempoComFoco();
        LimparAviso();
    }

    void AtualizarTextoTempo()
    {
        // Esconde todos primeiro
        if (textoTempo != null)         textoTempo.gameObject.SetActive(true);
        if (textoTempoNumero != null)    textoTempoNumero.gameObject.SetActive(false);
        if (textoTempoInfinito != null)  textoTempoInfinito.gameObject.SetActive(false);

        if (tempoSelecionado == -2) return; // nada selecionado — label fixo visível

        // Com seleção: esconde label fixo
        if (textoTempo != null) textoTempo.gameObject.SetActive(false);

        if (tempoSelecionado == -1)
        {
            // Infinito — LocalizedText cuida da tradução
            if (textoTempoInfinito != null) textoTempoInfinito.gameObject.SetActive(true);
        }
        else
        {
            // Número — script escreve direto (30, 60, 90 são iguais em qualquer idioma)
            if (textoTempoNumero != null)
            {
                textoTempoNumero.text = tempoSelecionado + "s";
                textoTempoNumero.gameObject.SetActive(true);
            }
        }
    }

    void AtualizarVisualTempo()
    {
        DefinirCorBotao(btnTempo30, corOriginalTempo30);
        DefinirCorBotao(btnTempo60, corOriginalTempo60);
        DefinirCorBotao(btnTempo90, corOriginalTempo90);
        DefinirCorBotao(btnTempoInfinito, corOriginalTempoInfinito);

        if (tempoSelecionado == 30) DefinirCorBotao(btnTempo30, corSelecionado);
        if (tempoSelecionado == 60) DefinirCorBotao(btnTempo60, corSelecionado);
        if (tempoSelecionado == 90) DefinirCorBotao(btnTempo90, corSelecionado);
        if (tempoSelecionado == -1) DefinirCorBotao(btnTempoInfinito, corSelecionado);
    }

    void AtualizarVisualTempoComFoco()
    {
        AtualizarVisualTempo();

        if (grupoAtual != 2) return;

        if (indiceLinhaInferior == 3 && tempoSelecionado != 30) DefinirCorBotao(btnTempo30, corHover);
        if (indiceLinhaInferior == 4 && tempoSelecionado != 60) DefinirCorBotao(btnTempo60, corHover);
        if (indiceLinhaInferior == 5 && tempoSelecionado != 90) DefinirCorBotao(btnTempo90, corHover);
        if (indiceLinhaInferior == 6 && tempoSelecionado != -1) DefinirCorBotao(btnTempoInfinito, corHover);
    }

    // Aplica o sprite correto conforme o estado do botão
    // Usa o Sprite Swap configurado no Inspector (Selected/Normal)
    void DefinirCorBotao(Button botao, Color cor)
    {
        if (botao == null) return;
        Image img = botao.GetComponent<Image>();
        if (img == null) return;

        // Usa Sprite Swap se configurado, senão aplica cor como fallback
        if (botao.transition == Selectable.Transition.SpriteSwap)
        {
            SpriteState ss = botao.spriteState;
            bool ehHover      = cor == corHover;
            bool ehSelecionado = cor == corSelecionado;

            if (ehSelecionado && ss.selectedSprite != null)
                img.sprite = ss.selectedSprite;
            else if (ehHover && ss.highlightedSprite != null)
                img.sprite = ss.highlightedSprite;
            else
            {
                // Restaura sprite normal (o que estava antes do Sprite Swap)
                // O sprite normal é o que está definido no campo "Normal" do botão
                // que é o próprio Source Image da Image
                Sprite normal = ObterSpriteNormal(botao);
                if (normal != null) img.sprite = normal;
            }
        }
        else
        {
            // Fallback: tinta por cor (para botões sem Sprite Swap configurado)
            img.color = cor;
        }
    }

    // Cache dos sprites normais para restaurar após hover/seleção
    private System.Collections.Generic.Dictionary<Button, Sprite> _spritesNormais
        = new System.Collections.Generic.Dictionary<Button, Sprite>();

    Sprite ObterSpriteNormal(Button botao)
    {
        if (_spritesNormais.TryGetValue(botao, out Sprite s)) return s;
        return null;
    }

    bool EstaSelecionado(int grupo, int indice)
    {
        if (grupo == 1)
        {
            if (indice == 0) return roundsSelecionados == 1;
            if (indice == 1) return roundsSelecionados == 3;
            if (indice == 2) return roundsSelecionados == 5;
        }

        if (grupo == 2)
        {
            if (indice == 0) return tempoSelecionado == 30;
            if (indice == 1) return tempoSelecionado == 60;
            if (indice == 2) return tempoSelecionado == 90;
            if (indice == 3) return tempoSelecionado == -1;
        }

        return false;
    }

    void RestaurarCorBotao(Button botao, int grupo, int indice)
    {
        if (botao == null) return;

        if (EstaSelecionado(grupo, indice))
        {
            DefinirCorBotao(botao, corSelecionado);
            return;
        }

        if (grupo == 1)
        {
            if (indice == 0) DefinirCorBotao(botao, corOriginalRound1);
            if (indice == 1) DefinirCorBotao(botao, corOriginalRound3);
            if (indice == 2) DefinirCorBotao(botao, corOriginalRound5);
        }
        else if (grupo == 2)
        {
            if (indice == 0) DefinirCorBotao(botao, corOriginalTempo30);
            if (indice == 1) DefinirCorBotao(botao, corOriginalTempo60);
            if (indice == 2) DefinirCorBotao(botao, corOriginalTempo90);
            if (indice == 3) DefinirCorBotao(botao, corOriginalTempoInfinito);
        }
    }

    public void TentarIniciarLuta()
    {
        if (arenaSelecionada == -1)
        {
            MostrarAvisoArenaObrigatoria();
            return;
        }

        if (roundsSelecionados == -1)
            roundsSelecionados = roundPadrao;

        if (tempoSelecionado == -2)
            tempoSelecionado = tempoPadrao;

        IniciarLuta();
    }

    void MostrarAvisoArenaObrigatoria()
    {
        if (textoAviso != null)
            textoAviso.text = "Selecione uma arena";

        StartCoroutine(LimparAvisoDepois(2f));
    }

    void LimparAviso()
    {
        if (textoAviso != null)
            textoAviso.text = "";
    }

    IEnumerator LimparAvisoDepois(float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (textoAviso != null)
            textoAviso.text = "";
    }

    public void IniciarLuta()
    {
        PlayerPrefs.SetInt("ArenaSelecionada", arenaSelecionada);
        PlayerPrefs.SetInt("RoundsSelecionados", roundsSelecionados);
        PlayerPrefs.SetInt("TempoSelecionado", tempoSelecionado);
        PlayerPrefs.Save();

        if (arenaSelecionada == 1) SceneManager.LoadScene("cena1");
        if (arenaSelecionada == 2) SceneManager.LoadScene("cena2");
    }

    public void Voltar()
    {
        SceneManager.LoadScene("SelecaoPlayer");
    }
}