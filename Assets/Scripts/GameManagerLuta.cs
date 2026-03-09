using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerLuta : MonoBehaviour
{
    [Header("Players da cena")]
    public SpriteRenderer player1Renderer;
    public SpriteRenderer player2Renderer;

    [Header("Sprites dos corpos")]
    public Sprite[] corposPersonagens;

    private string modoJogo;

    void Start()
    {
        // Lê modo de jogo
        modoJogo = PlayerPrefs.GetString("ModoJogo", "PVP");

        // Lê personagens escolhidos
        int personagemP1 = PlayerPrefs.GetInt("PersonagemP1", 0);
        int personagemP2 = PlayerPrefs.GetInt("PersonagemP2", 0);

        // Define sprite Player 1
        if (player1Renderer != null && personagemP1 >= 0 && personagemP1 < corposPersonagens.Length)
            player1Renderer.sprite = corposPersonagens[personagemP1];

        // Define sprite Player 2
        if (player2Renderer != null && personagemP2 >= 0 && personagemP2 < corposPersonagens.Length)
            player2Renderer.sprite = corposPersonagens[personagemP2];

        // ARENA
        string arena = PlayerPrefs.GetString("ArenaEscolhida", "cena1");

        if (arena == "aleatoria")
        {
            int sorteio = Random.Range(1, 3);
            arena = (sorteio == 1) ? "cena1" : "cena2";
        }

        Debug.Log("Modo de jogo: " + modoJogo);
        Debug.Log("P1: " + personagemP1 + " | P2: " + personagemP2);
        Debug.Log("Arena escolhida: " + arena);
    }
}