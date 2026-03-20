using System;
using System.Collections.Generic;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    public enum Language
    {
        Portuguese,
        English
    }

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
    }

    void SetupDictionaries()
    {
        pt = new Dictionary<string, string>()
        {
            { "MENU_GAME_MODE", "MODO DE JOGO" },
            { "MENU_CREDITS", "CRÉDITOS" },
            { "MENU_EXIT", "SAIR" },

            { "EXIT_CONFIRM", "Deseja realmente sair?" },
            { "EXIT_YES", "SIM" },
            { "EXIT_NO", "NÃO" },

            { "SETTINGS_TITLE", "CONFIGURAÇÕES" },
            { "SETTINGS_AUDIO", "ÁUDIO" },
            { "SETTINGS_VIDEO", "VÍDEO" },
            { "SETTINGS_CONTROLS", "CONTROLE" },
            { "SETTINGS_BACK", "VOLTAR" },

            { "AUDIO_MASTER", "Volume Geral" },
            { "AUDIO_MUSIC", "Volume Música" },
            { "AUDIO_SFX", "Volume Efeitos" },
            { "AUDIO_MUTE", "Sem Áudio" },

            { "VIDEO_RESOLUTION", "Resolução" },
            { "VIDEO_MODE", "Modo de Tela" },
            { "VIDEO_APPLY", "Aplicar" },
            { "VIDEO_RESTORE", "Restaurar" },

            { "PLAYER1", "Jogador 1" },
            { "PLAYER2", "Jogador 2" },

            { "BTN_DIREITA", "DIREITA" },
            { "BTN_ESQUERDA", "ESQUERDA" },
            { "BTN_PULO", "PULAR" },
            { "BTN_ATAQUE", "ATAQUE" },
            { "BTN_ESPECIAL", "ESPECIAL" },

            { "MODE_TITLE", "MODO DE JOGO" },
            { "MODE_PVP", "Jogador vs Jogador" },
            { "MODE_CPU", "Jogador vs CPU" },
            { "MODE_CPU_CPU", "CPU vs CPU" },

            { "SELECT_PLAYER", "SELEÇÃO DE JOGADOR" },
            { "START_GAME", "INICIAR" },

            { "ARENA_TITLE", "Arena do Jogo" },
            { "ARENA_SCHOOL", "Pátio da Etec" },
            { "ARENA_CLASS", "Sala de Aula" },

            { "PAUSE_TITLE", "MENU DE JOGO" },
            { "PAUSE_CONT", "CONTINUAR" },
            { "PAUSE_REBOOT", "REINICIAR" },
            { "PAUSE_SELECTION", "SELEÇÃO JOGADOR" },
            { "PAUSE_EXIT", "SAIR" },
        };

        en = new Dictionary<string, string>()
        {
            { "MENU_GAME_MODE", "GAME MODE" },
            { "MENU_CREDITS", "CREDITS" },
            { "MENU_EXIT", "EXIT" },

            { "EXIT_CONFIRM", "Do you really want to exit?" },
            { "EXIT_YES", "YES" },
            { "EXIT_NO", "NO" },

            { "SETTINGS_TITLE", "SETTINGS" },
            { "SETTINGS_AUDIO", "AUDIO" },
            { "SETTINGS_VIDEO", "VIDEO" },
            { "SETTINGS_CONTROLS", "CONTROLS" },
            { "SETTINGS_BACK", "BACK" },

            { "AUDIO_MASTER", "Master Volume" },
            { "AUDIO_MUSIC", "Music Volume" },
            { "AUDIO_SFX", "SFX Volume" },
            { "AUDIO_MUTE", "Mute" },

            { "VIDEO_RESOLUTION", "Resolution" },
            { "VIDEO_MODE", "Screen Mode" },
            { "VIDEO_APPLY", "Apply" },
            { "VIDEO_RESTORE", "Restore" },

            { "PLAYER1", "Player 1" },
            { "PLAYER2", "Player 2" },
    
            { "BTN_DIREITA", "RIGHT" },
            { "BTN_ESQUERDA", "LEFT" },
            { "BTN_PULO", "JUMP" },
            { "BTN_ATAQUE", "ATTACK" },
            { "BTN_ESPECIAL", "SPECIAL" },

            { "MODE_TITLE", "GAME MODE" },
            { "MODE_PVP", "Player vs Player" },
            { "MODE_CPU", "Player vs CPU" },
            { "MODE_CPU_CPU", "CPU vs CPU" },

            { "SELECT_PLAYER", "PLAYER SELECT" },
            { "START_GAME", "START" },

            { "ARENA_TITLE", "Game Arena" },
            { "ARENA_SCHOOL", "School Yard" },
            { "ARENA_CLASS", "Classroom" },

            { "PAUSE_TITLE", "GAME MENU" },
            { "PAUSE_CONT", "CONTINUE" },
            { "PAUSE_REBOOT", "RESTART" },
            { "PAUSE_SELECTION", "PLAYER SELECT" },
            { "PAUSE_EXIT", "EXIT" },
        };
    }

    public string GetText(string key)
    {
        var dict = CurrentLanguage == Language.Portuguese ? pt : en;

        if (dict.ContainsKey(key))
            return dict[key];

        return key;
    }

    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == Language.Portuguese
            ? Language.English
            : Language.Portuguese;

        OnLanguageChanged?.Invoke();
    }
}