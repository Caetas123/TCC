using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SomBotaoUI : MonoBehaviour, ISelectHandler, IPointerEnterHandler,
    IPointerDownHandler, ISubmitHandler
{
    private Button botao;

    private void Awake()
    {
        botao = GetComponent<Button>();
    }

    private void OnEnable()
    {
        ReconectarClique();
    }

    private void OnDisable()
    {
        if (botao != null)
            botao.onClick.RemoveListener(AoClicar);
    }

    // Outros scripts recompõem onClick ao abrir painéis; a busca central
    // recoloca este listener também nos botões que acionam ações por código.
    public void ReconectarClique()
    {
        if (botao == null)
            botao = GetComponent<Button>();
        if (botao == null)
            return;

        botao.onClick.RemoveListener(AoClicar);
        botao.onClick.AddListener(AoClicar);
    }

    private bool PodeInteragir()
    {
        return botao != null && botao.interactable && gameObject.activeInHierarchy;
    }

    private void AoClicar()
    {
        if (PodeInteragir())
            SonsBotoesUI.TocarClique(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (PodeInteragir())
            SonsBotoesUI.TocarSelecao(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PodeInteragir())
            SonsBotoesUI.TocarSelecao(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (PodeInteragir() && eventData.button == PointerEventData.InputButton.Left)
            SonsBotoesUI.TocarClique(gameObject);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (PodeInteragir())
            SonsBotoesUI.TocarClique(gameObject);
    }
}
