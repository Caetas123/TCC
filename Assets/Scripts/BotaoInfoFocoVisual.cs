using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Feedback visual específico dos botões de informação da seleção de jogador.
/// O foco mantém a arte normal e acrescenta uma borda; amarelo é reservado ao
/// pressionamento/confirmação.
/// </summary>
[DisallowMultipleComponent]
public sealed class BotaoInfoFocoVisual : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private Image imagem;
    private Outline borda;
    private Sprite spriteNormal;
    private Sprite spritePressionado;
    private Color corNormal = Color.white;
    private Color corPressionada = new Color(1f, 0.82f, 0.18f, 1f);
    private bool focoDaNavegacao;
    private bool apontado;
    private bool selecionadoPeloEventSystem;
    private bool pressionado;

    public void Configurar(Image imagemBotao, Sprite normal, Sprite amarelo, Color corDaBorda)
    {
        imagem = imagemBotao != null ? imagemBotao : GetComponent<Image>();
        if (imagem == null)
            return;

        spriteNormal = normal != null ? normal : imagem.sprite;
        spritePressionado = amarelo;
        corNormal = imagem.color;

        borda = GetComponent<Outline>();
        if (borda == null)
            borda = gameObject.AddComponent<Outline>();

        corDaBorda.a = 1f;
        borda.effectColor = corDaBorda;
        borda.effectDistance = new Vector2(4f, 4f);
        borda.useGraphicAlpha = false;

        Button botao = GetComponent<Button>();
        if (botao != null)
        {
            botao.targetGraphic = imagem;
            botao.transition = Selectable.Transition.None;
        }

        AtualizarVisual();
    }

    public void DefinirFocoDaNavegacao(bool focado)
    {
        focoDaNavegacao = focado;
        AtualizarVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        apontado = true;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
        AtualizarVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        apontado = false;
        AtualizarVisual();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selecionadoPeloEventSystem = true;
        AtualizarVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selecionadoPeloEventSystem = false;
        pressionado = false;
        AtualizarVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressionado = true;
        AtualizarVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressionado = false;
        AtualizarVisual();
    }

    private void AtualizarVisual()
    {
        if (imagem == null)
            return;

        bool exibirBorda = focoDaNavegacao || apontado || selecionadoPeloEventSystem;
        if (borda != null)
            borda.enabled = exibirBorda;

        imagem.sprite = pressionado && spritePressionado != null
            ? spritePressionado
            : spriteNormal;
        imagem.color = pressionado && spritePressionado == null
            ? corPressionada
            : corNormal;
    }
}
