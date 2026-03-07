using UnityEngine;

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
        modoJogo = PlayerPrefs.GetString("ModoJogo", "PVP");

        int personagemP1 = PlayerPrefs.GetInt("PersonagemP1", 0);
        int personagemP2 = PlayerPrefs.GetInt("PersonagemP2", 0);

        if (player1Renderer != null && personagemP1 >= 0 && personagemP1 < corposPersonagens.Length)
            player1Renderer.sprite = corposPersonagens[personagemP1];

        if (player2Renderer != null && personagemP2 >= 0 && personagemP2 < corposPersonagens.Length)
            player2Renderer.sprite = corposPersonagens[personagemP2];

        Debug.Log("Modo de jogo: " + modoJogo);
        Debug.Log("P1: " + personagemP1 + " | P2: " + personagemP2);
    }
}