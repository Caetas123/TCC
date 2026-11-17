using UnityEngine;

public class ConfiguracaoSonsBotoes : ScriptableObject
{
    public AudioClip somSelecionar;
    public AudioClip somClicar;
    [Range(0f, 1f)] public float volumeSelecionar = 0.6f;
    [Range(0f, 1f)] public float volumeClicar = 0.8f;
}
