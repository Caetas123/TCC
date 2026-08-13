using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Sistema visual dos botões.
///
/// NORMAL      = botão sem destaque
/// HIGHLIGHTED = botão atualmente selecionado no EventSystem (mouse, teclado
///               ou controle — todos passam pelo MESMO mecanismo)
/// SELECTED    = último botão realmente confirmado (clique ou Submit)
///
/// O Highlight NUNCA é controlado por uma variável própria: ele é sempre
/// derivado do OnSelect/OnDeselect, que a própria Unity garante disparar em
/// no máximo um objeto por vez (Deselect do antigo sempre acontece antes do
/// Select do novo). Por isso é IMPOSSÍVEL dois botões ficarem destacados ao
/// mesmo tempo, não importa se o destaque veio do mouse, do teclado ou do
/// controle — os três agora passam pelo mesmo caminho: o mouse só pede pra
/// Unity selecionar este botão (OnPointerEnter -> SetSelectedGameObject);
/// quem pinta o sprite é sempre o OnSelect/OnDeselect.
///
/// A Navigation do Unity não é alterada.
/// Os sprites são configurados manualmente no Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class ButtonVisualPersonalizado :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("Sprites")]

    [Tooltip("PNG normal deste botão.")]
    public Sprite normalSprite;

    [Tooltip("PNG quando este botão está em Highlight (mouse, teclado ou controle).")]
    public Sprite highlightedSprite;

    [Tooltip("PNG quando este botão é realmente confirmado.")]
    public Sprite selectedSprite;


    private Image imagem;
    private Button botao;


    // =========================================================
    // ESTADO GLOBAL
    // =========================================================

    // ÚNICO botão atualmente confirmado (clique real ou Submit). O Highlight
    // não precisa de uma variável equivalente — quem já garante isso é o
    // próprio EventSystem.currentSelectedGameObject.
    private static ButtonVisualPersonalizado selectedAtual;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        imagem = GetComponent<Image>();
        botao = GetComponent<Button>();

        // Desativa somente a transição visual automática.
        // A Navigation continua funcionando normalmente.
        botao.transition = Selectable.Transition.None;

        AtualizarVisual();
    }


    private void OnEnable()
    {
        AtualizarVisual();
    }


    private void OnDisable()
    {
        if (selectedAtual == this)
            selectedAtual = null;
    }


    // =========================================================
    // MOUSE
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!PodeInteragir())
            return;

        // O mouse só pede a seleção REAL — quem pinta é o OnSelect/OnDeselect,
        // igual ao teclado e ao controle. Nada de variável própria pro mouse.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (!PodeInteragir())
            return;

        // Clique REAL.
        DefinirSelected(this);
    }


    // =========================================================
    // TECLADO / CONTROLE (e também o mouse, via SetSelectedGameObject acima)
    // =========================================================

    public void OnSelect(BaseEventData eventData)
    {
        // A Unity garante que isto só dispara DEPOIS do OnDeselect do botão
        // anterior — nunca dois botões em Highlight ao mesmo tempo.
        PintarComoDestacado();
    }


    public void OnDeselect(BaseEventData eventData)
    {
        // Some o Highlight. Se este botão também for o Selected, o visual de
        // Selected continua (prioridade tratada dentro do próprio método).
        PintarComoNaoDestacado();
    }


    public void OnSubmit(BaseEventData eventData)
    {
        if (!PodeInteragir())
            return;

        // Enter / Space / A / X / botão de confirmação.
        DefinirSelected(this);
    }


    // =========================================================
    // DEFINIR SELECTED
    // =========================================================

    private static void DefinirSelected(ButtonVisualPersonalizado novoBotao)
    {
        if (novoBotao == null)
            return;

        ButtonVisualPersonalizado antigo = selectedAtual;
        selectedAtual = novoBotao;

        // Repinta o antigo (fora do OnSelect/OnDeselect, então é seguro
        // consultar o EventSystem diretamente pra saber se ele continua
        // destacado por Highlight).
        if (antigo != null && antigo != novoBotao)
            antigo.AtualizarVisual();

        novoBotao.AtualizarVisual();
    }


    // =========================================================
    // VISUAL
    // =========================================================

    // Usados DENTRO de OnSelect/OnDeselect: nesses dois métodos não dá pra
    // confiar em EventSystem.currentSelectedGameObject porque a Unity chama
    // Deselect do antigo ANTES de atualizar essa referência pro novo — então
    // aqui a prioridade Selected > Highlight > Normal é decidida direto.

    private void PintarComoDestacado()
    {
        if (imagem == null) return;

        imagem.sprite = selectedAtual == this
            ? (selectedSprite != null ? selectedSprite : normalSprite)
            : (highlightedSprite != null ? highlightedSprite : normalSprite);
    }

    private void PintarComoNaoDestacado()
    {
        if (imagem == null) return;

        imagem.sprite = selectedAtual == this
            ? (selectedSprite != null ? selectedSprite : normalSprite)
            : normalSprite;
    }

    // Usado fora do OnSelect/OnDeselect (Awake, OnEnable, mudança de
    // interactable, repintura do botão que perdeu o Selected) — aqui é seguro
    // consultar o EventSystem diretamente.
    private void AtualizarVisual()
    {
        if (imagem == null || botao == null)
            return;

        if (!botao.interactable)
        {
            imagem.sprite = normalSprite;
            return;
        }

        if (selectedAtual == this)
        {
            imagem.sprite = selectedSprite != null ? selectedSprite : normalSprite;
            return;
        }

        bool destacado = EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == gameObject;

        imagem.sprite = destacado
            ? (highlightedSprite != null ? highlightedSprite : normalSprite)
            : normalSprite;
    }


    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    private bool PodeInteragir()
    {
        return botao != null &&
               botao.interactable &&
               gameObject.activeInHierarchy;
    }
}