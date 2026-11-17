using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Troca o sprite de uma Image automaticamente quando o idioma muda.
/// Funciona igual ao LocalizedText, mas para imagens.
/// Adicione este script no mesmo GameObject que tem o componente Image.
/// </summary>
public class LocalizedImage : MonoBehaviour
{
    [Header("Sprites por idioma")]
    [Tooltip("Sprite exibido quando o idioma é Português")]
    public Sprite spritePT;

    [Tooltip("Sprite exibido quando o idioma é Inglês")]
    public Sprite spriteEN;

    [Tooltip("Sprite de empate exibido quando o idioma é Português")]
    public Sprite spriteEmpatePT;

    [Tooltip("Sprite de empate exibido quando o idioma é Inglês")]
    public Sprite spriteEmpateEN;

    private Image imagem;
    private bool exibindoEmpate;

    private void Awake()
    {
        imagem = GetComponent<Image>();
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += AtualizarImagem;
        AtualizarImagem();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= AtualizarImagem;
    }

    private void AtualizarImagem()
    {
        if (imagem == null) return;
        if (LanguageManager.Instance == null) return;

        bool ehPortugues = LanguageManager.Instance.CurrentLanguage
                           == LanguageManager.Language.Portuguese;

        Sprite alvo;
        if (exibindoEmpate)
            alvo = ehPortugues ? spriteEmpatePT : spriteEmpateEN;
        else
            alvo = ehPortugues ? spritePT : spriteEN;

        if (alvo != null)
            imagem.sprite = alvo;
    }

    public void DefinirEmpate(bool empate)
    {
        exibindoEmpate = empate;
        AtualizarImagem();
    }
}
