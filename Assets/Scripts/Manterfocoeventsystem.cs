using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Garante que o EventSystem nunca perca o foco após navegação por teclado.
/// NÃO interfere com o hover do mouse — deixa o Sprite Swap do Unity funcionar normalmente.
/// </summary>
public class ManterFocoEventSystem : MonoBehaviour
{
    private GameObject ultimoSelecionado;
    private bool navegandoPorTeclado = false;

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
            if (Input.GetKeyDown(tecla))
            {
                navegandoPorTeclado = true;
                break;
            }
        }

        // Detecta se o mouse se moveu — desativa modo teclado
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
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