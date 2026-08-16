using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Sistema visual dos botões.
///
/// NORMAL      = botão sem destaque
/// HIGHLIGHTED = botão atualmente selecionado no EventSystem (mouse, teclado
///               ou controle — todos passam pelo MESMO mecanismo)
/// SELECTED    = flash rápido no instante do clique/Submit (duração
///               configurável em duracaoFlashSelected) — depois volta
///               sozinho pro estado real: Highlighted se o foco continuar
///               nele, Normal se não.
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
/// O SELECTED NÃO fica gravado até outro botão ser clicado — clicar um botão
/// e depois só navegar/passar o mouse por outros não deixa mais nenhum
/// "amarelo preso" no botão antigo.
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

    [Tooltip("PNG quando este botão é clicado/confirmado — aparece por um instante só.")]
    public Sprite selectedSprite;

    [Header("Flash do Selected")]
    [Tooltip("Quanto tempo (em segundos) o sprite Selected fica visível após o clique/Submit, antes de voltar sozinho pro estado real de foco.")]
    public float duracaoFlashSelected = 0.15f;


    private Image imagem;
    private Button botao;
    private Coroutine flashEmAndamento;


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
        if (flashEmAndamento != null)
        {
            StopCoroutine(flashEmAndamento);
            flashEmAndamento = null;
        }
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

        // Clique REAL — dispara o flash.
        IniciarFlashSelected();
    }


    // =========================================================
    // TECLADO / CONTROLE (e também o mouse, via SetSelectedGameObject acima)
    // =========================================================

    public void OnSelect(BaseEventData eventData)
    {
        // A Unity garante que isto só dispara DEPOIS do OnDeselect do botão
        // anterior — nunca dois botões em Highlight ao mesmo tempo.
        // Se tiver um flash de Selected rolando, ele continua — só não
        // interrompe aqui, quem decide quando parar é o próprio flash.
        if (flashEmAndamento == null)
            PintarComoDestacado();
    }


    public void OnDeselect(BaseEventData eventData)
    {
        // Some o Highlight. Se tiver um flash de Selected rolando, deixa ele
        // terminar sozinho — ele mesmo repinta pro estado certo no final.
        if (flashEmAndamento == null)
            PintarComoNaoDestacado();
    }


    public void OnSubmit(BaseEventData eventData)
    {
        if (!PodeInteragir())
            return;

        // Enter / Space / A / X / botão de confirmação — mesmo flash do clique.
        IniciarFlashSelected();
    }


    // =========================================================
    // FLASH DO SELECTED
    // =========================================================

    private void IniciarFlashSelected()
    {
        if (flashEmAndamento != null)
            StopCoroutine(flashEmAndamento);

        flashEmAndamento = StartCoroutine(FlashSelectedRotina());
    }

    private System.Collections.IEnumerator FlashSelectedRotina()
    {
        if (imagem != null)
            imagem.sprite = selectedSprite != null ? selectedSprite : normalSprite;

        // Realtime, não Time.deltaTime — assim o flash funciona certinho mesmo em
        // telas que pausam o jogo (Time.timeScale = 0), como o menu de pause.
        yield return new WaitForSecondsRealtime(duracaoFlashSelected);

        flashEmAndamento = null;

        // Depois do flash, volta pro estado real: Highlighted se o foco ainda
        // estiver aqui, Normal se não — nunca mais "preso" no Selected.
        AtualizarVisual();
    }


    // =========================================================
    // VISUAL
    // =========================================================

    // Usados DENTRO de OnSelect/OnDeselect: nesses dois métodos não dá pra
    // confiar em EventSystem.currentSelectedGameObject porque a Unity chama
    // Deselect do antigo ANTES de atualizar essa referência pro novo — então
    // aqui a prioridade Highlight > Normal é decidida direto (Selected nunca
    // entra aqui: enquanto o flash está ativo, OnSelect/OnDeselect nem chamam
    // esses dois métodos — ver acima).

    private void PintarComoDestacado()
    {
        if (imagem == null) return;
        imagem.sprite = highlightedSprite != null ? highlightedSprite : normalSprite;
    }

    private void PintarComoNaoDestacado()
    {
        if (imagem == null) return;
        imagem.sprite = normalSprite;
    }

    // Usado fora do OnSelect/OnDeselect (Awake, OnEnable, mudança de
    // interactable, fim do flash) — aqui é seguro consultar o EventSystem
    // diretamente.
    private void AtualizarVisual()
    {
        if (imagem == null || botao == null)
            return;

        if (!botao.interactable)
        {
            imagem.sprite = normalSprite;
            return;
        }

        // Enquanto o flash está rolando, ele já pintou o sprite certo — não
        // sobrescreve por cima (evita "piscar" pro Highlighted no meio do flash
        // se algo mais chamar AtualizarVisual nesse meio tempo).
        if (flashEmAndamento != null)
            return;

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