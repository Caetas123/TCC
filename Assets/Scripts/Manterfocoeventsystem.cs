using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Garante que o EventSystem nunca perca o foco após navegação por teclado.
/// NÃO interfere com o hover do mouse — deixa o Sprite Swap do Unity funcionar normalmente.
///
/// A navegação direcional fica exclusivamente com o InputSystemUIInputModule.
/// Este componente só restaura o foco quando algum painel o perde. Antes ele
/// também chamava FindSelectableOn* manualmente, fazendo a mesma tecla ser
/// processada duas vezes e pulando um botão.
/// </summary>
public class ManterFocoEventSystem : MonoBehaviour
{
    private GameObject ultimoSelecionado;
    private bool navegandoPorTeclado = false;

    private void Awake()
    {
        // Este objeto deve ser apenas o EventSystem. Em algumas cenas ele tambem
        // possuiu um Button acidental, que entrava na rota de navegacao.
        var botaoDoEventSystem = GetComponent<Button>();
        if (botaoDoEventSystem != null)
        {
            botaoDoEventSystem.interactable = false;

            var navegacao = botaoDoEventSystem.navigation;
            navegacao.mode = Navigation.Mode.None;
            botaoDoEventSystem.navigation = navegacao;
        }
    }

    // Teclas de navegação — quando usadas, ativa modo teclado
    private static readonly KeyCode[] teclasNavegacao =
    {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
        KeyCode.Tab, KeyCode.Return, KeyCode.KeypadEnter
    };

    void Update()
    {
        if (EventSystem.current == null) return;

        // A tela refatorada possui rotas independentes para P1 e P2. A
        // navegação global deste componente faria W/S e setas atravessarem os
        // controles do outro jogador.
        if (FindObjectOfType<SelecaoPlayerNavigation>(true) != null)
            return;

        // Detecta se o usuário está usando o teclado para navegar
        foreach (KeyCode tecla in teclasNavegacao)
        {
            if (UIInputUtility.WasKeyPressed(tecla))
            {
                navegandoPorTeclado = true;
                break;
            }
        }

        // Detecta se o mouse se moveu — desativa modo teclado
        bool mouseMoveu = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            mouseMoveu = Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;
        }
        catch (System.InvalidOperationException)
        {
            // O projeto também pode estar usando somente o Input System novo.
        }
#endif
        if (!mouseMoveu && Mouse.current != null)
            mouseMoveu = Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        if (mouseMoveu)
        {
            navegandoPorTeclado = false;
        }

        // Atualiza o último selecionado sempre que houver foco
        var atual = EventSystem.current.currentSelectedGameObject;
        if (atual != null)
        {
            ultimoSelecionado = atual;
        }
        else if (navegandoPorTeclado && ultimoSelecionado != null)
        {
            // Só restaura o foco se estiver em modo teclado
            // Verifica se o objeto ainda existe e é interagível
            var selectable = ultimoSelecionado.GetComponent<Selectable>();
            if (selectable != null && selectable.interactable && selectable.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(ultimoSelecionado);
            }
            else
            {
                ultimoSelecionado = null;
            }
        }
    }

}
