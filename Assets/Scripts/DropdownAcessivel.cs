using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Padroniza a lista dos TMP_Dropdown usados pelos menus.
/// Mantém texto legível, foco visível, lista rolável e abertura para baixo.
/// </summary>
[DisallowMultipleComponent]
public sealed class DropdownAcessivel : MonoBehaviour
{
    private static readonly Color CorTexto = new Color(0.06f, 0.08f, 0.12f, 1f);
    private static readonly Color CorItemNormal = Color.white;
    private static readonly Color CorItemFoco = new Color(0.68f, 0.82f, 1f, 1f);
    private static readonly Color CorItemPressionado = new Color(0.32f, 0.56f, 0.9f, 1f);
    private static readonly Color CorItemSelecionado = new Color(0.24f, 0.43f, 0.75f, 1f);

    private TMP_Dropdown dropdown;
    private GameObject listaConfigurada;

    public static void Preparar(TMP_Dropdown alvo)
    {
        if (alvo == null)
            return;

        DropdownAcessivel componente = alvo.GetComponent<DropdownAcessivel>();
        if (componente == null)
            componente = alvo.gameObject.AddComponent<DropdownAcessivel>();

        componente.dropdown = alvo;
        componente.ConfigurarTemplate();
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
        ConfigurarTemplate();
    }

    private void LateUpdate()
    {
        if (dropdown == null)
            return;

        if (!dropdown.IsExpanded)
        {
            listaConfigurada = null;
            return;
        }

        GameObject lista = ObterListaAberta(dropdown);
        if (lista == null)
            return;

        if (lista != listaConfigurada)
        {
            ConfigurarListaInstanciada(lista);
            SelecionarItemAtual(lista);
        }

        listaConfigurada = lista;

        // O painel pode ser reposicionado pelo CanvasScaler durante a abertura.
        // Recalcular a posição em todos os frames mantém a lista presa ao dropdown,
        // em vez de deixá-la parada no ponto em que foi criada.
        PosicionarListaAbaixo(lista.transform as RectTransform);
    }

    private void ConfigurarTemplate()
    {
        if (dropdown == null || dropdown.template == null)
            return;

        RectTransform template = dropdown.template;
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);

        ScrollRect scroll = template.GetComponent<ScrollRect>();
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
        }

        ConfigurarImagensETextos(template);
    }

    private void ConfigurarListaInstanciada(GameObject lista)
    {
        RectTransform rect = lista.transform as RectTransform;
        if (rect != null)
        {
            Image fundo = rect.GetComponent<Image>();
            if (fundo != null)
                fundo.color = Color.white;

            ScrollRect scroll = rect.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 35f;
            }
        }

        ConfigurarImagensETextos(lista.transform);

        Toggle[] itens = lista.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < itens.Length; i++)
        {
            Toggle item = itens[i];
            if (item == null)
                continue;

            ColorBlock cores = item.colors;
            cores.normalColor = CorItemNormal;
            cores.highlightedColor = CorItemFoco;
            cores.pressedColor = CorItemPressionado;
            cores.selectedColor = CorItemSelecionado;
            cores.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            cores.colorMultiplier = 1f;
            cores.fadeDuration = 0.05f;
            item.colors = cores;

            // O TMP instancia os itens a partir do template. Em algumas versões do
            // pacote, o texto fica com o valor de exemplo ("Option A") ou perde a
            // cor durante o ClearOptions/AddOptions. Reaplicar o texto aqui garante
            // que a lista nunca apareça como um retângulo cinza vazio.
            if (dropdown != null && i < dropdown.options.Count)
            {
                TMP_Text rotulo = item.GetComponentInChildren<TMP_Text>(true);
                if (rotulo != null)
                {
                    rotulo.text = dropdown.options[i].text;
                    rotulo.color = CorTexto;
                    rotulo.alpha = 1f;
                    rotulo.gameObject.SetActive(true);
                }
            }

            // Navegação explícita vertical e circular, independente do layout
            // automático do EventSystem ou do pacote do TMP instalado.
            Navigation navigation = item.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = i > 0 ? itens[i - 1] : itens[itens.Length - 1];
            navigation.selectOnDown = i + 1 < itens.Length ? itens[i + 1] : itens[0];
            item.navigation = navigation;
        }

        CanvasGroup grupo = lista.GetComponent<CanvasGroup>();
        if (grupo != null)
            grupo.alpha = 1f;
    }

    private void SelecionarItemAtual(GameObject lista)
    {
        if (lista == null || dropdown == null || EventSystem.current == null)
            return;

        Toggle[] itens = lista.GetComponentsInChildren<Toggle>(true);
        if (itens.Length == 0)
            return;

        int indice = Mathf.Clamp(dropdown.value, 0, itens.Length - 1);
        if (itens[indice] == null ||
            !itens[indice].gameObject.activeInHierarchy)
            return;

        // O TMP cria a lista em runtime, mas em algumas versoes deixa o foco
        // no dropdown de origem. Selecionar o item atual garante que Cima/Baixo
        // navegue dentro da lista e que o ScrollRect acompanhe a selecao.
        EventSystem.current.SetSelectedGameObject(itens[indice].gameObject);
    }

    private static void ConfigurarImagensETextos(Transform raiz)
    {
        Image[] imagens = raiz.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imagens.Length; i++)
        {
            Image imagem = imagens[i];
            if (imagem == null)
                continue;

            string nome = imagem.gameObject.name.ToLowerInvariant();
            if (imagem.transform == raiz || nome.Contains("viewport") || nome.Contains("background"))
                imagem.color = Color.white;
        }

        TMP_Text[] textos = raiz.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < textos.Length; i++)
        {
            TMP_Text texto = textos[i];
            if (texto == null)
                continue;

            texto.color = CorTexto;
            texto.alpha = 1f;
            texto.enableWordWrapping = false;
        }
    }

    private void PosicionarListaAbaixo(RectTransform lista)
    {
        if (lista == null || dropdown == null)
            return;

        RectTransform origem = dropdown.transform as RectTransform;
        RectTransform pai = lista.parent as RectTransform;
        if (origem == null || pai == null)
            return;

        // A largura da lista deve acompanhar exatamente o controle que a abriu.
        lista.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origem.rect.width);

        Canvas canvas = dropdown.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3[] cantosOrigem = new Vector3[4];
        origem.GetWorldCorners(cantosOrigem);
        Vector3 centroInferior = (cantosOrigem[0] + cantosOrigem[1]) * 0.5f;
        Vector2 pontoLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pai,
                RectTransformUtility.WorldToScreenPoint(camera, centroInferior),
                camera,
                out pontoLocal))
            return;

        // Mantém a abertura sempre para baixo, mas limita a altura ao espaço que
        // realmente existe até a borda inferior do monitor. O conteúdo continua
        // maior dentro do ScrollRect e passa a rolar com roda, arraste ou teclado.
        float escalaCanvas = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        float yInferiorNaTela = RectTransformUtility.WorldToScreenPoint(camera, centroInferior).y;
        float espacoAbaixo = (Screen.height - yInferiorNaTela - 8f) / escalaCanvas;
        if (espacoAbaixo > 32f && lista.rect.height > espacoAbaixo)
            lista.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, espacoAbaixo);

        lista.anchorMin = Vector2.zero;
        lista.anchorMax = Vector2.zero;
        lista.pivot = new Vector2(0.5f, 1f);
        lista.anchoredPosition = pontoLocal + new Vector2(0f, -4f);
        lista.SetAsLastSibling();
    }
}
