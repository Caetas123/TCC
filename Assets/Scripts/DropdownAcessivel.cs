using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Mantem o TMP_Dropdown com o template e o visual padrao da Unity.
/// A unica funcao extra e acompanhar o item focado pelo teclado e deslocar o
/// ScrollRect apenas quando esse item sair da area visivel.
/// </summary>
[DisallowMultipleComponent]
public sealed class DropdownAcessivel : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private GameObject listaAtual;
    private ScrollRect scroll;
    private RectTransform viewport;
    private RectTransform content;
    private Toggle[] itens;

    public static void Preparar(TMP_Dropdown alvo)
    {
        if (alvo == null)
            return;

        DropdownAcessivel componente = alvo.GetComponent<DropdownAcessivel>();
        if (componente == null)
            componente = alvo.gameObject.AddComponent<DropdownAcessivel>();

        componente.dropdown = alvo;
    }

    public static GameObject ObterListaAberta(TMP_Dropdown alvo)
    {
        if (alvo == null || alvo.template == null || alvo.template.parent == null)
            return null;

        Transform pai = alvo.template.parent;
        for (int i = 0; i < pai.childCount; i++)
        {
            Transform filho = pai.GetChild(i);
            if (filho != null && filho.name == "Dropdown List" && filho.gameObject.activeInHierarchy)
                return filho.gameObject;
        }

        return null;
    }

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void LateUpdate()
    {
        if (dropdown == null)
            return;

        if (!dropdown.IsExpanded)
        {
            LimparListaAtual();
            return;
        }

        GameObject lista = ObterListaAberta(dropdown);
        if (lista == null)
            return;

        if (lista != listaAtual)
        {
            PrepararListaAberta(lista);
            SelecionarItemAtual();
        }

        AcompanharItemFocado();
    }

    private void PrepararListaAberta(GameObject lista)
    {
        listaAtual = lista;
        scroll = lista.GetComponent<ScrollRect>();
        viewport = scroll != null ? scroll.viewport : null;
        content = scroll != null ? scroll.content : null;
        itens = lista.GetComponentsInChildren<Toggle>(true);

        // O template original ja define todos os valores visuais e de layout.
        // Aqui nao alteramos cores, textos, tamanho, posicionamento ou navegacao.
    }

    private void SelecionarItemAtual()
    {
        if (EventSystem.current == null || itens == null || itens.Length == 0)
            return;

        int indice = Mathf.Clamp(dropdown.value, 0, itens.Length - 1);
        Toggle item = itens[indice];
        if (item == null || !item.gameObject.activeInHierarchy)
            return;

        // O TMP_Dropdown normalmente deixa o foco no controle que abriu a lista.
        // Colocar o foco no item atual faz Cima/Baixo navegarem pela lista desde
        // a primeira tecla e permite que o ScrollRect acompanhe a selecao.
        EventSystem.current.SetSelectedGameObject(item.gameObject);
        GarantirItemVisivel(item.transform as RectTransform);
    }

    private void AcompanharItemFocado()
    {
        if (EventSystem.current == null || itens == null)
            return;

        GameObject selecionado = EventSystem.current.currentSelectedGameObject;
        if (selecionado == null || !selecionado.transform.IsChildOf(listaAtual.transform))
            return;

        Toggle item = selecionado.GetComponent<Toggle>();
        if (item != null)
            GarantirItemVisivel(item.transform as RectTransform);
    }

    private void GarantirItemVisivel(RectTransform item)
    {
        if (scroll == null || viewport == null || content == null || item == null ||
            !scroll.vertical || content.rect.height <= viewport.rect.height)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
        Rect viewRect = viewport.rect;
        float deslocamento = 0f;

        if (itemBounds.max.y > viewRect.yMax)
            deslocamento = viewRect.yMax - itemBounds.max.y;
        else if (itemBounds.min.y < viewRect.yMin)
            deslocamento = viewRect.yMin - itemBounds.min.y;

        if (Mathf.Abs(deslocamento) < 0.01f)
            return;

        content.anchoredPosition += new Vector2(0f, deslocamento);
        scroll.StopMovement();
    }

    private void LimparListaAtual()
    {
        listaAtual = null;
        scroll = null;
        viewport = null;
        content = null;
        itens = null;
    }
}
