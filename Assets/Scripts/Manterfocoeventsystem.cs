using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Garante que o EventSystem nunca perca o foco após navegação por teclado.
/// NÃO interfere com o hover do mouse — deixa o Sprite Swap do Unity funcionar normalmente.
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

        // Alguns menus usam o Input System novo e outros ainda possuem scripts
        // legados. Quando o evento de navegacao nativo nao chega ao EventSystem,
        // fazemos a mesma troca de foco diretamente entre os Selectables.
        if (!mouseMoveu)
        {
            if (UIInputUtility.WasKeyPressed(KeyCode.UpArrow) || UIInputUtility.WasKeyPressed(KeyCode.W))
                MoverFoco(KeyCode.UpArrow);
            else if (UIInputUtility.WasKeyPressed(KeyCode.DownArrow) || UIInputUtility.WasKeyPressed(KeyCode.S))
                MoverFoco(KeyCode.DownArrow);
            else if (UIInputUtility.WasKeyPressed(KeyCode.LeftArrow) || UIInputUtility.WasKeyPressed(KeyCode.A))
                MoverFoco(KeyCode.LeftArrow);
            else if (UIInputUtility.WasKeyPressed(KeyCode.RightArrow) || UIInputUtility.WasKeyPressed(KeyCode.D))
                MoverFoco(KeyCode.RightArrow);
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

    private void MoverFoco(KeyCode tecla)
    {
        navegandoPorTeclado = true;

        var objetoAtual = EventSystem.current.currentSelectedGameObject;
        if (objetoAtual == null)
            objetoAtual = ultimoSelecionado;

        if (objetoAtual == null)
            return;

        var atual = objetoAtual.GetComponent<Selectable>();
        if (atual == null || !atual.interactable || !atual.gameObject.activeInHierarchy)
            return;

        Selectable proximo = null;
        switch (tecla)
        {
            case KeyCode.UpArrow:
                proximo = atual.FindSelectableOnUp();
                break;
            case KeyCode.DownArrow:
                proximo = atual.FindSelectableOnDown();
                break;
            case KeyCode.LeftArrow:
                proximo = atual.FindSelectableOnLeft();
                break;
            case KeyCode.RightArrow:
                proximo = atual.FindSelectableOnRight();
                break;
        }

        if (proximo != null && proximo.interactable && proximo.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(proximo.gameObject);
            ultimoSelecionado = proximo.gameObject;
        }
    }
}
