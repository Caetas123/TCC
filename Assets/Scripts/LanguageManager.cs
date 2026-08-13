using System;
using System.Collections.Generic;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    public enum Language { Portuguese, English }

    private const string CHAVE_IDIOMA = "IdiomaSelecionado";

    public Language CurrentLanguage = Language.Portuguese;

    public static event Action OnLanguageChanged;

    private Dictionary<string, string> pt;
    private Dictionary<string, string> en;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupDictionaries();
        CarregarIdioma();
    }

    private void CarregarIdioma()
    {
        int salvo = PlayerPrefs.GetInt(CHAVE_IDIOMA, 0);
        CurrentLanguage = salvo == 1 ? Language.English : Language.Portuguese;
    }

    private void SalvarIdioma()
    {
        PlayerPrefs.SetInt(CHAVE_IDIOMA, CurrentLanguage == Language.English ? 1 : 0);
        PlayerPrefs.Save();
    }

    public string GetText(string key)
    {
        var dict = CurrentLanguage == Language.Portuguese ? pt : en;
        return dict.ContainsKey(key) ? dict[key] : key;
    }

    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == Language.Portuguese
            ? Language.English
            : Language.Portuguese;
        SalvarIdioma();
        OnLanguageChanged?.Invoke();
    }

    public void SetLanguage(Language idioma)
    {
        if (CurrentLanguage == idioma) return;
        CurrentLanguage = idioma;
        SalvarIdioma();
        OnLanguageChanged?.Invoke();
    }

    void SetupDictionaries()
    {
        pt = new Dictionary<string, string>()
        {
            // ── Menu principal ────────────────────────────────────────────
            { "MENU_GAME_MODE", "JOGAR" },
            { "MENU_CREDITS",   "CRÉDITOS" },
            { "MENU_EXIT",      "SAIR" },
            { "CONF_OP",        "OPÇÕES" },

            // ── Confirmação de saída ──────────────────────────────────────
            { "EXIT_CONFIRM", "Deseja realmente sair?" },
            { "EXIT_YES",     "SIM" },
            { "EXIT_NO",      "NÃO" },

            // ── Configurações ─────────────────────────────────────────────
            { "SETTINGS_TITLE",    "CONFIGURAÇÕES" },
            { "SETTINGS_AUDIO",    "ÁUDIO" },
            { "SETTINGS_VIDEO",    "VÍDEO" },
            { "SETTINGS_CONTROLS", "CONTROLE" },
            { "SETTINGS_BACK",     "VOLTAR" },

            // ── Áudio ─────────────────────────────────────────────────────
            { "AUDIO_MASTER", "Volume Geral" },
            { "AUDIO_MUSIC",  "Volume Música" },
            { "AUDIO_SFX",    "Volume Efeitos" },
            { "AUDIO_MUTE",   "Sem Áudio" },

            // ── Vídeo ─────────────────────────────────────────────────────
            { "VIDEO_RESOLUTION", "Resolução" },
            { "VIDEO_MODE",       "Modo de Tela" },
            { "VIDEO_APPLY",      "Aplicar" },
            { "VIDEO_RESTORE",    "Restaurar" },
            { "HZ",               "Taxa de Atualização (Hz):" },

            // ── Jogadores ─────────────────────────────────────────────────
            { "PLAYER1",  "Jogador 1" },
            { "PLAYER2",  "Jogador 2" },
            { "PLAYERS",  "JOGADORES" },

            // ── Controles (botões de tecla) ───────────────────────────────
            { "BTN_DIREITA",  "DIREITA" },
            { "BTN_ESQUERDA", "ESQUERDA" },
            { "BTN_PULO",     "PULAR" },
            { "BTN_ATAQUE",   "ATAQUE" },
            { "BTN_ESPECIAL", "ESPECIAL" },
            { "BTN_ULTIMATE", "ULTIMATE" },
            { "BTN_DEFESA",   "DEFESA" },

            // ── Controles (avisos de remapeamento) ────────────────────────
            { "CTRL_CANCELADO",  "Cancelado!" },
            { "CTRL_PROIBIDA",   "Tecla não permitida!" },
            { "CTRL_EM_USO",     "Tecla já está em uso!" },
            { "CTRL_SALVA",      "Tecla salva!" },
            { "CTRL_PRESSIONE",  "Pressione uma tecla" },
            { "CTRL_RESTAURADO", "Controles restaurados!" },

            // ── Modo de jogo ──────────────────────────────────────────────
            { "MODE_TITLE",   "MODO DE JOGO" },
            { "MODE_PVP",     "Jogador vs Jogador" },
            { "MODE_CPU",     "Jogador vs CPU" },
            { "MODE_CPU_CPU", "CPU vs CPU" },

            // ── Seleção de personagem ─────────────────────────────────────
            { "SELECT_PLAYER",    "SELEÇÃO DE JOGADOR" },
            { "START_GAME",       "INICIAR" },
            { "BTN_SALVAR",       "SALVAR" },
            { "BTN_DESELECIONAR", "DESELECIONAR" },
            { "AVISO_DESCP1",     "Tecla Deselecionar: X" },
            { "AVISO_DESCP2",     "Tecla Deselecionar: M" },
            { "RANDOM_CHARACTER", "ALEATÓRIO" },

            // ── Avisos de seleção ─────────────────────────────────────────
            { "AVISO_SELECIONE_AMBOS", "Selecione um personagem para cada jogador antes de iniciar!" },
            { "AVISO_SELECIONE_P1",    "Selecione um personagem para o Jogador 1 antes de iniciar!" },
            { "AVISO_SELECIONE_CPU",   "Selecione o personagem da CPU antes de iniciar!" },
            { "AVISO_SELECIONE_BOTS",  "Selecione personagens para os bots antes de iniciar!" },

            // ── Configuração de IA ────────────────────────────────────────
            { "TXT_DIFICUL",           "DIFICULDADE" },
            { "TXT_ESTILO",            "ESTILO" },
            { "IA_ESTILO",             "Estilo" },
            { "IA_ESTILO_AGRESSIVO",   "AGRESSIVO" },
            { "IA_ESTILO_EQUILIBRADO", "EQUILIBRADO" },
            { "IA_ESTILO_DEFENSIVO",   "DEFENSIVO" },
            { "IA_DIFICIL",            "Dificuldade" },
            { "IA_DIFIC_FACIL",        "FÁCIL" },
            { "IA_DIFIC_MEDIO",        "MÉDIO" },
            { "IA_DIFIC_DIFICIL",      "DIFÍCIL" },

            // ── Arena ─────────────────────────────────────────────────────
            { "ARENA_TITLE",          "ARENA DO JOGO" },
            { "ARENA_1",              "Quadra da Escola" },
            { "ARENA_2",              "Sala de Aula" },
            { "ARENA_SELECIONE_AVISO","Selecione uma arena" },

            // ── Tempo e rounds ────────────────────────────────────────────
            { "TIME_30",       "30 SEGUNDOS" },
            { "TIME_60",       "60 SEGUNDOS" },
            { "TIME_90",       "90 SEGUNDOS" },
            { "TIME_INFINITE", "INFINITO" },
            { "TIME_LABEL",    "TEMPO" },

            { "ROUNDS_LABEL", "ROUNDS" },
            { "ROUNDS_1",     "1 Round" },
            { "ROUNDS_3",     "Melhor de 3" },
            { "ROUNDS_5",     "Melhor de 5" },

            // ── Pausa ─────────────────────────────────────────────────────
            { "PAUSE_TITLE",     "MENU DE JOGO" },
            { "PAUSE_CONT",      "CONTINUAR" },
            { "PAUSE_REBOOT",    "REINICIAR" },
            { "PAUSE_SELECTION", "SELEÇÃO JOGADOR" },
            { "PAUSE_EXIT",      "SAIR" },
            { "PAUSE_WIN",       "VENCEU!" },

            // ── Fim de partida ────────────────────────────────────────────
            { "FIM_EXIT",       "SAIR" },
            { "FIM_REBOOT",     "REINICIAR" },
            { "FIM_EMPATE",     "EMPATE!" },
            { "FIM_VITORIA",    "VITÓRIA!" },

            // ── Round Win ─────────────────────────────────────────────────
            { "ROUND_LABEL",    "Rodada" },
            { "ROUND_WIN",      "ganhou" },
            { "ROUND_VENCEDOR", "VENCEDOR" },
            { "ROUND_DRAW",     "Empate" },

            // ── Créditos ──────────────────────────────────────────────────
            { "CREDITOS_DEV", "DESENVOLVEDORES" },
            { "CREDITOS_DIS", "DESIGNER" },
            { "CREDITOS_CON", "CONCEITOS" },
        };

        en = new Dictionary<string, string>()
        {
            // ── Menu principal ────────────────────────────────────────────
            { "MENU_GAME_MODE", "PLAY" },
            { "MENU_CREDITS",   "CREDITS" },
            { "MENU_EXIT",      "EXIT" },
            { "CONF_OP",        "OPTIONS" },

            // ── Confirmação de saída ──────────────────────────────────────
            { "EXIT_CONFIRM", "Do you really want to exit?" },
            { "EXIT_YES",     "YES" },
            { "EXIT_NO",      "NO" },

            // ── Configurações ─────────────────────────────────────────────
            { "SETTINGS_TITLE",    "SETTINGS" },
            { "SETTINGS_AUDIO",    "AUDIO" },
            { "SETTINGS_VIDEO",    "VIDEO" },
            { "SETTINGS_CONTROLS", "CONTROLS" },
            { "SETTINGS_BACK",     "BACK" },

            // ── Áudio ─────────────────────────────────────────────────────
            { "AUDIO_MASTER", "Master Volume" },
            { "AUDIO_MUSIC",  "Music Volume" },
            { "AUDIO_SFX",    "SFX Volume" },
            { "AUDIO_MUTE",   "Mute" },

            // ── Vídeo ─────────────────────────────────────────────────────
            { "VIDEO_RESOLUTION", "Resolution" },
            { "VIDEO_MODE",       "Screen Mode" },
            { "VIDEO_APPLY",      "Apply" },
            { "VIDEO_RESTORE",    "Restore" },
            { "HZ",               "Refresh Rate (Hz):" },

            // ── Jogadores ─────────────────────────────────────────────────
            { "PLAYER1",  "Player 1" },
            { "PLAYER2",  "Player 2" },
            { "PLAYERS",  "PLAYERS" },

            // ── Controles (botões de tecla) ───────────────────────────────
            { "BTN_DIREITA",  "RIGHT" },
            { "BTN_ESQUERDA", "LEFT" },
            { "BTN_PULO",     "JUMP" },
            { "BTN_ATAQUE",   "ATTACK" },
            { "BTN_ESPECIAL", "SPECIAL" },
            { "BTN_ULTIMATE", "ULTIMATE" },
            { "BTN_DEFESA",   "DEFENSE" },

            // ── Controles (avisos de remapeamento) ────────────────────────
            { "CTRL_CANCELADO",  "Cancelled!" },
            { "CTRL_PROIBIDA",   "Key not allowed!" },
            { "CTRL_EM_USO",     "Key already in use!" },
            { "CTRL_SALVA",      "Key saved!" },
            { "CTRL_PRESSIONE",  "Press a key" },
            { "CTRL_RESTAURADO", "Controls restored!" },

            // ── Modo de jogo ──────────────────────────────────────────────
            { "MODE_TITLE",   "GAME MODE" },
            { "MODE_PVP",     "Player vs Player" },
            { "MODE_CPU",     "Player vs CPU" },
            { "MODE_CPU_CPU", "CPU vs CPU" },

            // ── Seleção de personagem ─────────────────────────────────────
            { "SELECT_PLAYER",    "PLAYER SELECT" },
            { "START_GAME",       "START" },
            { "BTN_SALVAR",       "SAVE" },
            { "BTN_DESELECIONAR", "DESELECT" },
            { "AVISO_DESCP1",     "Deselect Key: X" },
            { "AVISO_DESCP2",     "Deselect Key: M" },
            { "RANDOM_CHARACTER", "RANDOM" },

            // ── Avisos de seleção ─────────────────────────────────────────
            { "AVISO_SELECIONE_AMBOS", "Select a character for each player before starting!" },
            { "AVISO_SELECIONE_P1",    "Select a character for Player 1 before starting!" },
            { "AVISO_SELECIONE_CPU",   "Select the CPU character before starting!" },
            { "AVISO_SELECIONE_BOTS",  "Select characters for the bots before starting!" },

            // ── Configuração de IA ────────────────────────────────────────
            { "TXT_DIFICUL",           "DIFFICULTY" },
            { "TXT_ESTILO",            "STYLE" },
            { "IA_ESTILO",             "Style" },
            { "IA_ESTILO_AGRESSIVO",   "AGGRESSIVE" },
            { "IA_ESTILO_EQUILIBRADO", "BALANCED" },
            { "IA_ESTILO_DEFENSIVO",   "DEFENSIVE" },
            { "IA_DIFICIL",            "Difficulty" },
            { "IA_DIFIC_FACIL",        "EASY" },
            { "IA_DIFIC_MEDIO",        "MEDIUM" },
            { "IA_DIFIC_DIFICIL",      "HARD" },

            // ── Arena ─────────────────────────────────────────────────────
            { "ARENA_TITLE",           "GAME ARENA" },
            { "ARENA_1",               "School Court" },
            { "ARENA_2",               "The Classroom" },
            { "ARENA_SELECIONE_AVISO", "Select an arena" },

            // ── Tempo e rounds ────────────────────────────────────────────
            { "TIME_30",       "30 SECONDS" },
            { "TIME_60",       "60 SECONDS" },
            { "TIME_90",       "90 SECONDS" },
            { "TIME_INFINITE", "INFINITY" },
            { "TIME_LABEL",    "TIME" },

            { "ROUNDS_LABEL", "ROUNDS" },
            { "ROUNDS_1",     "1 Round" },
            { "ROUNDS_3",     "Best of 3" },
            { "ROUNDS_5",     "Best of 5" },

            // ── Pausa ─────────────────────────────────────────────────────
            { "PAUSE_TITLE",     "GAME MENU" },
            { "PAUSE_CONT",      "CONTINUE" },
            { "PAUSE_REBOOT",    "RESTART" },
            { "PAUSE_SELECTION", "PLAYER SELECT" },
            { "PAUSE_EXIT",      "EXIT" },
            { "PAUSE_WIN",       "WINS!" },

            // ── Fim de partida ────────────────────────────────────────────
            { "FIM_EXIT",       "EXIT" },
            { "FIM_REBOOT",     "RESTART" },
            { "FIM_EMPATE",     "DRAW!" },
            { "FIM_VITORIA",    "VICTORY!" },

            // ── Round Win ─────────────────────────────────────────────────
            { "ROUND_LABEL",    "Round" },
            { "ROUND_WIN",      "won" },
            { "ROUND_VENCEDOR", "WINNER" },
            { "ROUND_DRAW",     "Draw" },

            // ── Créditos ──────────────────────────────────────────────────
            { "CREDITOS_DEV", "DEVELOPERS" },
            { "CREDITOS_DIS", "DESIGNER" },
            { "CREDITOS_CON", "CONCEPTS" },
        };
    }
}